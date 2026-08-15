// EMERGENCE — P0: HUR LÅNGSAM ÄR MOTORN DÄR SPELET FAKTISKT KÖR? (D-259)
//
// Headless: släpp Reports/RUN_JINTPACE.trigger.
//
// VARFÖR DEN HÄR MÄTNINGEN FÖRST, FÖRE EN ENDA KANONRAD:
//   Hela djuptidsplanen vilar på ett tal ingen har mätt sedan juli. D-258 mätte motorn i V8
//   till 1,4–1,7 s per simulerat år vid pop ~120, vilket ger 3 000 år på ~78 minuter ren
//   beräkning — hanterbart, och efter spatial grid trivialt. Men SPELET kör inte i V8. Det kör
//   i Jint, inne i Unity, och den enda siffra vi har för den skillnaden är en anteckning från
//   juli om 160 år på 61 minuter i sandlådan mot 29 sekunder i V8 — omkring 126x, uppmätt på
//   en annan motorversion, i en annan miljö, och aldrig verifierad sedan.
//
//   Om multiplikatorn fortfarande är av den storleken kostar 3 000 år över 160 timmar i spelet
//   och HELA planen behöver ett portningsbeslut INNAN P2, inte efter. Om den är 5–10x går det.
//   Det är skillnaden mellan två helt olika projekt, och den avgörs av ett tal.
//
//   D-259 skrev avbrottskriteriet i förväg, så det inte kan förhandlas efteråt:
//       Jint > 20x V8  ->  portningsbeslutet lyfts före P2.
//
// Metodlagen som gäller: skriv ut mellanleden. Rapporten skriver kostnaden PER MÄTPUNKT med
// befolkningen bredvid, inte bara ett medelvärde — för det är kurvans FORM som avgör om det är
// O(pop^2) som biter eller ett fast overhead, och ett medelvärde döljer exakt den skillnaden.
#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;
using Debug = UnityEngine.Debug;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class JintPace
    {
        static double _next;
        static string EngineDir => Path.Combine(Application.dataPath, "Emergence", "Engine");
        static string Trigger   => Path.Combine(Application.dataPath, "..", "Reports", "RUN_JINTPACE.trigger");
        static string Done      => Path.Combine(Application.dataPath, "..", "Reports", "JINTPACE_DONE.txt");
        static string Report    => Path.Combine(Application.dataPath, "..", "Reports", "jint-pace.txt");

        /// <summary>V8-referensen ur D-258, mätt på samma motor (2.6.0), samma seed, samma år.
        /// Sekunder per simulerat år vid den befolkning mätpunkten faktiskt hade.</summary>
        static readonly (int year, double v8Seconds)[] V8Reference =
        {
            (  20, 0.110 ), (  40, 0.074 ), (  60, 0.144 ), (  80, 0.290 ),
            ( 100, 0.604 ), ( 120, 1.125 ), ( 140, 1.482 ), ( 160, 1.676 ),
        };

        const int Seed  = 4242;   // samma seed som D-255/D-258, så talen är jämförbara
        const int Years = 160;    // samma horisont som V8-mätningen

        static JintPace() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch { } }
        }

        [MenuItem("Emergence/P0/RUN JINT PACE (hur långsam är motorn i spelet?)")]
        public static void Run()
        {
            var host = EmergenceJintHost.FromDirectory(EngineDir, withHarness: false);
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — JINT PACE (D-259, port P0): kostnaden per simulerat år DÄR SPELET KÖR");
            sb.AppendLine($"genererad {DateTime.Now:yyyy-MM-dd HH:mm:ss}   seed {Seed}, {Years} år, motor-SHA {host.EngineSha256.Substring(0, 12)}");
            sb.AppendLine();
            sb.AppendLine("  år   levande   byar    Jint s/år    V8 s/år    multiplikator");
            sb.AppendLine("  " + new string('-', 62));

            // Världen skapas en gång och lever i Jint-heapen genom hela mätningen; C# driver bara
            // klockan. Ett år i taget, aldrig en batch — annars mäter vi genomsnittet och tappar formen.
            host.Engine.Evaluate($"var __S = Emergence.createWorld({Seed}); __S.silent = true;");
            var oneYear = "(function(){var y=Emergence.YEAR;for(var i=0;i<y;i++){if(__S.ended)break;Emergence.tickWorld(__S);}" +
                          "var n=0;for(var k=0;k<__S.agents.length;k++)if(!__S.agents[k].dead)n++;" +
                          "return n*1000 + (__S.villages.length||0);})()";

            var sw = new Stopwatch();
            double worst = 0, atYear = 0, lastSecs = 0;
            int lastAlive = 0, lastVill = 0;

            for (int year = 1; year <= Years; year++)
            {
                sw.Restart();
                var packed = Convert.ToInt32(host.Engine.Evaluate(oneYear).ToObject());
                sw.Stop();
                double secs = sw.Elapsed.TotalSeconds;
                int alive = packed / 1000, vill = packed % 1000;
                lastSecs = secs; lastAlive = alive; lastVill = vill;

                foreach (var r in V8Reference)
                {
                    if (r.year != year) continue;
                    double mult = r.v8Seconds > 0 ? secs / r.v8Seconds : 0;
                    if (mult > worst) { worst = mult; atYear = year; }
                    sb.AppendLine($"  {year,4}   {alive,7}   {vill,4}    {secs,9:0.000}  {r.v8Seconds,9:0.000}    {mult,10:0.0}x");
                }
            }

            // ---- domen, räknad och inte tyckt ----
            double hours3000 = lastSecs * 3000.0 / 3600.0;
            sb.AppendLine();
            sb.AppendLine("## VAD DET BETYDER");
            sb.AppendLine($"  sista mätpunkten: {lastSecs:0.000} s per simulerat år vid pop {lastAlive}, {lastVill} byar");
            sb.AppendLine($"  3 000 år i den takten, UTAN spatial grid: {hours3000:0.0} timmar ren beräkning");
            sb.AppendLine($"  samma sak med grid (antag samma 7,5x som V8-uppskattningen): {hours3000 / 7.5:0.0} timmar");
            sb.AppendLine($"  värsta uppmätta multiplikator mot V8: {worst:0.0}x (år {atYear:0})");
            sb.AppendLine();

            // Avbrottskriteriet stod skrivet i D-259 INNAN mätningen kördes. Det är hela poängen
            // med ett avbrottskriterium: det får inte förhandlas när talet väl står på skärmen.
            string verdict = worst > 20.0 ? "PORTNINGSBESLUT" : "GRÖNT";
            sb.AppendLine(worst > 20.0
                ? $"  ✗ {worst:0.0}x > 20x — D-259:s avbrottskriterium slår till. PORTNINGSBESLUTET LYFTS FÖRE P2.\n" +
                  "    Motorlogiken ändras fortfarande i JS i huvudlinjen; det som ska beslutas är om de\n" +
                  "    heta vägarna körs som C# under bit-likhetsgrind (trimningsstegen, JINT-GOLDEN §8)."
                : $"  ✓ {worst:0.0}x ≤ 20x — Jint bär planen. Ingen portning behövs före P2.");
            sb.AppendLine();
            sb.AppendLine("Läsning: kolumnen som betyder något är MULTIPLIKATORN, inte sekunderna. Sekunderna");
            sb.AppendLine("beror på maskinen; multiplikatorn beror på Jint, och det är Jint planen vilar på.");

            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} worstMult={worst:0.0} lastSecsPerYear={lastSecs:0.000} pop={lastAlive}\nse Reports/jint-pace.txt\n");
            Debug.Log($"[JintPace] {verdict} — värsta multiplikator {worst:0.0}x mot V8, {lastSecs:0.000} s/år vid pop {lastAlive}");
        }
    }
}
#endif
