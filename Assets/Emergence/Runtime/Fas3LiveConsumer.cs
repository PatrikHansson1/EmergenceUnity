// EMERGENCE — Fas3 LIVE CONSUMER (D-897): the runtime that makes the built scene LIVE.
// Fas3PresentationClock gates consumption on a presentation tick capped at driver.Tick, which does not
// advance in a plain built scene (verified: bufferMode=true pinned at genesis, false applied nothing).
// The PROVEN live path (Fas3ColdStartProbe) instead pumps driver.TakeYearSnapshot() directly and calls
// world.Apply — every produced year, in order, drained at a documentary pace. This component does exactly
// that at runtime. READS snapshots only (D-078 r4); never writes into the sim. Motor untouched.
#if true
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3LiveConsumer : MonoBehaviour
    {
        public Fas3SimDriver driver;
        public Fas3WorldRuntime world;
        [Tooltip("Real seconds between applied sim-years — the documentary pace.")]
        public float secondsPerYear = 1.5f;
        public int AppliedYears { get; private set; }
        public string LastError { get; private set; } = "";
        float _acc;

        void Awake()
        {
            if (driver == null) driver = FindAnyObjectByType<Fas3SimDriver>();
            if (world == null) world = FindAnyObjectByType<Fas3WorldRuntime>();
        }

        void Update()
        {
            if (driver == null || world == null) return;
            if (driver.LastError != null && driver.LastError.Length > 0) { LastError = driver.LastError; return; }
            _acc += Time.unscaledDeltaTime;
            if (_acc < secondsPerYear) return;
            _acc = 0f;
            var json = driver.TakeYearSnapshot();
            if (string.IsNullOrEmpty(json)) return;
            var S = JsonUtility.FromJson<WorldState>(json);
            if (S != null) { world.Apply(S); AppliedYears++; }
        }
    }
}
#endif
