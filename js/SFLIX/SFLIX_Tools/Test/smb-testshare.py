#!/usr/bin/env python3
"""Lokaler SMB-Testserver für die STEFFLIX-App im Emulator (nur Testdaten, übernommen aus Sync2PC).

Startet mit impacket einen SMB2-Server, der einen Ordner als Freigabe anbietet. Wird von
SFLIX_Tools\\Test\\smb-testshare.ps1 gestartet (venv, adb reverse; Freigabe immer .claude\\testenv, nur lesend).
Aufruf von Hand:
    python smb-testshare.py --dir <Ordner> [--share STEFFLIX] [--user stefflix] [--password stefflix-test-1] [--port 47123]
        [--readonly] [--throttle-kbps n] [--drop-after-bytes n [--drop-count k]]
(Port 47123, DEC-340: deutlich getrennt von den Ports anderer Projekte, z. B. 4445/4446 von Sync2PC.)
"""
import argparse
import logging
import os
import sys
import threading
import time

from impacket import ntlm, smbserver
from impacket import smb3structs as smb2
from impacket.nt_errors import (
    STATUS_END_OF_FILE, STATUS_INVALID_HANDLE, STATUS_NO_SUCH_FILE, STATUS_OBJECT_NAME_COLLISION,
    STATUS_OBJECT_NAME_NOT_FOUND, STATUS_OBJECT_PATH_SYNTAX_BAD, STATUS_ACCESS_DENIED, STATUS_SUCCESS,
)

STATUS_SMB_BAD_TID = smbserver.STATUS_SMB_BAD_TID


