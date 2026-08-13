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
        public async Task<string> WhyProse(string eventText, string[] causes)
        {
            string key = KeyFor(eventText, causes);
            if (_cache.TryGetValue(key, out var hit)) return hit;

            if (!useProse || character == null)
                return RuleBasedWhy(eventText, causes);   // fallback: no LLM, no await, always available

            Prime();
            character.seed = SeedFor(key);                // fixed per entry -> reproducible
            string reply;
            try { reply = await character.Chat(BuildPrompt(eventText, causes), null, null, false); }
            catch (Exception e)
            {
                Debug.LogWarning("[Fas4ProseDirector] LLM failed, using fallback: " + e.Message);
                return RuleBasedWhy(eventText, causes);
            }

            // shape -> word-ban -> invention guard. Any refusal falls back to the rule-based line:
            // a plainer sentence that is TRUE beats a finer one that is not.
            reply = Shape(reply);
            reply = Sanitize(reply);
            if (string.IsNullOrEmpty(reply)) { RefusedWordBan++; reply = RuleBasedWhy(eventText, causes); }
            else if (InventsNames(reply, eventText, causes)) { RefusedInvention++; reply = RuleBasedWhy(eventText, causes); }
            else Accepted++;
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

        // Word-ban guard (emergence-game-copywriter law 3) — if the model slips, drop to fallback.
        //
        // Two bans, one law. The first is the FOURTH-WALL ban: words that admit a machine is speaking.
        // The second is the ANACHRONISM ban, added 2026-08-13 after the live run answered "a first
        // child is born" with "the mother asked for an abortion and the father agreed to provide
        // financial support" — no forbidden word, no invented name, and still a sentence from another
        // world entirely. Emergence's legibility law says nothing anachronistic may be placed in the
        // world (D-106); a chronicle line is placed in the world too. A small model reaches for the
        // modern century whenever the facts are thin, so the thin cases must be caught by vocabulary.
        // Conservative by design: a refusal costs a plainer sentence, an accept costs the world.
        static readonly string[] Banned = {
            // fourth wall
            "simulation", "simulator", "simulated", "procedural", "algorithm", "deterministic", "engine", "AI system", "RNG",
            // modernity: money and institutions
            "financial", "finance", "money", "dollar", "budget", "insurance", "bank", "economy", "economic",
            "government", "police", "hospital", "doctor", "clinic", "nurse", "school", "university", "company",
            // modernity: technology and transport
            "phone", "computer", "internet", "electric", "engineered", "machine", "factory", "car ", "train ",
            // modernity: clinical and legal register
            "abortion", "medical", "medication", "diagnosis", "patient", "therapy", "legal", "lawyer", "contract",
            "percent", "statistics", "data" };
        static string Sanitize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            foreach (var b in Banned)
                if (s.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0)
                    return "";   // let the fallback take over rather than ship a banned word
            return s;
        }

        /// <summary>Deterministic, LLM-free 'why' from the causes — the safe default the flag falls back to.</summary>
        public static string RuleBasedWhy(string eventText, string[] causes)
        {
            if (causes == null || causes.Length == 0) return "The chronicle records no cause — it simply happened.";
            if (causes.Length == 1) return "It followed from one thing: " + causes[0] + ".";
            var sb = new StringBuilder("It followed from what came before: ");
            for (int i = 0; i < causes.Length; i++)
            {
                if (i > 0) sb.Append(i == causes.Length - 1 ? ", and " : ", ");
                sb.Append(causes[i]);
            }
            sb.Append('.');
            return sb.ToString();
        }
    }
}
