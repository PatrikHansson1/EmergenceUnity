// EMERGENCE — Fas 0 (D-107 A6): the performance measurement harness (editor half).
//
// A6 sets the perf/scale budget UP FRONT so the Codex fill-pass + reconciler build against it; Fas 1
// measures for real on a representative dressed village and calibrates these numbers. This harness
// does a static census of the currently open scene (renderers as a draw-call proxy, unique
// materials/meshes, total triangles, LODGroup coverage, skinned-mesh count as an agent proxy) and
// writes Reports/perf-report.txt with measured-vs-budget. It only READS the scene.
//
// The provisional numbers below are a deliberate, documented starting point (min-spec GTX 1660-class
// floor, 4070 Ti SUPER reference per D-074) — NOT yet calibrated. Fas 1 replaces them with measured
// values from a real village and writes the calibrated budget back into canon.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class PerfHarness
    {
        // ---- PROVISIONAL BUDGET (A6 — calibrate in Fas 1) ----
        public const int   BudgetTargetFps        = 60;
        public const int   BudgetVisibleAgents     = 150;     // skinned characters animated on-screen at once
        public const int   BudgetVisibleBuildings  = 400;
        public const int   BudgetUniqueMaterials   = 300;
        public const int   BudgetDrawCallProxy     = 2500;    // active renderers
        public const float BudgetTrisMillions      = 8.0f;    // total visible triangles

        const string ReportTxt = "Reports/perf-report.txt";

        [MenuItem("Emergence/Fas0/Perf Harness — census current scene")]
        public static void RunMenu() { Run(); }

        public static void RunHeadless() { Run(); }

        static void Run()
        {
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            var mats = new HashSet<Material>();
            var meshes = new HashSet<Mesh>();
            long tris = 0;
            int skinned = 0;

            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials) if (m != null) mats.Add(m);
                if (r is SkinnedMeshRenderer smr)
                {
                    skinned++;
                    if (smr.sharedMesh != null) { meshes.Add(smr.sharedMesh); tris += TrisOf(smr.sharedMesh); }
                }
            }
            foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude))
                if (mf.sharedMesh != null) { meshes.Add(mf.sharedMesh); if (mf.GetComponent<Renderer>() != null) tris += TrisOf(mf.sharedMesh); }

            int lodGroups = UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Exclude).Length;
            float trisM = tris / 1_000_000f;

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — PERF HARNESS census (Fas 0, A6)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"scene: {UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}");
            sb.AppendLine("NOTE: static editor census (no play-mode draw-call counts). Budget is PROVISIONAL — Fas 1 calibrates.");
            sb.AppendLine();
            sb.AppendLine($"{"metric",-26} {"measured",-12} {"budget",-10} verdict");
            Line(sb, "active renderers (DC proxy)", renderers.Length, BudgetDrawCallProxy);
            Line(sb, "skinned meshes (agents)",     skinned,          BudgetVisibleAgents);
            Line(sb, "unique materials",            mats.Count,       BudgetUniqueMaterials);
            Line(sb, "triangles (millions)",        trisM,            BudgetTrisMillions);
            sb.AppendLine($"{"unique meshes",-26} {meshes.Count,-12} {"-",-10} (info)");
            sb.AppendLine($"{"LODGroups",-26} {lodGroups,-12} {"-",-10} (info — want heavy meshes LOD'd)");
            sb.AppendLine();
            sb.AppendLine($"target FPS {BudgetTargetFps} (min-spec GTX 1660-class; 4070 Ti SUPER reference).");
            sb.AppendLine("Fas 1 action: run on a representative dressed village at genesis + growth, record real");
            sb.AppendLine("draw calls / SetPass / GPU ms in play mode, then write the calibrated budget into canon.");

            Directory.CreateDirectory("Reports");
            File.WriteAllText(ReportTxt, sb.ToString());
            Debug.Log($"[PerfHarness] renderers={renderers.Length} agents={skinned} mats={mats.Count} tris={trisM:0.00}M  (see {ReportTxt})");
        }

        static long TrisOf(Mesh m)
        {
            long c = 0;
            for (int s = 0; s < m.subMeshCount; s++) c += (long)(m.GetIndexCount(s) / 3);
            return c;
        }

        static void Line(StringBuilder sb, string name, float measured, float budget)
        {
            string verdict = measured <= budget ? "OK" : "OVER";
            sb.AppendLine($"{name,-26} {measured,-12:0.##} {budget,-10:0.##} {verdict}");
        }
    }
}
#endif
