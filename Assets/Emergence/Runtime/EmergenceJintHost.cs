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
        public const bool EpochActive = true; // D-577 (ordförandebeslut 2026-08-29): EPOK-PAKETET AKTIVERAT — kanon-världarna är epok-världar. Rollback = false (en rad) + återställ goldens från git-historiken.
        public const string ExpectedEngineSha = "f39e06848f9128dcc32d0c7f1c341944aba8610e19889f12663cb53264a88a61"; // + EMISSIONSKONTRAKTET S.ledger (D-649..D-653, 2026-09-01, autonom): additive top-level field OUTSIDE the golden payload — K1 deadBook, K2 births, K3 extract/depleted, K4 meals, K5 teach, K6 trade, K7 village relations, K8 travel, K9 stripped, K10 huts/fires/wood/smelt/tribute, K11 year snapshot, K12 hardship, K13 cohort deaths; write-only from mechanics (no S.rand, no ev, never read in tick logic), keys = ids/indices per (year, village index). Generated reproducibly by Reports/rig/ledger/mkledger.py (33 anchors). All 8 goldens (720+8640) BYTE-IDENTICAL in cloud V8; M-b consistent on 7 canon seeds x 120y; V8 time +3.5% (<=5%); ~2 kB/year. Exported as `ledger` in Fas3SimDriver.ExportJs next to pathUse. Prior sha (H0(a) city stability): 8907f6f6aa53... // + STADS-STABILITET H0(a) (D-617/D-620..D-623, 2026-08-31, autonom): POPCAP-fallback fold now requires catchment >= T.de+15 (was 12) so a city is never born already below its de-fold threshold. Rig: 8 canon seeds x 300y — one-year city flicker (17–41 folds/seed) -> single city holding 118–172y; T.agg worlds untouched (777 byte-identical). All 8 goldens (720+8640) BYTE-IDENTICAL in cloud V8 (first fold in any seed is y96+ > 60y horizon). Prior sha (text-only rev): d02bb1eebc63... // + TEXT-ONLY REV (D-614, 2026-08-31, autonom): the two aggregate chronicle lines (fold/de-fold) were Swedish — now English. Simulation byte-identical; event TEXT changes ⇒ goldens RED-by-intent → divergent-*.canon.txt verified text-only → new canon. Prior sha (§48 institutionsmodellen): cf5cba658777... // + INSTITUTIONSMODELLEN v2 (D-581/D-584, §48): J-A institution carries knowledge (checkExtinct counts living city knowsUnion as bearers — guild tech survival 0%->100%), J-B ruins remember (v._legacy on de-fold, 70%/tech FNV survival at next fold). All 8 activation goldens verified BYTE-IDENTICAL in cloud (no canon reaches these paths in 60y). I3 100% green; I1/I2 red-by-one inherited by future city-stability §. Prior sha: e1feedf482b7... // + AKTIVERINGS-DEFAULTS (D-577): under __PACE är default-aggT {70,50} + backstop 80 = exakt den 10-domar-mätta konfigurationen (gated; dormant byte-identisk mot gamla goldens verifierad i moln). Goldens OMSPELADE under full fysik (moln-node = Jint, byte-identiskt bevisat); founders-kanonen flyttad 4242→97013 (A1: 4242-founders dör under full fysik — kanonval är innehåll, Patrik-val). Prior sha (produktionskedjorna): ce718a28666b... // + PRODUKTIONSKEDJORNA (D-570..D-572, §46-§47, __PROD DORMANT): deer drops hide+bone; hide->clothes (coldDrain x0.55), bone->fishhooks (fishing 42->58), mill-harvest grain->bread (+35 at fire), iron->plow (harvest +25, grain 2->3) — four chains, chronicle events, dormancy golden-verified byte-identical vs checked-in goldens before every run. Prior sha (epok-paketet): 6bcdce458a01... // + knowledgeMax canon-guard (RED-triage 2026-08-29: TECHS.length 53->62 leaked into DNA metadata; guarded, cloud-verified byte-identical vs checked-in goldens seeds 97013/4242/20260718 t720). + EPOK-PAKETET (D-548..D-566, §37-§45): ALL DORMANT behind __PACE/__G29/__SOIL/__LADDER/__FOREST/__CLIMATE (canon runs set NONE) — metal-v2 deposits+guarantee, soil FT, capital ladder, forest seed-source, climate epochs, maturation law 170/60 (8 verdicts in band), city-as-mind (guild invention S1/S2, need-driven research S3, agrarian-scaled soil S4), village osmosis, quality engine dq∝use/q (no cap), 9 era7-10 techs (electricity..spaceflight/AI) with era>=7 canon-guard. Canon-NEUTRAL PROVEN: full world digest identical vs 1493016c on 4 canon seeds x 120y (D-566). Fire->Moon/AI demonstrated: seed 31415 AI@1115/space@1294, seed 2323 space@2846 alive@3000. Prior sha (B4): 1493016cacf8... // + B4 (D-507/D-508): sickness DORMANT on canon horizons — fever pressure q=0.02*max(0,N-50)/50 per person-year over catchment mass (living + cohorts, B2.2a contract), child x2/elder x3, medicine-bearer x0.5, aqueduct x0.7; deterministic, no new rand stream below threshold => canon-NEUTRAL all 8 (cloud facit, before==after==live canons). Prior sha (B2.2a): 73c79441cceb... // + B2.2a (D-500): aggregate switch DORMANT (T_AGG=200 catchment never reached by canon worlds) — cohort math (validated rates, n=10), 15 bearers, Malthus-gate + berry-ecology coupling, FNV re-individualization; rig-proven 6 versions, goldens UNCHANGED green = proof. // + B2.0 (D-481): villageAggregate(S,v) — cohort mirror, pure read, canon-NEUTRAL (goldens unchanged, green = proof of no leak). // + GEO2 (D-480): ore in DEPOSITS not uniform (2 centres/material, FNV, radius 6) — variation +20% pairwise divergence; trade -> Wave C. // + B5b (D-457): trait-weighted need gates (hunger/warmth/social/sleep + social order-flip); D 0.337, gain +0.115 on F1.4 engine, no harm. // + F1.4 (D-451): thirst -- init 80, drain (season x pottery x dry-year), ~1/7 dry years via FNV, opportunistic+seeking drink, dehydration WEAKENS (energy -1.5) not kills. Slutprov 32 seeds: W1 0.33, 1 dead/32 (31337 lives), 0 thirst-deaths. Worldgen+agentinit change -> goldens RED from first tick by design. Prior sha (F1.2d): 8a473f0dff7d...

        // P1 (D-605/D-610): the presentation layer is a SEPARATE file with its OWN sha — it never touches the engine sha above.
        // A pure read over S (writeIntervalReport); loaded after the engine when a host supplies it. Null = not loaded (golden/harness paths unchanged).
        public const string ExpectedPresentationSha = "51cca887ea7579cdf8a0f82d956691fcf269606f51c9f5e4e40ff6267b70833b"; // P1 v0.6 (D-648/D-656, 2026-09-01): tagFor uses the by-tag instead of a /144 division ("(born 0)" gone -> "of Stenholm"), SENTENCES{legend:2,customLost:2}, weightOf knowledgeLost scoped 20 (village-level while the tech lives) / 45 (world-level). P1-goldens on engine v20: 97013 460e621f / 4242 99e2a8fb / 20260718 699a93fa (Reports/rig/p1/pending-v06/p1-goldens-v06-on-v20.json). Prior sha (v0.5): 1c7ba2da... // emergence-presentation.js v0.5 (D-626/D-632: WEIGHT keyed to the engine's real 48 ev-types; P1-goldens on 8907f6f6). Prior: 38ffc100… (v0.4) // v0.4 (D-615/D-619: aggregate mask removed — engine v18 lines are English; P1-goldens on d02bb1ee). Prior: 3b1c7134… (v0.3) // v0.3 (D-613: actor spread; P1-goldens Reports/rig/p1/p1-goldens.json). Prior: 22438a87… (v0.2)

        private readonly Jint.Engine _engine;
        public string EngineSha256 { get; }
        public string PresentationSha256 { get; private set; }

        /// <param name="engineSrc">Verbatim engine source (emergence-engine.js).</param>
        /// <param name="preludeSrc">Math.hypot host prelude source.</param>
        /// <param name="harnessSrc">Optional golden-master harness source (test builds).</param>
        /// <param name="presentationSrc">Optional P1 presentation source (emergence-presentation.js) — sha-guarded separately (D-610).</param>
        public EmergenceJintHost(string engineSrc, string preludeSrc, string harnessSrc = null, string presentationSrc = null)
        {
            EngineSha256 = Sha256Hex(engineSrc);
            if (!string.Equals(EngineSha256, ExpectedEngineSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"ENGINE SHA MISMATCH — stop the line (UNITY-BRIDGE-SPEC §5). Expected {ExpectedEngineSha}, got {EngineSha256}. " +
                    "The engine copy has drifted from the recorded baseline; re-baseline consciously via the golden-master protocol.");

            _engine = new Jint.Engine(o => o.LimitRecursion(512));
            _engine.Execute(preludeSrc);   // host adaptation — legal; engine edits are not
            if (EpochActive) _engine.Execute("globalThis.__G29=true;globalThis.__SOIL=true;globalThis.__LADDER=true;globalThis.__FOREST=true;globalThis.__CLIMATE=true;globalThis.__PACE=true;globalThis.__PROD=true;"); // D-577: väckningen
            _engine.Execute(engineSrc);
            if (!string.IsNullOrEmpty(presentationSrc))
            {
                PresentationSha256 = Sha256Hex(presentationSrc);
                if (!string.Equals(PresentationSha256, ExpectedPresentationSha, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"PRESENTATION SHA MISMATCH — stop the line (D-610). Expected {ExpectedPresentationSha}, got {PresentationSha256}. " +
                        "emergence-presentation.js drifted from its recorded sha; bump ExpectedPresentationSha consciously with the P1-text golden.");
                _engine.Execute(presentationSrc); // pure read layer — never writes S, never draws randomness
            }
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

        /// <summary>P1 (D-610): the presentation file lives beside the living engine in StreamingAssets. Returns null when absent
        /// (a missing presentation layer is a degraded chronicle, not a broken install — unlike the engine).</summary>
        public static string PresentationSourcePath()
        {
            var sa = Path.Combine(UnityEngine.Application.streamingAssetsPath, "Emergence", "emergence-presentation.js");
            return File.Exists(sa) ? sa : null;
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
