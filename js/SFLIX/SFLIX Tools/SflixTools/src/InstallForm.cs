// Installationsfenster: erscheint, wenn das Programm nicht in einem STEFFLIX-Ordner liegt.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    public class InstallForm : Form
    {
        TextBox folderBox, ffmpegBox, licenseBox;
        DarkCheckBox ffmpegCheck;
        Control ffmpegRow;
        Button installButton, openButton, startButton, closeButton;
        DarkProgress progress;
        Label statusLabel;
        TextBox logBox;
        readonly ConcurrentQueue<string> pendingLog = new ConcurrentQueue<string>();
        readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        readonly object progressGate = new object();
        double latestFraction = -2;
        string latestStatus;
        volatile bool running;
        string installedFolder;

        public InstallForm()
        {
            Text = "STEFFLIX Setup";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            try
            {
                using (Stream s = typeof(InstallForm).Assembly.GetManifestResourceStream("app.ico"))
                    if (s != null) Icon = new Icon(s);
            }
            catch { }
            using (Graphics g = CreateGraphics()) Theme.Scale = g.DpiX / 96f;
            StartPosition = FormStartPosition.CenterScreen;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(Theme.S(900), work.Width - 40), Math.Min(Theme.S(760), work.Height - 60));
            MinimumSize = new Size(Math.Min(Theme.S(640), work.Width), Math.Min(Theme.S(360), work.Height));

            BuildUi();
            uiTimer.Interval = 100;
            uiTimer.Tick += delegate { FlushUi(); };
            uiTimer.Start();
        }

        void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(110)));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.BackColor = Theme.Back;
            Controls.Add(root);

            BannerPanel banner = new BannerPanel();
            banner.Version = "v" + App.VersionText;
            banner.Subtitle = "SETUP";
            banner.Dock = DockStyle.Fill;
            banner.Margin = new Padding(0);
            root.Controls.Add(banner, 0, 0);

            // Erklärung und Ordnerwahl
            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.AutoSize = true;
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.BackColor = Theme.Card;
            card.Padding = new Padding(Theme.S(14), Theme.S(10), Theme.S(14), Theme.S(12));
            card.Margin = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), 0);

            Label title = Theme.MakeLabel("Willkommen beim Setup von STEFFLIX", false);
            title.Font = new Font("Segoe UI Semibold", 12f);
            title.Margin = new Padding(0, 0, 0, Theme.S(4));
            card.Controls.Add(title);

            Label intro = Theme.MakeLabel(
                "In diesem Ordner ist STEFFLIX noch nicht installiert. STEFFLIX ist eine Browserapplikation im Stil " +
                "bekannter Streamingdienste, um lokal oder im Netzwerk gespeicherte Mediendateien abzuspielen. Ausser einem Browser braucht " +
                "es nichts weiter.\n\nWähle den Ordner, in dem STEFFLIX installiert werden soll, am besten den Hauptordner " +
                "deiner Medien (z.B. M:\\ oder C:\\Medien). Fehlende Ordner werden angelegt, vorhandene Dateien bleiben unverändert.", true);
            intro.MaximumSize = new Size(Theme.S(840), 0);
            intro.Margin = new Padding(0, 0, 0, Theme.S(10));
            card.Controls.Add(intro);

            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.AutoSize = true;
            row.ColumnCount = 3;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Margin = new Padding(0);
            Label l = Theme.MakeLabel("Installationsordner:", false);
            l.Margin = new Padding(0, Theme.S(5), Theme.S(6), 0);
            folderBox = Theme.MakeTextBox();
            folderBox.Dock = DockStyle.Fill;
            folderBox.Margin = new Padding(0, Theme.S(2), Theme.S(6), 0);
            folderBox.TextChanged += delegate { installButton.Enabled = !running && folderBox.Text.Trim().Length > 0; };
            Button browse = Theme.MakeButton("Durchsuchen …", false);
            browse.Padding = new Padding(Theme.S(8), 0, Theme.S(8), 0);
            browse.Margin = new Padding(0);
            browse.Click += delegate
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = "Ordner für STEFFLIX wählen";
                    dlg.ShowNewFolderButton = true;
                    if (Directory.Exists(folderBox.Text)) dlg.SelectedPath = folderBox.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) folderBox.Text = dlg.SelectedPath;
                }
            };
            row.Controls.Add(l, 0, 0);
            row.Controls.Add(folderBox, 1, 0);
            row.Controls.Add(browse, 2, 0);
            card.Controls.Add(row);

            // ffmpeg mitinstallieren (empfohlen), Ordner wählbar, Standard je Benutzer in AppData
            ffmpegCheck = new DarkCheckBox();
            ffmpegCheck.Text = "ffmpeg mitinstallieren (empfohlen)";
            ffmpegCheck.ForeColor = Theme.Text;
            ffmpegCheck.Switch = true;
            ffmpegCheck.Checked = true;
            ffmpegCheck.Margin = new Padding(0, Theme.S(14), 0, Theme.S(2));
            ffmpegCheck.FitText();
            card.Controls.Add(ffmpegCheck);
            Label ffmpegHint = Theme.MakeLabel("ffmpeg wird für Vorschaubilder, Konvertieren, den Importer, „Medien prüfen“ und „Duplikate finden“ " +
                                               "gebraucht (Download ca. 100 MB). Ohne wird es beim ersten Bedarf geladen.", true);
            ffmpegHint.MaximumSize = new Size(Theme.S(840), 0);
            ffmpegHint.Margin = new Padding(0, 0, 0, Theme.S(4));
            card.Controls.Add(ffmpegHint);
            ffmpegBox = Theme.MakeTextBox();
            ffmpegBox.Text = FfmpegLocation.DefaultFolder;
            ffmpegRow = FolderRow(ffmpegBox, "ffmpeg-Ordner:", "Ordner für ffmpeg wählen");
            card.Controls.Add(ffmpegRow);
            ffmpegCheck.CheckedChanged += delegate { ffmpegRow.Enabled = ffmpegCheck.Checked; };

            // Lizenzschlüssel (optional)
            Label licenseTitle = Theme.MakeLabel("Lizenzschlüssel (optional)", false);
            licenseTitle.Margin = new Padding(0, Theme.S(14), 0, Theme.S(2));
            card.Controls.Add(licenseTitle);
            Label licenseHint = Theme.MakeLabel("Schaltet das Feedback in STEFFLIX und im Updater frei. Lässt sich auch später im Updater unter „Lizenz“ eingeben. " +
                                                "Beim Speichern wird eine Bestätigung mit der Kennung dieser Installation verschickt.", true);
            licenseHint.MaximumSize = new Size(Theme.S(840), 0);
            licenseHint.Margin = new Padding(0, 0, 0, Theme.S(4));
            card.Controls.Add(licenseHint);
            licenseBox = Theme.MakeTextBox();
            licenseBox.Dock = DockStyle.Fill;
            licenseBox.Font = new Font("Consolas", 9.5f);
            licenseBox.Margin = new Padding(0, Theme.S(2), 0, 0);
            card.Controls.Add(licenseBox);
            root.Controls.Add(card, 0, 1);

            // Knöpfe
            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(Theme.S(16), Theme.S(12), Theme.S(16), 0);
            installButton = Theme.MakeButton("Installieren", true);
            installButton.Font = new Font("Segoe UI Semibold", 10.5f);
            installButton.Padding = new Padding(Theme.S(24), Theme.S(5), Theme.S(24), Theme.S(5));
            installButton.Enabled = false;
            installButton.Click += delegate { StartInstall(); };
            openButton = Theme.MakeButton("STEFFLIX öffnen", true);
            openButton.Font = installButton.Font;
            openButton.Padding = installButton.Padding;
            openButton.Visible = false;
            openButton.Click += delegate { OpenStefflix(); };
            startButton = Theme.MakeButton("Updater starten", false);
            startButton.Margin = new Padding(Theme.S(10), Theme.S(3), 0, 0);
            startButton.Padding = new Padding(Theme.S(14), Theme.S(6), Theme.S(14), Theme.S(6));
            startButton.Visible = false;
            startButton.Click += delegate { StartUpdater(); };
            closeButton = Theme.MakeButton("Schliessen", false);
            closeButton.Margin = new Padding(Theme.S(10), Theme.S(3), 0, 0);
            closeButton.Padding = new Padding(Theme.S(14), Theme.S(6), Theme.S(14), Theme.S(6));
            closeButton.Click += delegate { Close(); };
            buttons.Controls.Add(installButton);
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(startButton);
            buttons.Controls.Add(closeButton);
            root.Controls.Add(buttons, 0, 2);

            TableLayoutPanel prog = new TableLayoutPanel();
            // Oben verankert statt Fill: sonst streckt das Layout die Zeile, und der Status rutscht nach unten
            prog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            prog.AutoSize = true;
            prog.ColumnCount = 2;
            prog.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            prog.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            prog.Margin = new Padding(Theme.S(16), Theme.S(10), Theme.S(16), Theme.S(12));
            progress = new DarkProgress();
            progress.Dock = DockStyle.Top;
            progress.Height = Theme.S(6);
            progress.Margin = new Padding(0);
            statusLabel = Theme.MakeLabel("Bereit.", true);
            statusLabel.AutoSize = false;
            statusLabel.AutoEllipsis = true;
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Height = Theme.S(26);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Margin = new Padding(0, Theme.S(4), 0, 0);
            Button detailsButton = LogToggle.MakeButton();
            prog.Controls.Add(progress, 0, 0);
            prog.SetColumnSpan(progress, 2);
            prog.Controls.Add(statusLabel, 0, 1);
            prog.Controls.Add(detailsButton, 1, 1);
            root.Controls.Add(prog, 0, 3);

            logBox = new TextBox();
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.WordWrap = false;
            logBox.Font = new Font("Consolas", 9f);
            logBox.BackColor = Theme.LogBack;
            logBox.ForeColor = Color.FromArgb(0xd0, 0xd0, 0xd6);
            logBox.BorderStyle = BorderStyle.None;
            logBox.Dock = DockStyle.Fill;
            logBox.Margin = new Padding(Theme.S(16), 0, Theme.S(16), Theme.S(16));
            root.Controls.Add(logBox, 0, 4);

            // Protokoll standardmässig eingeklappt, "Details" blendet es ein
            logToggle = new LogToggle(root, 4, 100, logBox, detailsButton);
            logToggle.Changed = delegate(bool expanded)
            {
                LogToggle.FitWindow(this, root, expanded, -1, 0, null, Theme.S(300), 4);
            };
        }

        LogToggle logToggle;

        Control FolderRow(TextBox box, string label, string description)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.AutoSize = true;
            row.ColumnCount = 3;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Margin = new Padding(0);
            Label l = Theme.MakeLabel(label, false);
            l.Margin = new Padding(0, Theme.S(5), Theme.S(6), 0);
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0, Theme.S(2), Theme.S(6), 0);
            Button browse = Theme.MakeButton("Durchsuchen …", false);
            browse.Padding = new Padding(Theme.S(8), 0, Theme.S(8), 0);
            browse.Margin = new Padding(0);
            browse.Click += delegate
            {
                using (FolderBrowserDialog dlg = new FolderBrowserDialog())
                {
                    dlg.Description = description;
                    dlg.ShowNewFolderButton = true;
                    if (Directory.Exists(box.Text)) dlg.SelectedPath = box.Text;
                    if (dlg.ShowDialog(this) == DialogResult.OK) box.Text = dlg.SelectedPath;
                }
            };
            row.Controls.Add(l, 0, 0);
            row.Controls.Add(box, 1, 0);
            row.Controls.Add(browse, 2, 0);
            return row;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            logToggle.Set(false);
        }

        void StartInstall()
        {
            string target = folderBox.Text.Trim().Trim('"');
            if (target.Length == 0) return;
            try { target = Path.GetFullPath(target); }
            catch (Exception)
            {
                MessageBox.Show(this, "Ungültiger Pfad:\n" + target, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Directory.Exists(target))
            {
                if (MessageBox.Show(this, "Der Ordner existiert nicht:\n" + target + "\n\nSoll er erstellt werden?", Text,
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                try { Directory.CreateDirectory(target); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Der Ordner konnte nicht erstellt werden:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            InstallOptions options = new InstallOptions();
            options.Ffmpeg = ffmpegCheck.Checked;
            if (options.Ffmpeg)
            {
                string ff = ffmpegBox.Text.Trim().Trim('"');
                try { options.FfmpegFolder = Path.GetFullPath(ff.Length > 0 ? ff : FfmpegLocation.DefaultFolder); }
                catch (Exception)
                {
                    MessageBox.Show(this, "Ungültiger ffmpeg-Ordner:\n" + ff, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            string key = licenseBox.Text.Trim();
            if (key.Length > 0)
            {
                string error;
                if (LicenseKey.Decode(key, out error) == null)
                {
                    MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                options.LicenseKey = key;
            }

            running = true;
            installButton.Enabled = false;
            closeButton.Enabled = false;
            folderBox.ReadOnly = true;
            progress.Value = -1;
            statusLabel.Text = "Installation läuft ...";

            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.LogLine = delegate(string line) { pendingLog.Enqueue(line); };
            ctx.ProgressSink = delegate(double f, string s) { lock (progressGate) { latestFraction = f; latestStatus = s; } };

            Thread t = new Thread(delegate()
            {
                string error = null;
                try { InstallTask.Run(ctx, target, options); }
                catch (Exception ex) { error = ex.Message; }
                BeginInvoke((Action)delegate { Finished(target, error); });
            });
            t.IsBackground = true;
            t.Start();
        }

        void Finished(string target, string error)
        {
            running = false;
            FlushUi();
            closeButton.Enabled = true;
            folderBox.ReadOnly = false;
            pendingLog.Enqueue("");
            pendingLog.Enqueue(new string('=', 60));
            if (error == null)
            {
                installedFolder = target;
                pendingLog.Enqueue("  INSTALLATION ABGESCHLOSSEN");
                progress.Value = 1;
                statusLabel.Text = "Installation abgeschlossen.";
                installButton.Visible = false;
                openButton.Visible = true;
                startButton.Visible = true;
            }
            else
            {
                pendingLog.Enqueue("  FEHLER: " + error);
                progress.Value = 0;
                statusLabel.Text = "Installation fehlgeschlagen (siehe Details).";
                installButton.Enabled = true;
                installButton.Text = "Erneut versuchen";
            }
            pendingLog.Enqueue(new string('=', 60));
            FlushUi();
        }

        void OpenStefflix()
        {
            if (installedFolder == null) return;
            try
            {
                Process.Start(InstallTask.GuideUrl(installedFolder));
            }
            catch
            {
                try { Process.Start(Path.Combine(installedFolder, "start.html")); } catch { }
            }
        }

        void StartUpdater()
        {
            if (installedFolder == null) return;
            string exe = Path.Combine(installedFolder, App.ExeName);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe);
                psi.UseShellExecute = false;
                psi.WorkingDirectory = installedFolder;
                Process.Start(psi);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Der Updater konnte nicht gestartet werden:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void FlushUi()
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while (pendingLog.TryDequeue(out line)) sb.Append(line).Append("\r\n");
            if (sb.Length > 0) logBox.AppendText(sb.ToString());
            double f;
            string s;
            lock (progressGate) { f = latestFraction; s = latestStatus; latestFraction = -2; latestStatus = null; }
            if (running && f > -2) progress.Value = f;
            if (running && s != null) statusLabel.Text = s;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running) { e.Cancel = true; return; }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) uiTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
