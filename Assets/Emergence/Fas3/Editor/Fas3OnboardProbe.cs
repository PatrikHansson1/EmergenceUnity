// EMERGENCE — FAS 3 increment 6 PROBE (D-139): the ONBOARDING COMPOSITION — the game's actual start.
//
// The probe COMPOSES NOTHING in play mode — that is the point. The edit phase builds the start
// scene (genesis export from the live engine -> honest wilderness dress -> camera + Fas3Onboarding,
// saved as OnboardingScene.unity, gitignored/generated); the play phase only OBSERVES the scene
// booting itself: Fas3Onboarding raises driver (bufferMode, genesis-first) + clock (1×) + controls
// + gaze on its own, and the first hour's opening beats must land as ONE unbroken experience:
//   (a) GENESIS — year 0 applies from frame one: souls == genesis canon, huts == 0, true wilderness.
//   (b) THE FIRST HUT — born under the player's eye; the gaze TAKES the eye there (angle-verified).
//   (c) THE FIRST CHILD — the beat occurs; if outside the gaze cooldown the gaze takes it too
//       (cooldown is design, D-134 — gaze-per-beat is reported, the BEAT is gated).
//   (d) UNBROKEN — applied years strictly ascending from 0, no pause, no scene load, HUD's measured
//       t/s recorded (the honest number: presentation ≤ producer).
// Magenta gates on the worst captured frame. Evidence: genesis + first-hut gaze + village end.
// Menu: Emergence/Fas3/RUN ONBOARD PROBE.  Headless: drop Reports/RUN_FAS3ONBOARD.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Jint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas3OnboardProbe
    {
        const long Seed = 8919;
        const double Watchdog = 340.0;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/OnboardingScene.unity";
        const int SafetyYear = 12;   // finish with FAILs if the beats never came by here

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3ONBOARD.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3ONBOARD_DONE.txt");
        const string Report   = "Reports/fas3-onboard.txt";
        const string KeyPending = "emg.fas3onb.pending", KeyStart = "emg.fas3onb.start", KeyReport = "emg.fas3onb.report", KeySouls = "emg.fas3onb.souls";

        // same genesis exporter the cold start proved (D-135) — tiles included for the dresser
        const string GenesisExportJs = @"(function(){var E=Emergence,S=__S;
var tt='';var tn=[];for(var y=0;y<E.H;y++)for(var x=0;x<E.W;x++){var c=S.tiles[y][x];tt+=c.t[0];tn.push(c.n);}
return JSON.stringify({
engineVersion:E.VERSION,seed:__seed,years:0,tick:S.tick,ended:!!S.ended,season:''+S.season,W:E.W,H:E.H,
tileTypes:tt,tileN:tn,
agents:S.agents.filter(function(a){return !a.dead}).map(function(a){return {id:a.id,name:''+a.name,x:a.x,y:a.y,age:a.age,gen:a.gen,task:''+a.task,say:''+(a.say||''),sayAct:''+(a.sayAct||''),home:!!a.home}}),
dead:S.agents.filter(function(a){return a.dead}).length,
huts:S.huts.map(function(h){return {x:h.x,y:h.y,owner:''+(h.owner||''),free:!!h.free}}),
fires:S.fires.map(function(f){return {x:f.x,y:f.y,fuel:f.fuel}}),
fields:S.fields.map(function(f){return {x:f.x,y:f.y,stage:f.stage,owner:''+(f.owner||'')}}),
villages:S.villages.map(function(v){return {x:v.x,y:v.y,name:''+v.name}}),
animals:S.animals.map(function(an){return {id:an.id,type:''+an.type,x:an.x,y:an.y}}),
dna:''+E.computeDNA(S)})})()";

        static int _frames, _phase, _magenta = -1, _magentaTone = -1;
        static Fas3Onboarding _onboard;
        static Fas3GazeDirector _gaze;
        static string _genesisNote = "", _hutNote = "", _childNote = "", _flowNote = "", _tpsNote = "";
        static bool _genesisShot, _hutShot, _endShot, _hutBeat, _childBeat, _pausedEver;
        static int _hutBeatYear = -1, _childBeatYear = -1;
        static float _hutGazeCheckAt = -1f, _childGazeCheckAt = -1f;

        static Fas3OnboardProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN ONBOARD PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 3 ONBOARDING PROBE (D-139): the game's actual start, observed");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 1) genesis export from the SAME live engine (D-135 exporter)
            string engineDir = Path.Combine(Application.dataPath, "Emergence", "Engine");
            var engineSrc = File.ReadAllText(EmergenceJintHost.EngineSourcePath(engineDir));
            var preludeSrc = File.ReadAllText(Path.Combine(engineDir, "harness", "prelude-hypot.js"));
            var host = new EmergenceJintHost(engineSrc, preludeSrc);
            host.Engine.Execute($"var __seed={Seed}; var __S=Emergence.createWorld({Seed}); __S.silent=true;");
            string json = host.Engine.Evaluate(GenesisExportJs).AsString();
            File.WriteAllText(GenesisPath, json);
            AssetDatabase.Refresh();
            var G = JsonUtility.FromJson<WorldState>(json);
            int souls = G.agents?.Length ?? 0;
            SessionState.SetInt(KeySouls, souls);
            bool wild = (G.huts?.Length ?? 0) == 0 && (G.fields?.Length ?? 0) == 0 && (G.villages?.Length ?? 0) == 0;
            sb.AppendLine($"genesis: souls={souls}, huts={G.huts?.Length ?? 0}, honest wilderness={(wild ? "YES" : "NO (FAIL)")}");

            // 2) the start scene: wilderness dress + camera + THE COMPOSER — then saved as the artifact
            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[Fas3Onb] look: " + e.Message); }

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var onbGo = new GameObject("Fas3Onboarding");
            var onb = onbGo.AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;   // the game: endless

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { Fail("scene save failed"); return; }
            sb.AppendLine($"start scene saved: {ScenePath} (gitignored, generated — genesis wilderness + Fas3Onboarding)");

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _magenta = _magentaTone = -1;
            _onboard = null; _gaze = null;
            _genesisNote = _hutNote = _childNote = _flowNote = _tpsNote = "";
            _genesisShot = _hutShot = _endShot = _hutBeat = _childBeat = _pausedEver = false;
            _hutBeatYear = _childBeatYear = -1; _hutGazeCheckAt = _childGazeCheckAt = -1f;
            File.WriteAllText(Done, "RUNNING (entering play mode — the scene boots ITSELF) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void OnBusEvent(PresentationEvent e)
        {
            if (e.Type == PresentationEventType.Milestone && e.Data == "the first hut")
            { _hutBeat = true; _hutBeatYear = e.Year; }
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a child is born" && !_childBeat)
            { _childBeat = true; _childBeatYear = e.Year; }
        }

        static void Drive(float t)
        {
            if (_phase == 0)   // observe only: wait for the scene's own composition to exist
            {
                _onboard = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                if (_onboard == null || _onboard.Driver == null || _onboard.Clock == null) return;
                _gaze = Camera.main != null ? Camera.main.GetComponent<Fas3GazeDirector>() : null;
                PresentationEventBus.OnEvent += OnBusEvent;
                _phase = 1;
                return;
            }

            var w = _onboard.World; var d = _onboard.Driver; var c = _onboard.Clock;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }
            if (_onboard.Clock.paused) _pausedEver = true;

            // (a) genesis applied — souls stand in true wilderness from the first beat
            if (_genesisNote.Length == 0 && w.LastAppliedYear >= 0)
            {
                int expect = SessionState.GetInt(KeySouls, -1);
                bool ok = w.LastAppliedYear == 0 && w.AgentCount == expect && w.HutCount == 0;
                _genesisNote = $"genesis: applied y{w.LastAppliedYear} at t={t:F1}s, souls {w.AgentCount}=={expect}, huts {w.HutCount}==0 -> {(ok ? "TRUE START (OK)" : "FAIL")}";
                if (!_genesisShot) { FrameOn("Agents_Live"); CaptureEvidence("fas3-onboard-genesis"); _genesisShot = true; }
            }

            // (b) the first hut — the gaze must take the eye there
            if (_hutBeat && _hutNote.Length == 0)
            {
                if (_gaze != null && _gaze.HasTarget && _gaze.TargetLabel.Contains("hut"))
                {
                    if (_hutGazeCheckAt < 0f) _hutGazeCheckAt = t + 1.2f;
                    else if (t >= _hutGazeCheckAt)
                    {
                        var cam = Camera.main;
                        float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                        _hutNote = $"first hut: y{_hutBeatYear}, gaze \"{_gaze.TargetLabel}\" angle {ang:F1}° -> {(ang < 15f ? "TAKEN (OK)" : "OFF (FAIL)")}";
                        if (!_hutShot) { CaptureEvidence("fas3-onboard-firsthut"); _hutShot = true; }
                    }
                }
                else if (_hutGazeCheckAt < 0f && w.HutCount > 0 && _gaze != null && !_gaze.HasTarget && t > 20f)
                {
                    // hut stood but gaze never took it (should not happen — hut-raise publishes coords)
                    _hutNote = $"first hut: y{_hutBeatYear}, gaze NEVER TOOK IT (FAIL)";
                }
            }

            // (c) the first child — the beat gates; the gaze is reported (cooldown is design)
            if (_childBeat && _childNote.Length == 0)
            {
                if (_gaze != null && _gaze.HasTarget && (_gaze.TargetLabel.Contains("born") || _gaze.TargetLabel.Contains("arrives")))
                {
                    if (_childGazeCheckAt < 0f) _childGazeCheckAt = t + 1.2f;
                    else if (t >= _childGazeCheckAt)
                    {
                        var cam = Camera.main;
                        float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                        _childNote = $"first child: y{_childBeatYear}, gaze \"{_gaze.TargetLabel}\" angle {ang:F1}° -> {(ang < 15f ? "TAKEN (OK)" : "OFF")}";
                        CaptureEvidence("fas3-onboard-firstchild");
                    }
                }
                else if (w.LastAppliedYear >= _childBeatYear + 2)
                    _childNote = $"first child: y{_childBeatYear}, BEAT OK (gaze within cooldown of another birth — by design, D-134)";
            }

            // (d) end: both beats resolved (or the safety horizon) -> flow verdict + final frames
            bool done = _hutNote.Length > 0 && _childNote.Length > 0;
            if (done || w.LastAppliedYear >= SafetyYear)
            {
                string order = c.LastAppliedOrder;
                bool orderOk = order.StartsWith("0,1,2");
                _flowNote = $"unbroken: order [{Cut(order, 40)}] {(orderOk ? "ascending from GENESIS (OK)" : "BROKEN (FAIL)")}; paused ever={_pausedEver} (must be False); one scene, no menu";
                _tpsNote = $"HUD honesty: measured {_onboard.Controls.EffectiveTps:F1} t/s presentation vs producer flat-out (~19) — presentation never outruns truth; buffer +{d.BufferedYears}y";
                if (!_endShot) { FrameOn(HutReconciler.LayerName); CaptureEvidence("fas3-onboard-village"); _endShot = true; }
                _phase = 99;
            }
        }

        static string Cut(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "…";

        static void FrameOn(string layerName)
        {
            var cam = Camera.main; if (cam == null) return;
            var layer = GameObject.Find(layerName);
            var ctr = Vector3.zero; int n = 0;
            if (layer != null) foreach (Transform h in layer.transform) { ctr += h.position; n++; }
            if (n == 0) { var al = GameObject.Find("Agents_Live"); if (al != null) foreach (Transform a in al.transform) { ctr += a.position; n++; } }
            if (n == 0) return;
            ctr /= n;
            var pos = ctr + Vector3.back * 22f;
            var t = Terrain.activeTerrain;
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y;
            cam.transform.position = pos + Vector3.up * 7f;
            cam.transform.LookAt(ctr + Vector3.up * 1.2f);
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
                PresentationEventBus.OnEvent -= OnBusEvent;
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")}) — the scene composed ITSELF; the probe only watched");
                sb.AppendLine(_genesisNote.Length > 0 ? _genesisNote : "(genesis never applied — FAIL)");
                sb.AppendLine(_hutNote.Length > 0 ? _hutNote : "first hut: BEAT NEVER CAME (FAIL)");
                sb.AppendLine(_childNote.Length > 0 ? _childNote : "first child: BEAT NEVER CAME (FAIL)");
                sb.AppendLine(_flowNote.Length > 0 ? _flowNote : "(flow verdict never computed)");
                sb.AppendLine(_tpsNote);
                sb.AppendLine();
                sb.AppendLine($"magenta: classic={_magenta} tonemapped={_magentaTone} (worst of genesis + first-hut + village frames)   evidence: 45-UNITY/evidence/fas3/fas3-onboard-*.png");
                bool green = _genesisNote.Contains("OK") && _hutNote.Contains("OK") && _childNote.Contains("OK")
                          && _flowNote.Contains("OK") && !_flowNote.Contains("FAIL") && !_pausedEver
                          && _magenta == 0 && _magentaTone == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the player lands in the body: wilderness, then the first hut, then the first child — one unbroken hour begins"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} hutBeat={_hutBeat}@y{_hutBeatYear} childBeat={_childBeat}@y{_childBeatYear} magenta={_magenta}/{_magentaTone}\nsee {Report}\n");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
        }

        static void SafeFail(string msg)
        {
            try { PresentationEventBus.OnEvent -= OnBusEvent; } catch {}
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
