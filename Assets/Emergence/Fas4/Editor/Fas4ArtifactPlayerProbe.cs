// EMERGENCE — FAS 4 PROBE (D-148): the y120 CHRONICLE ARTIFACT in a REAL PLAYER BUILD.
//
// D-146 proved condition B in document form on y0..y90 in-editor and measured why the editor
// cannot carry the full window (~12+0.6·y s/year). This probe puts the artifact on the player
// vehicle (the D-138/D-140 pipeline): dress genesis -> compose the onboarding + the observer ->
// save scene -> in-editor BuildPlayer -> launch WITH graphics -> poll the player's txt (progress
// beats surfaced into the DONE file) -> copy the artifact pair into Reports/ + evidence to the
// studio. The observer (Fas4ArtifactPlayerProof) asserts the D-146 recalibrated spine at y120.
// Menu: Emergence/Fas4/RUN PLAYER ARTIFACT.  Headless: drop Reports/RUN_FAS4PART.trigger.
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
    public static class Fas4ArtifactPlayerProbe
    {
        const long Seed = 8919;
        const int TargetYear = 120;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/ArtifactProofScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";
        const double PollTimeout = 9900.0;   // observer watchdog 9000 s + build/boot margin (measured full window ≈ 4800 s, D-148)

        static double _next;
        static string Trigger    => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4PART.trigger");
        static string Done       => Path.Combine(Application.dataPath, "..", "Reports", "FAS4PART_DONE.txt");
        static string OutDir     => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceArtifact");
        static string ExePath    => Path.Combine(OutDir, "EmergenceArtifact.exe");
        static string PlayerTxt  => Path.Combine(OutDir, "artifact-player.txt");
        static string PlayerBeat => Path.Combine(OutDir, "artifact-player-beat.txt");
        static string PlayerArtTxt  => Path.Combine(OutDir, "chronicle-8919-y120.txt");
        static string PlayerArtHtml => Path.Combine(OutDir, "chronicle-8919-y120.html");
        static string PlayerPng  => Path.Combine(OutDir, "artifact-player-book.png");
        const string Report      = "Reports/fas4-player-artifact.txt";
        const string RepArtTxt   = "Reports/chronicle-8919-y120.txt";
        const string RepArtHtml  = "Reports/chronicle-8919-y120.html";
        const string RepPng      = "Reports/fas4-player-artifact-book.png";
        const string Evidence    = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas4";
        const string KeyWaiting = "emg.fas4part.waiting", KeyStart = "emg.fas4part.start", KeyReport = "emg.fas4part.report";

        static Fas4ArtifactPlayerProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN PLAYER ARTIFACT")]
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
            sb.AppendLine("EMERGENCE — FAS 4 PLAYER ARTIFACT (D-148): the full EA window's chronicle, exported by a real player");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}  seed={Seed}  window=y0..y{TargetYear}");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            Fas4UIAssetsBuild.Ensure();   // the native view's PanelSettings must exist as Resources BEFORE the build

            // the artifact scene: genesis wilderness + camera + Fas3Onboarding + THE OBSERVER
            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = TargetYear + 4;   // producer stops just past the window
            var proof = new GameObject("Fas4ArtifactPlayerProof").AddComponent<Fas4ArtifactPlayerProof>();
            proof.seed = Seed; proof.targetYear = TargetYear; proof.watchdogSecs = 9000f;
            sb.AppendLine("proof scene: genesis wilderness + Fas3Onboarding + Fas4 artifact observer");

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
            sb.AppendLine($"build: {s.result}, errors={s.totalErrors}, buildDirMB={s.totalSize / (1024f * 1024f):F0}, secs={(DateTime.Now - t0).TotalSeconds:F0}");
            string asmPath = Path.Combine(OutDir, "EmergenceArtifact_Data", "Managed", "Assembly-CSharp.dll");
            sb.AppendLine($"traceability: commit={GitSha()}, Assembly-CSharp.dll mtime={(File.Exists(asmPath) ? File.GetLastWriteTime(asmPath).ToString("yyyy-MM-dd HH:mm:ss") : "MISSING")}");
            RestoreScene();
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            { Fail("player build failed: " + s.result); return; }

            foreach (var p in new[] { PlayerTxt, PlayerBeat, PlayerArtTxt, PlayerArtHtml, PlayerPng })
                try { if (File.Exists(p)) File.Delete(p); } catch {}
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player racing the window ~15-40 min) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            double elapsed = EditorApplication.timeSinceStartup - start;
            if (!File.Exists(PlayerTxt))
            {
                // surface the player's own progress beat into the DONE file (stranger-readable)
                try
                {
                    if (File.Exists(PlayerBeat))
                        File.WriteAllText(Done, "RUNNING (player) " + File.ReadAllText(PlayerBeat).Trim() + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                }
                catch {}
                if (elapsed > PollTimeout) Fail($"player produced no artifact-player.txt within {PollTimeout:F0}s");
                return;
            }

            try
            {
                System.Threading.Thread.Sleep(800);
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());
                sb.AppendLine();

                int copied = 0;
                try
                {
                    if (File.Exists(PlayerArtTxt)) { File.Copy(PlayerArtTxt, RepArtTxt, true); copied++; }
                    if (File.Exists(PlayerArtHtml)) { File.Copy(PlayerArtHtml, RepArtHtml, true); copied++; }
                    if (File.Exists(PlayerPng)) { File.Copy(PlayerPng, RepPng, true); copied++; }
                    Directory.CreateDirectory(Evidence);
                    if (File.Exists(PlayerPng)) File.Copy(PlayerPng, Path.Combine(Evidence, "fas4-player-artifact-book.png"), true);
                }
                catch (Exception e) { sb.AppendLine("copy: " + e.Message); }
                sb.AppendLine($"artifacts: {copied}/3 copied -> {RepArtTxt} + {RepArtHtml} + {RepPng} (book png also to 45-UNITY/evidence/fas4)");

                bool green = player.Contains("span=OK") && player.Contains("spine=OK") && player.Contains("order=OK")
                          && player.Contains("artifact=OK") && player.Contains("evidence=OK") && player.Contains("COMPLETE")
                          && copied == 3;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — condition B on the FULL EA window, exported by the player vehicle: the chronicle is a document a stranger can read, produced by the shipping body"
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
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas4PArt] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
