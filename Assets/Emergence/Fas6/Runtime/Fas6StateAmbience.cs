// EMERGENCE — FAS 6 increment 2: the STATE EAR — ambience intensity + point sources from state.
//
// The Fas 6 gate demands "ljud skiftar korrekt med era/TILLSTÅND, deterministiskt". Increment 1
// gave the eras their beds; this is the purchase-free STATE layer: (a) an ACTIVITY BED — a soft
// village-life murmur whose intensity is a PURE FUNCTION of the applied state (fires and huts;
// season-tinted, the export carries S.season) — genesis wilderness is silent, a living village
// hums; (b) FIRE POINT SOURCES — spatialized crackle loops materialized at the applied state's
// fire positions (bounded), so walking the camera past a hearth is an audible event.
//
// Laws (same as increment 1, D-156): reads the APPLIED state only (the Almanac's law — no bus
// race); all synthesis from FIXED seeds, all per-fire variation hash(x,y)-based — never sim RNG
// (D-078 r4); the golden master cannot see this class. Pause honesty (D-141 school): beds keep
// sounding under pause, but intensity and the point set CANNOT move while paused because the
// applied state is frozen — asserted by the probe over real frames, not assumed. Never touches
// tps/pause. Position mapping = the reconcilers' P() + terrain grounding (HutReconciler school).
//
// Replace-path (same clause as Fas3AudioDirector/Fas6EraAmbience): when the real audio purchase
// lands, the state tracking + reconcile stay; only the synthesized sources swap.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas6StateAmbience : MonoBehaviour
    {
        public const int MaxPoints = 8;              // bounded — a village, not an orchestra
        public float activityVolume = 0.12f;         // ceiling; actual = ceiling * smoothed intensity
        public float pointVolume = 0.16f;
        public float smoothSecs = 3.0f;              // presentation-side ease toward the state's truth

        public float IntensityTarget { get; private set; }         // pure function of applied state
        public float IntensitySmoothed { get; private set; }
        public int PointSourceCount { get; private set; }
        public Vector3 FirstPointPos { get; private set; }
        public bool ActivityBedPlaying => _bed != null && _bed.isPlaying;

        AudioSource _bed;
        AudioClip _activityClip, _crackleClip;
        WorldState _seen;                            // reference of the last reconciled applied state
        readonly System.Collections.Generic.List<GameObject> _points = new();
        Fas3WorldRuntime _world;

        const float TileSize = 8f;                   // matches WorldDresser/reconcilers

        void Awake()
        {
            const int sr = 22050;                    // low textures — half rate keeps them cheap
            _activityClip = AudioClip.Create("emg_state_activity", (int)(sr * 6f), 1, sr, false);
            _activityClip.SetData(ActivityBedSamples(sr, 6f), 0);
            _crackleClip = AudioClip.Create("emg_state_crackle", (int)(sr * 4f), 1, sr, false);
            _crackleClip.SetData(CrackleSamples(sr, 4f), 0);

            _bed = gameObject.AddComponent<AudioSource>();
            _bed.clip = _activityClip; _bed.loop = true; _bed.spatialBlend = 0f; _bed.volume = 0f;
            _bed.Play();                             // sounding from frame one; intensity gates the volume
        }

        void Update()
        {
            if (_world == null) { _world = FindAnyObjectByType<Fas3WorldRuntime>(); if (_world == null) return; }
            var S = _world.LastState; if (S == null) return;

            if (!ReferenceEquals(S, _seen))          // a new state was APPLIED — reconcile the ear
            {
                IntensityTarget = IntensityFor(S);
                ReconcilePoints(S);
                _seen = S;
            }

            // presentation-side ease; unscaled so pause never stalls a blend mid-seam (the TARGET
            // is frozen under pause because the applied state is — that is the honesty, not this)
            IntensitySmoothed = Mathf.MoveTowards(IntensitySmoothed, IntensityTarget,
                Time.unscaledDeltaTime / Mathf.Max(0.1f, smoothSecs));
            if (_bed != null) _bed.volume = activityVolume * IntensitySmoothed;
        }

        /// <summary>PURE intensity law — deterministic function of the applied state, nothing else.
        /// Fires carry the village voice (0.30 each), huts the settled floor (0.12 each); season
        /// tints the whole (winter hushes). Genesis wilderness (0/0) = 0. The probe calls this on
        /// synthetic states AND asserts the live component agrees exactly.</summary>
        public static float IntensityFor(WorldState S)
        {
            if (S == null) return 0f;
            int fires = S.fires != null ? S.fires.Length : 0;
            int huts = S.huts != null ? S.huts.Length : 0;
            return Mathf.Clamp01(fires * 0.30f + huts * 0.12f) * SeasonGain(S.season);
        }

        /// <summary>PURE season tint — winter hushes the village to 75 %. (The Almanac's era-tile
        /// law — never show season as era — is about LABELS; the brief explicitly sanctions season
        /// as an ambience color: "vinter/säsong om exporten bär den".)</summary>
        public static float SeasonGain(string season) => season == "winter" ? 0.75f : 1f;

        void ReconcilePoints(WorldState S)
        {
            int want = S.fires != null ? Mathf.Min(S.fires.Length, MaxPoints) : 0;

            // cheap deterministic reconcile: state order, bounded — rebuild only when the set changed
            bool changed = _points.Count != want;
            if (!changed)
                for (int i = 0; i < want; i++)
                {
                    var expect = GroundW(P(S, S.fires[i].x, S.fires[i].y));
                    if ((_points[i].transform.position - expect).sqrMagnitude > 0.01f) { changed = true; break; }
                }
            if (!changed) { PointSourceCount = _points.Count; return; }

            foreach (var go in _points) if (go != null) Destroy(go);
            _points.Clear();
            for (int i = 0; i < want; i++)
            {
                var f = S.fires[i];
                int hx = Mathf.RoundToInt(f.x), hy = Mathf.RoundToInt(f.y);
                var go = new GameObject($"FirePoint_{hx}_{hy}");
                go.transform.SetParent(transform, false);
                go.transform.position = GroundW(P(S, f.x, f.y));
                var src = go.AddComponent<AudioSource>();
                src.clip = _crackleClip; src.loop = true; src.dopplerLevel = 0f;
                src.spatialBlend = 1f; src.rolloffMode = AudioRolloffMode.Linear;
                src.minDistance = 6f; src.maxDistance = 45f;
                src.volume = pointVolume;
                src.pitch = 0.9f + 0.2f * Hash01(hx, hy, 61);          // hash, never sim RNG
                src.time = Hash01(hx, hy, 62) * (_crackleClip.length - 0.05f);  // decorrelate the loops
                src.Play();
                _points.Add(go);
            }
            PointSourceCount = _points.Count;
            FirstPointPos = _points.Count > 0 ? _points[0].transform.position : Vector3.zero;
        }

        /// <summary>Pure activity-bed synthesis — fixed seed, a low murmur with a slow work-wobble
        /// and sparse soft knocks (the village heard from the tree line). The probe hashes this
        /// twice to prove determinism; Awake calls it for the real bed.</summary>
        public static float[] ActivityBedSamples(int sr, float secs)
        {
            int n = (int)(sr * secs);
            var data = new float[n];
            var rng = new System.Random(53000);      // fixed seed — same bed every run IN THIS RUNTIME (G-review r1 I3:
                                                     // System.Random sequence is implementation-defined across runtimes;
                                                     // replace-path: hash-PRNG or bought layers A4. Never touches sim.)
            // D-237: same fault as the other two beds — one pole is not a filter, it is a tilt, and
            // white noise tilted once is still hiss. Three noise beds each doing this, stacked under
            // real music, is what Patrik heard. The slow wobble stays: that is the breathing.
            float v = 0f, l1 = 0f, l2 = 0f, peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                v += (float)(rng.NextDouble() - 0.5) * 0.04f;
                v *= 0.999f;
                l1 += (v - l1) * 0.026f;
                l2 += (l1 - l2) * 0.026f;
                float wobble = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.3f * i / sr);
                data[i] = l2 * wobble;
                float m = data[i] < 0f ? -data[i] : data[i];
                if (m > peak) peak = m;
            }
            float norm = 0.7f / peak;
            for (int i = 0; i < n; i++) data[i] *= norm;
            int stride = (int)(sr * 1.1f);           // sparse knocks — wood on wood, far away
            for (int k = stride / 3; k < n; k += stride)
            {
                int len = Mathf.Min(sr / 5, n - k);
                float f0 = 180f + 40f * ((k / stride) % 3);
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)sr;
                    data[k + i] += Mathf.Sin(2f * Mathf.PI * f0 * t) * Mathf.Exp(-14f * t) * 0.06f;
                }
            }
            int fade = sr / 10;                      // click-free loop seam
            for (int i = 0; i < fade; i++) { float m = i / (float)fade; data[i] *= m; data[n - 1 - i] *= m; }
            return data;
        }

        /// <summary>Pure crackle synthesis — fixed seed, hiss floor + sparse decaying pops.</summary>
        public static float[] CrackleSamples(int sr, float secs)
        {
            int n = (int)(sr * secs);
            var data = new float[n];
            var rng = new System.Random(53111);      // fixed — distinct from the activity seed
            float pop = 0f;
            for (int i = 0; i < n; i++)
            {
                if (rng.NextDouble() < 0.0025) pop = 0.5f + 0.5f * (float)rng.NextDouble();
                pop *= 0.9992f - 0.15f * (pop > 0.02f ? 0.02f : 0f);
                pop *= 0.995f;
                float hiss = (float)(rng.NextDouble() - 0.5) * 0.05f;
                data[i] = hiss + (float)(rng.NextDouble() - 0.5) * pop * 0.8f;
            }
            int fade = sr / 10;
            for (int i = 0; i < fade; i++) { float m = i / (float)fade; data[i] *= m; data[n - 1 - i] *= m; }
            return data;
        }

        // reconciler-school mapping (HutReconciler/LiveReconciler): sim tile -> world, terrain-grounded
        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);
        static Vector3 GroundW(Vector3 world, float lift = 0.4f)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }
        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;
    }
}
