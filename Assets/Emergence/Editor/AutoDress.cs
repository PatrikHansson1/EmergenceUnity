// EMERGENCE — trigger-file runner for the D-101 environment/dressing pass (headless-from-bridge).
// Drop Reports/RUN_DRESS.trigger and this rebuilds + captures the codex-demo world (RunCodexDemo)
// without touching the (bridge-masked) menu bar. Mirrors AutoGolden. Gated entirely on the trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoDress
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_DRESS.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "DRESS_DONE.txt");

        static AutoDress() { EditorApplication.update += Tick; }

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
                Debug.Log("[AutoDress] trigger seen — rebuilding + capturing the codex-demo world (D-101)...");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate); // pick up external .mat/.asset edits (Auto Refresh may be off)
                // targeted force-reimport of the tree-leaf materials — Refresh alone wasn't reloading external .mat edits
                foreach (var mp in new[] {
                    "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Materials/Trees/M_TreeLarge_Leaves.mat",
                    "Assets/Polyart/PolyartStudio/DreamscapeMeadows/Materials/Trees/M_TreeBirch_Leaves.mat" })
                    AssetDatabase.ImportAsset(mp, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                string verdict;
                try { AuditionRunner.RunCodexAll(); verdict = "OK"; }
                catch (Exception ex) { verdict = "EXC: " + ex.Message; }
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " verdict=" + verdict + "\n");
                Debug.Log("[AutoDress] finished — " + verdict);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
