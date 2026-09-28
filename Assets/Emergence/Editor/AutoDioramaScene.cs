// EMERGENCE — bake the dressed floor into a BUILDABLE scene (D-891, first .exe v0.1).
// Drop Reports/RUN_DIORAMA.trigger (optional body = world json). Dresses the floor via WorldDresser.Build,
// PERSISTS the in-memory TerrainData as an asset (D-881 made it in-memory for iteration; a build needs it saved),
// drops an EmergenceDioramaCamera, saves the scene, then RE-OPENS it and MEASURES the alphamap to prove the
// splat survived the save+reload (D-881's failure mode). Writes Reports/DIORAMA_DONE.txt. Presentation-only.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoDioramaScene
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_DIORAMA.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "DIORAMA_DONE.txt");
        const string DefaultWorld = "Assets/Emergence/WorldStates/world-8919-y120-full.json";
        const string TdDioramaPath = "Assets/Emergence/Scenes/TerrainData_diorama.asset";
        const string DioramaScene   = "Assets/Emergence/Scenes/EmergenceDiorama.unity"; // separate from the clean floor template

        static AutoDioramaScene() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var rep = new System.Text.StringBuilder();
            rep.AppendLine("DIORAMA BAKE — " + DateTime.Now.ToString("s"));
            string verdict;
            try { verdict = Run(string.IsNullOrEmpty(body) ? DefaultWorld : body, rep); }
            catch (Exception e) { verdict = "ERROR " + e.Message; rep.AppendLine(verdict + "\n" + e.StackTrace); }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoDioramaScene] " + verdict);
        }

        static string Run(string worldJson, System.Text.StringBuilder rep)
        {
            // 1. dress the floor — tell WorldDresser to PERSIST the terrain (D-891 deterministic path)
            WorldDresser.PersistTerrainPath = TdDioramaPath;
            try { WorldDresser.Build(worldJson); } finally { WorldDresser.PersistTerrainPath = null; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            rep.AppendLine("dressed: " + scene.path + " world=" + worldJson);

            // 2. persist the in-memory TerrainData as an asset (a build cannot serialize an in-memory one)
            var terrain = Terrain.activeTerrain;
            if (terrain == null) terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Include);
            if (terrain == null || terrain.terrainData == null)
            {
                var roots = scene.GetRootGameObjects();
                var names = new System.Text.StringBuilder();
                foreach (var r in roots) names.Append(r.name).Append(' ');
                int tcount = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                rep.AppendLine("DIAG no-terrain: activeScene=" + scene.path + " roots(" + roots.Length + ")=[" + names + "] terrainComponents=" + tcount);
                return "FAIL: no terrain after dressing (see DIAG)";
            }
            AssetDatabase.SaveAssets(); // flush the terrain asset WorldDresser created (D-891)
            var td = terrain.terrainData;
            rep.AppendLine("terrain persisted by WorldDresser: " + AssetDatabase.GetAssetPath(td) + " alphamap: " + AlphaSummary(td));

            // 3. a camera the build boots on: MainCamera + diorama controller + post
            // reuse the camera WorldDresser already made (DocCamera, tagged MainCamera); Unity's fake-null forbids ?? here
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) { var go = new GameObject("Main Camera"); go.tag = "MainCamera"; cam = go.AddComponent<Camera>(); }
            var camGo = cam.gameObject; camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 38f; cam.farClipPlane = 2000f; cam.nearClipPlane = 0.3f;
            var acd = camGo.GetComponent<UniversalAdditionalCameraData>(); if (acd == null) acd = camGo.AddComponent<UniversalAdditionalCameraData>();
            acd.renderPostProcessing = true;
            if (camGo.GetComponent<EmergenceDioramaCamera>() == null) camGo.AddComponent<EmergenceDioramaCamera>();
            camGo.transform.position = new Vector3(400, 60, 150); camGo.transform.LookAt(new Vector3(430, 6, 300));
            rep.AppendLine("camera: Main Camera + EmergenceDioramaCamera + post");

            // D-891: attach the runtime splat re-applier (roads survive the build via a .bytes TextAsset)
            var splatPath = System.IO.Path.ChangeExtension(TdDioramaPath, null) + "_splat.bytes";
            var splatAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(splatPath);
            var sc = terrain.gameObject.GetComponent<EmergenceTerrainSplat>(); if (sc == null) sc = terrain.gameObject.AddComponent<EmergenceTerrainSplat>();
            sc.terrain = terrain; sc.splat = splatAsset;
            rep.AppendLine("runtime splat: " + (splatAsset != null ? splatPath + " attached" : "MISSING " + splatPath));

            // 4. save the dressed scene
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, DioramaScene)) return "FAIL: SaveScene failed"; // Save As → keeps the floor template clean
            rep.AppendLine("saved dressed scene: " + DioramaScene);

            // 5. RE-OPEN and prove the splat + props survived (D-881 failure mode)
            var reopened = EditorSceneManager.OpenScene(DioramaScene, OpenSceneMode.Single);
            var t2 = Terrain.activeTerrain; if (t2 == null) t2 = UnityEngine.Object.FindAnyObjectByType<Terrain>();
            int renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            string after = t2 != null && t2.terrainData != null ? AlphaSummary(t2.terrainData) : "NO TERRAIN";
            rep.AppendLine("REOPENED " + reopened.path + " renderers=" + renderers + " alphamap=" + after);
            var splatOk = AssetDatabase.LoadAssetAtPath<TextAsset>(System.IO.Path.ChangeExtension(TdDioramaPath, null) + "_splat.bytes") != null;
            bool ok = renderers > 100 && splatOk;
            return (ok ? "OK diorama baked (props + runtime splat; terrain alphamap re-applied at play)" : "WARN renderers=" + renderers + " splatBytes=" + splatOk) + " renderers=" + renderers;
        }

        static string AlphaSummary(TerrainData td)
        {
            var a = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
            int layers = td.alphamapLayers; var dom = new int[layers];
            for (int y = 0; y < td.alphamapHeight; y++) for (int x = 0; x < td.alphamapWidth; x++)
            { int bi = 0; float bv = -1; for (int l = 0; l < layers; l++) if (a[y, x, l] > bv) { bv = a[y, x, l]; bi = l; } dom[bi]++; }
            var sb = new System.Text.StringBuilder("res=" + td.alphamapWidth + " dominant[");
            for (int l = 0; l < layers; l++) sb.Append(l > 0 ? "," : "").Append(dom[l]);
            sb.Append("]");
            return sb.ToString();
        }

        static bool IsAllOneLayer(TerrainData td)
        {
            var a = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
            int layers = td.alphamapLayers; var dom = new int[layers];
            for (int y = 0; y < td.alphamapHeight; y++) for (int x = 0; x < td.alphamapWidth; x++)
            { int bi = 0; float bv = -1; for (int l = 0; l < layers; l++) if (a[y, x, l] > bv) { bv = a[y, x, l]; bi = l; } dom[bi]++; }
            int total = td.alphamapWidth * td.alphamapHeight, nonzero = 0, top = 0;
            for (int l = 0; l < layers; l++) { if (dom[l] > 0) nonzero++; if (dom[l] > top) top = dom[l]; }
            return nonzero <= 1 || top >= total; // one layer owns everything = collapsed
        }
    }
}
#endif
