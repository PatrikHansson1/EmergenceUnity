// EMERGENCE — Fas4 chronicle PROSE director (A5: LLM-backed "why" narration).
//
// Presentation-only (D-078 rule 4): given one event's facts + its causes[], it asks a local LLM
// to narrate WHY it happened, in the chronicle's voice. It NEVER writes to the sim and never feeds
// the golden master — the truth is the engine's event; this only phrases it. It is FLAVOR.
//
// Safe by construction:
//  - useProse defaults OFF -> the rule-based "why" is the default and the build needs no model.
//  - no LLMCharacter assigned -> rule-based fallback (never throws, always returns something).
//  - deterministic: fixed seed (hash of the entry's facts) + temperature 0 (greedy) + a per-entry
//    cache -> the same world reads the same line every time, and re-expanding never re-infers.
//  - word-ban guard: if the model slips a forbidden technical word, we drop to the fallback rather
//    than ship it (emergence-game-copywriter law 3).
//
// Voice: distilled from the emergence-game-copywriter skill (Norse, quiet, weighty, show-don't-tell,
// the word-ban, and above all INVENT NOTHING) but tuned for diegetic narration, not sales.
//
// This is the SERVICE. Wiring it into the chronicle feed/view (feed Entry carrying causes[]/id,
// Fas4ChronicleView.ExpandBookRow calling WhyProse) is a separate, deliberate step — see
// 20-DESIGN/FAS4-PROSE-DIRECTOR-ORDER-2026-08-11.md.
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using LLMUnity;
using Emergence.Runtime;   // WorldState / Fas3WorldRuntime — the ontology the narrator is bounded by

namespace Emergence.Fas4
{
    public sealed class Fas4ProseDirector : MonoBehaviour
    {
        [Tooltip("OFF by default: the rule-based 'why' stays and no model is needed. ON routes a turning-point 'why' through the LLM.")]
        public bool useProse = false;

        [Tooltip("An LLMUnity LLMCharacter with a model loaded. If null, the rule-based fallback is used and nothing breaks.")]
        public LLMCharacter character;

        [Tooltip("Max tokens for a 'why' line — kept short; the chronicle voice is terse. One sentence fits well inside 44.")]
        public int maxTokens = 44;

        // The chronicle-narrator voice (emergence-game-copywriter, diegetic variant).
        //
        // HARDENED 2026-08-13 after the first live run (RUN_FAS4PROSELIVE): a 1B model given a soft
        // "invent nothing" invented freely — a soul named Astrid who is in no world, "Ask, a Norse
        // goddess of wisdom", a cause of death drawn from biology it was never told. A chronicle
        // whose promise is "every line below happened" cannot ship that. So the prompt is now a
        // hard contract (one sentence, a word ceiling, no name that is not in the facts) AND the
        // service enforces it mechanically afterwards — see InventsNames. The prompt asks; the
        // guard decides. A model that will not obey simply falls back to the rule-based line.
        public const string SystemPrompt =
            "You retell one line of a chronicle. You are given an EVENT and its CAUSES. " +
            "Write ONE sentence, at most 25 words, saying why the event happened. " +
            "Rules, absolute: use ONLY the given facts. Do NOT name any person, place or thing that " +
            "is not written in the EVENT or CAUSES. Do NOT add ages, family, weather, feelings, " +
            "religion or reasons you were not given. If the causes barely explain it, say only what " +
            "they show. Plain human words — never 'simulation', 'algorithm', 'system', 'engine', " +
            "'data'. Quiet and weighty. No exclamation marks. No preamble — the sentence alone.";

        // Per-entry cache: identical facts -> identical line, no re-inference (determinism + cost).
        readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        bool _primed;

        void Prime()
        {
            if (_primed || character == null) return;
            character.systemPrompt = SystemPrompt;
            character.temperature = 0f;      // greedy -> reproducible for a given model + backend
            character.numPredict = maxTokens;
            // The first live run proved temperature 0 alone is NOT enough: two identical asks came
            // back different because the agent REUSED its KV cache from the previous entry, so an
            // entry's narration depended on which entry the reader happened to open before it.
            // A chronicle must not read differently depending on the order it is read in.
            character.cachePrompt = false;
            character.save = "";             // and nothing carries over between sessions either
            _primed = true;
        }

