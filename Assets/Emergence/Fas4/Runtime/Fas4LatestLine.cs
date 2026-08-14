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
        public const int StripWidth = 880;

        string _text = "", _tier = "";
        int _year = -1, _count = -1;
        float _arrived = -99f;

        static readonly string[] TierName = { "MINNS", "BERÄTTAT", "SKRIVET", "VÄGT" };

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
            _tier = e.voiceTier >= 0 && e.voiceTier < TierName.Length ? TierName[e.voiceTier] : "";
            _arrived = Time.unscaledTime;
        }

        void OnGUI()
        {
            if (!showUI || string.IsNullOrEmpty(_text)) return;
            EmergenceUI.Begin();          // reference space, so 21 px is 21 px on any screen

            // fade up in place. Never a slide, never a bounce: this is testimony arriving, not applause.
            float age = Time.unscaledTime - _arrived;
            float a = Mathf.Clamp01(age / EmergenceUI.DurEntry);
            a = 1f - (1f - a) * (1f - a);

            int w = Mathf.Min(StripWidth, Mathf.RoundToInt(EmergenceUI.W) - EmergenceUI.Sp6 * 2);
            int h = EmergenceUI.Sp7 + EmergenceUI.Sp4;
            var r = new Rect((EmergenceUI.W - w) * 0.5f, EmergenceUI.H - h - EmergenceUI.Sp6, w, h);
            EmergenceUI.DrawPanel(r);

            var prev = GUI.color;
            if (!string.IsNullOrEmpty(_tier))
            {
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(r.x + EmergenceUI.Sp5, r.y + EmergenceUI.Sp2, 110, 18), _tier, EmergenceUI.Meta);
            }
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(r.x + EmergenceUI.Sp5, r.y + EmergenceUI.Sp5, w - EmergenceUI.Sp5 * 2 - 70, 26),
                      _text, EmergenceUI.Label);
            GUI.color = new Color(1f, 1f, 1f, a * 0.85f);
            GUI.Label(new Rect(r.xMax - EmergenceUI.Sp5 - 56, r.y + EmergenceUI.Sp5, 56, 26),
                      _year >= 0 ? "år " + _year : "", EmergenceUI.Meta);
            GUI.color = prev;
            EmergenceUI.End();
        }
    }
}
