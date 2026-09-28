// EMERGENCE P1 — THE DRESSING LAYER (editor-driven v1: machinery, grammar iterates on top)
// D-078 rule 4 codified: this layer is PRESENTATION — it READS an exported world
// state, uses POSITION HASHES for all variety (never S.rand, never Unity Random
// seeded from sim), and never writes back. AD owns the look, GD owns the grammar's
// design language, TD enforces read-only. Density budgets are the Producer's knife.
//
// v1 scope (P1a): terrain from tiles (splat by type, water plane), hut->house
// placement, fields, village markers, trees/rocks/berries by density budget,
// light rig hookup. Composition grammar (plots/yards/fences/roads) iterates here.
// v1.5: fields as enclosures w/ gates. v2 (TD-031): houses face the village GREEN
// (hut centroid, the "tun") instead of a random grid + a lived-in yard per house.
// v2.1 (TD-031): worn desire-line paths splatted into the terrain (hut->green,
// village->village) + managed forest EDGE (thinner treeline + fallen trunks).
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;   // D-137: the world model (WorldState/Codex/…) now lives in Runtime/WorldModel.cs

namespace Emergence.Editor
{
    public static class WorldDresser
    {
        public const float TileSize = 8f;          // meters per sim tile (Producer knob)
        // D-882 (Patrik 2026-09-27: "en liten blandning av hus när civilisationen utvecklas … så levande värld som möjligt"):
        // a dwelling is chosen by TIER from the sim state — the village's development (RoadTier: pop + tech), the owner's
        // wealth rank among the living, and the hut's age — then by a deterministic hash within the tier's set, so one
        // save always renders the same houses while no two neighbours need look alike. All 11 dwellings are in play;
        // NOT dwellings: 01 tower, 13 water mill, 14 windmill. Lists are Patrik's to edit (contact sheet evidence/houses).
        public static readonly string[] HouseTier0 = { "P_BLD_house_02", "P_BLD_house_06", "P_BLD_house_09", "P_BLD_house_10" };                    // cottages — a hamlet, the poor, the founders' first roofs
        public static readonly string[] HouseTier1 = { "P_BLD_house_03", "P_BLD_house_04", "P_BLD_house_08", "P_BLD_house_06", "P_BLD_house_09" }; // timber houses — a developing village
        public static readonly string[] HouseTier2 = { "P_BLD_house_05", "P_BLD_house_07", "P_BLD_house_11", "P_BLD_house_12", "P_BLD_house_04" }; // the big houses — a town's rich, the settled heart rebuilt
        public const int   AlphaRes = 1024;
        public static string PersistTerrainPath = null; // D-891: when set, the TerrainData is saved as this asset using D-878's deterministic CreateAsset-BEFORE-SetAlphamaps (a build needs a persisted splat); null = in-memory (D-881, editor screenshots)        // D-879: terrain splat resolution (0,78 m/cell at W=100) — trails need it
        public const string FloorScenePath = "Assets/Emergence/Scenes/EmergenceFloor_day.unity"; // D-878: born from demoscene_village_day
        public const string NatureRoot = "Assets/Fantastic Nature Pack";   // D-875: L3 family = FANTASTIC; Dreamscape is out (magenta in URP 17.5)
        public const string VillageRoot = "Assets/Fantastic Village Pack";
        public const float GrassPerSqm = 0.2f;       // D-878: Nature grass as terrain TREE instances (pack table s.21: grass = Tree Objects)
        public const float GrassPerTile = 0.8f;    // TD-032: Dreamscape waving grass clumps per open-grass tile (the meadow look — EP: "gräset syns inte / vajar inte"). ~0.8×5725 g-tiles ≈ 4.6k clumps; tune up if the editor handles it
        public const float GrassScale = 1.3f;      // Dreamscape grass clumps read a touch small at 1 in our scale
        public const float TreesPerForestTile = 0.9f;  // density budgets (AD/Producer iterate)
        public const float RocksPerStoneTile = 0.9f;
        public const float BushesPerBerryTile = 1.1f;
        // TD-025 audition batch (Vefects fire + msVFX smoke) — Producer scale knobs
        public const float FireScale = 1.4f;         // Vefects fire authored ~1m; a hearth reads ~1.5m
        public const float SmokeScale = 0.6f;        // msVFX smoke is billowy — thin it to a chimney plume
        public const float SmokeRoofLift = 4.2f;     // above the house roof
        public const int   SmokeNearFireTiles = 3;   // huts this close to a burning fire get chimney smoke
        // TD-028 characters + tech anchors (the studio's OWN rendered GLBs, EP directive)
        public const float VillagerScale = 0.76f;   // D-215: MEASURED at rest — the adult GLB is 2.29 m, not the ~1.7 this comment claimed. 1.75/2.29 = 0.76.
        public const float TechAnchorScale = 0.3f;  // GLBs are big at 1 (well = giant staircase) — tuned down
        const string CharDir = "Assets/Emergence/Models/characters/";
        const string TechDir = "Assets/Emergence/Models/tech/";
        const string NatureDir = "Assets/Emergence/Models/nature/";
        public const float AnimalScale = 1f;   // deer/wolf GLBs — tune after first import
        // TD-031 composition grammar v2 (the "tun" reading — houses face the local green, each gets a yard)
        public const float HouseScale = 0.55f;         // pack houses at 1 are OVERSIZED (a house spans 2+ field plots, dwarfs props/yards); ~0.55 makes a house read as one village plot (~TileSize) — EP knob
        public const float HouseFrontYawOffset = 0f;  // pack houses' door axis: 0 if the front is +Z; AD flips to 180 if doors read as facing AWAY from the green
        public const int   YardPropsMax = 2;           // 0..2 work-life props per house on the door side (placed just beyond the house's real front face)

        // ---- deterministic presentation hash (the engine's own pattern; NEVER sim RNG) ----
        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
        static float Hash01(int x, int y, int salt) => Hash(x, y, salt) / 4294967295f;

        [MenuItem("Emergence/P1 Dressing/Build World From State (pick JSON)")]
        public static void BuildFromPicker()
        {
            var path = EditorUtility.OpenFilePanel("Pick exported world state", Path.Combine(Application.dataPath, "Emergence", "WorldStates"), "json");
            if (!string.IsNullOrEmpty(path)) Build(path);
        }

        public static void Build(string jsonPath)
        {
            var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(jsonPath));
            Debug.Log($"[Dresser] {Path.GetFileName(jsonPath)}: engine {S.engineVersion}, {S.W}x{S.H}, {S.agents.Length} souls, {S.huts.Length} huts, {S.villages.Length} villages, season {S.season}");
            // D-878 (DEMO-BYGGPLAN steg 5, VISUELL-TOTALPLAN L2 "demoscenen är golvet"): the world is dressed INTO the
            // floor scene born from the pack's own demo (sun, sky, ambient, post — a versioned file, L4) when it exists;
            // an empty scene only as fallback. The floor is made by RUN_SCENEBIRTH (AutoSceneBirth.cs).
            UnityEngine.SceneManagement.Scene scene;
            if (System.IO.File.Exists(FloorScenePath))
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(FloorScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log("[Dresser] floor scene opened: " + FloorScenePath + " (light/sky/post inherited from the pack demo)");
            }
            else
            {
                scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                Debug.LogWarning("[Dresser] no floor scene at " + FloorScenePath + " — empty scene (run RUN_SCENEBIRTH)");
            }

            var root = new GameObject($"World_{S.seed}_y{S.years}");
            // the documentary camera (P2 grows this into Cinemachine): start over the heartland
            var camGo = new GameObject("DocCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            camGo.transform.position = new Vector3(S.W * TileSize * 0.5f, 55f, (S.H * TileSize * 0.5f) - 90f);
            camGo.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
            // TD-032: a directional WindZone so the pack foliage (Dreamscape grass, Village flags/leaves) actually sways
            var windGo = new GameObject("Wind");
            var wind = windGo.AddComponent<WindZone>();
            wind.mode = WindZoneMode.Directional; wind.windMain = 0.5f; wind.windTurbulence = 0.4f;
            wind.windPulseMagnitude = 0.5f; wind.windPulseFrequency = 0.15f;
            windGo.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            windGo.transform.SetParent(root.transform, true);
            BuildTerrain(S, root.transform);
            BuildWater(S, root.transform);
            PlaceGroundFeatures(S, root.transform); // TD-031 terrain pass: field soil + desire-line paths as mesh decals (URP won't render the terrain splat)
            // PlaceGrass DISABLED — scatter stopgap was sparse + had a magenta sub-material; proper lush grass = terrain-detail P0 pass (audit). Method kept.
            // PlaceGrass(S, root.transform);
            PlaceHuts(S, root.transform);         // TD-031 v2: houses face the green (scaled) + lived-in yards per house
            PlaceFires(S, root.transform);
            PlaceFields(S, root.transform);
            PlaceNature(S, root.transform);
            PlaceMeadowFoliage(S, root.transform); // D-101d: fill the open meadow with real 3D foliage (flowers/tufts/bushes) — the near-field life that short detail-grass can't give
            PlaceAmbientFX(S, root.transform);     // D-115: Dreamscape's own drifting leaves + dust motes (atmosphere; visible in play mode)
            PlaceWorkMarks(S, root.transform);    // TD-031 v2.2b: quarry scars at depleted stone tiles (Materials layer)
            PlaceAgents(S, root.transform);       // the studio's own rendered villagers (EP directive)
            // C7 (D-233): PlaceTechAnchors is GONE. It stood a well in EVERY village regardless of
            // whether anyone there knew how to dig one, and picked forge/mill/kiln by position hash
            // — a world that lied about what its people could do, which is the exact opposite of the
            // codex's premise. The codex now owns those four, gated on their real techs, using the
            // same GLBs (mill.glb / well.glb / kiln.glb / forge.glb). One placer, one truth.
            PlaceCodexObjects(S, root.transform); // TD-033: discovery-driven objects (mill/tablets/star-banner/market by village development)
            PlaceAnimals(S, root.transform);      // the studio's own deer/wolf GLBs (animal upgrade)
            EmergenceLightRig.Apply(S.season, "day");
            StripImpostorsSceneWide();   // D-131: kill the distant-magenta class regardless of placement path
            Debug.Log("[Dresser] world built — iterate grammar/density from here (menu re-runs are idempotent: fresh scene each time)");
        }

        static char Tile(WorldState S, int x, int y) => S.tileTypes[y * S.W + x];
        static int TileN(WorldState S, int x, int y) => (S.tileN != null && y * S.W + x < S.tileN.Length) ? S.tileN[y * S.W + x] : 9;

