// Reiter "Download" (seit 4.0.5): Liste der Downloads mit Knöpfen, Dialog zum Hinzufügen und zum
// Ändern der Adresse, Meldungen eines Downloads. Der Ablauf steht in DownloadTask.cs.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    public class DownloadPage : TableLayoutPanel
    {
        public readonly DownloadManager Manager;
        public Action<string> Log;            // Protokoll (beliebiger Thread)
        public Action<string> FolderReady;    // Zielordner fertig: einlesen (UI-Thread)

        readonly Settings settings;
        readonly ListView list;
        readonly Button addButton, pauseButton, resumeButton, editButton, removeButton, messagesButton, cleanupButton;
        readonly ComboBox parallelBox;
        readonly Label summary;
        readonly System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer();
        volatile bool dirty = true;

        static int TextWidth { get { return Theme.S(800); } }

        public DownloadPage(Settings settings)
        {
            this.settings = settings;
            Dock = DockStyle.Top;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Margin = new Padding(0);
            BackColor = Theme.Back;

            Manager = new DownloadManager(settings);
            Manager.Changed = delegate { dirty = true; };
            Manager.Log = delegate(string line) { if (Log != null) Log("Download: " + line); };
            Manager.FolderReady = delegate(string folder)
            {
                if (IsHandleCreated) BeginInvoke((Action)delegate { if (FolderReady != null) FolderReady(folder); });
            };

            Label intro = Theme.MakeLabel("Videos aus einer m3u8-Adresse (HLS) laden, z.B. aus den Entwicklertools des Browsers. Die Teilstücke werden einzeln " +
                                          "geladen: Downloads lassen sich pausieren und nach einem Neustart fortsetzen. Ist die Adresse abgelaufen, mit „URL bearbeiten“ " +
                                          "eine neue einfügen, der Download läuft an derselben Stelle weiter. Danach wird geprüft, bei Bedarf konvertiert, ein Vorschaubild " +
                                          "erstellt und der Zielordner eingelesen. Nur Streams ohne Kopierschutz.", true);
            intro.MaximumSize = new Size(TextWidth, 0);
            intro.Margin = new Padding(0, 0, 0, Theme.S(10));
            Controls.Add(intro);

            // Knöpfe
            FlowLayoutPanel bar = new FlowLayoutPanel();
            bar.AutoSize = true;
            bar.WrapContents = true;
            bar.Dock = DockStyle.Fill;
            bar.Margin = new Padding(0, 0, 0, Theme.S(8));
            addButton = Theme.MakeButton("Hinzufügen …", true);
            pauseButton = SmallButton("Pausieren");
            resumeButton = SmallButton("Fortsetzen");
            editButton = SmallButton("URL bearbeiten …");
            removeButton = SmallButton("Entfernen");
            messagesButton = SmallButton("Meldungen …");
            cleanupButton = SmallButton("Aufräumen");
            addButton.Margin = new Padding(0, 0, Theme.S(10), 0);
            foreach (Control c in new Control[] { addButton, pauseButton, resumeButton, editButton, removeButton, messagesButton, cleanupButton }) bar.Controls.Add(c);
            Label parallelLabel = Theme.MakeLabel("Gleichzeitig:", true);
            parallelLabel.Margin = new Padding(Theme.S(16), Theme.S(7), Theme.S(6), 0);
            parallelBox = new ComboBox();
            parallelBox.DropDownStyle = ComboBoxStyle.DropDownList;
            parallelBox.FlatStyle = FlatStyle.Flat;
            parallelBox.BackColor = Theme.Input;
            parallelBox.ForeColor = Theme.Text;
            parallelBox.Width = Theme.S(56);
            parallelBox.Margin = new Padding(0, Theme.S(3), 0, 0);
            for (int i = 1; i <= 4; i++) parallelBox.Items.Add(i.ToString());
            parallelBox.SelectedIndex = Manager.MaxParallel - 1;
            parallelBox.SelectedIndexChanged += delegate
            {
                Manager.MaxParallel = parallelBox.SelectedIndex + 1;
                settings.SetInt("download.parallel", Manager.MaxParallel);
                settings.Save();
                Manager.Schedule();
            };
            bar.Controls.Add(parallelLabel);
            bar.Controls.Add(parallelBox);
            Controls.Add(bar);

            // Liste
            list = new ListView();
            list.View = View.Details;
            list.FullRowSelect = true;
            list.HideSelection = false;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.Height = Theme.S(320);
            list.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            list.Margin = new Padding(0);
            list.Columns.Add("Datei", Theme.S(170));
            list.Columns.Add("Zielordner", Theme.S(210));
            list.Columns.Add("Qualität", Theme.S(130));
            list.Columns.Add("Fortschritt", Theme.S(170));
            list.Columns.Add("Status", Theme.S(170));
            list.Columns.Add("Ergebnis", Theme.S(80));
            DarkList.Apply(list, false);
            list.SelectedIndexChanged += delegate { UpdateButtons(); };
            list.DoubleClick += delegate { ShowMessages(); };
            list.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) { RemoveSelected(); e.Handled = true; }
                else if (e.KeyCode == Keys.Enter) { ShowMessages(); e.Handled = true; }
            };
            Controls.Add(list);

            summary = Theme.MakeLabel("", true);
            summary.Margin = new Padding(0, Theme.S(6), 0, 0);
            summary.MaximumSize = new Size(TextWidth, 0);
            Controls.Add(summary);

            addButton.Click += delegate { AddDownloads(); };
            pauseButton.Click += delegate { foreach (DownloadJob j in Selected()) Manager.Pause(j); };
            resumeButton.Click += delegate { foreach (DownloadJob j in Selected()) Manager.Resume(j); };
            editButton.Click += delegate { EditUrl(); };
            removeButton.Click += delegate { RemoveSelected(); };
            messagesButton.Click += delegate { ShowMessages(); };
            cleanupButton.Click += delegate { Cleanup(); };

            if (App.IsReleaseFolder)
            {
                addButton.Enabled = false;
                summary.Text = ReleaseFolder.ShortNotice;
            }

            refreshTimer.Interval = 500;
            refreshTimer.Tick += delegate { if (dirty) RefreshList(); };
            refreshTimer.Start();

            // Verwaiste Zwischendateien (z.B. nach einem Absturz)
            ThreadPool.QueueUserWorkItem(delegate
            {
                long freed = DownloadStore.RemoveOrphans(new List<DownloadJob>(Manager.Jobs));
                if (freed > 0 && Log != null) Log("Download: verwaiste Zwischendateien gelöscht (" + Fmt.Size(freed) + ")");
            });
            RefreshList();
        }

        static Button SmallButton(string text)
        {
            Button b = Theme.MakeButton(text, false);
            b.Margin = new Padding(0, 0, Theme.S(6), 0);
            b.Padding = new Padding(Theme.S(10), Theme.S(3), Theme.S(10), Theme.S(3));
            return b;
        }

        List<DownloadJob> Selected()
        {
            List<DownloadJob> jobs = new List<DownloadJob>();
            foreach (ListViewItem it in list.SelectedItems)
            {
                DownloadJob j = it.Tag as DownloadJob;
                if (j != null) jobs.Add(j);
            }
            return jobs;
        }

        void UpdateButtons()
        {
            List<DownloadJob> sel = Selected();
            bool canPause = false, canResume = false, canEdit = sel.Count == 1, hasMessages = sel.Count == 1;
            foreach (DownloadJob j in sel)
            {
                if (j.State == DownloadState.Waiting || j.State == DownloadState.Running) canPause = true;
                if (j.State == DownloadState.Paused || j.State == DownloadState.Failed) canResume = true;
                if (j.State == DownloadState.Running || j.State == DownloadState.Processing || j.State == DownloadState.Done) canEdit = false;
            }
            bool locked = App.IsReleaseFolder;
            pauseButton.Enabled = canPause && !locked;
            resumeButton.Enabled = canResume && !locked;
            resumeButton.Text = sel.Count > 0 && sel.TrueForAll(delegate(DownloadJob j) { return j.State == DownloadState.Failed; }) ? "Wiederholen" : "Fortsetzen";
            editButton.Enabled = canEdit && !locked;
            removeButton.Enabled = sel.Count > 0 && !locked;
            messagesButton.Enabled = hasMessages;
        }

        static string StateText(DownloadJob j)
        {
            switch (j.State)
            {
                case DownloadState.Waiting: return "Wartet";
                case DownloadState.Paused: return j.Status.Length > 0 ? j.Status : "Pausiert";
                case DownloadState.Failed: return "Fehler (Doppelklick)";
                default: return j.Status;
            }
        }

        static string ProgressText(DownloadJob j)
        {
            if (j.State == DownloadState.Done) return "100 %" + (j.Bytes > 0 ? " · " + Fmt.Size(j.Bytes) : "");
            if (j.SegmentsTotal <= 0) return "–";
            int percent = (int)Math.Floor(100.0 * j.SegmentsDone / j.SegmentsTotal);
            return percent + " % · " + j.SegmentsDone + " / " + j.SegmentsTotal + (j.Bytes > 0 ? " · " + Fmt.Size(j.Bytes) : "");
        }

        void RefreshList()
        {
            dirty = false;
            List<DownloadJob> jobs = new List<DownloadJob>(Manager.Jobs);
            HashSet<string> selected = new HashSet<string>();
            foreach (DownloadJob j in Selected()) selected.Add(j.Id);
            list.BeginUpdate();
            try
            {
                // Zeilen nachführen statt neu aufbauen (Auswahl und Scrollposition bleiben)
                while (list.Items.Count > jobs.Count) list.Items.RemoveAt(list.Items.Count - 1);
                for (int i = 0; i < jobs.Count; i++)
                {
                    DownloadJob j = jobs[i];
                    string[] cells = {
                        j.FileName, j.Folder, j.VariantLabel.Length > 0 ? j.VariantLabel : "Beste", ProgressText(j), StateText(j), j.Result
                    };
                    ListViewItem it;
                    if (i < list.Items.Count) it = list.Items[i];
                    else
                    {
                        it = new ListViewItem(cells[0]);
                        for (int c = 1; c < cells.Length; c++) it.SubItems.Add("");
                        list.Items.Add(it);
                    }
                    it.Tag = j;
                    for (int c = 0; c < cells.Length; c++) if (it.SubItems[c].Text != cells[c]) it.SubItems[c].Text = cells[c];
                    Color color = j.Result == "Fehler" ? Color.FromArgb(0xff, 0x8a, 0x80)
                                : j.Result == "Warnung" ? Color.FromArgb(0xf0, 0xc0, 0x60)
                                : j.Result == "OK" ? Color.FromArgb(0x8c, 0xd9, 0x8c) : Theme.Text;
                    if (it.ForeColor != color) it.ForeColor = color;
                    bool sel = selected.Contains(j.Id);
                    if (it.Selected != sel) it.Selected = sel;
                }
            }
            finally { list.EndUpdate(); }

            if (!App.IsReleaseFolder)
            {
                int running = 0, waiting = 0, paused = 0, failed = 0, done = 0;
                foreach (DownloadJob j in jobs)
                {
                    if (j.State == DownloadState.Running || j.State == DownloadState.Processing) running++;
                    else if (j.State == DownloadState.Waiting) waiting++;
                    else if (j.State == DownloadState.Paused) paused++;
                    else if (j.State == DownloadState.Failed) failed++;
                    else done++;
                }
                summary.Text = jobs.Count == 0 ? "Noch keine Downloads. Mit „Hinzufügen …“ eine oder mehrere m3u8-Adressen einfügen."
                    : running + " laufen, " + waiting + " warten, " + paused + " pausiert, " + failed + " mit Fehler, " + done + " fertig. "
                      + "Doppelklick zeigt die Meldungen eines Downloads.";
            }
            UpdateButtons();
        }

        // ---------------------------------------------------------------

        void AddDownloads()
        {
            if (App.IsReleaseFolder) return;
            List<string> reserved = new List<string>();
            foreach (DownloadJob j in Manager.Jobs) if (j.IsActive || j.State == DownloadState.Paused) reserved.Add(j.FileName);
            using (DownloadAddDialog dlg = new DownloadAddDialog(settings, reserved))
            {
                List<DownloadJob> jobs = dlg.Ask(FindForm());
                if (jobs == null) return;
                foreach (DownloadJob j in jobs) Manager.Add(j);
                if (Log != null) Log("Download: " + jobs.Count + " hinzugefügt (Ziel: " + jobs[0].Folder + ")");
            }
        }

        void EditUrl()
        {
            List<DownloadJob> sel = Selected();
            if (sel.Count != 1) return;
            DownloadJob job = sel[0];
            using (DownloadUrlDialog dlg = new DownloadUrlDialog(job))
            {
                if (!dlg.Ask(FindForm())) return;
                Manager.ChangeUrl(job, dlg.Url, dlg.Referer, dlg.UserAgent);
                if (job.State == DownloadState.Failed || job.State == DownloadState.Paused)
                {
                    if (MessageBox.Show(FindForm(), "Adresse geändert. Download jetzt fortsetzen?", App.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        Manager.Resume(job);
                }
            }
        }

        void RemoveSelected()
        {
            List<DownloadJob> sel = Selected();
            if (sel.Count == 0 || App.IsReleaseFolder) return;
            bool unfinished = sel.Exists(delegate(DownloadJob j) { return j.State != DownloadState.Done; });
            string text = sel.Count == 1 ? "„" + sel[0].FileName + "“ aus der Liste entfernen?" : sel.Count + " Downloads aus der Liste entfernen?";
            if (unfinished) text += "\n\nNicht fertige Downloads werden abgebrochen, ihre geladenen Teilstücke gelöscht. Fertige Dateien im Zielordner bleiben.";
            if (MessageBox.Show(FindForm(), text, App.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (DownloadJob j in sel) Manager.Remove(j);
        }

        void ShowMessages()
        {
            List<DownloadJob> sel = Selected();
            if (sel.Count != 1) return;
            using (DownloadMessagesDialog dlg = new DownloadMessagesDialog(sel[0])) dlg.ShowDialog(FindForm());
        }

        void Cleanup()
        {
            long size = Manager.CleanupSize();
            int count = 0;
            foreach (DownloadJob j in Manager.Jobs) if (j.State == DownloadState.Failed || j.State == DownloadState.Done) count++;
            string text = "Entfernt " + count + " fertige oder fehlgeschlagene Downloads aus der Liste und löscht ihre Zwischendateien"
                        + (size > 0 ? " (" + Fmt.Size(size) + ")" : "") + " sowie verwaiste Zwischendateien.\n\n"
                        + "Pausierte Downloads bleiben, fertige Dateien im Zielordner ebenfalls. Fortfahren?";
            if (MessageBox.Show(FindForm(), text, App.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            long freed = Manager.Cleanup();
            if (Log != null) Log("Download: aufgeräumt" + (freed > 0 ? ", " + Fmt.Size(freed) + " freigegeben" : ""));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) refreshTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    // ------------------------------------------------------------------
    // Hinzufügen
    // ------------------------------------------------------------------

    public class DownloadAddDialog : Form
    {
        readonly TextBox urls, fileName, referer, userAgent;
        readonly ComboBox folder, quality;
        readonly Button qualityButton;
        readonly DarkCheckBox fullCheck, advanced;
        readonly Control advancedBox;
        readonly Label qualityInfo;
        readonly List<string> reserved;
        bool nameEdited, suggesting;
        List<HlsVariant> variants = new List<HlsVariant>();

        public DownloadAddDialog(Settings settings, List<string> reserved)
        {
            this.reserved = reserved;
            Text = "Downloads hinzufügen";
            ConflictDialog.Setup(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), Theme.S(12));
            int width = Theme.S(560);

            root.Controls.Add(Caption("m3u8-Adressen (eine je Zeile):"));
            urls = Theme.MakeTextBox();
            urls.Multiline = true;
            urls.ScrollBars = ScrollBars.Vertical;
            urls.WordWrap = false;
            urls.Width = width;
            urls.Height = Theme.S(92);
            urls.Margin = new Padding(0, Theme.S(4), 0, 0);
            urls.TextChanged += delegate { UpdateQualityState(); SuggestName(); };
            root.Controls.Add(urls);

            root.Controls.Add(Caption("Zielordner im Installationsordner (neue Ordner werden angelegt):"));
            FlowLayoutPanel folderRow = Row();
            folder = new ComboBox();
            folder.DropDownStyle = ComboBoxStyle.DropDown;
            folder.FlatStyle = FlatStyle.Flat;
            folder.BackColor = Theme.Input;
            folder.ForeColor = Theme.Text;
            folder.Width = width - Theme.S(130);
            folder.MaxDropDownItems = 20;
            foreach (string f in StefflixFolders()) folder.Items.Add(f);
            string last = settings.GetString("download.folder", "");
            folder.Text = last;
            folder.TextChanged += delegate { SuggestName(); };
            Button newFolder = Theme.MakeButton("Neuer Ordner …", false);
            newFolder.Margin = new Padding(Theme.S(8), 0, 0, 0);
            newFolder.Click += delegate
            {
                using (InputDialog dlg = new InputDialog("Neuer Ordner", "Name des neuen Ordners in „" + (folder.Text.Trim().Length > 0 ? folder.Text.Trim() : "Installationsordner") + "“ (z.B. Staffel 02):"))
                {
                    string name = dlg.Ask(this);
                    if (string.IsNullOrEmpty(name)) return;
                    string baseFolder = folder.Text.Trim().TrimEnd('\\');
                    folder.Text = baseFolder.Length > 0 ? baseFolder + "\\" + name : name;
                }
            };
            folderRow.Controls.Add(folder);
            folderRow.Controls.Add(newFolder);
            root.Controls.Add(folderRow);

            root.Controls.Add(Caption("Dateiname (bei mehreren Adressen fortlaufend nummeriert):"));
            fileName = Theme.MakeTextBox();
            fileName.Width = width;
            fileName.Margin = new Padding(0, Theme.S(4), 0, 0);
            fileName.TextChanged += delegate { if (!suggesting) nameEdited = fileName.Text.Trim().Length > 0; };
            root.Controls.Add(fileName);

            root.Controls.Add(Caption("Qualität:"));
            FlowLayoutPanel qualityRow = Row();
            quality = new ComboBox();
            quality.DropDownStyle = ComboBoxStyle.DropDownList;
            quality.FlatStyle = FlatStyle.Flat;
            quality.BackColor = Theme.Input;
            quality.ForeColor = Theme.Text;
            quality.Width = Theme.S(260);
            quality.Items.Add("Beste");
            quality.SelectedIndex = 0;
            qualityButton = Theme.MakeButton("Qualitäten abrufen", false);
            qualityButton.Margin = new Padding(Theme.S(8), 0, 0, 0);
            qualityButton.Click += delegate { LoadQualities(); };
            qualityInfo = Theme.MakeLabel("", true);
            qualityInfo.Margin = new Padding(Theme.S(8), Theme.S(6), 0, 0);
            qualityRow.Controls.Add(quality);
            qualityRow.Controls.Add(qualityButton);
            qualityRow.Controls.Add(qualityInfo);
            root.Controls.Add(qualityRow);

            fullCheck = new DarkCheckBox();
            fullCheck.Switch = true;
            fullCheck.Text = "Ganze Datei prüfen (dauert länger; sonst kurze Stichproben an Anfang, Mitte und Ende)";
            fullCheck.Margin = new Padding(0, Theme.S(12), 0, 0);
            root.Controls.Add(fullCheck);

            advanced = new DarkCheckBox();
            advanced.Switch = true;
            advanced.Text = "Erweitert: Referer und User-Agent (falls der Server sonst nichts liefert)";
            advanced.Margin = new Padding(0, Theme.S(6), 0, 0);
            root.Controls.Add(advanced);
            TableLayoutPanel adv = new TableLayoutPanel();
            adv.AutoSize = true;
            adv.ColumnCount = 1;
            adv.Margin = new Padding(Theme.S(20), 0, 0, 0);
            adv.Controls.Add(Caption("Referer (Seite, auf der das Video läuft):"));
            referer = Theme.MakeTextBox();
            referer.Width = width - Theme.S(20);
            adv.Controls.Add(referer);
            adv.Controls.Add(Caption("User-Agent (leer = wie ein üblicher Browser):"));
            userAgent = Theme.MakeTextBox();
            userAgent.Width = width - Theme.S(20);
            adv.Controls.Add(userAgent);
            adv.Visible = false;
            advancedBox = adv;
            advanced.CheckedChanged += delegate { advancedBox.Visible = advanced.Checked; };
            root.Controls.Add(adv);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(16), 0, 0);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button ok = Theme.MakeButton("Hinzufügen", true);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            ok.Click += delegate { if (BuildJobs(true) != null) DialogResult = DialogResult.OK; };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            root.Controls.Add(buttons);

            Controls.Add(root);
            CancelButton = cancel;
            UpdateQualityState();
            this.settings = settings;
        }

        readonly Settings settings;

        static Label Caption(string text)
        {
            Label l = Theme.MakeLabel(text, false);
            l.Margin = new Padding(0, Theme.S(10), 0, 0);
            return l;
        }

        static FlowLayoutPanel Row()
        {
            FlowLayoutPanel row = new FlowLayoutPanel();
            row.AutoSize = true;
            row.WrapContents = false;
            row.Margin = new Padding(0, Theme.S(4), 0, 0);
            return row;
        }

        // Ordner im Installationsordner (bis drei Ebenen, ohne Preview-, Ablage-, System- und versteckte Ordner)
        static List<string> StefflixFolders()
        {
            List<string> result = new List<string>();
            try
            {
                foreach (string category in Directory.GetDirectories(App.Root))
                {
                    string name = Path.GetFileName(category);
                    if (Folders.Ignore(category) || name.Equals("sflix_sys", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("SFLIX Tools", StringComparison.OrdinalIgnoreCase)) continue;
                    Collect(category, name, 1, result);
                }
            }
            catch { }
            result.Sort(ListSort.Natural);
            return result;
        }

        static void Collect(string dir, string rel, int depth, List<string> result)
        {
            result.Add(rel);
            if (depth >= 3 || result.Count > 2000) return;
            try
            {
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(sub);
                    if (Folders.Ignore(sub) || name.Equals("Preview", StringComparison.OrdinalIgnoreCase)) continue;
                    Collect(sub, rel + "\\" + name, depth + 1, result);
                }
            }
            catch { }
        }

        List<string> Urls()
        {
            List<string> list = new List<string>();
            foreach (string raw in urls.Lines)
            {
                string u = raw.Trim();
                if (u.Length > 0) list.Add(u);
            }
            return list;
        }

        void UpdateQualityState()
        {
            // Qualität wählen geht nur bei einer Adresse; bei mehreren gilt "Beste"
            bool single = Urls().Count == 1;
            qualityButton.Enabled = single;
            quality.Enabled = single && quality.Items.Count > 1;
            if (!single && quality.Items.Count > 1)
            {
                quality.Items.Clear();
                quality.Items.Add("Beste");
                quality.SelectedIndex = 0;
                variants.Clear();
                qualityInfo.Text = "";
            }
        }

        void SuggestName()
        {
            if (nameEdited) return;
            string rel = folder.Text.Trim().Trim('\\');
            string suggestion = rel.Length > 0 ? DownloadManager.SuggestName(Path.Combine(App.Root, rel), reserved) : null;
            suggesting = true;
            fileName.Text = suggestion ?? "";
            suggesting = false;
        }

        void LoadQualities()
        {
            List<string> list = Urls();
            if (list.Count != 1 || !Hls.IsHttpUrl(list[0]))
            {
                qualityInfo.Text = "Bitte eine gültige Adresse eingeben.";
                return;
            }
            string url = list[0];
            DownloadJob probe = new DownloadJob();
            probe.Referer = advanced.Checked ? referer.Text.Trim() : "";
            probe.UserAgent = advanced.Checked ? userAgent.Text.Trim() : "";
            qualityButton.Enabled = false;
            qualityInfo.Text = "Wird abgerufen …";
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<HlsVariant> found = null;
                string message;
                try
                {
                    string text = Hls.Fetch(url, probe, CancellationToken.None);
                    if (Hls.IsMaster(text))
                    {
                        Dictionary<string, string> audio;
                        found = Hls.Variants(text, url, out audio);
                        message = found.Count + " Qualitäten";
                    }
                    else message = "Nur eine Qualität";
                }
                catch (Exception e) { message = "Fehler: " + e.Message; }
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke((Action)delegate
                {
                    qualityButton.Enabled = Urls().Count == 1;
                    qualityInfo.Text = message;
                    quality.Items.Clear();
                    quality.Items.Add("Beste");
                    variants = found ?? new List<HlsVariant>();
                    foreach (HlsVariant v in variants) quality.Items.Add(v.Label);
                    quality.SelectedIndex = 0;
                    quality.Enabled = variants.Count > 0;
                });
            });
        }

        // Prüft die Eingaben; liefert die Downloads oder null (mit Meldung, wenn show)
        List<DownloadJob> BuildJobs(bool show)
        {
            string problem = null;
            List<string> list = Urls();
            string rel = folder.Text.Trim().Trim('\\');
            string name = DownloadManager.CleanFileName(fileName.Text);
            string reason;
            if (list.Count == 0) problem = "Bitte mindestens eine m3u8-Adresse eingeben.";
            else if (!list.TrueForAll(Hls.IsHttpUrl)) problem = "Jede Zeile muss eine Adresse mit http:// oder https:// sein.";
            else if (rel.Length == 0) problem = "Bitte einen Zielordner wählen oder eingeben, z.B. Serien\\Meine Serie\\Staffel 01.";
            else if (rel.Contains("..") || Path.IsPathRooted(rel) || rel.IndexOfAny(new char[] { '/', ':', '*', '?', '"', '<', '>', '|' }) >= 0)
                problem = "Der Zielordner ist ungültig. Bitte einen Ordner innerhalb des Installationsordners angeben, z.B. Serien\\Meine Serie\\Staffel 01.";
            else if (!Folders.IsInsideRoot(Path.Combine(App.Root, rel), out reason)) problem = "Der Zielordner liegt nicht im Installationsordner.";
            else if (name == null) problem = "Bitte einen Dateinamen eingeben.";
            if (problem != null)
            {
                if (show) MessageBox.Show(this, problem, App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }
            List<DownloadJob> jobs = new List<DownloadJob>();
            HlsVariant chosen = quality.SelectedIndex > 0 && quality.SelectedIndex - 1 < variants.Count ? variants[quality.SelectedIndex - 1] : null;
            for (int i = 0; i < list.Count; i++)
            {
                DownloadJob j = new DownloadJob();
                j.Id = Guid.NewGuid().ToString("N");
                j.Url = list[i];
                j.Folder = rel;
                j.FileName = DownloadManager.NumberedName(name, i);
                j.FullCheck = fullCheck.Checked;
                if (advanced.Checked)
                {
                    j.Referer = referer.Text.Trim();
                    j.UserAgent = userAgent.Text.Trim();
                }
                if (chosen != null)
                {
                    j.Variant = chosen.Url;
                    j.VariantLabel = chosen.Label;
                }
                j.Status = "Wartet";
                jobs.Add(j);
            }
            return jobs;
        }

        public List<DownloadJob> Ask(Form owner)
        {
            Icon = owner != null ? owner.Icon : null;
            if (ShowDialog(owner) != DialogResult.OK) return null;
            List<DownloadJob> jobs = BuildJobs(false);
            if (jobs != null && jobs.Count > 0)
            {
                settings.SetString("download.folder", jobs[0].Folder);
                settings.Save();
            }
            return jobs;
        }
    }

    // ------------------------------------------------------------------
    // Adresse ändern
    // ------------------------------------------------------------------

    public class DownloadUrlDialog : Form
    {
        readonly TextBox url, referer, userAgent;

        public string Url { get { return url.Text.Trim(); } }
        public string Referer { get { return referer.Text.Trim(); } }
        public string UserAgent { get { return userAgent.Text.Trim(); } }

        public DownloadUrlDialog(DownloadJob job)
        {
            Text = "URL bearbeiten";
            ConflictDialog.Setup(this);
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), Theme.S(12));
            int width = Theme.S(560);
            Label hint = Theme.MakeLabel("Neue m3u8-Adresse für „" + job.FileName + "“. Bereits geladene Teilstücke bleiben, wenn die Playlist gleich viele hat.", true);
            hint.MaximumSize = new Size(width, 0);
            root.Controls.Add(hint);
            root.Controls.Add(Field("m3u8-Adresse:", job.Url, width, out url));
            root.Controls.Add(Field("Referer:", job.Referer, width, out referer));
            root.Controls.Add(Field("User-Agent (leer = wie ein üblicher Browser):", job.UserAgent, width, out userAgent));

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(16), 0, 0);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button ok = Theme.MakeButton("Übernehmen", true);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            ok.Click += delegate
            {
                if (!Hls.IsHttpUrl(Url))
                {
                    MessageBox.Show(this, "Bitte eine Adresse mit http:// oder https:// eingeben.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        static Control Field(string caption, string value, int width, out TextBox box)
        {
            TableLayoutPanel p = new TableLayoutPanel();
            p.AutoSize = true;
            p.ColumnCount = 1;
            p.Margin = new Padding(0, Theme.S(10), 0, 0);
            p.Controls.Add(Theme.MakeLabel(caption, false));
            box = Theme.MakeTextBox();
            box.Width = width;
            box.Text = value ?? "";
            box.Margin = new Padding(0, Theme.S(4), 0, 0);
            p.Controls.Add(box);
            return p;
        }

        public bool Ask(Form owner)
        {
            Icon = owner != null ? owner.Icon : null;
            return ShowDialog(owner) == DialogResult.OK;
        }
    }

    // ------------------------------------------------------------------
    // Meldungen eines Downloads (ffmpeg, Prüfung)
    // ------------------------------------------------------------------

    public class DownloadMessagesDialog : Form
    {
        public DownloadMessagesDialog(DownloadJob job)
        {
            Text = "Meldungen – " + job.FileName;
            ConflictDialog.Setup(this);
            FormBorderStyle = FormBorderStyle.Sizable;
            AutoSize = false;
            Size = new Size(Theme.S(760), Theme.S(440));
            TextBox box = Theme.MakeTextBox();
            box.Multiline = true;
            box.ReadOnly = true;
            box.ScrollBars = ScrollBars.Both;
            box.WordWrap = false;
            box.Dock = DockStyle.Fill;
            box.BackColor = Theme.LogBack;
            box.Font = new Font("Consolas", 9.5f);
            List<string> lines = new List<string>();
            lines.Add("Datei: " + job.FileName);
            lines.Add("Zielordner: " + job.Folder);
            if (job.ResultPath.Length > 0) lines.Add("Gespeichert: " + job.ResultPath);
            lines.Add("Qualität: " + (job.VariantLabel.Length > 0 ? job.VariantLabel : "Beste"));
            lines.Add("Status: " + job.Status + (job.Result.Length > 0 ? " (" + job.Result + ")" : ""));
            lines.Add("Adresse: " + job.Url);
            lines.Add("");
            lock (job.Messages)
            {
                if (job.Messages.Count == 0) lines.Add("Keine Meldungen.");
                else lines.AddRange(job.Messages);
            }
            box.Lines = lines.ToArray();
            box.Select(0, 0);
            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(Theme.S(12));
            host.Controls.Add(box);
            Controls.Add(host);
        }
    }
}
