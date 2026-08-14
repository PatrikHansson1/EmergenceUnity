// EMERGENCE — FAS 6: the MUSIC DIRECTOR (score layer). The world gets a score that is a pure
// function of the state it is scoring.
//
// Until now the ear was procedural: era beds (D-156), an activity bed and fire crackle (D-157),
// milestone chimes (D-141) — colour, deliberately under everything. This is the layer above:
// written music, from the two owned packs, chosen by WHAT THE WORLD IS DOING.
//
// THE LAW (one function, no hidden state):
//   CueFor(S) = winter        -> the frostbound bed      (the season overrides the era: cold is
//                                what the world IS that year, whatever it knows)
//               drama in S    -> an ACTION cue from the viking pool, picked by hash(village|year)
//               otherwise     -> the era's ambient bed, from a declared era table
// Everything is derived from the APPLIED snapshot. No sim RNG is read (all variation is hash-based,
// D-078 r4), nothing is written back, and the golden master cannot see this class. Crossfades and
// the anti-flicker hold are PRESENTATION smoothing over that law — the law itself is pure and is
// what the probe asserts.
//
// PAUSE HONESTY (the D-141/D-157 school): a paused world keeps its cue sounding — the music is the
// world's colour, not its clock — but it never CHANGES cue while paused, because nothing happened.
//
// EAR CHECK (Patrik): which viking tracks are action and which are ambient was judged by TITLE.
// The split lives in the catalog (Fas6MusicCatalog.Entry.earCheck) and is one listening session
// away from being settled. The era table below is likewise the director's opinion, not canon.
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas6MusicDirector : MonoBehaviour
    {
        [Tooltip("Score volume — sits ABOVE the procedural beds but under the world's own sounds.")]
        public float volume = 0.32f;
        public float crossfadeSecs = 4f;
        [Tooltip("Seconds an action cue is held after the drama leaves the state, so a one-year raid does not flicker the score.")]
        public float actionHoldSecs = 20f;
        public bool showUI = false;

        /// <summary>The era table: which ambient track scores which era. A declared judgement (see
        /// header) — swap freely, it changes no law. Index = WorldEras interim era index 0..6.</summary>
        public static readonly string[] EraTrack =
        {
            "Elven Dawn",        // dawn   — the first morning, nothing built yet
            "Darkwood Path",     // stone  — the world is forest and the people are in it
            "Emberlight",        // bronze — fire has become craft
            "Winds of Valor",    // iron   — iron, and what people do with it
            "Silverbrook",       // mill   — water put to work
            "Moonspire",         // print  — the world starts keeping its own memory
            "Throne of Storms",  // steam  — the loudest age this engine reaches
        };
        /// <summary>The season override: winter is what the world IS, whatever it knows.</summary>
        public const string WinterTrack = "Frostbound";

        public string CurrentCue { get; private set; } = "";
        public int CueChanges { get; private set; }
        public bool Playing => Active != null && Active.isPlaying;
        public bool CatalogReady => _cat != null && _cat.Count > 0;
        public string LastNote { get; private set; } = "";

        Fas6MusicCatalog _cat;
        AudioSource _a, _b; bool _aActive; float _fadeT = 1f;
        float _actionUntil = -999f;
        float _gain = 1f, _gainPrev = 1f;   // per-track level match, see Fas6MusicCatalog.Gain
        Fas3WorldRuntime _world; Fas3PresentationClock _clock;
        AudioSource Active => _aActive ? _a : _b;

        Fas3WorldRuntime World() { if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>(); return _world; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }

        void Awake()
        {
            _cat = Fas6MusicCatalog.Load();
            if (_cat == null || _cat.Count == 0)
            {
                LastNote = "no music catalog — run Emergence/Fas6/BUILD MUSIC CATALOG; the procedural beds stand alone";
                enabled = false;   // DISARM: the ear never goes silent, it just goes back to colour
                return;
            }
            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b }) { s.loop = true; s.playOnAwake = false; s.volume = 0f; s.spatialBlend = 0f; }
            LastNote = "catalog: " + _cat.Count + " tracks (" + _cat.EarCheckCount + " awaiting the ear)";
        }

        // ---------------- THE LAW ----------------

        /// <summary>Is this state a state of drama? Pure read of the applied snapshot: an act in the
        /// air (a soul mid-feud/raid/steal) or the engine's own act events in this year's tail.</summary>
        public static bool IsDrama(WorldState S)
        {
            if (S == null) return false;
            if (S.agents != null)
                foreach (var a in S.agents)
                    if (a != null && (a.sayAct == "feud" || a.sayAct == "raid" || a.sayAct == "steal")) return true;
            if (S.events != null)
                foreach (var e in S.events)
                    if (e != null && (e.type == "feud" || e.type == "raid")) return true;
            return false;
        }

        /// <summary>Where the drama is — the name that makes the action pick deterministic and
        /// world-specific. "" when nothing is named; the year still varies the pick.</summary>
        public static string DramaKey(WorldState S)
        {
            if (S == null) return "";
            if (S.events != null)
                foreach (var e in S.events)
                    if (e != null && (e.type == "feud" || e.type == "raid") && !string.IsNullOrEmpty(e.village)) return e.village;
            if (S.villages != null && S.villages.Length > 0 && S.villages[0] != null) return S.villages[0].name ?? "";
            return "";
        }

        /// <summary>Stable non-negative hash — presentation variation is ALWAYS hash-based (D-078 r4).</summary>
        public static int Hash(string s)
        {
            unchecked { int h = 17; foreach (char c in s ?? "") h = h * 31 + c; return h & 0x7fffffff; }
        }

        /// <summary>THE CUE LAW. A pure function of the applied state and the action pool — the same
        /// world always sounds the same way. actionPool is passed in so the law is testable without
        /// a catalog, and so the probe asserts the LAW rather than the wiring.</summary>
        public static string CueFor(WorldState S, IList<string> actionPool, bool actionHeld)
        {
            if (S == null) return EraTrack[0];
            if ((IsDrama(S) || actionHeld) && actionPool != null && actionPool.Count > 0)
                return actionPool[Hash(DramaKey(S) + "|" + S.years) % actionPool.Count];
            if (S.season == "winter") return WinterTrack;
            int era = Mathf.Clamp(S.era, 0, EraTrack.Length - 1);
            return EraTrack[era];
        }

        // ---------------- the wiring around the law ----------------

        void Update()
        {
            if (_cat == null) return;
            var w = World(); var S = w != null ? w.LastState : null;
            var c = Clock();

            // pause honesty: a paused world keeps sounding but never CHANGES cue — nothing happened
            bool paused = c != null && c.paused;
            if (S != null && !paused)
            {
                if (IsDrama(S)) _actionUntil = Time.time + actionHoldSecs;
                string cue = CueFor(S, PoolKeys(), Time.time < _actionUntil);
                if (cue != CurrentCue) SwitchTo(cue);
            }

            // crossfade (presentation smoothing over the pure law)
            if (_fadeT < 1f)
            {
                _fadeT = Mathf.Min(1f, _fadeT + Time.unscaledDeltaTime / Mathf.Max(0.05f, crossfadeSecs));
                var to = Active; var from = _aActive ? _b : _a;
                if (to != null) to.volume = volume * _gain * _fadeT;
                if (from != null) { from.volume = volume * _gainPrev * (1f - _fadeT); if (_fadeT >= 1f) from.Stop(); }
            }
        }

        List<string> _poolKeys;
        List<string> PoolKeys()
        {
            if (_poolKeys != null) return _poolKeys;
            _poolKeys = new List<string>();
            if (_cat != null) foreach (var e in _cat.Pool(Fas6MusicCatalog.Role.Action)) _poolKeys.Add(e.key);
            return _poolKeys;
        }

        void SwitchTo(string cue)
        {
            var clip = _cat.Clip(cue);
            if (clip == null) { LastNote = "cue '" + cue + "' has no clip in the catalog — holding"; return; }
            _gainPrev = _gain;
            _aActive = !_aActive;
            var to = Active;
            _gain = _cat.Gain(cue);   // level-match the packs (see Fas6MusicCatalog.Gain)
            to.clip = clip; to.volume = CurrentCue.Length == 0 ? volume * _gain : 0f; to.Play();
            _fadeT = CurrentCue.Length == 0 ? 1f : 0f;    // the opening snaps; every later change fades
            CurrentCue = cue; CueChanges++;
        }

        /// <summary>Probe seam: apply the law to a state without a scene or a clock.</summary>
        public string CueForNow(WorldState S) => CueFor(S, PoolKeys(), false);

        void OnGUI()
        {
            // D-218: which cue is playing is a fact about the MACHINE, not about the world. It
            // belongs to a developer, not to a screenshot. Behind the diagnostics gate.
            if (!showUI || !EmergenceUI.Diagnostics) return;
            EmergenceUI.Begin();
            GUI.Label(new Rect(EmergenceUI.Sp6, EmergenceUI.H - 28, 700, 20),
                      "score: " + (CurrentCue.Length == 0 ? "—" : CurrentCue) + "   (" + CueChanges + " changes)",
                      EmergenceUI.Meta);
            EmergenceUI.End();
        }
    }
}
