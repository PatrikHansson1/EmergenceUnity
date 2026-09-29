// EMERGENCE — Fas3TroddenPainter (D-921, review D-919 "seen"): the village heart wears to earth LIVE.
// The static dresser painted the y120 footfall (WorldDresser.PaintTrodden, D-879) into the persisted terrain,
// so genesis showed a great trodden clearing before anyone had walked — the same lie about labour as the
// baked fields (D-914), work-marks (D-916) and routes. This paints the engine's own footfall (WorldState.pathUse,
// R2 INK1) into the terrain alphamap as it accumulates: once per applied year, over the bounding box of the
// walked tiles, with the bake's exact law (log-scaled, bilinear over the 8 m tiles, lightly walked grass stays
// grass, the busiest heart ~85 % bare earth, Perlin-mottled). IDEMPOTENT: it raises the path layer to its
// target weight and never compounds, so a yearly repaint over last year's paint is a no-op where nothing grew,
// and the road painter (same layer) is never undone. Presentation-only (D-078 r4): reads applied state,
// deterministic, disarms on any error.
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3TroddenPainter
    {
        public static string LastNote = "";
        public static int TexelsPainted;
        static int _lastYear = int.MinValue;
        static int _lastMax = -1;

        public static void Apply(WorldState S)
        {
            try { ApplyInner(S); }
            catch (Exception e) { LastNote = "trodden FAILED: " + e.Message; Debug.LogWarning("[Fas3TroddenPainter] " + e.Message); }
        }

        static void ApplyInner(WorldState S)
        {
            if (S == null || S.pathUse == null || S.pathUse.Length < S.W * S.H) { LastNote = "trodden: no footfall yet"; return; }
            int max = 0; foreach (var u in S.pathUse) if (u > max) max = u;
            if (max <= 0) { LastNote = "trodden: nobody has walked"; return; }
            if (S.years == _lastYear && max == _lastMax) return;      // once per applied year (or when the busiest tile changes)
            _lastYear = S.years; _lastMax = max;

            var terrain = Terrain.activeTerrain; if (terrain == null) { LastNote = "trodden: no terrain"; return; }
            var data = terrain.terrainData;
            var L = Fas3TerrainBuilder.Layers(data); int n = data.terrainLayers.Length;   // D-924: adopt the dressed terrain's layer order
            if (n == 0 || L.path < 0 || L.path >= n || L.grass < 0 || L.grass >= n) { LastNote = "trodden: no path layer"; return; }
            int A = data.alphamapResolution;

            // wear per tile, and the box of tiles that will actually paint (f >= 0.45 after bilinear can reach one tile out)
            float lmax = Mathf.Log(1f + max);
            var wear = new float[S.H, S.W];
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    int u = S.pathUse[y * S.W + x];
                    float f = u > 0 ? Mathf.Log(1f + u) / lmax : 0f;
                    wear[y, x] = f;
                    if (f >= 0.45f) { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                }
            if (minX == int.MaxValue) { LastNote = "trodden: footfall too light to show"; return; }
            minX = Mathf.Max(0, minX - 1); minY = Mathf.Max(0, minY - 1); maxX = Mathf.Min(S.W - 1, maxX + 1); maxY = Mathf.Min(S.H - 1, maxY + 1);

            // D-924: one map<->tile law (Fas3TerrainBuilder); alphamap y runs opposite the tile map
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Fas3TerrainBuilder.TileToCellX(minX - 0.5f, A, S.W)), 0, A - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Fas3TerrainBuilder.TileToCellX(maxX + 0.5f, A, S.W)), 0, A - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Fas3TerrainBuilder.TileToCellY(maxY + 0.5f, A, S.H)), 0, A - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Fas3TerrainBuilder.TileToCellY(minY - 0.5f, A, S.H)), 0, A - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1; if (w <= 0 || h <= 0) return;

            var am = data.GetAlphamaps(x0, y0, w, h);
            int painted = 0;
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float sx = Fas3TerrainBuilder.CellToTileX(x0 + i, A, S.W);   // D-924
                    float sy = Fas3TerrainBuilder.CellToTileY(y0 + j, A, S.H);
                    int tx0 = Mathf.Clamp((int)sx, 0, S.W - 2), ty0 = Mathf.Clamp((int)sy, 0, S.H - 2);
                    float fx = Mathf.Clamp01(sx - tx0), fy = Mathf.Clamp01(sy - ty0);
                    float f = Mathf.Lerp(Mathf.Lerp(wear[ty0, tx0], wear[ty0, tx0 + 1], fx), Mathf.Lerp(wear[ty0 + 1, tx0], wear[ty0 + 1, tx0 + 1], fx), fy);
                    if (f < 0.45f) continue;                                          // lightly walked grass stays grass
                    float target = Mathf.Clamp01((f - 0.45f) / 0.55f) * 0.85f;         // the busiest heart ~85 % bare earth
                    target *= 0.85f + 0.3f * Mathf.PerlinNoise(sx * 0.7f + 31f, sy * 0.7f + 13f);   // mottled, not a gradient
                    target = Mathf.Clamp01(target);
                    float d = am[j, i, L.path];
                    if (target <= d + 0.01f) continue;                                // idempotent: never lower, never compound
                    float keep = (1f - target) / Mathf.Max(0.001f, 1f - d);
                    for (int l = 0; l < n; l++) if (l != L.path) am[j, i, l] *= keep;
                    am[j, i, L.path] = target;
                    painted++;
                }
            if (painted > 0) data.SetAlphamaps(x0, y0, am);
            TexelsPainted = painted;
            LastNote = "trodden: y" + S.years + " max footfall " + max + ", " + painted + " texels worn (region " + w + "x" + h + ")";
            if (painted > 0) Debug.Log("[Fas3TroddenPainter] " + LastNote);
        }
    }
}
