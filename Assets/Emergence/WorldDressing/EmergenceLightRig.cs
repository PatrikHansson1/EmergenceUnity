// EMERGENCE P1 — THE LIGHT RIG (D-069 calibration + TD-012 §1 decoupled clock + TD-025 sky)
// Calibration source: the Village pack demo scene as measured on U-day —
// sun intensity 1.5 @ rotation (62,32,28); Environment Lighting = FLAT ambient
// RGB(84,98,106) — NOT skybox ambient (it washes everything lime/white).
// The decoupled-clock law: light is DIRECTION, not clock — live cycle only at 1x;
// above 1x the rig holds directed light (documentary principle).
//
// TD-025 (audition batch): the SKYBOX is a visual BACKDROP only — it kills the gray
// horizon gap without touching the object-lighting law. Ambient stays FLAT (objects
// are filled by the measured RGB, never by the sky), so the D-069 calibration holds;
// the Staggart painterly skyboxes only paint what the camera sees past the terrain.
// Painterly register + sobriety judge (VISUAL-QUALITY-BAR): Staggart hour set, not photoreal AllSky.
#if UNITY_EDITOR
using System;
using System.Linq;
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

        // Staggart Stylized Skyboxes (TD-025). Exact-name lookup (avoids "Sky_Noon (Cloudy)" etc.).
        static Material Sky(string exact)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Material {exact}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == exact)
                    return AssetDatabase.LoadAssetAtPath<Material>(p);
            }
            return null;
        }

        public static void Apply(string season, string phase)
        {
            var sunGo = GameObject.Find("Sun");
            if (sunGo == null) sunGo = new GameObject("Sun");
            // NEVER ?? on Unity objects (fake-null trap — GetComponent's miss is not caught by ??).
            if (!sunGo.TryGetComponent<Light>(out var sun)) sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat; // U-day law: flat, never skybox ambient

            // Skybox backdrop per phase (visual only; ambient stays flat above).
            // Rule 34 (EP 2026-07-19): the PACK's own skybox is the same hand as the world —
            // prefer it where it exists (day/night); the pack ships no dusk, so blue hour
            // auditions the Staggart Sky_Dusk. Free is fallback, never first.
            string skyName; Material sky;
            if (phase == "dusk") { skyName = "Sky_Dusk"; sky = Sky(skyName); }
            else if (phase == "night") { skyName = "M_ENV_SKYBOX_night"; sky = Sky(skyName) ?? Sky(skyName = "Sky_Night"); }
            else { skyName = "M_ENV_SKYBOX_day"; sky = Sky(skyName) ?? Sky(skyName = "Sky_Noon"); }
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                // ensure the doc camera actually shows the sky (not a solid gray clear)
                var cam = Camera.main;
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }
            else Debug.LogWarning($"[LightRig] skybox '{skyName}' not found — gray horizon (import Staggart Stylized Skyboxes).");

            // D-101e: a soft FILL directional light (no shadows) from opposite the key sun. The pack's
            // foliage shader responds to DIRECTIONAL light, not ambient — so this is the only thing that
            // lifts the shaded/back sides of tree canopies out of near-black ("the dark blob"). The sun
            // stays the key; this just fills. Disabled-ish at night.
            var fillGo = GameObject.Find("SunFill");
            if (fillGo == null) fillGo = new GameObject("SunFill");
            if (!fillGo.TryGetComponent<Light>(out var fill)) fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, 150f, 0f);  // low, roughly opposite the key sun
            fill.color = new Color(0.72f, 0.82f, 0.88f);                 // cool sky-tinted bounce
            fill.intensity = phase == "day" ? 0.6f : (phase == "dusk" ? 0.14f : 0.07f);

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
                    sun.intensity = 0.22f;                                  // a touch of moonlight so the ground reads
                    sun.color = new Color(0.6f, 0.7f, 0.95f);
                    RenderSettings.ambientLight = new Color32(48, 58, 84, 255); // lifted from 30,38,58 — dark BLUE snow, not black
                    break;
                default: // day — D-101: matched to Dreamscape's own Demo sun (warm, soft, lower)
                    sun.transform.rotation = Quaternion.Euler(45f, 335f, 0f);
                    sun.intensity = 1.3f;
                    sun.color = new Color(1f, 0.957f, 0.839f);   // their warm daylight
                    // D-101e: Dreamscape's Demo lights foliage with a BRIGHT sky-filled Trilight ambient
                    // (their sky ≈ teal-green). Flat dark ambient (the old D-069 Village-pack law) left the
                    // shaded side of tree canopies near-black — the "dark blob". Sky-fill lifts those tops.
                    RenderSettings.ambientMode = AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.42f, 0.62f, 0.64f);   // soft sky fill on leaf tops
                    RenderSettings.ambientEquatorColor = new Color(0.44f, 0.50f, 0.44f);
                    RenderSettings.ambientGroundColor = new Color(0.26f, 0.28f, 0.22f);
                    if (season == "winter") { sun.intensity = 1.1f; sun.color = new Color(0.96f, 0.97f, 1f); RenderSettings.ambientSkyColor = new Color(0.5f, 0.56f, 0.66f); }
                    break;
            }

            // D-101: atmospheric fog — Dreamscape's Demo uses light-blue fog for depth (kills the hard
            // green/blue horizon seam, gives distance). Phase-tinted; linear so near objects stay crisp.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            switch (phase)
            {
                case "dusk": RenderSettings.fogColor = new Color(0.42f, 0.44f, 0.58f); RenderSettings.fogStartDistance = 160f; RenderSettings.fogEndDistance = 1200f; break;
                case "night": RenderSettings.fogColor = new Color(0.16f, 0.20f, 0.32f); RenderSettings.fogStartDistance = 120f; RenderSettings.fogEndDistance = 950f; break;
                default: RenderSettings.fogColor = new Color(0.635f, 0.820f, 1.0f); RenderSettings.fogStartDistance = 240f; RenderSettings.fogEndDistance = 1500f; break;
            }
            Debug.Log($"[LightRig] {season}/{phase} applied (flat ambient; sky={(sky ? skyName : "none")}; decoupled-clock law: presentation time, never sim time)");
        }
    }
}
#endif
