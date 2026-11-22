"""Prüft checksums.sig gegen checksums.json (signierte Updates, DEC-356) ohne zusätzliche Pakete.

Die öffentlichen Schlüssel stehen in SFLIX_Tools/SflixTools/src/UpdateKeys.cs (geschrieben von
update_key.ps1). Signatur: PKCS#1 v1.5 mit SHA-256 über checksums.json mit CRLF -> LF, Base64.

    python update_signature.py pruefen     # Exit 0 = gültig, 1 = ungültig oder fehlt
"""
import base64
import hashlib
import os
import re
import sys

RELEASE_DIR = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
KEYS_FILE = os.path.join(RELEASE_DIR, "SFLIX_Tools", "SflixTools", "src", "UpdateKeys.cs")
# DigestInfo-Präfix für SHA-256 (RFC 8017, 9.2)
SHA256_PREFIX = bytes.fromhex("3031300d060960864801650304020105000420")


def public_keys(path=KEYS_FILE):
    """Liste (n, e) aus UpdateKeys.cs."""
    with open(path, encoding="utf-8") as reader:
        text = reader.read()
    keys = []
    for modulus, exponent in re.findall(r"<Modulus>([A-Za-z0-9+/=]+)</Modulus><Exponent>([A-Za-z0-9+/=]+)</Exponent>", text):
        keys.append((int.from_bytes(base64.b64decode(modulus), "big"), int.from_bytes(base64.b64decode(exponent), "big")))
    return keys


def verify(data, signature_text, keys):
    """True, wenn die Signatur (Base64-Text) zu einem der Schlüssel passt."""
    try:
        signature = base64.b64decode(signature_text.strip(), validate=True)
    except (ValueError, TypeError):
        return False
    digest = hashlib.sha256(data.replace(b"\r\n", b"\n")).digest()
    for n, e in keys:
        size = (n.bit_length() + 7) // 8
        if len(signature) != size:
            continue
        s = int.from_bytes(signature, "big")
        if s >= n:
            continue
        block = pow(s, e, n).to_bytes(size, "big")
        payload = SHA256_PREFIX + digest
        expected = b"\x00\x01" + b"\xff" * (size - len(payload) - 3) + b"\x00" + payload
        if block == expected:
            return True
    return False


def check(base_dir=RELEASE_DIR):
    """None = gültig, sonst die Fehlermeldung."""
    json_path = os.path.join(base_dir, "checksums.json")
    sig_path = os.path.join(base_dir, "checksums.sig")
    if not os.path.exists(sig_path):
        return "checksums.sig fehlt"
    keys = public_keys()
    if not keys:
        return "keine öffentlichen Schlüssel in UpdateKeys.cs (update_key.ps1 ausführen)"
    with open(json_path, "rb") as reader:
        data = reader.read()
    with open(sig_path, encoding="ascii") as reader:
        signature = reader.read()
    if not verify(data, signature, keys):
        return "checksums.sig passt nicht zu checksums.json"
    return None


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    if len(sys.argv) != 2 or sys.argv[1] != "pruefen":
        print(__doc__)
        sys.exit(2)
    error = check()
    if error:
        print("FEHLER: " + error)
        sys.exit(1)
    print("Signatur von checksums.json gültig")
