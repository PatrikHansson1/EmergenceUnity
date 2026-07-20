// TD-034: STUDY THE PACK REFERENCE SCENES. The EP: "miljön ser dålig ut." Root cause — we never
// used the packs' own environment treatment. This opens Dreamscape's Showcase/Demo, captures them as
// they ship (the reference quality, in OUR project), and DUMPS their setup (terrain detail grass,
// terrain layers/material, post volume, lighting, water) to Logs/showcase-study.txt so we can replicate it.
#if UNITY_EDITOR
using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emergence.Editor
{
    public static class StudyShowcase
    {
        const string EVID = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\audition-lightrig";

        [MenuItem("Emergence/P1 Dressing/STUDY Dreamscape Showcase (capture + dump config)")]
        public static void Study()
        {
            var scenes = new[] {
                "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Scenes/Showcase.unity",
                "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Scenes/Demo.unity",
            };
            var sb = new StringBuilder();
            foreach (var scenePath in scenes)
            {
                if (!File.Exists(scenePath)) { sb.AppendLine("MISSING: " + scenePath); continue; }
                var tag = Path.GetFileNameWithoutExtension(scenePath);
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                sb.AppendLine("========== " + tag + " ==========");

                // capture via the scene's own main camera (their framing + lighting + post)
                var cam = Object.FindFirstObjectByType<Camera>();
                if (cam != null)
                {
                    var rt = new RenderTexture(2560, 1440, 24);
                    var prev = cam.targetTexture; cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(2560, 1440, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 2560, 1440), 0, 0); tex.Apply();
                    Directory.CreateDirectory(EVID);
                    File.WriteAllBytes(Path.Combine(EVID, "REF-" + tag + ".png"), tex.EncodeToPNG());
                    cam.targetTexture = prev; RenderTexture.active = null;
                    Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
                    sb.AppendLine($"camera: fov={cam.fieldOfView} pos={cam.transform.position} euler={cam.transform.eulerAngles} clear={cam.clearFlags}");
                }

                // terrain — the heart of the look
                var terrain = Object.FindFirstObjectByType<Terrain>();
                if (terrain != null)
                {
                    var td = terrain.terrainData;
                    sb.AppendLine($"TERRAIN material={(terrain.materialTemplate ? terrain.materialTemplate.shader.name : "NULL")} drawInstanced={terrain.drawInstanced} detailDist={terrain.detailObjectDistance} detailDensity={terrain.detailObjectDensity} basemapDist={terrain.basemapDistance}");
                    sb.AppendLine($"  terrainLayers: {string.Join(", ", td.terrainLayers.Select(l => l ? l.name : "null"))}");
                    sb.AppendLine($"  detailPrototypes ({td.detailPrototypes.Length}) = DETAIL GRASS:");
                    foreach (var dp in td.detailPrototypes)
                        sb.AppendLine($"    render={dp.renderMode} proto={(dp.prototype ? dp.prototype.name : "-")} tex={(dp.prototypeTexture ? dp.prototypeTexture.name : "-")} healthyColor={dp.healthyColor} minW={dp.minWidth} maxW={dp.maxWidth} minH={dp.minHeight} maxH={dp.maxHeight} noiseSpread={dp.noiseSpread} usePrototypeMesh={dp.usePrototypeMesh}");
                    sb.AppendLine($"  wavingGrass: strength={td.wavingGrassStrength} speed={td.wavingGrassSpeed} amount={td.wavingGrassAmount} tint={td.wavingGrassTint}");
                    sb.AppendLine($"  treePrototypes ({td.treePrototypes.Length}): {string.Join(", ", td.treePrototypes.Select(t => t.prefab ? t.prefab.name : "-"))}");
                }
                else sb.AppendLine("TERRAIN: none (they may use mesh ground)");

                // post volume
                var vol = Object.FindFirstObjectByType<Volume>();
                if (vol != null && vol.profile != null)
                    sb.AppendLine($"POST volume '{vol.name}' overrides: {string.Join(", ", vol.profile.components.Select(c => c.GetType().Name))}");
                else sb.AppendLine("POST: no Volume found");

                // lighting
                var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
                if (sun != null) sb.AppendLine($"SUN: color={sun.color} intensity={sun.intensity} euler={sun.transform.eulerAngles} shadows={sun.shadows}");
                sb.AppendLine($"ambient: mode={RenderSettings.ambientMode} sky={RenderSettings.ambientSkyColor} fog={RenderSettings.fog} fogColor={RenderSettings.fogColor}");

                // water objects (by name)
                var waters = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(r => r.sharedMaterial != null && (r.sharedMaterial.name.ToLower().Contains("water") || r.name.ToLower().Contains("water") || r.name.ToLower().Contains("lake") || r.name.ToLower().Contains("river")))
                    .Select(r => r.name + " [" + (r.sharedMaterial ? r.sharedMaterial.name : "-") + "]").Distinct().Take(10);
                sb.AppendLine("WATER objects: " + string.Join(", ", waters));
                sb.AppendLine();
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/showcase-study.txt", sb.ToString());
            Debug.Log("[StudyShowcase] done — REF captures in evidence, config in Logs/showcase-study.txt\n" + sb.ToString());
        }
    }
}
#endif
