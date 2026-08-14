// EMERGENCE — FAS 3 increment 2 (D-134): the LIVE HUT RECONCILER — the village is BORN, not loaded.
//
// Fas 2's AgentReconciler made the population live; this does the same for the built world's first
// layer: it READS S.huts and reconciles "Huts_Live" (+ yards + age marks) incrementally — a new hut
// is RAISED the year the sim builds it, a vanished hut is retired. Mirrors WorldDresser.PlaceHuts
// EXACTLY (variant/yaw/scale/yard/age grammar, same hash salts) so a live-grown village is identical
// to a full-build one.
//
// FAS 3 increment 4 (D-137): PLAYER-RUNTIME REFACTOR. Moved Editor/ -> Runtime/: house variants,
// yard props, age marks and fresh-build props come from EmergenceAssetCatalog (Resources) — the moss
// list is captured by the catalog build IN the editor query's order, so age-mark picks stay
// hash-identical to WorldDresser's. Instantiation is plain Object.Instantiate.
//
// Determinism (D-078 r4): reads state only; every placement decision is Hash(hx,hy,salt) — never
// sim-RNG. Identity = the hut's tile (huts don't move in the sim); owner changes rename in place.
// Events: first hut ever -> Milestone; each raise -> AssetSpawned (Data carries world x/z so the
// gaze director can look without touching editor types); each loss -> AssetRemoved.
// Limitation (logged D-134): age marks are computed at raise time from that year's generations and
// are not re-aged retroactively; fires/fields/smoke stay static dressing (a later increment).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class HutReconciler
    {
        public const string LayerName = "Huts_Live";
        public const string YardLayerName = "Yards_Live";
        public const string AgeLayerName = "HutAge_Live";
        const float TileSize = 8f;                       // matches WorldDresser
        const float HouseScale = 0.55f;                  // matches WorldDresser.HouseScale — now only a fallback

        // D-216: WE WERE RAISING TOWN HALLS AS PEASANT HUTS.
        //
        // The pack ships fourteen whole buildings and this reconciler picked among thirteen of them
        // with hash % 13, as if they were one kind of thing. They are not. Measured by the probe, at
        // the scale we actually raise them (footprint / ridge height / multiples of a 1,75 m person):
        //
        //   house_02   6,8 x 5,2 m   19,8 m   11,3x     house_07  10,4 x 7,8 m   8,0 m   4,6x
        //   house_04   9,6 x 9,4 m   20,9 m   12,0x     house_08   9,0 x 6,3 m   7,8 m   4,4x
        //   house_05  11,0 x 6,4 m   21,0 m   12,0x     house_11   8,7 x 8,3 m   8,2 m   4,7x
        //   house_01   4,8 x 4,5 m   10,2 m    5,8x     house_09   4,8 x 4,5 m   5,7 m   3,3x
        //
        // Four of the thirteen are TWENTY-METRE buildings — a hall, a manor, a bell tower — and they
        // came up as a farmer's first shelter roughly three times in ten. That is the narrow spire
        // standing beside a cottage in Reports/eye-in-the-village.png. It was never a scale bug: it
        // was the wrong building drawn from a bag of right ones.
        //
        // The towers are not deleted. They are the wrong DWELLING and they will be the right
        // something else — a hall, a mill, a temple — when the era law that earns them is built.
        public static readonly int[] DwellingVariants = { 3, 6, 7, 8, 9, 11, 12, 13, 14 };

        // And the survivors still vary from 3,3x to 4,7x a person, which is the difference between a
        // cottage and a barn standing side by side for no reason the world can explain. Normalising
        // to a ridge height removes the pack's authoring inconsistency without touching its shapes:
        // scale each house so its ROOF sits here, and let the footprint follow. A hash gives the
        // village its variety back honestly — a person's house, not a template.
        public const float TargetRidge = 5.8f;           // metres — 3,3x a 1,75 m adult
        public const float RidgeSpread = 0.16f;          // +-16%: a big family's house is bigger
        const float HouseFrontYawOffset = 0f;            // matches WorldDresser
        const int YardPropsMax = 2;                      // matches WorldDresser

        sealed class Rec { public GameObject house; public readonly List<GameObject> extras = new(); public string owner; }
        readonly Dictionary<string, Rec> _huts = new();
        int _tick; bool _firstHutSeen;

        public int Count => _huts.Count;

        public struct Delta
        {
            public int raised, lost, kept, renamed;
            public override string ToString() => $"+{raised} -{lost} ={kept}" + (renamed > 0 ? $" renamed={renamed}" : "");
        }

        /// <summary>Reconcile the live hut layer to this WorldState. Raise + retire + rename only.</summary>
        public Delta Reconcile(WorldState S)
        {
            _tick++;
            var d = new Delta();
            if (S?.huts == null) return d;

            // desired set — identity is the hut's tile (stable; sim huts never move). Duplicate
            // tiles (possible in principle) get a deterministic ordinal by sim array order.
            var desired = new Dictionary<string, WorldHut>();
            foreach (var h in S.huts)
            {
                string key = Key(h);
                int n = 0; string k = key;
                while (desired.ContainsKey(k)) k = key + "#" + (++n);
                desired[k] = h;
            }

            // 1) losses — placed but no longer in the state (ruin-leaving is the Codex onLoss lane)
            foreach (var key in _huts.Keys.Where(k => !desired.ContainsKey(k)).ToList())
            {
                var rec = _huts[key];
                if (rec.house != null) LiveReconciler.Retire(rec.house);
                foreach (var e in rec.extras) if (e != null) LiveReconciler.Retire(e);
                _huts.Remove(key);
                d.lost++;
                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetRemoved, "hut:" + key, -1, "hut-lost"));
            }

            // 2) raises + owner renames
            var greens = VillageGreens(S);
            var genOf = new Dictionary<string, int>(); int maxGen = 1;
            if (S.agents != null)
                foreach (var a in S.agents) { if (!string.IsNullOrEmpty(a.name)) genOf[a.name] = a.gen; if (a.gen > maxGen) maxGen = a.gen; }

            foreach (var kv in desired)
            {
                if (_huts.TryGetValue(kv.Key, out var rec) && rec.house != null)
                {
                    if (rec.owner != kv.Value.owner)
                    {
                        rec.owner = kv.Value.owner;
                        rec.house.name = $"hut_{kv.Value.owner}";
                        d.renamed++;
                    }
                    else d.kept++;
                }
                else
                {
                    _huts[kv.Key] = Raise(S, kv.Value, kv.Key, greens, genOf, maxGen);
                    d.raised++;
                    var w = P(S, kv.Value.x, kv.Value.y);
                    if (!_firstHutSeen)
                    {
                        _firstHutSeen = true;
                        PresentationEventBus.Publish(new PresentationEvent(
                            _tick, S.years, WorldEras.Name(S), PresentationEventType.Milestone, "hut:" + kv.Key, -1, "the first hut"));
                    }
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetSpawned, "hut:" + kv.Key, -1,
                        string.Format(CultureInfo.InvariantCulture, "hut-raised x={0:F1} z={1:F1}", w.x, w.z)));
                }
            }
            return d;
        }

        public void Clear()
        {
            foreach (var r in _huts.Values)
            {
                if (r.house != null) LiveReconciler.Retire(r.house);
                foreach (var e in r.extras) if (e != null) LiveReconciler.Retire(e);
            }
            _huts.Clear();
            _firstHutSeen = false;
        }

        // ---- one hut, raised exactly the way PlaceHuts would have placed it ----
        Rec Raise(WorldState S, WorldHut h, string key, Vector2[] greens, Dictionary<string, int> genOf, int maxGen)
        {
            var rec = new Rec { owner = h.owner };
            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) { Debug.LogWarning("[HutReconciler] no asset catalog — run BUILD ASSET CATALOG"); return rec; }
            int hx = Mathf.RoundToInt(h.x), hy = Mathf.RoundToInt(h.y);
            // same salt as PlaceHuts; the RANGE is now the measured dwelling pool, not all thirteen
            int variant = DwellingVariants[(int)(Hash(hx, hy, 21) % (uint)DwellingVariants.Length)];
            var prefab = cat.Prefab($"P_BLD_house_{variant:00}") ?? cat.Prefab("P_BLD_house_09");
            if (prefab == null) { Debug.LogWarning("[HutReconciler] no house prefab found"); return rec; }

            var go = UnityEngine.Object.Instantiate(prefab, Layer(LayerName));
            go.transform.position = GroundW(P(S, h.x, h.y));
            float yaw = HouseYaw(S, h, greens, hx, hy);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * RidgeScale(go, hx, hy);
            SitOnGround(go);          // VÅG 1.2: the pivot is not the sill (see below)
            go.name = $"hut_{h.owner}";
            rec.house = go;

            PlaceYard(S, go, h, hx, hy, yaw, rec, cat);
            int og = genOf.TryGetValue(h.owner ?? "", out var gg) ? gg : maxGen;
            float ageFrac = maxGen > 1 ? 1f - og / (float)maxGen : 0.5f;
            PlaceHutAge(S, h, hx, hy, ageFrac, rec, cat);
            return rec;
        }

        /// <summary>Scale a house so its ridge lands at TargetRidge, whatever the pack authored it at.
        /// Measured from the instance's own renderers at scale 1, so a prefab we have never seen is
        /// handled by the same law. Falls back to the flat HouseScale if it has nothing to measure.</summary>
        static float RidgeScale(GameObject go, int hx, int hy)
        {
            go.transform.localScale = Vector3.one;
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return HouseScale;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            if (b.size.y < 0.01f) return HouseScale;
            float want = TargetRidge * (1f + (Hash01(hx, hy, 23) - 0.5f) * 2f * RidgeSpread);
            return want / b.size.y;
        }

        // mirror of WorldDresser.PlaceYard (same salts 71..74)
        void PlaceYard(WorldState S, GameObject house, WorldHut h, int hx, int hy, float yaw, Rec rec, EmergenceAssetCatalog cat)
        {
            var props = YardPropNames.Select(cat.Prefab).Where(p => p != null).ToArray();
            if (props.Length == 0) return;
            var rot = Quaternion.Euler(0, yaw, 0);
            var fwd = rot * Vector3.forward; var right = rot * Vector3.right;
            float front = 2.5f;
            var rends = house.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                front = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(fwd.x), 0f, Mathf.Abs(fwd.z))) + 0.9f;
            }
            int count = (int)(Hash(hx, hy, 71) % (uint)(YardPropsMax + 1));
            var basePos = P(S, h.x, h.y);
            for (int k = 0; k < count; k++)
            {
                var pf = props[Hash(hx, hy, 72 + k) % (uint)props.Length];
                var go = UnityEngine.Object.Instantiate(pf, Layer(YardLayerName));
                float lateral = (Hash01(hx, hy, 73 + k) - 0.5f) * 3.0f;
                go.transform.position = GroundW(basePos + fwd * front + right * lateral);
                go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 74 + k) % 360u, 0);
                go.name = $"yard_{h.owner}_{k}";
                rec.extras.Add(go);
            }
        }

        // mirror of WorldDresser.PlaceHutAge (same salts 81..91)
        void PlaceHutAge(WorldState S, WorldHut h, int hx, int hy, float ageFrac, Rec rec, EmergenceAssetCatalog cat)
        {
            if (ageFrac > 0.55f)
            {
                // moss list captured by CatalogBuild in the editor query's exact order (parity with WorldDresser)
                var moss = cat.mossPrefabs.Where(p => p != null).ToArray();
                if (moss.Length == 0) return;
                int n = 1 + (int)(Hash(hx, hy, 81) % 2u);
                for (int k = 0; k < n; k++)
                {
                    var pf = moss[Hash(hx, hy, 82 + k) % (uint)moss.Length];
                    var go = UnityEngine.Object.Instantiate(pf, Layer(AgeLayerName));
                    float ox = (Hash01(hx, hy, 83 + k) - 0.5f) * 4.5f, oz = (Hash01(hx, hy, 84 + k) - 0.5f) * 4.5f;
                    go.transform.position = GroundW(P(S, h.x, h.y) + new Vector3(ox, 0, oz));
                    go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 85 + k) % 360u, 0);
                    go.transform.localScale = Vector3.one * (0.4f + Hash01(hx, hy, 86 + k) * 0.3f);
                    go.name = $"overgrowth_{h.owner}_{k}";
                    rec.extras.Add(go);
                }
            }
            else if (ageFrac < 0.28f)
            {
                var fresh = FreshBuildNames.Select(cat.Prefab).Where(p => p != null).ToArray();
                if (fresh.Length == 0) return;
                var pf = fresh[Hash(hx, hy, 87) % (uint)fresh.Length];
                var go = UnityEngine.Object.Instantiate(pf, Layer(AgeLayerName));
                float ox = (Hash01(hx, hy, 88) - 0.5f) * 3.5f, oz = (Hash01(hx, hy, 89) - 0.5f) * 3.5f;
                go.transform.position = GroundW(P(S, h.x, h.y) + new Vector3(ox, 0, oz));
                go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 90) % 360u, 0);
                go.transform.localScale = Vector3.one * (0.7f + Hash01(hx, hy, 91) * 0.3f);
                go.name = $"freshbuild_{h.owner}";
                rec.extras.Add(go);
            }
        }

        static readonly string[] YardPropNames = {
            "P_PROP_cart_01","P_PROP_cart_02","P_PROP_barrel_01","P_PROP_barrel_03","P_PROP_crate_01",
            "P_PROP_crate_03","P_PROP_sack_02","P_PROP_sack_05","P_PROP_firepit_woodpile","P_PROP_hay_02",
            "P_PROP_hay_04","P_PROP_bucket_01","P_PROP_trough_01"
        };
        static readonly string[] FreshBuildNames = {
            "P_PROP_foundation_wood_01","P_PROP_foundation_wood_03","P_PROP_board_01","P_PROP_board_02","P_PROP_cart_wheel_small"
        };

        // ---- mirrors of WorldDresser's private geometry/lookup helpers ----
        static string Key(WorldHut h) => $"{Mathf.RoundToInt(h.x)}:{Mathf.RoundToInt(h.y)}";

        static Vector2[] VillageGreens(WorldState S)
        {
            int n = S.villages?.Length ?? 0;
            var g = new Vector2[n];
            if (n == 0) return g;
            var sum = new Vector2[n]; var cnt = new int[n];
            foreach (var h in S.huts)
            {
                int vi = NearestVillageIdx(S, h.x, h.y);
                if (vi >= 0) { sum[vi] += new Vector2(h.x, h.y); cnt[vi]++; }
            }
            for (int i = 0; i < n; i++) g[i] = cnt[i] > 0 ? sum[i] / cnt[i] : new Vector2(S.villages[i].x, S.villages[i].y);
            return g;
        }

        static int NearestVillageIdx(WorldState S, float x, float y)
        {
            if (S.villages == null) return -1;
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < S.villages.Length; i++)
            {
                float d = (S.villages[i].x - x) * (S.villages[i].x - x) + (S.villages[i].y - y) * (S.villages[i].y - y);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        static float HouseYaw(WorldState S, WorldHut h, Vector2[] greens, int hx, int hy)
        {
            float jitter = (Hash(hx, hy, 23) % 15) - 7f;
            int vi = NearestVillageIdx(S, h.x, h.y);
            if (vi >= 0 && greens != null && vi < greens.Length)
            {
                var hutW = P(S, h.x, h.y); var greenW = P(S, greens[vi].x, greens[vi].y);
                var d = new Vector2(greenW.x - hutW.x, greenW.z - hutW.z);
                if (d.sqrMagnitude > 1f)
                    return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + HouseFrontYawOffset + jitter;
            }
            return Hash(hx, hy, 22) % 4 * 90 + jitter;
        }

        static Transform Layer(string name)
        {
            var existing = GameObject.Find(name);
            return existing != null ? existing.transform : new GameObject(name).transform;
        }

        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);

        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }

        /// <summary>VÅG 1.2 (2026-08-14): SIT THE BUILDING ON THE GROUND, not through it.
        /// Placement puts the PIVOT at terrain height, which was harmless while the world was a flat
        /// plane at y=0 — but the living loop now has real relief (D-210) and the ground-capture probe
        /// measured door sills 6-12 cm INTO the hillside. Whatever sits between a prefab's pivot and
        /// the lowest point of its mesh has to be given back. Measured once at raise (buildings do not
        /// move) and only ever LIFTS — a model is never pushed down into the ground to satisfy this.</summary>
        static void SitOnGround(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            // A building is FLAT and the ground is not: on a slope the pivot-height sample is the
            // middle, so the uphill edge digs in whatever the pivot offset is. Sit on the HIGHEST
            // ground under the footprint — nothing buried, at the cost of a hair of daylight downhill
            // (which reads as a levelled building plot, and the village pads are levelled anyway).
            var t = Terrain.activeTerrain;
            float ground = go.transform.position.y;
            if (t != null)
            {
                float hi = float.NegativeInfinity;
                for (int cx = 0; cx <= 2; cx++)
                    for (int cz = 0; cz <= 2; cz++)
                    {
                        var p = new Vector3(Mathf.Lerp(b.min.x, b.max.x, cx * 0.5f), 0f,
                                            Mathf.Lerp(b.min.z, b.max.z, cz * 0.5f));
                        float gy = t.SampleHeight(p) + t.transform.position.y;
                        if (gy > hi) hi = gy;
                    }
                if (!float.IsNegativeInfinity(hi)) ground = hi;
            }
            float below = ground - b.min.y;
            if (below > 0.001f) go.transform.position += Vector3.up * below;
        }

        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;
    }
}
