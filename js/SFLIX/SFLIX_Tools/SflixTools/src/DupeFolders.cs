// "Duplikate finden": doppelte Ordner (Desktop 4.2.5, DEC-379, SFLIX-20261124-113).
// Aus den Gruppen werden Ordnerpaare gebildet (Ordner der Dateien einer Gruppe, je zwei verschiedene).
// Ein Paar wird nach oben zusammengefasst, solange die übergeordneten Ordner gleich heissen und
// keiner im anderen liegt (Serien\Serie\Staffel 1 und Kopie\Serie\Staffel 1 -> Serien\Serie und
// Kopie\Serie); Paare innerhalb eines anderen Paars gehören zu diesem (Unterordner). Gezeigt werden
// Paare ab MinShared gemeinsamen Gruppen. Ein Paar setzt nur die Häkchen seiner Gruppen: aussortiert
// werden nur Duplikate, nie ganze Ordner. Gruppen "gleiche Folge" und "gleicher Name" (verschiedene
// Dateien) zählen nur mit IncludeSimilar.

using System;
using System.Collections.Generic;
using System.IO;

namespace Sflix
{
    // Unterordnerpaar eines Paars (Ordner der Dateien) mit der Zahl gemeinsamer Gruppen
    public class DupeSubFolder
    {
        public string Left, Right;
        public int Shared;
    }

    public class DupeFolderPair
    {
        public string Left, Right;                                          // ganze Pfade, keiner liegt im anderen
        public int LeftFiles, RightFiles;                                   // Mediendateien darin (mit Unterordnern)
        public List<DupeGroup> Identical = new List<DupeGroup>();           // identische Dateien links und rechts
        public List<DupeGroup> Similar = new List<DupeGroup>();             // gleiche Folge, gleicher Name
        public List<DupeSubFolder> SubFolders = new List<DupeSubFolder>();
        public bool IncludeSimilar;                                         // Wahl am Paar, Standard aus

        // Gruppen, für die die Knöpfe des Paars gelten
        public List<DupeGroup> Groups()
        {
            List<DupeGroup> l = new List<DupeGroup>(Identical);
            if (IncludeSimilar) l.AddRange(Similar);
            return l;
        }

        public bool InLeft(string file) { return DupeFolders.Under(file, Left); }
        public bool InRight(string file) { return DupeFolders.Under(file, Right); }

        // Dateien einer Seite, die in einer Gruppe des Paars stehen
        public int Dupes(bool left)
        {
            HashSet<string> files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in Groups())
                foreach (string f in g.Files)
                    if (left ? InLeft(f) : InRight(f)) files.Add(f);
            return files.Count;
        }

        // Anteil doppelter Dateien in Prozent, abgerundet (100 nur, wenn alle doppelt sind)
        public int Percent(bool left)
        {
            int total = left ? LeftFiles : RightFiles;
            return total <= 0 ? 0 : (int)Math.Min(100, (long)Dupes(left) * 100 / total);
        }

        public bool Complete { get { return Percent(true) == 100 || Percent(false) == 100; } }

