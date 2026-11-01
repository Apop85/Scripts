// Player-Fenster (seit 4.0.0): STEFFLIX (start.html über file://) in einer WebView2, ohne Adresszeile,
// Linkvorschau, Kontextmenü und Zoom, wie die Android-App. Fremde Links öffnen im Standardbrowser.
//
// Vollbild: F11 (meldet STEFFLIX über die Brücke) oder Vollbild eines Videos; beim Verlassen des
// Video-Vollbilds kehrt das Fenster in den vorherigen Zustand zurück (war es per F11 im Vollbild, bleibt es).
//
// Brücke (window.chrome.webview, siehe initDesktopApp in ui.js):
//   STEFFLIX -> Programm (JSON-Text): {cmd:"ready"}, {cmd:"tools", page}, {cmd:"fullscreen"}, {cmd:"reload"},
//                                     {cmd:"importResult", ok, message}
//   Programm -> STEFFLIX (JSON):      {event:"dataChanged"}, {event:"importBackup", text}, {event:"toolsShown"}
// DevTools und Kontextmenü nur mit "player.devtools": true in updater_settings.json oder SFLIX_DEVTOOLS=1.

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
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

        async void InitWebView()
        {
            try
            {
                // Autoplay ohne vorherigen Klick (nächste Folge, Musik)
                CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, WebViewLibs.UserDataFolder(), options);
                await web.EnsureCoreWebView2Async(env);
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
            if (IsInternal(e.Uri)) return;
            e.Cancel = true;
            OpenExternal(e.Uri);
        }

        void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (IsInternal(e.Uri)) web.CoreWebView2.Navigate(e.Uri);
            else OpenExternal(e.Uri);
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
            }
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
            SaveWindowBounds();
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { web.Dispose(); }
                catch { }
            }
            base.Dispose(disposing);
        }
    }
}
