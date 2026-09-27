import re,sys
p='Assets/Emergence/WorldDressing/WorldDresser.cs'
s=open(p,encoding='utf-8').read()
crlf='\r\n' in s
s=s.replace('\r\n','\n')
n0=len(s)

def rep(a,b,cnt=1):
    global s
    assert s.count(a)==cnt, (a[:60], s.count(a))
    s=s.replace(a,b)

# 1) alphamap resolution 256 -> AlphaRes (1024): 3,1 m/cell -> 0,78 m/cell so a trail can be narrower than a house
rep('''            data.alphamapResolution = 256;
            var am = new float[256, 256, layers.Count];
            for (int ay = 0; ay < 256; ay++)
                for (int ax = 0; ax < 256; ax++)
                {
                    float sx = ax / 255f * (S.W - 1);
                    float sy = (1f - ay / 255f) * (S.H - 1);''',
'''            // D-879 (Patrik): roads start as narrow trails and only widen/pave as the civilisation develops. At 256 the
            // alphamap cell was 3,1 m (100 tiles x 8 m / 256) so the thinnest brush was a 9 m band — the "wide roads" seen
            // in D-878. 1024 gives 0,78 m cells: a trail can be ~1,5 m, a cart path ~3 m, a paved street ~4,5 m.
            data.alphamapResolution = AlphaRes;
            var am = new float[AlphaRes, AlphaRes, layers.Count];
            for (int ay = 0; ay < AlphaRes; ay++)
                for (int ax = 0; ax < AlphaRes; ax++)
                {
                    float sx = ax / (float)(AlphaRes - 1) * (S.W - 1);
                    float sy = (1f - ay / (float)(AlphaRes - 1)) * (S.H - 1);''')
rep('''            StampFields(S, am, liField, 256); // TD-031 v2.1b''','''            StampFields(S, am, liField, AlphaRes); // TD-031 v2.1b''')
rep('''            PaintRoutes(S, am, 256, liPath, liCobble); // D-116/120 EMERGENT ROADS: tie-derived, wear→width, tech-gated COBBLE tier''',
'''            PaintRoutes(S, am, AlphaRes, liPath, liCobble); // D-116/120/879 EMERGENT ROADS: tie-derived, tech-tiered trail→path→paved''')
rep('''        public const float TileSize = 8f;          // meters per sim tile (Producer knob)''',
'''        public const float TileSize = 8f;          // meters per sim tile (Producer knob)
        public const int   AlphaRes = 1024;        // D-879: terrain splat resolution (0,78 m/cell at W=100) — trails need it''')

# 2) Route tier from sim state
rep('''        struct Route { public Vector2 a, b; public float wear; public int layer; }''',
'''        struct Route { public Vector2 a, b; public float wear; public int layer; public int tier; } // tier: 0 trail, 1 path, 2 paved (D-879)

        // D-879 (Patrik 2026-09-27): "vägarna kanske ska vara smalare som stigar i början och sen när civ utvecklas så kan
        // dom bli bredare och stenlagda." Tier is READ from the village's tech (sim state), never authored:
        //   TRAIL  — no road tech: a trodden line of worn dirt, ~1,5 m, grass showing through
        //   PATH   — the village knows 'road' or 'wheel': carts widen it to ~3 m of gravel
        //   PAVED  — both ends know 'road' AND 'masonry': a ~4,5 m paved street (pavingstone blended with worn dirt)
        static int RoadTier(WorldVillage v) => v == null ? 0 : (Holds(v, "road") || Holds(v, "wheel")) ? ((Holds(v, "road") && Holds(v, "masonry")) ? 2 : 1) : 0;''')

rep('''                    routes.Add(new Route { a = new Vector2(h.x, h.y), b = g, wear = Mathf.Clamp01(0.30f + pop / 45f), layer = liDirt });''',
'''                    var hv = (vi >= 0 && S.villages != null && vi < S.villages.Length) ? S.villages[vi] : null;
                    int htier = Mathf.Min(RoadTier(hv), 1); // a hut's own lane is never paved — the street is, the doorstep path is not
                    routes.Add(new Route { a = new Vector2(h.x, h.y), b = g, wear = Mathf.Clamp01(0.30f + pop / 45f), layer = liDirt, tier = htier });''')

rep('''                        routes.Add(new Route { a = new Vector2(vi.x, vi.y), b = new Vector2(vj.x, vj.y), wear = wear, layer = liDirt });''',
'''                        int tier = Mathf.Min(RoadTier(vi), RoadTier(vj)); // the road is as developed as its lesser end
                        routes.Add(new Route { a = new Vector2(vi.x, vi.y), b = new Vector2(vj.x, vj.y), wear = wear, layer = tier == 2 ? liCobble : liDirt, tier = tier });''')

