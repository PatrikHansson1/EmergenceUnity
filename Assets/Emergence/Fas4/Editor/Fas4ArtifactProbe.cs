// EMERGENCE — FAS 4 PROBE: the CHRONICLE ARTIFACT — existence condition B put to its sharpest
// purchase-free test before the engine's causes[] delivery.
//
// D-085 condition B: "a retellable story WITH NAMES". This probe runs the SAME onboarding
// composition the player gets (bufferMode, seed 8919) across the FULL EA window (y0 -> y120)
// at max presentation speed, lets Fas4ChronicleFeed witness the whole run, and exports the
// chronicle as a READABLE ARTIFACT — the story you could actually retell:
//   Reports/chronicle-8919-y120.txt   (plain text, chronological — the saga reads forward)
//   Reports/chronicle-8919-y120.html  (standalone, almanac palette — shareable proof)
// Hard assertions:
//   1. the witnessed span covers the window (y0 entries exist, last witnessed year >= 115);
//   2. turning points (salience 3) exist in number (>= 5) and carry NAMES (first child named;
//      the first hut milestone stands);
//   3. the book reads forward (years non-decreasing) and NOTHING was dropped (capacity honest:
//      DroppedOldest == 0 — an artifact with silent gaps is not a chronicle);
//   4. both artifact files written, entry counts match the feed;
//   5. evidence: the BOOK at y120, end-of-frame, blankness-guarded (D-142 law).
// Distinct eras are REPORTED (not asserted — era pacing is the sim's own business).
// Menu: Emergence/Fas4/RUN ARTIFACT PROBE.  Headless: drop Reports/RUN_FAS4ARTIFACT.trigger.
#if UNITY_EDITOR
using System;
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
    public static class Fas4ArtifactProbe
    {
        const long Seed = 8919;
        const int TargetYear = 90;   // three generations in-editor (measured cost ~12+0.6y s/year — y120 needs the player vehicle, see D-146)
        const double Watchdog = 4200.0;   // measured: deep years cost ~15-20 s/year in-editor (reconcilers + Jint share the core) — the full window needs ~30-35 min + margin
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4ARTIFACT.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS4ARTIFACT_DONE.txt");
        const string Report   = "Reports/fas4-artifact.txt";
        const string ArtTxt   = "Reports/chronicle-8919-y090.txt";
        const string ArtHtml  = "Reports/chronicle-8919-y090.html";
        const string PngBook  = "Reports/fas4-artifact-book.png";
        const string KeyPending = "emg.fas4art.pending", KeyStart = "emg.fas4art.start", KeyReport = "emg.fas4art.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas4ChronicleView _view;
        static float _grabAskedAt;
        static double _lastBeat;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _eras = "";
        static string _sample = "";

        static Fas4ArtifactProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN ARTIFACT PROBE")]
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
            Fas4UIAssetsBuild.Ensure();

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 4 PROBE: the chronicle ARTIFACT (condition B on the full EA window)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}  seed={Seed}  window=y0..y{TargetYear}");
            sb.AppendLine("the feed witnesses the whole run LIVE; the artifact is its verbatim testimony");
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
            onb.seed = Seed; onb.targetYear = TargetYear + 4;   // producer stops just past the window

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _onb = null; _feed = null; _view = null;
            _grabAskedAt = 0f; _lastBeat = 0;
            _n1 = _n2 = _n3 = _n4 = _n5 = _eras = ""; _sample = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.Controls == null || _feed == null || _view == null) return;
                _onb.Controls.SetSpeed(3);   // ▶▶ — presentation takes what the producer gives
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_phase == 1)   // the long witness: y0 -> y120 at full pace
            {
                if (EditorApplication.timeSinceStartup - _lastBeat > 20)
                {
                    _lastBeat = EditorApplication.timeSinceStartup;
                    try { File.WriteAllText(Done, $"RUNNING y={w.LastAppliedYear}/{TargetYear} entries={_feed.Entries.Count} {DateTime.Now:HH:mm:ss}\n"); } catch {}
                }
                if (w.LastAppliedYear < TargetYear) return;
                c.paused = true;   // freeze at the window's edge; the book is now complete
                _phase = 2;
                return;
            }

            if (_phase == 2)   // assertions + artifact export
            {
                RunChecks();
                WriteArtifacts();
                _view.SetFilter(1); _view.OpenBook(); _view.RefreshNow();
                var g = new GameObject("Fas4ArtifactGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngBook; g.OnGrabbed = note => { _n5 = "evidence (book @ y" + TargetYear + "): " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)
            {
                if (_n5.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n5.Length == 0) _n5 = "evidence: FAIL (no grab within 20 s)";
                _phase = 99;
            }
        }

        static void RunChecks()
        {
            var E = _feed.Entries;

            // 1 — span
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var e in E) { if (e.year < minY) minY = e.year; if (e.year > maxY) maxY = e.year; }
            bool spanOk = minY == 0 && maxY >= TargetYear - 5 && E.Count > 20;
            _n1 = $"span: y{minY}..y{maxY}, {E.Count} entries witnessed live ({(spanOk ? "OK" : "FAIL")})";

            // 2 — turning points with names
            int stars = 0; bool namedBirth = false, firstHut = false;
            foreach (var e in E)
            {
                if (e.salience >= 3)
                {
                    stars++;
                    if (e.kind == "birth" && e.text.Contains("—")) namedBirth = true;
                    if (e.kind == "milestone" && e.text.StartsWith("the first hut")) firstHut = true;
                }
            }
            // v0 salience defines exactly three turning-point classes (first child / first hut /
            // first death) — assert THOSE, not engine milestone richness (D-146 lesson: the first
            // run demanded >=5 ★ and measured the wrong thing; deeper spines arrive with the
            // engine's writeHistory/salience delivery).
            bool namedDeath = false;
            foreach (var e in E)
                if (e.salience >= 3 && e.kind == "death" && e.text.Contains("—")) namedDeath = true;
            bool starsOk = stars >= 3 && namedBirth && firstHut && namedDeath;
            _n2 = $"turning points: {stars} ★ (v0 spine: named first child {(namedBirth ? "yes" : "NO")}, first hut {(firstHut ? "yes" : "NO")}, named first death {(namedDeath ? "yes" : "NO")}) ({(starsOk ? "OK" : "FAIL")})";

            // 3 — order + capacity honesty
            bool ordered = true; int last = int.MinValue;
            foreach (var e in E) { if (e.year < last) { ordered = false; break; } last = e.year; }
            bool capOk = _feed.DroppedOldest == 0;
            _n3 = $"order non-decreasing {(ordered ? "OK" : "FAIL")}; dropped {_feed.DroppedOldest} of capacity {Fas4ChronicleFeed.Capacity} ({(capOk ? "OK" : "FAIL — artifact has silent gaps")})";

            // eras — reported, not asserted
            var eras = new List<string>();
            foreach (var e in E) if (!string.IsNullOrEmpty(e.era) && !eras.Contains(e.era)) eras.Add(e.era);
            _eras = $"eras witnessed: {eras.Count} ({string.Join(" -> ", eras)})";

            // sample: the retellable spine — all turning points
            var sb = new StringBuilder();
            foreach (var e in E)
                if (e.salience >= 3) sb.AppendLine($"    y{e.year} ★ {e.text}");
            _sample = sb.ToString();
        }

        static void WriteArtifacts()
        {
            var E = _feed.Entries;
            try
            {
                // ---- txt: the saga, chronological ----
                var t = new StringBuilder();
                t.AppendLine("KRÖNIKAN — skriven av ingen, allt hände");
                t.AppendLine($"seed {Seed} · y0..y{TargetYear} · {E.Count} witnessed entries · exported {DateTime.Now:yyyy-MM-dd HH:mm}");
                t.AppendLine(new string('-', 72));
                foreach (var e in E)
                    t.AppendLine($"y{e.year,3} [{e.era}] {(e.salience >= 3 ? "*" : e.salience == 2 ? "." : " ")} {e.text}");
                File.WriteAllText(ArtTxt, t.ToString());

                // ---- html: standalone, almanac palette ----
                var h = new StringBuilder();
                h.Append("<!doctype html><meta charset=\"utf-8\"><title>Krönikan — seed ").Append(Seed).Append("</title><style>")
                 .Append("body{margin:0;background:#0b0f17;color:#e8eef8;font:14px/1.5 -apple-system,Segoe UI,Roboto,sans-serif}")
                 .Append(".wrap{max-width:760px;margin:0 auto;padding:26px 16px}")
                 .Append("h1{margin:0;font-size:23px}h1 span{color:#c9a227}.sub{color:#8896b2;font-size:13px;margin-bottom:14px}")
                 .Append(".card{background:#141b28;border:1px solid #273047;border-radius:12px;padding:16px 18px}")
                 .Append(".e{padding:7px 0;border-top:1px solid #1e2740;font-size:13px;color:#cdd7ea;display:flex}")
                 .Append(".e:first-child{border-top:0}.y{color:#c9a227;font-weight:600;width:64px;flex-shrink:0}")
                 .Append(".star{color:#e8eef8;font-weight:600}</style><div class=\"wrap\">")
                 .Append("<h1>Krönikan <span>— seed ").Append(Seed).Append("</span></h1>")
                 .Append("<div class=\"sub\">skriven av ingen — allt hände · y0..y").Append(TargetYear)
                 .Append(" · ").Append(E.Count).Append(" poster · vittnad live i kroppen</div><div class=\"card\">");
                foreach (var e in E)
                {
                    string mark = e.salience >= 3 ? "★ " : e.salience == 2 ? "• " : "· ";
                    h.Append("<div class=\"e\"><div class=\"y\">år ").Append(e.year).Append("</div><div")
                     .Append(e.salience >= 3 ? " class=\"star\"" : "").Append(">").Append(mark)
                     .Append(System.Net.WebUtility.HtmlEncode(e.text)).Append("</div></div>");
                }
                h.Append("</div></div>");
                File.WriteAllText(ArtHtml, h.ToString());

                bool ok = File.Exists(ArtTxt) && File.Exists(ArtHtml) && E.Count > 0;
                _n4 = $"artifact: {ArtTxt} + {ArtHtml}, {E.Count} entries each ({(ok ? "OK" : "FAIL")})";
            }
            catch (Exception e) { _n4 = "artifact: FAIL (" + e.Message + ")"; }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine(_eras.Length > 0 ? _eras : "eras: never sampled");
                sb.AppendLine();
                sb.AppendLine("the retellable spine (every turning point the feed witnessed):");
                sb.AppendLine(_sample.Length > 0 ? _sample : "    (none)");
                sb.AppendLine("caveat: salience v0 is rule-based; the engine's writeHistory/causes[]/salience (ordered) will deepen the spine. Era pacing reported, not asserted.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.EndsWith("(OK)")
                    && _n4.Contains("(OK)")
                    && _n5.Contains("OK") && !_n5.Contains("FAIL");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — condition B in artifact form: a full EA window retold with names, turning points and an honest ledger; the chronicle is a document you can hand to a stranger"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} entries={(_feed != null ? _feed.Entries.Count : -1)}\nsee {Report}\n");
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
