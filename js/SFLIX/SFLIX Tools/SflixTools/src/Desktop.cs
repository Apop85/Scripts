// Desktop (seit 4.0.0): STEFFLIX im eigenen Fenster (WebView2) und die Tools in einem Programm.
//   WebViewLibs      eingebettete WebView2-Bibliotheken laden, WebView2Loader.dll auspacken
//   WebViewApi       Runtime-Version (einzige Stelle ausser PlayerForm mit WebView2-Typen)
//   RuntimeInstaller fehlende WebView2-Runtime über den Bootstrapper von Microsoft installieren
//   Browsers         installierte Browser, STEFFLIX mit Adresse im Browser öffnen (Datenübernahme)
//   Shortcuts        Verknüpfungen auf dem Desktop und im Startmenü
//   ExeMigration     Umstieg von updateStefflix.exe auf STEFFLIX.exe
//   DesktopHost      verwaltet Player- und Tool-Fenster, zweiter Start holt das Fenster nach vorne

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Sflix
{
    // ---------------------------------------------------------------
    // WebView2-Bibliotheken: Core und WinForms sind als Ressourcen in der EXE (build.ps1) und werden
    // über AssemblyResolve geladen. WebView2Loader.dll ist nativ und wird je Architektur nach
    // %LOCALAPPDATA%\STEFFLIX\webview2\<SDK-Version>\<Architektur> ausgepackt.
    // Register() muss vor jedem Zugriff auf WebView2-Typen laufen (erste Zeile in Program.Main).
    // ---------------------------------------------------------------
    public static class WebViewLibs
    {
        public const string SdkVersion = "1.0.4258.31";   // gleich wie in build.ps1
        static bool registered;
        static readonly Dictionary<string, Assembly> loaded = new Dictionary<string, Assembly>();

        public static void Register()
        {
            if (registered) return;
            registered = true;
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            string name;
            try { name = new AssemblyName(args.Name).Name; }
            catch { return null; }
            if (name != "Microsoft.Web.WebView2.Core" && name != "Microsoft.Web.WebView2.WinForms") return null;
            lock (loaded)
            {
                Assembly a;
                if (loaded.TryGetValue(name, out a)) return a;
                byte[] data = Resource("webview2." + name + ".dll");
                if (data == null) return null;
                a = Assembly.Load(data);
                loaded[name] = a;
                return a;
            }
        }

        static byte[] Resource(string name)
        {
            using (Stream s = typeof(WebViewLibs).Assembly.GetManifestResourceStream(name))
            {
                if (s == null) return null;
                MemoryStream ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        // %LOCALAPPDATA%\STEFFLIX (auch ffmpeg liegt dort)
        public static string BaseFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "STEFFLIX"); }
        }

        // Architektur des laufenden Prozesses (AnyCPU: x64 auf 64-Bit-Windows, ARM64 mit .NET 4.8.1)
        static string Arch()
        {
            string a = (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "").ToUpperInvariant();
            if (a == "ARM64") return "arm64";
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        // Packt WebView2Loader.dll aus, falls sie fehlt oder anders ist. Ergebnis: Ordner oder null.
        public static string ExtractLoader(out string error)
        {
            error = null;
            string arch = Arch();
            string folder = Path.Combine(BaseFolder, "webview2", SdkVersion, arch);
            string file = Path.Combine(folder, "WebView2Loader.dll");
            try
            {
                byte[] data = Resource("webview2." + arch + ".WebView2Loader.dll");
                if (data == null) { error = "WebView2Loader.dll fehlt im Programm (" + arch + ")"; return null; }
                if (File.Exists(file) && SameContent(file, data)) return folder;
                Directory.CreateDirectory(folder);
                string tmp = file + "." + Process.GetCurrentProcess().Id + ".tmp";
                File.WriteAllBytes(tmp, data);
                try { FileOps.Replace(tmp, file); }
                catch
                {
                    // Von einem anderen STEFFLIX-Fenster belegt: gilt, wenn der Inhalt stimmt
                    if (!File.Exists(file) || !SameContent(file, data)) throw;
                }
                finally { FileOps.TryDelete(tmp); }
                return folder;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        static bool SameContent(string file, byte[] data)
        {
            byte[] have = File.ReadAllBytes(file);
            if (have.Length != data.Length) return false;
            for (int i = 0; i < have.Length; i++) if (have[i] != data[i]) return false;
            return true;
        }

        // Daten des Fensters (IndexedDB, Cache): je Installation getrennt wie im Browser über install.js.
        // Im Release-Ordner wird install.js beim Start gelöscht, dort gilt ein eigener Ordner.
        public static string UserDataFolder()
        {
            string id = "shared";
            if (App.IsReleaseFolder) id = "release";
            else
            {
                string folder;
                string installId = InstallId.Read(App.Root, out folder);
                if (installId != null) id = installId;
            }
            return Path.Combine(Path.Combine(BaseFolder, "webview"), id);
        }
    }

    // Einzige Klasse ausser PlayerForm, die WebView2-Typen verwendet (erst nach WebViewLibs.Register)
    public static class WebViewApi
    {
        static bool loaderSet;

        // Version der installierten WebView2-Runtime, null = fehlt
        public static string RuntimeVersion(string loaderFolder)
        {
            try
            {
                if (!loaderSet)
                {
                    Microsoft.Web.WebView2.Core.CoreWebView2Environment.SetLoaderDllFolderPath(loaderFolder);
                    loaderSet = true;
                }
                return Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException) { return null; }
        }
    }

    // ---------------------------------------------------------------
    // WebView2-Runtime installieren: Bootstrapper von Microsoft (etwa 2 MB, lädt den Rest selbst).
    // Die Datei ändert sich laufend, darum keine feste Prüfsumme: Sie muss gültig von Microsoft
    // signiert sein. Ohne Adminrechte installiert sie nur für den aktuellen Benutzer.
    // ---------------------------------------------------------------
    public static class RuntimeInstaller
    {
        const string BootstrapperUrl = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";

        // null = installiert, sonst Fehlertext
        public static string Install(CancellationToken token, Action<string> status)
        {
            string dir = Path.Combine(Path.GetTempPath(), "STEFFLIX");
            string file = Path.Combine(dir, "MicrosoftEdgeWebview2Setup.exe");
            try
            {
                Directory.CreateDirectory(dir);
                status("Lade das Setup von Microsoft …");
                byte[] data;
                try { data = Net.Get(BootstrapperUrl, token); }
                catch (DownloadException e) { return "Download nicht möglich: " + e.Message; }
                File.WriteAllBytes(file, data);
                status("Prüfe die Signatur …");
                string why = Signature.CheckMicrosoft(file);
                if (why != null) return why;
                status("Wird installiert, das kann einige Minuten dauern …");
                ProcessStartInfo psi = new ProcessStartInfo(file, "/silent /install");
                psi.UseShellExecute = true;   // falls Windows nach Adminrechten fragt
                using (Process p = Process.Start(psi))
                {
                    while (!p.WaitForExit(500)) token.ThrowIfCancellationRequested();
                    if (p.ExitCode != 0) return "Das Setup von Microsoft meldet einen Fehler (Code " + p.ExitCode + ").";
                }
                return null;
            }
            catch (OperationCanceledException) { return "Abgebrochen."; }
            catch (System.ComponentModel.Win32Exception e) { return "Das Setup konnte nicht gestartet werden: " + e.Message; }
            catch (Exception e) { return e.Message; }
            finally { FileOps.TryDelete(file); }
        }
    }

    // Authenticode-Prüfung einer Datei: gültige Signatur und Herausgeber Microsoft
    static class Signature
    {
        [DllImport("wintrust.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        static extern int WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid action, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        struct WinTrustFileInfo
        {
            public uint cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WinTrustData
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
            public IntPtr pSignatureSettings;
        }

        static readonly Guid GenericVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        // null = in Ordnung, sonst Grund
        public static string CheckMicrosoft(string file)
        {
            IntPtr path = Marshal.StringToHGlobalUni(file);
            IntPtr fileInfo = IntPtr.Zero, data = IntPtr.Zero;
            try
            {
                WinTrustFileInfo fi = new WinTrustFileInfo();
                fi.cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustFileInfo));
                fi.pcwszFilePath = path;
                fileInfo = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustFileInfo)));
                Marshal.StructureToPtr(fi, fileInfo, false);
                WinTrustData wd = new WinTrustData();
                wd.cbStruct = (uint)Marshal.SizeOf(typeof(WinTrustData));
                wd.dwUIChoice = 2;           // WTD_UI_NONE
                wd.fdwRevocationChecks = 0;  // WTD_REVOKE_NONE
                wd.dwUnionChoice = 1;        // WTD_CHOICE_FILE
                wd.pFile = fileInfo;
                wd.dwStateAction = 0;        // WTD_STATEACTION_IGNORE
                data = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WinTrustData)));
                Marshal.StructureToPtr(wd, data, false);
                int result = WinVerifyTrust(IntPtr.Zero, GenericVerifyV2, data);
                if (result != 0) return "Die heruntergeladene Datei ist nicht gültig signiert (Code 0x" + result.ToString("X8") + ").";
                X509Certificate cert = X509Certificate.CreateFromSignedFile(file);
                if (cert == null || cert.Subject.IndexOf("O=Microsoft Corporation", StringComparison.OrdinalIgnoreCase) < 0)
                    return "Die heruntergeladene Datei stammt nicht von Microsoft.";
                return null;
            }
            catch (Exception e) { return "Die Signatur konnte nicht geprüft werden: " + e.Message; }
            finally
            {
                Marshal.FreeHGlobal(path);
                if (fileInfo != IntPtr.Zero) Marshal.FreeHGlobal(fileInfo);
                if (data != IntPtr.Zero) Marshal.FreeHGlobal(data);
            }
        }
    }

    // ---------------------------------------------------------------
    // Browser für die Datenübernahme: STEFFLIX mit "?export=app" öffnen (über die Shell ginge der Teil
    // nach "?" bei file://-Adressen verloren, darum direkt mit dem Programm des Browsers)
    // ---------------------------------------------------------------
    public class BrowserInfo
    {
        public string Name;
        public string Exe;       // null = Standardprogramm von Windows
        public bool Firefox;
        public override string ToString() { return Name; }
    }

    public static class Browsers
    {
        static readonly string[][] Known =
        {
            new[] { "Microsoft Edge", "msedge.exe" },
            new[] { "Google Chrome", "chrome.exe" },
            new[] { "Firefox", "firefox.exe" },
            new[] { "Brave", "brave.exe" },
        };

        // Installierte Browser, der Standardbrowser zuerst
        public static List<BrowserInfo> Installed()
        {
            List<BrowserInfo> list = new List<BrowserInfo>();
            foreach (string[] k in Known)
            {
                string exe = AppPath(k[1]);
                if (exe == null) continue;
                BrowserInfo b = new BrowserInfo();
                b.Name = k[0];
                b.Exe = exe;
                b.Firefox = k[1] == "firefox.exe";
                list.Add(b);
            }
            string def = DefaultExe();
            if (def != null)
            {
                int index = list.FindIndex(delegate(BrowserInfo b) { return string.Equals(Path.GetFileName(b.Exe), Path.GetFileName(def), StringComparison.OrdinalIgnoreCase); });
                if (index > 0) { BrowserInfo b = list[index]; list.RemoveAt(index); list.Insert(0, b); }
                else if (index < 0)
                {
                    BrowserInfo b = new BrowserInfo();
                    b.Name = "Standardbrowser (" + Path.GetFileNameWithoutExtension(def) + ")";
                    b.Exe = def;
                    b.Firefox = Path.GetFileName(def).ToLowerInvariant().Contains("firefox");
                    list.Insert(0, b);
                }
            }
            if (list.Count == 0)
            {
                BrowserInfo b = new BrowserInfo();
                b.Name = "Standardbrowser";
                list.Add(b);
            }
            return list;
        }

        static string AppPath(string exe)
        {
            foreach (RegistryKey hive in new RegistryKey[] { Registry.CurrentUser, Registry.LocalMachine })
            {
                try
                {
                    using (RegistryKey key = hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe))
                    {
                        string path = key == null ? null : key.GetValue("") as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            path = path.Trim('"');
                            if (File.Exists(path)) return path;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        // Programm des Standardbrowsers (für http) oder null
        static string DefaultExe()
        {
            try
            {
                string progId;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice"))
                    progId = key == null ? null : key.GetValue("ProgId") as string;
                if (string.IsNullOrEmpty(progId)) return null;
                string command;
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(progId + @"\shell\open\command"))
                    command = key == null ? null : key.GetValue("") as string;
                if (string.IsNullOrEmpty(command)) return null;
                command = command.Trim();
                string exe;
                if (command.StartsWith("\"")) { int end = command.IndexOf('"', 1); exe = end > 0 ? command.Substring(1, end - 1) : null; }
                else { int end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase); exe = end > 0 ? command.Substring(0, end + 4) : null; }
                return exe != null && File.Exists(exe) ? exe : null;
            }
            catch { return null; }
        }

        public static void Open(BrowserInfo browser, string url)
        {
            if (browser == null || browser.Exe == null)
            {
                Process.Start(url);
                return;
            }
            string args = (browser.Firefox ? "-url " : "") + Proc.Quote(url);
            ProcessStartInfo psi = new ProcessStartInfo(browser.Exe, args);
            psi.UseShellExecute = false;
            Process.Start(psi);
        }

        // Download-Ordner des Benutzers
        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);

        public static string DownloadsFolder()
        {
            try
            {
                IntPtr p;
                if (SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero, out p) == 0)
                {
                    string s = Marshal.PtrToStringUni(p);
                    Marshal.FreeCoTaskMem(p);
                    if (!string.IsNullOrEmpty(s)) return s;
                }
            }
            catch { }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    // ---------------------------------------------------------------
    // Verknüpfungen (WScript.Shell über IDispatch, ohne zusätzliche Bibliothek)
    // ---------------------------------------------------------------
    public static class Shortcuts
    {
        public static string DesktopFolder { get { return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); } }
        public static string StartMenuFolder { get { return Environment.GetFolderPath(Environment.SpecialFolder.Programs); } }
        static string TaskbarFolder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar"); }
        }

        static object Shell()
        {
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            return t == null ? null : Activator.CreateInstance(t);
        }

        static object Call(object o, string method, params object[] args)
        {
            return o.GetType().InvokeMember(method, BindingFlags.InvokeMethod, null, o, args);
        }

        static object Get(object o, string prop)
        {
            return o.GetType().InvokeMember(prop, BindingFlags.GetProperty, null, o, null);
        }

        static void Set(object o, string prop, object value)
        {
            o.GetType().InvokeMember(prop, BindingFlags.SetProperty, null, o, new object[] { value });
        }

        // Symbol der Verknüpfungen (seit 4.0.4): lokal in %LOCALAPPDATA%\STEFFLIX\stefflix.ico. Liegt
        // STEFFLIX.exe auf einem Netzlaufwerk, liest die Taskleiste das Symbol beim Start über eine
        // Verknüpfung nicht aus der EXE und zeigt das Standardsymbol. Ergebnis: "Pfad,0" bzw. die EXE.
        public static string IconLocation(string exe)
        {
            string icon = LocalIcon();
            return (icon ?? exe) + ",0";
        }

        static string LocalIcon()
        {
            try
            {
                byte[] data;
                using (Stream s = typeof(Shortcuts).Assembly.GetManifestResourceStream("app.ico"))
                {
                    if (s == null) return null;
                    MemoryStream ms = new MemoryStream();
                    s.CopyTo(ms);
                    data = ms.ToArray();
                }
                string file = Path.Combine(WebViewLibs.BaseFolder, "stefflix.ico");
                bool same = false;
                if (File.Exists(file))
                {
                    byte[] have = File.ReadAllBytes(file);
                    same = have.Length == data.Length;
                    for (int i = 0; same && i < have.Length; i++) same = have[i] == data[i];
                }
                if (!same)
                {
                    Directory.CreateDirectory(WebViewLibs.BaseFolder);
                    string tmp = file + "." + Process.GetCurrentProcess().Id + ".tmp";
                    File.WriteAllBytes(tmp, data);
                    try { FileOps.Replace(tmp, file); }
                    finally { FileOps.TryDelete(tmp); }
                }
                return file;
            }
            catch { return null; }
        }

        // Beim Start (im Hintergrund, eigener STA-Thread): Verknüpfungen auf exe bekommen das lokale
        // Symbol, auch solche, die Windows beim Anheften an die Taskleiste selbst angelegt hat.
        public static void RefreshIconsInBackground(string exe)
        {
            Thread t = new Thread(delegate()
            {
                try
                {
                    string location = IconLocation(exe);
                    if (location.StartsWith(exe, StringComparison.OrdinalIgnoreCase)) return;   // kein lokales Symbol
                    object shell = Shell();
                    if (shell == null) return;
                    int changed = 0;
                    foreach (string lnk in AllShortcuts())
                    {
                        try
                        {
                            object link = Call(shell, "CreateShortcut", lnk);
                            if (!SamePath(Get(link, "TargetPath") as string, exe)) continue;
                            if (string.Equals(Get(link, "IconLocation") as string, location, StringComparison.OrdinalIgnoreCase)) continue;
                            Set(link, "IconLocation", location);
                            Call(link, "Save");
                            changed++;
                        }
                        catch { }
                    }
                    if (changed > 0) SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);   // SHCNE_ASSOCCHANGED: Symbole neu laden
                }
                catch { }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Start();
        }

        [DllImport("shell32.dll")]
        static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);

        // Verknüpfungen auf dem Desktop, in der Taskleiste und im Startmenü (mit Unterordnern)
        static List<string> AllShortcuts()
        {
            List<string> result = new List<string>();
            string[][] places = {
                new[] { DesktopFolder, "" }, new[] { TaskbarFolder, "" }, new[] { StartMenuFolder, "r" },
            };
            foreach (string[] place in places)
            {
                try
                {
                    if (string.IsNullOrEmpty(place[0]) || !Directory.Exists(place[0])) continue;
                    result.AddRange(Directory.GetFiles(place[0], "*.lnk", place[1] == "r" ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                }
                catch { }
            }
            return result;
        }

        static bool SamePath(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            try { return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        static string TargetOf(object shell, string lnk)
        {
            try { return Get(Call(shell, "CreateShortcut", lnk), "TargetPath") as string; }
            catch { return null; }
        }

        // Legt in folder eine Verknüpfung auf exe an, falls dort noch keine auf exe zeigt.
        // Ergebnis: Pfad der neuen Verknüpfung oder null.
        public static string Ensure(string folder, string exe)
        {
            try
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return null;
                object shell = Shell();
                if (shell == null) return null;
                foreach (string lnk in Directory.GetFiles(folder, "*.lnk"))
                    if (SamePath(TargetOf(shell, lnk), exe)) return null;
                string dir = Path.GetDirectoryName(exe);
                string path = Path.Combine(folder, "STEFFLIX.lnk");
                if (File.Exists(path))
                {
                    // Schon eine andere Installation: Ordnername dazu
                    string name = Path.GetFileName(dir.TrimEnd('\\'));
                    if (name.Length == 0) name = dir.Substring(0, 1);
                    path = Path.Combine(folder, "STEFFLIX (" + name + ").lnk");
                    if (File.Exists(path)) return null;
                }
                object link = Call(shell, "CreateShortcut", path);
                Set(link, "TargetPath", exe);
                Set(link, "WorkingDirectory", dir);
                Set(link, "IconLocation", IconLocation(exe));
                Set(link, "Description", "STEFFLIX");
                Call(link, "Save");
                return path;
            }
            catch { return null; }
        }

        // Biegt Verknüpfungen (Desktop, Startmenü, Taskleiste) von oldExe auf newExe um. Ergebnis: Anzahl
        public static int Redirect(string oldExe, string newExe)
        {
            int count = 0;
            object shell;
            try { shell = Shell(); }
            catch { return 0; }
            if (shell == null) return 0;
            string[][] places = {
                new[] { DesktopFolder, "" }, new[] { TaskbarFolder, "" }, new[] { StartMenuFolder, "r" },
            };
            foreach (string[] place in places)
            {
                string[] files;
                try
                {
                    if (string.IsNullOrEmpty(place[0]) || !Directory.Exists(place[0])) continue;
                    files = Directory.GetFiles(place[0], "*.lnk", place[1] == "r" ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                }
                catch { continue; }
                foreach (string lnk in files)
                {
                    try
                    {
                        object link = Call(shell, "CreateShortcut", lnk);
                        if (!SamePath(Get(link, "TargetPath") as string, oldExe)) continue;
                        Set(link, "TargetPath", newExe);
                        Set(link, "WorkingDirectory", Path.GetDirectoryName(newExe));
                        Set(link, "IconLocation", IconLocation(newExe));
                        Call(link, "Save");
                        count++;
                        // "updateStefflix.lnk" heisst danach "STEFFLIX.lnk" (nicht in der Taskleiste: Windows merkt sich den Namen)
                        string name = Path.GetFileNameWithoutExtension(lnk);
                        if (place[0] != TaskbarFolder && name.StartsWith("updateStefflix", StringComparison.OrdinalIgnoreCase))
                        {
                            string renamed = Path.Combine(Path.GetDirectoryName(lnk), "STEFFLIX" + name.Substring("updateStefflix".Length) + ".lnk");
                            if (!File.Exists(renamed)) File.Move(lnk, renamed);
                        }
                    }
                    catch { }
                }
            }
            return count;
        }
    }

    // ---------------------------------------------------------------
    // Umstieg: Updater bis 3.x laden das Programm als updateStefflix.exe. Startet diese Datei mit
    // Oberfläche, kopiert sie sich zu STEFFLIX.exe und startet diese mit "--migrated <alte Datei>".
    // STEFFLIX.exe biegt dann die Verknüpfungen um, legt fehlende an und löscht die alte Datei.
    // Ohne Oberfläche (z.B. Aufgabenplanung) läuft die alte Datei unverändert weiter.
    // Im Release-Ordner nie (dort liegen beide Dateien für ältere Updater).
    // ---------------------------------------------------------------
    public static class ExeMigration
    {
        public static bool RunsAsLegacyName
        {
            get { return string.Equals(Path.GetFileName(App.ExePath), App.LegacyExeName, StringComparison.OrdinalIgnoreCase); }
        }

        // true = STEFFLIX.exe wurde gestartet, dieses Programm beendet sich
        public static bool Run(string[] args)
        {
            try
            {
                string current = Path.GetFullPath(App.ExePath);
                string target = Path.Combine(Path.GetDirectoryName(current), App.ExeName);
                Version existing = FileVersionOf(target);
                if (existing == null || existing < App.Version)
                {
                    string tmp = target + ".new";
                    File.Copy(current, tmp, true);
                    FileOps.Replace(tmp, target);
                }
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLowerInvariant() == "--migrated") { i++; continue; }
                    sb.Append(Proc.Quote(args[i])).Append(' ');
                }
                sb.Append("--migrated ").Append(Proc.Quote(current));
                ProcessStartInfo psi = new ProcessStartInfo(target, sb.ToString());
                psi.UseShellExecute = false;
                psi.WorkingDirectory = Path.GetDirectoryName(current);
                Process.Start(psi);
                return true;
            }
            catch { return false; }
        }

        static Version FileVersionOf(string file)
        {
            try
            {
                if (!File.Exists(file)) return null;
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(file);
                return new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, 0);
            }
            catch { return null; }
        }

        // In STEFFLIX.exe nach dem Umstieg. Ergebnis: Hinweis für das Protokoll oder null.
        public static string Finish(string oldExe)
        {
            string current = Path.GetFullPath(App.ExePath);
            string old;
            try { old = Path.GetFullPath(oldExe); }
            catch { return null; }
            // Nur die alte Datei im selben Ordner, nie eine beliebige andere
            if (!string.Equals(Path.GetDirectoryName(old), Path.GetDirectoryName(current), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(old), App.LegacyExeName, StringComparison.OrdinalIgnoreCase)) return null;
            int redirected = Shortcuts.Redirect(old, current);
            int created = 0;
            if (Shortcuts.Ensure(Shortcuts.DesktopFolder, current) != null) created++;
            if (Shortcuts.Ensure(Shortcuts.StartMenuFolder, current) != null) created++;
            // Die alte Datei läuft vielleicht noch einen Moment
            ThreadPool.QueueUserWorkItem(delegate
            {
                for (int i = 0; i < 40 && File.Exists(old); i++)
                {
                    try { File.Delete(old); }
                    catch { Thread.Sleep(500); }
                }
            });
            string note = "Das Programm heisst jetzt " + App.ExeName + " (bisher " + App.LegacyExeName + ").";
            if (redirected > 0) note += " Verknüpfungen angepasst: " + redirected + ".";
            if (created > 0) note += " Neue Verknüpfungen auf dem Desktop bzw. im Startmenü: " + created + ".";
            return note;
        }
    }

    // ---------------------------------------------------------------
    // Fenster von STEFFLIX.exe: Player (WebView2) und Tools (MainForm) im selben Prozess.
    // Das Programm endet, wenn beide geschlossen sind. Schliessen des Players schliesst auch die Tools.
    // ---------------------------------------------------------------
    public class DesktopHost : ApplicationContext
    {
        public const int ShowPlayerCommand = 1, ShowToolsCommand = 2;

        readonly Settings settings;
        List<string> autoRun;
        string notice;
        readonly bool logToFile;
        PlayerForm player;
        MainForm tools;
        bool exitWhenToolsClosed, restarting, updateChecked, playerClosing;
        readonly ActivationWindow activation;

        public DesktopHost(Settings settings, List<string> autoRun, string notice, bool logToFile, bool openPlayer, bool openTools)
        {
            this.settings = settings;
            this.autoRun = autoRun;
            this.notice = notice;
            this.logToFile = logToFile;
            activation = new ActivationWindow(this);
            if (openPlayer) ShowPlayer();
            // Ohne Player (fehlende Runtime abgelehnt, im Browser geöffnet) bleibt es dabei, ausser die Tools sind verlangt
            if (openTools || !openPlayer) ShowTools(null);
            if (!HasWindows) activation.DestroyHandle();
        }

        // Zweiter Start: Nachricht an alle Fenster der obersten Ebene (siehe Program)
        public static int ActivationMessage(string root)
        {
            return RegisterWindowMessage("STEFFLIX-Activate-" + Program.Hash(root.ToLowerInvariant()));
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int RegisterWindowMessage(string name);

        public Settings Settings { get { return settings; } }
        public bool ToolsOpen { get { return tools != null; } }

        public void ShowPlayer()
        {
            if (player != null) { Bring(player); return; }
            string error;
            string loader = WebViewLibs.ExtractLoader(out error);
            string runtime = null;
            if (loader != null)
            {
                try { runtime = WebViewApi.RuntimeVersion(loader); }
                catch (Exception e) { error = e.Message; }
            }
            if (runtime == null)
            {
                RuntimeChoice choice;
                using (RuntimeDialog dlg = new RuntimeDialog(error)) choice = dlg.Ask(tools);
                if (choice == RuntimeChoice.Browser) { OpenInBrowser(); return; }
                if (choice != RuntimeChoice.Installed) return;
                try { runtime = WebViewApi.RuntimeVersion(loader ?? WebViewLibs.ExtractLoader(out error)); }
                catch (Exception e) { error = e.Message; }
                if (runtime == null)
                {
                    MessageBox.Show(tools, "Die Windows-Komponente ist installiert, lässt sich aber erst nach einem Neustart von STEFFLIX verwenden.",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            player = new PlayerForm(this);
            player.FormClosed += delegate
            {
                player = null;
                if (tools != null && (exitWhenToolsClosed || restarting)) return;   // Tools schliessen sich selbst
                ExitIfDone();
            };
            player.Show();
        }

        public static void OpenInBrowser()
        {
            try { Process.Start(Path.Combine(App.Root, "start.html")); }
            catch (Exception e)
            {
                MessageBox.Show("STEFFLIX konnte nicht im Browser geöffnet werden:\n" + e.Message, App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // page: updates, import, tools, license, help oder null
        public MainForm ShowTools(string page)
        {
            if (tools == null)
            {
                tools = new MainForm(settings, autoRun, notice, logToFile);
                autoRun = null;
                notice = null;
                // Ist der Player offen, prüft er beim Start auf Updates und zeigt den Hinweis
                tools.ShowUpdateDialogOnStart = player == null;
                tools.TasksFinished = OnTasksFinished;
                tools.ExtraRestartArgs = delegate { return player != null ? " --player" : ""; };
                tools.Restarting = delegate
                {
                    restarting = true;
                    if (player != null) player.CloseForRestart();
                };
                tools.FormClosed += delegate
                {
                    tools = null;
                    // Nicht, wenn der Player die Tools gerade selbst schliesst (er schliesst sich danach)
                    if ((exitWhenToolsClosed || restarting) && player != null && !playerClosing) player.CloseForRestart();
                    ExitIfDone();
                };
                // Unsichtbar aufbauen und erst fertig zeigen (sonst sieht man kurz das halbfertige
                // Fenster). Danach blendet der Player "Tools werden geöffnet …" aus.
                MainForm shown = tools;
                shown.Opacity = 0;
                shown.Shown += delegate
                {
                    shown.BeginInvoke((Action)delegate
                    {
                        if (shown.IsDisposed) return;
                        shown.Opacity = 1;
                        if (player != null) player.NotifyToolsShown();
                    });
                };
                tools.Show();
                if (page != null) tools.OpenPage(page);
                if (player == null) updateChecked = true;   // die Tools prüfen selbst
                return tools;
            }
            Bring(tools);
            if (page != null) tools.OpenPage(page);
            if (player != null) player.NotifyToolsShown();
            return tools;
        }

        static void Bring(Form f)
        {
            if (f.WindowState == FormWindowState.Minimized) f.WindowState = FormWindowState.Normal;
            f.Show();
            f.Activate();
        }

        void ExitIfDone()
        {
            if (player == null && tools == null)
            {
                activation.DestroyHandle();
                ExitThread();
            }
        }

        public bool HasWindows { get { return player != null || tools != null; } }

        // Player schliessen: true = darf schliessen. Die Tools werden mit geschlossen.
        public bool ConfirmPlayerClose(Form owner)
        {
            if (tools == null || restarting) return true;
            if (tools.IsRunning)
            {
                if (MessageBox.Show(owner, "In den Tools läuft noch eine Aufgabe. Abbrechen und STEFFLIX beenden?", App.Title,
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;
                exitWhenToolsClosed = true;
                playerClosing = true;
                tools.CancelAndClose();
                return true;
            }
            exitWhenToolsClosed = true;
            playerClosing = true;
            tools.Close();
            return true;
        }

        // Nach "Daten einlesen" oder einem Update: der Player lädt die Seite neu (oder bietet es an)
        void OnTasksFinished(List<string> tasks, TaskRunResult result)
        {
            if (player == null || result.Restart || result.Cancelled) return;
            if (tasks.Contains("data") || tasks.Contains("update") || tasks.Contains("import")) player.NotifyDataChanged();
        }

        // Der Player hat STEFFLIX geladen: einmal nach Updates suchen (wie bisher der Updater)
        public void CheckForUpdates(Form owner)
        {
            if (updateChecked || App.IsReleaseFolder) return;
            updateChecked = true;
            string ffmpegFolder = FfmpegLocation.Folder(settings);
            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateInfo info;
                try { info = UpdateCheck.Run(new FfmpegTools(App.Root, ffmpegFolder), CancellationToken.None); }
                catch { return; }   // ohne Verbindung: kein Hinweis
                if (!info.Any) return;
                try
                {
                    owner.BeginInvoke((Action)delegate
                    {
                        if (owner.IsDisposed || (tools != null && tools.IsRunning)) return;
                        bool now;
                        using (UpdateDialog dlg = new UpdateDialog(info, ShowPatchNotes)) now = dlg.Ask(owner);
                        if (now) ShowTools("updates").StartUpdate(info);
                    });
                }
                catch (InvalidOperationException) { }   // Fenster schon geschlossen
            });
        }

        void ShowPatchNotes(Form owner)
        {
            Sflix.MainForm.ShowPatchNotesFor(owner);   // "MainForm" allein wäre die Eigenschaft von ApplicationContext
        }

        // Nachricht eines zweiten Starts
        void OnActivate(int command)
        {
            if (command == ShowToolsCommand) ShowTools(null);
            else ShowPlayer();
            if (!HasWindows) ExitIfDone();
        }

        // Unsichtbares Fenster der obersten Ebene: empfängt die Nachricht eines zweiten Starts
        class ActivationWindow : NativeWindow
        {
            readonly DesktopHost host;
            readonly int message;

            public ActivationWindow(DesktopHost host)
            {
                this.host = host;
                message = ActivationMessage(App.Root);
                CreateParams cp = new CreateParams();
                cp.Caption = "STEFFLIX-Activation";
                CreateHandle(cp);
            }

            protected override void WndProc(ref Message m)
            {
                if (message != 0 && m.Msg == message)
                {
                    int command = (int)m.WParam;
                    SynchronizationContext ctx = SynchronizationContext.Current;
                    if (ctx != null) ctx.Post(delegate { host.OnActivate(command); }, null);
                    else host.OnActivate(command);
                    return;
                }
                base.WndProc(ref m);
            }
        }
    }
}
