// EMERGENCE — FAS 3 increment 4 (D-136/D-137): the PRESENTATION CLOCK — the player's hand on time,
// re-seated on the lookahead buffer.
//
// D-136 finding: player-Jint flat-out = 19 t/s < 24 t/s (1×). So the ENGINE can't be paced by the
// player's buttons — the BUFFER is. The driver (bufferMode) races flat-out and queues year
// snapshots; this clock advances a presentation tick at 1×/4× real pace, clamped to the producer's
// tick (presentation can NEVER outrun the sim's truth), and applies queued years IN ORDER through
// Fas3WorldRuntime the moment the presentation tick crosses each year boundary.
//
// TIME LAW (D-133) restated for the buffer era: pause/speed touch ONLY this clock — the producer
// keeps racing (that is the point: pausing WIDENS the lookahead). Scrub = JumpToYear: any produced
// year re-enters from its persisted checkpoint (ResetWorld + one Apply) — no resimulation needed at
// year granularity; sub-year scrub = resimulate-from-checkpoint, an engine-lane extension (R2-adjacent).
// D-078 r4: reads snapshots and files the driver wrote; writes nothing into the sim.
using System;
using System.IO;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3PresentationClock : MonoBehaviour
    {
        public Fas3SimDriver driver;        // auto-found if left empty
        public Fas3WorldRuntime world;      // auto-found if left empty
        public bool paused;
        [Tooltip("Presentation ticks consumed per real second: 24 = 1×, 96 = 4×.")]
        public float ticksPerSecond = 24f;

        public double PresentationTick { get; private set; }

        // FAS 4 (ChronicleFeed): TRUE while a JumpToYear is synchronously rebuilding the world from a
        // checkpoint. The reconcilers re-publish spawn/arrival events for everything they re-materialise —
        // that burst is RECONSTRUCTION, not newly witnessed history; consumers that keep a chronicle
        // check this flag and stay silent through it. Presentation-side truth only — D-078 r4.
        public bool ApplyingJump { get; private set; }
        public int PresentationYear => world != null && world.LastAppliedYear >= 0 ? world.LastAppliedYear : 0;
        public int BufferedYearsAhead => Driver() != null ? Driver().BufferedYears : 0;
        public string LastAppliedOrder => _order;     // "1,2,3,…" — the probe's in-order proof
        public string LastError { get; private set; } = "";

        string _order = "";

        Fas3SimDriver Driver() { if (driver == null) driver = FindAnyObjectByType<Fas3SimDriver>(); return driver; }
        Fas3WorldRuntime World() { if (world == null) world = FindAnyObjectByType<Fas3WorldRuntime>(); return world; }

        void Update()
        {
            var d = Driver(); var w = World();
            if (d == null || w == null) return;
            if (!paused) PresentationTick += ticksPerSecond * Time.unscaledDeltaTime;
            if (PresentationTick > d.Tick) PresentationTick = d.Tick;   // truth is a hard wall

            // apply every year whose boundary the presentation tick has crossed — strictly in order.
            // The queue is the fast path; the checkpoint grid is the truth a scrub can always re-enter
            // (post-jump, stale queue entries are discarded — every produced year is on disk anyway).
            int yearTicks = Mathf.Max(1, d.YearTicks);
            while (true)
            {
                // Fas 7 (save/load): pause applies NOTHING — not even genesis, whose boundary (0) is
                // always <= a frozen PresentationTick. A paused boot (startPaused) witnesses no history
                // until a restorer releases time; the opening (D-139) boots unpaused and is untouched.
                if (paused) break;
                // first apply is GENESIS (year 0) when the driver queued it (bufferMode) — from then on, +1 per year
                int nextYear = w.LastAppliedYear < 0 ? 0 : w.LastAppliedYear + 1;
                if ((double)nextYear * yearTicks > PresentationTick) break;   // its boundary isn't reached yet
                WorldState S = null;
                var qj = d.TakeYearSnapshot();
                if (qj != null) { S = JsonUtility.FromJson<WorldState>(qj); if (S != null && S.years != nextYear) S = null; }
                if (S == null) { var fj = LoadCheckpoint(d, nextYear); if (fj != null) S = JsonUtility.FromJson<WorldState>(fj); }
                if (S == null) break;
                w.Apply(S);
                _order += (_order.Length > 0 ? "," : "") + w.LastAppliedYear;
            }
        }

        static string LoadCheckpoint(Fas3SimDriver d, int year)
        {
            if (string.IsNullOrEmpty(d.CheckpointDir) || year > d.Year) return null;
            string path = Path.Combine(d.CheckpointDir, $"seq-{d.seed}-y{year:000}.json");
            try { return File.Exists(path) ? File.ReadAllText(path) : null; } catch { return null; }
        }

        /// <summary>Scrub to a produced year via its persisted checkpoint. Returns false if the file isn't there.</summary>
        public bool JumpToYear(int year)
        {
            var d = Driver(); var w = World();
            if (d == null || w == null || string.IsNullOrEmpty(d.CheckpointDir)) { LastError = "no driver/world/checkpointDir"; return false; }
            string path = Path.Combine(d.CheckpointDir, $"seq-{d.seed}-y{year:000}.json");
            if (!File.Exists(path)) { LastError = "no checkpoint: " + path; return false; }
            try
            {
                ApplyingJump = true;
                var json = File.ReadAllText(path);
                while (d.TakeYearSnapshot() != null) { }                // drop queued years — the jump owns time now
                w.ResetWorld();
                w.Apply(json);
                PresentationTick = (double)year * Mathf.Max(1, d.YearTicks);
                _order += (_order.Length > 0 ? "," : "") + $"J{year}";
                return true;
            }
            catch (Exception e) { LastError = "jump: " + e.Message; return false; }
            finally { ApplyingJump = false; }
        }
    }
}
