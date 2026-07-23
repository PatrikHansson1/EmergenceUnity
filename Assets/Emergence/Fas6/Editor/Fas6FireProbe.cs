// EMERGENCE — FAS 6 PROBE: the LIVING FIRE LAYER — the eye catches up with the ear.
//
// Boots the same self-composing opening and asserts FireReconciler through Fas3WorldRuntime:
//   1. genesis wilderness: zero live fires/smoke @y0;
//   2. FIXTURE (D-131/D-152 school — fires are transient, 0 in every y0..y55/y120 export on the
//      probe seeds): world-4242-y120-dusk (REAL engine export: 1 fire, 16 huts) through the SAME
//      Apply path — fire body at the mapped position, firelight carrying the LOCKED warm-point
//      identity (D-114/115b, dresser values verbatim);
//   3. chimney smoke: (a) the near-rule PURE at its boundary (3 tiles in, out beyond), (b) the
//      materialization via a MECHANISM fixture — the same real export with ONE hut moved adjacent
//      to the fire, declared openly: NO existing export carries a hut within 3 tiles of a burning
//      fire, so the branch has no live/raw-fixture window at all (the D-131 Fas2GateProof school);
//   4. evidence PNGs (two angles, raycast-picked against occlusion — the D-131 canopy lesson),
//      blankness-guarded AND humanly looked at;
//   5. onLoss: a fireless real export (seq-8919-y055) through the same path extinguishes the layer;
//   6. clock honesty: tps untouched through pause/apply/resume.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// Menu: Emergence/Fas6/RUN FIRE LAYER PROBE.  Headless: drop Reports/RUN_FAS6FIRE.trigger.
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
    public static class Fas6FireProbe
    {
        const long Seed = 8919;
        const double Watchdog = 420.0;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS6FIRE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS6FIRE_DONE.txt");
        const string Report   = "Reports/fas6-fire.txt";
        const string PngA     = "Reports/fas6-fire-a.png";
        const string PngB     = "Reports/fas6-fire-b.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixtureFire = "Assets/Emergence/WorldStates/world-4242-y120-dusk.json";   // real export: 1 fire + 16 huts
        const string FixtureCold = "Assets/Emergence/WorldStates/seq-8919-y055.json";          // real export: 0 fires
        const string KeyPending = "emg.fas6fire.pending", KeyStart = "emg.fas6fire.start", KeyReport = "emg.fas6fire.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static WorldState _fix;
        static Vector3 _firePos;
        static float _tpsBefore, _grabAskedAt;
        static int _firesAtCheck = -1, _smokeAtCheck = -1, _firesAfterLoss = -1;   // measurement-time stamps (R1 law)
        static string _n1 = "", _n2 = "", _n3 = "", _n4a = "", _n4b = "", _n5 = "", _n6 = "";

        static Fas6FireProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/RUN FIRE LAYER PROBE")]
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
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 6 PROBE: the living fire layer — the eye catches up with the ear");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = applied WorldState fires/huts (read-only); grammar = WorldDresser.PlaceFires verbatim (TD-025/D-114/115b)");
            sb.AppendLine();

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _onb = null; _fix = null; _firePos = Vector3.zero;
            _tpsBefore = 0f; _grabAskedAt = 0f;
            _firesAtCheck = -1; _smokeAtCheck = -1; _firesAfterLoss = -1;
            _n1 = _n2 = _n3 = _n4a = _n4b = _n5 = _n6 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
                var S = _onb.World.LastState; if (S == null) return;

                int fires = S.fires != null ? S.fires.Length : 0;
                bool silent = fires == 0 && _onb.World.FireCount == 0 && _onb.World.SmokeCount == 0;
                _n1 = $"genesis: state fires={fires}, live layer fires={_onb.World.FireCount} smoke={_onb.World.SmokeCount} @y{_onb.World.LastAppliedYear} ({(silent ? "OK" : "FAIL")})";

                _tpsBefore = _onb.Clock.ticksPerSecond;
                _onb.Clock.paused = true;   // freeze the live stream; fixtures go through the same Apply path
                _fix = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureFire));
                // G-review r1 I2: injection is reconstruction, not witnessed history — chronicle stays silent
                Fas3WorldRuntime.FixtureInjection = true;
                try { _onb.World.Apply(_fix); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // next frame: the fire body stands (raw real export — no huts near this fire)
            {
                int fxFires = _fix.fires != null ? _fix.fires.Length : 0;
                _firesAtCheck = w.FireCount;                                   // stamped at the fixture measurement
                bool countOk = _firesAtCheck == fxFires && fxFires > 0;
                bool smokeRaw = w.SmokeCount == 0;                             // honest: the raw export has none in range

                bool lightOk = false;
                var layer = GameObject.Find("Fires_Live");
                if (layer != null && layer.transform.childCount > 0)
                {
                    var fire = layer.transform.GetChild(0);
                    _firePos = fire.position;
                    var light = fire.GetComponentInChildren<Light>();
                    lightOk = light != null && light.type == LightType.Point
                        && Mathf.Approximately(light.intensity, FireReconciler.FirelightIntensity)
                        && Mathf.Approximately(light.range, FireReconciler.FirelightRange)
                        && (light.color - FireReconciler.FirelightColor).maxColorComponent < 0.01f;
                }
                bool posOk = false;
                if (fxFires > 0)
                {
                    var f = _fix.fires[0];
                    var expect = new Vector3(f.x * 8f, 0, (_fix.H - 1 - f.y) * 8f);
                    var t = Terrain.activeTerrain;
                    if (t != null) expect.y = t.SampleHeight(expect) + t.transform.position.y;
                    expect += Vector3.up * 0.1f;
                    posOk = (_firePos - expect).magnitude < 0.05f;
                }
                _n2 = $"fixture 4242-y120-dusk (riktig motor-export, samma Apply-väg): live fires {_firesAtCheck}=={fxFires}, warm point verbatim (D-114/115b)={lightOk}, pos match={posOk}, smoke on raw export=0 ({(countOk && lightOk && posOk && smokeRaw ? "OK" : "FAIL")})";

                // smoke: pure boundary rule + MECHANISM fixture (one hut moved adjacent — declared)
                var f0 = _fix.fires[0];
                bool ruleIn  = FireReconciler.NearAnyFire(_fix.fires, f0.x + FireReconciler.SmokeNearFireTiles, f0.y);
                bool ruleOut = !FireReconciler.NearAnyFire(_fix.fires, f0.x + FireReconciler.SmokeNearFireTiles + 0.5f, f0.y);
                var staged = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureFire));
                staged.huts[0].x = f0.x + 1f; staged.huts[0].y = f0.y;         // ONE hut moved adjacent, openly
                Fas3WorldRuntime.FixtureInjection = true;   // I2: injection, chronicle silent
                try { w.Apply(staged); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _n3 = $"smoke rule pure at boundary: in@3={ruleIn} out@3.5={ruleOut} ({(ruleIn && ruleOut ? "OK" : "FAIL")})";
                _phase = 2;
                return;
            }

            if (_phase == 2)   // next frame: smoke materialized on the staged mechanism fixture
            {
                int want = 0;
                var st = w.LastState;
                if (st.huts != null) foreach (var h in st.huts) if (FireReconciler.NearAnyFire(st.fires, h.x, h.y)) want++;
                _smokeAtCheck = w.SmokeCount;                                  // stamped at the staged measurement
                bool ok = _smokeAtCheck == want && want > 0 && w.FireCount == 1;
                _n3 += $" · mekanism-fixtur (EN hydda flyttad intill elden — ingen liggande export bär hydda inom {FireReconciler.SmokeNearFireTiles} tiles av brinnande eld): smoke {_smokeAtCheck}=={want} (>0), fires still 1 ({(ok ? "OK" : "FAIL")})";

                // evidence angle A: raycast-picked against occlusion (the D-131 canopy lesson)
                PlaceCamera(PickAngle());
                var g = new GameObject("Fas6FireGrabberA").AddComponent<Fas4NativeGrabber>();
                g.Path = PngA; g.OnGrabbed = note => { _n4a = "evidence A " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // wait grab A -> steep look-down angle B
            {
                if (_n4a.Length == 0 && Time.unscaledTime - _grabAskedAt < 10f) return;
                var cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.position = _firePos + new Vector3(2.5f, 9f, 2.5f);   // near-overhead: canopy gaps
                    cam.transform.LookAt(_firePos + Vector3.up * 0.6f);
                }
                var g = new GameObject("Fas6FireGrabberB").AddComponent<Fas4NativeGrabber>();
                g.Path = PngB; g.OnGrabbed = note => { _n4b = "evidence B " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // wait grab B -> extinguish via a fireless REAL export
            {
                if (_n4b.Length == 0 && Time.unscaledTime - _grabAskedAt < 10f) return;
                var cold = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureCold));
                Fas3WorldRuntime.FixtureInjection = true;   // I2: injection, chronicle silent
                try { w.Apply(cold); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _phase = 5;
                return;
            }

            if (_phase == 5)   // next frame: the layer is dismantled
            {
                _firesAfterLoss = w.FireCount;                                 // stamped at the loss measurement
                bool goneOk = _firesAfterLoss == 0 && w.SmokeCount == 0;
                _n5 = $"onLoss (seq-8919-y055, 0 fires, samma Apply-väg): live fires {_firesAfterLoss}, smoke {w.SmokeCount} ({(goneOk ? "OK" : "FAIL")})";

                c.paused = false;
                _phase = 6;
                return;
            }

            if (_phase == 6)   // resume: the layer never touched the clock
            {
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n6 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — the fire layer never touches the clock";
                _phase = 99;
            }
        }

        /// <summary>Pick the first of eight camera candidates (4 compass x 2 elevations) with an
        /// unoccluded ray to the fire; fall back to the last candidate. The D-131 canopy lesson,
        /// mechanized.</summary>
        static Vector3 PickAngle()
        {
            var target = _firePos + Vector3.up * 1.0f;
            Vector3 pick = _firePos + new Vector3(5f, 7f, 5f);
            foreach (var d in new[] { new Vector3(1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,1), new Vector3(-1,0,-1) })
                foreach (var h in new[] { 3.5f, 7f })
                {
                    var cand = _firePos + d.normalized * 6.5f + Vector3.up * h;
                    if (!Physics.Linecast(cand, target)) return cand;
                    pick = cand;
                }
            return pick;
        }

        static void PlaceCamera(Vector3 pos)
        {
            var cam = Camera.main; if (cam == null) return;
            cam.transform.position = pos;
            cam.transform.LookAt(_firePos + Vector3.up * 1.0f);
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4a, _n4b, _n5, _n6 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("fixture honesty: fire/onLoss proven with RAW real exports through the SAME Fas3WorldRuntime.Apply path; the smoke");
                sb.AppendLine("materialization uses a MECHANISM fixture (same export, ONE hut moved adjacent — declared above) because no existing");
                sb.AppendLine("export carries a hut within range of a burning fire (D-131 Fas2GateProof school). Grammar mirrors WorldDresser.PlaceFires");
                sb.AppendLine("verbatim; look/taste of VFX = the human eye pass. PNG guard proves non-blankness only — the fire must be SEEN by a human.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)") && !_n3.Contains("FAIL")
                    && (_n4a.Contains("OK") || _n4b.Contains("OK")) && _n5.Contains("(OK)") && _n6.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — fires now have bodies in the living loop: materialized, warm, smoking, dismantled, the clock untouched"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} firesAtCheck={_firesAtCheck} smokeAtCheck={_smokeAtCheck} firesAfterLoss={_firesAfterLoss}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
