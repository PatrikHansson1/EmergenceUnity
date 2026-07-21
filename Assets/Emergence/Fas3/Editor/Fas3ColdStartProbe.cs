// EMERGENCE — FAS 3 increment 3a PROBE (D-135): THE COLD START — the first hour begins in TRUE wilderness.
//
// Increments 1-2 cheated: the backdrop terrain was dressed from the y6 snapshot (already-lived
// world). Condition A's first hour starts at GENESIS. This probe removes the cheat:
//   1. GENESIS EXPORT — one Jint shot with the SAME engine source the live driver runs: createWorld
//      (seed 8919), export tick 0 WITH tiles (node-exporter schema; tileTypes = t[0] chars, tileN)
//      -> Assets/Emergence/WorldStates/seq-8919-y000-genesis.json. Self-consistent by construction
//      (the y006 seq file came from an older exporter era: tick 864 => YEAR 144 vs live YEAR 216 —
//      logged; the cold start must not inherit that skew).
//   2. WorldDresser.Build(genesis) — the dressed world must be HONEST wilderness: 0 huts, 0 roads,
//      no lived-in wear. Verified from the export itself (huts/fields/villages empty).
//   3. LIVE from genesis (Fas3SimDriver) -> y2: souls appear, the gaze finds the first one, time
//      controls attach. The first hour's opening beat, mechanically real.
// Menu: Emergence/Fas3/RUN COLD START PROBE.  Headless: drop Reports/RUN_FAS3COLD.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using Jint;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas3ColdStartProbe
    {
        const long Seed = 8919;
        const int TargetYear = 2;
        const double Watchdog = 180.0;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3COLD.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3COLD_DONE.txt");
        const string Report   = "Reports/fas3-coldstart.txt";
        const string KeyPending = "emg.fas3cold.pending", KeyStart = "emg.fas3cold.start", KeyReport = "emg.fas3cold.report";

        // genesis export: ExportJs parity PLUS real tiles (the dresser needs them at build time)
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

        static int _frames, _phase, _magenta = -1, _magentaTone = -1, _snapshots, _lastAgentCount = -1, _liveAgentCount = -1;
        static Fas3SimDriver _driver;
        static AgentReconciler _agents;
        static HutReconciler _huts;
        static Fas3GazeDirector _gaze;
        static string _gazeNote = "";
        static bool _evidenceDone;
        static float _gazeCheckAt = -1f;

        static Fas3ColdStartProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN COLD START PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 3 COLD-START PROBE (D-135): the first hour begins at genesis");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // 1) genesis export from the SAME engine source the live driver runs
            string engineDir = Path.Combine(Application.dataPath, "Emergence", "Engine");
            var engineSrc = File.ReadAllText(EmergenceJintHost.EngineSourcePath(engineDir));
            var preludeSrc = File.ReadAllText(Path.Combine(engineDir, "harness", "prelude-hypot.js"));
            var host = new EmergenceJintHost(engineSrc, preludeSrc);
            host.Engine.Execute($"var __seed={Seed}; var __S=Emergence.createWorld({Seed}); __S.silent=true;");
            string json = host.Engine.Evaluate(GenesisExportJs).AsString();
            File.WriteAllText(GenesisPath, json);
            AssetDatabase.Refresh();
            var G = JsonUtility.FromJson<WorldState>(json);
            int yearTicks = (int)host.Engine.Evaluate("Emergence.YEAR").AsNumber();
            sb.AppendLine($"genesis export: engine {G.engineVersion}, YEAR={yearTicks} ticks, {G.W}x{G.H}, tiles={G.tileTypes?.Length ?? 0}, " +
                          $"souls={G.agents?.Length ?? 0}, huts={G.huts?.Length ?? 0}, fields={G.fields?.Length ?? 0}, villages={G.villages?.Length ?? 0}");
            bool wildernessHonest = (G.huts?.Length ?? 0) == 0 && (G.fields?.Length ?? 0) == 0 && (G.villages?.Length ?? 0) == 0
                                     && (G.tileTypes?.Length ?? 0) == G.W * G.H;
            sb.AppendLine($"honest wilderness: {(wildernessHonest ? "YES — no huts/fields/villages, full tile grid" : "NO (FAIL)")}");
            sb.AppendLine("(note: seq-y006 backdrop era exported YEAR=144; this genesis comes from the LIVE engine, YEAR=" + yearTicks + " — no skew)");

            // 2) dress the world from GENESIS — no lived-in cheat left
            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[Fas3Cold] look: " + e.Message); }
            sb.AppendLine("dressed from genesis; born-from-state layers retired (they must GROW)");
            sb.AppendLine(wildernessHonest ? "" : "verdict: CHECK — genesis was not empty");

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _magenta = _magentaTone = -1; _snapshots = 0; _lastAgentCount = -1; _liveAgentCount = -1;
            _gazeNote = ""; _driver = null; _gaze = null; _evidenceDone = false; _gazeCheckAt = -1f;
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive(float t)
        {
            if (_phase == 0)
            {
                _agents = new AgentReconciler();
                _huts = new HutReconciler();
                var go = new GameObject("Fas3SimDriver");
                _driver = go.AddComponent<Fas3SimDriver>();
                _driver.seed = Seed; _driver.ticksPerSecond = Fas3TimeControls.MaxTps; _driver.targetYear = TargetYear;
                var ui = new GameObject("Fas3TimeControls");
                ui.AddComponent<Fas3TimeControls>().driver = _driver;
                _gaze = Camera.main != null ? Camera.main.GetComponent<Fas3GazeDirector>() : null;
                _phase = 1;
                return;
            }

            if (_driver != null)
            {
                var json = _driver.TakeYearSnapshot();
                if (json != null)
                {
                    var S = JsonUtility.FromJson<WorldState>(json);
                    _agents.Reconcile(S, false);
                    _huts.Reconcile(S);
                    _lastAgentCount = S.agents?.Length ?? -1;
                    _liveAgentCount = _agents.Count;
                    _snapshots++;
                }
                if (_driver.LastError.Length > 0) { SafeFail("driver: " + _driver.LastError); return; }
            }

            if (_gaze != null && _gaze.HasTarget && _gazeCheckAt < 0f) _gazeCheckAt = t + 1.2f;
            if (_gaze != null && _gazeCheckAt > 0f && t >= _gazeCheckAt && _gazeNote.Length == 0)
            {
                var cam = Camera.main;
                if (_gaze.HasTarget && cam != null)
                {
                    float ang = Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position);
                    _gazeNote = $"gaze: \"{_gaze.TargetLabel}\" angle-to-target {ang:F1}° -> {(ang < 15f ? "AIMED (OK)" : "OFF (FAIL)")}";
                    if (!_evidenceDone) { CaptureEvidence("fas3-cold-firstsoul"); _evidenceDone = true; }
                }
                else _gazeCheckAt = -1f;
            }

            if (_phase == 1 && _driver.Finished)
            {
                if (!_evidenceDone) { CaptureEvidence("fas3-cold-firstsoul"); _evidenceDone = true; }
                UnityEngine.Object.Destroy(_driver.gameObject);
                _driver = null;
                _phase = 99;
            }
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
                sb.AppendLine($"year snapshots applied live from genesis: {_snapshots} (target y{TargetYear})");
                bool agentsOk = _liveAgentCount >= 0 && _liveAgentCount == _lastAgentCount;
                sb.AppendLine($"souls live: {_liveAgentCount} == snapshot {_lastAgentCount} -> {(agentsOk ? "MATCH (OK)" : "MISMATCH (FAIL)")}");
                sb.AppendLine(_gazeNote.Length > 0 ? _gazeNote : "gaze: NEVER TOOK A TARGET (FAIL)");
                sb.AppendLine($"magenta: classic={_magenta} tonemapped={_magentaTone}   evidence: 45-UNITY/evidence/fas3/fas3-cold-firstsoul.png");
                bool wildOk = SessionState.GetString(KeyReport, "").Contains("honest wilderness: YES");
                bool gazeOk = _gazeNote.Contains("AIMED");
                bool green = wildOk && agentsOk && gazeOk && _snapshots >= TargetYear && _magenta == 0 && _magentaTone == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the first hour begins in true wilderness; the world grows from nothing under the player's eye"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} wilderness={wildOk} souls={_liveAgentCount}/{_lastAgentCount} " +
                                        $"gaze={gazeOk} snapshots={_snapshots} magenta={_magenta}/{_magentaTone}\nsee {Report}\n");
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
