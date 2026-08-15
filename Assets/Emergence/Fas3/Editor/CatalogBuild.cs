// EMERGENCE — FAS 3 increment 4 (D-137): the CATALOG BUILD pass.
//
// Editor-side inventory: resolves — with the SAME lookup rules the reconcilers used when they
// were editor-only (LoadCodexPrefab's .glb dirs + exact-name prefab search; PlaceHutAge's
// Prefab_Bush query IN QUERY ORDER) — every asset name the runtime reconcilers can ever ask for,
// and stores direct references in Assets/Emergence/Resources/EmergenceAssetCatalog.asset.
// Re-run after any codex edit or asset import that adds/renames prefabs the codex references.
// Menu: Emergence/Fas3/BUILD ASSET CATALOG.  Headless: drop Reports/RUN_CATALOG.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class CatalogBuild
    {
        const string CodexPath = "Assets/Emergence/Codex/object-codex.json";
        const string TechDir   = "Assets/Emergence/Models/tech/";
        const string NatureDir = "Assets/Emergence/Models/nature/";
        const string CharDir   = "Assets/Emergence/Models/characters/";
        const string AnimDir   = "Assets/Emergence/Fas2/Anim";
        const string CarryPropPath = "Assets/Fantastic City Pack/Prefabs/Props/Comps/COMP_PROP_basket_city_01.prefab";
        const string OutDir    = "Assets/Emergence/Resources";
        const string OutPath   = OutDir + "/" + EmergenceAssetCatalog.ResourcesName + ".asset";

        static readonly string[] YardPropNames = {
            "P_PROP_cart_01","P_PROP_cart_02","P_PROP_barrel_01","P_PROP_barrel_03","P_PROP_crate_01",
            "P_PROP_crate_03","P_PROP_sack_02","P_PROP_sack_05","P_PROP_firepit_woodpile","P_PROP_hay_02",
            "P_PROP_hay_04","P_PROP_bucket_01","P_PROP_trough_01"
        };
        static readonly string[] FreshBuildNames = {
            "P_PROP_foundation_wood_01","P_PROP_foundation_wood_03","P_PROP_board_01","P_PROP_board_02","P_PROP_cart_wheel_small"
        };
        static readonly string[] VillagerBodies = {
            "villager","villager-f","villager-child","villager-child-f","villager-elder","villager-elder-f"
        };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_CATALOG.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "CATALOG_DONE.txt");
        const string Report   = "Reports/catalog-build.txt";

        static CatalogBuild() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.5;
            try
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Trigger))
                {
                    File.Delete(Trigger);
                    Run();
                }
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas3/BUILD ASSET CATALOG")]
        public static void Run()
        {
            _exact = null;   // rebuilt per run: an asset imported since last time must be visible
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — CATALOG BUILD (D-137): runtime asset catalog for the player-runtime reconcilers");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            var cat = AssetDatabase.LoadAssetAtPath<EmergenceAssetCatalog>(OutPath);
            bool fresh = cat == null;
            if (fresh) cat = ScriptableObject.CreateInstance<EmergenceAssetCatalog>();
            cat.prefabs.Clear(); cat.controllers.Clear(); cat.mossPrefabs.Clear();

            int ok = 0, missing = 0;
            var wanted = new List<string>();

            // 1) codex: every prefab + ruinPrefab + the studio default ruin
            Codex codex = null;
            try { codex = JsonUtility.FromJson<Codex>(File.ReadAllText(CodexPath)); }
            catch (Exception e) { sb.AppendLine("codex parse FAILED: " + e.Message); }
            if (codex?.objects != null)
                foreach (var e in codex.objects)
                {
                    if (!string.IsNullOrWhiteSpace(e.prefab)) wanted.Add(e.prefab.Trim());
                    if (!string.IsNullOrWhiteSpace(e.ruinPrefab)) wanted.Add(e.ruinPrefab.Trim());
                    // D-244: AND EVERY OTHER NAME THE CODEX CAN PLACE. The reconciler asks
                    // cat.Prefab(VariantOf(...)) and, when that returns null, falls back to e.prefab --
                    // a fallback that is right (a missing model must never cost the object) and that hid
                    // this completely. Only the anchor was ever resolved here, so D-243's 295 variants
                    // and D-242's arrangement parts resolved to nothing at runtime: every crate in every
                    // village was the same crate again, and the coverage number said otherwise. A variant
                    // the catalog does not carry is not a variant. Missing ones now fail this pass by name.
                    if (e.variants != null)
                        foreach (var vn in e.variants)
                            if (!string.IsNullOrWhiteSpace(vn)) wanted.Add(vn.Trim());
                    if (e.arrangement != null)
                        foreach (var pt in e.arrangement)
                            if (pt != null && !string.IsNullOrWhiteSpace(pt.prefab)) wanted.Add(pt.prefab.Trim());
                }
            wanted.Add("P_PROP_wall_stone_small_02");     // DefaultRuinPrefab (D-112)

            // 2) huts: house variants 01..13 + yard props + fresh-build props
            // 14, not 13: the pack ships fourteen and the old bound left house_14 unpaid for.
            for (int v = 1; v <= 14; v++) wanted.Add($"P_BLD_house_{v:00}");
            wanted.AddRange(YardPropNames);
            wanted.AddRange(FreshBuildNames);

            // 3) agents: villager base bodies (bare-name keys) + the carry basket
            foreach (var b in VillagerBodies) wanted.Add(b);
            wanted.Add(Path.GetFileNameWithoutExtension(CarryPropPath));

            // 3b) fires (Fas 6 ink. 3, D-158): FireReconciler's fallback chains — OPTIONAL names
            // (the dresser tolerates absent variants; a missing chain member is not a defect, so
            // these resolve into the catalog when present but never count toward `missing`)
            // VÅG 1.1: the meadow's detail prototypes — grass clumps and wildflowers. The dresser found
            // them with an editor prefab query, so the living loop never had a meadow. Optional by the
            // same rule as the fire chain: an absent variant is not a defect.
            var optional = new[] { "VFX_Fire_01_Medium", "VFX_Fire_01_Big", "P_FX_fire", "PF_FX_fire", "fire",
                                   "msVFX_Stylized Smoke 1", "msVFX_Stylized Smoke 2",
                                   "Prefab_Grass_01_Detail", "Prefab_Grass_Group_01_Detail", "Prefab_Grass_03_Detail",
                                   "SM_Flower_01_Unity", "Prefab_Flower_02", "Prefab_Flower_04",
                                   // VÅG 1.1 (rest): the natural world — the dresser found these with an editor
                                   // prefix query, which is why the living loop had no trees, rocks or bushes.
                                   // Exact names, in the dresser's own preference order.
                                   "Prefab_TreeLarge_01", "Prefab_TreeLarge_02", "Prefab_TreeLarge_03", "Prefab_TreeLarge_04",
                                   "Prefab_Birch_01", "Prefab_Birch_02", "Prefab_Birch_04", "Prefab_Birch_05",
                                   "Prefab_RockFormation_01", "Prefab_RockFormation_02", "Prefab_RockFormation_03", "Prefab_RockFormation_04",
                                   "P_ENV_stone_01",
                                   "Prefab_Bush_01", "Prefab_Bush_02", "Prefab_Bush_03",
                                   // D-246 (EP order: the expensive packs first): the Village and City
                                   // packs ship their own ground plants and not one had ever been asked
                                   // for by name, so the meadow was built entirely of Dreamscape detail
                                   // props. A name the catalog does not carry is a name the runtime
                                   // cannot reach -- the same law the variants were losing to.
                                   "P_ENV_PLANT_grass_village", "P_ENV_grass_city_01", "P_ENV_PLANT_leaf_village",
                                   "P_ENV_flower_city_01", "P_ENV_flower_city_02", "P_ENV_flower_city_03",
                                   "P_PROP_treetrunk_01", "P_PROP_treetrunk_02", "P_PROP_treetrunk_03", "P_PROP_treetrunk_04",
                                   // VAG 1.5: the water. 4% of the map is lake and none of it was ever
                                   // rendered in the living loop — the dresser's water law was editor-only.
                                   "Prefab_WaterLake", "SM_WaterRiver" };
            int optOk = 0;
            foreach (var name in optional.Where(n => !wanted.Contains(n, StringComparer.OrdinalIgnoreCase)))
            {
                var pf = Resolve(name);
                cat.prefabs.Add(new EmergenceAssetCatalog.PrefabEntry { name = name, prefab = pf });
                if (pf != null) { ok++; optOk++; }
                else sb.AppendLine($"  optional (absent, tolerated): {name}");
            }
            sb.AppendLine($"fire/smoke chain (optional): {optOk}/{optional.Length} resolved");

            foreach (var name in wanted.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var pf = Resolve(name);
                cat.prefabs.Add(new EmergenceAssetCatalog.PrefabEntry { name = name, prefab = pf });
                if (pf != null) ok++;
                else { missing++; sb.AppendLine($"  MISSING: {name}"); }
            }

            // 4) age-mark moss — capture PlaceHutAge's exact query result IN ORDER (parity with WorldDresser)
            foreach (var pf in FindPrefabs("Prefab_Bush").Where(p => p != null && !p.name.Contains("Flower")).Take(3))
                cat.mossPrefabs.Add(pf);
            sb.AppendLine($"moss (Prefab_Bush query, order preserved): [{string.Join(", ", cat.mossPrefabs.Select(m => m.name))}]");

            // 5) villager animator controllers by band key
            foreach (var key in new[] { "adult", "adult-f", "child", "child-f", "elder", "elder-f" })
            {
                var c = key == "adult"
                    ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimDir + "/VillagerAnim.controller")
                    : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimDir}/Villager-{key}.overrideController");
                cat.controllers.Add(new EmergenceAssetCatalog.ControllerEntry { key = key, controller = c });
                if (c == null) { missing++; sb.AppendLine($"  MISSING controller: {key}"); }
            }

            // 6) the codex json itself, as a TextAsset reference (no Assets/-path file IO in the player)
            // VÅG 1.1 (D-209): terrain layers, resolved here so the RUNTIME terrain builder needs no
            // AssetDatabase. Names are exactly the dresser's own candidates, preference order intact.
            cat.terrainLayers.Clear();
            // D-249 (EP: "i paketen ligger även vägar, kullersten etc"): the project holds FIFTEEN
            // terrain layers and the catalog only ever asked for eleven. The two it never asked for
            // by name are the ones that matter most here -- Layer_walkway_city_01/02 are the City
            // pack's own ROAD surfaces, and a road painted with them is the pack's road rather than
            // our approximation of one. Same law as everywhere else tonight: a name the catalog does
            // not carry is a name the runtime cannot reach.
            var wantedLayers = new[] { "Layer_Grass", "Layer_grass_01", "Layer_grass_city", "Layer_farmfield", "Layer_Dirt",
                                       "Layer_Rock", "Layer_gravel_01", "Layer_gravel_02", "Layer_gravel_city",
                                       "Layer_Stone", "Layer_rock_01", "Layer_Cobblestone",
                                       "Layer_walkway_city_01", "Layer_walkway_city_02",
                                       "Layer_pavingstone_01", "Layer_pavingstone_02", "Layer_Sand" };
            int lok = 0;
            foreach (var ln in wantedLayers)
            {
                TerrainLayer tl = null;
                foreach (var g in AssetDatabase.FindAssets($"t:TerrainLayer {ln}"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(p) == ln) { tl = AssetDatabase.LoadAssetAtPath<TerrainLayer>(p); break; }
                }
                cat.terrainLayers.Add(new EmergenceAssetCatalog.TerrainLayerEntry { name = ln, layer = tl });
                if (tl != null) { lok++; if (tl.diffuseTexture == null) sb.AppendLine($"  WARNING: terrain layer {ln} has NO diffuse texture — it would render as flat checker"); }
            }
            sb.AppendLine($"terrain layers resolved: {lok}/{wantedLayers.Length}  [{string.Join(", ", cat.terrainLayers.Where(t => t.layer != null).Select(t => t.name))}]");

            // VÅG 1.2: skybox materials for the RUNTIME light rig (its only editor binding was Sky()).
            cat.skyboxes.Clear();
            var wantedSky = new[] { "Sky_Dusk", "M_ENV_SKYBOX_day", "Sky_Noon", "M_ENV_SKYBOX_night", "Sky_Night" };
            int sok = 0;
            foreach (var sn in wantedSky)
            {
                Material m = null;
                foreach (var g in AssetDatabase.FindAssets($"t:Material {sn}"))
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (Path.GetFileNameWithoutExtension(p) == sn) { m = AssetDatabase.LoadAssetAtPath<Material>(p); break; }
                }
                cat.skyboxes.Add(new EmergenceAssetCatalog.MaterialEntry { name = sn, material = m });
                if (m != null) sok++;
            }
            sb.AppendLine($"skyboxes resolved: {sok}/{wantedSky.Length}  [{string.Join(", ", cat.skyboxes.Where(x => x.material != null).Select(x => x.name))}]");

            cat.codexJson = AssetDatabase.LoadAssetAtPath<TextAsset>(CodexPath);
            if (cat.codexJson == null) { missing++; sb.AppendLine("  MISSING: object-codex.json as TextAsset"); }

            Directory.CreateDirectory(OutDir);
            if (fresh) AssetDatabase.CreateAsset(cat, OutPath);
            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            EmergenceAssetCatalog.Invalidate();

            sb.AppendLine();
            sb.AppendLine($"prefab names resolved: {ok}, missing: {missing}, controllers: {cat.controllers.Count(c => c.controller != null)}/6, moss: {cat.mossPrefabs.Count}/3");
            sb.AppendLine($"saved: {OutPath}");
            string verdict = missing == 0 ? "GREEN" : "CHECK (missing names listed above)";
            sb.AppendLine("verdict: " + verdict);
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} ok={ok} missing={missing}\nsee {Report}\n");
            Debug.Log("[CatalogBuild] " + verdict + $" ok={ok} missing={missing}");
        }

        // mirrors LiveReconciler.LoadCodexPrefab's rules (.glb dirs first, then exact-name prefab, then first hit)
        static GameObject Resolve(string name)
        {
            if (name.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>(TechDir + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + name);
            // villager base bodies are GLBs addressed by bare name
            var glb = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + name + ".glb");
            if (glb != null) return glb;
            // D-244, AND THE CRASH THAT PAID FOR IT. This used to run AssetDatabase.FindAssets
            // twice PER NAME. With the anchor alone that was ~120 searches and nobody noticed; the
            // moment the codex's variants and arrangement parts came in it became ~1400 searches in
            // one editor tick, and Unity died inside the asset-database iterator with the stack
            // ending in this method. The work was always O(project) per name for an answer that does
            // not change between names: ONE pass, indexed by exact base name, first hit wins in the
            // database's own order. Same rule as before, one search instead of fourteen hundred.
            if (_exact == null)
            {
                _exact = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var g in AssetDatabase.FindAssets("t:Prefab"))
                {
                    var pp = AssetDatabase.GUIDToAssetPath(g);
                    var bn = Path.GetFileNameWithoutExtension(pp);
                    if (!_exact.ContainsKey(bn)) _exact[bn] = pp;
                }
            }
            if (_exact.TryGetValue(name, out var hit)) return AssetDatabase.LoadAssetAtPath<GameObject>(hit);
            // no exact match: the old fuzzy fallback, kept so a name that resolved yesterday still
            // resolves today. It now fires only for names that are genuinely absent, so it is rare.
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        static Dictionary<string, string> _exact;   // base name -> asset path, built once per Run

        static IEnumerable<GameObject> FindPrefabs(string prefix)
            => AssetDatabase.FindAssets($"t:Prefab {prefix}")
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null && p.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
#endif
