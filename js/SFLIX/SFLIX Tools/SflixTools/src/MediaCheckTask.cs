// Tool "Medien prüfen" (seit 3.3): findet Medien, die der Browser nicht abspielen kann, defekte und
// leere Dateien sowie Videos ohne Vorschaubild. Im Ergebnis lassen sich Einträge anhaken und gleich
// konvertieren bzw. Vorschaubilder erstellen. Ohne Oberfläche wird nur das Ergebnis protokolliert.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    public static class MediaCheck
    {
        // Spielt der Browser die Datei ab? (Chrome, Edge, Firefox)
        public static bool IsPlayable(MediaFacts f, string ext)
        {
            if (!f.HasVideo)
            {
                // Musik: die Endungen in MediaTypes.ReadyAudio spielen alle Browser
                return MediaTypes.ReadyAudio.Contains(ext) || MediaTypes.ReadyVideo.Contains(ext);
            }
            List<string> audio = f.AudioCodecList;
            if (ext == ".webm")
            {
                bool v = f.VideoCodec == "vp8" || f.VideoCodec == "vp9" || f.VideoCodec == "av1";
                foreach (string a in audio) if (a != "opus" && a != "vorbis") return false;
                return v;
            }
            if (ext == ".ogv") return f.VideoCodec == "theora";
            if (ext != ".mp4" && ext != ".m4v") return false;
            // H.264 nur mit 8 Bit 4:2:0, HEVC (H.265) spielen Firefox und viele PCs nicht
            bool video = f.VideoCodec == "h264" && (f.PixelFormat == "yuv420p" || f.PixelFormat == "yuvj420p");
            bool sound = true;
            foreach (string a in audio) if (a != "aac" && a != "mp3") sound = false;
            return video && sound;
        }

        // Grund in Worten, warum eine Datei nicht abspielbar ist
        public static string Reason(MediaFacts f, string ext)
        {
            if (MediaTypes.NeedsConversion(ext)) return "Format " + ext + " wird vom Browser nicht abgespielt";
            List<string> parts = new List<string>();
            if (f.HasVideo && !(f.VideoCodec == "h264" && (f.PixelFormat == "yuv420p" || f.PixelFormat == "yuvj420p")) && ext != ".webm")
                parts.Add("Video " + f.VideoCodec + (f.PixelFormat.Contains("10") ? " 10 Bit" : ""));
            foreach (string a in f.AudioCodecList)
                if (a != "aac" && a != "mp3" && a != "opus" && a != "vorbis") { parts.Add("Ton " + a); break; }
            return parts.Count > 0 ? string.Join(", ", parts.ToArray()) : "Codec wird nicht unterstützt";
        }
    }

    public class MediaCheckItem
    {
        public string Path;
        public string Kind;     // "format", "broken", "empty", "preview"
        public string Problem;
        public MediaFacts Facts;
        public bool CanConvert { get { return Kind == "format" && MediaTypes.Kind(System.IO.Path.GetExtension(Path).ToLowerInvariant()) == "video"; } }
    }

    public class MediaCheckChoice
    {
        public string Action;               // "convert" oder "preview"
        public List<string> Files = new List<string>();
    }

    public static class MediaCheckTask
    {
        public static void Run(TaskContext ctx, List<string> folders, ConvertOptions convert, bool previewVideos, bool previewImages)
        {
            ctx.Ffmpeg.EnsureInstalled(ctx);
            List<string> roots = Folders.WithoutNested(folders != null && folders.Count > 0 ? folders : new List<string> { App.Root });
            HashSet<string> extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (HashSet<string> set in new HashSet<string>[] { MediaTypes.ReadyVideo, MediaTypes.ReadyAudio, MediaTypes.ReadyImage,
                                                                    MediaTypes.ConvertVideo, MediaTypes.ConvertAudio, MediaTypes.ConvertImage })
                extensions.UnionWith(set);

            ctx.Progress(-1, "Suche Medien ...");
            List<string> files = new List<string>();
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) throw new TaskFailedException("Ordner nicht gefunden: " + root);
                Folders.CollectFiles(root, extensions, true, files, ctx.Token);
            }
            files.RemoveAll(delegate(string f) { string n = Path.GetFileName(f); return n.StartsWith("._") || n.Contains(".__temp__"); });
            ctx.Log(files.Count + " Mediendateien gefunden in " + string.Join(", ", roots.ToArray()));

            List<MediaCheckItem> items = new List<MediaCheckItem>();
            for (int i = 0; i < files.Count; i++)
            {
                ctx.Check();
                string file = files[i];
                if (i % 5 == 0) ctx.Progress((double)i / Math.Max(1, files.Count), "Prüfe " + (i + 1) + " / " + files.Count + ": " + Path.GetFileName(file));
                string ext = Path.GetExtension(file).ToLowerInvariant();
                string kind = MediaTypes.Kind(ext);
                MediaFacts facts = MediaFacts.Basic(file);
                if (facts.Size == 0) { items.Add(Item(file, "empty", "leere Datei (0 Byte)", facts)); continue; }
                if (MediaTypes.NeedsConversion(ext))
                {
                    if (kind == "video") facts.Probe(ctx.Ffmpeg, ctx.Token);
                    items.Add(Item(file, "format", MediaCheck.Reason(facts, ext), facts));
                    continue;
                }
                if (kind == "video")
                {
                    facts.Probe(ctx.Ffmpeg, ctx.Token);
                    if (facts.ProbeError != null) { items.Add(Item(file, "broken", "defekt: " + facts.ProbeError, facts)); continue; }
                    if (!MediaCheck.IsPlayable(facts, ext)) items.Add(Item(file, "format", MediaCheck.Reason(facts, ext), facts));
                    if (previewVideos && !HasPreview(file)) items.Add(Item(file, "preview", "kein Vorschaubild", facts));
                }
                else if (kind == "image" && previewImages && !HasPreview(file))
                {
                    items.Add(Item(file, "preview", "kein Vorschaubild", facts));
                }
            }

            int format = 0, broken = 0, empty = 0, preview = 0;
            foreach (MediaCheckItem it in items)
            {
                if (it.Kind == "format") format++;
                else if (it.Kind == "broken") broken++;
                else if (it.Kind == "empty") empty++;
                else preview++;
                ctx.Log("  " + (it.Kind == "preview" ? "Hinweis: " : it.Kind == "format" ? "WARNUNG " : "FEHLER ") + it.Problem + "  –  " + it.Path);
            }
            ctx.Log("Nicht abspielbar: " + format + "   Defekt: " + broken + "   Leer: " + empty + "   Ohne Vorschaubild: " + preview);
            ctx.Summary = files.Count + " geprüft, " + format + " nicht abspielbar, " + broken + " defekt, " + empty + " leer, " + preview + " ohne Vorschaubild";
            if (items.Count == 0 || ctx.ChooseMediaCheck == null) return;

            MediaCheckChoice choice = ctx.ChooseMediaCheck(items);
            if (choice == null || choice.Files.Count == 0) return;
            if (choice.Action == "convert")
            {
                ctx.Log("");
                ctx.Log("Konvertiere " + choice.Files.Count + " Video(s) ...");
                foreach (string root in roots)
                {
                    HashSet<string> only = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string f in choice.Files) if (f.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) only.Add(f);
                    if (only.Count == 0) continue;
                    convert.Folder = root;
                    convert.OnlyFiles = only;
                    try { ConvertTask.Run(ctx, convert); }
                    finally { convert.OnlyFiles = null; }
                }
            }
            else if (choice.Action == "preview")
            {
                ctx.Log("");
                ctx.Log("Erstelle " + choice.Files.Count + " Vorschaubild(er) ...");
                PreviewTask.MakeFor(ctx, choice.Files);
            }
        }

        static MediaCheckItem Item(string path, string kind, string problem, MediaFacts facts)
        {
            MediaCheckItem it = new MediaCheckItem();
            it.Path = path; it.Kind = kind; it.Problem = problem; it.Facts = facts;
            return it;
        }

        static bool HasPreview(string file)
        {
            string dir = Path.Combine(Path.GetDirectoryName(file), "Preview");
            string stem = Path.GetFileNameWithoutExtension(file);
            foreach (string ext in new string[] { ".jpg", ".jpeg", ".png", ".webp" })
                if (File.Exists(Path.Combine(dir, stem + ext))) return true;
            return false;
        }
    }

    // Ergebnis von "Medien prüfen": anhaken, dann konvertieren oder Vorschaubilder erstellen
    public class MediaCheckDialog : Form
    {
        readonly ListView list;
        readonly Button convertButton, previewButton;
        readonly Label info;
        MediaCheckChoice result;

        public MediaCheckDialog(List<MediaCheckItem> items)
        {
            Text = "Medien prüfen – Ergebnis";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Theme.S(1040), Theme.S(600));
            MinimumSize = new Size(Theme.S(700), Theme.S(400));
            MinimizeBox = false;
            ShowInTaskbar = false;

            Label intro = Theme.MakeLabel(items.Count + " Auffälligkeiten gefunden. Einträge anhaken und wählen, was mit ihnen passieren soll. " +
                                          "Konvertieren geht für Videos (Originale nach „" + ConvertTask.OriginalsFolderName + "“), Musik und Bilder über den Importer. " +
                                          "Defekte und leere Dateien bitte von Hand ersetzen oder löschen.", true);
            intro.Dock = DockStyle.Top;
            intro.AutoSize = true;
            intro.MaximumSize = new Size(ClientSize.Width - Theme.S(24), 0);
            intro.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(8));

            list = new ListView();
            list.View = View.Details;
            list.CheckBoxes = true;
            list.FullRowSelect = true;
            list.Dock = DockStyle.Fill;
            list.BackColor = Theme.Card;
            list.ForeColor = Theme.Text;
            list.BorderStyle = BorderStyle.None;
            list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            list.ShowItemToolTips = true;
            DarkList.Apply(list);
            list.Columns.Add("Datei", Theme.S(300));
            list.Columns.Add("Problem", Theme.S(260));
            list.Columns.Add("Grösse", Theme.S(80));
            list.Columns.Add("Video / Ton", Theme.S(150));
            list.Columns.Add("Ordner", Theme.S(240));
            List<MediaCheckItem> sorted = new List<MediaCheckItem>(items);
            sorted.Sort(delegate(MediaCheckItem a, MediaCheckItem b)
            {
                int k = Order(a.Kind).CompareTo(Order(b.Kind));
                return k != 0 ? k : string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });
            foreach (MediaCheckItem it in sorted)
            {
                ListViewItem row = new ListViewItem(Path.GetFileName(it.Path));
                row.SubItems.Add(it.Problem);
                row.SubItems.Add(Fmt.Size(Math.Max(0, it.Facts.Size)));
                row.SubItems.Add(it.Facts.CodecText);
                row.SubItems.Add(DataTask.ToRelative(Path.GetDirectoryName(it.Path)) ?? Path.GetDirectoryName(it.Path));
                row.ToolTipText = it.Path;
                row.Tag = it;
                row.ForeColor = it.Kind == "broken" || it.Kind == "empty" ? Color.FromArgb(0xff, 0x6b, 0x6b)
                              : it.Kind == "format" ? Color.FromArgb(0xf0, 0xc4, 0x6c) : Theme.Text;
                list.Items.Add(row);
            }
            list.ItemChecked += delegate { UpdateButtons(); };

            Panel listHost = new Panel();
            listHost.Dock = DockStyle.Fill;
            listHost.Padding = new Padding(Theme.S(12), 0, Theme.S(12), 0);
            listHost.Controls.Add(list);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Bottom;
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Padding = new Padding(Theme.S(12), Theme.S(10), Theme.S(12), Theme.S(12));
            Button close = Theme.MakeButton("Schliessen", false);
            close.Click += delegate { result = null; DialogResult = DialogResult.Cancel; };
            convertButton = Theme.MakeButton("Ausgewählte konvertieren", true);
            convertButton.Click += delegate { Finish("convert"); };
            previewButton = Theme.MakeButton("Vorschaubilder erstellen", false);
            previewButton.Click += delegate { Finish("preview"); };
            Button allFormat = Theme.MakeButton("Alle nicht abspielbaren", false);
            allFormat.Click += delegate { CheckKind("format"); };
            Button allPreview = Theme.MakeButton("Alle ohne Vorschaubild", false);
            allPreview.Click += delegate { CheckKind("preview"); };
            info = Theme.MakeLabel("", true);
            info.Margin = new Padding(0, Theme.S(8), Theme.S(12), 0);
            buttons.Controls.Add(close);
            buttons.Controls.Add(convertButton);
            buttons.Controls.Add(previewButton);
            buttons.Controls.Add(allPreview);
            buttons.Controls.Add(allFormat);
            buttons.Controls.Add(info);

            Controls.Add(listHost);
            Controls.Add(buttons);
            Controls.Add(intro);
            CancelButton = close;
            UpdateButtons();
        }

        static int Order(string kind)
        {
            return kind == "broken" ? 0 : kind == "empty" ? 1 : kind == "format" ? 2 : 3;
        }

        void CheckKind(string kind)
        {
            foreach (ListViewItem row in list.Items) row.Checked = ((MediaCheckItem)row.Tag).Kind == kind;
        }

        List<MediaCheckItem> Checked()
        {
            List<MediaCheckItem> l = new List<MediaCheckItem>();
            for (int i = 0; i < list.Items.Count; i++)
            {
                ListViewItem row = list.Items[i];
                if (row != null && row.Checked && row.Tag is MediaCheckItem) l.Add((MediaCheckItem)row.Tag);
            }
            return l;
        }

        void UpdateButtons()
        {
            // Beim Schliessen meldet die Liste noch Änderungen, während sie abgebaut wird
            if (IsDisposed || Disposing || convertButton == null || info == null) return;
            int convert = 0, preview = 0;
            foreach (MediaCheckItem it in Checked())
            {
                if (it.CanConvert) convert++;
                if (it.Kind == "preview") preview++;
            }
            convertButton.Enabled = convert > 0;
            previewButton.Enabled = preview > 0;
            convertButton.Text = convert > 0 ? convert + " konvertieren" : "Ausgewählte konvertieren";
            previewButton.Text = preview > 0 ? preview + " Vorschaubild(er) erstellen" : "Vorschaubilder erstellen";
            info.Text = Checked().Count + " ausgewählt";
        }

        void Finish(string action)
        {
            result = new MediaCheckChoice();
            result.Action = action;
            foreach (MediaCheckItem it in Checked())
                if ((action == "convert" && it.CanConvert) || (action == "preview" && it.Kind == "preview"))
                    if (!result.Files.Contains(it.Path)) result.Files.Add(it.Path);
            DialogResult = DialogResult.OK;
        }

        public MediaCheckChoice Choose(Form owner)
        {
            Icon = owner.Icon;
            ShowDialog(owner);
            return result;
        }
    }
}
