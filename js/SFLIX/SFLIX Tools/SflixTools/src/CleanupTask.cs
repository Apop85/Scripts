// Aufgabe "Ablagen leeren": löscht die Ablage-Ordner der Werkzeuge (Name beginnt mit "__",
// z.B. __teile, __konvertiert, Preview\__gelöscht, Preview\__original) samt Inhalt endgültig.
// Vorher wird angezeigt, was gelöscht wird; nur Ausgewähltes wird gelöscht.

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
    }

    public static class CleanupTask
    {
        public static bool IsStorageFolder(string name)
        {
            return name.StartsWith("__", StringComparison.Ordinal);
        }

        public static void Run(TaskContext ctx, string root)
        {
            Run(ctx, new List<string> { root });
        }

        // Durchsucht alle Ordner und zeigt die Ablagen gemeinsam zur Auswahl. Gelöscht wird nur
        // innerhalb des Installationsordners, damit nicht versehentlich andere Daten verschwinden.
        public static void Run(TaskContext ctx, IList<string> roots)
        {
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
                return;
            }

            long total = 0;
            int totalFiles = 0;
            foreach (CleanupFolder f in found) { total += f.Size; totalFiles += f.Files; }
            ctx.Log(found.Count + " Ablage(n) gefunden: " + totalFiles + " Dateien, " + Fmt.Size(total));

            List<CleanupFolder> selected = ctx.ChooseCleanup != null ? ctx.ChooseCleanup(found) : found;
            if (selected == null || selected.Count == 0)
            {
                ctx.Log("Nichts gelöscht.");
                ctx.Summary = "nichts gelöscht";
                return;
            }

            int deleted = 0, failed = 0;
            long freed = 0;
            for (int i = 0; i < selected.Count; i++)
            {
                ctx.Check();
                CleanupFolder f = selected[i];
                ctx.Progress((double)i / selected.Count, "Lösche " + f.Path);
                try
                {
                    // Nochmals prüfen, unmittelbar vor dem Löschen
                    string reason;
                    if (!Folders.IsInsideRoot(f.Path, out reason)) throw new IOException("nicht gelöscht, " + reason);
                    DeleteTree(f.Path);
                    deleted++;
                    freed += f.Size;
                    ctx.Log("  GELÖSCHT  " + f.Path + "  (" + f.Files + " Dateien, " + Fmt.Size(f.Size) + ")");
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
