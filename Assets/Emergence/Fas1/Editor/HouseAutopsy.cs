// EMERGENCE — VÅG 1.2: WHY ARE THE HOUSES WHITE? A prefab autopsy.
//
// Four hypotheses have already been raised and falsified by measurement (D-211): not Unity's default
// material, not over-exposure, not a missing texture file, not a stale import. What survives is the
// probe's own blind spot — it read sharedMaterial, which is SLOT 0 ONLY. A house is a multi-material
// mesh; a white slot 1 would have been invisible to every check so far.
//
// So this opens the prefab itself and reports EVERY renderer, EVERY material slot, whether that slot
// has a base texture, and whether the mesh even has UVs to sample one with. No play mode, no scene,
// no guessing.
// Headless: drop Reports/RUN_HOUSEAUTOPSY.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class HouseAutopsy
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_HOUSEAUTOPSY.trigger");
        const string Report = "Reports/house-autopsy.txt";

        static HouseAutopsy() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas1/HOUSE AUTOPSY")]
        public static void RunMenu() => Run();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run();
            }
            catch (Exception e) { Debug.LogWarning("[HouseAutopsy] " + e.Message); }
        }

        static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — house autopsy: every renderer, EVERY material slot");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("the blind spot this closes: earlier checks read sharedMaterial = SLOT 0 only.");
            sb.AppendLine();

            int slotsNoTex = 0, slotsTotal = 0, meshesNoUV = 0, meshesTotal = 0;
            var offenders = new StringBuilder();

            foreach (var path in new[]
            {
                "Assets/Fantastic Village Pack/prefabs/buildings/P_BLD_house_01.prefab",
                "Assets/Fantastic Village Pack/prefabs/buildings/P_BLD_house_02.prefab",
                "Assets/Fantastic Village Pack/prefabs/buildings_modules/P_BLD_body_v01_01.prefab",
            })
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                sb.AppendLine("### " + Path.GetFileNameWithoutExtension(path) + (pf == null ? "   NOT FOUND" : ""));
                if (pf == null) { sb.AppendLine(); continue; }

                foreach (var r in pf.GetComponentsInChildren<Renderer>(true))
                {
                    var mats = r.sharedMaterials;
                    var mf = r.GetComponent<MeshFilter>();
                    var mesh = mf != null ? mf.sharedMesh : null;
                    if (mesh != null)
                    {
                        meshesTotal++;
                        bool hasUV = mesh.uv != null && mesh.uv.Length > 0;
                        if (!hasUV) { meshesNoUV++; offenders.AppendLine("   MESH WITHOUT UVs: " + r.name + " / " + mesh.name); }
                        sb.AppendLine("  " + r.name.PadRight(24) + " mesh=" + mesh.name
                                      + "  verts=" + mesh.vertexCount + "  subMeshes=" + mesh.subMeshCount
                                      + "  uv=" + (hasUV ? mesh.uv.Length.ToString() : "NONE"));
                    }
                    else sb.AppendLine("  " + r.name.PadRight(24) + " (no MeshFilter — " + r.GetType().Name + ")");

                    for (int i = 0; i < mats.Length; i++)
                    {
                        slotsTotal++;
                        var m = mats[i];
                        if (m == null) { slotsNoTex++; sb.AppendLine("      slot " + i + ": NULL MATERIAL"); offenders.AppendLine("   NULL MATERIAL: " + r.name + " slot " + i); continue; }
                        Texture t = null;
                        if (m.HasProperty("_BaseMap")) t = m.GetTexture("_BaseMap");
                        if (t == null && m.HasProperty("_MainTex")) t = m.GetTexture("_MainTex");
                        Color bc = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : (m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                        if (t == null) { slotsNoTex++; offenders.AppendLine("   NO TEXTURE: " + r.name + " slot " + i + " -> " + m.name + " (" + m.shader.name + ")  baseColor=" + bc); }
                        sb.AppendLine("      slot " + i + ": " + m.name.PadRight(22) + " " + m.shader.name.PadRight(38)
                                      + " tex=" + (t != null ? t.name : "*** NONE ***")
                                      + "  baseColor=" + bc);
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("SUMMARY");
            sb.AppendLine("  material slots inspected: " + slotsTotal + ", WITHOUT a base texture: " + slotsNoTex);
            sb.AppendLine("  meshes inspected: " + meshesTotal + ", WITHOUT UVs: " + meshesNoUV);
            sb.AppendLine();
            if (offenders.Length > 0) { sb.AppendLine("OFFENDERS"); sb.Append(offenders); }
            else sb.AppendLine("No offender found in the prefabs — the white must come from somewhere else.");

            // ---- THE ISOLATION SHOT ----
            // The prefabs are clean, so the white must come from the scene or the pipeline. Render ONE
            // house alone, in a controlled editor-mode shot, and read the pixels back. If it looks right
            // here the fault is scene-level; if it is white here too it is asset/pipeline-level.
            // Measured, not eyeballed: the roof texture averages a DARK teal (51,101,114) and the wood a
            // dark brown (99,43,0) — so a pale render is proof the texture is not being sampled.
            try
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Fantastic Village Pack/prefabs/buildings/P_BLD_house_01.prefab");
                if (src != null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                    go.transform.position = Vector3.zero;
                    var camGo = new GameObject("AutopsyCam");
                    var cam = camGo.AddComponent<Camera>();
                    var b = new Bounds(go.transform.position, Vector3.one);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    float dist = b.size.magnitude * 1.1f;
                    camGo.transform.position = b.center + new Vector3(dist * 0.7f, dist * 0.45f, dist * 0.7f);
                    camGo.transform.LookAt(b.center);
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.15f, 0.35f, 0.55f);   // a blue that is NOT in the pack
                    var sunGo = new GameObject("AutopsySun");
                    var sun = sunGo.AddComponent<Light>();
                    sun.type = LightType.Directional; sun.intensity = 1.0f; sun.color = Color.white;
                    sunGo.transform.rotation = Quaternion.Euler(45f, 335f, 0f);
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.38f);
                    RenderSettings.fog = false;

                    const int w = 1280, h = 720;
                    var rt = new RenderTexture(w, h, 24);
                    cam.targetTexture = rt; cam.Render();
                    RenderTexture.active = rt;
                    var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                    cam.targetTexture = null; RenderTexture.active = null;
                    File.WriteAllBytes("Reports/house-isolated.png", tex.EncodeToPNG());

                    // read the HOUSE pixels only (anything that is not the blue backdrop)
                    var px = tex.GetPixels32();
                    double rr = 0, gg = 0, bb = 0; int n = 0;
                    foreach (var c in px)
                    {
                        bool backdrop = c.b > c.r + 20 && c.b > 60 && c.r < 90;
                        if (backdrop) continue;
                        rr += c.r; gg += c.g; bb += c.b; n++;
                    }
                    sb.AppendLine();
                    sb.AppendLine("ISOLATION SHOT (Reports/house-isolated.png)");
                    if (n > 0)
                    {
                        sb.AppendLine("  house pixels: " + n + "   mean RGB = (" + (rr / n).ToString("F0") + ", " + (gg / n).ToString("F0") + ", " + (bb / n).ToString("F0") + ")");
                        sb.AppendLine("  for comparison, the source textures average: wall (233,206,150) cream, rooftiles (51,101,114) DARK TEAL, wood (99,43,0) dark brown");
                        double lum = (rr + gg + bb) / (3.0 * n);
                        sb.AppendLine("  VERDICT: " + (lum > 200 ? "WASHED OUT — textures are NOT being sampled" : "textures ARE sampling (the house is not white in isolation) -> the fault is scene-level"));
                    }
                    else sb.AppendLine("  no house pixels found (framing failed)");

                    UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
                    UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(sunGo); UnityEngine.Object.DestroyImmediate(go);
                }
            }
            catch (Exception e) { sb.AppendLine("isolation shot FAILED: " + e.Message); }

            Directory.CreateDirectory("Reports");
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Reports", "HOUSEAUTOPSY_DONE.txt"),
                              "DONE slotsNoTex=" + slotsNoTex + " meshesNoUV=" + meshesNoUV + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Debug.Log("[HouseAutopsy] -> " + Report);
        }
    }
}
#endif
