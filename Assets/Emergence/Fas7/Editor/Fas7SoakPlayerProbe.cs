// EMERGENCE — FAS 7 increment 2 PROBE: the SOAK runs in a REAL PLAYER BUILD (D-138 pipeline).
//
// Builds the proof scene (genesis wilderness + camera + Fas7SoakPlayerProof; the observer composes
// the onboarding itself), builds the player, launches it, polls soak-player.txt, folds the trend
// table into the report and copies evidence. GREEN requires: span reached undisturbed, order
// unbroken, pacing law clean, ALL bounded guards under their caps, no soft-lock, trend sane
// (gc < 4x), evidence framed + magenta 0/0, COMPLETE.
// Menu: Emergence/Fas7/RUN PLAYER SOAK.  Headless: drop Reports/RUN_FAS7SOAK.trigger.
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
    public static class Fas7SoakPlayerProbe
    {
        const long Seed = 8919;
        const int SoakYears = 40;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/SoakProofScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";

        static double _next;
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7SOAK.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FAS7SOAK_DONE.txt");
        static string OutDir    => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceSoak");
        static string ExePath   => Path.Combine(OutDir, "EmergenceSoak.exe");
        static string PlayerTxt => Path.Combine(OutDir, "soak-player.txt");
        static string PlayerTrend => Path.Combine(OutDir, "soak-player-trend.txt");
        static string PlayerPng => Path.Combine(OutDir, "soak-player.png");
        const string Report     = "Reports/fas7-soak-player.txt";
        const string Evidence   = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas7";
        const string KeyWaiting = "emg.fas7soak.waiting", KeyStart = "emg.fas7soak.start", KeyReport = "emg.fas7soak.report";

        static Fas7SoakPlayerProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN PLAYER SOAK")]
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
            sb.AppendLine("EMERGENCE — FAS 7 PLAYER SOAK: one undisturbed long session in a real player build");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"declared span: y0 -> y{SoakYears} at 1x/producer wall (full 120y window is producer-bound ~2.7+h, D-148 — WAITS on engine-lane deep-tick)");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // D-123's lesson in PLAYER form (this probe's first run): an UNFOCUSED player never ticks
            // Update — the observer that would have set runInBackground never runs. Bake it into the
            // build instead, and kill any stale hung instance so the exe isn't write-locked.
            PlayerSettings.runInBackground = true;
            try { Process.Start(new ProcessStartInfo("cmd.exe", "/c taskkill /IM EmergenceSoak.exe /F") { CreateNoWindow = true, UseShellExecute = false }); System.Threading.Thread.Sleep(1500); } catch { }

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var G = JsonUtility.FromJson<WorldState>(File.ReadAllText(GenesisPath));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7Soak] look: " + e.Message); }
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var proof = new GameObject("Fas7SoakPlayerProof").AddComponent<Fas7SoakPlayerProof>();
            proof.seed = Seed; proof.soakYears = SoakYears;
            proof.watchdogSecs = 1000f;   // first run cut at y36/600s — deep years cost ~14-16 s/year
            sb.AppendLine("proof scene: genesis wilderness + camera + Fas7SoakPlayerProof (observer composes the boot)");

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
            string asmPath = Path.Combine(OutDir, "EmergenceSoak_Data", "Managed", "Assembly-CSharp.dll");
            sb.AppendLine($"traceability: commit={GitSha()}, Assembly-CSharp.dll mtime={(File.Exists(asmPath) ? File.GetLastWriteTime(asmPath).ToString("yyyy-MM-dd HH:mm:ss") : "MISSING")}");
            RestoreScene();
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            { Fail("player build failed: " + s.result); return; }

            try { if (File.Exists(PlayerTxt)) File.Delete(PlayerTxt); } catch {}
            try { if (File.Exists(PlayerTrend)) File.Delete(PlayerTrend); } catch {}
            try { if (File.Exists(PlayerPng)) File.Delete(PlayerPng); } catch {}
            // -logFile beside the exe: the player's own log becomes readable without LocalLow access
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile player.log")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player soak ~6-10 min) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 1400.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no soak-player.txt within 1400s"); return; }

            try
            {
                System.Threading.Thread.Sleep(800);
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());
                sb.AppendLine();
                if (File.Exists(PlayerTrend))
                {
                    sb.AppendLine("## TREND (per applied year)");
                    sb.AppendLine(File.ReadAllText(PlayerTrend).Trim());
                    sb.AppendLine();
                }
                string evNote;
                try
                {
                    Directory.CreateDirectory(Evidence);
                    int copied = 0;
                    if (File.Exists(PlayerPng)) { File.Copy(PlayerPng, Path.Combine(Evidence, "fas7-soak-player.png"), true); copied++; }
                    if (File.Exists(PlayerTrend)) { File.Copy(PlayerTrend, Path.Combine(Evidence, "fas7-soak-trend.txt"), true); copied++; }
                    evNote = copied == 2 ? "evidence: soak png + trend -> 45-UNITY/evidence/fas7/ (rendered IN the player)" : $"evidence: only {copied}/2 artifacts";
                }
                catch (Exception e) { evNote = "evidence copy failed: " + e.Message; }
                sb.AppendLine(evNote);

                bool green = player.Contains("span=OK") && player.Contains("order=OK") && player.Contains("pace=OK")
                          && player.Contains("bounds=OK") && player.Contains("softlock=OK") && player.Contains("trend=OK")
                          && player.Contains("evidence=OK") && player.Contains("magenta=0/0") && player.Contains("COMPLETE")
                          && evNote.Contains("rendered IN the player");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — one undisturbed session holds: no crash, no soft-lock, every witness bounded, pacing honest"
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
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7Soak] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
