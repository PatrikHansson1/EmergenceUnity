// EMERGENCE — FAS 7 increment 1 PROBE: SAVE/LOAD runs in a REAL PLAYER BUILD.
//
// The editor probe proved the mechanism; this builds the D-138 player pipeline around
// Fas7SaveLoadPlayerProof: dress genesis wilderness -> save proof scene (wilderness + camera +
// observer; the observer composes/tears down the onboarding boots itself) -> in-editor BuildPlayer
// -> launch WITH graphics -> poll saveload-player.txt -> copy evidence. GREEN requires the full
// player line: save OK, grid wiped, SHA match, loaded world, mode restored, chronicle clean,
// lives on, evidence framed by the shared law, magenta 0/0, COMPLETE.
// Menu: Emergence/Fas7/RUN PLAYER SAVELOAD.  Headless: drop Reports/RUN_FAS7PSAVE.trigger.
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas7SaveLoadPlayerProbe
    {
        const long Seed = 8919;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/SaveLoadProofScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";

        static double _next;
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7PSAVE.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FAS7PSAVE_DONE.txt");
        static string OutDir    => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceSaveLoad");
        static string ExePath   => Path.Combine(OutDir, "EmergenceSaveLoad.exe");
        static string PlayerTxt => Path.Combine(OutDir, "saveload-player.txt");
        static string PlayerPng => Path.Combine(OutDir, "saveload-player.png");
        const string Report     = "Reports/fas7-saveload-player.txt";
        const string Evidence   = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas7";
        const string KeyWaiting = "emg.fas7psave.waiting", KeyStart = "emg.fas7psave.start", KeyReport = "emg.fas7psave.report";

        static Fas7SaveLoadPlayerProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN PLAYER SAVELOAD")]
        public static void RunMenu() => Run();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (SessionState.GetInt(KeyWaiting, 0) == 0 && File.Exists(Trigger))
                {
                    File.Delete(Trigger);
                    Run();
                    return;
                }
                if (SessionState.GetInt(KeyWaiting, 0) == 1) Poll();
            }
            catch (Exception e) { Fail("tick: " + e.Message); }
        }

        static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 7 PLAYER SAVELOAD: save/load A7 sharp inside a real player build");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // proof scene: wilderness + camera; the OBSERVER composes both boots at runtime
            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var G = JsonUtility.FromJson<WorldState>(File.ReadAllText(GenesisPath));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7PSave] look: " + e.Message); }
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var proof = new GameObject("Fas7SaveLoadPlayerProof").AddComponent<Fas7SaveLoadPlayerProof>();
            proof.seed = Seed; proof.saveYear = 6;
            sb.AppendLine("proof scene: genesis wilderness + camera + Fas7SaveLoadPlayerProof (observer composes the boots)");

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { RestoreScene(); Fail("scene save failed"); return; }

            File.WriteAllText(Done, "RUNNING (building player — minutes) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Directory.CreateDirectory(OutDir);
            var t0 = DateTime.Now;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            sb.AppendLine($"build: {s.result}, errors={s.totalErrors}, buildDirMB={s.totalSize / (1024f * 1024f):F0} (exe stub {(File.Exists(ExePath) ? new FileInfo(ExePath).Length / 1024 : -1)} KB), secs={(DateTime.Now - t0).TotalSeconds:F0}");
            string asmPath = Path.Combine(OutDir, "EmergenceSaveLoad_Data", "Managed", "Assembly-CSharp.dll");
            sb.AppendLine($"traceability: commit={GitSha()}, Assembly-CSharp.dll mtime={(File.Exists(asmPath) ? File.GetLastWriteTime(asmPath).ToString("yyyy-MM-dd HH:mm:ss") : "MISSING")} (exe stub is launcher-only, not rewritten incrementally)");
            RestoreScene();
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            { Fail("player build failed: " + s.result); return; }

            try { if (File.Exists(PlayerTxt)) File.Delete(PlayerTxt); } catch {}
            try { if (File.Exists(PlayerPng)) File.Delete(PlayerPng); } catch {}
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player save/load ~2-5 min) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 420.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no saveload-player.txt within 420s"); return; }

            try
            {
                System.Threading.Thread.Sleep(800);
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());
                sb.AppendLine();
                string evNote;
                try
                {
                    Directory.CreateDirectory(Evidence);
                    if (File.Exists(PlayerPng)) { File.Copy(PlayerPng, Path.Combine(Evidence, "fas7-saveload-player.png"), true); evNote = "evidence: saveload-player.png -> 45-UNITY/evidence/fas7/fas7-saveload-player.png (rendered IN the player)"; }
                    else evNote = "evidence: NO png produced";
                }
                catch (Exception e) { evNote = "evidence copy failed: " + e.Message; }
                sb.AppendLine(evNote);

                bool green = player.Contains("save=OK") && player.Contains("resim=OK") && player.Contains("shaMatch=OK")
                          && player.Contains("loaded=OK") && player.Contains("mode=OK") && player.Contains("feedNew=OK")
                          && player.Contains("liveOn=OK") && player.Contains("evidence=OK")
                          && player.Contains("magenta=0/0") && player.Contains("COMPLETE")
                          && evNote.Contains("rendered IN the player");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — save->load reproduces the world exactly INSIDE a real player (SHA-proven) and lives on"
                                                   : "CHECK — see player result above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")}\nsee {Report}\n");
            }
            catch (Exception e) { Fail("poll: " + e.Message); }
            finally { SessionState.SetInt(KeyWaiting, 0); }
        }

        static string GitSha()
        {
            try
            {
                string root = Path.GetDirectoryName(Application.dataPath);
                string head = File.ReadAllText(Path.Combine(root, ".git", "HEAD")).Trim();
                if (head.StartsWith("ref: "))
                {
                    string refPath = Path.Combine(root, ".git", head.Substring(5).Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(refPath)) return File.ReadAllText(refPath).Trim().Substring(0, 8) + " (" + head.Substring(5) + ")";
                    return "unresolved-ref " + head.Substring(5);
                }
                return head.Substring(0, 8);
            }
            catch (Exception e) { return "unknown (" + e.Message + ")"; }
        }

        static void RestoreScene()
        {
            try { EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7PSave] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
