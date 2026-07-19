// EMERGENCE — zero-pink scan v2: covers original packs + the free-assets audition batch (TD-025)
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class ZeroPinkScanV2
    {
        private static readonly string[] Roots = {
            "Assets/Fantastic Village Pack", "Assets/FANTASTIC - Village Pack", "Assets/Polyart", "Assets/Quaternius",
            "Assets/Vefects", "Assets/msVFX_Free Smoke Effects Pack", "Assets/PolyOne", "Assets/UpDraftArt",
            "Assets/AllSkyFree", "Assets/Bitgem", "Assets/Houidisoft technology", "Assets/Procedural Water Shader",
            "Packages/xyz.staggartcreations.skyboxes"
        };

        [MenuItem("Emergence/Pack Verify/1b. Zero-Pink Material Scan v2 (incl. audition batch)")]
        public static void Run()
        {
            var roots = Roots.Where(AssetDatabase.IsValidFolder).ToArray();
            var guids = AssetDatabase.FindAssets("t:Material", roots);
            var bad = new List<string>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) { bad.Add(path + " (null)"); continue; }
                if (mat.shader.name == "Hidden/InternalErrorShader" || !mat.shader.isSupported)
                    bad.Add(path + " -> " + mat.shader.name);
            }
            var verdict = $"materials={guids.Length} broken={bad.Count} => {(bad.Count == 0 ? "ZERO PINK PASS" : "FAIL")}";
            Debug.Log("[PackVerify-v2] " + verdict);
            var dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\pack-verify";
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "zero-pink-scan-v2-audition.txt"),
                verdict + "\nroots:\n  " + string.Join("\n  ", roots) + "\nbroken:\n  " + (bad.Count == 0 ? "(none)" : string.Join("\n  ", bad)) + "\n(generated " + System.DateTime.Now.ToString("s") + ")\n");
        }
    }
}
#endif
