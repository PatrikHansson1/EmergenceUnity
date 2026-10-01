// EMERGENCE — FAS 3 increment 2 (D-134): the ONBOARDING GAZE — "titta, något föddes".
//
// Existence condition A begins with the eye being TAKEN somewhere: when the world does something
// for the first time (a hut is raised, a child is born), the camera glides down and frames it at
// documentary eye height, holds a beat, then lets go. Listens on PresentationEventBus only — the
// channel Fas 0 reserved for exactly this; no reconciler was touched to add the gaze.
//
// LAW (D-078 r4): pure presentation. Reads events (which are pure state-reads), moves the camera,
// writes nothing back. Target positions come from event Data (hut world-x/z, published from sim
// state) or from the spawned agent's transform — never from sim-RNG.
using System;
using System.Globalization;
using UnityEngine;

namespace Emergence.Runtime
{
    [DisallowMultipleComponent]
    public sealed class Fas3GazeDirector : MonoBehaviour
    {
        public float holdSeconds = 3.5f;
        public float cooldownSeconds = 6f;
        public float approach = 3.0f;       // exponential settle rate
        public float viewDistance = 11f;    // documentary framing: close, low, slightly above (soul-sized targets)
        public float viewHeight = 5.5f;
        // D-139 retake lesson (the inc-6 first-hut frame was all roof): a HUT is ~4x a soul — frame it wider
        public float hutViewDistance = 19f;
        public float hutViewHeight = 7.5f;

        public bool HasTarget { get; private set; }
        public Vector3 Target { get; private set; }
        public string TargetLabel { get; private set; } = "";
        public int GazeCount { get; private set; }
        public int FoundersFramed { get; private set; }   // D-936: how many souls the opening frame held
        public float foundersHoldSeconds = 9f;             // D-936: the opening frame holds longer — it is the first thing seen
        bool _foundersPending;

        float _until, _cooldownUntil;
        Vector3 _wantPos;

        void OnEnable() { PresentationEventBus.OnEvent += OnBusEvent; }
        void OnDisable() { PresentationEventBus.OnEvent -= OnBusEvent; }

        void OnBusEvent(PresentationEvent e)
        {
            // D-139 (onboarding): a hut being RAISED always takes the eye — it bypasses the cooldown.
            // Births happen every year; a hut is rare and canonical ("byn föds"). Without priority, a
            // birth in the same/previous year could hold the cooldown exactly when the first hut rises.
            bool hutPriority = e.Type == PresentationEventType.AssetSpawned && e.Data.StartsWith("hut-raised");
            if (!hutPriority && Time.unscaledTime < _cooldownUntil) return;

            // (Milestone "the first hut" carries no coords — the paired AssetSpawned right after does)
            if (hutPriority)
            {
                if (TryParseXZ(e.Data, out var p)) Aim(Grounded(p), "a hut is raised (" + e.Id + ")", hutViewDistance, hutViewHeight);
            }
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a soul arrives")
            {
                // D-936 (Patrik 2026-10-01: "såg bara 1 orörlig karaktär inte 4"): all founders arrive in ONE frame; the
                // first event took the eye (11 m on one soul), the cooldown swallowed the other three, and the orbit then
                // adopted that framing — so the opening was one idle soul, close. Frame THE FOUNDERS instead: defer one
                // frame so every arrival is in the scene, then aim at their centroid at a distance that holds them all.
                _foundersPending = true;
            }
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a child is born")
            {
                var go = FindAgent(e.Id);
                if (go != null) Aim(go.transform.position, e.Data + " (" + e.Id + ")", viewDistance, viewHeight);
            }
        }

        void Aim(Vector3 worldPos, string label, float dist, float height)
        {
            Target = worldPos; TargetLabel = label; HasTarget = true; GazeCount++;
            _until = Time.unscaledTime + holdSeconds;
            _cooldownUntil = _until + cooldownSeconds;
            // keep the camera's current compass direction; come down to eye height at the target-sized distance
            var back = transform.position - worldPos; back.y = 0f;
            if (back.sqrMagnitude < 0.01f) back = Vector3.back;
            _wantPos = Grounded(worldPos + back.normalized * dist) + Vector3.up * height;
        }

        void LateUpdate()
        {
            if (_foundersPending) { _foundersPending = false; FrameFounders(); }
            if (!HasTarget) return;
            if (Time.unscaledTime > _until) { HasTarget = false; return; }
            float k = 1f - Mathf.Exp(-approach * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, _wantPos, k);
            var look = Quaternion.LookRotation((Target + Vector3.up * 0.8f) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, k);
        }

        /// <summary>D-936: the opening frame — every live soul in one picture, from their own compass side.</summary>
        void FrameFounders()
        {
            var layer = GameObject.Find("Agents_Live");
            if (layer == null) return;
            var sum = Vector3.zero; int n = 0;
            var pts = new System.Collections.Generic.List<Vector3>();
            foreach (Transform c in layer.transform) { if (!c.gameObject.activeInHierarchy) continue; pts.Add(c.position); sum += c.position; n++; }
            if (n == 0) return;
            var centre = sum / n;
            float spread = 0f;
            foreach (var p in pts) { var d = p - centre; d.y = 0f; if (d.magnitude > spread) spread = d.magnitude; }
            // hold them all with the 38° lens: distance grows with their spread, never closer than the soul framing
            // SEEN live-verify-opening.png at 85 m: four souls of ~20 px — Patrik's "1 orörlig karaktär" all over again.
            // 38° lens: at 50 m the frame is ~34 m tall / ~60 m wide — a 30 m spread fits, a soul stands ~40 px.
            float dist = Mathf.Clamp(spread * 1.4f + 8f, 24f, 70f);
            float height = Mathf.Clamp(dist * 0.42f, viewHeight, 30f);
            float keepHold = holdSeconds; holdSeconds = foundersHoldSeconds;
            Aim(Grounded(centre), "the founders (" + n + " souls, spread " + spread.ToString("F0") + " m)", dist, height);
            holdSeconds = keepHold;
            FoundersFramed = n;
            Debug.Log("[Fas3GazeDirector] D-936 opening frame: " + TargetLabel + " dist=" + dist.ToString("F0") + " height=" + height.ToString("F0"));
        }

        static bool TryParseXZ(string data, out Vector3 p)
        {
            p = Vector3.zero;
            float x = float.NaN, z = float.NaN;
            foreach (var part in data.Split(' '))
            {
                if (part.StartsWith("x=")) float.TryParse(part.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                else if (part.StartsWith("z=")) float.TryParse(part.Substring(2), NumberStyles.Float, CultureInfo.InvariantCulture, out z);
            }
            if (float.IsNaN(x) || float.IsNaN(z)) return false;
            p = new Vector3(x, 0f, z);
            return true;
        }

        static GameObject FindAgent(string eventId)
        {
            // event id "agent-123" -> scene object "agent_123_<name>" under Agents_Live
            if (eventId == null || !eventId.StartsWith("agent-")) return null;
            string prefix = "agent_" + eventId.Substring(6) + "_";
            var layer = GameObject.Find("Agents_Live");
            if (layer == null) return null;
            foreach (Transform c in layer.transform)
                if (c.name.StartsWith(prefix, StringComparison.Ordinal)) return c.gameObject;
            return null;
        }

        static Vector3 Grounded(Vector3 world)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world;
        }
    }
}
