// EMERGENCE — WorkMarkReconciler (D-916, sibling of FieldReconciler/D-914): quarry work-marks that
// APPEAR as the people quarry, like the fields and the roads. The static dresser (WorldDresser.PlaceWorkMarks)
// baked the mature y120 quarry scars into the live scene, so genesis showed ~21 worked-stone piles before
// anyone had swung a pick — exactly the "lies about labor" that D-140's own genesis-honesty guard was written
// to forbid, but that guard fires at BAKE time against the y120 world (which has huts) and so cannot protect a
// live scene that starts at year 0. This reconciles worked-stone props LIVE from the applied state: a tile is
// a work-mark when it is quarried-out stone (tile 's', tileN<=3) and, per D-140, only once a settlement exists
// (huts>0) — the wilderness stays honest. Presentation-only (D-078 r4): reads applied state, deterministic
// (hash-based, never sim-RNG, never Time), disarms on any error. The bare-earth SCAR DECAL that the editor
// dresser also lays is intentionally dropped for this path (its material comes from an editor-only TerrainLayer
// lookup); the worked-stone props alone carry the "people worked here" reading — the studio's call under
// Patrik's delegation (reversible).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class WorkMarkReconciler
    {
        const float TileSize = 8f;             // parity with WorldDresser
        const string LayerName = "LiveWorkMarks";
        static readonly string[] StoneNames =
            { "P_PROP_stone_01", "P_PROP_stone_02", "P_PROP_wall_stone_small_01", "P_PROP_wall_stone_small_02" };

        Transform _root;
        long _sig = long.MinValue;

        public int MarkCount { get; private set; }
        public string LastNote { get; private set; } = "";

        // salted hash matching WorldDresser.PlaceWorkMarks so live placement reads like the studio's static pass
        static uint Hash(int x, int y, int salt)
        { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;

        public void Reconcile(WorldState S)
        {
            try { ReconcileInner(S); LastNote = "workmarks=" + MarkCount; }
            catch (Exception e) { LastNote = "workmarks FAILED: " + e.Message; Debug.LogWarning("[WorkMarkReconciler] " + e.Message); }
        }

        static bool IsQuarried(WorldState S, int x, int y)
        {
            int idx = y * S.W + x;
            if (S.tileTypes == null || idx < 0 || idx >= S.tileTypes.Length) return false;
            if (S.tileTypes[idx] != 's') return false;
            int tn = (S.tileN != null && idx < S.tileN.Length) ? S.tileN[idx] : 9;
            return tn <= 3 && Hash01(x, y, 95) < 0.6f;   // a quarried-out stone tile (mirror WorldDresser rad 158)
        }

        void ReconcileInner(WorldState S)
        {
            int huts = (S != null && S.huts != null) ? S.huts.Length : 0;

            // signature: settlement gate + the quarried-tile set. Rebuild only when it actually changes.
            long sig = 1469598103934665603L;
            sig ^= huts > 0 ? 1L : 0L; sig *= 1099511628211L;
            if (S != null && huts > 0 && S.tileTypes != null)
                for (int y = 0; y < S.H; y++)
                    for (int x = 0; x < S.W; x++)
                        if (IsQuarried(S, x, y)) { sig ^= Hash(x, y, 0); sig *= 1099511628211L; }
            sig ^= (long)(S != null ? S.H : 0) << 40;
            if (sig == _sig) return;
            _sig = sig;

            if (_root == null) _root = new GameObject(LayerName).transform;
            for (int i = _root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_root.GetChild(i).gameObject);   // R5 (D-927)
            MarkCount = 0;

            // D-140 genesis honesty: no settlement -> no work-marks. The wilderness carries no lies about labor.
            if (S == null || huts == 0 || S.tileTypes == null) { LastNote = "pre-settlement (0 huts) — no work-marks"; return; }

            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) { LastNote = "no catalog"; return; }
            var variants = new List<GameObject>();
            foreach (var nm in StoneNames) { var p = cat.Prefab(nm); if (p != null) variants.Add(p); }
            if (variants.Count == 0) { LastNote = "no stone prefabs"; return; }

            var terrain = Terrain.activeTerrain;
            int H = S.H;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    if (!IsQuarried(S, x, y)) continue;
                    float cx = x * TileSize, cz = (H - 1 - y) * TileSize;   // sim y -> world -z, parity with WorldDresser.P
                    int n = 1 + (int)(Hash(x, y, 96) % 2u);
                    for (int k = 0; k < n; k++)
                    {
                        var pf = variants[(int)(Hash(x, y, 97 + k) % (uint)variants.Count)];
                        var go = UnityEngine.Object.Instantiate(pf, _root);
                        float ox = (Hash01(x, y, 98 + k) - 0.5f) * 4f, oz = (Hash01(x, y, 99 + k) - 0.5f) * 4f;
                        float wx = cx + ox, wz = cz + oz;
                        float wy = terrain != null ? terrain.SampleHeight(new Vector3(wx, 0, wz)) + terrain.transform.position.y : 0f;
                        go.transform.position = new Vector3(wx, wy, wz);
                        go.transform.rotation = Quaternion.Euler(0, Hash(x, y, 100 + k) % 360u, 0);
                        go.transform.localScale = Vector3.one * (0.7f + Hash01(x, y, 101 + k) * 0.5f);
                        go.name = "liveworkmark";
                    }
                    MarkCount++;
                }
        }
    }
}
