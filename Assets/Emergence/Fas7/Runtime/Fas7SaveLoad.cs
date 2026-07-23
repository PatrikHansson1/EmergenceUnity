// EMERGENCE — FAS 7 increment 1: SAVE/LOAD A7 SHARP — determinism IS the save format.
//
// The checkpoint-grid grammar (D-137) said it from the start: a world is fully named by
// (seed, tick). A save therefore stores seed + the witnessed presentation year + the presentation
// MODE (paused, tps) + the SHA of the state export at that year — nothing else. Load = boot the
// same deterministic engine from the seed, let the producer RESIMULATE flat-out (writing fresh
// checkpoints), then re-enter the saved year through the SAME JumpToYear path scrub uses
// (ResetWorld + one Apply) and restore the mode. The proof is the golden-master law applied to
// save/load: the resimulated export's SHA at the saved year == the continuous run's SHA (which the
// save file carries as its anchor).
//
// v1 declared limits (honest bookkeeping, not gaps hidden):
//   - year-granular (the grid's grammar; sub-year = resimulate-from-checkpoint, R2-adjacent D-137);
//   - the chronicle starts EMPTY after load — the feed is witnessed history and the load's rebuild
//     burst is reconstruction (ApplyingJump keeps it silent, D-144); persisting the feed itself is
//     a later increment;
//   - the camera pose is not saved (presentation garnish, not world state).
// D-078 r4: reads state and files the driver wrote; writes nothing into the sim.
using System;
using System.IO;
using UnityEngine;

namespace Emergence.Runtime
{
    [Serializable]
    public class Fas7SaveData
    {
        public int version = 1;
        public long seed;
        public int year;                 // the witnessed presentation year (LastAppliedYear)
        public bool paused;              // presentation mode at save
        public float ticksPerSecond;
        public string stateSha;          // sha256 of the state export at `year` — the save's own proof anchor
        public string savedAt;           // wall clock, traceability only
    }

    public static class Fas7SaveLoad
    {
        public static string SaveDir => Path.Combine(Application.persistentDataPath, "Emergence", "saves");
        public static string PathFor(long seed) => Path.Combine(SaveDir, $"save-{seed}.json");
        public static string CheckpointPath(Fas3SimDriver d, int year) => Path.Combine(d.CheckpointDir, $"seq-{d.seed}-y{year:000}.json");

        /// <summary>Write the save file for the currently witnessed year. Returns null + error on failure.</summary>
        public static Fas7SaveData Save(Fas3SimDriver d, Fas3PresentationClock c, Fas3WorldRuntime w, out string error)
        {
            error = "";
            if (d == null || c == null || w == null) { error = "missing driver/clock/world"; return null; }
            if (w.LastAppliedYear < 0) { error = "nothing witnessed yet"; return null; }
            if (string.IsNullOrEmpty(d.CheckpointDir)) { error = "no checkpoint grid (driver not in bufferMode)"; return null; }
            string chk = CheckpointPath(d, w.LastAppliedYear);
            if (!File.Exists(chk)) { error = "no checkpoint for witnessed year: " + chk; return null; }
            try
            {
                var data = new Fas7SaveData
                {
                    seed = d.seed,
                    year = w.LastAppliedYear,
                    paused = c.paused,
                    ticksPerSecond = c.ticksPerSecond,
                    stateSha = EmergenceJintHost.Sha256Hex(File.ReadAllText(chk)),
                    savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                };
                Directory.CreateDirectory(SaveDir);
                string tmp = PathFor(d.seed) + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(PathFor(d.seed))) File.Delete(PathFor(d.seed));
                File.Move(tmp, PathFor(d.seed));   // atomic-enough on the same volume
                return data;
            }
            catch (Exception e) { error = "save: " + e.Message; return null; }
        }

