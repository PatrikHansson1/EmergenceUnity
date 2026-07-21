// EMERGENCE — A6 GPU-INSTANCING (D-126). Collapses thousands of individual small-foliage
// MeshRenderers (grass clumps, flowers, bushes) into a handful of Graphics.RenderMeshInstanced
// calls per (mesh, materials, spatial cell). Per-call worldBounds give frustum culling for free;
// an optional cullDistance drops far cells of tiny foliage the LODs would have shrunk to nothing.
//
// Presentation only — reads nothing from the sim (D-078 r4). PACK-SAFE: original pack materials
// are never edited; instancing is enabled on runtime COPIES (rebuilt in OnEnable, so they survive
// the enter-playmode domain reload — only asset refs + TRS data are serialized).
// Populated by the A6Instancing editor pass; original renderers are DISABLED, not destroyed,
// so WorldDresser remains the source of truth and the pass is reversible by re-dressing.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    [ExecuteAlways]
    public sealed class FoliageInstancer : MonoBehaviour
    {
        [Serializable]
        public sealed class Group
        {
            public string note;             // e.g. "Grass/Prefab_Grass_01" (report/debug)
            public Mesh mesh;               // LOD0 mesh (asset ref)
            public Material[] sourceMats;   // pack material asset refs — copies get enableInstancing
            public List<Vector3> pos = new List<Vector3>();
            public List<Quaternion> rot = new List<Quaternion>();
            public List<Vector3> scl = new List<Vector3>();
            public float cullDistance;      // 0 = never distance-culled
            public bool castShadows;        // structures keep their shadows (look preserved); foliage off (D-118)
        }

        public List<Group> groups = new List<Group>();
        public float cellSize = 24f;   // measured best (24u: dc 3087 vs 64u: 3226 — finer frustum culling wins)

        sealed class Batch
        {
            public Mesh mesh; public Material[] mats; public Matrix4x4[] matrices;
            public Bounds bounds; public float cullDistance; public bool castShadows;
        }

        readonly List<Batch> _batches = new List<Batch>();
        readonly Dictionary<Material, Material> _instanced = new Dictionary<Material, Material>();

        public int BatchCount => _batches.Count;
        public int InstanceCount { get { int n = 0; foreach (var g in groups) n += g.pos.Count; return n; } }
        public int SubmittedLastFrame { get; private set; }

        void OnEnable() { Rebuild(); }

        void OnDisable()
        {
            foreach (var kv in _instanced)
                if (kv.Value != null) { if (Application.isPlaying) Destroy(kv.Value); else DestroyImmediate(kv.Value); }
            _instanced.Clear();
            _batches.Clear();
        }

        public void Rebuild()
        {
            _batches.Clear();
            foreach (var g in groups)
            {
                if (g.mesh == null || g.sourceMats == null || g.sourceMats.Length == 0 || g.pos.Count == 0) continue;
                var mats = new Material[g.sourceMats.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = InstancedCopy(g.sourceMats[i]);

                // spatial cells → per-call bounds = frustum culling per cell
                var cells = new Dictionary<(int, int), List<int>>();
                for (int i = 0; i < g.pos.Count; i++)
                {
                    var key = (Mathf.FloorToInt(g.pos[i].x / cellSize), Mathf.FloorToInt(g.pos[i].z / cellSize));
                    if (!cells.TryGetValue(key, out var l)) cells[key] = l = new List<int>();
                    l.Add(i);
                }
                float pad = Mathf.Max(g.mesh.bounds.extents.magnitude, 1f) * 3f; // scale/rotation headroom
                foreach (var kv in cells)
                    for (int o = 0; o < kv.Value.Count; o += 1023)   // instanced-draw hard cap
                    {
                        int n = Mathf.Min(1023, kv.Value.Count - o);
                        var m = new Matrix4x4[n];
                        var b = new Bounds(g.pos[kv.Value[o]], Vector3.zero);
                        for (int i = 0; i < n; i++)
                        {
                            int idx = kv.Value[o + i];
                            m[i] = Matrix4x4.TRS(g.pos[idx], g.rot[idx], g.scl[idx]);
                            b.Encapsulate(g.pos[idx]);
                        }
                        b.Expand(pad);
                        _batches.Add(new Batch { mesh = g.mesh, mats = mats, matrices = m, bounds = b, cullDistance = g.cullDistance, castShadows = g.castShadows });
                    }
            }
        }

        Material InstancedCopy(Material src)
        {
            if (src == null) return null;
            if (_instanced.TryGetValue(src, out var m) && m != null) return m;
            m = new Material(src) { enableInstancing = true, hideFlags = HideFlags.HideAndDontSave };
            _instanced[src] = m;
            return m;
        }

        void Update()
        {
            if (_batches.Count == 0 && groups.Count > 0) Rebuild();
            var cam = Camera.main;
            Vector3 cpos = cam != null ? cam.transform.position : Vector3.zero;
            int submitted = 0;
            foreach (var b in _batches)
            {
                if (b.cullDistance > 0f && cam != null && b.bounds.SqrDistance(cpos) > b.cullDistance * b.cullDistance)
                    continue;
                int sub = Mathf.Min(b.mats.Length, b.mesh.subMeshCount);
                for (int si = 0; si < sub; si++)
                {
                    if (b.mats[si] == null) continue;
                    var rp = new RenderParams(b.mats[si])
                    {
                        worldBounds = b.bounds,
                        shadowCastingMode = b.castShadows ? UnityEngine.Rendering.ShadowCastingMode.On
                                                          : UnityEngine.Rendering.ShadowCastingMode.Off, // foliage: off (D-118)
                        receiveShadows = b.castShadows
                    };
                    Graphics.RenderMeshInstanced(rp, b.mesh, si, b.matrices);
                    submitted++;
                }
            }
            SubmittedLastFrame = submitted;
        }
    }
}
