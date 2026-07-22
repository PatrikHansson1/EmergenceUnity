// EMERGENCE — FAS 4 PROBE: the NATIVE CHRONICLE VIEW, verified (per FAS4-NATIVE-CHRONICLE-VIEW-SKISS).
//
// Boots the same self-composing opening (Fas3Onboarding raises feed + native view) and asserts
// the UI Toolkit surface against the feed's own truth:
//   1. view READY on the real PanelSettings asset; the IMGUI v0 panel stands down (showUI=false);
//   2. FEED mode: rendered rows == the feed's filtered tail (bounded at FeedMaxRows);
//   3. filter switches: "vändpunkter" shows only ★ rows, count matches; back to "allt" restores;
//   4. BOOK mode: opening PAUSES the clock, tps untouched (the only permitted clock touch);
//   5. book holds the WHOLE history, newest FIRST (the reference's order), gold year badges;
//   6. why-expander STUB expands on click (the surface for the ordered engine causes[]);
//   7. closing the book RESTORES the prior pause state; feed panel returns;
//   8. evidence PNGs (book + feed) grabbed END-OF-FRAME with the blankness guard (D-142 law);
//   9. DISARM branch (gate review 2026-07-22 villkor 2): a view pointed at a bogus PanelSettings
//      resource disarms itself and the IMGUI v0 panel STANDS — the chronicle never goes dark.
// Menu: Emergence/Fas4/RUN NATIVE VIEW PROBE.  Headless: drop Reports/RUN_FAS4NATIVE.trigger.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas4NativeViewProbe
    {
        const long Seed = 8919;
        const double Watchdog = 260.0;
        const int Horizon = 10;   // first child y1, first hut y5 at seed 8919 — margin included
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4NATIVE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS4NATIVE_DONE.txt");
        const string Report   = "Reports/fas4-native-view.txt";
        const string PngBook  = "Reports/fas4-native-book.png";
        const string PngFeed  = "Reports/fas4-native-feed.png";
        const string KeyPending = "emg.fas4nat.pending", KeyStart = "emg.fas4nat.start", KeyReport = "emg.fas4nat.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas4ChronicleView _view;
        static float _tpsBefore, _grabAskedAt;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8a = "", _n8b = "", _n9 = "";

        static Fas4NativeViewProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN NATIVE VIEW PROBE")]
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
            Fas4UIAssetsBuild.Ensure();   // PanelSettings + runtime theme must exist as real assets

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 4 PROBE: the native chronicle view (UI Toolkit, feed/book)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = the SAME Fas4ChronicleFeed as v0 (no new truth); styling per almanac reference");
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
            _frames = 0; _phase = 0; _onb = null; _feed = null; _view = null;
            _tpsBefore = 0f; _grabAskedAt = 0f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8a = _n8b = _n9 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static int CountAtLeast(int sal)
        {
            int n = 0;
            foreach (var e in _feed.Entries) if (e.salience >= sal) n++;
            return n;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _feed == null || _view == null) return;
                if (!_view.enabled && _view.LastError.Length > 0) { SafeFail("view disarmed: " + _view.LastError); return; }
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_phase == 1)   // witness live until the opening beats exist in the feed
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
                c.paused = true;   // freeze the presentation so counts are stable under assertion
                _phase = 2;
                return;
            }

            if (_phase == 2)   // one frame paused: feed-mode checks
            {
                _view.RefreshNow();
                _n1 = $"ready: view READY={_view.Ready} on Resources/{Fas4ChronicleView.PanelSettingsResource}, IMGUI v0 stood down={!_feed.showUI} ({(_view.Ready && !_feed.showUI ? "OK" : "FAIL")})";

                int expAll = Mathf.Min(Fas4ChronicleView.FeedMaxRows, CountAtLeast(1));
                bool rowsOk = _view.FeedRowCount == expAll && expAll > 0;
                _n2 = $"feed rows: {_view.FeedRowCount} rendered / expected {expAll} of {_feed.Entries.Count} entries ({(rowsOk ? "OK" : "FAIL")})";

                _view.SetFilter(3); _view.RefreshNow();
                int expTurn = Mathf.Min(Fas4ChronicleView.FeedMaxRows, CountAtLeast(3));
                bool allStars = true;
                for (int i = 0; i < _view.FeedRowCount; i++) if (_view.FeedRowSalience(i) < 3) allStars = false;
                bool filterOk = _view.FeedRowCount == expTurn && expTurn > 0 && allStars;
                _view.SetFilter(1); _view.RefreshNow();
                bool restored = _view.FeedRowCount == expAll;
                _n3 = $"filter: vändpunkter -> {(filterOk ? "only ★ rows, " + expTurn + " shown OK" : "FAIL")}; allt restored {(restored ? "OK" : "FAIL")}";

                c.paused = false;                    // book-open must be what pauses next
                _tpsBefore = c.ticksPerSecond;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // book open + content + stub, then evidence grab
            {
                _view.OpenBook(); _view.RefreshNow();
                bool pausedOk = c.paused && _view.BookOpen;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n4 = $"book open: pauses clock {(pausedOk ? "OK" : "FAIL")}, tps untouched {_tpsBefore}->{c.ticksPerSecond} ({(tpsOk ? "OK" : "FAIL")})";

                int expBook = CountAtLeast(1);
                bool countOk = _view.BookRowCount == expBook && expBook > 0;
                int first = _view.BookRowYear(0), last = _view.BookRowYear(_view.BookRowCount - 1);
                bool orderOk = first >= last;
                _n5 = $"book content: {_view.BookRowCount}/{expBook} entries, newest first y{first}>=y{last} ({(countOk && orderOk ? "OK" : "FAIL")})";

                bool stubShown = _view.ExpandBookRow(0);
                _view.RefreshNow();   // expansion must SURVIVE a rebuild — and be in the evidence PNG
                bool stubHeld = stubShown && !_view.ExpandBookRow(0) && _view.ExpandBookRow(0);   // was open post-rebuild; toggle back on
                _n6 = $"why-expander stub: click row 0 -> visible {(stubShown ? "OK" : "FAIL")}, survives rebuild {(stubHeld ? "OK" : "FAIL")} (causes[] ordered in MOTOR-LANE-ORDER-R2-FAS4)";

                var g = new GameObject("Fas4NativeGrabberBook").AddComponent<Fas4NativeGrabber>();
                g.Path = PngBook; g.OnGrabbed = note => { _n8a = "book " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // wait for book grab, then close + feed grab
            {
                if (_n8a.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n8a.Length == 0) _n8a = "book evidence: FAIL (no grab within 20 s)";

                _view.CloseBook(); _view.RefreshNow();
                bool closeOk = !_view.BookOpen && !c.paused;   // prior state was unpaused — must be restored
                _n7 = $"book close: pause state restored (paused={c.paused}), feed panel back ({(closeOk ? "OK" : "FAIL")})";

                var g = new GameObject("Fas4NativeGrabberFeed").AddComponent<Fas4NativeGrabber>();
                g.Path = PngFeed; g.OnGrabbed = note => { _n8b = "feed " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 5;
                return;
            }

            if (_phase == 5)
            {
                if (_n8b.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n8b.Length == 0) _n8b = "feed evidence: FAIL (no grab within 20 s)";
                _phase = 6;
                return;
            }

            if (_phase == 6)   // DISARM branch (gate review 2026-07-22, villkor 2): missing UI assets must leave IMGUI v0 standing
            {
                _feed.showUI = true;   // arrange: the v0 panel visible, as in a world where the native assets never existed
                var go = new GameObject("Fas4DisarmProof");
                var v2 = go.AddComponent<Fas4ChronicleView>();
                v2.panelSettingsResourceOverride = "Fas4PanelSettings__MISSING__";   // probe seam — bogus resource, real assets untouched
                _phase = 7;
                return;
            }

            if (_phase == 7)   // one frame later: Start has run on the disarm candidate
            {
                var go = GameObject.Find("Fas4DisarmProof");
                var v2 = go != null ? go.GetComponent<Fas4ChronicleView>() : null;
                bool disarmed = v2 != null && !v2.Ready && !v2.enabled && v2.LastError.Contains("missing");
                bool imguiStands = _feed.showUI;
                _n9 = $"disarm branch: bogus PanelSettings resource -> view disarms itself (Ready={(v2 != null && v2.Ready)}, enabled={(v2 != null && v2.enabled)}, error set={(v2 != null && v2.LastError.Length > 0)}), IMGUI v0 STANDS (showUI={imguiStands}) ({(disarmed && imguiStands ? "OK" : "FAIL")})";
                if (go != null) UnityEngine.Object.Destroy(go);
                _feed.showUI = false;   // restore: the armed native face owns the surface again
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8a, _n8b, _n9 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: why-expander is a STUB by design (engine causes[] ordered); three scales + LLM prose out of v1 scope.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.EndsWith("OK") && !_n3.Contains("FAIL")
                    && _n4.Contains("OK") && !_n4.Contains("FAIL")
                    && _n5.Contains("(OK)") && _n6.Contains("OK") && !_n6.Contains("FAIL")
                    && _n7.Contains("(OK)")
                    && _n8a.Contains("OK") && !_n8a.Contains("FAIL")
                    && _n8b.Contains("OK") && !_n8b.Contains("FAIL")
                    && _n9.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the chronicle has its native face: almanac-styled feed + fullscreen book, honest pause semantics, the why-surface awaits the engine"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} feedRows={(_view != null ? _view.FeedRowCount : -1)} bookRows={(_view != null ? _view.BookRowCount : -1)}\nsee {Report}\n");
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

    /// <summary>End-of-frame grab with the D-142 blankness guard (parameterized target path).</summary>
    public sealed class Fas4NativeGrabber : MonoBehaviour
    {
        public string Path;
        public Action<string> OnGrabbed;

        void Start() { StartCoroutine(Grab()); }

        IEnumerator Grab()
        {
            yield return new WaitForEndOfFrame();   // UI Toolkit + IMGUI have drawn
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
                    File.WriteAllBytes(Path, tex.EncodeToPNG());
                    Destroy(tex);
                    note = $"evidence: {(ok ? "OK" : "FAIL(blank)")} (end-of-frame backbuffer, nonwhite {(nw * 100f).ToString("F0", CultureInfo.InvariantCulture)}%) -> {Path}";
                }
            }
            catch (Exception e) { note = "evidence: FAIL (" + e.Message + ")"; }
            var cb = OnGrabbed; if (cb != null) cb(note);
            Destroy(gameObject);
        }
    }
}
#endif
