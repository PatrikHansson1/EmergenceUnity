// EMERGENCE — trigger-file runner for the D-069 recipe (headless-from-bridge).
// Drop Reports/RUN_LGGFIX.trigger → PackVerifyTools.DisableLggOverrides(): disables ONLY the Lift Gamma Gain
// override in every VolumeProfile where it is active (broken in Unity 6 / URP 17 — magenta/teal cast), keeps the
// rest of each profile. Writes Reports/LGGFIX_DONE.txt; evidence in 45-UNITY/evidence/pack-verify/lgg-disable.txt.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoLggFix
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_LGGFIX.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "LGGFIX_DONE.txt");

        static AutoLggFix() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            try
            {
                File.Delete(Trigger);
                Directory.CreateDirectory(Path.GetDirectoryName(Done));
                File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                PackVerifyTools.DisableLggOverrides();
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " D-069 LGG overrides disabled — see 45-UNITY/evidence/pack-verify/lgg-disable.txt\n");
                Debug.Log("[AutoLggFix] done");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
