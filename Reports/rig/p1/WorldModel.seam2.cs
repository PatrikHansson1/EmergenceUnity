// EMERGENCE — FAS 3 increment 4 (D-137): the WORLD MODEL moves to RUNTIME.
//
// These serializable snapshot/codex types were born inside WorldDresser.cs (editor-only). The
// player-runtime refactor needs them on BOTH sides: the editor dressers/probes AND the runtime
// reconcilers that live in a player build. Moving the DATA MODEL is a pure structural refactor —
// no field, no name, no semantics changed — so every JsonUtility read of the existing snapshot
// files (seq-*.json / world-*.json) parses byte-identically.
//
// D-078 r4 still rules: these are READ-side types. Nothing here ever writes into the sim.
using System;

namespace Emergence.Runtime
{
    // R2 ink. 1 (Engine 2.3.2, TD-076): the export additively carries agents[].verb — one of the
    // engine's 15 canonical work verbs (idle move gather carry work harvest hunt fish eat rest grow
    // social ritual fight trade), derived engine-side from task. Old exports lack it → null → the
    // body falls back to the task classification (AgentTaskRead). Additive field only — parser untouched.
    // E1.5 (Engine 2.4.1, TD-080..082): the export additively carries agents[].wealth (E.wealthOf —
    // the aspiration/hoarding proxy, the Almanac's wealth sort feeds on it). Old exports lack it
    // → 0 → the wealth sort degrades to its tie law (age DESC) — no consumer breaks. Additive only.
    [Serializable] public class WorldAgent { public int id; public string name; public float x, y; public float age; public int gen; public string task, say, sayAct; public string verb; public float wealth; }
    [Serializable] public class WorldHut { public float x, y; public string owner; public bool free; }
    [Serializable] public class WorldFire { public float x, y; public float fuel; }
    [Serializable] public class WorldField { public float x, y; public int stage; public string owner; }
    // TD-033: villages carry their development profile (aggregate of members' knowledge + beliefs +
    // demographics) so the codex can place objects by DISCOVERY. Old exports lack these → default 0/null → safe.
    // E1.5 (Engine 2.4.1): villages additively carry leader (the recognized leader's NAME, '' when no
    // one is recognized — 4242 taught us big villages can go leaderless for 120 years by design) and
    // gift (the named gift-way, e.g. "The Hearth-Gift", '' until the custom is named). Old exports
    // lack both → null/'' → dossiers simply omit the rows. Additive only — parser untouched.
    [Serializable] public class WorldVillage { public float x, y; public string name; public int pop, maxGen, avgAge, crafts; public string cosmos; public string[] knows; public string[] beliefs; public string leader; public string gift; public int lost, everHeld; }
    [Serializable] public class WorldAnimal { public int id; public string type; public float x, y; }
    // TD-033: the object codex — discovery-driven placement. JsonUtility-friendly flat schema.
    // D-112 (Fas 1 inc 2): ruinOnLoss=1 → when this built structure's gate stops holding (Memory Engine
    // loses the tech), the object de-materialises INTO a ruin instead of empty ground; rediscovery rebuilds it.
    // ruinPrefab overrides the studio default ruin stand-in; ruinScale sizes it (0 = default). Ephemeral/portable
    // objects (banners, carts, pots) keep ruinOnLoss=0 and simply vanish.
    // D-106 fill-pass: tier = milestone|dressing|part (legibility law); statMeaning feeds the STATS/Almanac pillar (Fas 5).
    // C3 (D-232, EP: "objekten i codexen skall också kunna mixas då vi ju faktiskt inte vet vad som
    // kommer upptäckas under civilisationens gång"). A single requiresTech could only ever say ONE
    // thing, so a combination — bronze needs copper AND tin; a market needs trade OR a road — was
    // unsayable. `requires` says it: allOf / anyOf / noneOf over the same facts the flat fields use.
    // The flat fields REMAIN and are still evaluated (AND-ed with `requires`), so every existing entry
    // keeps working untouched — the shorthand for the overwhelmingly common single-tech case.
    // D-253 — THE CODEX INDEXED KNOWLEDGE AND NOT HISTORY.
    // Measured: 98 of 108 rows answer to a technology, 1 to a belief, 9 to deep time, and ZERO to
    // anything that HAPPENED — while the engine writes thirty-nine event kinds into the chronicle
    // (raid, feud, death, mourn, leader, tribute, rebel, conversion, steal, violence, legend, giftway…).
    // A village is raided, the book says so, and the village looks exactly as it did.
    // It was never an ASSET gap — we own the gallows, the coffin, the banners, the memorial stones.
    // It was a GATE gap: the predicate language could only speak about what a people KNOW.
    // These two facts have been in the export since E1.5 (D-178) and nothing has ever read them:
    //   hasLeader — this village has recognised someone (villages[].leader)
    //   hasGift   — this village is bound by a giving custom (villages[].gift)
    // Pure read of exported state, no engine change, D-078 r4 untouched.
    [Serializable] public class CodexCond { public string tech, custom; public int minPop, minCrafts, minGen; public int hasLeader, hasGift, minLost; }
    [Serializable] public class CodexRequire { public CodexCond[] allOf; public CodexCond[] anyOf; public CodexCond[] noneOf; }
    [Serializable] public class CodexEntry { public string id, prefab, category, requiresTech, requiresCustom, desc, placement, tier, statMeaning; public int era, minPop, minCrafts, minGen, count; public float scale; public int ruinOnLoss; public string ruinPrefab; public float ruinScale;
        public CodexRequire requires;          // C3: the predicate, when one tech cannot say it
        // C3 combination: when every named part is itself allowed here, those parts BECOME this whole —
        // the parts are then suppressed, because a market-square is not a market next to some crates.
        // Per OBJECT-CODEX-SPEC 5b.1: a combination with no resolvable prefab is TOLD, never shown broken.
        public string[] combinesWith;
        // ARRANGEMENT TEMPLATE (D-242, OBJECT-CODEX-SPEC §2b(1)). Without this a "whole" is only a
        // prefab with a higher count standing where its parts used to stand — which is why the first
        // combination entries read as less, not more. The template is the AUTHORED RECIPE: parts at
        // fixed offsets in the whole's own frame, so a market square is a square and not a heap.
        // Offsets are metres relative to the anchor, yaw is degrees added to the anchor's own facing,
        // and every part is grounded and parented to the anchor — so the whole retires, ruins and
        // settles as ONE thing. No RNG: the recipe is authored, the anchor is hash-placed (D-078 r4).
        public CodexPart[] arrangement;
        // VARIANTS (D-243). The coverage tool called 1611 assets "un-indexed content", and a large part
        // of that was an illusion of its own making: the codex named P_PROP_crate_01 and then reported
        // crate_02, _03 and _04 as orphans. They are not un-indexed things — they are the SAME thing made
        // by a different hand. Naming them here does two jobs at once: it indexes hundreds of owned assets
        // without inventing hundreds of gates, and it ends the world where every crate in every village is
        // the identical crate. The pick is hash(position, id, index) — deterministic, never sim RNG.
        public string[] variants; }
    [Serializable] public class CodexPart { public string prefab; public float dx, dz, yaw, scale; }
    [Serializable] public class Codex { public CodexEntry[] objects; }
    // FAS 4 PROSE WIRING (2026-08-13, FAS4-PROSE-DIRECTOR-ORDER §1): the engine has carried
    // causes[] on every event since R2 ink. 1 (D-172), but nothing crossed into the body — the live
    // export shipped STATE only, never S.events, so the why-expander had no data and the prose
    // director no substrate. The driver's export JS now additively reads a BOUNDED tail of the
    // engine's own causes-bearing events and RESOLVES each reference ('ev:<id>' | 'agent:<id>' |
    // 'tech:<id>' | 'cause:<key>') into a plain phrase engine-side, where the lookup data lives.
    // Pure READ (D-078 r4): the engine JS is untouched, no sim state is written, no RNG is consumed.
    // Old snapshots/checkpoints/fixtures lack the field -> null -> the rule-based "why" stands.
    [Serializable] public class WorldEvent
    {
        public int id;          // stable engine event id (index in S.events at emission)
        public int year;        // engine-side year (1-based; the body's S.years is 0-based)
        public string type;     // child | death | steal | raid | feud | mourn | sharing | leader | giftway | ...
        public int agent;       // acting soul's id, -1 when the event is not agent-scoped
        public string village;  // village name for village-scope events, "" otherwise
        public string[] causes; // RESOLVED, reader-ready cause phrases (never raw refs)
    }
    [Serializable] public class WorldState
    {
        public string engineVersion; public int seed, years, tick; public bool ended; public string season;
        // D-147: era = max TECH[t].era over living souls' knowledge (derived read-only in the driver's
        // export JS — the engine is untouched). Old snapshots/checkpoints lack the field → 0 = "dawn".
        public int era;
        // R2 ink. 1 (Engine 2.3.2, TD-076): the ENGINE now owns era canon — eraName carries the
        // canonical name ("The First Morning" … "The Age of Steam"). Old snapshots/checkpoints lack
        // the field → null/"" → WorldEras interim fallback. Additive field only — parser untouched.
        public string eraName;
        // P1 (D-611): the interval report — 3–5 lines for the last ~century, written by the presentation layer
        // (emergence-presentation.js, a pure read over S.events). Empty when the layer is not loaded. Additive.
        public string intervalReport;
        public int W, H; public string tileTypes; public int[] tileN;
        public WorldAgent[] agents; public WorldHut[] huts; public WorldFire[] fires;
        public WorldField[] fields; public WorldVillage[] villages; public WorldAnimal[] animals;
        // FAS 4 prose wiring: bounded tail of causes-bearing engine events for this snapshot year.
        public WorldEvent[] events;
        // FAS 4 voice ladder (2026-08-14, fientlig granskning inv. 2): villages[].knows is EMPTY
        // before the first village is founded, and again after a collapse empties them — so a
        // union over villages would silence the whole opening by an accident of the data model
        // rather than by law. The world's own living knowledge closes that hole. Pure read.
        public string[] worldKnows;
    }

