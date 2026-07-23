// EMERGENCE — FAS 6 increment 1: the ERA EAR — ambience shifts with the applied state's era.
//
// The Fas 6 gate demands "ljud skiftar korrekt med era/tillstånd, deterministiskt". This is the
// purchase-free mechanism: seven procedural ambience BEDS (one per interim era, D-147), each
// synthesized once in Awake from a FIXED per-era seed, crossfaded whenever the APPLIED state's
// era changes. The presentation follows applied state — the same law the Almanac obeys: no bus
// race, no witnessed-history question (a bed is a NOW-texture, not narration).
//
// Determinism (D-078 r4): bed synthesis is a pure function of (era, samplerate, seconds) — the
// probe hashes BedSamples twice and across eras. Nothing here reads sim RNG or writes state;
// the golden master cannot see this class. Pause honesty (D-141 school): the bed keeps sounding
// under pause (ambience from frame one), but the ERA cannot move while paused because the
// applied state is frozen — asserted by the probe, not assumed.
//
// Replace-path (same clause as Fas3AudioDirector): when a real audio purchase lands, the era
// tracking + crossfade stay; only the bed sources swap.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas6EraAmbience : MonoBehaviour
    {
        public const int EraCount = 7;            // WorldEras interim canon (dawn..steam, D-147)
        public float bedVolume = 0.14f;           // sits UNDER the v0 wind — a color, not a voice
        public float crossfadeSecs = 2.5f;

        public int CurrentEra { get; private set; } = -1;   // -1 until the first applied state
        public int CrossfadesDone { get; private set; }
        public bool BedPlaying => (_aActive ? _a : _b) != null && (_aActive ? _a : _b).isPlaying;

        AudioSource _a, _b;
        bool _aActive = true;
        float _fadeT = 1f;                        // 1 = settled
        readonly AudioClip[] _beds = new AudioClip[EraCount];
        Fas3WorldRuntime _world;

        void Awake()
        {
            const int sr = 22050;                 // beds are low textures — half rate keeps them cheap
            for (int e = 0; e < EraCount; e++)
            {
                var clip = AudioClip.Create("emg_era_" + WorldEras.Name(e), (int)(sr * 6f), 1, sr, false);
                clip.SetData(BedSamples(e, sr, 6f), 0);
                _beds[e] = clip;
            }
            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b }) { s.loop = true; s.spatialBlend = 0f; s.volume = 0f; }
        }

        void Update()
        {
            if (_world == null) { _world = FindAnyObjectByType<Fas3WorldRuntime>(); if (_world == null) return; }
            var S = _world.LastState; if (S == null) return;

            int era = Mathf.Clamp(S.era, 0, EraCount - 1);
            if (era != CurrentEra)
            {
                var target = _aActive ? _b : _a;   // fade toward the idle source
                target.clip = _beds[era];
                target.Play();
                _aActive = !_aActive;
                _fadeT = CurrentEra < 0 ? 1f : 0f; // first era = snap (opening), later = crossfade
                if (CurrentEra >= 0) CrossfadesDone++;
                CurrentEra = era;
            }

            // presentation-side fade; unscaled so pause never stalls the seam mid-blend
            if (_fadeT < 1f) _fadeT = Mathf.Min(1f, _fadeT + Time.unscaledDeltaTime / Mathf.Max(0.1f, crossfadeSecs));
            var on  = _aActive ? _a : _b;
            var off = _aActive ? _b : _a;
            if (on != null) { on.volume = bedVolume * _fadeT; if (!on.isPlaying && on.clip != null) on.Play(); }
            if (off != null)
            {
                off.volume = bedVolume * (1f - _fadeT);
                if (_fadeT >= 1f && off.isPlaying) off.Stop();
            }
        }

        /// <summary>Pure bed synthesis — a deterministic function of (era, sr, secs). Per-era FIXED
        /// seed; character walks the eras: the leak opens (brighter noise floor) and a sparse
        /// struck-partial pattern densifies as civilization advances. The probe calls this twice
        /// and across eras to prove determinism/distinctness; Awake calls it for the real beds.</summary>
        public static float[] BedSamples(int era, int sr, float secs)
        {
            int n = (int)(sr * secs);
            var data = new float[n];
            var rng = new System.Random(52000 + era * 37);     // fixed seed — same bed every run IN THIS RUNTIME (G-review r1 I3:
                                                               // System.Random sequence is implementation-defined across runtimes;
                                                               // replace-path: hash-PRNG or bought layers A4. Never touches sim.)
            float leak = 0.980f + era * 0.002f;                 // dawn = darkest, steam = most open
            float gain = 1.8f + era * 0.12f;
            float v = 0f;
            for (int i = 0; i < n; i++)
            {
                v += (float)(rng.NextDouble() - 0.5) * 0.05f;
                v *= leak;
                data[i] = v * gain;
            }
            // sparse era voice: one soft partial strike per ~1.5 s, pitch climbing with era
            int stride = (int)(sr * 1.5f);
            float f0 = 110f * (1f + era * 0.5f);
            for (int k = stride / 2; k < n; k += stride)
            {
                int len = Mathf.Min(sr / 2, n - k);
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)sr;
                    data[k + i] += Mathf.Sin(2f * Mathf.PI * f0 * t) * Mathf.Exp(-6f * t) * 0.10f;
                }
            }
            int fade = sr / 10;                                 // 100 ms seam fade — click-free loop
            for (int i = 0; i < fade; i++)
            {
                float m = i / (float)fade;
                data[i] *= m; data[n - 1 - i] *= m;
            }
            return data;
        }
    }
}
