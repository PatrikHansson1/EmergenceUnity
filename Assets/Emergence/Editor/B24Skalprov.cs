// EMERGENCE — B2.4 SKALPROVET v2 (D-481, förregistrerat måltal; prep D-510, v1 OGILTIG D-511)
//
// Headless: släpp Reports/RUN_B24.trigger.   Meny: Emergence/Våg B/RUN B2.4 SKALPROV.
//
// FRÅGAN (D-481, måltal satt FÖRE mätning): bär aggregatlagret 10 000 själar i Jint
// inom 625 ms/tick?  DÖDSKRITERIUM: superlinjär kostnadsväxt med massan.
//
// V1:S LÄXA (metodlagen fungerade): massa(kvar)=0 avslöjade att v3-Malthus-hårdkapen +
// T_DE-re-individualiseringen krossade injicerad massa mot verklighetens K≈90 — mätningen
// såg kollapskaos, inte buren massa. V2 lyfter K i MINNET (KLIFT-mönstret från molnriggarna:
// K×lyft i testbygge, ALDRIG på disk): motorkällan läses, bas-SHA ASSERTERAS mot
// EmergenceJintHost.ExpectedEngineSha, kapacitetsraden får ×(globalThis.__KLIFT||1),
// och en BAR Jint-motor kör testbygget. Disken rörs aldrig; ett SHA-fel stoppar linjen.
//
// METOD: värld 4242 körs 15 år organiskt (KLIFT=1). Per mätpunkt (1k/5k/10k/20k):
// KLIFT=massa/100 (K≈massan — buren, inte skänkt bort: dämpning+hårdkap arbetar normalt
// mot lyft K), massa injiceras i S.aggregates, varmkörning 160 tick (≥1 årsskifte så
// aggregateTick arbetat mot massan), sedan 8×36-ticks-block; ms/tick per block + massa(kvar).
// LÄSNING: detta mäter aggregatmaskineriets MARGINALKOSTNAD ovanpå världens fasta tickkostnad
// (~140–260 ms i Jint, jfr golden-t720); individkurvan är JintPace:s mätning. Löftet som
// prövas: kohortmassa adderar ~0 per tick (O(byar) årsvis, O(aggregat) i Malthus-grinden).
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jint;
using Emergence.Runtime;
using Debug = UnityEngine.Debug;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class B24Skalprov
    {
        static double _next;
        static string EngineDir => Path.Combine(Application.dataPath, "Emergence", "Engine");
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_B24.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "B24_DONE.txt");
        static string Report    => Path.Combine(Application.dataPath, "..", "Reports", "b24-skalprov.txt");

        static readonly int[] Masses = { 1000, 5000, 10000, 20000 };
        const int Seed = 4242;
        const int WarmYears = 15;
        const double TargetMsPerTick = 625.0;   // D-481, aldrig flyttat

        const string CapAnchor  = "let cap=10+S._capSites*0.28+Math.min(S.fields.length,24)*1.8;";
        const string CapLifted  = "let cap=(10+S._capSites*0.28+Math.min(S.fields.length,24)*1.8)*(globalThis.__KLIFT||1);";

        static B24Skalprov() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch { } }
        }

        [MenuItem("Emergence/Våg B/RUN B2.4 SKALPROV (10k själar i aggregat)")]
        public static void Run()
        {
            // TESTBYGGE med spärrar: bas-SHA asserteras, KLIFT-ankaret måste finnas EXAKT en gång.
            var enginePath = EmergenceJintHost.EngineSourcePath(EngineDir);
            var baseSrc = File.ReadAllText(enginePath);
            var baseSha = EmergenceJintHost.Sha256Hex(baseSrc);
            if (!string.Equals(baseSha, EmergenceJintHost.ExpectedEngineSha, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"B24: bas-SHA {baseSha.Substring(0, 12)} != förväntad — stoppa linjen.");
            int hits = (baseSrc.Length - baseSrc.Replace(CapAnchor, "").Length) / CapAnchor.Length;
            if (hits != 1) throw new InvalidOperationException($"B24: KLIFT-ankaret finns {hits} ggr (ska vara 1).");
            var testSrc = baseSrc.Replace(CapAnchor, CapLifted);
            var prelude = File.ReadAllText(Path.Combine(EngineDir, "harness", "prelude-hypot.js"));

            var jint = new Jint.Engine(o => o.LimitRecursion(512));
            jint.Execute(prelude);
            jint.Execute(testSrc);

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — B2.4 SKALPROVET v2 (D-481): bär aggregatet 10 000 själar i Jint?");
            sb.AppendLine($"genererad {DateTime.Now:yyyy-MM-dd HH:mm:ss}   seed {Seed}, bas-SHA {baseSha.Substring(0, 12)} ASSERTERAD, K-lyft i minnet (disk orörd)");
            sb.AppendLine();

            jint.Evaluate($"var __S = Emergence.createWorld({Seed}); __S.silent = true; globalThis.__KLIFT = 1;");
            jint.Evaluate("(function(){var y=Emergence.YEAR;for(var i=0;i<" + WarmYears + "*y;i++){if(__S.ended)break;Emergence.tickWorld(__S);}})()");
            jint.Evaluate(
                "function __setMass(total){" +
                "  __S.aggregates.length=0;" +
                "  var vils=[]; for(var i=0;i<__S.villages.length;i++)vils.push(__S.villages[i].name);" +
                "  for(var k=0;k<5;k++){" +
                "    var nm=k<vils.length?vils[k]:('Synthetic-'+k);" +
                "    var m=total/5;" +
                "    __S.aggregates.push({village:nm,cohorts:[m*0.25,m*0.40,m*0.25,m*0.10],knowsUnion:[],wealth:0," +
                "      traitsM:{curiosity:{mean:0.5,sd:0.1},social:{mean:0.5,sd:0.1},diligence:{mean:0.5,sd:0.1}},bearers:[]});" +
                "  }" +
                "}");

            sb.AppendLine("  massa(mål)   KLIFT   massa(kvar)   levande   ms/tick medel   ms/tick värsta block   mot 625");
            sb.AppendLine("  " + new string('-', 92));

            var sw = new Stopwatch();
            double msAt10k = -1, msAt20k = -1, msAt5k = -1;
            const int chunks = 8, perChunk = 36;

            foreach (var mass in Masses)
            {
                double klift = mass / 100.0;
                jint.Evaluate($"globalThis.__KLIFT = {klift.ToString(System.Globalization.CultureInfo.InvariantCulture)};");
                jint.Evaluate($"__setMass({mass})");
                jint.Evaluate("(function(){for(var i=0;i<160;i++)Emergence.tickWorld(__S);})()"); // varmkörning över ≥1 årsskifte

                double total = 0, worst = 0;
                for (int c = 0; c < chunks; c++)
                {
                    sw.Restart();
                    jint.Evaluate("(function(){for(var i=0;i<" + perChunk + ";i++)Emergence.tickWorld(__S);})()");
                    sw.Stop();
                    double msPerTick = sw.Elapsed.TotalMilliseconds / perChunk;
                    total += msPerTick;
                    if (msPerTick > worst) worst = msPerTick;
                }
                double avg = total / chunks;
                double left = Convert.ToDouble(jint.Evaluate(
                    "(function(){var t=0;for(var i=0;i<__S.aggregates.length;i++){var c=__S.aggregates[i].cohorts;t+=c[0]+c[1]+c[2]+c[3];}return t;})()").ToObject());
                int alive = Convert.ToInt32(jint.Evaluate(
                    "(function(){var n=0;for(var k=0;k<__S.agents.length;k++)if(!__S.agents[k].dead)n++;return n;})()").ToObject());

                if (mass == 5000) msAt5k = avg;
                if (mass == 10000) msAt10k = avg;
                if (mass == 20000) msAt20k = avg;
                sb.AppendLine($"  {mass,10}   {klift,5:0}   {left,11:0}   {alive,7}   {avg,13:0.00}   {worst,20:0.00}   {(mass == 10000 ? (avg <= TargetMsPerTick ? "✓" : "✗") : " "),6}");
            }

            // ---- domen, räknad och inte tyckt ----
            bool m1 = msAt10k >= 0 && msAt10k <= TargetMsPerTick;
            double growth = (msAt10k > 0 && msAt20k > 0) ? msAt20k / msAt10k : -1;
            bool superlinear = growth > 2.5;
            sb.AppendLine();
            sb.AppendLine("## VAD DET BETYDER");
            sb.AppendLine($"  MÅLTAL (D-481): 10 000 själar ≤ {TargetMsPerTick:0} ms/tick — uppmätt {msAt10k:0.00} ms/tick  {(m1 ? "✓" : "✗")}");
            sb.AppendLine($"  DÖDSKRITERIUM superlinjäritet: 20k/10k = {growth:0.00}× (platt ≈ 1,0× förväntas; >2,5× fäller)  {(superlinear ? "✗ FÄLLD" : "✓")}");
            sb.AppendLine($"  (5k som form-kontroll: {msAt5k:0.00} ms/tick)");
            sb.AppendLine();
            sb.AppendLine("  GILTIGHETSVAKT: massa(kvar) ska vara samma storleksordning som massa(mål) — annars är");
            sb.AppendLine("  mätningen ogiltig (v1-läxan). Kostnaden som prövas är aggregatmaskineriets MARGINAL ovanpå");
            sb.AppendLine("  världens fasta Jint-kostnad (~140–260 ms/tick, jfr golden-t720); individkurvan äger JintPace.");

            string verdict = (m1 && !superlinear) ? "GREEN" : "RED";
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} msPerTick10k={msAt10k:0.00} growth20kOver10k={growth:0.00} v=2\nse Reports/b24-skalprov.txt (GILTIGHETSVAKT: kolla massa-kvar-kolumnen)\n");
            Debug.Log($"[B24Skalprov v2] {verdict} — 10k: {msAt10k:0.00} ms/tick (mål ≤625), 20k/10k {growth:0.00}×");
        }
    }
}
#endif
