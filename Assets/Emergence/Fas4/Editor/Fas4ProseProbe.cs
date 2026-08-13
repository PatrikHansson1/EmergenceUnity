// EMERGENCE — FAS 4 PROBE: the PROSE DIRECTOR's SAFE PROPERTIES (FAS4-PROSE-DIRECTOR-ORDER §4).
//
// This probe deliberately runs with NO MODEL LOADED and useProse OFF — the state every build ships
// in by default. It proves the properties that must hold whether or not a model is ever present,
// because those are the ones a player's build depends on:
//
//   1. DETERMINISM of the default path: RuleBasedWhy is a pure function of (text, causes) — two
//      calls give a byte-identical line, and a different cause set gives a different line.
//   2. KEY + SEED stability: KeyFor/SeedFor are stable across calls and DISTINGUISH different
//      entries (a shared seed would collapse two events onto one narration).
//   3. FLAG HONESTY: with useProse=false the service returns the rule-based line SYNCHRONOUSLY
//      (task already completed on return) — no await, no latency, no model dependency.
//   4. NO-MODEL HONESTY: useProse=true but character=null takes the same instant fallback.
//   5. WORD-BAN GUARD: a planted banned word is REFUSED — Sanitize returns empty so the caller
//      falls back rather than shipping a forbidden word into the reader's book.
//   6. THE WIRING ITSELF: a feed fed a snapshot whose events[] carry causes MATCHES them onto the
//      witnessed beat (CauseMatches > 0), and the view's why-expander answers a real line from
//      them — the §1/§2 chain end to end, without a model.
//   7. THE CHAIN IS HONEST WHEN EMPTY: an entry with no engine event behind it (a codex milestone)
//      gets null causes and the "records no cause" line, never an invented one.
//   8. NO SIM MUTATION: the applied WorldState is byte-identical before and after the whole run
//      (the prose layer reads; it never writes — D-078 r4).
//
// Live inference (same turning point twice -> identical line, tone judged by eye) needs the model
// loaded in Unity and is §5 of the order — a separate, screen-driven step.
// Menu: Emergence/Fas4/RUN PROSE PROBE.  Headless: drop Reports/RUN_FAS4PROSE.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;
using Emergence.Fas4;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas4ProseProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4PROSE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS4PROSE_DONE.txt");
        const string Report   = "Reports/fas4-prose.txt";

        // A real export bearing E1.5 acts — the same fixture school the Fas 6/7 probes use.
        const string Fixture = "Assets/Emergence/WorldStates/seq-8919-y055.json";

        static Fas4ProseProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/RUN PROSE PROBE")]
        public static void RunMenu() => Run();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run();
            }
            catch (Exception e) { Debug.LogWarning("[Fas4ProseProbe] arm: " + e.Message); }
        }

        static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            Action<bool, string> Check = (ok, msg) => { if (ok) pass++; else fail++; sb.AppendLine((ok ? "  PASS  " : "  FAIL  ") + msg); };

            sb.AppendLine("EMERGENCE — FAS 4 PROBE: prose director safe properties (no model, useProse OFF)");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("scope: the properties every shipped build depends on. Live inference = order §5 (screen-driven).");
            sb.AppendLine();

            try
            {
                // ---------- 1. rule-based determinism ----------
                sb.AppendLine("1. RULE-BASED DETERMINISM (the default path is a pure function)");
                var causesA = new[] { "Ask the Firebringer", "hunger" };
                string r1 = Fas4ProseDirector.RuleBasedWhy("Embla departs", causesA);
                string r2 = Fas4ProseDirector.RuleBasedWhy("Embla departs", causesA);
                string r3 = Fas4ProseDirector.RuleBasedWhy("Embla departs", new[] { "the long winter" });
                Check(r1 == r2, "two calls, identical line: \"" + r1 + "\"");
                Check(r1 != r3, "a different cause set gives a different line: \"" + r3 + "\"");
                Check(Fas4ProseDirector.RuleBasedWhy("x", null).Length > 0, "no causes -> an honest line, never empty: \"" + Fas4ProseDirector.RuleBasedWhy("x", null) + "\"");
                sb.AppendLine();

                // ---------- 2. key + seed stability and distinctness ----------
                sb.AppendLine("2. KEY + SEED (stable across calls, distinct across entries)");
                string kA = Fas4ProseDirector.KeyFor("Embla departs", causesA);
                string kB = Fas4ProseDirector.KeyFor("Embla departs", new[] { "the long winter" });
                Check(kA == Fas4ProseDirector.KeyFor("Embla departs", causesA), "KeyFor stable");
                Check(kA != kB, "KeyFor distinguishes cause sets");
                int sA = Fas4ProseDirector.SeedFor(kA), sB = Fas4ProseDirector.SeedFor(kB);
                Check(sA == Fas4ProseDirector.SeedFor(kA), "SeedFor stable (" + sA + ")");
                Check(sA != sB, "SeedFor distinguishes entries (" + sA + " vs " + sB + ")");
                Check(sA >= 0 && sB >= 0, "SeedFor non-negative");
                sb.AppendLine();

                // ---------- 3+4. flag + no-model honesty ----------
                sb.AppendLine("3+4. FLAG AND NO-MODEL HONESTY (instant, synchronous, model-free)");
                var host = new GameObject("Fas4ProseProbeHost");
                try
                {
                    var d = host.AddComponent<Fas4ProseDirector>();
                    d.useProse = false; d.character = null;
                    // no Fas3WorldRuntime in this scene -> no world -> the STRICTEST voice (remembered)
                    string rStrict = Fas4ProseDirector.RuleBasedWhy("Embla departs", causesA, Fas4ProseDirector.TierRemembered);
                    var t = d.WhyProse("Embla departs", causesA);
                    Check(t.IsCompleted, "useProse=false -> task already completed on return (no await)");
                    Check(t.Result == rStrict, "useProse=false -> the rule-based line, at the voice this world can hold: \"" + t.Result + "\"");
                    d.useProse = true;   // flag on, but no model present
                    var t2 = d.WhyProse("Embla departs", causesA);
                    Check(t2.IsCompleted && t2.Result == rStrict, "useProse=true, character=null -> same instant fallback");
                    Check(d.WhyProse("Embla departs", causesA, Fas4ProseDirector.TierWeighed).Result != rStrict,
                          "a caller passing a FROZEN tier gets that tier, not the world's current one");
                    sb.AppendLine();

                    // ---------- 5. word-ban guard ----------
                    sb.AppendLine("5. WORD-BAN GUARD (a slipped technical word is refused, not shipped)");
                    var mi = typeof(Fas4ProseDirector).GetMethod("Sanitize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    Check(mi != null, "Sanitize reachable for test");
                    if (mi != null)
                    {
                        string clean = (string)mi.Invoke(null, new object[] { "  The fire went out and no one had learned to make it.  " });
                        string dirty = (string)mi.Invoke(null, new object[] { "The simulation decided the fire went out." });
                        string dirty2 = (string)mi.Invoke(null, new object[] { "The ENGINE rolled it that way." });
                        Check(clean == "The fire went out and no one had learned to make it.", "a clean line passes, trimmed");
                        Check(string.IsNullOrEmpty(dirty), "\"simulation\" -> refused (caller falls back)");
                        Check(string.IsNullOrEmpty(dirty2), "\"ENGINE\" -> refused case-insensitively");
                    }
                    sb.AppendLine();

                    // ---------- 5b. THE ANACHRONISM LAW, DERIVED FROM THE WORLD'S ONTOLOGY ----------
                    // The point Patrik pressed on (2026-08-14): we do not remove dark stories, we remove
                    // FALSE ones — and a hand-written blacklist cannot tell the difference. This asserts
                    // the replacement: the same word is refused in a world that cannot hold it and
                    // ACCEPTED in a world that has invented it. Nothing has to be unbanned by hand.
                    sb.AppendLine("5b. THE ANACHRONISM LAW IS DERIVED, NOT DECREED");
                    var stoneAge = new HashSet<string>(new[] { "fire", "sharp", "rope", "hut" });
                    var lateWorld = new HashSet<string>(new[] { "fire", "writing", "coinage", "law", "medicine", "smithing" });

                    Check(Fas4ProseDirector.AnachronismIn("The father agreed to provide financial support.", stoneAge) != null,
                          "\"financial\" REFUSED in a world of fire and rope");
                    Check(Fas4ProseDirector.AnachronismIn("She paid a coin for the grain.", stoneAge) != null,
                          "\"coin\" REFUSED before coinage exists");
                    Check(Fas4ProseDirector.AnachronismIn("She paid a coin for the grain.", lateWorld) == null,
                          "\"coin\" ACCEPTED once the world has invented coinage — the world EARNED the word");
                    Check(Fas4ProseDirector.AnachronismIn("The court would not hear him.", stoneAge) != null,
                          "\"court\" REFUSED before law exists");
                    Check(Fas4ProseDirector.AnachronismIn("The court would not hear him.", lateWorld) == null,
                          "\"court\" ACCEPTED once law is known");
                    Check(Fas4ProseDirector.AnachronismIn("No healer could help; the wound went bad.", stoneAge) != null,
                          "\"healer\" REFUSED before medicine exists");
                    Check(Fas4ProseDirector.AnachronismIn("No healer could help; the wound went bad.", lateWorld) == null,
                          "\"healer\" ACCEPTED once medicine is known");
                    Check(Fas4ProseDirector.AnachronismIn("He wrote it down so it would outlast him.", lateWorld) == null,
                          "\"wrote\" ACCEPTED once writing is known");
                    Check(Fas4ProseDirector.AnachronismIn("He wrote it down so it would outlast him.", stoneAge) != null,
                          "\"wrote\" REFUSED before writing is known");
                    Check(Fas4ProseDirector.AnachronismIn("The insurance would not cover it.", lateWorld) != null,
                          "\"insurance\" refused even in the LATEST world — the engine models no such thing anywhere");
                    Check(Fas4ProseDirector.AnachronismIn("Hunger owned the hand, and winter took the rest.", stoneAge) == null,
                          "an honest, DARK line about hunger and winter passes untouched — dark is not the enemy, false is");
                    Check(Fas4ProseDirector.AnachronismIn("He came for an old wrong and answered it with blood.", stoneAge) == null,
                          "blood-feud language passes untouched in the stone age");

                    // the ontology is READ from the world, never assumed
                    var Sk = new WorldState { villages = new[] {
                        new WorldVillage { name = "Torvhaven", knows = new[] { "fire", "writing" } },
                        new WorldVillage { name = "Bjornheim", knows = new[] { "fire", "coinage" } } } };
                    var known = Fas4ProseDirector.KnownTechs(Sk);
                    Check(known.Contains("writing") && known.Contains("coinage"), "KnownTechs unions the villages' own knowledge (" + known.Count + " techs)");
                    Check(!known.Contains("law"), "and claims nothing the world has not learned");
                    Check(Fas4ProseDirector.KnownTechs(null).Count == 0, "a null world knows nothing — the strictest reading, never a crash");
                    sb.AppendLine();

                    // ---------- 5c. THE VOICE LADDER (reviewed 2026-08-14; v1 lost five arguments) --
                    sb.AppendLine("5c. THE VOICE MATURES WITH THE WORLD — and never rewrites its own past");
                    var oral    = new HashSet<string> { "fire", "rope" };
                    var told    = new HashSet<string> { "fire", "storytelling" };
                    var written = new HashSet<string> { "fire", "storytelling", "writing" };
                    var weighed = new HashSet<string> { "fire", "storytelling", "writing", "philosophy" };
                    var counts  = new HashSet<string> { "fire", "numbers" };   // numbers has pre:[] — it CAN come first

                    Check(Fas4ProseDirector.VoiceTier(oral) == Fas4ProseDirector.TierRemembered, "a world of fire and rope REMEMBERS");
                    Check(Fas4ProseDirector.VoiceTier(told) == Fas4ProseDirector.TierTold, "storytelling -> it is TOLD");
                    Check(Fas4ProseDirector.VoiceTier(written) == Fas4ProseDirector.TierWritten, "writing -> it is WRITTEN");
                    Check(Fas4ProseDirector.VoiceTier(weighed) == Fas4ProseDirector.TierWeighed, "philosophy -> it is WEIGHED");
                    Check(Fas4ProseDirector.VoiceTier(counts) == Fas4ProseDirector.TierRemembered,
                          "numbers WITHOUT writing does NOT lift the voice — the ladder is monotonic even though `numbers` has pre:[]");
                    Check(Fas4ProseDirector.VoiceTier(null) == Fas4ProseDirector.TierRemembered, "an unknown world gets the STRICTEST voice");

                    var cs3 = new[] { "winter", "Saga", "desperation" };
                    string l0 = Fas4ProseDirector.RuleBasedWhy("x", cs3, 0);
                    string l1 = Fas4ProseDirector.RuleBasedWhy("x", cs3, 1);
                    string l2 = Fas4ProseDirector.RuleBasedWhy("x", cs3, 2);
                    string l3 = Fas4ProseDirector.RuleBasedWhy("x", cs3, 3);
                    sb.AppendLine("  remembered : " + l0);
                    sb.AppendLine("  told       : " + l1);
                    sb.AppendLine("  written    : " + l2);
                    sb.AppendLine("  weighed    : " + l3);
                    Check(l0 != l1 && l1 != l2 && l2 != l3 && l0 != l3, "four tiers, four different voices");
                    Check(l0 == Fas4ProseDirector.RuleBasedWhy("x", cs3, 0), "each tier is deterministic");
                    Check(l0.Contains("winter") && !l0.Contains("Saga"),
                          "REMEMBERED keeps only the nearest cause (the class-ordered first), not the chain");
                    Check(l0.Contains("not kept"),
                          "...and SAYS that there were others — 'allt hände' survives 'we remember only one', not a page pretending there was one");
                    Check(l1.Contains("They say"), "TOLD hedges — a telling is hearsay");
                    Check(!l2.Contains("They say"), "WRITTEN stops hedging — this is what was set down");
                    Check(l3.Contains("Little else"), "WEIGHED judges what else could have followed");
                    Check(Fas4ProseDirector.RuleBasedWhy("x", null, 0) != Fas4ProseDirector.RuleBasedWhy("x", null, 3),
                          "even silence is said differently at each tier");

                    // scoping: the WITNESSING village's knowledge, not a union over the world
                    var Sv = new WorldState {
                        worldKnows = new[] { "fire", "storytelling", "writing", "philosophy" },
                        villages = new[] {
                            new WorldVillage { name = "Torvhaven", knows = new[] { "fire", "rope" } },
                            new WorldVillage { name = "Bjornheim", knows = new[] { "fire", "storytelling", "writing", "philosophy" } } } };
                    Check(Fas4ProseDirector.VoiceTier(Fas4ProseDirector.KnownTechs(Sv, 0)) == Fas4ProseDirector.TierRemembered,
                          "the village that cannot write REMEMBERS — even though its neighbour philosophises (D-086: villages diverge)");
                    Check(Fas4ProseDirector.VoiceTier(Fas4ProseDirector.KnownTechs(Sv, 1)) == Fas4ProseDirector.TierWeighed,
                          "and the village that can, WEIGHS");
                    var Spre = new WorldState { worldKnows = new[] { "fire", "storytelling", "writing" }, villages = new WorldVillage[0] };
                    Check(Fas4ProseDirector.VoiceTier(Fas4ProseDirector.KnownTechs(Spre, -1)) == Fas4ProseDirector.TierWritten,
                          "BEFORE THE FIRST VILLAGE the world's own knowledge speaks — the opening is not silenced by an empty array");
                    sb.AppendLine();

                    // ---------- 6+7+8. the wiring, end to end, on a real fixture ----------
                    sb.AppendLine("6+7+8. THE WIRING (feed matches engine causes; the view answers; the sim is untouched)");
                    string json = File.Exists(Fixture) ? File.ReadAllText(Fixture) : null;
                    Check(json != null, "fixture present: " + Fixture);
                    if (json != null)
                    {
                        var S = JsonUtility.FromJson<WorldState>(json);
                        Check(S != null && S.agents != null && S.agents.Length > 0, "fixture parses: " + (S != null && S.agents != null ? S.agents.Length : 0) + " souls");

                        // The fixture predates the events[] export (§1 is new), so the wiring is proven
                        // on a DECLARED in-memory event set built to the exporter's exact schema —
                        // the same mechanism-fixture school as Fas6/Fas7 (D-158, D-179). Honest label:
                        // this proves the MATCH LAW, not that a given seed happens to bear these acts.
                        int actor = S.agents[0].id;
                        S.events = new[]
                        {
                            new WorldEvent { id = 41, year = 55, type = "steal", agent = actor, village = "",
                                             causes = new[] { "Hunger owned the hand", "the long winter" } },
                            new WorldEvent { id = 42, year = 55, type = "death", agent = actor + 100000, village = "",
                                             causes = new[] { "starvation" } },
                        };

                        var feedGo = new GameObject("Fas4ProseProbeFeed");
                        var worldGo = new GameObject("Fas4ProseProbeWorld");
                        try
                        {
                            string before = JsonUtility.ToJson(S);

                            var world = worldGo.AddComponent<Fas3WorldRuntime>();
                            typeof(Fas3WorldRuntime).GetProperty("LastState").GetSetMethod(true).Invoke(world, new object[] { S });
                            var feed = feedGo.AddComponent<Fas4ChronicleFeed>();
                            feed.showUI = false;
                            typeof(Fas4ChronicleFeed).GetField("_world", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(feed, world);

                            // the beat the body witnesses: this soul steals (sayAct), same actor as event 41
                            var mOnBus = typeof(Fas4ChronicleFeed).GetMethod("OnBus", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            mOnBus.Invoke(feed, new object[] { new PresentationEvent(660, 55, "The Ember Years", PresentationEventType.AgentActivity, "agent-" + actor, -1, "sayAct: steal") });
                            // and a codex milestone, which has NO engine event behind it
                            mOnBus.Invoke(feed, new object[] { new PresentationEvent(660, 55, "The Ember Years", PresentationEventType.Milestone, "codex:first-forge", -1, "the first forge is lit") });

                            Check(feed.Entries.Count == 2, "two beats witnessed (" + feed.Entries.Count + ")");
                            var stealEntry = default(Fas4ChronicleFeed.Entry);
                            var mileEntry = default(Fas4ChronicleFeed.Entry);
                            foreach (var en in feed.Entries) { if (en.kind == "steal") stealEntry = en; else if (en.kind == "milestone") mileEntry = en; }

                            Check(stealEntry.causes != null && stealEntry.causes.Length == 2, "the steal beat CARRIES the engine's causes (" + (stealEntry.causes == null ? "null" : string.Join(" | ", stealEntry.causes)) + ")");
                            Check(stealEntry.eventId == 41, "matched the right engine event (id=" + stealEntry.eventId + ", expected 41)");
                            Check(feed.CauseMatches == 1 && feed.CauseMisses == 1, "match bookkeeping honest: matches=" + feed.CauseMatches + " misses=" + feed.CauseMisses);
                            Check(mileEntry.causes == null && mileEntry.eventId == -1, "the milestone beat has NO engine causes and says so (never invented)");

                            // the FROZEN tier is what the line is written in — never today's tier
                            Check(stealEntry.voiceTier >= 0 && mileEntry.voiceTier >= 0,
                                  "every entry froze its voice tier at witness time (steal=" + Fas4ProseDirector.TierName(stealEntry.voiceTier) + ")");
                            string whySteal = Fas4ProseDirector.RuleBasedWhy(stealEntry.text, stealEntry.causes, stealEntry.voiceTier);
                            string whyMile  = Fas4ProseDirector.RuleBasedWhy(mileEntry.text, mileEntry.causes, mileEntry.voiceTier);
                            Check(whySteal.Contains("the long winter") || whySteal.Contains("Hunger owned the hand"),
                                  "the why-line SPEAKS the engine's cause: \"" + whySteal + "\"");
                            Check(whyMile.IndexOf("reason", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || whyMile.IndexOf("cause", System.StringComparison.OrdinalIgnoreCase) >= 0
                               || whyMile.IndexOf("not why", System.StringComparison.OrdinalIgnoreCase) >= 0,
                                  "the causeless beat answers honestly: \"" + whyMile + "\"");
                            sb.AppendLine("  why (steal)     : " + whySteal);
                            sb.AppendLine("  why (milestone) : " + whyMile);

                            string after = JsonUtility.ToJson(S);
                            Check(before == after, "NO SIM MUTATION: applied state byte-identical before/after (" + before.Length + " chars)");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(feedGo); UnityEngine.Object.DestroyImmediate(worldGo); }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(host); }
            }
            catch (Exception e) { fail++; sb.AppendLine("  FAIL  exception: " + e); }

            sb.AppendLine();
            sb.AppendLine("VERDICT: " + (fail == 0 ? "GREEN" : "RED") + "  (" + pass + "/" + (pass + fail) + ")");
            sb.AppendLine("declared limits: no model is loaded here by design — live inference (identical line twice + tone) is order §5.");
            sb.AppendLine("declared fixture: the events[] set is an in-memory DECLARED fixture in the exporter's exact schema (the standing");
            sb.AppendLine("  seq-8919-y055.json predates the §1 export). It proves the MATCH LAW; a live world proves the volume.");

            Directory.CreateDirectory("Reports");
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, (fail == 0 ? "GREEN" : "RED") + " " + pass + "/" + (pass + fail) + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            Debug.Log("[Fas4ProseProbe] " + (fail == 0 ? "GREEN" : "RED") + " " + pass + "/" + (pass + fail) + " -> " + Report);
        }
    }
}
#endif
