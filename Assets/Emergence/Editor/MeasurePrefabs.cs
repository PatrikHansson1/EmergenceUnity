// EMERGENCE — MEASURE PREFABS (D-924): the codex-guardian's rule made a tool. "Mät asseten, lita aldrig på dess
// namn" — this measures any prefab by name, headless, and reports what an eye would otherwise have to squint at:
// world-space size, where the pivot sits, renderer count, and every material's shader with a verdict on whether
// that shader family draws under URP at all (built-in Standard / Legacy / Particles families draw PINK, silently —
// the msVFX chimney smoke and the Polyart campfire glow did, for months, D-924).
//
// Headless: write the names (whitespace-separated) into Reports/RUN_MEASURE.trigger. Names resolve through the
// live catalog first (what the runtime would actually load), then an AssetDatabase search by exact prefab name.
// Instantiates in EDIT mode for one frame, reads Renderer bounds, destroys. Reads nothing from the sim.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class MeasurePrefabs
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_MEASURE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "MEASURE_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "measure-report.txt");

        static MeasurePrefabs() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger); File.Delete(Trigger); } catch { }
            Run(body.Split(new[] { ' ', '\n', '\r', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries));
        }

        [MenuItem("Emergence/Tools/MEASURE PREFABS (names in Reports/RUN_MEASURE.trigger)")]
        static void Menu() { if (File.Exists(Trigger)) Tick(); else Debug.Log("[MeasurePrefabs] write names into Reports/RUN_MEASURE.trigger first"); }

        public static void Run(string[] names)
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — MEASURE PREFABS (D-924): size, pivot, renderers, shaders — names lie, bounds do not");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}   {names.Length} names");
            sb.AppendLine();
            sb.AppendLine("name                                 w x h x d (m)     pivot(y from base)  rend  verdict / shaders");
            int pink = 0, missing = 0;
            var cat = EmergenceAssetCatalog.Load();
            foreach (var n in names)
            {
                GameObject pf = cat != null ? cat.Prefab(n) : null;
                string via = pf != null ? "catalog" : "";
                if (pf == null)
                {
                    foreach (var guid in AssetDatabase.FindAssets(n + " t:Prefab"))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (Path.GetFileNameWithoutExtension(path) != n) continue;
                        pf = AssetDatabase.LoadAssetAtPath<GameObject>(path); via = "assetdb"; break;
                    }
                }
                if (pf == null) { sb.AppendLine($"{n,-36} MISSING"); missing++; continue; }
                GameObject go = null;
                try
                {
                    go = (GameObject)UnityEngine.Object.Instantiate(pf); go.hideFlags = HideFlags.HideAndDontSave;
                    go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity;
                    var rs = go.GetComponentsInChildren<Renderer>(true);
                    if (rs.Length == 0) { sb.AppendLine($"{n,-36} (no renderers)  via {via}"); continue; }
                    var b = rs[0].bounds; for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    var shaders = rs.SelectMany(r => r.sharedMaterials).Where(m => m != null && m.shader != null).Select(m => m.shader.name).Distinct().ToArray();
                    var bad = shaders.Where(IsBuiltInFamily).ToArray();
                    if (bad.Length > 0) pink++;
                    string verdict = bad.Length > 0 ? "PINK-UNDER-URP: " + string.Join(" | ", bad) : "ok: " + string.Join(" | ", shaders);
                    sb.AppendLine($"{n,-36} {b.size.x,5:0.0} x {b.size.y,4:0.0} x {b.size.z,4:0.0}     {(-b.min.y),7:+0.00;-0.00}          {rs.Length,3}   {verdict}   [{via}]");
                }
                catch (Exception e) { sb.AppendLine($"{n,-36} ERROR {e.Message}"); }
                finally { if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            }
            sb.AppendLine();
            sb.AppendLine("pivot(y from base): how far the prefab's origin sits ABOVE its lowest point (0 = sits on its base, >0 = floats/needs sinking).");
            sb.AppendLine("PINK-UNDER-URP = built-in pipeline shader family (Standard / Legacy Shaders / Particles / Mobile / Nature): URP draws it magenta.");
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} names={names.Length} missing={missing} pinkUnderUrp={pink}\nsee Reports/measure-report.txt\n");
            Debug.Log($"[MeasurePrefabs] {names.Length} names, missing {missing}, pink-under-URP {pink}");
        }

        public static bool IsBuiltInFamily(string sn) =>
            sn == "Standard" || sn == "Standard (Specular setup)" || sn.StartsWith("Legacy Shaders/") || sn.StartsWith("Particles/")
            || sn.StartsWith("Mobile/") || sn.StartsWith("Nature/") || sn.StartsWith("Autodesk");
    }
}
#endif
