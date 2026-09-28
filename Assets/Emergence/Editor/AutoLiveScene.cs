// EMERGENCE — bake a LIVING scene into a BUILDABLE scene (D-896, väg A: the living world).
// Drop Reports/RUN_LIVESCENE.trigger (optional body = world json). Dresses the ENVIRONMENT only
// (WorldDresser.EnvironmentOnly — the live layers huts/fires/agents/codex are owned by the reconcilers,
// not placed statically, so they don't double), then assembles the PROVEN live rig: Fas3SimDriver
// (engine ticks live on a worker thread from genesis, StreamingAssets engine is packaged into builds) +
// Fas3WorldRuntime (reconciles agents/huts/fires/codex per year snapshot) + Fas3PresentationClock (the
// runtime consumer: pulls TakeYearSnapshot -> world.Apply each year) + Fas3TimeControls + the diorama
// camera. Persists the terrain + runtime splat like AutoDioramaScene. Saves EmergenceLive.unity, re-opens
// and confirms the rig + environment survived. Presentation-only; motor 5dd13837 untouched.
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
    public static class AutoLiveScene
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_LIVESCENE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "LIVESCENE_DONE.txt");
        const string DefaultWorld = "Assets/Emergence/WorldStates/world-8919-y120-full.json";
        const string TdLivePath   = "Assets/Emergence/Scenes/TerrainData_live.asset";
        const string LiveScene    = "Assets/Emergence/Scenes/EmergenceLive.unity";
        const long   Seed = 8919;

        static AutoLiveScene() { EditorApplication.update += Tick; }

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
            rep.AppendLine("LIVE SCENE BAKE — " + DateTime.Now.ToString("s"));
            string verdict;
            try { verdict = Run(string.IsNullOrEmpty(body) ? DefaultWorld : body, rep); }
            catch (Exception e) { verdict = "ERROR " + e.Message; rep.AppendLine(verdict + "\n" + e.StackTrace); }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoLiveScene] " + verdict);
        }

        static string Run(string worldJson, System.Text.StringBuilder rep)
        {
            // 1. dress the ENVIRONMENT only (live layers owned by the reconcilers) + persist the terrain
            WorldDresser.EnvironmentOnly = true;
            WorldDresser.PersistTerrainPath = TdLivePath;
            try { WorldDresser.Build(worldJson); }
            finally { WorldDresser.PersistTerrainPath = null; WorldDresser.EnvironmentOnly = false; }
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            rep.AppendLine("dressed ENVIRONMENT-only: " + scene.path + " world=" + worldJson);

            var terrain = Terrain.activeTerrain;
            if (terrain == null) terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>(FindObjectsInactive.Include);
            if (terrain == null || terrain.terrainData == null) return "FAIL: no terrain after dressing";
            AssetDatabase.SaveAssets();
            rep.AppendLine("terrain persisted: " + AssetDatabase.GetAssetPath(terrain.terrainData));

            // runtime splat re-applier (roads survive the build)
            var splatPath = Path.ChangeExtension(TdLivePath, null) + "_splat.bytes";
            var splatAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(splatPath);
            var sc = terrain.gameObject.GetComponent<EmergenceTerrainSplat>(); if (sc == null) sc = terrain.gameObject.AddComponent<EmergenceTerrainSplat>();
            sc.terrain = terrain; sc.splat = splatAsset;
            rep.AppendLine("runtime splat: " + (splatAsset != null ? "attached" : "MISSING " + splatPath));

            // 2. assemble the PROVEN live rig (all in Emergence.Runtime)
            var driverGo = new GameObject("Fas3SimDriver");
            var driver = driverGo.AddComponent<Fas3SimDriver>();
            driver.seed = Seed; driver.bufferMode = true; driver.targetYear = 150; driver.lookaheadYears = 16; // D-897: EXACT Fas3BufferProbe config (the proven live rig)
            var worldGo = new GameObject("Fas3WorldRuntime");
            var world = worldGo.AddComponent<Fas3WorldRuntime>();
            var clockGo = new GameObject("Fas3PresentationClock");
            var clock = clockGo.AddComponent<Fas3PresentationClock>(); clock.driver = driver; clock.world = world; clock.ticksPerSecond = Fas3TimeControls.BaseTps;
            var tcGo = new GameObject("Fas3TimeControls");
            var tc = tcGo.AddComponent<Fas3TimeControls>(); tc.driver = driver; tc.clock = clock;
            rep.AppendLine("rig: Fas3SimDriver(seed " + Seed + ", bufferMode, lookahead 16, targetYear 150) + WorldRuntime + PresentationClock(BaseTps) + TimeControls(driver+clock)");

            // 2b. the story organs (D-902, presentation-only, EXACT Fas3Onboarding pattern lines 73-97):
            // the chronicle feed (consumer #3 — self-subscribes to the PresentationEventBus in OnEnable,
            // reads the live reconcile path), its native UI-Toolkit face (disarms to IMGUI if UI assets
            // missing), the Latest Line (D-218: without it a 1x century-sim reads as MUTE — the whole
            // point of the living world), and the why-service (useProse defaults OFF -> rule-based why;
            // loading a model is Patriks screen-control step, never a headless bake).
            if (UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>() == null)
                new GameObject("Fas4ChronicleFeed").AddComponent<Fas4ChronicleFeed>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleView>() == null)
                new GameObject("Fas4ChronicleView").AddComponent<Fas4ChronicleView>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas4LatestLine>() == null)
                new GameObject("Fas4LatestLine").AddComponent<Fas4LatestLine>();
            if (UnityEngine.Object.FindAnyObjectByType<Emergence.Fas4.Fas4ProseDirector>() == null)
                new GameObject("Fas4ProseDirector").AddComponent<Emergence.Fas4.Fas4ProseDirector>();
            EmergenceUI.EnsureCursor();
            rep.AppendLine("story organs: Fas4ChronicleFeed + Fas4ChronicleView + Fas4LatestLine + Fas4ProseDirector(useProse OFF) + cursor");

            // 3. camera the build boots on (reuse WorldDresser's DocCamera; Unity fake-null forbids ??)
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

            // 4. save the live scene (Save As -> keeps the floor template clean)
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, LiveScene)) return "FAIL: SaveScene failed";
            rep.AppendLine("saved live scene: " + LiveScene);

            // 5. re-open and confirm the rig + environment survived the save
            var reopened = EditorSceneManager.OpenScene(LiveScene, OpenSceneMode.Single);
            int renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            bool hasDriver = UnityEngine.Object.FindAnyObjectByType<Fas3SimDriver>(FindObjectsInactive.Include) != null;
            bool hasWorld  = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>(FindObjectsInactive.Include) != null;
            bool hasClock  = UnityEngine.Object.FindAnyObjectByType<Fas3PresentationClock>(FindObjectsInactive.Include) != null;
            bool hasFeed   = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>(FindObjectsInactive.Include) != null;
            rep.AppendLine("REOPENED " + reopened.path + " renderers=" + renderers + " driver=" + hasDriver + " world=" + hasWorld + " clock=" + hasClock + " feed=" + hasFeed);
            bool ok = hasDriver && hasWorld && hasClock && hasFeed && renderers > 50;
            return (ok ? "OK live scene baked (rig + story organs present, environment dressed; run-mode movement + chronicle verified next)"
                       : "WARN rig=" + hasDriver + "/" + hasWorld + "/" + hasClock + " feed=" + hasFeed + " renderers=" + renderers);
        }
    }
}
#endif
