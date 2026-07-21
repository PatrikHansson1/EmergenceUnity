// EMERGENCE — FAS 3 increment 5 PROBE (D-138): the WATCH LOOP runs in a REAL PLAYER BUILD.
//
// The point of increment 4 was that this becomes possible; this probe makes it FACT. Mechanism =
// A6PlayerPerf (D-130) + Fas3PlayerCadence (D-136) combined: dress the honest backdrop (terrain
// from the verified seq-8919-y006 snapshot), retire every born-from-state layer (the world must
// GROW in the player), add camera rig + gaze + Fas3PlayerWatch, save WatchScene.unity (gitignored
// like PerfScene), in-editor BuildPlayer, launch WITH graphics windowed. Inside the player the
// watch component proves: huts == canon, strict year order, pause-decoupling (producer races on),
// J3/J6 scrub from the checkpoint grid, magenta 0/0 on a rendered frame — writes watch-player.txt,
// saves watch-player-y6.png, quits. The probe reads both, copies evidence to Dropbox, verdicts.
// Menu: Emergence/Fas3/RUN PLAYER WATCH.  Headless: drop Reports/RUN_FAS3PWATCH.trigger.
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
    public static class Fas3PlayerWatchProbe
    {
        const string Backdrop = "Assets/Emergence/WorldStates/seq-8919-y006.json";
        const string ScenePath = "Assets/Emergence/Scenes/WatchScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";
        const long Seed = 8919;
        const int TargetYear = 6;

        static double _next;
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3PWATCH.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FAS3PWATCH_DONE.txt");
        static string OutDir    => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceWatch");
        static string ExePath   => Path.Combine(OutDir, "EmergenceWatch.exe");
        static string PlayerTxt => Path.Combine(OutDir, "watch-player.txt");
        static string PlayerPng => Path.Combine(OutDir, "watch-player-y6.png");
        const string Report     = "Reports/fas3-player-watch.txt";
        const string Evidence   = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas3";
        const string KeyWaiting = "emg.fas3pwatch.waiting", KeyStart = "emg.fas3pwatch.start", KeyReport = "emg.fas3pwatch.report";

        static Fas3PlayerWatchProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN PLAYER WATCH")]
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
            sb.AppendLine("EMERGENCE — FAS 3 PLAYER WATCH (D-138): the WATCH loop in a real player build");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // 1) honest backdrop: terrain only — every born-from-state layer retired, the PLAYER grows them
            WorldDresser.Build(Backdrop);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            int hutsExpected = -1;
            try { hutsExpected = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop)).huts?.Length ?? -1; } catch { }
            var S6 = JsonUtility.FromJson<WorldState>(File.ReadAllText(Backdrop));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S6.season) ? "spring" : S6.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3PWatch] look: " + e.Message); }
            try { A6Optimize.Run(); } catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3PWatch] optimize: " + e.Message); }
            try { var (dis, grp, bat, inst) = A6Instancing.ConvertOpenScene(); sb.AppendLine($"foliage instanced: {dis} renderers -> {bat} batches ({inst} instances)"); }
            catch (Exception e) { sb.AppendLine("instancing skipped: " + e.Message); }
            sb.AppendLine($"backdrop from seq-8919-y006, born layers retired; y{TargetYear} canon huts={hutsExpected}");

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var watch = new GameObject("Fas3PlayerWatch").AddComponent<Fas3PlayerWatch>();
            watch.seed = Seed; watch.targetYear = TargetYear; watch.expectedFinalHuts = hutsExpected;

            // 2) save the scene (terrain data is already an asset)
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { RestoreScene(); Fail("scene save failed"); return; }
            sb.AppendLine($"scene saved: {ScenePath} (gitignored, generated)");

            // 3) in-editor player build
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

            // 4) launch WITH graphics, windowed — the watch component drives, writes, quits
            try { if (File.Exists(PlayerTxt)) File.Delete(PlayerTxt); } catch {}
            try { if (File.Exists(PlayerPng)) File.Delete(PlayerPng); } catch {}
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player proving ~2-4 min) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 330.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no watch-player.txt within 330s"); return; }

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
                        File.Copy(PlayerPng, Path.Combine(Evidence, "fas3-player-watch-y6.png"), true);
                        evNote = "evidence: 45-UNITY/evidence/fas3/fas3-player-watch-y6.png (rendered IN the player)";
                    }
                }
                catch (Exception e) { evNote = "evidence copy failed: " + e.Message; }
                sb.AppendLine(evNote);

                bool green = player.Contains("hutsOk=OK") && player.Contains("orderOk=OK")
                          && player.Contains("decouple=OK") && player.Contains("J3:OK") && player.Contains("J6:OK")
                          && player.Contains("magenta=0/0") && player.Contains("COMPLETE");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the WATCH loop runs END-TO-END in a real player: the village is born, the hand holds time, the grid scrubs"
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
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3PWatch] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
