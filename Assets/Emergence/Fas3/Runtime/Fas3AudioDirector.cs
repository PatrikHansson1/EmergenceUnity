// EMERGENCE — FAS 3 increment 8 (D-141): the AUDIO LANE v1 — the bus finally speaks.
//
// The audio event bus was laid EMPTY in Fas 0 (D-107 cross-lock: "ljudets event-buss läggs redan i
// Fas 0 — riv aldrig reconcilern för ljud senare"). This is the first consumer: a purchase-free
// PROCEDURAL v0 — the studio owns no ambience/stinger assets yet (only 3 Vefects fire loops), so
// the opening's sound is SYNTHESIZED: a low wind-ambience loop (brown noise, faded seam) and small
// struck-metal chimes for milestones (the first hut) with a softer tone for births. All clips are
// generated once in Awake from a FIXED seed — pure presentation, never sim-RNG (D-078 r4); the
// golden master cannot see this class.
//
// Replace-path: when a real audio pass/purchase lands (Fas 6), this director keeps its bus wiring
// and swaps clip sources — exactly why the bus existed before the sound did.
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3AudioDirector : MonoBehaviour
    {
        // D-237: was 0.22 back when this synthesized wind was the ONLY sound in the game. The studio
        // now owns real ambient music (Fas6MusicDirector), and a bed that was the whole soundscape
        // becomes a hiss ON TOP of one. It sits under the music now.
        public float ambienceVolume = 0.09f;
        public float stingerVolume = 0.45f;
        public float minStingerGap = 1.5f;   // presentation politeness — never machine-gun the chime

        public bool AmbiencePlaying => _amb != null && _amb.isPlaying;
        public int StingersPlayed { get; private set; }
        public int BirthTonesPlayed { get; private set; }     // "a child is born" ONLY — genesis honesty (D-135/D-140)
        public int ArrivalTonesPlayed { get; private set; }   // "a soul arrives" (genesis) — same clip, separate truth

        AudioSource _amb, _voice;
        AudioClip _wind, _chime, _soft;
        float _lastStinger = -99f;

        void Awake()
        {
            const int sr = 44100;
            _wind = BrownNoise("emg_wind", sr, 4.0f);
            _chime = Struck("emg_chime", sr, 0.9f, new[] { 660f, 990f, 1320f }, 4f);
            _soft = Struck("emg_soft", sr, 0.6f, new[] { 440f, 550f }, 5f);

            _amb = gameObject.AddComponent<AudioSource>();
            _amb.clip = _wind; _amb.loop = true; _amb.volume = ambienceVolume; _amb.spatialBlend = 0f;
            _amb.Play();
            _voice = gameObject.AddComponent<AudioSource>();
            _voice.loop = false; _voice.volume = stingerVolume; _voice.spatialBlend = 0f;
        }

        void OnEnable() { PresentationEventBus.OnEvent += OnBus; }
        void OnDisable() { PresentationEventBus.OnEvent -= OnBus; }

        void OnBus(PresentationEvent e)
        {
            if (_voice == null) return;
            float now = Time.unscaledTime;
            if (e.Type == PresentationEventType.Milestone)
            {
                // D-141 (same lesson as the gaze, D-140): milestones are RARE and canonical — they bypass
                // the politeness gap. A year's births land in the same Apply as its first hut; without
                // the bypass the birth tone swallowed the chime the mechanism exists to play.
                _voice.PlayOneShot(_chime, stingerVolume); StingersPlayed++; _lastStinger = now;
            }
            else if (e.Type == PresentationEventType.AgentActivity && (e.Data == "a child is born" || e.Data == "a soul arrives"))
            {
                // Gate-review fix (2026-07-22): an ARRIVING genesis soul is not a BIRTH — count them
                // apart so the report can never call a y0 arrival a "birth tone" again. Same soft clip.
                if (now - _lastStinger < minStingerGap) return;
                _voice.PlayOneShot(_soft, stingerVolume * 0.7f); _lastStinger = now;
                if (e.Data == "a child is born") BirthTonesPlayed++; else ArrivalTonesPlayed++;
            }
        }

        // ---- procedural clips (fixed-seed presentation audio; the sim never sees any of this) ----
        static AudioClip BrownNoise(string name, int sr, float secs)
        {
            int n = (int)(sr * secs);
            var data = new float[n];
            var rng = new System.Random(78901);   // fixed seed — same wind every run IN THIS RUNTIME (G-review r1 I3:
                                                  // System.Random is implementation-defined across runtimes; replace-path A4)
            // D-237: THIS WAS NEVER BROWN. A single leaky integrator at 0.985 is a one-pole lowpass
            // with its corner near 105 Hz — and one pole is only 6 dB per octave, so the whole top end
            // survives at a level you hear as HISS. Patrik heard it plainly the moment real music went
            // in underneath: "ett brus ligger ovanpå". Brown noise falls at 12 dB per octave and reads
            // as distant water or wind in a chimney, never as tape hiss. Two more poles, and the leak
            // moved to 0.999 so the integrator is actually an integrator. Normalised at the end rather
            // than multiplied by a magic 2.2, so changing the filter cannot change the level.
            float v = 0f, l1 = 0f, l2 = 0f, peak = 1e-6f;
            for (int i = 0; i < n; i++)
            {
                v += (float)(rng.NextDouble() - 0.5) * 0.06f;
                v *= 0.999f;                       // leak — stops the walk drifting to the rails, nothing more
                l1 += (v - l1) * 0.03f;            // ~210 Hz
                l2 += (l1 - l2) * 0.03f;           // ~210 Hz again: 18 dB/oct with the leak, no top end left
                data[i] = l2;
                float m = data[i] < 0f ? -data[i] : data[i];
                if (m > peak) peak = m;
            }
            float norm = 0.7f / peak;
            for (int i = 0; i < n; i++) data[i] *= norm;
            int fade = sr / 20;                    // 50 ms seam fade — click-free loop at v0 volume
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k; data[n - 1 - i] *= k;
            }
            var clip = AudioClip.Create(name, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Struck(string name, int sr, float secs, float[] partials, float decay)
        {
            int n = (int)(sr * secs);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)sr;
                float s = 0f;
                for (int p = 0; p < partials.Length; p++)
                    s += Mathf.Sin(2f * Mathf.PI * partials[p] * t) / (p + 1f);
                data[i] = s * Mathf.Exp(-decay * t) * 0.5f;
            }
            var clip = AudioClip.Create(name, n, 1, sr, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
