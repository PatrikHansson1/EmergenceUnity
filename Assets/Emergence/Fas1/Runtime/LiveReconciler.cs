// EMERGENCE — Fas 1 (D-107 Fas 1 / D-101d): the LIVE RECONCILER core.
//
// Fas 0 laid the empty event bus + skeleton. Fas 1 made the reconciler live: it READS a WorldState
// and reconciles the Codex OVERLAY (Layer 2) INCREMENTALLY against what is already placed —
// spawning objects the moment their `when` gate holds, and de-materialising them when it stops
// (onLoss). This is the mechanism that proves existence-condition C (worlds differentiate; knowledge
// is lost and rediscovered → objects appear, ruin, and return).
//
// FAS 3 increment 4 (D-137): PLAYER-RUNTIME REFACTOR. This class moved Editor/ -> Runtime/ and all
// AssetDatabase/PrefabUtility use is gone: prefabs come from EmergenceAssetCatalog (Resources), the
// codex json rides in the catalog as a TextAsset, instantiation is plain Object.Instantiate. The
// placement grammar (CodexQualifies / CodexPlacement / P / GroundW / Hash) is UNCHANGED — same
// salts, same decisions, in editor and player alike. Every placement decision is hash-based (never
// sim-RNG), so the golden master stays GREEN (D-078 r4). Presentation only reads.
//
// Emits on PresentationEventBus so audio (Fas 6) and story (Fas 4) attach with zero reconciler changes.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class LiveReconciler
    {
        public const float TileSize = 8f;                    // matches WorldDresser
        const string OverlayName = "CodexOverlay_Live";
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

            var cat = EmergenceAssetCatalog.Load();
            Codex codex = null;
            try { var txt = cat?.CodexText; if (txt != null) codex = JsonUtility.FromJson<Codex>(txt); }
            catch (Exception ex) { Debug.LogWarning("[Reconciler] codex parse failed: " + ex.Message); return d; }
            if (codex?.objects == null) { Debug.LogWarning("[Reconciler] no codex (catalog missing? run BUILD ASSET CATALOG)"); return d; }

            var overlay = Overlay();

            // 1) desired set from the current state (the `when` gate per village)
            var desired = new Dictionary<string, (CodexEntry e, WorldVillage v, int k, int cnt, int vi)>();
            // D-239. Two sets the removal pass cannot do without:
            //   standing — "<vi>:<id>" that still satisfies the gate. THE CAP GOVERNS WHAT MAY BE RAISED,
            //   NEVER WHAT MAY STAND: a village that dips from 12 souls to 11 must not pull down its
            //   newest milestone, leave rubble, and tell its own chronicle the knowledge was lost — then
            //   put it back when the eleventh child is born. That flicker is a population wobble, not history.
            //   absorbed — "<vi>:<id>" that became part of a greater whole. Absorption and loss are
            //   OPPOSITE events and only one of them leaves a ruin. Until this set existed, the first
            //   village to raise a smith's yard was told it had lost its forge, in the same tick.
            var standing = new HashSet<string>();
            var absorbed = new HashSet<string>();
            for (int vi = 0; vi < S.villages.Length; vi++)
            {
                var v = S.villages[vi];
                // C4+C5 (D-230): not every qualified object at once. A village raises what its
                // hands can raise, oldest first — see CodexBuildOrder for why knowledge is the
                // wrong pacer and hands are the right one.
                // D-239: the prefab test is passed IN, so a combined whole whose look does not
                // resolve absorbs nothing — spec §5b.1 made real instead of quoted.
                System.Func<string, bool> resolves = nm => !string.IsNullOrWhiteSpace(nm) && cat != null && cat.Prefab(nm) != null;
                HashSet<string> absorbedHere;
                var allowed = CodexBuildOrder.Allowed(v, codex.objects, resolves, out absorbedHere);
                foreach (var id in absorbedHere) absorbed.Add(vi + ":" + id);
                foreach (var id in CodexBuildOrder.Standing(v, codex.objects, resolves)) standing.Add(vi + ":" + id);
                foreach (var e in allowed)
                {
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
                {
                    var pp = id.Split(':');
                    string key = pp.Length > 1 ? pp[0] + ":" + pp[1] : id;
                    if (standing.Contains(key)) { d.kept++; continue; }        // still true, merely over the ceiling
                }
                var go = _placed[id];
                _placed.Remove(id);
                var parts = id.Split(':');
                string objId = parts.Length > 1 ? parts[1] : id;
                int vi = ParseVi(parts);
                byId.TryGetValue(objId, out var entry);

                bool wasAbsorbed = absorbed.Contains(vi + ":" + objId);
                if (wasAbsorbed)
                {
                    // it did not fall. It became part of something larger, and the larger thing is
                    // standing where it stood. No rubble, and no line in the chronicle about loss.
                    if (go != null) Retire(go);
                    d.removed++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetRemoved, objId, vi, "absorbed into a whole"));
                    continue;
                }
                if (entry != null && entry.ruinOnLoss == 1 && go != null && !_ruins.ContainsKey(id))
                {
                    // swap the fallen structure for rubble at the exact footprint it occupied
                    var pos = go.transform.position; var rot = go.transform.rotation;
                    Retire(go);
                    var ruinName = string.IsNullOrEmpty(entry.ruinPrefab) ? DefaultRuinPrefab : entry.ruinPrefab;
                    var rpf = cat != null ? cat.Prefab(ruinName) : null;
                    if (rpf != null)
                    {
                        var rgo = UnityEngine.Object.Instantiate(rpf, overlay);
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
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetRemoved, objId, vi, "onLoss:toRuin"));
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.Milestone, objId, vi,
                        "the knowledge was lost — where it stood, only a ruin remains"));
                }
                else
                {
                    if (go != null) Retire(go);
                    d.removed++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetRemoved, objId, vi, "onLoss"));
                }
            }

            // 3) spawns — desired but not yet placed
            foreach (var kv in desired)
            {
                if (_placed.ContainsKey(kv.Key))
                {
                    d.kept++;
                    Settle(_placed[kv.Key], kv.Value.v, kv.Value.e, kv.Value.k);   // C6 (D-236): a thing that has stood keeps standing DIFFERENTLY
                    continue;
                }
                var (e, v, k, cnt, vi) = kv.Value;
                // D-106 told-not-shown (spec §1): an entry may carry desc with NO prefab — the chronicle SPEAKS it
                // before the world SHOWS it. Record it (tracked, so it isn't re-narrated), emit the milestone, no spawn.
                if (string.IsNullOrWhiteSpace(e.prefab))
                {
                    _placed[kv.Key] = null;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.Milestone, e.id, vi, "(told-not-shown) " + e.desc));
                    d.spawned++;
                    continue;
                }
                // rediscovery: a ruin marks where this stood and the knowledge has returned → clear it, rebuild
                if (_ruins.TryGetValue(kv.Key, out var oldRuin))
                {
                    if (oldRuin != null) Retire(oldRuin);
                    _ruins.Remove(kv.Key);
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.Milestone, e.id, vi,
                        "rediscovered — the ruin is raised again"));
                }
                var pf = cat != null ? cat.Prefab(e.prefab) : null;
                if (pf == null) continue;
                var go = UnityEngine.Object.Instantiate(pf, overlay);
                var pos = CodexPlacement(v, e, k, cnt);
                go.transform.position = GroundW(P(S, pos.x, pos.y));
                go.transform.rotation = Quaternion.Euler(0f, Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length + k) % 360u, 0f);
                go.transform.localScale = Vector3.one * (e.scale <= 0f ? 1f : e.scale);
                go.name = $"codex_{e.id}_{v.name}_{k}";
                Settle(go, v, e, k);
                StripImpostorLods(go);
                _placed[kv.Key] = go;
                d.spawned++;

                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, WorldEras.Name(S), PresentationEventType.AssetSpawned, e.id, vi, "placement=" + e.placement));
                // milestone → carries the chronicle text (Fas 4 consumes this)
                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, WorldEras.Name(S), PresentationEventType.Milestone, e.id, vi, e.desc));
            }
            return d;
        }

        public void Clear()
        {
            foreach (var go in _placed.Values) if (go != null) Retire(go);
            _placed.Clear();
            foreach (var go in _ruins.Values) if (go != null) Retire(go);
            _ruins.Clear();
        }

        /// <summary>Play mode defers (Destroy); edit mode is immediate — same visible result, both runtime-legal.</summary>
        internal static void Retire(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        Transform Overlay()
        {
            var existing = GameObject.Find(OverlayName);
            if (existing != null) return existing.transform;
            return new GameObject(OverlayName).transform;
        }

        static int ParseVi(string[] parts) => (parts.Length > 0 && int.TryParse(parts[0], out var i)) ? i : -1;

        // ---- placement mirrors WorldDresser (so overlay == full-build overlay) ----
        // The GATE and the BUILD ORDER are literally one implementation now (CodexBuildOrder, D-230), and
        // the settle law is applied by both (D-239). "Exactly" was an overclaim the moment either side
        // grew something the other lacked; it is a claim worth re-earning at every change, not asserting.
        // the gate itself moved to CodexBuildOrder.Qualifies — it was two copies of one law,
        // here and in WorldDresser, and a law with two copies is a law with two futures.
        static bool CodexQualifies(WorldVillage v, CodexEntry e) => CodexBuildOrder.Qualifies(v, e);

        // C6 / DEEP TIME (D-236). Over a long run the object VOCABULARY stops growing — measured: kinds
        // climb 13 -> 38 across 180 years and then hold. If nothing else changes, the late world stops
        // moving. But a place is not a snapshot, it is a stack of decisions: a thing that has stood for
        // four generations does not stand the way a thing raised last spring stands. It has settled into
        // the ground, and it leans, because the ground under it is not flat and never was.
        //
        // The measure is GENERATIONS the village has held, not a clock — the codex's own law is that
        // nothing unlocks on the calendar (spec §4a), and settling is the same kind of fact. Applied
        // every reconcile, so an object that was raised young visibly ages in place across a run. Sink
        // and lean are hash-derived per object so two things beside each other age differently, and both
        // are hard-capped: this is a world that has been lived in, not a world that is falling over.
        const float SettleSinkPerGen = 0.018f;   // metres of ground taken per generation held
        const float SettleLeanPerGen = 0.30f;    // degrees of lean per generation held
        const int   SettleMaxGen     = 8;        // past this a place reads as old; more would read as broken
        static void Settle(GameObject go, WorldVillage v, CodexEntry e, int k)
        {
            if (go == null || v == null || e == null) return;
            int gens = Mathf.Clamp(v.maxGen, 0, SettleMaxGen);
            if (gens <= 0) return;
            // D-239: the re-derivation is the ONLY thing that stops a sink accumulating onto a sink,
            // and GroundW returns its input unchanged when there is no active terrain. With no terrain
            // there is no ground truth to re-derive from, so the honest move is to settle nothing at all
            // rather than subtract another 18 mm every reconcile, forever.
            if (Terrain.activeTerrain == null) return;
            uint h = Hash(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), e.id.Length * 13 + k);
            float bias = ((h & 0xFFu) / 255f) * 0.6f + 0.4f;             // 0.4..1.0 — each thing ages at its own rate
            float sink = gens * SettleSinkPerGen * bias;
            float lean = gens * SettleLeanPerGen * bias;
            float dir  = ((h >> 8) & 0xFFu) / 255f * 360f;
            var baseY  = GroundW(go.transform.position).y;               // re-derive: never accumulate a sink onto a sink
            var p = go.transform.position; p.y = baseY - sink; go.transform.position = p;
            // the yaw is RE-DERIVED from the same hash the placement used, never read back off the
            // transform: reading a tilted rotation's euler yaw gives a slightly different number every
            // time, and applied each reconcile that drift would slowly spin the whole village.
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

        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);

        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }

        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }

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
