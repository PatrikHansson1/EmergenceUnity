// EMERGENCE — season light with a gentle crossfade (D-910). D-908 made the world's light follow the
// season, but it SNAPPED between the tuned spring-day and winter-day states. The two differ in only
// three day-lighting properties (sun color, sun intensity, ambient sky colour); everything else — sun
// angle, fog, skybox, fill — is identical. This eases those three over a few seconds, giving the light
// the same crossfade the music already has, without touching the locked rig: it captures the target by
// asking Fas3LightRig itself (never drifts from the calibration), then lerps from where we were.
// Presentation-only (D-078 r4): reads nothing from the sim, uses unscaledTime (fades even while paused),
// disarms to a straight snap if anything is missing.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceSeasonLight : MonoBehaviour
    {
        public float crossfadeSecs = 4f;

        string _season;
        bool _fading;
        float _t;
        Light _sun;
        Color _fromSun, _toSun, _fromAmb, _toAmb;
        float _fromInt, _toInt;

        static Light FindSun() { var go = GameObject.Find("Sun"); return go != null ? go.GetComponent<Light>() : null; }

        /// <summary>Apply the season's day-light. The first call snaps (the opening); later season changes crossfade.</summary>
        public void ApplySeason(string season)
        {
            if (string.IsNullOrEmpty(season)) season = "spring";
            if (season == _season) return;
            bool first = _season == null;
            _season = season;

            if (first) { Fas3LightRig.Apply(season, "day"); return; }   // opening: snap, no fade

            // capture where we are, apply the target (snaps the globals), capture it, restore, then lerp there
            _sun = FindSun();
            Color fromSun = _sun != null ? _sun.color : Color.white;
            float fromInt = _sun != null ? _sun.intensity : 1.3f;
            Color fromAmb = RenderSettings.ambientSkyColor;

            Fas3LightRig.Apply(season, "day");

            _sun = FindSun();
            _toSun = _sun != null ? _sun.color : fromSun;
            _toInt = _sun != null ? _sun.intensity : fromInt;
            _toAmb = RenderSettings.ambientSkyColor;

            if (_sun != null) { _sun.color = fromSun; _sun.intensity = fromInt; }
            RenderSettings.ambientSkyColor = fromAmb;
            _fromSun = fromSun; _fromInt = fromInt; _fromAmb = fromAmb;
            _fading = true; _t = 0f;
        }

        void Update()
        {
            if (!_fading) return;
            _t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, crossfadeSecs));
            if (_sun != null) { _sun.color = Color.Lerp(_fromSun, _toSun, k); _sun.intensity = Mathf.Lerp(_fromInt, _toInt, k); }
            RenderSettings.ambientSkyColor = Color.Lerp(_fromAmb, _toAmb, k);
            if (k >= 1f) _fading = false;
        }
    }
}
