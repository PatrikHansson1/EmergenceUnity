// EMERGENCE — VÅG 1.1 (2026-08-14, D-209): THE TERRAIN LAW, MOVED TO RUNTIME.
//
// Why this file exists. Patrik reported the world looked wrong — flat, angular, props sinking, lakes
// odd, "it doesn't feel like we applied the environment we bought". The studio looked at actual
// screenshots instead of theorising, and the live world at year 120 was a flat green plane with one
// villager on it, while the store shots looked like a game. The cause was not art:
//
//   WorldDresser.cs is 1512 lines and the WHOLE FILE sits behind #if UNITY_EDITOR.
//
// So the dressing could never run in a player build, and Fas3Onboarding — the loop the player
// actually gets — never called it at all. Two worlds, and the player got the empty one.
//
// The terrain law itself was fine all along: three octaves of Perlin (broad hills, mid rolls, fine
// undulation), ~25 m of relief, water carved below the meadow, village centres settled flat so
// houses sit level, and the pack's real textured terrain layers. In that whole block there was
// exactly ONE editor-bound call — an AssetDatabase.CreateAsset that merely persisted the TerrainData
// as a file, which a runtime path does not need. The only genuine binding was finding the
// TerrainLayers, and that now goes through EmergenceAssetCatalog (D-137's school: resolve in the
// editor, load through Resources, one component runs in both vehicles).
//
// THIS IS ONE LAW, NOT A COPY. WorldDresser calls straight into it, so the editor dressing and the
// player's world are built by the same code and cannot drift. Presentation-only (D-078 r4): reads
// the applied snapshot, writes nothing back, consumes no sim RNG — all variation is Perlin/hash.
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3TerrainBuilder
    {
        public const float TileSize = 8f;        // metres per sim tile — must match WorldDresser.TileSize
        public const float TerrainHeight = 72f;  // vertical extent of the TerrainData
        public const float TerrainDropY = -3f;   // the dresser's ground offset

        /// <summary>Diagnostics from the last build, so a probe can assert on the ground instead of guessing.</summary>
        public static string LastDiag = "";
        public static float LastMinH, LastMaxH;   // metres, world space — the relief actually built
        public static int LastLayerCount;

        /// <summary>Build the world's terrain from an applied snapshot. Returns the Terrain GameObject.
        /// Idempotent by caller contract: destroy any previous "Terrain" object first.</summary>
        public static GameObject Build(WorldState S, Transform parent, bool withMeadow = true)
        {
            if (S == null || S.W <= 0 || S.H <= 0) return null;

            var data = new TerrainData { heightmapResolution = 257 };
            data.size = new Vector3(S.W * TileSize, TerrainHeight, S.H * TileSize);

            BuildHeights(S, data);
            var idx = BuildLayers(data);
            BuildAlphamap(S, data, idx);
            // D-215c: THE MEADOW MOVED, and the move is the point. It used to be planted AFTER
            // Terrain.CreateTerrainGameObject, and a Terrain caches its detail patches when it is
            // created. Flush() rebuilds the surface but not, reliably, patches that did not exist
            // when the component woke. So a million blades sat in the TerrainData and none stood in
            // the world: planted, measured, counted, invisible. Every defect this pass has had the
            // same shape - the law was right and ran at the wrong moment.
            if (withMeadow) Meadow(S, data);

            var tgo = Terrain.CreateTerrainGameObject(data);
            tgo.name = "Terrain";
            if (parent != null) tgo.transform.SetParent(parent, true);
            tgo.transform.position = new Vector3(0f, TerrainDropY, 0f);

            var terrain = tgo.GetComponent<Terrain>();
            // D-120: a fresh material instance so the splat keywords rebind to THIS terrain; >4 layers
            // needs URP's 8-layer path or layers 5-8 silently do not render.
            var sh = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (sh != null) terrain.materialTemplate = new Material(sh) { name = "EmergenceTerrainLit" };
            if (terrain.materialTemplate != null && data.terrainLayers.Length > 4)
                terrain.materialTemplate.EnableKeyword("_TERRAIN_8_LAYERS");
            terrain.drawInstanced = true;

            // D-215b: the detail meshes were built but never seen at eye level. Unity's defaults are
            // 80 m of detail distance and a density the pack's grass disappears at; state both rather
            // than inherit them, and let the grass reach the middle distance where the eye reads it.
            terrain.detailObjectDistance = 220f;
            terrain.detailObjectDensity = 1f;
            // D-215c: the meadow measured 1,3 MILLION planted instances and could still not be seen.
            // Terrain details are gated a second time by the QUALITY level, and an unattended editor
            // or a low preset can hold that scale near zero — at which point Unity draws nothing and
            // reports nothing. State it rather than inherit it; the probe now prints what it reads.
            QualitySettings.terrainDetailDensityScale = 1f;
            if (QualitySettings.terrainDetailDistance < 200f) QualitySettings.terrainDetailDistance = 220f;
            terrain.treeDistance = 500f;
            terrain.heightmapPixelError = 3f;

            data.SetBaseMapDirty();
            terrain.Flush();

            LastLayerCount = data.terrainLayers.Length;
            LastDiag = "terrain: " + S.W + "x" + S.H + " tiles -> " + data.size.x + "x" + data.size.z + " m, "
                     + "relief " + LastMinH.ToString("F1") + ".." + LastMaxH.ToString("F1") + " m ("
                     + (LastMaxH - LastMinH).ToString("F1") + " m of it), layers=" + LastLayerCount
                     + ", details=" + data.detailPrototypes.Length
                     + (string.IsNullOrEmpty(MeadowNote) ? "" : "\n     meadow: " + MeadowNote);
            return tgo;
        }

        // ---- the height law (D-215: rebuilt after the eye-level pictures showed mesas) ----
        //
        // Three faults were visible in Reports/ground-eye-level.png and all three had one shape in
        // common: a tile type is a STEP FUNCTION, and a step in a heightmap is a cliff.
        //   1. village pads levelled toward a FIXED 0.22 with a squared falloff over 6 tiles. Where
        //      the land around them sat higher you got a flat tan disc with a wall round it —
        //      Patrik's sandcastles. The pad was right; levelling to a global constant was wrong.
        //   2. 's' tiles were lifted +0.04 (2,9 m) and 'w' dropped -0.08 (5,8 m) per texel, so every
        //      stone region was a flat-topped mesa and every shore a kerb.
        //   3. the broad octave was too quiet to read. Perlin lives in roughly 0,25..0,75, so an
        //      amplitude of 0.24 only ever spent half of itself: 800 m of land moved 17,9 m. A lawn.
        // The remedy is the same in all three cases — SMOOTH the tile fields before they touch the
        // height, level a village to the LAND'S OWN height there instead of a constant, stretch the
        // noise across its full range, and blur the finished map once.

        /// <summary>Perlin lives in ~0,25..0,75. Stretch it, or half the amplitude is never spent.</summary>
        static float N(float v) { return Mathf.Clamp01((v - 0.25f) * 2f); }

        /// <summary>The bare land, before anything people did to it. In normalised terrain height.</summary>
        static float NoiseH(float sx, float sy, float vseed, float vseed2)
        {
            float n1 = N(Mathf.PerlinNoise(sx * 0.013f + vseed, sy * 0.013f + 3.1f));    // broad hills ~600 m
            float n2 = N(Mathf.PerlinNoise(sx * 0.038f + 11.7f, sy * 0.038f + vseed2));  // mid rolls  ~210 m
            float n3 = N(Mathf.PerlinNoise(sx * 0.100f + 7.3f, sy * 0.100f + 5.9f));     // undulation  ~80 m
            return 0.16f + 0.340f * n1 + 0.115f * n2 + 0.035f * n3;
        }

        /// <summary>A 0..1 field, one cell per sim tile, 1 where the tile is one of these kinds.</summary>
        static float[] TileField(WorldState S, params char[] kinds)
        {
            var f = new float[S.W * S.H];
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    char t = Tile(S, x, y);
                    for (int k = 0; k < kinds.Length; k++) if (t == kinds[k]) { f[y * S.W + x] = 1f; break; }
                }
            return f;
        }

        /// <summary>Separable box blur, in place. This is the line that turns a kerb into a beach.</summary>
        static void Blur(float[] f, int W, int H, int r, int passes)
        {
            var tmp = new float[f.Length];
            for (int p = 0; p < passes; p++)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float s = 0f; int n = 0;
                        for (int dx = -r; dx <= r; dx++) { s += f[y * W + Mathf.Clamp(x + dx, 0, W - 1)]; n++; }
                        tmp[y * W + x] = s / n;
                    }
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                    {
                        float s = 0f; int n = 0;
                        for (int dy = -r; dy <= r; dy++) { s += tmp[Mathf.Clamp(y + dy, 0, H - 1) * W + x]; n++; }
                        f[y * W + x] = s / n;
                    }
            }
        }

        /// <summary>Bilinear read of a per-tile field at fractional tile coordinates.</summary>
        static float Sample(float[] f, int W, int H, float sx, float sy)
        {
            sx = Mathf.Clamp(sx, 0f, W - 1.001f); sy = Mathf.Clamp(sy, 0f, H - 1.001f);
            int x0 = (int)sx, y0 = (int)sy, x1 = Mathf.Min(x0 + 1, W - 1), y1 = Mathf.Min(y0 + 1, H - 1);
            float fx = sx - x0, fy = sy - y0;
            return Mathf.Lerp(Mathf.Lerp(f[y0 * W + x0], f[y0 * W + x1], fx),
                              Mathf.Lerp(f[y1 * W + x0], f[y1 * W + x1], fx), fy);
        }

        /// <summary>Iron the ground flat where people build — but toward the land's OWN height at the
        /// green, never a global constant, so a building pad reads as a terrace and never as a mesa.</summary>
        static float VillagePad(WorldState S, float sx, float sy, float vseed, float vseed2, float h)
        {
            if (S == null || S.villages == null || S.villages.Length == 0) return h;
            const float R = 11f;                       // tiles: ~3 dead flat, then 8 of easing
            float bw = 0f, bh = h;
            foreach (var v in S.villages)
            {
                if (v == null) continue;
                float d = Mathf.Sqrt((v.x - sx) * (v.x - sx) + (v.y - sy) * (v.y - sy));
                float t = Mathf.Clamp01((d - 3f) / (R - 3f));
                float w = 1f - t * t * (3f - 2f * t);  // smoothstep, not a squared falloff
                if (w > bw) { bw = w; bh = NoiseH(v.x, v.y, vseed, vseed2); }
            }
            return Mathf.Lerp(h, bh, bw);
        }

        static void SmoothHeights(float[,] h, int res)
        {
            var t = new float[res, res];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    int x0 = Mathf.Max(0, x - 1), x1 = Mathf.Min(res - 1, x + 1);
                    int y0 = Mathf.Max(0, y - 1), y1 = Mathf.Min(res - 1, y + 1);
                    t[y, x] = (h[y0, x0] + h[y0, x] + h[y0, x1]
                             + h[y, x0] + h[y, x] * 2f + h[y, x1]
                             + h[y1, x0] + h[y1, x] + h[y1, x1]) / 10f;
                }
            for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) h[y, x] = t[y, x];
        }

        static void BuildHeights(WorldState S, TerrainData data)
        {
            int res = data.heightmapResolution;
            float vseed = S.seed % 991 * 0.137f, vseed2 = S.seed % 733 * 0.171f;

            // the tile fields, blurred before they are allowed anywhere near the height
            var stone = TileField(S, 's', 'i');
            var water = TileField(S, 'w');
            Blur(stone, S.W, S.H, 2, 2);
            Blur(water, S.W, S.H, 3, 2);
            // D-223: the paint needs the same blurred water field the height used. Recomputing it
            // there would be a second law that could drift from this one; handing the array over
            // keeps the shore and the basin derived from ONE field.
            LastWater = water;

            var heights = new float[res, res];
            for (int ry = 0; ry < res; ry++)
                for (int rx = 0; rx < res; rx++)
                {
                    float sx = rx / (float)(res - 1) * (S.W - 1);
                    float sy = (1f - ry / (float)(res - 1)) * (S.H - 1);
                    float h = NoiseH(sx, sy, vseed, vseed2);
                    h = VillagePad(S, sx, sy, vseed, vseed2, h);
                    h += 0.030f * Sample(stone, S.W, S.H, sx, sy);   // stone ground stands a touch proud
                    h -= 0.075f * Sample(water, S.W, S.H, sx, sy);   // ponds and rivers lie in a basin
                    heights[ry, rx] = Mathf.Clamp01(h);
                }

            SmoothHeights(heights, res);

            float lo = 1f, hi = 0f;
            for (int ry = 0; ry < res; ry++)
                for (int rx = 0; rx < res; rx++)
                {
                    float h = heights[ry, rx];
                    if (h < lo) lo = h;
                    if (h > hi) hi = h;
                }
            data.SetHeights(0, 0, heights);
            LastMinH = lo * TerrainHeight; LastMaxH = hi * TerrainHeight;
        }

        /// <summary>The blurred water field from the last height build. The basin was carved from it,
        /// the shore is painted from it, and the water SURFACE is now shaped by it — one field, three
        /// consumers, so they cannot disagree about where the lake is.</summary>
        public static float[] LastWater { get; private set; }

        /// <summary>Bilinear read of the blurred water field, for anyone shaping water.</summary>
        public static float WaterAt(WorldState S, float sx, float sy)
            => LastWater == null ? 0f : Sample(LastWater, S.W, S.H, sx, sy);

        // ---- layers, from the catalog instead of AssetDatabase ----
        public struct LayerIndex { public int grass, field, path, gravel, cobble; }

        static LayerIndex BuildLayers(TerrainData data)
        {
            var cat = EmergenceAssetCatalog.Load();
            var layers = new List<TerrainLayer>();
            var idx = new LayerIndex
            {
                grass  = Add(cat, layers, new[] { "Layer_Grass", "Layer_grass_01" }, new Color(0.35f, 0.5f, 0.22f)),
                field  = Add(cat, layers, new[] { "Layer_farmfield", "Layer_Dirt" }, new Color(0.45f, 0.35f, 0.2f)),
                path   = Add(cat, layers, new[] { "Layer_Dirt" }, new Color(0.42f, 0.32f, 0.2f)),
                gravel = Add(cat, layers, new[] { "Layer_gravel_01", "Layer_Rock", "Layer_Stone", "Layer_rock_01" }, new Color(0.5f, 0.48f, 0.45f)),
                cobble = Add(cat, layers, new[] { "Layer_pavingstone_01", "Layer_Cobblestone", "Layer_Dirt" }, new Color(0.55f, 0.53f, 0.5f)),
            };
            data.terrainLayers = layers.ToArray();
            return idx;
        }

        static int Add(EmergenceAssetCatalog cat, List<TerrainLayer> layers, string[] candidates, Color fallback)
        {
            var tl = cat != null ? cat.TerrainLayer(candidates) : null;
            if (tl == null)
                // never a hard failure: a flat-colour layer keeps the ground readable and the diag says so
                tl = new TerrainLayer { diffuseTexture = Texture2D.whiteTexture, diffuseRemapMax = new Vector4(fallback.r, fallback.g, fallback.b, 1) };
            layers.Add(tl);
            return layers.Count - 1;
        }

        // ---- the ground painting law (D-101/D-115, extended D-215 with SLOPE) ----
        //
        // The old law painted by tile type alone, so a 20-degree bank wore the same lawn as the
        // meadow floor and the eye read the whole map as a billiard table. Grass does not grow on
        // ground it would slide off. Steepness now overrides the tile: bare gravel begins to show
        // through at 18 degrees and owns the face by 40. It costs one GetSteepness per texel and it
        // is the single largest readability gain in the pass.
        static void BuildAlphamap(WorldState S, TerrainData data, LayerIndex L)
        {
            // 256 texels over 100 tiles is 2,5 per tile: every painted region came out as a rectangle
            // with 8 m steps, which is the blockiness visible around the village in house-in-scene.png.
            const int A = 512;
            data.alphamapResolution = A;
            int n = data.terrainLayers.Length;
            var am = new float[A, A, n];
            for (int ay = 0; ay < A; ay++)
                for (int ax = 0; ax < A; ax++)
                {
                    float sx = ax / (float)(A - 1) * (S.W - 1);
                    float sy = (1f - ay / (float)(A - 1)) * (S.H - 1);
                    // DOMAIN WARP: read the tile map at a wandering offset instead of straight.
                    // The map is a grid; ground is not. Displacing the lookup by ~2 tiles of Perlin
                    // makes the edge of a field or a stone patch meander the way a real one does,
                    // and costs two noise samples. (The height law is already smooth, so only the
                    // PAINT needs this.)
                    float wx = (Mathf.PerlinNoise(sx * 0.14f + 61f, sy * 0.14f + 17f) - 0.5f) * 4.0f;
                    float wy = (Mathf.PerlinNoise(sx * 0.14f + 5f, sy * 0.14f + 83f) - 0.5f) * 4.0f;
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx + wx), 0, S.W - 1);
                    int ty = Mathf.Clamp(Mathf.RoundToInt(sy + wy), 0, S.H - 1);
                    char tt = Tile(S, tx, ty);

                    float g = 0f, fi = 0f, pa = 0f, gr = 0f, co = 0f;
                    if (tt == 's' || tt == 'i')
                    {
                        float f = Mathf.PerlinNoise(sx * 0.3f + 5f, sy * 0.3f + 11f);
                        gr = 0.55f + f * 0.25f; g = 0.25f; pa = 0.20f - f * 0.10f;
                    }
                    else if (tt == 'c')
                    {
                        // the trodden green: worn earth in the middle, grass surviving at the edges,
                        // cobble where the feet go. Not the old flat 100% dirt disc.
                        // D-215b: 0.58 of bare path made the common read as a sand pit at eye level.
                        // A green people walk on is worn, not stripped: grass survives between the paths.
                        float w = Mathf.PerlinNoise(sx * 0.5f + 31f, sy * 0.5f + 13f);
                        pa = 0.40f + w * 0.16f; co = 0.12f; g = 0.48f - w * 0.14f;
                    }
                    else if (tt == 'a')
                    {
                        float w = Mathf.PerlinNoise(sx * 0.6f + 3f, sy * 0.6f + 27f);
                        fi = 0.74f + w * 0.14f; pa = 0.14f; g = 0.12f - w * 0.06f;   // ploughed field
                    }
                    else
                    {
                        // break the uniform "billiard green": worn-earth patches + faint rock flecks
                        // D-215c: at 0.09 the worn-earth octave has a ~90 m wavelength, so one patch
                        // filled the whole foreground and read as a beach. Halve the wavelength, raise the
                        // threshold, and CAP the wear — meadow that has been walked thin still has grass
                        // in it. A fully bare patch in a meadow is a quarry.
                        float patch = Mathf.PerlinNoise(sx * 0.19f + 21f, sy * 0.19f + 9f);
                        float fleck = Mathf.PerlinNoise(sx * 0.23f + 4f, sy * 0.23f + 17f);
                        float wDirt = patch > 0.74f ? Mathf.Min(0.72f, (patch - 0.74f) * 2.4f) : 0f;
                        float wRock = fleck > 0.78f ? Mathf.Clamp01((fleck - 0.78f) * 2.2f) : 0f;
                        g = Mathf.Max(0f, 1f - wDirt - wRock); pa = wDirt; gr = wRock;
                    }

                    // D-223: THE SHORE. Water met grass at a hard line — the lake read as a sheet of
                    // paper laid on a lawn, which is what eye-at-the-water.png shows. A real shore is
                    // the band where the water HAS BEEN: bleached, bare, and widest where the basin
                    // is shallowest. The blurred water field already describes exactly that band, so
                    // the beach costs one lookup and no new law: fully wet in the middle, fully green
                    // beyond the rim, and pale shingle in between.
                    if (LastWater != null)
                    {
                        float wet = Sample(LastWater, S.W, S.H, sx, sy);
                        float shore = wet > 0.04f && wet < 0.62f
                            ? Mathf.Sin(Mathf.Clamp01((wet - 0.04f) / 0.58f) * Mathf.PI) : 0f;
                        if (shore > 0.001f)
                        {
                            float keepS = 1f - shore * 0.85f;
                            g *= keepS; fi *= keepS; pa *= keepS; co *= keepS;
                            gr = gr * keepS + shore * 0.62f;
                            fi += shore * 0.23f;          // a warm sand cast under the shingle
                        }
                    }

                    // slope wins over the tile: a bank is bare, whatever the map says grows there
                    float steep = data.GetSteepness(ax / (float)(A - 1), 1f - ay / (float)(A - 1));
                    float rock = Mathf.Clamp01((steep - 18f) / 22f);
                    if (rock > 0.001f)
                    {
                        float keep = 1f - rock;
                        g *= keep; fi *= keep; pa *= keep; co *= keep;
                        gr = gr * keep + rock;
                    }

                    float sum = g + fi + pa + gr + co;
                    if (sum <= 0.0001f) { g = 1f; sum = 1f; }
                    am[ay, ax, L.grass]  += g  / sum;
                    am[ay, ax, L.field]  += fi / sum;
                    am[ay, ax, L.path]   += pa / sum;
                    am[ay, ax, L.gravel] += gr / sum;
                    am[ay, ax, L.cobble] += co / sum;
                }
            data.SetAlphamaps(0, 0, am);
        }

        // ---- the meadow: detail grass + wildflowers, prototypes from the catalog ----
        /// <summary>Why the meadow will or will not be drawn, in Unity's own words.</summary>
        public static string MeadowNote = "";

        static void Meadow(WorldState S, TerrainData data)
        {
            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) return;
            string[] names = { "Prefab_Grass_01_Detail", "Prefab_Grass_Group_01_Detail", "Prefab_Grass_03_Detail",
                               "SM_Flower_01_Unity", "Prefab_Flower_02", "Prefab_Flower_04" };
            Color[] greens = { new Color(0.82f, 0.95f, 0.70f), new Color(0.68f, 0.86f, 0.55f), new Color(0.90f, 0.93f, 0.72f) };
            var dps = new List<DetailPrototype>(); var isFlower = new List<bool>(); int gi = 0;
            MeadowNote = "";
            foreach (var nm in names)
            {
                var pf = cat.Prefab(nm);
                if (pf == null) continue;
                bool flower = nm.ToLower().Contains("flower");

                // D-215d — THE MEADOW, THIRD ATTEMPT, AND THIS TIME BY ELIMINATION RATHER THAN HOPE.
                // A million blades were planted (measured), the quality gate was open (measured), the
                // terrain was told to draw foliage at 220 m (measured), the prototypes had meshes
                // (measured), and the ground was still bare texture at three metres. Instanced and
                // non-instanced VertexLit both drew nothing. What all of those share is Unity's MESH
                // detail path, which in URP hands the prototype's own material to an internal detail
                // shader — and the pack's foliage shaders are not that. GrassBillboard is the one
                // detail path URP is documented to draw unconditionally: it wants a TEXTURE, not a
                // material, so nothing can silently fail to bind. The look changes (camera-facing
                // quads rather than modelled clumps) and for meadow grass that is the standard trade.
                // The mesh path stays as the fallback when a prototype has no texture to lift.
                Texture2D tex = null;
                var mr = pf.GetComponentInChildren<MeshRenderer>(true);
                if (mr != null && mr.sharedMaterial != null) tex = mr.sharedMaterial.mainTexture as Texture2D;

                dps.Add(new DetailPrototype
                {
                    prototype = tex != null ? null : pf,
                    prototypeTexture = tex,
                    usePrototypeMesh = tex == null,
                    useInstancing = false,
                    renderMode = tex != null ? DetailRenderMode.GrassBillboard : DetailRenderMode.VertexLit,
                    minWidth = flower ? 0.8f : 0.9f, maxWidth = flower ? 1.3f : 1.7f,
                    minHeight = flower ? 0.8f : 1.0f, maxHeight = flower ? 1.3f : 1.9f,
                    noiseSpread = flower ? 2.5f : 1.4f,
                    healthyColor = flower ? Color.white : greens[gi % greens.Length],
                    dryColor = flower ? new Color(0.95f, 0.9f, 0.7f) : new Color(0.80f, 0.78f, 0.48f, 1f)
                });
                isFlower.Add(flower);
                if (!flower) gi++;
            }
            if (dps.Count == 0) return;
            data.detailPrototypes = dps.ToArray();

            // ASK UNITY, DO NOT ASSUME. DetailPrototype.Validate says out loud why a prototype will
            // not be drawn; three cycles of this hunt would have been one if it had been called first.
            for (int v = 0; v < data.detailPrototypes.Length; v++)
            {
                string err;
                bool ok = data.detailPrototypes[v].Validate(out err);
                MeadowNote += (v > 0 ? " | " : "") + names[Mathf.Min(v, names.Length - 1)]
                            + (ok ? " ok/" + data.detailPrototypes[v].renderMode
                                  : " INVALID: " + err);
            }
            const int D = 512;
            data.SetDetailResolution(D, 16);
            for (int p = 0; p < dps.Count; p++)
            {
                var map = new int[D, D];
                bool flower = isFlower[p];
                for (int dy = 0; dy < D; dy++)
                    for (int dx = 0; dx < D; dx++)
                    {
                        float sx = dx / (float)(D - 1) * (S.W - 1);
                        float sy = (1f - dy / (float)(D - 1)) * (S.H - 1);
                        int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                        char tt = Tile(S, tx, ty);
                        if (tt != 'g' && tt != 'f') continue;      // meadow and forest floor only
                        float nz = Mathf.PerlinNoise(sx * 0.35f + 3f + p * 1.7f, sy * 0.35f + 7f);
                        if (flower) map[dy, dx] = nz > 0.72f ? 1 : 0;
                        else map[dy, dx] = nz > 0.35f ? (nz > 0.7f ? 3 : 2) : 1;
                    }
                data.SetDetailLayer(0, 0, p, map);
            }
        }

        // ---- shared readers ----
        public static char Tile(WorldState S, int x, int y)
        {
            if (S?.tileTypes == null || S.tileTypes.Length == 0) return 'g';
            int i = y * S.W + x;
            return i >= 0 && i < S.tileTypes.Length ? S.tileTypes[i] : 'g';
        }

        /// <summary>1 at a village centre, falling off with distance — the flat building pads.</summary>
        public static float VillageFlatten(WorldState S, float sx, float sy)
        {
            if (S?.villages == null || S.villages.Length == 0) return 0f;
            float best = 0f;
            foreach (var v in S.villages)
            {
                if (v == null) continue;
                float d = Mathf.Sqrt((v.x - sx) * (v.x - sx) + (v.y - sy) * (v.y - sy));
                float w = Mathf.Clamp01(1f - d / 6f);   // ~6 tiles of levelling around each green
                if (w > best) best = w;
            }
            return best * best;                        // squared falloff — the dresser's exact law
        }
    }
}
