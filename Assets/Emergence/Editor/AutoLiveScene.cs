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
using UnityEngine.Rendering;
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
        const string VolumePath   = "Assets/Emergence/Scenes/EmergenceLiveVolume.asset";   // D-920: the post volume the live scene boots with
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

            // 2c. the sound (D-903, presentation-only, EXACT Fas3Onboarding pattern): the four audio
            // layers — event stingers (Fas3AudioDirector, L3), era bed (Fas6EraAmbience, L1), state
            // activity + fire point-sources (Fas6StateAmbience, L2), and the written score over them
            // (Fas6MusicDirector, L4: era->ambient, drama->viking action, deterministic per-state cue,
            // level-matched to -24 dBFS, crossfade 4s / action-hold 20s). Each disarms itself if its
            // catalog/pack is missing (the ear never goes black). Reads applied state, never the sim.
            if (UnityEngine.Object.FindAnyObjectByType<Fas3AudioDirector>() == null)
                new GameObject("Fas3AudioDirector").AddComponent<Fas3AudioDirector>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas6EraAmbience>() == null)
                new GameObject("Fas6EraAmbience").AddComponent<Fas6EraAmbience>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas6StateAmbience>() == null)
                new GameObject("Fas6StateAmbience").AddComponent<Fas6StateAmbience>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas6MusicDirector>() == null)
                new GameObject("Fas6MusicDirector").AddComponent<Fas6MusicDirector>();
            rep.AppendLine("sound layers: Fas3AudioDirector + Fas6EraAmbience + Fas6StateAmbience + Fas6MusicDirector");

            // 2d. the opening (D-905): a title + controls hint that fades — the build announces itself
            // and teaches its own interactivity. Pure OnGUI overlay (Emergence.Runtime), disarms after the fade.
            if (UnityEngine.Object.FindAnyObjectByType<EmergenceIntro>() == null)
                new GameObject("EmergenceIntro").AddComponent<EmergenceIntro>();
            rep.AppendLine("opening: EmergenceIntro (title + controls hint, fades)");
            // 2e. the epoch line (D-907): a quiet persistent "Year N · Era" at top — orientation for a
            // century-scale world. Reads applied state (era) + clock (year); pure OnGUI overlay.
            if (UnityEngine.Object.FindAnyObjectByType<EmergenceEraHud>() == null)
                new GameObject("EmergenceEraHud").AddComponent<EmergenceEraHud>();
            rep.AppendLine("epoch line: EmergenceEraHud (Year N · Era)");

            // 2f. the patterns + panel keys (D-911): the Fas 5 almanac (metrics recorder + native
            // overview) was raised by Fas3Onboarding but never by this manual rig, so the built game
            // had no almanac. Add it (self-wires, disarms to nothing if UI assets missing) plus the
            // reading-panel hotkeys (B book, M almanac, Esc close). Presentation-only.
            if (UnityEngine.Object.FindAnyObjectByType<Fas5MetricsRecorder>() == null)
                new GameObject("Fas5MetricsRecorder").AddComponent<Fas5MetricsRecorder>();
            if (UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>() == null)
                new GameObject("Fas5AlmanacView").AddComponent<Fas5AlmanacView>();
            if (UnityEngine.Object.FindAnyObjectByType<EmergenceHotkeys>() == null)
                new GameObject("EmergenceHotkeys").AddComponent<EmergenceHotkeys>();
            rep.AppendLine("patterns+keys: Fas5MetricsRecorder + Fas5AlmanacView + EmergenceHotkeys (B/M/Esc)");

            // 3. camera the build boots on (reuse WorldDresser's DocCamera; Unity fake-null forbids ??)
            var cam = Camera.main;
            if (cam == null) cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) { var go = new GameObject("Main Camera"); go.tag = "MainCamera"; cam = go.AddComponent<Camera>(); }
            var camGo = cam.gameObject; camGo.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 38f; cam.farClipPlane = 2000f; cam.nearClipPlane = 0.3f;
            var acd = camGo.GetComponent<UniversalAdditionalCameraData>(); if (acd == null) acd = camGo.AddComponent<UniversalAdditionalCameraData>();
            acd.renderPostProcessing = true;
            if (camGo.GetComponent<EmergenceDioramaCamera>() == null) camGo.AddComponent<EmergenceDioramaCamera>();
            // the living gaze (D-904, D-134/D-139): the documentary eye that notices life — glides down
            // to frame a hut being raised or a child born (PresentationEventBus), holds a beat, releases.
            // No conflict with the diorama: it writes in LateUpdate (after the orbit's Update), so the
            // gaze wins while it has a target and the orbit resumes when it lets go. Presentation-only.
            if (camGo.GetComponent<Fas3GazeDirector>() == null) camGo.AddComponent<Fas3GazeDirector>();
            camGo.transform.position = new Vector3(400, 60, 150); camGo.transform.LookAt(new Vector3(430, 6, 300));
            rep.AppendLine("camera: Main Camera + EmergenceDioramaCamera + Fas3GazeDirector (living gaze) + post");

            // 3b. D-920 (review D-919): the live scene had NO Volume — renderPostProcessing was on, but nothing to
            // process. VISUAL-BIBLE: ACES, bloom tuned to the ONE warm point (the fires), a light vignette. The
            // profile is built here in code so the bake owns it; feel numbers are Patrik's eye pass.
            var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);
            if (prof == null) { prof = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(prof, VolumePath); }
            if (!prof.TryGet<Tonemapping>(out var tone)) tone = prof.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            if (!prof.TryGet<Bloom>(out var bloom)) bloom = prof.Add<Bloom>(true);
            bloom.threshold.Override(0.95f); bloom.intensity.Override(0.5f); bloom.scatter.Override(0.65f);
            if (!prof.TryGet<Vignette>(out var vig)) vig = prof.Add<Vignette>(true);
            vig.intensity.Override(0.18f); vig.smoothness.Override(0.6f);
            EditorUtility.SetDirty(prof);
            var volGo = GameObject.Find("PostVolume") ?? new GameObject("PostVolume");
            var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 0; vol.sharedProfile = prof;
            rep.AppendLine("post: global Volume (ACES + bloom thr 0.95/int 0.5 + vignette 0.18) -> " + VolumePath);

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
            bool hasMusic  = UnityEngine.Object.FindAnyObjectByType<Fas6MusicDirector>(FindObjectsInactive.Include) != null;
            bool hasGaze   = UnityEngine.Object.FindAnyObjectByType<Fas3GazeDirector>(FindObjectsInactive.Include) != null;
            rep.AppendLine("REOPENED " + reopened.path + " renderers=" + renderers + " driver=" + hasDriver + " world=" + hasWorld + " clock=" + hasClock + " feed=" + hasFeed + " music=" + hasMusic + " gaze=" + hasGaze);
            bool ok = hasDriver && hasWorld && hasClock && hasFeed && hasMusic && hasGaze && renderers > 50;
            return (ok ? "OK live scene baked (rig + story organs present, environment dressed; run-mode movement + chronicle verified next)"
                       : "WARN rig=" + hasDriver + "/" + hasWorld + "/" + hasClock + " feed=" + hasFeed + " renderers=" + renderers);
        }
    }
}
#endif