        // TD-031 v2.2b: work-marks — "stone is visible where stone is won" (grammar §2, Materials). A stone
        // tile the people have QUARRIED (low tileN = harvested) gets a bare-earth scar decal + worked-stone
        // props. Documentary-honest: low n IS worked stone in the sim. Hash-placed, RNG-neutral (D-078 r4).
        static void PlaceWorkMarks(WorldState S, Transform root)
        {
            // D-140 (genesis honesty, the inc-6 opening frame): a quarry scar SAYS "people worked here".
            // At genesis low tileN is BORN-poor stone, not quarried stone — nobody has swung a pick. No
            // settlement (0 huts) ⇒ no work-marks; the wilderness must not carry lies about labor.
            // (Slope-floating of legit scar decals on terrain steps = separate look-pass item, logged.)
            if ((S.huts?.Length ?? 0) == 0) { Debug.Log("[Dresser] work-marks skipped — pre-settlement world (genesis honesty, D-140)"); return; }
            var parent = new GameObject("WorkMarks").transform; parent.SetParent(root, true);
            var stoneProps = new[] { "P_PROP_stone_01", "P_PROP_stone_02", "P_PROP_wall_stone_small_01", "P_PROP_wall_stone_small_02", "Coal Pile" }
                .Select(FindPrefab).Where(p => p != null).ToArray();
            if (stoneProps.Length == 0) return;
            var scarMat = GroundMat("Layer_Dirt", new Color(0.44f, 0.36f, 0.26f));
            int marks = 0;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                    if (Tile(S, x, y) == 's' && TileN(S, x, y) <= 3 && Hash01(x, y, 95) < 0.6f) // a quarried-out stone tile
                    {
                        Decal(S, parent, scarMat, x, y, TileSize * 0.9f, 0.05f, $"quarryscar_{marks}");
                        int n = 1 + (int)(Hash(x, y, 96) % 2u);
                        for (int k = 0; k < n; k++)
                        {
                            var pf = stoneProps[Hash(x, y, 97 + k) % (uint)stoneProps.Length];
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                            float ox = (Hash01(x, y, 98 + k) - 0.5f) * 4f, oz = (Hash01(x, y, 99 + k) - 0.5f) * 4f;
                            go.transform.position = GroundW(P(S, x, y) + new Vector3(ox, 0, oz));
                            go.transform.rotation = Quaternion.Euler(0, Hash(x, y, 100 + k) % 360u, 0);
                            go.transform.localScale = Vector3.one * (0.7f + Hash01(x, y, 101 + k) * 0.5f);
                        }
                        marks++;
                    }
            Debug.Log($"[Dresser] {marks} quarry work-marks at depleted stone tiles (v2.2b)");
        }
        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize); // sim y -> world -z (map reads like the sim's screen)
        static Vector3 Ground(WorldState S, float x, float y, float lift = 0)
        {
            var pos = P(S, x, y);
            var t = Terrain.activeTerrain;
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y;
            return pos + Vector3.up * lift;
        }
        // snap an arbitrary WORLD-space point to the terrain (for props placed by direction+offset, not sim tile)
        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }

        static void BuildTerrain(WorldState S, Transform root)
        {
            var data = new TerrainData();
            int res = 257;
            data.heightmapResolution = res;
            data.size = new Vector3(S.W * TileSize, 72f, S.H * TileSize);
            // ONCE-AND-FOR-ALL (D-101b): real rolling relief that READS from the map camera (the 8m
            // version was a 1% grade — invisible from 55m up). Multi-octave noise → ~25m rolling hills,
            // water carved below, village centres settled flat so houses sit level. Seed-varied.
            float vseed = S.seed % 991 * 0.137f, vseed2 = S.seed % 733 * 0.171f;
            var heights = new float[res, res];
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
                    // settle the ground toward the local mean near village centres (flat building pads)
                    float flat = VillageFlatten(S, sx, sy);
                    baseH = Mathf.Lerp(baseH, 0.22f, flat);
                    if (Tile(S, tx, ty) == 'w') baseH -= 0.08f;       // ponds/rivers sit below the meadow
                    else if (Tile(S, tx, ty) == 's') baseH += 0.04f;  // stone ground stands a touch proud
                    heights[ry, rx] = baseH;
                }
            data.SetHeights(0, 0, heights);

            // D-101: prefer DREAMSCAPE's own textured terrain layers (real diffuse+normal, the reference
            // look) — fall back to the project's earlier layers, then to a flat colour only if nothing loads.
            var layers = new List<TerrainLayer>();
            // D-878: FANTASTIC Nature terrain layers first (2d/textures/terrain_layers — the pack's own splat set), Dreamscape names only as legacy fallback
            int liGrass = AddLayer(layers, new[] { "Layer_grass_01", "Layer_Grass" }, new Color(0.35f, 0.5f, 0.22f));
            // it.2 SEEN: Layer_sand on desire lines painted 8 m pale-yellow bands across the whole diorama — the Village pack's own
            // gravel (its village paths) and farmfield (its tilled soil) are the right hands; Nature's dirt/stone only as fallback.
            // it.3 MEASURED: the Village layers (farmfield/gravel_01) sit in the alphamap (12 % dominant) but do NOT render on the
            // runtime TerrainData (TD-031 class) while the Nature layers do — Nature's own dirt/gravel until that is understood.
            int liField = AddLayer(layers, new[] { "Layer_dirt", "Layer_farmfield", "Layer_Dirt" }, new Color(0.45f, 0.35f, 0.2f));
            int liPath = AddLayer(layers, new[] { "Layer_gravel", "Layer_gravel_01", "Layer_Dirt" }, new Color(0.42f, 0.32f, 0.2f)); // worn desire-line ground
            int liGravel = AddLayer(layers, new[] { "Layer_stone", "Layer_gravel", "Layer_Rock" }, new Color(0.5f, 0.48f, 0.45f));
            // D-120 roads v1.1: 5th layer = COBBLESTONE for the paved-street tier. >4 layers needs URP's 8-layer
            // path — we now enable the _TERRAIN_8_LAYERS keyword on the terrain material (below) so it renders.
            int liCobble = AddLayer(layers, new[] { "Layer_pavingstone_01", "Layer_stone", "Layer_Cobblestone" }, new Color(0.55f, 0.53f, 0.5f));
            data.terrainLayers = layers.ToArray();

            // D-879 (Patrik): roads start as narrow trails and only widen/pave as the civilisation develops. At 256 the
            // alphamap cell was 3,1 m (100 tiles x 8 m / 256) so the thinnest brush was a 9 m band — the "wide roads" seen
            // in D-878. 1024 gives 0,78 m cells: a trail can be ~1,5 m, a cart path ~3 m, a paved street ~4,5 m.
            data.alphamapResolution = AlphaRes;
            var am = new float[AlphaRes, AlphaRes, layers.Count];
            for (int ay = 0; ay < AlphaRes; ay++)
                for (int ax = 0; ax < AlphaRes; ax++)
                {
                    float sx = ax / (float)(AlphaRes - 1) * (S.W - 1);
                    float sy = (1f - ay / (float)(AlphaRes - 1)) * (S.H - 1);
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                    char tt = Tile(S, tx, ty);
                    if (tt == 's' || tt == 'i')
                    {
                        // D-115: stony ground, but blend with grass + a little worn dirt (was pure grey rock =
                        // a hard checkerboard at the village). Noise keeps it mottled, not a flat grey square.
                        float f = Mathf.PerlinNoise(sx * 0.3f + 5f, sy * 0.3f + 11f);
                        am[ay, ax, liGravel] = 0.55f + f * 0.25f;
                        am[ay, ax, liGrass] = 0.25f;
                        am[ay, ax, liPath] = 0.20f - f * 0.10f;
                    }
                    else if (tt == 'a' || tt == 'c') { am[ay, ax, liPath] = 1f; }
                    else
                    {
                        // D-101: break the uniform "billiard green" — grass with noise-driven worn-earth
                        // patches + faint rock flecks, so the ground reads as a living meadow, not felt.
                        float patch = Mathf.PerlinNoise(sx * 0.09f + 21f, sy * 0.09f + 9f);
                        float fleck = Mathf.PerlinNoise(sx * 0.23f + 4f, sy * 0.23f + 17f);
                        float wDirt = patch > 0.66f ? Mathf.Clamp01((patch - 0.66f) * 2.6f) : 0f;
                        float wRock = fleck > 0.80f ? Mathf.Clamp01((fleck - 0.80f) * 2.2f) : 0f;
                        float wGrass = Mathf.Max(0f, 1f - wDirt - wRock);
                        am[ay, ax, liGrass] = wGrass;
                        am[ay, ax, liPath] += wDirt;
                        am[ay, ax, liGravel] += wRock;
                    }
                }
            int trodden = PaintTrodden(S, am, AlphaRes, liPath, liGrass); // D-879: footfall wears the grass (states with pathUse)
            Debug.Log($"[Dresser] TRODDEN GROUND: {trodden} cells worn from pathUse (max footfall {(S.pathUse != null && S.pathUse.Length > 0 ? S.pathUse.Max() : 0)})");
            StampFields(S, am, liField, AlphaRes); // TD-031 v2.1b: tilled soil inside the field enclosures (was never stamped)
            PaintRoutes(S, am, AlphaRes, liPath, liCobble); // D-116/120/879 EMERGENT ROADS: tie-derived, tech-tiered trail→path→paved
            // D-878 it.6 MEASURED: every OTHER birth came out with an all-grass alphamap (Editor.log runs 4 and 6: grass=65536,
            // field/dirt/gravel=0) while the same code painted 55745/360/7850/1573 in runs 1–3 and 5. Only the reuse of the
            // existing TerrainData_generated.asset differed between runs — so the old asset is deleted first and the alphamap
            // is written AFTER the data has become a persistent asset.
            // D-881 MEASURED (13:03 birth): with the TerrainData as an ASSET, any save/reimport of it (SaveAssets at birth end, a
            // domain reload after RUN_COMPILE) hands back an all-grass alphamap — the splat textures SetAlphamaps creates are
            // not carried through the import (dresser diag right after SetAlphamaps: grass=969543 of 1048576; TerrainDiag a
            // few calls later: 1048576). The dressed world is rebuilt from S every time and never saved into the scene, so the
            // TerrainData now lives IN MEMORY only — no asset, nothing to reimport, nothing to go stale.
            if (!string.IsNullOrEmpty(PersistTerrainPath))
            {
                // D-891 build path: D-878's PROVEN-deterministic order — DeleteAsset → CreateAsset (empty) → SetAlphamaps →
                // Save. Creating the asset BEFORE SetAlphamaps makes the alphamap textures serialize as sub-assets, so the
                // splat survives the scene save + the build's reimport (the in-memory path below loses it on serialize).
                if (AssetDatabase.LoadAssetAtPath<TerrainData>(PersistTerrainPath) != null) AssetDatabase.DeleteAsset(PersistTerrainPath);
                data.name = "TerrainData_diorama";
                AssetDatabase.CreateAsset(data, PersistTerrainPath);
                data.SetAlphamaps(0, 0, am);
                // D-891: bake dominant-layer-per-cell to a .bytes TextAsset — this serializes into the build reliably
                // (TerrainData alphamaps do not), and EmergenceTerrainSplat re-applies it at runtime.
                int bw = data.alphamapWidth, bh = data.alphamapHeight, bl = am.GetLength(2);
                var splatBytes = new byte[9 + bw * bh];
                System.BitConverter.GetBytes(bw).CopyTo(splatBytes, 0);
                System.BitConverter.GetBytes(bh).CopyTo(splatBytes, 4);
                splatBytes[8] = (byte)bl;
                for (int by = 0; by < bh; by++)
                    for (int bx = 0; bx < bw; bx++)
                    { int di = 0; float dv = -1f; for (int l = 0; l < bl; l++) if (am[by, bx, l] > dv) { dv = am[by, bx, l]; di = l; } splatBytes[9 + by * bw + bx] = (byte)di; }
                var splatPath = System.IO.Path.ChangeExtension(PersistTerrainPath, null) + "_splat.bytes";
                System.IO.File.WriteAllBytes(splatPath, splatBytes);
                AssetDatabase.ImportAsset(splatPath);
                Debug.Log("[Dresser] D-891 terrain persisted → " + PersistTerrainPath + " + splat.bytes " + (bw*bh) + " cells → " + splatPath);
            }
            else
            {
                const string TdPath = "Assets/Emergence/Scenes/TerrainData_generated.asset";
                if (AssetDatabase.LoadAssetAtPath<TerrainData>(TdPath) != null) AssetDatabase.DeleteAsset(TdPath); // retire the old asset
                data.name = "TerrainData_generated (in-memory, D-881)";
                data.SetAlphamaps(0, 0, am);
            }
            var tgo = Terrain.CreateTerrainGameObject(data);
            tgo.name = "Terrain";
            tgo.transform.SetParent(root, true);
            tgo.transform.position = new Vector3(0, -3f, 0);
            var terrain = tgo.GetComponent<Terrain>();
            // TD-031 terrain pass: alphamap weights ARE stored (diag) but the shared TerrainLit material
            // renders only the base layer — force a FRESH material instance bound to this terrain so the
            // splat keywords/layer-count rebind, enable instanced draw, and rebuild the basemap.
            string matBefore = terrain.materialTemplate != null ? terrain.materialTemplate.name + "/" + terrain.materialTemplate.shader.name : "NULL";
            var urpTerrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (urpTerrainShader != null)
                terrain.materialTemplate = new Material(urpTerrainShader) { name = "EmergenceTerrainLit" };
            // D-120: >4 terrain layers → enable URP's 8-layer path so layers 5-8 (cobblestone) actually render.
            if (terrain.materialTemplate != null && data.terrainLayers.Length > 4)
                terrain.materialTemplate.EnableKeyword("_TERRAIN_8_LAYERS");
            terrain.drawInstanced = true;
            MeadowDetailAndTrees(S, data, terrain);   // D-101: the pack's OWN detail-grass + tree scatter (the meadow)
            data.SetBaseMapDirty();
            terrain.Flush();
            // DIAGNOSTIC (written to Logs/terrain-diag.txt so it's readable without the editor UI):
            // read the alphamap back and count where each layer's weight > 0.5 — this splits
            // "alphamap not stored" (counts 0) from "stored but not rendered" (counts > 0).
            var chk = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            int cf = 0, cd = 0, cg = 0, cgrass = 0;
            for (int yy = 0; yy < data.alphamapHeight; yy++)
                for (int xx = 0; xx < data.alphamapWidth; xx++)
                {
                    if (chk[yy, xx, liGrass] > 0.5f) cgrass++;
                    if (data.alphamapLayers > liField && chk[yy, xx, liField] > 0.5f) cf++;
                    if (data.alphamapLayers > liPath && chk[yy, xx, liPath] > 0.5f) cd++;
                    if (data.alphamapLayers > liGravel && chk[yy, xx, liGravel] > 0.5f) cg++;
                }
            var diag = $"[terrain-diag] terrainLayers={data.terrainLayers.Length} alphamapLayers={data.alphamapLayers} alphaRes={data.alphamapResolution}\n"
                     + $"material before={matBefore} after={(terrain.materialTemplate != null ? terrain.materialTemplate.name + "/" + terrain.materialTemplate.shader.name : "NULL")}\n"
                     + $"layers: {string.Join(", ", data.terrainLayers.Select((l, i) => i + ":" + (l != null ? l.name : "null")))}\n"
                     + $"alphamap cells >0.5  grass={cgrass} field={cf} dirt={cd} gravel={cg} (of {data.alphamapWidth * data.alphamapHeight})\n"
                     + $"basemapDistance={terrain.basemapDistance} drawInstanced={terrain.drawInstanced}\n";
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/terrain-diag.txt", diag);
            Debug.Log(diag);
        }

        // D-101: try each candidate layer name in order (Dreamscape's real textured layer first),
        // fall back to a flat-colour layer only if none of the named assets exist.
        static int AddLayer(List<TerrainLayer> layers, string[] candidates, Color fallback)
        {
            TerrainLayer tl = null;
            foreach (var nm in candidates)
            {
                foreach (var g in AssetDatabase.FindAssets($"t:TerrainLayer {nm}"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(p) == nm)
                    { tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p); break; }
                }
                if (tl != null) break;
            }
            if (tl == null)
            {
                tl = new TerrainLayer { diffuseTexture = Texture2D.whiteTexture, diffuseRemapMax = new Vector4(fallback.r, fallback.g, fallback.b, 1) };
                AssetDatabase.CreateAsset(tl, $"Assets/Emergence/Scenes/TL_{candidates[0]}.asset");
            }
            layers.Add(tl);
            return layers.Count - 1;
        }

        // ---- D-101 meadow helpers ---------------------------------------------------------------
        // settle-to-flat weight (0..1) near any village centre, so building pads are level
        static float VillageFlatten(WorldState S, float sx, float sy)
        {
            if (S.villages == null) return 0f;
            float best = 0f;
            foreach (var v in S.villages)
            {
                float d = Mathf.Sqrt((v.x - sx) * (v.x - sx) + (v.y - sy) * (v.y - sy));
                float w = Mathf.Clamp01(1f - d / 6f);   // ~6 tiles of levelling around each green
                if (w > best) best = w;
            }
            return best * best;
        }
        static bool NearVillage(WorldState S, int x, int y, float tiles)
        {
            if (S.villages == null) return false;
            foreach (var v in S.villages)
                if ((v.x - x) * (v.x - x) + (v.y - y) * (v.y - y) < tiles * tiles) return true;
            return false;
        }

        // THE MEADOW (D-101): adopt Dreamscape's OWN treatment wholesale — terrain-detail waving grass
        // (their exact detail prefabs + waving params, GPU-instanced, dense & cheap) and their birch/
        // bush/mushroom tree prototypes scattered across the open grassland. This is the single biggest
        // visual lift and it was never used (PlaceGrass was a disabled GameObject stopgap). RNG-neutral.
        static void MeadowDetailAndTrees(WorldState S, TerrainData data, Terrain terrain)
        {
            // -- detail grass + wildflowers (their reference detail set) --
            // D-878: pack table (Nature doc s.21): flowers/leaves/bushes with vertex maps = "Detail Mesh – grass" (wind via terrain);
            // GRASS itself = Tree Objects (custom shader kept) — placed below as terrain tree instances, never as detail.
            string[] protoNames = { "P_ENV_PLANT_flower_v1_01", "P_ENV_PLANT_flower_v1_02", "P_ENV_PLANT_flower_v1_03", "P_ENV_PLANT_leaf_v1_01", "P_ENV_PLANT_leaf_v2_01" };
            // D-101c: per-layer green variation so the meadow isn't one flat tone — some cooler, some
            // warmer-lit; flowers keep a white tint so their own texture colour shows.
            Color[] grassGreens = { new Color(0.82f, 0.95f, 0.70f), new Color(0.68f, 0.86f, 0.55f), new Color(0.90f, 0.93f, 0.72f) };
            var dps = new List<DetailPrototype>();
            var isFlower = new List<bool>();
            int gi = 0;
            foreach (var nm in protoNames)
            {
                var pf = FindPrefabIn(NatureRoot, nm) ?? FindPrefabExact(nm);
                if (pf == null) continue;
                bool flower = nm.ToLower().Contains("flower");
                dps.Add(new DetailPrototype
                {
                    prototype = pf,
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.Grass,   // D-878: "Detail Mesh – grass" — the pack's wind-via-vertexmap path
                    minWidth = flower ? 0.8f : 0.9f, maxWidth = flower ? 1.3f : 1.7f,
                    minHeight = flower ? 0.8f : 1.0f, maxHeight = flower ? 1.3f : 1.9f, // lusher, taller grass
                    noiseSpread = flower ? 2.5f : 1.4f,
                    healthyColor = flower ? Color.white : grassGreens[gi % grassGreens.Length],
                    dryColor = flower ? new Color(0.95f, 0.9f, 0.7f) : new Color(0.80f, 0.78f, 0.48f, 1f)
                });
                isFlower.Add(flower);
                if (!flower) gi++;
            }
            if (dps.Count > 0)
            {
                data.detailPrototypes = dps.ToArray();
                int dres = 512;
                data.SetDetailResolution(dres, 16);
                for (int p = 0; p < dps.Count; p++)
                {
                    var map = new int[dres, dres];
                    bool flower = isFlower[p];
                    for (int dy = 0; dy < dres; dy++)
                        for (int dx = 0; dx < dres; dx++)
                        {
                            float sx = dx / (float)(dres - 1) * (S.W - 1);
                            float sy = (1f - dy / (float)(dres - 1)) * (S.H - 1);
                            int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                            if (Tile(S, tx, ty) != 'g') continue;
                            float h = Hash01(dx, dy, 700 + p);
                            if (flower) { if (h > 0.86f) map[dy, dx] = h > 0.97f ? 2 : 1; } // fuller wildflower drifts
                            else { map[dy, dx] = h < 0.10f ? 0 : (h < 0.5f ? 2 : 3); }       // denser, taller waving grass
                        }
                    data.SetDetailLayer(0, 0, p, map);
                }
                data.wavingGrassStrength = 0.383f;
                data.wavingGrassSpeed = 0.066f;
                data.wavingGrassAmount = 0.235f;
                data.wavingGrassTint = new Color(0.538f, 0.538f, 0.538f, 1f);
                terrain.detailObjectDistance = 160f;
                terrain.detailObjectDensity = 1.0f;
            }
            else Debug.LogWarning("[Dresser] no Nature detail plants found — meadow detail skipped");

            // D-878: THE GRASS — FANTASTIC Nature P_GRASS_* as terrain TREE instances (pack table s.21: assets with custom
            // shaders go in as Tree Objects, never as terrain grass — the built-in grass shader would override the wind
            // shader). Hash-placed, RNG-neutral. Density GrassPerSqm over open 'g' tiles, clear of fields.
            var grassPfs = FindPrefabsIn(NatureRoot, "P_GRASS_");
            if (grassPfs.Length > 0)
            {
                var fieldSet2 = new HashSet<(int, int)>();
                if (S.fields != null) foreach (var f in S.fields) fieldSet2.Add((Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y)));
                var protos = new List<TreePrototype>();
                foreach (var p in grassPfs) protos.Add(new TreePrototype { prefab = p, bendFactor = 0f });
                var existing = data.treePrototypes ?? new TreePrototype[0];
                int baseIdx = existing.Length;
                data.treePrototypes = existing.Concat(protos).ToArray();
                var inst = new List<TreeInstance>(data.treeInstances ?? new TreeInstance[0]);
                int perTile = Mathf.Max(1, Mathf.RoundToInt(GrassPerSqm * TileSize * TileSize));
                var tsize = data.size;
                for (int y = 0; y < S.H; y++)
                    for (int x = 0; x < S.W; x++)
                    {
                        if (Tile(S, x, y) != 'g' || fieldSet2.Contains((x, y))) continue;
                        for (int i = 0; i < perTile; i++)
                        {
                            float jx = Hash01(x, y, 900 + i), jy = Hash01(x, y, 950 + i);
                            var w = Ground(S, x + jx - 0.5f, y + jy - 0.5f);
                            var local = w - terrain.transform.position;
                            inst.Add(new TreeInstance
                            {
                                prototypeIndex = baseIdx + (int)(Hash(x, y, 1000 + i) % (uint)protos.Count),
                                position = new Vector3(Mathf.Clamp01(local.x / tsize.x), 0f, Mathf.Clamp01(local.z / tsize.z)),
                                widthScale = 0.9f + Hash01(x, y, 1050 + i) * 0.4f, heightScale = 0.9f + Hash01(x, y, 1100 + i) * 0.4f,
                                rotation = Hash01(x, y, 1150 + i) * 6.2831853f, color = Color.white, lightmapColor = Color.white
                            });
                        }
                    }
                data.SetTreeInstances(inst.ToArray(), true);
                terrain.treeDistance = 220f; terrain.treeBillboardDistance = 120f; terrain.treeCrossFadeLength = 20f; terrain.treeMaximumFullLODCount = 400;
                Debug.Log($"[Dresser] D-878 grass: {inst.Count} Nature P_GRASS tree-instances over the open meadow ({grassPfs.Length} prototypes)");
            }
            else Debug.LogWarning("[Dresser] no Nature P_GRASS_ prefabs — meadow grass skipped");

            // -- trees as GAMEOBJECTS, not Unity terrain trees (D-101f). THE FIX: terrain trees render
            // through a separate path that ignores our fill light AND doesn't reflect material edits — that
            // was the "dark blob" (immune to 12 material/shader/reimport attempts). GameObjects light exactly
            // like the bushes that already read well. Sparse scatter over open meadow, clear of villages.
            // D-878: meadow trees from FANTASTIC Nature (wood_01 colour variant; the other variants are the season/biome hook)
            var treePfs = FindPrefabsIn(NatureRoot, "P_ENV_TREE_v1_", "_wood_01").ToArray();   // it.2 SEEN: v4 = large-leaf (tropical) trees — wrong biome
            if (treePfs.Length > 0)
            {
                var tparent = new GameObject("MeadowTrees").transform; tparent.SetParent(terrain.transform.root, true);
                int nt = 0;
                for (int y = 0; y < S.H; y++)
                    for (int x = 0; x < S.W; x++)
                    {
                        if (Tile(S, x, y) != 'g') continue;
                        if (Hash01(x, y, 760) > 0.05f) continue;      // sparse scatter (~5% of grass tiles)
                        if (NearVillage(S, x, y, 3f)) continue;       // keep building pads & greens clear
                        var pf = treePfs[Hash(x, y, 761) % (uint)treePfs.Length];
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, tparent);
                        float jx = Hash01(x, y, 762) - 0.5f, jy = Hash01(x, y, 763) - 0.5f;
                        go.transform.position = Ground(S, x + jx * 0.8f, y + jy * 0.8f);
                        go.transform.rotation = Quaternion.Euler(0, Hash(x, y, 765) % 360u, 0);
                        float sc = 0.8f + Hash01(x, y, 764) * 0.7f;
                        go.transform.localScale = Vector3.one * sc;
                        StripImpostorLods(go); // avoid the unlit billboard LOD (magenta/dark at distance)
                        nt++;
                    }
                Debug.Log($"[Dresser] meadow: {dps.Count} detail-grass layers + {nt} GameObject trees (D-101f, fill-lit like the bushes)");
            }
        }

        // TD-031 v2.1b: stamp tilled soil (the field layer) at every sim field cell, so the enclosed
        // infield reads as worked earth, not grass. Also the diagnostic for splat rendering: a big
        // unoccluded brown patch that either shows (splat works) or doesn't (material-level splat bug).
        static void StampFields(WorldState S, float[,,] am, int liField, int res)
        {
            if (S.fields == null) return;
            int nlayers = am.GetLength(2);
            foreach (var f in S.fields)
            {
                int ax = Mathf.RoundToInt(f.x / Mathf.Max(1, S.W - 1) * (res - 1));
                int ay = Mathf.RoundToInt((1f - f.y / Mathf.Max(1, S.H - 1)) * (res - 1));
                int rad = Mathf.Max(1, Mathf.RoundToInt(0.5f * (res - 1) / Mathf.Max(1, S.W - 1))); // D-879: half a tile in cells (1 at 256, 5 at 1024)
                for (int dy = -rad; dy <= rad; dy++)
                    for (int dx = -rad; dx <= rad; dx++)
                    {
                        int cx = ax + dx, cy = ay + dy;
                        if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                        // D-115: tilled soil that BLENDS into grass (was pure field=1 → a hard grey checkerboard).
                        // Noise keeps the earth mottled; the rest weight goes to grass (layer 0) so edges soften.
                        float n = Mathf.PerlinNoise(cx * 0.35f + 3f, cy * 0.35f + 7f);
                        for (int l = 0; l < nlayers; l++) am[cy, cx, l] = 0f;
                        am[cy, cx, liField] = 0.72f + n * 0.18f;
                        am[cy, cx, 0] = 1f - am[cy, cx, liField];   // layer 0 = grass

                    }
            }
        }

        // D-116 EMERGENT ROADS v1 (approved spec EMERGENT-ROADS-SPEC.md). A road exists because people have
        // WALKED it, its wear is HOW MUCH they walk it, all derived from sim state — never RNG, never authored.
        // Sources: hut→its village green (daily life, wear∝pop); village→village ONLY where a real tie exists
        // (shared culture + walkable reach + trade tech — NOT nearest-neighbour); wear→brush WIDTH; tech-gated
        // COBBLE tier (both hold masonry + high traffic) else dirt. Genesis: no huts ⇒ no roads. Growth/overgrow
        // fall out of the per-year rebuild (more ties → more roads; a lost village → its roads gone). Deterministic.
        struct Route { public Vector2 a, b; public float wear; public int layer; public int tier; } // tier: 0 trail, 1 path, 2 paved (D-879)

        // D-879 (Patrik 2026-09-27): "vägarna kanske ska vara smalare som stigar i början och sen när civ utvecklas så kan
        // dom bli bredare och stenlagda." Tier is READ from the village's tech (sim state), never authored:
        //   TRAIL  — no road tech: a trodden line of worn dirt, ~1,5 m, grass showing through
        //   PATH   — the village knows 'road' or 'wheel': carts widen it to ~3 m of gravel
        //   PAVED  — both ends know 'road' AND 'masonry': a ~4,5 m paved street (pavingstone blended with worn dirt)
        // MEASURED 2026-09-27 (seq-8919 y55/85, 97013, 900913): every village already knows road+wheel+masonry the year it is
        // founded — tech alone would pave everything at once. So the tier also needs the traffic to justify it: PATH from
        // 10 souls, PAVED from 20 (a paved street is a public work, not a hamlet's).
        static int RoadTier(WorldVillage v)
        {
            if (v == null) return 0;
            bool road = Holds(v, "road") || Holds(v, "wheel"), stone = Holds(v, "road") && Holds(v, "masonry");
            if (stone && v.pop >= 20) return 2;
            if (road && v.pop >= 10) return 1;
            return 0;
        }

        // D-879 TRODDEN GROUND: where the engine has counted footfall (pathUse, R2 INK1), the grass wears to earth in
        // proportion — the village heart becomes trodden dirt, the forage spokes faint worn lines. Bilinear over the
        // 8 m tiles so the wear reads as ground, not as a checkerboard. Log-scaled: a tile walked 1/10 as much as the
        // busiest is still a visible line. Pure read of S; RNG-neutral.
        static int PaintTrodden(WorldState S, float[,,] am, int res, int liDirt, int liGrass)
        {
            if (S.pathUse == null || S.pathUse.Length < S.W * S.H) return 0;
            int max = 0; foreach (var u in S.pathUse) if (u > max) max = u;
            if (max <= 0) return 0;
            float lmax = Mathf.Log(1f + max);
            var wear = new float[S.H, S.W];
            for (int y = 0; y < S.H; y++) for (int x = 0; x < S.W; x++) { int u = S.pathUse[y * S.W + x]; wear[y, x] = u > 0 ? Mathf.Log(1f + u) / lmax : 0f; }
            int cells = 0;
            for (int ay = 0; ay < res; ay++)
                for (int ax = 0; ax < res; ax++)
                {
                    float sx = ax / (float)(res - 1) * (S.W - 1);
                    float sy = (1f - ay / (float)(res - 1)) * (S.H - 1);
                    int x0 = Mathf.Clamp((int)sx, 0, S.W - 2), y0 = Mathf.Clamp((int)sy, 0, S.H - 2);
                    float fx = Mathf.Clamp01(sx - x0), fy = Mathf.Clamp01(sy - y0);
                    float f = Mathf.Lerp(Mathf.Lerp(wear[y0, x0], wear[y0, x0 + 1], fx), Mathf.Lerp(wear[y0 + 1, x0], wear[y0 + 1, x0 + 1], fx), fy);
                    if (f < 0.45f) continue;                                   // lightly walked grass stays grass
                    float w = Mathf.Clamp01((f - 0.45f) / 0.55f) * 0.85f;       // the busiest heart is ~85 % bare earth
                    w *= 0.85f + 0.3f * Mathf.PerlinNoise(sx * 0.7f + 31f, sy * 0.7f + 13f); // mottled, not a gradient
                    w = Mathf.Clamp01(w);
                    int nl = am.GetLength(2);
                    for (int l = 0; l < nl; l++) am[ay, ax, l] *= 1f - w;
                    am[ay, ax, liDirt] += w;
                    cells++;
                }
            return cells;
        }

        static bool Holds(WorldVillage v, string tech) => v.knows != null && System.Array.IndexOf(v.knows, tech) >= 0;
        static bool HoldsTrade(WorldVillage v) => Holds(v, "wheel") || Holds(v, "sailing");
        static int SharedCulture(WorldVillage a, WorldVillage b)
        {
            int n = 0;
            if (!string.IsNullOrEmpty(a.cosmos) && a.cosmos == b.cosmos) n++;
            if (a.beliefs != null && b.beliefs != null)
                foreach (var x in a.beliefs) if (System.Array.IndexOf(b.beliefs, x) >= 0) n++;
            return n;
        }

        static List<Route> ComputeRoutes(WorldState S, int liDirt, int liCobble)
        {
            var routes = new List<Route>();
            var greens = VillageGreens(S);
            // 1) hut → its village green: the trodden centre. Always present, wear grows with the village.
            if (S.huts != null)
                foreach (var h in S.huts)
                {
                    int vi = NearestVillageIdx(S, h.x, h.y);
                    Vector2 g = (vi >= 0 && greens != null && vi < greens.Length) ? greens[vi] : new Vector2(h.x, h.y);
                    int pop = (vi >= 0 && S.villages != null && vi < S.villages.Length) ? S.villages[vi].pop : 2;
                    var hv = (vi >= 0 && S.villages != null && vi < S.villages.Length) ? S.villages[vi] : null;
                    int htier = Mathf.Min(RoadTier(hv), 1); // a hut's own lane is never paved — the street is, the doorstep path is not
                    routes.Add(new Route { a = new Vector2(h.x, h.y), b = g, wear = Mathf.Clamp01(0.30f + pop / 45f), layer = liDirt, tier = htier });
                }
            // 2) village → village: ONLY where a real relationship exists (shared culture OR mutual trade tech,
            //    within a walkable reach). This is the "not random" fix — never nearest-neighbour geometry.
            if (S.villages != null)
                for (int i = 0; i < S.villages.Length; i++)
                    for (int j = i + 1; j < S.villages.Length; j++)
                    {
                        var vi = S.villages[i]; var vj = S.villages[j];
                        float dist = Vector2.Distance(new Vector2(vi.x, vi.y), new Vector2(vj.x, vj.y));
                        if (dist > 55f) continue;                                   // beyond a day's reach → no track forms
                        int shared = SharedCulture(vi, vj);
                        bool trade = HoldsTrade(vi) && HoldsTrade(vj);
                        if (shared <= 0 && !trade) continue;                        // no tie → no road
                        float traffic = Mathf.Min(vi.pop, vj.pop) * (1 + shared) / Mathf.Max(8f, dist);
                        float wear = Mathf.Clamp01(0.40f + traffic * 0.10f);
                        // D-120 v1.1: the tech-gated COBBLE tier + URP 8-layer rendering is PROVEN (below) — but the
                        // Dreamscape Cobblestone TEXTURE reads as the light checkerboard the EP disliked, so the paved
                        // tier is HELD at warm dirt (width still marks the busy trade route) pending a better paved
                        // texture / EP confirmation. Re-enable true cobble by flipping this layer to liCobble.
                        int tier = Mathf.Min(RoadTier(vi), RoadTier(vj)); // the road is as developed as its lesser end
                        routes.Add(new Route { a = new Vector2(vi.x, vi.y), b = new Vector2(vj.x, vj.y), wear = wear, layer = tier == 2 ? liCobble : liDirt, tier = tier });
                    }
            return routes;
        }

        static void PaintRoutes(WorldState S, float[,,] am, int res, int liDirt, int liCobble)
        {
            var routes = ComputeRoutes(S, liDirt, liCobble);
            int nlayers = am.GetLength(2);
            int cells = 0, cobbleRoutes = 0;
            foreach (var r in routes)
            {
                if (r.layer == liCobble) cobbleRoutes++;
                // D-879 WIDTH by tier: metres → cells (0,78 m/cell at 1024). Trail ~1,5 m · path ~3 m · paved ~4,5 m;
                // a heavily worn route (wear > 0,7) gains one cell. Soft-edged brush so the verge frays into grass.
                float cellM = Mathf.Max(1, S.W - 1) * TileSize / (res - 1);
                float halfW = r.tier == 2 ? 2.25f : r.tier == 1 ? 1.5f : 0.75f;
                int rr = Mathf.Max(1, Mathf.RoundToInt(halfW / cellM)) + (r.wear > 0.7f ? 1 : 0);
                float strength = r.tier == 0 ? 0.8f : 1f;                              // a trail never fully hides the grass
                float len = Vector2.Distance(r.a, r.b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 4f));
                for (int s = 0; s <= steps; s++)
                {
                    var p = Vector2.Lerp(r.a, r.b, s / (float)steps);
                    int ax = Mathf.RoundToInt(p.x / Mathf.Max(1, S.W - 1) * (res - 1));
                    int ay = Mathf.RoundToInt((1f - p.y / Mathf.Max(1, S.H - 1)) * (res - 1));
                    for (int dy = -rr; dy <= rr; dy++)
                        for (int dx = -rr; dx <= rr; dx++)
                        {
                            float d2 = dx * dx + dy * dy;
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
                            cells++;
                        }
                }
            }
            int t0 = routes.Count(x => x.tier == 0), t1 = routes.Count(x => x.tier == 1);
            Debug.Log($"[Dresser] EMERGENT ROADS: {routes.Count} routes (trail {t0} / path {t1} / paved {cobbleRoutes}), {cells} cells painted at {res} — tie-derived, tech-tiered width (D-116/D-879)");
        }

        // D-115: Dreamscape's own ambient particle FX — drifting leaves (Leaf_Particle_Wind) + dust motes
        // (Dust_Particle) sparsely over the open meadow. Atmosphere the still render can't show (particles
        // need play mode) but the EP sees on Play. Deterministic hash scatter (D-078 r4), never RNG.
        static void PlaceAmbientFX(WorldState S, Transform root)
        {
            var parent = new GameObject("AmbientFX").transform; parent.SetParent(root, true);
            var leaf = FindPrefabIn(NatureRoot, "P_FX_leaves_FNP");   // D-878: the pack's own drifting leaves
            GameObject dust = null;                                     // no dust motes in the Nature pack — leaves only
            if (leaf == null && dust == null) { Debug.LogWarning("[Dresser] no Nature ambient particles found"); return; }
            int placed = 0;
            for (int y = 6; y < S.H; y += 14)
                for (int x = 6; x < S.W; x += 14)
                {
                    if (Tile(S, x, y) != 'g') continue;                 // open meadow only
                    var pf = (Hash(x, y, 501) % 2u == 0u) ? leaf : dust;
                    if (pf == null) pf = leaf ?? dust;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                    go.transform.position = Ground(S, x + (Hash01(x, y, 7) - 0.5f) * 4f, y + (Hash01(x, y, 8) - 0.5f) * 4f, 2.5f);
                    placed++;
                }
            Debug.Log($"[Dresser] {placed} Nature ambient FX (drifting leaves)");
        }

        static void BuildWater(WorldState S, Transform root)
        {
            // D-101d: Dreamscape lake/river material on a basin-fitted quad per water tile.
            var parent = new GameObject("Water").transform; parent.SetParent(root, true);
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                    if (Tile(S, x, y) == 'w' && Hash(x, y, 7) % 1 == 0)
                    {
                        {
                            // D-115: use Dreamscape's OWN water PREFAB (Prefab_WaterLake / SM_WaterRiver — their
                            // showcase water mesh + shader + foam), scaled by its mesh bounds to one sim tile.
                            var pf = FindPrefabIn(NatureRoot, "P_FX_water_FNP") ?? FindPrefab("Prefab_WaterLake") ?? FindPrefab("SM_WaterRiver"); // D-878: Nature water prefab rig first (TD-PLAYBOOK: pack water needs its prefab)
                            if (pf != null)
                            {
                                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                                go.name = "w";
                                go.transform.position = Ground(S, x, y, 0.25f);
                                var mf = go.GetComponentInChildren<MeshFilter>();
                                var bs = (mf != null && mf.sharedMesh != null) ? mf.sharedMesh.bounds.size : Vector3.one;
                                float baseSize = Mathf.Max(0.01f, Mathf.Max(bs.x, bs.z));
                                float sc = (TileSize * 1.02f) / baseSize;
                                go.transform.localScale = new Vector3(sc, sc, sc);
                            }
                            else
                            {
                                // fallback: their water material on a flat quad
                                var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                                plane.name = "w"; plane.transform.SetParent(parent, true);
                                plane.transform.position = Ground(S, x, y, 0.25f);
                                plane.transform.rotation = Quaternion.Euler(90, 0, 0);
                                plane.transform.localScale = new Vector3(TileSize * 1.02f, TileSize * 1.02f, 1);
                                var wm = FindMaterial("M_ENV_water") ?? FindMaterial("water");
                                if (wm != null) plane.GetComponent<MeshRenderer>().sharedMaterial = wm;
                                else plane.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0.23f, 0.42f, 0.55f);
                            }
                        }
                    }
        }

        // TD-031 composition grammar v2: the "tun" reading — a house turns its door side toward the
        // village GREEN (the centroid of its own village's huts, where the well/fire commons sits),
        // not a random ±10° grid. A deterministic ±7° jitter keeps it lived-in, not mechanical.
        // Everything from sim data + position hashes — never RNG (D-078 rule 4).
        // TD-031 terrain pass — GUARANTEED ground rendering via mesh decals. The URP terrain won't
        // render splat layers beyond base grass on our procedural TerrainData (weights ARE stored, the
        // TerrainLit material is correct, fresh-material + drawInstanced + basemap-rebuild all no-op —
        // a URP procedural-terrain quirk). So we lay flat textured QUADS for the ground features, exactly
        // like the water quads (which render fine): tilled soil in the field enclosures + worn dirt along
        // the desire lines (hut->green, village->village). Deterministic geometry, textures from the pack
        // TerrainLayers. y-lift avoids z-fighting with the terrain surface.
        static Material GroundMat(string layerName, Color fallback)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh) { name = "GroundDecal_" + layerName };
            var guid = AssetDatabase.FindAssets($"t:TerrainLayer {layerName}").FirstOrDefault();
            if (guid != null)
            {
                var tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(guid));
                if (tl != null && tl.diffuseTexture != null)
                {
                    m.mainTexture = tl.diffuseTexture;
                    float tsx = tl.tileSize.x > 0.1f ? tl.tileSize.x : 8f, tsy = tl.tileSize.y > 0.1f ? tl.tileSize.y : 8f;
                    m.mainTextureScale = new Vector2(TileSize / tsx, TileSize / tsy);
                    m.SetFloat("_Smoothness", 0f);
                    return m;
                }
            }
            m.color = fallback; m.SetFloat("_Smoothness", 0f);
            return m;
        }

        static void Decal(WorldState S, Transform parent, Material mat, float sx, float sy, float size, float lift, string name)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            q.transform.SetParent(parent, true);
            q.transform.position = Ground(S, sx, sy, lift);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // lie flat, normal +Y
            q.transform.localScale = new Vector3(size, size, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat;
            var col = q.GetComponent<Collider>(); if (col != null) UnityEngine.Object.DestroyImmediate(col);
        }

        // TD-032: THE MEADOW — scatter Dreamscape's own waving grass clumps (Foliage wind shadergraph,
        // LOD'd) densely across the open grassland. This is the treatment their reference/showcase scenes
        // use and we never did — we'd only used their tree/rock prefabs. Answers EP: "gräset syns inte /
        // vajar inte i vinden". Hash-placed, RNG-neutral (D-078 r4). Skips tilled fields; grass on 'g' tiles.
        static void PlaceGrass(WorldState S, Transform root)
        {
            var parent = new GameObject("Grass").transform; parent.SetParent(root, true);
            var grass = new[] { "Prefab_Grass_Group_01", "Prefab_Grass_Group_02", "Prefab_Grass_01", "Prefab_Grass_02", "Prefab_Grass_03" }
                .Select(FindPrefab).Where(p => p != null).ToArray();
            if (grass.Length == 0) { Debug.LogWarning("[Dresser] no Dreamscape grass prefabs found — meadow skipped"); return; }
            var fieldSet = new HashSet<(int, int)>();
            if (S.fields != null) foreach (var f in S.fields) fieldSet.Add((Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y)));
            int placed = 0;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    if (Tile(S, x, y) != 'g') continue;         // open grassland only
                    if (fieldSet.Contains((x, y))) continue;    // not on tilled soil
                    int count = Mathf.FloorToInt(GrassPerTile) + (Hash01(x, y, 111) < GrassPerTile - Mathf.Floor(GrassPerTile) ? 1 : 0);
                    for (int i = 0; i < count; i++)
                    {
                        var pf = grass[Hash(x, y, 112 + i) % (uint)grass.Length];
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                        float jx = Hash01(x, y, 113 + i) - 0.5f, jy = Hash01(x, y, 114 + i) - 0.5f;
                        go.transform.position = Ground(S, x + jx, y + jy, 0f);
                        go.transform.rotation = Quaternion.Euler(0f, Hash(x, y, 115 + i) % 360u, 0f);
                        go.transform.localScale = Vector3.one * (0.8f + Hash01(x, y, 116 + i) * 0.5f) * GrassScale;
                        placed++;
                    }
                }
            Debug.Log($"[Dresser] {placed} Dreamscape grass clumps (waving foliage) across the open meadow");
        }

        // D-115: DEFAULT OFF — the empirical splat test proved the painted terrain renders paths/fields on its own,
        // so the mesh-decal "plates" workaround is retired. Paths/fields = painted terrain splat (pack-correct:
        // smooth, terrain-following, grass auto-masked). Kept as a toggle only for A/B diagnostics.
        public static bool GroundDecals = false;

        static void PlaceGroundFeatures(WorldState S, Transform root)
        {
            if (!GroundDecals) { Debug.Log("[Dresser] GroundDecals OFF — terrain splat only (no decal plates)."); return; }
            var parent = new GameObject("GroundFeatures").transform; parent.SetParent(root, true);
            var fieldMat = GroundMat("Layer_farmfield", new Color(0.42f, 0.32f, 0.20f));
            var dirtMat = PathMat("Layer_Dirt", new Color(0.46f, 0.36f, 0.24f));
            int fields = 0, path = 0;
            // field soil: one quad per field tile, inside the enclosures
            if (S.fields != null)
                foreach (var f in S.fields) { Decal(S, parent, fieldMat, f.x, f.y, TileSize * 0.98f, 0.06f, $"fieldsoil_{fields}"); fields++; }
            // TD-032 (EP: paths were "tråkiga" + use pack content): village STREETS in Dreamscape
            // COBBLESTONE (hut->green — the trodden centre), inter-village TRAILS in worn DIRT.
            var cobbleMat = PathMat("Layer_Cobblestone", new Color(0.55f, 0.53f, 0.50f));
            var greens = VillageGreens(S);
            var streets = new List<(Vector2 a, Vector2 b)>();
            foreach (var h in S.huts)
            {
                int vi = NearestVillageIdx(S, h.x, h.y);
                streets.Add((new Vector2(h.x, h.y), (vi >= 0 && vi < greens.Length) ? greens[vi] : new Vector2(h.x, h.y)));
            }
            var trails = new List<(Vector2 a, Vector2 b)>();
            if (S.villages != null)
                for (int i = 0; i < S.villages.Length; i++)
                {
                    int nj = -1; float bd = float.MaxValue;
                    for (int j = 0; j < S.villages.Length; j++)
                    {
                        if (j == i) continue;
                        float d = (S.villages[i].x - S.villages[j].x) * (S.villages[i].x - S.villages[j].x) + (S.villages[i].y - S.villages[j].y) * (S.villages[i].y - S.villages[j].y);
                        if (d < bd) { bd = d; nj = j; }
                    }
                    if (nj > i) trails.Add((new Vector2(S.villages[i].x, S.villages[i].y), new Vector2(S.villages[nj].x, S.villages[nj].y)));
                }
            path += LayPath(S, parent, streets, cobbleMat, 3.0f, "street", path);
            path += LayPath(S, parent, trails, dirtMat, 3.8f, "trail", path);
            Debug.Log($"[Dresser] ground: {fields} field-soil quads + {path} continuous path ribbons (Dreamscape cobble streets + dirt trails, normal-mapped)");
        }

        static int LayPath(WorldState S, Transform parent, List<(Vector2 a, Vector2 b)> segs, Material mat, float w, string tag, int start)
        {
            int n = 0;
            // A (D-114): ONE continuous terrain-following ribbon per route (not stepped quads) — kills the
            // "plattor" look; the pack texture flows ALONG the path via UVs. Reusable by the future emergent
            // path source (reconciler feeds segments + a wear/width per route into this same renderer).
            foreach (var (a, b) in segs) { PathRibbon(S, parent, a, b, w, mat, 0.08f, 2f, $"{tag}_{start + n}"); n++; }
            return n;
        }

        // path material: pack TerrainLayer diffuse + NORMAL map (the plates only had flat diffuse); mesh UVs tile.
        static Material PathMat(string layerName, Color fallback)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Path_" + layerName };
            var guid = AssetDatabase.FindAssets($"t:TerrainLayer {layerName}").FirstOrDefault();
            if (guid != null)
            {
                var tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(guid));
                if (tl != null && tl.diffuseTexture != null)
                {
                    m.mainTexture = tl.diffuseTexture;
                    if (tl.normalMapTexture != null) { m.EnableKeyword("_NORMALMAP"); m.SetTexture("_BumpMap", tl.normalMapTexture); }
                    m.SetFloat("_Smoothness", 0.05f);
                    return m;
                }
            }
            m.color = fallback; m.SetFloat("_Smoothness", 0f);
            return m;
        }

        // continuous flat strip a->b, terrain-height sampled per cross-section, texture tiling ALONG length (texTile metres/repeat)
        static void PathRibbon(WorldState S, Transform parent, Vector2 a, Vector2 b, float worldWidth, Material mat, float lift, float texTile, string name)
        {
            float lenTiles = Vector2.Distance(a, b);
            if (lenTiles < 0.01f) return;
            int steps = Mathf.Max(2, Mathf.CeilToInt(lenTiles));           // ~1 cross-section per sim tile (follow relief)
            Vector3 aW = Ground(S, a.x, a.y, lift), bW = Ground(S, b.x, b.y, lift);
            Vector3 dir = bW - aW; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            dir.Normalize();
            Vector3 perp = new Vector3(-dir.z, 0f, dir.x) * (worldWidth * 0.5f);
            var verts = new List<Vector3>((steps + 1) * 2);
            var uvs = new List<Vector2>((steps + 1) * 2);
            var tris = new List<int>(steps * 6);
            float dist = 0f; Vector3 prev = aW;
            for (int i = 0; i <= steps; i++)
            {
                var ct = Vector2.Lerp(a, b, i / (float)steps);
                Vector3 c = Ground(S, ct.x, ct.y, lift);
                if (i > 0) { var d = c - prev; d.y = 0f; dist += d.magnitude; }
                prev = c;
                verts.Add(c - perp); verts.Add(c + perp);
                float v = dist / texTile;
                uvs.Add(new Vector2(0f, v)); uvs.Add(new Vector2(worldWidth / texTile, v));
                if (i > 0)
                {
                    int b0 = (i - 1) * 2;
                    tris.Add(b0); tris.Add(b0 + 1); tris.Add(b0 + 2);       // winding → normal +Y (faces up)
                    tris.Add(b0 + 1); tris.Add(b0 + 3); tris.Add(b0 + 2);
                }
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static void PlaceHuts(WorldState S, Transform root)
        {
            var parent = new GameObject("Huts").transform; parent.SetParent(root, true);
            var yardParent = new GameObject("Yards").transform; yardParent.SetParent(root, true);
            var ageParent = new GameObject("HutAge").transform; ageParent.SetParent(root, true);
            var greens = VillageGreens(S);
            var yardProps = YardPropNames.Select(FindPrefab).Where(p => p != null).ToArray();
            // TD-031 v2.2: TIME made visible. Age each hut by its OWNER's generation (sim state) —
            // old huts (founder generations, the settled heart) grow overgrown/mossy; new huts (later
            // generations, the expanding edge) carry fresh raw timber. Expansion rings become legible.
            var mossProps = FindPrefabs("Prefab_Bush").Where(p => p != null && !p.name.Contains("Flower")).Take(3).ToArray();
            var freshProps = new[] { "P_PROP_foundation_wood_01", "P_PROP_foundation_wood_03", "P_PROP_board_01", "P_PROP_board_02", "P_PROP_cart_wheel_small" }
                .Select(FindPrefab).Where(p => p != null).ToArray();
            var genOf = new Dictionary<string, int>(); int maxGen = 1;
            if (S.agents != null) foreach (var a in S.agents) { if (!string.IsNullOrEmpty(a.name)) genOf[a.name] = a.gen; if (a.gen > maxGen) maxGen = a.gen; }
            int yardCount = 0, ageMarks = 0;
            // D-882: wealth rank among the living (E1.5 agents[].wealth; old snapshots → 0 everywhere → everyone 0.5)
            var wealthRank = new Dictionary<string, float>();
            if (S.agents != null && S.agents.Length > 1)
            {
                // it.1 MEASURED (8919 y120): ranked among ALL the living, hut owners were all in the top → 32 of 33 houses "big".
                // Owners are the established adults; the rank that separates them is the rank among OWNERS.
                // it.2 MEASURED: names are NOT unique (D-876: two living "Eira II") — a rank keyed by name let a namesake's
                // wealth lift a penniless owner to 0,68. Wealth per name = the poorest namesake (conservative), and the
                // rank is the share of owners strictly poorer — ties (the many at 0) all rank low, as they should.
                var owners = new HashSet<string>(S.huts.Where(x => !string.IsNullOrEmpty(x.owner)).Select(x => x.owner));
                var wealthOf = new Dictionary<string, float>();
                foreach (var a in S.agents) if (!string.IsNullOrEmpty(a.name) && owners.Contains(a.name)) wealthOf[a.name] = wealthOf.TryGetValue(a.name, out var prev) ? Mathf.Min(prev, a.wealth) : a.wealth;
                var ws = wealthOf.Values.OrderBy(w => w).ToArray();
                bool anyWealth = ws.Length > 0 && ws[ws.Length - 1] > 0f;
                foreach (var kv in wealthOf) wealthRank[kv.Key] = anyWealth ? ws.Count(w => w < kv.Value) / (float)Mathf.Max(1, ws.Length - 1) : 0.5f;
            }
            var tierCount = new int[3];
            for (int i = 0; i < S.huts.Length; i++)
            {
                var h = S.huts[i];
                int hx = Mathf.RoundToInt(h.x), hy = Mathf.RoundToInt(h.y);
                // D-880 (Patrik 2026-09-27, "fler typer av hus … mysigare"): the old hash over 01..13 lotted the water mill
                // (13) and the tower (01) in as dwellings. Now a dwelling is drawn from a SET chosen by the owner's generation
                // (TD-031 "time made visible"): founder-era houses in the settled heart are the larger timber houses, the young
                // edge gets small cottages. Both lists are Patrik's to edit (contact sheet 45-UNITY/evidence/houses/2026-09-27).
                int og = genOf.TryGetValue(h.owner, out var gg) ? gg : maxGen;
                float ageFrac = maxGen > 1 ? 1f - og / (float)maxGen : 0.5f; // 1 = oldest (founder), 0 = newest edge
                int hvi = NearestVillageIdx(S, h.x, h.y);
                int dev = (hvi >= 0 && S.villages != null && hvi < S.villages.Length) ? RoadTier(S.villages[hvi]) : 0; // 0 hamlet · 1 village · 2 town
                float wealthPct = wealthRank.TryGetValue(h.owner, out var wp) ? wp : 0.5f;                                  // 0 poorest … 1 richest among the living
                // a hamlet is cottages; a village mixes cottages and houses; a town adds big houses for its richest third —
                // and a founder's roof is kept small unless wealth rebuilt it.
                // it.4 (Patrik: "en liten blandning … så levande som möjligt, inte statisk"): the village's development sets the
                // CEILING (hamlet cottages only · village up to houses · town up to big houses), and within that ceiling each
                // hut lands on a tier by wealth + age + a per-hut hash — so a town shows all three types mixed, a village two,
                // a hamlet one. Deterministic (same save → same houses) but neighbours differ. MEASURED spread 8919 y120: 0/1/2 all present.
                float lift = 0.42f * wealthPct + 0.33f * ageFrac + 0.25f * Hash01(hx, hy, 23);
                int tier = Mathf.Clamp(Mathf.RoundToInt(lift * dev * 1.35f), 0, dev);
                var set = tier == 2 ? HouseTier2 : tier == 1 ? HouseTier1 : HouseTier0;
                var pick = set[(int)(Hash(hx, hy, 21) % (uint)set.Length)];
                tierCount[tier]++;
                var prefab = FindPrefabExact(pick) ?? FindPrefabExact("P_BLD_house_02");
                if (prefab == null) { Debug.LogWarning("[Dresser] no house prefab found"); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = Ground(S, h.x, h.y);
                float yaw = HouseYaw(S, h, greens, hx, hy);
                go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                go.transform.localScale = Vector3.one * HouseScale * (0.94f + 0.12f * Hash01(hx, hy, 22)); // v2: pack houses are oversized at 1 · D-882: ±6 % so no two roofs sit at the same height
                go.name = $"hut_{h.owner}";
                yardCount += PlaceYard(S, go, h, hx, hy, yaw, yardProps, yardParent);
                ageMarks += PlaceHutAge(S, h, hx, hy, ageFrac, mossProps, freshProps, ageParent);
            }
            Debug.Log($"[Dresser] {S.huts.Length} houses (scale {HouseScale}; tiers cottage {tierCount[0]} / house {tierCount[1]} / big {tierCount[2]}, D-882) + {yardCount} yard props + {ageMarks} age marks (v2.2 grammar, maxGen {maxGen})");
        }

        // TD-031 v2.2: one hut's age marks — old huts overgrow (moss/bush), new huts show fresh timber.
        static int PlaceHutAge(WorldState S, WorldHut h, int hx, int hy, float ageFrac, GameObject[] moss, GameObject[] fresh, Transform parent)
        {
            int placed = 0;
            if (ageFrac > 0.55f && moss.Length > 0) // OLD — the settled, overgrown heart
            {
                int n = 1 + (int)(Hash(hx, hy, 81) % 2u);
                for (int k = 0; k < n; k++)
                {
                    var pf = moss[Hash(hx, hy, 82 + k) % (uint)moss.Length];
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                    float ox = (Hash01(hx, hy, 83 + k) - 0.5f) * 4.5f, oz = (Hash01(hx, hy, 84 + k) - 0.5f) * 4.5f;
                    go.transform.position = GroundW(P(S, h.x, h.y) + new Vector3(ox, 0, oz));
                    go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 85 + k) % 360u, 0);
                    go.transform.localScale = Vector3.one * (0.4f + Hash01(hx, hy, 86 + k) * 0.3f);
                    go.name = $"overgrowth_{h.owner}_{k}";
                    placed++;
                }
            }
            else if (ageFrac < 0.28f && fresh.Length > 0) // NEW — fresh raw timber at the expanding edge
            {
                var pf = fresh[Hash(hx, hy, 87) % (uint)fresh.Length];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                float ox = (Hash01(hx, hy, 88) - 0.5f) * 3.5f, oz = (Hash01(hx, hy, 89) - 0.5f) * 3.5f;
                go.transform.position = GroundW(P(S, h.x, h.y) + new Vector3(ox, 0, oz));
                go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 90) % 360u, 0);
                go.transform.localScale = Vector3.one * (0.7f + Hash01(hx, hy, 91) * 0.3f);
                go.name = $"freshbuild_{h.owner}";
                placed++;
            }
            return placed;
        }

        // each village's GREEN = the centroid of the huts assigned to it (nearest village) — the local
        // open space the doors face, the shared "tun". Falls back to the village's recorded position,
        // then to a lone farmstead's own spot.
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

        // yaw (degrees) so the house FRONT (+Z, offset by HouseFrontYawOffset) faces its village green,
        // with a deterministic ±7° jitter. Falls back to a gentle grid for a hut standing on the green.
        static float HouseYaw(WorldState S, WorldHut h, Vector2[] greens, int hx, int hy)
        {
            float jitter = (Hash(hx, hy, 23) % 15) - 7f; // ±7°, deterministic
            int vi = NearestVillageIdx(S, h.x, h.y);
            if (vi >= 0 && greens != null && vi < greens.Length)
            {
                var hutW = P(S, h.x, h.y); var greenW = P(S, greens[vi].x, greens[vi].y);
                var d = new Vector2(greenW.x - hutW.x, greenW.z - hutW.z);
                if (d.sqrMagnitude > 1f) // not standing on the green itself
                    return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg + HouseFrontYawOffset + jitter; // +Z toward the green
            }
            return Hash(hx, hy, 22) % 4 * 90 + jitter; // lone house: gentle grid
        }

        // TD-031: a lived-in YARD on each house's door side (toward the green) — 0..2 work-life props
        // (cart / barrel / crate / sack / woodpile / hay / bucket), all from the Village pack (the
        // zero-pink family, TD-021). Presentation-only + hash-driven: same world ⇒ same yard, forever.
        static readonly string[] YardPropNames = {
            "P_PROP_cart_01","P_PROP_cart_02","P_PROP_barrel_01","P_PROP_barrel_03","P_PROP_crate_01",
            "P_PROP_crate_03","P_PROP_sack_02","P_PROP_sack_05","P_PROP_firepit_woodpile","P_PROP_hay_02",
            "P_PROP_hay_04","P_PROP_bucket_01","P_PROP_trough_01"
        };
        // one house's yard: 0..2 props on the door side, placed just beyond the house's REAL front face
        // (from renderer bounds, so it tracks HouseScale) — no floating props, no props buried in-wall.
        static int PlaceYard(WorldState S, GameObject house, WorldHut h, int hx, int hy, float yaw, GameObject[] props, Transform parent)
        {
            if (props.Length == 0) return 0;
            var rot = Quaternion.Euler(0, yaw, 0);
            var fwd = rot * Vector3.forward;   // door side (toward the green)
            var right = rot * Vector3.right;
            // front-face distance = the house AABB half-extent projected on the door direction, + clearance
            float front = 2.5f;
            var rends = house.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int r = 1; r < rends.Length; r++) b.Encapsulate(rends[r].bounds);
                front = Vector3.Dot(b.extents, new Vector3(Mathf.Abs(fwd.x), 0f, Mathf.Abs(fwd.z))) + 0.9f;
            }
            int count = (int)(Hash(hx, hy, 71) % (uint)(YardPropsMax + 1)); // 0..2
            int placed = 0;
            var basePos = P(S, h.x, h.y);
            for (int k = 0; k < count; k++)
            {
                var pf = props[Hash(hx, hy, 72 + k) % (uint)props.Length];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                float lateral = (Hash01(hx, hy, 73 + k) - 0.5f) * 3.0f; // spread along the wall
                var world = basePos + fwd * front + right * lateral;
                go.transform.position = GroundW(world);
                go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 74 + k) % 360u, 0);
                go.name = $"yard_{h.owner}_{k}";
                placed++;
            }
            return placed;
        }

        // TD-028: a villager per living soul — the studio's OWN toon-rendered GLBs (glTFast import).
        // Age from a.age, gender by position hash (presentation-only, D-078 rule 4 — never sim RNG).
        // FAS 2 (D-123): the classifier moved to runtime (Emergence.Runtime.AgentTaskRead) so the edit-mode
        // still-poses and the live play-mode animator can never disagree. These wrappers keep call sites.
        static bool Working(string task) => Emergence.Runtime.AgentTaskRead.Working(task);

        static bool Moving(string task) => Emergence.Runtime.AgentTaskRead.Moving(task);

        // the walk/work GLBs carry an AnimationClip; sampling it in EDIT mode POSES the model to a
        // frame (mid-stride / mid-work), so stills read dynamic + varied — no play mode needed.
        static AnimationClip LoadClip(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            return null;
        }

        // FAS 2 (D-123): controller assets built by Fas2AnimatorBuild (Editor-assembly — referenced by
        // path here, not by type, since WorldDresser lives in Assembly-CSharp). Null until first build.
        static RuntimeAnimatorController VillagerController(string band, bool female)
        {
            const string dir = "Assets/Emergence/Fas2/Anim";
            string key = band == "adult" ? (female ? "adult-f" : "adult") : band + (female ? "-f" : "");
            return key == "adult"
                ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dir + "/VillagerAnim.controller")
                : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{dir}/Villager-{key}.overrideController");
        }

        static void PlaceAgents(WorldState S, Transform root)
        {
            var parent = new GameObject("Agents").transform; parent.SetParent(root, true);
            int placed = 0;
            foreach (var a in S.agents)
            {
                string band = a.age < 14 ? "child" : a.age > 55 ? "elder" : "adult";
                // D-124: soul-stable sex — hash(id), never position (an agent that moved between
                // snapshots used to change body; identity is a property of the soul, not the spot).
                bool female = (Hash(a.id, 0, a.id * 31 + 7) & 1u) == 0u;
                // pose by task: working adults -> -work, movers -> -walk, else idle base
                string suffix = (band == "adult" && Working(a.task)) ? "-work" : Moving(a.task) ? "-walk" : "";
                string baseNm = band == "child" ? (female ? "villager-child-f" : "villager-child")
                              : band == "elder" ? (female ? "villager-elder-f" : "villager-elder")
                              : (female ? "villager-f" : "villager");
                string nm = baseNm + suffix;
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + nm + ".glb");
                if (pf == null) { nm = baseNm; pf = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + nm + ".glb"); } // fallback to base
                if (pf == null) continue; // glTFast not imported yet — dressing still succeeds
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                go.transform.position = Ground(S, a.x, a.y, 0f);
                go.transform.localScale = Vector3.one * VillagerScale;
                // POSE the model to a hash-varied frame of its clip (static, edit-mode)
                var clip = LoadClip(CharDir + nm + ".glb");
                if (clip != null && clip.length > 0f) clip.SampleAnimation(go, Hash01((int)a.x, (int)a.y, a.id + 11) * clip.length);
                // FAS 2 (D-123): live Animator — inert in edit mode (the sampled still stands), drives
                // Idle/Walk/Work in play mode. Controller per demographic (shared skeleton, D-123 build).
                var rac = VillagerController(band, female);
                if (rac != null)
                {
                    var anim = go.GetComponentInChildren<Animator>();
                    if (anim == null) anim = go.AddComponent<Animator>();
                    anim.runtimeAnimatorController = rac;
                    var aa = go.AddComponent<Emergence.Runtime.AgentAnimator>();
                    aa.agentId = a.id; aa.task = a.task; aa.canWork = band == "adult";
                }
                // face the nearest hut (a lived-in reading, less random) — orientation is presentation-only
                WorldHut nh = null; float nb = float.MaxValue;
                foreach (var h in S.huts) { float d = (h.x - a.x) * (h.x - a.x) + (h.y - a.y) * (h.y - a.y); if (d < nb) { nb = d; nh = h; } }
                if (nh != null && nb > 0.01f) { var t = Ground(S, nh.x, nh.y, 0f); t.y = go.transform.position.y; go.transform.LookAt(t); }
                else go.transform.rotation = Quaternion.Euler(0f, Hash((int)a.x, (int)a.y, a.id + 7) % 360u, 0f);
                go.name = $"agent_{a.id}_{a.name}";
                // GROUND THE POSE, NOT THE PIVOT (small fix, 2026-08-14). The villager is placed at
                // terrain height and only THEN sampled into a hash-varied frame of its clip — and a
                // pose moves the feet. A working crouch or a walk cycle's low foot puts the sole below
                // the origin, so the person stood ankle-deep in the ground. Which souls it hits changes
                // with every world, because the frame is hash-picked, which is exactly why it read as
                // intermittent rather than as a bug. Measured AFTER the pose is applied, and only ever
                // lifts: a soul may stand on the ground, never sink into it.
                LiftOntoGround(go);
                placed++;
            }
            Debug.Log($"[Dresser] placed {placed}/{S.agents.Length} villagers" + (placed == 0 ? " (0 — glTFast not imported yet?)" : ""));
        }

        // lift a posed GLB so its LOWEST rendered vertex rests on the terrain. Never lowers.
        static void LiftOntoGround(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var t = Terrain.activeTerrain;
            if (t == null) return;
            float ground = t.SampleHeight(go.transform.position) + t.transform.position.y;
            float under = ground - b.min.y;
            if (under > 0.005f) go.transform.position += Vector3.up * under;
        }

        // TD-029: the studio's own deer/wolf GLBs at the sim's animal positions (retires the
        // low-poly Quaternius question the EP raised). Positions are the sim's — documentary truth.
        // D-128 (SD-delegated look call): the own GLBs are STATIC (0 skins/clips) — a documentary of a
        // LIVING world needs living fauna, so the rigged Quaternius set (Idle/Graze/Sniff…) replaces
        // them, scale-matched to the GLB silhouettes. Revert = AnimatedAnimals=false (one line).
        public static bool AnimatedAnimals = true;

        static void PlaceAnimals(WorldState S, Transform root)
        {
            if (S.animals == null || S.animals.Length == 0) return;
            var parent = new GameObject("Animals").transform; parent.SetParent(root, true);
            int placed = 0;
            foreach (var an in S.animals)
            {
                string nm = an.type == "wolf" ? "wolf" : "deer";
                GameObject go = null;
                if (AnimatedAnimals)
                {
                    var rigged = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Quaternius/FBX/" + (nm == "wolf" ? "Wolf" : "Deer") + ".fbx");
                    var glb = AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + nm + ".glb");
                    if (rigged != null)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(rigged, parent);
                        // scale parity with the retired GLB silhouette (bounds height), so herd scale reads unchanged
                        float scale = AnimalScale;
                        if (glb != null)
                        {
                            float hGlb = BoundsHeight(glb), hRig = BoundsHeight(rigged);
                            if (hGlb > 0.01f && hRig > 0.01f) scale = AnimalScale * (hGlb / hRig);
                        }
                        go.transform.localScale = Vector3.one * scale;
                        if (nm == "wolf") ApplyWolfTint(go);   // D-131: FBX default was untextured light grey clay
                        var aa = go.AddComponent<Emergence.Runtime.AnimalAnimator>();
                        aa.animalId = an.id; aa.type = nm;
                        var anim = go.GetComponentInChildren<Animator>() ?? go.AddComponent<Animator>();
                        // controller by PATH (AnimalAnimBuild is Editor-assembly, same rule as VillagerController)
                        anim.runtimeAnimatorController = nm == "wolf"
                            ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Emergence/Fas2/Anim/AnimalAnim-wolf.overrideController")
                            : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Emergence/Fas2/Anim/AnimalAnim-deer.controller");
                    }
                }
                if (go == null) // AnimatedAnimals=false, or rigged prefab missing → the static GLB stands
                {
                    var pf = AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + nm + ".glb");
                    if (pf == null) continue;
                    go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                    go.transform.localScale = Vector3.one * AnimalScale;
                }
                go.transform.position = Ground(S, an.x, an.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, Hash((int)an.x, (int)an.y, an.id) % 360u, 0f);
                go.name = $"{an.type}_{an.id}";
                placed++;
            }
            Debug.Log($"[Dresser] placed {placed}/{S.animals.Length} animals ({(AnimatedAnimals ? "rigged Quaternius, D-128" : "own static GLBs")})");
        }

        // D-131 (grind-review): the wolf FBX resolved to untextured light-grey default material — "grå lera".
        // Tint into the painted register: dark warm grey-brown body, zero gloss. One shared material per dress.
        static Material _wolfMat;
        static void ApplyWolfTint(GameObject go)
        {
            if (_wolfMat == null)
            {
                var sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) return;
                // D-132 (re-review A2/R5): 0.33-albedon läste blek gråbeige i direkt sol — mörkare ton
                _wolfMat = new Material(sh) { name = "M_WolfTint_D131", color = new Color(0.22f, 0.20f, 0.18f) };
                _wolfMat.SetFloat("_Smoothness", 0.05f);
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = _wolfMat;
                r.sharedMaterials = mats;
            }
        }

        static float BoundsHeight(GameObject prefab)
        {
            var b = new Bounds(); bool first = true;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>())
            { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
            return first ? 0f : b.size.y;
        }

        // TD-028: forge/mill/kiln/well as COMPOSED markers (D-062), not scatter. v1: a well at each
        // village + one hash-picked craft anchor offset from centre. (Engine tech-per-village export
        // is a later refinement; for now every village reads as a settled place with a well + a craft.)
        // PlaceTechAnchors removed with C7 (D-233) — see the note at the call site.

        static void Anchor(GameObject pf, WorldState S, float x, float y, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.position = Ground(S, x, y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, Hash((int)x, (int)y, 5) % 360u, 0f);
            go.transform.localScale = Vector3.one * TechAnchorScale;
        }

        // TD-025 audition: Vefects fire = THE warm point; msVFX smoke = chimney plumes on huts
        // near a burning fire. Falls back to the pack fire so the branch can be dropped cleanly.
        static void PlaceFires(WorldState S, Transform root)
        {
            var parent = new GameObject("Fires").transform; parent.SetParent(root, true);
            var fx = FindPrefab("VFX_Fire_01_Medium") ?? FindPrefab("VFX_Fire_01_Big")
                     ?? FindPrefab("P_FX_fire") ?? FindPrefab("PF_FX_fire") ?? FindPrefab("fire");
            var smoke = FindPrefab("msVFX_Stylized Smoke 1") ?? FindPrefab("msVFX_Stylized Smoke 2");
            foreach (var f in S.fires)
            {
                var pos = Ground(S, f.x, f.y, 0.1f);
                if (fx != null)
                {
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(fx, parent);
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * FireScale;
                }
                // the warm point — kept even if the Vefects prefab carries its own light (guarantees the identity)
                var light = new GameObject("firelight").AddComponent<Light>();
                light.transform.SetParent(parent, true);
                light.transform.position = pos + Vector3.up * 1.2f;
                light.type = LightType.Point; light.color = new Color(1f, 0.62f, 0.28f); light.intensity = 2.6f; light.range = 12f;
            }
            // chimney smoke: a hut within SmokeNearFireTiles of a burning fire is "lived-in" at this hour
            if (smoke != null)
                foreach (var h in S.huts)
                {
                    if (!S.fires.Any(f => Mathf.Abs(f.x - h.x) <= SmokeNearFireTiles && Mathf.Abs(f.y - h.y) <= SmokeNearFireTiles)) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(smoke, parent);
                    go.transform.position = Ground(S, h.x, h.y, SmokeRoofLift);
                    go.transform.localScale = Vector3.one * SmokeScale;
                    go.name = $"chimneysmoke_{h.owner}";
                }
        }

        // D-883 (Patrik 2026-09-27: "staketen är lite random utsatta och ser konstigt ut ibland"): the old field fence laid
        // 3 segments per 8 m tile-edge CENTRED — leaving a 1.3 m gap at every corner, a 6 m OPEN gap on the gate edge, and no
        // corner posts, so the enclosures read as broken. AAA rebuild: trace each field cluster's boundary into continuous
        // straight RUNS (merged colinear edges), tile each run with the fence at its MEASURED footprint (no gaps/overlaps),
        // stand a wooden POST at every corner, and cut ONE clean gate on the run nearest the village. Presentation-only,
        // hash-driven (same world ⇒ same fences), self-calibrating (never hardcodes the segment count).
        static void PlaceFields(WorldState S, Transform root)
        {
            var parent = new GameObject("Fields").transform; parent.SetParent(root, true);
            var variants = new[] { "P_PROP_fence_v01_01", "P_PROP_fence_v01_02", "P_PROP_fence_v01_03", "P_PROP_fence_v01_04", "P_PROP_fence_v01_05" }
                .Select(FindPrefab).Where(p => p != null).ToArray();
            if (variants.Length == 0 || S.fields == null || S.fields.Length == 0) return;
            var fence0 = variants[0];
            var gate = FindPrefab("P_PROP_fence_door_gate") ?? fence0;
            var post = FindPrefab("P_PROP_wall_wood_post_01") ?? FindPrefab("P_PROP_wall_wood_post_02");

            // self-calibrate: measure the fence footprint so tiling never gaps or overlaps
            MeasureFootprint(fence0, out float segLen, out bool lenAlongX);
            if (segLen < 0.5f) { segLen = 2f; lenAlongX = true; }

            // D-886: wall_wood_post is a PALISADE post (structural, house-height) — at scale 1 it towered
            // over the village in the D-885 eye-level shot. Measure both props and shrink the post to the
            // fence's own height + 15 % so it reads as a corner post capping the line. Self-calibrating:
            // no hardcoded metre value, so a different fence variant or pack re-import still lands right.
            float fenceH = MeasureHeight(fence0);
            float postH = MeasureHeight(post);
            float postScale = (postH > 0.01f && fenceH > 0.01f) ? Mathf.Clamp(fenceH * 1.15f / postH, 0.05f, 1f) : 1f;

            var fieldSet = new HashSet<(int, int)>(S.fields.Select(f => (Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y))));
            var greens = VillageGreens(S);
            float half = TileSize * 0.5f;
            var seen = new HashSet<(int, int)>();
            int fenceN = 0, postN = 0, gateN = 0;
            foreach (var start in fieldSet)
            {
                if (seen.Contains(start)) continue;
                var cluster = new List<(int, int)>();
                var stack = new Stack<(int, int)>(); stack.Push(start); seen.Add(start);
                while (stack.Count > 0)
                {
                    var c = stack.Pop(); cluster.Add(c);
                    foreach (var n in new[] { (c.Item1 + 1, c.Item2), (c.Item1 - 1, c.Item2), (c.Item1, c.Item2 + 1), (c.Item1, c.Item2 - 1) })
                        if (fieldSet.Contains(n) && seen.Add(n)) stack.Push(n);
                }

                // boundary → continuous runs in WORLD space (merge colinear tile edges)
                var runs = new List<(Vector3 a, Vector3 b)>();
                // horizontal edges: north (cy-1 missing → +z) and south (cy+1 missing → -z), grouped by row, merged over cx
                for (int d = 0; d < 2; d++)
                {
                    int noff = d == 0 ? -1 : 1; float zsign = d == 0 ? 1f : -1f;
                    var tiles = cluster.Where(t => !fieldSet.Contains((t.Item1, t.Item2 + noff))).OrderBy(t => t.Item2).ThenBy(t => t.Item1).ToList();
                    int k = 0;
                    while (k < tiles.Count)
                    {
                        int cy = tiles[k].Item2, x0 = tiles[k].Item1, x1 = x0; k++;
                        while (k < tiles.Count && tiles[k].Item2 == cy && tiles[k].Item1 == x1 + 1) { x1 = tiles[k].Item1; k++; }
                        Vector3 pa = Ground(S, x0, cy), pb = Ground(S, x1, cy);
                        float z = pa.z + zsign * half;
                        runs.Add((new Vector3(pa.x - half, 0, z), new Vector3(pb.x + half, 0, z)));
                    }
                }
                // vertical edges: east (cx+1 missing → +x) and west (cx-1 missing → -x), grouped by col, merged over cy
                for (int d = 0; d < 2; d++)
                {
                    int noff = d == 0 ? 1 : -1; float xsign = d == 0 ? 1f : -1f;
                    var tiles = cluster.Where(t => !fieldSet.Contains((t.Item1 + noff, t.Item2))).OrderBy(t => t.Item1).ThenBy(t => t.Item2).ToList();
                    int k = 0;
                    while (k < tiles.Count)
                    {
                        int cx = tiles[k].Item1, y0 = tiles[k].Item2, y1 = y0; k++;
                        while (k < tiles.Count && tiles[k].Item1 == cx && tiles[k].Item2 == y1 + 1) { y1 = tiles[k].Item2; k++; }
                        Vector3 pa = Ground(S, cx, y0), pb = Ground(S, cx, y1);
                        float x = pa.x + xsign * half;
                        runs.Add((new Vector3(x, 0, pa.z - half), new Vector3(x, 0, pb.z + half)));
                    }
                }
                if (runs.Count == 0) continue;

                Vector3 access = new Vector3(1e9f, 0, 1e9f);
                int vi = NearestVillageIdx(S, cluster[0].Item1, cluster[0].Item2);
                if (vi >= 0 && greens != null && vi < greens.Length) access = Ground(S, greens[vi].x, greens[vi].y);

                var segs = new List<GameObject>();
                var corners = new HashSet<(int, int)>();
                foreach (var (a, b) in runs)
                {
                    Vector3 diff = b - a; float L = diff.magnitude; if (L < 0.01f) continue;
                    Vector3 dir = diff / L;
                    bool horiz = Mathf.Abs(dir.x) >= Mathf.Abs(dir.z);
                    float yaw = horiz ? (lenAlongX ? 0f : 90f) : (lenAlongX ? 90f : 0f);
                    int n = Mathf.Max(1, Mathf.RoundToInt(L / segLen));
                    float step = L / n;
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 c = a + dir * (step * (i + 0.5f));
                        var prefab = variants[(int)(Hash(Mathf.RoundToInt(c.x), Mathf.RoundToInt(c.z), 61) % (uint)variants.Length)];
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                        go.transform.position = GroundW(c);
                        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                        go.name = "fence";
                        segs.Add(go);
                    }
                    corners.Add((Mathf.RoundToInt(a.x), Mathf.RoundToInt(a.z)));
                    corners.Add((Mathf.RoundToInt(b.x), Mathf.RoundToInt(b.z)));
                }
                fenceN += segs.Count;

                // one clean gate: swap the segment nearest the village
                if (segs.Count > 0 && gate != null && gate != fence0)
                {
                    int gi = 0; float best = float.MaxValue;
                    for (int i = 0; i < segs.Count; i++) { float dd = (segs[i].transform.position - access).sqrMagnitude; if (dd < best) { best = dd; gi = i; } }
                    var old = segs[gi]; var pos = old.transform.position; var rot = old.transform.rotation;
                    UnityEngine.Object.DestroyImmediate(old);
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(gate, parent);
                    g.transform.position = pos; g.transform.rotation = rot; g.name = "fence_gate"; gateN++;
                }

                // corner posts hide the run-to-run joints
                if (post != null)
                    foreach (var (px, pz) in corners)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(post, parent);
                        go.transform.position = GroundW(new Vector3(px, 0, pz));
                        go.transform.rotation = Quaternion.Euler(0, Hash01(px, pz, 62) * 90f, 0);
                        go.transform.localScale = Vector3.one * postScale;
                        go.name = "fence_post"; postN++;
                    }
            }
            Debug.Log($"[Dresser] FIELD FENCES (D-883): {fenceN} segments + {postN} corner posts + {gateN} gates, measured segLen {segLen:0.0} m, fenceH {fenceH:0.00} m, postH {postH:0.00} m -> post scale {postScale:0.00} (D-886), continuous runs (was 3-per-tile with corner gaps)");
        }

        // D-886: measure a prop's rendered height (y) so props from different pack families can be matched by eye-height.
        static float MeasureHeight(GameObject prefab)
        {
            if (prefab == null) return 0f;
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var rs = probe.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) return 0f;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                return b.size.y;
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        // D-883: measure a prop's ground footprint (length along its longer horizontal axis) so fence tiling self-calibrates.
        static void MeasureFootprint(GameObject prefab, out float segLen, out bool lenAlongX)
        {
            segLen = 2f; lenAlongX = true;
            var probe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var rs = probe.GetComponentsInChildren<Renderer>(true);
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    lenAlongX = b.size.x >= b.size.z;
                    segLen = Mathf.Max(b.size.x, b.size.z);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }

        static void PlaceNature(WorldState S, Transform root)
        {
            var parent = new GameObject("Nature").transform; parent.SetParent(root, true);
            // D-101e: the village pack ships ONE tree (P_ENV_TREE_village) and it renders as a flat dark
            // blob — drop it. Use DREAMSCAPE's real tree library for the woodland (their reference-quality
            // large trees + birches). Village pack = the built world; Dreamscape = the natural world.
            // D-878: the natural world = FANTASTIC Nature (L3 one family). Woodland = v2 (conifers) + v1/v4 (broadleaf), wood_01 variant;
            // stones = the pack's 15 P_ENV_stone_* (Nature root — the Village pack has a same-named stone); bushes = P_ENV_PLANT_bush_v1_*.
            var trees = FindPrefabsIn(NatureRoot, "P_ENV_TREE_v2_", "_wood_01").Concat(FindPrefabsIn(NatureRoot, "P_ENV_TREE_v1_", "_wood_01")).ToArray();   // v4 (large-leaf) out — it.2 SEEN
            var rocks = FindPrefabsIn(NatureRoot, "P_ENV_stone_");
            var bushes = FindPrefabsIn(NatureRoot, "P_ENV_PLANT_bush_v1_");
            Debug.Log($"[Dresser] D-878 nature sets: trees={trees.Length} rocks={rocks.Length} bushes={bushes.Length} (Nature root)");
            // TD-031 v2.1: the woodland EDGE is managed (coppiced), the deep wood is not — edge tiles
            // get thinner trees + fallen trunks/stumps; interior forest stays dense (silhouette + §2 outfield).
            var trunks = new[] { "P_PROP_treetrunk_01", "P_PROP_treetrunk_02", "P_PROP_treetrunk_03", "P_PROP_treetrunk_04" }
                .Select(FindPrefab).Where(p => p != null).ToArray();
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    char t = Tile(S, x, y);
                    if (t == 'f' && trees.Length > 0)
                    {
                        bool edge = ForestEdge(S, x, y);
                        Scatter(S, parent, trees, x, y, edge ? TreesPerForestTile * 0.45f : TreesPerForestTile, 41);
                        if (edge && trunks.Length > 0 && Hash01(x, y, 51) < 0.45f) // coppice marks at the treeline
                        {
                            var pf = trunks[Hash(x, y, 52) % (uint)trunks.Length];
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                            float jx = Hash01(x, y, 53) - 0.5f, jy = Hash01(x, y, 54) - 0.5f;
                            go.transform.position = Ground(S, x + jx * 0.8f, y + jy * 0.8f);
                            go.transform.rotation = Quaternion.Euler(0, Hash(x, y, 55) % 360u, 0);
                            go.transform.localScale = Vector3.one * (0.8f + Hash01(x, y, 56) * 0.4f);
                        }
                    }
                    else if (t == 's' && rocks.Length > 0) Scatter(S, parent, rocks, x, y, RocksPerStoneTile, 43);
                    else if (t == 'b' && bushes.Length > 0) Scatter(S, parent, bushes, x, y, BushesPerBerryTile, 47);
                }
        }

        // a forest tile is an EDGE if any 4-neighbour is not forest (or it's a map border) — the treeline.
        static bool ForestEdge(WorldState S, int x, int y)
        {
            if (x <= 0 || y <= 0 || x >= S.W - 1 || y >= S.H - 1) return true;
            return Tile(S, x - 1, y) != 'f' || Tile(S, x + 1, y) != 'f' || Tile(S, x, y - 1) != 'f' || Tile(S, x, y + 1) != 'f';
        }

        static void Scatter(WorldState S, Transform parent, GameObject[] set, int x, int y, float perTile, int salt)
        {
            int count = Mathf.FloorToInt(perTile) + (Hash01(x, y, salt) < perTile - Mathf.Floor(perTile) ? 1 : 0);
            for (int i = 0; i < count; i++)
            {
                var prefab = set[Hash(x, y, salt + 100 + i) % (uint)set.Length];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                float jx = Hash01(x, y, salt + 200 + i) - 0.5f, jy = Hash01(x, y, salt + 300 + i) - 0.5f;
                go.transform.position = Ground(S, x + jx * 0.9f, y + jy * 0.9f);
                go.transform.rotation = Quaternion.Euler(0, Hash(x, y, salt + 400 + i) % 360, 0);
                float sc = 0.85f + Hash01(x, y, salt + 500 + i) * 0.4f;
                go.transform.localScale = Vector3.one * sc;   // D-878: Nature trees at authored size (measured in the birth report, L6 measuring stick)
                StripImpostorLods(go); // Dreamscape impostor billboards lack baked textures in edit mode -> magenta at distance
            }
        }

        // D-101d: THE MEADOW BODY — scatter real 3D Dreamscape foliage (wildflower clusters, grass tufts,
        // small flowering bushes) as GameObjects across the open grass. Terrain-detail grass is short and
        // reads flat in the near field; this fills the foreground/mid-field with life so the meadow is
        // lush right up to the camera — the natural canvas the genesis moment stands on. RNG-neutral (hash).
        static void PlaceMeadowFoliage(WorldState S, Transform root)
        {
            var parent = new GameObject("MeadowFoliage").transform; parent.SetParent(root, true);
            // D-878: Nature foliage accents (the base lushness is the terrain grass above)
            var flowers = FindPrefabsIn(NatureRoot, "P_ENV_PLANT_flower_v1_");
            var tufts = FindPrefabsIn(NatureRoot, "P_GRASS_");   // D-878 it.2: grass clumps as near-field accents (leaf_v3 read as 2 m leaves at eye level — seen)
            var smallBush = FindPrefabsIn(NatureRoot, "P_ENV_PLANT_bush_v2_");
            if (flowers.Length == 0 && tufts.Length == 0 && smallBush.Length == 0) { Debug.LogWarning("[Dresser] no meadow foliage prefabs found — skipped"); return; }
            int placed = 0;
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    if (Tile(S, x, y) != 'g') continue;
                    if (NearVillage(S, x, y, 2f)) continue;                 // keep the immediate green/commons clear
                    // D-119 (A6 enforcement, Director-delegated): ~halved 3D-clump density (was 0.45/0.34/0.06).
                    // The renderer/draw-call bottleneck was ~4.9k clumps; the engine-instanced terrain-detail grass
                    // carries the base lushness, so the meadow stays green while the 3D accents thin gracefully.
                    if (tufts.Length > 0 && Hash01(x, y, 820) < 0.22f) placed += Clump(S, parent, tufts, x, y, 821, 0.7f, 1.3f);
                    if (flowers.Length > 0 && Hash01(x, y, 830) < 0.16f) placed += Clump(S, parent, flowers, x, y, 831, 0.8f, 1.4f);
                    if (smallBush.Length > 0 && Hash01(x, y, 840) < 0.03f) placed += Clump(S, parent, smallBush, x, y, 841, 0.6f, 1.0f);
                }
            Debug.Log($"[Dresser] meadow foliage: {placed} 3D clumps (flowers/tufts/bushes) filling the open grass (D-101d)");
        }
        static int Clump(WorldState S, Transform parent, GameObject[] set, int x, int y, int salt, float scLo, float scHi)
        {
            var pf = set[Hash(x, y, salt) % (uint)set.Length];
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            float jx = Hash01(x, y, salt + 7) - 0.5f, jy = Hash01(x, y, salt + 9) - 0.5f;
            go.transform.position = Ground(S, x + jx * 0.9f, y + jy * 0.9f);
            go.transform.rotation = Quaternion.Euler(0, Hash(x, y, salt + 11) % 360u, 0);
            go.transform.localScale = Vector3.one * (scLo + Hash01(x, y, salt + 13) * (scHi - scLo)) * GrassScale;
            StripImpostorLods(go); // Dreamscape impostor billboards lack baked textures in edit mode -> magenta at distance
            return 1;
        }

        [MenuItem("Emergence/P1 Dressing/Find Pink In Scene")]
        public static void FindPinkInScene()
        {
            var bad = new Dictionary<string, int>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                foreach (var m in r.sharedMaterials)
                    if (m != null && m.shader != null && (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported))
                    {
                        var key = $"{r.transform.root.name}/{r.gameObject.name} -> {m.name}";
                        bad[key] = bad.TryGetValue(key, out var n) ? n + 1 : 1;
                    }
            Debug.Log("[Dresser] pink renderers: " + bad.Count + "\n" + string.Join("\n", bad.Select(kv => kv.Value + "x " + kv.Key).Take(30)));
        }

        // D-131 (grind-review fix): the per-placement strips missed at least one path — review found
        // TONEMAPPED error-magenta at distance (#FF00FF through ACES ≈ rgb(207,~33,207), under the old
        // r>220 detector). Kill the whole class: sweep EVERY renderer/LODGroup in the scene once after
        // dressing. Idempotent; call it last in Build().
        public static void StripImpostorsSceneWide()
        {
            int off = 0, errOff = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include))
            {
                if (r.GetComponent("ImpostorDataHolder") != null
                    || r.gameObject.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0
                    || r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0))
                { if (r.enabled) { r.enabled = false; off++; } continue; }
                // D-131b: the review's tonemapped-magenta flecks were the AmbientFX leaf PARTICLES —
                // M_Leaf_01 ships on a LEGACY built-in particle shader (fileID 210) that URP renders
                // magenta while isSupported stays true. Rescue, don't kill: swap to URP Particles/Unlit
                // with color+texture carried over (the leaves are green by design). Unfixable -> disable.
                if (r is ParticleSystemRenderer)
                {
                    var mats = r.sharedMaterials; bool changed = false, dead = false;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var m = mats[i];
                        if (m == null || m.shader == null) continue;
                        string sn = m.shader.name;
                        bool legacy = sn.StartsWith("Legacy Shaders/") || (sn.StartsWith("Particles/") && !sn.Contains("Universal"))
                                   || sn == "Hidden/InternalErrorShader" || !m.shader.isSupported;
                        if (!legacy) continue;
                        var urp = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                        if (urp == null) { dead = true; continue; }
                        var fix = new Material(urp) { name = m.name + "_URPfix_D131b" };
                        if (m.HasProperty("_Color")) fix.SetColor("_BaseColor", m.GetColor("_Color"));
                        if (m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null) fix.SetTexture("_BaseMap", m.GetTexture("_MainTex"));
                        fix.SetFloat("_Surface", 1f); fix.SetFloat("_Blend", 0f);   // transparent, alpha-blended
                        fix.renderQueue = 3000;
                        mats[i] = fix; changed = true;
                    }
                    if (changed) { r.sharedMaterials = mats; errOff++; }
                    else if (dead && r.enabled) { r.enabled = false; errOff++; }
                    continue;
                }
                if (r.sharedMaterials.Any(m => m != null && m.shader != null
                        && (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported)))
                { if (r.enabled) { r.enabled = false; errOff++; } }
            }
            foreach (var lg in UnityEngine.Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include))
            {
                var lods = lg.GetLODs();
                bool IsImp(Renderer r) => r != null && r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0);
                var keep = lods.Where(l => !l.renderers.Any(IsImp)).ToArray();
                if (keep.Length > 0 && keep.Length < lods.Length)
                {
                    keep[keep.Length - 1].screenRelativeTransitionHeight = 0.005f;
                    lg.SetLODs(keep);
                    foreach (var l in lods.Except(keep)) foreach (var r in l.renderers) if (r != null && IsImp(r)) { r.enabled = false; off++; }
                }
            }
            Debug.Log($"[Dresser] scene-wide sweep: {off} impostor + {errOff} error-shader renderers disabled (D-131/D-131b)");
        }

        // Impostor LOD levels (Polyart) render magenta without their runtime-baked data.
        // Strip them and extend the last real LOD — perf headroom is ample at our counts (4070 Ti reference).
        static void StripImpostorLods(GameObject go)
        {
            // impostors live as script-driven child renderers (ImpostorDataHolder), not only as LOD levels
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                if (r.GetComponent("ImpostorDataHolder") != null
                    || r.gameObject.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0
                    || r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0))
                    r.enabled = false;
            foreach (var lg in go.GetComponentsInChildren<LODGroup>())
            {
                var lods = lg.GetLODs();
                bool IsImpostor(Renderer r) => r != null && r.sharedMaterials.Any(m => m != null && m.shader != null && m.shader.name.IndexOf("impostor", StringComparison.OrdinalIgnoreCase) >= 0);
                var keep = lods.Where(l => !l.renderers.Any(IsImpostor)).ToArray();
                if (keep.Length > 0 && keep.Length < lods.Length)
                {
                    keep[keep.Length - 1].screenRelativeTransitionHeight = 0.005f;
                    lg.SetLODs(keep);
                    foreach (var l in lods.Except(keep)) foreach (var r in l.renderers) if (r != null && IsImpostor(r)) r.enabled = false;
                }
            }
        }

        static Material FindMaterial(string name)
        {
            var guid = AssetDatabase.FindAssets($"t:Material {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
        }

        // TD-033: THE CODEX IN ACTION — read object-codex.json, place each object where its DISCOVERY
        // predicate is true per village. This is the whole thesis: the world is a readout of what the
        // civilization has discovered. The dresser no longer hard-codes what a village gets — it asks the codex.
        static void PlaceCodexObjects(WorldState S, Transform root)
        {
            const string codexPath = "Assets/Emergence/Codex/object-codex.json";
            if (!File.Exists(codexPath)) return;
            Codex codex;
            try { codex = JsonUtility.FromJson<Codex>(File.ReadAllText(codexPath)); }
            catch (Exception ex) { Debug.LogWarning("[Dresser] codex parse failed: " + ex.Message); return; }
            if (codex?.objects == null || codex.objects.Length == 0 || S.villages == null) return;
            var parent = new GameObject("CodexObjects").transform; parent.SetParent(root, true);
            int placed = 0;
            foreach (var v in S.villages)
                // D-239: the prefab test is passed in so a whole with no resolvable look absorbs nothing.
                foreach (var e in CodexBuildOrder.Allowed(v, codex.objects, nm => LoadCodexPrefab(nm) != null))
                {
                    if (LoadCodexPrefab(e.prefab) == null) continue;
                    int cnt = Mathf.Max(1, e.count);
                    for (int k = 0; k < cnt; k++)
                    {
                        // D-243: the same deterministic variant law as the played world
                        var pf = LoadCodexPrefab(Emergence.Runtime.LiveReconciler.VariantOf(e, v, k)) ?? LoadCodexPrefab(e.prefab);
                        if (pf == null) continue;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                        var pos = CodexPlacement(v, e, k, cnt);
                        go.transform.position = GroundW(P(S, pos.x, pos.y));
                        go.transform.rotation = Quaternion.Euler(0f, Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length + k) % 360u, 0f);
                        go.transform.localScale = Vector3.one * (e.scale <= 0f ? 1f : e.scale);
                        go.name = $"codex_{e.id}_{v.name}_{k}";
                        RaiseArrangement(go, e);      // D-242: the same authored recipe as the played world
                        SettleCodex(go, v, e, k);     // D-239: the same age law the played world uses
                        StripImpostorLods(go);
                        placed++;
                    }
                }
            Debug.Log($"[Dresser] codex: {placed} discovery-driven objects across {S.villages.Length} villages (the world reads its own development)");
        }

        // the gate lives in CodexBuildOrder.Qualifies (D-230). The copy that stood here is gone:
        // one law, one implementation, or the two drift and the editor world stops matching the played one.

        // D-239: the settle law (D-236) was written into the LIVE reconciler only — and the editor path is
        // exactly where the studio takes its eye-height evidence. A law that cannot be seen in the pictures
        // we judge is a law nobody will ever check. Same constants, same hash, same re-derivation from the
        // ground; the editor builds once, so there is nothing here to accumulate onto.
        // D-242: the arrangement template, identical law to LiveReconciler.RaiseArrangement — parts in the
        // anchor's own frame, parented to it, missing prefabs skipped rather than leaving a gap.
        static void RaiseArrangement(GameObject anchor, CodexEntry e)
        {
            if (anchor == null || e?.arrangement == null || e.arrangement.Length == 0) return;
            var rot = anchor.transform.rotation;
            for (int i = 0; i < e.arrangement.Length; i++)
            {
                var part = e.arrangement[i];
                if (part == null || string.IsNullOrWhiteSpace(part.prefab)) continue;
                var pf = LoadCodexPrefab(part.prefab);
                if (pf == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, anchor.transform);
                go.transform.position = GroundW(anchor.transform.position + rot * new Vector3(part.dx, 0f, part.dz));
                go.transform.rotation = rot * Quaternion.Euler(0f, part.yaw, 0f);
                go.transform.localScale = Vector3.one * (part.scale <= 0f ? 1f : part.scale);
                go.name = $"part_{e.id}_{i}";
            }
        }

        const float SettleSinkPerGen = 0.018f, SettleLeanPerGen = 0.30f;
        const int   SettleMaxGen     = 8;
        static void SettleCodex(GameObject go, WorldVillage v, CodexEntry e, int k)
        {
            if (go == null || v == null || e == null) return;
            var t = Terrain.activeTerrain; if (t == null) return;
            int gens = Mathf.Clamp(v.maxGen, 0, SettleMaxGen);
            if (gens <= 0) return;
            uint h = Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length * 13 + k);
            float bias = ((h & 0xFFu) / 255f) * 0.6f + 0.4f;
            float sink = gens * SettleSinkPerGen * bias;
            float lean = gens * SettleLeanPerGen * bias;
            float dir  = ((h >> 8) & 0xFFu) / 255f * 360f;
            var p = go.transform.position; p.y = GroundW(p).y - sink; go.transform.position = p;
            float yaw = Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length + k) % 360u;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f)
                                  * Quaternion.AngleAxis(lean, Quaternion.Euler(0f, dir, 0f) * Vector3.forward);
        }

        static Vector2 CodexPlacement(WorldVillage v, CodexEntry e, int k, int cnt)
        {
            float baseAng = (Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length * 7) % 360u) * Mathf.Deg2Rad;
            float ang = baseAng + (cnt > 1 ? k * (6.2832f / cnt) : 0f);
            float r = e.placement == "edge" ? 5.0f : e.placement == "green" ? 2.4f : 3.5f;
            return new Vector2(v.x + Mathf.Cos(ang) * r, v.y + Mathf.Sin(ang) * r);
        }

        static GameObject LoadCodexPrefab(string name)
        {
            if (name.EndsWith(".glb")) return AssetDatabase.LoadAssetAtPath<GameObject>(TechDir + name)
                                           ?? AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + name)
                                           ?? AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + name);
            return FindPrefab(name);
        }

        // D-878: exact-name lookup restricted to a pack root (P_ENV_stone_01 exists in BOTH Village and Nature — "mät asseten, lita aldrig på namnet")
        static GameObject FindPrefabIn(string root, string exact)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {exact}", new[] { root }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == exact) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            return null;
        }
        static GameObject[] FindPrefabsIn(string root, string prefix, string mustContain = null)
            => AssetDatabase.FindAssets($"t:Prefab {prefix}", new[] { root })
               .Select(g => AssetDatabase.GUIDToAssetPath(g)).OrderBy(p => p, StringComparer.Ordinal)
               .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
               .Where(p => p != null && p.name.StartsWith(prefix, StringComparison.Ordinal) && (mustContain == null || p.name.Contains(mustContain))).ToArray();

        static GameObject FindPrefab(string name)
        {
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }
        // exact-name prefab lookup (FindPrefab's fuzzy FirstOrDefault would confuse e.g.
        // Prefab_Grass_01 with Prefab_Grass_01_Detail) — D-101 meadow needs the exact detail assets.
        static GameObject FindPrefabExact(string name)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            return null;
        }
        static IEnumerable<GameObject> FindPrefabs(string prefix)
            => AssetDatabase.FindAssets($"t:Prefab {prefix}").Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null && p.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
#endif
