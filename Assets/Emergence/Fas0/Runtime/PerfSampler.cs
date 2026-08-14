// EMERGENCE — Fas 0 (D-107 A6): runtime performance sampler.
//
// A6 sets the performance/scale budget NOW so the Codex fill-pass + reconciler are built against it;
// Fas 1 measures for real on a representative village and calibrates the numbers. This component is
// the runtime half: attach it to a world root and it samples frame time + a smoothed FPS + the count
// of active renderers/agents, exposing a snapshot other systems (and the editor harness) can read.
// It only READS — it never influences the sim (D-078 r4).
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class PerfSampler : MonoBehaviour
    {
        [Header("Provisional budget (A6 — calibrated for real in Fas 1)")]
        public int TargetFps = 60;               // GTX 1660-class floor; 4070 Ti SUPER reference
        public int MaxVisibleAgents = 150;        // agents animated on-screen at once (cull the rest)
        public int MaxVisibleBuildings = 400;

        public float Fps { get; private set; }
        public float FrameMs { get; private set; }
        public bool WithinFrameBudget => FrameMs <= 1000f / Mathf.Max(1, TargetFps);

        float _emaMs = 16.6f;

        void Update()
        {
            // unscaled so time-control (pause/speed, Fas 3) never distorts the perf read
            float dtMs = Time.unscaledDeltaTime * 1000f;
            _emaMs = Mathf.Lerp(_emaMs, dtMs, 0.05f);
            FrameMs = _emaMs;
            Fps = _emaMs > 0.0001f ? 1000f / _emaMs : 0f;
        }

        /// <summary>Cheap on-demand scene census (active renderers as a draw-call proxy).</summary>
        public int CountActiveRenderers()
            => FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Length;

        void OnGUI()
        {
            // D-218: a frame counter in a Steam screenshot is worth real wishlists. The old guard
            // was "debug build or editor", which means it was on in every editor capture we have
            // ever taken — including the ones we would have sent to a store page.
            if (!EmergenceUI.Diagnostics) return;
            var s = $"FPS {Fps:0} ({FrameMs:0.0} ms) budget {TargetFps}  {(WithinFrameBudget ? "OK" : "OVER")}";
            EmergenceUI.Begin();
            GUI.Label(new Rect(EmergenceUI.Sp6, EmergenceUI.H - 48, 360, 20), s, EmergenceUI.Meta);
            EmergenceUI.End();
        }
    }
}
