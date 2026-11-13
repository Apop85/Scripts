// Logo, Icon und Kopfbild im Stil des STEFFLIX-Logos (sflixLogo.svg), mit GDI+ gezeichnet.
// Dadurch bleibt alles bei jeder Bildschirmskalierung scharf.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace Sflix
{
    public static class Branding
    {
        public static readonly Color Background = Color.FromArgb(0x14, 0x14, 0x17);
        public static readonly Color Border = Color.FromArgb(0x2a, 0x2a, 0x30);
        public static readonly Color Red = Color.FromArgb(0xe8, 0x26, 0x1a);
        public static readonly Color DarkRed = Color.FromArgb(0xa8, 0x1a, 0x0f);

        // Verlauf wie im SVG: bis 30 % hellrot, ab 68 % dunkelrot (bezogen auf top..bottom).
        // Der Pinsel reicht weit über den Bereich hinaus, sonst wiederholt GDI+ den Verlauf
        // an den Rändern und es entstehen Streifen.
        static LinearGradientBrush RedBrush(float top, float bottom)
        {
            float span = bottom - top;
            float start = top - span, end = bottom + span;
            float total = end - start;
            float p1 = (top + 0.30f * span - start) / total;
            float p2 = (top + 0.68f * span - start) / total;
            LinearGradientBrush b = new LinearGradientBrush(new PointF(0, start), new PointF(0, end), Red, DarkRed);
            ColorBlend blend = new ColorBlend(4);
            blend.Colors = new Color[] { Red, Red, DarkRed, DarkRed };
            blend.Positions = new float[] { 0f, p1, p2, 1f };
            b.InterpolationColors = blend;
            return b;
        }

        // Schriften wie Bahnschrift haben überlappende Konturen: ohne Winding entstehen Löcher
        static GraphicsPath TextPath(string text, FontFamily family, FontStyle style, float size)
        {
            GraphicsPath p = new GraphicsPath(FillMode.Winding);
            p.AddString(text, family, (int)style, size, new PointF(0, 0), StringFormat.GenericTypographic);
            return p;
        }

        static GraphicsPath RoundedRect(RectangleF r, float radius)
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

        // Das Logo (512x512-Koordinaten des SVG) in das Quadrat bei (x, y) mit Kantenlänge size.
        public static void DrawLogo(Graphics g, float x, float y, float size, bool withTile)
        {
            float s = size / 512f;
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TranslateTransform(x, y);
            g.ScaleTransform(s, s);

            if (withTile)
            {
                using (GraphicsPath tile = RoundedRect(new RectangleF(8, 8, 496, 496), 112))
                {
                    using (SolidBrush bg = new SolidBrush(Background)) g.FillPath(bg, tile);
                    // Bei sehr kleinen Icons wäre der Rand nur Rauschen
                    if (size >= 32)
                        using (Pen border = new Pen(Border, 6)) g.DrawPath(border, tile);
                }
            }

            using (GraphicsPath sPath = new GraphicsPath())
            {
                sPath.AddBezier(362, 170, 334, 120, 176, 112, 166, 196);
                sPath.AddBezier(166, 196, 158, 244, 214, 252, 258, 262);
                sPath.AddBezier(258, 262, 304, 272, 354, 284, 350, 334);
                sPath.AddBezier(350, 334, 344, 414, 186, 414, 150, 346);
                using (LinearGradientBrush brush = RedBrush(112, 414))
                using (Pen pen = new Pen(brush, 84))
                {
                    pen.StartCap = LineCap.Flat;
                    pen.EndCap = LineCap.Flat;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawPath(pen, sPath);
                }
            }
            g.Restore(state);
        }

        public static Bitmap RenderLogo(int size)
        {
            Bitmap bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                DrawLogo(g, 0, 0, size, true);
            }
            return bmp;
        }

        static FontFamily TitleFamily()
        {
            string[] names = { "Bahnschrift SemiBold Condensed", "Bahnschrift Condensed", "Impact", "Arial Black" };
            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                foreach (string n in names)
                    foreach (FontFamily f in fonts.Families)
                        if (string.Equals(f.Name, n, StringComparison.OrdinalIgnoreCase)) return new FontFamily(f.Name);
            }
            return FontFamily.GenericSansSerif;
        }

        static FontFamily titleFamily;
        static FontFamily Family
        {
            get { if (titleFamily == null) titleFamily = TitleFamily(); return titleFamily; }
        }

        // Kopfbild: dunkle Fläche, Logo links, "STEFFLIX" als roter Schriftzug mit Verlauf,
        // darunter "UPDATER" gesperrt auf die Breite des Schriftzugs. Version rechts unten.
        public static void DrawBanner(Graphics g, Rectangle bounds, string version)
        {
            DrawBanner(g, bounds, version, "UPDATER");
        }

        public static void DrawBanner(Graphics g, Rectangle bounds, string version, string subtitle)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (SolidBrush bg = new SolidBrush(Background)) g.FillRectangle(bg, bounds);
            // feine rote Linie unten
            using (LinearGradientBrush line = new LinearGradientBrush(
                new Point(bounds.Left, 0), new Point(bounds.Right, 0), Color.FromArgb(0, Red), Color.FromArgb(0, Red)))
            {
                ColorBlend cb = new ColorBlend(3);
                cb.Colors = new Color[] { Color.FromArgb(0, Red), Color.FromArgb(200, Red), Color.FromArgb(0, Red) };
                cb.Positions = new float[] { 0f, 0.5f, 1f };
                line.InterpolationColors = cb;
                g.FillRectangle(line, bounds.Left, bounds.Bottom - 2, bounds.Width, 2);
            }

            float h = bounds.Height;
            float logoSize = h * 0.68f;
            float pad = h * 0.16f;

            // Titel messen, damit Logo + Schrift zusammen mittig stehen
            float titleSize = h * 0.50f;
            float subSize = h * 0.15f;
            RectangleF titleBox;
            using (GraphicsPath title = TextPath("STEFFLIX", Family, FontStyle.Bold, titleSize))
            {
                titleBox = title.GetBounds();
            }
            float gap = h * 0.14f;
            float subGap = h * 0.07f;
            float blockWidth = logoSize + gap + titleBox.Width;
            float left = bounds.Left + Math.Max(pad, (bounds.Width - blockWidth) / 2f);

            float textBlockHeight = titleBox.Height + subGap + subSize * 0.75f;
            float top = bounds.Top + (h - textBlockHeight) / 2f;

            DrawLogo(g, left, bounds.Top + (h - logoSize) / 2f, logoSize, true);

            float tx = left + logoSize + gap;
            using (GraphicsPath title = TextPath("STEFFLIX", Family, FontStyle.Bold, titleSize))
            {
                using (Matrix m = new Matrix())
                {
                    m.Translate(tx - titleBox.X, top - titleBox.Y);
                    title.Transform(m);
                }
                RectangleF b = title.GetBounds();
                // weicher Schatten
                using (Matrix shadow = new Matrix())
                using (GraphicsPath sh = (GraphicsPath)title.Clone())
                {
                    shadow.Translate(0, h * 0.025f);
                    sh.Transform(shadow);
                    using (SolidBrush sb = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillPath(sb, sh);
                }
                using (LinearGradientBrush brush = RedBrush(b.Top, b.Bottom)) g.FillPath(brush, title);

                // Untertitel gesperrt auf die Breite von "STEFFLIX"
                DrawSpaced(g, subtitle, b.Left, b.Bottom + subGap, b.Width, subSize, Color.FromArgb(0xd8, 0xd8, 0xdc));
            }

            if (!string.IsNullOrEmpty(version))
            {
                using (Font f = new Font("Segoe UI", Math.Max(7f, h * 0.075f), FontStyle.Regular, GraphicsUnit.Pixel))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(0x8a, 0x8a, 0x92)))
                {
                    SizeF sz = g.MeasureString(version, f);
                    g.DrawString(version, f, b, bounds.Right - sz.Width - h * 0.08f, bounds.Bottom - sz.Height - h * 0.08f);
                }
            }
        }

        static void DrawSpaced(Graphics g, string text, float x, float y, float width, float size, Color color)
        {
            using (GraphicsPath p = new GraphicsPath(FillMode.Winding))
            {
                float[] widths = new float[text.Length];
                float sum = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    using (GraphicsPath c = TextPath(text[i].ToString(), Family, FontStyle.Regular, size))
                    {
                        widths[i] = c.GetBounds().Width;
                        sum += widths[i];
                    }
                }
                float spacing = text.Length > 1 ? (width - sum) / (text.Length - 1) : 0;
                float cx = x;
                for (int i = 0; i < text.Length; i++)
                {
                    using (GraphicsPath c = TextPath(text[i].ToString(), Family, FontStyle.Regular, size))
                    {
                        RectangleF cb = c.GetBounds();
                        using (Matrix m = new Matrix())
                        {
                            m.Translate(cx - cb.X, y - cb.Y);
                            c.Transform(m);
                        }
                        p.AddPath(c, false);
                    }
                    cx += widths[i] + spacing;
                }
                using (SolidBrush b = new SolidBrush(color)) g.FillPath(b, p);
            }
        }

        public static Bitmap RenderBanner(int width, int height, string version)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp)) DrawBanner(g, new Rectangle(0, 0, width, height), version);
            return bmp;
        }

        // ICO-Datei mit PNG-Bildern in mehreren Grössen
        public static void WriteIcon(string path)
        {
            int[] sizes = { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
            byte[][] images = new byte[sizes.Length][];
            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap bmp = RenderLogo(sizes[i]))
                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    images[i] = ms.ToArray();
                }
            }
            using (FileStream fs = File.Create(path))
            using (BinaryWriter w = new BinaryWriter(fs))
            {
                w.Write((short)0);
                w.Write((short)1);
                w.Write((short)sizes.Length);
                int offset = 6 + 16 * sizes.Length;
                for (int i = 0; i < sizes.Length; i++)
                {
                    w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                    w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                    w.Write((byte)0);
                    w.Write((byte)0);
                    w.Write((short)1);
                    w.Write((short)32);
                    w.Write(images[i].Length);
                    w.Write(offset);
                    offset += images[i].Length;
                }
                foreach (byte[] img in images) w.Write(img);
            }
        }
    }
}
