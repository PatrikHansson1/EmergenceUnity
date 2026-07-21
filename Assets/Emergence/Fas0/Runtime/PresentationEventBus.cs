// EMERGENCE — Fas 0 (D-107 A4/A7): the presentation event bus, laid EMPTY now so audio (Fas 6)
// and the story layer (Fas 4) never require ripping the reconciler open later.
//
// LAW (D-078 rule 4 / A7): this is a PRESENTATION-side bus. The reconciler PUBLISHES events that
// are pure READS of deterministic sim-state; subscribers (audio, story, VFX) only listen. Nothing
// here writes back to the sim, and nothing here uses sim-RNG. Any presentation-side variation must
// be hash-based (e.g. hash(agentId)), decided by the subscriber, never by this bus. Given a
// deterministic reconciler tick order, the event stream is itself deterministic.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    /// <summary>The channels the body speaks on. Audio + Chronicle are reserved-empty in Fas 0.</summary>
    public enum PresentationEventType
    {
        AssetSpawned,   // reconciler materialised a Codex object (genesis/growth)
        AssetRemoved,   // onLoss / superseded — object de-materialised
        AssetUpgraded,  // hut -> house -> manor (supersedes)
        Milestone,      // a "first" — emits a chronicleEvent downstream (Fas 4)
        AgentActivity,  // an agent changed role/activity/emotion (drives Animator in Fas 2)
        Chronicle,      // reserved for the story layer (Fas 4)
        Audio,          // reserved for the audio layer (Fas 6)
        Custom
    }

    /// <summary>An immutable, state-derived presentation event. All fields come from sim-state; no RNG.</summary>
    public readonly struct PresentationEvent
    {
        public readonly long Tick;
        public readonly int Year;
        public readonly string Era;
        public readonly PresentationEventType Type;
        public readonly string Id;        // codex id / asset id / event id
        public readonly int VillageId;    // -1 = world scope
        public readonly string Data;      // small structured payload; never RNG-derived

        public PresentationEvent(long tick, int year, string era, PresentationEventType type,
                                 string id, int villageId = -1, string data = null)
        {
            Tick = tick; Year = year; Era = era ?? ""; Type = type;
            Id = id ?? ""; VillageId = villageId; Data = data ?? "";
        }

        public override string ToString()
            => $"t{Tick} y{Year} {Era} {Type} id={Id} v{VillageId}" + (Data.Length > 0 ? $" {{{Data}}}" : "");
    }

    /// <summary>
    /// Deterministic, read-only presentation event bus. Fas 0 lays it empty; Fas 1's reconciler
    /// publishes to it; Fas 4 (story) and Fas 6 (audio) subscribe. A bounded in-memory log lets the
    /// Fas-0 grind prove the bus carries dummy events without any subscriber wired.
    /// </summary>
    public static class PresentationEventBus
    {
        /// <summary>Subscribe presentation subsystems here. Invoked in publish order (deterministic).</summary>
        public static event Action<PresentationEvent> OnEvent;

        static readonly List<PresentationEvent> _log = new List<PresentationEvent>();

        /// <summary>Bounded log so an un-drained bus can never grow without limit.</summary>
        public static int LogCapacity = 8192;
        public static bool Verbose = false;

        public static int Count => _log.Count;
        public static IReadOnlyList<PresentationEvent> Log => _log;

        public static void Publish(in PresentationEvent e)
        {
            if (_log.Count < LogCapacity) _log.Add(e);
            if (Verbose) Debug.Log("[EventBus] " + e);
            var handler = OnEvent;
            if (handler != null) handler(e);
        }

        public static void Clear() => _log.Clear();

        /// <summary>Reset subscribers — call on world teardown so a rebuilt world starts clean (A7).</summary>
        public static void ResetSubscribers() => OnEvent = null;

        public static void DumpLog(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# PresentationEventBus log — {_log.Count} events");
            foreach (var e in _log) sb.AppendLine(e.ToString());
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, sb.ToString());
        }
    }
}
