// EMERGENCE — VÅG 1.2: FORCED TEXTURE REIMPORT.
//
// The house prefabs render as a white/grey checker in the living world. Measured, not guessed:
// the material is M_wood_05 on URP/Lit (NOT Unity's default material — that hypothesis was
// falsified), it points at T_wood_05_BC.png, and that file on disk is a perfectly good hand-painted
// brown wood texture. A dusk A/B ruled out over-exposure too: the houses stay white when the whole
// world darkens.
//
// That leaves a STALE IMPORT. The purchased packs were wiped off disk by a filter-branch checkout
// and restored from the backup branch (D-169) — the files came back, but Unity's import records may
// not have. This forces the pack's textures and materials through the importer again and reports
// what it found, so the answer is a fact rather than a theory.
// Headless: drop Reports/RUN_REIMPORT.trigger.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class PackReimport
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_REIMPORT.trigger");
        const string Report = "Reports/pack-reimport.txt";
        static readonly string[] Dirs =
        {
            "Assets/Fantastic Village Pack/2d/textures",
            "Assets/Fantastic Village Pack/materials",
            "Assets/Fantastic City Pack/2d/textures",
            "Assets/Fantastic City Pack/materials",
        };

        static PackReimport() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas1/FORCE PACK REIMPORT")]
        public static void RunMenu() => Run();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run();
            }
            catch (Exception e) { Debug.LogWarning("[PackReimport] " + e.Message); }
        }

        static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — forced pack reimport (VÅG 1.2, the white-house investigation)");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine();

            // FIRST: state the evidence BEFORE touching anything, so the report shows cause and effect
            var probe = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantastic Village Pack/2d/textures/T_wood_05_BC.png");
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantastic Village Pack/materials/M_wood_05.mat");
            sb.AppendLine("BEFORE:");
            sb.AppendLine("  texture asset loads: " + (probe != null ? "YES  " + probe.width + "x" + probe.height + "  format=" + probe.format + "  mips=" + probe.mipmapCount : "NO — the importer never produced an asset"));
            sb.AppendLine("  material loads: " + (mat != null ? "YES  shader=" + mat.shader.name : "NO"));
            if (mat != null)
            {
                var t = mat.GetTexture("_BaseMap") ?? mat.GetTexture("_MainTex");
                sb.AppendLine("  material's base texture: " + (t != null ? t.name + " (" + t.GetType().Name + ")" : "NULL  <-- THIS is the white/checker"));
                sb.AppendLine("  material base colour: " + (mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").ToString() : "n/a"));
            }
            sb.AppendLine();

            int files = 0;
            foreach (var d in Dirs)
            {
                if (!Directory.Exists(d)) { sb.AppendLine("  (absent: " + d + ")"); continue; }
                var all = Directory.GetFiles(d, "*.*", SearchOption.AllDirectories)
                                   .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                                   .Select(f => f.Replace('\\', '/')).ToArray();
                foreach (var f in all) { AssetDatabase.ImportAsset(f, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); files++; }
                sb.AppendLine("  reimported " + all.Length + " files in " + d);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            sb.AppendLine();
            sb.AppendLine("files reimported: " + files);

            probe = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Fantastic Village Pack/2d/textures/T_wood_05_BC.png");
            mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Fantastic Village Pack/materials/M_wood_05.mat");
            sb.AppendLine();
            sb.AppendLine("AFTER:");
            sb.AppendLine("  texture asset loads: " + (probe != null ? "YES  " + probe.width + "x" + probe.height + "  format=" + probe.format + "  mips=" + probe.mipmapCount : "NO"));
            if (mat != null)
            {
                var t = mat.GetTexture("_BaseMap") ?? mat.GetTexture("_MainTex");
                sb.AppendLine("  material's base texture: " + (t != null ? t.name : "STILL NULL"));
            }
            // and a global read: how many of the pack's materials have NO base texture at all?
            int noTex = 0, checkedMats = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { "Assets/Fantastic Village Pack/materials" }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m == null) continue;
                checkedMats++;
                var t = (m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null) ?? (m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null);
                if (t == null) noTex++;
            }
            sb.AppendLine("  pack materials without a base texture: " + noTex + " of " + checkedMats);

            Directory.CreateDirectory("Reports");
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "Reports", "REIMPORT_DONE.txt"),
                              "DONE " + files + " files " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Debug.Log("[PackReimport] -> " + Report);
        }
    }
}
#endif
