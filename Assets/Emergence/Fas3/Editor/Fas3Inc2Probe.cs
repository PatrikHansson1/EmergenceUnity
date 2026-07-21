// EMERGENCE — FAS 3 increment 2 PROBE (D-134): THE VILLAGE IS BORN + THE PLAYER'S HAND ON TIME.
//
// One live run, seed 8919, genesis -> y6 (y6 canon: 2 huts, 6 souls). Backdrop terrain dressed from
// the verified seq-8919-y006 snapshot; ALL born-from-state layers retired (huts/yards/age + agents
// + codex) so the world must GROW them live:
//   (a) HUT LAYER LIVE — HutReconciler raises huts the year the sim builds them; final live count
//       must equal the snapshot's hut count (and match the y6 canon file).
//   (b) TIME-CONTROL UI — Fas3TimeControls buttons are exercised programmatically mid-run: pause
//       (tick must freeze), 1x -> 24 t/s and 4x -> 96 t/s must land in the driver verbatim.
//   (c) CADENCE MEASUREMENT — the flat-out segment measures real editor-Jint ticks/s; the report
//       extrapolates the EA window (~26k ticks) under editor-Jint, 1x and 4x. The datapoint D-133b
//       asked for.
//   (d) THE GAZE — Fas3GazeDirector must take the camera to a birth (hut or child) on its own;
//       verified by angle-to-target, evidence captured mid-gaze ("titta, något föddes").
// Menu: Emergence/Fas3/RUN INC2 PROBE.  Headless: drop Reports/RUN_FAS3INC2.trigger.
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
    public static class Fas3Inc2Probe
    {
        const string Backdrop = "Assets/Emergence/WorldStates/seq-8919-y006.json";
        const long Seed = 8919;
        const int TargetYear = 6;          // y6 canon: 2 huts, 6 souls — huts are BORN inside the window
        const double Watchdog = 260.0;     // ~70 s flat-out sim + UI tests + margin

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3INC2.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3INC2_DONE.txt");
        const string Report   = "Reports/fas3-inc2.txt";
        const string KeyPending = "emg.fas3inc2.pending", KeyStart = "emg.fas3inc2.start", KeyReport = "emg.fas3inc2.report";

        static int _frames, _phase, _magenta = -1, _magentaTone = -1, _snapshots, _hutsRaised, _hutsFinal = -1, _hutsExpected = -1, _countMismatches;
        static Fas3SimDriver _driver;
        static AgentReconciler _agents;
        static HutReconciler _huts;
        static LiveReconciler _codex;
        static Fas3TimeControls _controls;
        static Fas3GazeDirector _gaze;
        static string _pauseNote = "", _speedNote = "", _gazeNote = "", _codexNote = "codex live: not attempted", _uiShotNote = "";
        static bool _evidenceDone;
        static int _pauseTickBefore = -1;
        static float _pauseUntil = -1f, _speedAt = -1f, _gazeCheckAt = -1f;
        static float _cadStart = -1f, _cadEnd = -1f; static int _cadTickStart, _cadTickEnd;

        static Fas3Inc2Probe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN INC2 PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 3 INCREMENT 2 PROBE (D-134): living huts + time UI + cadence + the gaze");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"one live run, seed {Seed}, genesis -> y{TargetYear}; all born-from-state layers retired, the world must GROW them.");
            sb.AppendLine();

            WorldDresser.Build(Backdrop);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var S6 = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop));
            _hutsExpected = S6.huts?.Length ?? -1;
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S6.season) ? "spring" : S6.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[Fas3Inc2] look: " + e.Message); }
            sb.AppendLine($"backdrop terrain from seq-8919-y006 (year-stable); static Huts/Yards/HutAge/Agents/Codex retired; y6 canon huts={_hutsExpected}");

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _magenta = _magentaTone = -1; _snapshots = 0; _hutsRaised = 0; _hutsFinal = -1; _countMismatches = 0;
            _pauseNote = _speedNote = _gazeNote = _uiShotNote = ""; _codexNote = "codex live: not attempted";
            _driver = null; _controls = null; _gaze = null; _evidenceDone = false;
            _pauseTickBefore = -1; _pauseUntil = -1f; _speedAt = -1f; _gazeCheckAt = -1f;
            _cadStart = _cadEnd = -1f; _cadTickStart = _cadTickEnd = 0;
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive(float t)
        {
            if (_phase == 0)   // boot: reconcilers + driver (flat-out) + time controls + gaze handle
            {
                _agents = new AgentReconciler();
                _huts = new HutReconciler();
                _codex = new LiveReconciler();
                var go = new GameObject("Fas3SimDriver");
                _driver = go.AddComponent<Fas3SimDriver>();
                _driver.seed = Seed; _driver.ticksPerSecond = Fas3TimeControls.MaxTps; _driver.targetYear = TargetYear;
                var ui = new GameObject("Fas3TimeControls");
                _controls = ui.AddComponent<Fas3TimeControls>();
                _controls.driver = _driver;
                _gaze = Camera.main != null ? Camera.main.GetComponent<Fas3GazeDirector>() : null;
                // recomputed here (not carried from edit phase): statics reset if enter-play does a domain reload
                try { _hutsExpected = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop)).huts?.Length ?? -1; }
                catch { _hutsExpected = -1; }
                _cadStart = t; _cadTickStart = 0;
                _phase = 1;
                return;
            }

            // consume year snapshots -> live reconcile (agents + huts + codex)
            if (_driver != null)
            {
                var json = _driver.TakeYearSnapshot();
                if (json != null)
                {
                    var S = JsonUtility.FromJson<WorldState>(json);
                    _agents.Reconcile(S, false);
                    var hd = _huts.Reconcile(S);
                    _hutsRaised += hd.raised;
                    if (_huts.Count != (S.huts?.Length ?? 0)) _countMismatches++;
                    _hutsFinal = _huts.Count;
                    try { _codex.Reconcile(S); _codexNote = "codex live: OK (reconciled every year)"; }
                    catch (Exception e) { _codexNote = "codex live: SKIPPED (" + e.Message + ")"; }
                    _snapshots++;
                }
                if (_driver.LastError.Length > 0) { SafeFail("driver: " + _driver.LastError); return; }
            }

            // the gaze is autonomous — verify it aimed whenever it claims a target (any phase)
            if (_gaze != null && _gaze.HasTarget && _gazeCheckAt < 0f) _gazeCheckAt = t + 1.2f;   // let the glide settle
            if (_gaze != null && _gazeCheckAt > 0f && t >= _gazeCheckAt && _gazeNote.Length == 0)
            {
                var cam = Camera.main;
                if (_gaze.HasTarget && cam != null)
                {
                    float ang = Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position);
                    _gazeNote = $"gaze: \"{_gaze.TargetLabel}\" angle-to-target {ang:F1}° -> {(ang < 15f ? "AIMED (OK)" : "OFF (FAIL)")}";
                    if (!_evidenceDone) { CaptureEvidence("fas3-inc2-gaze"); _evidenceDone = true; }
                }
                else _gazeCheckAt = -1f;   // target released before check — wait for the next gaze
            }

            switch (_phase)
            {
                case 1: // flat-out to y2 — this segment IS the cadence measurement
                    if (_driver.Year >= 2)
                    {
                        _cadEnd = t; _cadTickEnd = _driver.Tick;
                        _pauseTickBefore = _driver.Tick;
                        _controls.SetPause(true);          // through the UI path, not the driver directly
                        _pauseUntil = t + 0.9f;
                        _phase = 2;
                    }
                    break;

                case 2: // pause via UI: tick must freeze
                    if (t >= _pauseUntil)
                    {
                        bool frozen = _driver.Tick == _pauseTickBefore && _driver.paused;
                        _pauseNote = $"pause via UI: tick {_pauseTickBefore} == {_driver.Tick} after 0.9s -> {(frozen ? "FROZEN (OK)" : "MOVED (FAIL)")}";
                        _controls.SetSpeed(1);             // resume at 1×
                        _speedAt = t + 0.4f;
                        _phase = 3;
                    }
                    break;

                case 3: // speed mapping via UI: 1× then 4× must land verbatim in the driver
                    if (t >= _speedAt && _speedNote.Length == 0)
                    {
                        bool ok1 = Mathf.Approximately(_driver.ticksPerSecond, Fas3TimeControls.BaseTps) && !_driver.paused;
                        _controls.SetSpeed(2);
                        _speedNote = $"speed via UI: 1x -> {Fas3TimeControls.BaseTps} t/s {(ok1 ? "OK" : "FAIL")}";
                        _speedAt = t + 0.4f;
                    }
                    else if (t >= _speedAt && !_speedNote.Contains("4x"))
                    {
                        bool ok4 = Mathf.Approximately(_driver.ticksPerSecond, Fas3TimeControls.BaseTps * 4f);
                        _speedNote += $"; 4x -> {Fas3TimeControls.BaseTps * 4f} t/s {(ok4 ? "OK" : "FAIL")}";
                        _controls.SetSpeed(3);             // back to flat-out for the rest of the run
                        _phase = 4;
                    }
                    break;

                case 4: // run to y6; the huts of years 2-6 are born under our eyes
                    if (_driver.Finished)
                    {
                        try { _uiShotNote = CaptureUiShot() ? "UI evidence: fas3-inc2-ui.png (game view incl. HUD)" : "UI evidence: game-view capture unavailable (headless)"; }
                        catch (Exception e) { _uiShotNote = "UI evidence: failed (" + e.Message + ")"; }
                        if (!_evidenceDone && _hutsFinal > 0) { CaptureEvidence("fas3-inc2-gaze"); _evidenceDone = true; }
                        // village retake lesson (D-131/D-134): a single fixed offset can land INSIDE a canopy —
                        // shoot all four compass directions; magenta accumulates across every frame.
                        for (int dir = 0; dir < 4; dir++)
                            if (FrameVillage(dir)) CaptureEvidence("fas3-inc2-village-" + "NESW"[dir]);
                        UnityEngine.Object.Destroy(_driver.gameObject);
                        _driver = null;
                        _phase = 99;
                    }
                    break;
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
            var back = Quaternion.Euler(0f, dir * 90f, 0f) * Vector3.back * 22f;   // N/E/S/W around the huts
            var pos = c + back;
            var t = Terrain.activeTerrain;
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y;
            cam.transform.position = pos + Vector3.up * 7f;
            cam.transform.LookAt(c + Vector3.up * 1.2f);
            return true;
        }

        static bool CaptureUiShot()
        {
            // game-view backbuffer incl. IMGUI HUD — best effort, never gates the verdict
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null) return false;
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "fas3-inc2-ui.png"), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
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
            _magenta = Math.Max(_magenta, mag); _magentaTone = Math.Max(_magentaTone, tone);   // worst frame gates
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
                sb.AppendLine($"year snapshots applied live: {_snapshots} (target y{TargetYear})");
                bool hutsOk = _hutsRaised > 0 && _hutsFinal == _hutsExpected && _countMismatches == 0;
                sb.AppendLine($"huts BORN live: {_hutsRaised} raised, final live count {_hutsFinal} == y6 canon {_hutsExpected} -> {(hutsOk ? "MATCH (OK)" : "MISMATCH (FAIL)")} (yearly count mismatches: {_countMismatches})");
                sb.AppendLine(_pauseNote.Length > 0 ? _pauseNote : "(pause test did not run)");
                sb.AppendLine(_speedNote.Length > 0 ? _speedNote : "(speed test did not run)");
                sb.AppendLine(_gazeNote.Length > 0 ? _gazeNote : "gaze: NEVER TOOK A TARGET (FAIL)");
                sb.AppendLine(_codexNote);
                sb.AppendLine(_uiShotNote);
                sb.AppendLine();
                sb.AppendLine("## CADENCE (the D-133b datapoint)");
                float cadSecs = _cadEnd - _cadStart; int cadTicks = _cadTickEnd - _cadTickStart;
                float tps = cadSecs > 0.5f ? cadTicks / cadSecs : -1f;
                sb.AppendLine($"flat-out editor-Jint: {cadTicks} ticks in {cadSecs:F1}s = {tps:F1} ticks/s (genesis -> y2, incl. live reconciles)");
                if (tps > 0f)
                {
                    const int eaTicks = 26000;
                    sb.AppendLine($"EA window ~{eaTicks} ticks: flat-out editor-Jint ≈ {eaTicks / tps / 60f:F1} min; " +
                                  $"at 1x (24 t/s) ≈ {eaTicks / 24f / 60f:F1} min; at 4x (96 t/s, if compute allows) ≈ {eaTicks / 96f / 60f:F1} min");
                    string vs1 = tps >= Fas3TimeControls.BaseTps ? "holds 1x" : "can NOT even hold 1x";
                    sb.AppendLine(tps < 90f
                        ? $"finding: editor-Jint ({tps:F0} t/s) {vs1} ({Fas3TimeControls.BaseTps} t/s) and NOT 4x (96 t/s) — real-time speeds need player-build Jint speed and/or checkpoint+resimulate; measure player next."
                        : "finding: editor-Jint holds 4x — checkpoint strategy may be optional; confirm on player build.");
                }
                sb.AppendLine();
                sb.AppendLine($"magenta: classic={_magenta} tonemapped={_magentaTone} (worst frame of gaze + 4 village angles)   evidence: 45-UNITY/evidence/fas3/fas3-inc2-gaze.png + fas3-inc2-village-{{N,E,S,W}}.png");
                bool pauseOk = _pauseNote.Contains("FROZEN");
                bool speedOk = _speedNote.Contains("OK") && !_speedNote.Contains("FAIL");
                bool gazeOk = _gazeNote.Contains("AIMED");
                bool green = hutsOk && pauseOk && speedOk && gazeOk && _snapshots >= TargetYear
                             && _magenta == 0 && _magentaTone == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the village is BORN live; the player's hand holds presentation time; the eye goes to what was born"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} huts={_hutsRaised}/{_hutsExpected} " +
                                        $"pause={pauseOk} speed={speedOk} gaze={gazeOk} snapshots={_snapshots} magenta={_magenta}/{_magentaTone}\nsee {Report}\n");
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
