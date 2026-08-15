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
        // D-215d: the meadow, done the way that provably renders. Terrain details were planted
        // (1 298 669 instances, measured), validated by Unity itself, drawn at 220 m with the quality
        // gate open — and could not be seen at three metres through three different render modes.
        // Ordinary MeshRenderers demonstrably DO render here: 839 trees, rocks and bushes prove it in
        // the same frame. So the near meadow is scattered as real objects. It costs ~2 000 renderers
        // beside the 2 472 already standing, which a stylised 800x560 m world can afford, and unlike
        // the detail system it can be MEASURED in the inventory the probe already prints.
        // measured at 0.30/tile: one tuft per 64 square metres, which reads as a bald lawn with
        // occasional weeds. 1.0 puts a tuft roughly every 8 m of walking - sparse enough to stay a
        // meadow and not a jungle, dense enough that the ground has texture wherever you stand.
        // D-246 — THE MEADOW WAS TWO HUNDRED TIMES TOO THIN, AND THE COMMENT ABOVE SAYS WHY IT WAS
        // NOT NOTICED: "one tuft per 8 m of walking" is true PER TILE and meaningless per SQUARE.
        // A tile is 8x8 m = 64 m2, so 1.0/tile is one tuft per sixty-four square metres. At eye
        // level that is bare ground with a weed in it, which is exactly what the EP saw and what
        // ground-eye-level.png shows. Grass reads as grass at roughly one tuft per 2..5 m2.
        //
        // Density is not uniform, because the eye is not uniform: the camera lives in the village.
        // Near the huts the meadow is thick; out in the far field it stays cheap, where the tufts
        // are pixels anyway. The tufts merge per 48 m block AFTER placement (D-224), so the cost of
        // this is triangles and not draw calls — and the probe prices both against the A6 budget.
        // Second pass, and this time against a MEASURED blade rather than a guess: the probe now
        // prints "tuft height 0,42 m mean, 24% of a person". The blade is knee height and correct.
        // So height was never the term -- 3.0/tile is one tuft per 21 m2, and a knee-high tuft every
        // twenty-one square metres is a dot on a field. Grass reads as grass at roughly one per 2 m2.
        // FOURTH PASS, and the number refused to move: raising the near-settlement density from 30 to
        // 60 changed the tuft count by ZERO -- 37 655 both times, which is exactly 6,0 x the meadow
        // tiles. So the near mask was never true anywhere. It is built from S.huts, and NATURE IS
        // SOWN BEFORE ANYONE HAS BUILT A HUT: at world start there are no huts, so the mask is empty
        // and every tuft took the far-field value. The mask stays (a loaded save has huts, and it will
        // matter the day the meadow is re-sown), but it cannot be what carries the picture.
        // What carries it is COVERAGE, computed rather than felt: a tuft at scale ~0,7 of a ~1,5 m
        // authored plant covers roughly 0,8 m2, a tile is 64 m2, so 16/tile is about 20% ground
        // coverage -- thin meadow rather than lawn, and the honest limit of what 100 000 merged
        // objects can buy. Past this the answer is a ground texture that carries the near field, not
        // more objects; that is written down in STATE rather than guessed at here.
        public const float TuftsPerMeadowTile = 16.0f;
        public const float TuftsNearSettlement = 60.0f;   // where the eye stands, for coverage rather than count
        public const int   NearSettlementTiles = 12;     // 96 m around any hut
        // Raised with the density: at 120 a fifty-thousand-tuft meadow would take four hundred frames
        // to appear. Tufts are cheap to instantiate and are merged away immediately afterwards.
        public const int PlacementsPerFrame = 600;   // keeps the opening smooth on a mid machine

        public static string LastNote = "";
        public static string TuftHeightNote = "";
        public static int Placed { get; private set; }

        static readonly string[] TreeNames  = { "Prefab_TreeLarge_01", "Prefab_TreeLarge_02", "Prefab_TreeLarge_03", "Prefab_TreeLarge_04",
                                                "Prefab_Birch_01", "Prefab_Birch_02", "Prefab_Birch_04", "Prefab_Birch_05" };
        static readonly string[] RockNames  = { "Prefab_RockFormation_01", "Prefab_RockFormation_02", "Prefab_RockFormation_03", "Prefab_RockFormation_04", "P_ENV_stone_01" };
        static readonly string[] BushNames  = { "Prefab_Bush_01", "Prefab_Bush_02", "Prefab_Bush_03" };
        static readonly string[] TrunkNames = { "P_PROP_treetrunk_01", "P_PROP_treetrunk_02", "P_PROP_treetrunk_03", "P_PROP_treetrunk_04" };
        // D-246 — THE EXPENSIVE PACKS FIRST (EP order). The meadow was built entirely out of
        // Dreamscape TERRAIN-DETAIL props, which are authored for a detail prototype's own hidden
        // size multiplier and therefore need one applied by hand (see the scale law below). The two
        // packs this studio actually paid for ship their own standalone ground plants -- village
        // grass, city grass, bushes and flowers -- and not one of them had ever been placed. They are
        // authored as ordinary props at ordinary size, so they need no correction at all.
        // Bought geometry leads; the Dreamscape details stay as filler for spread.
        // SECOND PASS, FROM THE PICTURE. Putting the bought plants in was right; putting ALL of them
        // in was not. P_ENV_flower_city_* are the CITY pack's window-box flowers -- saturated purple
        // and yellow, authored to sit in a planter on a street. Scattered across open meadow at three
        // of eleven names they became a lavender field, and the world read as a garden centre rather
        // than as land nobody has planted. They keep their place -- in the planters, where the codex
        // already owns them. The MEADOW is grass, with the muted Dreamscape flowers as the rare one.
        // FIFTH PASS, from the picture again. The set is drawn from UNIFORMLY, so a name in it is a
        // tenth of the meadow -- and P_ENV_PLANT_leaf_village is a broad-leafed decorative rosette
        // three times the visual mass of a grass clump. One name in ten became the thing the eye
        // saw everywhere: the ground read as a bed of hostas. A set is a WEIGHTING, not a list; the
        // grasses are repeated because the meadow is grass, and the leaf and the flowers are the
        // rare thing you notice precisely because they are rare.
        static readonly string[] TuftNames  = { "P_ENV_PLANT_grass_village", "P_ENV_PLANT_grass_village", "P_ENV_PLANT_grass_village",
                                                "P_ENV_grass_city_01", "P_ENV_grass_city_01", "P_ENV_grass_city_01",
                                                "Prefab_Grass_01_Detail", "Prefab_Grass_03_Detail",
                                                "Prefab_Grass_Group_01_Detail", "Prefab_Grass_Group_01_Detail",
                                                "P_ENV_PLANT_leaf_village", "Prefab_Flower_02", "Prefab_Flower_04" };

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
            var tufts  = Resolve(cat, TuftNames);
            if (trees.Count == 0 && rocks.Count == 0 && bushes.Count == 0)
            { LastNote = "no nature prefabs in the catalog — run BUILD ASSET CATALOG"; yield break; }

            int budget = 0, treeN = 0, rockN = 0, bushN = 0, trunkN = 0, tuftN = 0;
            var tuftRoot = new GameObject("Tufts").transform;
            tuftRoot.SetParent(parent, false);
            // where the eye stands. A flag per tile beats a distance test per tuft, and it is a pure
            // read of exported hut positions - no RNG, so two runs of one world grow the same meadow.
            var treeInstances = new List<TreeInstance>();
            bool[] near = null;
            if (S.huts != null && S.huts.Length > 0)
            {
                near = new bool[S.W * S.H];
                foreach (var h in S.huts)
                {
                    int cx = Mathf.RoundToInt(h.x), cy = Mathf.RoundToInt(h.y);
                    for (int dy = -NearSettlementTiles; dy <= NearSettlementTiles; dy++)
                        for (int dx = -NearSettlementTiles; dx <= NearSettlementTiles; dx++)
                        {
                            int nx = cx + dx, ny = cy + dy;
                            if (nx < 0 || ny < 0 || nx >= S.W || ny >= S.H) continue;
                            if (dx * dx + dy * dy > NearSettlementTiles * NearSettlementTiles) continue;
                            near[ny * S.W + nx] = true;
                        }
                }
            }
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    char t = Fas3TerrainBuilder.Tile(S, x, y);
                    if (t == 'f' && trees.Count > 0)
                    {
                        bool edge = ForestEdge(S, x, y);
                        treeN += Place(S, parent, trees, x, y, edge ? TreesPerForestTile * 0.45f : TreesPerForestTile, 41, ref budget);
                        // D-215b: stumps sat ONLY on the treeline, one in five tiles, at full size —
                        // which drew a hedge of waist-high stumps along every forest edge (visible in
                        // Reports/ground-eye-level.png). Fewer at the edge, some inside the wood where
                        // a tree actually fell, and 0.45 scale: a cut stump is knee-high, not chest-high.
                        // NOTE the scale had to go HERE. The size law in Place() never reached these,
                        // because the treeline stumps are raised by a direct call — one of those bugs
                        // that a measurement catches and a reading does not (mean stayed 2,2 m).
                        float trunkOdds = edge ? 0.11f : 0.05f;
                        if (trunks.Count > 0 && Hash01(x, y, 51) < trunkOdds)
                        {
                            var pf = trunks[(int)(Hash(x, y, 52) % (uint)trunks.Count)];
                            float jx = Hash01(x, y, 53) - 0.5f, jy = Hash01(x, y, 54) - 0.5f;
                            Raise(pf, parent, Ground(S, x + jx * 0.95f, y + jy * 0.95f),
                                  Hash(x, y, 55) % 360u, (0.8f + Hash01(x, y, 56) * 0.4f) * 0.45f, Bed.Log, Hash(x, y, 57));
                            trunkN++; budget++;
                        }
                    }
                    else if (t == 's' && rocks.Count > 0) rockN += Place(S, parent, rocks, x, y, RocksPerStoneTile, 43, ref budget, Bed.Boulder, true);
                    else if (t == 'b' && bushes.Count > 0) bushN += Place(S, parent, bushes, x, y, BushesPerBerryTile, 47, ref budget, Bed.Shrub, true);

                    // grass tufts on open meadow and forest floor - the ground the player walks on.
                    // Kept under their OWN parent so the batching pass can find them without guessing.
                    if ((t == 'g' || t == 'f') && tufts.Count > 0)
                        tuftN += SowTufts(S, tufts, x, y,
                                          near != null && near[y * S.W + x] ? TuftsNearSettlement : TuftsPerMeadowTile,
                                          59, ref budget, treeInstances);

                    if (budget >= PlacementsPerFrame) { budget = 0; yield return null; }
                }

            // D-224: THE MEADOW COST ONE DRAW CALL PER BLADE.
            //
            // The probe priced the world honestly for the first time and the number was blunt: 8 810
            // renderers producing 8 427 draw calls — one call per renderer, i.e. NOTHING batched —
            // and the 6 255 tufts were nearly all of it. They exist as separate objects only because
            // Unity's terrain detail system would not draw them (D-215d): that decision saved the
            // picture and moved the cost here, which was the right trade at the time and is not the
            // right place to leave it.
            //
            // The remedy is the cheapest kind: MERGE THEM. Tufts never move, never animate and never
            // respond to state, so a block of them is one mesh as truthfully as it is six hundred.
            // Combining per 48 m block turns ~6 255 renderers into a few hundred without changing a
            // single blade's position — the placement law above is untouched, and the picture is
            // byte-identical. Merging is done AFTER placement rather than instead of it, so the
            // grounding, bedding and tilt laws stay exactly one implementation.
            // D-246: MEASURE THE BLADE BEFORE IT IS MERGED. Density was tripled and the ground still
            // read as texture, and the next move is NOT a fourth density guess -- it is the height of
            // one tuft in metres, which nobody had ever printed. Sampled before the merge, because a
            // merged block has no individual bounds left to ask.
            int tuftDrawn = SowMeadow(S, tufts, treeInstances);

            Placed = treeN + rockN + bushN + trunkN + tuftN;
            LastNote = "nature: " + treeN + " trees, " + rockN + " rocks, " + bushN + " bushes, " + trunkN + " fallen trunks, "
                     + tuftN + " grass tufts sown as terrain trees (" + tuftDrawn + " prototypes)"
                     + "  (sets: " + trees.Count + "/" + rocks.Count + "/" + bushes.Count + "/" + tufts.Count + ")";
            LastNote += "  | " + TuftHeightNote;
        }

        /// <summary>Merge the scattered tufts into one mesh per (block, material). Blocks are 48 m,
        /// which is coarse enough to collapse the count and fine enough that frustum culling still
        /// throws most of the meadow away. Returns the number of renderers left standing.</summary>
        const float TuftBlock = 48f;


        // ---- THE MEADOW, THE WAY THE PACK'S OWN DOCUMENTATION SAYS TO BUILD IT (D-248) ----
        //
        // From "Documentation - FANTASTIC Village Pack", chapter "Environment setup - Terrain Tool",
        // in the vendor's own words:
        //
        //     "It's important to note that bushes/leaves and grass will not work on the terrain as
        //      'Detail Objects' or 'Grass Texture'. The prefabs should be added as 'Tree Objects',
        //      because the built-in grass shader would otherwise override our custom shaders.
        //      This is not a limitation of the package, but rather a limitation of Unity."
        //
        // That paragraph is the answer to four failed attempts. D-215d tried the detail system three
        // ways (instanced mesh, non-instanced mesh, billboard) and got a validated million blades that
        // drew nothing usable; D-246 gave up on it and scattered a hundred thousand MeshRenderers
        // instead, which cost double the frame time for a meadow that still read thin. The detail
        // path was never going to work with this pack, because Unity's detail renderer SUBSTITUTES
        // its own grass shader for the material on the prefab -- and the whole reason these plants
        // look like anything is the pack's TidalFlask wind shader, with its ground fade and its wind
        // tint. The terrain TREE system keeps the prefab's own material, and the pack ships every one
        // of these plants with a LODGroup already on it, which is the shape the tree renderer wants.
        //
        // So: same placement law as before, to the hash -- the meadow does not move a blade -- but
        // the blades are sown as tree instances rather than instantiated and merged. Unity culls and
        // LODs them for us, the wind shader survives, and the frame stops paying for a hundred
        // thousand renderers. Deterministic: every position, pick and size is hash(tile, index).
        static int SowTufts(WorldState S, List<GameObject> set, int x, int y, float perTile, int salt,
                            ref int budget, List<TreeInstance> outp)
        {
            if (set.Count == 0) return 0;
            // identical clumping to Place(): some tiles crowd, others stay open
            perTile *= 0.30f + 1.55f * Mathf.PerlinNoise(x * 0.17f + salt * 0.31f, y * 0.17f + salt * 0.13f);
            int count = Mathf.FloorToInt(perTile) + (Hash01(x, y, salt) < perTile - Mathf.Floor(perTile) ? 1 : 0);
            var terrain = Terrain.activeTerrain;
            if (terrain == null) return 0;
            var td = terrain.terrainData;
            var origin = terrain.transform.position;

            for (int i = 0; i < count; i++)
            {
                int proto = (int)(Hash(x, y, salt + 100 + i) % (uint)set.Count);
                float jx = Hash01(x, y, salt + 200 + i) - 0.5f, jy = Hash01(x, y, salt + 300 + i) - 0.5f;
                var w = Ground(S, x + jx * 0.98f, y + jy * 0.98f);

                // the same size law the scattered version used, kept so the picture is comparable:
                // the pack's own plants sit in one band, the Dreamscape detail props in the other.
                var name = set[proto].name;
                // D-250, from the dusk picture: the broad-leafed rosette is the one plant in the set
                // whose SILHOUETTE does not belong. At the same size band as a grass clump it carries
                // three times the visual mass, so a thirteenth of the meadow read as most of it --
                // hostas in a Nordic field. It stays, because a meadow with only grass in it is a lawn,
                // but at the size a broad leaf actually is down among the blades.
                float sc = name.Contains("leaf")
                    ? 0.18f + Hash01(x, y, salt + 600 + i) * 0.20f
                    : name.StartsWith("P_ENV")
                      ? 0.38f + Hash01(x, y, salt + 600 + i) * 0.34f
                      : 0.45f + Hash01(x, y, salt + 600 + i) * 0.50f;

                float nx = (w.x - origin.x) / td.size.x;
                float nz = (w.z - origin.z) / td.size.z;
                if (nx < 0f || nz < 0f || nx > 1f || nz > 1f) continue;   // off the terrain is not a place

                outp.Add(new TreeInstance
                {
                    position       = new Vector3(nx, 0f, nz),   // snapped to the heightmap on apply
                    prototypeIndex = proto,
                    widthScale     = sc,
                    heightScale    = sc,
                    rotation       = (Hash(x, y, salt + 400 + i) % 360u) * Mathf.Deg2Rad,
                    color          = Color.white,
                    lightmapColor  = Color.white
                });
                budget++;
            }
            return count;
        }

        /// <summary>Hand the whole meadow to the terrain in one call and tell it how far to draw.
        /// Returns the prototype count so the report can say what the world is made of.</summary>
        static int SowMeadow(WorldState S, List<GameObject> set, List<TreeInstance> instances)
        {
            var terrain = Terrain.activeTerrain;
            if (terrain == null || set.Count == 0) { TuftHeightNote = "tufts: no terrain to sow into"; return 0; }
            var td = terrain.terrainData;

            var protos = new TreePrototype[set.Count];
            for (int i = 0; i < set.Count; i++) protos[i] = new TreePrototype { prefab = set[i], bendFactor = 0f };
            td.treePrototypes = protos;
            td.SetTreeInstances(instances.ToArray(), true);   // true = snap each one to the heightmap

            // Grass is not a landmark: draw it near, drop it far. Billboards are left off because
            // these prefabs carry no billboard asset -- a grass blade at 200 m is one pixel and is
            // better culled than faked.
            terrain.treeDistance = 260f;
            terrain.treeBillboardDistance = 0f;
            terrain.treeCrossFadeLength = 25f;
            terrain.treeMaximumFullLODCount = 0;
            terrain.Flush();

            // the blade, in metres, against the 1,75 m yardstick — measured from the prototypes and
            // the size law rather than from instantiated objects, because there are none any more.
            float lo = float.MaxValue, hi = 0f, sum = 0f; int n = 0;
            foreach (var pf in set)
            {
                if (pf == null) continue;
                var rs = pf.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                for (int k = 1; k < rs.Length; k++) b.Encapsulate(rs[k].bounds);
                float mid = b.size.y * (pf.name.StartsWith("P_ENV") ? 0.55f : 0.70f);
                if (mid <= 0f) continue;
                lo = Mathf.Min(lo, mid); hi = Mathf.Max(hi, mid); sum += mid; n++;
            }
            TuftHeightNote = n == 0
                ? "tufts: no bounds"
                : "tuft height " + (sum / n).ToString("F2") + " m mean (" + lo.ToString("F2") + ".." + hi.ToString("F2")
                  + "), " + (sum / n / 1.75f * 100f).ToString("F0") + "% of a person; sown as terrain trees per the pack's own manual";
            return set.Count;
        }

        /// <summary>The tuft, in metres, against the 1,75 m yardstick. A meadow the eye can read
        /// wants roughly knee height; anything under ~0,25 m is a texture no matter how many there are.</summary>
        static string MeasureTufts(Transform root)
        {
            if (root == null || root.childCount == 0) return "tufts: none";
            float min = float.MaxValue, max = 0f, sum = 0f; int n = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                var rs = root.GetChild(i).GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) continue;
                var b = rs[0].bounds;
                for (int k = 1; k < rs.Length; k++) b.Encapsulate(rs[k].bounds);
                float h = b.size.y;
                if (h <= 0f) continue;
                min = Mathf.Min(min, h); max = Mathf.Max(max, h); sum += h; n++;
            }
            if (n == 0) return "tufts: no bounds";
            float mean = sum / n;
            return "tuft height " + mean.ToString("F2") + " m mean (" + min.ToString("F2") + ".." + max.ToString("F2")
                 + "), " + (mean / 1.75f * 100f).ToString("F0") + "% of a person";
        }

        static int CombineTufts(Transform root)
        {
            if (root == null || root.childCount == 0) return 0;

            var groups = new Dictionary<string, List<CombineInstance>>();
            var mats = new Dictionary<string, Material>();
            var doomed = new List<GameObject>();

            for (int i = 0; i < root.childCount; i++)
            {
                var ch = root.GetChild(i);
                doomed.Add(ch.gameObject);
                foreach (var mf in ch.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (mr == null || mr.sharedMaterial == null || !mr.enabled) continue;
                    var p = mf.transform.position;
                    string key = Mathf.FloorToInt(p.x / TuftBlock) + "_" + Mathf.FloorToInt(p.z / TuftBlock)
                               + "_" + mr.sharedMaterial.name;
                    if (!groups.TryGetValue(key, out var list)) { list = new List<CombineInstance>(); groups[key] = list; mats[key] = mr.sharedMaterial; }
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = mf.transform.localToWorldMatrix });
                }
            }

            int made = 0;
            foreach (var kv in groups)
            {
                if (kv.Value.Count == 0) continue;
                var mesh = new Mesh { name = "Meadow_" + kv.Key };
                // a 48 m block of tufts can exceed 65 535 vertices; say so in the format rather than
                // silently truncating the meadow, which is how a merge quietly eats geometry.
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.RecalculateBounds();

                var go = new GameObject("Meadow_" + made);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mats[kv.Key];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // a blade casts nothing worth the cost
                made++;
            }

            foreach (var g in doomed) if (g != null) Object.DestroyImmediate(g);
            return made;
        }

        static List<GameObject> Resolve(EmergenceAssetCatalog cat, string[] names)
        {
            var o = new List<GameObject>();
            foreach (var n in names) { var p = cat.Prefab(n); if (p != null) o.Add(p); }
            return o;
        }

        /// <summary>How a thing meets the ground. The rock rows in Reports/ground-eye-level.png came
        /// from treating all four the same: one upright yaw-only instance per tile, sitting exactly on
        /// the surface. That inherits the 8 m tile grid and reads as a fence of menhirs (D-215).</summary>
        public enum Bed { Upright, Boulder, Shrub, Log, Tuft }

        static int Place(WorldState S, Transform parent, List<GameObject> set, int x, int y, float perTile, int salt, ref int budget)
            { return Place(S, parent, set, x, y, perTile, salt, ref budget, Bed.Upright, false); }

        static int Place(WorldState S, Transform parent, List<GameObject> set, int x, int y, float perTile, int salt,
                         ref int budget, Bed bed, bool clump)
        {
            // clumping: a stone field is not one boulder every eight metres. A slow Perlin makes some
            // tiles crowd and others empty, which is what breaks the grid the eye was reading.
            if (clump)
                perTile *= 0.30f + 1.55f * Mathf.PerlinNoise(x * 0.17f + salt * 0.31f, y * 0.17f + salt * 0.13f);

            int count = Mathf.FloorToInt(perTile) + (Hash01(x, y, salt) < perTile - Mathf.Floor(perTile) ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                var prefab = set[(int)(Hash(x, y, salt + 100 + i) % (uint)set.Count)];
                float jx = Hash01(x, y, salt + 200 + i) - 0.5f, jy = Hash01(x, y, salt + 300 + i) - 0.5f;
                float sc = 0.85f + Hash01(x, y, salt + 500 + i) * 0.4f;
                // MEASURED CORRECTIONS (D-215). With the human yardstick finally right — a villager
                // stands 1.75 m — the pack's own proportions can be judged instead of guessed:
                //   tree  16.6 m = 9.5x a person   correct for real trees, left alone (bar the baseline 0.72)
                //   bush   3.8 m = 2.2x a person   a bush that overtops a person is a thicket, not a shrub
                //   rock   4.6 m = 2.6x a person   these are Cliff meshes; on scattered stone tiles they
                //                                  should read as boulders a person could climb, not as menhirs
                // Targets: a bush ~1.4 m (waist-to-chest), a boulder ~2.6 m.
                if (prefab.name.StartsWith("Prefab_TreeLarge")) sc *= 0.72f;   // the woodland baseline, not a landmark
                else if (prefab.name.StartsWith("Prefab_Bush")) sc *= 0.37f;
                else if (prefab.name.StartsWith("Prefab_RockFormation") || prefab.name.StartsWith("P_ENV_stone"))
                    // SECOND PASS (D-215b). The first widening measured mean 3,6 m — two people tall —
                    // because these are CLIFF meshes borrowed as field stones. A stone in a meadow is
                    // ankle-to-shoulder, not a menhir. Mean ~1,5 m, spread 0,5..2,5 m: pebbles a person
                    // steps over AND a few they could shelter behind.
                    sc = (0.30f + Hash01(x, y, salt + 600 + i) * 1.10f) * 0.30f;
                else if (prefab.name.StartsWith("Prefab_Grass") || prefab.name.StartsWith("Prefab_Flower"))
                    // the pack authored these as terrain-detail props, sized for a detail prototype's
                    // own multiplier. As standalone objects they need that multiplier applied by hand;
                    // the wide spread is what makes a meadow read as tufts rather than as a lawn.
                    // measured 1,5 m mean - chest-high on an adult, which is a wheat field, not a
                    // meadow. Target ~0,55 m: over the boot, under the knee.
                    // D-221: the top of this range read as pale sticks at distance — a tuft that
                    // tall is a reed bed, not meadow. Capped where it stops being grass.
                    // THIRD PASS, and the term that had never been computed: COVERAGE. Thirty tufts
                    // of 0,38 m in a 64 m2 tile cover about 7% of it, and 7% is a lawn with weeds no
                    // matter how the count reads. Grass reads as grass at 40-70% ground coverage.
                    // Coverage goes up far cheaper through SIZE than through count -- doubling the
                    // width of one clump quadruples what it hides -- so the band widens and the set
                    // leans on the pack's GROUP mesh. Still under the knee at the top (0,95 x ~1,5 m
                    // authored = 1,4 m only for the rare tallest), which is where D-221 drew the line.
                    sc = 0.45f + Hash01(x, y, salt + 600 + i) * 0.50f;
                else if (prefab.name.StartsWith("P_ENV_PLANT") || prefab.name.StartsWith("P_ENV_grass"))
                    // The bought ground plants get the same yardstick as everything else, because they
                    // never had one: the probe measured the meadow at 0,63 m mean and 1,90 m max after
                    // they came in, and a 1,9 m plant in open grass is the menhir mistake with leaves.
                    // Target the same band as the tufts they stand among: over the boot, under the knee.
                    sc = 0.38f + Hash01(x, y, salt + 600 + i) * 0.34f;
                else if (prefab.name.StartsWith("P_PROP_treetrunk"))
                    // measured 2,0 m — a stump taller than a child. A cut stump is knee-to-thigh.
                    sc *= 0.45f;
                Raise(prefab, parent, Ground(S, x + jx * 0.98f, y + jy * 0.98f),
                      Hash(x, y, salt + 400 + i) % 360u, sc, bed, Hash(x, y, salt + 700 + i));
                budget++;
            }
            return count;
        }

        /// <summary>Instantiate, orient, scale — and SIT IT ON THE GROUND. The pivot is not the base
        /// (D-211): on 17.9 m of relief a trunk placed by its pivot slides half-under the hillside,
        /// which is exactly what the field report described.
        ///
        /// D-215 adds the other half. Sitting a boulder exactly ON the surface, perfectly upright, is
        /// how you get standing slabs in rows. Real stone leans and is half-buried; a fallen log lies
        /// tilted and settled. So each kind now declares a tilt and a bedding depth, and only a TREE
        /// is held strictly on top of the ground. All of it hash-driven — no sim RNG (D-078 r4).</summary>
        static void Raise(GameObject prefab, Transform parent, Vector3 pos, uint yaw, float scale)
            { Raise(prefab, parent, pos, yaw, scale, Bed.Upright, 0u); }

        static void Raise(GameObject prefab, Transform parent, Vector3 pos, uint yaw, float scale, Bed bed, uint h)
        {
            var go = Object.Instantiate(prefab, parent);

            float tilt, sink;
            switch (bed)
            {
                case Bed.Boulder: tilt = 24f; sink = 0.26f + (h % 1000) / 1000f * 0.26f; break;  // leans, 26-52% buried
                case Bed.Log:     tilt = 17f; sink = 0.14f + (h % 1000) / 1000f * 0.18f; break;  // settled in the leaf mould
                case Bed.Shrub:   tilt =  8f; sink = 0.04f + (h % 1000) / 1000f * 0.09f; break;  // roots in, not standing on
                case Bed.Tuft:    tilt = 11f; sink = 0.06f + (h % 1000) / 1000f * 0.10f; break;  // grows out of the soil
                // D-221: a tree DOES stand up, but it does not stand ON the ground — its root
                // flare grows out of the soil. At sink 0 the flares splayed across the grass like a
                // hand laid on a table, which is what eye-at-the-water.png shows. 4-9% of a 17 m
                // tree is 0,7-1,5 m of root tucked under, which is what a root does.
                default:          tilt =  3f; sink = 0.04f + (h % 1000) / 1000f * 0.05f; break;
            }
            float tx = ((h >> 10 & 1023) / 1023f - 0.5f) * 2f * tilt;
            float tz = ((h >> 20 & 1023) / 1023f - 0.5f) * 2f * tilt;
            go.transform.rotation = Quaternion.Euler(tx, yaw, tz);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            StripImpostors(go);

            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            float below = pos.y - b.min.y;
            if (below > 0.001f) go.transform.position += Vector3.up * below;
            if (sink > 0f) go.transform.position -= Vector3.up * (b.size.y * sink);
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
