// Dialoge des Players (seit 4.0.0): fehlende WebView2-Runtime und Übernahme der Daten aus dem Browser.

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    public enum RuntimeChoice { Cancel, Browser, Installed }

    static class DesktopUi
    {
        // Kopf mit Symbol und Titel wie im Update-Hinweis
        public static Control Head(Glyph glyph, string text)
        {
            TableLayoutPanel head = new TableLayoutPanel();
            head.AutoSize = true;
            head.ColumnCount = 2;
            head.Margin = new Padding(0, 0, 0, Theme.S(12));
            GlyphBox icon = new GlyphBox();
            icon.Icon = glyph;
            icon.Size = new Size(Theme.S(40), Theme.S(40));
            icon.Margin = new Padding(0, 0, Theme.S(12), 0);
            Label title = Theme.MakeLabel(text, false);
            title.Font = new Font("Segoe UI Semibold", 13f);
            title.Anchor = AnchorStyles.Left;
            head.Controls.Add(icon, 0, 0);
            head.Controls.Add(title, 1, 0);
            return head;
        }

        public static Label Text(string text, bool muted, int top)
        {
            Label l = Theme.MakeLabel(text, muted);
            l.MaximumSize = new Size(Theme.S(500), 0);
            l.Margin = new Padding(0, top, 0, 0);
            return l;
        }

        public static TableLayoutPanel Root()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(20), Theme.S(18), Theme.S(20), Theme.S(16));
            return root;
        }

        public static FlowLayoutPanel Buttons()
        {
            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(18), 0, 0);
            return buttons;
        }

        public static void SetIcon(Form f)
        {
            try
            {
                using (Stream s = typeof(DesktopUi).Assembly.GetManifestResourceStream("app.ico"))
                    if (s != null) { f.Icon = new Icon(s); f.ShowIcon = true; }
            }
            catch { }
        }
    }

    // ---------------------------------------------------------------
    // WebView2-Runtime fehlt: installieren, im Browser öffnen oder abbrechen
    // ---------------------------------------------------------------
    public class RuntimeDialog : Form
    {
        RuntimeChoice choice = RuntimeChoice.Cancel;
        readonly Button install, browser, cancel;
        readonly Label status;
        readonly DarkProgress progress;
        CancellationTokenSource cts;

        public RuntimeDialog(string error)
        {
            Text = "STEFFLIX";
            ConflictDialog.Setup(this);
            DesktopUi.SetIcon(this);
            TableLayoutPanel root = DesktopUi.Root();
            root.Controls.Add(DesktopUi.Head(Glyph.Up, "Windows-Komponente fehlt"));
            root.Controls.Add(DesktopUi.Text(
                "Für das Fenster von STEFFLIX fehlt die Windows-Komponente „WebView2“ von Microsoft. Unter Windows 11 ist sie " +
                "vorinstalliert, unter Windows 10 meistens auch.\n\nSie lässt sich jetzt installieren: STEFFLIX lädt das Setup von " +
                "Microsoft (etwa 2 MB), das den Rest selbst herunterlädt. Stattdessen kann STEFFLIX auch im Browser geöffnet werden.", false, 0));
            if (!string.IsNullOrEmpty(error)) root.Controls.Add(DesktopUi.Text("Grund: " + error, true, Theme.S(8)));

            progress = new DarkProgress();
            progress.Height = Theme.S(6);
            progress.Dock = DockStyle.Fill;
            progress.Margin = new Padding(0, Theme.S(14), 0, 0);
            progress.Visible = false;
            root.Controls.Add(progress);
            status = DesktopUi.Text("", true, Theme.S(6));
            status.Visible = false;
            root.Controls.Add(status);

            FlowLayoutPanel buttons = DesktopUi.Buttons();
            cancel = Theme.MakeButton("Abbrechen", false);
            browser = Theme.MakeButton("Im Browser öffnen", false);
            install = Theme.MakeButton("Jetzt installieren", true);
            cancel.Margin = new Padding(Theme.S(8), 0, 0, 0);
            browser.Margin = new Padding(Theme.S(8), 0, 0, 0);
            cancel.Click += delegate
            {
                if (cts != null) { cts.Cancel(); return; }
                choice = RuntimeChoice.Cancel;
                DialogResult = DialogResult.Cancel;
            };
            browser.Click += delegate { choice = RuntimeChoice.Browser; DialogResult = DialogResult.OK; };
            install.Click += delegate { StartInstall(); };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(browser);
            buttons.Controls.Add(install);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = install;
        }

        void StartInstall()
        {
            install.Enabled = false;
            browser.Enabled = false;
            progress.Visible = true;
            progress.Value = -1;
            status.Visible = true;
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            Thread t = new Thread(delegate()
            {
                string error = RuntimeInstaller.Install(token, delegate(string s)
                {
                    try { BeginInvoke((Action)delegate { status.Text = s; }); }
                    catch { }
                });
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        cts = null;
                        progress.Visible = false;
                        if (error == null)
                        {
                            choice = RuntimeChoice.Installed;
                            DialogResult = DialogResult.OK;
                            return;
                        }
                        status.Text = error;
                        install.Enabled = true;
                        install.Text = "Erneut versuchen";
                        browser.Enabled = true;
                    });
                }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (cts != null) { cts.Cancel(); e.Cancel = true; return; }   // erst nach dem Abbruch schliessen
            base.OnFormClosing(e);
        }

        public RuntimeChoice Ask(IWin32Window owner)
        {
            if (owner == null)
            {
                // Noch kein anderes Fenster: in der Taskleiste zeigen
                ShowInTaskbar = true;
                StartPosition = FormStartPosition.CenterScreen;
                ShowDialog();
            }
            else ShowDialog(owner);
            return choice;
        }
    }

    // ---------------------------------------------------------------
    // Erster Start des Players: Daten (Favoriten, Fortschritte, Einstellungen) aus dem Browser übernehmen.
    // Der Browser öffnet start.html?export=app, STEFFLIX speichert dort eine Datensicherung im
    // Download-Ordner, dieser Dialog erkennt die neue Datei und übergibt sie dem Player.
    // ---------------------------------------------------------------
    public class DataImportDialog : Form
    {
        readonly PlayerForm player;
        readonly ComboBox browserBox;
        readonly Label status;
        readonly Button fromBrowser, chooseFile, skip;
        readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer();
        readonly System.Windows.Forms.Timer timeout = new System.Windows.Forms.Timer();
        DateTime watchSince;
        bool importing;

        public DataImportDialog(PlayerForm player)
        {
            this.player = player;
            Text = "Daten übernehmen";
            ConflictDialog.Setup(this);
            DesktopUi.SetIcon(this);
            TableLayoutPanel root = DesktopUi.Root();
            root.Controls.Add(DesktopUi.Head(Glyph.Data, "Willkommen bei STEFFLIX"));
            root.Controls.Add(DesktopUi.Text(
                "Hast du STEFFLIX bisher im Browser geöffnet? Favoriten, Fortschritte, „Weiterschauen“ und Einstellungen lassen sich " +
                "hierher übernehmen. Der Browser behält seine Daten, beide laufen danach getrennt weiter.", false, 0));

            TableLayoutPanel row = new TableLayoutPanel();
            row.AutoSize = true;
            row.ColumnCount = 2;
            row.Margin = new Padding(0, Theme.S(14), 0, 0);
            Label l = Theme.MakeLabel("Bisheriger Browser:", false);
            l.Margin = new Padding(0, Theme.S(5), Theme.S(8), 0);
            browserBox = new ComboBox();
            browserBox.DropDownStyle = ComboBoxStyle.DropDownList;
            browserBox.FlatStyle = FlatStyle.Flat;
            browserBox.BackColor = Theme.Input;
            browserBox.ForeColor = Theme.Text;
            browserBox.Width = Theme.S(260);
            foreach (BrowserInfo b in Browsers.Installed()) browserBox.Items.Add(b);
            if (browserBox.Items.Count > 0) browserBox.SelectedIndex = 0;
            row.Controls.Add(l, 0, 0);
            row.Controls.Add(browserBox, 1, 0);
            root.Controls.Add(row);

            status = DesktopUi.Text("", true, Theme.S(12));
            status.Visible = false;
            root.Controls.Add(status);

            FlowLayoutPanel buttons = DesktopUi.Buttons();
            skip = Theme.MakeButton("Ohne Daten starten", false);
            chooseFile = Theme.MakeButton("Sicherung wählen …", false);
            fromBrowser = Theme.MakeButton("Aus dem Browser übernehmen", true);
            skip.Margin = new Padding(Theme.S(8), 0, 0, 0);
            chooseFile.Margin = new Padding(Theme.S(8), 0, 0, 0);
            skip.Click += delegate { DialogResult = DialogResult.Cancel; };
            chooseFile.Click += delegate { ChooseFile(); };
            fromBrowser.Click += delegate { OpenBrowser(); };
            buttons.Controls.Add(skip);
            buttons.Controls.Add(chooseFile);
            buttons.Controls.Add(fromBrowser);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = fromBrowser;
            CancelButton = skip;

            poll.Interval = 1000;
            poll.Tick += delegate { CheckDownloads(); };
            timeout.Interval = 30000;
            timeout.Tick += delegate
            {
                timeout.Stop();
                if (!importing) return;
                importing = false;
                ShowStatus("STEFFLIX hat die Sicherung nicht übernommen. Bitte erneut versuchen oder die Datei mit „Sicherung wählen …“ öffnen.");
                SetButtons(true);
            };
        }

        void ShowStatus(string text)
        {
            status.Text = text;
            status.Visible = true;
        }

        void SetButtons(bool enabled)
        {
            fromBrowser.Enabled = enabled;
            chooseFile.Enabled = enabled;
            browserBox.Enabled = enabled;
        }

        void OpenBrowser()
        {
            BrowserInfo browser = browserBox.SelectedItem as BrowserInfo;
            string url = new Uri(Path.Combine(App.Root, "start.html")).AbsoluteUri + "?export=app";
            watchSince = DateTime.UtcNow.AddSeconds(-2);
            try { Browsers.Open(browser, url); }
            catch (Exception e)
            {
                ShowStatus("Der Browser konnte nicht geöffnet werden (" + e.Message + "). Bitte im Browser in STEFFLIX unter " +
                           "Einstellungen › Datensicherung „Exportieren“ wählen und die Datei hier mit „Sicherung wählen …“ öffnen.");
                return;
            }
            ShowStatus("STEFFLIX ist im Browser geöffnet und speichert dort eine Datensicherung im Download-Ordner. Sobald sie da ist, " +
                       "wird sie übernommen …\n\nKlappt das nicht (z.B. fragt der Browser nach dem Speicherort): im Browser unter " +
                       "Einstellungen › Datensicherung „Exportieren“ wählen und die Datei hier mit „Sicherung wählen …“ öffnen.");
            poll.Start();
        }

        // Neue Sicherung im Download-Ordner? (Browser schreiben zuerst eine Teildatei und benennen sie dann um)
        void CheckDownloads()
        {
            if (importing) return;
            try
            {
                string dir = Browsers.DownloadsFolder();
                if (!Directory.Exists(dir)) return;
                FileInfo newest = null;
                foreach (FileInfo f in new DirectoryInfo(dir).GetFiles("stefflix-sicherung*.json"))
                    if (f.LastWriteTimeUtc >= watchSince && (newest == null || f.LastWriteTimeUtc > newest.LastWriteTimeUtc)) newest = f;
                if (newest == null) return;
                string text = ReadText(newest.FullName);
                if (text == null || !IsBackup(text)) return;   // noch unvollständig: beim nächsten Mal
                poll.Stop();
                StartImport(text);
            }
            catch { }
        }

        void ChooseFile()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "STEFFLIX-Sicherung wählen";
                dlg.Filter = "STEFFLIX-Sicherung (*.json)|*.json|Alle Dateien|*.*";
                dlg.InitialDirectory = Browsers.DownloadsFolder();
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string text = ReadText(dlg.FileName);
                if (text == null || !IsBackup(text))
                {
                    ShowStatus("Diese Datei ist keine gültige STEFFLIX-Sicherung.");
                    return;
                }
                poll.Stop();
                StartImport(text);
            }
        }

        static string ReadText(string file)
        {
            try
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader r = new StreamReader(fs, Encoding.UTF8, true))
                    return r.ReadToEnd();
            }
            catch { return null; }
        }

        static bool IsBackup(string text)
        {
            try
            {
                JsonObject o = Json.Parse(text) as JsonObject;
                return o != null && o.Str("app") == "STEFFLIX" && o.Obj("data") != null;
            }
            catch { return false; }
        }

        void StartImport(string text)
        {
            importing = true;
            SetButtons(false);
            ShowStatus("Sicherung gefunden, wird übernommen …");
            player.ImportBackup(text);
            timeout.Start();
        }

        // Antwort von STEFFLIX
        public void ImportFinished(bool ok, string message)
        {
            if (!importing) return;
            importing = false;
            timeout.Stop();
            if (ok)
            {
                ShowStatus(message ?? "Daten übernommen.");
                System.Windows.Forms.Timer close = new System.Windows.Forms.Timer();
                close.Interval = 1500;
                close.Tick += delegate { close.Stop(); close.Dispose(); if (!IsDisposed) DialogResult = DialogResult.OK; };
                close.Start();
                return;
            }
            ShowStatus(message ?? "Die Sicherung konnte nicht übernommen werden.");
            SetButtons(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                poll.Dispose();
                timeout.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
