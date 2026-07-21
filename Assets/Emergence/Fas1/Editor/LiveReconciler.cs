// EMERGENCE — Fas 1 (D-107 Fas 1 / D-101d): the LIVE RECONCILER core.
//
// Fas 0 laid the empty event bus + skeleton. Fas 1 makes the reconciler live: it READS a WorldState
// and reconciles the Codex OVERLAY (Layer 2) INCREMENTALLY against what is already placed —
// spawning objects the moment their `when` gate holds, and de-materialising them when it stops
// (onLoss). This is the mechanism that proves existence-condition C (worlds differentiate; knowledge
// is lost and rediscovered → objects appear, ruin, and return).
//
// It mirrors WorldDresser's placement EXACTLY (CodexQualifies / CodexPlacement / P / GroundW / Hash)
// so the incremental overlay is identical to the full-build overlay — but WorldDresser.cs is left
// untouched (those helpers are private), so the two co-exist. Every placement decision is
// hash-based (never sim-RNG), so the golden master stays GREEN (D-078 r4). Presentation only reads.
//
// Editor-driven v1, consistent with the existing dressing pipeline. Emits on PresentationEventBus so
// audio (Fas 6) and story (Fas 4) attach with zero reconciler changes.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    public sealed class LiveReconciler
    {
        public const float TileSize = 8f;                    // matches WorldDresser
        const string OverlayName = "CodexOverlay_Live";
        const string CodexPath   = "Assets/Emergence/Codex/object-codex.json";
        const string TechDir     = "Assets/Emergence/Models/tech/";
        const string NatureDir   = "Assets/Emergence/Models/nature/";
        const string CharDir     = "Assets/Emergence/Models/characters/";
        // D-112: studio default ruin stand-in (Village-pack broken wall-stone, same hand as the huts, render-verified
        // family — mason-stones uses _01). The OWNED "Ancient Ruins" pack swaps in via codex ruinPrefab once imported.
        const string DefaultRuinPrefab = "P_PROP_wall_stone_small_02";
        const float  DefaultRuinScale  = 1.0f;

        // stable-id -> placed instance. The stable id is what makes this a DIFF, not a rebuild.
        readonly Dictionary<string, GameObject> _placed = new Dictionary<string, GameObject>();
        // stable-id -> ruin left where a lost structure stood (persists until the tech is rediscovered).
        readonly Dictionary<string, GameObject> _ruins = new Dictionary<string, GameObject>();
        int _tick;

        public int PlacedCount => _placed.Count;
        public int RuinCount => _ruins.Count;
        public int Tick => _tick;

        public struct Delta { public int spawned, removed, kept, ruined; public override string ToString() => $"+{spawned} -{removed} ={kept}" + (ruined > 0 ? $" ruin+{ruined}" : ""); }

        /// <summary>Reconcile the codex overlay to this WorldState. Additive spawns + onLoss removals only.</summary>
        public Delta Reconcile(WorldState S)
        {
            _tick++;
            var d = new Delta();
            if (S?.villages == null) return d;

            Codex codex;
            try { codex = JsonUtility.FromJson<Codex>(File.ReadAllText(CodexPath)); }
            catch (Exception ex) { Debug.LogWarning("[Reconciler] codex parse failed: " + ex.Message); return d; }
            if (codex?.objects == null) return d;

            var overlay = Overlay();

            // 1) desired set from the current state (the `when` gate per village)
            var desired = new Dictionary<string, (CodexEntry e, WorldVillage v, int k, int cnt, int vi)>();
            for (int vi = 0; vi < S.villages.Length; vi++)
            {
                var v = S.villages[vi];
                foreach (var e in codex.objects)
                {
                    if (!CodexQualifies(v, e)) continue;
                    int cnt = Mathf.Max(1, e.count);
                    for (int k = 0; k < cnt; k++)
                        desired[$"{vi}:{e.id}:{k}"] = (e, v, k, cnt, vi);
                }
            }

            // id -> entry lookup for the onLoss decision (does this loss leave a ruin?)
            var byId = new Dictionary<string, CodexEntry>();
            foreach (var o in codex.objects) byId[o.id] = o;

            // 2) removals — placed but no longer desired (onLoss). A built structure whose knowledge is lost
            //    leaves a RUIN where it stood (D-112); ephemeral/portable objects simply vanish.
            foreach (var id in _placed.Keys.Where(id => !desired.ContainsKey(id)).ToList())
            {
                var go = _placed[id];
                _placed.Remove(id);
                var parts = id.Split(':');
                string objId = parts.Length > 1 ? parts[1] : id;
                int vi = ParseVi(parts);
                byId.TryGetValue(objId, out var entry);

                if (entry != null && entry.ruinOnLoss == 1 && go != null && !_ruins.ContainsKey(id))
                {
                    // swap the fallen structure for rubble at the exact footprint it occupied
                    var pos = go.transform.position; var rot = go.transform.rotation;
                    UnityEngine.Object.DestroyImmediate(go);
                    var ruinName = string.IsNullOrEmpty(entry.ruinPrefab) ? DefaultRuinPrefab : entry.ruinPrefab;
                    var rpf = LoadCodexPrefab(ruinName);
                    if (rpf != null)
                    {
                        var rgo = (GameObject)PrefabUtility.InstantiatePrefab(rpf, overlay);
                        rgo.transform.position = pos;
                        rgo.transform.rotation = rot;
                        rgo.transform.localScale = Vector3.one * (entry.ruinScale > 0f ? entry.ruinScale : DefaultRuinScale);
                        rgo.name = $"ruin_{objId}_{(parts.Length > 0 ? parts[0] : "")}";
                        StripImpostorLods(rgo);
                        _ruins[id] = rgo;
                        d.ruined++;
                    }
                    d.removed++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.AssetRemoved, objId, vi, "onLoss:toRuin"));
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.Milestone, objId, vi,
                        "the knowledge was lost — where it stood, only a ruin remains"));
                }
                else
                {
                    if (go != null) UnityEngine.Object.DestroyImmediate(go);
                    d.removed++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.AssetRemoved, objId, vi, "onLoss"));
                }
            }

            // 3) spawns — desired but not yet placed
            foreach (var kv in desired)
            {
                if (_placed.ContainsKey(kv.Key)) { d.kept++; continue; }
                var (e, v, k, cnt, vi) = kv.Value;
                // D-106 told-not-shown (spec §1): an entry may carry desc with NO prefab — the chronicle SPEAKS it
                // before the world SHOWS it. Record it (tracked, so it isn't re-narrated), emit the milestone, no spawn.
                if (string.IsNullOrWhiteSpace(e.prefab))
                {
                    _placed[kv.Key] = null;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.Milestone, e.id, vi, "(told-not-shown) " + e.desc));
                    d.spawned++;
                    continue;
                }
                // rediscovery: a ruin marks where this stood and the knowledge has returned → clear it, rebuild
                if (_ruins.TryGetValue(kv.Key, out var oldRuin))
                {
                    if (oldRuin != null) UnityEngine.Object.DestroyImmediate(oldRuin);
                    _ruins.Remove(kv.Key);
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.Milestone, e.id, vi,
                        "rediscovered — the ruin is raised again"));
                }
                var pf = LoadCodexPrefab(e.prefab);
                if (pf == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, overlay);
                var pos = CodexPlacement(v, e, k, cnt);
                go.transform.position = GroundW(P(S, pos.x, pos.y));
                go.transform.rotation = Quaternion.Euler(0f, Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length + k) % 360u, 0f);
                go.transform.localScale = Vector3.one * (e.scale <= 0f ? 1f : e.scale);
                go.name = $"codex_{e.id}_{v.name}_{k}";
                StripImpostorLods(go);
                _placed[kv.Key] = go;
                d.spawned++;

                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, S.season, PresentationEventType.AssetSpawned, e.id, vi, "placement=" + e.placement));
                // milestone → carries the chronicle text (Fas 4 consumes this)
                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, S.season, PresentationEventType.Milestone, e.id, vi, e.desc));
            }
            return d;
        }

        public void Clear()
        {
            foreach (var go in _placed.Values) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _placed.Clear();
            foreach (var go in _ruins.Values) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _ruins.Clear();
        }

        Transform Overlay()
        {
            var existing = GameObject.Find(OverlayName);
            if (existing != null) return existing.transform;
            return new GameObject(OverlayName).transform;
        }

        static int ParseVi(string[] parts) => (parts.Length > 0 && int.TryParse(parts[0], out var i)) ? i : -1;

        // ---- placement mirrors WorldDresser exactly (so overlay == full-build overlay) ----
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

        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);

        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }

        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }

        static GameObject LoadCodexPrefab(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;  // told-not-shown / empty → never match a random prefab
            if (name.EndsWith(".glb"))
                return AssetDatabase.LoadAssetAtPath<GameObject>(TechDir + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(NatureDir + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + name);
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            var guid = AssetDatabase.FindAssets($"t:Prefab {name}").FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
        }

        // strip the unlit billboard/impostor LOD (renders magenta/dark at distance) — matches the pack rule
        static void StripImpostorLods(GameObject go)
        {
            foreach (var bb in go.GetComponentsInChildren<BillboardRenderer>(true)) bb.enabled = false;
            var lg = go.GetComponentInChildren<LODGroup>(true);
            if (lg == null) return;
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
    }
}
#endif
