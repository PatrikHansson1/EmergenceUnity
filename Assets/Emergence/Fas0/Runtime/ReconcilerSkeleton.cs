// EMERGENCE — Fas 0 (D-107 Fas 0 / A7): the reconciler SKELETON.
//
// Fas 1 turns this into the live reconciler: read WorldState every tick, diff against the placed
// set, and materialise / upgrade / de-materialise Codex objects per each row's `when` gate
// (genesis -> growth -> onLoss). It reads state and NEVER writes back (D-078 rule 4). Any
// presentation variation is hash-based (hash(agentId) / hash(objectId)), never sim-RNG, so the
// golden master stays GREEN.
//
// Fas 0 deliberately carries NO sim coupling — it only owns the empty event bus and can emit a
// handful of dummy events to prove the plumbing (the Fas-0 grind: "event-bussen loggar dummy-events").
namespace Emergence.Runtime
{
    public sealed class ReconcilerSkeleton
    {
        public long Tick { get; private set; }

        /// <summary>
        /// Fas 1 signature will be Reconcile(worldState). For Fas 0 this emits one of each channel so
        /// the grind can confirm the bus + log + (future) subscribers are wired, with zero sim input.
        /// </summary>
        public void EmitSelfTestEvents()
        {
            Tick++;
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 0, "dawn", PresentationEventType.Milestone, "fas0.selftest.first-hearth", 0, "cause=fire"));
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 0, "dawn", PresentationEventType.AssetSpawned, "campfire", 0, "placement=green"));
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 1, "dawn", PresentationEventType.AssetUpgraded, "hut->house", 0, "supersedes=hut"));
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 2, "dawn", PresentationEventType.AssetRemoved, "hut", 0, "onLoss=remove"));
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 2, "dawn", PresentationEventType.AgentActivity, "agent:7", 0, "role=smith;activity=forge"));
            // reserved channels — proven present, wired to real producers later
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 2, "dawn", PresentationEventType.Chronicle, "fas0.selftest.chronicle", -1, "story hooks land in Fas 4"));
            PresentationEventBus.Publish(new PresentationEvent(
                Tick, 2, "dawn", PresentationEventType.Audio, "fas0.selftest.audio", -1, "audio hooks land in Fas 6"));
        }
    }
}
