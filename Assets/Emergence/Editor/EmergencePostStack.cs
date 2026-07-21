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
        // D-114: Dreamscape's OWN showcase grade (Meadows_URP_PostProcess = Tonemapping/ColorAdjustments/
        // WhiteBalance/LiftGammaGain/Bloom/DoF). Rule 34 — same hand as the natural world. Adopted as the
        // DAY/NATURE base; our identity grade (below) stays the dusk/night "one warm point" mood.
        const string DreamscapePP = "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Scenes/Meadows_URP_PostProcess.asset";

        [MenuItem("Emergence/P1 Dressing/Post - Apply URP stack (ACES/bloom)")]
        public static void ApplyMenu() => Apply("day");

        // Per-phase grade (fable-5's TD-026 finding: noon contrast crushes the blue hour).
        // Dusk/night LIFT exposure + drop contrast so the settlement stays legible UNDER the
        // one warm point, without flattening the "blue world" identity.
        public static void Apply(string phase = "day")
        {
            bool dim = phase == "dusk" || phase == "night";

            // DAY/NATURE → adopt Dreamscape's own tuned profile as the base (the showcase look, EP directive).
            if (!dim)
            {
                var dsp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(DreamscapePP);
                if (dsp != null)
                {
                    // their DoF blurs everything on our high doc/map camera → turn it OFF (runtime only, not saved to the pack asset)
                    if (dsp.TryGet<DepthOfField>(out var dof)) dof.active = false;
                    AssignVolume(dsp);
                    Debug.Log("[PostStack] DAY base = Dreamscape Meadows_URP_PostProcess (grade kept, DoF off for the map camera).");
                    return;
                }
                Debug.LogWarning("[PostStack] Dreamscape PP not found — falling back to homegrown grade.");
            }

            // DUSK/NIGHT (identity mood) or fallback → our restrained ACES one-warm-point grade.
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

            float exposure = phase == "dusk" ? 0.35f : phase == "night" ? 0.45f : 0.0f;
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
            AssignVolume(profile);
            Debug.Log("[PostStack] identity grade (dusk/night/fallback): ACES + warm bloom + contrast/sat + vignette.");
        }

        // assign a profile to the global PostVolume + enable post + SMAA on the doc camera
        static void AssignVolume(VolumeProfile profile)
        {
            var go = GameObject.Find("PostVolume") ?? new GameObject("PostVolume");
            var vol = go.GetComponent<Volume>() ?? go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.profile = profile;

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
            else Debug.LogWarning("[PostStack] no Camera.main — dress a world first, then apply.");
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
