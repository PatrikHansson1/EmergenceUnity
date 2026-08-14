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

            if (withMeadow) Meadow(S, data);

            data.SetBaseMapDirty();
            terrain.Flush();

            LastLayerCount = data.terrainLayers.Length;
            LastDiag = "terrain: " + S.W + "x" + S.H + " tiles -> " + data.size.x + "x" + data.size.z + " m, "
                     + "relief " + LastMinH.ToString("F1") + ".." + LastMaxH.ToString("F1") + " m ("
                     + (LastMaxH - LastMinH).ToString("F1") + " m of it), layers=" + LastLayerCount
                     + ", details=" + data.detailPrototypes.Length;
            return tgo;
        }

        // ---- the height law (verbatim from the dresser, D-101b) ----
        static void BuildHeights(WorldState S, TerrainData data)
        {
            int res = data.heightmapResolution;
            float vseed = S.seed % 991 * 0.137f, vseed2 = S.seed % 733 * 0.171f;
            var heights = new float[res, res];
            float lo = 1f, hi = 0f;
            for (int ry = 0; ry < res; ry++)
                for (int rx = 0; rx < res; rx++)
                {
                    float sx = rx / (float)(res - 1) * (S.W - 1);
                    float sy = (1f - ry / (float)(res - 1)) * (S.H - 1);
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                    float n1 = Mathf.PerlinNoise(sx * 0.018f + vseed, sy * 0.018f + 3.1f);   // broad hills
                    float n2 = Mathf.PerlinNoise(sx * 0.045f + 11.7f, sy * 0.045f + vseed2); // mid rolls
                    float n3 = Mathf.PerlinNoise(sx * 0.11f + 7.3f, sy * 0.11f + 5.9f);      // fine undulation
                    float baseH = 0.13f + 0.24f * n1 + 0.10f * n2 + 0.03f * n3;
                    baseH = Mathf.Lerp(baseH, 0.22f, VillageFlatten(S, sx, sy));  // level building pads
                    char tt = Tile(S, tx, ty);
                    if (tt == 'w') baseH -= 0.08f;        // ponds/rivers sit below the meadow
                    else if (tt == 's') baseH += 0.04f;   // stone ground stands a touch proud
                    heights[ry, rx] = baseH;
                    if (baseH < lo) lo = baseH;
                    if (baseH > hi) hi = baseH;
                }
            data.SetHeights(0, 0, heights);
            LastMinH = lo * TerrainHeight; LastMaxH = hi * TerrainHeight;
        }

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
                gravel = Add(cat, layers, new[] { "Layer_Rock", "Layer_gravel_01" }, new Color(0.5f, 0.48f, 0.45f)),
                cobble = Add(cat, layers, new[] { "Layer_Cobblestone", "Layer_pavingstone_01" }, new Color(0.55f, 0.53f, 0.5f)),
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

        // ---- the ground painting law (verbatim from the dresser, D-101/D-115) ----
        static void BuildAlphamap(WorldState S, TerrainData data, LayerIndex L)
        {
            const int A = 256;
            data.alphamapResolution = A;
            int n = data.terrainLayers.Length;
            var am = new float[A, A, n];
            for (int ay = 0; ay < A; ay++)
                for (int ax = 0; ax < A; ax++)
                {
                    float sx = ax / (float)(A - 1) * (S.W - 1);
                    float sy = (1f - ay / (float)(A - 1)) * (S.H - 1);
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                    char tt = Tile(S, tx, ty);
                    if (tt == 's' || tt == 'i')
                    {
                        float f = Mathf.PerlinNoise(sx * 0.3f + 5f, sy * 0.3f + 11f);
                        am[ay, ax, L.gravel] = 0.55f + f * 0.25f;
                        am[ay, ax, L.grass] = 0.25f;
                        am[ay, ax, L.path] = 0.20f - f * 0.10f;
                    }
                    else if (tt == 'a' || tt == 'c') { am[ay, ax, L.path] = 1f; }
                    else
                    {
                        // break the uniform "billiard green": worn-earth patches + faint rock flecks
                        float patch = Mathf.PerlinNoise(sx * 0.09f + 21f, sy * 0.09f + 9f);
                        float fleck = Mathf.PerlinNoise(sx * 0.23f + 4f, sy * 0.23f + 17f);
                        float wDirt = patch > 0.66f ? Mathf.Clamp01((patch - 0.66f) * 2.6f) : 0f;
                        float wRock = fleck > 0.80f ? Mathf.Clamp01((fleck - 0.80f) * 2.2f) : 0f;
                        am[ay, ax, L.grass] = Mathf.Max(0f, 1f - wDirt - wRock);
                        am[ay, ax, L.path] += wDirt;
                        am[ay, ax, L.gravel] += wRock;
                    }
                }
            data.SetAlphamaps(0, 0, am);
        }

        // ---- the meadow: detail grass + wildflowers, prototypes from the catalog ----
        static void Meadow(WorldState S, TerrainData data)
        {
            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) return;
            string[] names = { "Prefab_Grass_01_Detail", "Prefab_Grass_Group_01_Detail", "Prefab_Grass_03_Detail",
                               "SM_Flower_01_Unity", "Prefab_Flower_02", "Prefab_Flower_04" };
            Color[] greens = { new Color(0.82f, 0.95f, 0.70f), new Color(0.68f, 0.86f, 0.55f), new Color(0.90f, 0.93f, 0.72f) };
            var dps = new List<DetailPrototype>(); var isFlower = new List<bool>(); int gi = 0;
            foreach (var nm in names)
            {
                var pf = cat.Prefab(nm);
                if (pf == null) continue;
                bool flower = nm.ToLower().Contains("flower");
                dps.Add(new DetailPrototype
                {
                    prototype = pf, usePrototypeMesh = true, useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
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
