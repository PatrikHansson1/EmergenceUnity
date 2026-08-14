// EMERGENCE — SKÄRMBIBELN §3 (D-218): THE LATEST LINE.
//
// This is the HUD's actual content, and the argument for it is the strongest single line in the
// Screen Bible: without it, a running world is MUTE. A player watching a century-scale simulation at
// 1x sees nothing move for minutes at a time, concludes nothing is happening, and puts the game
// down — and they are not wrong to, because nothing on screen has told them otherwise. The Latest
// Line is the difference between a screensaver and a game.
//
// It also replaces a whole category of thing this game must never have. There are no toasts, no
// notification queue, no alert badges, no popups. New events go to the Chronicle, which is where
// events go — a parallel notification channel would mean the Chronicle is NOT the record, and the
// Chronicle's promise ("everything below happened") is the product.
//
// So: one line, the newest witnessed event, in its final prose, on a nearly-opaque slate strip. It
// does not slide in from the side; it fades up in place, as if the ink is drying. It never pulses
// and never asks to be clicked. And it carries the voice register beside it — REMEMBERED, TOLD,
// WRITTEN, WEIGHED — which is the ONE place in the game that marker is permitted, because inside
// the book the register is expressed as materiality and never as a badge (Skärmbibeln §9 K3). A
// player who never opens the Chronicle would otherwise never learn that the most distinctive
// systemic idea in the project exists.
//
// Presentation-only (D-078 r4): reads the feed, writes nothing, consumes no sim RNG.
using UnityEngine;

namespace Emergence.Runtime
{
    [DefaultExecutionOrder(50)]
    public sealed class Fas4LatestLine : MonoBehaviour
    {
        public bool showUI = true;
        public Fas4ChronicleFeed feed;

        /// <summary>The strip is 880 px at the 1280x800 reference — the Chronicle's own measure, so
        /// one line of the book and one line of the HUD wrap at the same place.</summary>
        public const int StripWidth = 736;

        string _text = "", _tierName = "";
        int _tier = 0;
        int _year = -1, _count = -1;
        float _arrived = -99f;

        // D-222: THE GAME SHIPS IN ENGLISH. These are the chronicle's four registers, in the art
        // director's own words - the ladder the book climbs as the world learns to keep records.
        static readonly string[] TierName = { "REMEMBERED", "TOLD", "WRITTEN", "WEIGHED" };

        Fas4ChronicleFeed Feed()
        {
            if (feed == null) feed = FindAnyObjectByType<Fas4ChronicleFeed>();
            return feed;
        }

        void Update()
        {
            var f = Feed();
            if (f == null || f.Entries == null || f.Entries.Count == 0) return;
            if (f.Entries.Count == _count) return;
            _count = f.Entries.Count;
            var e = f.Entries[f.Entries.Count - 1];
            if (e.text == _text && e.year == _year) return;
            _text = e.text ?? ""; _year = e.year;
            _tier = Mathf.Clamp(e.voiceTier, 0, TierName.Length - 1);
            _tierName = e.voiceTier >= 0 && e.voiceTier < TierName.Length ? TierName[e.voiceTier] : "";
            _arrived = Time.unscaledTime;
        }

