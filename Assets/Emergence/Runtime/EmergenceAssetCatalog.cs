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

        [Tooltip("Every prefab/GLB the runtime reconcilers may ask for, by bare name (no path/extension).")]
        public List<PrefabEntry> prefabs = new List<PrefabEntry>();
        [Tooltip("Villager animator controllers by band key: adult, adult-f, child, child-f, elder, elder-f.")]
        public List<ControllerEntry> controllers = new List<ControllerEntry>();
        [Tooltip("The object codex (object-codex.json) as a TextAsset — no file IO in the player.")]
        public TextAsset codexJson;
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
