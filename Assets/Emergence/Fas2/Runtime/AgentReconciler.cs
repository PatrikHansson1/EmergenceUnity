// EMERGENCE — Fas 2 steg 2 (D-124): the LIVE AGENT RECONCILER — the population becomes alive.
//
// Fas 1's LiveReconciler made the codex overlay live (objects). This does the same for PEOPLE:
// it READS S.agents and reconciles a "Agents_Live" layer incrementally — a new soul is BORN
// (spawned), a missing soul has DIED (retired), a child GROWS UP (band swap rebuilds the body),
// a changed `task` re-reads the animation state live (AgentAnimator.SetTask -> crossfade in play
// mode). Positions are teleported to the sim's truth per snapshot (smooth in-between motion is the
// v2 `pathUse` work, per D-116). Mirrors PlaceAgents' choices (band/model/scale/facing).
//
// FAS 3 increment 4 (D-137): PLAYER-RUNTIME REFACTOR. Moved Editor/ -> Runtime/: bodies and animator
// controllers come from EmergenceAssetCatalog (Resources), instantiation is Object.Instantiate.
// The edit-mode still-pose path (clip sampling) is the ONLY editor-gated remnant (#if UNITY_EDITOR) —
// it exists for edit-phase probes and never runs in a player.
//
// Determinism (D-078 r4): reads state only; sex/phase/rotation are hash(agentId) — soul-stable,
// never position-dependent, never sim-RNG. Events go on PresentationEventBus (AgentActivity is the
// channel reserved for exactly this since Fas 0).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class AgentReconciler
    {
        public const string LayerName = "Agents_Live";
        const float TileSize = 8f;   // matches WorldDresser/LiveReconciler
        // VÅG 1.2 (2026-08-14, D-215): MEASURED, not assumed. The code said "GLBs authored ~1.7m" and
        // left the scale at 1 — but measured at REST, with no animation inflating the bounds, the
        // adult villager GLB is 2.29 m. Every villager in the world has been a 2.3 m giant, and since
        // the whole world was judged against them, trees, bushes and rocks all read "too big" when in
        // fact the yardstick was too long. 1.75 / 2.29 = 0.76. Fix the ruler first, then the world.
        //   villager 2.29 m -> 0.76 | villager-f 2.22 -> 0.79 | villager-elder 2.23 -> 0.78
        // The adult sets the number: one scale for all bands keeps their relative proportions intact
        // (the child GLB measures TALLER at rest, which is a reaching pose inflating its bounds, not a
        // tall child — per-band scaling from rest bounds would encode that error into the world).
        const float Scale = 0.76f;   // -> a villager stands 1.75 m

        sealed class Rec { public GameObject go; public string band; public bool female; public string task; public string sayAct; public string verb; }
        readonly Dictionary<int, Rec> _agents = new();
        int _tick;

        public int Count => _agents.Count;

        public struct Delta
        {
            public int born, died, aged, retasked, moved, kept;
            public override string ToString() => $"+{born} -{died} aged={aged} retask={retasked} moved={moved} ={kept}";
        }

        /// <summary>E1.5: the six drama sayActs (Engine 2.4.1) — the ones the chronicle hears about.
        /// steal/raid/feud carry the violence arc, mourn/gift the human answer, submit the hierarchy.</summary>
        public static bool DramaAct(string act) => act == "steal" || act == "raid" || act == "feud"
            || act == "mourn" || act == "submit" || act == "gift";

        /// <summary>Soul-stable presentation sex — a property of the id, never of where the agent stands.</summary>
        public static bool Female(int id) => (Hash(id, 0, id * 31 + 7) & 1u) == 0u;
        public static string Band(float age) => age < 14 ? "child" : age > 55 ? "elder" : "adult";

        /// <summary>Rebuild the id->instance map from the scene (after the enter-playmode domain reload).</summary>
        public void Rehydrate()
        {
            _agents.Clear();
            var layer = GameObject.Find(LayerName);
            if (layer == null) return;
            foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                _agents[aa.agentId] = new Rec { go = aa.gameObject, band = aa.band, female = aa.female, task = aa.task, sayAct = aa.sayAct, verb = aa.verb };
        }

        public Delta Reconcile(WorldState S, bool editPose)
        {
            _tick++;
            var d = new Delta();
            if (S?.agents == null) return d;
            var layer = Layer();
            var desired = new Dictionary<int, WorldAgent>();
            foreach (var a in S.agents) desired[a.id] = a;

            // 1) deaths/departures — placed but no longer in the state
            foreach (var id in _agents.Keys.Where(id => !desired.ContainsKey(id)).ToList())
            {
                if (_agents[id].go != null) LiveReconciler.Retire(_agents[id].go);
                _agents.Remove(id);
                d.died++;
                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + id, -1, "a soul departs"));
            }

            // 2) births + updates
            foreach (var a in S.agents)
            {
                string band = Band(a.age);
                bool female = Female(a.id);
                if (_agents.TryGetValue(a.id, out var rec) && rec.go != null)
                {
                    if (rec.band != band)
                    {
                        // the body changes (child grows up / an adult greys) — rebuild at the same spot
                        LiveReconciler.Retire(rec.go);
                        rec.go = Spawn(S, a, band, female, layer, editPose);
                        rec.band = band;
                        d.aged++;
                        PresentationEventBus.Publish(new PresentationEvent(
                            _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + a.id, -1,
                            band == "adult" ? "comes of age" : "grows old"));
                    }
                    else
                    {
                        var pos = GroundW(P(S, a.x, a.y));
                        if ((rec.go.transform.position - pos).sqrMagnitude > 0.0001f)
                        {
                            // v2 (D-129): in play mode the soul WALKS there (heading-facing); edit mode stays instant
                            var aaMove = rec.go.GetComponent<AgentAnimator>();
                            if (Application.isPlaying && aaMove != null) aaMove.GlideTo(pos);
                            else { rec.go.transform.position = pos; Face(S, a, rec.go); }
                            d.moved++;
                        }
                        var aaLive = rec.go.GetComponent<AgentAnimator>();
                        // R2 ink. 1: verb rides with task (engine derives verb FROM task, so a verb
                        // change implies a task change in live exports; fixtures may mutate either).
                        if (rec.task != a.task || rec.verb != (a.verb ?? ""))
                        {
                            rec.task = a.task; rec.verb = a.verb ?? "";
                            if (aaLive != null)
                            {
                                if (Application.isPlaying) aaLive.SetTask(a.task, a.verb ?? "");   // live crossfade (verb selects the state)
                                else { aaLive.task = a.task; aaLive.verb = a.verb ?? ""; if (editPose) { StillPose(rec.go, a, band, female); LiftOntoGround(rec.go); } }
                                EnsureCarryProp(rec.go, a.task);                 // "bär" (D-131): basket in hand on carry/haul tasks
                            }
                            d.retasked++;
                            PresentationEventBus.Publish(new PresentationEvent(
                                _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + a.id, -1, "task: " + a.task));
                        }
                        else d.kept++;
                    }
                    // A2-polish (D-159): mood was TASK-CHANGE-GATED — a soul falling in love mid-task
                    // never got its tempo. Refresh on sayAct change, independent of task; attend-gaze
                    // recomputed every applied state (pure function of S). OUTSIDE the band-branch on
                    // purpose (probe finding: the aged-rebuilt body lost gaze/mood until next task change).
                    var aaExpr = rec.go.GetComponent<AgentAnimator>();
                    if (aaExpr != null)
                    {
                        string act = a.sayAct ?? "";
                        if (rec.sayAct != act)
                        {
                            rec.sayAct = act; aaExpr.SetMood(act);
                            // E1.5: the SIX drama acts are the chronicle's business too — a minimal
                            // ADDITIVE publication when the applied state shows a soul entering one
                            // (pure state diff, no RNG; routine acts stay off the bus as before).
                            if (DramaAct(act))
                                PresentationEventBus.Publish(new PresentationEvent(
                                    _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + a.id, -1, "sayAct: " + act));
                        }
                        if (Application.isPlaying) Attend(S, a, aaExpr);
                    }
                }
                else
                {
                    var go = Spawn(S, a, band, female, layer, editPose);
                    _agents[a.id] = new Rec { go = go, band = band, female = female, task = a.task, sayAct = a.sayAct ?? "", verb = a.verb ?? "" };
                    if (Application.isPlaying)
                    {
                        var aaNew = go.GetComponent<AgentAnimator>();
                        if (aaNew != null) Attend(S, a, aaNew);                  // A2-polish (D-159)
                    }
                    d.born++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + a.id, -1,
                        band == "child" ? "a child is born" : "a soul arrives"));
                    // E1.5: a soul can ENTER the witnessed world already inside a drama act
                    // (checkpoint re-entry, first sight after a jump) — publish it like any change.
                    if (DramaAct(a.sayAct ?? ""))
                        PresentationEventBus.Publish(new PresentationEvent(
                            _tick, S.years, WorldEras.Name(S), PresentationEventType.AgentActivity, "agent-" + a.id, -1, "sayAct: " + a.sayAct));
                }
            }
            return d;
        }

        public void Clear()
        {
            foreach (var r in _agents.Values) if (r.go != null) LiveReconciler.Retire(r.go);
            _agents.Clear();
        }

        // ---- spawn mirrors PlaceAgents (base-body GLB; the LIVE animator owns walk/work now) ----
        GameObject Spawn(WorldState S, WorldAgent a, string band, bool female, Transform layer, bool editPose)
        {
            string baseNm = band == "child" ? (female ? "villager-child-f" : "villager-child")
                          : band == "elder" ? (female ? "villager-elder-f" : "villager-elder")
                          : (female ? "villager-f" : "villager");
            var cat = EmergenceAssetCatalog.Load();
            var pf = cat != null ? cat.Prefab(baseNm) : null;
            if (pf == null) return null;
            var go = UnityEngine.Object.Instantiate(pf, layer);
            go.transform.position = GroundW(P(S, a.x, a.y));
            go.transform.localScale = Vector3.one * Scale;
            // D-215: the GLB ships emissiveFactor 1,1,1 and specularColorFactor 2,2,2 — a lamp in a
            // tunic. Corrected here, and dyed by hash so a village is not fifty identical people.
            GlbMaterialSanity.Apply(go, GlbMaterialSanity.TintFor(a.id));
            Face(S, a, go);
            go.name = $"agent_{a.id}_{a.name}";

            var rac = cat.Controller(band == "adult" ? (female ? "adult-f" : "adult") : band + (female ? "-f" : ""));
            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (rac != null) anim.runtimeAnimatorController = rac;
            var aa = go.AddComponent<AgentAnimator>();
            aa.agentId = a.id; aa.task = a.task; aa.canWork = band == "adult"; aa.band = band; aa.female = female;
            aa.verb = a.verb ?? "";                                          // R2 ink. 1: verb selects the state when present
            aa.sayAct = a.sayAct ?? "";                                      // A2-interim (D-131)
            EnsureCarryProp(go, a.task);                                     // "bär" (D-131)

            // D-237, THE ONE THAT EXPLAINS WHY NOTHING MOVED. Both the foot offset AND the probe that
            // reports the sink read `Renderer.bounds` — and for a SKINNED mesh that box does not follow
            // the pose. Unity derives it from the authored localBounds, so it is whatever the exporter
            // baked, usually the rest pose with padding. Two souls therefore reported 0,12 and 0,14 m
            // under the ground, to the centimetre, across four different grounding fixes: the number
            // could not move, because it was never measuring a body. It was measuring a box.
            // updateWhenOffscreen makes the bounds track the actual skinned pose. It costs a bounds
            // recompute per skinned renderer per frame — which is the price of a measurement that is
            // true, and the probe's own frame-time reading is where we find out if it is too dear.
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
                smr.updateWhenOffscreen = true;

            if (!Application.isPlaying && editPose) { StillPose(go, a, band, female); LiftOntoGround(go); }
            return go;
        }

        // D-131 ("bär"): a soul on a carry/haul task holds a basket — read-right without a carry clip
        // (the true carry cycle is Väg-1/clip-purchase work; the prop makes the VERB legible now).
        // Basket = era-neutral (weaving is stone-age canon). Attached under the spine so walk sway carries it.
        const string CarryPropName = "CarryProp_D131";
        static bool CarryTask(string task) => task != null && (task.Contains("carr") || task.Contains("haul"));
        public static void EnsureCarryProp(GameObject go, string task)
        {
            var existing = FindDeep(go.transform, CarryPropName);
            if (!CarryTask(task)) { if (existing != null) LiveReconciler.Retire(existing.gameObject); return; }
            if (existing != null) return;
            var cat = EmergenceAssetCatalog.Load();
            var pf = cat != null ? cat.Prefab("COMP_PROP_basket_city_01") : null;
            if (pf == null) return;
            Transform mount = null;
            foreach (var t in go.GetComponentsInChildren<Transform>())
                if (t.name.Contains("Spine")) mount = t;                     // deepest spine joint wins
            if (mount == null) mount = go.transform;
            var prop = UnityEngine.Object.Instantiate(pf);
            prop.name = CarryPropName;
            prop.transform.SetParent(mount, false);
            prop.transform.localPosition = new Vector3(0f, 0.05f, 0.28f);    // in front of the torso
            prop.transform.localRotation = Quaternion.identity;
            prop.transform.localScale = Vector3.one * 0.55f;
        }
        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        // edit-mode still: sample the task-right clip at a hash phase (parity with the still layer's read).
        // GROUND THE POSE, NOT THE PIVOT (small fix, 2026-08-14). A soul is placed at terrain height and
        // only THEN sampled into a hash-varied frame of its clip -- and a pose moves the feet. A working
        // crouch, or a walk cycle caught on its low foot, puts the sole below the origin, so the person
        // stood ankle-deep in the ground (0,12-0,14 m, caught by RUN_GROUNDCAP twice). Which souls it
        // hits changes with every world because the frame is hash-picked, which is exactly why it read
        // as intermittent rather than as a bug. Measured AFTER the pose, and it only ever LIFTS: a soul
        // may stand on the ground, never sink into it. Still-pose path only -- in play mode the Animator
        // owns the transform and a per-frame lift would fight it.
        static void LiftOntoGround(GameObject go)
        {
            if (go == null) return;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) return;
            var bb = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bb.Encapsulate(rends[i].bounds);
            var t = Terrain.activeTerrain;
            if (t == null) return;
            float ground = t.SampleHeight(go.transform.position) + t.transform.position.y;
            float under = ground - bb.min.y;
            if (under > 0.005f) go.transform.position += Vector3.up * under;
        }

        // Editor-only remnant (clip extraction from GLB sub-assets needs AssetDatabase) — probes only, never player.
        static void StillPose(GameObject go, WorldAgent a, string band, bool female)
        {
#if UNITY_EDITOR
            // R2 ink. 1: stills go through the SAME one state law as play mode (verb selects when present)
            string st = AgentTaskRead.StateFor(a.verb, a.task, band == "adult");
            string suffix = st == "Work" ? "-work" : st == "Walk" ? "-walk" : "";
            string baseNm = band == "child" ? (female ? "villager-child-f" : "villager-child")
                          : band == "elder" ? (female ? "villager-elder-f" : "villager-elder")
                          : (female ? "villager-f" : "villager");
            const string charDir = "Assets/Emergence/Models/characters/";
            var clip = LoadClip(charDir + baseNm + suffix + ".glb") ?? LoadClip(charDir + baseNm + ".glb");
            if (clip != null && clip.length > 0f)
                clip.SampleAnimation(go, (Hash(a.id, 1, a.id + 11) & 0xffffu) / 65536f * clip.length);
#endif
        }

