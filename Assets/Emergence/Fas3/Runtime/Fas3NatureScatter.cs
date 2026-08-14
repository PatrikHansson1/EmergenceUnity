// EMERGENCE — VÅG 1.1 (rest, 2026-08-14): THE NATURAL WORLD, MOVED TO RUNTIME.
//
// The terrain landed (D-210) and the light landed (D-211), and the ground stopped lying (D-213) —
// but the living world was still an empty meadow, because PlaceNature lives in WorldDresser and the
// whole dresser sits behind #if UNITY_EDITOR. Trees, rocks and bushes existed only in the editor's
// dressed scene and in the store shots. The player got grass.
//
// THIS IS THE SAME LAW, NOT A NEW ONE. Same tile rules (forest gets trees, thinner at the treeline
// where the wood is coppiced; stone gets rock formations; berry tiles get bushes), the same salts
// (41/43/47 and their +100/+200/+300/+400/+500 offsets), the same jitter, rotation and scale
// curves, the same 0.72 correction on the large trees. Given the same world, it places the same
// forest in the same places as the editor does — because it IS the editor's arithmetic.
//
// Determinism (D-078 r4): every choice comes from hash(x, y, salt). No sim RNG, no Random, no time.
// Presentation-only: reads the applied snapshot, writes nothing back.
//
// Cost is real and is budgeted: ~500 trees + ~280 rocks + ~160 bushes on a 100x70 map. Placement is
// SPREAD ACROSS FRAMES so the opening never hitches — the world grows in over a second or two,
// which also reads better than a forest popping into existence in one frame.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3NatureScatter
    {
        public const float TreesPerForestTile = 0.9f;
        public const float RocksPerStoneTile = 0.9f;
        public const float BushesPerBerryTile = 1.1f;
        public const int PlacementsPerFrame = 120;   // keeps the opening smooth on a mid machine

        public static string LastNote = "";
        public static int Placed { get; private set; }

        static readonly string[] TreeNames  = { "Prefab_TreeLarge_01", "Prefab_TreeLarge_02", "Prefab_TreeLarge_03", "Prefab_TreeLarge_04",
                                                "Prefab_Birch_01", "Prefab_Birch_02", "Prefab_Birch_04", "Prefab_Birch_05" };
        static readonly string[] RockNames  = { "Prefab_RockFormation_01", "Prefab_RockFormation_02", "Prefab_RockFormation_03", "Prefab_RockFormation_04", "P_ENV_stone_01" };
        static readonly string[] BushNames  = { "Prefab_Bush_01", "Prefab_Bush_02", "Prefab_Bush_03" };
        static readonly string[] TrunkNames = { "P_PROP_treetrunk_01", "P_PROP_treetrunk_02", "P_PROP_treetrunk_03", "P_PROP_treetrunk_04" };

        /// <summary>Scatter the natural world across frames. Returns a coroutine to drive from a MonoBehaviour.</summary>
        public static IEnumerator Scatter(WorldState S, Transform parent)
        {
            Placed = 0;
            if (S == null || string.IsNullOrEmpty(S.tileTypes)) { LastNote = "no map — nature skipped"; yield break; }
            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) { LastNote = "no catalog — nature skipped"; yield break; }

            var trees  = Resolve(cat, TreeNames);
            var rocks  = Resolve(cat, RockNames);
            var bushes = Resolve(cat, BushNames);
            var trunks = Resolve(cat, TrunkNames);
            if (trees.Count == 0 && rocks.Count == 0 && bushes.Count == 0)
            { LastNote = "no nature prefabs in the catalog — run BUILD ASSET CATALOG"; yield break; }

            int budget = 0, treeN = 0, rockN = 0, bushN = 0, trunkN = 0;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    char t = Fas3TerrainBuilder.Tile(S, x, y);
                    if (t == 'f' && trees.Count > 0)
                    {
                        bool edge = ForestEdge(S, x, y);
                        treeN += Place(S, parent, trees, x, y, edge ? TreesPerForestTile * 0.45f : TreesPerForestTile, 41, ref budget);
                        if (edge && trunks.Count > 0 && Hash01(x, y, 51) < 0.45f)   // coppice marks at the treeline
                        {
                            var pf = trunks[(int)(Hash(x, y, 52) % (uint)trunks.Count)];
                            float jx = Hash01(x, y, 53) - 0.5f, jy = Hash01(x, y, 54) - 0.5f;
                            Raise(pf, parent, Ground(S, x + jx * 0.8f, y + jy * 0.8f),
                                  Hash(x, y, 55) % 360u, 0.8f + Hash01(x, y, 56) * 0.4f);
                            trunkN++; budget++;
                        }
                    }
                    else if (t == 's' && rocks.Count > 0) rockN += Place(S, parent, rocks, x, y, RocksPerStoneTile, 43, ref budget);
                    else if (t == 'b' && bushes.Count > 0) bushN += Place(S, parent, bushes, x, y, BushesPerBerryTile, 47, ref budget);

                    if (budget >= PlacementsPerFrame) { budget = 0; yield return null; }
                }

            Placed = treeN + rockN + bushN + trunkN;
            LastNote = "nature: " + treeN + " trees, " + rockN + " rocks, " + bushN + " bushes, " + trunkN + " fallen trunks"
                     + "  (sets: " + trees.Count + "/" + rocks.Count + "/" + bushes.Count + ")";
        }

        static List<GameObject> Resolve(EmergenceAssetCatalog cat, string[] names)
        {
            var o = new List<GameObject>();
            foreach (var n in names) { var p = cat.Prefab(n); if (p != null) o.Add(p); }
            return o;
        }

        static int Place(WorldState S, Transform parent, List<GameObject> set, int x, int y, float perTile, int salt, ref int budget)
        {
            int count = Mathf.FloorToInt(perTile) + (Hash01(x, y, salt) < perTile - Mathf.Floor(perTile) ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                var prefab = set[(int)(Hash(x, y, salt + 100 + i) % (uint)set.Count)];
                float jx = Hash01(x, y, salt + 200 + i) - 0.5f, jy = Hash01(x, y, salt + 300 + i) - 0.5f;
                float sc = 0.85f + Hash01(x, y, salt + 500 + i) * 0.4f;
                if (prefab.name.StartsWith("Prefab_TreeLarge")) sc *= 0.72f;   // the woodland baseline, not a landmark
                Raise(prefab, parent, Ground(S, x + jx * 0.9f, y + jy * 0.9f), Hash(x, y, salt + 400 + i) % 360u, sc);
                budget++;
            }
            return count;
        }

        /// <summary>Instantiate, orient, scale — and SIT IT ON THE GROUND. The pivot is not the base
        /// (D-211): on 17.9 m of relief a trunk placed by its pivot slides half-under the hillside,
        /// which is exactly what the field report described. Only ever lifts.</summary>
        static void Raise(GameObject prefab, Transform parent, Vector3 pos, uint yaw, float scale)
        {
            var go = Object.Instantiate(prefab, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            StripImpostors(go);

            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float below = pos.y - b.min.y;
            if (below > 0.001f) go.transform.position += Vector3.up * below;
        }

        /// <summary>The pack's impostor billboards have no baked textures here and read as dark blobs
        /// or magenta at distance — the same rule the dresser applies.</summary>
        static void StripImpostors(GameObject go)
        {
            foreach (var bb in go.GetComponentsInChildren<BillboardRenderer>(true)) bb.enabled = false;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("billboard") || n.Contains("impostor"))
                {
                    var r = t.GetComponent<Renderer>();
                    if (r != null) r.enabled = false;
                }
            }
        }

        static bool ForestEdge(WorldState S, int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= S.W - 1 || y >= S.H - 1) return true;
            return Fas3TerrainBuilder.Tile(S, x - 1, y) != 'f' || Fas3TerrainBuilder.Tile(S, x + 1, y) != 'f'
                || Fas3TerrainBuilder.Tile(S, x, y - 1) != 'f' || Fas3TerrainBuilder.Tile(S, x, y + 1) != 'f';
        }

        static Vector3 Ground(WorldState S, float tx, float ty)
        {
            var w = new Vector3(tx * Fas3TerrainBuilder.TileSize, 0f, ty * Fas3TerrainBuilder.TileSize);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        // the engine's own hash pattern — NEVER sim RNG (D-078 r4)
        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;
    }
}
