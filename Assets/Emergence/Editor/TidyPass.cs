// EMERGENCE — THE TIDY PASS (D-231, EP standing order 2026-08-14: "vi måste bli bättre på att
// rensa och arkivera efter varje pass").
//
// A rule that depends on someone REMEMBERING it is a rule that will be broken, so the tidy is a
// STEP IN THE GATE and not a good habit: drop Reports/RUN_TIDY.trigger and everything superseded
// moves, dated, with a README that says WHY it moved. Nothing is deleted — D-127 (the VM mount can
// rename but never unlink) is the practical reason, and "archived beats deleted" is the better one:
// a moved file can still answer a question next month.
//
// What it sweeps, and the rule behind each:
//   · triage leftovers (divergent-*.canon.txt) — they became the goldens; the copy in Reports is spent
//   · superseded markers (*_prev*, *.stale, *.bak, STALE_MARKER*) — a name that says "old" is old
//   · one-shot evidence images older than KeepDays — the ones we judged and moved past
//   · spent triggers (*.trigger.stale)
// What it NEVER touches: the current *_DONE.txt and each probe's current report, the golden report
// (it is an append-only ledger), commitmsg-current.txt, and anything modified inside KeepDays.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class TidyPass
    {
        public const int KeepDays = 3;   // anything touched this recently is still the working set

        static double _next;
        static string Root    => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string RepDir  => Path.Combine(Root, "Reports");
        static string Trigger => Path.Combine(RepDir, "RUN_TIDY.trigger");
        static string Done    => Path.Combine(RepDir, "TIDY_DONE.txt");
        static string Report  => Path.Combine(RepDir, "tidy-report.txt");

        // never move these, whatever their age
        static readonly string[] KeepExact = { "golden-report.txt", "tidy-report.txt", "TIDY_DONE.txt" };

        static TidyPass() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch { } }
        }

        [MenuItem("Emergence/Housekeeping/RUN TIDY (archive what is spent)")]
        public static void Run()
        {
            var now = DateTime.Now;
            var stamp = now.ToString("yyyy-MM-dd");
            var dest = Path.Combine(Root, "_ARCHIVE", stamp + "-tidy");
            var moved = new List<(string name, string why)>();
            var kept = 0;

            foreach (var path in Directory.GetFiles(RepDir))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".meta")) continue;
                if (KeepExact.Contains(name)) { kept++; continue; }

                string why = null;
                if (name.StartsWith("divergent-") && name.EndsWith(".canon.txt")) why = "golden triage leftover — this content became the goldens";
                else if (name.EndsWith(".trigger.stale")) why = "spent trigger";
                else if (name.EndsWith(".bak") || name.Contains("STALE_MARKER")) why = "explicitly marked stale";
                else if (name.Contains("_prev") || name.Contains("_p0") || name.Contains("_p1")) why = "superseded revision (name says so)";
                else
                {
                    var age = (now - File.GetLastWriteTime(path)).TotalDays;
                    bool image = name.EndsWith(".png") || name.EndsWith(".jpg");
                    if (image && age > KeepDays) why = $"evidence image already judged ({age:0} days old)";
                }
                if (why == null) { kept++; continue; }

                Directory.CreateDirectory(dest);
                var target = Path.Combine(dest, name);
                if (File.Exists(target)) target = Path.Combine(dest, Path.GetFileNameWithoutExtension(name) + "-" + now.ToString("HHmmss") + Path.GetExtension(name));
                File.Move(path, target);
                var meta = path + ".meta";
                if (File.Exists(meta)) { var mt = target + ".meta"; if (!File.Exists(mt)) File.Move(meta, mt); }
                moved.Add((name, why));
            }

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — TIDY PASS (D-231)");
            sb.AppendLine($"generated {now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"kept in Reports: {kept}   archived: {moved.Count}   destination: _ARCHIVE/{stamp}-tidy/");
            sb.AppendLine();
            if (moved.Count == 0) sb.AppendLine("nothing was spent — Reports/ is already the working set only.");
            foreach (var m in moved.OrderBy(m => m.why).ThenBy(m => m.name)) sb.AppendLine($"  · {m.name}  —  {m.why}");
            File.WriteAllText(Report, sb.ToString());

            if (moved.Count > 0)
            {
                var rd = Path.Combine(dest, "README.md");
                var head = File.Exists(rd) ? File.ReadAllText(rd) : $"# Tidy pass — {stamp}\n\nArchived, never deleted: a moved file can still answer a question next month.\nEach line says WHY it left the working set.\n";
                var body = new StringBuilder(head);
                body.AppendLine();
                body.AppendLine($"## Sweep {now:HH:mm:ss}");
                foreach (var m in moved.OrderBy(m => m.name)) body.AppendLine($"- `{m.name}` — {m.why}");
                File.WriteAllText(rd, body.ToString());
            }

            File.WriteAllText(Done, $"DONE {now:HH:mm:ss} archived={moved.Count} kept={kept}\nsee Reports/tidy-report.txt\n");
            AssetDatabase.Refresh();
            Debug.Log($"[Tidy] archived {moved.Count}, kept {kept}");
        }
    }
}
#endif
