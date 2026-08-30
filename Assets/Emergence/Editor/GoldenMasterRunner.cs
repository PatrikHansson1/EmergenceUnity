// EMERGENCE — in-editor golden-master gate (READINESS §3 step 4; JINT-GOLDEN-MASTER-PLAN §6.8).
// Compares Jint-in-Unity canonical output against the checked-in 1.2.1 goldens.
// GREEN required to proceed. Batchmode: -executeMethod Emergence.Editor.GoldenMasterRunner.RunSuiteBatch
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jint;
using Emergence.Runtime;

namespace Emergence.Editor
{
    public static class GoldenMasterRunner
    {
        private static string EngineDir => Path.Combine(Application.dataPath, "Emergence", "Engine");
        private static string GoldensDir => Path.Combine(EngineDir, "harness", "goldens");
        private static string ReportPath => Path.Combine(Application.dataPath, "..", "Reports", "golden-report.txt");

        private const string TestFounders = "[{name:'Ask the First',traits:{curiosity:0.9,social:0.4,diligence:0.6,conformity:0.3}},{name:'Embla the First'},{traits:{social:0.85}},null]";
        private static readonly (string label, long seed, int ticks, string founders)[] Suite60 =
            { ("97013", 97013, 720, null), ("4242", 4242, 720, null), ("20260718", 20260718, 720, null), ("97013-founders", 97013, 720, TestFounders) };

        // D-131 (grind-review objection): the 720-tick horizon is the PER-STEP cadence (a full-depth run
        // is ~30 min — incompatible with "golden GREEN after every build step"). Depth-override lets the
        // GATE run at full 8640: write the tick count to Reports/GOLDEN_DEPTH.txt, drop the trigger,
        // delete the file afterwards. Both horizons hash the same engine — depth is coverage, not truth.
        static int DepthOverride()
        {
            try
            {
                var p = System.IO.Path.Combine(UnityEngine.Application.dataPath, "..", "Reports", "GOLDEN_DEPTH.txt");
                if (System.IO.File.Exists(p) && int.TryParse(System.IO.File.ReadAllText(p).Trim(), out var t) && t > 0) return t;
            }
            catch { }
            return 0;
        }

        [MenuItem("Emergence/Golden Master/1. Engine SHA Assert")]
        public static void ShaAssert()
        {
            var src = File.ReadAllText(EmergenceJintHost.EngineSourcePath(EngineDir));
            var sha = EmergenceJintHost.Sha256Hex(src);
            var recorded = File.ReadAllText(Path.Combine(EngineDir, "ENGINE-SHA.txt")).Trim();
            var ok = string.Equals(sha, recorded, StringComparison.OrdinalIgnoreCase)
                     && string.Equals(sha, EmergenceJintHost.ExpectedEngineSha, StringComparison.OrdinalIgnoreCase);
            if (!ok) throw new Exception($"ENGINE SHA MISMATCH: file={sha} recorded={recorded} const={EmergenceJintHost.ExpectedEngineSha}");
            Debug.Log($"[GoldenMaster] Engine SHA OK: {sha}");
        }

        [MenuItem("Emergence/Golden Master/2. Jint Smoke (createWorld + 24 ticks)")]
        public static void JintSmoke()
        {
            var host = EmergenceJintHost.FromDirectory(EngineDir, withHarness: false);
            var n = host.Engine.Evaluate(
                "(function(){var S=Emergence.createWorld(97013);S.silent=true;for(var i=0;i<24;i++)Emergence.tickWorld(S);return S.tick;})()")
                .AsNumber();
            if (n != 24) throw new Exception($"Jint smoke: expected tick 24, got {n}");
            Debug.Log("[GoldenMaster] Jint smoke OK: createWorld(97013) ticked to 24. Engine SHA " + host.EngineSha256);
        }

        [MenuItem("Emergence/Golden Master/3. Run Suite (3 seeds x 60y) — THE GATE")]
        public static void RunSuite() { var green = RunSuiteInternal(); if (!green) throw new Exception("GOLDEN MASTER RED — stop the line."); }

        public static void RunSuiteBatch() { var green = RunSuiteInternal(); EditorApplication.Exit(green ? 0 : 1); }

