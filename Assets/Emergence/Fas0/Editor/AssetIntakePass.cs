// EMERGENCE — Fas 0 (D-107 Fas 0 grind): the AssetIntake editor pass.
//
// Consolidates the Stage-1/2 technical gate (ASSET-INTAKE-STANDARD.md, D-103) into ONE pass that
// runs over the WHOLE owned library and writes a pass/fail + bounds/scale table. It extends the
// intent of ZeroPinkScanV2 / PackVerifyTools / StripImpostorLods from "materials only" to
// "every prefab": per prefab it checks magenta/error shaders, missing materials, impostor/billboard
// LODs, LOD presence for heavy meshes, and reports world-size bounds (so the Codex `scale` can be
// calibrated to TileSize = 8 m; person ~= 1.7 m).
//
// The grind is GREEN when the whole library scans with magenta = 0. Everything else is a warning.
//
// Efficiency: works on the prefab ASSET hierarchy (no scene instantiation, no capture) — so it is
// immune to the async-shader-compile capture flake (D-101f) and scans hundreds of prefabs in seconds.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class AssetIntakePass
    {
        // The owned library (folders that exist are used; the rest are skipped).
        static readonly string[] Roots =
        {
            "Assets/Fantastic Village Pack", "Assets/FANTASTIC - Village Pack",
            "Assets/Fantastic City Pack",    "Assets/FANTASTIC - City Pack",
            "Assets/Polyart", "Assets/PolyOne", "Assets/Quaternius",
            "Assets/Vefects", "Assets/msVFX_Free Smoke Effects Pack", "Assets/UpDraftArt",
            "Assets/Emergence/Models"
        };

        const int HeavyMeshTris = 20000;   // above this, an LODGroup is expected
        const string ReportTxt = "Reports/intake-report.txt";
        const string ReportCsv = "Reports/intake-report.csv";
        const string CodexJson = "Assets/Emergence/Codex/object-codex.json";

        struct Row
        {
            public string name, path;
            public int magentaMats, missingMats, totalMats;
            public bool hasLODGroup, billboardLOD;
            public int tris;
            public Vector3 size;
            public bool inCodex;
            public bool Pass => magentaMats == 0;   // the hard grind gate is magenta=0; empty slots are info
        }

        [MenuItem("Emergence/Fas0/Asset Intake — full-library Stage 1-2 scan")]
        public static void RunMenu() { Run(true); }

        /// <summary>Headless entry (trigger runner / -executeMethod). Returns the magenta total.</summary>
        public static int RunHeadless() { return Run(false); }

        static int Run(bool ping)
        {
            var roots = Roots.Where(AssetDatabase.IsValidFolder).ToArray();
            var codexPrefabs = LoadCodexPrefabNames();

            var guids = AssetDatabase.FindAssets("t:GameObject", roots)
                                     .Distinct().ToArray();

            var rows = new List<Row>(guids.Length);
            int scanned = 0, magentaTotal = 0, fails = 0;

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrEmpty(path)) continue;
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go == null) continue;

                    if (ping && (i % 25 == 0))
                        EditorUtility.DisplayProgressBar("Asset Intake", path, (float)i / guids.Length);

                    var r = ScanPrefab(go, path);
                    r.inCodex = codexPrefabs.Contains(Path.GetFileNameWithoutExtension(path));
                    rows.Add(r);
                    scanned++;
                    magentaTotal += r.magentaMats;
                    if (!r.Pass) fails++;
                }
            }
            finally { if (ping) EditorUtility.ClearProgressBar(); }

            WriteReports(rows, roots, scanned, magentaTotal, fails);

            var verdict = magentaTotal == 0 ? "GREEN (magenta=0)" : $"RED (magenta={magentaTotal})";
            Debug.Log($"[AssetIntake] scanned={scanned} magenta={magentaTotal} fails={fails} => {verdict}  (see {ReportTxt})");
            return magentaTotal;
        }

        static Row ScanPrefab(GameObject go, string path)
        {
            var r = new Row { name = Path.GetFileNameWithoutExtension(path), path = path };

            // ---- materials / magenta (the hard gate) ----
            foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
            {
                // Empty material slots are normal on particle/VFX/line renderers — only a MISSING slot on a
                // real mesh renderer is worth reporting (as info). Magenta (broken shader) is scanned on ALL.
                bool isMesh = rend is MeshRenderer || rend is SkinnedMeshRenderer;
                foreach (var m in rend.sharedMaterials)
                {
                    r.totalMats++;
                    if (m == null) { if (isMesh) r.missingMats++; continue; }
                    if (m.shader == null) { r.magentaMats++; continue; }
                    if (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported) r.magentaMats++;
                }
            }

            // ---- LOD / impostor ----
            var lod = go.GetComponentInChildren<LODGroup>(true);
            r.hasLODGroup = lod != null;
            if (go.GetComponentInChildren<BillboardRenderer>(true) != null) r.billboardLOD = true;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("billboard") || n.Contains("impostor")) { r.billboardLOD = true; break; }
            }

            // ---- bounds (root-local) + triangle count, without instantiating ----
            Bounds b = default; bool has = false; int tris = 0;
            var rootInv = go.transform.worldToLocalMatrix;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                AccumMesh(mf.sharedMesh, rootInv * mf.transform.localToWorldMatrix, ref b, ref has, ref tris);
            foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                AccumMesh(sm.sharedMesh, rootInv * sm.transform.localToWorldMatrix, ref b, ref has, ref tris);
            r.size = has ? b.size : Vector3.zero;
            r.tris = tris;
            return r;
        }

        static void AccumMesh(Mesh mesh, Matrix4x4 m, ref Bounds b, ref bool has, ref int tris)
        {
            if (mesh == null) return;
            for (int s = 0; s < mesh.subMeshCount; s++) tris += (int)(mesh.GetIndexCount(s) / 3);
            var mb = mesh.bounds;                       // local-space AABB (no read/write needed)
            Vector3 c = mb.center, e = mb.extents;
            for (int i = 0; i < 8; i++)
            {
                var corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                var w = m.MultiplyPoint3x4(corner);
                if (!has) { b = new Bounds(w, Vector3.zero); has = true; }
                else b.Encapsulate(w);
            }
        }

        static HashSet<string> LoadCodexPrefabNames()
        {
            var set = new HashSet<string>();
            try
            {
                if (File.Exists(CodexJson))
                {
                    // cheap, dependency-free: pull "prefab":"..." values, strip extension
                    foreach (var line in File.ReadAllLines(CodexJson))
                    {
                        if (line.IndexOf("\"prefab\"", StringComparison.Ordinal) < 0) continue;
                        // "prefab": "value"  -> value sits between the last two quotes on the line
                        var parts = line.Split('"');
                        if (parts.Length >= 4)
                        {
                            var val = parts[parts.Length - 2];
                            if (val.Length > 0) set.Add(Path.GetFileNameWithoutExtension(val));
                        }
                    }
                }
            }
            catch { /* codex cross-ref is advisory only */ }
            return set;
        }

        static void WriteReports(List<Row> rows, string[] roots, int scanned, int magentaTotal, int fails)
        {
            Directory.CreateDirectory("Reports");
            var ordered = rows.OrderByDescending(r => !r.Pass)     // fails first
                              .ThenByDescending(r => r.billboardLOD)
                              .ThenBy(r => r.name).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — ASSET INTAKE (Fas 0, Stage 1-2, full library)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}  engine=asset-level (no capture)");
            sb.AppendLine($"roots: {string.Join(", ", roots.Select(x => x.Replace("Assets/", "")))}");
            sb.AppendLine($"scanned={scanned}  magentaTotal={magentaTotal}  fails={fails}");
            sb.AppendLine($"GRIND: {(magentaTotal == 0 ? "GREEN — magenta=0 across the owned library" : "RED — fix magenta before Fas 1")}");
            sb.AppendLine();

            var warnBillboard = ordered.Where(r => r.billboardLOD).ToList();
            var warnHeavyNoLod = ordered.Where(r => r.tris > HeavyMeshTris && !r.hasLODGroup).ToList();
            var infoMissing = ordered.Where(r => r.missingMats > 0).ToList();
            sb.AppendLine($"warnings: impostor/billboard LODs={warnBillboard.Count}  heavy-mesh-without-LOD={warnHeavyNoLod.Count}  mesh-prefabs-with-empty-slot={infoMissing.Count} (info, not a fail)");
            if (warnBillboard.Count > 0)
                sb.AppendLine("  impostor/billboard (StripImpostorLods candidates): " + string.Join(", ", warnBillboard.Take(40).Select(r => r.name)));
            if (warnHeavyNoLod.Count > 0)
                sb.AppendLine("  heavy-no-LOD: " + string.Join(", ", warnHeavyNoLod.Take(40).Select(r => $"{r.name}({r.tris})")));
            sb.AppendLine();

            sb.AppendLine("BOUNDS / SCALE TABLE  (size in metres — calibrate Codex `scale` to TileSize=8m; person~1.7m)");
            sb.AppendLine($"{"PASS",-5} {"magenta",-7} {"miss",-4} {"lod",-4} {"bill",-4} {"tris",-8} {"sizeX",-7} {"sizeY",-7} {"sizeZ",-7} {"codex",-5} name");
            foreach (var r in ordered)
                sb.AppendLine($"{(r.Pass ? "ok" : "FAIL"),-5} {r.magentaMats,-7} {r.missingMats,-4} {(r.hasLODGroup ? "y" : "-"),-4} {(r.billboardLOD ? "y" : "-"),-4} {r.tris,-8} {r.size.x,-7:0.00} {r.size.y,-7:0.00} {r.size.z,-7:0.00} {(r.inCodex ? "y" : "-"),-5} {r.name}");

            File.WriteAllText(ReportTxt, sb.ToString());

            var csv = new StringBuilder();
            csv.AppendLine("pass,magentaMats,missingMats,totalMats,hasLODGroup,billboardLOD,tris,sizeX,sizeY,sizeZ,inCodex,name,path");
            foreach (var r in ordered)
                csv.AppendLine($"{r.Pass},{r.magentaMats},{r.missingMats},{r.totalMats},{r.hasLODGroup},{r.billboardLOD},{r.tris},{r.size.x:0.000},{r.size.y:0.000},{r.size.z:0.000},{r.inCodex},\"{r.name}\",\"{r.path}\"");
            File.WriteAllText(ReportCsv, csv.ToString());
        }
    }
}
#endif
