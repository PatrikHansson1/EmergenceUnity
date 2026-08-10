// EMERGENCE — STORE BOOK CAPTURE RIG (trailer round, slot 5: "The chronicle", STEAM-PAGE-DRAFT §7).
//
// Captures the BOOK view (READ, Fas4ChronicleView fullscreen) as a Steam store still, from a REAL
// witnessed run on the CURRENT engine (C2 law + the gate review's freshness lesson I1: the July 22
// book PNG predates four engine waves — a store shot must witness the world the buyer gets).
//
// Mechanism = Fas4ArtifactProbe's witness verbatim: the SAME onboarding composition the player
// gets (bufferMode, seed 8919), ▶▶ to y90, pause at the window's edge — the feed has witnessed
// the whole history live (nothing injected, nothing staged). Then a candidate ring over the book:
//   filter ★ (vändpunkter — the retellable spine, "år 43 ★ death finds the people…" visible)
//   filter allt scrolled to y43 (the caption's line in its context: one line among many)
//   each with and without the IMGUI time-HUD (presentation-only toggle; the eye picks, D-008).
// Grabs are end-of-frame backbuffer with the D-142 blankness guard (Fas4NativeGrabber), at the
// game view's resolution (2560x1440 store standard). Captions/world code composited OUTSIDE the
// rig (caption grammar owns the text layer, TD-088 precedent). Candidates -> Reports/store-cap/
// (gitignored, regenerable). Headless: drop Reports/RUN_BOOKCAP.trigger.
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
    public static class StoreBookCaptureRig
    {
        const long Seed = 8919;
        const int TargetYear = 90;        // the caption's window: seed 8919, y0–y90
        const int ScrollYear = 43;        // "y 43 — death finds the people for the first time — Torv departs"
        const double Watchdog = 5400.0;   // y90 in-editor measured ~50-65 min (D-146 cost curve) + margin
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string OutDir = "Reports/store-cap";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_BOOKCAP.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "BOOKCAP_DONE.txt");
        const string Report   = "Reports/store-bookcap-report.txt";
        const string KeyPending = "emg.bookcap.pending", KeyStart = "emg.bookcap.start";

        class Cand { public string name; public int filter; public int scrollYear; public bool hud; }
        static readonly Cand[] Cands =
        {
            new Cand { name = "book-spine-nohud",      filter = 3, scrollYear = -1,         hud = false },
            new Cand { name = "book-spine-y43-nohud",  filter = 3, scrollYear = ScrollYear, hud = false },
            new Cand { name = "book-all-y43-nohud",    filter = 1, scrollYear = ScrollYear, hud = false },
            new Cand { name = "book-spine-hud",        filter = 3, scrollYear = -1,         hud = true  },
            new Cand { name = "book-all-y43-hud",      filter = 1, scrollYear = ScrollYear, hud = true  },
        };

        static int _frames, _phase, _cand, _step, _scrollIdx;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas4ChronicleView _view;
        static Fas3TimeControls _hud;
        static float _grabAskedAt;
        static double _lastBeat;
        static string _grabNote;
        static readonly StringBuilder _log = new StringBuilder();

        static StoreBookCaptureRig() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Marketing/RUN STORE BOOK CAPTURE")]
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
            Directory.CreateDirectory(OutDir);

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
            onb.seed = Seed; onb.targetYear = TargetYear + 4;

            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _cand = 0; _step = 0; _scrollIdx = -2;
            _grabAskedAt = 0f; _lastBeat = 0; _grabNote = null;
            _log.Length = 0;
            _log.AppendLine("EMERGENCE — STORE BOOK CAPTURE RIG (slot 5 'The chronicle', real witnessed run)");
            _log.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}  seed={Seed}  window=y0..y{TargetYear}  candidates={Cands.Length}");
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
                _hud  = UnityEngine.Object.FindAnyObjectByType<Fas3TimeControls>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.Controls == null || _feed == null || _view == null) return;
                _onb.Controls.SetSpeed(3);   // ▶▶ — presentation takes what the producer gives
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_phase == 1)   // the long witness: y0 -> y90 at full pace
            {
                if (EditorApplication.timeSinceStartup - _lastBeat > 20)
                {
                    _lastBeat = EditorApplication.timeSinceStartup;
                    try { File.WriteAllText(Done, $"RUNNING y={w.LastAppliedYear}/{TargetYear} entries={_feed.Entries.Count} {DateTime.Now:HH:mm:ss}\n"); } catch {}
                }
                if (w.LastAppliedYear < TargetYear) return;
                c.paused = true;   // freeze at the window's edge; the book is now complete
                _view.OpenBook();
                _log.AppendLine($"witnessed: y0..y{w.LastAppliedYear}, {_feed.Entries.Count} entries, book open");
                _phase = 2; _cand = 0; _step = 0;
                return;
            }

            if (_phase == 2)   // candidate ring: configure -> settle/scroll -> grab -> next
            {
                if (_cand >= Cands.Length) { _phase = 99; return; }
                var cd = Cands[_cand];

                if (_step == 0)   // configure
                {
                    _view.SetFilter(cd.filter);
                    if (_hud != null) _hud.enabled = cd.hud;
                    _view.RefreshNow();
                    _scrollIdx = -2; _grabNote = null;
                    _step = 1;
                    return;
                }
                if (_step == 1)   // layout has run once since refresh — scroll if asked
                {
                    // scrollYear < 0 means TOP — reset explicitly (run 1 lesson: the ScrollView keeps
                    // the previous candidate's offset across a rebuild, so "top" silently inherited it)
                    _scrollIdx = _view.ScrollBookToYear(cd.scrollYear >= 0 ? cd.scrollYear : int.MaxValue);
                    _step = 2;
                    return;
                }
                if (_step == 2)   // one more frame so the scroll offset is applied in layout
                {
                    // run 1 lesson (all-y43 candidate): the view's own Update may rebuild rows the
                    // frame after our RefreshNow, leaving the offset computed against stale layout —
                    // re-apply the scroll now that layout is final (idempotent).
                    _scrollIdx = _view.ScrollBookToYear(cd.scrollYear >= 0 ? cd.scrollYear : int.MaxValue);
                    var g = new GameObject("BookCapGrabber").AddComponent<Fas4NativeGrabber>();
                    g.Path = Path.Combine(OutDir, cd.name + ".png");
                    g.OnGrabbed = note => { _grabNote = note; };
                    _grabAskedAt = Time.unscaledTime;
                    _step = 3;
                    return;
                }
                if (_step == 3)   // wait for the grab
                {
                    if (_grabNote == null && Time.unscaledTime - _grabAskedAt < 20f) return;
                    string note = _grabNote ?? "evidence: FAIL (no grab within 20 s)";
                    _log.AppendLine($"[{cd.name}] filter={(cd.filter == 3 ? "vändpunkter" : "allt")} scroll={(cd.scrollYear >= 0 ? "y" + cd.scrollYear + " (row " + _scrollIdx + ")" : "top")} hud={(cd.hud ? "on" : "off")} rows={_view.BookRowCount} — {note}");
                    _cand++; _step = 0;
                    return;
                }
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                if (_hud != null) _hud.enabled = true;   // leave the scene honest
                var sb = new StringBuilder(_log.ToString());
                sb.AppendLine();
                if (overtime) sb.AppendLine("WATCHDOG cut — candidate set may be incomplete");
                bool green = !overtime && _cand >= Cands.Length && !sb.ToString().Contains("FAIL");
                sb.AppendLine("The human eye picks the final frame (D-008); caption + world code composited outside the rig (TD-088 grammar).");
                sb.AppendLine($"verdict: {(green ? "GREEN" : "CHECK")} — {_cand}/{Cands.Length} candidates captured");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} candidates={_cand}/{Cands.Length}\nsee {Report}\n");
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
