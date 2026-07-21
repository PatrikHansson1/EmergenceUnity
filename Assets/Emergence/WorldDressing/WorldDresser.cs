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

namespace Emergence.Editor
{
    [Serializable] public class WorldAgent { public int id; public string name; public float x, y; public float age; public int gen; public string task, say, sayAct; }
    [Serializable] public class WorldHut { public float x, y; public string owner; public bool free; }
    [Serializable] public class WorldFire { public float x, y; public float fuel; }
    [Serializable] public class WorldField { public float x, y; public int stage; public string owner; }
    // TD-033: villages now carry their development profile (aggregate of members' knowledge + beliefs +
    // demographics) so the codex can place objects by DISCOVERY. Old exports lack these → default 0/null → safe.
    [Serializable] public class WorldVillage { public float x, y; public string name; public int pop, maxGen, avgAge, crafts; public string cosmos; public string[] knows; public string[] beliefs; }
    [Serializable] public class WorldAnimal { public int id; public string type; public float x, y; }
    // TD-033: the object codex — discovery-driven placement. JsonUtility-friendly flat schema.
    // D-112 (Fas 1 inc 2): ruinOnLoss=1 → when this built structure's gate stops holding (Memory Engine
    // loses the tech), the object de-materialises INTO a ruin instead of empty ground; rediscovery rebuilds it.
    // ruinPrefab overrides the studio default ruin stand-in; ruinScale sizes it (0 = default). Ephemeral/portable
    // objects (banners, carts, pots) keep ruinOnLoss=0 and simply vanish.
    // D-106 fill-pass: tier = milestone|dressing|part (legibility law); statMeaning feeds the STATS/Almanac pillar (Fas 5).
    [Serializable] public class CodexEntry { public string id, prefab, category, requiresTech, requiresCustom, desc, placement, tier, statMeaning; public int era, minPop, minCrafts, minGen, count; public float scale; public int ruinOnLoss; public string ruinPrefab; public float ruinScale; }
    [Serializable] public class Codex { public CodexEntry[] objects; }
    [Serializable] public class WorldState
    {
        public string engineVersion; public int seed, years, tick; public bool ended; public string season;
        public int W, H; public string tileTypes; public int[] tileN;
        public WorldAgent[] agents; public WorldHut[] huts; public WorldFire[] fires;
        public WorldField[] fields; public WorldVillage[] villages; public WorldAnimal[] animals;
    }

