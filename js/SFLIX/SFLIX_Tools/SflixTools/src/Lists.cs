// Tabellen (ListView): dunkle Spaltenköpfe und Sortieren per Klick auf den Spaltenkopf.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Sflix
{
    static class DarkList
    {
        // Spaltenköpfe dunkel zeichnen (sonst hell im Windows-Stil). sortable: Klick auf den Kopf sortiert.
        public static void Apply(ListView list)
        {
            Apply(list, true);
        }

        public static void Apply(ListView list, bool sortable)
        {
            list.OwnerDraw = true;
            list.DrawItem += delegate(object s, DrawListViewItemEventArgs e) { e.DrawDefault = true; };
            list.DrawSubItem += delegate(object s, DrawListViewSubItemEventArgs e) { e.DrawDefault = true; };
            list.DrawColumnHeader += delegate(object s, DrawListViewColumnHeaderEventArgs e) { DrawHeader(list, e); };
            if (sortable) ListSort.Enable(list);
            GuardChecks(list);
        }

        // Listen mit Häkchen: Markieren (Klick, Shift- oder Strg-Klick) ändert nie Häkchen. Mit OwnerDraw
        // schaltet die ListView bei einer Bereichsauswahl sonst Häkchen der markierten Zeilen um. Während
        // eine Maustaste gedrückt ist, gilt eine Änderung nur, wenn der Zeiger auf einem Kästchen steht
        // (Folgeänderungen im Code, z.B. andere Zeilen derselben Folge abhaken, laufen dann mit).
        // Leertaste: schaltet alle markierten Zeilen gemeinsam um.
        static void GuardChecks(ListView list)
        {
            list.ItemCheck += delegate(object s, ItemCheckEventArgs e)
            {
                if (!list.CheckBoxes || list.VirtualMode || Control.MouseButtons == MouseButtons.None) return;
                Point p = list.PointToClient(Control.MousePosition);
                ListViewHitTestInfo hit = list.HitTest(p);
                if (hit.Location != ListViewHitTestLocations.StateImage) e.NewValue = e.CurrentValue;
            };
            list.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (!list.CheckBoxes || list.VirtualMode || e.KeyCode != Keys.Space || e.Modifiers != Keys.None) return;
                if (list.SelectedItems.Count < 2) return;
                ListViewItem lead = list.FocusedItem != null && list.FocusedItem.Selected ? list.FocusedItem : list.SelectedItems[0];
                bool target = !lead.Checked;
                List<ListViewItem> sel = new List<ListViewItem>();
                foreach (ListViewItem it in list.SelectedItems) sel.Add(it);
                list.BeginUpdate();
                try { foreach (ListViewItem it in sel) if (it.Checked != target) it.Checked = target; }
                finally { list.EndUpdate(); }
                e.Handled = true;
                e.SuppressKeyPress = true;
            };
        }

        public static void DrawHeader(ListView list, DrawListViewColumnHeaderEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Theme.Input)) e.Graphics.FillRectangle(b, e.Bounds);
            using (Pen p = new Pen(Theme.Line)) e.Graphics.DrawLine(p, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            // Sortierte Spalte: kleines Dreieck rechts (aufwärts = aufsteigend)
            int arrow = 0;
            ListSort sort = ListSort.Of(list);
            if (sort != null && sort.Column == e.ColumnIndex) arrow = Theme.S(14);
            bool right = e.Header.TextAlign == HorizontalAlignment.Right;
            Rectangle r = new Rectangle(e.Bounds.X + Theme.S(6), e.Bounds.Y, e.Bounds.Width - Theme.S(12) - arrow, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, list.Font, r, sort != null && sort.Column == e.ColumnIndex ? Theme.Text : Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | (right ? TextFormatFlags.Right : TextFormatFlags.Left));
            if (arrow > 0)
            {
                int w = Theme.S(8), h = Theme.S(4);
                int x = e.Bounds.Right - Theme.S(8) - w;
                int y = e.Bounds.Top + (e.Bounds.Height - h) / 2;
                Point[] tri = sort.Descending
                    ? new Point[] { new Point(x, y), new Point(x + w, y), new Point(x + w / 2, y + h) }
                    : new Point[] { new Point(x, y + h), new Point(x + w, y + h), new Point(x + w / 2, y) };
                System.Drawing.Drawing2D.SmoothingMode old = e.Graphics.SmoothingMode;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (SolidBrush b = new SolidBrush(Theme.Muted)) e.Graphics.FillPolygon(b, tri);
                e.Graphics.SmoothingMode = old;
            }
        }
    }

    // Sortieren einer Tabelle per Klick auf den Spaltenkopf, nochmals klicken kehrt die Reihenfolge um.
    // Sortiert wird nach dem Tag einer Zelle (z.B. Grösse in Byte, Dauer in Sekunden), sonst nach dem
    // Text mit Zahlen als Zahlen ("Folge 2" vor "Folge 10").
    public class ListSort : IComparer
    {
        static readonly Dictionary<ListView, ListSort> all = new Dictionary<ListView, ListSort>();

        readonly ListView list;
        public int Column = -1;
        public bool Descending;
        // Eigene Sortierung (z.B. Vergleichstabelle mit Gruppen): ersetzt das Sortieren der Zeilen
        public Action Custom;
        // Liste im VirtualMode (VirtualRows): sortiert wird die Datenliste statt der ListView
        public VirtualRows Virtual;
        // Vor und nach dem Sortieren (z.B. Häkchen-Meldungen unterdrücken)
        public Action Sorting, Sorted;

        ListSort(ListView list) { this.list = list; }

        public static ListSort Enable(ListView list)
        {
            ListSort existing = Of(list);
            if (existing != null) return existing;
            ListSort sort = new ListSort(list);
            all[list] = sort;
            list.HeaderStyle = ColumnHeaderStyle.Clickable;
            list.ColumnClick += delegate(object s, ColumnClickEventArgs e) { sort.SortBy(e.Column); };
            list.Disposed += delegate { all.Remove(list); };
            return sort;
        }

        public static ListSort Of(ListView list)
        {
            ListSort s;
            return list != null && all.TryGetValue(list, out s) ? s : null;
        }

        public void SortBy(int column)
        {
            if (column == Column) Descending = !Descending;
            else { Column = column; Descending = false; }
            Apply();
        }

        // Nach dem Neuaufbau einer Tabelle erneut sortieren (gleiche Spalte und Richtung)
        public void Apply()
        {
            if (Column < 0) return;
            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (Custom != null) Custom();
                else if (Virtual != null)
                {
                    if (Sorting != null) Sorting();
                    Virtual.Sort(this);
                    if (Sorted != null) Sorted();
                    list.Invalidate();
                    Virtual.EnsureSelectionVisible();
                    return;
                }
                else
                {
                    if (Sorting != null) Sorting();
                    list.BeginUpdate();
                    try
                    {
                        // Sorter nur fürs Sortieren setzen: sonst sortiert jedes Items.Add die ganze Liste neu
                        list.ListViewItemSorter = this;
                        list.Sort();
                        list.ListViewItemSorter = null;
                    }
                    finally { list.EndUpdate(); }
                    if (Sorted != null) Sorted();
                }
                // Pfeil im Spaltenkopf aktualisieren
                list.Invalidate();
                if (list.SelectedItems.Count > 0) list.SelectedItems[0].EnsureVisible();
            }
            finally { Cursor.Current = old; }
        }

        public int Compare(object x, object y)
        {
            ListViewItem a = x as ListViewItem, b = y as ListViewItem;
            if (a == null || b == null) return 0;
            int r = CompareCells(Cell(a, Column), Cell(b, Column));
            if (r == 0 && Column != 0) r = CompareCells(Cell(a, 0), Cell(b, 0));
            return Descending ? -r : r;
        }

        static ListViewItem.ListViewSubItem Cell(ListViewItem it, int col)
        {
            return col < it.SubItems.Count ? it.SubItems[col] : null;
        }

        static int CompareCells(ListViewItem.ListViewSubItem a, ListViewItem.ListViewSubItem b)
        {
            if (a == null || b == null) return a == null ? (b == null ? 0 : -1) : 1;
            return CompareKeys(a.Tag ?? a.Text, b.Tag ?? b.Text);
        }

        // Schlüssel vergleichen: gleiche Typen direkt, Text natürlich (Zahlen als Zahlen), leer zuletzt
        public static int CompareKeys(object a, object b)
        {
            if (a != null && b != null && a.GetType() == b.GetType() && !(a is string) && a is IComparable)
                return ((IComparable)a).CompareTo(b);
            string sa = a == null ? "" : a.ToString(), sb = b == null ? "" : b.ToString();
            if (sa.Length == 0 || sb.Length == 0) return sa.Length == 0 ? (sb.Length == 0 ? 0 : 1) : -1;
            return Natural(sa, sb);
        }

        public static int Natural(string a, string b)
        {
            int i = 0, j = 0;
            while (i < a.Length && j < b.Length)
            {
                if (char.IsDigit(a[i]) && char.IsDigit(b[j]))
                {
                    int si = i, sj = j;
                    while (i < a.Length && char.IsDigit(a[i])) i++;
                    while (j < b.Length && char.IsDigit(b[j])) j++;
                    string na = a.Substring(si, i - si).TrimStart('0'), nb = b.Substring(sj, j - sj).TrimStart('0');
                    if (na.Length != nb.Length) return na.Length.CompareTo(nb.Length);
                    int c = string.CompareOrdinal(na, nb);
                    if (c != 0) return c;
                }
                else
                {
                    int c = string.Compare(a, i, b, j, 1, StringComparison.CurrentCultureIgnoreCase);
                    if (c != 0) return c;
                    i++; j++;
                }
            }
            return (a.Length - i).CompareTo(b.Length - j);
        }

        // Zelle mit Sortierschlüssel anhängen (z.B. Grösse: Text "1.2 GB", Schlüssel Bytes)
        public static ListViewItem.ListViewSubItem Add(ListViewItem it, string text, object key)
        {
            ListViewItem.ListViewSubItem sub = it.SubItems.Add(text);
            sub.Tag = key;
            return sub;
        }
    }

    // Tabelle im VirtualMode (seit Desktop 4.0.8): Die Zeilen (fertige ListViewItems) liegen in Rows,
    // die ListView fragt nur die sichtbaren ab. Aufbau und Filter mit Tausenden Zeilen dauern so nicht
    // mehr Sekunden. Sortieren (ListSort) und Filtern ändern Rows, die Auswahl bleibt erhalten.
    // Nicht für Listen mit CheckBoxes (VirtualMode kennt kein ItemChecked).
    // Über list.Items nie aufzählen (geht im VirtualMode nicht): Rows bzw. Selected() verwenden.
    public class VirtualRows
    {
        readonly ListView list;
        public readonly List<ListViewItem> Rows = new List<ListViewItem>();

        VirtualRows(ListView list) { this.list = list; }

        public static VirtualRows Attach(ListView list)
        {
            VirtualRows v = new VirtualRows(list);
            list.VirtualMode = true;
            list.VirtualListSize = 0;
            list.RetrieveVirtualItem += delegate(object s, RetrieveVirtualItemEventArgs e)
            {
                e.Item = e.ItemIndex >= 0 && e.ItemIndex < v.Rows.Count ? v.Rows[e.ItemIndex] : new ListViewItem("");
            };
            ListSort sort = ListSort.Of(list);
            if (sort != null) sort.Virtual = v;
            return v;
        }

        // Markierte Zeilen (in der Reihenfolge der Liste)
        public List<ListViewItem> Selected()
        {
            List<ListViewItem> sel = new List<ListViewItem>();
            foreach (int i in list.SelectedIndices) if (i >= 0 && i < Rows.Count) sel.Add(Rows[i]);
            return sel;
        }

        // Angezeigte Zeilen ersetzen (Filter); markierte Zeilen bleiben markiert, wenn sie noch dabei sind
        public void SetRows(IEnumerable<ListViewItem> rows)
        {
            List<ListViewItem> sel = Selected();
            Rows.Clear();
            Rows.AddRange(rows);
            list.BeginUpdate();
            try
            {
                list.SelectedIndices.Clear();
                list.VirtualListSize = Rows.Count;
                Reselect(sel);
            }
            finally { list.EndUpdate(); }
            list.Invalidate();
        }

        internal void Sort(ListSort sort)
        {
            List<ListViewItem> sel = Selected();
            // Stabil sortieren (gleiche Schlüssel behalten ihre Reihenfolge)
            List<KeyValuePair<int, ListViewItem>> indexed = new List<KeyValuePair<int, ListViewItem>>();
            for (int i = 0; i < Rows.Count; i++) indexed.Add(new KeyValuePair<int, ListViewItem>(i, Rows[i]));
            indexed.Sort(delegate(KeyValuePair<int, ListViewItem> a, KeyValuePair<int, ListViewItem> b)
            {
                int r = sort.Compare(a.Value, b.Value);
                return r != 0 ? r : a.Key.CompareTo(b.Key);
            });
            Rows.Clear();
            foreach (KeyValuePair<int, ListViewItem> p in indexed) Rows.Add(p.Value);
            list.BeginUpdate();
            try
            {
                list.SelectedIndices.Clear();
                Reselect(sel);
            }
            finally { list.EndUpdate(); }
        }

        void Reselect(List<ListViewItem> sel)
        {
            if (sel.Count == 0) return;
            Dictionary<ListViewItem, int> index = new Dictionary<ListViewItem, int>();
            for (int i = 0; i < Rows.Count; i++) index[Rows[i]] = i;
            foreach (ListViewItem it in sel)
            {
                int i;
                if (index.TryGetValue(it, out i)) list.SelectedIndices.Add(i);
            }
        }

        internal void EnsureSelectionVisible()
        {
            if (list.SelectedIndices.Count > 0) list.EnsureVisible(list.SelectedIndices[0]);
        }
    }
}
