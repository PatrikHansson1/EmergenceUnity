// EMERGENCE — FAS 3 increment 1 (D-133): the WATCH camera. Pan/zoom/orbit over the world at
// documentary eye height, terrain-clamped. Presentation only — never touches sim state.
// Human input (WASD/arrows, scroll, Q/E) is best-effort via legacy Input (guarded — the probe
// drives the rig programmatically; interactive polish is a later Fas 3 increment).
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    [DisallowMultipleComponent]
    public sealed class Fas3CameraRig : MonoBehaviour
    {
        public float panSpeed = 14f;
        public float zoomSpeed = 10f;
        public float orbitSpeed = 60f;
        public float minAboveTerrain = 2.5f;
        public float maxAboveTerrain = 55f;

        /// <summary>Programmatic pan in the camera's ground plane (x=right, y=forward).</summary>
        public void Pan(Vector2 d)
        {
            var fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            var right = transform.right; right.y = 0f; right.Normalize();
            transform.position += right * d.x + fwd * d.y;
            Clamp();
        }

        /// <summary>Programmatic zoom along view direction (positive = closer).</summary>
        public void Zoom(float d) { transform.position += transform.forward * d; Clamp(); }

        /// <summary>Yaw around the current ground focus point.</summary>
        public void Orbit(float deg)
        {
            Vector3 pivot = GroundFocus();
            transform.RotateAround(pivot, Vector3.up, deg);
            Clamp();
        }

        Vector3 GroundFocus()
        {
            var t = Terrain.activeTerrain;
            var ray = new Ray(transform.position, transform.forward);
            // cheap ground intersect: sample terrain height along the ray
            for (float s = 2f; s < 200f; s += 2f)
            {
                var p = ray.GetPoint(s);
                float th = t != null ? t.SampleHeight(p) + t.transform.position.y : 0f;
                if (p.y <= th + 0.2f) return new Vector3(p.x, th, p.z);
            }
            return transform.position + transform.forward * 20f;
        }

        void Update()
        {
            try
            {
                float dt = Time.unscaledDeltaTime;
                float px = Input.GetAxisRaw("Horizontal"), py = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(px) > 0.01f || Mathf.Abs(py) > 0.01f) Pan(new Vector2(px, py) * panSpeed * dt);
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll * zoomSpeed * dt * 10f);
                if (Input.GetKey(KeyCode.Q)) Orbit(-orbitSpeed * dt);
                if (Input.GetKey(KeyCode.E)) Orbit(orbitSpeed * dt);
            }
            catch (Exception) { /* input backend not active (new Input System only) — programmatic API still works */ }
        }

        void Clamp()
        {
            var t = Terrain.activeTerrain;
            if (t == null) return;
            var p = transform.position;
            float th = t.SampleHeight(p) + t.transform.position.y;
            p.y = Mathf.Clamp(p.y, th + minAboveTerrain, th + maxAboveTerrain);
            transform.position = p;
        }
    }
}
