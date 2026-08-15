// EMERGENCE — FAS 3: THE ROADS THAT APPEAR AS A PEOPLE SPREAD (D-247).
//
// THE MEASUREMENT THAT SET THE DESIGN, before a line of this was written:
//   • The engine exports `pathUse` — cumulative footfall per tile, every year, since R2 ink. 1.
//     It is real, and it is NOT a road: over three seeds at 60 years, HALF of all footfall in a
//     world sits on FOURTEEN TILES, and those fourteen are the village floor. 15–22 % of the map
//     carries some wear, which is the wandering. Painting wear straight from that data gives a
//     doormat with a faint wash around it, never a route between two places.
//   • And zero water tiles carried footfall in any seed, so a bridge could never be justified by
//     wear either. A people who cannot cross do not walk to the far bank and wear a line into it.
//
// So the network is DERIVED from what a civilization actually binds together — hut to its village,
// village to village, village to the water it drinks from — and the ground is worn along those.
// That is a pure read of exported state (positions, and what a village knows). No RNG, no clock,
// no write back into the sim: D-078 r4 holds exactly as it does for every other presentation law.
//
// WHY THIS IS NOT PART OF THE TERRAIN BUILD, which is where it belongs by rights:
// `Fas3WorldRuntime.EnsureGround` builds the terrain ONCE per session, on the first Apply — and at
// that moment the world has no villages and no huts, because nobody has built anything yet. The
// meadow's near-settlement mask learned this the expensive way in D-246 (its density change moved
// the tuft count by exactly zero, because the mask was empty every time). A road painted at build
// time would be a road through an empty map. So the corridors are painted INTO the alphamap as the
// settlements appear, one bounded sub-rectangle at a time, and re-painted only when the set of
// connections actually changes.
#if true
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3RoadPainter
    {
        /// <summary>A village links to another within this many tiles. Beyond it, two settlements are
        /// neighbours on a map and strangers on the ground.</summary>
        public const float LinkTiles = 45f;
        /// <summary>Half-width of a worn corridor, in tiles. A footpath is not a motorway: at 0.55 a
        /// track is about a tile across, which at 8 m per tile is a cart's width plus its verges.</summary>
        public const float TrackHalfWidth = 0.55f;
        /// <summary>How bare the middle of a track goes. A path people walk is worn, never stripped —
        /// the same law the trodden green already follows (D-215b): grass survives between the feet.</summary>
        public const float TrackWear = 0.72f;

        public static string LastNote = "";
        public static int Segments, TexelsPainted, CrossingCount;

        /// <summary>Where a track meets water. The codex places its bridges here — a bridge belongs
        /// at a crossing, and until this existed the only place the codex could put anything was on a
        /// ring around the village centre, which is why eleven of twelve bridge models were unused.</summary>
        public static readonly List<Vector2> Crossings = new List<Vector2>();

        static string _signature = "";

        struct Seg { public Vector2 a, b; public bool paved; }

        /// <summary>Repaint if, and only if, the set of connections changed. Called every Apply.</summary>
        public static void Apply(WorldState S)
        {
            var terrain = Terrain.activeTerrain;
            if (S == null || terrain == null || terrain.terrainData == null) return;
            if (S.W <= 0 || S.H <= 0) return;

            var segs = Build(S, out string sig);
            // The counts go in the note ALWAYS, even when nothing is drawn. The first version of this
            // returned silently on an empty signature and the probe printed a blank line next to
            // "0 links", which says nothing about WHY. A measurement that cannot fail out loud is a
            // measurement that will be misread.
            int nv = S.villages != null ? S.villages.Length : 0;
            int nh = S.huts != null ? S.huts.Length : 0;
            if (sig == _signature) return;
            _signature = sig;
            Segments = segs.Count;
            if (segs.Count == 0)
            {
                LastNote = "roads: nothing to join yet (" + nv + " villages, " + nh + " huts)";
                return;
            }

            try { Paint(S, terrain.terrainData, segs); }
            catch (Exception e) { LastNote = "roads FAILED: " + e.Message; Debug.LogWarning("[Fas3RoadPainter] " + LastNote); }
        }

        // ---- what a civilization binds together ----
        static List<Seg> Build(WorldState S, out string sig)
        {
            var segs = new List<Seg>();
            var sb = new StringBuilder();
            var villages = S.villages ?? new WorldVillage[0];

            // a village is paved when it knows how to make a road. Two villages are only joined by a
            // paved way when BOTH of them do — a road is a thing two peoples agree on, and one people
            // cobbling their half of a track is a claim the world cannot support.
            bool Knows(WorldVillage v, string tech)
                => v?.knows != null && Array.IndexOf(v.knows, tech) >= 0;

            for (int i = 0; i < villages.Length; i++)
            {
                var v = villages[i];
                if (v == null) continue;
                sb.Append(v.name).Append(':').Append(Mathf.RoundToInt(v.x)).Append(',').Append(Mathf.RoundToInt(v.y))
                  .Append(Knows(v, "road") ? "R" : "-").Append(';');

                // village to village — the trade road, and the one a stranger would recognise as a road
                for (int j = i + 1; j < villages.Length; j++)
                {
                    var w = villages[j];
                    if (w == null) continue;
                    if (Vector2.Distance(new Vector2(v.x, v.y), new Vector2(w.x, w.y)) > LinkTiles) continue;
                    segs.Add(new Seg { a = new Vector2(v.x, v.y), b = new Vector2(w.x, w.y),
                                       paved = Knows(v, "road") && Knows(w, "road") });
                }

                // and the way to the water this village lives by — the oldest errand there is
                var water = NearestTile(S, v.x, v.y, 'w', 22f);
                if (water.HasValue)
                    segs.Add(new Seg { a = new Vector2(v.x, v.y), b = water.Value, paved = false });
            }

            // Every hut to its own village -- and, BEFORE a village exists, to its nearest neighbour.
            // The first run of this found 0 links at year 22 with four huts standing, because the
            // engine had not yet named a village and every lane hung off a village that was not there.
            // People wear a path between two houses long before anyone calls the place a village; the
            // hut-to-hut lane is the older truth and it is what the eye in the village actually sees.
            var huts = S.huts ?? new WorldHut[0];
            foreach (var h in huts)
            {
                if (h == null) continue;
                sb.Append('h').Append(Mathf.RoundToInt(h.x)).Append(',').Append(Mathf.RoundToInt(h.y)).Append(';');
                var nearest = Nearest(villages, h.x, h.y);
                if (nearest.HasValue && Vector2.Distance(new Vector2(h.x, h.y), nearest.Value) <= 18f)
                {
                    segs.Add(new Seg { a = new Vector2(h.x, h.y), b = nearest.Value, paved = false });
                    continue;
                }
                // no village yet: join to the closest other hearth within a settlement's reach
                Vector2? mate = null; float bd = 16f;
                foreach (var o in huts)
                {
                    if (o == null || ReferenceEquals(o, h)) continue;
                    float d = Vector2.Distance(new Vector2(h.x, h.y), new Vector2(o.x, o.y));
                    if (d > 0.01f && d < bd) { bd = d; mate = new Vector2(o.x, o.y); }
                }
                if (mate.HasValue) segs.Add(new Seg { a = new Vector2(h.x, h.y), b = mate.Value, paved = false });
            }

            sig = sb.ToString();
            return segs;
        }

        static Vector2? Nearest(WorldVillage[] villages, float x, float y)
        {
            Vector2? best = null; float bd = float.MaxValue;
            foreach (var v in villages)
            {
                if (v == null) continue;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(v.x, v.y));
                if (d < bd) { bd = d; best = new Vector2(v.x, v.y); }
            }
            return best;
        }

        static Vector2? NearestTile(WorldState S, float x, float y, char type, float maxDist)
        {
            int cx = Mathf.RoundToInt(x), cy = Mathf.RoundToInt(y);
            int r = Mathf.CeilToInt(maxDist);
            Vector2? best = null; float bd = maxDist;
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int tx = cx + dx, ty = cy + dy;
                    if (tx < 0 || ty < 0 || tx >= S.W || ty >= S.H) continue;
                    if (Tile(S, tx, ty) != type) continue;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < bd) { bd = d; best = new Vector2(tx, ty); }
                }
            return best;
        }

        static char Tile(WorldState S, int x, int y)
        {
            int i = y * S.W + x;
            return (S.tileTypes != null && i >= 0 && i < S.tileTypes.Length) ? S.tileTypes[i] : 'g';
        }

        // ---- wear the ground along them ----
        static void Paint(WorldState S, TerrainData data, List<Seg> segs)
        {
            var L = Fas3TerrainBuilder.LastLayerIndex;
            int n = data.terrainLayers.Length;
            if (n == 0 || L.path >= n || L.grass >= n) { LastNote = "roads: no layers to paint into"; return; }
            int A = data.alphamapResolution;

            // The bounding box of everything we are about to touch, so one read and one write cover
            // the whole pass. A full 512x512xN read is four megabytes; a village's neighbourhood is a
            // few hundred kilobytes, and this runs whenever a settlement appears.
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var s in segs)
            {
                minX = Mathf.Min(minX, Mathf.Min(s.a.x, s.b.x)); maxX = Mathf.Max(maxX, Mathf.Max(s.a.x, s.b.x));
                minY = Mathf.Min(minY, Mathf.Min(s.a.y, s.b.y)); maxY = Mathf.Max(maxY, Mathf.Max(s.a.y, s.b.y));
            }
            float pad = TrackHalfWidth + 1f;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((minX - pad) / (S.W - 1) * (A - 1)), 0, A - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((maxX + pad) / (S.W - 1) * (A - 1)), 0, A - 1);
            // the alphamap's y runs opposite the tile map's, exactly as BuildAlphamap reads it
            int y0 = Mathf.Clamp(Mathf.FloorToInt((1f - (maxY + pad) / (S.H - 1)) * (A - 1)), 0, A - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt((1f - (minY - pad) / (S.H - 1)) * (A - 1)), 0, A - 1);
            int w = x1 - x0 + 1, h = y1 - y0 + 1;
            if (w <= 0 || h <= 0) { LastNote = "roads: empty region"; return; }

            var am = data.GetAlphamaps(x0, y0, w, h);
            Crossings.Clear();
            int painted = 0;

            foreach (var s in segs)
            {
                float len = Vector2.Distance(s.a, s.b);
                if (len < 0.5f) continue;
                int steps = Mathf.CeilToInt(len * 6f);          // ~6 samples per tile: no gaps at any angle
                bool wasWet = false;
                for (int k = 0; k <= steps; k++)
                {
                    var p = Vector2.Lerp(s.a, s.b, k / (float)steps);

                    // A track stops at the water's edge and starts again on the far bank. Where it
                    // would run INTO the lake is a crossing, and a crossing is where the codex may
                    // stand a bridge. Recorded once per entry into water, not once per sample.
                    int tx = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, S.W - 1);
                    int ty = Mathf.Clamp(Mathf.RoundToInt(p.y), 0, S.H - 1);
                    bool wet = Tile(S, tx, ty) == 'w';
                    if (wet && !wasWet) Crossings.Add(p);
                    wasWet = wet;
                    if (wet) continue;                           // never paint dirt onto open water

                    painted += Stamp(am, S, A, x0, y0, w, h, p, s.paved, L, n);
                }
            }

            data.SetAlphamaps(x0, y0, am);
            TexelsPainted = painted;
            CrossingCount = Crossings.Count;
            LastNote = "roads: " + segs.Count + " links worn into the ground (" + painted + " texels, "
                     + Crossings.Count + " water crossings, region " + w + "x" + h + " of " + A + ")";
            Debug.Log("[Fas3RoadPainter] " + LastNote);
        }

        static int Stamp(float[,,] am, WorldState S, int A, int x0, int y0, int w, int h,
                         Vector2 p, bool paved, Fas3TerrainBuilder.LayerIndex L, int n)
        {
            float texPerTile = (A - 1) / (float)(S.W - 1);
            int rad = Mathf.CeilToInt(TrackHalfWidth * texPerTile);
            int cxT = Mathf.RoundToInt(p.x / (S.W - 1) * (A - 1));
            int cyT = Mathf.RoundToInt((1f - p.y / (S.H - 1)) * (A - 1));
            int hits = 0;
            for (int dy = -rad; dy <= rad; dy++)
                for (int dx = -rad; dx <= rad; dx++)
                {
                    int ax = cxT + dx - x0, ay = cyT + dy - y0;
                    if (ax < 0 || ay < 0 || ax >= w || ay >= h) continue;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Max(1f, rad);
                    if (d > 1f) continue;
                    // soft edge: a track has verges, not kerbs
                    float wear = TrackWear * (1f - d * d);
                    if (wear <= 0.002f) continue;

                    // Never overwrite what is already something else. A road runs over turf; it does
                    // not pave a ploughed field or scrub out a shore the water law just painted.
                    float turf = am[ay, ax, L.grass] + (L.grass2 != L.grass ? am[ay, ax, L.grass2] : 0f);
                    if (turf < 0.25f) continue;
                    float take = Mathf.Min(wear, turf);

                    float g1 = am[ay, ax, L.grass], g2 = L.grass2 != L.grass ? am[ay, ax, L.grass2] : 0f;
                    float tot = Mathf.Max(0.0001f, g1 + g2);
                    am[ay, ax, L.grass] = g1 - take * (g1 / tot);
                    if (L.grass2 != L.grass) am[ay, ax, L.grass2] = g2 - take * (g2 / tot);
                    // bare earth by default; cobble where a people know how to lay a road
                    int lay = paved && L.cobble < n ? L.cobble : L.path;
                    am[ay, ax, lay] += take;
                    hits++;
                }
            return hits;
        }
    }
}
#endif
