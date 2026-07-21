// EMERGENCE — Fas 1: LIVE RECONCILER demo/driver (headless-from-bridge).
//
// Proves the Fas 1 grind mechanism on a REAL populated state (world-codex-demo, engine 2.3 — the only
// export with per-village knows/beliefs/pop). It drives three states, built in memory from that world,
// so the DIFF is visible and deterministic:
//
//   step 1  genesis   — villages stripped bare (no tech/faith/law)     -> ~nothing qualifies
//   step 2  growth     — the full codex-demo world                     -> SPAWNS as gates hold
//   step 3  loss       — cosmos + the `harm` law removed from villages  -> the star-banner & law-stone
//                                                                          DE-MATERIALISE (onLoss), rest stays
//
// This is existence-condition C in the body: a people that gains its faith and law raises the sign and
// the law-stone; a people that loses them sees them gone. Deterministic (hash placement, never sim-RNG).
// Writes Reports/reconcile-report.txt (+ RECONCILE_DONE.txt), dumps the event bus, runs the perf harness.
// Menu: Emergence/Fas1/RUN LIVE RECONCILER DEMO.  Headless: drop Reports/RUN_RECONCILE.trigger.
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
    public static class LiveReconcilerDemo
    {
        const string WorldPath = "Assets/Emergence/WorldStates/world-codex-demo.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_RECONCILE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "RECONCILE_DONE.txt");

        static LiveReconcilerDemo() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run(fromTrigger: true);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas1/RUN LIVE RECONCILER DEMO")]
        public static void RunMenu() { Run(false); }

        static void Run(bool fromTrigger)
        {
            Directory.CreateDirectory("Reports");
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — LIVE RECONCILER demo (Fas 1)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("proves: codex objects spawn when the `when` gate holds, de-materialise on loss. Deterministic.");
            sb.AppendLine();

            if (!File.Exists(WorldPath)) { File.WriteAllText(Done, "ERROR world-codex-demo.json missing\n"); return; }
            string json = File.ReadAllText(WorldPath);

            // three states built in memory from the same real world
            var genesis = Strip(Parse(json));
            var growth  = Parse(json);
            var loss    = LoseFaithAndLaw(Parse(json));
            var steps = new (string label, WorldState S)[] { ("genesis", genesis), ("growth", growth), ("loss faith+law", loss) };

            // minimal scene: ground + light rig (the core terrain scene is NOT rebuilt — this is the overlay proof)
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = Vector3.one * 200f;
            ground.transform.position = new Vector3(400f, 0f, 400f);
            ground.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.34f, 0.46f, 0.26f) };
            try { EmergenceLightRig.Apply("spring", "day"); } catch (Exception e) { Debug.LogWarning("[Reconcile] light rig: " + e.Message); }

            var recon = new LiveReconciler();
            PresentationEventBus.Clear();

            int magentaWorst = 0; string perfNote = "(not run)";
            foreach (var (label, S) in steps)
            {
                int before = PresentationEventBus.Count;
                var delta = recon.Reconcile(S);
                int qualifying = CountQualifying(S);
                int magenta = CaptureMagenta($"reconcile-{label.Replace(' ', '-')}");
                magentaWorst = Mathf.Max(magentaWorst, magenta);
                sb.AppendLine($"step {label,-14} villages={S.villages?.Length ?? 0} qualifying-placements={qualifying}  diff={delta}  placedNow={recon.PlacedCount}  magenta={magenta}");

                if (label == "growth")
                {
                    try { PerfHarness.RunHeadless(); perfNote = "ran on growth scene (Reports/perf-report.txt)"; }
                    catch (Exception e) { perfNote = "EXC: " + e.Message; }
                }
            }

            PresentationEventBus.DumpLog("Reports/reconcile-events.txt");
            sb.AppendLine();
            sb.AppendLine($"event bus: {PresentationEventBus.Count} events (Reports/reconcile-events.txt)");
            sb.AppendLine($"perf harness: {perfNote}");
            sb.AppendLine($"live-scene magenta (worst step): {magentaWorst}  => {(magentaWorst == 0 ? "GREEN" : "CHECK")}");
            sb.AppendLine();
            sb.AppendLine("reading: genesis places ~nothing (no tech/faith/law); growth SPAWNS the qualifying objects;");
            sb.AppendLine("loss removes exactly the star-banner (cosmos) + law-stone (harm) — the onLoss path — the rest kept.");
            sb.AppendLine("Next increment: drive from a live engine tick-stream (genesis->eras) instead of authored snapshots,");
            sb.AppendLine("and wire onLoss:toRuin so lost knowledge leaves a ruin, not just empty ground.");

            File.WriteAllText("Reports/reconcile-report.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} magentaWorst={magentaWorst} events={PresentationEventBus.Count}" +
                                    (fromTrigger ? " (headless)" : "") + "\nsee Reports/reconcile-report.txt\n");
            Debug.Log($"[Reconcile] done — magentaWorst={magentaWorst} events={PresentationEventBus.Count} — see Reports/reconcile-report.txt");
        }

        static WorldState Parse(string json) => JsonUtility.FromJson<WorldState>(json);

        static WorldState Strip(WorldState S)
        {
            if (S?.villages != null)
                foreach (var v in S.villages) { v.knows = new string[0]; v.beliefs = new string[0]; v.cosmos = ""; v.pop = 0; v.crafts = 0; v.maxGen = 0; }
            return S;
        }

        static WorldState LoseFaithAndLaw(WorldState S)
        {
            if (S?.villages != null)
                foreach (var v in S.villages)
                {
                    v.cosmos = "";                                   // the sky-faith forgotten -> star-banner onLoss
                    if (v.beliefs != null) v.beliefs = v.beliefs.Where(b => b != "harm").ToArray(); // the Peace of Kin lost -> law-stone onLoss
                }
            return S;
        }

        static int CountQualifying(WorldState S)
        {
            const string codexPath = "Assets/Emergence/Codex/object-codex.json";
            if (!File.Exists(codexPath) || S?.villages == null) return 0;
            Codex codex; try { codex = JsonUtility.FromJson<Codex>(File.ReadAllText(codexPath)); } catch { return 0; }
            if (codex?.objects == null) return 0;
            int n = 0;
            foreach (var v in S.villages)
                foreach (var e in codex.objects)
                {
                    bool tech = string.IsNullOrEmpty(e.requiresTech) || (v.knows != null && Array.IndexOf(v.knows, e.requiresTech) >= 0);
                    bool cust = string.IsNullOrEmpty(e.requiresCustom)
                                || (e.requiresCustom == "cosmos" ? !string.IsNullOrEmpty(v.cosmos)
                                    : (v.beliefs != null && Array.IndexOf(v.beliefs, e.requiresCustom) >= 0));
                    if (tech && cust && v.pop >= e.minPop && v.crafts >= e.minCrafts && v.maxGen >= e.minGen)
                        n += Mathf.Max(1, e.count);
                }
            return n;
        }

        static int CaptureMagenta(string name)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("DocCamera"); camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            var overlay = GameObject.Find("CodexOverlay_Live");
            Bounds b = new Bounds(new Vector3(400, 0, 400), new Vector3(200, 20, 200)); bool has = false;
            if (overlay != null)
                foreach (var r in overlay.GetComponentsInChildren<Renderer>())
                { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            float ext = Mathf.Max(b.extents.magnitude, 30f);
            cam.transform.position = b.center + new Vector3(0, ext * 1.6f, -ext * 0.9f);
            cam.transform.LookAt(b.center);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.7f, 0.9f);
            cam.farClipPlane = ext * 20f + 3000f;

            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
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
