// EMERGENCE — §33 SKOG/BRÄNSLE-RIGG i editorn (D-540/D-541): mät avskogning + trajektorie-divergens
// på deep-time-stacken, SNABBT i Jint (moln-Node var för långsamt — individ-simmar utan städer).
//
// Headless: släpp Reports/RUN_FOREST.trigger.   Meny: Emergence/Våg C/RUN SKOG-RIGG.
//
// Laddar den FÄRDIGPATCHADE stack-motorn ur Reports/rig/stack-forest-engine.js (metall v2 + jord +
// trappa + skog, genererad i molnet ur committad B4 1493016c). Den LIVE-committade motorn RÖRS INTE —
// stack-motorn är bara riggindata (samma princip som B24-skalprovets K-lyft: patch i minnet, disk orörd).
// Kör 32 frön × 300 år × 2 armar (skog PÅ/AV), T_AGG=70, registrerar pop-bana + skogsmängd.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Jint;
using Debug = UnityEngine.Debug;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class ForestRig
    {
        static double _next;
        static string EngineDir => Path.Combine(Application.dataPath, "Emergence", "Engine");
        static string RigEngine => Path.Combine(Application.dataPath, "..", "Reports", "rig", "stack-forest-engine.js");
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FOREST.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "FOREST_DONE.txt");
        static string Report    => Path.Combine(Application.dataPath, "..", "Reports", "forest-rig-report.txt");

        static readonly int[] Seeds = {
            4242,777,1234,8919,56433,97013,1066,900913,31337,90210,2718,31415,
            1618,1414,1732,2024,555,606,808,1010,1212,1313,1515,1717,
            1919,2121,2323,2525,2727,2929,3131,3333 };
        const int Years = 300;

        static ForestRig() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch { } }
        }

        class Res { public int seed; public bool forest; public int pop300; public bool ended;
            public int forest0, forestEnd; public bool coal; public List<int> traj = new List<int>(); }

        [MenuItem("Emergence/Våg C/RUN SKOG-RIGG (avskogning + divergens, in-editor)")]
        public static void Run()
        {
            var engineSrc = File.ReadAllText(RigEngine);
            var prelude = File.ReadAllText(Path.Combine(EngineDir, "harness", "prelude-hypot.js"));

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — §33 SKOG-RIGG (in-editor, Jint): avskogning + trajektorie-divergens");
            sb.AppendLine($"genererad {DateTime.Now:yyyy-MM-dd HH:mm:ss}   stacken (metall+jord+trappa+skog), T_AGG=70, {Seeds.Length} frön × {Years} år × 2 armar");
            sb.AppendLine();

            var onResults = new List<Res>();
            var offResults = new List<Res>();
            var sw = new Stopwatch(); sw.Start();

            foreach (var forestOn in new[] { true, false })
            {
                // en Jint-motor per arm (S skapas färskt per createWorld; globalerna består) — undviker 64 om-parsningar
                var jint = new Jint.Engine(o => o.LimitRecursion(512));
                jint.Execute(prelude);
                jint.Execute(engineSrc);
                jint.Evaluate("globalThis.__G29=true; globalThis.__SOIL=true; globalThis.__LADDER=true; globalThis.__URBAN=false;");
                jint.Evaluate("globalThis.__FOREST=" + (forestOn ? "true" : "false") + ";");
                foreach (var seed in Seeds)
                {
                    jint.Evaluate($"var __S=Emergence.createWorld({seed}); __S.silent=true; __S._aggT={{agg:70,de:50}}; __S._popCap=80;");
                    int forest0 = Convert.ToInt32(jint.Evaluate("(function(){var c=0;for(var y=0;y<Emergence.H;y++)for(var x=0;x<Emergence.W;x++)if(__S.tiles[y][x].t==='forest')c++;return c;})()").ToObject());
                    var r = new Res { seed = seed, forest = forestOn, forest0 = forest0 };
                    for (int y = 1; y <= Years; y++)
                    {
                        jint.Evaluate($"(function(){{var T={y}*Emergence.YEAR;while(__S.tick<T&&!__S.ended)Emergence.tickWorld(__S);}})()");
                        if (y % 10 == 0)
                        {
                            int p = Convert.ToInt32(jint.Evaluate("(function(){if(__S.ended)return 0;var n=0;for(var k=0;k<__S.agents.length;k++)if(!__S.agents[k].dead)n++;var cp=0;if(__S.aggregates)for(var i=0;i<__S.aggregates.length;i++){var c=__S.aggregates[i].cohorts;cp+=c[0]+c[1]+c[2]+c[3];}return n+Math.round(cp);})()").ToObject());
                            r.traj.Add(p);
                        }
                        bool ended = Convert.ToBoolean(jint.Evaluate("__S.ended").ToObject());
                        if (ended) { r.ended = true; break; }
                    }
                    r.forestEnd = Convert.ToInt32(jint.Evaluate("(function(){var c=0;for(var y=0;y<Emergence.H;y++)for(var x=0;x<Emergence.W;x++)if(__S.tiles[y][x].t==='forest')c++;return c;})()").ToObject());
                    r.coal = Convert.ToBoolean(jint.Evaluate("(!!(__S.knowledge&&__S.knowledge.coal))").ToObject());
                    r.pop300 = r.traj.Count > 0 ? r.traj[r.traj.Count - 1] : 0;
                    (forestOn ? onResults : offResults).Add(r);
                }
            }
            sw.Stop();

            // ---- måltal F1–F4 ----
            int f1 = 0; foreach (var r in onResults) if (r.forest0 > 0 && (1.0 - (double)r.forestEnd / r.forest0) > 0.30) f1++;
            double divOn = TrajDiversity(onResults), divOff = TrajDiversity(offResults);
            int extOn = 0, extOff = 0; foreach (var r in onResults) if (r.ended) extOn++; foreach (var r in offResults) if (r.ended) extOff++;
            // F4: kol-världar avskogas mindre (medel forestLoss kol vs ej-kol, ON-armen)
            double lossCoal = 0, lossNo = 0; int nC = 0, nN = 0;
            foreach (var r in onResults) { double loss = r.forest0 > 0 ? 1.0 - (double)r.forestEnd / r.forest0 : 0; if (r.coal) { lossCoal += loss; nC++; } else { lossNo += loss; nN++; } }
            lossCoal = nC > 0 ? lossCoal / nC : 0; lossNo = nN > 0 ? lossNo / nN : 0;

            sb.AppendLine("  F1 skogen kan tömmas: >30% skogstapp i " + f1 + "/32 världar   MÅLTAL ≥8: " + (f1 >= 8 ? "OK" : "MISS"));
            sb.AppendLine("  F2 DIVERGENSEN VÄXER: trajektorie-diversitet PÅ " + divOn.ToString("0.000") + " · AV " + divOff.ToString("0.000") + " (" + (divOff > 0 ? (divOn / divOff).ToString("0.00") : "inf") + "x)   MÅLTAL >1,3x: " + (divOn > 1.3 * divOff ? "OK" : "MISS"));
            sb.AppendLine("  F3 ingen kollaps: utdöda PÅ " + extOn + " · AV " + extOff + "   MÅLTAL ≤AV+2: " + (extOn <= extOff + 2 ? "OK" : "MISS"));
            sb.AppendLine("  F4 kol räddar: medel-skogstapp kol " + (lossCoal * 100).ToString("0") + "% vs ej-kol " + (lossNo * 100).ToString("0") + "%   (kol < ej-kol = transition syns: " + (lossCoal < lossNo ? "OK" : "MISS") + ")");
            sb.AppendLine();
            sb.AppendLine("  Kontext: baslinje-diversitet (D-537) var 0.063; körtid " + (sw.Elapsed.TotalMinutes).ToString("0.0") + " min");
            bool green = f1 >= 8 && divOn > 1.3 * divOff && extOn <= extOff + 2;
            sb.AppendLine();
            sb.AppendLine("  VERDICT: " + (green ? "GRÖNT — skogen ger avskogning + växande divergens; till deep-time-stacken." : "RÖTT/NYANSERAT — läs F1(avskogning)+F2(divergens)."));

            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "RED")} f1={f1} divOn={divOn:0.000} divOff={divOff:0.000} extOn={extOn} extOff={extOff}\nse Reports/forest-rig-report.txt\n");
            Debug.Log($"[ForestRig] {(green ? "GREEN" : "RED")} — F1 {f1}/32, div {divOn:0.000} vs {divOff:0.000}, {sw.Elapsed.TotalMinutes:0.0} min");
        }

        // normaliserad parvis trajektorie-diversitet (median)
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
    }
}
#endif