    public static class WorldDresser
    {
        public const float TileSize = 8f;          // meters per sim tile (Producer knob)
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
        public const float VillagerScale = 1f;   // GLBs authored ~1.7m; tune after first import
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
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);

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
            PlaceTechAnchors(S, root.transform);  // forge/mill/kiln/well — fills the D-062 pack gap
            PlaceCodexObjects(S, root.transform); // TD-033: discovery-driven objects (mill/tablets/star-banner/market by village development)
            PlaceAnimals(S, root.transform);      // the studio's own deer/wolf GLBs (animal upgrade)
            EmergenceLightRig.Apply(S.season, "day");
            Debug.Log("[Dresser] world built — iterate grammar/density from here (menu re-runs are idempotent: fresh scene each time)");
        }

        static char Tile(WorldState S, int x, int y) => S.tileTypes[y * S.W + x];
        static int TileN(WorldState S, int x, int y) => (S.tileN != null && y * S.W + x < S.tileN.Length) ? S.tileN[y * S.W + x] : 9;

        // TD-031 v2.2b: work-marks — "stone is visible where stone is won" (grammar §2, Materials). A stone
        // tile the people have QUARRIED (low tileN = harvested) gets a bare-earth scar decal + worked-stone
        // props. Documentary-honest: low n IS worked stone in the sim. Hash-placed, RNG-neutral (D-078 r4).
        static void PlaceWorkMarks(WorldState S, Transform root)
        {
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
            int liGrass = AddLayer(layers, new[] { "Layer_Grass", "Layer_grass_01" }, new Color(0.35f, 0.5f, 0.22f));
            int liField = AddLayer(layers, new[] { "Layer_farmfield", "Layer_Dirt" }, new Color(0.45f, 0.35f, 0.2f));
            int liPath = AddLayer(layers, new[] { "Layer_Dirt" }, new Color(0.42f, 0.32f, 0.2f)); // worn desire-line ground (also sand/clay)
            int liGravel = AddLayer(layers, new[] { "Layer_Rock", "Layer_gravel_01" }, new Color(0.5f, 0.48f, 0.45f));
            // D-120 roads v1.1: 5th layer = COBBLESTONE for the paved-street tier. >4 layers needs URP's 8-layer
            // path — we now enable the _TERRAIN_8_LAYERS keyword on the terrain material (below) so it renders.
            int liCobble = AddLayer(layers, new[] { "Layer_Cobblestone", "Layer_pavingstone_01" }, new Color(0.55f, 0.53f, 0.5f));
            data.terrainLayers = layers.ToArray();

            data.alphamapResolution = 256;
            var am = new float[256, 256, layers.Count];
            for (int ay = 0; ay < 256; ay++)
                for (int ax = 0; ax < 256; ax++)
                {
                    float sx = ax / 255f * (S.W - 1);
                    float sy = (1f - ay / 255f) * (S.H - 1);
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
            StampFields(S, am, liField, 256); // TD-031 v2.1b: tilled soil inside the field enclosures (was never stamped)
            PaintRoutes(S, am, 256, liPath, liCobble); // D-116/120 EMERGENT ROADS: tie-derived, wear→width, tech-gated COBBLE tier
            data.SetAlphamaps(0, 0, am);

            AssetDatabase.CreateAsset(data, "Assets/Emergence/Scenes/TerrainData_generated.asset");
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
            string[] protoNames = { "Prefab_Grass_01_Detail", "Prefab_Grass_Group_01_Detail", "Prefab_Grass_03_Detail", "SM_Flower_01_Unity", "Prefab_Flower_02", "Prefab_Flower_04" };
            // D-101c: per-layer green variation so the meadow isn't one flat tone — some cooler, some
            // warmer-lit; flowers keep a white tint so their own texture colour shows.
            Color[] grassGreens = { new Color(0.82f, 0.95f, 0.70f), new Color(0.68f, 0.86f, 0.55f), new Color(0.90f, 0.93f, 0.72f) };
            var dps = new List<DetailPrototype>();
            var isFlower = new List<bool>();
            int gi = 0;
            foreach (var nm in protoNames)
            {
                var pf = FindPrefabExact(nm);
                if (pf == null) continue;
                bool flower = nm.ToLower().Contains("flower");
                dps.Add(new DetailPrototype
                {
                    prototype = pf,
                    usePrototypeMesh = true,
                    useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit,
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
            else Debug.LogWarning("[Dresser] no Dreamscape detail-grass prefabs found — meadow detail skipped");

            // -- trees as GAMEOBJECTS, not Unity terrain trees (D-101f). THE FIX: terrain trees render
            // through a separate path that ignores our fill light AND doesn't reflect material edits — that
            // was the "dark blob" (immune to 12 material/shader/reimport attempts). GameObjects light exactly
            // like the bushes that already read well. Sparse scatter over open meadow, clear of villages.
            string[] treeNames = { "Prefab_Birch_01", "Prefab_Birch_02", "Prefab_Birch_03", "Prefab_TreeLarge_01", "Prefab_TreeLarge_02", "Prefab_TreeLarge_03" };
            var treePfs = treeNames.Select(FindPrefabExact).Where(p => p != null).ToArray();
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
                        if (pf.name.StartsWith("Prefab_TreeLarge")) sc *= 0.9f;
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
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
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
        struct Route { public Vector2 a, b; public float wear; public int layer; }

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
                    routes.Add(new Route { a = new Vector2(h.x, h.y), b = g, wear = Mathf.Clamp01(0.30f + pop / 45f), layer = liDirt });
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
                        routes.Add(new Route { a = new Vector2(vi.x, vi.y), b = new Vector2(vj.x, vj.y), wear = wear, layer = liDirt });
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
                int rr = 1 + Mathf.RoundToInt(Mathf.Clamp01(r.wear) * 2f);           // WIDTH by wear: 1..3 cells
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
                            if (dx * dx + dy * dy > rr * rr + 1) continue;           // round brush
                            int cx = ax + dx, cy = ay + dy;
                            if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                            for (int l = 0; l < nlayers; l++) am[cy, cx, l] = 0f;
                            // D-120: paved tier = mostly cobble but blended with worn dirt so it reads as a WARM
                            // trodden street, not a stark white checkerboard; dirt/trail tiers = full dirt.
                            if (r.layer == liCobble) { am[cy, cx, liCobble] = 0.4f; am[cy, cx, liDirt] = 0.6f; }
                            else am[cy, cx, r.layer] = 1f;
                            cells++;
                        }
                }
            }
            Debug.Log($"[Dresser] EMERGENT ROADS: {routes.Count} routes ({cobbleRoutes} paved/cobble), {cells} cells painted — tie-derived, wear→width (D-116)");
        }

        // D-115: Dreamscape's own ambient particle FX — drifting leaves (Leaf_Particle_Wind) + dust motes
        // (Dust_Particle) sparsely over the open meadow. Atmosphere the still render can't show (particles
        // need play mode) but the EP sees on Play. Deterministic hash scatter (D-078 r4), never RNG.
        static void PlaceAmbientFX(WorldState S, Transform root)
        {
            var parent = new GameObject("AmbientFX").transform; parent.SetParent(root, true);
            var leaf = FindPrefab("Leaf_Particle_Wind") ?? FindPrefab("Leaf_Particle");
            var dust = FindPrefab("Dust_Particle");
            if (leaf == null && dust == null) { Debug.LogWarning("[Dresser] no Dreamscape ambient particles found"); return; }
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
            Debug.Log($"[Dresser] {placed} Dreamscape ambient FX (drifting leaves + dust motes)");
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
                            var pf = FindPrefab("Prefab_WaterLake") ?? FindPrefab("SM_WaterRiver");
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
                                var wm = FindMaterial("MI_Water_MeadowsLake") ?? FindMaterial("M_Dreamscape_WaterRiver")
                                         ?? FindMaterial("M_ENV_water") ?? FindMaterial("water");
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
            for (int i = 0; i < S.huts.Length; i++)
            {
                var h = S.huts[i];
                int hx = Mathf.RoundToInt(h.x), hy = Mathf.RoundToInt(h.y);
                int variant = 1 + (int)(Hash(hx, hy, 21) % 13); // P_BLD_house_01..13 (14 exists too; 13 keeps U-day-verified range)
                var prefab = FindPrefab($"P_BLD_house_{variant:00}") ?? FindPrefab("P_BLD_house_01");
                if (prefab == null) { Debug.LogWarning("[Dresser] no house prefab found"); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = Ground(S, h.x, h.y);
                float yaw = HouseYaw(S, h, greens, hx, hy);
                go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                go.transform.localScale = Vector3.one * HouseScale; // v2: pack houses are oversized at 1
                go.name = $"hut_{h.owner}";
                yardCount += PlaceYard(S, go, h, hx, hy, yaw, yardProps, yardParent);
                int og = genOf.TryGetValue(h.owner, out var gg) ? gg : maxGen;
                float ageFrac = maxGen > 1 ? 1f - og / (float)maxGen : 0.5f; // 1 = oldest (founder), 0 = newest edge
                ageMarks += PlaceHutAge(S, h, hx, hy, ageFrac, mossProps, freshProps, ageParent);
            }
            Debug.Log($"[Dresser] {S.huts.Length} houses (scale {HouseScale}) + {yardCount} yard props + {ageMarks} age marks (v2.2 grammar, maxGen {maxGen})");
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
                placed++;
            }
            Debug.Log($"[Dresser] placed {placed}/{S.agents.Length} villagers" + (placed == 0 ? " (0 — glTFast not imported yet?)" : ""));
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
        static void PlaceTechAnchors(WorldState S, Transform root)
        {
            var parent = new GameObject("TechAnchors").transform; parent.SetParent(root, true);
            var well = AssetDatabase.LoadAssetAtPath<GameObject>(TechDir + "well.glb");
            string[] craft = { "forge", "mill", "kiln" };
            foreach (var v in S.villages)
            {
                if (well != null) Anchor(well, S, v.x, v.y, parent);
                var cName = craft[Hash((int)v.x, (int)v.y, 3) % (uint)craft.Length];
                var c = AssetDatabase.LoadAssetAtPath<GameObject>(TechDir + cName + ".glb");
                if (c != null) Anchor(c, S, v.x + 2.2f, v.y + 1.4f, parent);
            }
        }

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

        static void PlaceFields(WorldState S, Transform root)
        {
            // Composition grammar v1.5: fields are ENCLOSURES, not confetti.
            // Adjacent field tiles form a cluster; the fence follows the cluster's
            // OUTER edges only, with one hash-picked gate opening per cluster.
            var parent = new GameObject("Fields").transform; parent.SetParent(root, true);
            var fence = FindPrefab("P_PROP_fence_v01_01");
            var gate = FindPrefab("P_PROP_fence_door_gate") ?? fence;
            if (fence == null || S.fields.Length == 0) return;

            var fieldSet = new HashSet<(int, int)>(S.fields.Select(f => (Mathf.RoundToInt(f.x), Mathf.RoundToInt(f.y))));
            // cluster by flood fill
            var seen = new HashSet<(int, int)>();
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
                // outer edges of the cluster
                var edges = new List<(int x, int y, int side)>(); // side: 0=+z(N) 1=+x(E) 2=-z(S) 3=-x(W)
                foreach (var (cx, cy) in cluster)
                {
                    if (!fieldSet.Contains((cx, cy - 1))) edges.Add((cx, cy, 0)); // sim -y => world +z
                    if (!fieldSet.Contains((cx + 1, cy))) edges.Add((cx, cy, 1));
                    if (!fieldSet.Contains((cx, cy + 1))) edges.Add((cx, cy, 2));
                    if (!fieldSet.Contains((cx - 1, cy))) edges.Add((cx, cy, 3));
                }
                if (edges.Count == 0) continue;
                int gateIdx = (int)(Hash(cluster[0].Item1, cluster[0].Item2, 61) % (uint)edges.Count);
                for (int e = 0; e < edges.Count; e++)
                {
                    var (ex, ey, side) = edges[e];
                    float half = TileSize * 0.5f;
                    Vector3 off = side == 0 ? new Vector3(0, 0, half) : side == 1 ? new Vector3(half, 0, 0) : side == 2 ? new Vector3(0, 0, -half) : new Vector3(-half, 0, 0);
                    float yRot = side % 2 == 0 ? 0f : 90f;
                    var prefab = e == gateIdx ? gate : fence;
                    // fill the 8 m edge with segments along its direction
                    int segs = e == gateIdx ? 1 : 3;
                    for (int k = 0; k < segs; k++)
                    {
                        float t = segs == 1 ? 0f : (k - (segs - 1) * 0.5f) * (TileSize / segs);
                        Vector3 along = side % 2 == 0 ? new Vector3(t, 0, 0) : new Vector3(0, 0, t);
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                        go.transform.position = Ground(S, ex, ey) + off + along;
                        go.transform.rotation = Quaternion.Euler(0, yRot, 0);
                    }
                }
            }
        }

        static void PlaceNature(WorldState S, Transform root)
        {
            var parent = new GameObject("Nature").transform; parent.SetParent(root, true);
            // D-101e: the village pack ships ONE tree (P_ENV_TREE_village) and it renders as a flat dark
            // blob — drop it. Use DREAMSCAPE's real tree library for the woodland (their reference-quality
            // large trees + birches). Village pack = the built world; Dreamscape = the natural world.
            var trees = FindPrefabs("Prefab_TreeLarge").Where(p => !p.name.Contains("Coverage")).Take(4)
                .Concat(FindPrefabs("Prefab_Birch").Where(p => !p.name.Contains("Coverage") && !p.name.Contains("Red")).Take(4)).ToArray();
            var rocks = FindPrefabs("Prefab_RockFormation").Take(4).Concat(new[] { FindPrefab("P_ENV_stone_01") }.Where(p => p != null)).ToArray(); // NOT RocksRound: uses the broken M_RoundedRocks_Coverage (pack-author leftover, VERDICT.md)
            var bushes = FindPrefabs("Prefab_Bush").Where(p => !p.name.Contains("Flower")).Take(3).ToArray(); // berry tiles read as berries, not blossom (AD)
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
                if (prefab.name.StartsWith("Prefab_TreeLarge")) sc *= 0.72f; // D-101e: Dreamscape large trees are now the woodland baseline — proper tree size, not tiny landmarks
                go.transform.localScale = Vector3.one * sc;
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
            var flowers = new[] { "Prefab_Flower_01", "Prefab_Flower_03", "Prefab_Flower_04" }.Select(FindPrefabExact).Where(p => p != null).ToArray();
            var tufts = new[] { "Prefab_Grass_Group_01", "Prefab_Grass_Group_02" }.Select(FindPrefabExact).Where(p => p != null).ToArray();
            var smallBush = new[] { "Prefab_Bush_01", "Prefab_Bush_04_Flowers" }.Select(FindPrefabExact).Where(p => p != null).ToArray();
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
                foreach (var e in codex.objects)
                {
                    if (!CodexQualifies(v, e)) continue;
                    var pf = LoadCodexPrefab(e.prefab);
                    if (pf == null) continue;
                    int cnt = Mathf.Max(1, e.count);
                    for (int k = 0; k < cnt; k++)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                        var pos = CodexPlacement(v, e, k, cnt);
                        go.transform.position = GroundW(P(S, pos.x, pos.y));
                        go.transform.rotation = Quaternion.Euler(0f, Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length + k) % 360u, 0f);
                        go.transform.localScale = Vector3.one * (e.scale <= 0f ? 1f : e.scale);
                        go.name = $"codex_{e.id}_{v.name}_{k}";
                        StripImpostorLods(go);
                        placed++;
                    }
                }
            Debug.Log($"[Dresser] codex: {placed} discovery-driven objects across {S.villages.Length} villages (the world reads its own development)");
        }

        static bool CodexQualifies(WorldVillage v, CodexEntry e)
        {
            if (!string.IsNullOrEmpty(e.requiresTech) && (v.knows == null || Array.IndexOf(v.knows, e.requiresTech) < 0)) return false;
            if (!string.IsNullOrEmpty(e.requiresCustom))
            {
                if (e.requiresCustom == "cosmos") { if (string.IsNullOrEmpty(v.cosmos)) return false; }
                else if (v.beliefs == null || Array.IndexOf(v.beliefs, e.requiresCustom) < 0) return false;
            }
            return v.pop >= e.minPop && v.crafts >= e.minCrafts && v.maxGen >= e.minGen;
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
