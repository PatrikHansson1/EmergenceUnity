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
        public const string ExpectedEngineSha = "22e2ac053cd820b5656f8fa04b1392dde7c650c65d8f19df633f33b3f46d7020"; // ENGINE 2.4.1 + villageScope export-only readout (MOTOR-LANE-ORDER-VILLAGE-SCOPE 2026-08-09: per-village {name,pop,maxGen,avgAge,crafts,knows} census for the C-condition's loss half; pure READ, no S.rand, no S mutation — sim stream byte-identical, goldens NOT re-baselined [2.3.1 precedent: only the engine file's own SHA shifts]. Prior 2.4.1 sha: 91e5535db3df…)

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
                    "the game runs 2.4.1 with 53. Running it would silently produce a different world from " +
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
