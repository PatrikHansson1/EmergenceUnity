// EMERGENCE — FREE TRACK 2 (F1-KOPANALYS §noll-kostnad p.2, 2026-08-10): DOMESTIC ANIMAL INTAKE PROBE.
//
// The owned CC0 Quaternius FBX set (imported D-128 for deer/wolf) ALSO contains the domestic set:
// Alpaca/Bull/Cow/Donkey/Horse/Horse_White/Husky/ShibaInu. The purchase analysis mandates an intake
// test BEFORE any farm-animal purchase is considered. This probe answers exactly that question:
//   per FBX — (a) rigged? (SkinnedMeshRenderer present), (b) clip inventory + required core set
//   (Idle/Idle_2/Eating/Walk/Gallop + a HeadLow variant), (c) material shader sanity (no
//   InternalErrorShader/non-URP = magenta risk).
// READ-ONLY: builds nothing, places nothing (placement is documentary truth — S.animals carries only
// deer/wolf until the engine exports domestic types; a body-side spawn would be a determinism lie).
// R1 law (D-155): DONE keys stamped at measurement time, mirroring the report's OK rows.
// Headless: drop Reports/RUN_DOMESTIC.trigger.  Menu: Emergence/Fas2/DOMESTIC ANIMAL INTAKE.
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
    [InitializeOnLoad]
    public static class DomesticAnimalIntake
    {
        const string FbxDir = "Assets/Quaternius/FBX/";
        static readonly string[] Candidates = { "Alpaca", "Bull", "Cow", "Donkey", "Horse", "Horse_White", "Husky", "ShibaInu" };
        static readonly string[] CoreFrags = { "|Idle", "|Idle_2", "|Eating", "|Walk", "|Gallop" };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_DOMESTIC.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "DOMESTIC_DONE.txt");

        static DomesticAnimalIntake() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n" + e.StackTrace + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas2/DOMESTIC ANIMAL INTAKE")]
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — DOMESTIC ANIMAL INTAKE (free track 2, 2026-08-10)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("question: does the OWNED CC0 set make a farm-animal purchase unnecessary?");
            sb.AppendLine();

            int pass = 0, fail = 0;
            var badShaders = new List<string>();
            foreach (var name in Candidates)
            {
                string path = FbxDir + name + ".fbx";
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) { sb.AppendLine($"{name,-12} MISSING at {path}"); fail++; continue; }

                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                var clips = all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).Select(c => c.name).OrderBy(n => n).ToList();
                var skins = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                bool rigged = skins.Length > 0;

                var missingCore = CoreFrags.Where(f => !clips.Any(c => c.EndsWith(f))).ToList();
                bool headLow = clips.Any(c => c.Contains("HeadLow") || c.Contains("Headlow"));

                var shaders = skins.SelectMany(s => s.sharedMaterials).Where(m => m != null)
                                   .Select(m => m.shader != null ? m.shader.name : "NULL").Distinct().ToList();
                var suspect = shaders.Where(s => s == "Hidden/InternalErrorShader" || s == "NULL" || s.StartsWith("Standard")).ToList();
                badShaders.AddRange(suspect.Select(s => name + ":" + s));

                bool ok = rigged && missingCore.Count == 0 && headLow && suspect.Count == 0;
                if (ok) pass++; else fail++;
                sb.AppendLine($"{name,-12} {(ok ? "OK  " : "FAIL")} skins={skins.Length} clips={clips.Count} " +
                              $"coreMissing=[{string.Join(",", missingCore)}] headLow={(headLow ? "yes" : "NO")} " +
                              $"shaders=[{string.Join("; ", shaders)}]{(suspect.Count > 0 ? " SUSPECT=[" + string.Join(";", suspect) + "]" : "")}");
                sb.AppendLine($"             clip inventory: {string.Join(", ", clips)}");
            }

            sb.AppendLine();
            sb.AppendLine($"pass={pass}/{Candidates.Length} fail={fail} shaderSuspects={badShaders.Count}");
            sb.AppendLine("note: NOT wired into the world — S.animals exports only deer/wolf types; domestic");
            sb.AppendLine("      placement waits for an engine-lane export (documentary truth, D-078 r4).");
            string verdict = fail == 0 ? "GREEN" : "CHECK";
            sb.AppendLine($"verdict: {verdict} — {(fail == 0 ? "farm-animal purchase UNNECESSARY (owned CC0 set is rig-complete)" : "see FAIL rows before ruling out a purchase")}");

            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/domestic-intake.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} pass={pass}/{Candidates.Length} shaderSuspects={badShaders.Count}\nsee Reports/domestic-intake.txt\n");
            Debug.Log($"[DomesticIntake] {verdict} pass={pass}/{Candidates.Length}");
        }
    }
}
#endif
