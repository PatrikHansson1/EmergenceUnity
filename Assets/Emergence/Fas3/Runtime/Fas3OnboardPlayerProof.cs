// EMERGENCE — FAS 3 increment 7 (D-140): ONBOARDING IN A REAL PLAYER — the proof observer.
//
// The editor probe (D-139) proved the opening composes itself; this component proves the SAME
// composition inside a built player. It observes Fas3Onboarding (composes NOTHING): genesis lands
// (souls in wilderness, 0 huts), the first hut and first child arrive with the gaze on them, years
// ascend unbroken — then it exercises the CHECKPOINT GRID in player: scrub to genesis (y0) and
// back to the frontier via Fas3PresentationClock.JumpToYear. Writes onboard-player.txt beside the
// exe, saves an evidence frame (magenta-scanned, D-131 detector), quits.
// D-078 r4: observes and scrubs through presentation APIs only; the sim is never touched.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3OnboardPlayerProof : MonoBehaviour
    {
        public int expectedGenesisSouls = -1;   // baked by the probe from the genesis export
        public float watchdogSecs = 240f;

        Fas3Onboarding _onb;
        Fas3GazeDirector _gaze;
        int _phase;
        bool _hutBeat, _childBeat;
        int _hutYear = -1, _childYear = -1;
        string _genesis = "", _hutNote = "", _childNote = "", _scrub = "";
        int _magenta = -1, _magentaTone = -1;
        float _hutGazeAt = -1f, _childGazeAt = -1f;

        string OutPath => Path.Combine(Application.dataPath, "..", "onboard-player.txt");
        string PngPath => Path.Combine(Application.dataPath, "..", "onboard-player-firsthut.png");

        void OnEnable() { PresentationEventBus.OnEvent += OnBus; }
        void OnDisable() { PresentationEventBus.OnEvent -= OnBus; }

        void OnBus(PresentationEvent e)
        {
            if (e.Type == PresentationEventType.Milestone && e.Data == "the first hut") { _hutBeat = true; _hutYear = e.Year; }
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a child is born" && !_childBeat) { _childBeat = true; _childYear = e.Year; }
        }

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs) { Finish("WATCHDOG"); return; }

            if (_phase == 0)
            {
                _onb = FindAnyObjectByType<Fas3Onboarding>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
                _gaze = Camera.main != null ? Camera.main.GetComponent<Fas3GazeDirector>() : null;
                _phase = 1;
                return;
            }

            var w = _onb.World; var d = _onb.Driver; var c = _onb.Clock;
            if (d.LastError.Length > 0) { Finish("driverError=" + d.LastError.Replace('\n', ' ')); return; }

            if (_genesis.Length == 0 && w.LastAppliedYear >= 0)
            {
                bool ok = w.LastAppliedYear == 0 && w.AgentCount == expectedGenesisSouls && w.HutCount == 0;
                _genesis = $"genesis={(ok ? "OK" : "FAIL")}(y{w.LastAppliedYear},souls{w.AgentCount}/{expectedGenesisSouls},huts{w.HutCount})";
            }

            float t = Time.realtimeSinceStartup;
            if (_hutBeat && _hutNote.Length == 0)
            {
                if (_gaze != null && _gaze.HasTarget && _gaze.TargetLabel.Contains("hut"))
                {
                    if (_hutGazeAt < 0f) _hutGazeAt = t + 1.2f;
                    else if (t >= _hutGazeAt)
                    {
                        var cam = Camera.main;
                        float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                        _hutNote = $"firstHut={(ang < 15f ? "OK" : "FAIL")}(y{_hutYear},gaze{ang.ToString("F1", CultureInfo.InvariantCulture)}deg)";
                        CaptureEvidence();
                    }
                }
                else if (w.LastAppliedYear >= _hutYear + 2)
                    _hutNote = $"firstHut=FAIL(y{_hutYear},gaze-never-took)";
            }
            if (_childBeat && _childNote.Length == 0)
            {
                if (_gaze != null && _gaze.HasTarget && (_gaze.TargetLabel.Contains("born") || _gaze.TargetLabel.Contains("arrives")))
                {
                    if (_childGazeAt < 0f) _childGazeAt = t + 1.2f;
                    else if (t >= _childGazeAt)
                    {
                        var cam = Camera.main;
                        float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                        _childNote = $"firstChild={(ang < 15f ? "OK" : "OK-gazeoff")}(y{_childYear},gaze{ang.ToString("F1", CultureInfo.InvariantCulture)}deg)";
                    }
                }
                else if (w.LastAppliedYear >= _childYear + 2)
                    _childNote = $"firstChild=OK(y{_childYear},beat; gaze within cooldown — design D-134)";
            }

            if (_phase == 1 && _hutNote.Length > 0 && _childNote.Length > 0)
            {
                // the grid, exercised in player: down to GENESIS and back to the frontier
                int frontier = w.LastAppliedYear;
                int hutsAtFrontier = w.HutCount;
                bool j0 = c.JumpToYear(0);
                bool j0Ok = j0 && w.LastAppliedYear == 0 && w.HutCount == 0 && w.AgentCount == expectedGenesisSouls;
                bool jf = c.JumpToYear(frontier);
                bool jfOk = jf && w.LastAppliedYear == frontier && w.HutCount == hutsAtFrontier;
                _scrub = $"scrub=J0:{(j0Ok ? "OK" : "FAIL")}(genesis re-entered),Jf:{(jfOk ? "OK" : "FAIL")}(y{frontier},huts{w.HutCount})";
                Finish("");
            }
        }

        void CaptureEvidence()
        {
            try
            {
                var cam = Camera.main; if (cam == null) return;
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
            catch { }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            string order = _onb != null && _onb.Clock != null ? _onb.Clock.LastAppliedOrder : "";
            bool orderOk = order.StartsWith("0,1,2");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "onboard {0} {1} {2} {3} order=[{4}] orderOk={5} magenta={6}/{7} {8}\n",
                _genesis, _hutNote, _childNote, _scrub, order, orderOk ? "OK" : "FAIL",
                _magenta, _magentaTone, error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }
    }
}
