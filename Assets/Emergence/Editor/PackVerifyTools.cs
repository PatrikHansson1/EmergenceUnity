// EMERGENCE — pack-import verification tooling (PACK-IMPORT-VERIFICATION-PLAN).
// Menu-driven, evidence straight to Dropbox 45-UNITY/evidence/pack-verify/.
// Includes the D-069 gotcha fix: disable ONLY the Lift Gamma Gain override in the
// packs' post profiles (broken in Unity 6/URP 17 — magenta cast), keep the rest.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Emergence.Editor
{
    public static class PackVerifyTools
    {
        private const string EvidenceDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\pack-verify";
        // Pack roots as imported (packs stay byte-identical to import — plan §4 principle)
        private static readonly string[] PackRoots = { "Assets/Fantastic Village Pack", "Assets/FANTASTIC - Village Pack", "Assets/Polyart", "Assets/Quaternius",
            // free-assets audition batch 2026-07-19 (TD-025)
            "Assets/Vefects", "Assets/msVFX_Free Smoke Effects Pack", "Assets/PolyOne", "Assets/UpDraftArt", "Assets/AllSkyFree",
            "Assets/Bitgem", "Assets/Houidisoft technology", "Assets/Procedural Water Shader", "Assets/Staggart Creations" };

        private static string[] ExistingRoots() =>
            PackRoots.Where(AssetDatabase.IsValidFolder).ToArray();

        [MenuItem("Emergence/Pack Verify/0. Report Pipeline + Pack Folders")]
        public static void ReportEnvironment()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Unity {Application.unityVersion}; RP asset: {(GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "Built-in!")}");
            var urp = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.universal");
            sb.AppendLine($"URP package: {(urp != null ? urp.version : "NOT FOUND")}");
            sb.AppendLine("Top-level Assets folders:");
            foreach (var d in Directory.GetDirectories(Application.dataPath)) sb.AppendLine("  " + Path.GetFileName(d));
            Debug.Log("[PackVerify]\n" + sb);
            WriteEvidence("environment.txt", sb.ToString());
        }

        // Row 3 (programmatic half): every material in the pack folders must have a working shader.
        [MenuItem("Emergence/Pack Verify/1. Zero-Pink Material Scan")]
        public static void ZeroPinkScan()
        {
            var roots = ExistingRoots();
            if (roots.Length == 0) { Debug.LogError("[PackVerify] no pack folders found — update PackRoots"); return; }
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
            var verdict = $"materials={guids.Length} broken={bad.Count} => {(bad.Count == 0 ? "ZERO PINK (programmatic) PASS" : "FAIL")}";
            var body = verdict + "\nroots: " + string.Join(", ", roots) + "\n" + string.Join("\n", bad);
            Debug.Log("[PackVerify] " + verdict);
            WriteEvidence("zero-pink-material-scan.txt", body);
        }

        // Row 3 (visual half): instantiate every prefab in a grid for the camera sweep.
        [MenuItem("Emergence/Pack Verify/2. Instantiate All Prefabs Grid")]
        public static void InstantiateGrid()
        {
            var roots = ExistingRoots();
            if (roots.Length == 0) { Debug.LogError("[PackVerify] no pack folders found"); return; }
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var guids = AssetDatabase.FindAssets("t:Prefab", roots);
            int cols = Mathf.CeilToInt(Mathf.Sqrt(guids.Length));
            float spacing = 8f;
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.position = new Vector3((i % cols) * spacing, 0, (i / cols) * spacing);
            }
            Debug.Log($"[PackVerify] grid: {guids.Length} prefabs, {cols}x{cols} @ {spacing}m — sweep with Capture");
        }

        // D-069 recipe, verbatim behavior from EmergenceTrackU.cs: disable the ONE broken override.
        [MenuItem("Emergence/Pack Verify/3. Disable Lift Gamma Gain Overrides (all profiles)")]
        public static void DisableLggOverrides()
        {
            var guids = AssetDatabase.FindAssets("t:VolumeProfile");
            var touched = new List<string>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
                if (profile != null && profile.TryGet<LiftGammaGain>(out var lgg) && lgg.active)
                {
                    lgg.active = false;
                    EditorUtility.SetDirty(profile);
                    touched.Add(path);
                }
            }
            AssetDatabase.SaveAssets();
            var body = $"LGG overrides disabled in {touched.Count} profile(s) (rest of each profile kept — D-069):\n" + string.Join("\n", touched);
            Debug.Log("[PackVerify] " + body);
            WriteEvidence("lgg-disable.txt", body);
        }

        [MenuItem("Emergence/Pack Verify/4. Capture Scene View (2560x1440)")]
        public static void Capture() { CaptureNamed("capture"); }

        public static void CaptureNamed(string name)
        {
            var cam = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null) { Debug.LogError("[PackVerify] no camera"); return; }
            const int w = 2560, h = 1440;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            // pixel-level magenta count (visual zero-pink evidence)
            var px = tex.GetPixels32();
            int magenta = px.Count(c => c.r > 220 && c.b > 220 && c.g < 80);
            Directory.CreateDirectory(EvidenceDir);
            var file = Path.Combine(EvidenceDir, $"{name}-{DateTime.Now:HHmmss}.png");
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"[PackVerify] captured {file} magentaPixels={magenta}/{w * h}");
            File.AppendAllText(Path.Combine(EvidenceDir, "capture-log.txt"), $"{Path.GetFileName(file)} magentaPixels={magenta}\n");
        }

        // Row 6: texture/import stats
        [MenuItem("Emergence/Pack Verify/5. Texture Budget Report")]
        public static void TextureReport()
        {
            var roots = ExistingRoots();
            var guids = AssetDatabase.FindAssets("t:Texture2D", roots);
            long bytes = 0; int count = 0; int over2k = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;
                count++;
                if (Mathf.Max(tex.width, tex.height) > 2048) over2k++;
                var fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));
                if (fi.Exists) bytes += fi.Length;
            }
            var body = $"textures={count} sourceBytesMB={bytes / (1024 * 1024)} over2K={over2k} (native res as authored — D-074: no re-binding, no WebGL budgets)";
            Debug.Log("[PackVerify] " + body);
            WriteEvidence("texture-budget.txt", body);
        }


        // Row 7: Quaternius rigs + clips, programmatic
        [MenuItem("Emergence/Pack Verify/6. Quaternius Rig Report")]
        public static void QuaterniusRigReport()
        {
            var sb = new StringBuilder();
            var guids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Quaternius" });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(c => !c.name.StartsWith("__preview__")).ToList();
                var hasRig = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().Any();
                sb.AppendLine($"{Path.GetFileName(path)}: clips={clips.Count} avatar={hasRig} [{string.Join(", ", clips.Select(c => c.name).Take(15))}]");
            }
            Debug.Log("[PackVerify] Quaternius:\n" + sb);
            WriteEvidence("quaternius-rigs.txt", sb.ToString());
        }

        private static void WriteEvidence(string file, string body)
        {
            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllText(Path.Combine(EvidenceDir, file), body + "\n(generated " + DateTime.Now.ToString("s") + ")\n");
        }
    }
}
#endif

// touch: audition-roots v2 (force recompile 2026-07-19)