def patch_windows_behaviour() -> None:
    """Gleicht Abweichungen von impacket gegenüber Windows aus (aus Sync2PC), nur Testserver:
    1. Nicht vorhandene Datei: STATUS_OBJECT_NAME_NOT_FOUND statt STATUS_NO_SUCH_FILE (smbj erkennt sonst "fehlt" nicht).
    2. Lesen am Dateiende: STATUS_END_OF_FILE mit Fehler-Antwort (smbj: BufferException Underflow sonst).
    3. Rename mit geschlossenem Handle (Windows: os.rename scheitert bei offenem Handle).
    4. Handles abgebrochener Verbindungen schliessen.
    """
    cmds = smbserver.SMB2Commands

    orig_create = cmds.smb2Create

    def create(connId, smbServer, recvPacket):
        resp, extra, code = orig_create(connId, smbServer, recvPacket)
        if code == STATUS_NO_SUCH_FILE:
            code = STATUS_OBJECT_NAME_NOT_FOUND
        return resp, extra, code

    cmds.smb2Create = staticmethod(create)

    orig_read = cmds.smb2Read

    def read(connId, smbServer, recvPacket):
        resp, extra, code = orig_read(connId, smbServer, recvPacket)
        if code == STATUS_END_OF_FILE:
            return [smb2.SMB2Error()], extra, code
        return resp, extra, code

    cmds.smb2Read = staticmethod(read)

    orig_setinfo = cmds.smb2SetInfo

    def setinfo(connId, smbServer, recvPacket):
        try:
            info = smb2.SMB2SetInfo(recvPacket["Data"])
            is_rename = info["InfoType"] == smb2.SMB2_0_INFO_FILE and info["FileInfoClass"] == smb2.SMB2_FILE_RENAME_INFO
        except Exception:
            is_rename = False
        if not is_rename:
            return orig_setinfo(connId, smbServer, recvPacket)
        return rename(connId, smbServer, recvPacket, info)

    def rename(connId, smbServer, recvPacket, info):
        conn = smbServer.getConnectionData(connId)
        file_id = info["FileID"].getData()
        if file_id == b"\xff" * 16 and "SMB2_CREATE" in conn["LastRequest"]:
            file_id = conn["LastRequest"]["SMB2_CREATE"]["FileID"]
        tid = recvPacket["TreeID"]
        if tid not in conn["ConnectedShares"]:
            return [smb2.SMB2Error()], None, STATUS_SMB_BAD_TID
        share = conn["ConnectedShares"][tid]
        if file_id not in conn["OpenedFiles"]:
            return [smb2.SMB2Error()], None, STATUS_INVALID_HANDLE
        opened = conn["OpenedFiles"][file_id]
        root = share["path"]
        rinfo = smb2.FILE_RENAME_INFORMATION_TYPE_2(info["Buffer"])
        new_name = smbserver.normalize_path(rinfo["FileName"].decode("utf-16le"))
        new_path = os.path.join(root, new_name)
        if not smbserver.isInFileJail(root, new_name):
            return [smb2.SMB2Error()], None, STATUS_OBJECT_PATH_SYNTAX_BAD
        if rinfo["ReplaceIfExists"] == 0 and os.path.exists(new_path):
            return [smb2.SMB2Error()], None, STATUS_OBJECT_NAME_COLLISION
        if share["read only"] == "yes":
            return [smb2.SMB2Error()], None, STATUS_ACCESS_DENIED
        old_path = opened["FileName"]
        handle = opened["FileHandle"]
        had_handle = handle not in (smbserver.VOID_FILE_DESCRIPTOR, smbserver.PIPE_FILE_DESCRIPTOR)
        try:
            if had_handle:
                os.close(handle)
            os.replace(old_path, new_path)
            opened["FileName"] = new_path
            if had_handle:
                opened["FileHandle"] = os.open(new_path, os.O_RDWR | getattr(os, "O_BINARY", 0))
        except Exception as e:
            smbServer.log("smb2SetInfo(rename): %s" % e, logging.ERROR, connData=conn)
            if had_handle:
                try:
                    opened["FileHandle"] = os.open(opened["FileName"], os.O_RDWR | getattr(os, "O_BINARY", 0))
                except Exception:
                    opened["FileHandle"] = smbserver.VOID_FILE_DESCRIPTOR
            return [smb2.SMB2Error()], None, STATUS_ACCESS_DENIED
        smbServer.setConnectionData(connId, conn)
        return [smb2.SMB2SetInfo_Response()], None, STATUS_SUCCESS

    cmds.smb2SetInfo = staticmethod(setinfo)

    orig_remove = smbserver.SMBSERVER.removeConnection

    def remove_connection(self, name):
        try:
            conn = self.getConnectionData(name, checkStatus=False)
            for opened in list(conn.get("OpenedFiles", {}).values()):
                handle = opened.get("FileHandle")
                if handle not in (None, smbserver.VOID_FILE_DESCRIPTOR, smbserver.PIPE_FILE_DESCRIPTOR):
                    try:
                        os.close(handle)
                    except Exception:
                        pass
        except Exception:
            pass
        return orig_remove(self, name)

    smbserver.SMBSERVER.removeConnection = remove_connection


def patch_binary_open() -> None:
    """impacket öffnet Dateien mit os.open(pfad, modus) ohne O_BINARY. Unter Windows ist das der
    Textmodus: Byte 0x1A gilt als Dateiende, Bilder und Videos kämen abgeschnitten an. O_BINARY ergänzen."""
    binary = getattr(os, "O_BINARY", 0)
    if not binary:
        return
    orig_open = os.open

    def open_binary(path, flags, *args, **kwargs):
        return orig_open(path, flags | binary, *args, **kwargs)

    os.open = open_binary


def patch_readonly() -> None:
    """Nur lesen: Schreiben und Ändern werden abgelehnt (zusätzlich zu "read only" der Freigabe)."""
    cmds = smbserver.SMB2Commands

    def deny(connId, smbServer, recvPacket):
        return [smb2.SMB2Error()], None, STATUS_ACCESS_DENIED

    cmds.smb2Write = staticmethod(deny)
    orig_setinfo = cmds.smb2SetInfo

    def setinfo(connId, smbServer, recvPacket):
        return deny(connId, smbServer, recvPacket)

    cmds.smb2SetInfo = staticmethod(setinfo)


