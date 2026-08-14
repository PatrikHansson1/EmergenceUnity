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

        // ---- R2 ink. 1 (Engine 2.3.2): VERB-DRIVEN ANIMATION — the engine's verb picks the state ----
        // The export now carries agents[].verb (15 canonical verbs, engine-derived from task). NEW LAW:
        // a non-empty verb SELECTS the animation state; an empty verb (old exports/fixtures/checkpoints)
        // falls back to the task classification above — backward compatibility is part of the proof.
        //   idle/rest/eat/grow -> Idle;  move -> Walk;
        //   gather/hunt/fish/carry -> Work for adults (no Carry state exists — the true carry cycle is
        //     Väg-1/R3 purchase work; the D-131 basket prop stays task-driven and unchanged), Walk for
        //     bands without a work clip (the roam reads);
        //   work/harvest/trade/fight -> Work for adults, Idle otherwise;
        //   social/ritual -> keep their existing expression paths (sayAct tempo + attend-gaze, D-159);
        //     their STATE stays the task read;  unknown/future verbs -> task fallback (engine contract:
        //     body-side falls back to idle/walk).
        /// <summary>THE ONE state law since R2 ink. 1: verb selects when present, task classifies otherwise.</summary>
        public static string StateFor(string verb, string task, bool canWork)
        {
            switch (verb)
            {
                case "idle": case "rest": case "eat": case "grow": return "Idle";
                case "move": return "Walk";
                case "gather": case "hunt": case "fish": case "carry": return canWork ? "Work" : "Walk";
                case "work": case "harvest": case "trade": case "fight": return canWork ? "Work" : "Idle";
            }
            return StateFor(task, canWork);   // empty / social / ritual / unknown → the task classification stands
        }
    }

    [DisallowMultipleComponent]
    public sealed class AgentAnimator : MonoBehaviour
    {
        public int agentId;
        public string task;
        // R2 ink. 1: the engine's canonical work verb for this soul ("" on old exports → task fallback).
        // Serialized like task/band so Rehydrate survives the enter-playmode domain reload (D-124).
        public string verb = "";
        public bool canWork = true;   // adults only — the child/elder GLB sets carry no work clip
        // D-124: identity carried on the instance so the live agent-reconciler can rehydrate its
        // id->instance map after the enter-playmode domain reload (serialized fields survive it).
        public string band = "adult";
        public bool female;
        // A2-INTERIM (D-131): the sim's sayAct channel tints the read via animator TEMPO until the
        // Väg-1 clip set lands (full emotion body-states are purchase-dependent, D-122). Deterministic:
        // value comes straight from sim state, no RNG. Subtle by design — a hungry soul drags, a soul
        // in love has spring in the step, ritual slows into gravity.
        public string sayAct = "";

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
            _anim.speed = TempoFor(band, sayAct);                            // A2-polish (D-159): age × mood
            Apply(true);
        }

        /// <summary>Reconciler-facing: update the task live (crossfades only when the read changes).</summary>
        public void SetTask(string t) { task = t; if (_anim != null) Apply(false); }

        /// <summary>R2 ink. 1: task + engine verb together (the verb picks the state when non-empty).</summary>
        public void SetTask(string t, string v) { verb = v ?? ""; SetTask(t); }

        /// <summary>A2-polish (D-159): sim sayAct → animator tempo. Full body-states await the Väg-1 clips.</summary>
        public void SetMood(string act)
        {
            sayAct = act ?? "";
            if (_anim != null) _anim.speed = TempoFor(band, sayAct);
        }
        // D-159: the FULL engine sayAct vocabulary (9 verbs, audited from the engine's speak() calls —
        // discovery/ritual/observe/teach/love/small/hungry/cold/fail). Discovery lifts, observe slows
        // into attention, cold huddles, fail sags; small stays neutral tempo (it gets the social GAZE).
        // E1.5 (Engine 2.4.1, TENSION-WAVE §A.2): the SIX drama sayActs join the ONE register —
        // additive arms only, the D-159 values are UNTOUCHED. Director-chosen (same school as D-159):
        //   raid 1.12 — aggression peaks the register (above discovery: violence moves fastest);
        //   feud 1.08 — old anger carried hot, a stride with intent;
        //   gift 1.04 — giving lifts, a lighter spring (kept below love's 1.07);
        //   steal 0.92 — the skulk: the hand hurries but the body makes itself unseen;
        //   submit 0.88 — the body makes itself small before the stronger;
        //   mourn 0.82 — grief drags heaviest of all, below cold's huddle (the slowest gait we show).
        // Unknown/future acts (e.g. the engine's 'hail') fall through to 1f — the null-tempo contract.
        public static float MoodSpeed(string act) => act switch
        {
            "discovery" => 1.10f, "love" => 1.07f, "teach" => 0.96f, "observe" => 0.94f,
            "ritual" => 0.90f, "fail" => 0.90f, "hungry" => 0.86f, "cold" => 0.85f,
            "raid" => 1.12f, "feud" => 1.08f, "gift" => 1.04f,
            "steal" => 0.92f, "submit" => 0.88f, "mourn" => 0.82f, _ => 1f
        };
        // D-159: age reads in the gait — a child skips, an elder drags. Multiplicative with mood.
        public static float AgeGain(string band) => band == "child" ? 1.06f : band == "elder" ? 0.92f : 1f;
        /// <summary>The ONE tempo law (runtime + gate proof + probe all read THIS): age × mood.</summary>
        public static float TempoFor(string band, string act) => AgeGain(band) * MoodSpeed(act ?? "");

        // ---- A2-polish (D-159): social attention — the body orients toward what the soul attends ----
        // teach/love/small face the nearest soul; cold faces the nearest fire. The TARGET is computed by
        // the reconciler as a PURE function of applied state; this class only yaw-slerps toward it.
        // Never fights the glide (transit owns heading); edit mode untouched (Face() stands, D-124).
        Vector3 _attend; bool _hasAttend;
        public bool HasAttend => _hasAttend;
        public Vector3 AttendTarget => _attend;
        public void SetAttend(Vector3 worldPos) { _attend = worldPos; _hasAttend = true; }
        public void ClearAttend() => _hasAttend = false;
        /// <summary>Probe-facing: yaw error (deg) between current facing and the attend target.</summary>
        public float AttendYawError()
        {
            if (!_hasAttend) return 0f;
            var to = _attend - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.0004f) return 0f;
            return Vector3.Angle(transform.forward, to.normalized);
        }

        /// <summary>Reconciler-facing (v2, D-129): walk to the new sim position instead of teleporting.</summary>
        public void GlideTo(Vector3 target)
        {
            if (!Application.isPlaying) { transform.position = Grounded(target); return; }
            float d = Vector3.Distance(transform.position, target);
            if (d < 0.05f || d > MaxGlide) { transform.position = Grounded(target); if (_transit) EndTransit(); return; }
            _glideTarget = target;
            _speed = 1.15f + (Hash((uint)agentId * 2654435761u + 41u) & 0xffu) / 255f * 0.35f;   // 1.15–1.50 u/s
            if (!_transit) { _transit = true; if (_anim != null) Apply(false); }
        }

        void Update()
        {
            if (!_transit)
            {
                // A2-polish (D-159): attend-gaze — gentle yaw toward the attention target while stationary
                if (_hasAttend && Application.isPlaying)
                {
                    var att = _attend - transform.position; att.y = 0f;
                    if (att.sqrMagnitude > 0.0004f)
                        transform.rotation = Quaternion.Slerp(transform.rotation,
                            Quaternion.LookRotation(att.normalized), 4f * Time.deltaTime);
                }
                return;
            }
            if (_anim == null) { transform.position = Grounded(_glideTarget); _transit = false; return; }
            var pos = transform.position;
            var to = _glideTarget - pos; to.y = 0f;
            float step = _speed * Time.deltaTime;
            if (to.magnitude <= step) { transform.position = Grounded(_glideTarget); EndTransit(); return; }
            var dir = to.normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 6f * Time.deltaTime);
            transform.position = Grounded(pos + dir * step);
        }

        void EndTransit() { _transit = false; _state = ""; Apply(false); }   // re-read the task state

        // VÅG 1.2 (2026-08-14): THE FOOT OFFSET. Grounding put the PIVOT on the terrain, which was
        // harmless while the world was a flat plane at y=0 — but the living loop now has 17.9 m of
        // real relief (D-210), and the ground-capture probe measured villagers standing 0.19–0.53 m
        // INTO the hillside. A model's pivot is not necessarily its soles: whatever sits between the
        // pivot and the lowest point of the mesh has to be given back, or the people wade through
        // the ground. Measured ONCE per body (a bounds query per agent per frame would be 111 of
        // them) and cached; re-measured if the body is rebuilt.
        float _foot = float.NaN;

        float FootOffset()
        {
            if (!float.IsNaN(_foot)) return _foot;
            _foot = 0f;
            var rs = GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return _foot;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            _foot = Mathf.Max(0f, transform.position.y - b.min.y);   // never LIFT a model, only stop it sinking
            return _foot;
        }

        /// <summary>Forget the cached foot offset — call when the body is swapped or rescaled.</summary>
        public void InvalidateFoot() { _foot = float.NaN; }

        Vector3 Grounded(Vector3 w)
        {
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y + FootOffset();
            return w;
        }

        void Apply(bool hashPhase)
        {
            if (_anim.runtimeAnimatorController == null) return;
            string s = _transit ? "Walk" : AgentTaskRead.StateFor(verb, task, canWork);   // transit overrides the read (R2: verb selects when present)
            if (s == _state) return;
            _state = s;
            // phase de-sync so 111 villagers don't stride in lockstep — hash(agentId), never sim RNG
            float phase = hashPhase ? (Hash((uint)agentId * 2654435761u + 17u) & 0xffffu) / 65536f : 0f;
            _anim.CrossFade(s, 0.15f, 0, phase);
        }
    }
}
