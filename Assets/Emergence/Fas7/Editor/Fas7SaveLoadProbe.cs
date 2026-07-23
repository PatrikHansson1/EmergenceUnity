// EMERGENCE — FAS 7 PROBE: SAVE/LOAD A7 SHARP (editor vehicle).
//
// One play session, two boots:
//   BOOT 1 (continuous run): the game's own opening (Fas3Onboarding, seed 8919) races to the save
//     year; the clock is set back to 1× and Fas7SaveLoad.Save writes the save file. The probe
//     independently recomputes the continuous run's export SHA from the checkpoint the producer
//     wrote — that SHA is the truth the load must reproduce.
//   TEARDOWN: the whole composition is destroyed, the worker thread is WAITED OUT (WorkerAlive —
//     a dying worker may write one more checkpoint), and the ENTIRE checkpoint grid for the seed
//     is DELETED. Load must RESIMULATE — file reuse is disproven by construction.
//   BOOT 2 (cold load): a fresh onboarding (startPaused=true) + Fas7LoadBoot restore the save:
//     producer resimulates flat-out, fresh checkpoint SHA stamped, JumpToYear re-enters the saved
//     year, mode restored. PROOF: resimulated SHA == continuous SHA == save's anchor. Then the
//     loaded world must LIVE ON (the next year applies) — a save/load is not a freeze-frame.
// Evidence through EvidenceFraming.FrameSubjects (D-163 law), blankness-guarded, SEEN by a human
// (D-008). DONE keys stamped at measurement time (R1 law, D-155). Chronicle honesty: nothing is
// witnessed during restore (startPaused) and the jump is reconstruction (ApplyingJump) — the
// loaded chronicle starts EMPTY, a declared v1 limit.
// Menu: Emergence/Fas7/RUN SAVELOAD PROBE.  Headless: drop Reports/RUN_FAS7SAVE.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas7SaveLoadProbe
    {
        const long Seed = 8919;
        const int SaveYear = 6;        // cheap early ticks (D-135); the mechanism is year-agnostic
        const double Watchdog = 420.0;
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7SAVE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS7SAVE_DONE.txt");
        const string Report   = "Reports/fas7-saveload.txt";
        const string Png      = "Reports/fas7-saveload.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string KeyPending = "emg.fas7save.pending", KeyStart = "emg.fas7save.start", KeyReport = "emg.fas7save.report";

        static int _frames, _phase, _waitFrames;
        static Fas3Onboarding _onb;
        static Fas3SimDriver _oldDriver;
        static Fas7LoadBoot _boot;
        static Fas7SaveData _saved;
        static string _shaCont = "", _shaLoad = "";
        static int _chkDeleted = -1, _loadedSouls = -1, _loadedHuts = -1, _feedNew = -1, _liveOnYear = -1;
        static float _grabAskedAt, _teardownAt;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8 = "";

        static Fas7SaveLoadProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN SAVELOAD PROBE")]
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
            bool overtime = EditorApplication.timeSinceStartup - start > Watchdog;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    Drive();
                    if (_phase == 99 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within watchdog");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 7 PROBE: save/load A7 sharp — determinism IS the save format (D-137 grid grammar)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"save = seed + witnessed year + presentation mode + state-SHA anchor; load = RESIMULATE (grid wiped) + JumpToYear + mode restore");
            sb.AppendLine();

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;

            try { if (File.Exists(Fas7SaveLoad.PathFor(Seed))) File.Delete(Fas7SaveLoad.PathFor(Seed)); } catch { }

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _waitFrames = 0; _onb = null; _oldDriver = null; _boot = null; _saved = null;
            _shaCont = _shaLoad = ""; _chkDeleted = _loadedSouls = _loadedHuts = _feedNew = _liveOnYear = -1;
            _grabAskedAt = 0f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        static void Drive()
        {
            if (_phase == 0)   // boot 1: race the opening to the save year
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
                _onb.Clock.ticksPerSecond = Fas3TimeControls.MaxTps;   // ride the producer wall to the save year
                _phase = 1;
                return;
            }

            if (_phase == 1)   // at the save year: back to 1×, save, stamp the continuous SHA
            {
                var w = _onb.World; var c = _onb.Clock; var d = _onb.Driver;
                if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }
                if (w.LastAppliedYear < SaveYear) return;
                c.ticksPerSecond = Fas3TimeControls.BaseTps;   // the mode the save must carry: 1×, running
                c.paused = false;
                _saved = Fas7SaveLoad.Save(d, c, w, out var err);
                if (_saved == null) { SafeFail("save: " + err); return; }
                try { _shaCont = EmergenceJintHost.Sha256Hex(File.ReadAllText(Fas7SaveLoad.CheckpointPath(d, _saved.year))); }
                catch (Exception e) { SafeFail("cont sha: " + e.Message); return; }
                bool file = File.Exists(Fas7SaveLoad.PathFor(Seed));
                bool grammar = _saved.version == 1 && _saved.seed == Seed && _saved.year >= SaveYear
                            && !_saved.paused && Mathf.Approximately(_saved.ticksPerSecond, Fas3TimeControls.BaseTps)
                            && _saved.stateSha == _shaCont;   // the save's anchor == independently recomputed truth
                _n1 = $"save @y{_saved.year}: file={file}, grammar(seed/year/mode/anchor)={grammar}, contSha={_shaCont.Substring(0, 12)} ({(file && grammar ? "OK" : "FAIL")})";
                _oldDriver = d;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // teardown: destroy the composition, wait out the worker, wipe the grid
            {
                if (_waitFrames == 0)
                {
                    _teardownAt = Time.realtimeSinceStartup;
                    _oldDriver.StopWorker();   // explicit stop — Destroy's OnDestroy is frame-deferred
                    _onb.World.ResetWorld();
                    foreach (var t in new[] { typeof(Fas3Onboarding), typeof(Fas3WorldRuntime), typeof(Fas3SimDriver),
                        typeof(Fas3PresentationClock), typeof(Fas3TimeControls), typeof(Fas3AudioDirector),
                        typeof(Fas6EraAmbience), typeof(Fas6StateAmbience), typeof(Fas4ChronicleFeed),
                        typeof(Fas4ChronicleView), typeof(Fas5MetricsRecorder), typeof(Fas5AlmanacView) })
                    { var o = UnityEngine.Object.FindAnyObjectByType(t) as Component; if (o != null) UnityEngine.Object.Destroy(o.gameObject); }
                }
                _waitFrames++;
                if (_oldDriver.WorkerAlive)   // a dying worker may still write one checkpoint — wait it out
                {                             // time-based: it exits at the next between-batch check (≤ ~10 s editor)
                    if (Time.realtimeSinceStartup - _teardownAt > 45f) SafeFail("old worker never stopped (45s)");
                    return;
                }
                int deleted = 0;
                try
                {
                    foreach (var f in Directory.GetFiles(_oldDriver.CheckpointDir, $"seq-{Seed}-y*.json"))
                    { File.Delete(f); deleted++; }
                }
                catch (Exception e) { SafeFail("grid wipe: " + e.Message); return; }
                _chkDeleted = deleted;   // stamped at measurement (R1)
                _n2 = $"teardown: composition destroyed, worker stopped, checkpoint grid WIPED ({deleted} files) — load must resimulate ({(deleted > 0 ? "OK" : "FAIL")})";
                PresentationEventBus.Clear();
                PresentationEventBus.ResetSubscribers();
                _waitFrames = 0;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // boot 2: cold load — fresh onboarding paused from birth + the restorer
            {
                if (++_waitFrames < 3) return;   // let Destroy() land
                var onb2 = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
                onb2.seed = Seed; onb2.targetYear = -1; onb2.startPaused = true;
                _boot = new GameObject("Fas7LoadBoot").AddComponent<Fas7LoadBoot>();
                _boot.savePath = Fas7SaveLoad.PathFor(Seed);
                _onb = onb2;
                _waitFrames = 0;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // the restorer runs; then the verdict checks
            {
                if (!_boot.Done) return;
                if (!_boot.Ok) { _n3 = $"load FAIL: {_boot.Note}"; _phase = 99; return; }
                var w = _onb.World; var c = _onb.Clock;
                _shaLoad = _boot.LoadedSha;   // stamped at measurement (R1)
                bool shaMatch = _shaLoad == _shaCont && _shaLoad == _saved.stateSha;
                _n3 = $"THE PROOF: resimulated SHA == continuous SHA == save anchor: {shaMatch} ({_shaLoad.Substring(0, 12)}) ({(shaMatch ? "OK" : "FAIL")})";

                var S = w.LastState;
                _loadedSouls = w.AgentCount; _loadedHuts = w.HutCount;
                bool world = w.LastAppliedYear == _saved.year && S != null && S.tick == _saved.year * Mathf.Max(1, _onb.Driver.YearTicks)
                          && _loadedSouls == S.agents.Length && _loadedHuts == S.huts.Length;
                _n4 = $"loaded world: y{w.LastAppliedYear}=={_saved.year}, tick={S?.tick}, souls={_loadedSouls}, huts={_loadedHuts} ({(world ? "OK" : "FAIL")})";

                bool mode = !c.paused && Mathf.Approximately(c.ticksPerSecond, _saved.ticksPerSecond);
                _n5 = $"mode restored: paused={c.paused}=={_saved.paused}, tps={c.ticksPerSecond}=={_saved.ticksPerSecond} ({(mode ? "OK" : "FAIL")})";

                var feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _feedNew = feed != null ? feed.Entries.Count : -1;
                bool clean = feed != null && _feedNew == 0;
                _n6 = $"chronicle honesty: entries after load={_feedNew} (restore witnessed NOTHING; empty-after-load = declared v1 limit) ({(clean ? "OK" : "FAIL")})";
                _waitFrames = 0; _teardownAt = Time.realtimeSinceStartup;   // reused as the live-on wait anchor
                _phase = 5;
                return;
            }

            if (_phase == 5)   // the loaded world must LIVE ON — the next year applies from the restored mode
            {
                var w = _onb.World;
                if (w.LastAppliedYear <= _saved.year) { if (Time.realtimeSinceStartup - _teardownAt > 30f) { _n7 = "live-on: next year never applied within 30s (FAIL)"; _phase = 99; } return; }
                _liveOnYear = w.LastAppliedYear;   // stamped at measurement (R1)
                _n7 = $"lives on: y{_liveOnYear} applied after restore — a load is a running world, not a freeze-frame (OK)";

                // evidence through the shared law (D-163): subjects must be a FRAMEABLE CLUSTER, not a
                // map-spanning scatter (this probe's first eye rejection): the hut + the 2 souls nearest it
                var S = w.LastState;
                var subjects = new System.Collections.Generic.List<Vector3>();
                if (S != null && S.huts.Length > 0)
                {
                    var h0 = S.huts[0];
                    subjects.Add(Mapped(S, h0.x, h0.y));
                    var byDist = new System.Collections.Generic.List<WorldAgent>(S.agents);
                    byDist.Sort((a, b) => ((a.x - h0.x) * (a.x - h0.x) + (a.y - h0.y) * (a.y - h0.y))
                                .CompareTo((b.x - h0.x) * (b.x - h0.x) + (b.y - h0.y) * (b.y - h0.y)));
                    for (int i = 0; i < byDist.Count && subjects.Count < 3; i++) subjects.Add(Mapped(S, byDist[i].x, byDist[i].y));
                }
                else if (S != null)
                    for (int i = 0; i < S.agents.Length && subjects.Count < 2; i++) subjects.Add(Mapped(S, S.agents[i].x, S.agents[i].y));
                Vector3 pick = Emergence.Runtime.EvidenceFraming.FrameSubjects(out var lookAt, subjects.ToArray());
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(lookAt); }
                var g = new GameObject("Fas7SaveGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = Png; g.OnGrabbed = note => { _n8 = "evidence " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 6;
                return;
            }

            if (_phase == 6)
            {
                if (_n8.Length == 0 && Time.unscaledTime - _grabAskedAt < 10f) return;
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("lane honesty: save/load is presentation-side over the deterministic engine — the sim is never");
                sb.AppendLine("touched; load resimulates from seed (grid wiped first, worker waited out). Declared v1 limits:");
                sb.AppendLine("year-granular (D-137 grid grammar; sub-year = R2-adjacent), chronicle starts empty after load,");
                sb.AppendLine("camera pose not saved. Player-vehicle proof = RUN_FAS7PSAVE (same mechanism inside a built player).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)") && _n4.Contains("(OK)")
                    && _n5.Contains("(OK)") && _n6.Contains("(OK)") && _n7.Contains("(OK)") && _n8.Contains("OK");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — save->load reproduces the world exactly (SHA-proven) and the loaded world lives on"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} shaMatch={(_shaLoad.Length > 0 && _shaLoad == _shaCont ? "YES" : "NO")} sha12={(_shaLoad.Length >= 12 ? _shaLoad.Substring(0, 12) : _shaLoad)} savedYear={(_saved != null ? _saved.year : -1)} chkDeleted={_chkDeleted} loadedSouls={_loadedSouls} loadedHuts={_loadedHuts} feedNew={_feedNew} liveOn=y{_liveOnYear}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
