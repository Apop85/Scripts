// Vergleich von Mediendateien (seit 3.3): Grösse, Auflösung, Dauer, Video/Ton, Datum und ob der
// Browser die Datei abspielen kann. Genutzt von "Duplikate finden", "Folgen umbenennen" (Vorschau)
// und dem Dialog "Datei vorhanden" (Importer, Folgen umbenennen). Die besseren Werte einer Gruppe
// sind grün hervorgehoben. Auflösung, Dauer und Codecs kommen von ffprobe; fehlt ffmpeg, stehen
// nur Grösse und Datum da, mit einem Knopf zum Installieren.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Sflix
{
    // Angaben zu einer Datei
    public class MediaFacts
    {
        public string Path;
        public long Size = -1;
        public DateTime Modified;
        public bool Probed;          // ffprobe gelaufen
        public string ProbeError;    // ffprobe konnte die Datei nicht lesen
        public int Width, Height;
        public double Duration;
        public string VideoCodec = "", AudioCodecs = "", PixelFormat = "", Container = "";
        public List<string> AudioCodecList = new List<string>();
        public bool HasVideo;

        public static MediaFacts Basic(string path)
        {
            MediaFacts f = new MediaFacts();
            f.Path = path;
            try
            {
                FileInfo fi = new FileInfo(path);
                if (fi.Exists) { f.Size = fi.Length; f.Modified = fi.LastWriteTime; }
            }
            catch { }
            return f;
        }

        // Mit ffprobe ergänzen (nur Videos und Musik). Fehler stehen in ProbeError.
        public void Probe(FfmpegTools ff, CancellationToken token)
        {
            if (ff == null || !ff.Installed || Size <= 0 || MediaTypes.Kind(System.IO.Path.GetExtension(Path).ToLowerInvariant()) == "image") return;
            try
            {
                MediaInfo info = ff.Probe(Path, token);
                Probed = true;
                Duration = info.Duration;
                Width = info.Width;
                Height = info.Height;
                HasVideo = info.VideoIndex >= 0;
                VideoCodec = info.VideoCodec ?? "";
                PixelFormat = info.PixelFormat ?? "";
                Container = info.Container ?? "";
                List<string> audio = new List<string>();
                AudioCodecList.Clear();
                foreach (AudioStream a in info.Audio)
                {
                    AudioCodecList.Add(a.Codec);
                    audio.Add(a.Codec + (string.IsNullOrEmpty(a.Language) || a.Language == "und" ? "" : " " + a.Language));
                }
                AudioCodecs = string.Join(", ", audio.ToArray());
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e) { ProbeError = e.Message; }
        }

        public string ResolutionText
        {
            get
            {
                if (!Probed) return "–";
                if (!HasVideo) return "kein Video";
                string label = Height >= 2000 ? "4K" : Height >= 1000 ? "1080p" : Height >= 700 ? "720p" : Height >= 560 ? "576p" : Height + "p";
                return Width + "×" + Height + " (" + label + ")";
            }
        }

        public string CodecText
        {
            get
            {
                if (!Probed) return ProbeError != null ? "nicht lesbar" : "–";
                string v = HasVideo ? VideoCodec + (PixelFormat.Contains("10") ? " 10 Bit" : "") : "";
                return (v.Length > 0 ? v : "–") + " / " + (AudioCodecs.Length > 0 ? AudioCodecs : "kein Ton");
            }
        }

        // Kann der Browser die Datei abspielen? null = unbekannt (nicht untersucht)
        public bool? Playable
        {
            get
            {
                string ext = System.IO.Path.GetExtension(Path).ToLowerInvariant();
                if (MediaTypes.NeedsConversion(ext)) return false;
                if (Size == 0) return false;
                if (ProbeError != null) return false;
                if (!Probed) return null;
                return MediaCheck.IsPlayable(this, ext);
            }
        }

        public long Pixels { get { return (long)Width * Height; } }
    }

    // Ein Eintrag der Tabelle
    public class CompareEntry
    {
        public string Label;          // z.B. "vorhanden", "neu", "behalten"
        public string Path;
        public string Note;           // zusätzlicher Hinweis
        public string Reason;         // Spalte "Grund" (falls vorhanden)
        public bool ConvertNote;      // "nicht abspielbar" mit "wird konvertiert" ergänzen
        public MediaFacts Facts;
        public object Tag;
    }

    public class CompareView : TableLayoutPanel
    {
        public readonly ListView List;
        readonly Label ffmpegHint;
        readonly Button ffmpegButton;
        readonly Panel ffmpegRow;
        List<CompareEntry> entries = new List<CompareEntry>();
        List<List<CompareEntry>> groups = new List<List<CompareEntry>>();
        FfmpegTools ff;
        int generation;
        static readonly Color Better = Color.FromArgb(0x5e, 0xd3, 0x8a);
        static readonly Color Warn = Color.FromArgb(0xf0, 0xc4, 0x6c);

        // Häkchen je Zeile (z.B. Duplikate: aussortieren)
        public bool CheckBoxes { get { return List.CheckBoxes; } set { List.CheckBoxes = value; } }

        public CompareView(FfmpegTools ffmpeg, bool withLabelColumn) : this(ffmpeg, withLabelColumn, false) { }

        // withReasonColumn: Spalte "Grund" (Duplikate: Datei, Folge, Dateiname)
        public CompareView(FfmpegTools ffmpeg, bool withLabelColumn, bool withReasonColumn)
        {
            reasonColumn = withReasonColumn;
            ff = ffmpeg;
            ColumnCount = 1;
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            RowCount = 2;
            RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            RowStyles.Add(new RowStyle(SizeType.AutoSize));
            BackColor = Theme.Back;
            Margin = new Padding(0);

            List = new ListView();
            List.View = View.Details;
            List.FullRowSelect = true;
            List.Dock = DockStyle.Fill;
            List.BackColor = Theme.Card;
            List.ForeColor = Theme.Text;
            List.BorderStyle = BorderStyle.None;
            List.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            List.ShowItemToolTips = true;
            DarkList.Apply(List);
            // Sortieren innerhalb der Gruppen, die Gruppen nach ihrem ersten Eintrag
            sort = ListSort.Of(List);
            sort.Custom = delegate { if (groups != null) Render(); };
            if (withLabelColumn) List.Columns.Add("", Theme.S(90));
            if (withReasonColumn) List.Columns.Add("Grund", Theme.S(80));
            List.Columns.Add("Datei", Theme.S(300));
            List.Columns.Add("Grösse", Theme.S(80));
            List.Columns.Add("Auflösung", Theme.S(130));
            List.Columns.Add("Dauer", Theme.S(70));
            List.Columns.Add("Video / Ton", Theme.S(150));
            List.Columns.Add("Geändert", Theme.S(110));
            List.Columns.Add("Hinweis", Theme.S(170));
            labelColumn = withLabelColumn;

            ffmpegRow = new Panel();
            ffmpegRow.Dock = DockStyle.Fill;
            ffmpegRow.Height = Theme.S(36);
            ffmpegRow.Padding = new Padding(0, Theme.S(6), 0, 0);
            ffmpegRow.Visible = false;
            ffmpegHint = Theme.MakeLabel("Auflösung, Dauer und Codecs zeigt der Updater mit ffmpeg.", true);
            ffmpegHint.Dock = DockStyle.Fill;
            ffmpegHint.AutoSize = false;
            ffmpegHint.TextAlign = ContentAlignment.MiddleLeft;
            ffmpegButton = Theme.MakeButton("ffmpeg installieren (ca. 100 MB)", false);
            ffmpegButton.Dock = DockStyle.Right;
            ffmpegButton.Click += delegate { InstallFfmpeg(); };
            ffmpegRow.Controls.Add(ffmpegHint);
            ffmpegRow.Controls.Add(ffmpegButton);

            Controls.Add(List, 0, 0);
            Controls.Add(ffmpegRow, 0, 1);
            // Überschriftzeilen der Gruppen lassen sich nicht anhaken
            List.ItemCheck += delegate(object s, ItemCheckEventArgs e)
            {
                if (!(List.Items[e.Index].Tag is CompareEntry)) e.NewValue = CheckState.Unchecked;
            };
        }

        readonly bool labelColumn, reasonColumn;
        readonly ListSort sort;
        Font boldFont;

        // Für sehr viele Zeilen nur eine fette Schrift (jede Schrift belegt ein GDI-Handle)
        Font Bold
        {
            get
            {
                if (boldFont == null) { boldFont = new Font(List.Font, FontStyle.Bold); Disposed += delegate { boldFont.Dispose(); }; }
                return boldFont;
            }
        }

        // Sortierschlüssel einer Spalte
        object KeyOf(CompareEntry e, int col)
        {
            int c = col;
            if (labelColumn) { if (c == 0) return e.Label ?? ""; c--; }
            if (reasonColumn) { if (c == 0) return e.Reason ?? ""; c--; }
            MediaFacts f = e.Facts;
            switch (c)
            {
                case 0: return FileLabel(e);
                case 1: return f.Size < 0 ? null : (object)f.Size;
                case 2: return f.Probed && f.Pixels > 0 ? (object)f.Pixels : null;
                case 3: return f.Probed && f.Duration > 0 ? (object)f.Duration : null;
                case 4: return f.CodecText;
                case 5: return f.Size < 0 ? null : (object)f.Modified;
                default: return NoteOf(e);
            }
        }

        int CompareEntries(CompareEntry a, CompareEntry b)
        {
            int r = ListSort.CompareKeys(KeyOf(a, sort.Column), KeyOf(b, sort.Column));
            if (r == 0) r = ListSort.Natural(FileLabel(a), FileLabel(b));
            return sort.Descending ? -r : r;
        }

        // Gruppen in Anzeigereihenfolge (Index der Gruppe) und ihre Einträge sortiert
        List<KeyValuePair<int, List<CompareEntry>>> SortedGroups()
        {
            List<KeyValuePair<int, List<CompareEntry>>> result = new List<KeyValuePair<int, List<CompareEntry>>>();
            for (int g = 0; g < groups.Count; g++)
            {
                List<CompareEntry> copy = new List<CompareEntry>(groups[g]);
                if (sort.Column >= 0) StableSort(copy, CompareEntries);
                result.Add(new KeyValuePair<int, List<CompareEntry>>(g, copy));
            }
            if (sort.Column >= 0 && result.Count > 1)
                StableSort(result, delegate(KeyValuePair<int, List<CompareEntry>> a, KeyValuePair<int, List<CompareEntry>> b)
                {
                    if (a.Value.Count == 0 || b.Value.Count == 0) return b.Value.Count.CompareTo(a.Value.Count);
                    return CompareEntries(a.Value[0], b.Value[0]);
                });
            return result;
        }

        static void StableSort<T>(List<T> list, Comparison<T> cmp)
        {
            List<KeyValuePair<int, T>> indexed = new List<KeyValuePair<int, T>>();
            for (int i = 0; i < list.Count; i++) indexed.Add(new KeyValuePair<int, T>(i, list[i]));
            indexed.Sort(delegate(KeyValuePair<int, T> a, KeyValuePair<int, T> b)
            {
                int r = cmp(a.Value, b.Value);
                return r != 0 ? r : a.Key.CompareTo(b.Key);
            });
            for (int i = 0; i < list.Count; i++) list[i] = indexed[i].Value;
        }
        // Spaltenindex ab "Datei" (0), verschoben um die Spalten davor
        int Col(int i) { return i + (labelColumn ? 1 : 0) + (reasonColumn ? 1 : 0); }

        // Zeigt eine Gruppe (z.B. vorhanden/neu)
        public void SetEntries(List<CompareEntry> list)
        {
            SetGroups(new List<List<CompareEntry>> { list });
        }

        // Zeigt mehrere Gruppen (Duplikate), mit Überschriftzeile je Gruppe (Label des ersten Eintrags ist die Überschrift: GroupTitles)
        public List<string> GroupTitles;

        public void SetGroups(List<List<CompareEntry>> newGroups)
        {
            generation++;
            groups = newGroups;
            entries = new List<CompareEntry>();
            foreach (List<CompareEntry> g in groups) entries.AddRange(g);
            foreach (CompareEntry e in entries) if (e.Facts == null) e.Facts = MediaFacts.Basic(e.Path);
            Render();
            // Ergebnisse kommen per BeginInvoke zurück, das braucht das Fenster-Handle
            if (IsHandleCreated) ProbeInBackground();
            else probeWhenReady = true;
        }

        bool probeWhenReady;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (probeWhenReady)
            {
                probeWhenReady = false;
                ProbeInBackground();
            }
        }

        public IEnumerable<CompareEntry> Entries { get { return entries; } }

        void ProbeInBackground()
        {
            bool needsProbe = false;
            foreach (CompareEntry e in entries) if (!e.Facts.Probed && e.Facts.ProbeError == null) needsProbe = true;
            ffmpegRow.Visible = needsProbe && (ff == null || !ff.Installed);
            if (!needsProbe || ff == null || !ff.Installed) return;
            int gen = generation;
            List<CompareEntry> work = new List<CompareEntry>(entries);
            ThreadPool.QueueUserWorkItem(delegate
            {
                foreach (CompareEntry e in work)
                {
                    if (gen != generation || IsDisposed) return;
                    try { e.Facts.Probe(ff, CancellationToken.None); } catch { }
                }
                if (IsDisposed || !IsHandleCreated) return;
                try { BeginInvoke((Action)delegate { if (gen == generation) Render(); }); } catch { }
            });
        }

        void InstallFfmpeg()
        {
            if (ff == null) return;
            ffmpegButton.Enabled = false;
            ffmpegHint.Text = "ffmpeg wird geladen …";
            TaskContext ctx = new TaskContext();
            ctx.Token = CancellationToken.None;
            ctx.LogLine = delegate { };
            ctx.ProgressSink = delegate(double f, string s)
            {
                try { BeginInvoke((Action)delegate { ffmpegHint.Text = s; }); } catch { }
            };
            ThreadPool.QueueUserWorkItem(delegate
            {
                string error = null;
                try { ff.InstallNow(ctx); }
                catch (Exception e) { error = e.Message; }
                try
                {
                    BeginInvoke((Action)delegate
                    {
                        ffmpegButton.Enabled = true;
                        if (error != null) { ffmpegHint.Text = "ffmpeg konnte nicht installiert werden: " + error; return; }
                        ffmpegRow.Visible = false;
                        ProbeInBackground();
                    });
                }
                catch { }
            });
        }

        // Während des Neuaufbaus meldet die ListView angehakte Elemente schon beim Einfügen, bevor
        // sie fertig in der Liste stehen. Wer ItemChecked auswertet, prüft deshalb Rendering und
        // reagiert danach auf Rendered.
        public bool Rendering { get; private set; }
        public Action Rendered;

        void Render()
        {
            Rendering = true;
            try { RenderItems(); }
            finally { Rendering = false; }
            if (Rendered != null) Rendered();
        }

        void RenderItems()
        {
            // Häkchen beibehalten
            Dictionary<CompareEntry, bool> checks = new Dictionary<CompareEntry, bool>();
            for (int i = 0; i < List.Items.Count; i++)
            {
                ListViewItem it = List.Items[i];
                if (it != null && it.Tag is CompareEntry) checks[(CompareEntry)it.Tag] = it.Checked;
            }
            int top = List.TopItem != null ? List.TopItem.Index : 0;
            List.BeginUpdate();
            List.Items.Clear();
            List<ListViewItem> rows = new List<ListViewItem>();
            foreach (KeyValuePair<int, List<CompareEntry>> sortedGroup in SortedGroups())
            {
                int g = sortedGroup.Key;
                List<CompareEntry> group = sortedGroup.Value;
                if (GroupTitles != null && g < GroupTitles.Count)
                {
                    // Gruppenüberschrift in der Spalte "Datei"
                    int fileCol = Col(0);
                    ListViewItem head = new ListViewItem(fileCol == 0 ? GroupTitles[g] : "");
                    for (int hc = 1; hc <= fileCol; hc++) head.SubItems.Add(hc == fileCol ? GroupTitles[g] : "");
                    head.ForeColor = Theme.Muted;
                    head.Font = Bold;
                    head.BackColor = Theme.Input;
                    rows.Add(head);
                }
                // Beste Werte der Gruppe
                long maxPixels = 0, maxSize = 0;
                double maxDuration = 0;
                foreach (CompareEntry e in group)
                {
                    maxPixels = Math.Max(maxPixels, e.Facts.Pixels);
                    maxSize = Math.Max(maxSize, e.Facts.Size);
                    maxDuration = Math.Max(maxDuration, e.Facts.Duration);
                }
                bool differentPixels = false, differentSize = false, differentDuration = false;
                foreach (CompareEntry e in group)
                {
                    if (e.Facts.Pixels != maxPixels) differentPixels = true;
                    if (e.Facts.Size != maxSize) differentSize = true;
                    if (Math.Abs(e.Facts.Duration - maxDuration) > 2) differentDuration = true;
                }
                foreach (CompareEntry e in group)
                {
                    MediaFacts f = e.Facts;
                    // Spalten vor "Datei": Bezeichnung (z.B. behalten) und Grund (z.B. Folge)
                    List<string> lead = new List<string>();
                    if (labelColumn) lead.Add(e.Label ?? "");
                    if (reasonColumn) lead.Add(e.Reason ?? "");
                    lead.Add(FileLabel(e));
                    ListViewItem it = new ListViewItem(lead[0]);
                    it.UseItemStyleForSubItems = false;
                    for (int lc = 1; lc < lead.Count; lc++) it.SubItems.Add(lead[lc]);
                    it.SubItems.Add(f.Size < 0 ? "fehlt" : Fmt.Size(f.Size));
                    it.SubItems.Add(f.ResolutionText);
                    it.SubItems.Add(f.Probed && f.Duration > 0 ? FormatDuration(f.Duration) : "–");
                    it.SubItems.Add(f.CodecText);
                    it.SubItems.Add(f.Size < 0 ? "" : f.Modified.ToString("dd.MM.yyyy HH:mm"));
                    it.SubItems.Add(NoteOf(e));
                    it.ToolTipText = e.Path;
                    it.Tag = e;
                    if (group.Count > 1)
                    {
                        if (differentSize && f.Size == maxSize) Mark(it, Col(1), Bold);
                        if (differentPixels && f.Probed && f.Pixels == maxPixels && maxPixels > 0) Mark(it, Col(2), Bold);
                        if (differentDuration && f.Probed && Math.Abs(f.Duration - maxDuration) <= 2) Mark(it, Col(3), Bold);
                    }
                    bool? playable = f.Playable;
                    if (playable == false) it.SubItems[Col(6)].ForeColor = Warn;
                    bool c;
                    if (checks.TryGetValue(e, out c)) it.Checked = c;
                    else if (e.Tag is bool) it.Checked = (bool)e.Tag;
                    rows.Add(it);
                }
            }
            // Alle Zeilen auf einmal einfügen: bei tausenden Zeilen viel schneller als einzeln
            List.Items.AddRange(rows.ToArray());
            List.EndUpdate();
            if (top > 0 && top < List.Items.Count) List.TopItem = List.Items[top];
        }

        static void Mark(ListViewItem it, int col, Font bold)
        {
            it.SubItems[col].ForeColor = Better;
            it.SubItems[col].Font = bold;
        }

        static string FileLabel(CompareEntry e)
        {
            return Path.GetFileName(e.Path);
        }

        static string NoteOf(CompareEntry e)
        {
            List<string> parts = new List<string>();
            if (e.Facts.Size == 0) parts.Add("leer (0 Byte)");
            else if (e.Facts.ProbeError != null) parts.Add("defekt / nicht lesbar");
            else if (e.Facts.Playable == false) parts.Add(e.ConvertNote ? "nicht abspielbar, wird konvertiert" : "nicht abspielbar");
            if (!string.IsNullOrEmpty(e.Note)) parts.Add(e.Note);
            return string.Join(", ", parts.ToArray());
        }

        public static string FormatDuration(double seconds)
        {
            int s = (int)Math.Round(seconds);
            return s >= 3600 ? (s / 3600) + ":" + (s / 60 % 60).ToString("00") + ":" + (s % 60).ToString("00")
                             : (s / 60) + ":" + (s % 60).ToString("00");
        }

        // Angehakte Einträge
        public List<CompareEntry> CheckedEntries()
        {
            List<CompareEntry> list = new List<CompareEntry>();
            for (int i = 0; i < List.Items.Count; i++)
            {
                ListViewItem it = List.Items[i];
                if (it != null && it.Checked && it.Tag is CompareEntry) list.Add((CompareEntry)it.Tag);
            }
            return list;
        }
    }
}