#if UNITY_EDITOR
        static AnimationClip LoadClip(string path)
        {
            foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }
#endif

        // ---- A2-polish (D-159): what does this soul attend to? PURE function of applied state ----
        // Social verbs (teach/love/small) face the nearest OTHER soul; cold faces the nearest fire.
        // Sim-space radii (the engine's smalltalk range is close); the animator only yaw-slerps toward
        // the mapped point (presentation-only, D-078 r4 — reads state, never writes back, no RNG).
        // Replace-path (G-review r1, anm. 4): these are DIRECTOR-CHOSEN presentation constants without
        // engine backing — when the R2 export carries social reach, Attend reads the export instead.
        public const float SocialRadius = 6f, FireRadius = 12f;   // sim units
        // E1.5: gift/submit/mourn join the SOCIAL classification — a gift is handed TO someone, a
        // submission is made BEFORE someone, grief seeks the living. steal/raid/feud stay UNSOCIAL
        // by design (the thief hides, the raider closes distance via the sim's own position — the
        // body does not telegraph a target the export doesn't carry).
        public static bool SocialAct(string act) => act == "teach" || act == "love" || act == "small"
            || act == "gift" || act == "submit" || act == "mourn";
        static void Attend(WorldState S, WorldAgent a, AgentAnimator aa)
        {
            string act = a.sayAct ?? "";
            if (SocialAct(act) && S.agents != null)
            {
                WorldAgent best = null; float bd = SocialRadius * SocialRadius;
                foreach (var b in S.agents)
                {
                    if (b.id == a.id) continue;
                    float dd = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
                    if (dd < bd) { bd = dd; best = b; }
                }
                if (best != null) { aa.SetAttend(GroundW(P(S, best.x, best.y))); return; }
            }
            // cold seeks the fire (D-159); E1.5: a mourner with NO soul in social reach turns to the
            // nearest hearth too — grief without company faces the fire (same mechanism, same radius).
            if ((act == "cold" || act == "mourn") && S.fires != null)
            {
                WorldFire best = null; float bd = FireRadius * FireRadius;
                foreach (var f in S.fires)
                {
                    float dd = (f.x - a.x) * (f.x - a.x) + (f.y - a.y) * (f.y - a.y);
                    if (dd < bd) { bd = dd; best = f; }
                }
                if (best != null) { aa.SetAttend(GroundW(P(S, best.x, best.y))); return; }
            }
            aa.ClearAttend();
        }

        static void Face(WorldState S, WorldAgent a, GameObject go)
        {
            WorldHut nh = null; float nb = float.MaxValue;
            if (S.huts != null)
                foreach (var h in S.huts) { float dd = (h.x - a.x) * (h.x - a.x) + (h.y - a.y) * (h.y - a.y); if (dd < nb) { nb = dd; nh = h; } }
            if (nh != null && nb > 0.01f) { var t = GroundW(P(S, nh.x, nh.y)); t.y = go.transform.position.y; go.transform.LookAt(t); }
            else go.transform.rotation = Quaternion.Euler(0f, Hash(a.id, 2, a.id + 7) % 360u, 0f);
        }

        static Transform Layer()
        {
            var existing = GameObject.Find(LayerName);
            return existing != null ? existing.transform : new GameObject(LayerName).transform;
        }

        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);

        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }

        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }
    }
}
