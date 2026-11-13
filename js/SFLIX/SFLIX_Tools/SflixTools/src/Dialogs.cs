// Kleine Dialoge im STEFFLIX-Stil: Nachfrage bei vorhandenen Dateien, Texteingabe.

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Sflix
{
    // "Datei gibt es schon": ersetzen oder überspringen, auf Wunsch für alle weiteren merken.
    // Seit 3.3 mit Vergleich der beiden Dateien (Grösse, Auflösung, Dauer, Video/Ton, Datum).
    public class ConflictDialog : Form
    {
        readonly DarkCheckBox remember;
        bool replace, copy;

        public ConflictDialog(string target) : this(null, target, null) { }

        public ConflictDialog(string source, string target, FfmpegTools ff)
        {
            Text = "Datei schon vorhanden";
            Setup(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), Theme.S(12));

            int width = source != null ? Theme.S(1000) : Theme.S(560);
            Label title = Theme.MakeLabel("„" + Path.GetFileName(target) + "“ gibt es im Ziel schon.", false);
            title.Font = new Font("Segoe UI Semibold", 10.5f);
            title.MaximumSize = new Size(width, 0);
            Label path = Theme.MakeLabel(Path.GetDirectoryName(target), true);
            path.MaximumSize = new Size(width, 0);
            path.Margin = new Padding(0, Theme.S(4), 0, Theme.S(10));
            Label question = Theme.MakeLabel("Soll die vorhandene Datei durch die neue ersetzt werden? „Als Kopie speichern“ behält beide, " +
                                             "die neue heisst dann „" + Path.GetFileName(ConflictAnswer.CopyPath(target)) + "“.", false);
            question.MaximumSize = new Size(width, 0);

            CompareView view = null;
            if (source != null)
            {
                view = new CompareView(ff, true);
                view.Size = new Size(width, Theme.S(150));
                view.Margin = new Padding(0, 0, 0, Theme.S(10));
                CompareEntry existing = new CompareEntry();
                existing.Label = "vorhanden";
                existing.Path = target;
                CompareEntry incoming = new CompareEntry();
                incoming.Label = "neu";
                incoming.Path = source;
                // Nicht abspielbare Dateien wandelt der Importer beim Übernehmen um
                incoming.ConvertNote = MediaTypes.NeedsConversion(Path.GetExtension(source).ToLowerInvariant()) &&
                                       !string.Equals(Path.GetExtension(source), Path.GetExtension(target), StringComparison.OrdinalIgnoreCase);
                if (!string.Equals(Path.GetFileName(source), Path.GetFileName(target), StringComparison.OrdinalIgnoreCase))
                    incoming.Note = "aus " + Path.GetDirectoryName(source);
                view.SetEntries(new System.Collections.Generic.List<CompareEntry> { existing, incoming });
            }

            remember = new DarkCheckBox();
            remember.Text = "Für alle weiteren Dateien merken";
            remember.ForeColor = Theme.Text;
            remember.Margin = new Padding(0, Theme.S(10), 0, 0);
            remember.FitText();

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(14), 0, 0);
            Button skip = Theme.MakeButton("Überspringen", false);
            Button repl = Theme.MakeButton("Ersetzen", true);
            Button keep = Theme.MakeButton("Als Kopie speichern", false);
            skip.Click += delegate { replace = false; copy = false; DialogResult = DialogResult.OK; };
            repl.Click += delegate { replace = true; copy = false; DialogResult = DialogResult.OK; };
            keep.Click += delegate { replace = false; copy = true; DialogResult = DialogResult.OK; };
            buttons.Controls.Add(skip);
            buttons.Controls.Add(repl);
            buttons.Controls.Add(keep);

            root.Controls.Add(title);
            root.Controls.Add(path);
            if (view != null) root.Controls.Add(view);
            root.Controls.Add(question);
            root.Controls.Add(remember);
            root.Controls.Add(buttons);
            Controls.Add(root);
            CancelButton = skip;   // Esc = überspringen, nichts wird überschrieben
        }

        public ConflictAnswer Ask(Form owner)
        {
            Icon = owner.Icon;
            ShowIcon = true;
            ShowDialog(owner);
            ConflictAnswer a = new ConflictAnswer();
            a.Replace = replace;
            a.Copy = copy;
            a.Remember = remember.Checked;
            return a;
        }

        internal static void Setup(Form f)
        {
            f.BackColor = Theme.Back;
            f.ForeColor = Theme.Text;
            f.Font = new Font("Segoe UI", 9.5f);
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            f.ShowInTaskbar = false;
            f.StartPosition = FormStartPosition.CenterParent;
            f.AutoSize = true;
            f.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
    }

    // Hinweis beim Start: Update verfügbar, mit den Versionen von STEFFLIX, Desktop und ffmpeg
    public class UpdateDialog : Form
    {
        bool accepted;

        public UpdateDialog(UpdateInfo u, Action<Form> showNotes)
        {
            Text = "Update verfügbar";
            ConflictDialog.Setup(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(20), Theme.S(18), Theme.S(20), Theme.S(16));

            // Kopf: Symbol und Titel
            TableLayoutPanel head = new TableLayoutPanel();
            head.AutoSize = true;
            head.ColumnCount = 2;
            head.Margin = new Padding(0, 0, 0, Theme.S(12));
            GlyphBox icon = new GlyphBox();
            icon.Icon = Glyph.Up;
            icon.Size = new Size(Theme.S(40), Theme.S(40));
            icon.Margin = new Padding(0, 0, Theme.S(12), 0);
            Label title = Theme.MakeLabel("Update verfügbar", false);
            title.Font = new Font("Segoe UI Semibold", 13f);
            title.Anchor = AnchorStyles.Left;
            head.Controls.Add(icon, 0, 0);
            head.Controls.Add(title, 1, 0);

            // Versionen
            TableLayoutPanel rows = new TableLayoutPanel();
            rows.AutoSize = true;
            rows.ColumnCount = 2;
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(140)));
            rows.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Theme.S(240)));
            rows.Margin = new Padding(0);
            AddRow(rows, "STEFFLIX", u.WebChanged ? u.WebText() : "aktuell (" + (u.WebLocal ?? "?") + ")", u.WebChanged);
            AddRow(rows, "Desktop", u.SelfChanged ? App.VersionText + " → " + SelfUpdate.Short(u.SelfRemote) : "aktuell (" + App.VersionText + ")", u.SelfChanged);
            AddRow(rows, "ffmpeg", u.FfmpegLocal == null ? "wird bei Bedarf geladen"
                                   : u.FfmpegChanged ? u.FfmpegLocal + " → " + u.FfmpegRemote : "aktuell (" + u.FfmpegLocal + ")", u.FfmpegChanged);

            Label note = Theme.MakeLabel("Die Medien und Einstellungen bleiben unverändert.", true);
            note.Margin = new Padding(0, Theme.S(12), 0, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(18), 0, 0);
            Button later = Theme.MakeButton("Später", false);
            Button now = Theme.MakeButton("Jetzt aktualisieren", true);
            later.Margin = new Padding(Theme.S(8), 0, 0, 0);
            later.Click += delegate { accepted = false; DialogResult = DialogResult.Cancel; };
            now.Click += delegate { accepted = true; DialogResult = DialogResult.OK; };
            buttons.Controls.Add(later);
            buttons.Controls.Add(now);
            Button notes = Theme.MakeButton("Patchnotes", false);
            notes.Margin = new Padding(0, 0, Theme.S(8), 0);
            notes.Click += delegate { showNotes(this); };
            buttons.Controls.Add(notes);

            root.Controls.Add(head);
            root.Controls.Add(rows);
            root.Controls.Add(note);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = now;
            CancelButton = later;
        }

        static void AddRow(TableLayoutPanel rows, string name, string value, bool changed)
        {
            Label n = Theme.MakeLabel(name, false);
            n.Margin = new Padding(0, Theme.S(7), 0, Theme.S(7));
            Label v = Theme.MakeLabel(value, !changed);
            if (changed) v.Font = new Font("Segoe UI Semibold", 9.5f);
            v.Margin = new Padding(0, Theme.S(7), 0, Theme.S(7));
            v.Anchor = AnchorStyles.Right;
            int r = rows.RowCount;
            rows.RowCount = r + 2;
            rows.Controls.Add(n, 0, r);
            rows.Controls.Add(v, 1, r);
            Panel line = new Panel();
            line.BackColor = Theme.Line;
            line.Height = 1;
            line.Dock = DockStyle.Fill;
            line.Margin = new Padding(0);
            rows.Controls.Add(line, 0, r + 1);
            rows.SetColumnSpan(line, 2);
        }

        // true = jetzt aktualisieren
        public bool Ask(Form owner)
        {
            Icon = owner.Icon;
            ShowIcon = true;
            ShowDialog(owner);
            return accepted;
        }
    }

    // Patchnotes: je Version eine Überschrift, darunter die Einträge mit farbigem Typ.
    // Seit 4.0.3: Schalter "Alle Versionen" (nicht gespeichert) zeigt statt der ausgewählten
    // Versionen (neu seit der installierten) alle aus news.json, die neuesten oben.
    public class PatchNotesDialog : Form
    {
        public PatchNotesDialog(string heading, System.Collections.Generic.List<PatchVersion> versions)
            : this(heading, versions, null)
        {
        }

        public PatchNotesDialog(string heading, System.Collections.Generic.List<PatchVersion> versions,
                                System.Collections.Generic.List<PatchVersion> allVersions)
        {
            this.heading = heading;
            if (allVersions != null)
            {
                this.allVersions = new System.Collections.Generic.List<PatchVersion>(allVersions);
                this.allVersions.Sort(delegate(PatchVersion a, PatchVersion b) { return WebVersion.Compare(b.Version, a.Version); });
            }
            Text = "Patchnotes";
            BackColor = Theme.Back;
            ForeColor = Theme.Text;
            Font = new Font("Segoe UI", 9.5f);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            ClientSize = new Size(Theme.S(760), Theme.S(560));
            MinimumSize = new Size(Theme.S(420), Theme.S(300));

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Neuerungen von Desktop stehen immer oben, die von STEFFLIX und App lassen sich dazuschalten
            FlowLayoutPanel toggles = new FlowLayoutPanel();
            toggles.AutoSize = true;
            toggles.WrapContents = true;
            toggles.Dock = DockStyle.Fill;
            toggles.Margin = new Padding(0, 0, 0, Theme.S(8));
            Label also = Theme.MakeLabel("Auch zeigen:", false);
            also.ForeColor = Theme.Muted;
            also.AutoSize = true;
            also.Margin = new Padding(0, Theme.S(4), Theme.S(10), 0);
            toggles.Controls.Add(also);
            showWeb = MakeToggle("STEFFLIX");
            showApp = MakeToggle("Android-App");
            toggles.Controls.Add(showWeb);
            toggles.Controls.Add(showApp);
            if (this.allVersions != null)
            {
                showAll = MakeToggle("Alle Versionen");
                toggles.Controls.Add(showAll);
            }
            root.Padding = new Padding(Theme.S(20), Theme.S(16), Theme.S(20), Theme.S(14));

            Label title = Theme.MakeLabel(heading, false);
            titleLabel = title;
            title.Font = new Font("Segoe UI Semibold", 13f);
            title.Margin = new Padding(0, 0, 0, Theme.S(10));

            // Text in einer Fläche im Kartenton, mit Innenabstand
            Panel frame = new Panel();
            frame.Dock = DockStyle.Fill;
            frame.BackColor = Theme.Card;
            frame.Padding = new Padding(Theme.S(16), Theme.S(12), Theme.S(8), Theme.S(12));
            frame.Margin = new Padding(0);
            RichTextBox box = new RichTextBox();
            box.Dock = DockStyle.Fill;
            box.ReadOnly = true;
            box.BorderStyle = BorderStyle.None;
            box.BackColor = Theme.Card;
            box.ForeColor = Theme.Text;
            box.DetectUrls = false;
            box.Cursor = Cursors.Default;
            frame.Controls.Add(box);
            this.box = box;
            this.versions = versions;

            Button close = Theme.MakeButton("Schliessen", true);
            close.Anchor = AnchorStyles.Right;
            close.Margin = new Padding(0, Theme.S(14), 0, 0);
            close.Click += delegate { Close(); };

            root.Controls.Add(title, 0, 0);
            root.Controls.Add(toggles, 0, 1);
            root.Controls.Add(frame, 0, 2);
            root.Controls.Add(close, 0, 3);
            Controls.Add(root);
            CancelButton = close;
        }

        readonly RichTextBox box;
        readonly System.Collections.Generic.List<PatchVersion> versions;
        readonly System.Collections.Generic.List<PatchVersion> allVersions;   // null = kein Schalter "Alle Versionen"
        readonly string heading;
        readonly DarkCheckBox showWeb, showApp, showAll;
        Label titleLabel;

        // Angezeigt: alle Versionen oder nur die ausgewählten
        System.Collections.Generic.List<PatchVersion> ShownVersions
        {
            get { return showAll != null && showAll.Checked ? allVersions : versions; }
        }

        DarkCheckBox MakeToggle(string text)
        {
            DarkCheckBox c = new DarkCheckBox();
            c.Text = text;
            c.ForeColor = Theme.Text;
            c.Switch = true;
            c.Margin = new Padding(0, 0, Theme.S(16), 0);
            c.FitText();
            c.CheckedChanged += delegate { Refill(); };
            return c;
        }

        void Refill()
        {
            if (!IsHandleCreated) return;
            titleLabel.Text = showAll != null && showAll.Checked ? "Alle Neuerungen" : heading;
            box.Clear();
            Fill(box, ShownVersions, showWeb.Checked, showApp.Checked);
        }

        // Erst jetzt befüllen: beim Einsetzen ins Fenster übernimmt das Textfeld die Schrift
        // des Fensters und würde dabei alle Formatierungen verlieren
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try { SetWindowTheme(box.Handle, "DarkMode_Explorer", null); } catch { }   // dunkle Scrollleiste
            Fill(box, ShownVersions, showWeb.Checked, showApp.Checked);
        }

        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);

        static Color TypeColor(string type)
        {
            switch (type)
            {
                case "FEATURE": return Color.FromArgb(0xff, 0x4d, 0x57);
                case "BUGFIX": return Color.FromArgb(0x6c, 0xc8, 0x7a);
                case "HOTFIX": return Color.FromArgb(0xf0, 0x8c, 0x4c);
                case "INTERFACE": return Color.FromArgb(0x6c, 0xa8, 0xf0);
                case "OPTIMIERUNG": return Color.FromArgb(0xf0, 0xc4, 0x6c);
                case "BUG": return Color.FromArgb(0xd8, 0x6c, 0xf0);
            }
            return Theme.Muted;
        }

        // Überschriften einer Komponente: "Desktop 4.0.0" bzw. "Version 2.7.1" (STEFFLIX), gleiche zusammengefasst.
        // Die Komponente heisst in news.json weiterhin "updater" (ältere Leser), angezeigt wird "Desktop".
        static System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string[]>>> GroupsOf(
            System.Collections.Generic.List<PatchVersion> versions, string component)
        {
            var groups = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string[]>>>();
            foreach (PatchVersion v in versions)
                foreach (string[] item in v.Items)
                {
                    if (item[2] != component) continue;
                    string title = component == "stefflix" ? "Version " + v.Version
                        : (component == "updater" ? "Desktop " : "Android-App ") + (item[3] != "" ? item[3] : "(mit STEFFLIX " + v.Version + ")");
                    System.Collections.Generic.List<string[]> list = null;
                    foreach (var g in groups) if (g.Key == title) { list = g.Value; break; }
                    if (list == null)
                    {
                        list = new System.Collections.Generic.List<string[]>();
                        groups.Add(new System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.List<string[]>>(title, list));
                    }
                    list.Add(item);
                }
            return groups;
        }

        static void Fill(RichTextBox box, System.Collections.Generic.List<PatchVersion> versions, bool withWeb, bool withApp)
        {
            using (Font section = new Font("Segoe UI Semibold", 13f))
            using (Font head = new Font("Segoe UI Semibold", 12f))
            using (Font tag = new Font("Segoe UI Semibold", 9f))
            using (Font body = new Font("Segoe UI", 10f))
            {
                bool first = true;
                string[][] parts = { new[] { "updater", "Desktop" }, new[] { "stefflix", "STEFFLIX" }, new[] { "app", "Android-App" } };
                foreach (string[] part in parts)
                {
                    if ((part[0] == "stefflix" && !withWeb) || (part[0] == "app" && !withApp)) continue;
                    // Abschnittstitel nur, wenn mehr als Desktop gezeigt wird
                    if (withWeb || withApp)
                    {
                        Append(box, (first ? "" : "\n\n") + "Neuerungen " + part[1] + "\n", section, Branding.Red, 0, 0);
                        first = false;
                    }
                    var groups = GroupsOf(versions, part[0]);
                    if (groups.Count == 0)
                    {
                        Append(box, (first ? "" : "\n") + "Keine Neuerungen in diesen Versionen.\n", body, Theme.Muted, 0, 0);
                        first = false;
                    }
                    foreach (var g in groups)
                    {
                        Append(box, (first ? "" : "\n") + g.Key + "\n", head, Theme.Text, 0, 0);
                        first = false;
                        foreach (string[] item in g.Value)
                        {
                            // Einzug: Folgezeilen beginnen unter dem Text, nicht unter dem Typ
                            Append(box, item[0] + "\t", tag, TypeColor(item[0]), Theme.S(4), Theme.S(110));
                            Append(box, item[4] + "\n", body, Color.FromArgb(0xd8, 0xd8, 0xde), Theme.S(4), Theme.S(110));
                        }
                    }
                }
                if (versions.Count == 0) Append(box, "Keine Patchnotes vorhanden.", body, Theme.Muted, 0, 0);
            }
            box.SelectionStart = 0;
            box.ScrollToCaret();
        }

        static void Append(RichTextBox box, string text, Font font, Color color, int indent, int hanging)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;
            box.SelectionFont = font;
            box.SelectionColor = color;
            box.SelectionIndent = indent;
            box.SelectionHangingIndent = hanging;
            if (hanging > 0) box.SelectionTabs = new int[] { indent + hanging };
            box.AppendText(text);
        }
    }

    // Symbol in einem abgerundeten roten Feld
    public class GlyphBox : Control
    {
        public Glyph Icon;

        public GlyphBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Back);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Color back = Color.FromArgb(0x3e, 0x18, 0x1d);
            using (System.Drawing.Drawing2D.GraphicsPath p = Glyphs.RoundRect(new RectangleF(0, 0, Width - 1, Height - 1), Theme.S(9)))
            using (SolidBrush b = new SolidBrush(back)) g.FillPath(b, p);
            int s = Width * 11 / 20;
            Glyphs.Draw(g, Icon, new Rectangle((Width - s) / 2, (Height - s) / 2, s, s), Color.FromArgb(0xff, 0x5a, 0x45), back);
        }
    }

    // Einfache Texteingabe (z.B. Name einer neuen Kategorie)
    public class InputDialog : Form
    {
        readonly TextBox box;

        public InputDialog(string title, string prompt)
        {
            Text = title;
            ConflictDialog.Setup(this);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.AutoSize = true;
            root.ColumnCount = 1;
            root.Padding = new Padding(Theme.S(16), Theme.S(14), Theme.S(16), Theme.S(12));
            Label l = Theme.MakeLabel(prompt, false);
            l.MaximumSize = new Size(Theme.S(420), 0);
            box = Theme.MakeTextBox();
            box.Width = Theme.S(420);
            box.Margin = new Padding(0, Theme.S(8), 0, 0);

            FlowLayoutPanel buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Margin = new Padding(0, Theme.S(14), 0, 0);
            Button cancel = Theme.MakeButton("Abbrechen", false);
            Button ok = Theme.MakeButton("OK", true);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            ok.Click += delegate { DialogResult = DialogResult.OK; };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            root.Controls.Add(l);
            root.Controls.Add(box);
            root.Controls.Add(buttons);
            Controls.Add(root);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        // Eingegebener Text oder null bei Abbruch
        public string Ask(Form owner)
        {
            Icon = owner.Icon;
            return ShowDialog(owner) == DialogResult.OK ? box.Text.Trim() : null;
        }
    }
}
