// EMERGENCE — Fas 2 step 1 (D-123): PLAY-MODE PROOF THAT VILLAGERS LIVE.
//
// Enters play mode on the open (dressed core) scene, waits out warm-up, then verifies the claim
// "agents visibly walk/work" with numbers, not vibes:
//   - counts AgentAnimator agents and their animator states (Idle/Walk/Work),
//   - samples normalizedTime on up to 8 agents across frames → animation must ADVANCE,
//   - watches world-position drift on a walking agent → root-lock must HOLD (sim position is truth),
//   - captures a play-mode evidence PNG + magenta count.
// Modeled on PerfPlaymodeProbe (same watchdog: a headless run can never leave the editor stuck in play).
// Read-only vs the sim (D-078 r4).
//
// Menu: Emergence/Fas2/RUN FAS2 PLAY PROBE.  Headless: drop Reports/RUN_FAS2PLAY.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas2PlaymodeProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2PLAY.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2PLAY_DONE.txt");
        const string Report   = "Reports/fas2-playmode.txt";
        const string KeyPending = "emg.fas2play.pending", KeyStart = "emg.fas2play.start";

        static int _frames;
        static AgentAnimator[] _agents;
        static readonly List<Animator> _tracked = new();
        static readonly List<float> _t0 = new();
        static readonly List<Vector3> _p0 = new();
        static Transform _walker; static Vector3 _walkerP0;

        static Fas2PlaymodeProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN FAS2 PLAY PROBE")]
        public static void RunMenu() { SessionState.SetInt(KeyPending, 1); SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup); _frames = 0; EditorApplication.EnterPlaymode(); }

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
                        _frames = 0; _agents = null; _tracked.Clear(); _t0.Clear(); _walker = null;
                        _errors.Clear(); _pausedAtStart = false; _diag = ""; _forced = null; _stepUsed = false; _p0.Clear();
                        SessionState.SetInt(KeyPending, 1);
                        SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditorApplication.EnterPlaymode();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 30.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 1)
                    {
                        // D-123 debug: play mode froze at t=0 — an error+ErrorPause can pause the player
                        // loop on frame one. Record pause state, collect play-time errors, force unpause.
                        _pausedAtStart = EditorApplication.isPaused;
                        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
                        EditorApplication.isPaused = false;
                        // D-123 debug: Time.time froze at 0 with no errors — if no Game view is visible
                        // the editor may never step the player loop. Force one open + focused.
                        try
                        {
                            var gv = Type.GetType("UnityEditor.GameView,UnityEditor");
                            if (gv != null) { var w = EditorWindow.GetWindow(gv, false, null, true); w.Show(); w.Focus(); _gameViewForced = true; }
                        }
                        catch (Exception e) { _diag += "gameview EXC: " + e.Message + "\n"; }
                    }
                    // D-123 fix: with the editor unattended (focus lost / screen locked) the player loop
                    // never steps (frameCount froze at 1 while the editor kept repainting). Levers, in
                    // order: runInBackground, queued player-loop updates, and — decisive — Step(), which
                    // forces a player frame even without focus. Diag records what frameCount does.
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (_frames > 5 && Time.frameCount <= 1) _stepUsed = true;
                    if (_stepUsed) EditorApplication.Step();
                    if (_frames == 80) Arm();          // after warm-up + crossfades settled: baselines (drift needs a walker IN Walk)
                    if (_frames >= 160 || overtime) Finish(overtime);
                }
                catch (Exception e) { SafeFail("sample: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 30s");
        }

        static string _diag = "";
        static Animator _forced; static float _forcedT0;
        static bool _pausedAtStart, _gameViewForced, _stepUsed;
        static readonly List<string> _errors = new();

        static void OnLog(string msg, string stack, LogType t)
        {
            if (t != LogType.Error && t != LogType.Exception && t != LogType.Assert) return;
            if (_errors.Count < 12) _errors.Add($"[{t}] {msg}" + (t == LogType.Exception && !string.IsNullOrEmpty(stack) ? " @ " + stack.Split('\n')[0] : ""));
        }

        static void Arm()
        {
            _agents = UnityEngine.Object.FindObjectsByType<AgentAnimator>();
            foreach (var a in _agents)
            {
                var an = a.GetComponentInChildren<Animator>();
                if (an == null || an.runtimeAnimatorController == null) continue;
                if (_tracked.Count < 8) { _tracked.Add(an); _t0.Add(an.GetCurrentAnimatorStateInfo(0).normalizedTime); _p0.Add(an.transform.position); }
                if (_walker == null && an.GetCurrentAnimatorStateInfo(0).IsName("Walk")) { _walker = a.transform; _walkerP0 = a.transform.position; }
            }

            // DIAG (D-123 debug): dump the first agent's animator in depth + force-play a second one
            try
            {
                var sb = new StringBuilder();
                if (_agents.Length > 0)
                {
                    var a0 = _agents[0]; var an0 = a0.GetComponentInChildren<Animator>();
                    var st = an0 != null ? an0.GetCurrentAnimatorStateInfo(0) : default;
                    var clips = an0 != null ? an0.GetCurrentAnimatorClipInfo(0) : null;
                    sb.AppendLine($"diag agent0: id={a0.agentId} task='{a0.task}' canWork={a0.canWork} activeInHierarchy={a0.gameObject.activeInHierarchy}");
                    if (an0 != null)
                    {
                        sb.AppendLine($"  animator: enabled={an0.enabled} activeAndEnabled={an0.isActiveAndEnabled} init={an0.isInitialized} speed={an0.speed} updateMode={an0.updateMode} culling={an0.cullingMode}");
                        sb.AppendLine($"  controller='{an0.runtimeAnimatorController?.name}' layers={an0.layerCount} avatar='{(an0.avatar != null ? an0.avatar.name : "NULL")}' avatarValid={(an0.avatar != null && an0.avatar.isValid)} human={(an0.avatar != null && an0.avatar.isHuman)}");
                        sb.AppendLine($"  state: hash={st.shortNameHash} isIdle={st.IsName("Idle")} isWalk={st.IsName("Walk")} nt={st.normalizedTime:0.000} len={st.length:0.00} clipInfos={(clips != null ? clips.Length : -1)}{(clips != null && clips.Length > 0 ? " clip0='" + clips[0].clip.name + "'" : "")}");
                        sb.AppendLine($"  animatePhysics deltaTime={Time.deltaTime:0.0000} timeScale={Time.timeScale}");
                    }
                    else sb.AppendLine("  NO ANIMATOR");
                }
                if (_agents.Length > 1)
                {
                    _forced = _agents[1].GetComponentInChildren<Animator>();
                    if (_forced != null) { _forced.enabled = true; _forced.Play("Walk", 0, 0f); _forcedT0 = Time.time; sb.AppendLine($"diag forced: agent id={_agents[1].agentId} Play(\"Walk\") issued"); }
                }
                _diag = sb.ToString();
            }
            catch (Exception e) { _diag = "diag EXC: " + e.Message; }
        }

        static void Finish(bool overtime)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("EMERGENCE — FAS 2 PLAY-MODE PROBE (live villager animation, D-123)");
                sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}  frames={_frames}{(overtime ? "  [WATCHDOG cut]" : "")}");
                sb.AppendLine();

                int idle = 0, walk = 0, work = 0, noCtrl = 0, advancing = 0;
                if (_agents != null)
                    foreach (var a in _agents)
                    {
                        var an = a != null ? a.GetComponentInChildren<Animator>() : null;
                        if (an == null || an.runtimeAnimatorController == null) { noCtrl++; continue; }
                        var st = an.GetCurrentAnimatorStateInfo(0);
                        if (st.IsName("Walk")) walk++; else if (st.IsName("Work")) work++; else idle++;
                    }
                for (int i = 0; i < _tracked.Count; i++)
                    if (_tracked[i] != null && Mathf.Abs(_tracked[i].GetCurrentAnimatorStateInfo(0).normalizedTime - _t0[i]) > 0.05f) advancing++;

                // root-lock: NO tracked agent may leave its sim spot (idle or walking alike) — measure
                // max drift across all tracked, and name the state each ended in (covers walkers).
                float drift = -1f; int walkTracked = 0;
                var perAgent = new StringBuilder();
                for (int i = 0; i < _tracked.Count && i < _p0.Count; i++)
                {
                    if (_tracked[i] == null) continue;
                    float d = Vector3.Distance(_tracked[i].transform.position, _p0[i]);
                    var sti = _tracked[i].GetCurrentAnimatorStateInfo(0);
                    string stn = sti.IsName("Walk") ? "Walk" : sti.IsName("Work") ? "Work" : "Idle";
                    if (stn == "Walk") walkTracked++;
                    if (d > drift) drift = d;
                    perAgent.Append($"    [{stn}] drift={d:0.000}u\n");
                }
                if (_walker != null) drift = Mathf.Max(drift, Vector3.Distance(_walker.position, _walkerP0));
                int agents = _agents?.Length ?? 0;

                sb.AppendLine($"agents with AgentAnimator: {agents}   (no controller: {noCtrl})");
                sb.AppendLine($"states now:  Idle={idle}  Walk={walk}  Work={work}");
                sb.AppendLine($"animation advancing: {advancing}/{_tracked.Count} tracked animators moved >5% of a cycle");
                sb.AppendLine($"root drift (max over {_tracked.Count} tracked, {walkTracked} ended in Walk): {(drift < 0 ? "n/a" : drift.ToString("0.000") + " u")}  (must stay ~0 — sim position is truth)");
                sb.Append(perAgent);

                if (_diag.Length > 0) { sb.AppendLine(); sb.AppendLine("## DIAG"); sb.Append(_diag); }
                sb.AppendLine($"diag pausedAtStart={_pausedAtStart}  playErrors={_errors.Count}  Time.time={Time.time:0.00}  frameCount={Time.frameCount}  gameViewForced={_gameViewForced}  stepUsed={_stepUsed}  runInBg={Application.runInBackground}  drawCalls={UnityStats.drawCalls}");
                foreach (var e in _errors) sb.AppendLine("  " + e);
                if (_forced != null)
                {
                    var fst = _forced.GetCurrentAnimatorStateInfo(0);
                    sb.AppendLine($"diag forced now: isWalk={fst.IsName("Walk")} nt={fst.normalizedTime:0.000} (issued at t={_forcedT0:0.0}, now t={Time.time:0.0})");
                }
                sb.AppendLine();

                int magenta = Capture("fas2-playmode-live", sb);

                bool ok = agents > 0 && noCtrl == 0 && advancing > 0 && walk + work > 0 && (drift < 0.05f) && magenta == 0;
                sb.AppendLine();
                sb.AppendLine($"verdict: {(ok ? "GREEN — villagers animate live in play mode, root-locked, magenta clean" : "CHECK — see numbers above")}");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(ok ? "GREEN" : "CHECK")} agents={agents} idle={idle} walk={walk} work={work} advancing={advancing} drift={(drift < 0 ? "n/a" : drift.ToString("0.000"))} magenta={magenta}\nsee {Report}\n");
                Debug.Log($"[Fas2Play] done agents={agents} walk={walk} work={work} advancing={advancing}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                Application.logMessageReceived -= OnLog;
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static int Capture(string name, StringBuilder sb)
        {
            try
            {
                var cam = Camera.main;
                if (cam == null) { sb.AppendLine("evidence: no main camera"); return 0; }
                const int w = 1600, h = 900;
                var prev = cam.targetTexture;
                var rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                cam.targetTexture = prev; RenderTexture.active = null;
                int magenta = 0;
                foreach (var c in tex.GetPixels32()) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
                const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas2";
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
                sb.AppendLine($"evidence: 45-UNITY/evidence/fas2/{name}.png   magenta={magenta}");
                return magenta;
            }
            catch (Exception e) { sb.AppendLine("evidence: EXC " + e.Message); return 0; }
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
