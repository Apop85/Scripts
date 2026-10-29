// Hilfsfunktionen: JSON, Dateioperationen, Prozessaufrufe, Formatierung.
// Hinweis: wird mit dem csc.exe von .NET Framework 4 gebaut (C# 5) -> keine neueren Sprachfeatures.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Sflix
{
    // ------------------------------------------------------------------
    // JSON (geordnetes Objekt, damit die Reihenfolge wie bei Python bleibt)
    // ------------------------------------------------------------------

    public class JsonObject : IEnumerable<KeyValuePair<string, object>>
    {
        readonly List<string> keys = new List<string>();
        readonly Dictionary<string, object> map = new Dictionary<string, object>();

        public object this[string key]
        {
            get { object v; return map.TryGetValue(key, out v) ? v : null; }
            set
            {
                if (!map.ContainsKey(key)) keys.Add(key);
                map[key] = value;
            }
        }

        public bool ContainsKey(string key) { return map.ContainsKey(key); }
        public bool Remove(string key)
        {
            if (!map.Remove(key)) return false;
            keys.Remove(key);
            return true;
        }
        // Schlüssel stabil sortieren (gleich bewertete behalten ihre Reihenfolge)
        public void SortKeys(Func<string, int> rank)
        {
            List<string> sorted = new List<string>(System.Linq.Enumerable.OrderBy(keys, rank));
            keys.Clear();
            keys.AddRange(sorted);
        }
        public int Count { get { return keys.Count; } }
        public IList<string> Keys { get { return keys; } }

        public string Str(string key)
        {
            object v = this[key];
            return v == null ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        public double Num(string key, double fallback)
        {
            object v = this[key];
            if (v is double) return (double)v;
            double d;
            if (v is string && double.TryParse((string)v, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            return fallback;
        }

        public bool Bool(string key, bool fallback)
        {
            object v = this[key];
            return v is bool ? (bool)v : fallback;
        }

        public JsonObject Obj(string key) { return this[key] as JsonObject; }
        public List<object> Arr(string key) { return this[key] as List<object>; }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (string k in keys) yield return new KeyValuePair<string, object>(k, map[k]);
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    public static class Json
    {
        public static object Parse(string text)
        {
            int i = 0;
            object result = ParseValue(text, ref i);
            SkipWs(text, ref i);
            if (i != text.Length) throw new FormatException("JSON: unerwartete Zeichen am Ende");
            return result;
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n' || s[i] == '﻿')) i++;
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("JSON: unerwartetes Ende");
            char c = s[i];
            if (c == '{')
            {
                JsonObject o = new JsonObject();
                i++;
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return o; }
                while (true)
                {
                    SkipWs(s, ref i);
                    string key = ParseString(s, ref i);
                    SkipWs(s, ref i);
                    Expect(s, ref i, ':');
                    o[key] = ParseValue(s, ref i);
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    Expect(s, ref i, '}');
                    return o;
                }
            }
            if (c == '[')
            {
                List<object> a = new List<object>();
                i++;
                SkipWs(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return a; }
                while (true)
                {
                    a.Add(ParseValue(s, ref i));
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    Expect(s, ref i, ']');
                    return a;
                }
            }
            if (c == '"') return ParseString(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }

            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (start == i) throw new FormatException("JSON: ungültiger Wert an Position " + i);
            return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        static void Expect(string s, ref int i, char c)
        {
            if (i >= s.Length || s[i] != c) throw new FormatException("JSON: '" + c + "' erwartet an Position " + i);
            i++;
        }

        static string ParseString(string s, ref int i)
        {
            Expect(s, ref i, '"');
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= s.Length) throw new FormatException("JSON: Zeichenkette nicht beendet");
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber));
                        i += 4;
                        break;
                    default: throw new FormatException("JSON: ungültige Escape-Sequenz");
                }
            }
        }

        // Schreibt wie Python json.dumps(..., ensure_ascii=False): Trenner ", " und ": ".
        // indent > 0 schreibt mehrzeilig (für Einstellungsdateien).
        public static string Write(object value, int indent)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value, indent, 0);
            return sb.ToString();
        }

        public static string Write(object value) { return Write(value, 0); }

        static void NewLine(StringBuilder sb, int indent, int level)
        {
            sb.Append('\n');
            sb.Append(' ', indent * level);
        }

        static void WriteValue(StringBuilder sb, object v, int indent, int level)
        {
            if (v == null) { sb.Append("null"); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is int || v is long) { sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is double || v is float)
            {
                double d = Convert.ToDouble(v);
                if (Math.Floor(d) == d && Math.Abs(d) < 1e15) sb.Append(((long)d).ToString(CultureInfo.InvariantCulture));
                else sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            JsonObject o = v as JsonObject;
            if (o != null)
            {
                if (o.Count == 0) { sb.Append("{}"); return; }
                sb.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> kv in o)
                {
                    if (!first) sb.Append(indent > 0 ? "," : ", ");
                    first = false;
                    if (indent > 0) NewLine(sb, indent, level + 1);
                    WriteString(sb, kv.Key);
                    sb.Append(": ");
                    WriteValue(sb, kv.Value, indent, level + 1);
                }
                if (indent > 0) NewLine(sb, indent, level);
                sb.Append('}');
                return;
            }
            IEnumerable list = v as IEnumerable;
            if (list != null)
            {
                sb.Append('[');
                bool first = true;
                bool any = false;
                foreach (object item in list)
                {
                    if (!first) sb.Append(indent > 0 ? "," : ", ");
                    first = false;
                    any = true;
                    if (indent > 0) NewLine(sb, indent, level + 1);
                    WriteValue(sb, item, indent, level + 1);
                }
                if (indent > 0 && any) NewLine(sb, indent, level);
                sb.Append(']');
                return;
            }
            WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    // ------------------------------------------------------------------
    // Dateien
    // ------------------------------------------------------------------

    public static class FileOps
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool MoveFileEx(string existing, string target, int flags);

        const int MOVEFILE_REPLACE_EXISTING = 1;
        const int MOVEFILE_COPY_ALLOWED = 2;

        public static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        const int ERROR_ACCESS_DENIED = 5, ERROR_SHARING_VIOLATION = 32, ERROR_LOCK_VIOLATION = 33;

        // Wie Python os.replace: verschiebt und überschreibt ein vorhandenes Ziel.
        public static void Replace(string source, string target)
        {
            Move(source, target, MOVEFILE_REPLACE_EXISTING | MOVEFILE_COPY_ALLOWED);
        }

        // Verschiebt eine Datei, auch auf ein anderes Laufwerk (dann wird kopiert und gelöscht).
        public static void MoveAcross(string source, string target, bool overwrite)
        {
            Move(source, target, MOVEFILE_COPY_ALLOWED | (overwrite ? MOVEFILE_REPLACE_EXISTING : 0));
        }

        // Wie Python os.rename: schlägt fehl, wenn das Ziel existiert (ausser bei reiner
        // Änderung der Gross-/Kleinschreibung). Funktioniert auch für Ordner.
        public static void Rename(string source, string target)
        {
            Move(source, target, 0);
        }

        // Verschieben mit zwei Hilfen für Netzlaufwerke (NAS):
        // - Schreibgeschützte Dateien (Attribut, oft per SMB gesetzt) lässt Windows nicht verschieben,
        //   ersetzen oder löschen ("Zugriff verweigert"): Attribut entfernen und erneut versuchen,
        //   danach am Ziel wieder setzen.
        // - Kurz geöffnete Dateien (Indexdienst des NAS, Virenscanner, Vorschau): kurz warten und wiederholen.
        // - Fehlt auf der Datei das Recht "Löschen" (z.B. Dateien, die auf dem NAS selbst angelegt wurden und
        //   dem NAS-Administrator gehören), wird es für die Benutzer ergänzt, die die Datei schon ändern dürfen.
        static void Move(string source, string target, int flags)
        {
            bool readOnlyRemoved = false, deleteGranted = false;
            for (int attempt = 0; ; attempt++)
            {
                if (MoveFileEx(source, target, flags))
                {
                    if (readOnlyRemoved) TrySetReadOnly(target);
                    return;
                }
                int error = Marshal.GetLastWin32Error();
                if (error == ERROR_ACCESS_DENIED && !readOnlyRemoved)
                {
                    bool changed = ClearReadOnly(source);
                    if ((flags & MOVEFILE_REPLACE_EXISTING) != 0) ClearReadOnly(target);
                    if (changed) { readOnlyRemoved = true; continue; }
                }
                if (error == ERROR_ACCESS_DENIED && !deleteGranted)
                {
                    deleteGranted = true;
                    bool granted = TryGrantDelete(source);
                    if ((flags & MOVEFILE_REPLACE_EXISTING) != 0 && File.Exists(target)) granted |= TryGrantDelete(target);
                    if (granted) continue;
                }
                if ((error == ERROR_SHARING_VIOLATION || error == ERROR_LOCK_VIOLATION || error == ERROR_ACCESS_DENIED) && attempt < 3)
                {
                    Thread.Sleep(400 * (attempt + 1));
                    continue;
                }
                throw new IOException(Describe(error) + ": " + target);
            }
        }

        // Löscht eine Datei, auch schreibgeschützt; wiederholt bei kurz geöffneten Dateien
        public static void Delete(string path)
        {
            bool deleteGranted = false;
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    if (!File.Exists(path)) return;
                    ClearReadOnly(path);
                    File.Delete(path);
                    return;
                }
                catch (Exception e)
                {
                    if (!(e is IOException || e is UnauthorizedAccessException)) throw;
                    if (e is UnauthorizedAccessException && !deleteGranted)
                    {
                        deleteGranted = true;
                        if (TryGrantDelete(path)) continue;
                    }
                    if (attempt >= 3) throw new IOException(Describe(e is UnauthorizedAccessException ? ERROR_ACCESS_DENIED : ERROR_SHARING_VIOLATION) + ": " + path, e);
                    Thread.Sleep(400 * (attempt + 1));
                }
            }
        }

        // Leeren Ordner löschen, auch mit Attribut Schreibgeschützt
        public static void DeleteEmptyDirectory(string dir)
        {
            try { File.SetAttributes(dir, FileAttributes.Directory); } catch { }
            try { Directory.Delete(dir, false); }
            catch (UnauthorizedAccessException)
            {
                if (!TryGrantDelete(dir)) throw;
                Directory.Delete(dir, false);
            }
        }

        // Meldungen für das Protokoll (z.B. "Recht Löschen ergänzt"), wird vom laufenden Auftrag gesetzt
        public static Action<string> Notice;

        // Hat jede Regel mit Schreibrecht auch "Löschen"?
        static bool HasDeleteRight(string path, bool isDir)
        {
            System.Security.AccessControl.FileSystemSecurity sec = isDir
                ? (System.Security.AccessControl.FileSystemSecurity)Directory.GetAccessControl(path, System.Security.AccessControl.AccessControlSections.Access)
                : File.GetAccessControl(path, System.Security.AccessControl.AccessControlSections.Access);
            foreach (System.Security.AccessControl.FileSystemAccessRule r in sec.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier)))
            {
                if (r.AccessControlType != System.Security.AccessControl.AccessControlType.Allow) continue;
                if ((r.FileSystemRights & System.Security.AccessControl.FileSystemRights.WriteData) == 0) continue;
                if ((r.FileSystemRights & System.Security.AccessControl.FileSystemRights.Delete) == 0) return false;
            }
            return true;
        }

        // Ergänzt das Recht "Löschen" für alle, die die Datei bzw. den Ordner schon ändern dürfen (Schreiben erlaubt).
        // Klappt nur, wenn der Benutzer die Rechte ändern darf. Mit Sicherheitskennungen (SID), damit nichts
        // übersetzt werden muss: Benutzer des NAS sind unter Windows oft nicht bekannt.
        public static bool TryGrantDelete(string path)
        {
            try
            {
                bool isDir = Directory.Exists(path);
                if (!isDir && !File.Exists(path)) return false;
                System.Security.AccessControl.FileSystemSecurity sec = isDir
                    ? (System.Security.AccessControl.FileSystemSecurity)Directory.GetAccessControl(path, System.Security.AccessControl.AccessControlSections.Access)
                    : File.GetAccessControl(path, System.Security.AccessControl.AccessControlSections.Access);
                System.Security.AccessControl.AuthorizationRuleCollection rules =
                    sec.GetAccessRules(true, true, typeof(System.Security.Principal.SecurityIdentifier));
                bool changed = false;
                foreach (System.Security.AccessControl.FileSystemAccessRule r in rules)
                {
                    if (r.AccessControlType != System.Security.AccessControl.AccessControlType.Allow) continue;
                    if ((r.FileSystemRights & System.Security.AccessControl.FileSystemRights.WriteData) == 0) continue;
                    if ((r.FileSystemRights & System.Security.AccessControl.FileSystemRights.Delete) != 0) continue;
                    sec.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(r.IdentityReference,
                        System.Security.AccessControl.FileSystemRights.Delete, System.Security.AccessControl.AccessControlType.Allow));
                    changed = true;
                }
                if (!changed) return false;
                if (isDir) Directory.SetAccessControl(path, (System.Security.AccessControl.DirectorySecurity)sec);
                else File.SetAccessControl(path, (System.Security.AccessControl.FileSecurity)sec);
                // Manche NAS (Linux-Rechte) melden Erfolg, speichern "Löschen" aber nicht: nachprüfen
                if (!HasDeleteRight(path, isDir)) return false;
                Action<string> notice = Notice;
                if (notice != null) notice("  Hinweis: Recht „Löschen“ ergänzt (fehlte auf dem NAS): " + path);
                return true;
            }
            catch { return false; }
        }

        // true, wenn das Attribut gesetzt war und entfernt wurde
        static bool ClearReadOnly(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                FileAttributes a = File.GetAttributes(path);
                if ((a & FileAttributes.ReadOnly) == 0) return false;
                File.SetAttributes(path, a & ~FileAttributes.ReadOnly);
                return true;
            }
            catch { return false; }
        }

        static void TrySetReadOnly(string path)
        {
            try { if (File.Exists(path)) File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly); } catch { }
        }

        // Fehlermeldung mit Hinweis, woran es auf einem NAS meist liegt
        static string Describe(int error)
        {
            string text = new Win32Exception(error).Message;
            if (error == ERROR_ACCESS_DENIED)
                text += " (Datei evtl. geöffnet, z.B. in STEFFLIX oder vom Indexdienst des NAS, oder sie gehört auf dem NAS einem anderen Benutzer, " +
                        "z.B. dem Administrator, bzw. das Recht „Löschen“ fehlt: im NAS Besitzer oder Rechte für den Ordner neu setzen)";
            else if (error == ERROR_SHARING_VIOLATION || error == ERROR_LOCK_VIOLATION)
                text += " (Datei ist in einem anderen Programm geöffnet)";
            return text;
        }

        public static bool Exists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        // Freier Name im Ordner: "Name.ext", "Name (2).ext", "Name (3).ext", ...
        public static string UniquePath(string folder, string fileName)
        {
            string candidate = Path.Combine(folder, fileName);
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            int i = 2;
            while (Exists(candidate))
            {
                candidate = Path.Combine(folder, stem + " (" + i + ")" + ext);
                i++;
            }
            return candidate;
        }

        public static void TryDelete(string path)
        {
            try { Delete(path); } catch { }
        }

        // Python os.path.splitext: führende Punkte gehören zum Namen (".hidden" hat keine Endung).
        public static void SplitExt(string name, out string stem, out string ext)
        {
            int dot = name.LastIndexOf('.');
            int firstNonDot = 0;
            while (firstNonDot < name.Length && name[firstNonDot] == '.') firstNonDot++;
            if (dot <= 0 || dot < firstNonDot) { stem = name; ext = ""; return; }
            stem = name.Substring(0, dot);
            ext = name.Substring(dot);
        }

        public static string Sha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return Hex(sha.ComputeHash(data));
        }

        public static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
                return Hex(sha.ComputeHash(fs));
        }

        public static string Hex(byte[] hash)
        {
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static bool IsWindowsExe(byte[] data)
        {
            if (data.Length < 0x40 || data[0] != (byte)'M' || data[1] != (byte)'Z') return false;
            int pe = BitConverter.ToInt32(data, 0x3C);
            if (pe < 0 || pe + 4 > data.Length) return false;
            return data[pe] == (byte)'P' && data[pe + 1] == (byte)'E' && data[pe + 2] == 0 && data[pe + 3] == 0;
        }

        public static bool IsWindowsExeFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] head = new byte[4096];
                int n = fs.Read(head, 0, head.Length);
                if (n < 0x40) return false;
                int pe = BitConverter.ToInt32(head, 0x3C);
                if (pe < 0 || pe + 4 > n)
                {
                    if (pe < 0 || pe + 4 > fs.Length) return false;
                    fs.Position = pe;
                    byte[] sig = new byte[4];
                    fs.Read(sig, 0, 4);
                    return head[0] == 'M' && head[1] == 'Z' && sig[0] == 'P' && sig[1] == 'E' && sig[2] == 0 && sig[3] == 0;
                }
                return head[0] == 'M' && head[1] == 'Z' && head[pe] == 'P' && head[pe + 1] == 'E' && head[pe + 2] == 0 && head[pe + 3] == 0;
            }
        }
    }

    // ------------------------------------------------------------------
    // Externe Programme (ffmpeg/ffprobe)
    // ------------------------------------------------------------------

    public class ProcResult
    {
        public int ExitCode;
        public string StdOut;
        public string StdErr;
    }

    public static class Proc
    {
        // Laufende Prozesse, damit "Abbrechen" sie beenden kann
        static readonly HashSet<Process> running = new HashSet<Process>();

        public static string Quote(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new char[] { ' ', '\t', '"' }) < 0) return arg;
            StringBuilder sb = new StringBuilder("\"");
            int backslashes = 0;
            foreach (char c in arg)
            {
                if (c == '\\') { backslashes++; continue; }
                if (c == '"') { sb.Append('\\', backslashes * 2 + 1); sb.Append('"'); backslashes = 0; continue; }
                sb.Append('\\', backslashes);
                backslashes = 0;
                sb.Append(c);
            }
            sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }

        public static string Join(IList<string> args)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string a in args)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(Quote(a));
            }
            return sb.ToString();
        }

        // Startet ein Programm, wartet auf das Ende und liefert Ausgabe und Exit-Code.
        // onStdOutLine erhält jede Zeile der Standardausgabe sofort (z.B. für -progress).
        // Bei Abbruch wird der Prozess beendet und OperationCanceledException geworfen.
        public static ProcResult Run(string exe, IList<string> args, CancellationToken token, Action<string> onStdOutLine)
        {
            ProcessStartInfo psi = new ProcessStartInfo(exe, Join(args));
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardInput = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            object gate = new object();

            using (Process p = new Process())
            {
                p.StartInfo = psi;
                p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    if (onStdOutLine != null) { try { onStdOutLine(e.Data); } catch { } }
                    lock (gate) { if (stdout.Length < 32 * 1024 * 1024) stdout.AppendLine(e.Data); }
                };
                p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (gate)
                    {
                        stderr.AppendLine(e.Data);
                        // nur das Ende behalten, ffmpeg kann sehr viel ausgeben
                        if (stderr.Length > 256 * 1024) stderr.Remove(0, stderr.Length - 128 * 1024);
                    }
                };

                token.ThrowIfCancellationRequested();
                p.Start();
                try { p.StandardInput.Close(); } catch { }
                lock (running) running.Add(p);
                try
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    while (!p.WaitForExit(200))
                    {
                        if (token.IsCancellationRequested)
                        {
                            try { p.Kill(); } catch { }
                            p.WaitForExit(5000);
                            token.ThrowIfCancellationRequested();
                        }
                    }
                    p.WaitForExit(); // Ausgabe vollständig einlesen
                }
                finally
                {
                    lock (running) running.Remove(p);
                }

                ProcResult r = new ProcResult();
                r.ExitCode = p.ExitCode;
                lock (gate)
                {
                    r.StdOut = stdout.ToString();
                    r.StdErr = stderr.ToString();
                }
                if (token.IsCancellationRequested) throw new OperationCanceledException(token);
                return r;
            }
        }

        public static void KillAll()
        {
            lock (running)
            {
                foreach (Process p in running)
                {
                    try { p.Kill(); } catch { }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Formatierung
    // ------------------------------------------------------------------

    public static class Fmt
    {
        public static string Size(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024 * 1024)).ToString("0.00", CultureInfo.InvariantCulture) + " GB";
            if (bytes >= 1024L * 1024) return (bytes / (1024.0 * 1024)).ToString("0", CultureInfo.InvariantCulture) + " MB";
            return (bytes / 1024.0).ToString("0", CultureInfo.InvariantCulture) + " KB";
        }

        public static string Time(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0) return "?";
            long s = (long)Math.Round(seconds);
            return (s / 3600) + ":" + (s % 3600 / 60).ToString("00") + ":" + (s % 60).ToString("00");
        }

        public static string Inv(double d, string format)
        {
            return d.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
