// EMERGENCE — Fas 1: ENGINE TICK-STREAM driver.
//
// Drives the reconciler across an ENGINE-GENERATED genesis->growth sequence (deterministic snapshots
// exported from engine 2.3 at y6,y15,y30,y55,y85,y120 — same bytes Jint would produce, generated in
// Node only to dodge the minutes-long in-editor Jint step). Unlike the authored two-state demo, this
// is the world's OWN evolution over time: villages appear, techs accumulate, and where the Memory
// Engine LOSES a tech (real, ~y85) the matching codex object DE-MATERIALISES on its own. That is
// existence-condition C driven by the real simulation, not a hand-tweaked state.
//
// Menu: Emergence/Fas1/RUN ENGINE TICK-STREAM.  Headless: drop Reports/RUN_TICKSTREAM.trigger.
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
    public static class EngineTickStreamDemo
    {
        const string WorldDir = "Assets/Emergence/WorldStates/";
        static readonly string[] Seq =
        {
            "seq-8919-y006.json", "seq-8919-y015.json", "seq-8919-y030.json",
            "seq-8919-y055.json", "seq-8919-y085.json", "seq-8919-y120.json",
        };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_TICKSTREAM.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "TICKSTREAM_DONE.txt");

        static EngineTickStreamDemo() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run(true);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas1/RUN ENGINE TICK-STREAM")]
        public static void RunMenu() { Run(false); }

        static void Run(bool fromTrigger)
        {
            Directory.CreateDirectory("Reports");
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — ENGINE TICK-STREAM (Fas 1)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("the world's own genesis->growth: objects appear as techs/faith/law emerge, and de-materialise");
            sb.AppendLine("where the Memory Engine loses a tech. Deterministic (engine 2.3 snapshots, hash placement).");
            sb.AppendLine();

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = Vector3.one * 200f;
            ground.transform.position = new Vector3(400f, 0f, 400f);
            ground.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.34f, 0.46f, 0.26f) };
            try { EmergenceLightRig.Apply("spring", "day"); } catch (Exception e) { Debug.LogWarning("[TickStream] light rig: " + e.Message); }

            var recon = new LiveReconciler();
            PresentationEventBus.Clear();
            int magentaWorst = 0; string perfNote = "(not run)";

            foreach (var file in Seq)
            {
                var path = WorldDir + file;
                if (!File.Exists(path)) { sb.AppendLine($"{file}: MISSING — skipped"); continue; }
                WorldState S;
                try { S = JsonUtility.FromJson<WorldState>(File.ReadAllText(path)); }
                catch (Exception ex) { sb.AppendLine($"{file}: parse fail {ex.Message}"); continue; }

                var delta = recon.Reconcile(S);
                int vills = S.villages?.Length ?? 0;
                int pop = S.villages?.Sum(v => v.pop) ?? 0;
                int holds = S.villages?.Sum(v => v.knows?.Length ?? 0) ?? 0;
                int cosmos = S.villages?.Count(v => !string.IsNullOrEmpty(v.cosmos)) ?? 0;
                int magenta = CaptureMagenta("tickstream-y" + S.years.ToString("000"));
                magentaWorst = Mathf.Max(magentaWorst, magenta);
                string flag = delta.ruined > 0 ? "  <-- onLoss:toRuin (Memory Engine leaves a ruin)"
                            : delta.removed > 0 ? "  <-- onLoss (Memory Engine)" : "";
                sb.AppendLine($"y{S.years,-3} villages={vills} pop={pop,-4} techsHeld={holds,-4} cosmos={cosmos}  diff={delta}  placed={recon.PlacedCount} ruins={recon.RuinCount}  magenta={magenta}{flag}");

                if (file.Contains("y120"))
                {
                    try { PerfHarness.RunHeadless(); perfNote = "ran on final scene (Reports/perf-report.txt)"; }
                    catch (Exception e) { perfNote = "EXC: " + e.Message; }
                }
            }

            PresentationEventBus.DumpLog("Reports/tickstream-events.txt");
            sb.AppendLine();
            sb.AppendLine($"event bus: {PresentationEventBus.Count} events (Reports/tickstream-events.txt)");
            sb.AppendLine($"perf harness: {perfNote}");
            sb.AppendLine($"live-scene magenta (worst): {magentaWorst}  => {(magentaWorst == 0 ? "GREEN" : "CHECK")}");
            sb.AppendLine();
            sb.AppendLine("This is the real tick-stream: a world that builds itself, forgets, and leaves ruins where its");
            sb.AppendLine("knowledge fell (onLoss:toRuin, D-112) — a lost writing-post/manor becomes rubble, rediscovery rebuilds it.");
            sb.AppendLine("Ruin art = Village-pack stand-in until the owned Ancient Ruins pack is imported (bridge item).");
            sb.AppendLine("Next (increment 3): reconcile on the FULL dressed core scene + sharp A6 play-mode calibration there.");

            File.WriteAllText("Reports/tickstream-report.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} magentaWorst={magentaWorst} events={PresentationEventBus.Count}" +
                                    (fromTrigger ? " (headless)" : "") + "\nsee Reports/tickstream-report.txt\n");
            Debug.Log($"[TickStream] done magentaWorst={magentaWorst} events={PresentationEventBus.Count}");
        }

        static int CaptureMagenta(string name)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>(); }
            var overlay = GameObject.Find("CodexOverlay_Live");
            Bounds b = new Bounds(new Vector3(400, 0, 400), new Vector3(200, 20, 200)); bool has = false;
            if (overlay != null)
                foreach (var r in overlay.GetComponentsInChildren<Renderer>())
                { if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
            float ext = Mathf.Max(b.extents.magnitude, 30f);
            cam.transform.position = b.center + new Vector3(0, ext * 1.4f, -ext * 0.8f);
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
