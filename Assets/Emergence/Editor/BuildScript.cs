// EMERGENCE — batchmode build (BUILD-AUTOMATION §2).
// Green = exit 0 AND the player golden smoke passes (a build that renders but
// simulates wrong is not green).
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Emergence.Editor
{
    public static class BuildScript
    {
        private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        private const string BootstrapScene = "Assets/Emergence/Scenes/Bootstrap.unity";

        [MenuItem("Emergence/Build/Build Windows Player")]
        public static void BuildWindowsMenu() { BuildWindowsInternal(exitOnDone: false); }

        // batchmode entry: -executeMethod Emergence.Editor.BuildScript.BuildWindows
        public static void BuildWindows() { BuildWindowsInternal(exitOnDone: true); }

        private static void BuildWindowsInternal(bool exitOnDone)
        {
            try
            {
                SyncStreamingAssets();
                var scenes = new[] { BootstrapScene };
                var outDir = Path.Combine(ProjectRoot, "Builds", "EmergenceUnity");
                Directory.CreateDirectory(outDir);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = Path.Combine(outDir, "EmergenceUnity.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None,
                });
                var s = report.summary;
                var msg = $"[BuildScript] result={s.result} errors={s.totalErrors} warnings={s.totalWarnings} sizeMB={s.totalSize / (1024f * 1024f):F0} secs={s.totalTime.TotalSeconds:F0} out={s.outputPath}";
                Debug.Log(msg);
                File.AppendAllText(Path.Combine(ProjectRoot, "Builds", "build-report.txt"), DateTime.Now.ToString("s") + " " + msg + "\n");
                if (exitOnDone) EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
                else if (s.result != BuildResult.Succeeded) throw new Exception("Build failed: " + s.result);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildScript] " + e);
                if (exitOnDone) EditorApplication.Exit(1);
                else throw;
            }
        }

        // The engine/harness/goldens live canonically at Assets/Emergence/Engine/ (plan §4).
        // The player reads them from StreamingAssets — this sync keeps the two identical at build time.
        [MenuItem("Emergence/Build/Sync StreamingAssets (engine + harness)")]
        public static void SyncStreamingAssets()
        {
            var src = Path.Combine(Application.dataPath, "Emergence", "Engine");
            var dst = Path.Combine(Application.dataPath, "StreamingAssets", "Emergence");
            Directory.CreateDirectory(Path.Combine(dst, "harness"));
            CopyForce(Path.Combine(src, "emergence-engine.js"), Path.Combine(dst, "emergence-engine.js"));
            CopyForce(Path.Combine(src, "ENGINE-SHA.txt"), Path.Combine(dst, "ENGINE-SHA.txt"));
            CopyForce(Path.Combine(src, "harness", "harness.js"), Path.Combine(dst, "harness", "harness.js"));
            CopyForce(Path.Combine(src, "harness", "prelude-hypot.js"), Path.Combine(dst, "harness", "prelude-hypot.js"));
            AssetDatabase.Refresh();
            Debug.Log("[BuildScript] StreamingAssets synced from Assets/Emergence/Engine/");
        }


        // Mount-materialized files can carry the Windows read-only attribute; File.Copy(overwrite)
        // both fails on read-only destinations AND propagates the attribute. Normalize both sides.
        private static void CopyForce(string src, string dst)
        {
            if (File.Exists(dst)) File.SetAttributes(dst, FileAttributes.Normal);
            File.Copy(src, dst, true);
            File.SetAttributes(dst, FileAttributes.Normal);
        }

        [MenuItem("Emergence/Build/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var go = new GameObject("GoldenSmoke");
            go.AddComponent<Emergence.Runtime.GoldenSmoke>();
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Emergence", "Scenes"));
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, BootstrapScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScene, true) };
            Debug.Log("[BuildScript] Bootstrap scene created + set as build scene 0.");
        }

        // Spawns the detached queue script, then quits the editor so batchmode can take the project lock.
        // Same command line as BUILD-AUTOMATION §2, executed without EP hands.
        [MenuItem("Emergence/Build/Queue Batchmode Build + Smoke (quits editor)")]
        public static void QueueBatchmodeBuild()
        {
            var bat = Path.Combine(ProjectRoot, "Tools", "batch-build-queued.bat");
            if (!File.Exists(bat)) { Debug.LogError("Missing " + bat); return; }
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"" + bat + "\"",
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            System.Diagnostics.Process.Start(psi);
            Debug.Log("[BuildScript] Batchmode build queued — editor exits now; watch Builds/batch-queue.log");
            EditorApplication.Exit(0);
        }
    }
}
#endif
