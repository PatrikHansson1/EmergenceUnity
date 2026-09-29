// EMERGENCE — CodexPlacements (D-922, review D-919 C4): the codex could only say WHERE in two words —
// `edge` (a ring 5 tiles out) and `green` (a ring 2.4 tiles out) — so every village read as a plane with a
// ring around it. These four ask the WORLD where a thing belongs, and answer honestly when the world has no
// such place (spec 5b.1: the codex tells what it cannot show):
//   water — the nearest shore within reach: the object stands on the land tile that touches the water.
//   road  — beside a living road link (Fas3RoadPainter.Links): a third of the way out, a little off the track.
//   hill  — the highest ground within 8 tiles; on flat land it falls back to the edge ring (a shrine on a
//           meadow is not a lie, a bridge on grass is).
//   field — beside the village's nearest field: a granary or scarecrow waits for farming by PLACEMENT alone.
// ONE implementation for the live reconciler and the editor dresser (codex-vaktare law 7: one gate, shared).
// Presentation-only, deterministic (hash + applied state + baked terrain), never sim-RNG.
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class CodexPlacements
    {
        const float TileSize = 8f;   // parity with the reconcilers
        public const float WaterReach = 15f, FieldReach = 12f, HillReach = 8f, RoadReach = 40f;

        public static bool IsWorldPlacement(string p) => p == "water" || p == "road" || p == "hill" || p == "field";

        /// <summary>Ring placement — the original grammar (edge 5.0 / green 2.4 / default 3.5), hash-angled.</summary>
        public static Vector2 Ring(WorldVillage v, CodexEntry e, int k, int cnt, float r)
        {
            float baseAng = (Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length * 7) % 360u) * Mathf.Deg2Rad;
            float ang = baseAng + (cnt > 1 ? k * (6.2832f / cnt) : 0f);
            return new Vector2(v.x + Mathf.Cos(ang) * r, v.y + Mathf.Sin(ang) * r);
        }
        public static float RingRadius(string placement) => placement == "edge" ? 5.0f : placement == "green" ? 2.4f : 3.5f;

        /// <summary>True with a position when the world contains the place this object needs; false = do not place.</summary>
        public static bool TryWorldPlacement(WorldState S, WorldVillage v, CodexEntry e, int k, int cnt, out Vector2 pos)
        {
            pos = Vector2.zero;
            if (S == null || v == null || e == null) return false;
            switch (e.placement)
            {
                case "water": return TryWater(S, v, e, k, out pos);
                case "road":  return TryRoad(S, v, e, k, out pos);
                case "hill":  pos = Hill(S, v, e, k, cnt); return true;
                case "field": return TryField(S, v, e, k, out pos);
            }
            return false;
        }

        // ---- water: nearest shore tile (land touching water) within reach; k-th nearest so two objects never share it
        static bool TryWater(WorldState S, WorldVillage v, CodexEntry e, int k, out Vector2 pos)
        {
            pos = Vector2.zero;
            if (S.tileTypes == null) return false;
            int cx = Mathf.RoundToInt(v.x), cy = Mathf.RoundToInt(v.y), R = Mathf.CeilToInt(WaterReach);
            var found = new System.Collections.Generic.List<(float d, int x, int y)>();
            for (int y = Mathf.Max(0, cy - R); y <= Mathf.Min(S.H - 1, cy + R); y++)
                for (int x = Mathf.Max(0, cx - R); x <= Mathf.Min(S.W - 1, cx + R); x++)
                {
                    if (Tile(S, x, y) == 'w') continue;                       // we stand on land...
                    if (!(Tile(S, x + 1, y) == 'w' || Tile(S, x - 1, y) == 'w' || Tile(S, x, y + 1) == 'w' || Tile(S, x, y - 1) == 'w')) continue;   // ...that touches water
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(v.x, v.y));
                    if (d <= WaterReach && d >= 1.5f) found.Add((d, x, y));
                }
            if (found.Count == 0) return false;
            found.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : (a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x)));
            var t = found[Mathf.Min(k, found.Count - 1)];
            // a hand's breadth back from the water's edge, deterministic
            float jx = (Hash01(t.x, t.y, e.id.Length + 11) - 0.5f) * 0.4f, jy = (Hash01(t.x, t.y, e.id.Length + 13) - 0.5f) * 0.4f;
            pos = new Vector2(t.x + jx, t.y + jy);
            return true;
        }

        // ---- road: beside the k-th nearest living link that touches this village, a third of the way along
        static bool TryRoad(WorldState S, WorldVillage v, CodexEntry e, int k, out Vector2 pos)
        {
            pos = Vector2.zero;
            var links = Fas3RoadPainter.Links;
            if (links == null || links.Count == 0) return false;
            var mine = new System.Collections.Generic.List<(float d, Vector2 a, Vector2 b)>();
            var vp = new Vector2(v.x, v.y);
            foreach (var (a, b) in links)
            {
                float da = Vector2.Distance(a, vp), db = Vector2.Distance(b, vp);
                if (Mathf.Min(da, db) > 2.5f) continue;                       // a link that leaves from here
                var near = da <= db ? a : b; var far = da <= db ? b : a;
                float len = Vector2.Distance(near, far); if (len < 3f || len > RoadReach) continue;
                mine.Add((len, near, far));
            }
            if (mine.Count == 0) return false;
            mine.Sort((p, q) => p.d != q.d ? p.d.CompareTo(q.d) : p.b.x.CompareTo(q.b.x));
            var s = mine[Mathf.Min(k, mine.Count - 1)];
            var dir = (s.b - s.a).normalized; var side = new Vector2(-dir.y, dir.x);
            float t = 0.30f + Hash01(Mathf.RoundToInt(s.b.x), Mathf.RoundToInt(s.b.y), e.id.Length + 17) * 0.15f;   // 30–45 % out
            float sgn = Hash(Mathf.RoundToInt(s.b.x), Mathf.RoundToInt(s.b.y), e.id.Length + 19) % 2u == 0 ? 1f : -1f;
            pos = s.a + dir * (Vector2.Distance(s.a, s.b) * t) + side * (1.2f * sgn);   // 1.2 tiles off the track: beside, not on
            return true;
        }

        // ---- hill: the highest sampled ground within reach; flat land -> the edge ring (never a lie, never a fail)
        static Vector2 Hill(WorldState S, WorldVillage v, CodexEntry e, int k, int cnt)
        {
            var terrain = Terrain.activeTerrain;
            var fallback = Ring(v, e, k, cnt, 6.0f);
            if (terrain == null) return fallback;
            float cx = v.x, cy = v.y;
            float baseH = terrain.SampleHeight(World(S, cx, cy));
            float bestH = baseH; Vector2 best = fallback; bool any = false;
            for (float dy = -HillReach; dy <= HillReach; dy += 1f)
                for (float dx = -HillReach; dx <= HillReach; dx += 1f)
                {
                    float x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x > S.W - 1 || y > S.H - 1) continue;
                    if (dx * dx + dy * dy < 9f || dx * dx + dy * dy > HillReach * HillReach) continue;   // not on the green, not too far
                    int tx = Mathf.RoundToInt(x), ty = Mathf.RoundToInt(y);
                    if (Tile(S, tx, ty) == 'w') continue;
                    float h = terrain.SampleHeight(World(S, x, y));
                    if (h > bestH + 0.001f) { bestH = h; best = new Vector2(x, y); any = true; }
                }
            if (!any || bestH - baseH < 1.5f) return fallback;   // no rise worth the name: the edge ring
            // the k-th object on the same hill steps a little around the crest so they never stack
            float ang = (Hash(Mathf.RoundToInt(best.x), Mathf.RoundToInt(best.y), e.id.Length * 3 + k) % 360u) * Mathf.Deg2Rad;
            return best + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (k > 0 ? 1.2f : 0f);
        }

        // ---- field: beside the k-th nearest field tile, on the village side of it; no fields -> not placed
        static bool TryField(WorldState S, WorldVillage v, CodexEntry e, int k, out Vector2 pos)
        {
            pos = Vector2.zero;
            if (S.fields == null || S.fields.Length == 0) return false;
            var vp = new Vector2(v.x, v.y);
            var near = new System.Collections.Generic.List<(float d, Vector2 p)>();
            foreach (var f in S.fields)
            {
                var fp = new Vector2(Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y));
                float d = Vector2.Distance(fp, vp);
                if (d <= FieldReach) near.Add((d, fp));
            }
            if (near.Count == 0) return false;
            near.Sort((a, b) => a.d != b.d ? a.d.CompareTo(b.d) : (a.p.y != b.p.y ? a.p.y.CompareTo(b.p.y) : a.p.x.CompareTo(b.p.x)));
            var t = near[Mathf.Min(k, near.Count - 1)].p;
            var toV = (vp - t); if (toV.sqrMagnitude < 0.01f) toV = Vector2.up; toV.Normalize();
            pos = t + toV * 0.9f;   // just outside the fence, on the village side
            return true;
        }

        static char Tile(WorldState S, int x, int y)
        {
            if (x < 0 || y < 0 || x >= S.W || y >= S.H) return '?';
            int i = y * S.W + x; return (S.tileTypes != null && i < S.tileTypes.Length) ? S.tileTypes[i] : '?';
        }
        static Vector3 World(WorldState S, float x, float y) => new Vector3(x * TileSize, 0f, (S.H - 1 - y) * TileSize);
        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;
    }
}
