// EMERGENCE — DJUPTIDS-DOMEN (§35, förregistrerad D-544): den RENA 32-fröns-mätningen av hela
// hållbarhets-triplet:en (jord §30 + skog §33 + klimat §34) mot bas-stacken, i editorn (Jint, snabbt).
// Moln-Node är för långsamt för 32×300 med full fysik (individ-simmar utan städer) — bekräftat D-543.
//
// Headless: släpp Reports/RUN_DEEPTIME.trigger.   Meny: Emergence/Våg C/RUN DJUPTIDS-DOM.
//
// Laddar den FÄRDIGPATCHADE stack-motorn ur Reports/rig/stack-forest-engine.js (metall v2 + jord +
// trappa + skog + KLIMAT, genererad ur committad B4 1493016c; rigg-SHA 66b1a64048bbefbd). Den
// LIVE-committade motorn RÖRS INTE — stack-motorn är bara riggindata (samma princip som skog-riggen).
//
// TVÅ ARMAR, båda på metall+trappa+T_AGG70 (delad pop-backstop 80 => avbryter i differensen):
//   FULL = jord + skog + klimat PÅ         BAS = alla tre hållbarhetslagar AV
// Hysteres de=50 (ren, proportionell mot T_AGG=70 — INTE de=20-snabbhacket som spökdödade i D-543).
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jint;
using Debug = UnityEngine.Debug;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class DeepTimeRig
    {
        static double _next;
        static string RigEngine => Path.Combine(Application.dataPath, "..", "Reports", "rig", "stack-forest-engine.js");
        static string EngineDir => Path.Combine(Application.dataPath, "Emergence", "Engine");
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_DEEPTIME.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "DEEPTIME_DONE.txt");
        static string Report    => Path.Combine(Application.dataPath, "..", "Reports", "deeptime-rig-report.txt");

        const string ExpectedRigSha = "66b1a64048bbefbd"; // stack-forest-engine.js med §34 klimat

        static readonly int[] Seeds = {
            4242,777,1234,8919,56433,97013,1066,900913,31337,90210,2718,31415,
            1618,1414,1732,2024,555,606,808,1010,1212,1313,1515,1717,
            1919,2121,2323,2525,2727,2929,3131,3333 };
        const int Years = 300;

        static DeepTimeRig() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch { } }
        }

        class Res {
            public int seed; public bool full; public int pop300; public bool ended;
            public int forest0, forestEnd; public int peak;
            public double climSum; public int climN;   // medel-klimat (endast FULL)
            public List<int> traj = new List<int>();
        }

        [MenuItem("Emergence/Våg C/RUN DJUPTIDS-DOM (jord+skog+klimat, ren 32-frön, in-editor)")]
        public static void Run()
        {
            var engineSrc = File.ReadAllText(RigEngine);
            string sha16;
            using (var sha = SHA256.Create())
            {
                var h = sha.ComputeHash(File.ReadAllBytes(RigEngine));
                var sbh = new StringBuilder(); foreach (var b in h) sbh.Append(b.ToString("x2"));
                sha16 = sbh.ToString().Substring(0, 16);
            }
            var prelude = File.ReadAllText(Path.Combine(EngineDir, "harness", "prelude-hypot.js"));

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — DJUPTIDS-DOMEN §35 (in-editor, Jint): jord+skog+klimat vs bas-stacken");
            sb.AppendLine($"genererad {DateTime.Now:yyyy-MM-dd HH:mm:ss}   {Seeds.Length} frön × {Years} år × 2 armar (FULL/BAS), T_AGG=70, de=50");
            sb.AppendLine($"rigg-motor SHA {sha16}  (väntat {ExpectedRigSha}: {(sha16 == ExpectedRigSha ? "MATCH" : "AVVIKER — kontrollera!")})");
            sb.AppendLine();
            if (sha16 != ExpectedRigSha)
                sb.AppendLine("  ⚠ VARNING: rigg-motorns SHA avviker från den §34-klimatpatchade — domen kan bygga på fel byte.");

            var fullR = new List<Res>();
            var basR  = new List<Res>();
            var sw = new Stopwatch(); sw.Start();

            foreach (var isFull in new[] { true, false })
            {
                var jint = new Jint.Engine(o => o.LimitRecursion(512));
                jint.Execute(prelude);
                jint.Execute(engineSrc);
                // metall + trappa i BÅDA; jord+skog+klimat bara i FULL
                jint.Evaluate("globalThis.__G29=true; globalThis.__LADDER=true; globalThis.__URBAN=false;");
                jint.Evaluate("globalThis.__SOIL="    + (isFull ? "true" : "false") + ";");
                jint.Evaluate("globalThis.__FOREST="  + (isFull ? "true" : "false") + ";");
                jint.Evaluate("globalThis.__CLIMATE=" + (isFull ? "true" : "false") + ";");
                foreach (var seed in Seeds)
                {
                    jint.Evaluate($"var __S=Emergence.createWorld({seed}); __S.silent=true; __S._aggT={{agg:70,de:50}}; __S._popCap=80;");
                    int forest0 = Convert.ToInt32(jint.Evaluate("(function(){var c=0;for(var y=0;y<Emergence.H;y++)for(var x=0;x<Emergence.W;x++)if(__S.tiles[y][x].t==='forest')c++;return c;})()").ToObject());
                    var r = new Res { seed = seed, full = isFull, forest0 = forest0 };
                    for (int y = 1; y <= Years; y++)
                    {
                        jint.Evaluate($"(function(){{var T={y}*Emergence.YEAR;while(__S.tick<T&&!__S.ended)Emergence.tickWorld(__S);}})()");
                        if (y % 10 == 0)
                        {
                            int p = Convert.ToInt32(jint.Evaluate("(function(){if(__S.ended)return 0;var n=0;for(var k=0;k<__S.agents.length;k++)if(!__S.agents[k].dead)n++;var cp=0;if(__S.aggregates)for(var i=0;i<__S.aggregates.length;i++){var c=__S.aggregates[i].cohorts;cp+=c[0]+c[1]+c[2]+c[3];}return n+Math.round(cp);})()").ToObject());
                            r.traj.Add(p);
                            if (p > r.peak) r.peak = p;
                            if (isFull)
                            {
                                var cl = jint.Evaluate("(__S._climate===undefined?1:__S._climate)").ToObject();
                                r.climSum += Convert.ToDouble(cl); r.climN++;
                            }
                        }
                        bool ended = Convert.ToBoolean(jint.Evaluate("__S.ended").ToObject());
                        if (ended) { r.ended = true; break; }
                    }
                    r.forestEnd = Convert.ToInt32(jint.Evaluate("(function(){var c=0;for(var y=0;y<Emergence.H;y++)for(var x=0;x<Emergence.W;x++)if(__S.tiles[y][x].t==='forest')c++;return c;})()").ToObject());
                    r.pop300 = r.traj.Count > 0 ? r.traj[r.traj.Count - 1] : 0;
                    (isFull ? fullR : basR).Add(r);
                }
            }
            sw.Stop();

            // ---------- måltal G1–G5 (förregistrerade §35, D-544) ----------
            double divFull = TrajDiversity(fullR), divBas = TrajDiversity(basR);
            int extFull = 0, extBas = 0;
            foreach (var r in fullR) if (r.ended) extFull++;
            foreach (var r in basR) if (r.ended) extBas++;
            int spanFull = PeakSpan(fullR), spanBas = PeakSpan(basR);
            int f1 = 0; foreach (var r in fullR) if (r.forest0 > 0 && (1.0 - (double)r.forestEnd / r.forest0) > 0.30) f1++;
            // G5: klimat-goda världar (övre tercil medel-klimat) toppar högre än dåliga (nedre tercil)
            double g5Hi, g5Lo; int g5nHi, g5nLo; ClimateTercile(fullR, out g5Hi, out g5Lo, out g5nHi, out g5nLo);

            bool G1 = divFull > 1.3 * divBas;
            bool G2 = extFull <= extBas + 2 && extFull <= 6;
            bool G3 = spanFull >= 1.3 * spanBas;
            bool G4 = f1 >= 8;
            bool green = G1 && G2 && G3 && G4;

            sb.AppendLine("MÅLTAL (förregistrerade §35, D-544 — satta FÖRE körning):");
            sb.AppendLine("  G1 · DIVERGENSEN VÄXER (kärnan): FULL " + divFull.ToString("0.000") + " · BAS " + divBas.ToString("0.000")
                + " (" + (divBas > 0 ? (divFull / divBas).ToString("0.00") : "inf") + "x)   MÅLTAL >1,3x: " + (G1 ? "OK" : "MISS"));
            sb.AppendLine("  G2 · REN KÖRNING: utdöda FULL " + extFull + "/32 · BAS " + extBas + "/32   MÅLTAL FULL≤BAS+2 OCH ≤6: " + (G2 ? "OK" : "MISS"));
            sb.AppendLine("  G3 · TOPPOP-SPANN VÄXER: FULL " + spanFull + " · BAS " + spanBas
                + " (" + (spanBas > 0 ? ((double)spanFull / spanBas).ToString("0.00") : "inf") + "x)   MÅLTAL ≥1,3x: " + (G3 ? "OK" : "MISS"));
            sb.AppendLine("  G4 · AVSKOGNING VERKLIG: >30% skogstapp i " + f1 + "/32 FULL-världar   MÅLTAL ≥8: " + (G4 ? "OK" : "MISS"));
            sb.AppendLine("  G5 · KLIMAT BITER (riktning): medel-toppop goda-epok-världar " + g5Hi.ToString("0")
                + " (n" + g5nHi + ") vs dåliga " + g5Lo.ToString("0") + " (n" + g5nLo + ")   (goda>dåliga = klimat syns: " + (g5Hi > g5Lo ? "JA" : "NEJ") + ")");
            sb.AppendLine();
            sb.AppendLine("  Kontext: baslinje-divergens (D-537) var 0.063; kontaminerad moln-riktning (D-543) FULL 0.092 / BAS 0.079; körtid " + sw.Elapsed.TotalMinutes.ToString("0.0") + " min");
            sb.AppendLine();
            sb.AppendLine("  VERDICT: " + (green
                ? "GRÖNT — hela hållbarhets-triplet:en ökar historisk divergens utan spök-kollaps ⇒ diska stacken (väg B, en ombaselinering)."
                : "RÖTT/NYANSERAT — läs G1(divergens)+G2(ren körning)+G3(spann); triplet:en gör inte allt den lovar."));

            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "RED")} sha={sha16} G1={(G1?1:0)} G2={(G2?1:0)} G3={(G3?1:0)} G4={(G4?1:0)} divFull={divFull:0.000} divBas={divBas:0.000} extFull={extFull} extBas={extBas} spanFull={spanFull} spanBas={spanBas} f1={f1}\nse Reports/deeptime-rig-report.txt\n");
            Debug.Log($"[DeepTimeRig] {(green ? "GREEN" : "RED")} — div {divFull:0.000} vs {divBas:0.000}, ext {extFull}/{extBas}, span {spanFull}/{spanBas}, F1 {f1}/32, {sw.Elapsed.TotalMinutes:0.0} min");
        }

        // normaliserad parvis trajektorie-diversitet (median) — samma mått som skog-riggen/D-537
        static double TrajDiversity(List<Res> rs)
        {
            var norm = new List<double[]>();
            foreach (var r in rs)
            {
                if (r.traj.Count == 0) continue;
                int mx = 1; foreach (var v in r.traj) if (v > mx) mx = v;
                var a = new double[r.traj.Count];
                for (int i = 0; i < r.traj.Count; i++) a[i] = (double)r.traj[i] / mx;
                norm.Add(a);
            }
            var ds = new List<double>();
            for (int i = 0; i < norm.Count; i++)
                for (int j = i + 1; j < norm.Count; j++)
                {
                    int n = Math.Min(norm[i].Length, norm[j].Length); if (n == 0) continue;
                    double d = 0; for (int k = 0; k < n; k++) d += Math.Abs(norm[i][k] - norm[j][k]);
                    ds.Add(d / n);
                }
            if (ds.Count == 0) return 0;
            ds.Sort();
            return ds[ds.Count / 2];
        }

        // toppop-spann = max−min av per-frö-topp (levande världar)
        static int PeakSpan(List<Res> rs)
        {
            int hi = int.MinValue, lo = int.MaxValue, n = 0;
            foreach (var r in rs) { if (r.peak <= 0) continue; n++; if (r.peak > hi) hi = r.peak; if (r.peak < lo) lo = r.peak; }
            return n >= 2 ? hi - lo : 0;
        }

        // klimat-tercil: dela FULL-frön i övre/nedre tredjedel på medel-klimat, jämför medel-toppop
        static void ClimateTercile(List<Res> rs, out double hiPop, out double loPop, out int nHi, out int nLo)
        {
            var live = new List<Res>();
            foreach (var r in rs) if (r.climN > 0 && r.peak > 0) live.Add(r);
            live.Sort((a, b) => (a.climSum / a.climN).CompareTo(b.climSum / b.climN));
            int t = Math.Max(1, live.Count / 3);
            double sHi = 0, sLo = 0; nHi = 0; nLo = 0;
            for (int i = 0; i < t; i++) { sLo += live[i].peak; nLo++; }
            for (int i = live.Count - t; i < live.Count; i++) { sHi += live[i].peak; nHi++; }
            hiPop = nHi > 0 ? sHi / nHi : 0; loPop = nLo > 0 ? sLo / nLo : 0;
        }
    }
}
#endif
