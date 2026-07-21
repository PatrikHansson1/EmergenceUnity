// EMERGENCE — FAS 3 increment 5 (D-138): the WATCH LOOP IN A REAL PLAYER — the proof component.
//
// Increment 4 made every WATCH piece player-clean (runtime reconcilers + catalog + buffer driver +
// presentation clock). This component runs INSIDE the built player and proves the loop end-to-end
// on the dressed core scene: the village is BORN live (huts == canon), the player's hand works
// (pause decouples — the producer races on), years apply strictly in order, and scrub re-enters
// produced years from the checkpoint grid. It writes watch-player.txt beside the exe (the probe
// polls it), saves a RenderTexture evidence frame (watch-player-y6.png) with the same magenta
// detector the probes use (D-131 classic + tonemapped), then quits.
//
// D-078 r4: reads state, writes nothing back. All pacing is presentation-side (the clock).
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3PlayerWatch : MonoBehaviour
    {
        public long seed = 8919;
        public int targetYear = 6;
        public int expectedFinalHuts = -1;   // baked by the probe from the backdrop canon
        public float watchdogSecs = 240f;

        Fas3SimDriver _driver;
        Fas3WorldRuntime _world;
        Fas3PresentationClock _clock;
        Fas3TimeControls _controls;
        int _phase;
        float _pauseStart = -1f;
        int _pausedAppliedYear = -1, _producedAtPause = -1, _producedUnderPause = -1;
        string _decouple = "", _scrub = "";
        int _magenta = -1, _magentaTone = -1;

        string OutPath => Path.Combine(Application.dataPath, "..", "watch-player.txt");
        string PngPath => Path.Combine(Application.dataPath, "..", "watch-player-y6.png");

        void Start()
        {
            Application.runInBackground = true;
            _world = new GameObject("Fas3WorldRuntime").AddComponent<Fas3WorldRuntime>();
            var dgo = new GameObject("Fas3SimDriver");
            _driver = dgo.AddComponent<Fas3SimDriver>();
            _driver.seed = seed; _driver.bufferMode = true; _driver.targetYear = targetYear; _driver.lookaheadYears = 16;
            var cgo = new GameObject("Fas3PresentationClock");
            _clock = cgo.AddComponent<Fas3PresentationClock>();
            _clock.driver = _driver; _clock.world = _world;
            var ugo = new GameObject("Fas3TimeControls");
            _controls = ugo.AddComponent<Fas3TimeControls>();
            _controls.driver = _driver; _controls.clock = _clock;
            _controls.SetSpeed(2);   // 4× — consume the lookahead as it forms; producer-bound anyway
        }

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs) { Finish("WATCHDOG"); return; }
            if (_driver == null) return;
            if (_driver.LastError.Length > 0) { Finish("driverError=" + _driver.LastError.Replace('\n', ' ')); return; }

            switch (_phase)
            {
                case 0: // first year applied -> pause, prove the decoupling
                    if (_world.LastAppliedYear >= 1)
                    {
                        _pausedAppliedYear = _world.LastAppliedYear;
                        _producedAtPause = _driver.Year;
                        _controls.SetPause(true);
                        _pauseStart = Time.realtimeSinceStartup;
                        _phase = 1;
                    }
                    break;

                case 1: // producer must keep racing under pause
                    if (_world.LastAppliedYear != _pausedAppliedYear)
                    { _decouple = "decouple=FAIL(applied-moved)"; _controls.SetSpeed(2); _phase = 2; break; }
                    if (_driver.Year >= _producedAtPause + 2 || _driver.Finished)
                    {
                        _producedUnderPause = _driver.Year - _producedAtPause;
                        _decouple = $"decouple={(_producedUnderPause >= 2 ? "OK" : "FAIL")}(+{_producedUnderPause}y,buffer+{_driver.BufferedYears})";
                        _controls.SetSpeed(2);
                        _phase = 2;
                    }
                    else if (Time.realtimeSinceStartup - _pauseStart > 90f)
                    { _decouple = "decouple=FAIL(producer-stalled)"; _controls.SetSpeed(2); _phase = 2; }
                    break;

                case 2: // consume to the end, then scrub J3/J6 and finish
                    if (_driver.Finished && _world.LastAppliedYear >= targetYear)
                    {
                        int hutsAtEnd = _world.HutCount;
                        bool j3 = _clock.JumpToYear(3);
                        int hutsJ3 = _world.HutCount;
                        bool j6 = _clock.JumpToYear(targetYear);
                        bool j6Ok = j6 && _world.HutCount == hutsAtEnd;
                        _scrub = $"scrub=J3:{(j3 ? "OK" : "FAIL")}(huts={hutsJ3}),J6:{(j6Ok ? "OK" : "FAIL")}(huts={_world.HutCount})";
                        CaptureEvidence();
                        Finish("");
                    }
                    break;
            }
        }

        void CaptureEvidence()
        {
            try
            {
                var cam = Camera.main; if (cam == null) return;
                // frame the village from the north, eye height — same grammar as the editor probes
                var layer = GameObject.Find(HutReconciler.LayerName);
                var c = Vector3.zero; int n = 0;
                if (layer != null) foreach (Transform h in layer.transform) { c += h.position; n++; }
                if (n > 0)
                {
                    c /= n;
                    var pos = c + Vector3.back * 22f;
                    var t = Terrain.activeTerrain;
                    if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y;
                    cam.transform.position = pos + Vector3.up * 7f;
                    cam.transform.LookAt(c + Vector3.up * 1.2f);
                }
                bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
                const int pw = 1600, ph = 900;
                var rt = new RenderTexture(pw, ph, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                RenderSettings.fog = fogWas;
                var px = tex.GetPixels32(); int mag = 0, tone = 0;
                foreach (var p in px)
                {
                    if (p.r > 220 && p.b > 220 && p.g < 80) mag++;
                    else if (Math.Abs(p.r - p.b) < 15 && p.r > 170 && p.g < p.r - 90) tone++;
                }
                _magenta = mag; _magentaTone = tone;
                File.WriteAllBytes(PngPath, tex.EncodeToPNG());
                Destroy(tex); Destroy(rt);
            }
            catch (Exception e) { _scrub += " evidence-failed:" + e.Message.Replace('\n', ' '); }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            bool hutsOk = expectedFinalHuts < 0 || _world.HutCount == expectedFinalHuts;
            string order = _clock != null ? _clock.LastAppliedOrder : "";
            bool orderOk = order.StartsWith("1,2,3,4,5,6");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "watch huts={0} expected={1} hutsOk={2} order=[{3}] orderOk={4} {5} {6} agents={7} magenta={8}/{9} year={10} tick={11} {12}\n",
                _world != null ? _world.HutCount : -1, expectedFinalHuts, hutsOk ? "OK" : "FAIL",
                order, orderOk ? "OK" : "FAIL", _decouple, _scrub,
                _world != null ? _world.AgentCount : -1, _magenta, _magentaTone,
                _driver != null ? _driver.Year : -1, _driver != null ? _driver.Tick : -1,
                error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }
    }
}
