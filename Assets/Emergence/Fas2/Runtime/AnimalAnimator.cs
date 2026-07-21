// EMERGENCE — Fas 2 step 4 (D-128): ANIMALS LIVE. The AgentAnimator pattern applied to fauna.
//
// The sim's WorldAnimal carries {id, type, x, y} — no task channel — so the behavioural read is a
// deterministic presentation-side rotation: each animal dwells in a state (deer graze-heavy, wolves
// alert-idle) and switches on a per-animal cadence. ALL variation is hash(id, epoch) — never sim RNG
// (D-078 r4). No locomotion states are triggered at rest (a walk cycle on a stationary animal would
// lie about the sim); Walk/Gallop exist in the controller for the v2-movement step.
// Legibility law: no Attack/Death without a sim event to justify it — those clips are not wired.
using UnityEngine;

namespace Emergence.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AnimalAnimator : MonoBehaviour
    {
        public int animalId;
        public string type = "deer";   // "deer" | "wolf"

        Animator _anim;
        string _state = "";
        int _epoch = -1;

        // states must exist in AnimalAnim-deer.controller (wolf swaps clips via override controller)
        static readonly string[] DeerStates = { "Graze", "Graze", "Graze", "Idle", "Idle", "Sniff", "Idle2" };
        static readonly string[] WolfStates = { "Idle", "Idle", "Idle2", "Idle2", "Sniff", "Sniff", "Graze" };

        static uint Hash(uint x) { x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16; return x; }

        public string CurrentState => _state;

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            if (_anim == null) return;
            _anim.applyRootMotion = false;                                   // sim position is truth
            _anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;    // A6: off-screen animals cost less
            Step(true);
        }

        void Update()
        {
            if (_anim == null || _anim.runtimeAnimatorController == null) return;
            Step(false);
        }

        void Step(bool first)
        {
            // per-animal dwell 7–15 s (hash-varied) → grazing herds never tick in lockstep
            float dwell = 7f + (Hash((uint)animalId * 2654435761u + 5u) & 0xff) / 255f * 8f;
            int epoch = (int)(Time.time / dwell);
            if (!first && epoch == _epoch) return;
            _epoch = epoch;

            var table = type == "wolf" ? WolfStates : DeerStates;
            uint h = Hash((uint)animalId * 2654435761u ^ (uint)epoch * 0x9E3779B9u);
            string s = table[h % (uint)table.Length];
            if (s == _state) return;
            _state = s;
            float phase = first ? (Hash((uint)animalId + 977u) & 0xffffu) / 65536f : 0f;
            _anim.CrossFade(s, 0.35f, 0, phase);
        }
    }
}
