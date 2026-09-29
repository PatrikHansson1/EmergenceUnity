// EMERGENCE — diorama camera (D-890, first .exe v0.1). A documentary orbit over the dressed floor world:
// slow auto-orbit around the village centroid, left-drag to look, scroll to zoom, WASD/arrows to pan the
// pivot, space to pause the orbit. Presentation-only runtime behaviour; touches no sim state. When no target
// is set it auto-frames the scene bounds at Awake so a bare build still shows the world.
// D-918 (sprint review D-917): yields to Fas3GazeDirector while it holds, adopts its framing on release; orbit
// toggle moved Space->O (Space is pause). Deliberately UNGUARDED: a dead input backend must be loud, not silent.
using UnityEngine;

namespace Emergence.Runtime
{
    [AddComponentMenu("Emergence/Diorama Camera")]
    public class EmergenceDioramaCamera : MonoBehaviour
    {
        public Transform pivot;                 // orbit centre; auto-found from scene bounds if null
        public float distance = 120f;           // metres from pivot
        public float minDistance = 20f, maxDistance = 400f;
        public float height = 55f;              // pivot lift so we look down into the village
        public float autoOrbitDegPerSec = 3.5f; // slow documentary drift
        public float dragLookSpeed = 0.25f, panSpeed = 40f, zoomSpeed = 40f;

        float _yaw, _pitch = 24f;
        bool _autoOrbit = true;
        Vector3 _pivotPos;
        Fas3GazeDirector _gaze;   // D-918: the living gaze on the same camera; the orbit yields while it holds
        bool _wasGazing;

        void Awake()
        {
            if (pivot != null) _pivotPos = pivot.position;
            else _pivotPos = SceneCentroid();
            _pivotPos.y += 0f;
            Apply();
        }

        static Vector3 SceneCentroid()
        {
            var rs = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (rs.Length == 0) return Vector3.zero;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return new Vector3(b.center.x, b.min.y, b.center.z);
        }

        void Update()
        {
            // D-918: yield to the living gaze (Fas3GazeDirector writes in LateUpdate). While it holds a target the
            // orbit neither reads input nor writes the transform; on release it ADOPTS the gaze's framing (pivot =
            // the thing the gaze showed, exact inverse of Apply) so it resumes with no snap and orbits what mattered.
            // Before this the gaze only worked because this Update threw on line 1 every frame (review D-917).
            if (_gaze == null) _gaze = GetComponent<Fas3GazeDirector>();
            if (_gaze != null && _gaze.HasTarget) { _wasGazing = true; return; }
            if (_wasGazing) { _wasGazing = false; AdoptGazeFraming(); }

            if (Input.GetKeyDown(KeyCode.O)) _autoOrbit = !_autoOrbit;   // D-918: was Space — clashed with TimeControls' pause
            if (_autoOrbit) _yaw += autoOrbitDegPerSec * Time.deltaTime;

            if (Input.GetMouseButton(0))
            {
                _autoOrbit = false;
                _yaw   += Input.GetAxis("Mouse X") * dragLookSpeed * 60f * Time.deltaTime;
                _pitch -= Input.GetAxis("Mouse Y") * dragLookSpeed * 60f * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, 5f, 80f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
                distance = Mathf.Clamp(distance - scroll * zoomSpeed * 10f, minDistance, maxDistance);

            var fwd = Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;
            float h = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float v = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            if (h != 0 || v != 0) _pivotPos += (right * h + fwd * v) * panSpeed * Time.deltaTime;

            Apply();
        }

        // D-918: re-derive the orbit state from where the gaze left the camera. Point of interest = what the gaze
        // looked at (its Target + the 0.8 m it aims above ground); distance = how far we are from it now (may sit
        // under minDistance until the next scroll — that is the gaze's own close framing, kept on purpose).
        void AdoptGazeFraming()
        {
            var look = _gaze.Target + Vector3.up * 0.8f;
            var e = transform.rotation.eulerAngles;
            float pitch = e.x > 180f ? e.x - 360f : e.x;
            _pitch = Mathf.Clamp(pitch, 5f, 80f); _yaw = e.y;
            distance = Mathf.Max(1f, Vector3.Distance(look, transform.position));
            _pivotPos = look - Vector3.up * height;
        }

        void Apply()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0);
            var target = _pivotPos + Vector3.up * height;
            transform.position = target - rot * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }
    }
}
