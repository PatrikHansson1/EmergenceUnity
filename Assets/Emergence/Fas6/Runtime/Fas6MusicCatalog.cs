// EMERGENCE — FAS 6: the MUSIC CATALOG. A Resources-loadable list of the score's clips, so the
// director needs no scene wiring and the same component works in editor and player alike (the
// EmergenceAssetCatalog school, D-137). Built by Emergence/Fas6/BUILD MUSIC CATALOG.
//
// Roles, not filenames, are what the director asks for: an ERA wants an ambient bed, a village at
// each other's throats wants an action cue. The role split of the viking pack is a DECLARED
// judgement made by title, marked earCheck until a human has listened (STATE §7: "action/ambient-
// uppdelningen av viking-spåren är en öronkoll").
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas6MusicCatalog : ScriptableObject
    {
        public const string ResourcesName = "Fas6MusicCatalog";

        public enum Role { Ambient, Action }

        [Serializable]
        public struct Entry
        {
            public string key;      // the track's own name, e.g. "Echoes of Valhalla"
            public string pack;     // viking | medieval
            public Role role;       // ambient bed or action cue
            public bool earCheck;   // true = the MEASUREMENT and the TITLE disagree — a human ear settles it
            public float gainDb;    // per-track level match: what to add so every cue plays at the same loudness
            public AudioClip clip;
        }

        public List<Entry> entries = new List<Entry>();

        static Fas6MusicCatalog _loaded; static bool _tried;
        public static Fas6MusicCatalog Load()
        {
            if (_loaded != null || _tried) return _loaded;
            _tried = true;
            _loaded = Resources.Load<Fas6MusicCatalog>(ResourcesName);
            return _loaded;
        }
        public static void Invalidate() { _loaded = null; _tried = false; }

        public AudioClip Clip(string key)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].key == key && entries[i].clip != null) return entries[i].clip;
            return null;
        }

        /// <summary>Linear gain that brings this track to the catalog's shared loudness target. The
        /// two owned packs are mastered 14 dB apart (measured 2026-08-13: -19.1 dBFS for the loudest
        /// viking loop, -33.0 for the quietest medieval one). Without this, the score would JUMP
        /// every time drama pulled a viking cue in over a medieval bed — the most audible bug a
        /// state-driven score can have, and one no listener would call a bug: they would just say
        /// the music is broken. 1 when the track is unknown.</summary>
        public float Gain(string key)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].key == key) return Mathf.Pow(10f, entries[i].gainDb / 20f);
            return 1f;
        }

        /// <summary>Every clip with this role, in catalog order (a stable, deterministic pool).</summary>
        public List<Entry> Pool(Role role)
        {
            var o = new List<Entry>();
            for (int i = 0; i < entries.Count; i++) if (entries[i].role == role && entries[i].clip != null) o.Add(entries[i]);
            return o;
        }

        public int Count => entries.Count;
        public int EarCheckCount { get { int n = 0; foreach (var e in entries) if (e.earCheck) n++; return n; } }
    }
}
