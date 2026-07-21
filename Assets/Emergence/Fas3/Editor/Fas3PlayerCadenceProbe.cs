// EMERGENCE — FAS 3 increment 3b PROBE (D-136): build + run the PLAYER-JINT CADENCE measurement.
//
// A6PlayerPerf's mechanism (D-130), minimal edition: a tiny empty scene with Fas3PlayerCadence,
// in-editor BuildPlayer (editor stays alive), player launched -batchmode -nographics (pure CPU
// measurement, no window), poll for cadence-player.txt, verdict the time strategy:
//   - player tps vs 1x (24 t/s) and 4x (96 t/s), with a 25% headroom margin (the real game adds
//     reconcile/present cost on the main thread — the driver thread still owns the Jint budget),
//   - EA window (120y x YEAR ticks) minutes at player flat-out.
// The engine source is the SAME StreamingAssets 2.3 twin the editor runs (D-093 + driver's loader)
// — packaged into the player automatically. GREEN = mechanism proved (build+run+number);
// holds/doesn't-hold is DATA for the strategy decision, not a gate.
// Menu: Emergence/Fas3/RUN PLAYER CADENCE.  Headless: drop Reports/RUN_FAS3PCAD.trigger.
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas3PlayerCadenceProbe
    {
        const string ScenePath = "Assets/Emergence/Scenes/CadenceScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";

        static double _next;
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3PCAD.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FAS3PCAD_DONE.txt");
        static string OutDir    => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceCadence");
        static string ExePath   => Path.Combine(OutDir, "EmergenceCadence.exe");
        static string PlayerTxt => Path.Combine(OutDir, "cadence-player.txt");
        const string Report     = "Reports/fas3-player-cadence.txt";
        const string KeyWaiting = "emg.fas3pcad.waiting", KeyStart = "emg.fas3pcad.start", KeyReport = "emg.fas3pcad.report";

        static Fas3PlayerCadenceProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN PLAYER CADENCE")]
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
            sb.AppendLine("EMERGENCE — FAS 3 PLAYER-JINT CADENCE (D-136): the time-strategy number");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("editor baseline (D-134): 16 t/s flat-out incl. live reconciles; engine YEAR=144 (D-135)");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // 1) minimal measurement scene — no world, just the meter (pure Jint cost)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Camera") { tag = "MainCamera" }; camGo.AddComponent<Camera>();
            new GameObject("Fas3PlayerCadence").AddComponent<Fas3PlayerCadence>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { RestoreScene(); Fail("scene save failed"); return; }
            sb.AppendLine($"scene saved: {ScenePath} (empty + Fas3PlayerCadence; engine JS ships via StreamingAssets)");

            // 2) in-editor player build
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

            // 3) launch headless — worker-thread Jint is what we're timing
            try { if (File.Exists(PlayerTxt)) File.Delete(PlayerTxt); } catch {}
            Process.Start(new ProcessStartInfo(ExePath, "-batchmode -nographics")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player measuring ~40s) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 180.0;
            if (!File.Exists(PlayerTxt)) { if (overtime) Fail("player produced no cadence-player.txt within 180s"); return; }

            try
            {
                System.Threading.Thread.Sleep(500);
                string player = File.ReadAllText(PlayerTxt);
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT");
                sb.AppendLine(player.Trim());
                sb.AppendLine();

                float tps = ParseF(player, "avg=");
                float yearTicks = ParseF(player, "yearTicks=");
                if (yearTicks <= 0) yearTicks = 144f;
                bool ok = tps > 0f && !player.Contains("driverError");
                if (ok)
                {
                    const float margin = 1.25f;   // headroom: the real game reconciles/presents on top
                    float eaTicks = 120f * yearTicks;
                    bool holds1x = tps >= 24f * margin, holds4x = tps >= 96f * margin;
                    sb.AppendLine("## TIME-STRATEGY VERDICT");
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "player flat-out {0:F1} t/s (editor was 16): 1x (24 t/s x{1} margin) -> {2}; 4x (96 t/s x{1}) -> {3}",
                        tps, margin, holds1x ? "HOLDS" : "DOES NOT HOLD", holds4x ? "HOLDS" : "DOES NOT HOLD"));
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "EA window {0:F0} ticks (120y x YEAR {1:F0}): flat-out player = {2:F1} min; at 1x = {3:F1} min; at 4x = {4:F1} min",
                        eaTicks, yearTicks, eaTicks / tps / 60f, eaTicks / 24f / 60f, eaTicks / 96f / 60f));
                    sb.AppendLine(holds4x
                        ? "strategy: player Jint carries BOTH 1x and 4x in real time — checkpoint+resimulate NOT required for the EA window."
                        : holds1x
                            ? "strategy: 1x real-time HOLDS on player; 4x needs checkpoint+resimulate (or a tick-budget beyond Jint) — scope 4x accordingly."
                            : "strategy: even 1x does not hold on player — checkpoint+resimulate becomes the primary cadence strategy.");
                }
                bool green = ok;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the player number exists; the time strategy is now a decision, not a guess"
                                                   : "CHECK — see player result above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} tps={tps:F1}\nsee {Report}\n");
            }
            catch (Exception e) { Fail("poll: " + e.Message); }
            finally { SessionState.SetInt(KeyWaiting, 0); }
        }

        static float ParseF(string txt, string key)
        {
            int i = txt.IndexOf(key, StringComparison.Ordinal);
            if (i < 0) return -1f;
            string rest = txt.Substring(i + key.Length);
            int end = 0; while (end < rest.Length && (char.IsDigit(rest[end]) || rest[end] == '.' || rest[end] == '-')) end++;
            return float.TryParse(rest.Substring(0, end), NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : -1f;
        }

        static void RestoreScene()
        {
            try { EditorSceneManager.OpenScene(SampleScene, OpenSceneMode.Single); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas3PCad] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
