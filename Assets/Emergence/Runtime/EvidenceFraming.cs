// EMERGENCE — FAS 7 increment 1 (relocation of the D-163 law): the SHARED EVIDENCE FRAMING LAW.
//
// D-163 placed FrameSubjects in Emergence/Editor — which made it invisible to PLAYER-side proof
// observers (the D-138 school runs inside a built player, no editor assembly). Fas 7's save/load
// player proof needs the SAME law, so the law moves to Runtime VERBATIM (same grammar, one source);
// Emergence/Editor/EvidenceFraming.cs remains as a thin forwarder so the Fas 6 probes' call sites
// are untouched. (The old file could not be deleted from the build VM — D-127 mount constraint —
// hence forwarder, not removal. One law, two names, zero duplicated grammar.)
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
// Probe tooling (editor AND player proofs). Presentation-side geometry math — no sim contact, no RNG.
using UnityEngine;

namespace Emergence.Runtime
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

            // Fas 7 ink. 1 (the D-163 builder note came due — an eye rejection over occluded subjects):
            // the candidate set is WIDENED and the fallback is no longer "last candidate" but the
            // candidate with the FEWEST occluded subjects (fully-clear still wins immediately).
            if (subjects.Length == 1)
            {
                var t = subjects[0];
                Vector3 best = t + new Vector3(5f, 7f, 5f); int bestOcc = int.MaxValue;
                foreach (var d in new[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1),
                                          new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1) })
                    foreach (var h in new[] { 3.5f, 7f, 11f })
                        foreach (var r in new[] { 6.5f, 4.5f })
                        {
                            var cand = t + d.normalized * r + Vector3.up * h;
                            int occ = Occluded(cand, subjects);
                            if (occ == 0) return cand;
                            if (occ < bestOcc) { bestOcc = occ; best = cand; }
                        }
                return best;
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
            var axisDir = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
            var perp = Vector3.Cross(axisDir, Vector3.up);

            // eye-height law (D-131): the camera may widen its search sideways but never flee upward
            // or outward — distance is CLAMPED to a human frame (the 55m map camera is banned evidence).
            float dNear = Mathf.Clamp(span * 0.8f, 8f, 22f);
            float dFar  = Mathf.Clamp(span * 1.2f, 10f, 30f);
            Vector3 pick = lookAt + perp * dFar + Vector3.up * 4f; int pickOcc = int.MaxValue;
            foreach (var side in new[] { perp, -perp, axisDir, -axisDir,
                                         (perp + axisDir).normalized, (perp - axisDir).normalized,
                                         (-perp + axisDir).normalized, (-perp - axisDir).normalized })
                foreach (var h in new[] { 3.5f, 6.5f, 10f })
                    foreach (var dist in new[] { dFar, dNear })
                    {
                        var cand = lookAt + side * dist + Vector3.up * h;
                        int occ = Occluded(cand, subjects);
                        if (occ == 0) return cand;
                        if (occ < pickOcc) { pickOcc = occ; pick = cand; }
                    }
            return pick;
        }

        static int Occluded(Vector3 cam, Vector3[] subjects)
        {
            int n = 0;
            foreach (var s in subjects)
                if (Physics.Linecast(cam, s + Vector3.up * 1.0f)) n++;
            return n;
        }
    }
}
