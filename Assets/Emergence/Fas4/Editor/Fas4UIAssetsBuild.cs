// EMERGENCE — FAS 4: one-time UI Toolkit asset builder for the native chronicle view.
//
// A runtime UIDocument needs a PanelSettings asset with a theme — these are the ONLY two
// UI Toolkit assets in the project, both under Assets/Emergence/Resources so the player build
// carries them without touching the EmergenceAssetCatalog. Idempotent: safe to call every
// probe run; creates only what is missing. The theme is the standard runtime default
// (@import unity-theme://default) — all actual styling lives in Fas4ChronicleView code.
// Menu: Emergence/Fas4/BUILD UI ASSETS.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Emergence.Editor
{
    public static class Fas4UIAssetsBuild
    {
        const string Dir       = "Assets/Emergence/Resources";
        const string ThemePath = Dir + "/Fas4RuntimeTheme.tss";
        const string PsPath    = Dir + "/Fas4PanelSettings.asset";

        [MenuItem("Emergence/Fas4/BUILD UI ASSETS")]
        public static void Ensure()
        {
            Directory.CreateDirectory(Dir);

            if (!File.Exists(ThemePath))
            {
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(ThemePath);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null) { Debug.LogError("[Fas4UIAssetsBuild] theme import failed: " + ThemePath); return; }

            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PsPath);
            bool made = false;
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, PsPath);
                made = true;
            }
            // D-218: ConstantPixelSize meant the chronicle and the almanac drew at raw pixels, so on
            // a 2560x1440 capture they were HALF the intended size beside an IMGUI half that now
            // scales — two surfaces, two scales, one screen. Match the Screen Bible's own reference
            // (1280x800, matched on HEIGHT so extra width becomes gutter rather than text) and the
            // two halves finally agree. Applied on every build, not only on creation: the asset
            // already existed with the wrong mode and would never have been corrected.
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1280, 800);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 1f;
            ps.sortingOrder = 50;
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            Debug.Log("[Fas4UIAssetsBuild] " + (made ? "created " : "updated ") + PsPath + " (1280x800, match height)");
        }
    }
}
#endif
