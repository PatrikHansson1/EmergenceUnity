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
    [Serializable] public class WorldAgent { public int id; public string name; public float x, y; public float age; public int gen; public string task, say, sayAct; }
    [Serializable] public class WorldHut { public float x, y; public string owner; public bool free; }
    [Serializable] public class WorldFire { public float x, y; public float fuel; }
    [Serializable] public class WorldField { public float x, y; public int stage; public string owner; }
    // TD-033: villages carry their development profile (aggregate of members' knowledge + beliefs +
    // demographics) so the codex can place objects by DISCOVERY. Old exports lack these → default 0/null → safe.
    [Serializable] public class WorldVillage { public float x, y; public string name; public int pop, maxGen, avgAge, crafts; public string cosmos; public string[] knows; public string[] beliefs; }
    [Serializable] public class WorldAnimal { public int id; public string type; public float x, y; }
    // TD-033: the object codex — discovery-driven placement. JsonUtility-friendly flat schema.
    // D-112 (Fas 1 inc 2): ruinOnLoss=1 → when this built structure's gate stops holding (Memory Engine
    // loses the tech), the object de-materialises INTO a ruin instead of empty ground; rediscovery rebuilds it.
    // ruinPrefab overrides the studio default ruin stand-in; ruinScale sizes it (0 = default). Ephemeral/portable
    // objects (banners, carts, pots) keep ruinOnLoss=0 and simply vanish.
    // D-106 fill-pass: tier = milestone|dressing|part (legibility law); statMeaning feeds the STATS/Almanac pillar (Fas 5).
    [Serializable] public class CodexEntry { public string id, prefab, category, requiresTech, requiresCustom, desc, placement, tier, statMeaning; public int era, minPop, minCrafts, minGen, count; public float scale; public int ruinOnLoss; public string ruinPrefab; public float ruinScale; }
    [Serializable] public class Codex { public CodexEntry[] objects; }
    [Serializable] public class WorldState
    {
        public string engineVersion; public int seed, years, tick; public bool ended; public string season;
        public int W, H; public string tileTypes; public int[] tileN;
        public WorldAgent[] agents; public WorldHut[] huts; public WorldFire[] fires;
        public WorldField[] fields; public WorldVillage[] villages; public WorldAnimal[] animals;
    }
}
