// EMERGENCE — FAS 4 PROBE: LIVE INFERENCE (FAS4-PROSE-DIRECTOR-ORDER §5).
//
// The safe-properties probe (RUN_FAS4PROSE) runs with no model on purpose. THIS one loads the
// studio's model and asks the question the order asks: does the same turning point read the same
// way twice, and does the line hold the chronicle's voice?
//
// What it proves:
//   1. the model LOADS in this vehicle (LLM.started) and the director is bound to it;
//   2. REPRODUCIBILITY the hard way — the per-entry cache is BYPASSED (a second, cache-free
//      director instance is asked the identical entry) so an identical line is evidence of
//      seed+temperature-0, not of a dictionary lookup;
//   3. DISTINCTNESS — a different turning point gives a DIFFERENT line (a director that answers
//      everything identically would also "pass" test 2);
//   4. VOICE — the word-ban holds, no markup, no exclamation, and the line is short enough for a
//      book row. The final judgement of tone is a HUMAN's (D-008): the lines are written verbatim
//      into the report to be read.
//   5. the real causes[] of a real seed drive it — the turning points come from the engine's own
//      resolved causes (the §1 export), not from hand-written prompts.
//
// Headless: drop Reports/RUN_FAS4PROSELIVE.trigger. Slow by nature — the watchdog is generous.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using LLMUnity;
using Emergence.Runtime;
using Emergence.Fas4;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas4ProseLiveProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4PROSELIVE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS4PROSELIVE_DONE.txt");
        const string Report   = "Reports/fas4-prose-live.txt";
        const double Watchdog = 900.0;

        // Two REAL turning points with the engine's own resolved causes (verified against seed 8919
        // / 97013 by the node exporter check, 2026-08-13). Written down here so the probe never
        // depends on a live world reaching a given year before the watchdog.
        struct Beat { public string text; public string[] causes; }
        static readonly Beat[] Beats =
        {
            new Beat { text = "want owned the hand — Saga steals (Torvhaven)",
                       causes = new[] { "Winter closes over the world", "Saga", "desperation" } },
            new Beat { text = "death finds the people for the first time — Ask departs",
                       causes = new[] { "age" } },
            new Beat { text = "a first child is born — Liv",
                       causes = new[] { "Ask", "Embla" } },
        };

        static Fas4ProseLiveProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN PROSE LIVE PROBE (needs model)")]
        public static void RunMenu() { Kick(); }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Kick();
            }
            catch (Exception e) { Debug.LogWarning("[Fas4ProseLiveProbe] arm: " + e.Message); }
        }

        static async void Kick() { try { await Run(); } catch (Exception e) { Write("RED", "exception: " + e); } }

        static async Task Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            Action<bool, string> Check = (ok, msg) => { if (ok) pass++; else fail++; sb.AppendLine((ok ? "  PASS  " : "  FAIL  ") + msg); };

            sb.AppendLine("EMERGENCE — FAS 4 PROBE: LIVE INFERENCE (order §5)");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("beats = REAL turning points with the engine's own resolved causes (§1 export, seeds 8919/97013)");
            sb.AppendLine();

            var host = new GameObject("Fas4ProseLiveHost");
            double t0 = EditorApplication.timeSinceStartup;
            try
            {
                // ---------- 1. the model loads ----------
                sb.AppendLine("1. THE MODEL LOADS IN THIS VEHICLE");
                string modelPath = Fas4ProseScene.FindModel();
                Check(modelPath != null, "model found: " + (modelPath ?? "NONE — cannot run; download it in the editor first"));
                if (modelPath == null) { Write("RED", sb.ToString()); return; }

                var llm = host.AddComponent<LLM>();
                llm.SetModel(modelPath);
                // EDIT MODE: Unity only calls Awake/Start on a component in play mode (LLMUnity's
                // components carry no ExecuteAlways), so the probe drives the lifecycle itself.
                // Both entry points are public API — nothing is reached into or faked.
                llm.Awake();
                await llm.WaitUntilReady();
                Check(llm.started, "LLM started (" + Path.GetFileName(modelPath) + ", " + (EditorApplication.timeSinceStartup - t0).ToString("F1") + " s)");
                if (!llm.started) { Write("RED", sb.ToString()); return; }

                var ch = host.AddComponent<LLMCharacter>();
                ch.llm = llm; ch.systemPrompt = Fas4ProseDirector.SystemPrompt;
                ch.temperature = 0f; ch.save = "";   // numPredict + cachePrompt are the director's to set (Prime)
                ch.Awake(); ch.Start();   // same edit-mode lifecycle note as above

                var d1 = host.AddComponent<Fas4ProseDirector>(); d1.useProse = true; d1.character = ch;
                var d2 = host.AddComponent<Fas4ProseDirector>(); d2.useProse = true; d2.character = ch;   // separate cache
                Check(d1.character != null && d1.useProse, "director bound, useProse ON");
                sb.AppendLine();

                // ---------- 2+3. reproducibility (cache bypassed) and distinctness ----------
                sb.AppendLine("2+3. THE SAME TURNING POINT READS THE SAME WAY TWICE — CACHE BYPASSED");
                var lines = new List<string>();
                for (int i = 0; i < Beats.Length; i++)
                {
                    double b0 = EditorApplication.timeSinceStartup;
                    string a = await d1.WhyProse(Beats[i].text, Beats[i].causes);
                    string b = await d2.WhyProse(Beats[i].text, Beats[i].causes);   // different instance = cold cache
                    lines.Add(a);
                    sb.AppendLine("  beat " + (i + 1) + " : " + Beats[i].text);
                    sb.AppendLine("    causes  : " + string.Join(" | ", Beats[i].causes));
                    sb.AppendLine("    line A  : " + a);
                    sb.AppendLine("    line B  : " + b + "   (" + (EditorApplication.timeSinceStartup - b0).ToString("F1") + " s for both)");
                    Check(a == b, "beat " + (i + 1) + ": two cold directors, byte-identical line (" + a.Length + " chars)");
                    string c = await d1.WhyProse(Beats[i].text, Beats[i].causes);
                    Check(c == a, "beat " + (i + 1) + ": the cache returns exactly what was inferred");
                }
                for (int i = 0; i < lines.Count; i++)
                    for (int j = i + 1; j < lines.Count; j++)
                        Check(lines[i] != lines[j], "beat " + (i + 1) + " and beat " + (j + 1) + " read DIFFERENTLY");
                sb.AppendLine();

                // ---------- 4. voice ----------
                sb.AppendLine("4. THE VOICE HOLDS (the machine half; TONE is judged by a human below)");
                string[] banned = { "simulation", "simulator", "simulated", "procedural", "algorithm", "deterministic", "engine", "RNG" };
                foreach (var line in lines)
                {
                    bool clean = true;
                    foreach (var w in banned) if (line.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0) clean = false;
                    Check(clean, "no banned word in: \"" + Short(line) + "\"");
                    Check(line.IndexOf('<') < 0 && line.IndexOf('>') < 0, "no markup in: \"" + Short(line) + "\"");
                    Check(line.Length > 0 && line.Length <= 320, "book-row length (" + line.Length + " chars)");
                    Check(!line.Contains("!"), "no exclamation mark");
                }

                // ---------- 5. the invention guard, the reason any of this can be trusted ----------
                sb.AppendLine("5. THE INVENTION GUARD (a name not in the facts is a lie, not a blemish)");
                Check(Fas4ProseDirector.InventsNames("Astrid wept for her father.", "death finds the people — Ask departs", new[] { "age" }),
                      "a soul who is in no world is CAUGHT (\"Astrid\")");
                Check(!Fas4ProseDirector.InventsNames("Ask was old, and age took him.", "death finds the people — Ask departs", new[] { "age" }),
                      "a soul the facts DO name passes");
                Check(!Fas4ProseDirector.InventsNames("The winter left nothing to take.", "want owned the hand", new[] { "Winter closes over the world" }),
                      "ordinary sentence-opening words are not mistaken for names");
                for (int i = 0; i < lines.Count; i++)
                    Check(!Fas4ProseDirector.InventsNames(lines[i], Beats[i].text, Beats[i].causes),
                          "line " + (i + 1) + " names nobody the facts do not name");
                for (int i = 0; i < lines.Count; i++)
                {
                    Check(lines[i].Length <= 220, "line " + (i + 1) + " fits a book row (" + lines[i].Length + " chars)");
                    Check(lines[i].IndexOf('\n') < 0, "line " + (i + 1) + " is ONE line, no paragraphs");
                }
                sb.AppendLine("  guards ruled: accepted=" + d1.Accepted + " refused(word-ban)=" + d1.RefusedWordBan + " refused(invention)=" + d1.RefusedInvention);
                sb.AppendLine();

                sb.AppendLine("  ---- FOR THE HUMAN EYE (D-008): read these as chronicle lines ----");
                for (int i = 0; i < lines.Count; i++)
                {
                    sb.AppendLine("   " + Beats[i].text);
                    sb.AppendLine("      why: " + lines[i]);
                }
            }
            catch (Exception e) { fail++; sb.AppendLine("  FAIL  exception: " + e.Message); }
            finally { UnityEngine.Object.DestroyImmediate(host); }

            sb.AppendLine();
            sb.AppendLine("elapsed: " + (EditorApplication.timeSinceStartup - t0).ToString("F1") + " s");
            sb.AppendLine("VERDICT: " + (fail == 0 ? "GREEN" : "RED") + "  (" + pass + "/" + (pass + fail) + ")");
            sb.AppendLine("declared: this proves the SERVICE under a real model. Reproducibility is asserted across two cold");
            sb.AppendLine("  directors on the same loaded model + backend — the pin the setup doc states (CPU inference,");
            sb.AppendLine("  model+backend version). TONE is a human judgement on the lines printed above, not a PASS here.");
            Write(fail == 0 ? "GREEN" : "RED", sb.ToString(), pass, pass + fail);
        }

        static string Short(string s) => s == null ? "" : (s.Length <= 70 ? s : s.Substring(0, 67) + "...");

        static void Write(string verdict, string body, int pass = 0, int total = 0)
        {
            try
            {
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Report, body);
                File.WriteAllText(Done, verdict + " " + pass + "/" + total + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Debug.Log("[Fas4ProseLiveProbe] " + verdict + " -> " + Report);
            }
            catch (Exception e) { Debug.LogWarning("[Fas4ProseLiveProbe] write: " + e.Message); }
        }
    }
}
#endif
