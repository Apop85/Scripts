// Reiter "Hilfe" (seit 3.3): links die Themen mit Suche, rechts der Text (HelpContent).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sflix
{
    public class HelpPage : TableLayoutPanel
    {
        readonly List<HelpTopic> topics = HelpContent.Topics();
        readonly ListBox list;
        readonly RichTextBox view;
        readonly TextBox search;
        readonly Label noHits;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        const int EM_SETCUEBANNER = 0x1501;

        public HelpPage()
        {
            ColumnCount = 2;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(220)));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            RowCount = 1;
            RowStyles.Add(new RowStyle(SizeType.Absolute, Theme.S(470)));
            Dock = DockStyle.Fill;
            Height = Theme.S(470);
            Margin = new Padding(0);
            BackColor = Theme.Back;

            TableLayoutPanel left = new TableLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.ColumnCount = 1;
            left.RowCount = 3;
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            left.BackColor = Theme.Card;
            left.Padding = new Padding(Theme.S(8));
            left.Margin = new Padding(0, 0, Theme.S(8), 0);
            search = Theme.MakeTextBox();
            search.Dock = DockStyle.Fill;
            search.Margin = new Padding(0, 0, 0, Theme.S(8));
            search.HandleCreated += delegate { SendMessage(search.Handle, EM_SETCUEBANNER, (IntPtr)1, "Hilfe durchsuchen …"); };
            search.TextChanged += delegate { Filter(); };
            list = new ListBox();
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.IntegralHeight = false;
            list.Font = new Font("Segoe UI", 10f);
            list.ItemHeight = Theme.S(26);
            list.DrawMode = DrawMode.OwnerDrawFixed;
            list.DrawItem += DrawTopic;
            list.SelectedIndexChanged += delegate { Show(list.SelectedItem as HelpTopic); };
            noHits = Theme.MakeLabel("Keine Treffer.", true);
            noHits.Visible = false;
            left.Controls.Add(search, 0, 0);
            left.Controls.Add(list, 0, 1);
            left.Controls.Add(noHits, 0, 2);

            view = new RichTextBox();
            view.Dock = DockStyle.Fill;
            view.ReadOnly = true;
            view.BorderStyle = BorderStyle.None;
            view.BackColor = Theme.Card;
            view.ForeColor = Theme.Text;
            view.DetectUrls = false;
            view.Margin = new Padding(0);
            view.ScrollBars = RichTextBoxScrollBars.Vertical;
            Panel viewHost = new Panel();
            viewHost.Dock = DockStyle.Fill;
            viewHost.BackColor = Theme.Card;
            viewHost.Padding = new Padding(Theme.S(16), Theme.S(12), Theme.S(8), Theme.S(12));
            viewHost.Margin = new Padding(0);
            viewHost.Controls.Add(view);

            Controls.Add(left, 0, 0);
            Controls.Add(viewHost, 1, 0);
            Filter();
        }

        // Thema nach Titel wählen (z.B. von aussen "Kommandozeile")
        public void ShowTopic(string title)
        {
            search.Text = "";
            foreach (HelpTopic t in list.Items)
                if (t.Title == title) { list.SelectedItem = t; return; }
        }

        void DrawTopic(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush back = new SolidBrush(selected ? Theme.Input : Theme.Card))
                e.Graphics.FillRectangle(back, e.Bounds);
            if (selected)
                using (SolidBrush red = new SolidBrush(Theme.Red))
                    e.Graphics.FillRectangle(red, e.Bounds.Left, e.Bounds.Top + Theme.S(4), Theme.S(3), e.Bounds.Height - Theme.S(8));
            Rectangle r = new Rectangle(e.Bounds.Left + Theme.S(10), e.Bounds.Top, e.Bounds.Width - Theme.S(12), e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, ((HelpTopic)list.Items[e.Index]).Title, list.Font, r, selected ? Color.White : Theme.Text,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        void Filter()
        {
            string q = search.Text.Trim();
            HelpTopic current = list.SelectedItem as HelpTopic;
            list.BeginUpdate();
            list.Items.Clear();
            foreach (HelpTopic t in topics)
                if (q.Length == 0 || t.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 || t.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    list.Items.Add(t);
            list.EndUpdate();
            noHits.Visible = list.Items.Count == 0;
            if (list.Items.Count == 0) { view.Clear(); return; }
            if (current != null && list.Items.Contains(current)) { list.SelectedItem = current; Show(current); }
            else list.SelectedIndex = 0;
        }

        void Show(HelpTopic topic)
        {
            view.Clear();
            if (topic == null) return;
            Font normal = new Font("Segoe UI", 10f);
            Font heading = new Font("Segoe UI Semibold", 10.5f);
            Font title = new Font("Segoe UI Semibold", 13f);
            Font mono = new Font("Consolas", 9.5f);
            Append(topic.Title + "\n", title, Color.White, 0);
            bool afterBlank = true;
            foreach (string raw in topic.Text.Split('\n'))
            {
                string line = raw.TrimEnd();
                bool blank = line.Length == 0;
                // Zwischenüberschrift: Abstand davor nur einmal
                if (line.StartsWith("# ")) Append((afterBlank ? "" : "\n") + line.Substring(2) + "\n", heading, Color.FromArgb(0xff, 0x5a, 0x45), 0);
                else if (line.StartsWith("- ")) Append("•  " + line.Substring(2) + "\n", normal, Theme.Text, Theme.S(14));
                else if (line.StartsWith("> ")) Append(line.Substring(2) + "\n", mono, Color.FromArgb(0xf0, 0xc4, 0x6c), Theme.S(14));
                else Append(line + "\n", normal, Theme.Text, 0);
                afterBlank = blank;
            }
            // Suchbegriff hervorheben
            string q = search.Text.Trim();
            if (q.Length > 0)
            {
                int first = -1;
                int pos = view.Find(q, 0, RichTextBoxFinds.None);
                while (pos >= 0)
                {
                    if (first < 0) first = pos;
                    view.SelectionBackColor = Color.FromArgb(0x5a, 0x45, 0x10);
                    pos = pos + q.Length < view.TextLength ? view.Find(q, pos + q.Length, RichTextBoxFinds.None) : -1;
                }
                view.SelectionStart = first >= 0 ? first : 0;
                view.SelectionLength = 0;
                view.ScrollToCaret();
            }
            else
            {
                view.SelectionStart = 0;
                view.ScrollToCaret();
            }
        }

        void Append(string text, Font font, Color color, int indent)
        {
            view.SelectionStart = view.TextLength;
            view.SelectionLength = 0;
            view.SelectionFont = font;
            view.SelectionColor = color;
            view.SelectionIndent = indent;
            view.SelectionHangingIndent = indent > 0 ? Theme.S(14) : 0;
            view.SelectedText = text;
        }
    }
}
