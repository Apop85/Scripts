// Dialoge für "Folgen umbenennen": Serie suchen und wählen, Vorschau der Änderungen.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    // Serie suchen: Treffer der gewählten Quelle; die Quelle lässt sich in beide Richtungen wechseln
    // (seit Desktop 4.0.12, vorher nur "nächste Quelle"). Ohne Treffer sucht "Suchen" automatisch in
    // der nächsten Quelle weiter. Netzwerkzugriffe laufen im Hintergrund, das Fenster bleibt bedienbar.
    public class EpisodeSearchDialog : Form
    {
        readonly TextBox term;
        readonly Button searchButton, loadButton;
        readonly ComboBox sourceBox;
        readonly Label status;
        readonly ListView list;
        int source;
        bool settingSource;
        int busy;   // laufende Anfrage (Nummer), ältere Antworten werden verworfen
        CancellationTokenSource cts = new CancellationTokenSource();
        public EpisodeList Result;

        public EpisodeSearchDialog(string searchTerm) : this(searchTerm, null) { }

        // startSource: Name der Quelle, mit der die Suche beginnt (null = erste Quelle)
        public EpisodeSearchDialog(string searchTerm, string startSource)
        {
            for (int i = 0; i < EpisodeSource.All.Length; i++)
                if (startSource != null && string.Equals(EpisodeSource.All[i].Name, startSource, StringComparison.OrdinalIgnoreCase)) source = i;
            Text = "Folgen umbenennen – Serie suchen";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(760), Theme.S(470));
            MinimumSize = new Size(Theme.S(560), Theme.S(360));
            MinimizeBox = false;
            ShowInTaskbar = false;

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Top;
            top.AutoSize = true;
            top.ColumnCount = 3;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.Padding = new Padding(Theme.S(12), Theme.S(12), Theme.S(12), Theme.S(4));
            Label l = Theme.MakeLabel("Serie:", false);
            l.Margin = new Padding(0, Theme.S(6), Theme.S(8), 0);
            term = Theme.MakeTextBox();
            term.Text = searchTerm;
            term.Dock = DockStyle.Fill;
            term.Margin = new Padding(0, Theme.S(3), Theme.S(8), 0);
            searchButton = Theme.MakeButton("Suchen", false);
            searchButton.Margin = new Padding(0);
            searchButton.Click += delegate { Search(true); };
            top.Controls.Add(l, 0, 0);
            top.Controls.Add(term, 1, 0);
            top.Controls.Add(searchButton, 2, 0);
            Label sl = Theme.MakeLabel("Quelle:", false);
            sl.Margin = new Padding(0, Theme.S(10), Theme.S(8), 0);
            sourceBox = SourceBox();
            sourceBox.Margin = new Padding(0, Theme.S(7), 0, 0);
            sourceBox.SelectedIndex = source;
            // Andere Quelle gewählt: dort suchen (ohne automatisch weiterzuspringen)
            sourceBox.SelectedIndexChanged += delegate
            {
                if (settingSource || sourceBox.SelectedIndex < 0) return;
                source = sourceBox.SelectedIndex;
                Search(false);
            };
            top.Controls.Add(sl, 0, 1);
            top.Controls.Add(sourceBox, 1, 1);

            list = new ListView();
            list.View = View.Details;
            list.FullRowSelect = true;
            list.MultiSelect = false;
            list.HideSelection = false;
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            DarkList.Apply(list);
            list.Columns.Add("Serie", Theme.S(330));
            list.Columns.Add("Jahr", Theme.S(60));
            list.Columns.Add("Info", Theme.S(320));
            list.SelectedIndexChanged += delegate { UpdateButtons(); };
            list.DoubleClick += delegate { if (loadButton.Enabled) LoadEpisodes(); };
            Panel listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(Theme.S(12), Theme.S(6), Theme.S(12), 0);
            listHost.Controls.Add(list);

            status = Theme.MakeLabel("", true);
            status.AutoSize = false;
            status.Dock = DockStyle.Bottom;
            status.Height = Theme.S(28);
            status.Padding = new Padding(Theme.S(12), Theme.S(6), Theme.S(12), 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(6), Theme.S(12), Theme.S(12));
            loadButton = Theme.MakeButton("Folgen laden", true);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            loadButton.Click += delegate { LoadEpisodes(); };
            cancel.Click += delegate { Result = null; DialogResult = DialogResult.Cancel; };
            buttons.Controls.Add(loadButton);
            buttons.Controls.Add(cancel);

            Controls.Add(listHost);
            Controls.Add(top);
            Controls.Add(status);
            Controls.Add(buttons);
            AcceptButton = searchButton;
            CancelButton = cancel;
            term.Enter += delegate { AcceptButton = searchButton; };
            list.Enter += delegate { AcceptButton = loadButton; };
            Shown += delegate { list.Columns[2].Width = -2; Search(true); };
            FormClosed += delegate { cts.Cancel(); };
        }

        EpisodeSource Current { get { return EpisodeSource.All[source]; } }

        // Auswahlliste aller Quellen ("Name (Sprache)"), Index wie EpisodeSource.All
        public static ComboBox SourceBox()
        {
            ComboBox box = new DarkCombo();
            box.DropDownStyle = ComboBoxStyle.DropDownList;
            box.FlatStyle = FlatStyle.Flat;
            box.BackColor = Theme.Input;
            box.ForeColor = Theme.Text;
            int width = 0;
            foreach (EpisodeSource s in EpisodeSource.All)
            {
                string text = s.Name + " (" + s.Language + ")";
                box.Items.Add(text);
                width = Math.Max(width, TextRenderer.MeasureText(text, box.Font).Width);
            }
            box.Width = width + Theme.S(36);
            return box;
        }

        void ShowSource()
        {
            settingSource = true;
            try { sourceBox.SelectedIndex = source; }
            finally { settingSource = false; }
        }

        void SetBusy(bool value, string text)
        {
            status.Text = text;
            searchButton.Enabled = !value;
            term.Enabled = !value;
            sourceBox.Enabled = !value;
            UseWaitCursor = value;
            UpdateButtons(value);
        }

        void UpdateButtons() { UpdateButtons(UseWaitCursor); }

        void UpdateButtons(bool isBusy)
        {
            loadButton.Enabled = !isBusy && list.SelectedItems.Count == 1;
        }

        // autoAdvance: ohne Treffer automatisch in der nächsten Quelle weitersuchen
        void Search(bool autoAdvance)
        {
            string name = term.Text.Trim();
            if (name.Length == 0) { status.Text = "Bitte einen Namen eingeben."; return; }
            int request = ++busy;
            EpisodeSource src = Current;
            ShowSource();
            list.Items.Clear();
            SetBusy(true, "Suche auf " + src.Name + " …");
            CancellationToken token = cts.Token;
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<SeriesHit> hits = null;
                string error = null;
                try { hits = src.Search(name, token); }
                catch (Exception e) { error = e.Message; }
                if (token.IsCancellationRequested || IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (request != busy || IsDisposed) return;
                        if (error != null) { SetBusy(false, src.Name + ": Fehler bei der Suche (" + error + ")."); return; }
                        if (hits.Count == 0 && autoAdvance && source < EpisodeSource.All.Length - 1)
                        {
                            // Keine Treffer: automatisch in der nächsten Quelle weitersuchen
                            source++;
                            Search(true);
                            return;
                        }
                        foreach (SeriesHit h in hits.GetRange(0, Math.Min(hits.Count, 25)))
                        {
                            ListViewItem item = new ListViewItem(h.Title);
                            item.SubItems.Add(h.Year ?? "");
                            item.SubItems.Add(h.Info ?? "");
                            item.Tag = h;
                            list.Items.Add(item);
                        }
                        // Gewählte Sortierung auch für neue Treffer
                        ListSort.Of(list).Apply();
                        if (list.Items.Count > 0) { list.Items[0].Selected = true; list.Focus(); }
                        SetBusy(false, hits.Count == 0
                            ? src.Name + ": keine Treffer. Tipp: andere Quelle wählen, Originaltitel oder einen kürzeren Namen versuchen."
                            : src.Name + ": " + hits.Count + " Treffer. Richtige Serie wählen und „Folgen laden“ klicken, sonst oben eine andere Quelle wählen.");
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        void LoadEpisodes()
        {
            if (list.SelectedItems.Count != 1) return;
            SeriesHit hit = (SeriesHit)list.SelectedItems[0].Tag;
            EpisodeSource src = Current;
            int request = ++busy;
            SetBusy(true, "Lade Folgen von „" + hit.Title + "“ …");
            CancellationToken token = cts.Token;
            ThreadPool.QueueUserWorkItem(delegate
            {
                EpisodeList result = null;
                string error = null;
                try { result = src.Load(hit, token); }
                catch (Exception e) { error = e.Message; }
                if (token.IsCancellationRequested || IsDisposed) return;
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        if (request != busy || IsDisposed) return;
                        if (error != null) { SetBusy(false, src.Name + ": Folgen konnten nicht geladen werden (" + error + ")."); return; }
                        if (result.Count == 0) { SetBusy(false, src.Name + ": Für diese Serie wurden keine Folgen gefunden."); return; }
                        Result = result;
                        DialogResult = DialogResult.OK;
                    });
                }
                catch (InvalidOperationException) { }
            });
        }

        public EpisodeList Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowDialog(owner);
            return DialogResult == DialogResult.OK ? Result : null;
        }
    }

    // Gespeicherte Folgenliste gefunden: verwenden, aktualisieren (gleiche Quelle), neu suchen oder abbrechen.
    // session = true: Liste wurde in dieser Sitzung schon von dieser Quelle geladen (Quellenwechsel in der
    // Vorschau); sie wird nie automatisch verwendet.
    public class EpisodeCacheDialog : Form
    {
        string choice;


        public EpisodeCacheDialog(EpisodeList cached, string failure, bool session)
        {
            Text = session ? "Gemerkte Folgenliste" : "Gespeicherte Folgenliste";
            ConflictDialog.Setup(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), Theme.S(12));

            Label title = Theme.MakeLabel(session
                ? "Von " + cached.Source + " ist in dieser Sitzung schon eine Folgenliste geladen: „" + cached.SeriesTitle + "“."
                : "Für „" + cached.SeriesTitle + "“ ist eine Folgenliste gespeichert.", false);
            title.Font = new Font("Segoe UI Semibold", 10.5f);
            title.MaximumSize = new Size(Theme.S(560), 0);
            Label info = Theme.MakeLabel(cached.Source + ", geladen am " + cached.Loaded.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture) +
                ", " + cached.Seasons.Count + (cached.Seasons.Count == 1 ? " Staffel, " : " Staffeln, ") + cached.Count + " Folgen", true);
            info.MaximumSize = new Size(Theme.S(560), 0);
            info.Margin = new Padding(0, Theme.S(4), 0, Theme.S(10));
            Label question = Theme.MakeLabel(failure != null
                ? (session ? "Neu laden ist fehlgeschlagen (" + failure + "). Die gemerkte Liste kann weiterhin verwendet werden."
                           : "Aktualisieren ist fehlgeschlagen (" + failure + "). Die gespeicherte Liste bleibt erhalten und kann verwendet werden.")
                : session ? "Gemerkte Liste verwenden, von " + cached.Source + " neu laden oder dort neu suchen?"
                : "Gespeicherte Liste verwenden, von " + cached.Source + " aktualisieren oder neu suchen (zuerst auf " + EpisodeSource.All[0].Name + ")?", false);
            question.MaximumSize = new Size(Theme.S(560), 0);
            if (failure != null) question.ForeColor = Color.FromArgb(0xfa, 0xcc, 0x15);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(14), 0, 0);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button search = Theme.MakeButton("Neu suchen", false);
            Button reload = Theme.MakeButton(session ? (failure == null ? "Neu laden" : "Erneut laden")
                                                     : (failure == null ? "Liste aktualisieren" : "Erneut aktualisieren"), false);
            Button use = Theme.MakeButton(session ? "Gemerkte verwenden" : "Gespeicherte verwenden", true);
            cancel.Click += delegate { choice = null; DialogResult = DialogResult.Cancel; };
            search.Click += delegate { choice = "search"; DialogResult = DialogResult.OK; };
            reload.Click += delegate { choice = "reload"; DialogResult = DialogResult.OK; };
            use.Click += delegate { choice = "use"; DialogResult = DialogResult.OK; };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(search);
            buttons.Controls.Add(reload);
            buttons.Controls.Add(use);

            root.Controls.Add(title);
            root.Controls.Add(info);
            root.Controls.Add(question);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = use;
            CancelButton = cancel;
        }

        public string Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowIcon = true;
            ShowDialog(owner);
            return DialogResult == DialogResult.OK ? choice : null;
        }
    }

    // Vorschau: alt -> neu, je Zeile ein Haken. Bei mehreren Dateien für dieselbe Folge darf nur
    // eine gewählt sein. Fehlende Folgen und nicht zugeordnete Dateien stehen darunter.
    public class EpisodePlanDialog : Form
    {
        readonly EpisodePlan plan;
        readonly ListView list;
        readonly TextBox details;
        readonly CompareView compare;
        readonly Label compareTitle;
        bool adjusting;
        bool confirmed;
        readonly ComboBox sourceBox;


        public EpisodePlanDialog(EpisodePlan plan, FfmpegTools ff)
        {
            this.plan = plan;
            Text = "Folgen umbenennen – Vorschau";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1040), Theme.S(620));
            MinimumSize = new Size(Theme.S(700), Theme.S(420));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(
                plan.SeriesName + ": Folgenliste „" + plan.List.SeriesTitle + "“ von " + plan.List.Source + " (" + plan.List.Seasons.Count +
                " Staffeln, " + plan.List.Count + " Folgen). " + plan.Ops.Count + " Änderungen, " + plan.Unchanged +
                " Dateien heissen bereits richtig. Nur angehakte Zeilen werden ausgeführt, danach lässt sich alles rückgängig machen.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width, 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));
            Resize += delegate { intro.MaximumSize = new Size(ClientSize.Width, 0); };

            list = new ListView();
            list.View = View.Details;
            list.CheckBoxes = true;
            list.FullRowSelect = true;
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.ShowItemToolTips = true;
            DarkList.Apply(list);
            list.Columns.Add("Aktion", Theme.S(120));
            list.Columns.Add("Bisher", Theme.S(340));
            list.Columns.Add("Neu", Theme.S(340));
            list.Columns.Add("Grösse", Theme.S(80));
            list.Columns.Add("Hinweis", Theme.S(200));
            foreach (EpisodeOp op in plan.Ops)
            {
                string action = op.Kind == "folder" ? "Ordner" : op.Kind == "merge" ? "Zusammenführen" : op.Kind == "move" ? "Verschieben" : "Umbenennen";
                ListViewItem item = new ListViewItem(action);
                item.SubItems.Add(Rel(op.Source));
                item.SubItems.Add(op.Kind == "folder" || op.Kind == "merge" ? Path.GetFileName(op.Target) : Rel(op.Target));
                long size = op.Size;   // vom Arbeitsthread ermittelt (EpisodeTask.Plan)
                ListSort.Add(item, size >= 0 ? Fmt.Size(size) : "", size >= 0 ? (object)size : null);
                item.SubItems.Add(op.Note);
                item.Tag = op;
                item.Checked = op.Selected;
                item.ToolTipText = op.Source + "\n->  " + op.Target;
                if (op.Group != null) item.ForeColor = Color.FromArgb(0xf0, 0xc4, 0x6c);
                list.Items.Add(item);
            }
            list.ItemChecked += OnItemChecked;

            // Fehlende Folgen und nicht zugeordnete Dateien
            List<string> info = new List<string>();
            if (plan.Missing.Count > 0)
            {
                info.Add("Fehlende Folgen:");
                foreach (string m in plan.Missing) info.Add("  " + m);
            }
            if (plan.Unmatched.Count > 0)
            {
                if (info.Count > 0) info.Add("");
                info.Add("Nicht zugeordnet (bleiben unverändert):");
                foreach (string u in plan.Unmatched) info.Add("  " + u);
            }
            if (info.Count == 0) info.Add("Alle Folgen der Liste sind vorhanden, alle Dateien wurden zugeordnet.");
            info.Add("");
            info.Add("Tipp: Eine gelbe Zeile (mehrere Dateien für eine Folge) oder eine Zeile mit vorhandenem Ziel anklicken, um die Dateien zu vergleichen.");
            details = new TextBox(); DarkWindow.Apply(details);
            details.Multiline = true;
            details.ReadOnly = true;
            details.ScrollBars = ScrollBars.Vertical;
            details.BackColor = Theme.LogBack;
            details.ForeColor = Theme.Muted;
            details.BorderStyle = BorderStyle.None;
            details.Font = new Font("Consolas", 9f);
            details.Text = string.Join("\r\n", info.ToArray());
            details.Dock = DockStyle.Fill;
            // Vergleich der Dateien einer Konfliktzeile (statt der Liste fehlender Folgen)
            compare = new CompareView(ff, true);
            compare.Dock = DockStyle.Fill;
            compare.Visible = false;
            compareTitle = Theme.MakeLabel("", true);
            compareTitle.Dock = DockStyle.Top;
            compareTitle.Visible = false;
            compareTitle.Padding = new Padding(0, 0, 0, Theme.S(4));
            Panel detailsHost = new Panel();
            detailsHost.Dock = DockStyle.Bottom;
            detailsHost.Height = Theme.S(170);
            // Abstand unten: die letzte Zeile des Hinweises stiess sonst an die Knopfzeile
            detailsHost.Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), Theme.S(6));
            detailsHost.Controls.Add(details);
            detailsHost.Controls.Add(compare);
            detailsHost.Controls.Add(compareTitle);
            list.SelectedIndexChanged += delegate { ShowCompare(); };

            Panel listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            listHost.Controls.Add(list);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button ok = Theme.MakeButton("Umbenennen", true);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button all = Theme.MakeButton("Alle ohne Konflikt", false);
            Button none = Theme.MakeButton("Keine", false);
            ok.Click += delegate
            {
                foreach (ListViewItem i in list.Items) ((EpisodeOp)i.Tag).Selected = i.Checked;
                confirmed = true;
                DialogResult = DialogResult.OK;
            };
            cancel.Click += delegate { confirmed = false; plan.SwitchTo = null; DialogResult = DialogResult.Cancel; };
            all.Click += delegate
            {
                adjusting = true;
                // Ohne Gruppe und ohne belegtes Ziel (SFLIX-20261124-116)
                foreach (ListViewItem i in list.Items)
                {
                    EpisodeOp candidate = (EpisodeOp)i.Tag;
                    if (candidate.Group == null && !candidate.TargetTaken) i.Checked = true;
                }
                adjusting = false;
            };
            none.Click += delegate
            {
                adjusting = true;
                foreach (ListViewItem i in list.Items) i.Checked = false;
                adjusting = false;
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(none);
            buttons.Controls.Add(all);

            // Quelle der Folgenliste wechseln (seit Desktop 4.0.12): schliesst die Vorschau, die Aufgabe
            // sucht dieselbe Serie auf der gewählten Quelle und zeigt danach eine neue Vorschau
            FlowLayoutPanel sourceRow = new FlowLayoutPanel();
            sourceRow.AutoSize = true;
            sourceRow.WrapContents = false;
            sourceRow.Margin = new Padding(0, 0, Theme.S(16), 0);
            Label sl = Theme.MakeLabel("Quelle:", false);
            sl.Margin = new Padding(0, Theme.S(9), Theme.S(6), 0);
            sourceBox = EpisodeSearchDialog.SourceBox();
            sourceBox.Margin = new Padding(0, Theme.S(6), 0, 0);
            for (int i = 0; i < EpisodeSource.All.Length; i++)
                if (string.Equals(EpisodeSource.All[i].Name, plan.List.Source, StringComparison.OrdinalIgnoreCase)) sourceBox.SelectedIndex = i;
            sourceBox.SelectedIndexChanged += delegate
            {
                if (sourceBox.SelectedIndex < 0) return;
                string name = EpisodeSource.All[sourceBox.SelectedIndex].Name;
                if (string.Equals(name, plan.List.Source, StringComparison.OrdinalIgnoreCase)) return;
                plan.SwitchTo = name;
                confirmed = false;
                DialogResult = DialogResult.Cancel;
            };
            sourceRow.Controls.Add(sl);
            sourceRow.Controls.Add(sourceBox);
            buttons.Controls.Add(sourceRow);

            Controls.Add(listHost);
            Controls.Add(intro);
            Controls.Add(detailsHost);
            Controls.Add(buttons);
            CancelButton = cancel;
        }

        // Gewählte Zeile: Dateien derselben Folge bzw. vorhandenes Ziel vergleichen
        void ShowCompare()
        {
            EpisodeOp op = list.SelectedItems.Count > 0 ? list.SelectedItems[0].Tag as EpisodeOp : null;
            List<CompareEntry> entries = new List<CompareEntry>();
            if (op != null && op.Kind != "folder" && op.Kind != "merge")
            {
                if (op.Group != null)
                {
                    foreach (EpisodeOp other in plan.Ops)
                        if (other.Group == op.Group) entries.Add(Entry(other == op ? "gewählt" : "andere", other.Source, Path.GetFileName(other.Target)));
                }
                if (op.TargetExists)
                {
                    if (entries.Count == 0) entries.Add(Entry("neu", op.Source, null));
                    bool known = false;
                    foreach (CompareEntry e in entries) if (string.Equals(e.Path, op.Target, StringComparison.OrdinalIgnoreCase)) known = true;
                    if (!known) entries.Add(Entry("vorhanden", op.Target, "wird ersetzt (Ablage „" + EpisodeTask.ReplacedFolderName + "“)"));
                }
            }
            bool show = entries.Count >= 2;
            details.Visible = !show;
            compare.Visible = show;
            compareTitle.Visible = show;
            if (!show) return;
            compareTitle.Text = op.Group != null ? "Mehrere Dateien für dieselbe Folge, nur eine kann umbenannt werden:" : "Das Ziel gibt es schon:";
            compare.SetEntries(entries);
        }

        static CompareEntry Entry(string label, string path, string note)
        {
            CompareEntry e = new CompareEntry();
            e.Label = label;
            e.Path = path;
            e.Note = note;
            return e;
        }

        string Rel(string path)
        {
            return path.StartsWith(plan.SeriesDir + "\\", StringComparison.OrdinalIgnoreCase) ? path.Substring(plan.SeriesDir.Length + 1) : path;
        }

        // Mehrere Dateien für dieselbe Folge: eine angehakte nimmt den Haken der anderen weg
        void OnItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (adjusting || !e.Item.Checked) return;
            EpisodeOp op = (EpisodeOp)e.Item.Tag;
            if (op.Group == null) return;
            adjusting = true;
            foreach (ListViewItem other in list.Items)
                if (other != e.Item && ((EpisodeOp)other.Tag).Group == op.Group) other.Checked = false;
            adjusting = false;
        }

        public bool Confirm(Form owner)
        {
            Icon = owner.Icon;
            Shown += delegate { list.Columns[list.Columns.Count - 1].Width = -2; };
            ShowDialog(owner);
            return confirmed;
        }
    }
}
