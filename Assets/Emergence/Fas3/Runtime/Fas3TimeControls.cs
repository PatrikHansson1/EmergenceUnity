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

            // D-218: the head of this panel was a debug readout wearing a box. It asserted four
            // things and exactly ONE is a fact a witness has: the year. Ticks, ticks/s and buffer
            // depth are the machine describing itself; they moved behind the diagnostics gate.
            //
            // D-220 — AND IT WAS STILL A BOX. Patrik, on seeing it: "stela och fyrkantiga". Right.
            // THE PRICKED CORNER. This panel draws no rectangle at all. It is braced at two opposite
            // corners and DISSOLVES at the other two, so there is no closed shape for the eye to read
            // as a box — four identically treated edges IS a box and there is no way to draw one that
            // is not. A pricking column (the marks a scribe made to SET the ruling) breaks the left
            // edge with rhythm instead of a line. And the four speed buttons — four identical squares,
            // the boxiest widget available — become a scribe's TALLY of rising strokes, so speed is
            // carried by HEIGHT as well as by fill and survives colour-blindness and a glance.
            const int w = 196;
            int h = scrubbable ? 108 : 80;
            var r = new Rect(EmergenceUI.Sp6, EmergenceUI.Sp6, w, h);

            var body = new Rect(r.x, r.y, r.width - 12f, r.height - 12f);
            var fill = EmergenceUI.Surface1; fill.a = EmergenceUI.PanelAlpha;
            var pc = GUI.color; GUI.color = fill;
            GUI.DrawTexture(body, Texture2D.whiteTexture); GUI.color = pc;
            EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width, r.height - 12f), fill, fill.a, true,  true, 12);
            EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width - 12f, r.height), fill, fill.a, false, true, 12);
            EmergenceUI.LayTooth(new Rect(r.x, r.y, r.width, r.height), EmergenceUI.ToothBody);

            EmergenceUI.Bracket(body, EmergenceUI.Corner.TopLeft, EmergenceUI.Hairline);
            EmergenceUI.Bracket(body, EmergenceUI.Corner.BottomRight, EmergenceUI.Hairline);
            // the lamp is up and to the left, so that bracket alone catches a second, gold rule
            EmergenceUI.Bracket(new Rect(body.x + 3, body.y + 3, body.width, body.height),
                                EmergenceUI.Corner.TopLeft, EmergenceUI.GoldLeaf);
            EmergenceUI.PrickColumn(r.x + EmergenceUI.PrickInset, r.y + EmergenceUI.Sp5,
                                    body.height - EmergenceUI.Sp7, EmergenceUI.Hairline, 8);

            int year = c != null ? c.PresentationYear : d.Year;
            float tx = r.x + EmergenceUI.Sp5;
            GUI.Label(new Rect(tx, r.y + EmergenceUI.Sp3, 60, 16), "YEAR", EmergenceUI.Meta);
            GUI.Label(new Rect(tx, r.y + EmergenceUI.Sp3 + 12, 120, 38), year.ToString(), EmergenceUI.Display);
            float numW = EmergenceUI.Display.CalcSize(new GUIContent(year.ToString())).x;
            // the rubricator's underline, spanning exactly the numeral. The panel's only gold.
            EmergenceUI.RuleH(tx, r.y + EmergenceUI.Sp3 + 48, numW, EmergenceUI.GoldLeaf, false);

            if (EmergenceUI.Diagnostics)
            {
                string diag = c != null
                    ? $"tick {(int)c.PresentationTick}  {EffectiveTps:F0} t/s  +{c.BufferedYearsAhead}"
                    : $"tick {d.Tick}  {EffectiveTps:F0} t/s";
                GUI.Label(new Rect(r.x + EmergenceUI.Sp5, r.y + h - 15, w - 30, 14), diag, EmergenceUI.Meta);
            }

            // WORDS FOR STATE, MARKS FOR DEGREE.
            // the pause mark is DRAWN, not implied: a tally alone tells a player how fast the world
            // runs and never tells them they may stop it.
            float pauseX = r.x + w - 76f, tallyX = pauseX + 16f, tallyBase = r.y + EmergenceUI.Sp3 + 46f;
            EmergenceUI.PauseMark(pauseX, tallyBase, Paused, EmergenceUI.Ink100, EmergenceUI.Hairline, EmergenceUI.GoldLeaf);
            EmergenceUI.Tally(tallyX, tallyBase, Mathf.Clamp(SpeedIndex - 1, 0, 2), Paused,
                              EmergenceUI.Ink100, EmergenceUI.Hairline, EmergenceUI.GoldLeaf);
            if (Paused)
                GUI.Label(new Rect(r.x + EmergenceUI.Sp5, tallyBase + 6f, 90, 14), "PAUSED", EmergenceUI.Dim);

            if (GUI.Button(new Rect(pauseX - 4f, tallyBase - 16f, 16f, 22f), GUIContent.none, GUIStyle.none))
                SetPause(!Paused);
            for (int i = 0; i < 3; i++)
            {
                var hit = new Rect(tallyX + i * 7f - 3f, tallyBase - 18f, 10f, 24f);
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none)) SetSpeed(i + 1);
            }

            // D-140: the TIMELINE — drag anywhere in produced history; released -> JumpToYear from the
            // checkpoint grid (year-grained scrub, D-137). Dragging never touches the sim.
            if (scrubbable)
            {
                float shown = _scrubShown >= 0f ? _scrubShown : c.PresentationYear;
                float sy = r.y + 66f;
                float slid = EmergenceUI.Slider(new Rect(r.x + EmergenceUI.Sp5, sy - 4f, w - 80f, 12f), shown, 0f, d.Year);
                GUI.Label(new Rect(r.x + EmergenceUI.Sp5, sy + 17f, 130, 14), $"{Mathf.RoundToInt(slid)} of {d.Year}", EmergenceUI.Meta);
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
