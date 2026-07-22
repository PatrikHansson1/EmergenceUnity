// EMERGENCE — FAS 6 PROBE: the ERA EAR — deterministic beds, live era shifts, clock honesty.
//
// Boots the same self-composing opening as the Fas 5 probes and asserts Fas6EraAmbience:
//   1. determinism: BedSamples(era) is a pure function — two calls byte-identical, and distinct
//      eras yield distinct beds (dawn != bronze);
//   2. live shifts: seed 8919's early era rush (dawn->stone->bronze inside y0..y5, D-147) is
//      witnessed LIVE — >=2 crossfades, CurrentEra tracks the applied state's era exactly;
//   3. clock honesty: the ambience never touches tps; under pause the bed keeps sounding while
//      the ERA freezes (applied state frozen) — asserted over real frames, not assumed.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law from the Fas 5 gate review).
// Menu: Emergence/Fas6/RUN ERA AMBIENCE PROBE.  Headless: drop Reports/RUN_FAS6ERA.trigger.
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
    public static class Fas6EraAmbienceProbe
    {
        const long Seed = 8919;
        const double Watchdog = 420.0;
        const int Horizon = 10;   // bronze lands by y5 at seed 8919 — margin included

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS6ERA.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS6ERA_DONE.txt");
        const string Report   = "Reports/fas6-era-ambience.txt";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string KeyPending = "emg.fas6era.pending", KeyStart = "emg.fas6era.start", KeyReport = "emg.fas6era.report";

        static int _frames, _phase, _pauseFrames;
        static Fas3Onboarding _onb;
        static Fas6EraAmbience _amb;
        static float _tpsBefore;
        static int _eraAtPause = -1, _crossfadesAtCheck = -1, _eraAtCheck = -1;   // measurement-time stamps (R1 law)
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "";

        static Fas6EraAmbienceProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/RUN ERA AMBIENCE PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 6 PROBE: the era ear — deterministic beds + live era shifts");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = applied WorldState era (read-only; presentation follows applied state — the Almanac's law)");
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
            _tpsBefore = 0f; _eraAtPause = -1; _crossfadesAtCheck = -1; _eraAtCheck = -1;
            _n1 = _n2 = _n3 = _n4 = "";
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
                _amb = UnityEngine.Object.FindAnyObjectByType<Fas6EraAmbience>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _amb == null) return;

                // 1. determinism — pure function, distinct eras (cheap: 1 s at 22050)
                var a1 = Fas6EraAmbience.BedSamples(2, 22050, 1f);
                var a2 = Fas6EraAmbience.BedSamples(2, 22050, 1f);
                var b1 = Fas6EraAmbience.BedSamples(0, 22050, 1f);
                bool same = SamplesEqual(a1, a2), diff = !SamplesEqual(a1, b1);
                _n1 = $"determinism: BedSamples(bronze) x2 identical={same}, bronze!=dawn={diff} ({(same && diff ? "OK" : "FAIL")})";
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // witness live until >=2 era shifts (dawn->stone->bronze by ~y5)
            {
                var S = w.LastState;
                if (S == null) return;
                if (_amb.CrossfadesDone < 2)
                {
                    if (w.LastAppliedYear >= Horizon) SafeFail($"only {_amb.CrossfadesDone} era shifts by y{w.LastAppliedYear} (expected >=2 by y5)");
                    return;
                }
                _crossfadesAtCheck = _amb.CrossfadesDone;                       // stamped at the live measurement
                _eraAtCheck = _amb.CurrentEra;
                bool trackOk = _amb.CurrentEra == S.era && _amb.BedPlaying;
                _n2 = $"live shifts: crossfades {_crossfadesAtCheck} (>=2), CurrentEra {_eraAtCheck}==state.era {S.era} '{WorldEras.Name(S.era)}' @y{w.LastAppliedYear}, bed playing={_amb.BedPlaying} ({(trackOk ? "OK" : "FAIL")})";

                _tpsBefore = c.ticksPerSecond;
                c.paused = true;
                _eraAtPause = _amb.CurrentEra;                                   // stamped at pause entry
                _pauseFrames = 0;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // ~60 real frames under pause: era frozen, bed sounding, tps untouched
            {
                if (++_pauseFrames < 60) return;
                bool frozen = _amb.CurrentEra == _eraAtPause;
                bool sounding = _amb.BedPlaying;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n3 = $"pause honesty: era frozen {_amb.CurrentEra}=={_eraAtPause} over {_pauseFrames} frames={frozen}, bed still sounding={sounding}, tps untouched {_tpsBefore}->{c.ticksPerSecond} ({(frozen && sounding && tpsOk ? "OK" : "FAIL")})";

                c.paused = false;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // resume: the clock's hands stay the player's — audio never held them
            {
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n4 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — the ambience never touches the clock";
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: v1 beds are procedural stand-ins (the purchase-free lane) — real ambience assets swap in on the");
                sb.AppendLine("replace-path when the Fas 6 audio purchase lands; speaker truth (levels/taste) = the human ear pass (Patrik).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)") && _n4.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the ear follows the eras: deterministic beds, live shifts witnessed, the clock untouched"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} crossfadesAtCheck={_crossfadesAtCheck} eraAtCheck={_eraAtCheck}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
