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

        [Tooltip("Max tokens for a 'why' line — kept short; the chronicle voice is terse.")]
        public int maxTokens = 60;

        // The chronicle-narrator voice (emergence-game-copywriter, diegetic variant).
        public const string SystemPrompt =
            "You are the voice of a chronicle that no one wrote — a history found in an archive. " +
            "You are given the facts of one event and its causes. In ONE or TWO short sentences, tell " +
            "why it happened, using ONLY those facts. Invent nothing: if the causes do not explain it, " +
            "say only what they show. Use plain human words — never technical words (no 'simulation', " +
            "'algorithm', 'system', 'engine', 'data'). Norse, quiet, weighty. Show, do not tell. No " +
            "exclamation marks. Name people when the facts name them.";

        // Per-entry cache: identical facts -> identical line, no re-inference (determinism + cost).
        readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        bool _primed;

        void Prime()
        {
            if (_primed || character == null) return;
            character.systemPrompt = SystemPrompt;
            character.temperature = 0f;      // greedy -> reproducible for a given model + backend
            character.numPredict = maxTokens;
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

            reply = Sanitize(reply);
            if (string.IsNullOrEmpty(reply)) reply = RuleBasedWhy(eventText, causes);
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

        // Word-ban guard (emergence-game-copywriter law 3) — if the model slips, drop to fallback.
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
