// EMERGENCE — VÅG 1.5 (D-216): THE WATER, AT LAST, IN THE WORLD THE PLAYER GETS.
//
// The measurement that forced this: GroundCaptureProbe's tile histogram reads
//   g=80%  f=8%  s=4%  w=4%  b=2%  c=1%
// Four per cent of the map is lake and river — 261 tiles, 16 700 square metres — and not one square
// metre of it rendered. The law existed, in WorldDresser.BuildWater, behind #if UNITY_EDITOR, the
// same trap that hid the terrain and the light (D-209). Patrik's field report said "lakes feel too
// small and look odd"; he was looking at the editor dressing, which is the ONLY place they existed.
//
// The dresser's law was also wrong in a way that would have survived the move. It laid one quad per
// water TILE and set each quad's height from the ground under that tile, so a lake was a staircase
// of 261 separate surfaces at 261 different levels. Water does not do that. Water finds ONE level per
// body and holds it.
//
// So: flood-fill the water tiles into bodies, give each body a single plane at a single height, and
// derive that height from the basin the terrain law already carved rather than from a constant. The
// terrain drops water tiles by 0.075 of the height range (~5,4 m) with a blurred falloff, so the
// deepest point of a body is its middle and the rim is the shoreline. Filling to a little BELOW the
// rim is what puts a beach between the grass and the water instead of a kerb.
//
// Presentation-only (D-078 r4): reads the applied snapshot, writes nothing back, no sim RNG. Every
// choice that varies is hash- or terrain-derived. Runtime, so the editor and the player build the
// same water from the same code and cannot drift again.
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3WaterBuilder
    {
        public const float RimDrop = 0.55f;   // metres below the shoreline rim — the beach's width
        public const int MinBodyTiles = 2;    // a single stray tile is a puddle, not a lake

        public static string LastNote = "";
        public static int Bodies, Tiles;

        public static GameObject Build(WorldState S, Transform parent, Terrain terrain)
        {
            Bodies = 0; Tiles = 0; LastNote = "";
            if (S == null || string.IsNullOrEmpty(S.tileTypes) || terrain == null)
            { LastNote = "no map or no terrain — water skipped"; return null; }

            var root = new GameObject("Water");
            if (parent != null) root.transform.SetParent(parent, true);

            var cat = EmergenceAssetCatalog.Load();
            var lakePf = cat != null ? (cat.Prefab("Prefab_WaterLake") ?? cat.Prefab("SM_WaterRiver")) : null;

            int W = S.W, H = S.H;
            var seen = new bool[W * H];
            var queue = new Queue<int>();

            for (int y0 = 0; y0 < H; y0++)
                for (int x0 = 0; x0 < W; x0++)
                {
                    int i0 = y0 * W + x0;
                    if (seen[i0] || Fas3TerrainBuilder.Tile(S, x0, y0) != 'w') continue;

                    // ---- flood fill one body ----
                    var body = new List<int>();
                    seen[i0] = true; queue.Enqueue(i0);
                    while (queue.Count > 0)
                    {
                        int i = queue.Dequeue(); body.Add(i);
                        int x = i % W, y = i / W;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                            int ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
                            int ni = ny * W + nx;
                            if (seen[ni] || Fas3TerrainBuilder.Tile(S, nx, ny) != 'w') continue;
                            seen[ni] = true; queue.Enqueue(ni);
                        }
                    }
                    if (body.Count < MinBodyTiles) continue;

                    // ---- the surface height: the body's RIM, not its floor ----
                    // A lake fills to its lowest exit, so the rim — the shoreline — is what sets the
                    // level. Taking the mean of the basin floor would sink the water below its own
                    // banks and leave a dry crater, which is exactly how the old per-tile quads read.
                    float rim = float.NegativeInfinity, floor = float.PositiveInfinity;
                    float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                    foreach (int i in body)
                    {
                        int x = i % W, y = i / W;
                        var w = World(x, y);
                        float h = terrain.SampleHeight(w) + terrain.transform.position.y;
                        if (h > rim) rim = h;
                        if (h < floor) floor = h;
                        if (w.x < minX) minX = w.x; if (w.x > maxX) maxX = w.x;
                        if (w.z < minZ) minZ = w.z; if (w.z > maxZ) maxZ = w.z;
                    }
                    float level = Mathf.Max(floor + 0.25f, rim - RimDrop);

                    float halfT = Fas3TerrainBuilder.TileSize * 0.5f;
                    minX -= halfT; maxX += halfT; minZ -= halfT; maxZ += halfT;
                    var centre = new Vector3((minX + maxX) * 0.5f, level, (minZ + maxZ) * 0.5f);
                    float sizeX = Mathf.Max(1f, maxX - minX), sizeZ = Mathf.Max(1f, maxZ - minZ);

                    var go = MakeSurface(lakePf, root.transform, centre, sizeX, sizeZ);
                    go.name = "Water_" + Bodies + "_" + body.Count + "t";
                    Bodies++; Tiles += body.Count;
                }

            LastNote = Bodies == 0
                ? "no water bodies of " + MinBodyTiles + "+ tiles in this map"
                : "water: " + Bodies + " bodies over " + Tiles + " tiles ("
                  + (Tiles * Fas3TerrainBuilder.TileSize * Fas3TerrainBuilder.TileSize / 10000f).ToString("F2") + " ha)";
            return root;
        }

        /// <summary>Dreamscape's own lake mesh where the catalog has it, a quad on their material where
        /// it does not. Never a hard failure — a missing pack should cost the picture, not the run.</summary>
        static GameObject MakeSurface(GameObject prefab, Transform parent, Vector3 centre, float sizeX, float sizeZ)
        {
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, parent);
                go.transform.position = centre;
                go.transform.rotation = Quaternion.identity;
                var mf = go.GetComponentInChildren<MeshFilter>();
                var bs = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.size : Vector3.one;
                float bx = Mathf.Max(0.01f, bs.x), bz = Mathf.Max(0.01f, bs.z);
                // 1.06 of overlap: the shoreline is a blurred falloff, so the surface must reach a
                // little past the tile edge or a rim of dry basin shows all round the water.
                go.transform.localScale = new Vector3(sizeX * 1.06f / bx, 1f, sizeZ * 1.06f / bz);
                return go;
            }

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.transform.SetParent(parent, true);
            q.transform.position = centre;
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(sizeX * 1.06f, sizeZ * 1.06f, 1f);
            var col = q.GetComponent<Collider>(); if (col != null) Object.DestroyImmediate(col);
            var mr = q.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh != null)
            {
                var m = new Material(sh) { name = "EmergenceWaterFallback" };
                m.SetColor("_BaseColor", new Color(0.20f, 0.40f, 0.52f, 1f));
                m.SetFloat("_Smoothness", 0.92f);
                m.SetFloat("_Metallic", 0f);
                mr.sharedMaterial = m;
            }
            return q;
        }

        static Vector3 World(int x, int y)
        {
            float T = Fas3TerrainBuilder.TileSize;
            return new Vector3((x + 0.5f) * T, 0f, (y + 0.5f) * T);
        }
    }
}
