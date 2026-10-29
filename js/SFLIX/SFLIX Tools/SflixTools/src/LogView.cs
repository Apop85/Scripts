// Protokoll rechts im Hauptfenster: farbige Zeilen mit Uhrzeit, Filter "Nur Fehler und Warnungen",
// Suche, Kopieren, Leeren und Protokolldatei öffnen. Dazu der verschiebbare Splitter.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Sflix
{
    public enum LogKind { Normal, Heading, Error, Warning, Success, Muted }

    public class LogEntry
    {
        public DateTime Time;
        public string Text;
        public LogKind Kind;
    }

    public class LogPanel : TableLayoutPanel
    {
        const int MaxEntries = 20000;
        static readonly Color ErrorColor = Color.FromArgb(0xff, 0x6b, 0x6b);
        static readonly Color WarningColor = Color.FromArgb(0xf0, 0xc4, 0x6c);
        static readonly Color SuccessColor = Color.FromArgb(0x5e, 0xd3, 0x8a);
        static readonly Color HeadingColor = Color.White;
        static readonly Color NormalColor = Color.FromArgb(0xd0, 0xd0, 0xd6);
        static readonly Color TimeColor = Color.FromArgb(0x6c, 0x6c, 0x76);

        readonly List<LogEntry> entries = new List<LogEntry>();
        readonly RichTextBox box;
        readonly TextBox search;
        readonly DarkCheckBox onlyProblems;
        readonly DarkCheckBox saveToFile;
        readonly Button openButton;
        readonly Label countLabel;
        readonly Timer searchTimer = new Timer();

        // Wird bei einer neuen Fehlerzeile aufgerufen (z.B. um das Protokoll aufzuklappen)
        public Action ErrorAdded;
        // Pfad der Protokolldatei; vor dem Öffnen wird BeforeOpenFile aufgerufen (Datei schreiben)
        public string LogFile;
        public Action BeforeOpenFile;
        // Schalter "In Datei speichern" geändert (true = ab jetzt in die Datei schreiben)
        public Action<bool> SaveToFileChanged;
        // Protokoll dieser Sitzung als Text (für "Speichern unter …" bei ausgeschalteter Datei)
        public Func<string> SessionText;

        public bool SaveToFile
        {
            get { return saveToFile.Checked; }
            set { saveToFile.Checked = value; }
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        const int EM_SETCUEBANNER = 0x1501;

        public LogPanel()
        {
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            RowCount = 4;
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            BackColor = Theme.Card;
            Padding = new Padding(Theme.S(10), Theme.S(8), Theme.S(10), Theme.S(10));
            Margin = new Padding(0);

            // Kopf: Titel, Anzahl, Knöpfe
            TableLayoutPanel head = new TableLayoutPanel();
            head.Dock = DockStyle.Fill;
            head.AutoSize = true;
            head.ColumnCount = 3;
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            head.Margin = new Padding(0, 0, 0, Theme.S(6));
            head.BackColor = Color.Transparent;
            Label title = Theme.MakeLabel("Protokoll", false);
            title.Font = new Font("Segoe UI Semibold", 10.5f);
            title.Margin = new Padding(0, Theme.S(3), Theme.S(8), 0);
            countLabel = Theme.MakeLabel("", true);
            countLabel.Font = new Font("Segoe UI", 8.5f);
            countLabel.Margin = new Padding(0, Theme.S(6), 0, 0);
            FlowLayoutPanel tools = new FlowLayoutPanel();
            tools.AutoSize = true;
            tools.WrapContents = false;
            tools.Margin = new Padding(0);
            tools.BackColor = Color.Transparent;
            Button copy = ToolButton("Kopieren");
            Button clear = ToolButton("Leeren");
            openButton = ToolButton("Speichern unter …");
            copy.Click += delegate { CopyVisible(); };
            clear.Click += delegate { Clear(); };
            openButton.Click += delegate { if (saveToFile.Checked) OpenFile(); else SaveAs(); };
            tools.Controls.Add(copy);
            tools.Controls.Add(clear);
            tools.Controls.Add(openButton);
            head.Controls.Add(title, 0, 0);
            head.Controls.Add(countLabel, 1, 0);
            head.Controls.Add(tools, 2, 0);

            // Suche und Filter
            TableLayoutPanel filter = new TableLayoutPanel();
            filter.Dock = DockStyle.Fill;
            filter.AutoSize = true;
            filter.ColumnCount = 2;
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            filter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            filter.Margin = new Padding(0, 0, 0, Theme.S(6));
            filter.BackColor = Color.Transparent;
            search = Theme.MakeTextBox();
            search.Dock = DockStyle.Fill;
            search.Margin = new Padding(0, Theme.S(2), Theme.S(10), 0);
            search.HandleCreated += delegate { SendMessage(search.Handle, EM_SETCUEBANNER, (IntPtr)1, "Suchen …"); };
            searchTimer.Interval = 250;
            searchTimer.Tick += delegate { searchTimer.Stop(); Render(); };
            search.TextChanged += delegate { searchTimer.Stop(); searchTimer.Start(); };
            onlyProblems = new DarkCheckBox();
            onlyProblems.Text = "Nur Fehler und Warnungen";
            onlyProblems.ForeColor = Theme.Text;
            onlyProblems.Switch = true;
            onlyProblems.Margin = new Padding(0);
            onlyProblems.FitText();
            onlyProblems.CheckedChanged += delegate { Render(); };
            // Protokolldatei nur auf Wunsch (wird nicht gespeichert, beim Start immer aus)
            saveToFile = new DarkCheckBox();
            saveToFile.Text = "In Datei speichern";
            saveToFile.ForeColor = Theme.Text;
            saveToFile.Switch = true;
            saveToFile.Margin = new Padding(0, 0, 0, Theme.S(6));
            saveToFile.FitText();
            saveToFile.CheckedChanged += delegate
            {
                openButton.Text = saveToFile.Checked ? "Datei öffnen" : "Speichern unter …";
                if (SaveToFileChanged != null) SaveToFileChanged(saveToFile.Checked);
            };
            filter.Controls.Add(search, 0, 0);
            filter.Controls.Add(onlyProblems, 1, 0);

            box = new RichTextBox();
            box.ReadOnly = true;
            box.DetectUrls = false;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.LogBack;
            box.ForeColor = NormalColor;
            box.Font = new Font("Consolas", 9f);
            box.WordWrap = true;
            box.ScrollBars = RichTextBoxScrollBars.Vertical;
            box.Dock = DockStyle.Fill;
            box.Margin = new Padding(0);
            box.HideSelection = false;

            Controls.Add(head, 0, 0);
            Controls.Add(filter, 0, 1);
            Controls.Add(saveToFile, 0, 2);
            Controls.Add(box, 0, 3);
            UpdateCount();
        }

        Button ToolButton(string text)
        {
            Button b = Theme.MakeButton(text, false);
            b.Font = new Font("Segoe UI", 8.5f);
            b.Padding = new Padding(Theme.S(6), 0, Theme.S(6), 0);
            b.Margin = new Padding(Theme.S(4), 0, 0, 0);
            return b;
        }

        public static LogKind Classify(string line)
        {
            string t = line.Trim();
            if (t.Length == 0) return LogKind.Normal;
            if (t.StartsWith("===") && t.Replace("=", "").Trim().Length > 0) return LogKind.Heading;
            if (t.Replace("=", "").Length == 0) return LogKind.Muted;
            if (t.Contains("FEHLER") || t.StartsWith("Fehler", StringComparison.Ordinal)) return LogKind.Error;
            if (t.StartsWith("Hinweis") || t.StartsWith("WARNUNG") || t.StartsWith("Achtung") || t.Contains("ABGEBROCHEN") ||
                t.StartsWith("Abgebrochen") || t.Contains("übersprungen)") || t.StartsWith("Kein Zugriff") || t.StartsWith("FEHLT") ||
                t.StartsWith("ÜBERSPRUNGEN") || t.Contains("konnten nicht"))
                return LogKind.Warning;
            if (t.StartsWith("OK ") || t == "OK" || t.Contains("ERFOLGREICH") || t.StartsWith("GELÖSCHT") || t.StartsWith("ORDNER ") ||
                t.StartsWith("ZUSAMMENGEFÜHRT") || t.StartsWith("fertig:") || t.StartsWith("data.js gespeichert"))
                return LogKind.Success;
            return LogKind.Normal;
        }

        static Color ColorOf(LogKind kind)
        {
            switch (kind)
            {
                case LogKind.Error: return ErrorColor;
                case LogKind.Warning: return WarningColor;
                case LogKind.Success: return SuccessColor;
                case LogKind.Heading: return HeadingColor;
                case LogKind.Muted: return TimeColor;
            }
            return NormalColor;
        }

        bool IsShown(LogEntry e)
        {
            if (onlyProblems.Checked && e.Kind != LogKind.Error && e.Kind != LogKind.Warning) return false;
            string q = search.Text.Trim();
            return q.Length == 0 || e.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static string Display(LogEntry e)
        {
            // Überschriften "=== Daten einlesen ===" ohne Gleichheitszeichen
            if (e.Kind == LogKind.Heading) return e.Text.Trim().Trim('=').Trim().ToUpperInvariant();
            return e.Text;
        }

        public void Add(IList<string> lines)
        {
            if (lines.Count == 0) return;
            bool error = false;
            List<LogEntry> added = new List<LogEntry>();
            DateTime now = DateTime.Now;
            foreach (string line in lines)
            {
                LogEntry e = new LogEntry();
                e.Time = now;
                e.Text = line;
                e.Kind = Classify(line);
                if (e.Kind == LogKind.Error) error = true;
                entries.Add(e);
                added.Add(e);
            }
            if (entries.Count > MaxEntries)
            {
                entries.RemoveRange(0, entries.Count - MaxEntries / 2);
                Render();
            }
            else
            {
                List<LogEntry> shown = added.FindAll(IsShown);
                if (shown.Count > 20)
                {
                    // Viele Zeilen auf einmal als RTF anhängen: Zeile für Zeile dauert bei tausenden Zeilen Minuten
                    StringBuilder rtf = new StringBuilder();
                    RtfHeader(rtf);
                    bool first = box.TextLength == 0;
                    foreach (LogEntry e in shown)
                    {
                        if (!first) rtf.Append(@"\par ");
                        first = false;
                        RtfEntry(rtf, e);
                    }
                    rtf.Append("}");
                    box.SelectionStart = box.TextLength;
                    box.SelectionLength = 0;
                    box.SelectedRtf = rtf.ToString();
                }
                else
                {
                    box.SuspendLayout();
                    foreach (LogEntry e in shown) AppendEntry(e);
                    box.ResumeLayout();
                }
                ScrollToEnd();
            }
            UpdateCount();
            if (error && ErrorAdded != null) ErrorAdded();
        }

        void AppendEntry(LogEntry e)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            if (box.TextLength > 0)
            {
                box.SelectionColor = NormalColor;
                box.SelectedText = "\n";
            }
            if (e.Text.Trim().Length == 0) return;
            box.SelectionColor = TimeColor;
            box.SelectedText = e.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "  ";
            box.SelectionColor = ColorOf(e.Kind);
            box.SelectionFont = e.Kind == LogKind.Heading ? new Font(box.Font, FontStyle.Bold) : box.Font;
            box.SelectedText = Display(e);
        }

        void ScrollToEnd()
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.ScrollToCaret();
        }

        // Alles neu aufbauen (nach Filter, Suche, Kürzen): als RTF, das geht auch bei vielen Zeilen schnell
        void Render()
        {
            StringBuilder rtf = new StringBuilder();
            RtfHeader(rtf);
            bool first = true;
            int shown = 0;
            foreach (LogEntry e in entries)
            {
                if (!IsShown(e)) continue;
                if (!first) rtf.Append(@"\par ");
                first = false;
                shown++;
                RtfEntry(rtf, e);
            }
            if (shown == 0 && entries.Count > 0) rtf.Append(@"\cf1 Keine passenden Zeilen.");
            rtf.Append("}");
            box.Rtf = rtf.ToString();
            ScrollToEnd();
            UpdateCount();
        }

        void RtfHeader(StringBuilder rtf)
        {
            rtf.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Consolas;}}{\colortbl;");
            foreach (Color c in new Color[] { TimeColor, NormalColor, ErrorColor, WarningColor, SuccessColor, HeadingColor })
                rtf.Append(@"\red").Append(c.R).Append(@"\green").Append(c.G).Append(@"\blue").Append(c.B).Append(";");
            rtf.Append(@"}\f0\fs18 ");
        }

        static void RtfEntry(StringBuilder rtf, LogEntry e)
        {
            if (e.Text.Trim().Length == 0) return;
            rtf.Append(@"\cf1 ").Append(e.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append("  ");
            int color = e.Kind == LogKind.Error ? 3 : e.Kind == LogKind.Warning ? 4 : e.Kind == LogKind.Success ? 5
                      : e.Kind == LogKind.Heading ? 6 : e.Kind == LogKind.Muted ? 1 : 2;
            rtf.Append(@"\cf").Append(color).Append(e.Kind == LogKind.Heading ? @"\b " : " ");
            Escape(rtf, Display(e));
            if (e.Kind == LogKind.Heading) rtf.Append(@"\b0 ");
        }

        static void Escape(StringBuilder sb, string text)
        {
            foreach (char ch in text)
            {
                if (ch == '\\' || ch == '{' || ch == '}') sb.Append('\\').Append(ch);
                else if (ch == '\t') sb.Append(@"\tab ");
                else if (ch > 127) sb.Append(@"\u").Append((int)(short)ch).Append('?');
                else sb.Append(ch);
            }
        }

        void UpdateCount()
        {
            int errors = 0, warnings = 0;
            foreach (LogEntry e in entries)
            {
                if (e.Kind == LogKind.Error) errors++;
                else if (e.Kind == LogKind.Warning) warnings++;
            }
            List<string> parts = new List<string>();
            if (errors > 0) parts.Add(errors + (errors == 1 ? " Fehler" : " Fehler"));
            if (warnings > 0) parts.Add(warnings + (warnings == 1 ? " Hinweis" : " Hinweise"));
            countLabel.Text = string.Join(", ", parts.ToArray());
            countLabel.ForeColor = errors > 0 ? ErrorColor : warnings > 0 ? WarningColor : Theme.Muted;
        }

        public void Clear()
        {
            entries.Clear();
            box.Clear();
            UpdateCount();
        }

        void CopyVisible()
        {
            StringBuilder sb = new StringBuilder();
            foreach (LogEntry e in entries)
                if (IsShown(e)) sb.Append(e.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture)).Append("  ").Append(e.Text).Append("\r\n");
            if (sb.Length == 0) return;
            try { Clipboard.SetText(sb.ToString()); }
            catch { }
        }

        void OpenFile()
        {
            if (BeforeOpenFile != null) BeforeOpenFile();
            if (string.IsNullOrEmpty(LogFile) || !File.Exists(LogFile))
            {
                MessageBox.Show(FindForm(), "Es gibt noch keine Protokolldatei.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try { Process.Start(LogFile); }
            catch (Exception e)
            {
                MessageBox.Show(FindForm(), "Die Protokolldatei konnte nicht geöffnet werden:\n" + e.Message, App.Title,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Protokoll dieser Sitzung in eine selbst gewählte Datei speichern
        void SaveAs()
        {
            string text = SessionText != null ? SessionText() : null;
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show(FindForm(), "Das Protokoll ist noch leer.", App.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Protokoll speichern";
                dlg.Filter = "Textdatei|*.txt|Alle Dateien|*.*";
                dlg.FileName = "updater-protokoll-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".txt";
                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
                try { File.WriteAllText(dlg.FileName, text, new UTF8Encoding(true)); }
                catch (Exception e)
                {
                    MessageBox.Show(FindForm(), "Das Protokoll konnte nicht gespeichert werden:\n" + e.Message, App.Title,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        public void ScrollToBottom() { ScrollToEnd(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) searchTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    // Senkrechter Griff zwischen Inhalt und Protokoll. Ziehen ändert die Breite des Protokolls.
    public class SplitBar : Control
    {
        int startX, startWidth;
        bool dragging, hover;
        public Func<int> GetWidth;          // aktuelle Breite des Protokolls
        public Action<int> SetWidth;        // neue Breite (wird vom Hauptfenster begrenzt)
        public Action DragEnded;

        public SplitBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.VSplit;
            Width = Theme.S(8);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || GetWidth == null) return;
            dragging = true;
            startX = Cursor.Position.X;
            startWidth = GetWidth();
            Capture = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging || SetWidth == null) return;
            // Nach links ziehen = Protokoll breiter
            SetWidth(startWidth - (Cursor.Position.X - startX));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!dragging) return;
            dragging = false;
            Capture = false;
            Invalidate();
            if (DragEnded != null) DragEnded();
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Back);
            int x = Width / 2;
            using (Pen p = new Pen(hover || dragging ? Theme.Red : Theme.Line, Theme.S(2)))
                e.Graphics.DrawLine(p, x, Theme.S(8), x, Height - Theme.S(8));
        }
    }
}
