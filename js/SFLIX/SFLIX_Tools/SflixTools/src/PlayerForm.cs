// Player-Fenster (seit 4.0.0): STEFFLIX (start.html über file://) in einer WebView2, ohne Adresszeile,
// Linkvorschau, Kontextmenü und Zoom, wie die Android-App. Fremde Links öffnen im Standardbrowser.
//
// Vollbild: F11 (meldet STEFFLIX über die Brücke) oder Vollbild eines Videos; beim Verlassen des
// Video-Vollbilds kehrt das Fenster in den vorherigen Zustand zurück (war es per F11 im Vollbild, bleibt es).
//
// Brücke (window.chrome.webview, siehe initDesktopApp in ui.js):
//   STEFFLIX -> Programm (JSON-Text): {cmd:"ready"}, {cmd:"tools", page}, {cmd:"fullscreen"}, {cmd:"reload"},
//                                     {cmd:"importResult", ok, message}, {cmd:"pip", on, paused},
//                                     {cmd:"damaged", path, at, skipped}, {cmd:"damagedRemove", path} (DEC-374)
//
// Bild-im-Bild (DEC-341, DEC-344): Das BiB ist das native Fenster von Chromium (gehört dem Prozess
// msedgewebview2.exe). Beim Start ({cmd:"pip", on:true}) merkt sich das Fenster seinen Zustand und
// minimiert sich; das BiB-Fenster bekommt das Symbol app_pip.ico (Suche über BrowserProcessId,
// WM_SETICON). Beim Ende ({cmd:"pip", on:false, paused}) unterscheidet der Pausenzustand:
// paused=false ("Zurück zum Tab") stellt den vorherigen Zustand im Vordergrund wieder her,
// paused=true (X, Chromium pausiert beim Schliessen) lässt das Fenster minimiert. Heuristik über das
// Verhalten von Chromium: findet sich das Fenster nicht, passiert still nichts.
//   Programm -> STEFFLIX (JSON):      {event:"dataChanged"}, {event:"importBackup", text}, {event:"toolsShown"}
// DevTools und Kontextmenü nur mit "player.devtools": true in updater_settings.json oder SFLIX_DEVTOOLS=1.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Sflix
{
    public class PlayerForm : Form
    {
        readonly DesktopHost host;
        readonly Settings settings;
        readonly WebView2 web;
        readonly string rootUri;          // file:///…/ des Installationsordners (entschlüsselt, klein)
        bool fullscreen, fullscreenByVideo, readyOnce, closingForRestart, coreReady;
        FormWindowState stateBeforeFull;
        Rectangle boundsBeforeFull;
        DataImportDialog importDialog;
        // Bild-im-Bild: aktiv, Fensterzustand vor dem Minimieren, Suche nach dem BiB-Fenster
        bool pipActive;
        FormWindowState stateBeforePip;
        Timer pipIconTimer;
        int pipIconTries;
        Icon pipIconSmall, pipIconBig;
        // Eigenes BiB-Fenster (Document Picture-in-Picture, DEC-351): Standard ein, abschalten mit
        // "player.docpip": false in updater_settings.json oder SFLIX_DOCPIP=0 (dann natives BiB)
        bool docPipEnabled;
        int docPipWidth, docPipHeight;
        DateTime docPipRequested = DateTime.MinValue;
        DocPipWindow docPipWindow;
        // Hintergrund bis STEFFLIX geladen ist (wie --sflix-bg in sflix.css), damit nichts aufblitzt
        static readonly Color PageBack = Color.FromArgb(0x10, 0x10, 0x13);

        public PlayerForm(DesktopHost host)
        {
            this.host = host;
            settings = host.Settings;
            Text = App.Title;
            BackColor = PageBack;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            try
            {
                using (Stream s = typeof(PlayerForm).Assembly.GetManifestResourceStream("app.ico"))
                    if (s != null) Icon = new Icon(s);
            }
            catch { }
            using (Graphics g = CreateGraphics()) Theme.Scale = g.DpiX / 96f;
            MinimumSize = new Size(Theme.S(480), Theme.S(320));
            RestoreWindowBounds();
            KeyPreview = true;

            rootUri = NormalizeUri(new Uri(Path.GetFullPath(App.Root).TrimEnd('\\') + "\\").AbsoluteUri);

            web = new WebView2();
            web.Dock = DockStyle.Fill;
            web.DefaultBackgroundColor = PageBack;
            web.AllowExternalDrop = false;
            Controls.Add(web);
        }

        static bool DevTools(Settings s)
        {
            return s.GetBool("player.devtools", false) || Environment.GetEnvironmentVariable("SFLIX_DEVTOOLS") == "1";
        }

        // ---------------------------------------------------------------
        // Fensterlage
        // ---------------------------------------------------------------

        void RestoreWindowBounds()
        {
            StartPosition = FormStartPosition.Manual;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            Rectangle wanted = new Rectangle(0, 0, Math.Min(Theme.S(1280), work.Width - 40), Math.Min(Theme.S(800), work.Height - 40));
            wanted.X = work.X + (work.Width - wanted.Width) / 2;
            wanted.Y = work.Y + (work.Height - wanted.Height) / 2;
            string saved = settings.GetString("player.bounds", "");
            string[] p = saved.Split(',');
            int x, y, w, h;
            if (p.Length == 4 && int.TryParse(p[0], out x) && int.TryParse(p[1], out y) && int.TryParse(p[2], out w) && int.TryParse(p[3], out h)
                && w >= MinimumSize.Width && h >= MinimumSize.Height)
            {
                Rectangle r = new Rectangle(x, y, w, h);
                // Nur übernehmen, wenn die Titelleiste auf einem Bildschirm liegt
                foreach (Screen s in Screen.AllScreens)
                    if (s.WorkingArea.IntersectsWith(new Rectangle(r.X + 40, r.Y, Math.Max(1, r.Width - 80), 30))) { wanted = r; break; }
            }
            Bounds = wanted;
            if (settings.GetBool("player.maximized", false)) WindowState = FormWindowState.Maximized;
        }

        void SaveWindowBounds()
        {
            Rectangle r;
            bool maximized;
            if (fullscreen) { r = boundsBeforeFull; maximized = stateBeforeFull == FormWindowState.Maximized; }
            else { r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds; maximized = WindowState == FormWindowState.Maximized; }
            if (r.Width <= 0 || r.Height <= 0) return;
            settings.SetString("player.bounds", r.X + "," + r.Y + "," + r.Width + "," + r.Height);
            settings.SetBool("player.maximized", maximized);
            settings.Save();
        }

        // Dunkle Titelleiste (Windows 10 ab 20H1: Attribut 20, ältere Builds 19)
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
            }
            catch { }
        }

        // ---------------------------------------------------------------
        // WebView2
        // ---------------------------------------------------------------

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            InitWebView();
        }

        const string LegacyBrowserArguments = "--autoplay-policy=no-user-gesture-required";
        const string BrowserArguments = LegacyBrowserArguments + " --disable-features=BackgroundVideoTrackOptimization,BackgroundVideoPauseOptimization";

        static System.Threading.Tasks.Task<CoreWebView2Environment> CreateEnvironment(string arguments)
        {
            return CoreWebView2Environment.CreateAsync(null, WebViewLibs.UserDataFolder(), new CoreWebView2EnvironmentOptions(arguments));
        }

        async void InitWebView()
        {
            try
            {
                // Autoplay ohne vorherigen Klick (nächste Folge, Musik). Videospur nicht abschalten, wenn die
                // Seite verborgen ist: beim eigenen Bild-im-Bild ist das Hauptfenster minimiert, das Video
                // läuft im BiB-Fenster weiter (sonst nur Ton und ein stehendes Bild, SFLIX-20261123-095)
                bool legacy = false;
                try { await web.EnsureCoreWebView2Async(await CreateEnvironment(BrowserArguments)); }
                catch (ArgumentException) { legacy = true; }   // Umgebung läuft schon mit anderen Argumenten (gleicher Datenordner)
                catch (COMException e)
                {
                    if (e.ErrorCode != unchecked((int)0x8007139F)) throw;   // ERROR_INVALID_STATE: andere Optionen
                    legacy = true;
                }
                if (legacy) await web.EnsureCoreWebView2Async(await CreateEnvironment(LegacyBrowserArguments));
            }
            catch (Exception ex)
            {
                if (IsDisposed) return;
                if (MessageBox.Show(this, "STEFFLIX konnte nicht im eigenen Fenster geöffnet werden:\n" + ex.Message +
                                    "\n\nStattdessen im Browser öffnen?", App.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    DesktopHost.OpenInBrowser();
                closingForRestart = true;   // ohne Nachfrage schliessen
                Close();
                return;
            }
            if (IsDisposed) return;
            CoreWebView2 core = web.CoreWebView2;
            bool dev = DevTools(settings);
            CoreWebView2Settings s = core.Settings;
            s.AreDefaultContextMenusEnabled = dev;
            s.AreDevToolsEnabled = dev;
            s.IsStatusBarEnabled = false;            // keine Linkvorschau unten links
            s.IsZoomControlEnabled = false;          // kein Zoom mit Strg+Mausrad / Strg+Plus
            s.AreBrowserAcceleratorKeysEnabled = dev; // kein Drucken, Suchen, Neu laden usw. des Browsers
            s.AreDefaultScriptDialogsEnabled = true;  // STEFFLIX nutzt confirm()
            Optional(delegate { s.IsSwipeNavigationEnabled = false; });
            Optional(delegate { s.IsPinchZoomEnabled = false; });
            Optional(delegate { s.IsGeneralAutofillEnabled = false; });
            Optional(delegate { s.IsPasswordAutosaveEnabled = false; });

            core.NavigationStarting += OnNavigationStarting;
            core.NewWindowRequested += OnNewWindowRequested;
            core.WebMessageReceived += OnWebMessage;
            core.ContainsFullScreenElementChanged += OnFullScreenElementChanged;
            core.DownloadStarting += OnDownloadStarting;
            core.ProcessFailed += OnProcessFailed;
            docPipEnabled = settings.GetBool("player.docpip", true) && Environment.GetEnvironmentVariable("SFLIX_DOCPIP") != "0";
            if (docPipEnabled)
            {
                try { await core.AddScriptToExecuteOnDocumentCreatedAsync("window.sflixDocPip = true;"); }
                catch { docPipEnabled = false; }
            }
            coreReady = true;
            core.Navigate(new Uri(Path.Combine(App.Root, "start.html")).AbsoluteUri);
        }

        // Einstellungen, die eine ältere WebView2-Runtime noch nicht kennt, überspringen
        static void Optional(Action set)
        {
            try { set(); }
            catch { }
        }

        // Vergleichbare Form einer file://-Adresse: entschlüsselt, Gross-/Kleinschreibung egal
        static string NormalizeUri(string uri)
        {
            try { return Uri.UnescapeDataString(uri ?? "").Replace('\\', '/').ToLowerInvariant(); }
            catch { return (uri ?? "").ToLowerInvariant(); }
        }

        // Seite von STEFFLIX (im Installationsordner) oder ein interner Inhalt
        bool IsInternal(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return false;
            string u = NormalizeUri(uri);
            return u.StartsWith(rootUri) || u.StartsWith("about:") || u.StartsWith("data:") || u.StartsWith("blob:");
        }

        static void OpenExternal(string uri)
        {
            try
            {
                Uri u = new Uri(uri);
                if (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeMailto) Process.Start(u.AbsoluteUri);
            }
            catch { }
        }

        void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsInternal(e.Uri))
            {
                // Neu laden beendet ein laufendes BiB ohne Meldung der Seite. Das eigene BiB-Fenster
                // schliessen: sein Dokument gehört zur alten Seite und würde jedes neue BiB blockieren.
                ResetPip();
                if (docPipWindow != null && !docPipWindow.IsDisposed) docPipWindow.Close();
                docPipWindow = null;
                return;
            }
            e.Cancel = true;
            OpenExternal(e.Uri);
        }

        void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            string uri = e.Uri ?? "";
            // Leere Fenster (about:blank, z.B. documentPictureInPicture.requestWindow) ersetzen nie den Player
            if (uri.Length == 0 || uri.StartsWith("about:blank", StringComparison.OrdinalIgnoreCase))
            {
                if (docPipEnabled && docPipWindow == null && (DateTime.Now - docPipRequested).TotalSeconds < 5)
                {
                    docPipRequested = DateTime.MinValue;
                    OpenDocPipWindow(e);
                }
                else Trace.WriteLine("STEFFLIX: leeres Fenster abgelehnt (" + uri + ")");
                return;
            }
            if (IsInternal(uri)) web.CoreWebView2.Navigate(uri);
            else OpenExternal(uri);
        }

        // Angemeldetes BiB-Fenster mit eigener WebView2 (gleiche Umgebung) beantworten
        async void OpenDocPipWindow(CoreWebView2NewWindowRequestedEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            DocPipWindow w = null;
            try
            {
                LoadPipIcons();
                w = new DocPipWindow(this, settings, docPipWidth, docPipHeight, pipIconBig ?? Icon, DevTools(settings));
                w.FormClosed += delegate
                {
                    if (docPipWindow == w) docPipWindow = null;
                    PostEvent("pipWindowClosed");   // falls pagehide im BiB-Dokument ausbleibt
                };
                w.Show();
                await w.Web.EnsureCoreWebView2Async(web.CoreWebView2.Environment);
                w.Attach();
                e.NewWindow = w.Web.CoreWebView2;
                docPipWindow = w;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("STEFFLIX: eigenes BiB-Fenster nicht möglich: " + ex.Message);
                if (w != null && !w.IsDisposed) w.Close();
            }
            finally { deferral.Complete(); }
        }

        void OnProcessFailed(object sender, CoreWebView2ProcessFailedEventArgs e)
        {
            if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessExited
                || e.ProcessFailedKind == CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
            {
                try { web.CoreWebView2.Reload(); }
                catch { }
                return;
            }
            if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
            {
                BeginInvoke((Action)delegate
                {
                    MessageBox.Show(this, "Die Anzeige von STEFFLIX wurde unerwartet beendet. Bitte STEFFLIX neu öffnen.", App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    closingForRestart = true;
                    Close();
                });
            }
        }

        // Downloads (z.B. Datensicherung exportieren): Speicherort wie gewohnt wählen
        void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            CoreWebView2Deferral deferral = e.GetDeferral();
            e.Handled = true;   // ohne die Download-Leiste des Browsers
            string suggested = e.ResultFilePath;
            BeginInvoke((Action)delegate
            {
                try
                {
                    using (SaveFileDialog dlg = new SaveFileDialog())
                    {
                        dlg.Title = "Speichern unter";
                        dlg.FileName = Path.GetFileName(suggested);
                        dlg.InitialDirectory = Browsers.DownloadsFolder();
                        string ext = Path.GetExtension(suggested);
                        dlg.Filter = (string.IsNullOrEmpty(ext) ? "" : "*" + ext + "|*" + ext + "|") + "Alle Dateien|*.*";
                        if (dlg.ShowDialog(this) == DialogResult.OK) e.ResultFilePath = dlg.FileName;
                        else e.Cancel = true;
                    }
                }
                catch { e.Cancel = true; }
                finally { deferral.Complete(); }
            });
        }

        void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (!IsInternal(e.Source)) return;
            JsonObject msg;
            try { msg = Json.Parse(e.TryGetWebMessageAsString()) as JsonObject; }
            catch { return; }
            if (msg == null) return;
            switch (msg.Str("cmd") ?? "")
            {
                case "ready":
                    if (!readyOnce)
                    {
                        readyOnce = true;
                        BeginInvoke((Action)OnFirstReady);
                    }
                    break;
                case "tools":
                    string page = msg.Str("page");
                    BeginInvoke((Action)delegate { host.ShowTools(page); });
                    break;
                case "fullscreen":
                    ToggleFullscreen();
                    break;
                case "reload":
                    web.CoreWebView2.Reload();
                    break;
                case "importResult":
                    if (importDialog != null) importDialog.ImportFinished(msg.Bool("ok", false), msg.Str("message"));
                    break;
                case "pipWindow":
                    // Nächste leere Fensteranfrage (requestWindow) ist das BiB-Fenster
                    if (!docPipEnabled) break;
                    docPipWidth = Math.Max(240, Math.Min(1920, (int)msg.Num("width", 480)));
                    docPipHeight = Math.Max(160, Math.Min(1080, (int)msg.Num("height", 320)));
                    docPipRequested = DateTime.Now;
                    PostEvent("pipWindowReady");
                    break;
                case "pipDrag":
                    // Videobild im BiB-Fenster gezogen -> Fenster verschieben
                    DocPipWindow drag = docPipWindow;
                    if (drag != null) BeginInvoke((Action)drag.BeginDrag);
                    break;
                case "pip":
                    bool on = msg.Bool("on", false);
                    bool paused = msg.Bool("paused", true);
                    BeginInvoke((Action)delegate { OnPip(on, paused); });
                    break;
                case "damaged":
                    // Player hat eine beschädigte Stelle übersprungen (DEC-373/374): Liste und player.log.
                    // Datei im Hintergrund (Netzlaufwerk), Pfad nur innerhalb des Installationsordners.
                    string damagedPath = msg.Str("path");
                    double at = msg.Num("at", -1), skipped = msg.Num("skipped", 5);
                    System.Threading.ThreadPool.QueueUserWorkItem(delegate { DamagedList.ReportFromPlayer(damagedPath, at, skipped); });
                    break;
                case "damagedRemove":
                    string removePath = msg.Str("path");
                    if (DamagedList.FromWebPath(removePath) != null)
                        System.Threading.ThreadPool.QueueUserWorkItem(delegate { DamagedList.RemoveWeb(removePath); });
                    break;
            }
        }

        // ---------------------------------------------------------------
        // Bild-im-Bild
        // ---------------------------------------------------------------

        void OnPip(bool on, bool paused)
        {
            if (IsDisposed) return;
            if (on)
            {
                if (pipActive) return;
                pipActive = true;
                // Normal, maximiert oder eigener Vollbildmodus (randlos, Zustand Normal, fullscreen bleibt gesetzt)
                stateBeforePip = WindowState == FormWindowState.Minimized ? FormWindowState.Normal : WindowState;
                // Eigenes BiB-Fenster hat sein Symbol schon; nur im Rückfall das native Fenster von Chromium suchen
                if (docPipWindow == null) StartPipIconSearch();
                if (docPipWindow != null && !docPipWindow.IsDisposed)
                {
                    // Eigenes BiB: Hauptfenster ausblenden statt minimieren. Minimiert friert die Darstellung
                    // des BiB-WebView2 ein und wird schwarz (SFLIX-20261123-101). Nur das Fenster, nicht die
                    // WebView2 (Visible = false würde die Seite als verborgen melden). Taskleiste: BiB-Symbol.
                    hiddenForPip = true;
                    ShowWindow(Handle, SW_HIDE);
                }
                else WindowState = FormWindowState.Minimized;
                return;
            }
            if (!pipActive) return;
            if (hiddenForPip && !paused)
            {
                hiddenForPip = false;
                ShowWindow(Handle, SW_SHOW);   // im Zustand vor dem BiB (normal, maximiert, Vollbild)
            }
            ResetPip();
            if (paused) return;   // X: Benutzer will etwas anderes tun, Fenster bleibt minimiert
            if (WindowState == FormWindowState.Minimized) WindowState = stateBeforePip;
            Activate();
        }

        // BiB beendet (auch ohne Meldung: Neu laden, Schliessen)
        void ResetPip()
        {
            pipActive = false;
            if (pipIconTimer != null) pipIconTimer.Stop();
            // Ausgeblendetes Hauptfenster nie verloren gehen lassen: minimiert zurück in die Taskleiste
            if (hiddenForPip)
            {
                hiddenForPip = false;
                if (IsHandleCreated && !IsDisposed) ShowWindow(Handle, SW_SHOWMINNOACTIVE);
            }
        }

        // Zweiter Start oder Tools holen den Player nach vorne, auch während des eigenen BiB
        public void ShowIfHiddenForPip()
        {
            if (!hiddenForPip) return;
            hiddenForPip = false;
            ShowWindow(Handle, SW_SHOW);
        }

        bool hiddenForPip;
        const int SW_HIDE = 0, SW_SHOWMINNOACTIVE = 7, SW_SHOW = 5;
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hwnd, int cmd);

        // Das BiB-Fenster erscheint erst kurz nach enterpictureinpicture: einige Versuche bis etwa 2 s
        void StartPipIconSearch()
        {
            if (!LoadPipIcons()) return;
            if (pipIconTimer == null)
            {
                pipIconTimer = new Timer();
                pipIconTimer.Interval = 200;
                pipIconTimer.Tick += delegate
                {
                    pipIconTries++;
                    bool found = pipActive && SetPipWindowIcon();
                    if (found || !pipActive || pipIconTries >= 10)
                    {
                        pipIconTimer.Stop();
                        if (!found && pipActive) Trace.WriteLine("STEFFLIX: Fenster des Bild-im-Bild nicht gefunden, Symbol nicht gesetzt");
                    }
                };
            }
            pipIconTries = 0;
            pipIconTimer.Stop();
            pipIconTimer.Start();
        }

        bool LoadPipIcons()
        {
            if (pipIconSmall != null) return true;
            try
            {
                using (Stream s = typeof(PlayerForm).Assembly.GetManifestResourceStream("app_pip.ico"))
                {
                    if (s == null) return false;
                    byte[] data = new byte[s.Length];
                    int read = 0;
                    while (read < data.Length)
                    {
                        int n = s.Read(data, read, data.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    using (MemoryStream a = new MemoryStream(data)) pipIconSmall = new Icon(a, SystemInformation.SmallIconSize);
                    using (MemoryStream b = new MemoryStream(data)) pipIconBig = new Icon(b, SystemInformation.IconSize);
                }
                return true;
            }
            catch
            {
                pipIconSmall = null;
                pipIconBig = null;
                return false;
            }
        }

        // Sichtbare Top-Level-Fenster der Klasse Chrome_WidgetWin_1 im Browserprozess der WebView2,
        // ohne Hilfsfenster (Tooltips, Auswahllisten) und ohne das eigene Fenster
        bool SetPipWindowIcon()
        {
            uint browserPid;
            try { browserPid = web.CoreWebView2.BrowserProcessId; }
            catch { return false; }
            IntPtr own = Handle;
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate (IntPtr hwnd, IntPtr param)
            {
                if (hwnd == own || !IsWindowVisible(hwnd)) return true;
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != browserPid) return true;
                StringBuilder cls = new StringBuilder(64);
                GetClassName(hwnd, cls, cls.Capacity);
                if (cls.ToString() != "Chrome_WidgetWin_1") return true;
                long exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;
                found = hwnd;
                return false;
            }, IntPtr.Zero);
            if (found == IntPtr.Zero) return false;
            SendMessage(found, WM_SETICON, (IntPtr)ICON_SMALL, pipIconSmall.Handle);
            SendMessage(found, WM_SETICON, (IntPtr)ICON_BIG, pipIconBig.Handle);
            return true;
        }

        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr param);
        const int WM_SETICON = 0x0080, ICON_SMALL = 0, ICON_BIG = 1, GWL_EXSTYLE = -20;
        const long WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr param);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr hwnd, StringBuilder name, int maxCount);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        static extern int GetWindowLong32(IntPtr hwnd, int index);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        static IntPtr GetWindowLongPtr(IntPtr hwnd, int index)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hwnd, index) : new IntPtr(GetWindowLong32(hwnd, index));
        }

        // STEFFLIX ist zum ersten Mal geladen: einmalig die Datenübernahme anbieten, dann nach Updates suchen
        void OnFirstReady()
        {
            if (IsDisposed) return;
            if (!App.IsReleaseFolder && !settings.GetBool("player.dataImportAsked", false))
            {
                using (importDialog = new DataImportDialog(this))
                    importDialog.ShowDialog(this);
                importDialog = null;
                settings.SetBool("player.dataImportAsked", true);
                settings.Save();
            }
            if (!IsDisposed) host.CheckForUpdates(this);
        }

        // Datensicherung (JSON-Text) an STEFFLIX übergeben; Ergebnis kommt als "importResult"
        public void ImportBackup(string text)
        {
            if (!coreReady) return;
            JsonObject o = new JsonObject();
            o["event"] = "importBackup";
            o["text"] = text;
            web.CoreWebView2.PostWebMessageAsJson(Json.Write(o));
        }

        // Nach "Daten einlesen" oder einem Update in den Tools
        public void NotifyDataChanged()
        {
            PostEvent("dataChanged");
        }

        // Das Fenster der Tools ist sichtbar: Hinweis "Tools werden geöffnet …" ausblenden
        public void NotifyToolsShown()
        {
            PostEvent("toolsShown");
        }

        void PostEvent(string name)
        {
            if (!coreReady || IsDisposed) return;
            JsonObject o = new JsonObject();
            o["event"] = name;
            try { web.CoreWebView2.PostWebMessageAsJson(Json.Write(o)); }
            catch { }
        }

        // ---------------------------------------------------------------
        // Vollbild
        // ---------------------------------------------------------------

        void SetFullscreen(bool on)
        {
            if (on == fullscreen) return;
            SuspendLayout();
            if (on)
            {
                stateBeforeFull = WindowState;
                boundsBeforeFull = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                Screen screen = Screen.FromControl(this);
                if (WindowState != FormWindowState.Normal) WindowState = FormWindowState.Normal;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = screen.Bounds;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = boundsBeforeFull;
                if (stateBeforeFull == FormWindowState.Maximized) WindowState = FormWindowState.Maximized;
            }
            fullscreen = on;
            ResumeLayout(true);
        }

        // F11
        void ToggleFullscreen()
        {
            if (fullscreen)
            {
                bool video = coreReady && web.CoreWebView2.ContainsFullScreenElement;
                fullscreenByVideo = false;
                SetFullscreen(false);
                if (video)
                {
                    try { web.CoreWebView2.ExecuteScriptAsync("document.exitFullscreen && document.exitFullscreen()"); }
                    catch { }
                }
            }
            else
            {
                fullscreenByVideo = false;
                SetFullscreen(true);
            }
        }

        // Video (bzw. Player von STEFFLIX) im Vollbild: das Fenster auch
        void OnFullScreenElementChanged(object sender, object e)
        {
            bool element = web.CoreWebView2.ContainsFullScreenElement;
            if (element && !fullscreen)
            {
                fullscreenByVideo = true;
                SetFullscreen(true);
            }
            else if (!element && fullscreenByVideo)
            {
                fullscreenByVideo = false;
                SetFullscreen(false);
            }
        }

        // Tasten, wenn die Seite den Fokus nicht hat (sonst meldet STEFFLIX F11 und F5 über die Brücke)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F11) { ToggleFullscreen(); return true; }
            if (keyData == Keys.F5 && coreReady) { web.CoreWebView2.Reload(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ---------------------------------------------------------------
        // Schliessen
        // ---------------------------------------------------------------

        // Ohne Nachfrage schliessen (Neustart nach Selbst-Update, Tools beenden das Programm)
        public void CloseForRestart()
        {
            closingForRestart = true;
            if (!IsDisposed) Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!closingForRestart && !host.ConfirmPlayerClose(this))
            {
                e.Cancel = true;
                return;
            }
            ResetPip();
            SaveWindowBounds();
            // Programmende schliesst das BiB-Fenster (merkt dabei seine Lage)
            if (docPipWindow != null && !docPipWindow.IsDisposed) docPipWindow.Close();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { web.Dispose(); }
                catch { }
                if (docPipWindow != null && !docPipWindow.IsDisposed) docPipWindow.Close();
                if (pipIconTimer != null) pipIconTimer.Dispose();
                if (pipIconSmall != null) pipIconSmall.Dispose();
                if (pipIconBig != null) pipIconBig.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // Fenster für Document Picture-in-Picture (DEC-351). Die WebView2 wird als e.NewWindow der Anfrage
    // von documentPictureInPicture.requestWindow übergeben; ihr Dokument füllt player.js (Video mit
    // Bedienelementen über dem Bild). Immer im Vordergrund, eigenes Symbol, Titel = Videotitel.
    // Lage und Breite in "player.pipBounds" (x,y,Breite,Höhe), Höhe beim Öffnen im Seitenverhältnis des Videos.
    class DocPipWindow : Form
    {
        public readonly WebView2 Web;
        readonly bool devTools;
        readonly Settings settings;

        public DocPipWindow(Form owner, Settings settings, int width, int height, Icon icon, bool devTools)
        {
            this.devTools = devTools;
            this.settings = settings;
            Text = App.Title;
            if (icon != null) Icon = icon;
            TopMost = true;
            ShowInTaskbar = true;
            BackColor = Color.Black;
            // Ohne Titelleiste: nur Bild und Bedienleiste. Verschieben über das Videobild (player.js meldet
            // {cmd:"pipDrag"}, BeginDrag), Grösse über einen schmalen Rand (WndProc, WM_NCHITTEST).
            FormBorderStyle = FormBorderStyle.None;
            Padding = new Padding(ResizeBorder);
            MinimumSize = new Size(Theme.S(240), Theme.S(150));
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(Theme.S(width), Theme.S(height));
            // Standard: unten rechts auf dem Bildschirm des Players
            Rectangle work = Screen.FromControl(owner).WorkingArea;
            Location = new Point(work.Right - Width - Theme.S(24), work.Bottom - Height - Theme.S(24));
            ApplySavedBounds(width, height);
            Web = new WebView2();
            Web.Dock = DockStyle.Fill;
            Web.DefaultBackgroundColor = Color.Black;
            Web.AllowExternalDrop = false;
            Controls.Add(Web);
        }

        // Nach EnsureCoreWebView2Async, vor e.NewWindow
        public void Attach()
        {
            CoreWebView2 core = Web.CoreWebView2;
            CoreWebView2Settings s = core.Settings;
            s.AreDefaultContextMenusEnabled = devTools;
            s.AreDevToolsEnabled = devTools;
            s.IsStatusBarEnabled = false;
            s.IsZoomControlEnabled = false;
            s.AreBrowserAcceleratorKeysEnabled = devTools;
            core.DocumentTitleChanged += delegate
            {
                string title = core.DocumentTitle;
                if (!string.IsNullOrEmpty(title) && !title.StartsWith("about:")) Text = title;
            };
            core.WindowCloseRequested += delegate { if (!IsDisposed) BeginInvoke((Action)Close); };
            core.NewWindowRequested += delegate(object sender, CoreWebView2NewWindowRequestedEventArgs e) { e.Handled = true; };
            core.NavigationStarting += delegate(object sender, CoreWebView2NavigationStartingEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Uri) && !e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) e.Cancel = true;
            };
        }

        // Gemerkte Lage und Breite übernehmen, Höhe im gewünschten Seitenverhältnis; immer ganz auf
        // einem sichtbaren Bildschirm (sonst Standardlage)
        void ApplySavedBounds(int width, int height)
        {
            string[] p = settings.GetString("player.pipBounds", "").Split(',');
            int x, y, w, h;
            if (p.Length != 4 || !int.TryParse(p[0], out x) || !int.TryParse(p[1], out y)
                || !int.TryParse(p[2], out w) || !int.TryParse(p[3], out h)) return;
            Rectangle wanted = new Rectangle(x, y, w, h);
            Screen screen = null;
            foreach (Screen s in Screen.AllScreens)
                if (s.WorkingArea.IntersectsWith(wanted)) { screen = s; break; }
            if (screen == null) return;
            Rectangle work = screen.WorkingArea;
            int border = ResizeBorder * 2;
            int newWidth = Math.Max(MinimumSize.Width, Math.Min(work.Width, w));
            int newHeight = (int)Math.Round((newWidth - border) * (double)height / Math.Max(1, width)) + border;
            newHeight = Math.Max(MinimumSize.Height, Math.Min(work.Height, newHeight));
            if (newHeight == work.Height && height > 0)
                newWidth = Math.Max(MinimumSize.Width, (int)Math.Round((newHeight - border) * (double)width / height) + border);
            int newX = Math.Max(work.Left, Math.Min(work.Right - newWidth, x));
            int newY = Math.Max(work.Top, Math.Min(work.Bottom - newHeight, y));
            Bounds = new Rectangle(newX, newY, newWidth, newHeight);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Rectangle r = Bounds;
            if (WindowState == FormWindowState.Normal && r.Width > 0 && r.Height > 0)
            {
                settings.SetString("player.pipBounds", r.X + "," + r.Y + "," + r.Width + "," + r.Height);
                settings.Save();
            }
            base.OnFormClosing(e);
        }

        // Rand, an dem sich das Fenster ohne Titelleiste vergrössern lässt (gehört dem Formular, nicht der WebView2)
        static int ResizeBorder { get { return Math.Max(4, Theme.S(6)); } }

        const int WM_NCHITTEST = 0x0084, WM_NCLBUTTONDOWN = 0x00A1;
        const int HTCLIENT = 1, HTCAPTION = 2, HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14,
                  HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != WM_NCHITTEST || (int)m.Result != HTCLIENT) return;
            long lp = m.LParam.ToInt64();
            Point p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
            int b = ResizeBorder;
            bool left = p.X < b, right = p.X >= ClientSize.Width - b, top = p.Y < b, bottom = p.Y >= ClientSize.Height - b;
            int hit = top && left ? HTTOPLEFT : top && right ? HTTOPRIGHT : bottom && left ? HTBOTTOMLEFT : bottom && right ? HTBOTTOMRIGHT
                    : left ? HTLEFT : right ? HTRIGHT : top ? HTTOP : bottom ? HTBOTTOM : HTCLIENT;
            m.Result = (IntPtr)hit;
        }

        // Verschieben wie über eine Titelleiste (Maustaste ist im Videobild gedrückt)
        public void BeginDrag()
        {
            if (IsDisposed || (MouseButtons & MouseButtons.Left) == 0) return;
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { Web.Dispose(); }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
}
