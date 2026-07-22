// EMERGENCE — FAS 4 v0 PROBE: the CHRONICLE FEED, verified (per FAS4-KICKOFF-BRIEF 2026-07-22).
//
// Boots the same self-composing opening (Fas3Onboarding now also raises Fas4ChronicleFeed) and
// asserts the BOOK against the bus's own truth, recorded independently by this probe:
//   1. genesis entries are ARRIVALS at y0, never births (D-142 semantics);
//   2. the first child's entry lands on the bus's own first-child year, salience 3;
//   3. the first hut's milestone entry exists on the bus's first-hut year;
//   4. every Milestone the bus carried has its text in the feed VERBATIM (Codex chronicleEvent path);
//   5. entry years are non-decreasing (the book reads forward);
//   6. J0 scrub: feed TRIMS to y0, arrivals survive, the rebuild burst was suppressed;
//   7. after the jump the chronicle re-witnesses (a birth entry for y>=1 returns, firsts recomputed);
//   8. evidence PNG grabbed END-OF-FRAME with the blankness guard (D-142 law — no white sheets).
// Menu: Emergence/Fas4/RUN CHRONICLE PROBE.  Headless: drop Reports/RUN_FAS4CHRON.trigger.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas4ChronicleProbe
    {
        const long Seed = 8919;
        const double Watchdog = 260.0;
        const int Horizon = 8;   // first child y1, first hut y5 at seed 8919 — y8 covers both with margin
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4CHRON.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS4CHRON_DONE.txt");
        const string Report   = "Reports/fas4-chronicle.txt";
        const string Png      = "Reports/fas4-chronicle-feed.png";
        const string KeyPending = "emg.fas4chr.pending", KeyStart = "emg.fas4chr.start", KeyReport = "emg.fas4chr.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        // the probe's OWN bus record — the truth the feed is judged against
        static int _busFirstChildYear = -1, _busFirstHutYear = -1, _busArrivals;
        static readonly List<string> _busMilestones = new List<string>();   // "year|text"
        static int _preScrubY0Count;
        static float _resumeStart, _grabAskedAt;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8 = "";
        static string _sample = "";

        static Fas4ChronicleProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN CHRONICLE PROBE")]
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
                    // subscribe on the FIRST play tick AND replay what the bounded bus log already carries:
                    // the domain reload lets the player loop run frames before the first editor callback, so
                    // the genesis burst (y0 arrivals) can predate ANY editor-side subscription. The log is the
                    // bus's own deterministic memory — replaying it closes the race for good (runs 1–2 missed y0).
                    if (_frames == 1)
                    {
                        foreach (var le in PresentationEventBus.Log) OnBusEvent(le);
                        PresentationEventBus.OnEvent += OnBusEvent;
                    }
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
            sb.AppendLine("EMERGENCE — FAS 4 v0 PROBE: the chronicle feed (consumer #3 on the Fas 0 bus)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("names = the sim's own WorldAgent.name (state read) — condition B needs a story WITH names");
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
            _frames = 0; _phase = 0; _onb = null; _feed = null;
            _busFirstChildYear = -1; _busFirstHutYear = -1; _busArrivals = 0; _busMilestones.Clear();
            _preScrubY0Count = 0; _resumeStart = 0f; _grabAskedAt = 0f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8 = ""; _sample = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void OnBusEvent(PresentationEvent e)
        {
            var clock = _onb != null ? _onb.Clock : null;
            if (clock != null && clock.ApplyingJump) return;   // keep the probe's record as honest as the feed's
            if (e.Type == PresentationEventType.Milestone)
            {
                _busMilestones.Add(e.Year + "|" + e.Data);
                if (e.Data == "the first hut" && _busFirstHutYear < 0) _busFirstHutYear = e.Year;
            }
            else if (e.Type == PresentationEventType.AgentActivity)
            {
                if (e.Data == "a child is born" && _busFirstChildYear < 0) _busFirstChildYear = e.Year;
                else if (e.Data == "a soul arrives") _busArrivals++;
            }
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _feed == null) return;
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_phase == 1)   // live witnessing until the story's three opening beats have landed
            {
                bool beats = _busArrivals > 0 && _busFirstChildYear >= 0 && _busFirstHutYear >= 0;
                if (!beats && w.LastAppliedYear < Horizon) return;
                RunPreScrubChecks();
                _preScrubY0Count = CountAtYear(0);
                if (!c.JumpToYear(0)) { SafeFail("JumpToYear(0): " + c.LastError); return; }
                _phase = 2;
                return;
            }

            if (_phase == 2)   // one frame later: the feed's Update has seen the backward tick
            {
                if (_feed.TrimCount < 1) return;   // give it a frame; watchdog catches a real failure
                int maxY = MaxYear();
                int y0 = CountAtYear(0);
                bool trimOk = maxY == 0 && y0 == _preScrubY0Count && y0 > 0;
                bool suppOk = _feed.SuppressedDuringJump > 0;
                _n6 = $"J0 scrub: trim {(trimOk ? "OK" : "FAIL")} (max year {maxY}, y0 entries {y0}/{_preScrubY0Count}, trims {_feed.TrimCount}); rebuild burst {(suppOk ? "suppressed OK" : "NOT suppressed (FAIL)")} (x{_feed.SuppressedDuringJump})";
                _resumeStart = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // the chronicle must re-witness: presentation flows again from y0
            {
                bool rebirth = false;
                for (int i = 0; i < _feed.Entries.Count; i++)
                    if (_feed.Entries[i].kind == "birth" && _feed.Entries[i].year >= 1) { rebirth = true; break; }
                if (!rebirth && Time.unscaledTime - _resumeStart < 30f) return;
                int sal = -1;
                for (int i = 0; i < _feed.Entries.Count; i++)
                    if (_feed.Entries[i].kind == "birth" && _feed.Entries[i].year >= 1) { sal = _feed.Entries[i].salience; break; }
                _n7 = rebirth
                    ? $"re-witness after J0: birth entry returned at y>=1 with salience {sal} ({(sal == 3 ? "first again — recompute OK" : "FAIL: firsts not recomputed")})"
                    : "re-witness after J0: NO birth entry within 30 s (FAIL)";
                var grabber = new GameObject("Fas4ProbeGrabber").AddComponent<Fas4ProbeGrabber>();
                grabber.OnGrabbed = note => { _n8 = note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)
            {
                if (_n8.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n8.Length == 0) _n8 = "evidence: FAIL (no grab within 20 s)";
                _phase = 99;
            }
        }

        static void RunPreScrubChecks()
        {
            // 1 — genesis semantics: y0 = arrivals, never births
            int y0Arr = 0, y0Birth = 0;
            foreach (var e in _feed.Entries)
            {
                if (e.year == 0 && e.kind == "arrival") y0Arr++;
                if (e.year == 0 && e.kind == "birth") y0Birth++;
            }
            _n1 = $"genesis: {y0Arr} arrival entries at y0 (bus carried {_busArrivals}), {y0Birth} birth entries at y0 ({(y0Arr == _busArrivals && y0Arr > 0 && y0Birth == 0 ? "OK" : "FAIL")})";

            // 2 — first child: right year, salience 3, and it carries a NAME (not the raw agent id)
            Fas4ChronicleFeed.Entry fc = default; bool found = false;
            foreach (var e in _feed.Entries)
                if (e.kind == "birth") { fc = e; found = true; break; }
            bool fcOk = found && fc.year == _busFirstChildYear && fc.salience == 3 && !fc.text.Contains("agent-");
            _n2 = found
                ? $"first child: y{fc.year} (bus y{_busFirstChildYear}) salience {fc.salience} \"{fc.text}\" ({(fcOk ? "OK" : "FAIL")})"
                : "first child: NO birth entry (FAIL)";

            // 3 — first hut milestone
            bool hutOk = false; int hutY = -1;
            foreach (var e in _feed.Entries)
                if (e.kind == "milestone" && e.text.StartsWith("the first hut")) { hutOk = e.year == _busFirstHutYear; hutY = e.year; break; }
            _n3 = $"first hut: milestone entry {(hutY >= 0 ? "y" + hutY : "MISSING")} (bus y{_busFirstHutYear}) ({(hutOk ? "OK" : "FAIL")})";

            // 4 — bus mirror: every milestone the bus carried is in the book verbatim (Codex text path)
            int missing = 0; string firstMiss = "";
            foreach (var m in _busMilestones)
            {
                int cut = m.IndexOf('|');
                int my = int.Parse(m.Substring(0, cut), CultureInfo.InvariantCulture);
                string mt = m.Substring(cut + 1);
                bool has = false;
                foreach (var e in _feed.Entries)
                    if (e.kind == "milestone" && e.year == my && e.text.StartsWith(mt)) { has = true; break; }
                if (!has) { missing++; if (firstMiss.Length == 0) firstMiss = m; }
            }
            int codexTexts = 0;
            foreach (var m in _busMilestones)
                if (!m.EndsWith("|the first hut") && !m.Contains("(told-not-shown)")) codexTexts++;
            _n4 = $"bus mirror: {_busMilestones.Count} bus milestones, {missing} missing from feed ({(missing == 0 && _busMilestones.Count > 0 ? "OK" : "FAIL")})"
                + (firstMiss.Length > 0 ? $" first missing: {firstMiss}" : "")
                + $"; codex-desc milestones in window: {codexTexts}";

            // 5 — order: the book reads forward
            bool ordered = true; int last = int.MinValue;
            foreach (var e in _feed.Entries) { if (e.year < last) { ordered = false; break; } last = e.year; }
            _n5 = $"order: entry years non-decreasing ({(ordered ? "OK" : "FAIL")}), {_feed.Entries.Count} entries, dedupe hits {_feed.DedupeHits}";

            // sample — the retellable opening, as the feed holds it (turning points only)
            var sb = new StringBuilder();
            int shown = 0;
            foreach (var e in _feed.Entries)
            {
                if (e.salience < 2 && shown >= 6) continue;
                if (shown >= 14) break;
                sb.AppendLine($"    y{e.year} {(e.salience >= 3 ? "★" : e.salience == 2 ? "•" : "·")} {e.text}");
                shown++;
            }
            _sample = sb.ToString();
        }

        static int CountAtYear(int y)
        {
            int n = 0;
            foreach (var e in _feed.Entries) if (e.year == y) n++;
            return n;
        }

        static int MaxYear()
        {
            int m = int.MinValue;
            foreach (var e in _feed.Entries) if (e.year > m) m = e.year;
            return m == int.MinValue ? -1 : m;
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                PresentationEventBus.OnEvent -= OnBusEvent;
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("the chronicle's own opening (sampled from the feed):");
                sb.AppendLine(_sample.Length > 0 ? _sample : "    (no sample)");
                sb.AppendLine("caveat: v0 is the witness — engine-lane writeHistory/causes[]/salience (with R2) makes it an oracle; native view next.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)")
                    && _n4.Contains("(OK)") && _n5.Contains("(OK)")
                    && _n6.Contains("OK") && !_n6.Contains("FAIL")
                    && _n7.Contains("OK") && !_n7.Contains("FAIL")
                    && _n8.Contains("OK") && !_n8.Contains("FAIL");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the chronicle speaks: named souls arrive, a first child, a first hut; the book trims and re-witnesses honestly under scrub"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} entries={(_feed != null ? _feed.Entries.Count : -1)} trims={(_feed != null ? _feed.TrimCount : -1)}\nsee {Report}\n");
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
            try { PresentationEventBus.OnEvent -= OnBusEvent; } catch {}
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }

    /// <summary>End-of-frame HUD grab with the D-142 blankness guard (editor play-mode variant).</summary>
    public sealed class Fas4ProbeGrabber : MonoBehaviour
    {
        public Action<string> OnGrabbed;

        void Start() { StartCoroutine(Grab()); }

        IEnumerator Grab()
        {
            yield return new WaitForEndOfFrame();   // IMGUI (time HUD + chronicle panel) has drawn
            string note;
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) note = "evidence: FAIL (null grab)";
                else
                {
                    var px = tex.GetPixels32();
                    int nonWhite = 0, dark = 0;
                    foreach (var p in px)
                    {
                        if (!(p.r > 245 && p.g > 245 && p.b > 245)) nonWhite++;
                        if (p.r < 90 && p.g < 90 && p.b < 90) dark++;
                    }
                    float nw = nonWhite / (float)px.Length;
                    bool ok = nw > 0.10f && dark > px.Length / 1000;
                    File.WriteAllBytes("Reports/fas4-chronicle-feed.png", tex.EncodeToPNG());
                    Destroy(tex);
                    note = $"evidence: {(ok ? "OK" : "FAIL(blank)")} (end-of-frame backbuffer incl. feed panel, nonwhite {(nw * 100f).ToString("F0", CultureInfo.InvariantCulture)}%) -> Reports/fas4-chronicle-feed.png";
                }
            }
            catch (Exception e) { note = "evidence: FAIL (" + e.Message + ")"; }
            var cb = OnGrabbed; if (cb != null) cb(note);
            Destroy(gameObject);
        }
    }
}
#endif
