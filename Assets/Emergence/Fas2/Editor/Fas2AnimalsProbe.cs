// EMERGENCE — Fas 2 step 4 (D-128): LIVING ANIMALS PROBE. Proves the fauna lives in the dressed
// core scene: builds the animal animator set, dresses y120 (PlaceAnimals now spawns the rigged
// Quaternius set, scale-matched to the retired GLB silhouettes), full live scene (codex + 111 live
// agents), then PLAY MODE: every animal must hold a valid behavioural state (Idle/Idle2/Sniff/Graze —
// deterministic hash(id,epoch) rotation, D-078 r4), locomotion never triggers at rest, magenta=0,
// RT evidence captures framed on the herd. Read-only vs the sim; golden untouched.
// Menu: Emergence/Fas2/RUN ANIMALS PROBE.  Headless: drop Reports/RUN_FAS2ANIMALS.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas2AnimalsProbe
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        static readonly string[] ValidStates = { "Idle", "Idle2", "Sniff", "Graze" };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2ANIMALS.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2ANIMALS_DONE.txt");
        const string Report   = "Reports/fas2-animals.txt";
        const string KeyPending = "emg.fas2animals.pending", KeyStart = "emg.fas2animals.start", KeyReport = "emg.fas2animals.report";

        static int _frames, _magenta = -1;
        static string _invariant = "";

        static Fas2AnimalsProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN ANIMALS PROBE")]
        public static void RunMenu() => EditPhase();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.5;
                try
                {
                    if (SessionState.GetInt(KeyPending, 0) == 0 && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Trigger))
                    {
                        File.Delete(Trigger);
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (edit phase) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditPhase();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 60.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;   // D-123: unattended editor
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();

                    if (_frames == 60) _invariant = CheckInvariant();
                    if (_frames == 90) FrameAnimals("deer");
                    if (_frames == 94) _magenta = Capture("animals-deer-live");
                    if (_frames == 104) FrameAnimals("wolf");
                    if (_frames == 108) { int m = Capture("animals-wolf-live"); if (m > _magenta) _magenta = m; }
                    if (_frames >= 130 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 60s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 LIVING ANIMALS (D-128)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            AnimalAnimBuild.Build();   // idempotent: clips + controller + wolf override

            WorldDresser.Build(World120);
            var staticCodex = GameObject.Find("CodexObjects");
            if (staticCodex != null) UnityEngine.Object.DestroyImmediate(staticCodex);
            var staticAgents = GameObject.Find("Agents");
            if (staticAgents != null) UnityEngine.Object.DestroyImmediate(staticAgents);
            var S120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            PresentationEventBus.Clear();
            var codex = new LiveReconciler(); codex.Reconcile(S120);
            var agents = new AgentReconciler(); agents.Reconcile(S120, true);
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S120.season) ? "spring" : S120.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[AnimalsProbe] look: " + e.Message); }

            var animals = CollectAnimals();
            int deer = animals.Count(a => a.type != "wolf"), wolves = animals.Count(a => a.type == "wolf");
            int noCtrl = animals.Count(a => { var an = a.GetComponentInChildren<Animator>(); return an == null || an.runtimeAnimatorController == null; });
            sb.AppendLine($"dressed y120: animals={animals.Count} (deer={deer}, wolf={wolves}), missing controller={noCtrl}");
            sb.AppendLine($"sim animals in snapshot: {(S120.animals != null ? S120.animals.Length : 0)}");
            sb.AppendLine();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _magenta = -1; _invariant = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static List<AnimalAnimator> CollectAnimals()
        {
            var l = new List<AnimalAnimator>();
            var layer = GameObject.Find("Animals");
            if (layer != null) l.AddRange(layer.GetComponentsInChildren<AnimalAnimator>());
            return l;
        }

        static string CheckInvariant()
        {
            var animals = CollectAnimals();
            int ok = 0, off = 0, noCtrl = 0;
            var stateCount = new Dictionary<string, int>();
            foreach (var aa in animals)
            {
                var an = aa.GetComponentInChildren<Animator>();
                if (an == null || an.runtimeAnimatorController == null) { noCtrl++; continue; }
                string s = aa.CurrentState;
                bool valid = !string.IsNullOrEmpty(s) && ValidStates.Contains(s)
                          && (an.GetCurrentAnimatorStateInfo(0).IsName(s)
                              || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(s)));
                if (valid) { ok++; stateCount[s] = stateCount.TryGetValue(s, out var n) ? n + 1 : 1; }
                else off++;
            }
            string dist = string.Join(", ", stateCount.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"));
            return $"behaviour invariant: {ok}/{animals.Count} in valid hash-state ({dist}); off={off}, noController={noCtrl}";
        }

        static void FrameAnimals(string type)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            var pts = CollectAnimals().Where(a => (type == "wolf") == (a.type == "wolf")).Select(a => a.transform.position).ToList();
            if (pts.Count == 0) pts = CollectAnimals().Select(a => a.transform.position).ToList();
            if (pts.Count == 0) return;
            Vector3 center = pts[0]; int best = -1;
            foreach (var p in pts) { int n = pts.Count(q => (q - p).sqrMagnitude < 400f); if (n > best) { best = n; center = p; } }
            var cl = pts.Where(q => (q - center).sqrMagnitude < 400f).ToList();
            var c2 = Vector3.zero; foreach (var q in cl) c2 += q; center = c2 / cl.Count;
            cam.transform.position = center + new Vector3(5.5f, 3.2f, -8f);
            cam.transform.LookAt(center + Vector3.up * 0.6f);
        }

        static int Capture(string name)   // manual RT capture (D-125 pattern; ScreenCapture is white unattended)
        {
            var cam = Camera.main; if (cam == null) return -1;
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas2";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
            return magenta;
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                var animals = CollectAnimals();
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine(_invariant.Length > 0 ? _invariant : "(invariant check did not run)");
                sb.AppendLine($"magenta (deer+wolf captures): {_magenta}   evidence: 45-UNITY/evidence/fas2/animals-*.png");

                bool green = animals.Count > 0 && _magenta == 0 && !overtime
                          && _invariant.Contains($"{animals.Count}/{animals.Count} in valid hash-state");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the fauna lives (deterministic graze/idle reads, no purchases)"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} animals={animals.Count} magenta={_magenta}\nsee {Report}\n");
                Debug.Log($"[AnimalsProbe] done animals={animals.Count} magenta={_magenta}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static void SafeFail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