        /// <summary>Deterministic key for an entry — its facts define it, so identical facts cache-hit.</summary>
        public static string KeyFor(string eventText, IReadOnlyList<string> causes)
        {
            var sb = new StringBuilder(eventText ?? "");
            if (causes != null) foreach (var c in causes) { sb.Append('|'); sb.Append(c); }
            return sb.ToString();
        }

        /// <summary>Stable non-negative hash of the entry key -> the fixed LLM seed for this entry.</summary>
        public static int SeedFor(string key)
        {
            unchecked { int h = 17; foreach (char c in key ?? "") h = h * 31 + c; return h & 0x7fffffff; }
        }

        /// <summary>
        /// The chronicle's "why" for one event. Presentation-only. Returns the rule-based fallback
        /// immediately when prose is off or no model is present; otherwise the cached/LLM line.
        /// </summary>
        public Task<string> WhyProse(string eventText, string[] causes)
            => WhyProse(eventText, causes, VoiceTier(KnownTechs(StateNow())));

        /// <summary>The chronicle's "why" at a GIVEN voice tier. The tier is passed in, not computed
        /// here, because it must be the one that held WHEN THE ENTRY WAS WITNESSED — not the one that
        /// holds now.
        ///
        /// The hostile review's sharpest objection (2026-08-14, inv. 5): computing it live means a
        /// year-3 entry silently acquires the philosopher's voice the moment the world invents
        /// philosophy at year 90 — the book rewrites its own past every time the people learn
        /// something, and the studio has already been burned once by a chronicle line that could not
        /// be reproduced from its world code (the ⛔ I8 press-kit finding). The chronicle is condition
        /// B and it gets QUOTED; a shared seed + year must reproduce the line. And knowledge can be
        /// LOST, so a live tier would make the book grow dumber mid-page — with the entry announcing
        /// the death of the last writer being the one entry whose chain gets hidden.
        ///
        /// So: the feed freezes the tier into the Entry at witness time and it never changes again.
        /// A book does not forget that it could once write.</summary>
        public async Task<string> WhyProse(string eventText, string[] causes, int tier)
        {
            string key = tier + "|" + KeyFor(eventText, causes);
            if (_cache.TryGetValue(key, out var hit)) return hit;

            if (!useProse || character == null)
                return RuleBasedWhy(eventText, causes, tier);   // fallback: no LLM, no await, always available

            Prime();
            character.systemPrompt = SystemPromptFor(tier);   // the voice this world could have written in
            character.seed = SeedFor(key);                // fixed per entry -> reproducible
            string reply;
            try { reply = await character.Chat(BuildPrompt(eventText, causes), null, null, false); }
            catch (Exception e)
            {
                Debug.LogWarning("[Fas4ProseDirector] LLM failed, using fallback: " + e.Message);
                return RuleBasedWhy(eventText, causes, tier);
            }

            // shape -> word-ban -> invention guard. Any refusal falls back to the rule-based line:
            // a plainer sentence that is TRUE beats a finer one that is not.
            reply = Shape(reply);
            reply = Sanitize(reply);
            if (string.IsNullOrEmpty(reply)) { RefusedWordBan++; reply = RuleBasedWhy(eventText, causes, tier); }
            else if (InventsNames(reply, eventText, causes)) { RefusedInvention++; reply = RuleBasedWhy(eventText, causes, tier); }
            else
            {
                string bad = AnachronismIn(reply, KnownTechs(StateNow()));
                if (bad != null) { RefusedAnachronism++; LastRefusedWord = bad; reply = RuleBasedWhy(eventText, causes, tier); }
                else Accepted++;
            }
            _cache[key] = reply;
            return reply;
        }

        static string BuildPrompt(string eventText, string[] causes)
        {
            var sb = new StringBuilder();
            sb.Append("Event: ").Append(eventText ?? "").Append('\n');
            if (causes != null && causes.Length > 0)
            {
                sb.Append("Causes:\n");
                foreach (var c in causes) sb.Append("- ").Append(c).Append('\n');
            }
            else sb.Append("Causes: none recorded.\n");
            sb.Append("Why did it happen? One or two sentences, from the facts only.");
            return sb.ToString();
        }

