// Auswahl bei "Ablagen leeren": jeder Ablage-Ordner mit Art, Dateianzahl und Grösse.
// Seit 3.4.3: sortierbar (Klick auf den Spaltenkopf), Filter nach Art, "Wiederherstellen" neben
// "Endgültig löschen", und je Ordner lassen sich einzelne Dateien wählen ("Dateien wählen …" oder
// Doppelklick). "Alle"/"Keine" rechnen die Summe nur einmal neu (früher bei jedem Haken).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public class CleanupDialog : Form
    {
        readonly ListView list;
        readonly Label totalLabel;
        readonly List<ListViewItem> allRows = new List<ListViewItem>();
        readonly Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
        readonly string root;
        readonly FfmpegTools ff;
        string filterKind;          // null = alle
        bool bulk;                  // "Alle"/"Keine" laufen: Summe erst am Ende
        bool shown;                 // Fenster angezeigt: vorher meldet die Liste beim Aufbau jedes Häkchen,
                                    // und Items darf dann nicht gelesen werden (NullReferenceException)
        CleanupChoice result;

        public CleanupDialog(List<CleanupFolder> folders, string root) : this(folders, root, null) { }

        public CleanupDialog(List<CleanupFolder> folders, string root, FfmpegTools ff)
        {
            this.root = root;
            this.ff = ff;
            Text = "Ablagen leeren";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(980), Theme.S(520));
            MinimumSize = new Size(Theme.S(760), Theme.S(400));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(
                "„Endgültig löschen“ entfernt die angehakten Ablagen samt Inhalt (nicht in den Papierkorb). " +
                "„Wiederherstellen“ legt ihren Inhalt zurück in den Ordner darüber, z.B. Filme\\__gelöscht\\Film.mp4 nach Filme\\Film.mp4. " +
                "Mit „Dateien wählen …“ oder einem Doppelklick gilt beides nur für einzelne Dateien eines Ordners.", false);
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
            list.ShowItemToolTips = true;
            DarkList.Apply(list);
            list.Columns.Add("Art", Theme.S(110));
            list.Columns.Add("Ordner (im Medienordner)", Theme.S(470));
            list.Columns.Add("Dateien", Theme.S(80), HorizontalAlignment.Right);
            list.Columns.Add("Grösse", Theme.S(100), HorizontalAlignment.Right);
            list.Columns.Add("Hinweis", Theme.S(160));

            string prefix = root.TrimEnd('\\') + "\\";
            Dictionary<string, int> kinds = new Dictionary<string, int>();
            foreach (CleanupFolder f in folders)
            {
                // Relativ zum Medienordner anzeigen, damit der wichtige Teil (z.B. "Filme\__teile") sichtbar ist
                string shown = f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? f.Path.Substring(prefix.Length) : f.Path;
                string kind = f.Kind ?? CleanupTask.KindOf(Path.GetFileName(f.Path));
                ListViewItem item = new ListViewItem(kind);
                item.SubItems.Add(shown);
                ListSort.Add(item, f.Files.ToString(), f.Files);
                ListSort.Add(item, Fmt.Size(f.Size), f.Size);
                item.SubItems.Add(f.Problem ?? "");
                item.ToolTipText = f.Path;
                item.Tag = f;
                item.Checked = true;
                allRows.Add(item);
                int n;
                kinds.TryGetValue(kind, out n);
                kinds[kind] = n + 1;
            }
            list.ItemChecked += delegate { if (!bulk && shown) UpdateTotal(); };
            list.DoubleClick += delegate { ChooseFiles(); };

            // Filter nach Art
            FlowLayoutPanel filterBar = new FlowLayoutPanel();
            filterBar.Dock = DockStyle.Top;
            filterBar.AutoSize = true;
            filterBar.WrapContents = true;
            filterBar.Padding = new Padding(Theme.S(12), 0, Theme.S(12), Theme.S(8));
            AddFilterButton(filterBar, null, "Alle (" + folders.Count + ")");
            List<string> kindNames = new List<string>(kinds.Keys);
            kindNames.Sort(StringComparer.CurrentCultureIgnoreCase);
            foreach (string k in kindNames) AddFilterButton(filterBar, k, k + " (" + kinds[k] + ")");

            Panel listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            listHost.Controls.Add(list);

            totalLabel = Theme.MakeLabel("", false);
            totalLabel.Font = new Font("Segoe UI Semibold", 10f);
            totalLabel.Margin = new Padding(0, Theme.S(8), Theme.S(20), 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button delete = Theme.MakeButton("Endgültig löschen", true);
            Button restore = Theme.MakeButton("Wiederherstellen", false);
            Button cancel = Theme.MakeButton("Nichts tun", false);
            Button files = Theme.MakeButton("Dateien wählen …", false);
            Button all = Theme.MakeButton("Alle", false);
            Button none = Theme.MakeButton("Keine", false);
            delete.Click += delegate { Finish(false); };
            restore.Click += delegate { Finish(true); };
            cancel.Click += delegate { result = null; DialogResult = DialogResult.Cancel; };
            files.Click += delegate { ChooseFiles(); };
            all.Click += delegate { SetVisibleChecked(true); };
            none.Click += delegate { SetVisibleChecked(false); };
            buttons.Controls.Add(delete);
            buttons.Controls.Add(restore);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(files);
            buttons.Controls.Add(none);
            buttons.Controls.Add(all);
            buttons.Controls.Add(totalLabel);

            Controls.Add(listHost);
            Controls.Add(buttons);
            Controls.Add(filterBar);
            Controls.Add(intro);
            // Kein AcceptButton: Löschen nur mit bewusstem Klick, Enter/Esc brechen ab
            CancelButton = cancel;
            ApplyFilter();
        }

        void AddFilterButton(FlowLayoutPanel bar, string kind, string text)
        {
            Button b = Theme.MakeButton(text, false);
            b.Margin = new Padding(0, 0, Theme.S(6), 0);
            b.Click += delegate { filterKind = kind; ApplyFilter(); };
            filterButtons[kind ?? ""] = b;
            bar.Controls.Add(b);
        }

        // Nur die Ablagen der gewählten Art zeigen (Haken bleiben erhalten)
        void ApplyFilter()
        {
            foreach (KeyValuePair<string, Button> p in filterButtons)
            {
                bool active = p.Key == (filterKind ?? "");
                p.Value.BackColor = active ? Theme.Line : Theme.Input;
                p.Value.FlatAppearance.BorderColor = active ? Theme.Muted : Theme.Line;
            }
            List<ListViewItem> shown = new List<ListViewItem>();
            foreach (ListViewItem row in allRows)
                if (filterKind == null || row.Text == filterKind) shown.Add(row);
            bulk = true;
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                list.Items.AddRange(shown.ToArray());
                ListSort sort = ListSort.Of(list);
                if (sort != null) sort.Apply();
            }
            finally
            {
                list.EndUpdate();
                bulk = false;
            }
            UpdateTotal();
        }

        void SetVisibleChecked(bool value)
        {
            bulk = true;
            list.BeginUpdate();
            try
            {
                foreach (ListViewItem i in list.Items) i.Checked = value;
            }
            finally
            {
                list.EndUpdate();
                bulk = false;
            }
            UpdateTotal();
        }

        // Angehakt und sichtbar (ausgeblendete Arten zählen nicht)
        List<CleanupFolder> Checked()
        {
            List<CleanupFolder> sel = new List<CleanupFolder>();
            foreach (ListViewItem i in list.Items) if (i.Checked) sel.Add((CleanupFolder)i.Tag);
            return sel;
        }

        void UpdateTotal()
        {
            long size = 0;
            int files = 0, count = 0;
            foreach (CleanupFolder f in Checked())
            {
                if (f.OnlyFiles != null)
                {
                    files += f.OnlyFiles.Count;
                    foreach (string file in f.OnlyFiles)
                        try { size += new FileInfo(file).Length; } catch { }
                }
                else
                {
                    size += f.Size;
                    files += f.Files;
                }
                count++;
            }
            totalLabel.Text = count + " Ordner, " + files + " Dateien, " + Fmt.Size(size);
        }

        // Einzelne Dateien des markierten Ordners wählen
        void ChooseFiles()
        {
            if (list.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Bitte zuerst eine Ablage in der Liste markieren.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            ListViewItem row = list.SelectedItems[0];
            CleanupFolder f = (CleanupFolder)row.Tag;
            using (CleanupFilesDialog dlg = new CleanupFilesDialog(f, ff))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                f.OnlyFiles = dlg.Selected;
            }
            int total = CleanupTask.FilesIn(f.Path).Count;
            if (f.OnlyFiles != null && f.OnlyFiles.Count >= total) f.OnlyFiles = null;
            row.SubItems[4].Text = f.OnlyFiles == null ? (f.Problem ?? "") : f.OnlyFiles.Count + " von " + total + " Dateien gewählt";
            if (f.OnlyFiles != null && f.OnlyFiles.Count > 0) row.Checked = true;
            if (f.OnlyFiles != null && f.OnlyFiles.Count == 0) row.Checked = false;
            UpdateTotal();
        }

        void Finish(bool restore)
        {
            List<CleanupFolder> sel = Checked();
            if (sel.Count == 0)
            {
                MessageBox.Show(this, "Keine Ablage angehakt.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!restore && MessageBox.Show(this, "Die angehakten Ablagen bzw. Dateien werden endgültig gelöscht (" + totalLabel.Text + "). Fortfahren?",
                                            App.Title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
            result = new CleanupChoice();
            result.Restore = restore;
            result.Folders = sel;
            DialogResult = DialogResult.OK;
        }

        public CleanupChoice Choose(Form owner)
        {
            Icon = owner.Icon;
            Shown += delegate
            {
                list.Columns[list.Columns.Count - 1].Width = -2;
                shown = true;
                UpdateTotal();
            };
            ShowDialog(owner);
            return result;
        }
    }

    // Dateien einer Ablage wählen: Liste mit Häkchen, rechts der Vergleich mit einer gleichnamigen
    // Datei am Ziel (falls vorhanden)
    public class CleanupFilesDialog : Form
    {
        readonly ListView list;
        readonly CompareView compare;
        readonly CleanupFolder folder;
        public List<string> Selected;

        public CleanupFilesDialog(CleanupFolder folder, FfmpegTools ff)
        {
            this.folder = folder;
            Text = "Dateien wählen – " + Path.GetFileName(folder.Path);
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(980), Theme.S(560));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel("Angehakte Dateien werden wiederhergestellt bzw. gelöscht. Ein Klick auf eine Datei zeigt den Vergleich " +
                                          "mit einer gleichnamigen Datei am Ziel, falls es sie schon gibt.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));

            list = new ListView();
            list.View = View.Details;
            list.CheckBoxes = true;
            list.FullRowSelect = true;
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            DarkList.Apply(list);
            list.Columns.Add("Datei", Theme.S(520));
            list.Columns.Add("Grösse", Theme.S(100), HorizontalAlignment.Right);
            list.Columns.Add("Am Ziel", Theme.S(120));
            string parent = Path.GetDirectoryName(folder.Path.TrimEnd('\\'));
            string basePath = Path.GetFullPath(folder.Path).TrimEnd('\\') + "\\";
            HashSet<string> chosen = folder.OnlyFiles != null ? new HashSet<string>(folder.OnlyFiles, StringComparer.OrdinalIgnoreCase) : null;
            foreach (string file in CleanupTask.FilesIn(folder.Path))
            {
                string rel = Path.GetFullPath(file).Substring(basePath.Length);
                ListViewItem item = new ListViewItem(rel);
                long size = 0;
                try { size = new FileInfo(file).Length; } catch { }
                ListSort.Add(item, Fmt.Size(size), size);
                item.SubItems.Add(File.Exists(Path.Combine(parent, rel)) ? "vorhanden" : "");
                item.Tag = file;
                item.Checked = chosen == null || chosen.Contains(file);
                list.Items.Add(item);
            }

            compare = new CompareView(ff, true);
            compare.Dock = DockStyle.Bottom;
            compare.Height = Theme.S(150);
            list.SelectedIndexChanged += delegate { ShowCompare(parent, basePath); };

            Panel host = new Panel();
            host.Dock = DockStyle.Fill;
            host.Padding = new Padding(Theme.S(12), 0, Theme.S(12), Theme.S(8));
            host.Controls.Add(list);
            host.Controls.Add(compare);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button ok = Theme.MakeButton("Übernehmen", true);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button all = Theme.MakeButton("Alle", false);
            Button none = Theme.MakeButton("Keine", false);
            ok.Click += delegate
            {
                Selected = new List<string>();
                foreach (ListViewItem i in list.Items) if (i.Checked) Selected.Add((string)i.Tag);
                DialogResult = DialogResult.OK;
            };
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            all.Click += delegate { list.BeginUpdate(); foreach (ListViewItem i in list.Items) i.Checked = true; list.EndUpdate(); };
            none.Click += delegate { list.BeginUpdate(); foreach (ListViewItem i in list.Items) i.Checked = false; list.EndUpdate(); };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(none);
            buttons.Controls.Add(all);

            Controls.Add(host);
            Controls.Add(buttons);
            Controls.Add(intro);
            CancelButton = cancel;
        }

        void ShowCompare(string parent, string basePath)
        {
            if (list.SelectedItems.Count == 0) { compare.SetEntries(new List<CompareEntry>()); return; }
            string file = (string)list.SelectedItems[0].Tag;
            string target = Path.Combine(parent, Path.GetFullPath(file).Substring(basePath.Length));
            List<CompareEntry> entries = new List<CompareEntry>();
            CompareEntry stored = new CompareEntry();
            stored.Label = "in der Ablage";
            stored.Path = file;
            entries.Add(stored);
            if (File.Exists(target))
            {
                CompareEntry existing = new CompareEntry();
                existing.Label = "am Ziel";
                existing.Path = target;
                entries.Add(existing);
            }
            compare.SetEntries(entries);
        }
    }
}
