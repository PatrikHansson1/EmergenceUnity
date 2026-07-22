// EMERGENCE — FAS 5 v0 (per FAS5-KICKOFF-BRIEF 2026-07-22): the METRICS RECORDER — ANALYZE's
// first organ. Consumer #4 on the Fas 0 PresentationEventBus (gaze D-134, ear D-141, chronicle
// D-144 came before): it subscribes, never writes back, and turns the deterministic bus stream +
// applied world state into bounded yearly series — population, births, deaths, huts, era.
//
// DETERMINISM (D-078 r4): records derive ONLY from the bus stream + applied state, both
// deterministic. No RNG of any kind — same run, same series. The sim is never read outside its
// own export, never written.
// SCRUB HONESTY (D-137/D-144 law): a backward jump TRIMS the series to the presentation year
// (derived totals recompute by construction — they are sums over the surviving records); the
// JumpToYear rebuild burst is reconstruction, not witnessed history (clock.ApplyingJump guards).
//
// v0 deliberately does NOT do: Gini/tech-loss/trade/faith (the engine's metrics export, R2 order),
// correlation/drill-down views (need causes[] + metrics), cross-seed comparison (Fas 7 surface).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas5MetricsRecorder : MonoBehaviour
    {
        [Serializable]
        public struct YearRecord
        {
            public int year;
            public int era;          // WorldState.era at the moment the year applied (D-147)
            public int pop;          // agents alive in the applied snapshot
            public int births;       // witnessed this year (bus AgentActivity)
            public int deaths;       // witnessed this year
            public int hutsDelta;    // spawned - removed this year (bus Asset*)
            public bool sampled;     // pop/era filled from an applied snapshot (bus deltas alone otherwise)
        }

        public const int Capacity = 512;   // bounded like every witness — series never grow without limit

        readonly SortedDictionary<int, YearRecord> _years = new SortedDictionary<int, YearRecord>();

        public int TrimCount { get; private set; }
        public int SuppressedDuringJump { get; private set; }
        public int DroppedOldest { get; private set; }

        Fas3PresentationClock _clock;
        Fas3WorldRuntime _world;
        double _lastPresTick;
        int _lastSampledYear = int.MinValue;

        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }
        Fas3WorldRuntime World() { if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>(); return _world; }

        void OnEnable() { PresentationEventBus.OnEvent += OnBus; }
        void OnDisable() { PresentationEventBus.OnEvent -= OnBus; }

        // ---------------- read surface (view + probe) ----------------

        public int RecordCount => _years.Count;

        public YearRecord[] Series()
        {
            var a = new YearRecord[_years.Count];
            _years.Values.CopyTo(a, 0);
            return a;
        }

        public bool TryGet(int year, out YearRecord r) => _years.TryGetValue(year, out r);

        public int TotalBirths { get { int n = 0; foreach (var r in _years.Values) n += r.births; return n; } }
        public int TotalDeaths { get { int n = 0; foreach (var r in _years.Values) n += r.deaths; return n; } }
        public int HutCount    { get { int n = 0; foreach (var r in _years.Values) n += r.hutsDelta; return n; } }
        public int LatestYear  { get { int y = -1; foreach (var k in _years.Keys) y = k; return y; } }

        public YearRecord Latest()
        {
            YearRecord last = default;
            foreach (var r in _years.Values) last = r;
            return last;
        }

        // ---------------- witnessing ----------------

        void Update()
        {
            var c = Clock(); if (c == null) return;

            // backward jump: the series keeps only what this timeline has lived (same law as the feed)
            if (c.PresentationTick < _lastPresTick - 0.5) TrimTo(c.PresentationYear);
            _lastPresTick = c.PresentationTick;

            // sample pop/era when a new year stands applied (a pure read of the presented snapshot)
            var w = World();
            var S = w != null ? w.LastState : null;
            int y = w != null ? w.LastAppliedYear : -1;
            if (S != null && y >= 0 && y != _lastSampledYear && !c.ApplyingJump)
            {
                _lastSampledYear = y;
                var r = GetOrMake(y);
                r.pop = S.agents != null ? S.agents.Length : 0;
                r.era = S.era;
                r.sampled = true;
                _years[y] = r;
                Bound();
            }
        }

        void OnBus(PresentationEvent e)
        {
            var c = Clock();
            if (c != null && c.ApplyingJump) { SuppressedDuringJump++; return; }   // reconstruction, not history

            switch (e.Type)
            {
                case PresentationEventType.AgentActivity:
                    if (e.Data == "a child is born") { var r = GetOrMake(e.Year); r.births++; _years[e.Year] = r; }
                    else if (e.Data == "a soul departs") { var r = GetOrMake(e.Year); r.deaths++; _years[e.Year] = r; }
                    break;

                case PresentationEventType.AssetSpawned:
                    if (e.Id != null && e.Id.StartsWith("hut:")) { var r = GetOrMake(e.Year); r.hutsDelta++; _years[e.Year] = r; }
                    break;

                case PresentationEventType.AssetRemoved:
                    if (e.Id != null && e.Id.StartsWith("hut:")) { var r = GetOrMake(e.Year); r.hutsDelta--; _years[e.Year] = r; }
                    break;
            }
            Bound();
        }

        YearRecord GetOrMake(int year)
        {
            YearRecord r;
            if (!_years.TryGetValue(year, out r)) r = new YearRecord { year = year };
            return r;
        }

        void Bound()
        {
            while (_years.Count > Capacity)
            {
                int oldest = int.MaxValue;
                foreach (var k in _years.Keys) { oldest = k; break; }
                _years.Remove(oldest);
                DroppedOldest++;
            }
        }

        /// <summary>Backward scrub: drop every record later than the presentation year.</summary>
        public void TrimTo(int year)
        {
            var drop = new List<int>();
            foreach (var k in _years.Keys) if (k > year) drop.Add(k);
            if (drop.Count == 0) return;
            foreach (var k in drop) _years.Remove(k);
            TrimCount++;
            if (_lastSampledYear > year) _lastSampledYear = int.MinValue;   // re-sample honestly on re-witness
        }
    }
}
