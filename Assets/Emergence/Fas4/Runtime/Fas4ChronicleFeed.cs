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
            public string kind;     // arrival | birth | death | milestone | asset
            public string text;
            public string key;      // dedupe identity: year|type|id|data
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

            string key = e.Year + "|" + e.Type + "|" + e.Id + "|" + e.Data;
            int salience; string kind, text;

            switch (e.Type)
            {
                case PresentationEventType.Milestone:
                    // the canon beat — Codex chronicleEvent text (e.desc) or a reconciler's own line, verbatim
                    salience = 3; kind = "milestone"; text = e.Data;
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
                    else return;   // task/age changes are the body's business, not the chronicle's (v0)
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

            _entries.Add(new Entry { year = e.Year, era = e.Era, salience = salience, kind = kind, text = text, key = key });
            if (_entries.Count > Capacity)
            { _keys.Remove(_entries[0].key); _entries.RemoveAt(0); DroppedOldest++; }
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
            if (!showUI) return;
            var c = Clock();
            const int w = 360;
            int x = Screen.width - w - 12;
            int h = Mathf.Min(Screen.height - 24, 460);
            var r = new Rect(x, 12, w, h);
            GUI.Box(r, GUIContent.none);
            string head = "KRÖNIKAN" + (c != null ? "   år " + c.PresentationYear : "") + "   (" + _entries.Count + " poster)";
            GUI.Label(new Rect(r.x + 10, r.y + 4, w - 20, 20), head);

            string[] labels = { "allt", "märkbart", "vändpunkter" };
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
