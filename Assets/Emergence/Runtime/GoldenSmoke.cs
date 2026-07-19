// EMERGENCE — player-build golden smoke (BUILD-AUTOMATION §2).
// A build that renders but simulates wrong is not green: the shipped player must
// reproduce the checked-in golden. Runs when launched with -goldenSmoke; writes
// smoke-result.txt beside the exe and exits 0 (PASS) / 1 (FAIL).
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Emergence.Runtime
{
    public class GoldenSmoke : MonoBehaviour
    {
        // Reference values regenerated 2026-07-19 against engine 1.2.1 (see Engine/harness/goldens/GOLDENS-MANIFEST.md)
        public const long SmokeSeed = 97013;
        public const int SmokeTicks = 1440;
        public const string ExpectedCanonSha = "dba53207c7f7e004a322933561de6fcd2fa1fbebdd4c3eb75442036a0b7ef65f";

        private void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (!args.Contains("-goldenSmoke")) return;
            int exit = 1;
            var resultPath = Path.Combine(Application.dataPath, "..", "smoke-result.txt");
            try
            {
                var dir = Path.Combine(Application.streamingAssetsPath, "Emergence");
                var host = EmergenceJintHost.FromDirectory(dir, withHarness: true);
                var t0 = Time.realtimeSinceStartup;
                var canon = host.RunGolden(SmokeSeed, SmokeTicks);
                var sha = EmergenceJintHost.Sha256Hex(canon);
                var pass = string.Equals(sha, ExpectedCanonSha, StringComparison.OrdinalIgnoreCase);
                exit = pass ? 0 : 1;
                File.WriteAllText(resultPath,
                    (pass ? "PASS" : "FAIL") +
                    $"\nseed={SmokeSeed} ticks={SmokeTicks}\nengineSha={host.EngineSha256}\ncanonSha={sha}\nexpected={ExpectedCanonSha}\nseconds={Time.realtimeSinceStartup - t0:F1}\nunity={Application.unityVersion} platform={Application.platform}\n");
                Debug.Log($"[GoldenSmoke] {(pass ? "PASS" : "FAIL")} canonSha={sha}");
            }
            catch (Exception e)
            {
                try { File.WriteAllText(resultPath, "FAIL (exception)\n" + e + "\n"); } catch { }
                Debug.LogError("[GoldenSmoke] " + e);
            }
            Application.Quit(exit);
        }
    }
}