        /// <summary>Bookkeeping the probes assert on: how the guards actually ruled.</summary>
        public int Accepted { get; private set; }
        public int RefusedWordBan { get; private set; }
        public int RefusedInvention { get; private set; }
        public int RefusedAnachronism { get; private set; }
        /// <summary>The word that got the last line refused — so a report can say WHY, not just that.</summary>
        public string LastRefusedWord { get; private set; } = "";

        /// <summary>Cut a model's answer down to a chronicle line: the first paragraph, the first
        /// sentence, a hard ceiling. Small models pad and then run out of tokens mid-word; a book row
        /// is not the place to show that.</summary>
        public static string Shape(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Replace("\r", "\n").Trim();
            int para = s.IndexOf("\n\n", StringComparison.Ordinal);
            if (para > 0) s = s.Substring(0, para);
            s = s.Replace('\n', ' ');
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            s = s.Trim();
            // strip a lead-in the small models like ("Why it happened:", "Answer:")
            int colon = s.IndexOf(": ", StringComparison.Ordinal);
            if (colon > 0 && colon < 22 && s.IndexOf(' ') < colon) s = s.Substring(colon + 2).Trim();
            int stop = s.IndexOfAny(new[] { '.', '?' });
            if (stop >= 20) s = s.Substring(0, stop + 1);
            if (s.Length > 220) s = "";     // it never found an end — do not ship a fragment
            return s.Trim();
        }

        // Capitalised words that can honestly open or sit in an English sentence without naming
        // anyone. Everything else that is capitalised must be IN THE FACTS.
        static readonly HashSet<string> Ordinary = new HashSet<string>(StringComparer.Ordinal)
        {
            "The","They","He","She","It","His","Her","Their","There","This","That","These","Those",
            "A","An","And","But","So","Then","When","While","After","Before","Because","No","None",
            "Nothing","Not","One","Two","Three","Many","Most","Some","All","Both","Neither","Each",
            "What","Who","Where","Why","How","With","Without","Within","For","From","In","Of","On",
            "At","By","To","Up","Out","Over","Under","Once","Even","Only","Still","Yet","Its","As"
        };

        /// <summary>THE INVENTION GUARD (the reason this service can be trusted at all): a name in the
        /// line that is not in the facts is a name the model made up. The chronicle's whole promise is
        /// that every line happened — so a made-up name is not a blemish, it is a lie, and the line is
        /// refused. Conservative by design: a false refusal costs a plainer sentence, a false accept
        /// costs the promise.</summary>
        public static bool InventsNames(string line, string eventText, string[] causes)
        {
            if (string.IsNullOrEmpty(line)) return false;
            var facts = new StringBuilder(eventText ?? "");
            if (causes != null) foreach (var c in causes) { facts.Append(' '); facts.Append(c); }
            string haystack = facts.ToString();

            var word = new StringBuilder();
            for (int i = 0; i <= line.Length; i++)
            {
                char ch = i < line.Length ? line[i] : ' ';
                if (char.IsLetter(ch) || ch == '\'') { word.Append(ch); continue; }
                if (word.Length >= 3 && char.IsUpper(word[0]))
                {
                    string w = word.ToString().TrimEnd('\'');
                    int apo = w.IndexOf('\'');
                    if (apo > 0) w = w.Substring(0, apo);          // "Astrid's" -> "Astrid"
                    if (w.Length >= 3 && !Ordinary.Contains(w)
                        && haystack.IndexOf(w, StringComparison.OrdinalIgnoreCase) < 0)
                    { return true; }
                }
                word.Length = 0;
            }
            return false;
        }

        // ---- BAN 1: THE FOURTH WALL. Words that admit a machine is speaking. No world can ever
        // license these, because they are not about the world at all. (copywriter law 3.)
        static readonly string[] Banned = { "simulation", "simulator", "simulated", "procedural", "algorithm", "deterministic", "engine", "AI system", "RNG" };

        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            foreach (var b in Banned)
                if (s.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                    return "";   // let the fallback take over rather than ship a banned word
            return s;
        }