# 3) PaintRoutes: width by tier (+wear), soft-edged brush, paved blend
rep('''                if (r.layer == liCobble) cobbleRoutes++;
                int rr = 1 + Mathf.RoundToInt(Mathf.Clamp01(r.wear) * 2f);           // WIDTH by wear: 1..3 cells''',
'''                if (r.layer == liCobble) cobbleRoutes++;
                // D-879 WIDTH by tier: metres → cells (0,78 m/cell at 1024). Trail ~1,5 m · path ~3 m · paved ~4,5 m;
                // a heavily worn route (wear > 0,7) gains one cell. Soft-edged brush so the verge frays into grass.
                float cellM = Mathf.Max(1, S.W - 1) * TileSize / (res - 1);
                float halfW = r.tier == 2 ? 2.25f : r.tier == 1 ? 1.5f : 0.75f;
                int rr = Mathf.Max(1, Mathf.RoundToInt(halfW / cellM)) + (r.wear > 0.7f ? 1 : 0);
                float strength = r.tier == 0 ? 0.8f : 1f;                              // a trail never fully hides the grass''')

rep('''                            if (dx * dx + dy * dy > rr * rr + 1) continue;           // round brush
                            int cx = ax + dx, cy = ay + dy;
                            if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                            for (int l = 0; l < nlayers; l++) am[cy, cx, l] = 0f;
                            // D-120: paved tier = mostly cobble but blended with worn dirt so it reads as a WARM
                            // trodden street, not a stark white checkerboard; dirt/trail tiers = full dirt.
                            if (r.layer == liCobble) { am[cy, cx, liCobble] = 0.4f; am[cy, cx, liDirt] = 0.6f; }
                            else am[cy, cx, r.layer] = 1f;
                            cells++;''',
'''                            float d2 = dx * dx + dy * dy;
                            if (d2 > rr * rr + 1) continue;                          // round brush
                            int cx = ax + dx, cy = ay + dy;
                            if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                            float w = strength * Mathf.Clamp01(1.15f - Mathf.Sqrt(d2) / Mathf.Max(1f, rr)); // 1 at centre → 0 at the verge
                            if (w <= 0.02f) continue;
                            float keep = 1f - w;
                            for (int l = 0; l < nlayers; l++) am[cy, cx, l] *= keep;
                            // D-120/879: paved = pavingstone blended with worn dirt so it reads as a WARM trodden street,
                            // not a stark checkerboard; path/trail = dirt/gravel blended over what was there.
                            if (r.layer == liCobble) { am[cy, cx, liCobble] += 0.55f * w; am[cy, cx, liDirt] += 0.45f * w; }
                            else am[cy, cx, r.layer] += w;
                            cells++;''')

rep('''            Debug.Log($"[Dresser] EMERGENT ROADS: {routes.Count} routes ({cobbleRoutes} paved/cobble), {cells} cells painted — tie-derived, wear→width (D-116)");''',
'''            int t0 = routes.Count(x => x.tier == 0), t1 = routes.Count(x => x.tier == 1);
            Debug.Log($"[Dresser] EMERGENT ROADS: {routes.Count} routes (trail {t0} / path {t1} / paved {cobbleRoutes}), {cells} cells painted at {res} — tie-derived, tech-tiered width (D-116/D-879)");''')

# 4) StampFields: the 3x3 stamp was one tile at 256; keep it one tile at any resolution
rep('''                int ax = Mathf.RoundToInt(f.x / Mathf.Max(1, S.W - 1) * (res - 1));
                int ay = Mathf.RoundToInt((1f - f.y / Mathf.Max(1, S.H - 1)) * (res - 1));
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int cx = ax + dx, cy = ay + dy;
                        if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                        // D-115: tilled soil''',
'''                int ax = Mathf.RoundToInt(f.x / Mathf.Max(1, S.W - 1) * (res - 1));
                int ay = Mathf.RoundToInt((1f - f.y / Mathf.Max(1, S.H - 1)) * (res - 1));
                int rad = Mathf.Max(1, Mathf.RoundToInt(0.5f * (res - 1) / Mathf.Max(1, S.W - 1))); // D-879: half a tile in cells (1 at 256, 5 at 1024)
                for (int dy = -rad; dy <= rad; dy++)
                    for (int dx = -rad; dx <= rad; dx++)
                    {
                        int cx = ax + dx, cy = ay + dy;
                        if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                        // D-115: tilled soil''')

out=s.replace('\n','\r\n') if crlf else s
open(p,'w',encoding='utf-8',newline='').write(out)
print('patched ok', n0, '->', len(s))
