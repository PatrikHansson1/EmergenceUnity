// EMERGENCE — FAS 3 increment 7 PROBE (D-140): the OPENING runs in a REAL PLAYER BUILD.
//
// D-139 proved the game's start composes itself in the editor; this builds it into a player and
// lets Fas3OnboardPlayerProof observe the same beats there: genesis wilderness, first hut (gaze
// TAKEN), first child, unbroken order — plus the checkpoint grid exercised IN player (J0 to
// genesis and back to the frontier). Mechanism: the D-138 player pipeline (dress -> save scene ->
// in-editor BuildPlayer -> launch WITH graphics -> poll the player's txt -> copy evidence).
// The proof scene = the onboarding artifact + the observer (artifact itself stays observer-free).
// Menu: Emergence/Fas3/RUN PLAYER ONBOARD.  Headless: drop Reports/RUN_FAS3PONB.trigger.
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Jint;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas3PlayerOnboardProbe
    {
        const long Seed = 8919;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/OnboardProofScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";

        static double _next;
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3PONB.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FAS3PONB_DONE.txt");
        static string OutDir    => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceOnboard");
        static string ExePath   => Path.Combine(OutDir, "EmergenceOnboard.exe");
        static string PlayerTxt => Path.Combine(OutDir, "onboard-player.txt");
        static string PlayerPng => Path.Combine(OutDir, "onboard-player-firsthut.png");
        const string Report     = "Reports/fas3-player-onboard.txt";
        const string Evidence   = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
        const string KeyWaiting = "emg.fas3ponb.waiting", KeyStart = "emg.fas3ponb.start", KeyReport = "emg.fas3ponb.report";

        static Fas3PlayerOnboardProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN PLAYER ONBOARD")]
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
            sb.AppendLine("EMERGENCE — FAS 3 PLAYER ONBOARD (D-140): the opening in a real player build");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // genesis is fresh from D-139's run; parse the canon soul count from it
            int souls = -1;
            try { souls = JsonUtility.FromJson<WorldState>(File.ReadAllText(GenesisPath)).agents?.Length ?? -1; } catch { }
            if (souls < 0) { Fail("no genesis export — run the onboard probe (D-139) first"); return; }

            // the opening scene: wilderness + camera + Fas3Onboarding + THE OBSERVER
            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var G = JsonUtility.FromJson<WorldState>(File.ReadAllText(GenesisPath));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3POnb] look: " + e.Message); }
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;
            var proof = new GameObject("Fas3OnboardPlayerProof").AddComponent<Fas3OnboardPlayerProof>();
            proof.expectedGenesisSouls = souls;
            sb.AppendLine($"proof scene: genesis wilderness (souls={souls}) + Fas3Onboarding + observer");

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
            sb.AppendLine($"build: {s.result}, errors={s.totalErrors}, sizeMB={s.totalSize / (1024f * 1024f):F0}, secs={(DateTime.Now - t0).TotalSeconds:F0}");
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
            File.WriteAllText(Done, "RUNNING (player opening ~1-3 min) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 330.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no onboard-player.txt within 330s"); return; }

            try
            {
                System.Threading.Thread.Sleep(800);
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());
                sb.AppendLine();
                string evNote = "no evidence png from player";
                try
                {
                    if (File.Exists(PlayerPng))
                    {
                        Directory.CreateDirectory(Evidence);
                        File.Copy(PlayerPng, Path.Combine(Evidence, "fas3-player-onboard-firsthut.png"), true);
                        evNote = "evidence: 45-UNITY/evidence/fas3/fas3-player-onboard-firsthut.png (rendered IN the player)";
                    }
                }
                catch (Exception e) { evNote = "evidence copy failed: " + e.Message; }
                sb.AppendLine(evNote);

                bool green = player.Contains("genesis=OK") && player.Contains("firstHut=OK")
                          && player.Contains("firstChild=OK") && player.Contains("J0:OK") && player.Contains("Jf:OK")
                          && player.Contains("orderOk=OK") && player.Contains("magenta=0/0") && player.Contains("COMPLETE");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the game's opening runs in a real player: wilderness, the first hut under the eye, the grid scrubs to genesis and back"
                                                   : "CHECK — see player result above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")}\nsee {Report}\n");
            }
            catch (Exception e) { Fail("poll: " + e.Message); }
            finally { SessionState.SetInt(KeyWaiting, 0); }
        }

        static void RestoreScene()
        {
            try { EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3POnb] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