        // ---- BAN 2: THE ANACHRONISM LAW, DERIVED FROM THE WORLD'S OWN ONTOLOGY ----
        //
        // The first version of this was a hand-written blacklist, and Patrik was right to push on it
        // (2026-08-14): it was MY taste, not a law, and it was already wrong. The engine's tech tree
        // contains `coinage`, `medicine`, `law`, `school`, `university`, `temple`, `philosophy` — so a
        // world that has invented coinage has EARNED the word "coin", and blacklisting it forever was
        // censoring a true sentence. Emergence's promise is not that the chronicle is tame; it is that
        // every line HAPPENED. Dark is the product. False is the enemy.
        //
        // So the law is now the same one the Codex obeys (D-106 legibility): nothing may be placed in
        // the world before the world can hold it. A word is a placement too. Each gated word names the
        // TECH that licenses it; the moment the world invents that tech, the word opens by itself. No
        // one has to remember to unban anything.
        //
        // The ungated set below is deliberately short, and every entry earns its place the same way:
        // the engine models NO such thing at any point in its tech tree, so a line containing it can
        // only be invention. The day the engine gains the concept, the entry moves up into the table
        // above it — that is the maintenance instruction, written here so it is not lost.
        static readonly Dictionary<string, string> GatedWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // word              tech that licenses it
            { "coin", "coinage" }, { "coins", "coinage" }, { "money", "coinage" }, { "price", "coinage" },
            { "paid", "coinage" }, { "payment", "coinage" }, { "wage", "coinage" }, { "debt", "coinage" },
            { "financial", "coinage" }, { "finance", "coinage" }, { "currency", "coinage" }, { "purchase", "coinage" },
            { "doctor", "medicine" }, { "medicine", "medicine" }, { "medical", "medicine" }, { "healer", "medicine" },
            { "remedy", "medicine" }, { "abortion", "medicine" }, { "midwife", "medicine" },
            { "law", "law" }, { "lawyer", "law" }, { "court", "law" }, { "judge", "law" }, { "legal", "law" },
            { "trial", "law" }, { "contract", "law" },
            { "school", "school" }, { "teacher", "school" }, { "pupil", "school" },
            { "university", "university" }, { "scholar", "scholarship" },
            { "temple", "temple" }, { "priest", "temple" },
            { "wrote", "writing" }, { "written", "writing" }, { "letter", "writing" }, { "book", "writing" },
            { "ink", "writing" }, { "page", "writing" },
            { "printed", "printpress" }, { "press", "printpress" },
            { "clock", "clock" }, { "hour", "clock" }, { "minute", "clock" },
            { "calendar", "calendar" }, { "month", "calendar" },
            { "forge", "smithing" }, { "smith", "smithing" }, { "anvil", "smithing" },
            { "cart", "wheel" }, { "wagon", "wheel" }, { "wheel", "wheel" },
            { "mill", "mill" }, { "ship", "sailing" }, { "sail", "sailing" }, { "boat", "sailing" },
            { "glass", "glass" }, { "brick", "brick" }, { "farm", "farming" }, { "crop", "farming" },
            { "harvest", "farming" }, { "granary", "granary" }, { "well", "well" }, { "road", "road" },
            { "steam", "steam" },
        };

        /// <summary>Concepts the engine models NOWHERE in its tech tree — a line containing one can only
        /// be invention, whatever the world has learned. Move an entry into GatedWords the day the
        /// engine gains the concept; do not just delete it.</summary>
        static readonly string[] NeverModelled =
        {
            "bank", "insurance", "company", "corporation", "tax", "taxes",
            "police", "government", "parliament", "president",
            "hospital", "clinic", "nurse", "patient", "diagnosis", "therapy", "vaccine",
            "phone", "computer", "internet", "electric", "electricity", "factory",
            "car", "train", "aeroplane", "airplane", "percent", "statistics",
        };

        /// <summary>What this world has actually learned — the union of its villages' knowledge, read
        /// straight from the applied snapshot. Pure read (D-078 r4).</summary>
        public static HashSet<string> KnownTechs(WorldState S) => KnownTechs(S, -1);

