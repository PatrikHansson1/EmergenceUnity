// EMERGENCE — headless Windows build via the trigger system (toward the first diorama .exe, D-885).
// Drop Reports/RUN_BUILD.trigger → runs BuildScript.BuildWindowsMenu() (BuildPipeline in the open editor,
// does NOT quit), writes Reports/BUILD_DONE.txt with result/errors/size/path. Presentation/tooling only —
// the engine and golden are untouched. Optional body = a scene path to build instead of the default Bootstrap.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoBuild
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_BUILD.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "BUILD_DONE.txt");

        static AutoBuild() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            try { File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            try
            {
                BuildScript.BuildWindowsMenu();
                var root = Path.GetDirectoryName(Application.dataPath);
                var exe = Path.Combine(root, "Builds", "EmergenceUnity", "EmergenceUnity.exe");
                string tail = "";
                var rep = Path.Combine(root, "Builds", "build-report.txt");
                if (File.Exists(rep)) { var lines = File.ReadAllLines(rep); if (lines.Length > 0) tail = lines[lines.Length - 1]; }
                bool ok = File.Exists(exe);
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + (ok ? " OK " : " NO-EXE ") + tail + "\n");
                Debug.Log("[AutoBuild] done exe=" + ok + " " + tail);
            }
            catch (Exception e)
            {
                File.WriteAllText(Done, "ERROR " + DateTime.Now.ToString("HH:mm:ss") + " " + e.Message + "\n");
                Debug.LogError("[AutoBuild] " + e);
            }
        }
    }
}
#endif
