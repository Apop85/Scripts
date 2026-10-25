// Hauptfenster: Aufgaben auswählen, starten, Fortschritt und Protokoll.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    public class MainForm : Form
    {
        // Feste Reihenfolge: Update zuerst, Daten einlesen zuletzt (die anderen verändern Dateien).
        // Ablagen leeren vor Zusammenführen/Konvertieren, damit frisch gesicherte Originale bleiben.
        public static readonly string[] TaskIds = { "update", "cleanup", "merge", "convert", "preview", "data" };

        static readonly Dictionary<string, string> TaskTitles = new Dictionary<string, string> {
            { "update", "Update" },
            { "cleanup", "Ablagen leeren" },
            { "merge", "Gesplittete Medien zusammenführen" },
            { "convert", "Videos für SFLIX konvertieren" },
            { "preview", "Vorschaubilder erstellen" },
            { "data", "Daten einlesen" },
        };

        readonly Settings settings;
        readonly Dictionary<string, DarkCheckBox> checks = new Dictionary<string, DarkCheckBox>();
        readonly List<string> autoRun;
        readonly string startNotice;

        // Optionen
        DarkCheckBox optFfmpeg, optMoveParts, optDeleteOriginal;
        TextBox mediaFolder;
        NumericUpDown convertParallel;

        bool twoColumns;
        LogToggle logToggle;
        Panel tasksScroller;
        BannerPanel banner;
        Panel updateBar;
        Label updateLabel;
        Button updateButton;
        Button startButton, cancelButton;
        DarkProgress progress;
        Label statusLabel;
        TextBox logBox;

        // Laufende Aufgaben
        Thread worker;
        CancellationTokenSource cts;
        volatile bool running;
        bool closeWhenDone;
        readonly ConcurrentQueue<string> pendingLog = new ConcurrentQueue<string>();
        readonly System.Windows.Forms.Timer uiTimer = new System.Windows.Forms.Timer();
        double latestFraction = -2;
        string latestStatus;
        readonly object progressGate = new object();
        byte[] pendingSelfUpdate;
        string logFile;

        public MainForm(Settings settings, List<string> autoRun, string startNotice)
        {
            this.settings = settings;
            this.autoRun = autoRun;
            this.startNotice = startNotice;
            logFile = Path.Combine(App.SysDir, "updater.log");

            Text = App.Title;
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            try
            {
                using (Stream s = typeof(MainForm).Assembly.GetManifestResourceStream("app.ico"))
                    if (s != null) Icon = new Icon(s);
            }
            catch { }

            using (Graphics g = CreateGraphics()) Theme.Scale = g.DpiX / 96f;
            StartPosition = FormStartPosition.CenterScreen;
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            twoColumns = work.Width - 40 >= Theme.S(1100);
            ClientSize = new Size(Math.Min(Theme.S(twoColumns ? 1180 : 880), work.Width - 40),
                                  Math.Min(Theme.S(twoColumns ? 860 : 1060), work.Height - 60));
            MinimumSize = new Size(Math.Min(Theme.S(700), work.Width), Math.Min(Theme.S(400), work.Height));

            BuildUi();
            LoadOptions();

            uiTimer.Interval = 100;
            uiTimer.Tick += delegate { FlushUi(); };
            uiTimer.Start();
        }

        // ---------------------------------------------------------------
        // Aufbau
        // ---------------------------------------------------------------

        void BuildUi()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 6;
            root.BackColor = Theme.Back;
            root.Padding = new Padding(0);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(110)));   // Kopfbild
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Update-Hinweis
            root.RowStyles.Add(new RowStyle(SizeType.Percent, twoColumns ? 66 : 80)); // Aufgaben (scrollbar)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Knöpfe
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Fortschritt
            root.RowStyles.Add(new RowStyle(SizeType.Percent, twoColumns ? 34 : 20)); // Protokoll
            Controls.Add(root);

            banner = new BannerPanel();
            banner.Version = "v" + App.VersionText;
            banner.Dock = DockStyle.Fill;
            banner.Margin = new Padding(0);
            root.Controls.Add(banner, 0, 0);

            // Hinweis auf neue Programmversion
            updateBar = new Panel();
            updateBar.Dock = DockStyle.Fill;
            updateBar.BackColor = Theme.Notice;
            updateBar.Margin = new Padding(Theme.S(16), Theme.S(10), Theme.S(16), 0);
            updateBar.Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(8), Theme.S(8));
            updateBar.Height = Theme.S(48);
            updateBar.Visible = false;
            updateLabel = Theme.MakeLabel("", false);
            updateLabel.Dock = DockStyle.Fill;
            updateLabel.AutoSize = false;
            updateLabel.TextAlign = ContentAlignment.MiddleLeft;
            updateButton = Theme.MakeButton("Jetzt aktualisieren", true);
            updateButton.Dock = DockStyle.Right;
            updateButton.Click += delegate { InstallSelfUpdate(); };
            updateBar.Controls.Add(updateLabel);
            updateBar.Controls.Add(updateButton);
            root.Controls.Add(updateBar, 0, 1);

            // Aufgaben (scrollen, falls der Bildschirm zu klein ist)
            Panel scroller = new Panel();
            tasksScroller = scroller;
            scroller.Dock = DockStyle.Fill;
            scroller.AutoScroll = true;
            scroller.Margin = new Padding(Theme.S(16), Theme.S(12), Theme.S(6), 0);
            scroller.BackColor = Theme.Back;
            root.Controls.Add(scroller, 0, 2);

            TableLayoutPanel tasks = new TableLayoutPanel();
            tasks.Dock = DockStyle.Top;
            tasks.AutoSize = true;
            tasks.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tasks.ColumnCount = twoColumns ? 2 : 1;
            tasks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            if (twoColumns) tasks.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            tasks.Margin = new Padding(0);
            tasks.Padding = new Padding(0, 0, Theme.S(10), 0);
            tasks.BackColor = Theme.Back;
            scroller.Controls.Add(tasks);

            // Ein Medienordner für alle Aufgaben, die Videos durchsuchen. Wird gespeichert.
            mediaFolder = Theme.MakeTextBox();
            TableLayoutPanel folderCard = new TableLayoutPanel();
            folderCard.Dock = DockStyle.Fill;
            folderCard.AutoSize = true;
            folderCard.ColumnCount = 1;
            folderCard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            folderCard.BackColor = Theme.Card;
            folderCard.Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), Theme.S(8));
            folderCard.Margin = new Padding(0, 0, 0, Theme.S(10));
            folderCard.Controls.Add(FolderRow(mediaFolder, "Medienordner:"));
            Label folderHint = Theme.MakeLabel("Gilt für Zusammenführen, Konvertieren, Vorschaubilder und Ablagen leeren. " +
                                               "Wird gespeichert – nur ändern, wenn sich der Ordner ändert.", true);
            folderHint.Margin = new Padding(0, Theme.S(2), 0, 0);
            folderCard.Controls.Add(folderHint);
            tasks.Controls.Add(folderCard, 0, 0);
            if (twoColumns) tasks.SetColumnSpan(folderCard, 2);

            Label heading = Theme.MakeLabel("Was möchtest du machen?", false);
            heading.Font = new Font("Segoe UI Semibold", 11f);
            heading.Margin = new Padding(0, 0, 0, Theme.S(6));
            tasks.Controls.Add(heading, 0, 1);
            if (twoColumns) tasks.SetColumnSpan(heading, 2);

            optFfmpeg = SmallCheck("ffmpeg ebenfalls aktualisieren");
            Control updateCard = TaskCard("update", "STEFFLIX-Dateien und dieses Programm von GitHub aktualisieren.",
                new Control[] { optFfmpeg });

            optMoveParts = SmallCheck("Teile danach nach „" + MergeTask.PartsFolderName + "“ verschieben");
            Control mergeCard = TaskCard("merge", "Videos wie „Titel pt1“, „Titel pt2“ zu einer Datei zusammenfügen. Du wählst vorher aus, welche.",
                new Control[] { optMoveParts });

            convertParallel = new NumericUpDown();
            convertParallel.Minimum = 0;
            convertParallel.Maximum = 16;
            convertParallel.Width = Theme.S(56);
            convertParallel.BackColor = Theme.Input;
            convertParallel.ForeColor = Theme.Text;
            convertParallel.BorderStyle = BorderStyle.FixedSingle;
            optDeleteOriginal = SmallCheck("Originale löschen statt nach „" + ConvertTask.OriginalsFolderName + "“ verschieben");
            FlowLayoutPanel parallelRow = new FlowLayoutPanel();
            parallelRow.AutoSize = true;
            parallelRow.WrapContents = false;
            parallelRow.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
            Label parallelLabel = Theme.MakeLabel("Gleichzeitig:", true);
            parallelLabel.Margin = new Padding(0, Theme.S(5), Theme.S(6), 0);
            Label parallelHint = Theme.MakeLabel("(0 = automatisch)", true);
            parallelHint.Margin = new Padding(Theme.S(6), Theme.S(5), 0, 0);
            parallelRow.Controls.Add(parallelLabel);
            parallelRow.Controls.Add(convertParallel);
            parallelRow.Controls.Add(parallelHint);
            Control convertCard = TaskCard("convert", "Videos in browserkompatible MP4 (H.264/AAC) umwandeln. Das Original kommt nach „" + ConvertTask.OriginalsFolderName + "“.",
                new Control[] { parallelRow, optDeleteOriginal });

            Control previewCard = TaskCard("preview", "Aus der Mitte jedes Videos ein Vorschaubild (480×270) erstellen.",
                new Control[0]);

            Control dataCard = TaskCard("data", "Medien neu einlesen (data.js), ungültige Zeichen in Namen korrigieren.",
                new Control[0]);

            Control cleanupCard = TaskCard("cleanup", "„__teile“, „__konvertiert“, „__gelöscht“ und „__original“ endgültig löschen. Du siehst vorher, was gelöscht wird.",
                new Control[0]);

            if (twoColumns)
            {
                // Breiter Bildschirm: zwei Spalten, damit alles ohne Scrollen sichtbar ist
                TableLayoutPanel left = Column(), right = Column();
                left.Margin = new Padding(0, 0, Theme.S(5), 0);
                right.Margin = new Padding(Theme.S(5), 0, 0, 0);
                foreach (Control c in new Control[] { updateCard, mergeCard, previewCard, convertCard, cleanupCard, dataCard })
                {
                    // Karten nicht in die Höhe ziehen
                    c.Dock = DockStyle.None;
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                }
                left.Controls.Add(updateCard);
                left.Controls.Add(mergeCard);
                left.Controls.Add(previewCard);
                right.Controls.Add(convertCard);
                right.Controls.Add(cleanupCard);
                right.Controls.Add(dataCard);
                tasks.Controls.Add(left, 0, 2);
                tasks.Controls.Add(right, 1, 2);
            }
            else
            {
                foreach (Control c in new Control[] { updateCard, cleanupCard, mergeCard, convertCard, previewCard, dataCard })
                    tasks.Controls.Add(c);
            }

            // Knöpfe
            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(Theme.S(16), Theme.S(10), Theme.S(16), 0);
            startButton = Theme.MakeButton("Start", true);
            startButton.Font = new Font("Segoe UI Semibold", 10.5f);
            startButton.Padding = new Padding(Theme.S(28), Theme.S(5), Theme.S(28), Theme.S(5));
            startButton.Click += delegate { StartTasks(null); };
            cancelButton = Theme.MakeButton("Abbrechen", false);
            cancelButton.Margin = new Padding(Theme.S(10), Theme.S(3), 0, 0);
            cancelButton.Padding = new Padding(Theme.S(14), Theme.S(6), Theme.S(14), Theme.S(6));
            cancelButton.Enabled = false;
            cancelButton.Click += delegate { CancelTasks(); };
            buttons.Controls.Add(startButton);
            buttons.Controls.Add(cancelButton);
            root.Controls.Add(buttons, 0, 3);

            // Fortschritt
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
            statusLabel.AutoEllipsis = true;
            statusLabel.AutoSize = false;
            statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            statusLabel.Height = Theme.S(26);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Margin = new Padding(0, Theme.S(4), 0, 0);
            Button detailsButton = LogToggle.MakeButton();
            prog.Controls.Add(progress, 0, 0);
            prog.SetColumnSpan(progress, 2);
            prog.Controls.Add(statusLabel, 0, 1);
            prog.Controls.Add(detailsButton, 1, 1);
            root.Controls.Add(prog, 0, 4);

            // Protokoll
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
            root.Controls.Add(logBox, 0, 5);

            // Protokoll standardmässig eingeklappt, "Details" blendet es ein
            logToggle = new LogToggle(root, 5, 100, logBox, detailsButton);
            logToggle.Changed = delegate(bool expanded)
            {
                LogToggle.FitWindow(this, root, expanded, 2, 100, tasksScroller, Theme.S(300), 5);
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            logToggle.Set(false);
        }

        Control TaskCard(string id, string description, Control[] options)
        {
            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.AutoSize = true;
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.BackColor = Theme.Card;
            card.Padding = new Padding(Theme.S(12), Theme.S(6), Theme.S(12), Theme.S(6));
            card.Margin = new Padding(0, 0, 0, Theme.S(5));

            DarkCheckBox check = new DarkCheckBox();
            check.Text = TaskTitles[id];
            check.Font = new Font("Segoe UI Semibold", 10.5f);
            check.ForeColor = Theme.Text;
            check.Margin = new Padding(0);
            check.FitText();
            checks[id] = check;
            card.Controls.Add(check);

            int indent = Theme.S(26);
            Label desc = Theme.MakeLabel(description, true);
            desc.Margin = new Padding(indent, 0, 0, options.Length > 0 ? Theme.S(4) : 0);
            desc.MaximumSize = new Size(Theme.S(twoColumns ? 480 : 760), 0);
            card.Controls.Add(desc);

            foreach (Control c in options)
            {
                c.Margin = new Padding(indent, c.Margin.Top, 0, c.Margin.Bottom);
                card.Controls.Add(c);
            }

            // Klick auf die Karte schaltet die Aufgabe um
            card.Click += delegate { check.Checked = !check.Checked; };
            desc.Click += delegate { check.Checked = !check.Checked; };
            check.CheckedChanged += delegate { card.BackColor = check.Checked ? Color.FromArgb(0x26, 0x20, 0x23) : Theme.Card; card.Invalidate(true); };
            return card;
        }

        static TableLayoutPanel Column()
        {
            TableLayoutPanel col = new TableLayoutPanel();
            col.Dock = DockStyle.Fill;
            col.AutoSize = true;
            col.ColumnCount = 1;
            col.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            col.Padding = new Padding(0);
            col.BackColor = Theme.Back;
            return col;
        }

        DarkCheckBox SmallCheck(string text)
        {
            DarkCheckBox c = new DarkCheckBox();
            c.Text = text;
            c.ForeColor = Theme.Text;
            c.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
            c.FitText();
            return c;
        }

        Control FolderRow(TextBox box, string label)
        {
            TableLayoutPanel row = new TableLayoutPanel();
            row.Dock = DockStyle.Fill;
            row.AutoSize = true;
            row.ColumnCount = 3;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
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
                    dlg.SelectedPath = Directory.Exists(box.Text) ? box.Text : App.Root;
                    dlg.ShowNewFolderButton = false;
                    if (dlg.ShowDialog(this) == DialogResult.OK) box.Text = dlg.SelectedPath;
                }
            };
            row.Controls.Add(l, 0, 0);
            row.Controls.Add(box, 1, 0);
            row.Controls.Add(browse, 2, 0);
            return row;
        }

        // ---------------------------------------------------------------
        // Einstellungen
        // ---------------------------------------------------------------

        void LoadOptions()
        {
            foreach (string id in TaskIds)
            {
                bool def = id == "update" || id == "data";
                checks[id].Checked = autoRun != null ? autoRun.Contains(id) : settings.GetBool("task." + id, def);
            }
            optFfmpeg.Checked = settings.GetBool("update.ffmpeg", true);
            mediaFolder.Text = settings.MediaFolder();
            optMoveParts.Checked = settings.GetBool("merge.moveParts", true);
            convertParallel.Value = Math.Max(0, Math.Min(16, settings.GetInt("convert.parallel", 0)));
            optDeleteOriginal.Checked = settings.GetBool("convert.deleteOriginal", false);
        }

        static string FolderSetting(string text)
        {
            text = text.Trim();
            return string.Equals(text.TrimEnd('\\'), App.Root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) ? "" : text;
        }

        void SaveOptions(bool saveTaskSelection)
        {
            if (saveTaskSelection)
                foreach (string id in TaskIds) settings.SetBool("task." + id, checks[id].Checked);
            settings.SetBool("update.ffmpeg", optFfmpeg.Checked);
            settings.SetString("media.folder", FolderSetting(mediaFolder.Text));
            // Getrennte Ordner früherer Versionen sind damit ersetzt
            foreach (string old in new string[] { "convert.folder", "merge.folder", "preview.folder" }) settings.SetString(old, "");
            settings.SetBool("merge.moveParts", optMoveParts.Checked);
            settings.SetInt("convert.parallel", (int)convertParallel.Value);
            settings.SetBool("convert.deleteOriginal", optDeleteOriginal.Checked);
            settings.Save();
        }

        static string FolderOf(TextBox box)
        {
            string t = box.Text.Trim();
            return t.Length == 0 ? App.Root : t;
        }

        // ---------------------------------------------------------------
        // Start
        // ---------------------------------------------------------------

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AppendLog(App.Title + " " + App.VersionText + "  –  " + App.Root);
            if (startNotice != null) AppendLog(startNotice);
            if (autoRun != null && autoRun.Count > 0) StartTasks(autoRun);
            else CheckForUpdateInBackground();
        }

        void CheckForUpdateInBackground()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    JsonObject checksums = Net.LoadChecksums(CancellationToken.None);
                    Version v;
                    string msg;
                    byte[] data = SelfUpdate.FindNewer(checksums, CancellationToken.None, out v, out msg);
                    if (data == null) return;
                    BeginInvoke((Action)delegate
                    {
                        pendingSelfUpdate = data;
                        updateLabel.Text = "Neue Version " + SelfUpdate.Short(v) + " verfügbar (installiert: " + App.VersionText + ").";
                        updateBar.Visible = true;
                        updateButton.Enabled = !running;
                        logToggle.Set(logToggle.Expanded);   // Fensterhöhe an den Hinweis anpassen
                    });
                }
                catch (Exception ex)
                {
                    pendingLog.Enqueue("Hinweis: Update-Prüfung nicht möglich (" + ex.Message + ")");
                }
            });
        }

        void InstallSelfUpdate()
        {
            if (running || pendingSelfUpdate == null) return;
            try
            {
                SaveOptions(true);
                SelfUpdate.Install(pendingSelfUpdate);
                WriteLogFile();
                SelfUpdate.Restart("--updated");
                closeWhenDone = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Das Update konnte nicht installiert werden:\n" + ex.Message, App.Title,
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void StartTasks(List<string> preset)
        {
            if (running) return;
            List<string> selected = new List<string>();
            foreach (string id in TaskIds) if (checks[id].Checked) selected.Add(id);
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Bitte mindestens eine Aufgabe auswählen.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string media = FolderOf(mediaFolder);
            if ((selected.Contains("merge") || selected.Contains("convert") || selected.Contains("preview") || selected.Contains("cleanup"))
                && !Directory.Exists(media))
            {
                MessageBox.Show(this, "Der Medienordner wurde nicht gefunden:\n" + media + "\n\nBitte oben einen vorhandenen Ordner wählen.",
                                App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Beim automatischen Start nach einem Update die gespeicherte Auswahl nicht überschreiben
            SaveOptions(preset == null);

            ConvertOptions conv = new ConvertOptions();
            conv.Folder = media;
            conv.Parallel = (int)convertParallel.Value;
            conv.DeleteOriginal = optDeleteOriginal.Checked;
            conv.Preset = settings.GetString("convert.preset", "medium");
            conv.QualitySD = settings.GetInt("convert.qualitySD", 21);
            conv.QualityHD = settings.GetInt("convert.qualityHD", 19);
            conv.QualityFullHD = settings.GetInt("convert.qualityFullHD", 18);
            conv.Quality4K = settings.GetInt("convert.quality4K", 17);

            TaskRunner runner = new TaskRunner();
            runner.Tasks = selected;
            runner.UpdateFfmpeg = optFfmpeg.Checked;
            runner.MergeFolder = media;
            runner.MoveParts = optMoveParts.Checked;
            runner.Convert = conv;
            runner.PreviewFolder = media;
            runner.CleanupFolder = media;

            cts = new CancellationTokenSource();
            TaskContext ctx = new TaskContext();
            ctx.Token = cts.Token;
            ctx.Settings = settings;
            ctx.LogLine = delegate(string line) { pendingLog.Enqueue(line); };
            ctx.ProgressSink = delegate(double f, string s) { lock (progressGate) { latestFraction = f; latestStatus = s; } };
            ctx.ChooseMerge = delegate(List<MergeGroup> groups)
            {
                return (List<MergeGroup>)Invoke((Func<List<MergeGroup>>)delegate
                {
                    using (MergeDialog dlg = new MergeDialog(groups)) return dlg.Choose(this);
                });
            };
            ctx.ChooseCleanup = delegate(List<CleanupFolder> folders)
            {
                return (List<CleanupFolder>)Invoke((Func<List<CleanupFolder>>)delegate
                {
                    using (CleanupDialog dlg = new CleanupDialog(folders, media)) return dlg.Choose(this);
                });
            };
            ctx.Ffmpeg = new FfmpegTools(App.Root, settings.GetString("ffmpeg.folder", ""));

            SetRunning(true);
            worker = new Thread(delegate()
            {
                TaskRunResult result = runner.Run(ctx);
                BeginInvoke((Action)delegate { Finished(result); });
            });
            worker.IsBackground = true;
            worker.Start();
        }

        void SetRunning(bool value)
        {
            running = value;
            startButton.Enabled = !value;
            cancelButton.Enabled = value;
            updateButton.Enabled = !value;
            foreach (DarkCheckBox c in checks.Values) c.Enabled = !value;
            if (value) { progress.Value = -1; statusLabel.Text = "Läuft ..."; }
        }

        void CancelTasks()
        {
            if (!running || cts == null) return;
            cancelButton.Enabled = false;
            statusLabel.Text = "Wird abgebrochen ...";
            cts.Cancel();
            Proc.KillAll();
        }

        void Finished(TaskRunResult result)
        {
            FlushUi();
            SetRunning(false);
            progress.Value = result.Success ? 1 : 0;
            statusLabel.Text = result.Cancelled ? "Vorgang abgebrochen."
                             : result.Success ? "Vorgang beendet – alles erfolgreich."
                             : "Vorgang beendet – mit Fehlern (siehe Details).";
            WriteLogFile();

            if (result.Restart)
            {
                string args = "--updated";
                if (result.Remaining.Count > 0) args += " --run " + string.Join(",", result.Remaining);
                try
                {
                    SelfUpdate.Restart(args);
                    closeWhenDone = true;
                    Close();
                    return;
                }
                catch (Exception ex)
                {
                    AppendLog("Neustart nicht möglich: " + ex.Message + ". Bitte das Programm neu starten.");
                }
            }
            if (closeWhenDone) Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running && !closeWhenDone)
            {
                if (MessageBox.Show(this, "Es läuft noch eine Aufgabe. Abbrechen und beenden?", App.Title,
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    closeWhenDone = true;
                    CancelTasks();
                }
                e.Cancel = true;
                return;
            }
            if (running) { e.Cancel = true; return; }   // wartet auf das Ende des Abbruchs
            SaveOptions(autoRun == null);
            WriteLogFile();
            base.OnFormClosing(e);
        }

        // ---------------------------------------------------------------
        // Protokoll und Fortschritt
        // ---------------------------------------------------------------

        readonly StringBuilder unsavedLog = new StringBuilder();

        void AppendLog(string line)
        {
            pendingLog.Enqueue(line);
            FlushUi();
        }

        void FlushUi()
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while (pendingLog.TryDequeue(out line))
            {
                sb.Append(line).Append("\r\n");
                unsavedLog.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ").Append(line).Append("\r\n");
            }
            if (sb.Length > 0)
            {
                if (logBox.TextLength > 2000000) logBox.Text = logBox.Text.Substring(logBox.TextLength - 1000000);
                logBox.AppendText(sb.ToString());
            }

            double f;
            string s;
            lock (progressGate) { f = latestFraction; s = latestStatus; latestFraction = -2; latestStatus = null; }
            if (running && f > -2) progress.Value = f;
            if (running && s != null) statusLabel.Text = s;
            if (unsavedLog.Length > 64 * 1024) WriteLogFile();
        }

        void WriteLogFile()
        {
            if (unsavedLog.Length == 0) return;
            try
            {
                // Alte Protokolle kürzen (die letzten ~1 MB bleiben)
                if (File.Exists(logFile) && new FileInfo(logFile).Length > 4 * 1024 * 1024)
                {
                    string old = File.ReadAllText(logFile, Encoding.UTF8);
                    File.WriteAllText(logFile, old.Substring(old.Length - 1024 * 1024), FileOps.Utf8NoBom);
                }
                File.AppendAllText(logFile, unsavedLog.ToString(), FileOps.Utf8NoBom);
                unsavedLog.Length = 0;
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) uiTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    // ---------------------------------------------------------------
    // Führt die ausgewählten Aufgaben nacheinander aus (ohne Oberfläche nutzbar)
    // ---------------------------------------------------------------

    public class TaskRunResult
    {
        public bool Success = true;
        public bool Cancelled;
        public bool Restart;
        public List<string> Remaining = new List<string>();
    }

    public class TaskRunner
    {
        public List<string> Tasks;
        public bool UpdateFfmpeg = true;
        public string MergeFolder;
        public bool MoveParts = true;
        public ConvertOptions Convert;
        public string PreviewFolder;
        public string CleanupFolder;

        static string Title(string id)
        {
            switch (id)
            {
                case "update": return "Update";
                case "cleanup": return "Ablagen leeren";
                case "merge": return "Gesplittete Medien zusammenführen";
                case "convert": return "Videos für SFLIX konvertieren";
                case "preview": return "Vorschaubilder erstellen";
                case "data": return "Daten einlesen";
            }
            return id;
        }

        public TaskRunResult Run(TaskContext ctx)
        {
            TaskRunResult result = new TaskRunResult();
            List<string> summary = new List<string>();
            DateTime started = DateTime.Now;
            ctx.Log("");
            ctx.Log("Start " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") + ": " + string.Join(", ", Tasks.ConvertAll(Title)));

            for (int i = 0; i < Tasks.Count; i++)
            {
                string id = Tasks[i];
                ctx.Log("");
                ctx.Log("=== " + Title(id) + " ===");
                ctx.Progress(-1, Title(id) + " ...");
                string outcome;
                try
                {
                    Execute(id, ctx);
                    outcome = "OK";
                }
                catch (OperationCanceledException)
                {
                    ctx.Log("Abgebrochen.");
                    summary.Add(Title(id).PadRight(36) + "abgebrochen");
                    result.Cancelled = true;
                    result.Success = false;
                    break;
                }
                catch (TaskFailedException e)
                {
                    ctx.Log("FEHLER: " + e.Message);
                    outcome = "Fehler";
                    result.Success = false;
                }
                catch (Exception e)
                {
                    ctx.Log("FEHLER: " + e.Message);
                    outcome = "Fehler";
                    result.Success = false;
                }
                summary.Add(Title(id).PadRight(36) + outcome);

                if (ctx.RestartRequested)
                {
                    result.Restart = true;
                    for (int j = i + 1; j < Tasks.Count; j++) result.Remaining.Add(Tasks[j]);
                    if (result.Remaining.Count > 0)
                        ctx.Log("Die neue Version wird gestartet und macht weiter mit: " + string.Join(", ", result.Remaining.ConvertAll(Title)));
                    break;
                }
            }

            ctx.Log("");
            ctx.Log("=== Zusammenfassung ===");
            foreach (string s in summary) ctx.Log(s);

            // Deutlicher Abschluss, damit man im Protokoll sieht, dass nichts mehr läuft
            string end;
            if (result.Restart) end = "PROGRAMM WURDE AKTUALISIERT UND STARTET NEU";
            else if (result.Cancelled) end = "VORGANG ABGEBROCHEN";
            else if (result.Success) end = "VORGANG BEENDET - ALLES ERFOLGREICH";
            else end = "VORGANG BEENDET - MIT FEHLERN (siehe oben)";
            ctx.Log("");
            ctx.Log(new string('=', 60));
            ctx.Log("  " + end);
            ctx.Log("  " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + ", Dauer " + Fmt.Time((DateTime.Now - started).TotalSeconds));
            ctx.Log(new string('=', 60));
            ctx.Progress(result.Success ? 1 : 0, end);
            return result;
        }

        void Execute(string id, TaskContext ctx)
        {
            switch (id)
            {
                case "update": UpdateTask.Run(ctx, UpdateFfmpeg); break;
                case "merge": MergeTask.Run(ctx, MergeFolder, MoveParts); break;
                case "convert": ConvertTask.Run(ctx, Convert); break;
                case "preview": PreviewTask.Run(ctx, PreviewFolder); break;
                case "cleanup": CleanupTask.Run(ctx, CleanupFolder); break;
                case "data": DataTask.Run(ctx); break;
                default: throw new TaskFailedException("Unbekannte Aufgabe: " + id);
            }
        }
    }
}
