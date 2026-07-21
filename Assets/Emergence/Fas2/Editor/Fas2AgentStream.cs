// EMERGENCE — Fas 2 steg 2 (D-124): AGENT TICK-STREAM PROOF — the population lives through time.
//
// Two phases, one trigger:
//   EDIT:  fresh mini scene; AgentReconciler walks the real engine-2.3 sequence y6->y85 (seed 8919):
//          souls are born, die, grow up, change task — the report shows the population's own history.
//   PLAY:  enter play mode on the y85 world, then reconcile y120 WHILE PLAYING — 75 births, 21 deaths,
//          30 band changes and the task changes crossfade LIVE (AgentAnimator.SetTask). Verifies the
//          global invariant "every agent's animator state matches its task read" (current or in
//          transition to it), captures evidence, exits on its own (watchdog, D-123 runInBackground).
//
// Read-only vs the sim (D-078 r4); all identity/variation is hash(agentId). Golden master untouched.
// Menu: Emergence/Fas2/RUN AGENT STREAM.  Headless: drop Reports/RUN_FAS2TICK.trigger.
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
    public static class Fas2AgentStream
    {
        const string WorldDir = "Assets/Emergence/WorldStates/";
        static readonly string[] EditSeq =
        {
            "seq-8919-y006.json", "seq-8919-y015.json", "seq-8919-y030.json",
            "seq-8919-y055.json", "seq-8919-y085.json",
        };
        const string PlaySnapshot = "seq-8919-y120.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2TICK.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2TICK_DONE.txt");
        const string Report   = "Reports/fas2-agentstream.txt";
        const string KeyPending = "emg.fas2tick.pending", KeyStart = "emg.fas2tick.start", KeyReport = "emg.fas2tick.report";

        static int _frames;
        static AgentReconciler _recon;
        static AgentReconciler.Delta _playDelta;
        static int _count85, _magentaPlay = -1;
        static string _playNote = "";

        static Fas2AgentStream() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN AGENT STREAM")]
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
            bool overtime = EditorApplication.timeSinceStartup - start > 40.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;   // D-123: unattended editor
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();

                    if (_frames == 10) { _recon = new AgentReconciler(); _recon.Rehydrate(); _count85 = _recon.Count; }
                    if (_frames == 50)
                    {
                        var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(WorldDir + PlaySnapshot));
                        _playDelta = _recon.Reconcile(S, false);            // LIVE, in play mode
                        _playNote = $"y120 IN PLAY: {_playDelta}";
                    }
                    if (_frames >= 170 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 40s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 AGENT TICK-STREAM (D-124)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("the population's own history out of engine-2.3 snapshots (seed 8919): souls are born, die,");
            sb.AppendLine("grow up and change task — reconciled INCREMENTALLY, then y120 applied LIVE in play mode.");
            sb.AppendLine();

            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = Vector3.one * 200f;
            ground.transform.position = new Vector3(400f, 0f, 400f);
            ground.GetComponent<Renderer>().sharedMaterial =
                new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.34f, 0.46f, 0.26f) };
            try { EmergenceLightRig.Apply("spring", "day"); } catch (Exception e) { Debug.LogWarning("[AgentStream] light rig: " + e.Message); }

            var recon = new AgentReconciler();
            PresentationEventBus.Clear();
            int magentaWorst = 0;

            foreach (var file in EditSeq)
            {
                var path = WorldDir + file;
                if (!File.Exists(path)) { sb.AppendLine($"{file}: MISSING — skipped"); continue; }
                var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(path));
                var delta = recon.Reconcile(S, true);
                int magenta = CaptureMagenta("agentstream-y" + S.years.ToString("000"));
                magentaWorst = Mathf.Max(magentaWorst, magenta);
                sb.AppendLine($"y{S.years,-3} souls={recon.Count,-4} diff={delta}  magenta={magenta}");
            }
            sb.AppendLine($"edit-phase magenta worst: {magentaWorst}");
            sb.AppendLine();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0;
            File.WriteAllText(Done, "RUNNING (entering play mode on y85) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine($"rehydrated after domain reload: {_count85} souls (y85 baseline)");
                sb.AppendLine(_playNote.Length > 0 ? _playNote : "(y120 reconcile did not run)");

                // global invariant: every agent's animator is in (or transitioning to) its task's state
                int match = 0, mismatch = 0, total = 0;
                var layer = GameObject.Find(AgentReconciler.LayerName);
                var mismatches = new StringBuilder();
                if (layer != null)
                    foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                    {
                        total++;
                        var an = aa.GetComponentInChildren<Animator>();
                        if (an == null || an.runtimeAnimatorController == null) { mismatch++; continue; }
                        string expect = AgentTaskRead.StateFor(aa.task, aa.canWork);
                        bool ok = an.GetCurrentAnimatorStateInfo(0).IsName(expect)
                               || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(expect));
                        if (ok) match++;
                        else { mismatch++; if (mismatch <= 5) mismatches.AppendLine($"    id={aa.agentId} task='{aa.task}' expect={expect}"); }
                    }
                sb.AppendLine($"task->state invariant: {match}/{total} agents in (or transitioning to) the expected state, {mismatch} off");
                sb.Append(mismatches);

                _magentaPlay = CaptureMagenta("agentstream-y120-live");
                sb.AppendLine($"play-phase magenta: {_magentaPlay}   evidence: 45-UNITY/evidence/fas2/agentstream-*.png");
                PresentationEventBus.DumpLog("Reports/fas2-agentstream-events.txt");
                sb.AppendLine($"event bus: {PresentationEventBus.Count} events (Reports/fas2-agentstream-events.txt)");

                bool ok2 = total > 0 && total == 111 && _playDelta.born == 75 && _playDelta.died == 21
                        && mismatch == 0 && _magentaPlay == 0 && !overtime;
                sb.AppendLine();
                string verdict = ok2 ? "GREEN — the population lives through time; births/deaths/aging/task changes land live" : "CHECK — see numbers above";
                sb.AppendLine("verdict: " + verdict);
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(ok2 ? "GREEN" : "CHECK")} souls={total} " +
                                        $"delta=({_playDelta}) invariant={match}/{total} magenta={_magentaPlay}\nsee {Report}\n");
                Debug.Log($"[AgentStream] done souls={total} delta={_playDelta} invariant={match}/{total}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static int CaptureMagenta(string name)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>(); }
            // frame the DENSEST cluster of villagers at person scale (a wide world shot shows only fog;
            // D-008: evidence must show people, not dots) — fog off for the shot only, restored after.
            var layer = GameObject.Find(AgentReconciler.LayerName);
            var pts = new List<Vector3>();
            if (layer != null) foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) pts.Add(aa.transform.position);
            Vector3 center = new Vector3(400, 0, 400); int best = -1;
            foreach (var p in pts)
            {
                int n = pts.Count(q => (q - p).sqrMagnitude < 144f);   // neighbours within 12 m
                if (n > best) { best = n; center = p; }
            }
            var cluster = pts.Where(q => (q - center).sqrMagnitude < 144f).ToList();
            if (cluster.Count > 0) { var c = Vector3.zero; foreach (var q in cluster) c += q; center = c / cluster.Count; }
            float ext = 14f;
            cam.transform.position = center + new Vector3(ext * 0.5f, ext * 0.45f, -ext * 0.8f);
            cam.transform.LookAt(center + Vector3.up * 0.8f);
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.5f, 0.7f, 0.9f);
            cam.farClipPlane = ext * 20f + 3000f;
            const int w = 1280, h = 720;
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
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            return magenta;
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
