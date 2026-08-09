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
        public const string ExpectedEngineSha = "91e5535db3dfaae4a1ce3fafcb70e088cd48425489abb7e65f6e8b1f0c3ec7b0"; // ENGINE 2.4.1 (E1.5b hardening, D-176: review conditions V1 rebuilt leader rule [unclamped standing + absolute sqrt-eased lead], V2 feud trigger reads grudge bookkeeping [ev: by construction], V6 steal/raid drive-chains [winter-onset season events, per-village famine marks, hoard milestone events], V8 feud room law in writeHistory; histories DIVERGE from 2.4.0 by design — goldens re-baselined)

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
        public static string EngineSourcePath(string engineDir)
        {
            var sa = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Emergence", "emergence-engine.js");
            if (File.Exists(sa)) return sa;
            return Path.Combine(engineDir, "emergence-engine.js");
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
