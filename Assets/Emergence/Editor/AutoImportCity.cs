// EMERGENCE — trigger-file .unitypackage importer (headless-from-bridge).
// Drop Reports/RUN_IMPORT.trigger containing a project-relative .unitypackage path;
// this imports it silently (no dialog) so the user doesn't have to navigate Unity.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoImportCity
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_IMPORT.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "IMPORT_DONE.txt");

        static AutoImportCity() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(Trigger)) return;
                var pkg = File.ReadAllText(Trigger).Trim();
                File.Delete(Trigger);
                Directory.CreateDirectory(Path.GetDirectoryName(Done));
                File.WriteAllText(Done, "IMPORTING " + pkg + "\n");
                Debug.Log("[AutoImport] importing package: " + pkg);
                AssetDatabase.ImportPackage(pkg, false);   // silent, import everything, no dialog
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + pkg + "\n");
                Debug.Log("[AutoImport] done: " + pkg);
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }
    }
}
#endif
