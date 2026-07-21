// EMERGENCE — trigger-file runner for the Golden Master gate (D-093, headless-from-bridge).
// A tiny editor poll: when Reports/RUN_GOLDEN.trigger appears, run the in-editor Golden Master
// suite once and write Reports/golden-report.txt. Lets the gate be driven by dropping a file
// (via the Cowork bridge) without clicking the menu bar. Gated entirely on the trigger file.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoGolden
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GOLDEN.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "GOLDEN_DONE.txt");

        static AutoGolden() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0; // poll every 2s
            try
            {
                if (!File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Directory.CreateDirectory(Path.GetDirectoryName(Done));
                File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Debug.Log("[AutoGolden] trigger seen — running the Golden Master suite...");
                string verdict;
                try { GoldenMasterRunner.RunSuite(); verdict = "GREEN"; }
                catch (Exception ex) { verdict = "RED/EXC: " + ex.Message; }
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " verdict=" + verdict + "\n");
                Debug.Log("[AutoGolden] finished — verdict " + verdict + " — see Reports/golden-report.txt");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
