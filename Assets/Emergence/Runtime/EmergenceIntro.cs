// EMERGENCE — the opening (D-905): a brief title + controls hint that fades, so the built world
// announces itself and teaches its own interactivity ("an interactive build worth being proud of").
// Presentation-only: pure OnGUI over the scene, reads no sim state, touches no bus, and disables
// itself once the fade completes. Times off unscaledTime so a paused world still shows it. The FEEL
// (wording, timing, size, wash weight) is a later eye-pass — Patrik / the copywriter own the words.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceIntro : MonoBehaviour
    {
        public float holdSeconds = 3.5f;   // fully shown
        public float fadeSeconds = 2.5f;   // then eases out
        public string title = "EMERGENCE";
        public string hint  = "Space pause  ·  1/2/3 speed  ·  WASD / drag / scroll  camera  ·  B book  ·  M almanac  ·  -/+ volume";

        float _t0 = -1f;
        GUIStyle _titleStyle, _hintStyle;

        void OnGUI()
        {
            if (_t0 < 0f) _t0 = Time.unscaledTime;
            float t = Time.unscaledTime - _t0;
            float a = t <= holdSeconds ? 1f
                    : 1f - Mathf.Clamp01((t - holdSeconds) / Mathf.Max(0.01f, fadeSeconds));
            if (a <= 0f) { enabled = false; return; }   // one-shot: stop drawing once faded

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EmergenceUI.Display) { fontSize = 60, alignment = TextAnchor.MiddleCenter };
                _hintStyle  = new GUIStyle(EmergenceUI.Meta)    { alignment = TextAnchor.MiddleCenter };
            }

            EmergenceUI.Begin();
            float W = EmergenceUI.W, H = EmergenceUI.H;

            // a soft wash so the words read over any scene, lifting with the fade
            EmergenceUI.Wash(new Rect(0, 0, W, H), new Color(0.04f, 0.04f, 0.05f), 0.42f * a);

            var prev = GUI.color;
            var ink = EmergenceUI.Ink100;
            GUI.color = new Color(ink.r, ink.g, ink.b, a);
            GUI.Label(new Rect(0, H * 0.36f - 42f, W, 84f), title, _titleStyle);

            var dim = EmergenceUI.Ink70;
            GUI.color = new Color(dim.r, dim.g, dim.b, a);
            GUI.Label(new Rect(0, H * 0.36f + 40f, W, 22f), hint, _hintStyle);

            GUI.color = prev;
            EmergenceUI.End();
        }
    }
}
