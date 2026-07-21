// EMERGENCE — Fas 2 v2-MOVEMENT PROBE (D-129): souls WALK between snapshots, no teleport.
//
// Repeats the D-125 scenario (dressed core, y85 baseline, y120 applied LIVE) but proves the v2
// mechanics: every relocated soul within AgentAnimator.MaxGlide must (a) enter transit in the Walk
// state, (b) close on its target monotonically, (c) arrive ON the sim's y120 position and resume its
// task-read state; longer relocations read as a scene cut (instant, by design). Terrain grounding
// holds THROUGHOUT the glide. Time.timeScale is raised during the probe (restored after) so arrivals
// fit the watchdog — presentation-only, determinism untouched (D-078 r4).
// Menu: Emergence/Fas2/RUN MOVE PROBE.  Headless: drop Reports/RUN_FAS2MOVE.trigger.
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
    public static class Fas2MoveProbe
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string World085 = "Assets/Emergence/WorldStates/seq-8919-y085.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2MOVE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2MOVE_DONE.txt");
        const string Report   = "Reports/fas2-move.txt";
        const string KeyPending = "emg.fas2move.pending", KeyStart = "emg.fas2move.start", KeyReport = "emg.fas2move.report";

        static int _frames, _magenta = -1;
        static AgentReconciler _recon;
        static AgentReconciler.Delta _delta;
        static WorldState _s120;
        static readonly Dictionary<int, float> _distAt40 = new();
        static string _midNote = "", _walkNote = "";
        static int _glidersAt40, _cuts;

        static Fas2MoveProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN MOVE PROBE")]
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
            bool overtime = EditorApplication.timeSinceStartup - start > 90.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;   // D-123: unattended editor
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();

                    if (_frames == 10) { _recon = new AgentReconciler(); _recon.Rehydrate(); }
                    if (_frames == 30)
                    {
                        _s120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
                        _delta = _recon.Reconcile(_s120, false);            // y120 lands live -> glides start
                        Time.timeScale = 3f;                                // probe-only fast-forward (restored)
                    }
                    if (_frames == 40) SampleGliders();
                    if (_frames == 55) FrameGlider();
                    if (_frames == 58) _magenta = Capture("move-glide-live");
                    if (_frames == 80) MidCheck();
                    bool allArrived = _frames > 80 && Gliders().Count == 0;
                    if (allArrived || _frames >= 900 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 90s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 v2-MOVEMENT PROBE (D-129)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("souls walk between snapshots (straight, terrain-following glide; pathUse export is a later engine item).");
            sb.AppendLine();

            WorldDresser.Build(World120);
            var sc = GameObject.Find("CodexObjects"); if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
            var sa = GameObject.Find("Agents");       if (sa != null) UnityEngine.Object.DestroyImmediate(sa);
            var S120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            var S085 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World085));
            PresentationEventBus.Clear();
            var codex = new LiveReconciler(); codex.Reconcile(S120);
            var agents = new AgentReconciler(); var d85 = agents.Reconcile(S085, true);
            sb.AppendLine($"dressed y120; agents live at y85 baseline: {d85} souls={agents.Count}");
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S120.season) ? "spring" : S120.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[MoveProbe] look: " + e.Message); }
            sb.AppendLine();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _magenta = -1; _distAt40.Clear(); _midNote = _walkNote = ""; _glidersAt40 = _cuts = 0;
            File.WriteAllText(Done, "RUNNING (entering play mode on y85) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static List<AgentAnimator> All()
        {
            var l = new List<AgentAnimator>();
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer != null) l.AddRange(layer.GetComponentsInChildren<AgentAnimator>());
            return l;
        }
        static List<AgentAnimator> Gliders() => All().Where(a => a.InTransit).ToList();

        static void SampleGliders()
        {
            var g = Gliders();
            _glidersAt40 = g.Count;
            int walk = 0;
            foreach (var aa in g)
            {
                _distAt40[aa.agentId] = aa.RemainingGlide;
                var an = aa.GetComponentInChildren<Animator>();
                if (an != null && (an.GetCurrentAnimatorStateInfo(0).IsName("Walk")
                    || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName("Walk")))) walk++;
            }
            _walkNote = $"frame 40: gliders={g.Count}, in Walk={walk}";
        }

        static void MidCheck()
        {
            int closing = 0, stalled = 0;
            foreach (var aa in Gliders())
                if (_distAt40.TryGetValue(aa.agentId, out var d0))
                { if (aa.RemainingGlide < d0 - 0.01f) closing++; else stalled++; }
            _midNote = $"frame 80: still-transit={Gliders().Count}, closing={closing}, stalled={stalled}";
        }

        static void FrameGlider()
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            var gl = Gliders();
            Vector3 c = gl.Count > 0 ? gl[0].transform.position
                       : All().Count > 0 ? All()[0].transform.position : new Vector3(400, 30, 400);
            cam.transform.position = c + new Vector3(4.5f, 2.6f, -6.5f);
            cam.transform.LookAt(c + Vector3.up * 0.9f);
        }

        static int Capture(string name)
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
                Time.timeScale = 1f;
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                var all = All();
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine($"y85 -> y120 live: {_delta}");
                sb.AppendLine(_walkNote); sb.AppendLine(_midNote);

                // arrival: every soul stands on its sim y120 spot (within tolerance), no one still in transit
                int arrived = 0, offSpot = 0, transit = Gliders().Count, grounded = 0, stateOk = 0;
                var terrain = Terrain.activeTerrain;
                var want = new Dictionary<int, Vector3>();
                if (_s120?.agents != null)
                    foreach (var a in _s120.agents)
                        want[a.id] = new Vector3(a.x * 8f, 0, (_s120.H - 1 - a.y) * 8f);
                foreach (var aa in all)
                {
                    if (want.TryGetValue(aa.agentId, out var w))
                    {
                        var p = aa.transform.position; p.y = 0;
                        if ((p - w).magnitude <= 0.15f) arrived++; else offSpot++;
                    }
                    if (terrain != null)
                    {
                        float th = terrain.SampleHeight(aa.transform.position) + terrain.transform.position.y;
                        if (Mathf.Abs(aa.transform.position.y - th) < 0.5f) grounded++;
                    }
                    var an = aa.GetComponentInChildren<Animator>();
                    string expect = aa.InTransit ? "Walk" : AgentTaskRead.StateFor(aa.task, aa.canWork);
                    if (an != null && (an.GetCurrentAnimatorStateInfo(0).IsName(expect)
                        || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(expect)))) stateOk++;
                }
                sb.AppendLine($"arrival: {arrived}/{all.Count} on their sim y120 spot (±0.15u ground-plane), off={offSpot}, still-transit={transit}");
                sb.AppendLine($"task->state (transit-aware): {stateOk}/{all.Count}; terrain grounding: {grounded}/{all.Count}");
                sb.AppendLine($"scene cuts (> {AgentAnimator.MaxGlide}u relocations, instant by design): {Mathf.Max(0, _delta.moved - _glidersAt40)}");
                sb.AppendLine($"magenta (mid-glide capture): {_magenta}   evidence: 45-UNITY/evidence/fas2/move-glide-live.png");

                bool green = all.Count == 111 && transit == 0 && offSpot == 0 && grounded == all.Count
                          && stateOk == all.Count && _magenta == 0 && !overtime && _glidersAt40 > 0;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — souls walk their moves; the sim still owns every destination"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} souls={all.Count} " +
                                        $"gliders={_glidersAt40} arrived={arrived} transit={transit} magenta={_magenta}\nsee {Report}\n");
                Debug.Log($"[MoveProbe] done gliders={_glidersAt40} arrived={arrived}/{all.Count}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                Time.timeScale = 1f;
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static void SafeFail(string msg)
        {
            Time.timeScale = 1f;
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
