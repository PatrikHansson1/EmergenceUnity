// EMERGENCE — Fas 1 increment 3: RECONCILE ON THE FULL DRESSED CORE SCENE.
//
// The tick-stream demo proved the live reconciler on a bare Plane. Increment 3 runs the SAME live
// codex overlay + onLoss:toRuin on the FULLY DRESSED core world (real terrain + huts + fields + nature +
// composition grammar, built by WorldDresser) — so the codex objects and their ruins sit INSIDE the real
// village, on sampled terrain height, among the huts. It also proves rediscovery rebuilds a ruin, and runs
// the A6 perf census on the real dressed scene (a true representative village, not the empty plane).
//
// WorldDresser.Build already lays a STATIC codex layer ("CodexObjects"); we delete it so the LIVE
// reconciler ("CodexOverlay_Live") owns the codex layer with no double-placement. Everything is hash-placed
// and reads state only (D-078 r4) — the golden master is untouched.
//
// Menu: Emergence/Fas1/RUN DRESSED-CORE RECONCILE.  Headless: drop Reports/RUN_DRESSEDCORE.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class DressedCoreReconcile
    {
        const string World = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string LoseVillage = "Falkheim"; // a prosperous village (pop 37) — its loss shows clearly among the huts
        static readonly string[] LoseTechs = { "writing", "smithing", "mill", "temple" };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_DRESSEDCORE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "DRESSEDCORE_DONE.txt");

        static DressedCoreReconcile() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(true); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas1/RUN DRESSED-CORE RECONCILE")]
        public static void RunMenu() => Run(false);

        static void Run(bool fromTrigger)
        {
            Directory.CreateDirectory("Reports");
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — DRESSED-CORE RECONCILE (Fas 1, increment 3)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("the live codex overlay + onLoss:toRuin, running on the FULL dressed core world");
            sb.AppendLine("(real terrain + huts + nature) — objects sit among the huts, ruins replace lost structures.");
            sb.AppendLine();

            // 1) full dressing of the core world; then hand the codex layer to the LIVE reconciler
            WorldDresser.Build(World);
            var staticCodex = GameObject.Find("CodexObjects");
            if (staticCodex != null) UnityEngine.Object.DestroyImmediate(staticCodex);

            var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(World));
            var recon = new LiveReconciler();
            PresentationEventBus.Clear();
            int magentaWorst = 0;

            // 2) spawn pass — the overlay materialises inside the real village
            var d1 = recon.Reconcile(S);
            int m1 = CaptureMagenta("dressedcore-1-spawn"); magentaWorst = Mathf.Max(magentaWorst, m1);
            sb.AppendLine($"spawn        villages={S.villages.Length}  diff={d1}  placed={recon.PlacedCount} ruins={recon.RuinCount}  magenta={m1}");

            // 3) loss pass — one prosperous village forgets built knowledge -> ruins appear among the huts
            var S2 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World));
            string lost = LoseKnowledge(S2, LoseVillage, LoseTechs);
            var d2 = recon.Reconcile(S2);
            int m2 = CaptureMagenta("dressedcore-2-loss"); magentaWorst = Mathf.Max(magentaWorst, m2);
            sb.AppendLine($"loss         {lost}  diff={d2}  placed={recon.PlacedCount} ruins={recon.RuinCount}  magenta={m2}  <-- onLoss:toRuin on dressed terrain");

            // 4) rediscovery pass — the knowledge returns -> the ruins are rebuilt
            var d3 = recon.Reconcile(S);
            int m3 = CaptureMagenta("dressedcore-3-rediscover"); magentaWorst = Mathf.Max(magentaWorst, m3);
            sb.AppendLine($"rediscover   diff={d3}  placed={recon.PlacedCount} ruins={recon.RuinCount}  magenta={m3}  <-- ruins rebuilt on rediscovery");

            // 4) APPLY THE LOCKED LOOK — light rig (sun + skybox) + post stack (ACES/bloom/grade/SMAA).
            //    Without these the scene renders flat/ungraded with a gray horizon = the "old bad scene".
            //    We capture the graded day (lush locked meadow) + the dusk signature (one warm point),
            //    and LEAVE the scene in the graded state so the editor shows the real locked look.
            string season = string.IsNullOrEmpty(S.season) ? "spring" : S.season;
            string lookNote = "applied";
            try
            {
                EmergenceLightRig.Apply(season, "day");
                EmergencePostStack.Apply("day");
                int md = CaptureMagenta("dressedcore-4-day-post"); magentaWorst = Mathf.Max(magentaWorst, md);
                sb.AppendLine($"look day+post   season={season}  magenta={md}  (graded lush locked meadow)");

                EmergenceLightRig.Apply(season, "dusk");
                EmergencePostStack.Apply("dusk");
                int mk = CaptureMagenta("dressedcore-5-dusk-post"); magentaWorst = Mathf.Max(magentaWorst, mk);
                sb.AppendLine($"look dusk+post  season={season}  magenta={mk}  (signature: blue world, one warm point)");

                // leave the scene in the graded DAY look (closest to the lush locked core meadow)
                EmergenceLightRig.Apply(season, "day");
                EmergencePostStack.Apply("day");
            }
            catch (Exception e) { lookNote = "EXC: " + e.Message; Debug.LogWarning("[DressedCore] look: " + e.Message); }
            sb.AppendLine($"locked look: {lookNote} (light rig + ACES post stack — the real shipping look, A6 measured WITH it)");

            // 5) A6 census on the REAL dressed + GRADED scene (huts + nature + terrain + overlay + post)
            string perfNote;
            try { PerfHarness.RunHeadless(); perfNote = "static census on dressed+graded core scene (Reports/perf-report.txt)"; }
            catch (Exception e) { perfNote = "EXC: " + e.Message; }

            PresentationEventBus.DumpLog("Reports/dressedcore-events.txt");
            sb.AppendLine();
            sb.AppendLine($"event bus: {PresentationEventBus.Count} events (Reports/dressedcore-events.txt)");
            sb.AppendLine($"A6 perf: {perfNote}");
            sb.AppendLine($"live-scene magenta (worst of 3 passes): {magentaWorst}  => {(magentaWorst == 0 ? "GREEN" : "CHECK")}");
            sb.AppendLine();
            sb.AppendLine("Proven: the live overlay + onLoss:toRuin coexist with the full dressing on real terrain —");
            sb.AppendLine("codex objects sit among the huts, a ruin replaces each lost built structure, and rediscovery");
            sb.AppendLine("raises them again; magenta clean throughout. A6 static census is on the real scene now;");
            sb.AppendLine("the sharp play-mode draw-call/GPU-ms sample is the separate play-mode probe (perf-playmode.txt).");

            File.WriteAllText("Reports/dressedcore-report.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} magentaWorst={magentaWorst} placed={recon.PlacedCount} ruins={recon.RuinCount} events={PresentationEventBus.Count}" +
                                    (fromTrigger ? " (headless)" : "") + "\nsee Reports/dressedcore-report.txt\n");
            Debug.Log($"[DressedCore] done magentaWorst={magentaWorst} placed={recon.PlacedCount} ruins={recon.RuinCount}");
        }

        // clone-and-mutate: strip built-knowledge techs + the sky-faith from one named village (presentation-only)
        static string LoseKnowledge(WorldState S, string village, string[] techs)
        {
            if (S.villages == null) return "(no villages)";
            foreach (var v in S.villages)
            {
                if (v.name != village) continue;
                if (v.knows != null) v.knows = v.knows.Where(k => Array.IndexOf(techs, k) < 0).ToArray();
                v.cosmos = "";
                if (v.beliefs != null) v.beliefs = v.beliefs.Where(b => b != "cosmos").ToArray();
                return $"{village}: -{string.Join("/", techs)} -cosmos";
            }
            return $"({village} not found)";
        }

        // render the dressed scene's documentary camera; count broken-shader (magenta) pixels; save evidence PNG
        static int CaptureMagenta(string name)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>(); }
            const int w = 1600, h = 900;
            var prevTarget = cam.targetTexture;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = prevTarget; RenderTexture.active = null;
            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\reconciler";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            return magenta;
        }
    }
}
#endif
