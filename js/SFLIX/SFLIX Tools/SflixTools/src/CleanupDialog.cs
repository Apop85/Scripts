// Bestätigung vor dem Leeren der Ablagen: zeigt jeden Ordner mit Dateianzahl und Grösse.

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
        List<CleanupFolder> chosen = new List<CleanupFolder>();

        public CleanupDialog(List<CleanupFolder> folders, string root)
        {
            Text = "Ablagen leeren";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(900), Theme.S(460));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(
                "Diese Ordner werden samt Inhalt ENDGÜLTIG gelöscht (nicht in den Papierkorb). " +
                "Entferne das Häkchen bei allem, was du behalten möchtest.", false);
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
            list.Columns.Add("Ordner (im Medienordner)", Theme.S(560));
            list.Columns.Add("Dateien", Theme.S(80), HorizontalAlignment.Right);
            list.Columns.Add("Grösse", Theme.S(100), HorizontalAlignment.Right);
            list.Columns.Add("Hinweis", Theme.S(140));

            foreach (CleanupFolder f in folders)
            {
                // Relativ zum Medienordner anzeigen, damit der wichtige Teil (z.B. "Filme\__teile") sichtbar ist
                string prefix = root.TrimEnd('\\') + "\\";
                string shown = f.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? f.Path.Substring(prefix.Length) : f.Path;
                ListViewItem item = new ListViewItem(shown);
                item.ToolTipText = f.Path;
                ListSort.Add(item, f.Files.ToString(), f.Files);
                ListSort.Add(item, Fmt.Size(f.Size), f.Size);
                item.SubItems.Add(f.Problem ?? "");
                item.Tag = f;
                item.Checked = true;
                list.Items.Add(item);
            }
            list.ShowItemToolTips = true;
            list.ItemChecked += delegate { UpdateTotal(); };

            totalLabel = Theme.MakeLabel("", false);
            totalLabel.Font = new Font("Segoe UI Semibold", 10f);
            totalLabel.Margin = new Padding(0, Theme.S(8), Theme.S(20), 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button ok = Theme.MakeButton("Endgültig löschen", true);
            Button cancel = Theme.MakeButton("Nichts löschen", false);
            Button all = Theme.MakeButton("Alle", false);
            Button none = Theme.MakeButton("Keine", false);
            ok.Click += delegate
            {
                List<CleanupFolder> sel = new List<CleanupFolder>();
                foreach (ListViewItem i in list.Items) if (i.Checked) sel.Add((CleanupFolder)i.Tag);
                if (sel.Count == 0) { DialogResult = DialogResult.Cancel; return; }
                chosen = sel;
                DialogResult = DialogResult.OK;
            };
            cancel.Click += delegate { chosen = new List<CleanupFolder>(); DialogResult = DialogResult.Cancel; };
            all.Click += delegate { foreach (ListViewItem i in list.Items) i.Checked = true; };
            none.Click += delegate { foreach (ListViewItem i in list.Items) i.Checked = false; };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(none);
            buttons.Controls.Add(all);
            buttons.Controls.Add(totalLabel);

            Controls.Add(list);
            Controls.Add(intro);
            Controls.Add(buttons);
            // Kein AcceptButton: Löschen nur mit bewusstem Klick, Enter/Esc brechen ab
            CancelButton = cancel;
            UpdateTotal();
        }

        void UpdateTotal()
        {
            long size = 0;
            int files = 0, count = 0;
            foreach (ListViewItem i in list.Items)
            {
                if (!i.Checked) continue;
                CleanupFolder f = (CleanupFolder)i.Tag;
                size += f.Size;
                files += f.Files;
                count++;
            }
            totalLabel.Text = count + " Ordner, " + files + " Dateien, " + Fmt.Size(size);
        }

        public List<CleanupFolder> Choose(Form owner)
        {
            Icon = owner.Icon;
            Shown += delegate { list.Columns[list.Columns.Count - 1].Width = -2; };
            ShowDialog(owner);
            return chosen;
        }
    }
}
