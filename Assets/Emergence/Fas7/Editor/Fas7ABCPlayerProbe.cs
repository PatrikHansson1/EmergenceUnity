// EMERGENCE — FAS 7 increment 3 PROBE: the A/B/C SIMULTANEITY PROOF runs in a REAL PLAYER BUILD
// (D-138 pipeline, the soak probe's shape).
//
// Builds the proof scene (genesis wilderness + camera + Fas7ABCPlayerProof; the observer composes
// the reference producer AND the onboarding itself), builds the player, launches it (-logFile
// beside the exe, D-169 standard), polls abc-player.txt, folds the three condition lines into the
// report and copies the chronicle artifact + evidence out. GREEN requires ALL of:
//   A: genesis OK, first child OK, first hut OK (gaze), beats in order, undisturbed
//      (0 pauses / 0 jumps / 0 order breaks / 0 pace violations), declared span reached;
//   B: feed span OK, ★-spine OK (named child + hut + named death), drama entries present in the
//      book, artifact pair written, book evidence grabbed;
//   C: divergence OK (same-year SHA + DNA differ vs reference seed), loss half WITNESSED or
//      honestly DECLARED (see the proof header — unwitnessable live today, engine-lane order);
//   magenta 0/0, COMPLETE.
// This is the TECHNICAL half of the gate's A/B/C criterion. The HUMAN half (cold tester +
// recorded first hour) is Patrik's and is NEVER built around — it stays on the VÄNTAR ledger.
// Menu: Emergence/Fas7/RUN PLAYER ABC.  Headless: drop Reports/RUN_FAS7ABC.trigger.
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
    public static class Fas7ABCPlayerProbe
    {
        const long Seed = 8919;
        const long RefSeed = 4242;
        const int RefYear = 8;
        const int WindowYears = 56;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string ScenePath = "Assets/Emergence/Scenes/ABCProofScene.unity";
        const string SampleScene = "Assets/Scenes/SampleScene.unity";

        static double _next;
        static string Trigger    => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7ABC.trigger");
        static string Done       => Path.Combine(Application.dataPath, "..", "Reports", "FAS7ABC_DONE.txt");
        static string OutDir     => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Builds", "EmergenceABC");
        static string ExePath    => Path.Combine(OutDir, "EmergenceABC.exe");
        static string PlayerTxt  => Path.Combine(OutDir, "abc-player.txt");
        static string PlayerBeat => Path.Combine(OutDir, "abc-beat.txt");
        static string ArtTxt     => Path.Combine(OutDir, $"chronicle-{Seed}-y{WindowYears:000}-abc.txt");
        static string ArtHtml    => Path.Combine(OutDir, $"chronicle-{Seed}-y{WindowYears:000}-abc.html");
        const string Report      = "Reports/fas7-abc-player.txt";
        const string Evidence    = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas7";
        const string Qa          = @"C:\Users\patri\Dropbox\Emergence\30-QA";
        const string KeyWaiting = "emg.fas7abc.waiting", KeyStart = "emg.fas7abc.start", KeyReport = "emg.fas7abc.report";

        static Fas7ABCPlayerProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN PLAYER ABC")]
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
            sb.AppendLine("EMERGENCE — FAS 7 A/B/C SIMULTANEITY PROOF: the three existence conditions in ONE undisturbed player run");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"declared window: seed {Seed}, y0 -> y{WindowYears} at 1x/producer wall (~17-20 min; full 120y is producer-bound, D-148 — WAITS on engine-lane deep-tick)");
            sb.AppendLine($"reference for C: seed {RefSeed} produced headless to y{RefYear} in the SAME player BEFORE the witness begins (never applied, never witnessed)");
            sb.AppendLine("HUMAN half of the gate (cold tester + recorded first hour) = Patrik — flagged VÄNTAR, never built around.");
            sb.AppendLine();
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING (building scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");

            // D-123's lesson in player form: bake runInBackground into the build; kill stale instances
            // so the exe isn't write-locked (soak school).
            PlayerSettings.runInBackground = true;
            try { Process.Start(new ProcessStartInfo("cmd.exe", "/c taskkill /IM EmergenceABC.exe /F") { CreateNoWindow = true, UseShellExecute = false }); System.Threading.Thread.Sleep(1500); } catch { }

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var G = JsonUtility.FromJson<WorldState>(File.ReadAllText(GenesisPath));
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(G.season) ? "spring" : G.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7ABC] look: " + e.Message); }
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var proof = new GameObject("Fas7ABCPlayerProof").AddComponent<Fas7ABCPlayerProof>();
            proof.seed = Seed; proof.refSeed = RefSeed; proof.refYear = RefYear; proof.windowYears = WindowYears;
            proof.expectedGenesisSouls = G.agents != null ? G.agents.Length : -1;
            proof.watchdogSecs = 2600f;   // window + margin (soak: y0->40 = 714 s, deep years ~15-20 s/yr; + ref run + boot)
            sb.AppendLine($"proof scene: genesis wilderness + camera + Fas7ABCPlayerProof (observer composes ref producer + onboarding; expectedGenesisSouls={proof.expectedGenesisSouls})");

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
            string asmPath = Path.Combine(OutDir, "EmergenceABC_Data", "Managed", "Assembly-CSharp.dll");
            sb.AppendLine($"traceability: commit={GitSha()}, Assembly-CSharp.dll mtime={(File.Exists(asmPath) ? File.GetLastWriteTime(asmPath).ToString("yyyy-MM-dd HH:mm:ss") : "MISSING")}");
            RestoreScene();
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            { Fail("player build failed: " + s.result); return; }

            foreach (var f in new[] { PlayerTxt, PlayerBeat, ArtTxt, ArtHtml,
                Path.Combine(OutDir, "abc-a-firsthut.png"), Path.Combine(OutDir, "abc-world.png"), Path.Combine(OutDir, "abc-book.png") })
            { try { if (File.Exists(f)) File.Delete(f); } catch {} }
            // -logFile beside the exe: the player's own log stays readable without LocalLow (D-169 standard)
            Process.Start(new ProcessStartInfo(ExePath, "-screen-fullscreen 0 -screen-width 1600 -screen-height 900 -logFile player.log")
            { UseShellExecute = true, WorkingDirectory = OutDir });
            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyWaiting, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            File.WriteAllText(Done, "RUNNING (player ~17-22 min — ref run + undisturbed window y0->y" + WindowYears + ") " + DateTime.Now.ToString("HH:mm:ss") + "\n");
        }

        static void Poll()
        {
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 3200.0;
            if (!File.Exists(PlayerTxt))
            {
                // heartbeat into DONE so a watcher sees progress without touching the player
                try { if (File.Exists(PlayerBeat)) File.WriteAllText(Done, "RUNNING " + File.ReadAllText(PlayerBeat).Trim() + " " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
                if (overtime) Fail("player produced no abc-player.txt within 3200s");
                return;
            }

            try
            {
                System.Threading.Thread.Sleep(800);
                string player = File.ReadAllText(PlayerTxt);
                var lines = player.Replace("\r", "").Split('\n');
                string lA = "", lB = "", lC = "", lEnd = "";
                foreach (var l in lines)
                {
                    if (l.StartsWith("A ")) lA = l;
                    else if (l.StartsWith("B ")) lB = l;
                    else if (l.StartsWith("C ")) lC = l;
                    else if (l.StartsWith("abc ")) lEnd = l;
                }
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine("## PLAYER RESULT (one line per condition)");
                sb.AppendLine(player.Trim());
                sb.AppendLine();

                string evNote;
                try
                {
                    Directory.CreateDirectory(Evidence);
                    Directory.CreateDirectory(Qa);
                    int copied = 0;
                    foreach (var (src, dst) in new[] {
                        (Path.Combine(OutDir, "abc-a-firsthut.png"), Path.Combine(Evidence, "fas7-abc-a-firsthut.png")),
                        (Path.Combine(OutDir, "abc-world.png"),     Path.Combine(Evidence, "fas7-abc-world.png")),
                        (Path.Combine(OutDir, "abc-book.png"),      Path.Combine(Evidence, "fas7-abc-book.png")),
                        (ArtTxt,  Path.Combine(Evidence, Path.GetFileName(ArtTxt))),
                        (ArtHtml, Path.Combine(Evidence, Path.GetFileName(ArtHtml))),
                        (ArtTxt,  Path.Combine(Qa, Path.GetFileName(ArtTxt))),
                        (ArtHtml, Path.Combine(Qa, Path.GetFileName(ArtHtml))),
                        (ArtTxt,  Path.Combine(Path.GetDirectoryName(Application.dataPath), "Reports", Path.GetFileName(ArtTxt))),
                        (ArtHtml, Path.Combine(Path.GetDirectoryName(Application.dataPath), "Reports", Path.GetFileName(ArtHtml))),
                    })
                        if (File.Exists(src)) { File.Copy(src, dst, true); copied++; }
                    evNote = copied >= 9 ? "artifacts+evidence copied: 3 PNG + chronicle txt/html -> 45-UNITY/evidence/fas7/, chronicle -> 30-QA/ + Reports/ (rendered IN the player)"
                                         : $"artifact copy PARTIAL: {copied}/9";
                }
                catch (Exception e) { evNote = "artifact copy failed: " + e.Message; }
                sb.AppendLine(evNote);

                bool aOk = lA.Contains("genesis=OK") && lA.Contains("firstChild=OK") && lA.Contains("firstHut=OK")
                        && lA.Contains("beatsOrder=OK") && lA.Contains("undisturbed=OK") && lA.Contains("span=OK");
                bool bOk = lB.Contains("span=OK") && lB.Contains("spine=OK") && lB.Contains("drama=OK")
                        && lB.Contains("artifact=OK") && lB.Contains("book=OK");
                bool cOk = lC.Contains("divergence=OK") && (lC.Contains("loss=WITNESSED") || lC.Contains("loss=DECLARED-ABSENT"));
                bool endOk = lEnd.Contains("evidence=OK") && lEnd.Contains("magenta=0/0") && lEnd.Contains("COMPLETE");
                bool green = aOk && bOk && cOk && endOk && evNote.Contains("rendered IN the player");
                sb.AppendLine();
                sb.AppendLine($"conditions: A={(aOk ? "OK" : "FAIL")}  B={(bOk ? "OK" : "FAIL")}  C={(cOk ? "OK" : "FAIL")}  end={(endOk ? "OK" : "FAIL")}");
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — A/B/C hold SIMULTANEOUSLY in one undisturbed player run (technical half; the human half is Patrik's, VÄNTAR)"
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
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Fas7ABC] restore scene: " + e.Message); }
        }

        static void Fail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyWaiting, 0);
        }
    }
}
#endif
