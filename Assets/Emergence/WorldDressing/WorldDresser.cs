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
    [Serializable] public class WorldVillage { public float x, y; public string name; }
    [Serializable] public class WorldAnimal { public int id; public string type; public float x, y; }
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
            BuildTerrain(S, root.transform);
            BuildWater(S, root.transform);
            PlaceGroundFeatures(S, root.transform); // TD-031 terrain pass: field soil + desire-line paths as mesh decals (URP won't render the terrain splat)
            PlaceHuts(S, root.transform);         // TD-031 v2: houses face the green (scaled) + lived-in yards per house
            PlaceFires(S, root.transform);
            PlaceFields(S, root.transform);
            PlaceNature(S, root.transform);
            PlaceAgents(S, root.transform);       // the studio's own rendered villagers (EP directive)
            PlaceTechAnchors(S, root.transform);  // forge/mill/kiln/well — fills the D-062 pack gap
            PlaceAnimals(S, root.transform);      // the studio's own deer/wolf GLBs (animal upgrade)
            EmergenceLightRig.Apply(S.season, "day");
            Debug.Log("[Dresser] world built — iterate grammar/density from here (menu re-runs are idempotent: fresh scene each time)");
        }

        static char Tile(WorldState S, int x, int y) => S.tileTypes[y * S.W + x];
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
            data.size = new Vector3(S.W * TileSize, 30f, S.H * TileSize);
            // gentle deterministic relief; water carved slightly below
            var heights = new float[res, res];
            for (int ry = 0; ry < res; ry++)
                for (int rx = 0; rx < res; rx++)
                {
                    float sx = rx / (float)(res - 1) * (S.W - 1);
                    float sy = (1f - ry / (float)(res - 1)) * (S.H - 1);
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                    float baseH = 0.10f
                        + 0.012f * Mathf.PerlinNoise(sx * 0.07f + S.seed % 977 * 0.13f, sy * 0.07f)
                        + 0.03f * Mathf.PerlinNoise(sx * 0.015f, sy * 0.015f + S.seed % 719 * 0.17f);
                    if (Tile(S, tx, ty) == 'w') baseH -= 0.035f;
                    else if (Tile(S, tx, ty) == 's') baseH += 0.012f;
                    heights[ry, rx] = baseH;
                }
            data.SetHeights(0, 0, heights);

            // splat layers resolved from the packs (as imported); fallback = plain color layers
            var layers = new List<TerrainLayer>();
            int liGrass = AddLayer(layers, "Layer_grass_01", new Color(0.35f, 0.5f, 0.22f));
            int liField = AddLayer(layers, "Layer_farmfield", new Color(0.45f, 0.35f, 0.2f));
            int liPath = AddLayer(layers, "Layer_Dirt", new Color(0.42f, 0.32f, 0.2f)); // TD-031 v2.1: worn desire-line ground (also serves sand/clay tiles as bare earth)
            int liGravel = AddLayer(layers, "Layer_gravel_01", new Color(0.5f, 0.48f, 0.45f));
            // NOTE: keep total terrain layers <= 4 — the URP terrain base pass renders only the first 4 splats;
            // a 5th layer silently paints nothing (the v2.1 path bug). grass/field/dirt/gravel is the budget.
            data.terrainLayers = layers.ToArray();

            data.alphamapResolution = 256;
            var am = new float[256, 256, layers.Count];
            for (int ay = 0; ay < 256; ay++)
                for (int ax = 0; ax < 256; ax++)
                {
                    float sx = ax / 255f * (S.W - 1);
                    float sy = (1f - ay / 255f) * (S.H - 1);
                    int tx = Mathf.Clamp(Mathf.RoundToInt(sx), 0, S.W - 1), ty = Mathf.Clamp(Mathf.RoundToInt(sy), 0, S.H - 1);
                    int li = liGrass;
                    switch (Tile(S, tx, ty))
                    {
                        case 's': case 'i': li = liGravel; break;
                        case 'a': case 'c': li = liPath; break; // sand/clay read as bare earth (dirt layer)
                    }
                    am[ay, ax, li] = 1f;
                }
            StampFields(S, am, liField, 256); // TD-031 v2.1b: tilled soil inside the field enclosures (was never stamped)
            PaintPaths(S, am, liPath, 256); // TD-031 v2.1: worn desire lines (hut->green, village->village)
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
            terrain.drawInstanced = true;
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

        static int AddLayer(List<TerrainLayer> layers, string packLayerName, Color fallback)
        {
            var guid = AssetDatabase.FindAssets($"t:TerrainLayer {packLayerName}").FirstOrDefault();
            TerrainLayer tl;
            if (guid != null) tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(AssetDatabase.GUIDToAssetPath(guid));
            else
            {
                tl = new TerrainLayer { diffuseTexture = Texture2D.whiteTexture, diffuseRemapMax = new Vector4(fallback.r, fallback.g, fallback.b, 1) };
                AssetDatabase.CreateAsset(tl, $"Assets/Emergence/Scenes/TL_{packLayerName}.asset");
            }
            layers.Add(tl);
            return layers.Count - 1;
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
                        for (int l = 0; l < nlayers; l++) am[cy, cx, l] = (l == liField) ? 1f : 0f;
                    }
            }
        }

        // TD-031 v2.1: paint worn ground along DESIRE LINES into the terrain alphamap — each hut to its
        // village green (the commons everyone walks to), and each village to its nearest neighbour (the
        // trackway between settlements). Deterministic geometry from sim data — no RNG (D-078 rule 4).
        static void PaintPaths(WorldState S, float[,,] am, int liPath, int res)
        {
            var greens = VillageGreens(S);
            var segs = new List<(Vector2 a, Vector2 b)>();
            foreach (var h in S.huts)
            {
                int vi = NearestVillageIdx(S, h.x, h.y);
                Vector2 g = (vi >= 0 && greens != null && vi < greens.Length) ? greens[vi] : new Vector2(h.x, h.y);
                segs.Add((new Vector2(h.x, h.y), g));
            }
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
                    if (nj > i) segs.Add((new Vector2(S.villages[i].x, S.villages[i].y), new Vector2(S.villages[nj].x, S.villages[nj].y)));
                }
            int nlayers = am.GetLength(2);
            int cellsPainted = 0;
            const int rr = 2; // brush radius in alphamap cells (~3.1 m/cell → ~15 m worn track)
            foreach (var (a, b) in segs)
            {
                float len = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 4f));
                for (int s = 0; s <= steps; s++)
                {
                    var p = Vector2.Lerp(a, b, s / (float)steps);
                    int ax = Mathf.RoundToInt(p.x / Mathf.Max(1, S.W - 1) * (res - 1));
                    int ay = Mathf.RoundToInt((1f - p.y / Mathf.Max(1, S.H - 1)) * (res - 1));
                    for (int dy = -rr; dy <= rr; dy++)
                        for (int dx = -rr; dx <= rr; dx++)
                        {
                            if (dx * dx + dy * dy > rr * rr + 1) continue; // round brush
                            int cx = ax + dx, cy = ay + dy;
                            if (cx < 0 || cy < 0 || cx >= res || cy >= res) continue;
                            for (int l = 0; l < nlayers; l++) am[cy, cx, l] = (l == liPath) ? 1f : 0f;
                            cellsPainted++;
                        }
                }
            }
            Debug.Log($"[Dresser] paths: {segs.Count} desire-line segments, {cellsPainted} alphamap cells painted (layer {liPath})");
        }

        static void BuildWater(WorldState S, Transform root)
        {
            // v1: pack water prefab per water tile-cluster centroid if found, else one plane per tile group
            var waterPrefab = FindPrefab("P_FX_water_FVP");
            var parent = new GameObject("Water").transform; parent.SetParent(root, true);
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                    if (Tile(S, x, y) == 'w' && Hash(x, y, 7) % 1 == 0)
                    {
                        if (waterPrefab != null && Hash(x, y, 11) % 4 == 0) // sparse instancing of the pack's water FX
                        {
                            var w = (GameObject)PrefabUtility.InstantiatePrefab(waterPrefab, parent);
                            w.transform.position = P(S, x, y, -1.1f);
                        }
                        else
                        {
                            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            plane.name = "w";
                            plane.transform.SetParent(parent, true);
                            plane.transform.position = P(S, x, y, -0.9f);
                            plane.transform.rotation = Quaternion.Euler(90, 0, 0);
                            plane.transform.localScale = new Vector3(TileSize, TileSize, 1);
                            var mr = plane.GetComponent<MeshRenderer>();
                            var wm = FindMaterial("M_ENV_water") ?? FindMaterial("M_FX_water") ?? FindMaterial("M_water") ?? FindMaterial("water");
                            if (wm != null) mr.sharedMaterial = wm;
                            else mr.sharedMaterial.color = new Color(0.23f, 0.42f, 0.55f); // sober fallback: no white lakes
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

        static void PlaceGroundFeatures(WorldState S, Transform root)
        {
            var parent = new GameObject("GroundFeatures").transform; parent.SetParent(root, true);
            var fieldMat = GroundMat("Layer_farmfield", new Color(0.42f, 0.32f, 0.20f));
            var dirtMat = GroundMat("Layer_Dirt", new Color(0.46f, 0.36f, 0.24f));
            int fields = 0, path = 0;
            // field soil: one quad per field tile, inside the enclosures
            if (S.fields != null)
                foreach (var f in S.fields) { Decal(S, parent, fieldMat, f.x, f.y, TileSize * 0.98f, 0.06f, $"fieldsoil_{fields}"); fields++; }
            // desire-line paths: overlapping quads stepped along hut->green + village->village
            var greens = VillageGreens(S);
            var segs = new List<(Vector2 a, Vector2 b)>();
            foreach (var h in S.huts)
            {
                int vi = NearestVillageIdx(S, h.x, h.y);
                segs.Add((new Vector2(h.x, h.y), (vi >= 0 && vi < greens.Length) ? greens[vi] : new Vector2(h.x, h.y)));
            }
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
                    if (nj > i) segs.Add((new Vector2(S.villages[i].x, S.villages[i].y), new Vector2(S.villages[nj].x, S.villages[nj].y)));
                }
            foreach (var (a, b) in segs)
            {
                float len = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
                for (int s = 0; s <= steps; s++) { var p = Vector2.Lerp(a, b, s / (float)steps); Decal(S, parent, dirtMat, p.x, p.y, 3.4f, 0.08f, $"path_{path}"); path++; }
            }
            Debug.Log($"[Dresser] ground decals: {fields} field-soil + {path} path quads (mesh-decal ground; terrain splat won't render)");
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
        static bool Working(string task) => task != null && (task.Contains("work") || task.Contains("forg")
            || task.Contains("tend") || task.Contains("build") || task.Contains("mill") || task.Contains("craft")
            || task.Contains("bak") || task.Contains("smith") || task.Contains("dig") || task.Contains("chop"));

        static bool Moving(string task) => task != null && (task.Contains("walk") || task.Contains("forag")
            || task.Contains("fish") || task.Contains("hunt") || task.Contains("gather") || task.Contains("carr")
            || task.Contains("seek") || task.Contains("wander") || task.Contains("go ") || task.Contains("toward")
            || task.Contains("travel") || task.Contains("herd"));

        // the walk/work GLBs carry an AnimationClip; sampling it in EDIT mode POSES the model to a
        // frame (mid-stride / mid-work), so stills read dynamic + varied — no play mode needed.
        static AnimationClip LoadClip(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                    return c;
            return null;
        }

        static void PlaceAgents(WorldState S, Transform root)
        {
            var parent = new GameObject("Agents").transform; parent.SetParent(root, true);
            int placed = 0;
            foreach (var a in S.agents)
            {
                string band = a.age < 14 ? "child" : a.age > 55 ? "elder" : "adult";
                bool female = (Hash((int)a.x, (int)a.y, a.id) & 1u) == 0u;
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
        static void PlaceAnimals(WorldState S, Transform root)
        {
            if (S.animals == null || S.animals.Length == 0) return;
            var parent = new GameObject("Animals").transform; parent.SetParent(root, true);
            int placed = 0;
            foreach (var an in S.animals)
            {
                string nm = an.type == "wolf" ? "wolf" : "deer";
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + nm + ".glb");
                if (pf == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
                go.transform.position = Ground(S, an.x, an.y, 0f);
                go.transform.rotation = Quaternion.Euler(0f, Hash((int)an.x, (int)an.y, an.id) % 360u, 0f);
                go.transform.localScale = Vector3.one * AnimalScale;
                go.name = $"{an.type}_{an.id}";
                placed++;
            }
            Debug.Log($"[Dresser] placed {placed}/{S.animals.Length} animals (own deer/wolf GLBs)");
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
            var trees = new[] { FindPrefab("P_ENV_TREE_village") }.Where(p => p != null)
                .Concat(FindPrefabs("Prefab_TreeLarge").Where(p => !p.name.Contains("Coverage")).Take(4)).ToArray(); // no *_Coverage variants (same broken material family)
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
                if (prefab.name.StartsWith("Prefab_TreeLarge")) sc *= 0.42f; // Dreamscape canopy trees are landmarks, not the forest baseline (silhouette law)
                go.transform.localScale = Vector3.one * sc;
                StripImpostorLods(go); // Dreamscape impostor billboards lack baked textures in edit mode -> magenta at distance
            }
        }

        [MenuItem("Emergence/P1 Dressing/Find Pink In Scene")]
        public static void FindPinkInScene()
        {
            var bad = new Dictionary<string, int>();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
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

        static GameObject FindPrefab(string name)
        {
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }
        static IEnumerable<GameObject> FindPrefabs(string prefix)
            => AssetDatabase.FindAssets($"t:Prefab {prefix}").Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null && p.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
#endif
