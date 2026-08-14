// EMERGENCE — THE CODEX BUILD ORDER (C4+C5 of D-226/D-230).
// ONE place decides two things the codex could not say before:
//   WHETHER a village may show an object at all (the gate — was duplicated in
//   LiveReconciler and WorldDresser, two copies of one law), and
//   WHETHER IT HAS GOT THERE YET (the order and the ceiling).
//
// The measurement that forced this: over 3 seeds x 300 years, 43 of 72 codex kinds
// lit in a SINGLE checkpoint around year 60, and then nothing new appeared for 240
// years. The reason is that the gate only ever asked what a village KNOWS, and a
// village of six people knows twenty-six crafts by year 40 — knowledge arrives fast.
// What should pace a BUILDING is not knowledge. It is hands and lifetimes.
//
// So: a people build the oldest things first, and only as many as they can raise and
// keep. Capacity grows with population and with the generations a place has held —
// which is exactly what grows slowly over centuries, and is therefore what turns a
// flood into a history. Pure read of exported sim state; deterministic; no RNG at all
// (the order is a total sort on codex data, not a hash). D-078 r4 holds.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class CodexBuildOrder
    {
        // a village raises what its hands can raise: one to start, one per three souls,
        // one per generation the place has held. Capped so a city does not become a catalogue.
        public const int MilestoneCapMax = 24;
        public const int DressingCapMax  = 14;

        /// <summary>The `when` gate — the ONE copy. True when this village's state satisfies the entry.</summary>
        public static bool Qualifies(WorldVillage v, CodexEntry e)
        {
            if (v == null || e == null) return false;
            if (!string.IsNullOrEmpty(e.requiresTech) && (v.knows == null || Array.IndexOf(v.knows, e.requiresTech) < 0)) return false;
            if (!string.IsNullOrEmpty(e.requiresCustom))
            {
                if (e.requiresCustom == "cosmos") { if (string.IsNullOrEmpty(v.cosmos)) return false; }
                else if (v.beliefs == null || Array.IndexOf(v.beliefs, e.requiresCustom) < 0) return false;
            }
            return v.pop >= e.minPop && v.crafts >= e.minCrafts && v.maxGen >= e.minGen;
        }

        public static int MilestoneCap(WorldVillage v) => v == null ? 0 : Mathf.Clamp(1 + v.pop / 3 + v.maxGen, 1, MilestoneCapMax);
        public static int DressingCap(WorldVillage v)  => v == null ? 0 : Mathf.Clamp(v.pop / 2, 0, DressingCapMax);

        /// <summary>What this village may actually SHOW: qualified, oldest-first, cut at capacity.</summary>
        public static List<CodexEntry> Allowed(WorldVillage v, CodexEntry[] objects)
        {
            var outp = new List<CodexEntry>();
            if (v == null || objects == null) return outp;

            var q = new List<CodexEntry>();
            foreach (var e in objects) if (Qualifies(v, e)) q.Add(e);

            // the build order: the oldest things first, then the ones a small people could
            // manage, then a stable tiebreak. A total order on codex data alone — two runs of
            // the same state produce the same village, on any machine, forever.
            q.Sort((a, b) =>
            {
                int c = a.era.CompareTo(b.era);          if (c != 0) return c;
                c = a.minPop.CompareTo(b.minPop);        if (c != 0) return c;
                return string.CompareOrdinal(a.id, b.id);
            });

            int mCap = MilestoneCap(v), dCap = DressingCap(v), m = 0, d = 0;
            foreach (var e in q)
            {
                bool milestone = e.tier == "milestone";
                if (milestone) { if (m >= mCap) continue; m++; }
                else           { if (d >= dCap) continue; d++; }
                outp.Add(e);
            }
            return outp;
        }
    }
}
