// EMERGENCE — FAS 5 PROBE: the ALMANAC Overview v0, verified (per FAS5-KICKOFF-BRIEF 2026-07-22).
//
// Boots the same self-composing opening (Fas3Onboarding now also raises recorder + almanac) and
// asserts the ANALYZE surface against the recorder's own truth AND the world state:
//   1. recorder records years from genesis; view READY on the REUSED Fas4 PanelSettings resource;
//   2. truth: latest record pop == applied snapshot's agents.Length; recorder totals (births,
//      deaths) == the chronicle feed's witnessed birth/death entries (two consumers, one stream);
//   3. ALMANAC open: pauses the clock, tps untouched (the only permitted clock touch, D-145 law);
//   4. tiles == recorder truth verbatim; curve rendered from ALL records, last year == presentation;
//   5. era strip carries the derived era name (D-147) — an ERA, never a season;
//   6. close: prior pause state restored, opener button back;
//   7. scrub honesty: JumpToYear(0) trims the series to y0 (TrimCount increments);
//   8. DISARM branch (villkor-2 school): bogus PanelSettings resource -> view disarms, opener
//      never appears, WATCH/READ untouched;
//   9. evidence PNG (almanac open) grabbed END-OF-FRAME with the blankness guard (D-142 law).
// Menu: Emergence/Fas5/RUN ALMANAC PROBE.  Headless: drop Reports/RUN_FAS5ALM.trigger.
#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas5AlmanacProbe
    {
        const long Seed = 8919;
        const double Watchdog = 260.0;
        const int Horizon = 10;   // first child y1, first hut y5 at seed 8919 — margin included
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS5ALM.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS5ALM_DONE.txt");
        const string Report   = "Reports/fas5-almanac.txt";
        const string PngAlm   = "Reports/fas5-almanac.png";
        const string KeyPending = "emg.fas5alm.pending", KeyStart = "emg.fas5alm.start", KeyReport = "emg.fas5alm.report";

        static int _frames, _phase, _p4frames;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas5MetricsRecorder _rec;
        static Fas5AlmanacView _view;
        static float _tpsBefore, _grabAskedAt;
        static int _recordsAtTruth = -1, _curvePtsAtTruth = -1;   // R1 (Fas 5 review): DONE key figures stamped AT MEASUREMENT TIME, never read from view/recorder state at finish
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8 = "", _n9 = "";

        static Fas5AlmanacProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas5/RUN ALMANAC PROBE")]
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
            Fas4UIAssetsBuild.Ensure();   // the Fas4 PanelSettings is REUSED — one resource, one seam

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 5 PROBE: the Almanac Overview v0 (ANALYZE's first native face)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = Fas5MetricsRecorder (consumer #4 on the bus + applied state, read-only)");
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
            _frames = 0; _phase = 0; _p4frames = 0; _onb = null; _feed = null; _rec = null; _view = null;
            _tpsBefore = 0f; _grabAskedAt = 0f; _recordsAtTruth = -1; _curvePtsAtTruth = -1;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8 = _n9 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static int FeedCount(string kind)
        {
            int n = 0;
            foreach (var e in _feed.Entries) if (e.kind == kind) n++;
            return n;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _rec = UnityEngine.Object.FindAnyObjectByType<Fas5MetricsRecorder>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _feed == null || _rec == null || _view == null) return;
                if (!_view.enabled && _view.LastError.Length > 0) { SafeFail("view disarmed: " + _view.LastError); return; }
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_phase == 1)   // witness live until the opening beats exist
            {
                bool hut = false, birth = false;
                foreach (var e in _feed.Entries)
                {
                    if (e.kind == "milestone" && e.text.StartsWith("the first hut")) hut = true;
                    else if (e.kind == "birth") birth = true;
                }
                if (!(hut && birth))
                {
                    if (w.LastAppliedYear >= Horizon) SafeFail($"beats missing by y{w.LastAppliedYear} (hut={hut} birth={birth})");
                    return;
                }
                c.paused = true;   // freeze so counts are stable under assertion
                _phase = 2;
                return;
            }

            if (_phase == 2)   // one frame paused: recorder truth vs state vs feed
            {
                bool readyOk = _view.Ready && _rec.RecordCount > 0;
                _n1 = $"ready: view READY={_view.Ready} on reused Resources/{Fas4ChronicleView.PanelSettingsResource}, recorder {_rec.RecordCount} year-records ({(readyOk ? "OK" : "FAIL")})";

                var S = w.LastState;
                var latest = _rec.Latest();
                bool popOk = S != null && S.agents != null && latest.pop == S.agents.Length && latest.year == w.LastAppliedYear;
                int fBirths = FeedCount("birth"), fDeaths = FeedCount("death");
                int sHuts = S != null && S.huts != null ? S.huts.Length : -1;
                bool busOk = _rec.TotalBirths == fBirths && _rec.TotalDeaths == fDeaths && _rec.HutCount == sHuts;
                _n2 = $"truth: latest pop {latest.pop}==state {(S != null && S.agents != null ? S.agents.Length : -1)} @y{latest.year}=={w.LastAppliedYear}, births {_rec.TotalBirths}==feed {fBirths}, deaths {_rec.TotalDeaths}==feed {fDeaths}, huts {_rec.HutCount}==state {sHuts} ({(popOk && busOk ? "OK" : "FAIL")})";

                _tpsBefore = c.ticksPerSecond;
                bool wasPaused = c.paused;   // true — we froze; open must remember and restore THAT
                _view.OpenAlmanac(); _view.RefreshNow();
                bool pausedOk = c.paused && _view.AlmanacOpen;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n3 = $"almanac open: pauses clock {(pausedOk ? "OK" : "FAIL")} (was paused={wasPaused}), tps untouched {_tpsBefore}->{c.ticksPerSecond} ({(tpsOk ? "OK" : "FAIL")})";

                bool tilesOk = _view.TilePop == latest.pop && _view.TileBirths == _rec.TotalBirths
                            && _view.TileDeaths == _rec.TotalDeaths && _view.TileHuts == _rec.HutCount
                            && _view.TileYear == c.PresentationYear;
                bool curveOk = _view.CurvePointCount == _rec.RecordCount && _view.CurveLastYear == w.LastAppliedYear;
                _n4 = $"tiles==recorder: pop {_view.TilePop}/{latest.pop} births {_view.TileBirths}/{_rec.TotalBirths} deaths {_view.TileDeaths}/{_rec.TotalDeaths} huts {_view.TileHuts}/{_rec.HutCount} år {_view.TileYear}/{c.PresentationYear} ({(tilesOk ? "OK" : "FAIL")}); curve {_view.CurvePointCount} pts to y{_view.CurveLastYear} ({(curveOk ? "OK" : "FAIL")})";
                _recordsAtTruth = _rec.RecordCount; _curvePtsAtTruth = _view.CurvePointCount;   // stamped at the truth check (review I2)

                // R2 ink. 1: THE ONE ERA-NAME LAW — the engine's eraName wins when present, interim fallback otherwise
                bool eraOk = _view.TileEra == WorldEras.Name(S) && _view.TileEra != S.season;
                _n5 = $"era tile: '{_view.TileEra}' == WorldEras.Name(S) (era {(S != null ? S.era : 0)}, engine eraName '{(S != null ? S.eraName : "?")}'), never the season ('{(S != null ? S.season : "?")}') ({(eraOk ? "OK" : "FAIL")})";

                var g = new GameObject("Fas5AlmGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngAlm; g.OnGrabbed = note => { _n9 = "almanac " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // wait for grab, then close + scrub honesty
            {
                if (_n9.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n9.Length == 0) _n9 = "almanac evidence: FAIL (no grab within 20 s)";

                _view.CloseAlmanac(); _view.RefreshNow();
                bool closeOk = !_view.AlmanacOpen && c.paused;   // prior state was PAUSED — must be restored as paused
                _n6 = $"close: prior pause state restored (paused={c.paused}, expected True), opener back ({(closeOk ? "OK" : "FAIL")})";

                c.paused = false;
                if (!c.JumpToYear(0)) { SafeFail("JumpToYear(0) refused"); return; }
                _phase = 4;
                return;
            }

            if (_phase == 4)   // after the jump applied: series trimmed to y0
            {
                if (c.ApplyingJump) return;
                if (w.LastAppliedYear != 0) return;   // wait for the jump to land
                if (_rec.TrimCount == 0 && _p4frames++ < 600) return;   // the recorder's own Update must witness the tick drop
                bool trimOk = _rec.TrimCount > 0 && _rec.LatestYear <= 0 && _rec.RecordCount >= 1;
                _n7 = $"scrub honesty: J0 -> series trimmed to y{_rec.LatestYear} ({_rec.RecordCount} records, trims {_rec.TrimCount}, suppressed-in-jump {_rec.SuppressedDuringJump}) ({(trimOk ? "OK" : "FAIL")})";
                c.paused = true;

                // DISARM branch: a second view on a bogus resource must go dark WITHOUT touching anything
                var go = new GameObject("Fas5DisarmProof");
                var v2 = go.AddComponent<Fas5AlmanacView>();
                v2.panelSettingsResourceOverride = "Fas4PanelSettings__MISSING__";
                _phase = 5;
                return;
            }

            if (_phase == 5)   // one frame later: Start has run on the disarm candidate
            {
                var go = GameObject.Find("Fas5DisarmProof");
                var v2 = go != null ? go.GetComponent<Fas5AlmanacView>() : null;
                bool disarmed = v2 != null && !v2.Ready && !v2.enabled && v2.LastError.Contains("missing");
                bool armedIntact = _view.Ready;
                _n8 = $"disarm branch: bogus resource -> disarms (Ready={(v2 != null && v2.Ready)}, enabled={(v2 != null && v2.enabled)}, error set={(v2 != null && v2.LastError.Length > 0)}), armed view intact={armedIntact} ({(disarmed && armedIntact ? "OK" : "FAIL")})";
                if (go != null) UnityEngine.Object.Destroy(go);
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8, _n9 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: Overview v0 only — Gini/tech-loss/trade/faith + correlations await the engine metrics export (R2); the Chronicle tab IS the Fas 4 book.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)")
                    && _n3.Contains("OK") && !_n3.Contains("FAIL")
                    && _n4.Contains("OK") && !_n4.Contains("FAIL")
                    && _n5.Contains("(OK)") && _n6.Contains("(OK)") && _n7.Contains("(OK)") && _n8.Contains("(OK)")
                    && _n9.Contains("OK") && !_n9.Contains("FAIL");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — ANALYZE has its first native face: the Overview lives, honest pause + scrub semantics, the pattern-depth awaits the engine"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} recordsAtTruth={_recordsAtTruth} curvePtsAtTruth={_curvePtsAtTruth}\nsee {Report}\n");   // key figures are measurement-time stamps, a mirror of the report's OK lines (review R1)
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
