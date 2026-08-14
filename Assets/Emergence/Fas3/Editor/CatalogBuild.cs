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
                }
            wanted.Add("P_PROP_wall_stone_small_02");     // DefaultRuinPrefab (D-112)

            // 2) huts: house variants 01..13 + yard props + fresh-build props
            for (int v = 1; v <= 13; v++) wanted.Add($"P_BLD_house_{v:00}");
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
                                   "SM_Flower_01_Unity", "Prefab_Flower_02", "Prefab_Flower_04" };
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
            var wantedLayers = new[] { "Layer_Grass", "Layer_grass_01", "Layer_farmfield", "Layer_Dirt",
                                       "Layer_Rock", "Layer_gravel_01", "Layer_Cobblestone", "Layer_pavingstone_01" };
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
                if (tl != null) lok++;
            }
            sb.AppendLine($"terrain layers resolved: {lok}/{wantedLayers.Length}  [{string.Join(", ", cat.terrainLayers.Where(t => t.layer != null).Select(t => t.name))}]");

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
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        static IEnumerable<GameObject> FindPrefabs(string prefix)
            => AssetDatabase.FindAssets($"t:Prefab {prefix}")
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null && p.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
#endif
