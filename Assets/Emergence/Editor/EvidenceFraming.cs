// EMERGENCE — FAS 7 increment 1: THIN FORWARDER — the framing law lives in Runtime now.
//
// D-163 placed FrameSubjects here (editor assembly), which made it invisible to player-side proof
// observers (the D-138 school). The law moved VERBATIM to Emergence/Runtime/EvidenceFraming.cs
// (namespace Emergence.Runtime) so editor probes AND player proofs share ONE source. This file
// stays only because the build VM cannot delete files (D-127 mount constraint) and the Fas 6
// probes' call sites resolve here; it contains ZERO grammar — it forwards.
#if UNITY_EDITOR
using UnityEngine;

namespace Emergence.Editor
{
    public static class EvidenceFraming
    {
        /// <summary>Forwarder — the one law is Emergence.Runtime.EvidenceFraming.FrameSubjects.</summary>
        public static Vector3 FrameSubjects(out Vector3 lookAt, params Vector3[] subjects)
            => Emergence.Runtime.EvidenceFraming.FrameSubjects(out lookAt, subjects);
    }
}
#endif
