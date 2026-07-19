// EMERGENCE P1 — the URP post stack (VISUAL-QUALITY-BAR: ACES + restrained bloom + AO note)
// D-074 §3 protections honored: NO effect soup. Every override serves the documentary
// look — ACES filmic response, a bloom that only blooms the ONE warm point (high threshold),
// a whisper of vignette to seat the eye, and gentle contrast/saturation. Nothing sparkles.
// SSAO is a URP *renderer feature* (not a Volume override) — toggled on the renderer asset;
// left as a P1 follow-up note here rather than risk-editing the pipeline asset from script.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Emergence.Editor
{
    public static class EmergencePostStack
    {
        const string ProfilePath = "Assets/Emergence/WorldDressing/EmergencePost.asset";

        [MenuItem("Emergence/P1 Dressing/Post - Apply URP stack (ACES/bloom)")]
        public static void ApplyMenu() => Apply("day");

        // Per-phase grade (fable-5's TD-026 finding: noon contrast crushes the blue hour).
        // Dusk/night LIFT exposure + drop contrast so the settlement stays legible UNDER the
        // one warm point, without flattening the "blue world" identity.
        public static void Apply(string phase = "day")
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            else
            {
                // rebuild from clean so re-runs are idempotent
                foreach (var c in profile.components.ToArray()) Object.DestroyImmediate(c, true);
                profile.components.Clear();
            }

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES); // filmic — the industry-standard response (D-074)

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.15f);  // only the warm point / bright speculars bloom
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(1f, 0.94f, 0.85f)); // warm, so fire glow reads warm

            bool dim = phase == "dusk" || phase == "night";
            float exposure = phase == "dusk" ? 0.35f : phase == "night" ? 0.15f : 0.0f;
            float contrast = dim ? 4f : 8f;
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(4f);   // painterly, not cartoon-saturated (sobriety)

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.20f);
            vig.smoothness.Override(0.5f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            // global volume in the scene
            var go = GameObject.Find("PostVolume") ?? new GameObject("PostVolume");
            var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.profile = profile;

            // enable post + SMAA on the doc camera
            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
            else Debug.LogWarning("[PostStack] no Camera.main — dress a world first, then apply.");

            Debug.Log("[PostStack] ACES + bloom(1.15/0.45) + contrast/sat + vignette applied; post+SMAA on doc camera. (SSAO = renderer-feature follow-up.)");
        }

        [MenuItem("Emergence/P1 Dressing/Post - Remove (A/B before shot)")]
        public static void Remove()
        {
            var go = GameObject.Find("PostVolume");
            if (go != null) Object.DestroyImmediate(go);
            var cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            Debug.Log("[PostStack] removed — camera post off (the A/B 'before' state).");
        }
    }
}
#endif