        /// <summary>What the community that witnessed this knows. SCOPED, not unioned: the engine
        /// rewrote its knowledge model in 2.1 (D-086) precisely to stop villages converging into one
        /// all-knowing state, and a union over every village would have put that hole straight back —
        /// in the voice. A village that has LOST writing would have gone on reasoning like a literate
        /// one because a neighbour forty tiles away still could. villageId &lt; 0 (or a village with no
        /// knowledge of its own) falls back to the world's living knowledge, which is also the honest
        /// reading before the first village exists: the whole small world is then one community.</summary>
        public static HashSet<string> KnownTechs(WorldState S, int villageId)
        {
            var k = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (S == null) return k;
            if (villageId >= 0 && S.villages != null && villageId < S.villages.Length)
            {
                var v = S.villages[villageId];
                if (v?.knows != null && v.knows.Length > 0)
                {
                    foreach (var t in v.knows) if (!string.IsNullOrEmpty(t)) k.Add(t);
                    return k;
                }
            }
            if (S.worldKnows != null)
                foreach (var t in S.worldKnows) if (!string.IsNullOrEmpty(t)) k.Add(t);
            if (k.Count == 0 && S.villages != null)                      // pre-worldKnows exports
                foreach (var v in S.villages)
                    if (v?.knows != null) foreach (var t in v.knows) if (!string.IsNullOrEmpty(t)) k.Add(t);
            return k;
        }

        /// <summary>The offending word, or null if the line fits the world. A gated word is fine once
        /// the world has invented what licenses it — that is the whole point.</summary>
        public static string AnachronismIn(string line, HashSet<string> known)
        {
            if (string.IsNullOrEmpty(line)) return null;
            foreach (var w in Words(line))
            {
                foreach (var n in NeverModelled)
                    if (string.Equals(w, n, StringComparison.OrdinalIgnoreCase)) return w;
                string needs;
                if (GatedWords.TryGetValue(w, out needs) && (known == null || !known.Contains(needs)))
                    return w;
            }
            return null;
        }

        static IEnumerable<string> Words(string line)
        {
            var sb = new StringBuilder();
            for (int i = 0; i <= line.Length; i++)
            {
                char c = i < line.Length ? line[i] : ' ';
                if (char.IsLetter(c)) { sb.Append(c); continue; }
                if (sb.Length > 1) yield return sb.ToString();
                sb.Length = 0;
            }
        }

        Fas3WorldRuntime _world;
        /// <summary>The world as applied right now — the ontology the narrator is bounded by.</summary>
        WorldState StateNow()
        {
            if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>();
            return _world != null ? _world.LastState : null;
        }

        // ============ THE VOICE MATURES WITH THE WORLD — v2 after the colleagues' review ============
        //
        // The chronicle cannot be wiser than the people who could have written it. Same law the engine
        // already keeps (star-faith before arithmetic, the calendar gated behind numeracy), applied to
        // the telling itself.
        //
        // v1 was reviewed by a narrative designer and a hostile reviewer on 2026-08-14 and lost five
        // arguments. What changed, and why:
        //
        //  * `numbers` -> "Three things came before it" is GONE. Both reviewers killed it independently:
        //    counting to three is the dullest thing numeracy can do, the line said nothing tier 1 had
        //    not already said, and it read as verbosity rather than maturity. Worse, `numbers` has
        //    pre:[] in the engine — it can be invented BEFORE writing, so the ladder was not a ladder:
        //    a world could count without being able to write.
        //  * The rungs are now REGISTER, not sentence length: remembered -> told -> written -> weighed.
        //    What moves is how much the book DARES CLAIM ("they say" -> "it followed" -> "little else
        //    could have followed"), which is the axis a reader actually feels.
        //  * `storytelling` is the oral rung. It exists in the engine's own tree and IS the oral
        //    tradition; hanging chains on `writing` wasted the tree's most dramatic tech on grammar.
        //    Writing's real gift is FIXATION — the book stops hedging.
        //  * The ladder is monotonic BY CONSTRUCTION (each rung tested highest-first and every rung
        //    above 1 requires `writing`), so no tech order can produce an incoherent voice.
        //  * Tier 0 no longer deletes causes in silence. It keeps the one an oral memory would keep
        //    and SAYS that there were others. "Allt hände" survives "we remember only one"; it does
        //    not survive a page pretending there was only one.
        //
        // Declared risk (for the eye): a drifting voice can read as UNEVEN QUALITY rather than intent.
        // The early lines must feel young, not cheap. Mitigations proposed by the reviewers — a
        // one-time threshold entry in the book ("From here the book is written down. What came before
        // was remembered."), a voice legend in ANALYZE, and never re-rendering old pages in the new
        // voice — are ordered separately; the third is already law here (see Entry.voiceTier).
        public const int TierRemembered = 0, TierTold = 1, TierWritten = 2, TierWeighed = 3;

