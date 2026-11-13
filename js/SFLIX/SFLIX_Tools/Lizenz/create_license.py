"""Lizenzschlüssel für STEFFLIX erzeugen (Format SFX2, seit STEFFLIX 2.5.0 / Updater 3.3).

Jede Lizenz hat einen eigenen Discord-Webhook. So legst du eine Lizenz an:
  1. In Discord beim Feedback-Kanal: Kanal-Einstellungen > Integrationen > Webhooks > Neuer Webhook.
     Den Webhook nach dem Lizenznehmer benennen (der Name erscheint als Absender) und die URL kopieren.
     Hinweis: Discord erlaubt höchstens 15 Webhooks pro Kanal, danach einen zweiten Kanal anlegen.
  2. python create_license.py "https://discord.com/api/webhooks/123.../abc..." --name "Familie Muster" --test
  3. Den ausgegebenen Schlüssel weitergeben. Er wird im Updater unter "Lizenz" eingegeben.

Wird eine Lizenz missbraucht, den Webhook in Discord löschen: Nur dieser Schlüssel wird ungültig.

Befehle:
  python create_license.py <Webhook-URL> [--name NAME] [--test]   Schlüssel erzeugen
  python create_license.py --decode <Schlüssel>                     Schlüssel prüfen und Webhook-ID zeigen
  python create_license.py --liste [--test]                         ausgegebene Lizenzen (mit --test: Webhooks prüfen)

Die Liste steht in lizenzen.json neben diesem Skript: Name, Datum, Webhook-ID, ohne Token.
Sie wird nie gepusht.

Format (gleich in ui.js von STEFFLIX und License.cs des Updaters):
  61 Bytes = Webhook-ID (8 Bytes, big endian) + Token (51 Bytes, Base64url-dekodiert)
             + CRC-16/CCITT-FALSE der ersten 59 Bytes (2 Bytes)
  XOR mit festem Schlüsselstrom (xorshift32, Startwert 0x5f3759df), Base64 ohne "=",
  Vierergruppen mit Bindestrich: SFX2-xxxx-xxxx-...
"""
import argparse
import base64
import datetime
import json
import os
import re
import sys
import urllib.error
import urllib.request
import uuid

PREFIX = "SFX2"
LENGTH = 61
LIST_FILE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "lizenzen.json")
WEBHOOK_PATTERN = re.compile(r"^https://(?:\w+\.)?discord(?:app)?\.com/api/webhooks/(\d{1,20})/([A-Za-z0-9_-]{68})/?$")


def keystream(length):
    state = 0x5F3759DF
    out = bytearray()
    for _ in range(length):
        state ^= (state << 13) & 0xFFFFFFFF
        state ^= state >> 17
        state ^= (state << 5) & 0xFFFFFFFF
        out.append(state & 0xFF)
    return bytes(out)


def crc16(data):
    crc = 0xFFFF
    for value in data:
        crc ^= value << 8
        for _ in range(8):
            crc = ((crc << 1) ^ 0x1021) & 0xFFFF if crc & 0x8000 else (crc << 1) & 0xFFFF
    return crc


def b64url_decode(text):
    return base64.urlsafe_b64decode(text + "=" * (-len(text) % 4))


def encode(webhook_id, token):
    raw_token = b64url_decode(token)
    if len(raw_token) != 51 or base64.urlsafe_b64encode(raw_token).decode().rstrip("=") != token:
        raise ValueError("Der Token des Webhooks hat ein unerwartetes Format.")
    if not 0 < int(webhook_id) < 2 ** 64:
        raise ValueError("Die Webhook-ID ist ungültig.")
    data = int(webhook_id).to_bytes(8, "big") + raw_token
    data += crc16(data).to_bytes(2, "big")
    mixed = bytes(a ^ b for a, b in zip(data, keystream(LENGTH)))
    body = base64.b64encode(mixed).decode().rstrip("=")
    return PREFIX + "-" + "-".join(body[i:i + 4] for i in range(0, len(body), 4))


def decode(key):
    text = re.sub(r"\s+", "", key)
    if not text.upper().startswith(PREFIX):
        raise ValueError("Kein SFX2-Schlüssel (veraltet?)")
    body = text[len(PREFIX):].replace("-", "")
    raw = base64.b64decode(body + "=" * (-len(body) % 4))
    if len(raw) != LENGTH:
        raise ValueError("Falsche Länge")
    data = bytes(a ^ b for a, b in zip(raw, keystream(LENGTH)))
    if crc16(data[:59]) != int.from_bytes(data[59:61], "big"):
        raise ValueError("Prüfsumme stimmt nicht (Tippfehler?)")
    webhook_id = str(int.from_bytes(data[:8], "big"))
    token = base64.urlsafe_b64encode(data[8:59]).decode().rstrip("=")
    return webhook_id, token


