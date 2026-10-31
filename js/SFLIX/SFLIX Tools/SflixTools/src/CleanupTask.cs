// Aufgabe "Ablagen leeren": löscht die Ablage-Ordner der Werkzeuge (Name beginnt mit "__",
// z.B. __teile, __konvertiert, Preview\__gelöscht, Preview\__original) samt Inhalt endgültig.
// Vorher wird angezeigt, was gelöscht wird; nur Ausgewähltes wird gelöscht.
// Seit 3.4.3: Ablagen lassen sich auch wiederherstellen (ganz oder einzelne Dateien), der Inhalt
// kommt in den Ordner über der Ablage zurück; danach werden die betroffenen Ordner neu eingelesen.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Sflix
{
    public class CleanupFolder
    {
        public string Path;
        public int Files;
        public long Size;
        public string Problem;   // z.B. kein Zugriff beim Zählen
        public string Kind;      // Art der Ablage für Anzeige und Filter, z.B. "Gelöscht"
        public List<string> OnlyFiles;   // gewählte Dateien (volle Pfade); null = ganzer Ordner
    }

    // Auswahl im Fenster: löschen oder wiederherstellen
    public class CleanupChoice
    {
        public bool Restore;
        public List<CleanupFolder> Folders = new List<CleanupFolder>();
    }

    public static class CleanupTask
    {
        public static bool IsStorageFolder(string name)
        {
            return name.StartsWith("__", StringComparison.Ordinal);
        }

        // Art einer Ablage aus ihrem Namen (__gelöscht -> "Gelöscht")
        public static string KindOf(string folderName)
        {
            switch (folderName.ToLowerInvariant())
            {
                case "__gelöscht": return "Gelöscht";
                case "__konvertiert": return "Konvertiert";
                case "__original": return "Original";
                case "__duplikate": return "Duplikate";
                case "__teile": return "Teile";
                case "__ersetzt": return "Ersetzt";
            }
            string name = folderName.TrimStart('_');
            return name.Length == 0 ? folderName : char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        // Alle Dateien einer Ablage (ohne Verknüpfungen zu verfolgen)
        public static List<string> FilesIn(string dir)
        {
            List<string> result = new List<string>();
            try
            {
                result.AddRange(Directory.GetFiles(dir));
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                    result.AddRange(FilesIn(sub));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
            return result;
        }

        public static void Run(TaskContext ctx, string root)
        {
            Run(ctx, new List<string> { root });
        }

        // Durchsucht alle Ordner und zeigt die Ablagen gemeinsam zur Auswahl. Gelöscht wird nur
        // innerhalb des Installationsordners, damit nicht versehentlich andere Daten verschwinden.
        // Rückgabe: Ordner, in die etwas wiederhergestellt wurde (zum Neueinlesen)
        public static List<string> Run(TaskContext ctx, IList<string> roots)
        {
            List<string> restoredInto = new List<string>();
            ctx.Progress(-1, "Suche Ablagen ...");
            List<CleanupFolder> found = new List<CleanupFolder>();
            List<string> searched = new List<string>();
            foreach (string root in Folders.WithoutNested(roots))
            {
                string reason;
                if (!Directory.Exists(root)) { ctx.Log("Hinweis: Ordner nicht gefunden, übersprungen: " + root); continue; }
                if (!Folders.IsInsideRoot(root, out reason))
                {
                    ctx.Log("Hinweis: " + root + " " + reason + ", übersprungen. Ablagen werden nur im Installationsordner geleert.");
                    continue;
                }
                searched.Add(root);
                Find(root, found, ctx.Log, ctx.Token);
            }
            if (searched.Count == 0) throw new TaskFailedException("Kein gültiger Ordner im Installationsordner gewählt");
            if (found.Count == 0)
            {
                ctx.Log("Keine Ablagen (__teile, __konvertiert, __gelöscht, __original ...) gefunden in " + string.Join(", ", searched.ToArray()));
                ctx.Summary = "keine Ablagen gefunden";
                return restoredInto;
            }

            long total = 0;
            int totalFiles = 0;
            foreach (CleanupFolder f in found) { total += f.Size; totalFiles += f.Files; }
            ctx.Log(found.Count + " Ablage(n) gefunden: " + totalFiles + " Dateien, " + Fmt.Size(total));

            // Ohne Oberfläche: alles löschen (wie bisher)
            CleanupChoice choice;
            if (ctx.ChooseCleanup != null) choice = ctx.ChooseCleanup(found);
            else { choice = new CleanupChoice(); choice.Folders = found; }
            if (choice == null || choice.Folders.Count == 0)
            {
                ctx.Log("Nichts geändert.");
                ctx.Summary = "nichts geändert";
                return restoredInto;
            }
            if (choice.Restore) return Restore(ctx, choice.Folders);

            int deleted = 0, failed = 0;
            long freed = 0;
            for (int i = 0; i < choice.Folders.Count; i++)
            {
                ctx.Check();
                CleanupFolder f = choice.Folders[i];
                ctx.Progress((double)i / choice.Folders.Count, "Lösche " + f.Path);
                try
                {
                    // Nochmals prüfen, unmittelbar vor dem Löschen
                    string reason;
                    if (!Folders.IsInsideRoot(f.Path, out reason)) throw new IOException("nicht gelöscht, " + reason);
                    if (f.OnlyFiles != null)
                    {
                        // Nur die gewählten Dateien, danach leere Ordner der Ablage entfernen
                        long size = 0;
                        foreach (string file in f.OnlyFiles)
                        {
                            if (!IsInside(file, f.Path) || !File.Exists(file)) continue;
                            size += new FileInfo(file).Length;
                            FileOps.Delete(file);
                        }
                        RemoveEmptyDirs(f.Path);
                        freed += size;
                        ctx.Log("  GELÖSCHT  " + f.OnlyFiles.Count + " Datei(en) in " + f.Path + "  (" + Fmt.Size(size) + ")");
                    }
                    else
                    {
                        DeleteTree(f.Path);
                        freed += f.Size;
                        ctx.Log("  GELÖSCHT  " + f.Path + "  (" + f.Files + " Dateien, " + Fmt.Size(f.Size) + ")");
                    }
                    deleted++;
                }
                catch (Exception e)
                {
                    failed++;
                    ctx.Log("  FEHLER    " + f.Path + " :: " + e.Message);
                }
            }
            ctx.Log("Gelöscht: " + deleted + " Ablage(n), " + Fmt.Size(freed) + " frei   Fehler: " + failed);
            ctx.Summary = deleted + " Ablage(n) gelöscht, " + Fmt.Size(freed) + " frei, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Ablage(n) konnten nicht (vollständig) gelöscht werden");
            return restoredInto;
        }

        static bool IsInside(string file, string dir)
        {
            return Path.GetFullPath(file).StartsWith(Path.GetFullPath(dir).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase);
        }

        // Wiederherstellen: Dateien zurück in den Ordner über der Ablage, mit gleicher Unterstruktur
        // (z.B. Filme\__gelöscht\Film.mp4 -> Filme\Film.mp4, Preview\__original\X.jpg -> Preview\X.jpg).
        // Ist das Ziel schon da: Nachfrage (ersetzen, überspringen, als Kopie speichern).
        static List<string> Restore(TaskContext ctx, List<CleanupFolder> folders)
        {
            List<string> restoredInto = new List<string>();
            int restored = 0, skipped = 0, failed = 0;
            ConflictAnswer remembered = null;
            for (int i = 0; i < folders.Count; i++)
            {
                ctx.Check();
                CleanupFolder f = folders[i];
                ctx.Progress((double)i / folders.Count, "Stelle wieder her: " + f.Path);
                string parent = Path.GetDirectoryName(f.Path.TrimEnd('\\'));
                string reason;
                if (!Folders.IsInsideRoot(f.Path, out reason) || !Folders.IsInsideRoot(parent, out reason))
                {
                    failed++;
                    ctx.Log("  FEHLER    " + f.Path + " :: nicht wiederhergestellt, " + reason);
                    continue;
                }
                List<string> files = f.OnlyFiles ?? FilesIn(f.Path);
                int before = restored;
                foreach (string file in files)
                {
                    ctx.Check();
                    if (!IsInside(file, f.Path) || !File.Exists(file)) continue;
                    string rel = Path.GetFullPath(file).Substring(Path.GetFullPath(f.Path).TrimEnd('\\').Length + 1);
                    string target = Path.Combine(parent, rel);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        if (File.Exists(target))
                        {
                            string chosen = ConflictAnswer.Resolve(ctx, file, target, ref remembered);
                            if (chosen == null)
                            {
                                skipped++;
                                ctx.Log("  ÜBERSPRUNGEN  " + rel + " (am Ziel vorhanden)");
                                continue;
                            }
                            if (string.Equals(chosen, target, StringComparison.OrdinalIgnoreCase)) FileOps.Replace(file, target);
                            else FileOps.Rename(file, chosen);
                            target = chosen;
                        }
                        else
                        {
                            FileOps.Rename(file, target);
                        }
                        restored++;
                        ctx.Log("  WIEDERHERGESTELLT  " + target);
                    }
                    catch (Exception e)
                    {
                        failed++;
                        ctx.Log("  FEHLER    " + file + " :: " + e.Message);
                    }
                }
                RemoveEmptyDirs(f.Path);
                if (restored > before)
                {
                    // Zum Neueinlesen: der Medienordner (bei Preview\__… der Ordner über Preview)
                    string media = string.Equals(Path.GetFileName(parent), "Preview", StringComparison.OrdinalIgnoreCase)
                        ? Path.GetDirectoryName(parent) : parent;
                    if (!restoredInto.Exists(delegate(string r) { return string.Equals(r, media, StringComparison.OrdinalIgnoreCase); }))
                        restoredInto.Add(media);
                }
            }
            ctx.Log("Wiederhergestellt: " + restored + " Datei(en)   Übersprungen: " + skipped + "   Fehler: " + failed);
            ctx.Summary = restored + " wiederhergestellt, " + skipped + " übersprungen, " + failed + " Fehler";
            if (failed > 0) throw new TaskFailedException(failed + " Datei(en) konnten nicht wiederhergestellt werden");
            return restoredInto;
        }

        // Entfernt leere Ordner von unten nach oben (die Ablage selbst eingeschlossen)
        static void RemoveEmptyDirs(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (string sub in Directory.GetDirectories(dir))
            {
                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                RemoveEmptyDirs(sub);
            }
            if (Directory.GetFileSystemEntries(dir).Length == 0)
            {
                try { FileOps.DeleteEmptyDirectory(dir); } catch { }
            }
        }

        // Nur zählen (für die Anzeige im Reiter, läuft im Hintergrund): Ablagen in den Ordnern im Installationsordner
        public static List<CleanupFolder> Scan(IList<string> roots, CancellationToken token)
        {
            List<CleanupFolder> found = new List<CleanupFolder>();
            foreach (string root in Folders.WithoutNested(roots))
            {
                string reason;
                if (!Directory.Exists(root) || !Folders.IsInsideRoot(root, out reason)) continue;
                Find(root, found, delegate { }, token);
            }
            return found;
        }

        // Sucht Ablagen, ohne in sie hineinzugehen
        static void Find(string dir, List<CleanupFolder> result, Action<string> log, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            string[] dirs;
            try { dirs = Directory.GetDirectories(dir); }
            catch (UnauthorizedAccessException) { return; }
            catch (IOException) { return; }

            foreach (string d in dirs)
            {
                string name = System.IO.Path.GetFileName(d);
                // Verknüpfungen/Junctions nie verfolgen und nie als Ablage löschen (sie können
                // auf Ordner ausserhalb von STEFFLIX zeigen)
                bool link;
                try { link = (File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0; }
                catch { continue; }
                if (link)
                {
                    if (IsStorageFolder(name)) log("Hinweis: " + d + " ist eine Verknüpfung und wird nicht geleert.");
                    continue;
                }
                if (IsStorageFolder(name))
                {
                    CleanupFolder f = new CleanupFolder();
                    f.Path = d;
                    f.Kind = KindOf(name);
                    try { Measure(d, f); }
                    catch (Exception e) { f.Problem = e.Message; }
                    result.Add(f);
                    continue;
                }
                if (Folders.Ignore(d)) continue;   // sflix_sys, Papierkorb, System- und versteckte Ordner
                Find(d, result, log, token);
            }
        }

        static void Measure(string dir, CleanupFolder f)
        {
            foreach (string file in Directory.GetFiles(dir))
            {
                f.Files++;
                f.Size += new FileInfo(file).Length;
            }
            foreach (string sub in Directory.GetDirectories(dir))
            {
                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                Measure(sub, f);
            }
        }

        // Löscht einen Ordner samt Inhalt, auch schreibgeschützte Dateien.
        // Verknüpfungen (Junctions) werden nur entfernt, ihr Ziel bleibt unberührt.
        static void DeleteTree(string dir)
        {
            foreach (string sub in Directory.GetDirectories(dir))
            {
                if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) Directory.Delete(sub, false);
                else DeleteTree(sub);
            }
            // FileOps.Delete entfernt den Schreibschutz und wiederholt bei kurz geöffneten Dateien (NAS)
            foreach (string file in Directory.GetFiles(dir)) FileOps.Delete(file);
            FileOps.DeleteEmptyDirectory(dir);
        }
    }
}