        public static int VoiceTier(HashSet<string> known)
        {
            if (known == null) return TierRemembered;      // a world we know nothing about gets the strictest voice
            bool writes = known.Contains("writing");
            if (writes && known.Contains("philosophy")) return TierWeighed;
            if (writes) return TierWritten;
            if (known.Contains("storytelling")) return TierTold;
            return TierRemembered;
        }

        public static string TierName(int tier)
            => tier >= TierWeighed ? "weighed" : tier == TierWritten ? "written" : tier == TierTold ? "told" : "remembered";

        /// <summary>The rule-based 'why' AT A VOICE TIER — deterministic, LLM-free, and the line that
        /// actually ships while the prose flag is off.</summary>
        public static string RuleBasedWhy(string eventText, string[] causes, int tier)
        {
            int n = causes == null ? 0 : causes.Length;
            if (n == 0)
            {
                switch (tier)
                {
                    case TierRemembered: return "It happened. No one kept the reason.";
                    case TierTold:       return "They tell it, but not why.";
                    case TierWritten:    return "No cause was set down beside it.";
                    default:             return "No cause was set down beside it — and not all that happens leaves one to find.";
                }
            }

            // REMEMBERED: an oral memory holds the nearest, heaviest thing — and admits it held only
            // that. The causes arrive ordered by class (forces first) so this choice is not accident.
            if (tier == TierRemembered)
                return n == 1 ? causes[0] + " had come first."
                              : causes[0] + " had come first. Other things too, not kept.";

            string list = Join(causes);
            // TOLD: the tellers can carry a chain now, but a telling is still hearsay.
            if (tier == TierTold)
                return n == 1 ? "They say it came of " + causes[0] + "."
                              : "They say it came of " + list + ".";
            // WRITTEN: it stops hedging. This is what was set down.
            if (tier == TierWritten)
                return n == 1 ? "It followed from one thing: " + causes[0] + "."
                              : "It followed from what came before: " + list + ".";
            // WEIGHED: it no longer only reports; it judges what else could have come.
            return n == 1 ? "It followed from " + causes[0] + ". Little else could have followed."
                          : "It followed from " + list + ". Little else could have followed.";
        }

        /// <summary>State-free default (the WRITTEN voice) — used only where no world is at hand: the
        /// view's failure paths. The living path freezes the tier at witness time (Entry.voiceTier).</summary>
        public static string RuleBasedWhy(string eventText, string[] causes) => RuleBasedWhy(eventText, causes, TierWritten);

        /// <summary>The rule-based line as THIS world would write it right now. Prefer the frozen
        /// per-entry tier where one exists — see the note on retroactivity in WhyProse.</summary>
        public string RuleBasedWhyNow(string eventText, string[] causes)
            => RuleBasedWhy(eventText, causes, VoiceTier(KnownTechs(StateNow())));

        static string Join(string[] causes)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < causes.Length; i++)
            {
                if (i > 0) sb.Append(i == causes.Length - 1 ? ", and " : ", ");
                sb.Append(causes[i]);
            }
            return sb.ToString();
        }

        /// <summary>The model's instructions AT A TIER — the same ladder, said to the model instead of
        /// executed in code, so an opened prose flag inherits the law rather than escaping it.</summary>
        public static string SystemPromptFor(int tier)
        {
            switch (tier)
            {
                case 0: return SystemPrompt + " This people has no writing. Speak as remembered aloud: " +
                               "ONE short clause, name at most one thing that came before, no chain of reasons.";
                case 1: return SystemPrompt + " This people has writing. One sentence; you may set the " +
                               "causes in order, plainly.";
                case 2: return SystemPrompt + " This people has writing and number. One sentence; you may " +
                               "count what came before.";
                default: return SystemPrompt + " This people has writing, number and philosophy. One " +
                               "sentence; you may weigh whether anything else could have followed.";
            }
        }
    }
}
