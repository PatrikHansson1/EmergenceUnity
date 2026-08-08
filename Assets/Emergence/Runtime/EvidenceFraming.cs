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
            // PRIMARY-SUBJECT law (ink-2 review, third repeat of the same failure class): subjects[0]
            // is the PRIMARY subject by convention (callers pass it first). The fallback must never
            // pick a frame that occludes the primary while merely minimizing the total count — a
            // clear line to the primary OUTRANKS fewest-total-occluded. Two passes: candidates with
            // the primary visible win on fewest-occluded; only if NO candidate sees the primary does
            // the old total-count fallback apply (and the human eye remains the last word, D-008).
            // THE PHYSICS LIE (ink-2 review, rounds 2-3): trunks, canopies and even boulders in the
            // dressed scene carry NO colliders — Physics.Linecast reports "clear" through a wall of
            // bark. Any physics-gated choice therefore fails unpredictably at ground level. The law's
            // multi-subject answer is the CRANE SHOT: camera ABOVE canopy height on a three-quarter
            // down-angle at the PRIMARY subject (subjects[0], callers pass it first) — no ground
            // object can stand between a crane and its subject. This is not the banned 55m map camera
            // (D-131): height scales with subject distance (~14-20u), a standard establishing shot.
            // Physics survives only to pick among crane DIRECTIONS (real colliders — rocks, terrain
            // walls — do register); the human eye remains the last word (D-008).
            Vector3 primary = subjects[0];
            lookAt = primary + Vector3.up * 1.0f;   // the crane AIMS AT the primary — a blended aim
                                                    // point let the primary drift out of frame at
                                                    // steep angles (ink-2 round 4's lesson)
            float dist2 = Mathf.Clamp(span * 0.8f, 12f, 20f);
            float craneH = dist2 * 0.9f + 6f;                    // clears ~8-12u canopies at all spans
            Vector3 pick = primary - axisDir * dist2 + Vector3.up * craneH;
            foreach (var side in new[] { -axisDir, perp, -perp, axisDir,
                                         (perp - axisDir).normalized, (-perp - axisDir).normalized,
                                         (perp + axisDir).normalized, (-perp + axisDir).normalized })
            {
                var cand = primary + side * dist2 + Vector3.up * craneH;
                if (!Physics.Linecast(cand, primary + Vector3.up * 1.0f)) return cand;
                pick = cand;
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
