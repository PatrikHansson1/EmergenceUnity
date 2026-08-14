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
            EmergenceUI.Begin();          // draw in the Screen Bible's own 1280x800 reference space

            // D-218 (Skärmbibeln §3): THE HEAD OF THIS PANEL WAS A DEBUG READOUT WEARING A BOX.
            // "År 27   tick 3888   96 ticks/s   buffert +4 år" states four things and only ONE of
            // them is a fact a witness has: the year. Ticks, ticks-per-second and buffer depth are
            // the machine describing itself, and the Screen Bible's whole hierarchy argument is that
            // when the player cannot act, every number they must justify to themselves is noise.
            // The year is the anchor. The speed is the only verb. Everything else moves behind the
            // diagnostics gate, where a developer can still see it and a screenshot cannot.
            const int w = 332;
            int headH = EmergenceUI.Sp5 + EmergenceUI.Sp2;
            int h = EmergenceUI.Sp3 + headH + 26 + (scrubbable ? 30 : 0);
            var r = new Rect(EmergenceUI.Sp6, EmergenceUI.Sp6, w, h);
            EmergenceUI.DrawPanel(r);

            int year = c != null ? c.PresentationYear : d.Year;
            GUI.Label(new Rect(r.x + EmergenceUI.Sp4, r.y + EmergenceUI.Sp3, 140, 24), "ÅR " + year, EmergenceUI.Label);
            if (Paused)
                GUI.Label(new Rect(r.x + EmergenceUI.Sp4 + 92, r.y + EmergenceUI.Sp3 + 2, 120, 22), "PAUSAD", EmergenceUI.Meta);
            if (EmergenceUI.Diagnostics)
            {
                string diag = c != null
                    ? $"tick {(int)c.PresentationTick}   {EffectiveTps:F0} t/s   buffert +{c.BufferedYearsAhead}"
                    : $"tick {d.Tick}   {EffectiveTps:F0} t/s";
                GUI.Label(new Rect(r.x + EmergenceUI.Sp4, r.y + EmergenceUI.Sp3 + 22, w - 32, 18), diag, EmergenceUI.Meta);
            }

            // Four pips, and the active one is FILLED and LABELLED — two channels, so a player can
            // never be unable to tell whether the world froze or the game hung. That ambiguity is
            // the single largest generator of "I could not tell what was going on".
            string[] labels = { "❚❚", "1×", "4×", "▶▶" };
            int bw = (w - EmergenceUI.Sp4 * 2 - EmergenceUI.Sp2 * 3) / 4;
            for (int i = 0; i < 4; i++)
            {
                bool active = Paused ? i == 0 : SpeedIndex == i;
                var br = new Rect(r.x + EmergenceUI.Sp4 + i * (bw + EmergenceUI.Sp2), r.y + EmergenceUI.Sp3 + headH, bw, 22);
                if (GUI.Button(br, labels[i], active ? EmergenceUI.ButtonOn : EmergenceUI.Button))
                {
                    if (i == 0) SetPause(!Paused);
                    else SetSpeed(i);
                }
            }

            // D-140: the TIMELINE — drag anywhere in produced history; released -> JumpToYear from the
            // checkpoint grid (year-grained scrub, D-137). The slider shows presentation year against
            // the producer's frontier; dragging never touches the sim (the grid re-enters, D-078 r4).
            if (scrubbable)
            {
                float shown = _scrubShown >= 0f ? _scrubShown : c.PresentationYear;
                float sy = r.y + EmergenceUI.Sp3 + headH + 28;
                float slid = GUI.HorizontalSlider(new Rect(r.x + EmergenceUI.Sp4, sy + 4, w - 84, 16), shown, 0f, d.Year);
                GUI.Label(new Rect(r.x + w - 62, sy, 54, 20), $"{Mathf.RoundToInt(slid)}/{d.Year}", EmergenceUI.Meta);
                ScrubStep(slid, GUIUtility.hotControl != 0);
            }
            EmergenceUI.End();
        }

        /// <summary>
        /// The slider's whole semantic, extracted so a probe can drive the SAME code path OnGUI uses
        /// (gate-review fix 2026-07-22: "drag -> release -> ONE jump" must be proven, not narrated).
        /// held=true while the mouse drags (values accumulate, NO jump); held=false on release
        /// (exactly one JumpToYear for the final value). y0 = genesis; the grid holds y000 in bufferMode.
        /// </summary>
        public void ScrubStep(float slid, bool held)
        {
            var c = Clock(); if (c == null) return;
            float shown = _scrubShown >= 0f ? _scrubShown : c.PresentationYear;
            if (held && Mathf.Abs(slid - shown) > 0.001f) { _scrubShown = slid; _scrubPending = Mathf.RoundToInt(slid); }
            if (!held && _scrubPending >= 0)   // released — one jump, not one per dragged year
            {
                int y = _scrubPending; _scrubPending = -1; _scrubShown = -1f;
                if (y != c.PresentationYear && c.JumpToYear(y)) ScrubJumps++;
            }
        }
    }
}
