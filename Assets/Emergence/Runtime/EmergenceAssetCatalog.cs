// EMERGENCE — FAS 3 increment 4 (D-137): the RUNTIME ASSET CATALOG.
//
// The player-runtime refactor's load path. In the editor, reconcilers used AssetDatabase.FindAssets
// — a pure editor API that does not exist in a player build. The catalog replaces it: an editor
// pass (CatalogBuild) resolves every prefab/controller name the reconcilers can ever ask for and
// stores DIRECT references in this ScriptableObject, saved under a Resources/ folder so
// Resources.Load reaches it in editor AND player. The references pull the assets into the build.
//
// Determinism note (D-078 r4): the catalog is a NAME -> ASSET map only. All placement decisions
// stay hash-based in the reconcilers; the catalog cannot influence them — same name in, same
// prefab out, in editor and player alike. The codex json rides along as a TextAsset so the player
// needs no Assets/-path file IO.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceAssetCatalog : ScriptableObject
    {
        public const string ResourcesName = "EmergenceAssetCatalog";   // Resources.Load key

        [Serializable] public struct PrefabEntry { public string name; public GameObject prefab; }
        [Serializable] public struct ControllerEntry { public string key; public RuntimeAnimatorController controller; }
        [Serializable] public struct TerrainLayerEntry { public string name; public TerrainLayer layer; }
        [Serializable] public struct MaterialEntry { public string name; public Material material; }

        [Tooltip("Every prefab/GLB the runtime reconcilers may ask for, by bare name (no path/extension).")]
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
        [Tooltip("Villager animator controllers by band key: adult, adult-f, child, child-f, elder, elder-f.")]
        public List<ControllerEntry> controllers = new List<ControllerEntry>();
        [Tooltip("The object codex (object-codex.json) as a TextAsset — no file IO in the player.")]
        public TextAsset codexJson;
        // VÅG 1.1 (2026-08-14, D-209): the terrain's own layers, so the LIVING loop can build ground
        // that looks like ground. The dresser found them with AssetDatabase.FindAssets — editor-only,
        // which is exactly why the player's world was a flat green plane: the whole dresser sits
        // behind #if UNITY_EDITOR and simply cannot run in a build. Same catalog school as the
        // prefabs (D-137): resolve once in the editor, load through Resources at runtime.
        [Tooltip("Skybox materials by name (Sky_Dusk, M_ENV_SKYBOX_day, Sky_Night...) — the light rig's only editor binding.")]
        public List<MaterialEntry> skyboxes = new List<MaterialEntry>();

        [Tooltip("Terrain layers by name (Layer_Grass, Layer_farmfield, Layer_Dirt, Layer_Rock, Layer_Cobblestone...).")]
        public List<TerrainLayerEntry> terrainLayers = new List<TerrainLayerEntry>();

        [Tooltip("Age-mark moss prefabs, captured in the exact order WorldDresser's editor query returned them (parity).")]
        public List<GameObject> mossPrefabs = new List<GameObject>();

        Dictionary<string, GameObject> _byName;
        Dictionary<string, RuntimeAnimatorController> _ctrl;

        static EmergenceAssetCatalog _loaded; static bool _loadTried;

        /// <summary>Resources-load the catalog (cached). Null (with one warning) if the build pass never ran.</summary>
        public static EmergenceAssetCatalog Load()
        {
            if (_loaded == null && !_loadTried)
            {
                _loadTried = true;
                _loaded = Resources.Load<EmergenceAssetCatalog>(ResourcesName);
                if (_loaded == null) Debug.LogWarning("[Catalog] EmergenceAssetCatalog missing — run Emergence/Fas3/BUILD ASSET CATALOG");
            }
            return _loaded;
        }

        /// <summary>Editor probes re-run in one session — let a rebuilt catalog be picked up.</summary>
        public static void Invalidate() { _loaded = null; _loadTried = false; }

        /// <summary>The first terrain layer matching any of these candidate names, or null. The dresser's
        /// own preference order (Dreamscape's real textured layer first) is preserved by the caller.</summary>
        public TerrainLayer TerrainLayer(string[] candidates)
        {
            if (candidates == null) return null;
            foreach (var nm in candidates)
                for (int i = 0; i < terrainLayers.Count; i++)
                    if (terrainLayers[i].layer != null &&
                        string.Equals(terrainLayers[i].name, nm, StringComparison.OrdinalIgnoreCase))
                        return terrainLayers[i].layer;
            return null;
        }

        /// <summary>A skybox material by exact name, or null.</summary>
        public Material Skybox(string name)
        {
            for (int i = 0; i < skyboxes.Count; i++)
                if (skyboxes[i].material != null && string.Equals(skyboxes[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return skyboxes[i].material;
            return null;
        }

        public GameObject Prefab(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (_byName == null)
            {
                _byName = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in prefabs) if (!string.IsNullOrEmpty(e.name) && !_byName.ContainsKey(e.name)) _byName[e.name] = e.prefab;
            }
            return _byName.TryGetValue(name, out var pf) ? pf : null;
        }

        public RuntimeAnimatorController Controller(string key)
        {
            if (_ctrl == null)
            {
                _ctrl = new Dictionary<string, RuntimeAnimatorController>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in controllers) if (!string.IsNullOrEmpty(e.key) && !_ctrl.ContainsKey(e.key)) _ctrl[e.key] = e.controller;
            }
            return _ctrl.TryGetValue(key, out var c) ? c : null;
        }

        public string CodexText => codexJson != null ? codexJson.text : null;
    }
}
