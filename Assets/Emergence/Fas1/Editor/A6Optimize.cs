// EMERGENCE — A6 optimization pass (D-118). Pack-safe, reversible, look-preserving levers to bring the
// dressed core scene's play-mode draw calls toward the ≤2500 budget at an acceptable FPS. Operates on the
// CURRENTLY OPEN scene (run after dressing). It edits SCENE instances only — never pack assets:
//   1) small foliage: cast/receive shadows OFF (the shadow pass is the single biggest cost on ~5k clumps).
//   2) shorter QualitySettings.shadowDistance (far shadows off — invisible at our camera, huge win).
//   3) StaticBatchingUtility.Combine on the STATIC built layer (huts/props/nature/codex overlay) → collapses
//      many draws into few; wind foliage/grass/trees are EXCLUDED (their vertex-shader sway needs live pivots).
// Read-only w.r.t. sim state (D-078 r4). Golden master untouched (presentation only).
//
// Menu: Emergence/Fas1/RUN A6 OPTIMIZE.  Headless: drop Reports/RUN_A6OPT.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class A6Optimize
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_A6OPT.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "A6OPT_DONE.txt");

        // scene-object name fragments that identify WIND foliage (exclude from static batching; shadows off)
        static readonly string[] FoliageParents = { "MeadowFoliage", "MeadowTrees", "Grass", "AmbientFX" };
        // static built layers that are safe to combine (no vertex-sway; never move)
        static readonly string[] StaticParents = { "Huts", "Yards", "HutAge", "GroundFeatures", "WorkMarks",
                                                    "TechAnchors", "Fences", "CodexObjects", "CodexOverlay_Live", "Nature" };

        static A6Optimize() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas1/RUN A6 OPTIMIZE")]
        public static void Run()
        {
            int foliageShadowsOff = 0, batchedRoots = 0;

            // 1) foliage shadows off (cast + receive)
            foreach (var pn in FoliageParents)
            {
                var go = GameObject.Find(pn);
                if (go == null) continue;
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                    foliageShadowsOff++;
                }
            }

            // 2) shorter shadow distance (far shadows are invisible at the doc camera) — big fill-rate/draw win
            float prevShadow = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = Mathf.Min(prevShadow, 35f); // LOWER only — far shadows off (invisible at doc cam)
            QualitySettings.shadowCascades = Mathf.Min(QualitySettings.shadowCascades, 2);

            // 3) static-batch the built layer (collapse many static draws)
            foreach (var pn in StaticParents)
            {
                var go = GameObject.Find(pn);
                if (go == null) continue;
                try { StaticBatchingUtility.Combine(go); batchedRoots++; }
                catch (Exception e) { Debug.LogWarning($"[A6Opt] combine {pn}: {e.Message}"); }
            }

            // census after (static proxy) — the sharp number comes from the play-mode probe next
            int renderers = UnityEngine.Object.FindObjectsByType<Renderer>().Length; // (D-123: CS0618 sort-mode overload deprecated)
            var msg = $"A6 OPTIMIZE — {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                      $"foliage renderers shadows OFF: {foliageShadowsOff}\n" +
                      $"shadowDistance: {prevShadow} -> {QualitySettings.shadowDistance}, cascades -> {QualitySettings.shadowCascades}\n" +
                      $"static-batched roots: {batchedRoots} ({string.Join(",", StaticParents.Where(p => GameObject.Find(p) != null))})\n" +
                      $"active renderers now: {renderers}\n" +
                      $"NEXT: drop RUN_PERFPLAY.trigger to measure play-mode draw calls/FPS after these levers.\n";
            File.WriteAllText(Done, msg);
            Debug.Log("[A6Opt] " + msg.Replace("\n", " | "));
        }
    }
}
#endif