        [MenuItem("Emergence/Golden Master/4. Run 160y (4242) — confirmation depth")]
        public static void Run160()
        {
            var report = new StringBuilder();
            var green = RunOne("4242", 4242, 23040, null, report);
            Write(report);
            if (!green) throw new Exception("GOLDEN MASTER RED (160y) — stop the line.");
        }

        private static bool RunSuiteInternal()
        {
            var report = new StringBuilder();
            report.AppendLine($"GOLDEN MASTER — in-editor (Unity {Application.unityVersion}, {DateTime.Now:yyyy-MM-dd HH:mm})");
            report.AppendLine($"commit: {GitHeadSha()}");   // D-143 gate remark: the report carries its own commit SHA (label fixed per Fas 5 review I3 — it IS the commit, not the tree object)
            ShaAssert();
            report.AppendLine("Engine SHA assert: OK");
            bool green = true;
            int depth = DepthOverride();   // D-131: gate runs at full 8640 via Reports/GOLDEN_DEPTH.txt
            foreach (var (label, seed, ticks, founders) in Suite60) green &= RunOne(label, seed, depth > 0 ? depth : ticks, founders, report);
            report.AppendLine(green ? "VERDICT: GREEN" : "VERDICT: RED — STOP THE LINE (UNITY-BRIDGE-SPEC §5)");
            Write(report);
            Debug.Log("[GoldenMaster] " + (green ? "GREEN" : "RED") + " — report at Reports/golden-report.txt");
            return green;
        }

        private static bool RunOne(string label, long seed, int ticks, string founders, StringBuilder report)
        {
            var goldenPath = Path.Combine(GoldensDir, $"seed-{label}-t{ticks}.canon.txt");
            if (!File.Exists(goldenPath)) { report.AppendLine($"seed={label} t={ticks}: MISSING GOLDEN {goldenPath}"); return false; }
            var golden = File.ReadAllText(goldenPath);
            var host = EmergenceJintHost.FromDirectory(EngineDir, withHarness: true);
            var t0 = EditorApplication.timeSinceStartup;
            var canon = host.RunGolden(seed, ticks, founders);
            var secs = EditorApplication.timeSinceStartup - t0;
            var pass = string.Equals(canon, golden, StringComparison.Ordinal);
            var line = $"seed={label} t={ticks}: {(pass ? "GREEN" : "RED")} jintSha={EmergenceJintHost.Sha256Hex(canon)} goldenSha={EmergenceJintHost.Sha256Hex(golden)} bytes={canon.Length} secs={secs:F0}";
            report.AppendLine(line);
            Debug.Log("[GoldenMaster] " + line);
            if (!pass)
            {
                var diffAt = FirstDiff(canon, golden);
                report.AppendLine($"  first divergent char index: {diffAt} (triage per JINT-GOLDEN-MASTER-PLAN §5 / readable variant)");
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "Reports", $"divergent-{label}-t{ticks}.canon.txt"), canon);
            }
            return pass;
        }

        private static int FirstDiff(string a, string b)
        {
            var n = Math.Min(a.Length, b.Length);
            for (int i = 0; i < n; i++) if (a[i] != b[i]) return i;
            return a.Length == b.Length ? -1 : n;
        }

        /// <summary>D-143 gate remark ("golden report without its own SHA"): resolve HEAD by reading
        /// .git directly — no process spawn, works headless. (Working-tree dirtiness is NOT claimed
        /// here — the session protocol's git-status check owns that truth.)</summary>
        private static string GitHeadSha()
        {
            try
            {
                string root = Path.Combine(Application.dataPath, "..");
                string head = File.ReadAllText(Path.Combine(root, ".git", "HEAD")).Trim();
                string sha = head;
                if (head.StartsWith("ref: "))
                {
                    string refPath = Path.Combine(root, ".git", head.Substring(5).Replace('/', Path.DirectorySeparatorChar));
                    sha = File.Exists(refPath) ? File.ReadAllText(refPath).Trim() : "unresolved(" + head + ")";
                }
                return sha.Length >= 12 ? sha.Substring(0, 12) + (head.StartsWith("ref: ") ? " (" + head.Substring(5) + ")" : "") : sha;
            }
            catch (Exception e) { return "unknown (" + e.Message + ")"; }
        }

        private static void Write(StringBuilder report)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.AppendAllText(ReportPath, report.ToString() + "\n");
        }
    }
}
#endif
