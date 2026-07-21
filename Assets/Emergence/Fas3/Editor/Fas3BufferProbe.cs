// EMERGENCE — FAS 3 increment 4 PROBE (D-136/D-137): the LOOKAHEAD BUFFER + CHECKPOINT GRID.
//
// One live run, seed 8919, genesis -> y6, bufferMode: the driver RACES flat-out while the
// presentation clock consumes years at the player's pace. Proves, in order:
//   (a) PACING HONESTY — year 1 is applied when the presentation tick crosses its boundary
//       (~6 s at 1×), NOT when the producer finishes it (the buffer decouples, the clock paces).
//   (b) THE DECOUPLING ITSELF — pausing the clock freezes the applied year while the producer
//       keeps racing: produced years grow UNDER pause (D-136's core: pausing widens the lookahead).
//   (c) IN-ORDER CONSUMPTION — every year 1..6 reconciles exactly once, strictly ascending, and the
//       final live hut count matches the y6 canon (2 huts).
//   (d) THE CHECKPOINT GRID — all 6 year snapshots persist (seq pattern, persistentDataPath);
//       JumpToYear(3) rebuilds y3's world from disk (hut count == the checkpoint's own), and
//       JumpToYear(6) returns to the canon end state. Scrub without resimulation, year-grained.
// Magenta gates on the worst frame (gaze + jump evidence + 4 village angles), D-131 detector.
// Menu: Emergence/Fas3/RUN BUFFER PROBE.  Headless: drop Reports/RUN_FAS3BUF.trigger.
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
    public static class Fas3BufferProbe
    {
        const string Backdrop = "Assets/Emergence/WorldStates/seq-8919-y006.json";
        const long Seed = 8919;
        const int TargetYear = 6;
        const double Watchdog = 320.0;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3BUF.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3BUF_DONE.txt");
        const string Report   = "Reports/fas3-buffer.txt";
        const string KeyPending = "emg.fas3buf.pending", KeyStart = "emg.fas3buf.start", KeyReport = "emg.fas3buf.report";

        static int _frames, _phase, _magenta = -1, _magentaTone = -1, _hutsExpected = -1;
        static Fas3SimDriver _driver;
        static Fas3WorldRuntime _world;
        static Fas3PresentationClock _clock;
        static Fas3TimeControls _controls;
        static float _firstApplyAt = -1f, _pauseStart = -1f;
        static int _pausedAppliedYear = -1, _producedAtPause = -1, _producedUnderPause = -1;
        static string _paceNote = "", _decoupleNote = "", _orderNote = "", _jumpNote = "", _gridNote = "";
        static bool _evidenceDone;

        static Fas3BufferProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN BUFFER PROBE")]
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
                    Drive((float)EditorApplication.timeSinceStartup - start);
                    if (_phase == 99 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within watchdog");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 3 INCREMENT 4 PROBE (D-136/D-137): lookahead buffer + checkpoint grid");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"one live run, seed {Seed}, genesis -> y{TargetYear}, bufferMode: producer races, the clock paces.");
            sb.AppendLine();

            WorldDresser.Build(Backdrop);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var S6 = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop));
            _hutsExpected = S6.huts?.Length ?? -1;
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S6.season) ? "spring" : S6.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[Fas3Buf] look: " + e.Message); }
            sb.AppendLine($"backdrop terrain from seq-8919-y006; born-from-state layers retired; y6 canon huts={_hutsExpected}");

            // stale-grid honesty: this run must WRITE its own checkpoints — clear previous ones
            try
            {
                string grid = Path.Combine(Application.persistentDataPath, "Emergence", "checkpoints");
                if (Directory.Exists(grid))
                    foreach (var f in Directory.GetFiles(grid, $"seq-{Seed}-y*.json")) File.Delete(f);
                sb.AppendLine($"checkpoint grid cleared for seed {Seed}: {grid}");
            }
            catch (Exception e) { sb.AppendLine("grid clear failed (non-gating): " + e.Message); }

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _magenta = _magentaTone = -1; _evidenceDone = false;
            _firstApplyAt = -1f; _pauseStart = -1f; _pausedAppliedYear = _producedAtPause = _producedUnderPause = -1;
            _paceNote = _decoupleNote = _orderNote = _jumpNote = _gridNote = "";
            _driver = null; _world = null; _clock = null; _controls = null;
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive(float t)
        {
            if (_phase == 0)   // boot: world runtime + buffer driver + clock at 1× + controls
            {
                _world = new GameObject("Fas3WorldRuntime").AddComponent<Fas3WorldRuntime>();
                var go = new GameObject("Fas3SimDriver");
                _driver = go.AddComponent<Fas3SimDriver>();
                _driver.seed = Seed; _driver.bufferMode = true; _driver.targetYear = TargetYear; _driver.lookaheadYears = 16;
                var cgo = new GameObject("Fas3PresentationClock");
                _clock = cgo.AddComponent<Fas3PresentationClock>();
                _clock.driver = _driver; _clock.world = _world; _clock.ticksPerSecond = Fas3TimeControls.BaseTps;
                var ui = new GameObject("Fas3TimeControls");
                _controls = ui.AddComponent<Fas3TimeControls>();
                _controls.driver = _driver; _controls.clock = _clock;
                try { _hutsExpected = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop)).huts?.Length ?? -1; }
                catch { _hutsExpected = -1; }
                _phase = 1;
                return;
            }

            if (_driver != null && _driver.LastError.Length > 0) { SafeFail("driver: " + _driver.LastError); return; }

            switch (_phase)
            {
                case 1: // (a) pacing honesty — y1 applies at ~6 s (144/24), never early, buffer notwithstanding
                    if (_world.LastAppliedYear >= 1 && _firstApplyAt < 0f)
                    {
                        _firstApplyAt = t;
                        // t includes play-mode boot overhead (~1-2 s before the clock starts) — the guard is
                        // against EARLY application (producer speed leaking into presentation), so >= 5 s gates.
                        _paceNote = $"pacing: y1 applied at t={t:F1}s (boundary 6.0s at 1x + boot) -> {(t >= 5.0f ? "PACED (OK)" : "EARLY (FAIL)")}";
                        _pausedAppliedYear = _world.LastAppliedYear;
                        _producedAtPause = _driver.Year;
                        _controls.SetPause(true);          // through the UI path
                        _pauseStart = t;
                        _phase = 2;
                    }
                    break;

                case 2: // (b) decoupling — presentation frozen, producer keeps racing
                    if (_world.LastAppliedYear != _pausedAppliedYear)
                    { _decoupleNote = $"decouple: applied year MOVED under pause ({_pausedAppliedYear} -> {_world.LastAppliedYear}) (FAIL)"; _phase = 3; _controls.SetSpeed(2); break; }
                    if (_driver.Year >= _producedAtPause + 2 || _driver.Finished)
                    {
                        _producedUnderPause = _driver.Year - _producedAtPause;
                        _decoupleNote = $"decouple: paused at applied y{_pausedAppliedYear}; producer advanced y{_producedAtPause} -> y{_driver.Year} (+{_producedUnderPause}) with buffer +{_driver.BufferedYears} -> {(_producedUnderPause >= 2 ? "RACING (OK)" : "STALLED (FAIL)")}";
                        _controls.SetSpeed(2);             // resume at 4× — consume the lookahead
                        _phase = 3;
                    }
                    else if (t - _pauseStart > 120f)
                    { _decoupleNote = "decouple: producer never advanced 2 years under pause (FAIL)"; _controls.SetSpeed(2); _phase = 3; }
                    break;

                case 3: // (c) consume to y6 in order
                    if (_driver.Finished && _world.LastAppliedYear >= TargetYear)
                    {
                        string order = _clock.LastAppliedOrder;
                        bool inOrder = order.StartsWith("1,2,3,4,5,6");
                        bool hutsOk = _world.HutCount == _hutsExpected;
                        _orderNote = $"order: applied [{order}] -> {(inOrder ? "STRICT 1..6 (OK)" : "OUT OF ORDER (FAIL)")}; live huts {_world.HutCount} == y6 canon {_hutsExpected} -> {(hutsOk ? "MATCH (OK)" : "MISMATCH (FAIL)")}";
                        if (!_evidenceDone) { CaptureEvidence("fas3-buffer-y6"); _evidenceDone = true; }
                        _phase = 4;
                    }
                    break;

                case 4: // (d) checkpoint grid + scrub
                {
                    var sbGrid = new StringBuilder();
                    int found = 0;
                    for (int y = 1; y <= TargetYear; y++)
                    {
                        string p = Path.Combine(_driver.CheckpointDir, $"seq-{Seed}-y{y:000}.json");
                        if (File.Exists(p)) found++;
                        else sbGrid.Append($" y{y:000}-MISSING");
                    }
                    _gridNote = $"grid: {found}/{TargetYear} checkpoints persisted in {_driver.CheckpointDir}{sbGrid}";

                    int y3Huts = -1;
                    try
                    {
                        var s3 = JsonUtility.FromJson<WorldState>(File.ReadAllText(Path.Combine(_driver.CheckpointDir, $"seq-{Seed}-y003.json")));
                        y3Huts = s3.huts?.Length ?? 0;
                    }
                    catch { }
                    bool j3 = _clock.JumpToYear(3);
                    bool j3Ok = j3 && _world.LastAppliedYear == 3 && y3Huts >= 0 && _world.HutCount == y3Huts;
                    CaptureEvidence("fas3-buffer-jump-y3");
                    bool j6 = _clock.JumpToYear(6);
                    bool j6Ok = j6 && _world.LastAppliedYear == 6 && _world.HutCount == _hutsExpected;
                    _jumpNote = $"scrub: J3 {(j3Ok ? $"rebuilt y3 from disk, huts=={y3Huts} (OK)" : $"FAIL ({_clock.LastError})")}; J6 {(j6Ok ? $"returned to canon, huts=={_hutsExpected} (OK)" : $"FAIL ({_clock.LastError})")}";
                    for (int dir = 0; dir < 4; dir++)
                        if (FrameVillage(dir)) CaptureEvidence("fas3-buffer-village-" + "NESW"[dir]);
                    try
                    {
                        var tex = ScreenCapture.CaptureScreenshotAsTexture();
                        if (tex != null)
                        {
                            const string dirp = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
                            Directory.CreateDirectory(dirp);
                            File.WriteAllBytes(Path.Combine(dirp, "fas3-buffer-ui.png"), tex.EncodeToPNG());
                            UnityEngine.Object.Destroy(tex);
                        }
                    }
                    catch { }
                    UnityEngine.Object.Destroy(_driver.gameObject);
                    _driver = null;
                    _phase = 99;
                    break;
                }
            }
        }

        static bool FrameVillage(int dir)
        {
            var cam = Camera.main; if (cam == null) return false;
            var layer = GameObject.Find(HutReconciler.LayerName);
            var c = Vector3.zero; int n = 0;
            if (layer != null) foreach (Transform h in layer.transform) { c += h.position; n++; }
            if (n == 0) { var al = GameObject.Find("Agents_Live"); if (al != null) foreach (Transform a in al.transform) { c += a.position; n++; } }
            if (n == 0) return false;
            c /= n;
            var back = Quaternion.Euler(0f, dir * 90f, 0f) * Vector3.back * 22f;
            var pos = c + back;
            var t = Terrain.activeTerrain;
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y;
            cam.transform.position = pos + Vector3.up * 7f;
            cam.transform.LookAt(c + Vector3.up * 1.2f);
            return true;
        }

        static void CaptureEvidence(string name)
        {
            var cam = Camera.main; if (cam == null) return;
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            var px = tex.GetPixels32(); int mag = 0, tone = 0;
            foreach (var c in px)
            {
                if (c.r > 220 && c.b > 220 && c.g < 80) mag++;
                else if (Math.Abs(c.r - c.b) < 15 && c.r > 170 && c.g < c.r - 90) tone++;
            }
            _magenta = Math.Max(_magenta, mag); _magentaTone = Math.Max(_magentaTone, tone);
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine(_paceNote.Length > 0 ? _paceNote : "(pacing test did not run)");
                sb.AppendLine(_decoupleNote.Length > 0 ? _decoupleNote : "(decoupling test did not run)");
                sb.AppendLine(_orderNote.Length > 0 ? _orderNote : "(order test did not run)");
                sb.AppendLine(_gridNote.Length > 0 ? _gridNote : "(grid test did not run)");
                sb.AppendLine(_jumpNote.Length > 0 ? _jumpNote : "(scrub test did not run)");
                sb.AppendLine();
                sb.AppendLine($"magenta: classic={_magenta} tonemapped={_magentaTone} (worst frame incl. jump + 4 village angles)   evidence: 45-UNITY/evidence/fas3/fas3-buffer-*.png");
                bool green = _paceNote.Contains("OK") && !_paceNote.Contains("FAIL")
                          && _decoupleNote.Contains("OK") && !_decoupleNote.Contains("FAIL")
                          && _orderNote.Contains("OK") && !_orderNote.Contains("FAIL")
                          && _gridNote.Contains($"{TargetYear}/{TargetYear}")
                          && _jumpNote.Contains("OK") && !_jumpNote.Contains("FAIL")
                          && _magenta == 0 && _magentaTone == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the producer races, the player's clock paces; every year lands in order; any produced year re-enters from the grid"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} magenta={_magenta}/{_magentaTone}\nsee {Report}\n");
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
