// Eigene Steuerelemente im dunklen STEFFLIX-Stil.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sflix
{
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
        public static readonly Color RedHover = Color.FromArgb(0xf0, 0x2a, 0x34);
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
            return t;
        }

        public static Label MakeLabel(string text, bool muted)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.ForeColor = muted ? Muted : Text;
            l.BackColor = Color.Transparent;
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

    // Checkbox, die auch auf dunklem Hintergrund gut aussieht
    public class DarkCheckBox : CheckBox
    {
        public DarkCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            AutoSize = false;
            Cursor = Cursors.Hand;
        }

        public void FitText()
        {
            Size sz = TextRenderer.MeasureText(Text, Font);
            int box = BoxSize();
            Size = new Size(box + Theme.S(8) + sz.Width + Theme.S(6), Math.Max(box, sz.Height) + Theme.S(4));
        }

        // Die Schrift kommt oft erst vom übergeordneten Element: dann neu messen
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); FitText(); }
        protected override void OnParentFontChanged(EventArgs e) { base.OnParentFontChanged(e); FitText(); }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); FitText(); }

        int BoxSize() { return Math.Max(Theme.S(16), (int)(Font.Height * 0.95)); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Back);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int box = BoxSize();
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
