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

                    int minTx = int.MaxValue, maxTx = int.MinValue, minTy = int.MaxValue, maxTy = int.MinValue;
                    foreach (int i in body)
                    {
                        int x = i % W, y = i / W;
                        if (x < minTx) minTx = x; if (x > maxTx) maxTx = x;
                        if (y < minTy) minTy = y; if (y > maxTy) maxTy = y;
                    }

                    var go = MakeSurface(S, lakePf, root.transform, level, minTx, maxTx, minTy, maxTy);
                    if (go == null) continue;
                    go.name = "Water_" + Bodies + "_" + body.Count + "t";
                    Bodies++; Tiles += body.Count;
                }

            LastNote = Bodies == 0
                ? "no water bodies of " + MinBodyTiles + "+ tiles in this map"
                : "water: " + Bodies + " bodies over " + Tiles + " tiles ("
                  + (Tiles * Fas3TerrainBuilder.TileSize * Fas3TerrainBuilder.TileSize / 10000f).ToString("F2") + " ha)";
            return root;
        }

        /// <summary>D-223: THE LAKE WAS A RECTANGLE.
        ///
        /// The first version scaled the pack's lake prefab — a QUAD — to the body's bounding box. A
        /// lake is not a bounding box, so wherever the basin curved away from its own corners the
        /// surface carried straight past the shore and cut a hard horizontal line across the grass.
        /// That line is visible in eye-at-the-water.png and it is why the water read as a sheet of
        /// paper laid on a lawn even after the shore was painted.
        ///
        /// So the surface is GENERATED as a mesh shaped by the same blurred water field that carved
        /// the basin and paints the beach — one field, three consumers, and they cannot disagree
        /// about where the lake is. This is step 1 of the production ladder rather than step 3: a
        /// mesh written in code is deterministic, free, needs no asset, and can be shaped by state,
        /// which an imported quad can never be. The pack's MATERIAL is kept — their water shader is
        /// the look we bought — and only its geometry is replaced.</summary>
        static GameObject MakeSurface(WorldState S, GameObject prefab, Transform parent, float level,
                                      int minTx, int maxTx, int minTy, int maxTy)
        {
            var go = new GameObject("surface");
            go.transform.SetParent(parent, true);
            // the LEVEL lives on the transform and the mesh is built flat at y=0. Baking the height
            // into the vertices instead left every surface reporting its level as 0,0 m, which made
            // the probe's own claim about the water false — a measurement that lies is worse than no
            // measurement, and this one lied the moment the geometry changed under it.
            go.transform.position = new Vector3(0f, level, 0f);

            float T = Fas3TerrainBuilder.TileSize;
            int w = maxTx - minTx + 3, h = maxTy - minTy + 3;      // one cell of margin each side
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            var index = new int[w * h];
            for (int i = 0; i < index.Length; i++) index[i] = -1;

            // A CELL IS WATER IF THE BLURRED FIELD SAYS SO, not if the tile map does. The blur already
            // rounded the lake's outline, so the surface inherits an organic shape for free instead
            // of the 8 m staircase the raw tile set would give.
            // 0.33 was too strict and it cost us three of four lakes: the blur that rounds a small
            // pond also thins it, so a pond never reached the threshold and silently produced no
            // surface at all. A cell is water if the blurred field says so OR if the MAP says so —
            // the field gives the outline its organic shape, the map guarantees a body can never
            // vanish. A rounding law must never be able to delete the thing it is rounding.
            System.Func<int, int, bool> wet = (gx, gy) =>
            {
                float sx = minTx - 1 + gx + 0.5f, sy = minTy - 1 + gy + 0.5f;
                if (Fas3TerrainBuilder.WaterAt(S, sx, sy) > 0.20f) return true;
                int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1);
                int ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                return Fas3TerrainBuilder.Tile(S, tx, ty) == 'w';
            };
            System.Func<int, int, int> vert = (gx, gy) =>
            {
                int k = gy * w + gx;
                if (index[k] >= 0) return index[k];
                float wx = (minTx - 1 + gx) * T, wz = (minTy - 1 + gy) * T;
                index[k] = verts.Count;
                verts.Add(new Vector3(wx, 0f, wz));
                uvs.Add(new Vector2(wx / (T * 8f), wz / (T * 8f)));
                return index[k];
            };

            for (int gy = 0; gy < h - 1; gy++)
                for (int gx = 0; gx < w - 1; gx++)
                {
                    if (!wet(gx, gy)) continue;
                    int a = vert(gx, gy), b = vert(gx + 1, gy), c = vert(gx + 1, gy + 1), d = vert(gx, gy + 1);
                    tris.Add(a); tris.Add(d); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(b);
                }

            if (tris.Count == 0) { Object.DestroyImmediate(go); return null; }

            var mesh = new Mesh { name = "EmergenceWater" };
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = WaterMaterial(prefab);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        /// <summary>The pack's own water material where we have it — their shader is the look we
        /// bought. A flat lit fallback otherwise: a missing pack costs the picture, never the run.</summary>
        static Material WaterMaterial(GameObject prefab)
        {
            if (prefab != null)
            {
                var src = prefab.GetComponentInChildren<MeshRenderer>();
                if (src != null && src.sharedMaterial != null) return src.sharedMaterial;
            }
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) return null;
            var m = new Material(sh) { name = "EmergenceWaterFallback" };
            m.SetColor("_BaseColor", new Color(0.20f, 0.40f, 0.52f, 1f));
            m.SetFloat("_Smoothness", 0.92f);
            m.SetFloat("_Metallic", 0f);
            return m;
        }

        static Vector3 World(int x, int y)
        {
            float T = Fas3TerrainBuilder.TileSize;
            return new Vector3((x + 0.5f) * T, 0f, (y + 0.5f) * T);
        }
    }
}
