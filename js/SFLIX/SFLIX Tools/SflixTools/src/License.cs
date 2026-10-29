// Lizenz (seit 3.3): Der Schlüssel "SFX2-…" enthält einen Discord-Webhook (ID und Token), über den
// das Feedback verschickt wird. Jede Lizenz hat ihren eigenen Webhook; wird einer missbraucht,
// wird nur er gelöscht.
//
// Format (gleich in "SFLIX Tools\Lizenz\create_license.py" und ui.js von STEFFLIX):
//   61 Bytes = Webhook-ID (8 Bytes, big endian) + Token (51 Bytes, Base64url-dekodiert)
//              + CRC-16/CCITT-FALSE der ersten 59 Bytes (2 Bytes)
//   XOR mit einem festen Schlüsselstrom (xorshift32, Startwert 0x5f3759df), dann Base64 ohne "=",
//   in Vierergruppen: "SFX2-xxxx-xxxx-…"
//
// Gespeichert wird der Schlüssel in sflix_sys\license.js (für alle Browser dieser Installation):
//   var licenseFile = { "key": "SFX2-…", "confirmedFor": "<installId>" };
// confirmedFor: Für diese STEFFLIX-ID wurde "Lizenz eingelöst" verschickt. Erst dann ist das
// Feedback in STEFFLIX verfügbar. Eine Kopie des Ordners (neue ID) bestätigt sich neu.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Sflix
{
    public class LicenseData
    {
        public string WebhookId, Token;
        public string Url { get { return "https://discord.com/api/webhooks/" + WebhookId + "/" + Token; } }
    }

    public static class LicenseKey
    {
        public const string Prefix = "SFX2";
        const int Length = 61;

        static byte[] Keystream(int length)
        {
            uint state = 0x5f3759df;
            byte[] bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                bytes[i] = (byte)(state & 0xff);
            }
            return bytes;
        }

        public static int Crc16(byte[] data, int count)
        {
            int crc = 0xffff;
            for (int i = 0; i < count; i++)
            {
                crc ^= data[i] << 8;
                for (int bit = 0; bit < 8; bit++)
                    crc = (crc & 0x8000) != 0 ? ((crc << 1) ^ 0x1021) & 0xffff : (crc << 1) & 0xffff;
            }
            return crc;
        }

        // Entschlüsselt einen Schlüssel. error: Grund für Nutzer, wenn null zurückkommt.
        public static LicenseData Decode(string key, out string error)
        {
            error = null;
            string text = Regex.Replace(key ?? "", @"\s+", "");
            if (text.Length == 0) { error = "Bitte den Lizenzschlüssel eingeben."; return null; }
            if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                error = "Dieser Lizenzschlüssel ist veraltet. Bitte einen neuen Schlüssel anfordern.";
                return null;
            }
            string body = text.Substring(Prefix.Length).Replace("-", "");
            if (!Regex.IsMatch(body, "^[A-Za-z0-9+/]+$")) { error = "Der Lizenzschlüssel enthält ungültige Zeichen."; return null; }
            byte[] raw;
            try { raw = Convert.FromBase64String(body + new string('=', (4 - body.Length % 4) % 4)); }
            catch (FormatException) { error = "Der Lizenzschlüssel ist unvollständig oder fehlerhaft."; return null; }
            if (raw.Length != Length) { error = "Der Lizenzschlüssel ist unvollständig oder fehlerhaft."; return null; }
            byte[] stream = Keystream(Length);
            for (int i = 0; i < Length; i++) raw[i] ^= stream[i];
            if (Crc16(raw, 59) != ((raw[59] << 8) | raw[60]))
            {
                error = "Der Lizenzschlüssel ist unvollständig oder fehlerhaft (Tippfehler?).";
                return null;
            }
            ulong id = 0;
            for (int i = 0; i < 8; i++) id = (id << 8) | raw[i];
            byte[] token = new byte[51];
            Array.Copy(raw, 8, token, 0, 51);
            LicenseData d = new LicenseData();
            d.WebhookId = id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            d.Token = Convert.ToBase64String(token).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return d;
        }
    }

    // Nachrichten an den Discord-Webhook (Text und Dateien als multipart/form-data)
    public static class Discord
    {
        public class Attachment
        {
            public string Name;
            public byte[] Data;
            public Attachment(string name, string text) { Name = name; Data = new UTF8Encoding(false).GetBytes(text ?? ""); }
        }

        // HTTP-Status (2xx = zugestellt), -1 = keine Verbindung. error: Text für Nutzer bei Fehlern.
        public static int Send(string webhookUrl, string content, List<Attachment> files, out string error)
        {
            error = null;
            try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; } catch { }
            // Discord nimmt höchstens 2000 Zeichen, längere Texte zusätzlich als Datei
            if (files == null) files = new List<Attachment>();
            if (content.Length > 1900)
            {
                files.Insert(0, new Attachment("nachricht.txt", content));
                content = content.Substring(0, 1800) + "\n… (vollständig in nachricht.txt)";
            }
            JsonObject payload = new JsonObject();
            payload["content"] = content;
            JsonObject mentions = new JsonObject();
            mentions["parse"] = new List<object>();   // keine Erwähnungen (@everyone) auslösen
            payload["allowed_mentions"] = mentions;

            string boundary = "----STEFFLIX" + Guid.NewGuid().ToString("N");
            MemoryStream body = new MemoryStream();
            Part(body, boundary, "payload_json", null, "application/json", new UTF8Encoding(false).GetBytes(Json.Write(payload, 0)));
            for (int i = 0; i < files.Count; i++)
                Part(body, boundary, "files[" + i + "]", files[i].Name, "text/plain; charset=utf-8", files[i].Data);
            byte[] end = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");
            body.Write(end, 0, end.Length);

            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(webhookUrl + "?wait=true");
                req.Method = "POST";
                req.UserAgent = "STEFFLIX-Updater/" + App.VersionText;
                req.ContentType = "multipart/form-data; boundary=" + boundary;
                req.Timeout = 30000;
                byte[] data = body.ToArray();
                req.ContentLength = data.Length;
                using (Stream s = req.GetRequestStream()) s.Write(data, 0, data.Length);
                using (HttpWebResponse res = (HttpWebResponse)req.GetResponse()) return (int)res.StatusCode;
            }
            catch (WebException e)
            {
                HttpWebResponse res = e.Response as HttpWebResponse;
                if (res == null)
                {
                    error = "Keine Verbindung (" + e.Message + "). Bitte Internetverbindung prüfen.";
                    return -1;
                }
                int status = (int)res.StatusCode;
                res.Close();
                error = Describe(status);
                return status;
            }
            catch (Exception e)
            {
                error = "Senden nicht möglich: " + e.Message;
                return -1;
            }
        }

        public static bool IsGone(int status)
        {
            return status == 401 || status == 403 || status == 404;
        }

        public static string Describe(int status)
        {
            if (IsGone(status)) return "Der Lizenzschlüssel ist nicht mehr gültig. Bitte einen neuen Schlüssel anfordern.";
            if (status == 429) return "Es wurden gerade zu viele Nachrichten verschickt. Bitte in einer Minute erneut versuchen.";
            if (status == 400 || status == 413) return "Die Nachricht wurde abgelehnt (zu lang oder ungültig).";
            return "Die Nachricht konnte nicht verschickt werden (Fehler " + status + ").";
        }

        static void Part(Stream s, string boundary, string name, string fileName, string type, byte[] data)
        {
            StringBuilder h = new StringBuilder();
            h.Append("--").Append(boundary).Append("\r\n");
            h.Append("Content-Disposition: form-data; name=\"").Append(name).Append('"');
            if (fileName != null) h.Append("; filename=\"").Append(fileName).Append('"');
            h.Append("\r\nContent-Type: ").Append(type).Append("\r\n\r\n");
            byte[] head = new UTF8Encoding(false).GetBytes(h.ToString());
            s.Write(head, 0, head.Length);
            s.Write(data, 0, data.Length);
            s.Write(new byte[] { 13, 10 }, 0, 2);
        }
    }

    public enum LicenseStatus { None, Invalid, Pending, Active }

    // sflix_sys\license.js lesen und schreiben, Bestätigung und Abmeldung verschicken
    public static class LicenseStore
    {
        public const string FileName = "license.js";
        static readonly Regex KeyPattern = new Regex("\"key\"\\s*:\\s*\"([^\"]*)\"");
        static readonly Regex ConfirmedPattern = new Regex("\"confirmedFor\"\\s*:\\s*\"([0-9a-f]*)\"");

        public static string FilePath(string root) { return Path.Combine(root, "sflix_sys", FileName); }

        public static string ReadKey(string root, out string confirmedFor)
        {
            confirmedFor = null;
            try
            {
                string file = FilePath(root);
                if (!File.Exists(file)) return null;
                string text = File.ReadAllText(file, Encoding.UTF8);
                Match k = KeyPattern.Match(text);
                if (!k.Success || k.Groups[1].Value.Length == 0) return null;
                Match c = ConfirmedPattern.Match(text);
                if (c.Success) confirmedFor = c.Groups[1].Value;
                return k.Groups[1].Value;
            }
            catch { return null; }
        }

        static void Write(string root, string key, string confirmedFor)
        {
            string content =
                "// Lizenzschlüssel dieser STEFFLIX-Installation (vom Updater geschrieben, nicht weitergeben)\r\n" +
                "var licenseFile = { \"key\": \"" + key.Replace("\"", "") + "\", \"confirmedFor\": \"" + (confirmedFor ?? "") + "\" };\r\n";
            string file = FilePath(root);
            string tmp = file + ".tmp";
            File.WriteAllText(tmp, content, FileOps.Utf8NoBom);
            FileOps.Replace(tmp, file);
        }

        static string InstallIdOf(string root)
        {
            string folder;
            return InstallId.Read(root, out folder);
        }

        public static LicenseStatus Status(string root)
        {
            string confirmedFor;
            string key = ReadKey(root, out confirmedFor);
            if (key == null) return LicenseStatus.None;
            string error;
            if (LicenseKey.Decode(key, out error) == null) return LicenseStatus.Invalid;
            string id = InstallIdOf(root);
            return id != null && confirmedFor == id ? LicenseStatus.Active : LicenseStatus.Pending;
        }

        public static LicenseData Current(string root)
        {
            string confirmedFor, error;
            string key = ReadKey(root, out confirmedFor);
            return key == null ? null : LicenseKey.Decode(key, out error);
        }

        // Kopfzeile jeder Nachricht: STEFFLIX-ID, Versionen
        public static string Header(string root)
        {
            string web = null;
            try { web = UpdateCheck.ParseWebVersion(File.ReadAllText(Path.Combine(root, "sflix_sys", "version.js"), Encoding.UTF8)); }
            catch { }
            return "STEFFLIX-ID: " + (InstallIdOf(root) ?? "ohne Kennung") + "\n" +
                   "STEFFLIX " + (web ?? "?") + " · über Updater " + App.VersionText + " · " + Environment.OSVersion.VersionString;
        }

        static int SendNotice(string root, LicenseData d, string title, out string error)
        {
            return Discord.Send(d.Url, "**" + title + "**\n" + Header(root), null, out error);
        }

        public static bool Save(string key, out string message) { return Save(App.Root, key, out message); }

        // Schlüssel prüfen, "Lizenz eingelöst" senden und speichern. Ohne Verbindung wird er
        // gespeichert und die Bestätigung später nachgeholt. Ungültig: nichts gespeichert.
        public static bool Save(string root, string key, out string message)
        {
            string error;
            LicenseData d = LicenseKey.Decode(key, out error);
            if (d == null) { message = error; return false; }
            string normalized = Normalize(key);
            int status = SendNotice(root, d, "Lizenz eingelöst", out error);
            if (Discord.IsGone(status)) { message = Discord.Describe(status); return false; }
            bool sent = status >= 200 && status < 300;
            try { Write(root, normalized, sent ? InstallIdOf(root) : ""); }
            catch (Exception e) { message = "Der Lizenzschlüssel konnte nicht gespeichert werden: " + e.Message; return false; }
            message = sent ? "Lizenz gespeichert und bestätigt. Das Feedback ist im Updater und in STEFFLIX verfügbar."
                           : "Lizenz gespeichert, die Bestätigung folgt beim nächsten Start mit Internetverbindung (" + error + ")";
            return true;
        }

        // Ausstehende Bestätigung nachholen (neuer Ordner, früher keine Verbindung). Meldung oder null.
        public static string ConfirmPending()
        {
            try
            {
                string confirmedFor;
                string key = ReadKey(App.Root, out confirmedFor);
                string id = InstallIdOf(App.Root);
                if (key == null || id == null || confirmedFor == id) return null;
                string error;
                LicenseData d = LicenseKey.Decode(key, out error);
                if (d == null) return "Hinweis: Der hinterlegte Lizenzschlüssel ist ungültig (" + error + ")";
                int status = SendNotice(App.Root, d, "Lizenz eingelöst", out error);
                if (Discord.IsGone(status)) return "Hinweis: " + Discord.Describe(status);
                if (status < 200 || status >= 300) return null;   // ohne Verbindung: beim nächsten Start
                Write(App.Root, key, id);
                return "Lizenz bestätigt, das Feedback ist verfügbar.";
            }
            catch { return null; }
        }

        // Abmelden: "Lizenz entfernt" senden, dann license.js löschen. Gibt false zurück, wenn ohne
        // Verbindung nicht gesendet werden konnte und force nicht gesetzt ist (nichts gelöscht).
        public static bool Remove(bool force, out string message) { return Remove(force, false, out message); }

        public static bool Remove(bool force, bool offlineFails, out string message)
        {
            LicenseData d = Current(App.Root);
            if (d != null)
            {
                string error;
                int status = SendNotice(App.Root, d, "Lizenz entfernt", out error);
                bool sent = status >= 200 && status < 300;
                if (!sent && !Discord.IsGone(status) && offlineFails && !force)
                {
                    message = "Die Abmeldung konnte nicht gesendet werden (" + error + ")";
                    return false;
                }
            }
            try { if (File.Exists(FilePath(App.Root))) File.Delete(FilePath(App.Root)); }
            catch (Exception e) { message = "Der Lizenzschlüssel konnte nicht entfernt werden: " + e.Message; return false; }
            message = "Lizenzschlüssel entfernt. Das Feedback ist nicht mehr verfügbar.";
            return true;
        }

        // Feedback senden. Anhänge: Diagnose, Protokoll. Gibt eine Meldung zurück, ok = zugestellt.
        public static bool SendFeedback(string sender, string text, List<Discord.Attachment> files, out string message)
        {
            LicenseData d = Current(App.Root);
            if (d == null) { message = "Kein gültiger Lizenzschlüssel hinterlegt."; return false; }
            string content = Header(App.Root).Split('\n')[0] + " · Updater " + App.VersionText + "\n" +
                             "**Absender:** " + sender + "\n**Nachricht:**\n" + text;
            string error;
            int status = Discord.Send(d.Url, content, files, out error);
            if (status >= 200 && status < 300) { message = "Danke! Die Nachricht wurde verschickt."; return true; }
            message = error ?? Discord.Describe(status);
            return false;
        }

        // Schlüssel einheitlich schreiben: SFX2-xxxx-xxxx-…
        public static string Normalize(string key)
        {
            string text = Regex.Replace(key ?? "", @"\s+", "");
            string body = text.Substring(LicenseKey.Prefix.Length).Replace("-", "");
            StringBuilder sb = new StringBuilder(LicenseKey.Prefix);
            for (int i = 0; i < body.Length; i += 4)
                sb.Append('-').Append(body.Substring(i, Math.Min(4, body.Length - i)));
            return sb.ToString();
        }
    }
}
