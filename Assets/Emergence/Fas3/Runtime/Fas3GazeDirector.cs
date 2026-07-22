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
            else if (e.Type == PresentationEventType.AgentActivity &&
                     (e.Data == "a child is born" || e.Data == "a soul arrives"))
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
            if (!HasTarget) return;
            if (Time.unscaledTime > _until) { HasTarget = false; return; }
            float k = 1f - Mathf.Exp(-approach * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, _wantPos, k);
            var look = Quaternion.LookRotation((Target + Vector3.up * 0.8f) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, k);
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
