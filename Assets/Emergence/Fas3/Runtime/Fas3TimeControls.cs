// EMERGENCE — FAS 3 increment 2 (D-134): TIME CONTROLS — the player's hand on presentation time.
//
// TIME LAW (D-133, proven): these buttons touch ONLY presentation pacing — how many ticks the
// presentation may consume per real second. The sim's truth at tick T is pacing-independent by
// construction; pausing freezes the WATCH, never the world's logic.
//
// FAS 3 increment 4 (D-136/D-137): with the lookahead buffer, the buttons re-seat onto
// Fas3PresentationClock when one is present (the driver races flat-out underneath; pausing WIDENS
// the buffer). Without a clock they fall back to the increment-2 wiring (driver pacing) so every
// older probe runs verbatim. Same three speeds: 1× = 24 t/s (one sim year = 6 s at YEAR=144),
// 4× = 96, ▶▶ = uncapped consumption (buffer-bound in buffer mode, compute-bound otherwise).
// IMGUI overlay (no scene/canvas dependencies — works in any dressed world, evidence-friendly).
// Keys: Space = pause, 1/2/3 = 1×/4×/max (legacy Input, guarded like Fas3CameraRig).
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3TimeControls : MonoBehaviour
    {
        public const float BaseTps = 24f;   // 1× — one sim year = 6 s (YEAR = 144 ticks, D-135)
        public const float MaxTps = 999f;   // ▶▶ — takes what the buffer/Jint gives

        public Fas3SimDriver driver;          // auto-found if left empty
        public Fas3PresentationClock clock;   // auto-found; when present, the buttons steer THIS
        public float EffectiveTps { get; private set; }   // measured, not requested — the honest number
        public int SpeedIndex { get; private set; } = 1;  // 0=paused 1=1× 2=4× 3=max
        public int ScrubJumps { get; private set; }       // proof counter: timeline jumps performed

        double _lastTick; float _windowStart;
        int _scrubPending = -1; float _scrubShown = -1f;  // D-140: timeline drag state (jump on release)

        bool Paused => clock != null ? clock.paused : (Driver() != null && Driver().paused);

        public void SetPause(bool p)
        {
            if (Clock() != null) clock.paused = p;
            else { var d = Driver(); if (d == null) return; d.paused = p; }
            if (p) SpeedIndex = 0;
            else if (SpeedIndex == 0) SetSpeed(1);
        }

        /// <summary>1 = 1× (24 t/s), 2 = 4× (96 t/s), 3 = uncapped.</summary>
        public void SetSpeed(int idx)
        {
            float tps = idx == 3 ? MaxTps : idx == 2 ? BaseTps * 4f : BaseTps;
            if (Clock() != null) { clock.paused = false; clock.ticksPerSecond = tps; }
            else { var d = Driver(); if (d == null) return; d.paused = false; d.ticksPerSecond = tps; }
            SpeedIndex = Mathf.Clamp(idx, 1, 3);
        }

        Fas3SimDriver Driver()
        {
            if (driver == null) driver = FindAnyObjectByType<Fas3SimDriver>();
            return driver;
        }

        Fas3PresentationClock Clock()
        {
            if (clock == null) clock = FindAnyObjectByType<Fas3PresentationClock>();
            return clock;
        }

        void Update()
        {
            var d = Driver(); if (d == null) return;
            var c = Clock();
            // measured ticks/s over a 0.5 s window — what the machine actually delivers (presentation-side when clocked)
            double tick = c != null ? c.PresentationTick : d.Tick;
            float t = Time.unscaledTime;
            if (tick < _lastTick) { _windowStart = t; _lastTick = tick; EffectiveTps = 0f; }   // D-140: a scrub jumped time backwards — restart the window, never report a lie
            if (t - _windowStart >= 0.5f)
            {
                if (_windowStart > 0f) EffectiveTps = (float)((tick - _lastTick) / (t - _windowStart));
                _windowStart = t; _lastTick = tick;
            }
            try
            {
                if (Input.GetKeyDown(KeyCode.Space)) SetPause(!Paused);
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(3);
            }
            catch (Exception) { /* new Input System only — buttons still work */ }
        }

        void OnGUI()
        {
            var d = Driver(); if (d == null) return;
            var c = Clock();
            // D-140: the timeline (scrub) row exists when the clock rides the checkpoint grid
            bool scrubbable = c != null && d.bufferMode && d.Year >= 1;
            const int w = 332;
            int h = scrubbable ? 84 : 58;
            var r = new Rect(12, 12, w, h);
            GUI.Box(r, GUIContent.none);
            string head = c != null
                ? $"År {c.PresentationYear}   tick {(int)c.PresentationTick}   {EffectiveTps:F0} ticks/s   buffert +{c.BufferedYearsAhead} år"
                : $"År {d.Year}   tick {d.Tick}   {EffectiveTps:F0} ticks/s";
            GUI.Label(new Rect(r.x + 10, r.y + 4, w - 20, 22), head + (Paused ? "   ❚❚ PAUS" : ""));
            string[] labels = { "❚❚", "1×", "4×", "▶▶" };
            for (int i = 0; i < 4; i++)
            {
                bool active = Paused ? i == 0 : SpeedIndex == i;
                var br = new Rect(r.x + 10 + i * 78, r.y + 28, 70, 24);
                GUI.backgroundColor = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
                if (GUI.Button(br, labels[i]))
                {
                    if (i == 0) SetPause(!Paused);
                    else SetSpeed(i);
                }
            }
            GUI.backgroundColor = Color.white;

            // D-140: the TIMELINE — drag anywhere in produced history; released -> JumpToYear from the
            // checkpoint grid (year-grained scrub, D-137). The slider shows presentation year against
            // the producer's frontier; dragging never touches the sim (the grid re-enters, D-078 r4).
            if (scrubbable)
            {
                float shown = _scrubShown >= 0f ? _scrubShown : c.PresentationYear;
                float slid = GUI.HorizontalSlider(new Rect(r.x + 10, r.y + 62, w - 66, 16), shown, 0f, d.Year);
                GUI.Label(new Rect(r.x + w - 52, r.y + 58, 46, 20), $"y{Mathf.RoundToInt(slid)}/{d.Year}");
                if (Mathf.Abs(slid - shown) > 0.001f) { _scrubShown = slid; _scrubPending = Mathf.RoundToInt(slid); }
                if (_scrubPending >= 0 && GUIUtility.hotControl == 0)   // released — one jump, not one per dragged year
                {
                    int y = _scrubPending; _scrubPending = -1; _scrubShown = -1f;
                    if (y != c.PresentationYear && c.JumpToYear(y)) ScrubJumps++;   // y0 = genesis; the grid holds y000 in bufferMode
                }
            }
        }
    }
}
