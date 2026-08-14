// EMERGENCE — VÅG 1.2 (2026-08-14): THE LIGHT RIG, MOVED TO RUNTIME.
//
// Same story as the terrain (D-210): the light law was good and had been tuned for weeks — a warm
// key sun matched to the pack's own demo, a cool fill so canopies do not go to black blobs, flat or
// trilight ambient by phase, linear fog for depth, and the locked dusk identity (a blue world with
// exactly ONE warm point, the fires carrying the warmth). And like the terrain, it lived behind
// #if UNITY_EDITOR, so the player's world was lit by whatever a bare scene happened to have: one
// default directional light, no fog, no sky. That is why the living loop looked washed out and
// chalky next to the store shots — not a material problem, a LIGHTING one.
//
// Its only editor binding was finding the skybox material. That now comes from the catalog, exactly
// like the terrain layers. Everything else here was already runtime-safe.
//
// Presentation-only (D-078 r4). The phase is PRESENTATION time, never sim time (the decoupled-clock
// law) — the world does not get darker because the simulation says so.
using UnityEngine;
using UnityEngine.Rendering;

namespace Emergence.Runtime
{
    public static class Fas3LightRig
    {
        public static string LastNote = "";

        /// <summary>Light the world. season = spring|winter, phase = day|dusk|night.</summary>
        public static void Apply(string season, string phase)
        {
            var sunGo = GameObject.Find("Sun");
            if (sunGo == null) sunGo = new GameObject("Sun");
            if (!sunGo.TryGetComponent<Light>(out var sun)) sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;

            RenderSettings.ambientMode = AmbientMode.Flat;

            var cat = EmergenceAssetCatalog.Load();
            string skyName; Material sky = null;
            if (phase == "dusk") { skyName = "Sky_Dusk"; sky = cat != null ? cat.Skybox(skyName) : null; }
            else if (phase == "night")
            {
                skyName = "M_ENV_SKYBOX_night";
                sky = cat != null ? cat.Skybox(skyName) : null;
                if (sky == null && cat != null) { skyName = "Sky_Night"; sky = cat.Skybox(skyName); }
            }
            else
            {
                skyName = "M_ENV_SKYBOX_day";
                sky = cat != null ? cat.Skybox(skyName) : null;
                if (sky == null && cat != null) { skyName = "Sky_Noon"; sky = cat.Skybox(skyName); }
            }
            if (sky != null)
            {
                RenderSettings.skybox = sky;
                var cam = Camera.main;
                if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
            }

            // the fill: the pack's foliage shader answers DIRECTIONAL light, not ambient — without this
            // the shaded side of every canopy goes near-black (the "dark blob", D-101e)
            var fillGo = GameObject.Find("SunFill");
            if (fillGo == null) fillGo = new GameObject("SunFill");
            if (!fillGo.TryGetComponent<Light>(out var fill)) fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(28f, 150f, 0f);
            fill.color = new Color(0.72f, 0.82f, 0.88f);
            fill.intensity = phase == "day" ? 0.6f : (phase == "dusk" ? 0.14f : 0.07f);

            switch (phase)
            {
                case "dusk":   // the locked identity: a blue world, exactly ONE warm point
                    sun.transform.rotation = Quaternion.Euler(8f, 55f, 0f);
                    sun.intensity = 0.55f;
                    sun.color = new Color(1f, 0.55f, 0.35f);
                    RenderSettings.ambientLight = new Color32(52, 62, 84, 255);
                    break;
                case "night":
                    sun.transform.rotation = Quaternion.Euler(-30f, 40f, 0f);
                    sun.intensity = 0.22f;
                    sun.color = new Color(0.6f, 0.7f, 0.95f);
                    RenderSettings.ambientLight = new Color32(48, 58, 84, 255);
                    break;
                default:       // day — matched to the pack's own demo sun (warm, soft, lower)
                    sun.transform.rotation = Quaternion.Euler(45f, 335f, 0f);
                    sun.intensity = 1.3f;
                    sun.color = new Color(1f, 0.957f, 0.839f);
                    RenderSettings.ambientMode = AmbientMode.Trilight;
                    RenderSettings.ambientSkyColor = new Color(0.42f, 0.62f, 0.64f);
                    RenderSettings.ambientEquatorColor = new Color(0.44f, 0.50f, 0.44f);
                    RenderSettings.ambientGroundColor = new Color(0.26f, 0.28f, 0.22f);
                    if (season == "winter")
                    {
                        sun.intensity = 1.1f; sun.color = new Color(0.96f, 0.97f, 1f);
                        RenderSettings.ambientSkyColor = new Color(0.5f, 0.56f, 0.66f);
                    }
                    break;
            }

            // linear fog for depth — kills the hard horizon seam and gives the land distance
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            switch (phase)
            {
                case "dusk":  RenderSettings.fogColor = new Color(0.42f, 0.44f, 0.58f); RenderSettings.fogStartDistance = 160f; RenderSettings.fogEndDistance = 1200f; break;
                case "night": RenderSettings.fogColor = new Color(0.16f, 0.20f, 0.32f); RenderSettings.fogStartDistance = 120f; RenderSettings.fogEndDistance = 950f; break;
                default:      RenderSettings.fogColor = new Color(0.635f, 0.820f, 1.0f); RenderSettings.fogStartDistance = 240f; RenderSettings.fogEndDistance = 1500f; break;
            }

            LastNote = "light: " + season + "/" + phase + "  sky=" + (sky != null ? skyName : "NONE (grey horizon)")
                     + "  sun=" + sun.intensity.ToString("F2") + "  fill=" + fill.intensity.ToString("F2") + "  fog=on";
        }

        /// <summary>Is the world lit by us, or by whatever a bare scene happened to carry?</summary>
        public static bool Applied => GameObject.Find("Sun") != null && GameObject.Find("SunFill") != null && RenderSettings.fog;
    }
}