def patch_throttle_and_drop(throttle_kbps: int, drop_after_bytes: int, drop_count: int) -> None:
    """Testhilfen für reproduzierbare Abbrüche: Datenrate bremsen bzw. Verbindung nach n Bytes hart trennen."""
    cmds = smbserver.SMB2Commands
    lock = threading.Lock()
    transferred = {}
    doomed = set()
    state = {"dropped": 0}

    def account(conn_id, nbytes):
        if nbytes > 0 and throttle_kbps > 0:
            time.sleep(nbytes / (throttle_kbps * 1024.0))
        if drop_after_bytes > 0 and nbytes > 0:
            with lock:
                transferred[conn_id] = transferred.get(conn_id, 0) + nbytes
                if transferred[conn_id] >= drop_after_bytes and state["dropped"] < drop_count and conn_id not in doomed:
                    doomed.add(conn_id)
                    state["dropped"] += 1
                    print(f"[test] Verbindung {conn_id} wird nach {transferred[conn_id]} Bytes getrennt "
                          f"({state['dropped']}/{drop_count})", flush=True)

    orig_read = cmds.smb2Read

    def read(connId, smbServer, recvPacket):
        resp, extra, code = orig_read(connId, smbServer, recvPacket)
        try:
            n = int(resp[0]["DataLength"]) if code == STATUS_SUCCESS else 0
        except Exception:
            n = 0
        account(connId, n)
        return resp, extra, code

    cmds.smb2Read = staticmethod(read)
    orig_process = smbserver.SMBSERVER.processRequest

    def process(self, connId, data):
        resp = orig_process(self, connId, data)
        if connId in doomed:
            doomed.discard(connId)
            raise ConnectionAbortedError("Testabbruch (--drop-after-bytes)")
        return resp

    smbserver.SMBSERVER.processRequest = process


def main() -> int:
    ap = argparse.ArgumentParser(description="SMB-Testfreigabe für die STEFFLIX-App")
    ap.add_argument("--dir", required=True, help="Ordner, der als Freigabe bereitgestellt wird (.claude\\testenv)")
    ap.add_argument("--share", default="STEFFLIX")
    ap.add_argument("--user", default="stefflix")
    ap.add_argument("--password", default="stefflix-test-1", help="öffentliches Testpasswort (nur Testdaten)")
    ap.add_argument("--port", type=int, default=47123, help="lokaler Port (Windows belegt 445; DEC-340, nicht in der Nähe anderer Testserver)")
    ap.add_argument("--address", default="127.0.0.1", help="nur localhost")
    ap.add_argument("--readonly", action="store_true", help="nur lesen")
    ap.add_argument("--throttle-kbps", type=int, default=0)
    ap.add_argument("--drop-after-bytes", type=int, default=0)
    ap.add_argument("--drop-count", type=int, default=1)
    args = ap.parse_args()

    if not os.path.isdir(args.dir):
        print("Ordner fehlt: " + args.dir, file=sys.stderr)
        return 2
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(message)s")
    patch_binary_open()
    patch_windows_behaviour()
    if args.readonly:
        patch_readonly()
    if args.throttle_kbps > 0 or args.drop_after_bytes > 0:
        patch_throttle_and_drop(args.throttle_kbps, args.drop_after_bytes, args.drop_count)
    server = smbserver.SimpleSMBServer(listenAddress=args.address, listenPort=args.port)
    try:
        server.addShare(args.share.upper(), os.path.abspath(args.dir), "STEFFLIX-Test", readOnly="yes" if args.readonly else "no")
    except TypeError:
        server.addShare(args.share.upper(), os.path.abspath(args.dir), "STEFFLIX-Test")
    server.setSMB2Support(True)
    nthash = ntlm.compute_nthash(args.password).hex()
    server.addCredential(args.user, 1000, "aad3b435b51404eeaad3b435b51404ee", nthash)
    server.setLogFile("")
    print(f"SMB-Testserver auf {args.address}:{args.port}, Freigabe {args.share.upper()} -> {os.path.abspath(args.dir)}"
          f"{' (nur lesen)' if args.readonly else ''}, Benutzer {args.user}", flush=True)
    server.start()
    return 0


if __name__ == "__main__":
    sys.exit(main())