        // Häkchen (true = aussortieren) für "links behalten" bzw. "rechts behalten": in jeder Gruppe des
        // Paars bleiben die Dateien der behaltenen Seite, die der anderen Seite werden aussortiert.
        // Dateien ausserhalb des Paars bleiben, wie sie sind.
        public Dictionary<string, bool> Keep(bool left)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in Groups())
                foreach (string f in g.Files)
                {
                    if (left ? InLeft(f) : InRight(f)) result[f] = false;
                    else if ((left ? InRight(f) : InLeft(f)) && !result.ContainsKey(f)) result[f] = true;
                }
            return result;
        }

        // Gruppen "gleiche Folge"/"gleicher Name", in denen die aussortierte Datei die höhere Auflösung hat
        public int BetterRemoved(bool keepLeft)
        {
            if (!IncludeSimilar) return 0;
            int count = 0;
            foreach (DupeGroup g in Similar)
            {
                long kept = 0, removed = 0;
                for (int i = 0; i < g.Files.Count && i < g.Facts.Count; i++)
                {
                    bool keepSide = keepLeft ? InLeft(g.Files[i]) : InRight(g.Files[i]);
                    bool otherSide = keepLeft ? InRight(g.Files[i]) : InLeft(g.Files[i]);
                    if (keepSide) kept = Math.Max(kept, g.Facts[i].Pixels);
                    else if (otherSide) removed = Math.Max(removed, g.Facts[i].Pixels);
                }
                if (removed > kept) count++;
            }
            return count;
        }
    }

    public static class DupeFolders
    {
        public const int MinShared = 3;

        // Liegt path in folder (oder ist es folder)?
        public static bool Under(string path, string folder)
        {
            string p = path.TrimEnd('\\'), f = folder.TrimEnd('\\');
            return string.Equals(p, f, StringComparison.OrdinalIgnoreCase) ||
                   p.StartsWith(f + "\\", StringComparison.OrdinalIgnoreCase);
        }

        // Paar nach oben zusammenfassen: solange die übergeordneten Ordner gleich heissen, im
        // durchsuchten Ordner liegen und keiner im anderen liegt
        static void Lift(ref string a, ref string b, string root)
        {
            while (true)
            {
                string pa = Path.GetDirectoryName(a), pb = Path.GetDirectoryName(b);
                if (string.IsNullOrEmpty(pa) || string.IsNullOrEmpty(pb)) return;
                pa = pa.TrimEnd('\\'); pb = pb.TrimEnd('\\');
                if (!string.Equals(Path.GetFileName(pa), Path.GetFileName(pb), StringComparison.OrdinalIgnoreCase)) return;
                if (!Under(pa, root) || !Under(pb, root)) return;
                if (Under(pa, pb) || Under(pb, pa)) return;
                a = pa; b = pb;
            }
        }

        class Builder
        {
            public string Left, Right;
            public HashSet<DupeGroup> Groups = new HashSet<DupeGroup>();
            public Dictionary<string, DupeSubFolder> Subs = new Dictionary<string, DupeSubFolder>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, HashSet<DupeGroup>> SubGroups = new Dictionary<string, HashSet<DupeGroup>>(StringComparer.OrdinalIgnoreCase);

            public void AddSub(string left, string right, DupeGroup g)
            {
                string key = left + "|" + right;
                DupeSubFolder s;
                if (!Subs.TryGetValue(key, out s)) { s = new DupeSubFolder(); s.Left = left; s.Right = right; Subs[key] = s; SubGroups[key] = new HashSet<DupeGroup>(); }
                if (SubGroups[key].Add(g)) s.Shared++;
                Groups.Add(g);
            }
        }

        // files: alle gefundenen Mediendateien (für den Anteil je Ordner)
        public static List<DupeFolderPair> Find(string root, List<string> files, List<DupeGroup> groups)
        {
            root = root.TrimEnd('\\');
            Dictionary<string, Builder> byKey = new Dictionary<string, Builder>(StringComparer.OrdinalIgnoreCase);
            foreach (DupeGroup g in groups)
            {
                for (int i = 0; i < g.Files.Count; i++)
                    for (int j = i + 1; j < g.Files.Count; j++)
                    {
                        string da = (Path.GetDirectoryName(g.Files[i]) ?? "").TrimEnd('\\');
                        string db = (Path.GetDirectoryName(g.Files[j]) ?? "").TrimEnd('\\');
                        if (da.Length == 0 || db.Length == 0 || Under(da, db) || Under(db, da)) continue;
                        string ta = da, tb = db;
                        Lift(ref ta, ref tb, root);
                        // Feste Reihenfolge links/rechts (Unterordner gleich mitdrehen)
                        if (string.Compare(ta, tb, StringComparison.OrdinalIgnoreCase) > 0)
                        {
                            string t = ta; ta = tb; tb = t;
                            t = da; da = db; db = t;
                        }
                        string key = ta + "|" + tb;
                        Builder b;
                        if (!byKey.TryGetValue(key, out b)) { b = new Builder(); b.Left = ta; b.Right = tb; byKey[key] = b; }
                        b.AddSub(da, db, g);
                    }
            }

            // Paare innerhalb eines anderen Paars gehören zu diesem (äussere Paare zuerst)
            List<Builder> all = new List<Builder>(byKey.Values);
            all.Sort(delegate(Builder x, Builder y) { return (x.Left.Length + x.Right.Length).CompareTo(y.Left.Length + y.Right.Length); });
            List<Builder> outer = new List<Builder>();
            foreach (Builder p in all)
            {
                Builder into = null;
                bool swapped = false;
                foreach (Builder q in outer)
                {
                    if (Under(p.Left, q.Left) && Under(p.Right, q.Right)) { into = q; break; }
                    if (Under(p.Left, q.Right) && Under(p.Right, q.Left)) { into = q; swapped = true; break; }
                }
                if (into == null) { outer.Add(p); continue; }
                foreach (KeyValuePair<string, DupeSubFolder> kv in p.Subs)
                    foreach (DupeGroup g in p.SubGroups[kv.Key])
                        if (swapped) into.AddSub(kv.Value.Right, kv.Value.Left, g);
                        else into.AddSub(kv.Value.Left, kv.Value.Right, g);
            }

            List<DupeFolderPair> result = new List<DupeFolderPair>();
            foreach (Builder b in outer)
            {
                if (b.Groups.Count < MinShared) continue;
                DupeFolderPair pair = new DupeFolderPair();
                pair.Left = b.Left;
                pair.Right = b.Right;
                foreach (DupeGroup g in b.Groups)
                    if (g.Identical) pair.Identical.Add(g); else pair.Similar.Add(g);
                pair.SubFolders.AddRange(b.Subs.Values);
                pair.SubFolders.Sort(delegate(DupeSubFolder x, DupeSubFolder y) { return ListSort.Natural(x.Left, y.Left); });
                foreach (string f in files)
                {
                    if (Under(f, pair.Left)) pair.LeftFiles++;
                    else if (Under(f, pair.Right)) pair.RightFiles++;
                }
                result.Add(pair);
            }
            Sort(result);
            return result;
        }

        // Vollständig enthaltene zuoberst, dann nach Anteil und Zahl gemeinsamer Gruppen
        public static void Sort(List<DupeFolderPair> pairs)
        {
            pairs.Sort(delegate(DupeFolderPair x, DupeFolderPair y)
            {
                int r = y.Complete.CompareTo(x.Complete);
                if (r == 0) r = Math.Max(y.Percent(true), y.Percent(false)).CompareTo(Math.Max(x.Percent(true), x.Percent(false)));
                if (r == 0) r = (y.Identical.Count + y.Similar.Count).CompareTo(x.Identical.Count + x.Similar.Count);
                if (r == 0) r = ListSort.Natural(x.Left, y.Left);
                return r;
            });
        }

        // Anzeige eines Ordners (relativ zum Installationsordner, wenn möglich)
        public static string Display(string folder)
        {
            string rel = DataTask.ToRelative(folder);
            return string.IsNullOrEmpty(rel) ? folder : rel;
        }
    }
}
