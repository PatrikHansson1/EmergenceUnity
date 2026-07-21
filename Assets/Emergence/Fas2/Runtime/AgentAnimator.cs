// EMERGENCE — Fas 2 step 1 (D-122 → D-123): LIVE ANIMATION IN PLAY MODE.
//
// A tiny runtime component the dresser/reconciler feeds from the sim agent's `task`. In EDIT mode it is
// inert (the dresser's sampled still-pose stands, D-122); in PLAY mode it drives the shared Animator
// controller (Idle/Walk/Work) so villagers visibly walk and work.
//
// Determinism law (D-078 r4): presentation READS state, never writes back. The sim owns `task`; this class
// only translates it to an animation state. All variation (cycle phase) is hash(agentId) — never sim RNG.
// Root motion is OFF: the agent's world position is the sim's truth; the clip may not move it.
using UnityEngine;

namespace Emergence.Runtime
{
    /// <summary>
    /// The ONE task→read classifier (used by both the editor dresser's still-poses and the live animator,
    /// so stills and play mode can never disagree). Working = work loop; Moving = walk cycle; else idle.
    /// </summary>
    public static class AgentTaskRead
    {
        public static bool Working(string task) => task != null && (task.Contains("work") || task.Contains("forg")
            || task.Contains("tend") || task.Contains("build") || task.Contains("mill") || task.Contains("craft")
            || task.Contains("bak") || task.Contains("smith") || task.Contains("dig") || task.Contains("chop"));

        // D-123: extended with "search"/"heading" — the engine's dominant movement tasks
        // ("searching for clay", "heading to berry") previously read as idle. Presentation-only.
        public static bool Moving(string task) => task != null && (task.Contains("walk") || task.Contains("forag")
            || task.Contains("fish") || task.Contains("hunt") || task.Contains("gather") || task.Contains("carr")
            || task.Contains("seek") || task.Contains("wander") || task.Contains("go ") || task.Contains("toward")
            || task.Contains("travel") || task.Contains("herd") || task.Contains("search") || task.Contains("heading"));

        /// <summary>Animator state name for a task. canWork=false for bands without a work clip (child/elder).</summary>
        public static string StateFor(string task, bool canWork) =>
            (canWork && Working(task)) ? "Work" : Moving(task) ? "Walk" : "Idle";
    }

    [DisallowMultipleComponent]
    public sealed class AgentAnimator : MonoBehaviour
    {
        public int agentId;
        public string task;
        public bool canWork = true;   // adults only — the child/elder GLB sets carry no work clip
        // D-124: identity carried on the instance so the live agent-reconciler can rehydrate its
        // id->instance map after the enter-playmode domain reload (serialized fields survive it).
        public string band = "adult";
        public bool female;

        Animator _anim;
        string _state = "";

        // ---- v2 movement (D-129): between snapshots a soul WALKS to its new sim position ----
        // The sim still owns the destination (D-078 r4); the glide is presentation-side easing along a
        // straight, terrain-following line (true desire-lines need the engine's pathUse export — a
        // Simulation-Architect item). Speed is hash(id)-varied; > MaxGlide reads as a scene cut (teleport).
        public const float MaxGlide = 60f;
        Vector3 _glideTarget; bool _transit; float _speed;
        public bool InTransit => _transit;
        public float RemainingGlide => _transit ? Vector3.Distance(transform.position, _glideTarget) : 0f;

        static uint Hash(uint x) { x ^= x >> 16; x *= 0x7feb352du; x ^= x >> 15; x *= 0x846ca68bu; x ^= x >> 16; return x; }

        void Start()
        {
            _anim = GetComponentInChildren<Animator>();
            if (_anim == null) return;
            _anim.applyRootMotion = false;                                   // sim position is truth
            _anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;    // A6: off-screen agents cost less
            Apply(true);
        }

        /// <summary>Reconciler-facing: update the task live (crossfades only when the read changes).</summary>
        public void SetTask(string t) { task = t; if (_anim != null) Apply(false); }

        /// <summary>Reconciler-facing (v2, D-129): walk to the new sim position instead of teleporting.</summary>
        public void GlideTo(Vector3 target)
        {
            if (!Application.isPlaying) { transform.position = target; return; }
            float d = Vector3.Distance(transform.position, target);
            if (d < 0.05f || d > MaxGlide) { transform.position = target; if (_transit) EndTransit(); return; }
            _glideTarget = target;
            _speed = 1.15f + (Hash((uint)agentId * 2654435761u + 41u) & 0xffu) / 255f * 0.35f;   // 1.15–1.50 u/s
            if (!_transit) { _transit = true; if (_anim != null) Apply(false); }
        }

        void Update()
        {
            if (!_transit) return;
            if (_anim == null) { transform.position = _glideTarget; _transit = false; return; }
            var pos = transform.position;
            var to = _glideTarget - pos; to.y = 0f;
            float step = _speed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = Grounded(_glideTarget); EndTransit(); return; }
            var dir = to.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
            transform.position = Grounded(pos + dir * step);
        }

        void EndTransit() { _transit = false; _state = ""; Apply(false); }   // re-read the task state

        static Vector3 Grounded(Vector3 w)
        {
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        void Apply(bool hashPhase)
        {
            if (_anim.runtimeAnimatorController == null) return;
            string s = _transit ? "Walk" : AgentTaskRead.StateFor(task, canWork);   // transit overrides the read
            if (s == _state) return;
            _state = s;
            // phase de-sync so 111 villagers don't stride in lockstep — hash(agentId), never sim RNG
            float phase = hashPhase ? (Hash((uint)agentId * 2654435761u + 17u) & 0xffffu) / 65536f : 0f;
            _anim.CrossFade(s, 0.15f, 0, phase);
        }
    }
}
