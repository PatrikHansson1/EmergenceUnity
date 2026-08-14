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
        readonly FireReconciler _fires = new FireReconciler();   // Fas 6 ink. 3 (D-158): the living fire layer

        public int AppliedCount { get; private set; }
        public int LastAppliedYear { get; private set; } = -1;

        // FAS 6 gate review r1, I2: probes inject fixture snapshots through this SAME Apply path
        // (data injection, never a logic fork). Injection is reconstruction, NOT witnessed history —
        // same law as the scrub burst (Fas3PresentationClock.ApplyingJump). Probes set this flag
        // around a fixture Apply so chronicle-class consumers stay silent; production never sets it.
        public static bool FixtureInjection;

        // FAS 7 ink. 0 (G-review r2 fynd I4): the synchronous flag can't cover LATE readers (the
        // metrics recorder samples LastState in Update, frames after the Apply) — so the Apply
        // stamps whether the snapshot standing applied came from an injection. A following live
        // Apply clears it. Read-only outside.
        public bool LastApplyWasFixture { get; private set; }

        // FAS 4 (ChronicleFeed): the last two applied snapshots, exposed READ-ONLY so bus consumers
        // can resolve sim-given NAMES (WorldAgent.name) at event time. Set BEFORE reconciling so the
        // synchronous event burst of an Apply can already see the state it derives from; PrevState
        // serves departures (a departing soul only exists in the previous snapshot). Pure read — D-078 r4.
        public WorldState LastState { get; private set; }
        public WorldState PrevState { get; private set; }
        public int HutCount => _huts.Count;
        public int AgentCount => _agents.Count;
        public int FireCount => _fires.Count;
        public int SmokeCount => _fires.SmokeCount;
        public int CodexPlacedCount => _codex.PlacedCount;
        public string LastCodexNote { get; private set; } = "";

        /// <summary>Reconcile all live layers to this snapshot. Codex failures never break agents/huts.</summary>
        public void Apply(WorldState S)
        {
            if (S == null) return;
            LastApplyWasFixture = FixtureInjection;   // I4: late readers ask the applied snapshot, not the flag
            PrevState = LastState; LastState = S;
            EnsureGround(S);                          // VÅG 1.1: the world needs ground before anything stands on it
            _agents.Reconcile(S, false);
            _huts.Reconcile(S);
            try { _codex.Reconcile(S); LastCodexNote = "ok"; }
            catch (Exception e) { LastCodexNote = e.Message; Debug.LogWarning("[Fas3WorldRuntime] codex: " + e.Message); }
            // fires are dressing-tier: a failure here must never break agents/huts (same clause as codex)
            try { _fires.Reconcile(S); }
            catch (Exception e) { Debug.LogWarning("[Fas3WorldRuntime] fires: " + e.Message); }
            // E1.5: village-scope drama (a leader recognized/lost, a gift-way named) reaches the bus
            // as a minimal ADDITIVE publication — a pure diff of applied snapshots, no RNG, bounded
            // by the village count. Same failure clause as codex: never breaks agents/huts.
            try { PublishVillageDrama(PrevState, S); }
            catch (Exception e) { Debug.LogWarning("[Fas3WorldRuntime] villages: " + e.Message); }
            AppliedCount++;
            LastAppliedYear = S.years;
        }

        // ---- VÅG 1.1 (D-209): THE GROUND ----
        // The living loop built no terrain at all until today: the entire WorldDresser sits behind
        // #if UNITY_EDITOR, so the player's world was a flat green plane while the capture rig's
        // screenshots looked like a game. The terrain law now lives in Runtime (Fas3TerrainBuilder)
        // and is raised HERE, from the first applied snapshot that carries a map — which is the
        // genesis snapshot, since the map is fixed for the whole run.
        //
        // Idempotent and forgiving by design: an already-dressed scene (a probe, the store rig) is
        // left alone, and a failure here must never break agents/huts — a world without ground still
        // simulates, it just looks wrong, and GroundNote says so out loud.
        public bool GroundBuilt { get; private set; }
        public string GroundNote { get; private set; } = "";

        void EnsureGround(WorldState S)
        {
            if (GroundBuilt || S == null || string.IsNullOrEmpty(S.tileTypes)) return;
            GroundBuilt = true;                        // one attempt per session, success or not
            try
            {
                var existing = GameObject.Find("Terrain");
                if (existing != null) { GroundNote = "terrain already in the scene — left alone"; return; }
                var go = Fas3TerrainBuilder.Build(S, null);
                GroundNote = go != null ? Fas3TerrainBuilder.LastDiag : "terrain build returned null";
                Debug.Log("[Fas3WorldRuntime] " + GroundNote);
            }
            catch (Exception e)
            {
                GroundNote = "terrain FAILED: " + e.Message;
                Debug.LogWarning("[Fas3WorldRuntime] " + GroundNote);
            }
        }

        // ---- E1.5 village drama diff (Engine 2.4.1: villages[].leader + villages[].gift) ----
        // WITNESS LAW: only CHANGES between two applied snapshots are events (the first Apply of a
        // session publishes nothing — a pre-existing leader is standing fact, not witnessed history).
        // Consumers keep their own guards (ApplyingJump / FixtureInjection) exactly as for agents.
        static void PublishVillageDrama(WorldState prev, WorldState S)
        {
            if (prev == null || S?.villages == null) return;
            for (int i = 0; i < S.villages.Length; i++)
            {
                var v = S.villages[i];
                if (v == null || string.IsNullOrEmpty(v.name)) continue;
                string pl = "", pg = "";
                if (prev.villages != null)
                    foreach (var pv in prev.villages)
                        if (pv != null && pv.name == v.name) { pl = pv.leader ?? ""; pg = pv.gift ?? ""; break; }
                string leader = v.leader ?? "", gift = v.gift ?? "";
                if (leader.Length > 0 && leader != pl)
                    PresentationEventBus.Publish(new PresentationEvent(
                        S.tick, S.years, WorldEras.Name(S), PresentationEventType.Custom, "village:" + v.name, i, "leader: " + leader));
                else if (leader.Length == 0 && pl.Length > 0)
                    PresentationEventBus.Publish(new PresentationEvent(
                        S.tick, S.years, WorldEras.Name(S), PresentationEventType.Custom, "village:" + v.name, i, "leader-gone: " + pl));
                if (gift.Length > 0 && gift != pg)
                    PresentationEventBus.Publish(new PresentationEvent(
                        S.tick, S.years, WorldEras.Name(S), PresentationEventType.Custom, "village:" + v.name, i, "giftway: " + gift));
            }
        }

        public void Apply(string snapshotJson) => Apply(JsonUtility.FromJson<WorldState>(snapshotJson));

        /// <summary>Clear every live layer (scrub entry point — a checkpoint Apply rebuilds the world).</summary>
        public void ResetWorld()
        {
            _agents.Clear(); _huts.Clear(); _codex.Clear(); _fires.Clear();
            AppliedCount = 0; LastAppliedYear = -1;
            LastState = null; PrevState = null;
            GroundBuilt = false; GroundNote = "";
        }
    }
}
