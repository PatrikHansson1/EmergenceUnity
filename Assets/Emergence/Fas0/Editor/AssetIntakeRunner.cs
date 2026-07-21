// EMERGENCE — Fas 0: headless trigger-runner for the full-library AssetIntake pass.
// Drop Reports/RUN_INTAKE_ALL.trigger (via the bridge) and this runs AssetIntakePass over the whole
// owned library and writes Reports/INTAKE_ALL_DONE.txt — no menu click needed once compiled.
//
// Distinct from CityIntakeRunner (RUN_INTAKE / INTAKE_DONE), which is the render-test lineup.
// Mirrors AutoGolden / AutoDress exactly.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AssetIntakeRunner
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_INTAKE_ALL.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "INTAKE_ALL_DONE.txt");

        static AssetIntakeRunner() { EditorApplication.update += Tick; }

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
                Debug.Log("[AssetIntakeRunner] trigger seen — running full-library intake...");
                string verdict;
                try
                {
                    int magenta = AssetIntakePass.RunHeadless();
                    verdict = magenta == 0 ? "GREEN magenta=0" : ("RED magenta=" + magenta);
                }
                catch (Exception ex) { verdict = "EXC: " + ex.Message; }
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " verdict=" + verdict +
                                        "\nsee Reports/intake-report.txt (+ .csv)\n");
                Debug.Log("[AssetIntakeRunner] finished — " + verdict);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
