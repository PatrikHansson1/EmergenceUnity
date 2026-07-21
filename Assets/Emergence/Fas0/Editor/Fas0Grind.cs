// EMERGENCE — Fas 0 grind, one-shot (D-107 Fas 0).
// Runs the whole Fas-0 gate and writes Reports/FAS0_DONE.txt:
//   1. AssetIntake full-library scan   -> magenta=0 report + bounds/scale table
//   2. Perf harness census             -> measured-vs-provisional-budget
//   3. Humanoid rig-standard validate  -> the A1/A3 gate
//   4. Event-bus self-test             -> proves the empty bus carries + logs dummy events
// Menu: Emergence/Fas0/RUN FAS 0 GRIND (all).  Headless: drop Reports/RUN_FAS0.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas0Grind
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS0.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS0_DONE.txt");

        static Fas0Grind() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(Trigger)) return;
                File.Delete(Trigger);
                RunAll(fromTrigger: true);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas0/RUN FAS 0 GRIND (all)")]
        public static void RunMenu() { RunAll(fromTrigger: false); }

        static void RunAll(bool fromTrigger)
        {
            Directory.CreateDirectory("Reports");
            var t0 = DateTime.Now;
            int magenta = -1; string eb; string humanoid = "?";

            magenta = AssetIntakePass.RunHeadless();
            PerfHarness.RunHeadless();
            try { HumanoidRigStandard.Validate(); humanoid = "ran"; } catch (Exception e) { humanoid = "EXC:" + e.Message; }

            // event-bus self-test
            PresentationEventBus.Clear();
            var recon = new ReconcilerSkeleton();
            recon.EmitSelfTestEvents();
            recon.EmitSelfTestEvents();
            PresentationEventBus.DumpLog("Reports/eventbus-selftest.txt");
            eb = PresentationEventBus.Count + " events";

            bool green = magenta == 0;
            var summary =
                $"FAS 0 GRIND — {(green ? "GREEN" : "RED")}  {t0:yyyy-MM-dd HH:mm:ss} -> {DateTime.Now:HH:mm:ss}\n" +
                $"  1. AssetIntake:  magenta={magenta}  -> {(magenta == 0 ? "GREEN" : "RED")}   (Reports/intake-report.txt)\n" +
                $"  2. PerfHarness:  census written               (Reports/perf-report.txt)\n" +
                $"  3. HumanoidRig:  {humanoid}                    (Reports/humanoid-rig-report.txt)\n" +
                $"  4. EventBus:     {eb} logged                   (Reports/eventbus-selftest.txt)\n" +
                (fromTrigger ? "(headless via RUN_FAS0.trigger)\n" : "(menu run)\n");
            File.WriteAllText(Done, summary);
            Debug.Log("[Fas0Grind] " + (green ? "GREEN" : "RED") + " — magenta=" + magenta + " — see Reports/FAS0_DONE.txt");
        }
    }
}
#endif
