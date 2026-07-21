// EMERGENCE — FAS 3 increment 3b (D-136): PLAYER-JINT CADENCE — the number that decides the
// time strategy. The editor measured 16 t/s flat-out (D-134, incl. live reconciles); whether the
// EA window can run REAL-TIME at 1x (24 t/s) or 4x (96 t/s) without a checkpoint+resimulate
// strategy depends on what a real player build delivers. This component runs in a minimal player
// scene (built by Fas3PlayerCadenceProbe): boots the live driver flat-out from genesis, warms up,
// samples a fixed wall-clock window, writes cadence-player.txt beside the exe, quits.
// Pure measurement — no reconcilers, no graphics needed (launched -batchmode -nographics).
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3PlayerCadence : MonoBehaviour
    {
        public long seed = 8919;
        public float warmupSecs = 4f;
        public float sampleSecs = 30f;

        Fas3SimDriver _driver;
        int _phase, _tickAtSample;
        float _t0 = -1f, _sampleStart;

        string OutPath => Path.Combine(Application.dataPath, "..", "cadence-player.txt");

        void Start()
        {
            Application.runInBackground = true;
            var go = new GameObject("Fas3SimDriver");
            _driver = go.AddComponent<Fas3SimDriver>();
            _driver.seed = seed;
            _driver.ticksPerSecond = 99999f;   // flat-out: the worker takes what Jint gives
            _driver.targetYear = -1;           // endless; we quit after the sample window
        }

        void Update()
        {
            if (_driver == null || _phase == 9) return;
            if (_driver.LastError.Length > 0)
            {
                Write(-1f, 0, 0f, "driverError=" + _driver.LastError.Replace('\n', ' '));
                _phase = 9; Application.Quit(); return;
            }
            _driver.TakeYearSnapshot();        // drain (parity with real presentation; worker never blocks on it)

            float t = Time.realtimeSinceStartup;
            if (_phase == 0 && _driver.Tick > 0) { _t0 = t; _phase = 1; }                      // engine alive
            else if (_phase == 1 && t - _t0 >= warmupSecs)
            { _tickAtSample = _driver.Tick; _sampleStart = t; _phase = 2; }
            else if (_phase == 2 && t - _sampleStart >= sampleSecs)
            {
                int ticks = _driver.Tick - _tickAtSample;
                float secs = t - _sampleStart;
                Write(ticks / secs, ticks, secs, $"totalTick={_driver.Tick} year={_driver.Year}");
                _phase = 9; Application.Quit();
            }
        }

        void Write(float tps, int ticks, float secs, string extra)
        {
            File.WriteAllText(OutPath, string.Format(CultureInfo.InvariantCulture,
                "tps avg={0:F1} ticks={1} secs={2:F1} yearTicks={3} warmup={4:F0} {5}\n",
                tps, ticks, secs, _driver != null ? _driver.YearTicks : -1, warmupSecs, extra));
        }
    }
}
