// EMERGENCE — Fas 2 steg 2 (D-124): the LIVE AGENT RECONCILER — the population becomes alive.
//
// Fas 1's LiveReconciler made the codex overlay live (objects). This does the same for PEOPLE:
// it READS S.agents and reconciles a "Agents_Live" layer incrementally — a new soul is BORN
// (spawned), a missing soul has DIED (retired), a child GROWS UP (band swap rebuilds the body),
// a changed `task` re-reads the animation state live (AgentAnimator.SetTask -> crossfade in play
// mode). Positions are teleported to the sim's truth per snapshot (smooth in-between motion is the
// v2 `pathUse` work, per D-116). Mirrors PlaceAgents' choices (band/model/scale/facing) the same
// way LiveReconciler mirrors WorldDresser — the still layer stays untouched.
//
// Determinism (D-078 r4): reads state only; sex/phase/rotation are hash(agentId) — soul-stable,
// never position-dependent, never sim-RNG. Events go on PresentationEventBus (AgentActivity is the
// channel reserved for exactly this since Fas 0).
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    public sealed class AgentReconciler
    {
        public const string LayerName = "Agents_Live";
        const string CharDir = "Assets/Emergence/Models/characters/";
        const float TileSize = 8f;   // matches WorldDresser/LiveReconciler
        const float Scale = 1f;      // matches WorldDresser.VillagerScale

        sealed class Rec { public GameObject go; public string band; public bool female; public string task; }
        readonly Dictionary<int, Rec> _agents = new();
        int _tick;

        public int Count => _agents.Count;

        public struct Delta
        {
            public int born, died, aged, retasked, moved, kept;
            public override string ToString() => $"+{born} -{died} aged={aged} retask={retasked} moved={moved} ={kept}";
        }

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
                _agents[aa.agentId] = new Rec { go = aa.gameObject, band = aa.band, female = aa.female, task = aa.task };
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
                if (_agents[id].go != null) UnityEngine.Object.DestroyImmediate(_agents[id].go);
                _agents.Remove(id);
                d.died++;
                PresentationEventBus.Publish(new PresentationEvent(
                    _tick, S.years, S.season, PresentationEventType.AgentActivity, "agent-" + id, -1, "a soul departs"));
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
                        UnityEngine.Object.DestroyImmediate(rec.go);
                        rec.go = Spawn(S, a, band, female, layer, editPose);
                        rec.band = band;
                        d.aged++;
                        PresentationEventBus.Publish(new PresentationEvent(
                            _tick, S.years, S.season, PresentationEventType.AgentActivity, "agent-" + a.id, -1,
                            band == "adult" ? "comes of age" : "grows old"));
                    }
                    else
                    {
                        var pos = GroundW(P(S, a.x, a.y));
                        if ((rec.go.transform.position - pos).sqrMagnitude > 0.0001f)
                        {
                            rec.go.transform.position = pos;
                            Face(S, a, rec.go);
                            d.moved++;
                        }
                        if (rec.task != a.task)
                        {
                            rec.task = a.task;
                            var aa = rec.go.GetComponent<AgentAnimator>();
                            if (aa != null)
                            {
                                if (Application.isPlaying) aa.SetTask(a.task);   // live crossfade
                                else { aa.task = a.task; if (editPose) StillPose(rec.go, a, band, female); }
                            }
                            d.retasked++;
                            PresentationEventBus.Publish(new PresentationEvent(
                                _tick, S.years, S.season, PresentationEventType.AgentActivity, "agent-" + a.id, -1, "task: " + a.task));
                        }
                        else d.kept++;
                    }
                }
                else
                {
                    var go = Spawn(S, a, band, female, layer, editPose);
                    _agents[a.id] = new Rec { go = go, band = band, female = female, task = a.task };
                    d.born++;
                    PresentationEventBus.Publish(new PresentationEvent(
                        _tick, S.years, S.season, PresentationEventType.AgentActivity, "agent-" + a.id, -1,
                        band == "child" ? "a child is born" : "a soul arrives"));
                }
            }
            return d;
        }

        public void Clear()
        {
            foreach (var r in _agents.Values) if (r.go != null) UnityEngine.Object.DestroyImmediate(r.go);
            _agents.Clear();
        }

        // ---- spawn mirrors PlaceAgents (base-body GLB; the LIVE animator owns walk/work now) ----
        GameObject Spawn(WorldState S, WorldAgent a, string band, bool female, Transform layer, bool editPose)
        {
            string baseNm = band == "child" ? (female ? "villager-child-f" : "villager-child")
                          : band == "elder" ? (female ? "villager-elder-f" : "villager-elder")
                          : (female ? "villager-f" : "villager");
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(CharDir + baseNm + ".glb");
            if (pf == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, layer);
            go.transform.position = GroundW(P(S, a.x, a.y));
            go.transform.localScale = Vector3.one * Scale;
            Face(S, a, go);
            go.name = $"agent_{a.id}_{a.name}";

            var rac = VillagerController(band, female);
            var anim = go.GetComponentInChildren<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            if (rac != null) anim.runtimeAnimatorController = rac;
            var aa = go.AddComponent<AgentAnimator>();
            aa.agentId = a.id; aa.task = a.task; aa.canWork = band == "adult"; aa.band = band; aa.female = female;

            if (!Application.isPlaying && editPose) StillPose(go, a, band, female);
            return go;
        }

        // edit-mode still: sample the task-right clip at a hash phase (parity with the still layer's read)
        static void StillPose(GameObject go, WorldAgent a, string band, bool female)
        {
            string suffix = (band == "adult" && AgentTaskRead.Working(a.task)) ? "-work"
                          : AgentTaskRead.Moving(a.task) ? "-walk" : "";
            string baseNm = band == "child" ? (female ? "villager-child-f" : "villager-child")
                          : band == "elder" ? (female ? "villager-elder-f" : "villager-elder")
                          : (female ? "villager-f" : "villager");
            var clip = LoadClip(CharDir + baseNm + suffix + ".glb") ?? LoadClip(CharDir + baseNm + ".glb");
            if (clip != null && clip.length > 0f)
                clip.SampleAnimation(go, (Hash(a.id, 1, a.id + 11) & 0xffffu) / 65536f * clip.length);
        }

        static AnimationClip LoadClip(string path)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            return null;
        }

        static RuntimeAnimatorController VillagerController(string band, bool female)
        {
            const string dir = "Assets/Emergence/Fas2/Anim";
            string key = band == "adult" ? (female ? "adult-f" : "adult") : band + (female ? "-f" : "");
            return key == "adult"
                ? AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(dir + "/VillagerAnim.controller")
                : AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{dir}/Villager-{key}.overrideController");
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
#endif
