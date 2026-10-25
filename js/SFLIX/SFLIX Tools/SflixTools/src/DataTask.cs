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
        static readonly HashSet<string> AllowedFileTypes = new HashSet<string> {
            "mp4", "m4v", "webm", "ogv",
            "mp3", "m4a", "ogg", "oga", "opus", "aac", "flac", "wav",
            "jpg", "jpeg", "png", "gif", "webp", "avif", "bmp",
        };
        static readonly string[] DisallowedItems = { "System Volume Information", "$RECYCLE.BIN", ".vscode", "sflix_sys" };

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
        int shrinkCounter;
        int notShrinkable;
        int migratedOriginals;
        int counter;

        DataTask(TaskContext ctx)
        {
            this.ctx = ctx;
            homeDir = App.Root;
        }

        public static void Run(TaskContext ctx)
        {
            new DataTask(ctx).Execute();
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
                // Zeichen ausserhalb der BMP (z.B. Emojis) bestehen aus zwei char
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

            string[] entries;
            try
            {
                entries = Directory.GetFileSystemEntries(path);
            }
            catch (UnauthorizedAccessException)
            {
                skippedFolders.Add(path);
                return data;
            }

            foreach (string entry in entries)
            {
                string item = Path.GetFileName(entry);
                string fullPath = Path.Combine(path, item);
                bool isFile = File.Exists(fullPath);
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
                // Ordner mit "__" im Namen sind Ablagen der Werkzeuge (__teile, __konvertiert,
                // __gelöscht) und gehören nicht in die Mediathek
                else if (Directory.Exists(fullPath) && Array.IndexOf(DisallowedItems, item) < 0 && !Folders.IsToolFolder(item))
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

        void Execute()
        {
            ctx.Progress(-1, "Durchsuche Ordner ...");
            JsonObject filelist = Crawl(homeDir);
            ctx.Log("Ordner durchsucht: " + counter + " Medien gefunden");

            string dataFile = Path.Combine(homeDir, "sflix_sys", "data.js");
            if (filelist.Count > 0)
            {
                if (File.Exists(dataFile)) FileOps.Replace(dataFile, dataFile + ".bak");
                File.WriteAllText(dataFile, "var data = " + Json.Write(filelist) + ";", FileOps.Utf8NoBom);
                ctx.Log("data.js gespeichert (Sicherung: data.js.bak)");

                try { WriteRenamedPaths(); }
                catch (Exception) { ctx.Log("Umbenennungen konnten nicht gespeichert werden"); }

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
