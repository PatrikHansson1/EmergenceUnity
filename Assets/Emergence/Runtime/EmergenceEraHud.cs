// EMERGENCE — the epoch line (D-907): a quiet, persistent "Year N · <Era>" at the top of the frame,
// so a player watching a century-scale world always knows WHEN they are. Pure presentation: reads the
// applied state's era (engine eraName wins, WorldEras interim fallback — the one era-name law, D-147)
// and the presentation clock's year; writes nothing, touches no bus, uses no RNG. OnGUI in EmergenceUI's
// parchment look. Placement/size is a studio call (D-895); feel is tunable.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceEraHud : MonoBehaviour
    {
        Fas3WorldRuntime _rt;
        Fas3PresentationClock _clock;
        GUIStyle _style;

        Fas3WorldRuntime Rt() { if (_rt == null) _rt = FindAnyObjectByType<Fas3WorldRuntime>(); return _rt; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }

        void OnGUI()
        {
            var rt = Rt();
            if (rt == null || rt.LastState == null) return;      // nothing applied yet: stay silent
            var c = Clock();
            int year = c != null ? c.PresentationYear : 0;

            string era = WorldEras.Name(rt.LastState);
            if (!string.IsNullOrEmpty(era)) era = char.ToUpper(era[0]) + era.Substring(1);
            string label = "Year " + year + "     ·     " + era;

            if (_style == null)
                _style = new GUIStyle(EmergenceUI.Meta) { alignment = TextAnchor.UpperCenter };

            EmergenceUI.Begin();
            float W = EmergenceUI.W;
            var prev = GUI.color;
            var ink = EmergenceUI.Ink70;
            GUI.color = new Color(ink.r, ink.g, ink.b, 0.9f);
            GUI.Label(new Rect(0f, EmergenceUI.Sp5, W, 22f), label, _style);
            GUI.color = prev;
            EmergenceUI.End();
        }
    }
}
