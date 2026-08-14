// EMERGENCE — FAS 4 v0 (per FAS4-KICKOFF-BRIEF 2026-07-22): the CHRONICLE FEED — the story engine
// begins to speak. Existence condition B ("a retellable story WITH NAMES") gets its first organ.
//
// This is consumer #3 on the Fas 0 PresentationEventBus (after the gaze D-134 and the ear D-141):
// it subscribes, never writes back, and turns the deterministic event stream into chronicle
// entries — year + era + text + rule-based salience v0. NAMES come from the sim's own export
// (WorldAgent.name via Fas3WorldRuntime.LastState/PrevState — a pure state READ at event time);
// the engine names its souls, the chronicle retells them. Milestone events already carry the
// Codex chronicleEvent text in Data (LiveReconciler publishes e.desc since Fas 1) — used verbatim.
//
// DETERMINISM (D-078 r4): entries derive ONLY from the bus stream + applied state, both of which
// are deterministic. No RNG of any kind here — not even hash variation; same run, same chronicle.
// SCRUB HONESTY (D-137/D-140): JumpToYear rebuilds the world from a checkpoint — that synchronous
// re-materialisation burst is reconstruction, NOT newly witnessed history (clock.ApplyingJump
// guards it); a backward jump TRIMS the feed to the presentation year, the same checkpoint honesty
// as every other layer. Forward jumps leave an un-witnessed gap by design in v0 (the chronicle is
// a witness, not an oracle — engine-lane writeHistory/causes[] will make it one, see the brief).
//
// v0 deliberately does NOT do: LLM prose (A5), why-expander (needs engine causes[]), native
// UI Toolkit view (IMGUI proof first), three scales (world scale only).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas4ChronicleFeed : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public int year;
            public string era;
            public int salience;    // 1 = routine ledger, 2 = notable, 3 = a turning point
            public string kind;     // arrival | birth | death | milestone | asset | steal | raid | feud | mourn | gift | leader | giftway (E1.5)
            public string text;
            public string key;      // dedupe identity: year|type|id|data
            // FAS 4 PROSE WIRING (2026-08-13): the engine's own causes for this beat, already
            // RESOLVED to reader-ready phrases by the export (see WorldModel.WorldEvent). Empty
            // when the beat has no engine event behind it (codex milestones, asset placements)
            // or when an old export/fixture carries no events[] — the why-line then says so.
            public string[] causes;
            public int eventId;     // engine event id, -1 when unmatched
            // FROZEN AT WITNESS TIME (2026-08-14, hostile review inv. 4+5): the voice this entry was
            // written in. Never recomputed — the book does not rewrite its own past when the people
            // learn something new, and it does not grow dumber when they forget. A shared seed + year
            // must reproduce a quoted line, or the chronicle stops being evidence (condition B).
            public int voiceTier;
        }

        public const int Capacity = 4096;          // bounded like the bus — a feed can never grow without limit
        public bool showUI = true;
        public int minSalience = 1;                // IMGUI filter: 1 = allt, 2 = märkbart, 3 = vändpunkter

        readonly List<Entry> _entries = new List<Entry>();
        readonly HashSet<string> _keys = new HashSet<string>();
        bool _firstChildSeen, _firstDeathSeen;

        public IReadOnlyList<Entry> Entries => _entries;
        public int TrimCount { get; private set; }             // proof: backward scrubs that trimmed the feed
        public int SuppressedDuringJump { get; private set; }  // proof: rebuild-burst events kept out
        public int SuppressedDuringFixture { get; private set; } // proof (G-review r1 I2): fixture-injection events kept out
        public int DroppedOldest { get; private set; }
        public int DedupeHits { get; private set; }

        Fas3PresentationClock _clock;
        Fas3WorldRuntime _world;
        double _lastPresTick;

        void OnEnable() { PresentationEventBus.OnEvent += OnBus; }
        void OnDisable() { PresentationEventBus.OnEvent -= OnBus; }

        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }
        Fas3WorldRuntime World() { if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>(); return _world; }

        void Update()
        {
            var c = Clock(); if (c == null) return;
            // a backward jump moved presentation time — the chronicle keeps only what that timeline has lived
            if (c.PresentationTick < _lastPresTick - 0.5) TrimTo(c.PresentationYear);
            _lastPresTick = c.PresentationTick;
        }

        void OnBus(PresentationEvent e)
        {
            var c = Clock();
            if (c != null && c.ApplyingJump) { SuppressedDuringJump++; return; }   // reconstruction, not history
            // G-review r1 I2: a probe's fixture Apply is injection, not witnessed history — the chronicle stays clean
            if (Fas3WorldRuntime.FixtureInjection) { SuppressedDuringFixture++; return; }

            string key = e.Year + "|" + e.Type + "|" + e.Id + "|" + e.Data;
            int salience; string kind, text;

            switch (e.Type)
            {
                case PresentationEventType.Milestone:
                    // the canon beat — Codex chronicleEvent text (e.desc) or a reconciler's own line.
                    // Trailer round (slot 5 capture finding): the D-106 "(told-not-shown …)" mechanism
                    // marker (reconciler prefix + codex-desc suffix) leaked verbatim into the READER's
                    // book — internal grammar, not story. Stripped HERE, at the READ layer only: the
                    // bus keeps carrying it (probe classification + traceability are bus-side).
                    salience = 3; kind = "milestone"; text = StripMechanismMarkers(e.Data);
                    break;

                case PresentationEventType.AgentActivity:
                    if (e.Data == "a soul arrives")
                    { salience = 2; kind = "arrival"; text = NameOf(e.Id, false) + " arrives — a soul steps onto the empty land"; }
                    else if (e.Data == "a child is born")
                    {
                        if (!_firstChildSeen) { _firstChildSeen = true; salience = 3; text = "a first child is born — " + NameOf(e.Id, false); }
                        else { salience = 1; text = NameOf(e.Id, false) + " is born"; }
                        kind = "birth";
                    }
                    else if (e.Data == "a soul departs")
                    {
                        if (!_firstDeathSeen) { _firstDeathSeen = true; salience = 3; text = "death finds the people for the first time — " + NameOf(e.Id, true) + " departs"; }
                        else { salience = 1; text = NameOf(e.Id, true) + " departs"; }
                        kind = "death";
                    }
                    // E1.5 (Engine 2.4.1): the DRAMA acts reach the book. Rule-based v0-salience,
                    // extended per the E1.5 body order: feud + raid = ★ TURNING POINTS (blood answers
                    // blood, violence over goods), steal = notable, mourn/gift = notable. submit is
                    // deliberately the body's business only (tempo + attend) — the chronicle does not
                    // ledger every bowed head in v0. Names come from the applied state as always.
                    else if (e.Data.StartsWith("sayAct: "))
                    {
                        switch (e.Data.Substring(8))
                        {
                            case "feud":
                                salience = 3; kind = "feud";
                                text = NameOf(e.Id, false) + " comes for an old wrong — a feud, not forgotten"; break;
                            case "raid":
                                salience = 3; kind = "raid";
                                text = NameOf(e.Id, false) + " sets upon another's stores — a raid"; break;
                            case "steal":
                                salience = 2; kind = "steal";
                                text = "want owned the hand — " + NameOf(e.Id, false) + " steals"; break;
                            case "mourn":
                                salience = 2; kind = "mourn";
                                text = NameOf(e.Id, false) + " mourns — and does not forget"; break;
                            case "gift":
                                salience = 2; kind = "gift";
                                text = NameOf(e.Id, false) + " gives — the gift itself binds"; break;
                            default: return;   // submit + future acts: witnessed by the body, not the book (v0)
                        }
                    }
                    else return;   // task/age changes are the body's business, not the chronicle's (v0)
                    break;

                // E1.5: village-scope drama published by Fas3WorldRuntime's applied-state diff —
                // a leader recognized/lost, a gift-way named. All NOTABLE (salience 2) per the body
                // order; the village name rides the shared VillageId suffix below.
                case PresentationEventType.Custom:
                    if (e.Data.StartsWith("leader: "))
                    { salience = 2; kind = "leader"; text = "the people begin to listen when " + e.Data.Substring(8) + " speaks"; }
                    else if (e.Data.StartsWith("leader-gone: "))
                    { salience = 2; kind = "leader"; text = e.Data.Substring(13) + "'s voice is gone — no one yet speaks for all"; }
                    else if (e.Data.StartsWith("giftway: "))
                    { salience = 2; kind = "giftway"; text = "the giving has become a way with a name — " + e.Data.Substring(9); }
                    else return;
                    break;

                case PresentationEventType.AssetSpawned:
                    salience = 1; kind = "asset";
                    text = e.Id.StartsWith("hut:") ? "a hut is raised" : e.Id + " takes its place in the world";
                    break;

                case PresentationEventType.AssetRemoved:
                    kind = "asset";
                    if (e.Data.Contains("toRuin")) { salience = 2; text = e.Id + " falls to ruin"; }
                    else { salience = 1; text = e.Id.StartsWith("hut:") ? "a hut is lost" : e.Id + " is gone"; }
                    break;

                case PresentationEventType.AssetUpgraded:
                    salience = 2; kind = "asset"; text = e.Id + " is raised anew";
                    break;

                default: return;
            }

            if (!_keys.Add(key)) { DedupeHits++; return; }   // same witnessed fact twice = once in the book

            string vname = VillageName(e.VillageId);
            if (vname != null) text += " (" + vname + ")";

            int evId; var causes = CausesFor(kind, e, out evId);
            // the voice of the community that WITNESSED this — scoped to its village, not unioned
            // over the world (the engine dropped global knowledge in 2.1/D-086 for the same reason)
            var wNow = World();
            int tier = Emergence.Fas4.Fas4ProseDirector.VoiceTier(
                Emergence.Fas4.Fas4ProseDirector.KnownTechs(wNow != null ? wNow.LastState : null, e.VillageId));
            _entries.Add(new Entry { year = e.Year, era = e.Era, salience = salience, kind = kind, text = text, key = key, causes = causes, eventId = evId, voiceTier = tier });
            if (_entries.Count > Capacity)
            { _keys.Remove(_entries[0].key); _entries.RemoveAt(0); DroppedOldest++; }
        }

        // ---- FAS 4 PROSE WIRING: the engine's causes, matched to the beat the body just witnessed ----
        //
        // The body's bus events are DERIVED from applied state (a state diff), while the engine's
        // causes[] ride the engine's own event log. They are two accounts of the same moment, so the
        // wiring is a MATCH, not a lookup: the applied snapshot carries a bounded tail of the
        // engine's causes-bearing events (WorldState.events, see Fas3SimDriver.ExportJs), and each
        // chronicle kind names the engine event types that can stand behind it.
        //
        // Matching is on ACTOR + TYPE, not on year: the engine counts years 1-based and the body
        // 0-based, and the exported tail is by construction already only the recent past — adding a
        // year predicate would buy nothing and risk a silent off-by-one that drops every cause.
        // Newest match wins (the tail is oldest-first, so we scan backward). No match -> null, and
        // the why-line honestly says the chronicle records no cause.
        //
        // Pure read (D-078 r4): LastState is set by Fas3WorldRuntime.Apply BEFORE the event burst,
        // exactly as the NAME resolution above relies on. Nothing is written, no RNG is consumed.
        static readonly Dictionary<string, string[]> KindTypes = new Dictionary<string, string[]>
        {
            { "birth",   new[] { "child" } },
            { "death",   new[] { "death" } },
            { "feud",    new[] { "feud" } },
            { "raid",    new[] { "raid" } },
            { "steal",   new[] { "steal" } },
            { "mourn",   new[] { "mourn" } },
            { "gift",    new[] { "sharing", "giftway" } },
            { "leader",  new[] { "leader" } },
            { "giftway", new[] { "giftway" } },
        };

        public int CauseMatches { get; private set; }   // proof: beats that found their engine causes
        public int CauseMisses  { get; private set; }   // proof: beats with no engine event behind them

        string[] CausesFor(string kind, PresentationEvent e, out int eventId)
        {
            eventId = -1;
            string[] types;
            if (!KindTypes.TryGetValue(kind, out types)) { CauseMisses++; return null; }

            var w = World();
            var S = w != null ? w.LastState : null;
            if (S == null || S.events == null || S.events.Length == 0) { CauseMisses++; return null; }

            int agentId = AgentIdOf(e.Id);
            string village = VillageName(e.VillageId);

            for (int i = S.events.Length - 1; i >= 0; i--)
            {
                var ev = S.events[i];
                if (ev == null || ev.causes == null || ev.causes.Length == 0) continue;
                if (System.Array.IndexOf(types, ev.type) < 0) continue;
                if (agentId >= 0) { if (ev.agent != agentId) continue; }
                else if (!string.IsNullOrEmpty(village)) { if (ev.village != village) continue; }
                else continue;
                eventId = ev.id; CauseMatches++;
                return ev.causes;
            }
            CauseMisses++; return null;
        }

        static int AgentIdOf(string eventId)
        {
            int id;
            if (eventId != null && eventId.StartsWith("agent-") && int.TryParse(eventId.Substring(6), out id)) return id;
            return -1;
        }

        /// <summary>Backward scrub: drop everything later than the presentation year; firsts recompute.</summary>
        public void TrimTo(int year)
        {
            int before = _entries.Count;
            for (int i = _entries.Count - 1; i >= 0; i--)
                if (_entries[i].year > year) { _keys.Remove(_entries[i].key); _entries.RemoveAt(i); }
            if (_entries.Count != before)
            {
                TrimCount++;
                _firstChildSeen = _firstDeathSeen = false;
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].kind == "birth") _firstChildSeen = true;
                    else if (_entries[i].kind == "death") _firstDeathSeen = true;
                }
            }
        }

        /// <summary>READ-layer sanitizer (trailer round, slot 5 finding): removes every
        /// "(told-not-shown…)" mechanism parenthetical (D-106 §1 grammar — reconciler prefix AND
        /// codex-desc suffix variants) from reader-facing chronicle text, then collapses the
        /// whitespace the removal leaves. The BUS text is untouched — probes classify on it.</summary>
        public static string StripMechanismMarkers(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf("(told-not-shown", System.StringComparison.Ordinal) < 0) return s;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*\(told-not-shown[^)]*\)\s*", " ");
            return s.Trim();
        }

        // ---- name resolution: the sim's own names, read from the applied snapshot (never invented) ----
        string NameOf(string eventId, bool preferPrev)
        {
            int id;
            if (eventId == null || !eventId.StartsWith("agent-") || !int.TryParse(eventId.Substring(6), out id))
                return eventId ?? "a soul";
            var w = World();
            string n = null;
            if (w != null)
            {
                if (preferPrev) n = FindName(w.PrevState, id) ?? FindName(w.LastState, id);
                else n = FindName(w.LastState, id) ?? FindName(w.PrevState, id);
            }
            return string.IsNullOrEmpty(n) ? "soul " + id : n;
        }

        static string FindName(WorldState S, int id)
        {
            if (S == null || S.agents == null) return null;
            for (int i = 0; i < S.agents.Length; i++)
                if (S.agents[i] != null && S.agents[i].id == id) return S.agents[i].name;
            return null;
        }

        string VillageName(int vi)
        {
            if (vi < 0) return null;
            var w = World();
            var S = w != null ? w.LastState : null;
            if (S == null || S.villages == null || vi >= S.villages.Length || S.villages[vi] == null) return null;
            var n = S.villages[vi].name;
            return string.IsNullOrEmpty(n) ? null : n;
        }

        // ---- IMGUI v0 view (evidence-friendly, same school as Fas3TimeControls; native view comes later) ----
        void OnGUI()
        {
            // D-218: this overlay predates Fas4ChronicleView and now DUPLICATES it — two chronicles
            // on one screen, in two different visual languages, which is precisely the disease the
            // Screen Bible was written to cure. It stays as the FALLBACK for when the UI Toolkit view
            // could not build (a missing PanelSettings must cost the styling, never the record), and
            // is otherwise silent. Also available on demand behind the diagnostics gate.
            if (!showUI) return;
            var view = FindAnyObjectByType<Fas4ChronicleView>();
            if (view != null && !EmergenceUI.Diagnostics) return;
            var c = Clock();
            const int w = 360;
            int x = Screen.width - w - 12;
            int h = Mathf.Min(Screen.height - 24, 460);
            var r = new Rect(x, 12, w, h);
            GUI.Box(r, GUIContent.none);
            string head = "THE CHRONICLE" + (c != null ? "   yr " + c.PresentationYear : "") + "   (" + _entries.Count + " entries)";
            GUI.Label(new Rect(r.x + 10, r.y + 4, w - 20, 20), head);

            string[] labels = { "all", "notable", "turning points" };
            for (int i = 0; i < 3; i++)
            {
                bool active = minSalience == i + 1;
                GUI.backgroundColor = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
                if (GUI.Button(new Rect(r.x + 10 + i * 114, r.y + 26, 108, 20), labels[i])) minSalience = i + 1;
            }
            GUI.backgroundColor = Color.white;

            // newest at the bottom — the book reads downward; show the tail that fits
            int lineH = 18;
            int fit = Mathf.Max(1, (h - 60) / lineH);
            var shown = new List<Entry>();
            for (int i = _entries.Count - 1; i >= 0 && shown.Count < fit; i--)
                if (_entries[i].salience >= minSalience) shown.Add(_entries[i]);
            shown.Reverse();
            for (int i = 0; i < shown.Count; i++)
            {
                var e = shown[i];
                string mark = e.salience >= 3 ? "★ " : e.salience == 2 ? "• " : "· ";
                GUI.Label(new Rect(r.x + 10, r.y + 52 + i * lineH, w - 20, lineH),
                          "y" + e.year + "  " + mark + e.text);
            }
        }
    }
}
