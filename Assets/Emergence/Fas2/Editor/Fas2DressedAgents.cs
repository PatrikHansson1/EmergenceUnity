// EMERGENCE — Fas 2 steg 3 (D-125): THE LIVING POPULATION IN THE DRESSED CORE SCENE.
//
// Step 2 proved the agent reconciler on a bare plane. Step 3 gives it the real body: the FULLY
// DRESSED core world (terrain, huts, fields, nature, codex overlay) where the STATIC "Agents"
// layer is retired and "Agents_Live" takes over — the same handover DressedCoreReconcile made for
// the codex layer. Then it runs REAL engine history among the huts: the scene starts at y85
// (57 souls, verified engine snapshot) and y120 is applied LIVE IN PLAY MODE — 75 births, 21
// deaths, 30 comings-of-age and the task changes crossfade in front of the camera.
//
// Data note: seq-8919-y120.json and world-8919-y120-newforces.json are the SAME world state
// (verified id/pos/task/hut-identical), so the y85 seq snapshot is this world's own past.
// Read-only vs the sim (D-078 r4); golden untouched. Evidence: 45-UNITY/evidence/fas2/.
// Menu: Emergence/Fas2/RUN DRESSED AGENTS.  Headless: drop Reports/RUN_FAS2DRESSED.trigger.
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
    public static class Fas2DressedAgents
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string World085 = "Assets/Emergence/WorldStates/seq-8919-y085.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2DRESSED.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2DRESSED_DONE.txt");
        const string Report   = "Reports/fas2-dressed-agents.txt";
        const string KeyPending = "emg.fas2dressed.pending", KeyStart = "emg.fas2dressed.start", KeyReport = "emg.fas2dressed.report";

        static int _frames;
        static AgentReconciler _recon;
        static AgentReconciler.Delta _playDelta;
        static int _count85;
        static string _playNote = "";

        static Fas2DressedAgents() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN DRESSED AGENTS")]
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
                        File.WriteAllText(Done, "RUNNING (edit phase: dressing) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditPhase();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 45.0;

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
                        var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
                        _playDelta = _recon.Reconcile(S, false);            // REAL history, LIVE, among the huts
                        _playNote = $"y85 -> y120 IN PLAY on the dressed scene: {_playDelta}";
                    }
                    if (_frames >= 170 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 45s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 DRESSED-SCENE LIVING POPULATION (D-125)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("the static Agents layer retires; Agents_Live owns the people in the dressed core world.");
            sb.AppendLine("Scene starts at y85 (the world's own past); y120 lands LIVE in play mode among the huts.");
            sb.AppendLine();

            // 1) full dressed core world; retire BOTH static layers (codex handover proven in D-113)
            WorldDresser.Build(World120);
            var staticCodex = GameObject.Find("CodexObjects");
            if (staticCodex != null) UnityEngine.Object.DestroyImmediate(staticCodex);
            var staticAgents = GameObject.Find("Agents");
            if (staticAgents != null) UnityEngine.Object.DestroyImmediate(staticAgents);
            sb.AppendLine("dressed core built; static CodexObjects + Agents layers retired");

            var S120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            var S085 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World085));
            PresentationEventBus.Clear();

            // 2) live codex overlay at y120 (unchanged, D-113) + live agents at y85 (the past)
            var codex = new LiveReconciler();
            var dCodex = codex.Reconcile(S120);
            var agents = new AgentReconciler();
            var dAgents = agents.Reconcile(S085, true);
            sb.AppendLine($"codex overlay (y120): diff={dCodex} placed={codex.PlacedCount}");
            sb.AppendLine($"agents live   (y85):  diff={dAgents} souls={agents.Count} — among the huts, on terrain height");

            // 3) locked look + evidence
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S120.season) ? "spring" : S120.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[DressedAgents] look: " + e.Message); }
            int magenta = CaptureVillage("dressed-agents-y085");
            sb.AppendLine($"edit-phase magenta: {magenta}");
            sb.AppendLine();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0;
            File.WriteAllText(Done, "RUNNING (entering play mode on dressed y85) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
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

                int match = 0, mismatch = 0, total = 0, grounded = 0;
                var layer = GameObject.Find(AgentReconciler.LayerName);
                var terrain = Terrain.activeTerrain;
                if (layer != null)
                    foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                    {
                        total++;
                        var an = aa.GetComponentInChildren<Animator>();
                        if (an == null || an.runtimeAnimatorController == null) { mismatch++; continue; }
                        string expect = AgentTaskRead.StateFor(aa.task, aa.canWork);
                        bool ok = an.GetCurrentAnimatorStateInfo(0).IsName(expect)
                               || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(expect));
                        if (ok) match++; else mismatch++;
                        if (terrain != null)
                        {
                            float th = terrain.SampleHeight(aa.transform.position) + terrain.transform.position.y;
                            if (Mathf.Abs(aa.transform.position.y - th) < 0.5f) grounded++;
                        }
                    }
                sb.AppendLine($"task->state invariant: {match}/{total} in (or transitioning to) expected state, {mismatch} off");
                sb.AppendLine($"terrain grounding: {grounded}/{total} within 0.5u of terrain height");

                int magenta = CaptureVillage("dressed-agents-y120-live");
                sb.AppendLine($"play-phase magenta: {magenta}   evidence: 45-UNITY/evidence/fas2/dressed-agents-*.png");
                PresentationEventBus.DumpLog("Reports/fas2-dressed-agents-events.txt");
                sb.AppendLine($"event bus: {PresentationEventBus.Count} events (Reports/fas2-dressed-agents-events.txt)");

                bool ok2 = total == 111 && _playDelta.born == 75 && _playDelta.died == 21
                        && mismatch == 0 && grounded == total && magenta == 0 && !overtime;
                sb.AppendLine();
                string verdict = ok2 ? "GREEN — the population lives among the huts; real engine history lands live in the dressed body"
                                     : "CHECK — see numbers above";
                sb.AppendLine("verdict: " + verdict);
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(ok2 ? "GREEN" : "CHECK")} souls={total} " +
                                        $"delta=({_playDelta}) invariant={match}/{total} grounded={grounded} magenta={magenta}\nsee {Report}\n");
                Debug.Log($"[DressedAgents] done souls={total} delta={_playDelta} invariant={match}/{total} grounded={grounded}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        // frame the densest villager cluster at person scale (fog off for the shot only, D-008)
        static int CaptureVillage(string name)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera"); g.tag = "MainCamera"; cam = g.AddComponent<Camera>(); }
            var layer = GameObject.Find(AgentReconciler.LayerName);
            var pts = new List<Vector3>();
            if (layer != null) foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) pts.Add(aa.transform.position);
            Vector3 center = pts.Count > 0 ? pts[0] : new Vector3(400, 0, 400); int best = -1;
            foreach (var p in pts)
            {
                int n = pts.Count(q => (q - p).sqrMagnitude < 144f);
                if (n > best) { best = n; center = p; }
            }
            var cluster = pts.Where(q => (q - center).sqrMagnitude < 144f).ToList();
            if (cluster.Count > 0) { var c = Vector3.zero; foreach (var q in cluster) c += q; center = c / cluster.Count; }
            float ext = 14f;
            var prevPos = cam.transform.position; var prevRot = cam.transform.rotation;
            cam.transform.position = center + new Vector3(ext * 0.5f, ext * 0.45f, -ext * 0.8f);
            cam.transform.LookAt(center + Vector3.up * 0.8f);
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            cam.transform.position = prevPos; cam.transform.rotation = prevRot;
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