def webhook_url(webhook_id, token):
    return f"https://discord.com/api/webhooks/{webhook_id}/{token}"


def send_test(url, name):
    """Testnachricht senden. Gibt den HTTP-Status zurück (0 = keine Verbindung)."""
    boundary = "----STEFFLIX" + uuid.uuid4().hex
    payload = json.dumps({
        "content": f"**Test des Lizenzgenerators**\nLizenz: {name or '(ohne Namen)'}\nDiese Nachricht kann gelöscht werden.",
        "allowed_mentions": {"parse": []},
    })
    body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"payload_json\"\r\n"
            f"Content-Type: application/json\r\n\r\n{payload}\r\n--{boundary}--\r\n").encode("utf-8")
    request = urllib.request.Request(url + "?wait=true", data=body, method="POST", headers={
        "Content-Type": f"multipart/form-data; boundary={boundary}", "User-Agent": "STEFFLIX-Lizenzgenerator"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code
    except (urllib.error.URLError, OSError):
        return 0


def check_webhook(url):
    """Prüft ohne Nachricht, ob der Webhook noch existiert (GET). Status oder 0."""
    request = urllib.request.Request(url, headers={"User-Agent": "STEFFLIX-Lizenzgenerator"})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return response.status
    except urllib.error.HTTPError as error:
        return error.code
    except (urllib.error.URLError, OSError):
        return 0


def load_list():
    try:
        with open(LIST_FILE, encoding="utf-8") as reader:
            return json.load(reader)
    except (OSError, ValueError):
        return []


def save_list(entries):
    with open(LIST_FILE, "w", encoding="utf-8") as writer:
        json.dump(entries, writer, ensure_ascii=False, indent=2)
        writer.write("\n")


def main():
    parser = argparse.ArgumentParser(description="Lizenzschlüssel für STEFFLIX (SFX2) erzeugen")
    parser.add_argument("webhook", nargs="?", help="Webhook-URL aus Discord")
    parser.add_argument("--name", help="Lizenznehmer (nur für die eigene Liste)")
    parser.add_argument("--test", action="store_true", help="Testnachricht senden bzw. Webhooks prüfen")
    parser.add_argument("--decode", metavar="SCHLÜSSEL", help="Schlüssel prüfen")
    parser.add_argument("--liste", action="store_true", help="ausgegebene Lizenzen anzeigen")
    args = parser.parse_args()

    if args.decode:
        try:
            webhook_id, token = decode(args.decode)
        except ValueError as error:
            print(f"UNGÜLTIG: {error}")
            return 1
        print(f"Gültig. Webhook-ID: {webhook_id}")
        if args.test:
            status = check_webhook(webhook_url(webhook_id, token))
            print("Webhook vorhanden." if status == 200 else f"Webhook nicht erreichbar (Status {status}).")
        return 0

    if args.liste:
        entries = load_list()
        if not entries:
            print("Noch keine Lizenzen ausgegeben.")
            return 0
        print(f"{len(entries)} Lizenz(en), höchstens 15 Webhooks pro Discord-Kanal:")
        for entry in entries:
            line = f"  {entry.get('datum', '?'):10}  {entry.get('name') or '(ohne Namen)':30}  Webhook {entry.get('webhookId')}"
            if args.test:
                # Ohne Token lässt sich nur über Discord selbst prüfen: Hinweis
                line += "  (Prüfung mit --decode <Schlüssel> --test)"
            print(line)
        print("Gesperrt wird eine Lizenz, indem ihr Webhook in Discord gelöscht wird.")
        return 0

    if not args.webhook:
        parser.print_help()
        return 1
    match = WEBHOOK_PATTERN.match(args.webhook.strip())
    if not match:
        print("Das ist keine gültige Discord-Webhook-URL (https://discord.com/api/webhooks/<ID>/<Token>).")
        return 1
    webhook_id, token = match.group(1), match.group(2)
    try:
        key = encode(webhook_id, token)
    except ValueError as error:
        print(f"FEHLER: {error}")
        return 1

    # Selbstprüfung: der Schlüssel muss genau den Webhook ergeben
    if decode(key) != (webhook_id, token):
        print("FEHLER: Selbstprüfung fehlgeschlagen, Schlüssel NICHT verwenden.")
        return 1

    if args.test:
        status = send_test(webhook_url(webhook_id, token), args.name)
        if 200 <= status < 300:
            print("Testnachricht zugestellt.")
        else:
            print(f"WARNUNG: Testnachricht nicht zugestellt (Status {status}). Webhook prüfen.")

    entries = load_list()
    if not any(entry.get("webhookId") == webhook_id for entry in entries):
        entries.append({"name": args.name or "", "datum": datetime.date.today().isoformat(), "webhookId": webhook_id})
        save_list(entries)

    print()
    print("Lizenzschlüssel:")
    print(key)
    return 0


if __name__ == "__main__":
    sys.exit(main())
