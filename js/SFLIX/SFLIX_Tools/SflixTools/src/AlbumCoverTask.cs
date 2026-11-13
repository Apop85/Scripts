// Albumbilder für Musik (seit 3.4.3, Teil von "Vorschaubilder" mit "für Musik").
//
// Ein Album ist ein Ordner, dessen Musik und Videos zu mindestens 90 % Musik sind und für den es
// im Ordner darüber noch kein Vorschaubild gibt (Preview\<Album>.jpg). Mögliche Bilder: alle Bilddateien
// im Album und eine Ebene darunter (beliebige Namen, auch TIFF, HEIC, AVIF …), dazu das Cover aus der
// ersten Musikdatei (sonst aus zwei zufälligen anderen). Im Fenster wählst du je Album ein Bild; übrige
// Bilder können nach __gelöscht. Das Original kommt nach Preview\__original\<Album>.<Endung>, das
// Vorschaubild (480×270) zeigt das ganze Cover, links und rechts unscharf aufgefüllt.
// Ohne Oberfläche: nur Alben mit genau einem möglichen Bild, nie wird etwas gelöscht.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public class AlbumCandidate
    {
        public string AlbumDir;      // Ordner des Albums
        public string Name;          // Ordnername = Name des Vorschaubilds
        public string PreviewDir;    // Preview-Ordner im Ordner darüber
        public List<string> Images = new List<string>();   // Bilddateien im Album und eine Ebene darunter
        public string Embedded;      // Cover aus einer Musikdatei (JPG im Temp-Ordner) oder null
        public string EmbeddedFrom;  // Musikdatei, aus der das Cover stammt
        public string Chosen;        // gewähltes Bild (ein Eintrag aus Images oder Embedded), null = keins
        public bool Include;         // übernehmen
        public bool DeleteRest;      // übrige Bilder nach __gelöscht

        public List<string> Sources
        {
            get
            {
                List<string> all = new List<string>(Images);
                if (Embedded != null) all.Add(Embedded);
                return all;
            }
        }
    }

    public static class AlbumCovers
    {
        public static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".heic", ".heif", ".avif"
        };
        // Diese Formate liest Windows (GDI+) direkt, die übrigen gehen über ffmpeg
        static readonly HashSet<string> GdiFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".jpg", ".jpeg", ".jfif", ".png", ".gif", ".bmp", ".tif", ".tiff"
        };
        const double MusicShare = 0.9;
        const int Width = 480, Height = 270;
        const string DeletedFolderName = "__gelöscht";

        public static void Run(TaskContext ctx, string root)
        {
            ctx.Progress(-1, "Suche Musikalben ohne Vorschaubild ...");
            List<AlbumCandidate> albums = new List<AlbumCandidate>();
            Find(ctx, root, albums);
            if (albums.Count == 0)
            {
                ctx.Log("Musik: keine Alben ohne Vorschaubild gefunden.");
                return;
            }
            ctx.Log("Musik: " + albums.Count + " Album/Alben ohne Vorschaubild gefunden, suche Bilder ...");

            // Cover aus den Musikdateien (ffmpeg), parallel
            int done = 0;
            Workers.ForEach(albums, Workers.Degree(ctx, 2), ctx.Token, delegate(int i, AlbumCandidate a)
            {
                ExtractEmbedded(ctx, a);
                int n = System.Threading.Interlocked.Increment(ref done);
                ctx.Progress((double)n / albums.Count, "Albumcover " + n + " / " + albums.Count);
            });

            List<AlbumCandidate> withImages = albums.FindAll(delegate(AlbumCandidate a) { return a.Sources.Count > 0; });
            foreach (AlbumCandidate a in albums)
                if (a.Sources.Count == 0) ctx.Log("  KEIN BILD  " + a.AlbumDir);
            // Vorschlag: genau ein mögliches Bild
            foreach (AlbumCandidate a in withImages)
            {
                if (a.Sources.Count == 1) { a.Chosen = a.Sources[0]; a.Include = true; }
            }
            try
            {
                if (withImages.Count == 0) return;
                if (ctx.ChooseAlbumCovers != null)
                {
                    if (!ctx.ChooseAlbumCovers(withImages)) { ctx.Log("Musik: keine Albumbilder übernommen."); return; }
                }
                else
                {
                    foreach (AlbumCandidate a in withImages)
                        if (!a.Include) ctx.Log("  AUSWAHL NÖTIG  " + a.AlbumDir + " (" + a.Sources.Count + " mögliche Bilder, ohne Oberfläche übersprungen)");
                }

                int created = 0, failed = 0;
                foreach (AlbumCandidate a in withImages)
                {
                    ctx.Check();
                    if (!a.Include || a.Chosen == null) continue;
                    try
                    {
                        Apply(ctx, a);
                        created++;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception e)
                    {
                        failed++;
                        ctx.Log("  FEHLER  " + a.AlbumDir + " :: " + e.Message);
                    }
                }
                ctx.Log("Albumbilder erstellt: " + created + "   Fehler: " + failed);
                if (failed > 0) throw new TaskFailedException(failed + " Albumbild(er) konnten nicht erstellt werden");
            }
            finally
            {
                foreach (AlbumCandidate a in albums)
                    if (a.Embedded != null) FileOps.TryDelete(a.Embedded);
            }
        }

        // Alben suchen: Musik (ohne Bilder gezählt) zu mindestens 90 %, kein Vorschaubild im Ordner darüber.
        // Kategorien direkt im Installationsordner (z.B. "Musik") sind keine Alben.
        static void Find(TaskContext ctx, string dir, List<AlbumCandidate> result)
        {
            ctx.Check();
            string[] files, dirs;
            try
            {
                files = Directory.GetFiles(dir);
                dirs = Directory.GetDirectories(dir);
            }
            catch (UnauthorizedAccessException) { return; }
            catch (IOException) { return; }

            int music = 0, video = 0;
            List<string> musicFiles = new List<string>();
            foreach (string f in files)
            {
                string kind = MediaTypes.Kind(Path.GetExtension(f).ToLowerInvariant());
                if (kind == "audio") { music++; musicFiles.Add(f); }
                else if (kind == "video") video++;
            }
            string full = Path.GetFullPath(dir).TrimEnd('\\');
            string parent = Path.GetDirectoryName(full);
            string appRoot = Path.GetFullPath(App.Root).TrimEnd('\\');
            bool isCategoryOrRoot = string.Equals(full, appRoot, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(parent, appRoot, StringComparison.OrdinalIgnoreCase);
            if (music > 0 && music >= MusicShare * (music + video) && !isCategoryOrRoot && parent != null)
            {
                string name = Path.GetFileName(full);
                string previewDir = Path.Combine(parent, "Preview");
                if (!HasPreview(previewDir, name))
                {
                    AlbumCandidate a = new AlbumCandidate();
                    a.AlbumDir = full;
                    a.Name = name;
                    a.PreviewDir = previewDir;
                    AddImages(a.Images, files);
                    foreach (string sub in dirs)
                    {
                        if (Folders.Ignore(sub) || string.Equals(Path.GetFileName(sub), "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                        try { AddImages(a.Images, Directory.GetFiles(sub)); } catch { }
                    }
                    musicFiles.Sort(ListSort.Natural);
                    a.EmbeddedFrom = string.Join("|", musicFiles.ToArray());   // vorläufig: alle Kandidaten
                    result.Add(a);
                }
            }
            foreach (string sub in dirs)
            {
                if (Folders.Ignore(sub) || string.Equals(Path.GetFileName(sub), "Preview", StringComparison.OrdinalIgnoreCase)) continue;
                Find(ctx, sub, result);
            }
        }

        static void AddImages(List<string> list, string[] files)
        {
            List<string> found = new List<string>();
            foreach (string f in files)
                if (ImageExtensions.Contains(Path.GetExtension(f)) && !Path.GetFileName(f).StartsWith("._")) found.Add(f);
            found.Sort(ListSort.Natural);
            list.AddRange(found);
        }

        static bool HasPreview(string previewDir, string name)
        {
            foreach (string ext in new string[] { ".jpg", ".jpeg", ".png", ".webp" })
                if (File.Exists(Path.Combine(previewDir, name + ext))) return true;
            return false;
        }

        // Cover aus der ersten Musikdatei, sonst aus zwei zufälligen anderen
        static void ExtractEmbedded(TaskContext ctx, AlbumCandidate a)
        {
            string[] music = (a.EmbeddedFrom ?? "").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            a.EmbeddedFrom = null;
            if (music.Length == 0) return;
            List<string> tries = new List<string> { music[0] };
            List<string> others = new List<string>(music);
            others.RemoveAt(0);
            Random rnd = new Random(a.AlbumDir.GetHashCode());
            while (tries.Count < 3 && others.Count > 0)
            {
                int k = rnd.Next(others.Count);
                tries.Add(others[k]);
                others.RemoveAt(k);
            }
            foreach (string file in tries)
            {
                ctx.Check();
                string temp = Path.Combine(Path.GetTempPath(), "sflix-cover-" + Guid.NewGuid().ToString("N") + ".jpg");
                try
                {
                    ProcResult r = Proc.Run(ctx.Ffmpeg.FfmpegPath, new string[] {
                        "-v", "error", "-y", "-i", file, "-an", "-map", "0:v:0", "-frames:v", "1", "-q:v", "2", temp }, ctx.Token, null);
                    if (r.ExitCode == 0 && File.Exists(temp) && new FileInfo(temp).Length > 0)
                    {
                        a.Embedded = temp;
                        a.EmbeddedFrom = file;
                        return;
                    }
                }
                catch (OperationCanceledException) { FileOps.TryDelete(temp); throw; }
                catch { }
                FileOps.TryDelete(temp);
            }
        }

        // Übernehmen: Original sichern, Vorschaubild mit unscharfem Rand, übrige Bilder, leere Unterordner
        static void Apply(TaskContext ctx, AlbumCandidate a)
        {
            bool embedded = a.Chosen == a.Embedded;
            Directory.CreateDirectory(a.PreviewDir);
            string originals = Path.Combine(a.PreviewDir, "__original");
            Directory.CreateDirectory(originals);
            string ext = embedded ? ".jpg" : Path.GetExtension(a.Chosen).ToLowerInvariant();
            string original = FileOps.UniquePath(originals, a.Name + ext);
            if (embedded) File.Copy(a.Embedded, original);
            else FileOps.MoveAcross(a.Chosen, original, false);

            string target = Path.Combine(a.PreviewDir, a.Name + ".jpg");
            RenderPadded(ctx, original, target);

            int removed = 0;
            if (a.DeleteRest)
            {
                foreach (string img in a.Images)
                {
                    if (img == a.Chosen || !File.Exists(img)) continue;
                    // Ablage im Album (Unterordner als Unterordner der Ablage), damit leere Bildordner verschwinden können
                    string storage = Path.Combine(a.AlbumDir, DeletedFolderName);
                    string sub = Path.GetDirectoryName(img);
                    if (!string.Equals(sub, a.AlbumDir, StringComparison.OrdinalIgnoreCase))
                        storage = Path.Combine(storage, Path.GetFileName(sub));
                    Directory.CreateDirectory(storage);
                    FileOps.Rename(img, FileOps.UniquePath(storage, Path.GetFileName(img)));
                    removed++;
                }
            }
            // Unterordner, in denen Bilder lagen und die jetzt leer sind, entfernen
            HashSet<string> subs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string img in a.Images)
            {
                string d = Path.GetDirectoryName(img);
                if (!string.Equals(d, a.AlbumDir, StringComparison.OrdinalIgnoreCase)) subs.Add(d);
            }
            foreach (string d in subs)
                if (Directory.Exists(d) && Directory.GetFileSystemEntries(d).Length == 0)
                    try { FileOps.DeleteEmptyDirectory(d); } catch { }

            ctx.Log("  ALBUMBILD  " + target + "  (aus " + (embedded ? "der Musikdatei " + Path.GetFileName(a.EmbeddedFrom) : Path.GetFileName(a.Chosen)) +
                    (removed > 0 ? ", " + removed + " übrige Bild(er) nach " + DeletedFolderName : "") + ")");
        }

        // Bild als Bitmap laden (andere Formate wie WEBP, HEIC, AVIF über ffmpeg, verkleinert)
        public static Bitmap Load(FfmpegTools ff, string path, int maxSize, System.Threading.CancellationToken token)
        {
            if (GdiFormats.Contains(Path.GetExtension(path)))
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(path)))
                    using (Image img = Image.FromStream(ms))
                        return new Bitmap(img);
                }
                catch { }   // z.B. ungewöhnliches TIFF: über ffmpeg versuchen
            }
            if (ff == null || !ff.Installed) return null;
            string temp = Path.Combine(Path.GetTempPath(), "sflix-image-" + Guid.NewGuid().ToString("N") + ".bmp");
            try
            {
                Proc.Run(ff.FfmpegPath, new string[] {
                    "-v", "error", "-y", "-i", path, "-frames:v", "1",
                    "-vf", "scale=w=" + maxSize + ":h=" + maxSize + ":force_original_aspect_ratio=decrease", temp }, token, null);
                if (!File.Exists(temp)) return null;
                using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(temp)))
                using (Image img = Image.FromStream(ms))
                    return new Bitmap(img);
            }
            catch (OperationCanceledException) { throw; }
            catch { return null; }
            finally { FileOps.TryDelete(temp); }
        }

        // 480×270: das ganze Bild in der Mitte, dahinter dasselbe Bild füllend, stark unscharf und abgedunkelt
        static void RenderPadded(TaskContext ctx, string source, string target)
        {
            using (Bitmap src = Load(ctx.Ffmpeg, source, 1200, ctx.Token))
            {
                if (src == null) throw new Exception("Bild konnte nicht gelesen werden");
                using (Bitmap canvas = new Bitmap(Width, Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(canvas))
                        DrawPadded(g, src);
                    ImageCodecInfo jpeg = null;
                    foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders()) if (c.MimeType == "image/jpeg") jpeg = c;
                    EncoderParameters enc = new EncoderParameters(1);
                    enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                    // Erst als Zwischendatei, damit nie ein halbes Bild im Preview-Ordner liegt
                    string temp = Path.Combine(Path.GetDirectoryName(target), Path.GetFileNameWithoutExtension(target) + ".__temp__.jpg");
                    canvas.Save(temp, jpeg, enc);
                    FileOps.Replace(temp, target);
                }
            }
        }

        // Zeichnet das Albumbild mit unscharfem Rand (auch für die Vorschau im Auswahlfenster)
        public static void DrawPadded(Graphics g, Bitmap src)
        {
            // Hintergrund: auf 32×18 verkleinern und wieder vergrössern ergibt eine starke Unschärfe
            using (Bitmap small = new Bitmap(32, 18, PixelFormat.Format24bppRgb))
            {
                using (Graphics gs = Graphics.FromImage(small))
                {
                    gs.InterpolationMode = InterpolationMode.HighQualityBilinear;
                    gs.DrawImage(src, new Rectangle(0, 0, 32, 18), CoverRect(src.Width, src.Height, 32.0 / 18.0), GraphicsUnit.Pixel);
                }
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(small, new Rectangle(-30, -18, Width + 60, Height + 36));
            }
            using (SolidBrush shade = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                g.FillRectangle(shade, 0, 0, Width, Height);
            // Vordergrund: ganzes Bild
            double scale = Math.Min((double)Width / src.Width, (double)Height / src.Height);
            int w = (int)Math.Round(src.Width * scale), h = (int)Math.Round(src.Height * scale);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(src, new Rectangle((Width - w) / 2, (Height - h) / 2, w, h));
        }

        public static Bitmap RenderPreview(Bitmap src)
        {
            Bitmap canvas = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(canvas)) DrawPadded(g, src);
            return canvas;
        }

        // Ausschnitt mit Seitenverhältnis ratio aus der Bildmitte
        static Rectangle CoverRect(int w, int h, double ratio)
        {
            if ((double)w / h > ratio)
            {
                int cw = (int)(h * ratio);
                return new Rectangle((w - cw) / 2, 0, cw, h);
            }
            int ch = (int)(w / ratio);
            return new Rectangle(0, (h - ch) / 2, w, ch);
        }
    }

    // Auswahl der Albumbilder: links die Alben (angehakt = übernehmen), in der Mitte die möglichen Bilder
    // des markierten Albums (Klick = dieses Bild verwenden), rechts die Vorschau so, wie sie in STEFFLIX
    // erscheint (ganzes Bild, unscharfer Rand). Je Album: "Übrige Bilder nach __gelöscht".
    public class AlbumCoverDialog : Form
    {
        readonly List<AlbumCandidate> albums;
        readonly FfmpegTools ff;
        readonly ListView albumList, imageList;
        readonly PictureBox preview;
        readonly DarkCheckBox deleteRest;
        readonly Label hint;
        readonly Dictionary<string, Bitmap> previews = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        AlbumCandidate current;
        bool filling;
        bool accepted;

        public AlbumCoverDialog(List<AlbumCandidate> albums, FfmpegTools ff)
        {
            this.albums = albums;
            this.ff = ff;
            Text = "Albumbilder wählen";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1180), Theme.S(620));
            MinimumSize = new Size(Theme.S(900), Theme.S(480));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(albums.Count + " Alben ohne Vorschaubild. Links die Alben (angehakt = übernehmen), in der Mitte die gefundenen Bilder " +
                                          "des markierten Albums: Ein Klick wählt das Bild. Rechts siehst du, wie es in STEFFLIX erscheint. " +
                                          "Das Original kommt nach Preview\\__original im Ordner über dem Album.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));
            Resize += delegate { intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0); };

            albumList = MakeList(true);
            albumList.Columns.Add("Album", Theme.S(240));
            albumList.Columns.Add("Bilder", Theme.S(60), HorizontalAlignment.Right);
            foreach (AlbumCandidate a in albums)
            {
                ListViewItem item = new ListViewItem(a.Name);
                ListSort.Add(item, a.Sources.Count.ToString(), a.Sources.Count);
                item.ToolTipText = a.AlbumDir;
                item.Tag = a;
                item.Checked = a.Include;
                albumList.Items.Add(item);
            }
            albumList.ShowItemToolTips = true;
            albumList.ItemChecked += OnAlbumChecked;
            albumList.SelectedIndexChanged += delegate { if (albumList.SelectedItems.Count > 0) ShowAlbum((AlbumCandidate)albumList.SelectedItems[0].Tag); };

            imageList = MakeList(false);
            imageList.MultiSelect = false;
            imageList.Columns.Add("Bild", Theme.S(220));
            imageList.Columns.Add("Ort", Theme.S(120));
            imageList.Columns.Add("Grösse", Theme.S(80), HorizontalAlignment.Right);
            imageList.Columns.Add("", Theme.S(70));
            imageList.SelectedIndexChanged += OnImageSelected;

            preview = new PictureBox();
            preview.Dock = DockStyle.Top;
            preview.Height = Theme.S(270);
            preview.SizeMode = PictureBoxSizeMode.Zoom;
            preview.BackColor = Color.Black;
            deleteRest = new DarkCheckBox();
            deleteRest.Text = "Übrige Bilder dieses Albums nach „__gelöscht“ verschieben";
            deleteRest.ForeColor = Theme.Text;
            deleteRest.FitText();
            deleteRest.Dock = DockStyle.Top;
            deleteRest.CheckedChanged += delegate { if (current != null && !filling) current.DeleteRest = deleteRest.Checked; };
            hint = Theme.MakeLabel("", true);
            hint.Dock = DockStyle.Top;
            hint.AutoSize = true;
            hint.MaximumSize = new Size(Theme.S(420), 0);
            hint.Padding = new Padding(0, Theme.S(8), 0, 0);
            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.Padding = new Padding(Theme.S(8), 0, 0, 0);
            right.Controls.Add(hint);
            right.Controls.Add(deleteRest);
            right.Controls.Add(preview);

            TableLayoutPanel body = new TableLayoutPanel();
            body.Dock = DockStyle.Fill;
            body.ColumnCount = 3;
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
            body.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            body.Controls.Add(albumList, 0, 0);
            body.Controls.Add(imageList, 1, 0);
            body.Controls.Add(right, 2, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button ok = Theme.MakeButton("Übernehmen", true);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            ok.Click += delegate { accepted = true; DialogResult = DialogResult.OK; };
            cancel.Click += delegate { accepted = false; DialogResult = DialogResult.Cancel; };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);

            Controls.Add(body);
            Controls.Add(buttons);
            Controls.Add(intro);
            CancelButton = cancel;
            FormClosed += delegate { foreach (Bitmap b in previews.Values) if (b != null) b.Dispose(); };
            Shown += delegate { if (albumList.Items.Count > 0) albumList.Items[0].Selected = true; };
        }

        static ListView MakeList(bool checkBoxes)
        {
            ListView l = new ListView();
            l.View = View.Details;
            l.CheckBoxes = checkBoxes;
            l.FullRowSelect = true;
            l.HideSelection = false;
            l.Dock = DockStyle.Fill;
            l.BackColor = Theme.Card;
            l.ForeColor = Theme.Text;
            l.BorderStyle = BorderStyle.None;
            DarkList.Apply(l);
            return l;
        }

        void OnAlbumChecked(object sender, ItemCheckedEventArgs e)
        {
            if (filling) return;
            AlbumCandidate a = (AlbumCandidate)e.Item.Tag;
            if (e.Item.Checked && a.Chosen == null)
            {
                filling = true;
                e.Item.Checked = false;
                filling = false;
                e.Item.Selected = true;
                hint.Text = "Bitte für „" + a.Name + "“ zuerst in der Mitte ein Bild anklicken.";
                return;
            }
            a.Include = e.Item.Checked;
        }

        void ShowAlbum(AlbumCandidate a)
        {
            current = a;
            filling = true;
            try
            {
                imageList.BeginUpdate();
                imageList.Items.Clear();
                foreach (string src in a.Sources)
                {
                    bool embedded = src == a.Embedded;
                    ListViewItem item = new ListViewItem(embedded ? "Cover aus der Musikdatei" : Path.GetFileName(src));
                    string where = embedded ? Path.GetFileName(a.EmbeddedFrom)
                        : string.Equals(Path.GetDirectoryName(src), a.AlbumDir, StringComparison.OrdinalIgnoreCase) ? "im Album"
                        : Path.GetFileName(Path.GetDirectoryName(src));
                    item.SubItems.Add(where);
                    long size = 0;
                    try { size = new FileInfo(src).Length; } catch { }
                    ListSort.Add(item, Fmt.Size(size), size);
                    item.SubItems.Add(src == a.Chosen ? "gewählt" : "");
                    item.ToolTipText = src;
                    item.Tag = src;
                    imageList.Items.Add(item);
                    if (src == a.Chosen) item.Selected = true;
                }
                imageList.EndUpdate();
                deleteRest.Checked = a.DeleteRest;
                deleteRest.Enabled = a.Images.Count > 1 || (a.Images.Count == 1 && a.Chosen == a.Embedded);
                hint.Text = a.AlbumDir;
            }
            finally { filling = false; }
            ShowPreview(a.Chosen);
        }

        void OnImageSelected(object sender, EventArgs e)
        {
            if (filling || current == null || imageList.SelectedItems.Count == 0) return;
            string src = (string)imageList.SelectedItems[0].Tag;
            current.Chosen = src;
            current.Include = true;
            foreach (ListViewItem i in imageList.Items) i.SubItems[3].Text = (string)i.Tag == src ? "gewählt" : "";
            foreach (ListViewItem row in albumList.Items)
                if (row.Tag == current) { filling = true; row.Checked = true; filling = false; }
            deleteRest.Enabled = current.Images.Count > 1 || (current.Images.Count == 1 && src == current.Embedded);
            ShowPreview(src);
        }

        void ShowPreview(string src)
        {
            if (src == null) { preview.Image = null; return; }
            Bitmap shown;
            if (!previews.TryGetValue(src, out shown))
            {
                Cursor old = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                try
                {
                    using (Bitmap raw = AlbumCovers.Load(ff, src, 900, System.Threading.CancellationToken.None))
                        shown = raw != null ? AlbumCovers.RenderPreview(raw) : null;
                }
                finally { Cursor.Current = old; }
                previews[src] = shown;
            }
            preview.Image = shown;
            if (shown == null) hint.Text = "Vorschau nicht möglich (Format nicht lesbar).";
        }

        public bool Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowDialog(owner);
            return accepted;
        }
    }
}
