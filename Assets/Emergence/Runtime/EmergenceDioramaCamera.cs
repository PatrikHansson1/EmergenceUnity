// EMERGENCE — diorama camera (D-890, first .exe v0.1). A documentary orbit over the dressed floor world:
// slow auto-orbit around the village centroid, left-drag to look, scroll to zoom, WASD/arrows to pan the
// pivot, space to pause the orbit. Presentation-only runtime behaviour; touches no sim state. When no target
// is set it auto-frames the scene bounds at Awake so a bare build still shows the world.
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
            if (Input.GetKeyDown(KeyCode.Space)) _autoOrbit = !_autoOrbit;
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

        void Apply()
        {
            var rot = Quaternion.Euler(_pitch, _yaw, 0);
            var target = _pivotPos + Vector3.up * height;
            transform.position = target - rot * Vector3.forward * distance;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }
    }
}
