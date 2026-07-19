// EMERGENCE P1 — THE DRESSING LAYER (editor-driven v1: machinery, grammar iterates on top)
// D-078 rule 4 codified: this layer is PRESENTATION — it READS an exported world
// state, uses POSITION HASHES for all variety (never S.rand, never Unity Random
// seeded from sim), and never writes back. AD owns the look, GD owns the grammar's
// design language, TD enforces read-only. Density budgets are the Producer's knife.
//
// v1 scope (P1a): terrain from tiles (splat by type, water plane), hut->house
// placement, fields, village markers, trees/rocks/berries by density budget,
// light rig hookup. Composition grammar (plots/yards/fences/roads) iterates here.
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
            PlaceHuts(S, root.transform);
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
            int liGravel = AddLayer(layers, "Layer_gravel_01", new Color(0.5f, 0.48f, 0.45f));
            int liSand = AddLayer(layers, "Layer_pavingstone", new Color(0.76f, 0.7f, 0.5f));
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
                        case 'a': case 'c': li = liSand; break;
                    }
                    am[ay, ax, li] = 1f;
                }
            data.SetAlphamaps(0, 0, am); // field-splat stamping joins the grammar iteration

            AssetDatabase.CreateAsset(data, "Assets/Emergence/Scenes/TerrainData_generated.asset");
            var tgo = Terrain.CreateTerrainGameObject(data);
            tgo.name = "Terrain";
            tgo.transform.SetParent(root, true);
            tgo.transform.position = new Vector3(0, -3f, 0);
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

        static void PlaceHuts(WorldState S, Transform root)
        {
            var parent = new GameObject("Huts").transform; parent.SetParent(root, true);
            for (int i = 0; i < S.huts.Length; i++)
            {
                var h = S.huts[i];
                int hx = Mathf.RoundToInt(h.x), hy = Mathf.RoundToInt(h.y);
                int variant = 1 + (int)(Hash(hx, hy, 21) % 13); // P_BLD_house_01..13 (14 exists too; 13 keeps U-day-verified range)
                var prefab = FindPrefab($"P_BLD_house_{variant:00}") ?? FindPrefab("P_BLD_house_01");
                if (prefab == null) { Debug.LogWarning("[Dresser] no house prefab found"); return; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = Ground(S, h.x, h.y);
                go.transform.rotation = Quaternion.Euler(0, Hash(hx, hy, 22) % 4 * 90 + (Hash(hx, hy, 23) % 21 - 10), 0); // grid-ish with jitter — grammar iterates
                go.name = $"hut_{h.owner}";
            }
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
            for (int y = 0; y < S.H; y++)
                for (int x = 0; x < S.W; x++)
                {
                    char t = Tile(S, x, y);
                    if (t == 'f' && trees.Length > 0) Scatter(S, parent, trees, x, y, TreesPerForestTile, 41);
                    else if (t == 's' && rocks.Length > 0) Scatter(S, parent, rocks, x, y, RocksPerStoneTile, 43);
                    else if (t == 'b' && bushes.Length > 0) Scatter(S, parent, bushes, x, y, BushesPerBerryTile, 47);
                }
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
