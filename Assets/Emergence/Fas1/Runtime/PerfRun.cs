// EMERGENCE — A6 FINAL VERDICT instrument (D-130): the in-player FPS meter.
// Placed in the saved PerfScene by A6PlayerPerf. In the PLAYER it: kills vsync/frame caps, warms up
// 5 s, samples unscaled frame times for 20 s while slowly yawing the camera (village -> meadow sweep,
// a fair average view), writes the histogram report next to the exe, and quits. In the EDITOR it is
// inert (the editor probes own that measurement). Presentation-only; the sim never runs here — the
// scene is a frozen, fully dressed y120 world (the render load is what A6 budgets, D-107).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class PerfRun : MonoBehaviour
    {
        public float warmupSeconds = 5f;
        public float sampleSeconds = 20f;
        public float yawDegPerSec = 14f;

        readonly List<float> _ms = new List<float>(4096);
        float _t;
        bool _done;

        void Start()
        {
            if (Application.isEditor) { enabled = false; return; }
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;   // unattended machine: keep rendering without focus
        }

        void Update()
        {
            if (_done) return;
            var cam = Camera.main;
            if (cam != null) cam.transform.Rotate(0f, yawDegPerSec * Time.unscaledDeltaTime, 0f, Space.World);

            _t += Time.unscaledDeltaTime;
            if (_t < warmupSeconds) return;
            if (_t < warmupSeconds + sampleSeconds) { _ms.Add(Time.unscaledDeltaTime * 1000f); return; }

            _done = true;
            try { Write(); } finally { Application.Quit(0); }
        }

        void Write()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — PLAYER PERF RUN (A6 final verdict, D-130)");
            sb.AppendLine($"generated {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"resolution {Screen.width}x{Screen.height}  vsync=0  frames={_ms.Count}  sample={sampleSeconds:0}s (5s warm-up)");
            if (_ms.Count > 10)
            {
                var sorted = _ms.OrderBy(x => x).ToList();
                float sum = _ms.Sum();
                float avgMs = sum / _ms.Count;
                float med = sorted[sorted.Count / 2];
                float p95 = sorted[(int)(sorted.Count * 0.95f)];
                float p99 = sorted[(int)Mathf.Min(sorted.Count - 1, sorted.Count * 0.99f)];
                sb.AppendLine($"frame ms   avg={avgMs:0.00}  median={med:0.00}  p95={p95:0.00}  p99={p99:0.00}  worst={sorted[^1]:0.00}");
                sb.AppendLine($"fps        avg={1000f / avgMs:0.0}  median={1000f / med:0.0}  1%low={1000f / p99:0.0}");
            }
            else sb.AppendLine("TOO FEW SAMPLES");
            string path = Path.Combine(Application.dataPath, "..", "perf-player.txt");
            File.WriteAllText(path, sb.ToString());
        }
    }
}
