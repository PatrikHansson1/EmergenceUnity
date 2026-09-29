// EMERGENCE — FieldReconciler (D-914, D-906 path 1): farmland that GROWS with the sim, like the roads.
// The static dresser baked the mature y120 fields, so genesis showed tilled fields before any farming
// existed (a legibility break, same class as a codex anachronism, D-106). This reconciles fenced field
// enclosures LIVE from the applied state's WorldField[]: only EXPOSED tile-edges (a neighbour that is
// not itself a field) get a fence, so interiors stay open and the outer perimeter forms itself; fields
// appear and spread as the civilisation takes up farming. Presentation-only (D-078 r4): reads applied
// state, deterministic (hash-based variant + placement, never sim-RNG, never Time), disarms on any
// error. The tilled-soil SPLAT is intentionally dropped for this path (fences on grass) — the studio's
// call under Patrik's delegation (reversible); the live-splat variant (path 2) was judged too costly.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class FieldReconciler
    {
        const float TileSize = 8f;             // parity with WorldDresser
        const string LayerName = "LiveFields";
        static readonly string[] FenceNames =
            { "P_PROP_fence_v01_01","P_PROP_fence_v01_02","P_PROP_fence_v01_03","P_PROP_fence_v01_04","P_PROP_fence_v01_05" };

        Transform _root;
        long _sig = long.MinValue;
        float _segLen = 2f; bool _lenAlongX = true; bool _measured;

        public int FenceCount { get; private set; }
        public string LastNote { get; private set; } = "";

        static uint Hash(int a, int b)
        { unchecked { uint h = 2166136261u; h = (h ^ (uint)a) * 16777619u; h = (h ^ (uint)b) * 16777619u; return h ^ (h >> 13); } }

        public void Reconcile(WorldState S)
        {
            try { ReconcileInner(S); LastNote = "fields=" + FenceCount; }
            catch (Exception e) { LastNote = "fields FAILED: " + e.Message; Debug.LogWarning("[FieldReconciler] " + e.Message); }
        }

        void ReconcileInner(WorldState S)
        {
            var fields = S != null ? S.fields : null;
            long sig = 1469598103934665603L;
            int n = fields != null ? fields.Length : 0;
            sig ^= n; sig *= 1099511628211L;
            if (fields != null)
                foreach (var f in fields) { sig ^= Hash(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y)); sig *= 1099511628211L; }
            sig ^= (long)(S != null ? S.H : 0) << 40;
            if (sig == _sig) return;               // only rebuild when the field-set actually changes
            _sig = sig;

            if (_root == null) _root = new GameObject(LayerName).transform;
            for (int i = _root.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(_root.GetChild(i).gameObject);
            FenceCount = 0;
            if (fields == null || fields.Length == 0) return;

            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) { LastNote = "no catalog"; return; }
            var variants = new List<GameObject>();
            foreach (var nm in FenceNames) { var p = cat.Prefab(nm); if (p != null) variants.Add(p); }
            if (variants.Count == 0) { LastNote = "no fence prefabs"; return; }
            if (!_measured) { Measure(variants[0]); _measured = true; }

            var terrain = Terrain.activeTerrain;
            var set = new HashSet<(int, int)>();
            foreach (var f in fields) set.Add((Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y)));
            int H = S.H;
            float half = TileSize * 0.5f;

            foreach (var (tx, ty) in set)
            {
                float cx = tx * TileSize, cz = (H - 1 - ty) * TileSize;   // sim y -> world -z, parity with WorldDresser.P
                if (!set.Contains((tx + 1, ty))) EdgeFences(variants, terrain, cx + half, cz - half, cx + half, cz + half); // east, along Z
                if (!set.Contains((tx - 1, ty))) EdgeFences(variants, terrain, cx - half, cz - half, cx - half, cz + half); // west, along Z
                if (!set.Contains((tx, ty + 1))) EdgeFences(variants, terrain, cx - half, cz - half, cx + half, cz - half); // south (+ sim y), along X
                if (!set.Contains((tx, ty - 1))) EdgeFences(variants, terrain, cx - half, cz + half, cx + half, cz + half); // north (- sim y), along X
            }
        }

        void EdgeFences(List<GameObject> variants, Terrain terrain, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az; float L = Mathf.Sqrt(dx * dx + dz * dz); if (L < 0.01f) return;
            bool alongX = Mathf.Abs(dx) >= Mathf.Abs(dz);
            float yaw = alongX ? (_lenAlongX ? 0f : 90f) : (_lenAlongX ? 90f : 0f);
            int cnt = Mathf.Max(1, Mathf.RoundToInt(L / Mathf.Max(0.5f, _segLen)));
            float ux = dx / L, uz = dz / L, step = L / cnt;
            for (int i = 0; i < cnt; i++)
            {
                float t = step * (i + 0.5f); float wx = ax + ux * t, wz = az + uz * t;
                var pf = variants[(int)(Hash(Mathf.RoundToInt(wx), Mathf.RoundToInt(wz)) % (uint)variants.Count)];
                var go = UnityEngine.Object.Instantiate(pf, _root);
                float y = terrain != null ? terrain.SampleHeight(new Vector3(wx, 0, wz)) + terrain.transform.position.y : 0f;
                go.transform.position = new Vector3(wx, y, wz);
                go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                go.name = "livefence";
                FenceCount++;
            }
        }

        void Measure(GameObject prefab)
        {
            _segLen = 2f; _lenAlongX = true;
            try
            {
                var probe = UnityEngine.Object.Instantiate(prefab);
                var rs = probe.GetComponentsInChildren<Renderer>(true);
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    _lenAlongX = b.size.x >= b.size.z;
                    _segLen = Mathf.Max(0.5f, Mathf.Max(b.size.x, b.size.z));
                }
                UnityEngine.Object.DestroyImmediate(probe);
            }
            catch { }
        }
    }
}
