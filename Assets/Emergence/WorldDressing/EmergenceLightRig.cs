// EMERGENCE P1 — THE LIGHT RIG (D-069 calibration + TD-012 §1 decoupled clock)
// Calibration source: the Village pack demo scene as measured on U-day —
// sun intensity 1.5 @ rotation (62,32,28); Environment Lighting = FLAT ambient
// RGB(84,98,106) — NOT skybox ambient (it washes everything lime/white).
// The decoupled-clock law: light is DIRECTION, not clock — live cycle only at 1x;
// above 1x the rig holds directed light (documentary principle).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emergence.Editor
{
    public static class EmergenceLightRig
    {
        [MenuItem("Emergence/P1 Dressing/Light - Noon")] public static void Noon() => Apply("spring", "day");
        [MenuItem("Emergence/P1 Dressing/Light - Dusk (one warm point)")] public static void Dusk() => Apply("spring", "dusk");
        [MenuItem("Emergence/P1 Dressing/Light - Night")] public static void Night() => Apply("spring", "night");

        public static void Apply(string season, string phase)
        {
            var sunGo = GameObject.Find("Sun") ?? new GameObject("Sun");
            var sun = sunGo.GetComponent<Light>() ?? sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat; // U-day law: flat, never skybox ambient

            switch (phase)
            {
                case "dusk": // the locked identity: a blue world with exactly ONE warm point (fires carry the warmth)
                    sun.transform.rotation = Quaternion.Euler(8f, 55f, 0f);
                    sun.intensity = 0.55f;
                    sun.color = new Color(1f, 0.55f, 0.35f);
                    RenderSettings.ambientLight = new Color32(52, 62, 84, 255);
                    break;
                case "night":
                    sun.transform.rotation = Quaternion.Euler(-30f, 40f, 0f);
                    sun.intensity = 0.12f;
                    sun.color = new Color(0.55f, 0.65f, 0.9f);
                    RenderSettings.ambientLight = new Color32(30, 38, 58, 255);
                    break;
                default: // day — the D-069 measured calibration
                    sun.transform.rotation = Quaternion.Euler(62f, 32f, 28f);
                    sun.intensity = 1.5f;
                    sun.color = Color.white;
                    RenderSettings.ambientLight = new Color32(84, 98, 106, 255);
                    // winter reads colder even at noon
                    if (season == "winter") { sun.intensity = 1.15f; RenderSettings.ambientLight = new Color32(88, 96, 112, 255); }
                    break;
            }
            Debug.Log($"[LightRig] {season}/{phase} applied (flat ambient; decoupled-clock law: presentation time, never sim time)");
        }
    }
}
#endif
