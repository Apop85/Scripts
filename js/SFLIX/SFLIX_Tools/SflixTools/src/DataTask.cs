// Aufgabe "Daten einlesen" (Portierung von getfilepath.py / updateData.exe).
//
// Durchsucht den STEFFLIX-Ordner, korrigiert Namen mit Zeichen, die der Browser (btoa) nicht
// verarbeiten kann, verkleinert zu grosse Vorschaubilder und schreibt sflix_sys\data.js.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sflix
{
    public class DataTask
    {
        // Muss mit den Listen in sflix_sys/js/core.js übereinstimmen (mediaTypes, musicTypes, imageTypes)
        // Abspielbare Endungen aus MediaTypes, ohne Punkt
        static readonly HashSet<string> AllowedFileTypes = Bare(MediaTypes.ReadyVideo, MediaTypes.ReadyAudio, MediaTypes.ReadyImage);

        static HashSet<string> Bare(params HashSet<string>[] sets)
        {
            HashSet<string> s = new HashSet<string>();
            foreach (HashSet<string> set in sets) foreach (string e in set) s.Add(e.TrimStart('.').ToLowerInvariant());
            return s;
        }

        // Zu grosse Vorschaubilder werden auf 480x270 zugeschnitten (16:9). Das Original
        // kommt nach Preview\__original\<Dateiname>.
        const string PreviewFolder = "Preview";
        const int PreviewWidth = 480, PreviewHeight = 270;
        const long PreviewQuality = 80;
        const string OriginalFolder = "__original";   // Preview\__original\<Datei>; "Ablagen leeren" räumt ihn auf
        const string OriginalSuffix = ".original";    // Ablage bis Version 3.0.3 ("<Datei>.original")

        // Zeichenfolgen, die STEFFLIX intern als Trennzeichen nutzt oder die in URLs Probleme machen
        static readonly string[,] ForbiddenSymbols = { { "$=$", "-" }, { "|", "-" }, { "#", "" }, { "%", "" } };

        // Häufige Zeichen ausserhalb von Latin-1 mit sinnvollem Ersatz
        static readonly string[,] Replacements = {
            { "–", "-" }, { "—", "-" }, { "‐", "-" }, { "‑", "-" }, { "‒", "-" }, { "−", "-" },
            { "‘", "'" }, { "’", "'" }, { "‚", "," }, { "‛", "'" },
            { "“", "'" }, { "”", "'" }, { "„", "'" }, { "«", "'" }, { "»", "'" },
            { "…", "..." }, { "€", "EUR" }, { "•", "-" }, { "·", "-" },
            { " ", " " }, { "™", "" }, { "®", "" }, { "©", "" },
            { "Ł", "L" }, { "ł", "l" }, { "Đ", "D" }, { "đ", "d" }, { "Œ", "OE" }, { "œ", "oe" },
        };

        const string RenamedPrefix = "var renamedPaths = ";

        readonly TaskContext ctx;
        readonly string homeDir;
        readonly List<string[]> renamedItems = new List<string[]>();   // {alter Pfad, neuer Name}
        readonly List<object> renamedPairs = new List<object>();
        readonly List<string> errorList = new List<string>();
        readonly List<string> skippedFolders = new List<string>();
        readonly List<string> innerUnderscore = new List<string>();   // Ordner mit "__" mitten im Namen
        readonly List<string> unreadableFolders = new List<string>(); // Lesefehler (nicht fehlende Rechte)
        readonly List<string> skippedLinks = new List<string>();      // ausgelassene Verknüpfungen
        int shrinkCounter;
        int notShrinkable;
        int migratedOriginals;
        int counter;
        int branchMedia;   // Teil-Einlesen: Medien in den gewählten Ordnern (ohne Preview darüber)

        DataTask(TaskContext ctx)
        {
            this.ctx = ctx;
            homeDir = App.Root;
        }

        // Nur diese Ordner einlesen (absolute Pfade im STEFFLIX-Ordner). Ihr Zweig in der
        // bestehenden data.js wird ersetzt, alles andere bleibt. Leer oder null: alles einlesen.
        public static void Run(TaskContext ctx, IList<string> folders)
        {
            DataTask task = new DataTask(ctx);
            if (folders == null || folders.Count == 0 || !task.ExecutePartial(folders)) task.Execute();
        }

        // Einstellungen: data.all (alles einlesen) und data.folders (Ordner relativ zum
        // STEFFLIX-Ordner, getrennt mit "|")
        public const char FolderSeparator = '|';

        public static List<string> SplitFolders(string setting)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(setting)) return list;
            foreach (string part in setting.Split(FolderSeparator))
            {
                string p = part.Trim().Trim('\\');
                if (p.Length > 0 && !list.Contains(p)) list.Add(p);
            }
            return list;
        }

        // Relativer Pfad (Einstellungen, Kommandozeile) -> absoluter Pfad im STEFFLIX-Ordner
        public static string ToAbsolute(string folder)
        {
            folder = folder.Trim().Replace('/', '\\');
            return Path.GetFullPath(Path.IsPathRooted(folder) ? folder : Path.Combine(App.Root, folder.TrimStart('.', '\\')));
        }

        // Absoluter Pfad -> relativ zum STEFFLIX-Ordner; null, wenn er ausserhalb liegt
        public static string ToRelative(string folder)
        {
            string root = Path.GetFullPath(App.Root).TrimEnd('\\');
            string full = Path.GetFullPath(folder).TrimEnd('\\');
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return "";
            if (!full.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)) return null;
            return full.Substring(root.Length + 1);
        }

        // ---------------------------------------------------------------
        // Namen bereinigen
        // ---------------------------------------------------------------

        // Ersetzt oder entfernt alle Zeichen, die btoa() im Browser nicht verarbeiten kann
        // (alles ausserhalb von Latin-1) sowie die Trennzeichen von STEFFLIX.
        public static string SanitizeName(string name)
        {
            for (int i = 0; i < Replacements.GetLength(0); i++) name = name.Replace(Replacements[i, 0], Replacements[i, 1]);
            for (int i = 0; i < ForbiddenSymbols.GetLength(0); i++) name = name.Replace(ForbiddenSymbols[i, 0], ForbiddenSymbols[i, 1]);

            StringBuilder result = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                // Zeichen ausserhalb der BMP (z.B. Emojis) bestehen aus zwei char. Ein einzelnes Surrogat
                // (kaputter Name) wird entfernt; ConvertToUtf32 würde sonst das ganze Einlesen abbrechen.
                if (char.IsSurrogate(name[i]) && !char.IsSurrogatePair(name, i)) continue;
                string ch = char.IsSurrogatePair(name, i) ? name.Substring(i++, 2) : name[i].ToString();
                int code = char.ConvertToUtf32(ch, 0);
                if (code < 0x20 || (code >= 0x7F && code <= 0x9F)) continue;
                if (code <= 0xFF) { result.Append(ch); continue; }
                // z.B. "ő" -> "o"
                string decomposed;
                try { decomposed = ch.Normalize(NormalizationForm.FormKD); }
                catch (ArgumentException) { continue; }
                foreach (char part in decomposed)
                {
                    if ((part >= 0x20 && part <= 0x7E) || (part >= 0xA0 && part <= 0xFF)) result.Append(part);
                }
            }

            // Mehrfache Leerzeichen zusammenfassen, Windows mag keine Leerzeichen/Punkte am Ende
            string joined = string.Join(" ", result.ToString().Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
            joined = joined.TrimEnd(' ', '.');
            return joined.Length == 0 ? "unbenannt" : joined;
        }

        public static string SanitizeFileName(string fileName)
        {
            string stem, ext;
            FileOps.SplitExt(fileName, out stem, out ext);
            return SanitizeName(stem) + ext.ToLowerInvariant();
        }

        // Benennt Dateien/Ordner mit ungültigen Zeichen um. Gibt den neuen Namen zurück oder
        // null, wenn das Umbenennen nicht möglich war.
        string RenameIfNeeded(string path, string item, bool isFile)
        {
            string newName = isFile ? SanitizeFileName(item) : SanitizeName(item);
            if (newName == item) return item;

            string stem, ext;
            if (isFile) FileOps.SplitExt(newName, out stem, out ext);
            else { stem = newName; ext = ""; }

            string candidate = newName;
            int number = 2;
            while (FileOps.Exists(Path.Combine(path, candidate)) && candidate.ToLowerInvariant() != item.ToLowerInvariant())
            {
                candidate = stem + " (" + number + ")" + ext;
                number++;
            }

            try
            {
                FileOps.Rename(Path.Combine(path, item), Path.Combine(path, candidate));
                renamedItems.Add(new string[] { Path.Combine(path, item), candidate });
                renamedPairs.Add(new List<object> { RelativeUrl(Path.Combine(path, item)), RelativeUrl(Path.Combine(path, candidate)) });
                return candidate;
            }
            catch (IOException)
            {
                errorList.Add(Path.Combine(path, item));
                return null;
            }
        }

        string Relative(string fullPath)
        {
            string root = homeDir.TrimEnd('\\');
            return fullPath.Substring(root.Length + 1);
        }

        string RelativeUrl(string fullPath)
        {
            return "./" + Relative(fullPath).Replace('\\', '/');
        }

        // ---------------------------------------------------------------
        // Vorschaubilder verkleinern
        // ---------------------------------------------------------------

        static ImageCodecInfo jpegCodec;

        void ShrinkPreview(string fullPath)
        {
            string ext = fullPath.Substring(fullPath.LastIndexOf('.') + 1).ToLowerInvariant();
            ImageFormat format;
            if (ext == "jpg" || ext == "jpeg") format = ImageFormat.Jpeg;
            else if (ext == "png") format = ImageFormat.Png;
            else if (ext == "bmp") format = ImageFormat.Bmp;
            else
            {
                // WebP, AVIF, GIF bleiben unverändert (WebP kann Windows hier nicht schreiben)
                if (ext == "webp") notShrinkable++;
                return;
            }
            // Schon klein genug (Normalfall nach dem ersten Einlesen): nur den Dateikopf lesen, nicht
            // das ganze Bild dekodieren. Bei unklarem Kopf oder EXIF-Drehung um 90° wie bisher dekodieren.
            int headerWidth, headerHeight;
            bool turned;
            if (ImageHeader.TryGetSize(fullPath, out headerWidth, out headerHeight, out turned)
                && !turned && headerWidth <= PreviewWidth && headerHeight <= PreviewHeight) return;
            string originalPath = OriginalPathFor(fullPath);

            string tempPath = fullPath + ".tmp";
            try
            {
                using (Bitmap result = LoadFitted(fullPath, format == ImageFormat.Png))
                {
                    if (result == null) return;
                    if (format == ImageFormat.Jpeg)
                    {
                        if (jpegCodec == null)
                            foreach (ImageCodecInfo c in ImageCodecInfo.GetImageEncoders())
                                if (c.MimeType == "image/jpeg") jpegCodec = c;
                        EncoderParameters enc = new EncoderParameters(1);
                        enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, PreviewQuality);
                        result.Save(tempPath, jpegCodec, enc);
                    }
                    else
                    {
                        result.Save(tempPath, format);
                    }
                }
                // Erst wenn die neue Version fertig ist: Original nach Preview\__original sichern
                // (gleicher Dateiname), neue Version an seine Stelle. Ein dort schon vorhandenes
                // Original stammt von einem früher ersetzten Bild und wird durch das neuere ersetzt.
                Directory.CreateDirectory(Path.GetDirectoryName(originalPath));
                FileOps.Replace(fullPath, originalPath);
                FileOps.Replace(tempPath, fullPath);
                shrinkCounter++;
            }
            catch (Exception)
            {
                FileOps.TryDelete(tempPath);
                if (!File.Exists(fullPath) && File.Exists(originalPath))
                {
                    try { FileOps.Replace(originalPath, fullPath); } catch { }
                }
            }
        }

        static string OriginalPathFor(string previewFile)
        {
            return Path.Combine(Path.GetDirectoryName(previewFile), OriginalFolder, Path.GetFileName(previewFile));
        }

        // Bis Version 3.0.3 lagen Originale als "<Datei>.original" direkt im Preview-Ordner.
        // Sie ziehen nach Preview\__original\<Datei> um. Gibt es dort schon eine Datei gleichen
        // Namens, bleiben beide erhalten ("Name (2).jpg").
        void MigrateOldOriginal(string previewDir, string item)
        {
            string name = item.Substring(0, item.Length - OriginalSuffix.Length);
            if (name.Length == 0) return;
            try
            {
                string folder = Path.Combine(previewDir, OriginalFolder);
                Directory.CreateDirectory(folder);
                FileOps.Rename(Path.Combine(previewDir, item), FileOps.UniquePath(folder, name));
                migratedOriginals++;
            }
            catch (Exception)
            {
                errorList.Add(Path.Combine(previewDir, item));
            }
        }

        // Bildgrösse aus dem Dateikopf (JPEG SOF, PNG IHDR, BMP), ohne das Bild zu dekodieren.
        // turned: JPEG mit EXIF-Ausrichtung 5 bis 8 (um 90° gedreht, Breite und Höhe vertauscht).
        static class ImageHeader
        {
            public static bool TryGetSize(string path, out int width, out int height, out bool turned)
            {
                width = height = 0;
                turned = false;
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
                    {
                        byte[] head = new byte[26];
                        if (ReadFully(fs, head, 0, 26) < 26) return false;
                        // PNG: Signatur, dann IHDR mit Breite und Höhe (Big Endian)
                        if (head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47
                            && head[12] == 0x49 && head[13] == 0x48 && head[14] == 0x44 && head[15] == 0x52)
                        {
                            width = BigEndian(head, 16, 4);
                            height = BigEndian(head, 20, 4);
                            return width > 0 && height > 0;
                        }
                        // BMP: Breite und Höhe ab Byte 18 (Little Endian, Höhe negativ = von oben)
                        if (head[0] == 0x42 && head[1] == 0x4D)
                        {
                            width = BitConverter.ToInt32(head, 18);
                            height = Math.Abs(BitConverter.ToInt32(head, 22));
                            return width > 0 && height > 0;
                        }
                        if (head[0] == 0xFF && head[1] == 0xD8)
                        {
                            fs.Position = 2;
                            return ReadJpeg(fs, out width, out height, out turned);
                        }
                    }
                }
                catch { }
                return false;
            }

            static bool ReadJpeg(FileStream fs, out int width, out int height, out bool turned)
            {
                width = height = 0;
                turned = false;
                byte[] two = new byte[2];
                for (int guard = 0; guard < 200; guard++)
                {
                    int b = fs.ReadByte();
                    if (b != 0xFF) return false;
                    int marker;
                    do { marker = fs.ReadByte(); } while (marker == 0xFF);
                    if (marker < 0) return false;
                    if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) continue;   // ohne Länge
                    if (marker == 0xD9 || marker == 0xDA) return false;                   // Ende bzw. Bilddaten ohne SOF
                    if (ReadFully(fs, two, 0, 2) < 2) return false;
                    int length = BigEndian(two, 0, 2);
                    if (length < 2) return false;
                    bool sof = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                    if (sof)
                    {
                        byte[] sofData = new byte[5];
                        if (ReadFully(fs, sofData, 0, 5) < 5) return false;
                        height = BigEndian(sofData, 1, 2);
                        width = BigEndian(sofData, 3, 2);
                        return width > 0 && height > 0;
                    }
                    if (marker == 0xE1 && length > 8 && length <= 65535)
                    {
                        byte[] app = new byte[length - 2];
                        if (ReadFully(fs, app, 0, app.Length) < app.Length) return false;
                        int orientation = ExifOrientation(app);
                        if (orientation >= 5 && orientation <= 8) turned = true;
                        continue;
                    }
                    fs.Seek(length - 2, SeekOrigin.Current);
                }
                return false;
            }

            // Ausrichtung (Tag 0x0112) aus IFD0 eines APP1-Segments "Exif\0\0", 0 = keine
            static int ExifOrientation(byte[] app)
            {
                if (app.Length < 14 || app[0] != 'E' || app[1] != 'x' || app[2] != 'i' || app[3] != 'f' || app[4] != 0 || app[5] != 0) return 0;
                int t = 6;
                bool little = app[t] == 'I' && app[t + 1] == 'I';
                if (!little && !(app[t] == 'M' && app[t + 1] == 'M')) return 0;
                int ifd = Read32(app, t + 4, little);
                int pos = t + ifd;
                if (ifd < 8 || pos + 2 > app.Length) return 0;
                int count = Read16(app, pos, little);
                for (int i = 0; i < count; i++)
                {
                    int e = pos + 2 + i * 12;
                    if (e + 12 > app.Length) return 0;
                    if (Read16(app, e, little) == 0x0112) return Read16(app, e + 8, little);
                }
                return 0;
            }

            static int Read16(byte[] d, int o, bool little)
            {
                return little ? d[o] | (d[o + 1] << 8) : (d[o] << 8) | d[o + 1];
            }

            static int Read32(byte[] d, int o, bool little)
            {
                return little ? d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24)
                              : (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];
            }

            static int BigEndian(byte[] d, int o, int n)
            {
                int v = 0;
                for (int i = 0; i < n; i++) v = (v << 8) | d[o + i];
                return v;
            }

            static int ReadFully(Stream s, byte[] buffer, int offset, int count)
            {
                int total = 0;
                while (total < count)
                {
                    int n = s.Read(buffer, offset + total, count - total);
                    if (n <= 0) break;
                    total += n;
                }
                return total;
            }
        }

        // Liefert das auf 480x270 zugeschnittene Bild oder null, wenn es klein genug ist.
        static Bitmap LoadFitted(string path, bool keepAlpha)
        {
            byte[] bytes = File.ReadAllBytes(path);
            using (MemoryStream ms = new MemoryStream(bytes))
            using (Image img = Image.FromStream(ms, false, false))
            {
                // Animierte Bilder nicht anfassen
                try
                {
                    if (img.FrameDimensionsList.Length > 0 && img.GetFrameCount(new FrameDimension(img.FrameDimensionsList[0])) > 1) return null;
                }
                catch { }

                RotateFlipType rotate = ExifRotation(img);
                bool swap = rotate == RotateFlipType.Rotate90FlipNone || rotate == RotateFlipType.Rotate270FlipNone
                         || rotate == RotateFlipType.Rotate90FlipX || rotate == RotateFlipType.Rotate270FlipX;
                int w = swap ? img.Height : img.Width;
                int h = swap ? img.Width : img.Height;
                if (w <= PreviewWidth && h <= PreviewHeight) return null;

                using (Bitmap oriented = new Bitmap(img))
                {
                    if (rotate != RotateFlipType.RotateNoneFlipNone) oriented.RotateFlip(rotate);

                    // Wie Pillow ImageOps.fit: mittig auf 16:9 zuschneiden, dann verkleinern
                    double targetRatio = (double)PreviewWidth / PreviewHeight;
                    double srcRatio = (double)oriented.Width / oriented.Height;
                    RectangleF crop;
                    if (srcRatio > targetRatio)
                    {
                        float cw = (float)(oriented.Height * targetRatio);
                        crop = new RectangleF((oriented.Width - cw) / 2f, 0, cw, oriented.Height);
                    }
                    else
                    {
                        float ch = (float)(oriented.Width / targetRatio);
                        crop = new RectangleF(0, (oriented.Height - ch) / 2f, oriented.Width, ch);
                    }

                    Bitmap result = new Bitmap(PreviewWidth, PreviewHeight, keepAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb);
                    using (Graphics g = Graphics.FromImage(result))
                    using (ImageAttributes attr = new ImageAttributes())
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.CompositingQuality = CompositingQuality.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        if (!keepAlpha) g.Clear(Color.Black);
                        attr.SetWrapMode(WrapMode.TileFlipXY); // keine hellen/dunklen Ränder
                        g.DrawImage(oriented, new Rectangle(0, 0, PreviewWidth, PreviewHeight),
                                    crop.X, crop.Y, crop.Width, crop.Height, GraphicsUnit.Pixel, attr);
                    }
                    return result;
                }
            }
        }

        static RotateFlipType ExifRotation(Image img)
        {
            try
            {
                foreach (int id in img.PropertyIdList)
                {
                    if (id != 0x0112) continue;
                    int o = BitConverter.ToUInt16(img.GetPropertyItem(id).Value, 0);
                    switch (o)
                    {
                        case 2: return RotateFlipType.RotateNoneFlipX;
                        case 3: return RotateFlipType.Rotate180FlipNone;
                        case 4: return RotateFlipType.Rotate180FlipX;
                        case 5: return RotateFlipType.Rotate90FlipX;
                        case 6: return RotateFlipType.Rotate90FlipNone;
                        case 7: return RotateFlipType.Rotate270FlipX;
                        case 8: return RotateFlipType.Rotate270FlipNone;
                    }
                }
            }
            catch { }
            return RotateFlipType.RotateNoneFlipNone;
        }

        // ---------------------------------------------------------------
        // Durchsuchen
        // ---------------------------------------------------------------

        JsonObject Crawl(string path)
        {
            ctx.Check();
            JsonObject data = new JsonObject();
            bool isPreviewFolder = Path.GetFileName(path) == PreviewFolder;

            // Art und Attribute kommen aus der Auflistung (kein File.Exists, Directory.Exists und
            // GetAttributes je Eintrag: auf einem Netzlaufwerk je eine Anfrage). Gleiche Reihenfolge
            // wie Directory.GetFileSystemEntries.
            FileSystemInfo[] entries;
            try
            {
                entries = new DirectoryInfo(path).GetFileSystemInfos();
            }
            catch (Exception e)
            {
                // Kein Zugriff, Pfad zu lang, Verknüpfung ohne Ziel, Netzlaufwerk weg: Ordner melden
                // (vorher brach bei allem ausser fehlenden Rechten das ganze Einlesen ab)
                if (!(e is UnauthorizedAccessException || e is IOException || e is System.Security.SecurityException)) throw;
                skippedFolders.Add(path);
                if (e is IOException) unreadableFolders.Add(path + " (" + e.Message + ")");
                return data;
            }

            foreach (FileSystemInfo entry in entries)
            {
                string item = entry.Name;
                string fullPath = Path.Combine(path, item);
                bool isFile = entry is FileInfo;
                bool isDir = entry is DirectoryInfo;
                // Zwischendateien (Konvertieren, Download, Kopieren) und macOS-Begleitdateien nie einlesen
                if (isFile && MediaTypes.IsWorkFile(item)) continue;
                if (isDir && Folders.HasInnerDoubleUnderscore(item) && innerUnderscore.Count < 50) innerUnderscore.Add(fullPath);
                // Verknüpfungen auf Ordner werden seit 4.1.0 ausgelassen (DEC-359): im Protokoll nennen
                if (isDir && (entry.Attributes & FileAttributes.ReparsePoint) != 0) { if (skippedLinks.Count < 50) skippedLinks.Add(fullPath); continue; }
                string fileExtension = item.Substring(item.LastIndexOf('.') + 1).ToLowerInvariant();

                if (isFile && isPreviewFolder && item.EndsWith(OriginalSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    MigrateOldOriginal(path, item);
                }
                else if (isFile && AllowedFileTypes.Contains(fileExtension))
                {
                    item = RenameIfNeeded(path, item, true);
                    if (item == null) continue;
                    fullPath = Path.Combine(path, item);
                    if (isPreviewFolder) ShrinkPreview(fullPath);
                    data[RelativeUrl(fullPath)] = null;
                    counter++;
                    if (counter % 50 == 0) ctx.Progress(-1, "Durchsuche Ordner ... " + counter + " Medien");
                }
                // Ablagen der Werkzeuge (__teile, __konvertiert, __gelöscht), Systemordner und
                // Papierkörbe (z.B. @Recycle) gehören nicht in die Mediathek
                else if (isDir && !Folders.Ignore(entry))
                {
                    item = RenameIfNeeded(path, item, false);
                    if (item == null) continue;
                    data[item] = Crawl(Path.Combine(path, item));
                }
            }
            return data;
        }

        // Speichert die Umbenennungen in sflix_sys/renamed.js. STEFFLIX passt damit beim
        // nächsten Start Favoriten, Zeitstempel und "Weiterschauen" an.
        void WriteRenamedPaths()
        {
            WriteRenamed(homeDir, renamedPairs);
        }

        // Umbenennungen anderer Aufgaben (z.B. "Folgen umbenennen") in renamed.js ergänzen.
        // pairs: Listen ["./alter/Pfad", "./neuer/Pfad"] in der Reihenfolge der Umbenennung.
        public static void AppendRenamedPairs(List<object> pairs)
        {
            if (pairs == null || pairs.Count == 0) return;
            WriteRenamed(App.Root, pairs);
        }

        static void WriteRenamed(string homeDir, List<object> renamedPairs)
        {
            string file = Path.Combine(homeDir, "sflix_sys", "renamed.js");
            JsonObject previous = null;
            if (File.Exists(file))
            {
                try
                {
                    string content = File.ReadAllText(file, Encoding.UTF8).Trim();
                    if (content.StartsWith(RenamedPrefix))
                        previous = Json.Parse(content.Substring(RenamedPrefix.Length).TrimEnd(';')) as JsonObject;
                }
                catch { }
            }

            List<object> pairs = new List<object>();
            string renameId = "";
            if (previous != null)
            {
                if (previous.Arr("pairs") != null) pairs.AddRange(previous.Arr("pairs"));
                renameId = previous.Str("id") ?? "";
            }
            pairs.AddRange(renamedPairs);
            if (renamedPairs.Count > 0 || renameId.Length == 0)
                renameId = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            if (pairs.Count > 5000) pairs = pairs.GetRange(pairs.Count - 5000, 5000);

            JsonObject o = new JsonObject();
            o["id"] = renameId;
            o["pairs"] = pairs;
            File.WriteAllText(file, RenamedPrefix + Json.Write(o) + ";", FileOps.Utf8NoBom);
        }

        const string DataPrefix = "var data = ";

        string DataFile { get { return Path.Combine(homeDir, "sflix_sys", "data.js"); } }

        // Bestehende data.js lesen; null, wenn sie fehlt oder nicht lesbar ist
        JsonObject LoadExistingData()
        {
            try
            {
                if (!File.Exists(DataFile)) return null;
                string text = File.ReadAllText(DataFile, Encoding.UTF8).Trim();
                if (!text.StartsWith(DataPrefix)) return null;
                return Json.Parse(text.Substring(DataPrefix.Length).TrimEnd(';')) as JsonObject;
            }
            catch { return null; }
        }

        // Erst vollständig in data.js.tmp schreiben, dann ersetzen (vorher: data.js nach .bak umbenannt und
        // direkt geschrieben; ein Abbruch dazwischen hinterliess keine bzw. eine halbe data.js)
        void SaveData(JsonObject filelist)
        {
            string dataFile = DataFile;
            string tmp = dataFile + ".tmp";
            try
            {
                File.WriteAllText(tmp, DataPrefix + Json.Write(filelist) + ";", FileOps.Utf8NoBom);
                if (File.Exists(dataFile))
                {
                    try { File.Copy(dataFile, dataFile + ".bak", true); }
                    catch (Exception e) { ctx.Log("Hinweis: Sicherung data.js.bak nicht erstellt (" + e.Message + ")"); }
                }
                FileOps.Replace(tmp, dataFile);
            }
            catch
            {
                FileOps.TryDelete(tmp);
                throw;
            }
            ctx.Log("data.js gespeichert (Sicherung: data.js.bak)");

            try { WriteRenamedPaths(); }
            catch (Exception) { ctx.Log("Umbenennungen konnten nicht gespeichert werden"); }
        }

        // Teil-Einlesen: gibt false zurück, wenn stattdessen alles eingelesen werden muss
        bool ExecutePartial(IList<string> folders)
        {
            // Gewählte Ordner prüfen; der STEFFLIX-Ordner selbst bedeutet "alles"
            List<string> relFolders = new List<string>();
            foreach (string f in folders)
            {
                string rel = ToRelative(ToAbsolute(f));
                if (rel == null) { ctx.Log("Liegt nicht im STEFFLIX-Ordner, übersprungen: " + f); continue; }
                if (rel.Length == 0) { ctx.Log("Der ganze STEFFLIX-Ordner ist gewählt: alles wird eingelesen."); return false; }
                if (Folders.IgnoreAny(homeDir, rel)) { ctx.Log("Ablage-, System- oder versteckter Ordner bzw. Verknüpfung, übersprungen: " + rel); continue; }
                relFolders.Add(rel);
            }
            // Doppelte weglassen, ebenso Ordner, die in einem anderen gewählten Ordner liegen
            List<string> unique = new List<string>();
            foreach (string a in relFolders)
            {
                bool covered = false;
                foreach (string b in relFolders)
                    if (a.StartsWith(b + "\\", StringComparison.OrdinalIgnoreCase)) covered = true;
                foreach (string u in unique)
                    if (string.Equals(u, a, StringComparison.OrdinalIgnoreCase)) covered = true;
                if (!covered) unique.Add(a);
            }
            if (unique.Count == 0)
            {
                ctx.Log("Keine gültigen Ordner gewählt, es wurde nichts eingelesen.");
                return true;
            }

            JsonObject data = LoadExistingData();
            if (data == null)
            {
                ctx.Log("data.js fehlt oder ist nicht lesbar: alles wird eingelesen.");
                return false;
            }

            foreach (string rel in unique)
            {
                ctx.Check();
                ctx.Progress(-1, "Lese ein: " + rel);
                UpdateBranch(data, rel.Split('\\'));
            }
            ctx.Log("Teilweise eingelesen: " + unique.Count + " Ordner, " + branchMedia + " Medien gefunden");
            SaveData(data);
            List<string> categories = new List<string>();
            foreach (string rel in unique)
            {
                string category = rel.Split('\\')[0];
                if (!categories.Contains(category)) categories.Add(category);
            }
            LogOverview(data, categories);
            ctx.Summary = unique.Count + " Ordner, " + branchMedia + " Medien";
            LogSummary();
            return true;
        }

        // ---------------------------------------------------------------
        // Übersicht je Kategorie (Protokoll)
        // ---------------------------------------------------------------

        class Counts
        {
            public int Videos, Music, Images, Folders, MissingPreviews;
            public void Add(Counts c)
            {
                Videos += c.Videos; Music += c.Music; Images += c.Images; Folders += c.Folders; MissingPreviews += c.MissingPreviews;
            }
        }

        static readonly HashSet<string> VideoTypes = Bare(MediaTypes.ReadyVideo);
        static readonly HashSet<string> MusicTypes = Bare(MediaTypes.ReadyAudio);

        static void CountNode(JsonObject node, Counts c)
        {
            HashSet<string> previews = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            JsonObject preview = node.Obj(PreviewFolder);
            if (preview != null)
                foreach (string key in preview.Keys)
                {
                    string name = key.Substring(key.LastIndexOf('/') + 1);
                    int dot = name.LastIndexOf('.');
                    previews.Add(dot > 0 ? name.Substring(0, dot) : name);
                }
            foreach (KeyValuePair<string, object> kv in node)
            {
                if (kv.Key == PreviewFolder) continue;
                JsonObject child = kv.Value as JsonObject;
                if (child != null)
                {
                    c.Folders++;
                    CountNode(child, c);
                    continue;
                }
                string file = kv.Key.Substring(kv.Key.LastIndexOf('/') + 1);
                int dot = file.LastIndexOf('.');
                string ext = dot >= 0 ? file.Substring(dot + 1).ToLowerInvariant() : "";
                if (VideoTypes.Contains(ext))
                {
                    c.Videos++;
                    if (!previews.Contains(file.Substring(0, dot))) c.MissingPreviews++;
                }
                else if (MusicTypes.Contains(ext)) c.Music++;
                else c.Images++;
            }
        }

        static string Describe(Counts c)
        {
            List<string> parts = new List<string>();
            if (c.Videos > 0) parts.Add(c.Videos + (c.Videos == 1 ? " Video" : " Videos"));
            if (c.Music > 0) parts.Add(c.Music + (c.Music == 1 ? " Titel" : " Titel"));
            if (c.Images > 0) parts.Add(c.Images + (c.Images == 1 ? " Bild" : " Bilder"));
            if (c.Folders > 0) parts.Add(c.Folders + " Ordner");
            if (parts.Count == 0) parts.Add("leer");
            string text = string.Join(", ", parts.ToArray());
            if (c.MissingPreviews > 0) text += "  –  " + c.MissingPreviews + (c.MissingPreviews == 1 ? " Video ohne Vorschaubild" : " Videos ohne Vorschaubild");
            return text;
        }

        // Je Kategorie (oberste Ordner): Videos, Musik, Bilder, Ordner, Videos ohne eigenes Vorschaubild.
        // only: nur diese Kategorien (Teil-Einlesen), null = alle
        void LogOverview(JsonObject data, ICollection<string> only)
        {
            Counts total = new Counts();
            List<string[]> rows = new List<string[]>();
            foreach (KeyValuePair<string, object> kv in data)
            {
                JsonObject category = kv.Value as JsonObject;
                if (category == null || kv.Key == PreviewFolder) continue;
                if (only != null && !only.Contains(kv.Key)) continue;
                Counts c = new Counts();
                CountNode(category, c);
                total.Add(c);
                rows.Add(new string[] { kv.Key, Describe(c) });
            }
            if (rows.Count == 0) return;
            int width = 8;
            foreach (string[] r in rows) width = Math.Max(width, r[0].Length + 2);
            ctx.Log(only == null ? "Übersicht:" : "Übersicht der eingelesenen Kategorien:");
            foreach (string[] r in rows) ctx.Log("  " + r[0].PadRight(width) + r[1]);
            if (rows.Count > 1) ctx.Log("  " + "Gesamt".PadRight(width) + Describe(total));
        }

        // Ersetzt den Zweig eines Ordners in data.js. Fehlende Ordner auf dem Weg werden angelegt,
        // ein nicht mehr vorhandener Ordner wird entfernt. Der Preview-Ordner darüber (Bild des
        // Ordners selbst) wird ebenfalls neu eingelesen.
        void UpdateBranch(JsonObject data, string[] segments)
        {
            JsonObject node = data;
            // Nicht kürzen: Bei STEFFLIX direkt im Laufwerk (D:\) wäre "D:" + "Filme" = "D:Filme",
            // ein relativer Pfad, und in data.js stünde "./ilme/..."
            string dir = homeDir;
            for (int i = 0; i < segments.Length; i++)
            {
                string name = segments[i];
                bool last = i == segments.Length - 1;
                if (!Directory.Exists(Path.Combine(dir, name)))
                {
                    // Zweig mit Medien entfernen nur nach Rückfrage (Laufwerk oder Verknüpfungsziel evtl. nur weg)
                    JsonObject gone = node.Obj(name);
                    int media = gone == null ? 0 : CountMedia(gone);
                    if (media > 0 && (ctx.Confirm == null || !ctx.Confirm("Der Ordner „" + Relative(Path.Combine(dir, name)) + "“ ist nicht erreichbar. "
                            + media + " Medien daraus aus der Mediathek entfernen?")))
                    {
                        ctx.Log("  " + Relative(Path.Combine(dir, name)) + ": nicht erreichbar, Einträge bleiben (" + media + " Medien)");
                        return;
                    }
                    if (node.Remove(name)) ctx.Log("  " + Relative(Path.Combine(dir, name)) + ": nicht mehr vorhanden, entfernt");
                    else ctx.Log("  " + Relative(Path.Combine(dir, name)) + ": nicht gefunden");
                    RefreshPreview(node, dir);
                    return;
                }
                // Ungültige Zeichen korrigieren wie beim vollständigen Einlesen
                string fixedName = RenameIfNeeded(dir, name, false);
                if (fixedName == null) return;
                if (fixedName != name) node.Remove(name);

                if (last)
                {
                    RefreshPreview(node, dir);
                    int before = counter;
                    node[fixedName] = Crawl(Path.Combine(dir, fixedName));
                    branchMedia += counter - before;
                    ctx.Log("  " + Relative(Path.Combine(dir, fixedName)) + ": " + (counter - before) + " Medien");
                    OrderLikeFolder(node, dir);
                    return;
                }
                JsonObject child = node.Obj(fixedName);
                if (child == null)
                {
                    child = new JsonObject();
                    node[fixedName] = child;
                    OrderLikeFolder(node, dir);
                }
                node = child;
                dir = Path.Combine(dir, fixedName);
            }
        }

        // Preview-Ordner eines Ordners neu einlesen (falls vorhanden)
        void RefreshPreview(JsonObject node, string dir)
        {
            string preview = Path.Combine(dir, PreviewFolder);
            if (Directory.Exists(preview))
            {
                bool isNew = !node.ContainsKey(PreviewFolder);
                node[PreviewFolder] = Crawl(preview);
                if (isNew) OrderLikeFolder(node, dir);
            }
            else node.Remove(PreviewFolder);
        }

        // Einträge in derselben Reihenfolge wie beim vollständigen Einlesen (Reihenfolge im
        // Ordner). Teile der Weboberfläche zeigen Einträge in dieser Reihenfolge an.
        static void OrderLikeFolder(JsonObject node, string dir)
        {
            Dictionary<string, int> position = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string[] entries = Directory.GetFileSystemEntries(dir);
                for (int i = 0; i < entries.Length; i++) position[Path.GetFileName(entries[i])] = i;
            }
            catch { return; }
            node.SortKeys(delegate(string key)
            {
                // Dateien stehen als "./Pfad/Datei.mp4" in data.js, Ordner mit ihrem Namen
                string name = key.StartsWith("./") ? key.Substring(key.LastIndexOf('/') + 1) : key;
                int p;
                return position.TryGetValue(name, out p) ? p : int.MaxValue;
            });
        }

        void Execute()
        {
            ctx.Progress(-1, "Durchsuche Ordner ...");
            JsonObject filelist = Crawl(homeDir);
            ctx.Log("Ordner durchsucht: " + counter + " Medien gefunden");

            ctx.Summary = counter + " Medien";
            if (filelist.Count > 0 && !PlausibleFullScan())
            {
                ctx.Log("data.js bleibt unverändert.");
                ctx.Summary = "abgebrochen, data.js unverändert";
                LogSummary();
                return;
            }
            if (filelist.Count > 0)
            {
                SaveData(filelist);
                LogOverview(filelist, null);

                // Bis Version 2.01 wurden verkleinerte Kopien in sflix_sys\thumbs abgelegt
                string oldThumbs = Path.Combine(homeDir, "sflix_sys", "thumbs");
                if (Directory.Exists(oldThumbs))
                {
                    try { Directory.Delete(oldThumbs, true); ctx.Log("Alte verkleinerte Vorschaubilder entfernt (sflix_sys/thumbs)"); }
                    catch { }
                }
            }
            else
            {
                ctx.Log("Im STEFFLIX-Verzeichnis wurden keine Medien gefunden.");
            }
            LogSummary();
        }

        // Medien (Dateien) in einem Zweig von data.js
        static int CountMedia(JsonObject node)
        {
            int n = 0;
            foreach (KeyValuePair<string, object> kv in node)
            {
                JsonObject child = kv.Value as JsonObject;
                if (child != null) n += CountMedia(child);
                else n++;
            }
            return n;
        }

        // Vollständiges Einlesen: deutlich weniger Medien als in der bisherigen data.js oder nicht lesbare
        // Ordner (z.B. Netzlaufwerk oder Ziel einer Verknüpfung nicht erreichbar) -> nachfragen, bevor die
        // Einträge aus der Mediathek verschwinden. Ohne Oberfläche: nicht speichern.
        bool PlausibleFullScan()
        {
            JsonObject old = LoadExistingData();
            int before = old == null ? 0 : CountMedia(old);
            bool drop = before >= 50 && counter < before / 2;
            if (!drop && unreadableFolders.Count == 0) return true;
            StringBuilder text = new StringBuilder();
            if (drop) text.Append("Es wurden nur " + counter + " Medien gefunden, in der bisherigen data.js sind es " + before + ".\n");
            if (unreadableFolders.Count > 0)
            {
                text.Append(unreadableFolders.Count + " Ordner waren nicht lesbar, z.B.:\n");
                for (int i = 0; i < Math.Min(5, unreadableFolders.Count); i++) text.Append("  " + unreadableFolders[i] + "\n");
            }
            text.Append("\nIst ein Laufwerk oder das Ziel einer Verknüpfung nicht erreichbar, fehlen diese Medien danach in STEFFLIX " +
                        "(Favoriten und Zeitstempel bleiben erhalten).\n\ndata.js trotzdem speichern?");
            ctx.Log("Hinweis: " + text.ToString().Replace("\n", " ").Trim());
            return ctx.Confirm != null && ctx.Confirm(text.ToString());
        }

        void LogSummary()
        {
            if (skippedLinks.Count > 0)
            {
                ctx.Log("Hinweis: Verknüpfungen auf Ordner (Junction, symbolischer Link) werden nicht eingelesen:");
                foreach (string d in skippedLinks) ctx.Log("  " + d);
            }
            if (innerUnderscore.Count > 0)
            {
                ctx.Log("Hinweis: Ordner mit „__“ mitten im Namen werden seit Desktop 4.1.0 eingelesen (nur „__“ am Anfang ist eine Ablage):");
                foreach (string d in innerUnderscore) ctx.Log("  " + d);
            }
            if (shrinkCounter > 0)
                ctx.Log(shrinkCounter + " Vorschaubilder verkleinert (Original in Preview\\" + OriginalFolder + " gesichert)");
            if (migratedOriginals > 0)
                ctx.Log(migratedOriginals + " ältere .original-Dateien nach Preview\\" + OriginalFolder + " verschoben");
            if (notShrinkable > 0)
                ctx.Log(notShrinkable + " WebP-Vorschaubilder sind grösser als 480x270 und bleiben unverändert");

            if (renamedItems.Count > 0)
            {
                ctx.Log("Umbenannt, weil ungültige Zeichen enthalten waren:");
                foreach (string[] r in renamedItems) ctx.Log("  " + Relative(r[0]) + "  ->  " + r[1]);
            }
            if (errorList.Count > 0)
            {
                ctx.Log("Enthalten ungültige Zeichen und konnten nicht umbenannt werden (werden ignoriert):");
                foreach (string e in errorList) ctx.Log("  " + Relative(e));
            }
            if (skippedFolders.Count > 0)
            {
                ctx.Log("Kein Zugriff, übersprungen:");
                foreach (string s in skippedFolders) ctx.Log("  " + s);
            }
        }
    }
}