        public static Fas7SaveData Read(string path, out string error)
        {
            error = "";
            try
            {
                var data = JsonUtility.FromJson<Fas7SaveData>(File.ReadAllText(path));
                if (data == null || data.version != 1 || data.seed == 0 || data.year < 0 || string.IsNullOrEmpty(data.stateSha))
                { error = "malformed save (version/seed/year/sha)"; return null; }
                return data;
            }
            catch (Exception e) { error = "read: " + e.Message; return null; }
        }
    }

    /// <summary>The RESTORER: attach beside a fresh Fas3Onboarding booted with startPaused=true and
    /// the SAME seed as the save. Waits for the producer to resimulate past the saved year (fresh
    /// checkpoints), stamps the resimulated export's SHA, re-enters the saved year via JumpToYear
    /// (the scrub path — ResetWorld + one Apply, reconstruction silenced), then restores the saved
    /// presentation mode. Poll Done/Ok/Note. Presentation-side only.</summary>
    public sealed class Fas7LoadBoot : MonoBehaviour
    {
        public string savePath = "";
        public float watchdogSecs = 180f;

        public bool Done { get; private set; }
        public bool Ok { get; private set; }
        public string LoadedSha { get; private set; } = "";
        public string Note { get; private set; } = "booting";

        Fas7SaveData _save;
        Fas3Onboarding _onb;
        float _t0 = -1f;
        int _settleFrames;

        void Update()
        {
            if (Done) return;
            if (_t0 < 0f) _t0 = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - _t0 > watchdogSecs) { Finish(false, "WATCHDOG " + Note); return; }

            if (_save == null)
            {
                _save = Fas7SaveLoad.Read(savePath, out var err);
                if (_save == null) { Finish(false, "save unreadable: " + err); return; }
                Note = $"save read (seed {_save.seed}, y{_save.year})";
                return;
            }
            if (_onb == null)
            {
                _onb = FindAnyObjectByType<Fas3Onboarding>();
                if (_onb == null) { Note = "waiting for onboarding"; return; }
                return;
            }
            var d = _onb.Driver; var c = _onb.Clock; var w = _onb.World;
            if (d == null || c == null || w == null) { Note = "waiting for composition"; return; }
            if (d.LastError.Length > 0) { Finish(false, "driver: " + d.LastError); return; }
            if (d.seed != _save.seed) { Finish(false, $"seed mismatch: driver {d.seed} != save {_save.seed}"); return; }
            if (!c.paused)   // nothing may be witnessed during restore — replay is not new history
            { Finish(false, "clock not paused during restore — boot the onboarding with startPaused=true"); return; }

            // 1. the producer resimulates flat-out; the saved year's FRESH checkpoint is the load's truth
            string chk = Fas7SaveLoad.CheckpointPath(d, _save.year);
            if (d.Year < _save.year || !File.Exists(chk))
            { Note = $"resimulating {d.Year}/{_save.year}"; return; }
            if (++_settleFrames < 3) return;   // the worker stamps Year before the file write settles — give it frames

            try
            {
                LoadedSha = EmergenceJintHost.Sha256Hex(File.ReadAllText(chk));   // stamped at measurement (R1)
            }
            catch (Exception e) { Finish(false, "sha read: " + e.Message); return; }

            // 2. re-enter the saved year through the scrub path (reconstruction, silenced by ApplyingJump)
            if (!c.JumpToYear(_save.year)) { Finish(false, "jump: " + c.LastError); return; }
            bool applied = w.LastAppliedYear == _save.year;

            // 3. restore the saved presentation mode — the player's hand comes back exactly as it left
            c.ticksPerSecond = _save.ticksPerSecond;
            c.paused = _save.paused;

            bool shaOk = LoadedSha == _save.stateSha;
            Finish(applied && shaOk,
                $"loaded y{w.LastAppliedYear} (applied={applied}), resimSha{(shaOk ? "==" : "!=")}saveSha ({LoadedSha.Substring(0, Math.Min(12, LoadedSha.Length))})");
        }

        void Finish(bool ok, string note) { Ok = ok; Note = note; Done = true; }
    }
}
