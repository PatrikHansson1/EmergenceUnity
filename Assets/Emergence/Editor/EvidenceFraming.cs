// EMERGENCE — FAS 7 increment 0 (D-163): the SHARED EVIDENCE FRAMING LAW — FrameSubjects.
//
// G-review lineage (Fas 6 round 1, demolition candidate R1): two increments in a row had their
// first evidence frame rejected by the human eye (camera in a canopy D-158; the soul outside the
// frame D-159), and each got its own local, hand-written framing fix. Per-probe framing craft is
// a pattern of repeat mistakes — this is the ONE law all future probes go through.
//
// Grammar (the D-159 PAIR-framing generalized):
//   - lookAt = centroid of all subjects, lifted 1u;
//   - 1 subject   -> 8 candidates: 4 compass diagonals x 2 elevations at fixed distance
//                    (D-158's mechanized canopy lesson, verbatim);
//   - 2+ subjects -> axis = the two FARTHEST subjects, camera on the perpendicular, distance
//                    proportional to the span (every subject IN frame), 2 sides x 2 elevations;
//   - a candidate wins only when the ray to EVERY subject is unoccluded (Physics.Linecast);
//     falls back to the last candidate — the blankness guard and the HUMAN EYE remain the last
//     word on evidence (D-008); this law only mechanizes the first attempt.
//
// Editor-only probe tooling. Presentation-side geometry math — no sim contact, no RNG.
#if UNITY_EDITOR
using UnityEngine;

namespace Emergence.Editor
{
    public static class EvidenceFraming
    {
        /// <summary>Pick a camera position that frames ALL subjects (see the law above).
        /// Returns the position; lookAt receives the point to aim at.</summary>
        public static Vector3 FrameSubjects(out Vector3 lookAt, params Vector3[] subjects)
        {
            if (subjects == null || subjects.Length == 0)
            { lookAt = Vector3.zero; return new Vector3(5f, 7f, 5f); }

            Vector3 centroid = Vector3.zero;
            foreach (var s in subjects) centroid += s;
            centroid /= subjects.Length;
            lookAt = centroid + Vector3.up * 1.0f;

            Vector3 pick;
            if (subjects.Length == 1)
            {
                var t = subjects[0];
                pick = t + new Vector3(5f, 7f, 5f);
                foreach (var d in new[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1) })
                    foreach (var h in new[] { 3.5f, 7f })
                    {
                        var cand = t + d.normalized * 6.5f + Vector3.up * h;
                        if (ClearToAll(cand, subjects)) return cand;
                        pick = cand;
                    }
                return pick;
            }

            // 2+ subjects: the axis between the two farthest apart carries the span
            int ia = 0, ib = 1; float far2 = -1f;
            for (int i = 0; i < subjects.Length; i++)
                for (int j = i + 1; j < subjects.Length; j++)
                {
                    float d2 = (subjects[i] - subjects[j]).sqrMagnitude;
                    if (d2 > far2) { far2 = d2; ia = i; ib = j; }
                }
            var axis = subjects[ib] - subjects[ia]; axis.y = 0f;
            float span = Mathf.Max(axis.magnitude, 8f);
            var perp = Vector3.Cross(axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward, Vector3.up);

            pick = lookAt + perp * span * 1.2f + Vector3.up * 4f;
            foreach (var side in new[] { perp, -perp })
                foreach (var h in new[] { 3.5f, 6.5f })
                {
                    var cand = lookAt + side * span * 1.2f + Vector3.up * h;
                    if (ClearToAll(cand, subjects)) return cand;
                    pick = cand;
                }
            return pick;
        }

        static bool ClearToAll(Vector3 cam, Vector3[] subjects)
        {
            foreach (var s in subjects)
                if (Physics.Linecast(cam, s + Vector3.up * 1.0f)) return false;
            return true;
        }
    }
}
#endif
