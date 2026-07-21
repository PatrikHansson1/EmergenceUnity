// EMERGENCE — A6 FINAL VERDICT (D-130): the definitive FPS measurement on a REAL PLAYER BUILD.
// The editor numbers (draw calls ~3150, set-pass ~220) are proxies distorted by editor overhead;
// the A6 gate criterion is player FPS (D-107/D-119). This runner, WITHOUT quitting the editor:
//   1) builds the full living y120 scene (dress + live codex + 111 live agents + living animals +
//      A6 levers + foliage instancing), frames the doc camera at village eye height, adds PerfRun,
//   2) SAVES it as Assets/Emergence/Scenes/PerfScene.unity (TerrainData is already an asset),
//   3) BuildPipeline.BuildPlayer -> Builds/EmergencePerf/ (in-editor build keeps the trigger infra alive),
//   4) launches the player windowed 1600x900 WITH graphics; PerfRun samples 20 s, writes
//      perf-player.txt beside the exe and quits; the runner polls, verdicts vs the 60-fps floor,
//   5) restores SampleScene as the open scene.
// Menu: Emergence/Fas1/RUN A6 PLAYER PERF.  Headless: drop Reports/RUN_A6PLAYER.trigger.
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
    public static class A6PlayerPerf
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string ScenePath = "Assets/Emergence/Scenes/PerfScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";
        const int TargetFps = 60;

        static double _next;
        static string Trigger  => Path.Combine(Application.dataPath, "..", "Reports", "RUN_A6PLAYER.trigger");
        static string Done     => Path.Combine(Application.dataPath, "..", "Reports", "A6PLAYER_DONE.txt");
        static string OutDir   => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergencePerf");
        static string ExePath  => Path.Combine(OutDir, "EmergencePerf.exe");
        static string PlayerTxt => Path.Combine(OutDir, "perf-player.txt");
        const string Report    = "Reports/a6-player-perf.txt";
        const string KeyWaiting = "emg.a6player.waiting", KeyStart = "emg.a6player.start", KeyReport = "emg.a6player.report";

        static A6PlayerPerf() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas1/RUN A6 PLAYER PERF")]
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
            sb.AppendLine("EMERGENCE — A6 PLAYER PERF (D-130): the definitive FPS number");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // 1) the full living scene (same recipe the probes proved GREEN)
            WorldDresser.Build(World120);
            var sc = GameObject.Find("CodexObjects"); if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
            var sa = GameObject.Find("Agents");       if (sa != null) UnityEngine.Object.DestroyImmediate(sa);
            var S120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            PresentationEventBus.Clear();
            new LiveReconciler().Reconcile(S120);
            var agents = new AgentReconciler(); agents.Reconcile(S120, true);
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S120.season) ? "spring" : S120.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[A6Player] look: " + e.Message); }
            A6Optimize.Run();
            var (disabled, groups, batches, instances) = A6Instancing.ConvertOpenScene();
            sb.AppendLine($"scene: souls={agents.Count}, foliage converted: {disabled} renderers -> {batches} cell-batches ({instances} instances, {groups} groups)");

            // camera: village cluster at eye height (PerfRun yaws it across village + meadow)
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            Vector3 center = VillageCenter();
            cam.transform.position = center + new Vector3(9f, 5.5f, -13f);
            cam.transform.LookAt(center + Vector3.up * 1.0f);
            var runGo = new GameObject("PerfRun");
            runGo.AddComponent<PerfRun>();
            sb.AppendLine($"camera at village cluster {center}, PerfRun added (5s warm-up + 20s sample, yaw 14 deg/s)");

            // 2) save the scene (terrain data is already an asset: TerrainData_generated.asset)
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { Fail("scene save failed"); return; }
            sb.AppendLine($"scene saved: {ScenePath}");

            // 3) in-editor player build (editor stays alive; triggers keep working)
            File.WriteAllText(Done, "RUNNING (building player — this takes minutes) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Directory.CreateDirectory(OutDir);
            var t0 = DateTime.Now;
            var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var s = report.summary;
            sb.AppendLine($"build: {s.result}, errors={s.totalErrors}, sizeMB={s.totalSize / (1024f * 1024f):F0}, secs={(DateTime.Now - t0).TotalSeconds:F0}");
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            { RestoreScene(); Fail("player build failed: " + s.result); return; }

            // 4) launch the player WITH graphics, windowed — PerfRun writes + quits
            try { if (File.Exists(PlayerTxt)) File.Delete(PlayerTxt); } catch {}
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player measuring ~30s) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            RestoreScene();   // 5) editor back on SampleScene while the player measures
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 180.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no perf-player.txt within 180s"); return; }

            try
            {
                System.Threading.Thread.Sleep(500);   // let the player finish the write + quit
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());

                // verdict: avg fps vs the 60 floor (4070 Ti SUPER = calibration anchor; min-spec 1660 is a later pass)
                float avgFps = ParseAvgFps(player);
                bool green = avgFps >= TargetFps;
                sb.AppendLine();
                sb.AppendLine($"A6 verdict: avg {avgFps:0.0} fps vs floor {TargetFps} -> {(green ? "GREEN" : "UNDER")} " +
                              "(reference GPU 4070 Ti SUPER; min-spec GTX 1660 measurement is a separate later pass)");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "UNDER")} avgFps={avgFps:0.0}\nsee {Report}\n");
                UnityEngine.Debug.Log($"[A6Player] done avgFps={avgFps:0.0} verdict={(green ? "GREEN" : "UNDER")}");
            }
            catch (Exception e) { Fail("poll: " + e.Message); }
            finally { SessionState.SetInt(KeyWaiting, 0); }
        }

        static float ParseAvgFps(string txt)
        {
            foreach (var line in txt.Split('\n'))
                if (line.TrimStart().StartsWith("fps"))
                {
                    int i = line.IndexOf("avg=");
                    if (i >= 0)
                    {
                        string rest = line.Substring(i + 4);
                        int end = 0; while (end < rest.Length && (char.IsDigit(rest[end]) || rest[end] == '.' || rest[end] == ',')) end++;
                        if (float.TryParse(rest.Substring(0, end).Replace(',', '.'),
                            System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f)) return f;
                    }
                }
            return 0f;
        }

        static Vector3 VillageCenter()
        {
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer == null) return new Vector3(400, 30, 400);
            var pts = new System.Collections.Generic.List<Vector3>();
            foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) pts.Add(aa.transform.position);
            if (pts.Count == 0) return new Vector3(400, 30, 400);
            Vector3 best = pts[0]; int bestN = -1;
            foreach (var p in pts)
            {
                int n = 0; foreach (var q in pts) if ((q - p).sqrMagnitude < 144f) n++;
                if (n > bestN) { bestN = n; best = p; }
            }
            return best;
        }

        static void RestoreScene()
        {
            try { EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[A6Player] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
