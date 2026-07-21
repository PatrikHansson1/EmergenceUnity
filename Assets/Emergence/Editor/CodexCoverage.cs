// EMERGENCE — CODEX COVERAGE TOOL (D-121, per OBJECT-CODEX-SPEC §3: the anti-orphan guarantee).
// Scans every prefab/model in Assets/** and diffs against object-codex.json:
//   • DANGLING  — a codex entry whose prefab name matches no asset (a broken pointer). Must be 0.
//   • ORPHAN    — an asset with no codex entry ("no one knows when to use this").
//   • COVERAGE% — referenced / total, overall and per top-level pack.
// Makes an un-indexed asset impossible to forget. Read-only. Headless: drop Reports/RUN_COVERAGE.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class CodexCoverage
    {
        const string CodexPath = "Assets/Emergence/Codex/object-codex.json";
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_COVERAGE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "COVERAGE_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "codex-coverage.txt");

        static CodexCoverage() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [Serializable] class Entry { public string id, prefab, tier; }
        [Serializable] class Codex { public Entry[] objects; }

        [MenuItem("Emergence/Codex/RUN COVERAGE (orphans/dangling)")]
        public static void Run()
        {
            var codex = JsonUtility.FromJson<Codex>(File.ReadAllText(CodexPath));
            // referenced prefab base-names (strip .glb; empty = told-not-shown, ignored)
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toldNotShown = 0;
            foreach (var e in codex.objects)
            {
                if (string.IsNullOrWhiteSpace(e.prefab)) { toldNotShown++; continue; }
                referenced.Add(Path.GetFileNameWithoutExtension(e.prefab));
            }

            // all prefab + model assets in the project → base-name + pack
            var guids = AssetDatabase.FindAssets("t:GameObject");
            var assetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var perPack = new Dictionary<string, (int total, int refd)>();
            var orphans = new List<string>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.StartsWith("Assets/")) continue;
                if (!(path.EndsWith(".prefab") || path.EndsWith(".glb") || path.EndsWith(".fbx"))) continue;
                var name = Path.GetFileNameWithoutExtension(path);
                assetNames.Add(name);
                // top-level pack = first folder under Assets/
                var parts = path.Split('/');
                var pack = parts.Length > 1 ? parts[1] : "(root)";
                var cur = perPack.TryGetValue(pack, out var v) ? v : (total: 0, refd: 0);
                bool isRef = referenced.Contains(name);
                perPack[pack] = (cur.total + 1, cur.refd + (isRef ? 1 : 0));
                if (!isRef) orphans.Add($"{pack}/{name}");
            }

            // dangling = referenced names that match no asset
            var dangling = referenced.Where(r => !assetNames.Contains(r)).OrderBy(x => x).ToList();

            int total = perPack.Values.Sum(v => v.total);
            int refd = perPack.Values.Sum(v => v.refd);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("EMERGENCE — CODEX COVERAGE (D-121, anti-orphan)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"codex entries: {codex.objects.Length}  ({referenced.Count} unique prefabs referenced, {toldNotShown} told-not-shown)");
            sb.AppendLine($"project assets (prefab/glb/fbx): {total}");
            sb.AppendLine($"OVERALL COVERAGE: {refd}/{total} = {(total > 0 ? 100f * refd / total : 0):0.0}%  |  ORPHANS: {orphans.Count}  |  DANGLING: {dangling.Count}");
            sb.AppendLine();
            sb.AppendLine($"## DANGLING (codex → missing asset) — MUST be 0");
            sb.AppendLine(dangling.Count == 0 ? "  none ✓" : string.Join("\n", dangling.Select(d => "  ✗ " + d)));
            sb.AppendLine();
            sb.AppendLine("## COVERAGE PER PACK (referenced / total)");
            foreach (var kv in perPack.OrderByDescending(k => k.Value.total))
                sb.AppendLine($"  {kv.Value.refd,4}/{kv.Value.total,-5} {(kv.Value.total > 0 ? 100f * kv.Value.refd / kv.Value.total : 0),5:0.0}%  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine($"## ORPHANS (asset with no codex entry) — {orphans.Count} total, first 60:");
            sb.AppendLine(string.Join("\n", orphans.OrderBy(x => x).Take(60).Select(o => "  · " + o)));
            sb.AppendLine();
            sb.AppendLine("Reading: dangling MUST be 0 (broken codex pointers). Orphans are the un-indexed backlog —");
            sb.AppendLine("the codex fill-pass shrinks this over time; a high-value orphan is a candidate for a new codex row.");

            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} coverage={(total>0?100f*refd/total:0):0.0}% orphans={orphans.Count} dangling={dangling.Count}\nsee Reports/codex-coverage.txt\n");
            Debug.Log($"[CodexCoverage] {refd}/{total} = {(total>0?100f*refd/total:0):0.0}% | orphans={orphans.Count} dangling={dangling.Count}");
        }
    }
}
#endif
