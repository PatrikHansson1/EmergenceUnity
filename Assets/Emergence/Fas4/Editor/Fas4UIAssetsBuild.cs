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
            if (ps == null)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.themeStyleSheet = theme;
                ps.scaleMode = PanelScaleMode.ConstantPixelSize;
                ps.sortingOrder = 50;
                AssetDatabase.CreateAsset(ps, PsPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Fas4UIAssetsBuild] created " + PsPath);
            }
            else if (ps.themeStyleSheet == null)
            {
                ps.themeStyleSheet = theme;
                EditorUtility.SetDirty(ps);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
