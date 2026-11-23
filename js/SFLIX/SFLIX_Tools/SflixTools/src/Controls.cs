// Eigene Steuerelemente im dunklen STEFFLIX-Stil.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sflix
{
    // Rückruf aus einem Hintergrund-Thread in den UI-Thread. BeginInvoke auf einem inzwischen
    // geschlossenen Fenster (oder ohne Handle) wirft InvalidOperationException; unbehandelt im
    // Thread-Pool beendet das den ganzen Prozess, also auch den Player.
    public static class Ui
    {
        // Liefert false, wenn das Steuerelement nicht mehr lebt (die Aktion entfällt dann). Ein Handle,
        // das gerade neu erzeugt wird (z.B. durch Opacity), wird in Hintergrund-Threads kurz abgewartet.
        public static bool Post(Control c, Action action)
        {
            if (c == null || action == null) return false;
            for (int attempt = 0; ; attempt++)
            {
                if (c.IsDisposed || c.Disposing) return false;
                if (c.IsHandleCreated)
                {
                    try
                    {
                        c.BeginInvoke((Action)delegate
                        {
                            if (c.IsDisposed || c.Disposing) return;
                            action();
                        });
                        return true;
                    }
                    catch (ObjectDisposedException) { return false; }
                    catch (InvalidOperationException) { }   // Handle zwischen Prüfung und Aufruf entfernt
                }
                // Im UI-Thread nicht warten (blockiert sonst das Fenster); nach etwa 2 s aufgeben
                if (!System.Threading.Thread.CurrentThread.IsBackground || attempt >= 40) return false;
                System.Threading.Thread.Sleep(50);
            }
        }
    }

    // Dunkles Windows-Design für ein Steuerelement (Bildlaufleisten, Markierung). Wird bei jedem neuen
    // Handle wiederholt. Auf älteren Windows ohne dunkles Design bleibt es beim hellen.
    public static class DarkWindow
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", EntryPoint = "#133")]
        static extern bool AllowDarkModeForWindow(IntPtr hwnd, bool allow);

        public static void Apply(Control c)
        {
            c.HandleCreated += delegate { Set(c); };
            if (c.IsHandleCreated) Set(c);
        }

        public static void Set(Control c)
        {
            try { AllowDarkModeForWindow(c.Handle, true); } catch { }
            try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { }
        }
    }

    // Auswahlliste mit dunklem Pfeil-Knopf. Mit FlatStyle.Flat zeichnet Windows Forms den Knopf hell;
    // nach jedem Zeichnen wird er dunkel übermalt (SFLIX-20261123-090).
    public class DarkCombo : ComboBox
    {
        public DarkCombo()
        {
            FlatStyle = FlatStyle.Flat;
            BackColor = Theme.Input;
            ForeColor = Theme.Text;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg != 0x000F || !IsHandleCreated) return;   // WM_PAINT
            try
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                {
                    int w = SystemInformation.VerticalScrollBarWidth;
                    Rectangle r = new Rectangle(Width - w - 1, 1, w, Height - 2);
                    using (SolidBrush b = new SolidBrush(BackColor)) g.FillRectangle(b, r);
                    int cx = r.Left + r.Width / 2, cy = r.Top + r.Height / 2, s = Math.Max(3, w / 4);
                    SmoothingMode old = g.SmoothingMode;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (SolidBrush b = new SolidBrush(Enabled ? Theme.Text : Theme.Muted))
                        g.FillPolygon(b, new Point[] { new Point(cx - s, cy - s / 2), new Point(cx + s + 1, cy - s / 2), new Point(cx, cy + s / 2 + 1) });
                    g.SmoothingMode = old;
                    using (Pen p = new Pen(Theme.Line)) g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                }
            }
            catch { }
        }
    }

    public static class Theme
    {
        public static readonly Color Back = Color.FromArgb(0x18, 0x18, 0x1c);
        public static readonly Color Card = Color.FromArgb(0x20, 0x20, 0x25);
        public static readonly Color Input = Color.FromArgb(0x2a, 0x2a, 0x31);
        public static readonly Color Line = Color.FromArgb(0x34, 0x34, 0x3c);
        public static readonly Color Text = Color.FromArgb(0xec, 0xec, 0xee);
        public static readonly Color Muted = Color.FromArgb(0x9c, 0x9c, 0xa6);
        public static readonly Color LogBack = Color.FromArgb(0x10, 0x10, 0x13);
        public static readonly Color Red = Branding.Red;
        public static readonly Color RedHover = Color.FromArgb(0xf2, 0x44, 0x30);
        public static readonly Color Notice = Color.FromArgb(0x3a, 0x2a, 0x08);

        public static float Scale = 1f;
        public static int S(int px) { return (int)Math.Round(px * Scale); }

        public static Button MakeButton(string text, bool primary)
        {
            Button b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.UseVisualStyleBackColor = false;
            b.BackColor = primary ? Red : Input;
            b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = primary ? Red : Line;
            b.FlatAppearance.MouseOverBackColor = primary ? RedHover : Line;
            b.FlatAppearance.MouseDownBackColor = primary ? Branding.DarkRed : Card;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(S(14), S(4), S(14), S(4));
            b.Cursor = Cursors.Hand;
            return b;
        }

        public static TextBox MakeTextBox()
        {
            TextBox t = new TextBox();
            t.BackColor = Input;
            t.ForeColor = Text;
            t.BorderStyle = BorderStyle.FixedSingle;
            DarkWindow.Apply(t);   // Bildlaufleisten mehrzeiliger Felder dunkel (SFLIX-20261123-090)
            return t;
        }

        public static Label MakeLabel(string text, bool muted)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = muted ? Muted : Text;
            l.BackColor = Color.Transparent;
            l.UseMnemonic = false;   // "&" anzeigen statt als Tastenkürzel deuten
            return l;
        }
    }

    // Blendet das Protokoll (eine Zeile im Haupt-Layout) über einen "Details"-Knopf ein und aus.
    // Standard: eingeklappt.
    public class LogToggle
    {
        readonly TableLayoutPanel root;
        readonly int row;
        readonly float percent;
        readonly TextBox log;
        readonly Button button;
        public bool Expanded { get; private set; }

        // Wird nach dem Umschalten aufgerufen, damit das Fenster seine Grösse anpassen kann
        public Action<bool> Changed;

        public LogToggle(TableLayoutPanel root, int row, float percent, TextBox log, Button button)
        {
            this.root = root;
            this.row = row;
            this.percent = percent;
            this.log = log;
            this.button = button;
            button.Click += delegate { Set(!Expanded); };
        }

        public void Set(bool expanded)
        {
            Expanded = expanded;
            root.SuspendLayout();
            log.Visible = expanded;
            root.RowStyles[row] = expanded ? new RowStyle(SizeType.Percent, percent) : new RowStyle(SizeType.Absolute, 0);
            button.Text = expanded ? "Details ▴" : "Details ▾";
            root.ResumeLayout(true);
            if (Changed != null) Changed(expanded);
            if (expanded)
            {
                log.SelectionStart = log.TextLength;
                log.ScrollToCaret();
            }
        }

        // Passt die Fensterhöhe an: eingeklappt so hoch wie der Inhalt, ausgeklappt plus Protokoll.
        // fitRow (optional) ist eine Zeile mit scrollbarem Inhalt, die eingeklappt auf ihre
        // natürliche Höhe schrumpft (scroller = deren Panel).
        public static void FitWindow(Form form, TableLayoutPanel root, bool expanded, int fitRow, float fitPercent,
                                     Panel scroller, int expandedLogHeight, int logRow)
        {
            if (form.WindowState != FormWindowState.Normal) return;
            Rectangle work = Screen.FromControl(form).WorkingArea;
            int maxClient = work.Height - (form.Height - form.ClientSize.Height) - 20;
            int minLog = Theme.S(140);

            // Natürliche Höhe ohne Protokoll messen
            root.SuspendLayout();
            if (fitRow >= 0)
            {
                root.RowStyles[fitRow] = new RowStyle(SizeType.AutoSize);
                scroller.AutoSize = true;
                scroller.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }
            root.RowStyles[logRow] = new RowStyle(SizeType.Absolute, 0);
            root.ResumeLayout(true);
            int natural = root.GetPreferredSize(new Size(root.Width, 0)).Height;

            // Protokoll bekommt, was der Bildschirm noch hergibt (höchstens expandedLogHeight)
            int logHeight = expanded ? Math.Min(expandedLogHeight, maxClient - natural) : 0;
            bool overflow = natural > maxClient || (expanded && logHeight < minLog);

            root.SuspendLayout();
            if (overflow && fitRow >= 0)
            {
                // Bildschirm zu niedrig: Aufgaben bekommen den Rest, mit Scrollleiste
                scroller.AutoSize = false;
                root.RowStyles[fitRow] = new RowStyle(SizeType.Percent, fitPercent);
                if (expanded) logHeight = minLog;
                root.RowStyles[logRow] = expanded ? new RowStyle(SizeType.Absolute, logHeight) : new RowStyle(SizeType.Absolute, 0);
            }
            else
            {
                // Aufgaben in natürlicher Höhe, das Protokoll füllt den Rest (auch beim Vergrössern)
                root.RowStyles[logRow] = expanded ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, 0);
            }
            root.ResumeLayout(true);
            form.ClientSize = new Size(form.ClientSize.Width, Math.Min(natural + logHeight, maxClient));

            // Eingeklappt: auf die tatsächlich genutzte Höhe kürzen (GetPreferredSize rechnet
            // bei umbrechenden Texten manchmal etwas zu grosszügig)
            if (!expanded && !overflow)
            {
                root.PerformLayout();
                int used = 0;
                foreach (Control c in root.Controls)
                    if (c.Visible) used = Math.Max(used, c.Bottom + c.Margin.Bottom);
                used += root.Padding.Bottom;
                if (used > 0 && used < form.ClientSize.Height)
                    form.ClientSize = new Size(form.ClientSize.Width, used);
            }

            // Ragt das Fenster nach dem Vergrössern unten aus dem Bildschirm, nach oben schieben
            if (form.Bottom > work.Bottom) form.Top = Math.Max(work.Top, work.Bottom - form.Height);
        }

        public static Button MakeButton()
        {
            Button b = Theme.MakeButton("Details ▾", false);
            b.Padding = new Padding(Theme.S(10), 0, Theme.S(10), 0);
            b.Margin = new Padding(Theme.S(8), Theme.S(4), 0, 0);
            b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            return b;
        }
    }

    // Knopfzeile, die umbricht und ihre Höhe selbst aus der Breite berechnet; ein optionales
    // Element (RightControl) steht fest rechts in der ersten Zeile. Ersetzt verschachtelte AutoSize-
    // Panels, deren Höhe bei grosser Skalierung zu gross berechnet wurde.
    public class WrapRow : Panel
    {
        Control right;
        bool arranging;

        public WrapRow()
        {
            AutoSize = false;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }

        public Control RightControl
        {
            get { return right; }
            set { right = value; if (value != null && !Controls.Contains(value)) Controls.Add(value); PerformLayout(); }
        }

        static Size Outer(Control c)
        {
            Size s = c.AutoSize ? c.GetPreferredSize(Size.Empty) : c.Size;
            return new Size(s.Width + c.Margin.Horizontal, s.Height + c.Margin.Vertical);
        }

        // Ordnet die Elemente für die Breite an (place = false: nur messen) und gibt die Höhe zurück
        int Arrange(int width, bool place)
        {
            int rightWidth = 0, rightHeight = 0;
            if (right != null && right.Visible)
            {
                Size r = Outer(right);
                rightWidth = r.Width;
                rightHeight = r.Height;
            }
            int x = 0, y = 0, rowHeight = 0;
            foreach (Control c in Controls)
            {
                if (c == right || !c.Visible) continue;
                Size o = Outer(c);
                // Neben dem rechten Element nur die Restbreite, darunter die ganze Breite
                int avail = Math.Max(1, y < rightHeight ? width - rightWidth : width);
                if (x > 0 && x + o.Width > avail)
                {
                    y += rowHeight;
                    x = 0;
                    rowHeight = 0;
                }
                if (place) c.SetBounds(x + c.Margin.Left, y + c.Margin.Top, o.Width - c.Margin.Horizontal, o.Height - c.Margin.Vertical);
                x += o.Width;
                rowHeight = Math.Max(rowHeight, o.Height);
            }
            if (place && rightWidth > 0)
            {
                Size r = Outer(right);
                right.SetBounds(width - r.Width + right.Margin.Left, right.Margin.Top, r.Width - right.Margin.Horizontal, r.Height - right.Margin.Vertical);
            }
            int total = y + rowHeight;
            return Math.Max(total, rightHeight);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int w = proposedSize.Width > 0 && proposedSize.Width < int.MaxValue / 2 ? proposedSize.Width : Width;
            return new Size(w, Arrange(w, false));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (arranging) return;
            arranging = true;
            try
            {
                int h = Arrange(Width, true);
                if (Height != h) Height = h;
            }
            finally { arranging = false; }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            PerformLayout();
        }
    }

    // Kopfbild
    public class BannerPanel : Control
    {
        public string Version;
        public string Subtitle = "UPDATER";

        public BannerPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Branding.DrawBanner(e.Graphics, ClientRectangle, Version, Subtitle);
        }
    }

    // Registerkarten im Stil von Aktenreitern: Symbol und Text, die aktive Karte geht nahtlos
    // in den Inhalt darunter über.
    public class TabStrip : Control
    {
        readonly List<string> titles = new List<string>();
        readonly List<Glyph> icons = new List<Glyph>();
        readonly List<Rectangle> bounds = new List<Rectangle>();
        int selected, hover = -1;

        // Wird nach einem Wechsel aufgerufen (Index der neuen Karte)
        public Action<int> Changed;

        // Aussehen: oben unter dem Kopfbild gross, unter den Kacheln kleiner und heller
        public Color StripBack = Branding.Background;
        public Color ActiveFill = Theme.Back;
        public Color InactiveFill = Color.FromArgb(0x1c, 0x1c, 0x21);
        public Color HoverFill = Color.FromArgb(0x24, 0x24, 0x2a);
        public int IconSize = 18, Pad = 16, TopGap = 8, Inset = 16;

        public TabStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Font = new Font("Segoe UI Semibold", 10f);
            Cursor = Cursors.Hand;
            TabStop = true;
        }

        public void Add(string title, Glyph icon)
        {
            titles.Add(title);
            icons.Add(icon);
            Measure();
        }

        public int Count { get { return titles.Count; } }

        // Titel und Symbol eines Reiters ändern (z.B. "Lizenz" -> "Feedback")
        public void SetTab(int index, string title, Glyph icon)
        {
            if (index < 0 || index >= titles.Count) return;
            titles[index] = title;
            icons[index] = icon;
            Measure();
        }

        // Alle Reiter ersetzen, ohne Changed auszulösen
        public void SetTabs(IList<string> newTitles, IList<Glyph> newIcons, int select)
        {
            titles.Clear(); titles.AddRange(newTitles);
            icons.Clear(); icons.AddRange(newIcons);
            selected = Math.Max(0, Math.Min(select, titles.Count - 1));
            hover = -1;
            Measure();
        }

        public int Selected
        {
            get { return selected; }
            set
            {
                if (value < 0 || value >= titles.Count || value == selected) return;
                selected = value;
                Invalidate();
                if (Changed != null) Changed(value);
            }
        }

        void Measure()
        {
            bounds.Clear();
            int x = Theme.S(Inset);
            int top = Theme.S(TopGap);
            int iconSize = Theme.S(IconSize);
            foreach (string t in titles)
            {
                int w = Theme.S(Pad) + iconSize + Theme.S(8) + TextRenderer.MeasureText(t, Font).Width + Theme.S(Pad);
                bounds.Add(new Rectangle(x, top, w, Height - top));
                x += w + Theme.S(4);
            }
            Invalidate();
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); Measure(); }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); Measure(); }

        int HitTest(Point p)
        {
            for (int i = 0; i < bounds.Count; i++) if (bounds[i].Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
            Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int h = HitTest(e.Location);
            if (h >= 0 && e.Button == MouseButtons.Left) { Focus(); Selected = h; }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left) Selected = Math.Max(0, selected - 1);
            if (e.KeyCode == Keys.Right) Selected = Math.Min(titles.Count - 1, selected + 1);
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(StripBack);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int bottom = Height - 1;
            int radius = Theme.S(7);
            for (int i = 0; i < bounds.Count; i++)
            {
                Rectangle r = bounds[i];
                bool active = i == selected;
                Color fill = active ? ActiveFill : (i == hover ? HoverFill : InactiveFill);
                using (GraphicsPath p = TabPath(r, radius))
                {
                    using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                    using (Pen pen = new Pen(active ? Theme.Line : Color.FromArgb(0x2a, 0x2a, 0x30), 1f)) g.DrawPath(pen, p);
                }
                if (active)
                {
                    // roter Streifen oben auf dem aktiven Reiter
                    using (SolidBrush red = new SolidBrush(Theme.Red))
                        g.FillRectangle(red, r.Left + radius, r.Top, r.Width - 2 * radius, Theme.S(3));
                }
                Color fore = active ? Theme.Text : (i == hover ? Color.FromArgb(0xd0, 0xd0, 0xd6) : Theme.Muted);
                int iconSize = Theme.S(IconSize);
                Rectangle ir = new Rectangle(r.Left + Theme.S(Pad), r.Top + (r.Height - iconSize) / 2 + Theme.S(1), iconSize, iconSize);
                Glyphs.Draw(g, icons[i], ir, active ? Theme.Red : fore, fill);
                Rectangle tr = new Rectangle(ir.Right + Theme.S(8), r.Top + Theme.S(1), r.Right - ir.Right - Theme.S(8), r.Height);
                TextRenderer.DrawText(g, titles[i], Font, tr, fore, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                if (active && Focused && ShowFocusCues)
                {
                    Rectangle fr = Rectangle.Inflate(r, -Theme.S(4), -Theme.S(6));
                    fr.Y += Theme.S(2);
                    ControlPaint.DrawFocusRectangle(g, fr, fore, fill);
                }
            }
            // Grundlinie, unter dem aktiven Reiter offen
            using (Pen line = new Pen(Theme.Line, 1f))
            {
                if (selected < bounds.Count)
                {
                    Rectangle a = bounds[selected];
                    g.DrawLine(line, 0, bottom, a.Left, bottom);
                    g.DrawLine(line, a.Right, bottom, Width, bottom);
                }
                else g.DrawLine(line, 0, bottom, Width, bottom);
            }
        }

        static GraphicsPath TabPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddLine(r.Left, r.Bottom + 1, r.Left, r.Top + radius);
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddLine(r.Right, r.Top + radius, r.Right, r.Bottom + 1);
            return p;
        }

    }

    public enum Glyph { Up, Import, Tools, Data, Photo, Merge, Convert, Trash, Settings, Rename, Feedback, Key, Help, Inspect, Duplicate, Download }

    // Einfache Strichsymbole im Stil der Registerkarten
    public static class Glyphs
    {
        static PointF P(float s, float x, float y) { return new PointF(s * x, s * y); }

        public static void Draw(Graphics g, Glyph icon, Rectangle r, Color color, Color back)
        {
            float s = r.Width;
            float cx = r.Left + s / 2f, cy = r.Top + s / 2f;
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(cx, cy);
            using (Pen pen = new Pen(color, Math.Max(1.6f, s * 0.11f)))
            using (SolidBrush brush = new SolidBrush(color))
            using (SolidBrush bg = new SolidBrush(back))
            {
                pen.StartCap = pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                switch (icon)
                {
                    case Glyph.Up:
                        g.DrawLine(pen, 0, s * 0.44f, 0, -s * 0.38f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.32f, -0.06f), P(s, 0, -0.40f), P(s, 0.32f, -0.06f) });
                        break;
                    case Glyph.Import:
                        g.DrawLines(pen, new PointF[] { P(s, -0.44f, 0.06f), P(s, -0.44f, 0.42f), P(s, 0.44f, 0.42f), P(s, 0.44f, 0.06f) });
                        g.DrawLine(pen, 0, -s * 0.46f, 0, s * 0.16f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.22f, -0.06f), P(s, 0, 0.17f), P(s, 0.22f, -0.06f) });
                        break;
                    case Glyph.Tools:
                        // Gabelschlüssel, schräg gestellt: Kopf oben rechts, Griff unten links
                        g.RotateTransform(45f);
                        g.FillEllipse(brush, -s * 0.24f, -s * 0.52f, s * 0.48f, s * 0.48f);
                        using (Pen handle = new Pen(color, s * 0.2f))
                        {
                            handle.StartCap = handle.EndCap = LineCap.Round;
                            g.DrawLine(handle, 0, -s * 0.2f, 0, s * 0.44f);
                        }
                        // Maulöffnung oben
                        g.FillRectangle(bg, -s * 0.085f, -s * 0.62f, s * 0.17f, s * 0.3f);
                        g.FillEllipse(bg, -s * 0.085f, -s * 0.39f, s * 0.17f, s * 0.14f);
                        break;
                    case Glyph.Data:
                        // Datenbank: drei Scheiben übereinander
                        g.DrawEllipse(pen, -s * 0.38f, -s * 0.46f, s * 0.76f, s * 0.26f);
                        g.DrawLine(pen, -s * 0.38f, -s * 0.33f, -s * 0.38f, s * 0.33f);
                        g.DrawLine(pen, s * 0.38f, -s * 0.33f, s * 0.38f, s * 0.33f);
                        g.DrawArc(pen, -s * 0.38f, -s * 0.13f, s * 0.76f, s * 0.26f, 0, 180);
                        g.DrawArc(pen, -s * 0.38f, s * 0.20f, s * 0.76f, s * 0.26f, 0, 180);
                        break;
                    case Glyph.Photo:
                        using (GraphicsPath frame = RoundRect(new RectangleF(-s * 0.44f, -s * 0.36f, s * 0.88f, s * 0.72f), s * 0.12f))
                            g.DrawPath(pen, frame);
                        g.DrawEllipse(pen, s * 0.08f, -s * 0.20f, s * 0.16f, s * 0.16f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.40f, 0.28f), P(s, -0.14f, -0.02f), P(s, 0.10f, 0.22f), P(s, 0.22f, 0.10f), P(s, 0.40f, 0.28f) });
                        break;
                    case Glyph.Merge:
                        g.DrawLine(pen, -s * 0.42f, -s * 0.34f, -s * 0.06f, 0);
                        g.DrawLine(pen, -s * 0.42f, s * 0.34f, -s * 0.06f, 0);
                        g.DrawLine(pen, -s * 0.06f, 0, s * 0.42f, 0);
                        g.DrawLines(pen, new PointF[] { P(s, 0.22f, -0.18f), P(s, 0.42f, 0), P(s, 0.22f, 0.18f) });
                        break;
                    case Glyph.Convert:
                        g.DrawLine(pen, -s * 0.40f, -s * 0.18f, s * 0.40f, -s * 0.18f);
                        g.DrawLines(pen, new PointF[] { P(s, 0.22f, -0.36f), P(s, 0.40f, -0.18f), P(s, 0.22f, 0) });
                        g.DrawLine(pen, s * 0.40f, s * 0.20f, -s * 0.40f, s * 0.20f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.22f, 0.02f), P(s, -0.40f, 0.20f), P(s, -0.22f, 0.38f) });
                        break;
                    case Glyph.Trash:
                        g.DrawLine(pen, -s * 0.42f, -s * 0.28f, s * 0.42f, -s * 0.28f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.14f, -0.28f), P(s, -0.10f, -0.44f), P(s, 0.10f, -0.44f), P(s, 0.14f, -0.28f) });
                        g.DrawLines(pen, new PointF[] { P(s, -0.30f, -0.16f), P(s, -0.24f, 0.44f), P(s, 0.24f, 0.44f), P(s, 0.30f, -0.16f) });
                        g.DrawLine(pen, -s * 0.08f, -s * 0.04f, -s * 0.07f, s * 0.30f);
                        g.DrawLine(pen, s * 0.08f, -s * 0.04f, s * 0.07f, s * 0.30f);
                        break;
                    case Glyph.Rename:
                        // Textzeilen mit Stift: Namen bearbeiten
                        g.DrawLine(pen, -s * 0.42f, -s * 0.30f, s * 0.06f, -s * 0.30f);
                        g.DrawLine(pen, -s * 0.42f, -s * 0.04f, -s * 0.10f, -s * 0.04f);
                        g.DrawLine(pen, -s * 0.42f, s * 0.22f, -s * 0.24f, s * 0.22f);
                        using (Pen pencil = new Pen(color, s * 0.17f))
                        {
                            pencil.StartCap = LineCap.Triangle;
                            pencil.EndCap = LineCap.Round;
                            g.DrawLine(pencil, -s * 0.06f, s * 0.36f, s * 0.34f, -s * 0.04f);
                        }
                        break;
                    case Glyph.Settings:
                        // Zahnrad: Ring mit acht Zähnen
                        using (Pen teeth = new Pen(color, s * 0.16f))
                        {
                            teeth.StartCap = teeth.EndCap = LineCap.Round;
                            for (int i = 0; i < 8; i++)
                            {
                                double a = i * Math.PI / 4;
                                g.DrawLine(teeth, (float)(Math.Cos(a) * s * 0.30f), (float)(Math.Sin(a) * s * 0.30f),
                                                  (float)(Math.Cos(a) * s * 0.40f), (float)(Math.Sin(a) * s * 0.40f));
                            }
                        }
                        g.FillEllipse(brush, -s * 0.33f, -s * 0.33f, s * 0.66f, s * 0.66f);
                        g.FillEllipse(bg, -s * 0.13f, -s * 0.13f, s * 0.26f, s * 0.26f);
                        break;
                    case Glyph.Feedback:
                        // Sprechblase mit drei Punkten
                        using (GraphicsPath bubble = RoundRect(new RectangleF(-s * 0.44f, -s * 0.38f, s * 0.88f, s * 0.62f), s * 0.14f))
                            g.DrawPath(pen, bubble);
                        g.DrawLines(pen, new PointF[] { P(s, -0.20f, 0.24f), P(s, -0.28f, 0.44f), P(s, 0.02f, 0.24f) });
                        for (int i = -1; i <= 1; i++) g.FillEllipse(brush, s * (i * 0.2f - 0.05f), -s * 0.12f, s * 0.1f, s * 0.1f);
                        break;
                    case Glyph.Key:
                        // Schlüssel: Ring links, Bart rechts
                        g.DrawEllipse(pen, -s * 0.44f, -s * 0.20f, s * 0.36f, s * 0.36f);
                        g.DrawLine(pen, -s * 0.08f, -s * 0.02f, s * 0.44f, -s * 0.02f);
                        g.DrawLine(pen, s * 0.24f, -s * 0.02f, s * 0.24f, s * 0.20f);
                        g.DrawLine(pen, s * 0.38f, -s * 0.02f, s * 0.38f, s * 0.16f);
                        break;
                    case Glyph.Help:
                        // Fragezeichen im Kreis
                        g.DrawEllipse(pen, -s * 0.44f, -s * 0.44f, s * 0.88f, s * 0.88f);
                        g.DrawArc(pen, -s * 0.15f, -s * 0.28f, s * 0.30f, s * 0.26f, 180, 230);
                        g.DrawLine(pen, s * 0.02f, s * 0.0f, 0, s * 0.10f);
                        g.FillEllipse(brush, -s * 0.06f, s * 0.20f, s * 0.12f, s * 0.12f);
                        break;
                    case Glyph.Inspect:
                        // Lupe mit Haken
                        g.DrawEllipse(pen, -s * 0.42f, -s * 0.42f, s * 0.62f, s * 0.62f);
                        g.DrawLine(pen, s * 0.14f, s * 0.14f, s * 0.42f, s * 0.42f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.25f, -0.11f), P(s, -0.13f, 0.01f), P(s, 0.05f, -0.21f) });
                        break;
                    case Glyph.Download:
                        // Pfeil nach unten auf eine Linie (Importer: Pfeil in eine Ablage)
                        g.DrawLine(pen, 0, -s * 0.46f, 0, s * 0.2f);
                        g.DrawLines(pen, new PointF[] { P(s, -0.24f, -0.04f), P(s, 0, 0.2f), P(s, 0.24f, -0.04f) });
                        g.DrawLine(pen, -s * 0.4f, s * 0.42f, s * 0.4f, s * 0.42f);
                        break;
                    case Glyph.Duplicate:
                        // Zwei überlappende Blätter
                        using (GraphicsPath back1 = RoundRect(new RectangleF(-s * 0.42f, -s * 0.42f, s * 0.56f, s * 0.62f), s * 0.08f))
                            g.DrawPath(pen, back1);
                        using (GraphicsPath front = RoundRect(new RectangleF(-s * 0.14f, -s * 0.20f, s * 0.56f, s * 0.62f), s * 0.08f))
                        {
                            g.FillPath(bg, front);
                            g.DrawPath(pen, front);
                        }
                        break;
                }
            }
            g.Restore(state);
        }

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Kachel zum Antippen: Symbol, Titel, kurze Beschreibung. Gewählt: roter Rand und Haken.
    public class TileButton : Control
    {
        public Glyph Icon;
        public string Title = "", Subtitle = "";
        bool isChecked, hover, compact;

        // Kompakt: Symbol links neben Titel und Untertitel (niedriger, z.B. bei den Tools)
        public bool Compact
        {
            get { return compact; }
            set { compact = value; Height = Theme.S(value ? 70 : 112); Invalidate(); }
        }
        public event EventHandler CheckedChanged;
        readonly Font subFont = new Font("Segoe UI", 9f);
        readonly Font compactTitleFont = new Font("Segoe UI Semibold", 10f);
        readonly Font compactSubFont = new Font("Segoe UI", 8.5f);

        public TileButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);
            Font = new Font("Segoe UI Semibold", 10.5f);
            Cursor = Cursors.Hand;
            TabStop = true;
            Height = Theme.S(112);
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            Focus();
            Checked = !Checked;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (Enabled && (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)) { Checked = !Checked; e.Handled = true; }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = Theme.Back;
            for (Control p = Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) { back = p.BackColor; break; }
            g.Clear(back);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool hot = hover && Enabled;
            Color fill = isChecked ? Color.FromArgb(0x2a, 0x1f, 0x23) : (hot ? Color.FromArgb(0x26, 0x26, 0x2c) : Theme.Card);
            int bw = Theme.S(2);
            RectangleF r = new RectangleF(bw / 2f, bw / 2f, Width - bw - 1, Height - bw - 1);
            using (GraphicsPath p = Glyphs.RoundRect(r, Theme.S(10)))
            {
                using (SolidBrush b = new SolidBrush(fill)) g.FillPath(b, p);
                Color border = isChecked ? Theme.Red : (hot ? Theme.Line : (Focused && ShowFocusCues ? Theme.Line : fill));
                using (Pen pen = new Pen(border, bw)) g.DrawPath(pen, p);
            }

            // Symbol in einem abgerundeten Feld
            int box = Theme.S(38);
            Rectangle ib = compact ? new Rectangle(Theme.S(14), (Height - box) / 2, box, box)
                                   : new Rectangle(Theme.S(14), Theme.S(14), box, box);
            Color iconBack = isChecked ? Color.FromArgb(0x3e, 0x18, 0x1d) : Theme.Input;
            using (GraphicsPath p = Glyphs.RoundRect(ib, Theme.S(8)))
            using (SolidBrush b = new SolidBrush(iconBack)) g.FillPath(b, p);
            int gs = Theme.S(20);
            Color iconColor = !Enabled ? Theme.Line : (isChecked ? Color.FromArgb(0xff, 0x5a, 0x45) : Theme.Muted);
            Glyphs.Draw(g, Icon, new Rectangle(ib.Left + (box - gs) / 2, ib.Top + (box - gs) / 2, gs, gs), iconColor, iconBack);

            // Haken oben rechts
            if (isChecked)
            {
                // Kompakt: kleines Abzeichen auf der Ecke des Symbols, damit der Text die ganze Breite hat
                int d = Theme.S(compact ? 18 : 22);
                Rectangle cb = compact ? new Rectangle(ib.Right - d + Theme.S(6), ib.Top - Theme.S(6), d, d)
                                       : new Rectangle(Width - Theme.S(12) - d, Theme.S(12), d, d);
                using (SolidBrush b = new SolidBrush(Theme.Red)) g.FillEllipse(b, cb);
                using (Pen pen = new Pen(Color.White, Math.Max(2f, d / 9f)))
                {
                    pen.StartCap = pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, new PointF[] {
                        new PointF(cb.Left + d * 0.28f, cb.Top + d * 0.52f),
                        new PointF(cb.Left + d * 0.44f, cb.Top + d * 0.68f),
                        new PointF(cb.Left + d * 0.74f, cb.Top + d * 0.34f) });
                }
            }

            TextFormatFlags f = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix |
                                TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            if (compact)
            {
                // Titel und Untertitel rechts neben dem Symbol, zusammen vertikal zentriert
                int tx = ib.Right + Theme.S(12), tw = Width - tx - Theme.S(10);
                int top = (Height - Theme.S(42)) / 2;
                TextRenderer.DrawText(g, Title, compactTitleFont, new Rectangle(tx, top, tw, Theme.S(22)), Enabled ? Theme.Text : Theme.Muted, f);
                TextRenderer.DrawText(g, Subtitle, compactSubFont, new Rectangle(tx, top + Theme.S(23), tw, Theme.S(19)), Theme.Muted, f);
            }
            else
            {
                int tx = Theme.S(14), tw = Width - 2 * tx;
                TextRenderer.DrawText(g, Title, Font, new Rectangle(tx, ib.Bottom + Theme.S(10), tw, Theme.S(24)),
                                      Enabled ? Theme.Text : Theme.Muted, f);
                TextRenderer.DrawText(g, Subtitle, subFont, new Rectangle(tx, ib.Bottom + Theme.S(34), tw, Theme.S(20)), Theme.Muted, f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { subFont.Dispose(); compactTitleFont.Dispose(); compactSubFont.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // Checkbox, die auch auf dunklem Hintergrund gut aussieht. Mit Switch = true als Schiebeschalter.
    public class DarkCheckBox : CheckBox
    {
        bool isSwitch;

        public DarkCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;
            Cursor = Cursors.Hand;
        }

        public bool Switch
        {
            get { return isSwitch; }
            set { isSwitch = value; FitText(); Invalidate(); }
        }

        int MarkWidth() { return isSwitch ? (int)(BoxSize() * 1.75) : BoxSize(); }

        public void FitText()
        {
            Size sz = TextRenderer.MeasureText(Text, Font);
            int box = BoxSize();
            Size = new Size(MarkWidth() + Theme.S(10) + sz.Width + Theme.S(6), Math.Max(box, sz.Height) + Theme.S(6));
        }

        // Die Schrift kommt oft erst vom übergeordneten Element: dann neu messen
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); FitText(); }
        protected override void OnParentFontChanged(EventArgs e) { base.OnParentFontChanged(e); FitText(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); FitText(); }

        int BoxSize() { return Math.Max(Theme.S(16), (int)(Font.Height * 0.95)); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // Hintergrund der nächsten nicht durchsichtigen Fläche (z.B. der Karte)
            Color back = Theme.Back;
            for (Control p = Parent; p != null; p = p.Parent)
                if (p.BackColor.A == 255) { back = p.BackColor; break; }
            g.Clear(back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int box = BoxSize();
            if (isSwitch)
            {
                // Schiebeschalter: Bahn rot, wenn an; Knopf rechts
                int w = MarkWidth(), h = (int)(box * 0.95);
                RectangleF track = new RectangleF(1, (Height - h) / 2f, w - 2, h);
                Color trackColor = Checked ? (Enabled ? Theme.Red : Branding.DarkRed) : Theme.Line;
                using (GraphicsPath p = Glyphs.RoundRect(track, h / 2f))
                using (SolidBrush b = new SolidBrush(trackColor)) g.FillPath(b, p);
                float knob = h - Theme.S(6);
                float kx = Checked ? track.Right - Theme.S(3) - knob : track.Left + Theme.S(3);
                using (SolidBrush b = new SolidBrush(Checked ? Color.White : Theme.Muted))
                    g.FillEllipse(b, kx, track.Top + Theme.S(3), knob, knob);
                if (Focused && ShowFocusCues)
                    using (Pen pen = new Pen(Theme.Muted, 1f))
                    using (GraphicsPath p = Glyphs.RoundRect(RectangleF.Inflate(track, 2, 2), h / 2f + 2)) g.DrawPath(pen, p);
                Rectangle tr = new Rectangle(w + Theme.S(10), 0, Width - w - Theme.S(10), Height);
                TextRenderer.DrawText(g, Text, Font, tr, Enabled ? ForeColor : Theme.Muted,
                                      TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                return;
            }
            Rectangle r = new Rectangle(1, (Height - box) / 2, box - 2, box - 2);
            using (GraphicsPath p = Rounded(r, Theme.S(3)))
            {
                if (Checked)
                {
                    using (SolidBrush b = new SolidBrush(Theme.Red)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Color.White, Math.Max(2f, box / 8f)))
                    {
                        pen.StartCap = pen.EndCap = LineCap.Round;
                        g.DrawLines(pen, new PointF[] {
                            new PointF(r.Left + r.Width * 0.22f, r.Top + r.Height * 0.52f),
                            new PointF(r.Left + r.Width * 0.42f, r.Top + r.Height * 0.72f),
                            new PointF(r.Left + r.Width * 0.78f, r.Top + r.Height * 0.30f) });
                    }
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Theme.Input)) g.FillPath(b, p);
                    using (Pen pen = new Pen(Focused ? Theme.Muted : Theme.Line, 1.5f)) g.DrawPath(pen, p);
                }
            }
            Rectangle textRect = new Rectangle(box + Theme.S(8), 0, Width - box - Theme.S(8), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, Enabled ? ForeColor : Theme.Muted,
                                  TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
        }

        static GraphicsPath Rounded(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    // Fortschrittsbalken in STEFFLIX-Rot; Wert < 0 = unbestimmt (laufender Balken)
    public class DarkProgress : Control
    {
        double value;
        float phase;
        readonly Timer timer = new Timer();

        public DarkProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            timer.Interval = 30;
            timer.Tick += delegate { phase += 0.012f; if (phase > 1.3f) phase = -0.3f; Invalidate(); };
        }

        public double Value
        {
            get { return value; }
            set
            {
                this.value = value;
                timer.Enabled = value < 0 && Visible;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Input);
            if (value < 0)
            {
                int w = Width / 4;
                int x = (int)(phase * Width) - w / 2;
                using (SolidBrush b = new SolidBrush(Theme.Red)) g.FillRectangle(b, x, 0, w, Height);
            }
            else if (value > 0)
            {
                int w = (int)(Math.Min(1, value) * Width);
                using (LinearGradientBrush b = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, w), Height), Branding.DarkRed, Theme.Red, 0f))
                    g.FillRectangle(b, 0, 0, w, Height);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
