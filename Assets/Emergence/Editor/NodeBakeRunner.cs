// EMERGENCE — trigger-file runner for DETACHED node jobs (D-627/G1, 2026-08-31).
// Purpose: long V8 bakes (hours) must survive both the cloud container (reclaimed per turn, D-606)
// and the bridge VM (dies with the call, D-604). The Unity editor process is the one long-lived
// process on this machine — so it SPAWNS a detached node.exe and returns immediately.
// Contract: Reports/RUN_NODEBAKE.trigger — first line = node args (whitelist-checked), e.g.
//   --version
//   bake-deep.js 97013 3000
// cwd = Reports/rig/bake. stdout+stderr -> Reports/rig/bake/nodebake-<stamp>.log.
// NODEBAKE_DONE.txt records STARTED + pid (the JOB's completion is written by the script itself).
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class NodeBakeRunner
    {
        static double _next;
        static string Root    => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string Trigger => Path.Combine(Root, "Reports", "RUN_NODEBAKE.trigger");
        static string Done    => Path.Combine(Root, "Reports", "NODEBAKE_DONE.txt");
        static string BakeDir => Path.Combine(Root, "Reports", "rig", "bake");

        static NodeBakeRunner() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(Trigger)) return;
                var args = (File.ReadAllLines(Trigger) is var L && L.Length > 0 ? L[0] : "").Trim();
                File.Delete(Trigger);
                Directory.CreateDirectory(BakeDir);
                // whitelist: filenames, numbers, dashes, dots, spaces — no shell metacharacters
                if (args.Length == 0 || args.Length > 200 || !Regex.IsMatch(args, @"^[A-Za-z0-9_\-\. ]+$"))
                { File.WriteAllText(Done, "REFUSED bad args " + DateTime.Now.ToString("HH:mm:ss") + "\n"); return; }
                var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                // resolve node.exe: PATH first, then common install locations (Unity's env can lack PATH entries)
                string nodeExe = "node";
                try
                {
                    var candidates = new string[] {
                        @"C:\Program Files\nodejs\node.exe",
                        @"C:\Program Files (x86)\nodejs\node.exe",
                        Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\nodejs\node.exe"),
                        Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\scoop\apps\nodejs\current\node.exe"),
                        Environment.ExpandEnvironmentVariables(@"%ProgramData%\chocolatey\bin\node.exe")
                    };
                    foreach (var c in candidates) if (File.Exists(c)) { nodeExe = c; break; }
                }
                catch {}
                if (args == "diag")
                {
                    var diagLog = Path.Combine(BakeDir, "nodebake-diag.txt");
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("resolved nodeExe = " + nodeExe + (nodeExe == "node" ? " (PATH fallback — no candidate file found)" : " (file exists)"));
                    try { var pw = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "cmd.exe", Arguments = "/c where node", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }); sb.AppendLine("where node -> " + pw.StandardOutput.ReadToEnd().Trim() + pw.StandardError.ReadToEnd().Trim()); pw.WaitForExit(5000); } catch (Exception dx) { sb.AppendLine("where failed: " + dx.Message); }
                    File.WriteAllText(diagLog, sb.ToString());
                    File.WriteAllText(Done, "DIAG " + DateTime.Now.ToString("HH:mm:ss") + " -> rig/bake/nodebake-diag.txt\n");
                    return;
                }
                var log = Path.Combine(BakeDir, "nodebake-" + stamp + ".log");
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"\"" + nodeExe + "\" " + args + "\"" + " > \"" + log + "\" 2>&1",
                    WorkingDirectory = BakeDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = System.Diagnostics.Process.Start(psi);
                File.WriteAllText(Done, "STARTED " + DateTime.Now.ToString("HH:mm:ss") + " pid=" + (p != null ? p.Id : -1) + " args=" + args + " log=" + Path.GetFileName(log) + "\n");
                File.WriteAllText(Path.Combine(BakeDir, "nodebake-latest.txt"), Path.GetFileName(log) + "\n");
                Debug.Log("[NodeBake] spawned detached: node " + args + " -> " + log);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
