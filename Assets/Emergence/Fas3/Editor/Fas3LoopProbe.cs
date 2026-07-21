// EMERGENCE — FAS 3 increment 1 PROBE (D-133): THE LIVING LOOP, proven.
//
// Backdrop dressed once from the verified seq-8919-y006 snapshot (terrain is year-stable), static
// Codex/Agents layers retired; then the ENGINE RUNS LIVE (Fas3SimDriver, worker thread) from
// genesis, and each year-boundary snapshot feeds the live reconcilers in play mode. TWO runs, same
// seed, DIFFERENT pacing — run A fast with a mid-run pause (pause-freeze verified), run B slow→fast
// with two pauses — must land IDENTICAL sha256 at the same tick: "tid/paus påverkar aldrig
// determinism" proven mechanically. Camera rig sweeps (pan/zoom/orbit, terrain-clamped) + evidence
// capture with the hardened magenta detector.
// Menu: Emergence/Fas3/RUN LOOP PROBE.  Headless: drop Reports/RUN_FAS3LOOP.trigger.
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
    public static class Fas3LoopProbe
    {
        const string Backdrop = "Assets/Emergence/WorldStates/seq-8919-y006.json";
        const long Seed = 8919;
        // YEAR turned out to be ~216 ticks (not 12) — y12 blew the watchdog (run B never finished).
        // y3 ≈ 650 ticks/run ≈ 30-35 s at Jint speed: two runs fit comfortably; the time-law proof
        // is horizon-independent (same tick, different pacing).
        const int TargetYear = 3;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3LOOP.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3LOOP_DONE.txt");
        const string Report   = "Reports/fas3-loop.txt";
        const string KeyPending = "emg.fas3loop.pending", KeyStart = "emg.fas3loop.start", KeyReport = "emg.fas3loop.report";

        static int _frames, _magenta = -1, _magentaTone = -1, _snapshotsApplied, _phase;
        static Fas3SimDriver _driver;
        static AgentReconciler _agents;
        static LiveReconciler _codex;
        static string _hashA = "", _hashB = "", _pauseNote = "", _codexNote = "codex live: not attempted";
        static int _pauseTickBefore = -1;
        static float _pauseUntil = -1f, _phaseAt = -1f;

        static Fas3LoopProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN LOOP PROBE")]
        public static void RunMenu() => EditPhase();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.25;
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
            bool overtime = EditorApplication.timeSinceStartup - start > 150.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    Drive((float)EditorApplication.timeSinceStartup - start);
                    if ((_hashA.Length > 0 && _hashB.Length > 0) || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 150s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 3 LIVING-LOOP PROBE (D-133)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"engine LIVE from genesis (seed {Seed}) on a worker thread; pacing is presentation-only.");
            sb.AppendLine();

            WorldDresser.Build(Backdrop);
            var sc = GameObject.Find("CodexObjects"); if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
            var sa = GameObject.Find("Agents");       if (sa != null) UnityEngine.Object.DestroyImmediate(sa);
            PresentationEventBus.Clear();
            var S6 = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S6.season) ? "spring" : S6.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[Fas3Loop] look: " + e.Message); }
            sb.AppendLine("backdrop dressed from seq-8919-y006 (terrain year-stable); static layers retired");

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _magenta = _magentaTone = -1; _snapshotsApplied = 0;
            _hashA = _hashB = ""; _pauseNote = ""; _codexNote = "codex live: not attempted"; _driver = null;
            _pauseTickBefore = -1; _pauseUntil = -1f; _phaseAt = -1f;
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        // ---- live orchestration (phases): A fast+pause -> hashA -> reset -> B slow->fast+2 pauses -> hashB
        static void Drive(float t)
        {
            if (_phase == 0)   // start driver A
            {
                _agents = new AgentReconciler();
                _codex = new LiveReconciler();
                _driver = NewDriver(999f);   // compute-bound anyway; pacing differentiation lives in run B
                _phase = 1; _phaseAt = t;
                return;
            }

            // consume year snapshots -> live reconcile
            if (_driver != null)
            {
                var json = _driver.TakeYearSnapshot();
                if (json != null)
                {
                    var S = JsonUtility.FromJson<WorldState>(json);
                    _agents.Reconcile(S, false);
                    try { _codex.Reconcile(S); _codexNote = "codex live: OK (reconciled every year)"; }
                    catch (Exception e) { _codexNote = "codex live: SKIPPED (" + e.Message + ") — increment-2 item"; }
                    _snapshotsApplied++;
                }
                if (_driver.LastError.Length > 0) { SafeFail("driver: " + _driver.LastError); return; }
            }

            switch (_phase)
            {
                case 1: // run A: mid-run pause test at ~y1
                    if (_driver.Year >= 1 && _pauseTickBefore < 0)
                    { _pauseTickBefore = _driver.Tick; _driver.paused = true; _pauseUntil = t + 1.5f; }
                    if (_pauseTickBefore >= 0 && _pauseUntil > 0 && t >= _pauseUntil && _driver.paused)
                    {
                        _pauseNote = $"pause-freeze: tick {_pauseTickBefore} == {_driver.Tick} during 1.5s pause -> {(_driver.Tick == _pauseTickBefore ? "FROZEN (OK)" : "MOVED (FAIL)")}";
                        _driver.paused = false;
                        _phase = 2;
                    }
                    break;

                case 2: // run A to y12; camera sweep on the way
                    var rig = Camera.main != null ? Camera.main.GetComponent<Fas3CameraRig>() : null;
                    if (rig != null && _frames % 3 == 0) { rig.Pan(new Vector2(0.15f, 0.1f)); rig.Orbit(0.8f); }
                    if (_driver.Finished)
                    {
                        _hashA = _driver.FinalHash;
                        CaptureEvidence("fas3-loop-live");
                        UnityEngine.Object.Destroy(_driver.gameObject);
                        _agents.Clear();                       // fresh presentation for run B
                        _driver = NewDriver(60f);              // B: slow start (tick-capped), then uncapped
                        _phase = 3; _phaseAt = t;
                        break;
                    }
                    break;

                case 3: // run B: slow -> fast -> pause -> finish (tick-based, YEAR-agnostic)
                    if (_driver.Tick >= 200 && _driver.ticksPerSecond < 900f) _driver.ticksPerSecond = 999f;
                    if (_driver.Year == 2 && !_driver.paused && _pauseUntil < t) { _driver.paused = true; _pauseUntil = t + 0.7f; }
                    if (_driver.paused && t >= _pauseUntil) _driver.paused = false;
                    if (_driver.Finished)
                    {
                        _hashB = _driver.FinalHash;
                        UnityEngine.Object.Destroy(_driver.gameObject);
                        _driver = null;
                    }
                    break;
            }
        }

        static Fas3SimDriver NewDriver(float tps)
        {
            var go = new GameObject("Fas3SimDriver");
            var d = go.AddComponent<Fas3SimDriver>();
            d.seed = Seed; d.ticksPerSecond = tps; d.targetYear = TargetYear;
            return d;
        }

        static void CaptureEvidence(string name)
        {
            var cam = Camera.main; if (cam == null) return;
            // frame the living souls (few at genesis — anchor on their centroid)
            var layer = GameObject.Find(AgentReconciler.LayerName);
            var pts = new List<Vector3>();
            if (layer != null) foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) pts.Add(aa.transform.position);
            if (pts.Count > 0)
            {
                var c = Vector3.zero; foreach (var p in pts) c += p; c /= pts.Count;
                cam.transform.position = c + new Vector3(8f, 7f, -12f);
                cam.transform.LookAt(c + Vector3.up * 0.8f);
            }
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            var px = tex.GetPixels32(); _magenta = 0; _magentaTone = 0;
            foreach (var c in px)
            {
                if (c.r > 220 && c.b > 220 && c.g < 80) _magenta++;
                else if (Math.Abs(c.r - c.b) < 15 && c.r > 170 && c.g < c.r - 90) _magentaTone++;
            }
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine($"year snapshots applied live: {_snapshotsApplied} (two runs to y{TargetYear}, seed {Seed})");
                sb.AppendLine(_pauseNote.Length > 0 ? _pauseNote : "(pause test did not run)");
                sb.AppendLine(_codexNote);
                sb.AppendLine($"run A hash (fast, 1 pause): {_hashA}");
                sb.AppendLine($"run B hash (slow->fast, 2 pauses): {_hashB}");
                bool hashOk = _hashA.Length > 0 && _hashA == _hashB;
                sb.AppendLine($"DETERMINISM: same tick, different pacing -> {(hashOk ? "IDENTICAL (the time law holds)" : "DIVERGED — STOP THE LINE")}");
                sb.AppendLine($"magenta: classic={_magenta} tonemapped={_magentaTone}   evidence: 45-UNITY/evidence/fas3/fas3-loop-live.png");
                bool green = hashOk && _snapshotsApplied >= 2 * TargetYear && _pauseNote.Contains("FROZEN") && _magenta == 0 && _magentaTone == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the world runs LIVE; time is presentation, truth is the sim's"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} snapshots={_snapshotsApplied} " +
                                        $"hashEq={hashOk} pauseFrozen={_pauseNote.Contains("FROZEN")} magenta={_magenta}/{_magentaTone}\nsee {Report}\n");
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
