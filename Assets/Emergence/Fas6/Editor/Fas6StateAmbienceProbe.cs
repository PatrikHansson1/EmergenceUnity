// EMERGENCE — FAS 6 PROBE: the STATE EAR — pure intensity law, live rise, point sources, clock honesty.
//
// Boots the same self-composing opening as the era-ear probe and asserts Fas6StateAmbience:
//   1. determinism: ActivityBedSamples/CrackleSamples are pure functions — two calls byte-identical,
//      and the two textures are distinct;
//   2. the pure intensity law: IntensityFor on synthetic states returns the exact documented values
//      (null=0, empty=0, 2 fires+3 huts, winter hush 0.75);
//   3. live: genesis wilderness (0 fires/0 huts, D-135) => intensity 0, zero point sources; then the
//      village is BORN (first hut y5 at seed 8919) and the ear rises — live intensity == the pure
//      function of the applied state EXACTLY, point count == min(fires, cap), first point grounded
//      at the reconciler-mapped fire position;
//   4. clock honesty: pause freezes intensity target + point set (applied state frozen) while the
//      bed keeps sounding; tps untouched through pause and resume;
//   5. the fire-point + winter branch, FIXTURE-proven (the D-131/D-152 school): fires are rare and
//      transient in the exports (0 at y0..y55 on seed 8919) — ~no live window buys the mechanism —
//      so a REAL engine export (world-4242-y120-dusk: 1 fire, season winter) goes through the SAME
//      Fas3WorldRuntime.Apply path the clock uses: point source materialized at the mapped fire
//      position, intensity == pure(S) with the winter hush end-to-end.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// Menu: Emergence/Fas6/RUN STATE AMBIENCE PROBE.  Headless: drop Reports/RUN_FAS6STATE.trigger.
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
    public static class Fas6StateAmbienceProbe
    {
        const long Seed = 8919;
        const double Watchdog = 420.0;
        const int Horizon = 10;   // first hut y5 at seed 8919 — margin included

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS6STATE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS6STATE_DONE.txt");
        const string Report   = "Reports/fas6-state-ambience.txt";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixturePath = "Assets/Emergence/WorldStates/world-4242-y120-dusk.json";   // real engine export: 1 fire + winter
        const string KeyPending = "emg.fas6state.pending", KeyStart = "emg.fas6state.start", KeyReport = "emg.fas6state.report";

        static int _frames, _phase, _pauseFrames;
        static Fas3Onboarding _onb;
        static Fas6StateAmbience _amb;
        static float _tpsBefore;
        // measurement-time stamps (R1 law)
        static float _intensityAtCheck = -1f, _intensityAtPause = -1f, _intensityAtFixture = -1f;
        static int _firesAtCheck = -1, _hutsAtCheck = -1, _pointsAtCheck = -1, _pointsAtPause = -1, _pointsAtFixture = -1;
        static WorldState _fixture;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "";

        static Fas6StateAmbienceProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/RUN STATE AMBIENCE PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 6 PROBE: the state ear — pure intensity law + live village rise + fire points");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = applied WorldState fires/huts/season (read-only; presentation follows applied state — the Almanac's law)");
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

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _pauseFrames = 0; _onb = null; _amb = null;
            _tpsBefore = 0f;
            _intensityAtCheck = -1f; _intensityAtPause = -1f; _intensityAtFixture = -1f;
            _firesAtCheck = -1; _hutsAtCheck = -1; _pointsAtCheck = -1; _pointsAtPause = -1; _pointsAtFixture = -1;
            _fixture = null;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static bool SamplesEqual(float[] x, float[] y)
        {
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++) if (x[i] != y[i]) return false;
            return true;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _amb = UnityEngine.Object.FindAnyObjectByType<Fas6StateAmbience>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _amb == null) return;

                // 1. determinism — pure synthesis, distinct textures (cheap: 1 s at 22050)
                var a1 = Fas6StateAmbience.ActivityBedSamples(22050, 1f);
                var a2 = Fas6StateAmbience.ActivityBedSamples(22050, 1f);
                var c1 = Fas6StateAmbience.CrackleSamples(22050, 1f);
                var c2 = Fas6StateAmbience.CrackleSamples(22050, 1f);
                bool same = SamplesEqual(a1, a2) && SamplesEqual(c1, c2);
                bool diff = !SamplesEqual(a1, c1);
                _n1 = $"determinism: activity x2 identical + crackle x2 identical={same}, activity!=crackle={diff} ({(same && diff ? "OK" : "FAIL")})";

                // 2. the pure intensity law on synthetic states — exact values, no live noise
                var empty = new WorldState { season = "spring", fires = new WorldFire[0], huts = new WorldHut[0] };
                var live2 = new WorldState { season = "spring", fires = new WorldFire[2], huts = new WorldHut[3] };
                var wint2 = new WorldState { season = "winter", fires = new WorldFire[2], huts = new WorldHut[3] };
                float e0 = Fas6StateAmbience.IntensityFor(null);
                float e1 = Fas6StateAmbience.IntensityFor(empty);
                float e2 = Fas6StateAmbience.IntensityFor(live2);
                float e3 = Fas6StateAmbience.IntensityFor(wint2);
                float want2 = Mathf.Clamp01(2 * 0.30f + 3 * 0.12f);
                bool lawOk = e0 == 0f && e1 == 0f && Mathf.Approximately(e2, want2) && Mathf.Approximately(e3, want2 * 0.75f);
                _n2 = $"pure law: null={e0} empty={e1} 2f+3h={e2:F3}(want {want2:F3}) winter={e3:F3}(want {want2 * 0.75f:F3}) ({(lawOk ? "OK" : "FAIL")})";
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // genesis wilderness: the ear is silent (D-135 honesty)
            {
                var S = w.LastState;
                if (S == null) return;
                int fires = S.fires != null ? S.fires.Length : 0;
                int huts = S.huts != null ? S.huts.Length : 0;
                bool silent = fires == 0 && huts == 0 && _amb.IntensityTarget == 0f && _amb.PointSourceCount == 0;
                _n3 = $"genesis silence: fires={fires} huts={huts} intensityTarget={_amb.IntensityTarget} points={_amb.PointSourceCount} @y{w.LastAppliedYear} ({(silent ? "OK" : "FAIL")})";
                _phase = 2;
                return;
            }

            if (_phase == 2)   // live until the village is audible (first hut y5; fires as they come)
            {
                var S = w.LastState;
                if (Fas6StateAmbience.IntensityFor(S) <= 0f)
                {
                    if (w.LastAppliedYear >= Horizon) SafeFail($"ear still silent at y{w.LastAppliedYear} (expected huts/fires by y5)");
                    return;
                }
                // stamped at the live measurement (R1 law)
                _intensityAtCheck = _amb.IntensityTarget;
                _firesAtCheck = S.fires != null ? S.fires.Length : 0;
                _hutsAtCheck = S.huts != null ? S.huts.Length : 0;
                _pointsAtCheck = _amb.PointSourceCount;

                bool exact = _intensityAtCheck == Fas6StateAmbience.IntensityFor(S);
                int wantPoints = Mathf.Min(_firesAtCheck, Fas6StateAmbience.MaxPoints);
                bool pointsOk = _pointsAtCheck == wantPoints;
                bool posOk = true;
                if (_pointsAtCheck > 0)
                {
                    var f = S.fires[0];
                    var expect = new Vector3(f.x * 8f, 0, (S.H - 1 - f.y) * 8f);
                    var t = Terrain.activeTerrain;
                    if (t != null) expect.y = t.SampleHeight(expect) + t.transform.position.y;
                    expect += Vector3.up * 0.4f;
                    posOk = (_amb.FirstPointPos - expect).magnitude < 0.05f;
                }
                bool rise = _intensityAtCheck > 0f && _amb.ActivityBedPlaying;
                _n4 = $"village rise: intensity {_intensityAtCheck:F3}==pure(S)={exact}, fires={_firesAtCheck} huts={_hutsAtCheck} points {_pointsAtCheck}=={wantPoints}={pointsOk}, firstPointPos match={posOk}, bed playing+rising={rise} @y{w.LastAppliedYear} ({(exact && pointsOk && posOk && rise ? "OK" : "FAIL")})";

                _tpsBefore = c.ticksPerSecond;
                c.paused = true;
                _intensityAtPause = _amb.IntensityTarget;   // stamped at pause entry
                _pointsAtPause = _amb.PointSourceCount;
                _pauseFrames = 0;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // ~60 real frames under pause: target+points frozen, bed sounding, tps untouched
            {
                if (++_pauseFrames < 60) return;
                bool frozen = _amb.IntensityTarget == _intensityAtPause && _amb.PointSourceCount == _pointsAtPause;
                bool sounding = _amb.ActivityBedPlaying;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n5 = $"pause honesty: intensity frozen {_amb.IntensityTarget:F3}=={_intensityAtPause:F3} + points frozen {_amb.PointSourceCount}=={_pointsAtPause} over {_pauseFrames} frames={frozen}, bed still sounding={sounding}, tps untouched {_tpsBefore}->{c.ticksPerSecond} ({(frozen && sounding && tpsOk ? "OK" : "FAIL")})";

                // fire-point + winter branch, FIXTURE (real engine export) through the SAME apply
                // path the clock uses — clock still paused, so nothing races the injected state
                _fixture = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixturePath));
                // G-review r1 I2: injection is reconstruction, not witnessed history — chronicle stays silent
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_fixture); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _phase = 4;
                return;
            }

            if (_phase == 4)   // next frame: the component's Update has reconciled the fixture state
            {
                int fxFires = _fixture.fires != null ? _fixture.fires.Length : 0;
                _intensityAtFixture = _amb.IntensityTarget;                     // stamped at the fixture measurement
                _pointsAtFixture = _amb.PointSourceCount;
                float wantI = Fas6StateAmbience.IntensityFor(_fixture);
                bool exact = _intensityAtFixture == wantI && _fixture.season == "winter";
                bool pointsOk = _pointsAtFixture == Mathf.Min(fxFires, Fas6StateAmbience.MaxPoints) && _pointsAtFixture > 0;
                bool posOk = false;
                if (pointsOk)
                {
                    var f = _fixture.fires[0];
                    var expect = new Vector3(f.x * 8f, 0, (_fixture.H - 1 - f.y) * 8f);
                    var t = Terrain.activeTerrain;
                    if (t != null) expect.y = t.SampleHeight(expect) + t.transform.position.y;
                    expect += Vector3.up * 0.4f;
                    posOk = (_amb.FirstPointPos - expect).magnitude < 0.05f;
                }
                _n6 = $"fixture 4242-y120-dusk (riktig motor-export, samma Apply-väg): fires={fxFires} season={_fixture.season} intensity {_intensityAtFixture:F3}==pure(S) {wantI:F3} (winter hush end-to-end)={exact}, points={_pointsAtFixture}>0 ok={pointsOk}, firstPointPos match={posOk} ({(exact && pointsOk && posOk ? "OK" : "FAIL")})";

                c.paused = false;   // resume — the clock's next live apply reclaims the 8919 stream
                _phase = 5;
                return;
            }

            if (_phase == 5)   // resume: the clock's hands stay the player's — audio never held them
            {
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n7 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — the ambience never touches the clock";
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: v1 textures are procedural stand-ins (the purchase-free lane) — real ambience assets swap in on the");
                sb.AppendLine("replace-path when the Fas 6 audio purchase lands; speaker truth (levels/taste) = the human ear pass (Patrik).");
                sb.AppendLine("fixture honesty: the fire-point/winter branch is proven with world-4242-y120-dusk.json — a REAL engine export");
                sb.AppendLine("through the SAME Fas3WorldRuntime.Apply path (D-131/D-152 school); fires are transient in the seed-8919 window.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)")
                    && _n4.Contains("(OK)") && _n5.Contains("(OK)") && _n6.Contains("(OK)") && _n7.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the ear follows the state: silent wilderness, audible village, honest pause, the clock untouched"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} intensityAtCheck={_intensityAtCheck:F3} hutsAtCheck={_hutsAtCheck} pointsAtFixture={_pointsAtFixture} intensityAtFixture={_intensityAtFixture:F3}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