        void OnGUI()
        {
            if (!showUI || string.IsNullOrEmpty(_text)) return;
            EmergenceUI.Begin();          // reference space, so 21 px is 21 px on any screen

            // D-220 — THIS STRIP IS NOT A PANEL. It was a wide dark box laid across the bottom of a
            // beautiful meadow, and killing that rectangle is, on its own, the single change that
            // most answers "boxy". A single line of prose in a fixed measure is exactly the problem
            // the manuscript LINE-FILLER was invented to solve, so this is the best ornament
            // opportunity in the game: the world darkens INTO a line of writing instead of being
            // interrupted by a box, the rule above it ends in lozenges rather than simply stopping,
            // and the leftover space between the prose and the year is filled the way a scribe
            // filled a short last line — at a richness that tracks the chronicle's own register.

            float age = Time.unscaledTime - _arrived;
            float a = Mathf.Clamp01(age / EmergenceUI.DurEntry);
            a = 1f - (1f - a) * (1f - a);          // fades up IN PLACE, as ink dries. Never a slide.

            int measure = Mathf.Min(StripWidth, Mathf.RoundToInt(EmergenceUI.W) - EmergenceUI.Sp6 * 2);
            float cx = EmergenceUI.W * 0.5f;
            float x0 = cx - measure * 0.5f;
            float baseY = EmergenceUI.H - EmergenceUI.Sp7 - EmergenceUI.Sp4;

            // the wash: no rect, no border, no scrim — the world simply darkens into the writing
            EmergenceUI.Wash(new Rect(x0 - EmergenceUI.Sp8, baseY - EmergenceUI.Sp7 - EmergenceUI.Sp2,
                                      measure + EmergenceUI.Sp8 * 2f, EmergenceUI.Sp7 + EmergenceUI.Sp7),
                             EmergenceUI.Surface0, 0.62f);

            var prev = GUI.color;

            // the rule, spanning only the MEASURE, ending in marks
            GUI.color = new Color(1f, 1f, 1f, a);
            // the hairline is blue-grey and at 1px over a lit meadow it reads as a UI divider.
            // A scribe's rule is warm and faint: bone at low alpha, gold once the page is ruled.
            var ruleC = _tier >= 2 ? EmergenceUI.GoldLeaf : Dim(EmergenceUI.Ink55, 0.55f);
            EmergenceUI.RuleH(x0, baseY - EmergenceUI.Sp5, measure, ruleC);

            // the register: a WORD (colour never carries meaning alone) preceded by a lozenge whose
            // fill tracks the register — two channels, neither of them colour on its own
            float lz = x0 + 3f, lzy = baseY - EmergenceUI.Sp5 + 15f;
            var lzc = _tier >= 3 ? EmergenceUI.GoldLeaf : (_tier >= 2 ? EmergenceUI.Ink70 : EmergenceUI.Hairline);
            EmergenceUI.LozengeAt(lz, lzy, EmergenceUI.Lozenge, lzc);
            if (!string.IsNullOrEmpty(_tierName))
                GUI.Label(new Rect(x0 + 12f, baseY - EmergenceUI.Sp5 + 7f, 130, 16), _tierName, EmergenceUI.Meta);

            // the prose
            float textY = baseY - 2f;
            var content = new GUIContent(_text);
            float textW = EmergenceUI.Prose.CalcSize(content).x;
            GUI.Label(new Rect(x0, textY, measure - 88f, 26), content, EmergenceUI.Prose);

            // the year, behind its own capped separator
            float sepX = x0 + measure - 62f;
            EmergenceUI.RuleV(sepX, textY + 4f, 16f, EmergenceUI.Hairline, false);
            EmergenceUI.LozengeAt(sepX + 0.5f, textY + 3f, 3, EmergenceUI.Hairline);
            GUI.Label(new Rect(sepX + 10f, textY, 54, 26), _year >= 0 ? "yr " + _year : "", EmergenceUI.Meta);

            // THE LINE-FILLER: the leftover between the prose and the separator, at a richness that
            // tracks the register. Suppressed under 48 px — a cramped filler reads as a glitch.
            float fx = x0 + textW + EmergenceUI.Sp4, fw = sepX - EmergenceUI.Sp4 - fx;
            if (fw >= 48f) Filler(fx, textY + 13f, fw);

            GUI.color = prev;
            EmergenceUI.End();
        }

        /// <summary>How a scribe finished a short last line. R1 leaves it empty (an oral memory does
        /// not tidy itself), R2 a dotted lead, R3 a rule with a mark at its end, R4 three vine ticks
        /// in gold. The ornament IS the register — which turns "add decoration" into a feature, and
        /// gives the register a second channel that is not colour.</summary>
        static Color Dim(Color c, float a) { c.a = a; return c; }

        void Filler(float x, float y, float w)
        {
            switch (_tier)
            {
                case 1:
                    for (float p = x; p < x + w; p += 6f)
                        EmergenceUI.RuleH(p, y, 2f, Dim(EmergenceUI.Ink55, 0.5f), false);
                    break;
                case 2:
                    EmergenceUI.RuleH(x, y, w, Dim(EmergenceUI.Ink55, 0.45f), false);
                    EmergenceUI.LozengeAt(x + w, y + 0.5f, EmergenceUI.Lozenge, Dim(EmergenceUI.Ink55, 0.7f));
                    break;
                case 3:
                    for (int i = 0; i < 3; i++)
                    {
                        float px = x + w - 6f - i * 12f;
                        if (px < x) break;
                        EmergenceUI.LozengeAt(px, y + 0.5f, 3, EmergenceUI.GoldLeaf);
                        EmergenceUI.RuleH(px - 5f, y, 5f, EmergenceUI.GoldLeaf, false);
                    }
                    break;
            }
        }
    }
}
