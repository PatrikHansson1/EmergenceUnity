// EMERGENCE — D-222: THE GAME SHIPS IN ENGLISH, AND A PROBE ENFORCES IT.
//
// Patrik caught this after the ornament pass: "glöm inte att spelet ska vara helt på engelska, inte
// svenska ... inkluderat huddens rubriker etc". He was right and it would have shipped. A sweep of
// the runtime found 117 player-facing Swedish strings — the ENTIRE Almanac (seven tab names, every
// header, every dossier label, every empty state), the Chronicle's headings and filters, the time
// panel's ÅR and PAUSAD, the four voice registers, and — worst — the game's central promise line
// "skriven av ingen, allt hände" inside the shareable HTML chronicle a player exports and posts.
//
// WHY IT HAPPENED, so the lesson survives the fix. The studio thinks, writes its decisions, and
// talks to its owner in Swedish. The engine emits English ("a hut is raised", "a first child is
// born"), so the CONTENT was always English and only the CHROME drifted — which is precisely the
// half nobody re-reads, because you stop seeing your own furniture. A word we typed once in year one
// stays until someone looks at the screen as a stranger.
//
// A translation pass fixes today. Only a probe fixes tomorrow: every new label is written by someone
// thinking in Swedish, so without this the drift resumes on the next view.
//
// SCOPE, stated so the rule is enforceable rather than moralistic. This checks STRING LITERALS in
// RUNTIME code — the text that can reach a player. It deliberately does NOT check:
//   - comments (the studio reasons in Swedish and should keep doing so; that is the archive)
//   - Editor/ and probe code (internal instruments, read by us, never by a buyer)
//   - report and log text (evidence, not product)
// Trigger: Reports/RUN_LANG.trigger
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class LanguageProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_LANG.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "LANG_DONE.txt");
        const string Report   = "Reports/language-report.txt";

        // Swedish letters are the cheap half of the test. The expensive half is the words that
        // contain none of them — "allt", "stäng" without its ä, "poster", "gen", "by". A word list
        // beats a letter class here, and it is honest about being a list rather than a language.
        // FALSE FRIENDS ARE THE WHOLE DIFFICULTY. The first list contained "by", "under", "till",
        // "med", "post" and "all" — every one of them an ordinary English word, so the probe's first
        // run flagged its own freshly-translated English, including the game's central promise line.
        // A test that cries wolf gets switched off, so this list now holds ONLY words that cannot be
        // English. The Swedish-letter check below carries the rest, and carries it reliably.
        static readonly string[] SwedishWords =
        {
            "allt","alla","ingen","inga","inget","eller","inte","utan","inom","varje","mest","minst",
            "andra","första","sista","nya","gamla","stäng","stang","avbryt","tillbaka","hände","hande",
            "poster","namn","alder","byar","sjal","sjalar","hyddor","hydda","rikedom","hantverk",
            "ledare","medelalder","befolkning","fodda","doda","stolder","kronikan","kronika",
            "almanacken","almanacka","oversikt","samhalle","teknik","dynastier","vandpunkter",
            "markbart","pausad","skriven","vantar","motorns","varlden","varld","egenskaper","klicka",
            "levande","presenterade","framsta","erkand","varje","nagon","nagot","mycket","ocksa"
        };

        // Instruments, not product: these WRITE EVIDENCE FILES for us and are never read by a buyer.
        // Naming them here rather than widening the word list keeps the rule about the player.
        static readonly string[] ExemptFiles =
        { "PlayerProof.cs", "PerfRun.cs", "AutoCompile.cs", "WorldDresser.cs", "PresentationEventBus.cs" };

        static readonly char[] SwedishLetters = { 'å', 'ä', 'ö', 'Å', 'Ä', 'Ö' };

        static LanguageProbe() { EditorApplication.update += Poll; }

        [MenuItem("Emergence/Fas1/RUN LANGUAGE PROBE")]
        public static void RunMenu() => Run();

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.5;
            try
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Trigger))
                {
                    File.Delete(Trigger);
                    Run();
                }
            }
            catch (Exception e) { Debug.LogWarning("[LanguageProbe] " + e.Message); }
        }

        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — LANGUAGE PROBE: does the game speak English?");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("scope: STRING LITERALS in runtime code. Comments, Editor/ and reports are");
            sb.AppendLine("  deliberately exempt — the studio reasons in Swedish and that is the archive.");
            sb.AppendLine();

            var root = Path.Combine(Application.dataPath, "Emergence");
            var files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                                 .Where(f => f.Replace('\\', '/').IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) < 0)
                                 .Where(f => !ExemptFiles.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                                 .OrderBy(f => f).ToArray();

            var findings = new List<string>();
            int scanned = 0, literals = 0;
            var rx = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"");

            foreach (var f in files)
            {
                scanned++;
                var lines = File.ReadAllLines(f);
                for (int i = 0; i < lines.Length; i++)
                {
                    var raw = StripComment(lines[i]);
                    var t = raw.TrimStart();
                    if (t.Length == 0 || t.StartsWith("*") || t.StartsWith("/*")) continue;
                    // [Tooltip] and [Header] are INSPECTOR text — read by us in the editor, never by
                    // a player. The first run flagged them and it was right to be told, and wrong to
                    // call them defects.
                    if (t.StartsWith("[Tooltip") || t.StartsWith("[Header") || t.Contains("Debug.Log")) continue;
                    foreach (Match m in rx.Matches(raw))
                    {
                        var s = m.Groups[1].Value;
                        if (s.Length < 2) continue;
                        literals++;
                        if (!Suspect(s)) continue;
                        findings.Add("  " + Rel(f) + ":" + (i + 1) + "  \"" + (s.Length > 90 ? s.Substring(0, 90) + "…" : s) + "\"");
                    }
                }
            }

            sb.AppendLine("scanned " + scanned + " runtime files, " + literals + " string literals");
            sb.AppendLine();
            if (findings.Count == 0)
                sb.AppendLine("  PASS  no Swedish found in any player-facing string");
            else
            {
                sb.AppendLine("  FAIL  " + findings.Count + " player-facing strings still speak Swedish:");
                foreach (var x in findings.Take(60)) sb.AppendLine(x);
                if (findings.Count > 60) sb.AppendLine("  … and " + (findings.Count - 60) + " more");
            }
            sb.AppendLine();
            sb.AppendLine("VERDICT: " + (findings.Count == 0 ? "GREEN" : "RED"));
            sb.AppendLine("declared: a word list is not a language. This catches the drift we actually make —");
            sb.AppendLine("  a Swedish label typed by someone thinking in Swedish — and it will not catch a");
            sb.AppendLine("  sentence that happens to avoid every word in the list. Widen the list when one slips.");

            Directory.CreateDirectory("Reports");
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, (findings.Count == 0 ? "GREEN" : "RED " + findings.Count)
                                    + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Debug.Log("[LanguageProbe] -> " + Report);
        }

        static bool Suspect(string s)
        {
            if (s.IndexOfAny(SwedishLetters) >= 0) return true;
            foreach (var w in Regex.Matches(s.ToLowerInvariant(), "[a-zåäö]+").Cast<Match>())
                if (Array.IndexOf(SwedishWords, w.Value) >= 0) return true;
            return false;
        }

        /// <summary>Drop a trailing // comment without cutting inside a string — the first version
        /// only skipped lines that BEGAN with //, so a Swedish word in a trailing comment counted as
        /// a player-facing defect three times over.</summary>
        static string StripComment(string line)
        {
            bool inStr = false, esc = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (esc) { esc = false; continue; }
                if (c == '\\' && inStr) { esc = true; continue; }
                if (c == '"') { inStr = !inStr; continue; }
                if (!inStr && c == '/' && i + 1 < line.Length && line[i + 1] == '/') return line.Substring(0, i);
            }
            return line;
        }

        static string Rel(string f)
        {
            var p = f.Replace('\\', '/');
            int k = p.IndexOf("/Assets/", StringComparison.Ordinal);
            return k >= 0 ? p.Substring(k + 1) : p;
        }
    }
}
#endif
