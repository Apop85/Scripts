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
        // Ausführungsreihenfolge (unabhängig von der Anordnung im Fenster): Update zuerst, Daten
        // einlesen zuletzt (die anderen verändern Dateien). Ablagen leeren vor Import, Zusammenführen
        // und Konvertieren, damit frisch gesicherte Originale nicht im selben Lauf gelöscht werden.
        // Folgen umbenennen vor Zusammenführen, Konvertieren und Vorschaubildern: diese arbeiten
        // dann schon mit den neuen Namen.
        public static readonly string[] TaskIds = { "update", "cleanup", "import", "rename", "renameundo", "merge", "convert", "preview", "data" };

        static readonly Dictionary<string, string> TaskTitles = new Dictionary<string, string> {
            { "update", "Update" },
            { "cleanup", "Ablagen leeren" },
            { "import", "Medien importieren" },
            { "rename", "Folgen umbenennen" },
            { "renameundo", "Folgen umbenennen rückgängig machen" },
            { "merge", "Gesplittete Medien zusammenführen" },
            { "convert", "Videos für STEFFLIX konvertieren" },
            { "preview", "Vorschaubilder erstellen" },
            { "data", "Daten einlesen" },
        };

        readonly Settings settings;
        readonly Dictionary<string, TileButton> checks = new Dictionary<string, TileButton>();
        readonly List<string> autoRun;
        readonly string startNotice;

        // Optionen
        DarkCheckBox optFfmpeg, optMoveParts, optDeleteOriginal, optPreviewVideos, optPreviewImages;
        DarkCheckBox optImportStructure, optImportConvert, optImportMove, optImportPreview;
        DarkCheckBox optDataAll;
        ListBox dataFolders;
        Control dataFoldersRow;
        TextBox installFolderBox, importSub, renameFolder;
        FolderPick pickPreview, pickMerge, pickConvert, pickCleanup;
        Label renameUndoInfo;
        Button renameUndoButton;
        NumericUpDown convertParallel;
        ListBox importList;
        ComboBox importCategory;
        Label importHint;

        // Registerkarten: nur der Bereich der gewählten Karte ist sichtbar
        const int PageUpdates = 0, PageImport = 1, PageTools = 2;
        static readonly string[] PageSubtitles = { "UPDATES", "IMPORTER", "TOOLS" };
        static readonly string[] ToolIds = { "cleanup", "rename", "merge", "convert", "preview" };
        TabStrip tabs;
        Control[] pages;
        TableLayoutPanel rootLayout;

        // Protokoll rechts neben dem Inhalt (Details), Breite per Splitter
        LogPanel logPanel;
        SplitBar splitBar;
        TableLayoutPanel body;
        Button detailsButton;
        bool logExpanded;
        int logWidth;
        Panel tasksScroller;
        BannerPanel banner;
        Panel updateBar;
        Label updateLabel;
        Button updateButton;
        Button startButton, cancelButton;
        DarkProgress progress;
        Label statusLabel;

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
            ClientSize = new Size(Math.Min(Theme.S(940), work.Width - 40), Math.Min(Theme.S(820), work.Height - 60));
            MinimumSize = new Size(Math.Min(Theme.S(700), work.Width), Math.Min(Theme.S(400), work.Height));

            BuildUi();
            LoadOptions();
            EnableDrop(this);

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
            rootLayout = root;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(110)));   // Kopfbild
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(46)));    // Registerkarten
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Update-Hinweis
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));              // Inhalt der Karte, rechts das Protokoll
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Knöpfe
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));                  // Fortschritt
            Controls.Add(root);

            banner = new BannerPanel();
            banner.Version = "v" + App.VersionText;
            banner.Subtitle = PageSubtitles[PageUpdates];
            banner.Dock = DockStyle.Fill;
            banner.Margin = new Padding(0);
            root.Controls.Add(banner, 0, 0);

            tabs = new TabStrip();
            tabs.Dock = DockStyle.Fill;
            tabs.Margin = new Padding(0);
            tabs.Add("Updates", Glyph.Up);
            tabs.Add("Importer", Glyph.Import);
            tabs.Add("Tools", Glyph.Tools);
            tabs.Changed = ShowPage;
            root.Controls.Add(tabs, 0, 1);

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
            updateButton.Click += delegate { RunUpdateNow(); };
            updateBar.Controls.Add(updateLabel);
            updateBar.Controls.Add(updateButton);
            root.Controls.Add(updateBar, 0, 2);

            // Inhalt der gewählten Karte (scrollen, falls der Bildschirm zu klein ist)
            Panel scroller = new Panel();
            tasksScroller = scroller;
            scroller.Dock = DockStyle.Fill;
            scroller.AutoScroll = true;
            scroller.Margin = new Padding(Theme.S(16), Theme.S(12), Theme.S(6), 0);
            scroller.BackColor = Theme.Back;

            // Inhalt | Splitter | Protokoll (eingeklappt: nur der Inhalt)
            body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.AutoSize = true;
            body.ColumnCount = 3;
            body.RowCount = 1;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.Margin = new Padding(0);
            body.Padding = new Padding(0);
            body.BackColor = Theme.Back;
            splitBar = new SplitBar();
            splitBar.Dock = DockStyle.Fill;
            splitBar.Margin = new Padding(0, Theme.S(12), 0, 0);
            splitBar.Visible = false;
            logWidth = settings.GetInt("log.width", 0);
            if (logWidth < Theme.S(280)) logWidth = Theme.S(460);
            splitBar.GetWidth = delegate { return logWidth; };
            splitBar.SetWidth = SetLogWidth;
            splitBar.DragEnded = delegate { settings.SetInt("log.width", logWidth); EqualizeOptionHosts(); Relayout(); };
            logPanel = new LogPanel();
            logPanel.Dock = DockStyle.Fill;
            logPanel.Margin = new Padding(0, Theme.S(12), Theme.S(16), 0);
            logPanel.Visible = false;
            logPanel.LogFile = logFile;
            logPanel.BeforeOpenFile = WriteLogFile;
            // Bei einem Fehler das Protokoll von selbst zeigen
            logPanel.ErrorAdded = delegate { if (!logExpanded) SetLogExpanded(true); };
            body.Controls.Add(scroller, 0, 0);
            body.Controls.Add(splitBar, 1, 0);
            body.Controls.Add(logPanel, 2, 0);
            root.Controls.Add(body, 0, 3);

            // --- Updates: Kernkomponenten ---
            optFfmpeg = SmallCheck("ffmpeg ebenfalls aktualisieren");
            optDataAll = SmallCheck("Alle Ordner einlesen");
            optDataAll.CheckedChanged += delegate { dataFoldersRow.Visible = !optDataAll.Checked; Relayout(); };
            dataFoldersRow = BuildDataFoldersRow();
            Control updatesSection = TileSection(new TileSpec[] {
                Spec("update", Glyph.Up, "Update", "STEFFLIX und Updater von GitHub",
                     "STEFFLIX-Dateien und dieses Programm von GitHub aktualisieren.", optFfmpeg),
                Spec("data", Glyph.Data, "Daten einlesen", "Medien neu einlesen",
                     "Medien neu einlesen (data.js), ungültige Zeichen in Namen korrigieren.", optDataAll, dataFoldersRow),
            }, null);

            // --- Tools: Kacheln, darunter je gewähltem Tool ein Reiter mit seinen Optionen ---
            optPreviewVideos = SmallCheck("für Videos (Bild aus der Mitte)");
            optPreviewImages = SmallCheck("für Bilder");
            FlowLayoutPanel previewRow = new FlowLayoutPanel();
            previewRow.AutoSize = true;
            previewRow.WrapContents = true;
            previewRow.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
            optPreviewImages.Margin = new Padding(Theme.S(20), Theme.S(2), 0, Theme.S(2));
            previewRow.Controls.Add(optPreviewVideos);
            previewRow.Controls.Add(optPreviewImages);

            optMoveParts = SmallCheck("Teile danach nach „" + MergeTask.PartsFolderName + "“ verschieben");

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

            // Einstellungen: der Installationsordner wird bei der Installation festgelegt und hier nur angezeigt
            installFolderBox = Theme.MakeTextBox();
            installFolderBox.ReadOnly = true;
            installFolderBox.BackColor = Theme.Card;
            installFolderBox.ForeColor = Theme.Muted;
            Label folderHint = Theme.MakeLabel("Wird bei der Installation festgelegt. Mit „Alles“ durchsuchen die Tools den ganzen Installationsordner, " +
                                               "sonst nur die gewählten Ordner (auch ausserhalb von STEFFLIX, ausser bei „Ablagen leeren“).", true);
            folderHint.MaximumSize = new Size(TextWidth, 0);
            folderHint.Margin = new Padding(0, Theme.S(4), 0, 0);

            // Ordner je Tool (werden nicht gespeichert, Standard: Alles)
            pickPreview = MakeFolderPick("Vorschaubilder", false);
            pickMerge = MakeFolderPick("Zusammenführen", false);
            pickConvert = MakeFolderPick("Konvertieren", false);
            pickCleanup = MakeFolderPick("Ablagen leeren", true);

            // Folgen umbenennen: ein Serienordner, darunter "Rückgängig"
            renameFolder = Theme.MakeTextBox();
            Control renameRow = FolderRow(renameFolder, "Serienordner:");
            FlowLayoutPanel undoRow = new FlowLayoutPanel();
            undoRow.AutoSize = true;
            undoRow.WrapContents = false;
            undoRow.Margin = new Padding(0, Theme.S(8), 0, 0);
            undoRow.BackColor = Color.Transparent;
            renameUndoButton = SmallButton("Letztes Umbenennen rückgängig");
            renameUndoButton.Click += delegate { UndoRename(); };
            renameUndoInfo = Theme.MakeLabel("", true);
            renameUndoInfo.Margin = new Padding(Theme.S(4), Theme.S(6), 0, 0);
            undoRow.Controls.Add(renameUndoButton);
            undoRow.Controls.Add(renameUndoInfo);

            Control toolsSection = TileSection(new TileSpec[] {
                Spec("preview", Glyph.Photo, "Vorschaubilder", "Fehlende Bilder erstellen",
                     "Fehlende Vorschaubilder (480×270) im Preview-Ordner erstellen.", previewRow, pickPreview.Root),
                Spec("rename", Glyph.Rename, "Folgen umbenennen", "Titel aus dem Internet",
                     "Folgen einer Serie anhand der Folgenliste von fernsehserien.de (sonst TVmaze) umbenennen: „Serie - S01E02 - Titel“ " +
                     "im Ordner „Staffel 01“. Erkannt werden S01E02 und 1x02. Du wählst die Serie und siehst vorher alle Änderungen.",
                     renameRow, undoRow),
                Spec("merge", Glyph.Merge, "Zusammenführen", "pt1, pt2 … zu einer Datei",
                     "Gesplittete Videos wie „Titel pt1“, „Titel pt2“ zu einer Datei zusammenfügen. Du wählst vorher aus, welche.", optMoveParts, pickMerge.Root),
                Spec("convert", Glyph.Convert, "Konvertieren", "Videos in MP4 umwandeln",
                     "Videos in browserkompatible MP4 (H.264/AAC) umwandeln. Das Original kommt nach „" + ConvertTask.OriginalsFolderName + "“.",
                     parallelRow, optDeleteOriginal, pickConvert.Root),
                Spec("cleanup", Glyph.Trash, "Ablagen leeren", "__-Ordner endgültig löschen",
                     "„__teile“, „__konvertiert“, „__gelöscht“, „__original“ und „" + EpisodeTask.ReplacedFolderName + "“ endgültig löschen. " +
                     "Du siehst vorher, was gelöscht wird. Nur innerhalb des Installationsordners möglich.", pickCleanup.Root),
            }, Spec("settings", Glyph.Settings, "Einstellungen", null, null, FolderRow(installFolderBox, "Installationsordner:", false), folderHint));

            Label toolsNote = Theme.MakeLabel("Die gewählten Tools laufen nacheinander. Danach werden die betroffenen Ordner automatisch neu " +
                                              "eingelesen („Daten einlesen“), sofern sie im Installationsordner liegen.", true);
            toolsNote.MaximumSize = new Size(TextWidth, 0);
            toolsNote.Margin = new Padding(0, 0, 0, Theme.S(10));

            // Je Registerkarte eine Seite. Ausgeführt wird in der Reihenfolge von TaskIds.
            // Updates: links die Kacheln, rechts die installierten und verfügbaren Versionen
            TableLayoutPanel updatesRow = new TableLayoutPanel();
            updatesRow.AutoSize = true;
            updatesRow.Dock = DockStyle.Fill;
            updatesRow.ColumnCount = 2;
            updatesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            updatesRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(330)));
            updatesRow.Margin = new Padding(0);
            updatesRow.BackColor = Theme.Back;
            updatesRow.Controls.Add(updatesSection, 0, 0);
            updatesRow.Controls.Add(BuildVersionsCard(), 1, 0);

            pages = new Control[] {
                Page(new Control[] { updatesRow }),
                Page(BuildImportPage()),
                Page(new Control[] { toolsNote, toolsSection }),
            };
            // Dock Top: in umgekehrter Reihenfolge einfügen, sichtbar ist ohnehin nur eine
            for (int i = pages.Length - 1; i >= 0; i--) scroller.Controls.Add(pages[i]);
            for (int i = 0; i < pages.Length; i++) pages[i].Visible = i == PageUpdates;

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
            root.Controls.Add(buttons, 0, 4);

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
            // "Details" blendet das Protokoll rechts ein und aus (standardmässig eingeklappt)
            detailsButton = LogToggle.MakeButton();
            detailsButton.Text = "Details ▸";
            detailsButton.Click += delegate { SetLogExpanded(!logExpanded); };
            prog.Controls.Add(progress, 0, 0);
            prog.SetColumnSpan(progress, 2);
            prog.Controls.Add(statusLabel, 0, 1);
            prog.Controls.Add(detailsButton, 1, 1);
            root.Controls.Add(prog, 0, 5);
        }

        // ---------------------------------------------------------------
        // Protokoll ein- und ausblenden, Breite, Fensterhöhe
        // ---------------------------------------------------------------

        const int MinContentWidth = 560;   // Mindestbreite des Inhalts neben dem Protokoll (vor Skalierung)

        void SetLogExpanded(bool expanded)
        {
            bool changed = expanded != logExpanded;
            logExpanded = expanded;
            detailsButton.Text = expanded ? "Details ◂" : "Details ▸";
            Rectangle work = Screen.FromControl(this).WorkingArea;
            int extra = splitBar.Width + logWidth;
            Size minimum = new Size(Math.Min(Theme.S(700) + (expanded ? extra : 0), work.Width), MinimumSize.Height);
            // Beim Zuklappen die Mindestgrösse zuerst verkleinern, sonst lässt sich das Fenster nicht
            // schmaler machen. Beim Aufklappen erst danach vergrössern, sonst wächst es doppelt.
            if (!expanded || !changed) MinimumSize = minimum;

            body.SuspendLayout();
            splitBar.Visible = expanded;
            logPanel.Visible = expanded;
            body.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, expanded ? splitBar.Width : 0);
            body.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, expanded ? logWidth : 0);
            body.ResumeLayout(true);

            // Das Fenster wird um das Protokoll breiter bzw. wieder schmaler
            if (changed && IsHandleCreated && WindowState == FormWindowState.Normal)
            {
                if (expanded)
                {
                    Width = Math.Min(Width + extra, work.Width);
                    if (Right > work.Right) Left = Math.Max(work.Left, work.Right - Width);
                    SetLogWidth(logWidth);   // passt das Protokoll nicht ganz, wird es schmaler
                }
                else Width = Math.Max(MinimumSize.Width, Width - extra);
            }
            if (expanded && changed) MinimumSize = minimum;
            if (expanded) logPanel.ScrollToBottom();
            EqualizeOptionHosts();
            Relayout();
        }

        // Breite des Protokolls (Splitter), begrenzt: der Inhalt behält seine Mindestbreite
        void SetLogWidth(int width)
        {
            int max = body.ClientSize.Width - splitBar.Width - Theme.S(MinContentWidth);
            width = Math.Max(Theme.S(280), Math.Min(width, Math.Max(Theme.S(280), max)));
            logWidth = width;
            if (!logExpanded) return;
            body.ColumnStyles[2] = new ColumnStyle(SizeType.Absolute, width);
        }

        // Fensterhöhe an den Inhalt anpassen. Ist das Protokoll offen, bekommt es mindestens
        // eine brauchbare Höhe; ist der Bildschirm zu niedrig, scrollt der Inhalt.
        void FitHeight()
        {
            if (!IsHandleCreated || WindowState != FormWindowState.Normal || rootLayout == null) return;
            Rectangle work = Screen.FromControl(this).WorkingArea;
            int maxClient = work.Height - (Height - ClientSize.Height) - 20;

            rootLayout.SuspendLayout();
            rootLayout.RowStyles[3] = new RowStyle(SizeType.AutoSize);
            tasksScroller.AutoSize = true;
            tasksScroller.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            rootLayout.ResumeLayout(true);
            int natural = rootLayout.GetPreferredSize(new Size(rootLayout.Width, 0)).Height;
            if (logExpanded) natural = Math.Max(natural, Theme.S(640));

            rootLayout.SuspendLayout();
            tasksScroller.AutoSize = false;
            rootLayout.RowStyles[3] = new RowStyle(SizeType.Percent, 100);
            rootLayout.ResumeLayout(true);
            ClientSize = new Size(ClientSize.Width, Math.Min(natural, maxClient));
            if (Bottom > work.Bottom) Top = Math.Max(work.Top, work.Bottom - Height);
            // Eine beim Vergrössern kurz nötige Scrollleiste bliebe sonst stehen
            tasksScroller.AutoScroll = false;
            tasksScroller.AutoScroll = true;
        }

        // ---------------------------------------------------------------
        // Ordnerwahl der Tools: "Alles" oder eine Liste von Ordnern
        // ---------------------------------------------------------------

        class FolderItem
        {
            public string Path;
            public override string ToString()
            {
                string rel = DataTask.ToRelative(Path);
                return string.IsNullOrEmpty(rel) ? Path : rel;
            }
        }

        class FolderPick
        {
            public DarkCheckBox All;
            public ListBox List;
            public Control Row;
            public Control Root;
            public string Tool;
            public bool InsideOnly;

            public List<string> Folders()
            {
                List<string> list = new List<string>();
                if (All.Checked) { list.Add(App.Root); return list; }
                foreach (FolderItem f in List.Items) list.Add(f.Path);
                return list;
            }
        }

        FolderPick MakeFolderPick(string tool, bool insideOnly)
        {
            FolderPick pick = new FolderPick();
            pick.Tool = tool;
            pick.InsideOnly = insideOnly;
            pick.All = SmallCheck("Alles (ganzer Installationsordner)");
            pick.All.Checked = true;

            pick.List = new ListBox();
            pick.List.BackColor = Theme.Input;
            pick.List.ForeColor = Theme.Text;
            pick.List.BorderStyle = BorderStyle.None;
            pick.List.IntegralHeight = false;
            pick.List.SelectionMode = SelectionMode.MultiExtended;
            pick.List.Dock = DockStyle.Fill;
            pick.List.Margin = new Padding(0, 0, Theme.S(8), 0);
            pick.List.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemovePickFolders(pick); };

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0);
            Button add = SmallButton("Hinzufügen …");
            Button remove = SmallButton("Entfernen");
            foreach (Button b in new Button[] { add, remove })
            {
                b.AutoSize = false;
                b.Size = new Size(Theme.S(120), Theme.S(30));
                b.Margin = new Padding(0, 0, 0, Theme.S(6));
                buttons.Controls.Add(b);
            }
            add.Click += delegate { AddPickFolder(pick); };
            remove.Click += delegate { RemovePickFolders(pick); };

            TableLayoutPanel row = new TableLayoutPanel();
            row.AutoSize = true;
            row.ColumnCount = 2;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(72)));
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Margin = new Padding(0, Theme.S(4), 0, 0);
            row.Controls.Add(pick.List, 0, 0);
            row.Controls.Add(buttons, 1, 0);
            row.Visible = false;
            pick.Row = row;

            TableLayoutPanel box = new TableLayoutPanel();
            box.AutoSize = true;
            box.ColumnCount = 1;
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            box.Margin = new Padding(0, Theme.S(6), 0, 0);
            box.BackColor = Color.Transparent;
            box.Controls.Add(pick.All);
            box.Controls.Add(row);
            pick.Root = box;
            pick.All.CheckedChanged += delegate { row.Visible = !pick.All.Checked; Relayout(); };
            return pick;
        }

        void AddPickFolder(FolderPick pick)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Ordner für „" + pick.Tool + "“ wählen" + (pick.InsideOnly ? " (nur im Installationsordner)" : "");
                dlg.ShowNewFolderButton = false;
                dlg.SelectedPath = App.Root;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string path = Path.GetFullPath(dlg.SelectedPath);
                string rel = DataTask.ToRelative(path);
                string reason;
                if (pick.InsideOnly && !Folders.IsInsideRoot(path, out reason))
                {
                    MessageBox.Show(this, "„" + pick.Tool + "“ ist nur im Installationsordner möglich, damit nicht versehentlich andere Daten gelöscht werden.\n\n" +
                                    "Der gewählte Ordner " + reason + ":\n" + path, App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (rel != null && rel.Length == 0)
                {
                    MessageBox.Show(this, "Für den ganzen Installationsordner bitte „Alles“ einschalten.", App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (rel != null && Folders.IgnoreAny(App.Root, rel))
                {
                    MessageBox.Show(this, "Ablage-, System- und versteckte Ordner werden von den Tools übersprungen.", App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (FolderItem existing in pick.List.Items)
                    if (string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)) return;
                FolderItem item = new FolderItem();
                item.Path = path;
                pick.List.Items.Add(item);
            }
        }

        void RemovePickFolders(FolderPick pick)
        {
            List<object> sel = new List<object>();
            foreach (object o in pick.List.SelectedItems) sel.Add(o);
            foreach (object o in sel) pick.List.Items.Remove(o);
        }

        FolderPick PickOf(string id)
        {
            switch (id)
            {
                case "preview": return pickPreview;
                case "merge": return pickMerge;
                case "convert": return pickConvert;
                case "cleanup": return pickCleanup;
            }
            return null;
        }

        // ---------------------------------------------------------------
        // Folgen umbenennen: Rückgängig
        // ---------------------------------------------------------------

        void UpdateUndoInfo()
        {
            string series;
            string info = EpisodeTask.UndoInfo(out series);
            renameUndoButton.Enabled = info != null && !running;
            renameUndoInfo.Text = info ?? "Noch nichts umbenannt.";
        }

        void UndoRename()
        {
            string series;
            string info = EpisodeTask.UndoInfo(out series);
            if (info == null || running) return;
            if (MessageBox.Show(this, "Das letzte Umbenennen rückgängig machen?\n\n" + info, App.Title,
                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            StartTasks(new List<string> { "renameundo", "data" });
        }

        // Breite für umbrechende Beschreibungen
        static int TextWidth { get { return Theme.S(800); } }

        static Control Page(Control[] content)
        {
            TableLayoutPanel page = Column();
            page.Dock = DockStyle.Top;
            page.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            page.Margin = new Padding(0);
            page.Padding = new Padding(0, 0, Theme.S(10), 0);
            foreach (Control c in content) page.Controls.Add(c);
            return page;
        }

        // Registerkarte wechseln: nur deren Bereich zeigen, Untertitel im Kopfbild anpassen
        void ShowPage(int index)
        {
            if (tabs.Selected != index) { tabs.Selected = index; return; }   // ruft ShowPage erneut auf
            SuspendLayout();
            for (int i = 0; i < pages.Length; i++) pages[i].Visible = i == index;
            tasksScroller.AutoScrollPosition = new Point(0, 0);
            ResumeLayout(true);
            banner.Subtitle = PageSubtitles[index];
            banner.Invalidate();
            UpdateStartText();
            Relayout();
        }

        // Karte ohne Häkchen mit Überschrift (für den Importer)
        static Control SectionCard(string title, Control[] content)
        {
            TableLayoutPanel card = new TableLayoutPanel();
            card.Dock = DockStyle.Fill;
            card.AutoSize = true;
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.BackColor = Theme.Card;
            card.Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), Theme.S(8));
            card.Margin = new Padding(0, 0, 0, Theme.S(5));
            Label t = Theme.MakeLabel(title, false);
            t.Font = new Font("Segoe UI Semibold", 10.5f);
            t.Margin = new Padding(0, 0, 0, Theme.S(2));
            card.Controls.Add(t);
            foreach (Control c in content)
            {
                c.Margin = new Padding(0, c.Margin.Top, 0, c.Margin.Bottom);
                card.Controls.Add(c);
            }
            return card;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            EqualizeOptionHosts();
            UpdateStartText();
            SetLogExpanded(false);
        }

        // ---------------------------------------------------------------
        // Kacheln mit Options-Reitern (Updates und Tools)
        // ---------------------------------------------------------------

        class TileSpec
        {
            public string Id, Title, Subtitle, Description;
            public Glyph Icon;
            public Control[] Options;
        }

        static TileSpec Spec(string id, Glyph icon, string title, string subtitle, string description, params Control[] options)
        {
            TileSpec s = new TileSpec();
            s.Id = id; s.Icon = icon; s.Title = title; s.Subtitle = subtitle; s.Description = description; s.Options = options;
            return s;
        }

        // Ein Bereich: Kacheln oben, darunter ein Reiter je gewählter Kachel (plus ein fester
        // Reiter, z.B. Einstellungen) mit Beschreibung und Optionen.
        class TileSectionState
        {
            public TileSpec[] Specs;
            public TileSpec Fixed;
            public TabStrip Strip;
            public TableLayoutPanel Host;
            public Label Empty;
            public readonly Dictionary<string, Control> Panels = new Dictionary<string, Control>();
            public readonly List<string> Shown = new List<string>();
        }

        readonly List<TileSectionState> sections = new List<TileSectionState>();

        Control TileSection(TileSpec[] specs, TileSpec fixedTab)
        {
            TileSectionState sec = new TileSectionState();
            sec.Specs = specs;
            sec.Fixed = fixedTab;
            sections.Add(sec);

            TableLayoutPanel col = Column();
            col.Margin = new Padding(0);

            // Kacheln nebeneinander, gleich breit
            TableLayoutPanel grid = new TableLayoutPanel();
            grid.AutoSize = true;
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = specs.Length;
            grid.RowCount = 1;
            grid.Margin = new Padding(0, 0, 0, Theme.S(14));
            grid.BackColor = Theme.Back;
            for (int i = 0; i < specs.Length; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / specs.Length));
                TileSpec spec = specs[i];
                TileButton tile = new TileButton();
                tile.Icon = spec.Icon;
                tile.Title = spec.Title;
                tile.Subtitle = spec.Subtitle;
                tile.Dock = DockStyle.Fill;
                tile.Margin = new Padding(i == 0 ? 0 : Theme.S(5), 0, i == specs.Length - 1 ? 0 : Theme.S(5), 0);
                tile.CheckedChanged += delegate
                {
                    RefreshOptionTabs(sec, tile.Checked && !loading ? spec.Id : null);
                    UpdateStartText();
                };
                checks[spec.Id] = tile;
                grid.Controls.Add(tile, i, 0);
            }

            // Reiter für die Optionen
            TabStrip strip = new TabStrip();
            strip.StripBack = Theme.Back;
            strip.ActiveFill = Theme.Card;
            strip.InactiveFill = Color.FromArgb(0x1c, 0x1c, 0x21);
            strip.HoverFill = Color.FromArgb(0x24, 0x24, 0x2a);
            strip.IconSize = 16;
            strip.Pad = 12;
            strip.TopGap = 0;
            strip.Inset = 0;
            strip.Font = new Font("Segoe UI Semibold", 9.5f);
            strip.Height = Theme.S(38);
            strip.Dock = DockStyle.Fill;
            strip.Margin = new Padding(0);
            strip.Changed = delegate(int index) { ShowOptionPanel(sec, sec.Shown[index]); };
            sec.Strip = strip;

            TableLayoutPanel host = new TableLayoutPanel();
            host.AutoSize = true;
            host.ColumnCount = 1;
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            host.Dock = DockStyle.Fill;
            host.BackColor = Theme.Card;
            host.Padding = new Padding(Theme.S(16), Theme.S(12), Theme.S(16), Theme.S(14));
            host.Margin = new Padding(0, 0, 0, Theme.S(4));
            sec.Host = host;

            List<TileSpec> all = new List<TileSpec>(specs);
            if (fixedTab != null) all.Add(fixedTab);
            foreach (TileSpec spec in all)
            {
                TableLayoutPanel panel = new TableLayoutPanel();
                panel.AutoSize = true;
                panel.ColumnCount = 1;
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                panel.Dock = DockStyle.Fill;
                panel.Margin = new Padding(0);
                panel.BackColor = Color.Transparent;
                if (!string.IsNullOrEmpty(spec.Description))
                {
                    Label desc = Theme.MakeLabel(spec.Description, true);
                    desc.MaximumSize = new Size(TextWidth, 0);
                    desc.Margin = new Padding(0, 0, 0, spec.Options.Length > 0 ? Theme.S(8) : 0);
                    panel.Controls.Add(desc);
                }
                foreach (Control c in spec.Options)
                {
                    c.Margin = new Padding(0, c.Margin.Top, 0, c.Margin.Bottom);
                    panel.Controls.Add(c);
                }
                panel.Visible = false;
                sec.Panels[spec.Id] = panel;
                host.Controls.Add(panel);
            }
            sec.Empty = Theme.MakeLabel("Wähle oben aus, was ausgeführt werden soll. Hier erscheinen dann die Optionen.", true);
            sec.Empty.Margin = new Padding(0);
            host.Controls.Add(sec.Empty);

            col.Controls.Add(grid);
            col.Controls.Add(strip);
            col.Controls.Add(host);
            RefreshOptionTabs(sec, null);
            return col;
        }

        // Reiter neu aufbauen: einer je gewählter Kachel (in Kachel-Reihenfolge), der feste zuletzt.
        // activate: dieser Reiter wird gezeigt (z.B. die eben gewählte Kachel).
        void RefreshOptionTabs(TileSectionState sec, string activate)
        {
            // Beim Laden der Einstellungen: erster gewählter Reiter vorne
            string current = !loading && sec.Shown.Count > 0 && sec.Strip.Selected < sec.Shown.Count ? sec.Shown[sec.Strip.Selected] : null;
            sec.Shown.Clear();
            List<string> titles = new List<string>();
            List<Glyph> icons = new List<Glyph>();
            List<TileSpec> all = new List<TileSpec>();
            foreach (TileSpec s in sec.Specs) if (checks[s.Id].Checked) all.Add(s);
            if (sec.Fixed != null) all.Add(sec.Fixed);
            foreach (TileSpec s in all) { sec.Shown.Add(s.Id); titles.Add(s.Title); icons.Add(s.Icon); }

            string want = activate != null && sec.Shown.Contains(activate) ? activate
                        : current != null && sec.Shown.Contains(current) ? current
                        : sec.Shown.Count > 0 ? sec.Shown[0] : null;
            sec.Strip.SetTabs(titles, icons, want == null ? 0 : sec.Shown.IndexOf(want));
            ShowOptionPanel(sec, want);
        }

        void ShowOptionPanel(TileSectionState sec, string id)
        {
            sec.Host.SuspendLayout();
            foreach (KeyValuePair<string, Control> p in sec.Panels) p.Value.Visible = p.Key == id;
            // Hinweis nur, wenn noch nichts gewählt ist
            bool anyTile = false;
            foreach (TileSpec s in sec.Specs) if (checks[s.Id].Checked) anyTile = true;
            sec.Empty.Visible = !anyTile && id == null;
            if (!anyTile && id != null && sec.Fixed != null && id == sec.Fixed.Id) sec.Empty.Visible = false;
            sec.Host.ResumeLayout(true);
            Relayout();
        }

        // Reiter einer Kachel (oder des festen Reiters) nach vorne holen, z.B. bei einer Rückfrage
        void ShowOption(string id)
        {
            foreach (TileSectionState sec in sections)
            {
                if (!sec.Panels.ContainsKey(id)) continue;
                for (int p = 0; p < pages.Length; p++)
                    if (IsChildOf(sec.Host, pages[p])) ShowPage(p);
                if (sec.Shown.Contains(id))
                {
                    sec.Strip.SetTabs(TabTitles(sec), TabIcons(sec), sec.Shown.IndexOf(id));
                    ShowOptionPanel(sec, id);
                }
            }
        }

        static bool IsChildOf(Control c, Control parent)
        {
            for (Control p = c; p != null; p = p.Parent) if (p == parent) return true;
            return false;
        }

        List<string> TabTitles(TileSectionState sec)
        {
            List<string> l = new List<string>();
            foreach (string id in sec.Shown) l.Add(SpecOf(sec, id).Title);
            return l;
        }

        List<Glyph> TabIcons(TileSectionState sec)
        {
            List<Glyph> l = new List<Glyph>();
            foreach (string id in sec.Shown) l.Add(SpecOf(sec, id).Icon);
            return l;
        }

        static TileSpec SpecOf(TileSectionState sec, string id)
        {
            foreach (TileSpec s in sec.Specs) if (s.Id == id) return s;
            return sec.Fixed;
        }

        // Alle Options-Bereiche so hoch wie ihr höchster Reiter: das Fenster springt beim
        // Wechseln der Reiter nicht auf und ab
        void EqualizeOptionHosts()
        {
            int width = tasksScroller.ClientSize.Width - Theme.S(10);
            if (width < Theme.S(300)) width = ClientSize.Width - Theme.S(40);
            foreach (TileSectionState sec in sections)
            {
                int inner = width - sec.Host.Padding.Horizontal;
                int max = sec.Empty.GetPreferredSize(new Size(inner, 0)).Height;
                foreach (Control p in sec.Panels.Values)
                    max = Math.Max(max, p.GetPreferredSize(new Size(inner, 0)).Height);
                sec.Host.MinimumSize = new Size(0, max + sec.Host.Padding.Vertical);
            }
        }

        // ---------------------------------------------------------------
        // Versionen (rechts auf der Seite Updates)
        // ---------------------------------------------------------------

        readonly Label[] verInstalled = new Label[3], verAvailable = new Label[3];
        Label verStatus;
        Button verRecheck;

        Control BuildVersionsCard()
        {
            TableLayoutPanel card = new TableLayoutPanel();
            card.AutoSize = true;
            card.ColumnCount = 1;
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            card.BackColor = Theme.Card;
            card.Padding = new Padding(Theme.S(16), Theme.S(12), Theme.S(16), Theme.S(12));
            card.Margin = new Padding(Theme.S(12), 0, 0, 0);

            Label title = Theme.MakeLabel("Versionen", false);
            title.Font = new Font("Segoe UI Semibold", 10.5f);
            title.Margin = new Padding(0, 0, 0, Theme.S(8));
            card.Controls.Add(title);

            TableLayoutPanel grid = new TableLayoutPanel();
            grid.AutoSize = true;
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            grid.Margin = new Padding(0);
            grid.BackColor = Color.Transparent;

            string[] heads = { "", "Installiert", "Verfügbar" };
            for (int c = 0; c < 3; c++)
            {
                Label h = Theme.MakeLabel(heads[c], true);
                h.Font = new Font("Segoe UI", 8.5f);
                h.Margin = new Padding(0, 0, 0, Theme.S(4));
                grid.Controls.Add(h, c, 0);
            }
            string[] names = { "STEFFLIX", "Updater", "ffmpeg" };
            for (int r = 0; r < 3; r++)
            {
                Panel line = new Panel();
                line.BackColor = Theme.Line;
                line.Height = 1;
                line.Dock = DockStyle.Fill;
                line.Margin = new Padding(0);
                grid.Controls.Add(line, 0, 1 + r * 2);
                grid.SetColumnSpan(line, 3);

                Label n = Theme.MakeLabel(names[r], false);
                verInstalled[r] = Theme.MakeLabel("", false);
                verAvailable[r] = Theme.MakeLabel("…", true);
                foreach (Label l in new Label[] { n, verInstalled[r], verAvailable[r] })
                    l.Margin = new Padding(0, Theme.S(7), Theme.S(4), Theme.S(7));
                grid.Controls.Add(n, 0, 2 + r * 2);
                grid.Controls.Add(verInstalled[r], 1, 2 + r * 2);
                grid.Controls.Add(verAvailable[r], 2, 2 + r * 2);
            }
            card.Controls.Add(grid);

            verStatus = Theme.MakeLabel("Wird geprüft …", true);
            verStatus.Font = new Font("Segoe UI", 8.5f);
            verStatus.MaximumSize = new Size(Theme.S(290), 0);
            verStatus.Margin = new Padding(0, Theme.S(8), 0, Theme.S(8));
            card.Controls.Add(verStatus);

            FlowLayoutPanel foot = new FlowLayoutPanel();
            foot.AutoSize = true;
            foot.WrapContents = false;
            foot.Margin = new Padding(0);
            foot.BackColor = Color.Transparent;
            Button notes = SmallButton("Patchnotes");
            notes.Click += delegate { ShowPatchNotes(this); };
            verRecheck = SmallButton("Neu prüfen");
            verRecheck.Click += delegate { CheckForUpdateInBackground(false); };
            foot.Controls.Add(notes);
            foot.Controls.Add(verRecheck);
            card.Controls.Add(foot);

            ShowInstalledVersions();
            return card;
        }

        // Patchnotes von GitHub laden und zeigen: alle Versionen, die neuer sind als die installierte
        // (sonst die aktuelle). owner: Fenster, über dem sie erscheinen.
        void ShowPatchNotes(Form owner)
        {
            string local = UpdateCheck.LocalWebVersion();
            Cursor old = owner.Cursor;
            owner.Cursor = Cursors.WaitCursor;
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<PatchVersion> all = null;
                string error = null;
                try { all = PatchNotes.Load(CancellationToken.None); }
                catch (Exception ex) { error = ex.Message; }
                BeginInvoke((Action)delegate
                {
                    owner.Cursor = old;
                    if (owner.IsDisposed) return;
                    if (error != null)
                    {
                        MessageBox.Show(owner, "Die Patchnotes konnten nicht geladen werden:\n" + error, App.Title,
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    bool newer;
                    List<PatchVersion> list = PatchNotes.Select(all, local, out newer);
                    string heading = newer ? "Neu seit STEFFLIX " + local
                                   : "Neuerungen in STEFFLIX " + (list.Count > 0 ? list[0].Version : (local ?? ""));
                    using (PatchNotesDialog dlg = new PatchNotesDialog(heading, list))
                    {
                        dlg.Icon = Icon;
                        dlg.ShowDialog(owner);
                    }
                });
            });
        }

        // Installierte Versionen stehen sofort fest (ohne Internet)
        void ShowInstalledVersions()
        {
            string web = UpdateCheck.LocalWebVersion();
            FfmpegTools ff = new FfmpegTools(App.Root, settings.GetString("ffmpeg.folder", ""));
            verInstalled[0].Text = web ?? "unbekannt";
            verInstalled[1].Text = App.VersionText;
            verInstalled[2].Text = ff.Installed ? (ff.InstalledVersionName() ?? "unbekannt") : "nicht installiert";
        }

        void ShowAvailableVersions(UpdateInfo u)
        {
            ShowInstalledVersions();
            SetAvailable(0, u.WebChanged ? (u.WebRemote ?? "neu") + (u.WebRemote != null && u.WebRemote == u.WebLocal ? " (neu)" : "") : u.WebLocal ?? "?", u.WebChanged);
            SetAvailable(1, u.SelfAvailable ?? "?", u.SelfChanged);
            SetAvailable(2, u.FfmpegRemote ?? "?", u.FfmpegChanged);
            verStatus.Text = (u.Any ? "Update verfügbar" : "Alles aktuell") + "  ·  geprüft " + DateTime.Now.ToString("HH:mm");
        }

        void SetAvailable(int row, string text, bool newer)
        {
            Label l = verAvailable[row];
            l.Text = text;
            l.ForeColor = newer ? Color.FromArgb(0xff, 0x4d, 0x57) : Theme.Muted;
            l.Font = newer ? new Font("Segoe UI Semibold", 9.5f) : Font;
        }

        // Beschriftung des Start-Knopfs passend zur sichtbaren Karte
        void UpdateStartText()
        {
            if (startButton == null || tabs == null) return;
            string text = "Start";
            if (tabs.Selected == PageImport) text = "Importieren";
            else if (tabs.Selected == PageTools)
            {
                int n = 0;
                foreach (string id in ToolIds) if (checks[id].Checked) n++;
                text = n == 0 ? "Tools starten" : n == 1 ? "1 Tool starten" : n + " Tools starten";
            }
            else
            {
                int n = 0;
                foreach (string id in new string[] { "update", "data" }) if (checks[id].Checked) n++;
                text = n == 2 ? "Aktualisieren und einlesen" : checks["update"].Checked ? "Aktualisieren" : checks["data"].Checked ? "Daten einlesen" : "Start";
            }
            startButton.Text = text;
        }

        // Nach einer Änderung der Inhalte: Fensterhöhe an den Inhalt anpassen
        void Relayout()
        {
            FitHeight();
        }

        // ---------------------------------------------------------------
        // Medien importieren
        // ---------------------------------------------------------------

        Control[] BuildImportPage()
        {
            // Liste der gewählten Dateien und Ordner, Knöpfe rechts daneben
            importList = new ListBox();
            importList.BackColor = Theme.Input;
            importList.ForeColor = Theme.Text;
            importList.BorderStyle = BorderStyle.None;
            importList.IntegralHeight = false;
            importList.Height = Theme.S(112);
            importList.HorizontalScrollbar = false;
            importList.SelectionMode = SelectionMode.MultiExtended;
            importList.Dock = DockStyle.Fill;
            importList.Margin = new Padding(0, 0, Theme.S(8), 0);
            importList.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemoveSelectedSources(); };

            FlowLayoutPanel sourceButtons = new FlowLayoutPanel();
            sourceButtons.AutoSize = true;
            sourceButtons.FlowDirection = FlowDirection.TopDown;
            sourceButtons.WrapContents = false;
            sourceButtons.Margin = new Padding(0);
            Button addFiles = SmallButton("Dateien …");
            Button addFolder = SmallButton("Ordner …");
            Button remove = SmallButton("Entfernen");
            foreach (Button b in new Button[] { addFiles, addFolder, remove })
            {
                b.AutoSize = false;
                b.Size = new Size(Theme.S(120), Theme.S(30));
                b.Margin = new Padding(0, 0, 0, Theme.S(6));
                sourceButtons.Controls.Add(b);
            }
            addFiles.Click += delegate { ChooseImportFiles(); };
            addFolder.Click += delegate { ChooseImportFolder(); };
            remove.Click += delegate { RemoveSelectedSources(); };

            TableLayoutPanel sourceRow = new TableLayoutPanel();
            sourceRow.AutoSize = true;
            sourceRow.ColumnCount = 2;
            sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            sourceRow.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(112)));
            sourceRow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sourceRow.Margin = new Padding(0, Theme.S(4), 0, Theme.S(2));
            sourceRow.Controls.Add(importList, 0, 0);
            sourceRow.Controls.Add(sourceButtons, 1, 0);

            Label sourceDesc = Theme.MakeLabel("Videos, Musik und Bilder auswählen oder per Drag & Drop ins Fenster ziehen.", true);
            sourceDesc.MaximumSize = new Size(TextWidth, 0);

            // Ziel: Kategorie und Unterordner
            TableLayoutPanel target = new TableLayoutPanel();
            target.AutoSize = true;
            target.ColumnCount = 3;
            target.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            target.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            target.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            target.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            target.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));

            importCategory = new ComboBox();
            importCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            importCategory.FlatStyle = FlatStyle.Flat;
            importCategory.BackColor = Theme.Input;
            importCategory.ForeColor = Theme.Text;
            importCategory.Dock = DockStyle.Fill;
            importCategory.Margin = new Padding(0, Theme.S(2), Theme.S(6), Theme.S(2));
            Button newCategory = SmallButton("Neu …");
            newCategory.Click += delegate { AddCategory(); };

            importSub = Theme.MakeTextBox();
            importSub.Dock = DockStyle.Fill;
            importSub.Margin = new Padding(0, Theme.S(2), Theme.S(6), Theme.S(2));
            Button browseSub = SmallButton("Durchsuchen …");
            browseSub.Click += delegate { ChooseSubFolder(); };
            // gleich breit wie die Knöpfe neben der Liste
            newCategory.MinimumSize = new Size(Theme.S(120), 0);
            browseSub.MinimumSize = new Size(Theme.S(120), 0);
            newCategory.Margin = browseSub.Margin = new Padding(0, Theme.S(1), 0, Theme.S(1));

            Label catLabel = Theme.MakeLabel("Kategorie:", true);
            catLabel.Margin = new Padding(0, Theme.S(6), Theme.S(6), 0);
            Label subLabel = Theme.MakeLabel("Unterordner:", true);
            subLabel.Margin = new Padding(0, Theme.S(6), Theme.S(6), 0);
            target.Controls.Add(catLabel, 0, 0);
            target.Controls.Add(importCategory, 1, 0);
            target.Controls.Add(newCategory, 2, 0);
            target.Controls.Add(subLabel, 0, 1);
            target.Controls.Add(importSub, 1, 1);
            target.Controls.Add(browseSub, 2, 1);

            optImportStructure = SmallCheck("Ordnerstruktur übernehmen");
            optImportStructure.CheckedChanged += delegate { UpdateImportHint(); };
            optImportConvert = SmallCheck("In browserkompatibles Format konvertieren (Video MP4, Musik MP3, Bild JPG)");
            optImportMove = SmallCheck("Verschieben statt kopieren (konvertierte Originale nach „" + ConvertTask.OriginalsFolderName + "“)");
            optImportPreview = SmallCheck("Danach fehlende Vorschaubilder erstellen");

            importHint = Theme.MakeLabel("", false);
            importHint.ForeColor = Color.FromArgb(0xf0, 0xc4, 0x6c);
            importHint.MaximumSize = new Size(TextWidth, 0);
            importHint.Margin = new Padding(0, Theme.S(4), 0, 0);
            importHint.Visible = false;

            Label dataNote = Theme.MakeLabel("Anschliessend wird der Zielordner automatisch eingelesen, damit die neuen Medien gleich in STEFFLIX erscheinen. Der Rest von data.js bleibt unverändert.", true);
            dataNote.MaximumSize = new Size(TextWidth, 0);
            dataNote.Margin = new Padding(0, Theme.S(4), 0, 0);

            return new Control[] {
                SectionCard("Was importieren?", new Control[] { sourceDesc, sourceRow, importHint }),
                SectionCard("Ziel in STEFFLIX", new Control[] { target }),
                SectionCard("Optionen", new Control[] { optImportStructure, optImportConvert, optImportMove, optImportPreview, dataNote }),
            };
        }

        // ---------------------------------------------------------------
        // Daten einlesen: nur bestimmte Ordner
        // ---------------------------------------------------------------

        Control BuildDataFoldersRow()
        {
            dataFolders = new ListBox();
            dataFolders.BackColor = Theme.Input;
            dataFolders.ForeColor = Theme.Text;
            dataFolders.BorderStyle = BorderStyle.None;
            dataFolders.IntegralHeight = false;
            dataFolders.SelectionMode = SelectionMode.MultiExtended;
            dataFolders.Dock = DockStyle.Fill;
            dataFolders.Margin = new Padding(0, 0, Theme.S(8), 0);
            dataFolders.KeyDown += delegate(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) RemoveDataFolders(); };

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0);
            Button add = SmallButton("Hinzufügen …");
            Button remove = SmallButton("Entfernen");
            foreach (Button b in new Button[] { add, remove })
            {
                b.AutoSize = false;
                b.Size = new Size(Theme.S(120), Theme.S(30));
                b.Margin = new Padding(0, 0, 0, Theme.S(6));
                buttons.Controls.Add(b);
            }
            add.Click += delegate { AddDataFolder(); };
            remove.Click += delegate { RemoveDataFolders(); };

            TableLayoutPanel row = new TableLayoutPanel();
            row.AutoSize = true;
            row.ColumnCount = 2;
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(76)));
            row.Controls.Add(dataFolders, 0, 0);
            row.Controls.Add(buttons, 1, 0);

            Label hint = Theme.MakeLabel("Nur diese Ordner werden neu eingelesen, der Rest von data.js bleibt. " +
                                         "Nach Änderungen an vielen Orten „Alle Ordner einlesen“ verwenden.", true);
            hint.MaximumSize = new Size(TextWidth, 0);
            hint.Margin = new Padding(0, Theme.S(4), 0, 0);

            TableLayoutPanel box = new TableLayoutPanel();
            box.AutoSize = true;
            box.ColumnCount = 1;
            box.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            box.Margin = new Padding(0, Theme.S(4), 0, 0);
            box.BackColor = Color.Transparent;
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Margin = new Padding(0);
            box.Controls.Add(row);
            box.Controls.Add(hint);
            return box;
        }

        void AddDataFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Ordner in STEFFLIX wählen, der neu eingelesen werden soll";
                dlg.ShowNewFolderButton = false;
                dlg.SelectedPath = App.Root;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string rel = DataTask.ToRelative(dlg.SelectedPath);
                if (rel == null || rel.Length == 0)
                {
                    MessageBox.Show(this, rel == null
                        ? "Bitte einen Ordner innerhalb des STEFFLIX-Ordners wählen."
                        : "Für den ganzen STEFFLIX-Ordner bitte „Alle Ordner einlesen“ einschalten.",
                        App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (Folders.IgnoreAny(App.Root, rel))
                {
                    MessageBox.Show(this, "Ablage-, System- und versteckte Ordner werden nicht eingelesen.", App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                foreach (string existing in dataFolders.Items)
                    if (string.Equals(existing, rel, StringComparison.OrdinalIgnoreCase)) return;
                dataFolders.Items.Add(rel);
            }
        }

        void RemoveDataFolders()
        {
            List<object> sel = new List<object>();
            foreach (object o in dataFolders.SelectedItems) sel.Add(o);
            foreach (object o in sel) dataFolders.Items.Remove(o);
        }

        List<string> DataFolderList()
        {
            List<string> list = new List<string>();
            foreach (string s in dataFolders.Items) list.Add(s);
            return list;
        }

        Button SmallButton(string text)
        {
            Button b = Theme.MakeButton(text, false);
            b.Padding = new Padding(Theme.S(8), 0, Theme.S(8), 0);
            b.Margin = new Padding(0, 0, Theme.S(6), 0);
            return b;
        }

        static readonly string MediaFilter =
            "Medien|*.mp4;*.m4v;*.webm;*.ogv;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.ts;*.m2ts;*.mts;*.mpeg;*.mpg;*.3gp;*.vob;*.divx;*.asf;*.f4v;" +
            "*.mp3;*.m4a;*.ogg;*.oga;*.opus;*.aac;*.flac;*.wav;*.wma;*.aiff;*.aif;*.ape;*.ac3;*.dts;*.amr;*.mka;*.wv;" +
            "*.jpg;*.jpeg;*.png;*.gif;*.webp;*.avif;*.bmp;*.tif;*.tiff;*.heic;*.heif;*.jfif|Alle Dateien|*.*";

        void ChooseImportFiles()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Multiselect = true;
                dlg.Filter = MediaFilter;
                dlg.Title = "Medien zum Importieren wählen";
                if (dlg.ShowDialog(this) == DialogResult.OK) AddImportSources(dlg.FileNames);
            }
        }

        void ChooseImportFolder()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Ordner zum Importieren wählen";
                dlg.ShowNewFolderButton = false;
                if (dlg.ShowDialog(this) == DialogResult.OK) AddImportSources(new string[] { dlg.SelectedPath });
            }
        }

        // Eintrag der Quellenliste: Name vorne, Ort dahinter (lange Pfade wären sonst abgeschnitten)
        class SourceItem
        {
            public string Path;
            public override string ToString()
            {
                string name = System.IO.Path.GetFileName(Path.TrimEnd('\\'));
                if (name.Length == 0) name = Path;
                string where = System.IO.Path.GetDirectoryName(Path.TrimEnd('\\')) ?? "";
                return (Directory.Exists(Path) ? "Ordner: " : "") + name + (where.Length > 0 ? "      in " + where : "");
            }
        }

        void AddImportSources(IEnumerable<string> paths)
        {
            foreach (string p in paths)
            {
                if (!File.Exists(p) && !Directory.Exists(p)) continue;
                bool known = false;
                foreach (SourceItem o in importList.Items) if (string.Equals(o.Path, p, StringComparison.OrdinalIgnoreCase)) known = true;
                if (!known)
                {
                    SourceItem item = new SourceItem();
                    item.Path = p;
                    importList.Items.Add(item);
                }
            }
            // Hineingezogene Dateien landen im Importer, auch wenn gerade eine andere Karte offen ist
            if (importList.Items.Count > 0) ShowPage(PageImport);
            UpdateImportHint();
        }

        void RemoveSelectedSources()
        {
            List<object> sel = new List<object>();
            foreach (object o in importList.SelectedItems) sel.Add(o);
            foreach (object o in sel) importList.Items.Remove(o);
            UpdateImportHint();
        }

        List<string> ImportSources()
        {
            List<string> list = new List<string>();
            foreach (SourceItem o in importList.Items) list.Add(o.Path);
            return list;
        }

        // Zeigt, wie viele Dateien importiert werden; nicht kompatible erzwingen "Konvertieren"
        void UpdateImportHint()
        {
            if (importList == null) return;
            List<string> sources = ImportSources();
            if (sources.Count == 0)
            {
                importHint.Visible = false;
                // Nur erzwungenes Häkchen zurücknehmen, die eigene Wahl bleibt
                if (!optImportConvert.Enabled && importConvertWasOff) optImportConvert.Checked = false;
                importConvertWasOff = false;
                optImportConvert.Enabled = true;
                Relayout();
                return;
            }
            int ignored;
            List<ImportItem> items = ImportTask.Plan(sources, optImportStructure.Checked, out ignored);
            List<string> incompatible = new List<string>();
            foreach (ImportItem it in items)
                if (MediaTypes.NeedsConversion(Path.GetExtension(it.Source))) incompatible.Add(Path.GetFileName(it.Source));

            StringBuilder sb = new StringBuilder();
            sb.Append(items.Count + " Mediendatei(en) ausgewählt");
            if (ignored > 0) sb.Append(", " + ignored + " andere Datei(en) werden nicht importiert");
            sb.Append(".");
            if (incompatible.Count > 0)
            {
                // Pflicht: diese Dateien kann STEFFLIX nur nach einer Konvertierung abspielen
                if (!optImportConvert.Checked) importConvertWasOff = true;
                optImportConvert.Checked = true;
                optImportConvert.Enabled = false;
                sb.Append(" " + incompatible.Count + " davon " + (incompatible.Count == 1 ? "ist" : "sind") +
                          " nicht browserkompatibel und " + (incompatible.Count == 1 ? "wird" : "werden") + " konvertiert (z.B. " +
                          string.Join(", ", incompatible.GetRange(0, Math.Min(3, incompatible.Count))) +
                          (incompatible.Count > 3 ? ", …" : "") + ").");
            }
            else
            {
                if (!optImportConvert.Enabled && importConvertWasOff) optImportConvert.Checked = false;
                importConvertWasOff = false;
                optImportConvert.Enabled = true;
            }
            importHint.Text = sb.ToString();
            importHint.Visible = true;
            Relayout();
        }

        bool importConvertWasOff;

        void LoadCategories(string selected)
        {
            importCategory.Items.Clear();
            try
            {
                foreach (string d in Directory.GetDirectories(App.Root))
                {
                    string name = Path.GetFileName(d);
                    if (Folders.Ignore(d) || string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                    importCategory.Items.Add(name);
                }
            }
            catch { }
            if (!string.IsNullOrEmpty(selected) && !importCategory.Items.Contains(selected)) importCategory.Items.Add(selected);
            if (!string.IsNullOrEmpty(selected)) importCategory.SelectedItem = selected;
            else if (importCategory.Items.Count > 0) importCategory.SelectedIndex = 0;
        }

        void AddCategory()
        {
            using (InputDialog dlg = new InputDialog("Neue Kategorie", "Name der neuen Kategorie (z.B. Dokumentationen). Der Ordner wird beim Import angelegt."))
            {
                string name = dlg.Ask(this);
                if (string.IsNullOrEmpty(name)) return;
                name = DataTask.SanitizeName(name);
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Folders.Skip(name) ||
                    string.Equals(name, "Preview", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "Dieser Name ist als Kategorie nicht möglich.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (!importCategory.Items.Contains(name)) importCategory.Items.Add(name);
                importCategory.SelectedItem = name;
            }
        }

        void ChooseSubFolder()
        {
            string category = importCategory.SelectedItem as string;
            if (string.IsNullOrEmpty(category)) return;
            string catDir = Path.Combine(App.Root, category);
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Unterordner in „" + category + "“ wählen (oder neu anlegen)";
                dlg.ShowNewFolderButton = true;
                string current = Path.Combine(catDir, importSub.Text.Trim().Trim('\\'));
                dlg.SelectedPath = Directory.Exists(current) ? current : (Directory.Exists(catDir) ? catDir : App.Root);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                string chosen = dlg.SelectedPath.TrimEnd('\\');
                string prefix = catDir.TrimEnd('\\');
                if (string.Equals(chosen, prefix, StringComparison.OrdinalIgnoreCase)) { importSub.Text = ""; return; }
                if (!chosen.StartsWith(prefix + "\\", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "Bitte einen Ordner innerhalb der Kategorie „" + category + "“ wählen.", App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                importSub.Text = chosen.Substring(prefix.Length + 1);
            }
        }

        // Dateien und Ordner per Drag & Drop irgendwo ins Fenster ziehen
        void EnableDrop(Control c)
        {
            c.AllowDrop = true;
            c.DragEnter += delegate(object s, DragEventArgs e)
            {
                e.Effect = !running && e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            };
            c.DragDrop += delegate(object s, DragEventArgs e)
            {
                if (running) return;
                string[] paths = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (paths != null) AddImportSources(paths);
            };
            foreach (Control child in c.Controls) EnableDrop(child);
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
            c.Switch = true;
            c.Margin = new Padding(0, Theme.S(2), 0, Theme.S(2));
            c.FitText();
            return c;
        }

        Control FolderRow(TextBox box, string label, bool withBrowse = true)
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
            if (withBrowse) row.Controls.Add(browse, 2, 0);
            else box.Margin = new Padding(0, Theme.S(2), 0, 0);
            return row;
        }

        // ---------------------------------------------------------------
        // Einstellungen
        // ---------------------------------------------------------------

        void LoadOptions()
        {
            loading = true;
            // Import hat kein Häkchen: er läuft, wenn man im Importer auf Start klickt
            foreach (string id in checks.Keys)
            {
                bool def = id == "update" || id == "data";
                checks[id].Checked = autoRun != null ? autoRun.Contains(id) : settings.GetBool("task." + id, def);
            }
            optImportPreview.Checked = settings.GetBool("import.preview", true);
            optFfmpeg.Checked = settings.GetBool("update.ffmpeg", true);
            installFolderBox.Text = App.Root;
            UpdateUndoInfo();
            optMoveParts.Checked = settings.GetBool("merge.moveParts", true);
            convertParallel.Value = Math.Max(0, Math.Min(16, settings.GetInt("convert.parallel", 0)));
            optDeleteOriginal.Checked = settings.GetBool("convert.deleteOriginal", false);
            optPreviewVideos.Checked = settings.GetBool("preview.videos", true);
            optPreviewImages.Checked = settings.GetBool("preview.images", false);
            optImportStructure.Checked = settings.GetBool("import.structure", true);
            optImportConvert.Checked = settings.GetBool("import.convert", false);
            optImportMove.Checked = settings.GetBool("import.move", false);
            importSub.Text = settings.GetString("import.sub", "");
            LoadCategories(settings.GetString("import.category", ""));
            dataFolders.Items.Clear();
            foreach (string f in DataTask.SplitFolders(settings.GetString("data.folders", ""))) dataFolders.Items.Add(f);
            optDataAll.Checked = settings.GetBool("data.all", true);
            dataFoldersRow.Visible = !optDataAll.Checked;
            loading = false;
        }

        bool loading;

        void SaveOptions(bool saveTaskSelection)
        {
            if (saveTaskSelection)
                foreach (string id in checks.Keys) settings.SetBool("task." + id, checks[id].Checked);
            settings.SetBool("update.ffmpeg", optFfmpeg.Checked);
            // Der frühere Medienordner ist durch die Ordnerwahl je Tool ersetzt
            foreach (string old in new string[] { "media.folder", "convert.folder", "merge.folder", "preview.folder" }) settings.SetString(old, "");
            settings.SetInt("log.width", logWidth);
            settings.SetBool("merge.moveParts", optMoveParts.Checked);
            settings.SetInt("convert.parallel", (int)convertParallel.Value);
            settings.SetBool("convert.deleteOriginal", optDeleteOriginal.Checked);
            settings.SetBool("preview.videos", optPreviewVideos.Checked);
            settings.SetBool("preview.images", optPreviewImages.Checked);
            settings.SetBool("import.structure", optImportStructure.Checked);
            // Nur die eigene Wahl merken, nicht das erzwungene Häkchen bei nicht kompatiblen Dateien
            if (optImportConvert.Enabled) settings.SetBool("import.convert", optImportConvert.Checked);
            settings.SetBool("import.move", optImportMove.Checked);
            settings.SetString("import.sub", importSub.Text.Trim());
            settings.SetString("import.category", importCategory.SelectedItem as string ?? "");
            settings.SetBool("import.preview", optImportPreview.Checked);
            settings.SetBool("data.all", optDataAll.Checked);
            settings.SetString("data.folders", string.Join(DataTask.FolderSeparator.ToString(), DataFolderList().ToArray()));
            settings.SetBool("task.import", false);
            settings.Save();
        }

        // ---------------------------------------------------------------
        // Start
        // ---------------------------------------------------------------

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AppendLog(App.Title + " " + App.VersionText + "  –  " + App.Root);
            if (startNotice != null) AppendLog(startNotice);
            if (autoRun != null && autoRun.Count > 0)
            {
                StartTasks(autoRun);
                CheckForUpdateInBackground(false);   // nur die Versionsübersicht füllen
            }
            else CheckForUpdateInBackground(true);
        }

        // Beim Start im Hintergrund prüfen, ob STEFFLIX, der Updater oder ffmpeg neuer sind.
        // Falls ja: Fenster mit den Versionen, "Jetzt aktualisieren" startet das Update sofort.
        // showDialog: beim Start das Hinweisfenster zeigen; sonst nur Übersicht und Balken aktualisieren
        void CheckForUpdateInBackground(bool showDialog)
        {
            string ffmpegFolder = settings.GetString("ffmpeg.folder", "");
            verStatus.Text = "Wird geprüft …";
            verRecheck.Enabled = false;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    UpdateInfo info = UpdateCheck.Run(new FfmpegTools(App.Root, ffmpegFolder), CancellationToken.None);
                    BeginInvoke((Action)delegate
                    {
                        verRecheck.Enabled = true;
                        ShowAvailableVersions(info);
                        if (!info.Any)
                        {
                            pendingUpdate = null;
                            if (updateBar.Visible) { updateBar.Visible = false; Relayout(); }
                            return;
                        }
                        pendingUpdate = info;
                        updateLabel.Text = "Update verfügbar: " + info.Summary() + ".";
                        updateBar.Visible = true;
                        updateButton.Enabled = !running;
                        Relayout();   // Fensterhöhe an den Hinweis anpassen
                        if (!showDialog || running) return;
                        using (UpdateDialog dlg = new UpdateDialog(info, ShowPatchNotes))
                            if (dlg.Ask(this)) RunUpdateNow();
                    });
                }
                catch (Exception ex)
                {
                    pendingLog.Enqueue("Hinweis: Update-Prüfung nicht möglich (" + ex.Message + ")");
                    BeginInvoke((Action)delegate
                    {
                        verRecheck.Enabled = true;
                        verStatus.Text = "Prüfung nicht möglich (keine Verbindung?)";
                        for (int i = 0; i < 3; i++) SetAvailable(i, "?", false);
                    });
                }
            });
        }

        UpdateInfo pendingUpdate;
        bool forceFfmpeg;

        // Update sofort ausführen (aus dem Hinweisfenster oder dem Balken)
        void RunUpdateNow()
        {
            if (running) return;
            ShowPage(PageUpdates);
            forceFfmpeg = pendingUpdate != null && pendingUpdate.FfmpegChanged;
            StartTasks(new List<string> { "update" });
        }

        void StartTasks(List<string> preset)
        {
            if (running) return;
            // Ausgeführt wird, was auf der sichtbaren Karte gewählt ist (nach einem Selbst-Update
            // die Aufgaben, die noch übrig waren)
            int page = tabs.Selected;
            List<string> selected = new List<string>();
            foreach (string id in TaskIds)
            {
                bool run;
                if (preset != null) run = preset.Contains(id) && id != "import";
                else if (page == PageUpdates) run = (id == "update" || id == "data") && checks[id].Checked;
                else if (page == PageImport) run = id == "import" || id == "data" || (id == "preview" && optImportPreview.Checked);
                else run = Array.IndexOf(ToolIds, id) >= 0 && checks[id].Checked;
                if (run) selected.Add(id);
            }
            // Nach den Tools die Medien automatisch neu einlesen
            if (preset == null && page == PageTools && selected.Count > 0) selected.Add("data");
            if (selected.Count == 0)
            {
                MessageBox.Show(this, page == PageTools ? "Bitte mindestens ein Tool auswählen." : "Bitte mindestens eine Aufgabe auswählen.",
                                App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            bool previewVideos = optPreviewVideos.Checked, previewImages = optPreviewImages.Checked;
            // Nach einem Import: Einstellung der Vorschaubilder aus den Tools, mindestens für Videos
            if (selected.Contains("import") && !previewVideos && !previewImages) previewVideos = true;
            // Ordner der Tools (Standard "Alles" = Installationsordner)
            Dictionary<string, List<string>> toolFolders = new Dictionary<string, List<string>>();
            foreach (string id in new string[] { "preview", "merge", "convert", "cleanup" })
            {
                List<string> list = PickOf(id).Folders();
                if (selected.Contains(id) && preset == null && page == PageTools)
                {
                    if (list.Count == 0)
                    {
                        ShowOption(id);
                        MessageBox.Show(this, "Bitte bei „" + PickOf(id).Tool + "“ mindestens einen Ordner hinzufügen oder „Alles“ einschalten.",
                                        App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    foreach (string f in list)
                    {
                        string reason = null;
                        bool missing = !Directory.Exists(f);
                        if (missing || (id == "cleanup" && !Folders.IsInsideRoot(f, out reason)))
                        {
                            ShowOption(id);
                            MessageBox.Show(this, missing ? "Ordner nicht gefunden:\n" + f
                                : "„Ablagen leeren“ ist nur im Installationsordner möglich. Dieser Ordner " + reason + ":\n" + f,
                                App.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }
                if (list.Count == 0) list.Add(App.Root);   // automatischer Start: ganzer Installationsordner
                toolFolders[id] = list;
            }
            string renameDir = null;
            if (selected.Contains("rename"))
            {
                renameDir = renameFolder.Text.Trim();
                string message = null;
                if (renameDir.Length == 0) message = "Bitte bei „Folgen umbenennen“ den Ordner der Serie wählen.";
                else if (!Directory.Exists(renameDir)) message = "Der Serienordner wurde nicht gefunden:\n" + renameDir;
                else if (DataTask.ToRelative(renameDir) == "") message = "Bitte den Ordner einer einzelnen Serie wählen, nicht den ganzen Installationsordner.";
                if (message != null)
                {
                    ShowOption("rename");
                    MessageBox.Show(this, message, App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                renameDir = Path.GetFullPath(renameDir).TrimEnd('\\');
            }
            if (selected.Contains("preview") && !previewVideos && !previewImages)
            {
                ShowOption("preview");
                MessageBox.Show(this, "Bitte bei „Vorschaubilder“ „für Videos“ und/oder „für Bilder“ einschalten.",
                                App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ImportOptions import = null;
            if (selected.Contains("import"))
            {
                if (importList.Items.Count == 0)
                {
                    MessageBox.Show(this, "Bitte zuerst Dateien oder Ordner zum Importieren wählen (oder ins Fenster ziehen).",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string category = importCategory.SelectedItem as string;
                if (string.IsNullOrEmpty(category))
                {
                    MessageBox.Show(this, "Bitte eine Kategorie wählen oder mit „Neu …“ anlegen.",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string sub = importSub.Text.Trim().Trim('\\');
                if (sub.Contains("..") || Path.IsPathRooted(sub) || sub.IndexOfAny(new char[] { '/', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                {
                    MessageBox.Show(this, "Der Unterordner ist ungültig. Bitte einen Ordner innerhalb der Kategorie angeben, z.B. Meine Serie\\Staffel 1.",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                import = new ImportOptions();
                import.Sources = ImportSources();
                import.Category = category;
                import.SubFolder = sub;
                import.KeepStructure = optImportStructure.Checked;
                import.Convert = optImportConvert.Checked;
                import.Move = optImportMove.Checked;
            }

            // Welche Ordner "Daten einlesen" durchsucht (null = alle)
            List<string> dataScope = null;
            if (import != null)
            {
                // Importer: nur der Zielordner
                dataScope = new List<string> { Path.Combine(App.Root, import.Category, import.SubFolder) };
            }
            else if (preset == null && page == PageTools)
            {
                // Tools: nur die Ordner, in denen sich etwas geändert haben kann und die im
                // Installationsordner liegen. "Alles" liest alles ein. Ablagen leeren ändert data.js nicht.
                List<string> touched = new List<string>();
                foreach (string id in new string[] { "preview", "merge", "convert" })
                    if (selected.Contains(id)) touched.AddRange(toolFolders[id]);
                if (renameDir != null) touched.Add(renameDir);
                bool all = false;
                List<string> inside = new List<string>();
                foreach (string f in touched)
                {
                    string rel = DataTask.ToRelative(f);
                    if (rel == null) continue;
                    if (rel.Length == 0) all = true;
                    else inside.Add(f);
                }
                if (!all)
                {
                    if (inside.Count > 0) dataScope = Folders.WithoutNested(inside);
                    else selected.Remove("data");   // nichts in STEFFLIX betroffen
                }
            }
            else if (selected.Contains("renameundo"))
            {
                // Rückgängig: nur den Serienordner neu einlesen
                string series;
                EpisodeTask.UndoInfo(out series);
                string rel = series != null ? DataTask.ToRelative(series) : null;
                if (rel == null) selected.Remove("data");
                else if (rel.Length > 0) dataScope = new List<string> { series };
            }
            else if (selected.Contains("data") && !optDataAll.Checked)
            {
                dataScope = DataFolderList();
                // Automatischer Start (z.B. nach einem Selbst-Update) ohne Ordner: alles einlesen
                if (dataScope.Count == 0 && preset != null) dataScope = null;
                else if (dataScope.Count == 0)
                {
                    ShowOption("data");
                    MessageBox.Show(this, "Bitte bei „Daten einlesen“ mindestens einen Ordner hinzufügen oder „Alle Ordner einlesen“ einschalten.",
                                    App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            // Beim automatischen Start nach einem Update die gespeicherte Auswahl nicht überschreiben
            SaveOptions(preset == null);

            ConvertOptions conv = new ConvertOptions();
            conv.Folder = App.Root;
            conv.Parallel = (int)convertParallel.Value;
            conv.DeleteOriginal = optDeleteOriginal.Checked;
            conv.Preset = settings.GetString("convert.preset", "medium");
            conv.QualitySD = settings.GetInt("convert.qualitySD", 21);
            conv.QualityHD = settings.GetInt("convert.qualityHD", 19);
            conv.QualityFullHD = settings.GetInt("convert.qualityFullHD", 18);
            conv.Quality4K = settings.GetInt("convert.quality4K", 17);

            TaskRunner runner = new TaskRunner();
            runner.Tasks = selected;
            runner.UpdateFfmpeg = optFfmpeg.Checked || forceFfmpeg;
            forceFfmpeg = false;
            lastRunUpdated = selected.Contains("update");
            runner.MergeFolders = toolFolders["merge"];
            runner.MoveParts = optMoveParts.Checked;
            runner.Convert = conv;
            runner.ConvertFolders = toolFolders["convert"];
            // Nach einem Import nur im Zielordner nach fehlenden Vorschaubildern suchen
            runner.PreviewFolders = import != null ? new List<string> { dataScope[0] } : toolFolders["preview"];
            runner.PreviewVideos = previewVideos;
            runner.PreviewImages = previewImages;
            runner.CleanupFolders = toolFolders["cleanup"];
            runner.RenameFolder = renameDir;
            runner.DataFolders = dataScope;
            if (import != null) import.Video = conv;
            runner.Import = import;
            lastRunImported = import != null;

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
                    using (CleanupDialog dlg = new CleanupDialog(folders, App.Root)) return dlg.Choose(this);
                });
            };
            ctx.ChooseEpisodeList = delegate(string term)
            {
                return (EpisodeList)Invoke((Func<EpisodeList>)delegate
                {
                    using (EpisodeSearchDialog dlg = new EpisodeSearchDialog(term)) return dlg.Choose(this);
                });
            };
            ctx.ChooseCachedEpisodeList = delegate(EpisodeList cached, string failure)
            {
                return (string)Invoke((Func<string>)delegate
                {
                    using (EpisodeCacheDialog dlg = new EpisodeCacheDialog(cached, failure)) return dlg.Choose(this);
                });
            };
            ctx.ConfirmEpisodePlan = delegate(EpisodePlan plan)
            {
                return (bool)Invoke((Func<bool>)delegate
                {
                    using (EpisodePlanDialog dlg = new EpisodePlanDialog(plan)) return dlg.Confirm(this);
                });
            };
            ctx.AskConflict = delegate(string target)
            {
                return (ConflictAnswer)Invoke((Func<ConflictAnswer>)delegate
                {
                    using (ConflictDialog dlg = new ConflictDialog(target)) return dlg.Ask(this);
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

        bool lastRunImported, lastRunUpdated;

        void SetRunning(bool value)
        {
            running = value;
            startButton.Enabled = !value;
            cancelButton.Enabled = value;
            updateButton.Enabled = !value;
            foreach (TileButton c in checks.Values) c.Enabled = !value;
            renameUndoButton.Enabled = !value && EpisodeTask.UndoFileExists;
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
            UpdateUndoInfo();
            progress.Value = result.Success ? 1 : 0;
            // Erfolgreicher Import: Auswahl leeren, damit beim nächsten Start nicht nochmal importiert wird
            // Update erledigt: Versionen neu prüfen (blendet den Hinweis aus, wenn alles aktuell ist)
            if (lastRunUpdated && !result.Restart) CheckForUpdateInBackground(false);
            if (result.Success && lastRunImported)
            {
                importList.Items.Clear();
                UpdateImportHint();
            }
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
            List<string> lines = new List<string>();
            string line;
            while (pendingLog.TryDequeue(out line))
            {
                lines.Add(line);
                unsavedLog.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("  ").Append(line).Append("\r\n");
            }
            if (lines.Count > 0) logPanel.Add(lines);

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
        // Ordner je Tool; null = ganzer Installationsordner. Mehrere Ordner laufen nacheinander.
        public List<string> MergeFolders, ConvertFolders, PreviewFolders, CleanupFolders;
        public bool MoveParts = true;
        public ConvertOptions Convert;
        public bool PreviewVideos = true;
        public bool PreviewImages;
        public string RenameFolder;        // Folgen umbenennen: Serienordner
        public ImportOptions Import;
        public List<string> DataFolders;   // null = alles einlesen, sonst nur diese Ordner

        static string Title(string id)
        {
            switch (id)
            {
                case "update": return "Update";
                case "cleanup": return "Ablagen leeren";
                case "import": return "Medien importieren";
                case "rename": return "Folgen umbenennen";
                case "renameundo": return "Folgen umbenennen rückgängig";
                case "merge": return "Gesplittete Medien zusammenführen";
                case "convert": return "Videos für STEFFLIX konvertieren";
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
                ctx.Summary = null;
                DateTime taskStart = DateTime.Now;
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
                // z.B. "Daten einlesen    OK  –  1234 Medien  (12 s)"
                summary.Add(Title(id).PadRight(36) + outcome +
                            (string.IsNullOrEmpty(ctx.Summary) ? "" : "  –  " + ctx.Summary) +
                            "  (" + Fmt.Time((DateTime.Now - taskStart).TotalSeconds) + ")");

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
                case "merge": ForEachFolder(ctx, MergeFolders, delegate(string f) { MergeTask.Run(ctx, f, MoveParts); }); break;
                case "convert": ForEachFolder(ctx, ConvertFolders, delegate(string f) { Convert.Folder = f; ConvertTask.Run(ctx, Convert); }); break;
                case "preview":
                    // Nach einem Import ohne neue Dateien gibt es den Zielordner eventuell nicht
                    if (Import != null && PreviewFolders != null && PreviewFolders.Count > 0 && !Directory.Exists(PreviewFolders[0]))
                    {
                        ctx.Log("Zielordner nicht vorhanden, nichts zu tun.");
                        break;
                    }
                    ForEachFolder(ctx, PreviewFolders, delegate(string f) { PreviewTask.Run(ctx, f, PreviewVideos, PreviewImages); });
                    break;
                case "import":
                    if (Import == null) throw new TaskFailedException("Medien importieren ist nur über die Oberfläche möglich.");
                    ImportTask.Run(ctx, Import);
                    break;
                case "cleanup": CleanupTask.Run(ctx, CleanupFolders ?? new List<string> { App.Root }); break;
                case "rename": EpisodeTask.Run(ctx, RenameFolder); break;
                case "renameundo": EpisodeTask.Undo(ctx); break;
                case "data": DataTask.Run(ctx, DataFolders); break;
                default: throw new TaskFailedException("Unbekannte Aufgabe: " + id);
            }
        }

        // Führt ein Tool für jeden gewählten Ordner aus. Ein Fehler in einem Ordner hält die
        // anderen nicht auf; am Ende werden alle Fehler gemeldet.
        static void ForEachFolder(TaskContext ctx, List<string> folders, Action<string> run)
        {
            List<string> list = Folders.WithoutNested(folders != null && folders.Count > 0 ? folders : new List<string> { App.Root });
            List<string> errors = new List<string>();
            List<string> results = new List<string>();
            foreach (string f in list)
            {
                ctx.Check();
                if (list.Count > 1) ctx.Log("Ordner: " + f);
                ctx.Summary = null;
                try { run(f); }
                catch (TaskFailedException e) { errors.Add(list.Count > 1 ? Path.GetFileName(f.TrimEnd('\\')) + ": " + e.Message : e.Message); }
                if (!string.IsNullOrEmpty(ctx.Summary))
                    results.Add(list.Count > 1 ? Path.GetFileName(f.TrimEnd('\\')) + ": " + ctx.Summary : ctx.Summary);
            }
            ctx.Summary = results.Count > 0 ? string.Join("; ", results.ToArray()) : null;
            if (errors.Count > 0) throw new TaskFailedException(string.Join("; ", errors.ToArray()));
        }
    }
}