    /// <summary>D-147: presentation-side era naming — the D-146 finding was that the bus's Era slot
    /// carried the SEASON ("spring"). These interim names label the derived era index until the
    /// engine owns era canon officially (ordered: MOTOR-LANE-ORDER-R2-FAS4 §5). Pure labels — no
    /// state, no RNG (D-078 r4).</summary>
    public static class WorldEras
    {
        static readonly string[] Names = { "dawn", "stone", "bronze", "iron", "mill", "print", "steam" };
        public static string Name(int era) => era >= 0 && era < Names.Length ? Names[era] : "era-" + era;

        // ---- R2 ink. 1 (Engine 2.3.2): the ENGINE owns era canon. THE ONE ERA-NAME LAW: ----
        // a non-empty engine eraName IS the name; empty/null (old exports, old checkpoints, old
        // fixtures) falls back to the interim names above. Backward compatibility is part of the
        // proof (Fas7R2BodyProbe) — no consumer may ever show an empty era string.
        /// <summary>Era label for a (derived era index, engine eraName) pair — engine name wins when present.</summary>
        public static string Name(int era, string eraName) => !string.IsNullOrEmpty(eraName) ? eraName : Name(era);
        /// <summary>Era label for an applied state — engine name wins when present; null-safe.</summary>
        public static string Name(WorldState S) => S == null ? Names[0] : Name(S.era, S.eraName);
    }
}
