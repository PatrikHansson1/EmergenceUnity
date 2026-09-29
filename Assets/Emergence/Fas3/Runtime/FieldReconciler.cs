// EMERGENCE — FieldReconciler (D-914, D-906 path 1): farmland that GROWS with the sim, like the roads.
// The static dresser baked the mature y120 fields, so genesis showed tilled fields before any farming
// existed (a legibility break, same class as a codex anachronism, D-106). This reconciles fenced field
// enclosures LIVE from the applied state's WorldField[]: only EXPOSED tile-edges (a neighbour that is
// not itself a field) get a fence, so interiors stay open and the outer perimeter forms itself; fields
// appear and spread as the civilisation takes up farming. Presentation-only (D-078 r4): reads applied
// state, deterministic (hash-based variant + placement, never sim-RNG, never Time), disarms on any
// error. D-920 (review D-919, SEEN at eye height): fences on bare grass read as EMPTY PENS — the D-914 call
// is REVERSED. The tilled soil is now painted LIVE into the terrain alphamap under each field tile, by the
// same GetAlphamaps/SetAlphamaps law Fas3RoadPainter already uses for the roads (path 2 was not costly
// after all — the mechanism existed). Tiles that leave the set go back to meadow. Presentation-only.
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
        HashSet<(int, int)> _soil = new HashSet<(int, int)>();   // D-920: tiles currently carrying painted soil
        public int SoilTexels { get; private set; }
        float _segLen = 2f; bool _lenAlongX = true; bool _measured;

        public int FenceCount { get; private set; }
        public string LastNote { get; private set; } = "";

        static uint Hash(int a, int b)
        { unchecked { uint h = 2166136261u; h = (h ^ (uint)a) * 16777619u; h = (h ^ (uint)b) * 16777619u; return h ^ (h >> 13); } }

        public void Reconcile(WorldState S)
        {
            try
            {
                long before = _sig; ReconcileInner(S); LastNote = "fields=" + FenceCount + " soil=" + SoilTexels;
                if (_sig != before) Debug.Log("[FieldReconciler] " + LastNote);   // D-920: durable evidence per rebuild (R2 lesson: green is not seen)
            }
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
            var soilSet = new HashSet<(int, int)>();
            if (fields != null) foreach (var f in fields) soilSet.Add((Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y)));
            PaintSoil(S, soilSet);   // D-920: the soil follows the field-set exactly, fences ride its grass rim
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

        // D-920: paint tilled soil into the terrain alphamap under the current field tiles (inset so the fence
        // stands on a grass rim), restore meadow under tiles that left the set. One read + one write over the
        // union bounding box — the road painter's law. Alphamap y runs opposite the tile map's.
        void PaintSoil(WorldState S, HashSet<(int, int)> cur)
        {
            var terrain = Terrain.activeTerrain; if (terrain == null || S == null) return;
            var data = terrain.terrainData; if (data == null) return;
            var L = Fas3TerrainBuilder.LastLayerIndex; int n = data.terrainLayers.Length;
            if (n == 0 || L.field < 0 || L.field >= n || L.grass < 0 || L.grass >= n) { LastNote = "soil: no field layer"; return; }
            if (cur.Count == 0 && _soil.Count == 0) return;
            int A = data.alphamapResolution;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var t in cur) { minX = Mathf.Min(minX, t.Item1); maxX = Mathf.Max(maxX, t.Item1); minY = Mathf.Min(minY, t.Item2); maxY = Mathf.Max(maxY, t.Item2); }
            foreach (var t in _soil) { minX = Mathf.Min(minX, t.Item1); maxX = Mathf.Max(maxX, t.Item1); minY = Mathf.Min(minY, t.Item2); maxY = Mathf.Max(maxY, t.Item2); }
            float W1 = Mathf.Max(1, S.W - 1), H1 = Mathf.Max(1, S.H - 1);
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - 0.5f) / W1 * (A - 1)), 0, A - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX + 0.5f) / W1 * (A - 1)), 0, A - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt((1f - (maxY + 0.5f) / H1) * (A - 1)), 0, A - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((1f - (minY - 0.5f) / H1) * (A - 1)), 0, A - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1; if (w <= 0 || h <= 0) return;
            var am = data.GetAlphamaps(x0, y0, w, h);
            int painted = 0;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float ax = (x0 + i) / (float)(A - 1) * W1;             // texel centre in tile units
                    float ay = (1f - (y0 + j) / (float)(A - 1)) * H1;
                    int tx = Mathf.RoundToInt(ax), ty = Mathf.RoundToInt(ay);
                    bool inCur = cur.Contains((tx, ty)), inOld = _soil.Contains((tx, ty));
                    if (!inCur && !inOld) continue;
                    if (inCur)
                    {
                        if (Mathf.Abs(ax - tx) > 0.44f || Mathf.Abs(ay - ty) > 0.44f) continue;   // grass rim for the fence
                        for (int l = 0; l < n; l++) am[j, i, l] *= 0.15f;
                        am[j, i, L.field] += 0.85f; painted++;
                    }
                    else { for (int l = 0; l < n; l++) am[j, i, l] = 0f; am[j, i, L.grass] = 1f; }   // abandoned: back to meadow
                }
            data.SetAlphamaps(x0, y0, am);
            _soil = new HashSet<(int, int)>(cur);
            SoilTexels = painted;
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
