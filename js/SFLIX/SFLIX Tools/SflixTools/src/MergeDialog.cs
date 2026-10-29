// Auswahl der gefundenen Teil-Gruppen vor dem Zusammenfügen (ersetzt die j/n/a/q-Fragen).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public class MergeDialog : Form
    {
        readonly List<MergeGroup> groups;
        readonly ListView list;
        List<MergeGroup> chosen = new List<MergeGroup>();

        public MergeDialog(List<MergeGroup> groups)
        {
            this.groups = groups;
            Text = "Gesplittete Medien zusammenführen";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(980), Theme.S(520));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(
                "Welche Videos sollen zusammengefügt werden? Videos mit unterschiedlichen Formaten werden neu kodiert, " +
                "das dauert deutlich länger und ist deshalb nicht vorausgewählt.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width, 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(10));
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
            DarkList.Apply(list);
            list.Columns.Add("Ergebnis", Theme.S(300));
            list.Columns.Add("Teile", Theme.S(60), HorizontalAlignment.Right);
            list.Columns.Add("Länge", Theme.S(80), HorizontalAlignment.Right);
            list.Columns.Add("Grösse", Theme.S(80), HorizontalAlignment.Right);
            list.Columns.Add("Hinweis", Theme.S(420));

            foreach (MergeGroup g in groups)
            {
                ListViewItem item = new ListViewItem(Path.GetFileName(g.Target));
                ListSort.Add(item, g.Parts.Count.ToString(), g.Parts.Count);
                ListSort.Add(item, g.Problem == null ? Fmt.Time(g.TotalDuration) : "", g.Problem == null ? (object)g.TotalDuration : null);
                ListSort.Add(item, g.Problem == null ? Fmt.Size(g.TotalSize) : "", g.Problem == null ? (object)g.TotalSize : null);
                string hint = g.Problem ?? (g.NeedsReencode ? "Formate unterschiedlich – wird neu kodiert (dauert länger)" : "");
                item.SubItems.Add(hint + (hint.Length > 0 ? "   " : "") + Path.GetDirectoryName(g.Target));
                item.ToolTipText = string.Join("\n", g.Parts.ConvertAll(Path.GetFileName));
                item.Tag = g;
                if (g.Problem != null) item.ForeColor = Theme.Muted;
                item.Checked = g.Problem == null && !g.NeedsReencode;
                list.Items.Add(item);
            }
            list.ShowItemToolTips = true;
            list.ItemCheck += delegate(object s, ItemCheckEventArgs e)
            {
                MergeGroup g = (MergeGroup)list.Items[e.Index].Tag;
                if (g.Problem != null) e.NewValue = CheckState.Unchecked;   // nicht auswählbar
            };

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button ok = Theme.MakeButton("Zusammenfügen", true);
            Button cancel = Theme.MakeButton("Nichts zusammenfügen", false);
            Button all = Theme.MakeButton("Alle", false);
            Button none = Theme.MakeButton("Keine", false);
            ok.Click += delegate
            {
                chosen = new List<MergeGroup>();
                foreach (ListViewItem i in list.Items) if (i.Checked) chosen.Add((MergeGroup)i.Tag);
                DialogResult = DialogResult.OK;
            };
            cancel.Click += delegate { chosen = new List<MergeGroup>(); DialogResult = DialogResult.Cancel; };
            all.Click += delegate { foreach (ListViewItem i in list.Items) i.Checked = true; };
            none.Click += delegate { foreach (ListViewItem i in list.Items) i.Checked = false; };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(none);
            buttons.Controls.Add(all);

            Controls.Add(list);
            Controls.Add(intro);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public List<MergeGroup> Choose(Form owner)
        {
            Icon = owner.Icon;
            // Letzte Spalte füllt die restliche Breite (sonst bleibt rechts im Kopf eine weisse Fläche)
            Shown += delegate { list.Columns[list.Columns.Count - 1].Width = -2; };
            list.Resize += delegate { if (list.Columns.Count > 0) list.Columns[list.Columns.Count - 1].Width = -2; };
            ShowDialog(owner);
            return chosen;
        }
    }
}
