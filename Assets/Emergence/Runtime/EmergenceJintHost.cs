// EMERGENCE — Jint host (UNITY-BRIDGE-SPEC §3).
// One Jint.Engine per world; LimitRecursion(512); load order: prelude → engine → (harness in test paths).
// The hypot prelude is part of the .NET hosting contract (TD-008). The engine file is NEVER edited.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Jint;

namespace Emergence.Runtime
{
    public sealed class EmergenceJintHost
    {
        public const string ExpectedEngineSha = "98a190aa7dd09e664d31ca2b579e1224ddb3bff934fc419dfd5105f0825a2085"; // + GEO2 (D-480): ore in DEPOSITS not uniform (2 centres/material, FNV, radius 6) — variation +20% pairwise divergence; trade -> Wave C. // + B5b (D-457): trait-weighted need gates (hunger/warmth/social/sleep + social order-flip); D 0.337, gain +0.115 on F1.4 engine, no harm. // + F1.4 (D-451): thirst -- init 80, drain (season x pottery x dry-year), ~1/7 dry years via FNV, opportunistic+seeking drink, dehydration WEAKENS (energy -1.5) not kills. Slutprov 32 seeds: W1 0.33, 1 dead/32 (31337 lives), 0 thirst-deaths. Worldgen+agentinit change -> goldens RED from first tick by design. Prior sha (F1.2d): 8a473f0dff7d...

        private readonly Jint.Engine _engine;
        public string EngineSha256 { get; }

        /// <param name="engineSrc">Verbatim engine source (emergence-engine.js).</param>
        /// <param name="preludeSrc">Math.hypot host prelude source.</param>
        /// <param name="harnessSrc">Optional golden-master harness source (test builds).</param>
        public EmergenceJintHost(string engineSrc, string preludeSrc, string harnessSrc = null)
        {
            EngineSha256 = Sha256Hex(engineSrc);
            if (!string.Equals(EngineSha256, ExpectedEngineSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"ENGINE SHA MISMATCH — stop the line (UNITY-BRIDGE-SPEC §5). Expected {ExpectedEngineSha}, got {EngineSha256}. " +
                    "The engine copy has drifted from the recorded baseline; re-baseline consciously via the golden-master protocol.");

            _engine = new Jint.Engine(o => o.LimitRecursion(512));
            _engine.Execute(preludeSrc);   // host adaptation — legal; engine edits are not
            _engine.Execute(engineSrc);
            if (!string.IsNullOrEmpty(harnessSrc)) _engine.Execute(harnessSrc);
        }

        public Jint.Engine Engine => _engine;

        /// <summary>Golden run via the shared harness (requires harnessSrc). Returns the canonical string.</summary>
        public string RunGolden(long seed, int ticks, string foundersJs = null)
            => _engine.Evaluate($"EmergenceGolden.runGolden({seed},{ticks},{(string.IsNullOrEmpty(foundersJs) ? "null" : foundersJs)})").AsString();

        /// <summary>Static SHA-256 (lowercase hex) of a string's UTF-8 bytes — same convention as `sha256sum` on the golden files.</summary>
        public static string Sha256Hex(string s)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>Resolve the engine source path. 2.3 re-baseline (D-093): the canon Engine/ copy is
        /// read-only-locked on disk, so the writable 2.3 twin lives in StreamingAssets — prefer it when present.</summary>
        /// <summary>D-225: THE FALLBACK WAS SILENT, AND A SILENT FALLBACK TO A DIFFERENT ENGINE IS
        /// THE WORST FAILURE THIS PROJECT COULD HAVE.
        ///
        /// The living engine is the StreamingAssets twin (2.4.1, 53 techs). Assets/Emergence/Engine
        /// held a 2.0.1 relic with 17 techs that this method would quietly fall back to if
        /// StreamingAssets were ever missing — no error, no warning, just a DIFFERENT WORLD from the
        /// same seed. Determinism is the product; a fallback that changes the simulation without
        /// saying so is not a safety net, it is a trapdoor.
        ///
        /// The relic is archived (see _ARCHIVE/2026-08-14-engine-relic/README.md — it cost a wrong
        /// codex audit to prove the point) and the fallback now FAILS LOUDLY instead of substituting
        /// an engine. If StreamingAssets is missing, that is a broken install and the honest answer
        /// is to say so, not to run something else and call it Emergence.</summary>
        public static string EngineSourcePath(string engineDir)
        {
            var sa = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Emergence", "emergence-engine.js");
            if (File.Exists(sa)) return sa;
            var legacy = Path.Combine(engineDir, "emergence-engine.js");
            if (File.Exists(legacy))
                throw new FileNotFoundException(
                    "The living engine (StreamingAssets/Emergence/emergence-engine.js) is missing, and " +
                    "Assets/Emergence/Engine holds an engine file. That file is a 2.0.1 RELIC with 17 techs; " +
                    "the game runs 2.6.0 with 53. Running it would silently produce a different world from " +
                    "the same seed. Restore StreamingAssets instead — see _ARCHIVE/2026-08-14-engine-relic/.");
            throw new FileNotFoundException("Engine source not found: " + sa);
        }

        /// <summary>Read engine + prelude (+ optional harness) from a directory laid out like Assets/Emergence/Engine/.</summary>
        public static EmergenceJintHost FromDirectory(string engineDir, bool withHarness)
        {
            var engine = File.ReadAllText(EngineSourcePath(engineDir));
            var prelude = File.ReadAllText(Path.Combine(engineDir, "harness", "prelude-hypot.js"));
            var harness = withHarness ? File.ReadAllText(Path.Combine(engineDir, "harness", "harness.js")) : null;
            return new EmergenceJintHost(engine, prelude, harness);
        }
    }
}
