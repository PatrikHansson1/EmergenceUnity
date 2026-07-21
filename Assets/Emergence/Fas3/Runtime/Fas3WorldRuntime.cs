// EMERGENCE — FAS 3 increment 4 (D-137): the RUNTIME WATCH LOOP — one component owns the world's
// living layers in a player build.
//
// Until now every probe wired the three reconcilers by hand in editor code. This MonoBehaviour is
// the player-shippable owner: agents (Fas 2), huts (Fas 3 inc 2) and the codex overlay (Fas 1) are
// reconciled per applied year snapshot. It has NO editor dependencies — reconcilers load through
// EmergenceAssetCatalog (Resources), so the same component runs in editor play mode and player.
//
// D-078 r4: reads snapshots, writes nothing back. ResetWorld() clears all live layers — the scrub
// path (Fas3PresentationClock.JumpToYear) rebuilds any checkpointed year from a single Apply.
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3WorldRuntime : MonoBehaviour
    {
        readonly AgentReconciler _agents = new AgentReconciler();
        readonly HutReconciler _huts = new HutReconciler();
        readonly LiveReconciler _codex = new LiveReconciler();

        public int AppliedCount { get; private set; }
        public int LastAppliedYear { get; private set; } = -1;
        public int HutCount => _huts.Count;
        public int AgentCount => _agents.Count;
        public int CodexPlacedCount => _codex.PlacedCount;
        public string LastCodexNote { get; private set; } = "";

        /// <summary>Reconcile all live layers to this snapshot. Codex failures never break agents/huts.</summary>
        public void Apply(WorldState S)
        {
            if (S == null) return;
            _agents.Reconcile(S, false);
            _huts.Reconcile(S);
            try { _codex.Reconcile(S); LastCodexNote = "ok"; }
            catch (Exception e) { LastCodexNote = e.Message; Debug.LogWarning("[Fas3WorldRuntime] codex: " + e.Message); }
            AppliedCount++;
            LastAppliedYear = S.years;
        }

        public void Apply(string snapshotJson) => Apply(JsonUtility.FromJson<WorldState>(snapshotJson));

        /// <summary>Clear every live layer (scrub entry point — a checkpoint Apply rebuilds the world).</summary>
        public void ResetWorld()
        {
            _agents.Clear(); _huts.Clear(); _codex.Clear();
            AppliedCount = 0; LastAppliedYear = -1;
        }
    }
}
