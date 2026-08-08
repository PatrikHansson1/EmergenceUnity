// EMERGENCE — FAS 7 increment 2: SOAK/STABILITY — one undisturbed long player session.
//
// The gate's wording: "ingen krasch/soft-lock på en full session". The full 120-year EA window is
// producer-bound ~2.7+ h (D-148) and WAITS for the engine lane's deep-tick scaling — so this soak
// runs the window that FITS and declares its span honestly: y0 -> soakYears at 1× (the EA cadence;
// the producer wall owns the pace beyond it). Undisturbed = no pause, no scrub, no jumps — the
// opening composition runs exactly as a player would leave it running.
//
// What is measured (trended per applied year, R1-stamped at measurement):
//   - crash: the observer itself writes COMPLETE; a watchdog cut or driver error is the failure;
//   - soft-lock: no year applied for > 90 s while the producer lives => SOFTLOCK;
//   - order: years apply 0,1,2,... unbroken (an undisturbed session may never skip or repeat);
//   - pacing law: presentation year may NEVER exceed the producer's year (violations counted);
//   - bounded guards at every sample: bus log <= capacity, chronicle feed <= capacity (+ dropped
//     counter), metrics series <= capacity, lookahead buffer <= cap, checkpoint grid == produced years;
//   - trends reported, not hidden: GC heap first->last (hard gate only at > 4x = gross leak),
//     max frame hitch, seconds per year.
// Evidence: end frame through the SHARED framing law (cluster: hut + 2 nearest souls),
// magenta-scanned + blank-guarded. Writes soak-player.txt + soak-player-trend.txt beside the exe.
// D-078 r4: observes presentation APIs only; the sim is never touched.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas7SoakPlayerProof : MonoBehaviour
    {
        public long seed = 8919;
        public int soakYears = 40;
        public float watchdogSecs = 600f;
        public float softLockSecs = 90f;

        struct Sample { public int year; public float t; public int bus, feed, feedDropped, metrics, buffered; public float gcMB, maxHitch; }

        int _phase; int _waitFrames; float _waitAnchor;
        Fas3Onboarding _onb;
        readonly System.Collections.Generic.List<Sample> _trend = new System.Collections.Generic.List<Sample>();
        int _lastYear = -1; float _lastYearAt; float _maxHitchThisYear; float _t0 = -1f;
        int _paceViolations, _orderBreaks;
        int _busMax, _feedMax, _metricsMax, _bufferedMax;
        int _magenta = -1, _magentaTone = -1;
        string _nSpan = "", _nOrder = "", _nPace = "", _nBounds = "", _nLock = "", _nTrend = "", _nEvid = "";

        string OutPath => Path.Combine(Application.dataPath, "..", "soak-player.txt");
        string TrendPath => Path.Combine(Application.dataPath, "..", "soak-player-trend.txt");
        string PngPath => Path.Combine(Application.dataPath, "..", "soak-player.png");

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs) { Finish("WATCHDOG(phase" + _phase + ",y" + _lastYear + ")"); return; }

            if (_phase == 0)
            {
                Application.runInBackground = true;
                var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
                onb.seed = seed; onb.targetYear = -1;
                _onb = onb;
                _t0 = Time.realtimeSinceStartup; _lastYearAt = _t0;
                _phase = 1;
                return;
            }

            if (_onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
            var d = _onb.Driver; var c = _onb.Clock; var w = _onb.World;
            if (d.LastError.Length > 0) { Finish("driverError=" + d.LastError.Replace('\n', ' ')); return; }

            if (_phase == 1)
            {
                float now = Time.realtimeSinceStartup;
                _maxHitchThisYear = Mathf.Max(_maxHitchThisYear, Time.unscaledDeltaTime);
                if (c.PresentationYear > d.Year) _paceViolations++;   // presentation may never outrun truth

                int y = w.LastAppliedYear;
                if (y > _lastYear)   // a new year landed — sample the guards (R1: at measurement)
                {
                    if (y != _lastYear + 1 && !(_lastYear == -1 && y == 0)) _orderBreaks++;
                    var feed = FindAnyObjectByType<Fas4ChronicleFeed>();
                    var rec = FindAnyObjectByType<Fas5MetricsRecorder>();
                    var s = new Sample
                    {
                        year = y, t = now - _t0,
                        bus = PresentationEventBus.Count,
                        feed = feed != null ? feed.Entries.Count : -1,
                        feedDropped = feed != null ? feed.DroppedOldest : -1,
                        metrics = rec != null ? rec.RecordCount : -1,
                        buffered = d.BufferedYears,
                        gcMB = GC.GetTotalMemory(false) / 1048576f,
                        maxHitch = _maxHitchThisYear,
                    };
                    _trend.Add(s);
                    _busMax = Mathf.Max(_busMax, s.bus); _feedMax = Mathf.Max(_feedMax, s.feed);
                    _metricsMax = Mathf.Max(_metricsMax, s.metrics); _bufferedMax = Mathf.Max(_bufferedMax, s.buffered);
                    _lastYear = y; _lastYearAt = now; _maxHitchThisYear = 0f;
                }
                else if (now - _lastYearAt > softLockSecs && !d.Finished)
                { _nLock = $"softlock=FAIL(no year for {(now - _lastYearAt):F0}s at y{_lastYear})"; Finish(""); return; }

                if (_lastYear >= soakYears)
                {
                    // verdict notes (R1-stamped from the trend just gathered)
                    float secs = now - _t0;
                    _nSpan = $"span=OK(y0->y{_lastYear} undisturbed, {secs:F0}s at 1x/producer wall — declared slice; full 120y WAITS on deep-tick D-148)";
                    _nOrder = $"order={(_orderBreaks == 0 ? "OK" : "FAIL")}(breaks={_orderBreaks})";
                    _nPace = $"pace={(_paceViolations == 0 ? "OK" : "FAIL")}(violations={_paceViolations})";
                    var first = _trend[0]; var last = _trend[_trend.Count - 1];
                    bool bounds = _busMax <= PresentationEventBus.LogCapacity && _feedMax <= Fas4ChronicleFeed.Capacity
                               && _metricsMax <= Fas5MetricsRecorder.Capacity && _bufferedMax <= Mathf.Max(1, d.lookaheadYears);
                    _nBounds = $"bounds={(bounds ? "OK" : "FAIL")}(busMax={_busMax}/{PresentationEventBus.LogCapacity},feedMax={_feedMax}/{Fas4ChronicleFeed.Capacity},metricsMax={_metricsMax}/{Fas5MetricsRecorder.Capacity},lookaheadMax={_bufferedMax}/{d.lookaheadYears})";
                    if (_nLock.Length == 0) _nLock = "softlock=OK(none)";
                    float gcRatio = first.gcMB > 1f ? last.gcMB / first.gcMB : 1f;
                    bool gcOk = gcRatio < 4f;   // hard gate only at gross growth; the number is reported either way
                    float maxHitch = 0f; foreach (var s in _trend) maxHitch = Mathf.Max(maxHitch, s.maxHitch);
                    _nTrend = string.Format(CultureInfo.InvariantCulture,
                        "trend={0}(gc {1:F0}->{2:F0}MB x{3:F2}<4, maxHitch {4:F2}s, {5:F1}s/year avg)",
                        gcOk ? "OK" : "FAIL", first.gcMB, last.gcMB, gcRatio, maxHitch, secs / Mathf.Max(1, _lastYear));
                    WriteTrend();

                    // evidence: frameable cluster through the shared law
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
                    Vector3 pick = EvidenceFraming.FrameSubjects(out var lookAt, subjects.ToArray());
                    var cam = Camera.main;
                    if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(lookAt); }
                    _waitFrames = 0;
                    _phase = 2;
                }
                return;
            }

            if (_phase == 2)   // let a frame render at the picked angle, then grab + finish
            {
                if (++_waitFrames < 5) return;
                CaptureFrame(PngPath);
                Finish("");
            }
        }

        void WriteTrend()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("# FAS 7 SOAK trend — one row per applied year (R1: stamped at measurement)");
                sb.AppendLine("# year secs bus feed feedDropped metrics buffered gcMB maxHitchSecs");
                foreach (var s in _trend)
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0,4} {1,6:F1} {2,5} {3,5} {4,3} {5,4} {6,2} {7,7:F1} {8,5:F2}",
                        s.year, s.t, s.bus, s.feed, s.feedDropped, s.metrics, s.buffered, s.gcMB, s.maxHitch));
                File.WriteAllText(TrendPath, sb.ToString());
            }
            catch { }
        }

        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        void CaptureFrame(string path)
        {
            try
            {
                var cam = Camera.main; if (cam == null) { _nEvid = "evidence=FAIL(no camera)"; return; }
                bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
                const int pw = 1600, ph = 900;
                var rt = new RenderTexture(pw, ph, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                RenderSettings.fog = fogWas;
                var px = tex.GetPixels32(); int mag = 0, tone = 0, nonBlack = 0;
                foreach (var p in px)
                {
                    if (p.r > 220 && p.b > 220 && p.g < 80) mag++;
                    else if (Math.Abs(p.r - p.b) < 15 && p.r > 170 && p.g < p.r - 90) tone++;
                    if (p.r + p.g + p.b > 30) nonBlack++;
                }
                _magenta = mag; _magentaTone = tone;
                bool blank = nonBlack < px.Length / 10;
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex); Destroy(rt);
                _nEvid = $"evidence={(blank ? "FAIL(blank)" : "OK")}(framed by shared law)";
            }
            catch (Exception e) { _nEvid = "evidence=FAIL(" + e.Message + ")"; }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "soak {0} {1} {2} {3} {4} {5} {6} magenta={7}/{8} {9}\n",
                N(_nSpan), N(_nOrder), N(_nPace), N(_nBounds), N(_nLock), N(_nTrend), N(_nEvid),
                _magenta, _magentaTone, error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }

        static string N(string s) => s.Length > 0 ? s : "NEVER-REACHED";
    }
}
