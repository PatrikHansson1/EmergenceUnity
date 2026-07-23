// EMERGENCE — FAS 6 increment 3 (D-158): the LIVING FIRE LAYER — the eye catches up with the ear.
//
// Fires had visuals ONLY in the static dresser (PlaceFires, TD-025): Vefects fire + the warm point
// light + msVFX chimney smoke — all frozen at dress time. The living loop (Fas3WorldRuntime) never
// reconciled them, so in play mode/player a fire in state was INVISIBLE — while increment 2's
// state ear plays crackle point sources at exactly those positions. Sound without body breaks the
// world's coherence; this reconciler closes the seam.
//
// Mirrors the dresser's grammar 1:1 (same constants, same warm-point identity — D-114/115b's dusk
// law: "fires carry the warmth", the light is kept even without a VFX prefab): fire VFX at
// state fire positions, firelight point, chimney smoke on huts within SmokeNearFireTiles of a
// burning fire. Loads through EmergenceAssetCatalog (Resources) so the same code runs in editor
// play mode and player (D-137 school). D-078 r4: reads snapshots, writes nothing back; no sim RNG.
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class FireReconciler
    {
        public const float TileSize = 8f;            // matches WorldDresser
        public const float FireScale = 1.4f;         // matches WorldDresser.FireScale
        public const float SmokeScale = 0.6f;        // matches WorldDresser.SmokeScale
        public const float SmokeRoofLift = 4.2f;     // matches WorldDresser.SmokeRoofLift
        public const int   SmokeNearFireTiles = 3;   // matches WorldDresser.SmokeNearFireTiles

        // the locked warm-point identity (WorldDresser.PlaceFires — D-114/115b)
        public static readonly Color FirelightColor = new Color(1f, 0.62f, 0.28f);
        public const float FirelightIntensity = 2.6f;
        public const float FirelightRange = 12f;
        public const float FirelightLift = 1.2f;

        public int Count { get; private set; }
        public int SmokeCount { get; private set; }

        static readonly string[] FireNames = { "VFX_Fire_01_Medium", "VFX_Fire_01_Big", "P_FX_fire", "PF_FX_fire", "fire" };
        static readonly string[] SmokeNames = { "msVFX_Stylized Smoke 1", "msVFX_Stylized Smoke 2" };

        readonly Dictionary<string, GameObject> _fires = new();
        readonly Dictionary<string, GameObject> _smokes = new();
        Transform _layer;

        public void Reconcile(WorldState S)
        {
            if (S == null) return;
            if (_layer == null)
            {
                var existing = GameObject.Find("Fires_Live");
                _layer = existing != null ? existing.transform : new GameObject("Fires_Live").transform;
            }

            var fx = FirstInCatalog(FireNames);
            var smokePf = FirstInCatalog(SmokeNames);

            // --- fires: key on rounded tile — same fire, same body across applies
            var wantFires = new HashSet<string>();
            var fires = S.fires ?? System.Array.Empty<WorldFire>();
            foreach (var f in fires)
            {
                string key = Key(f.x, f.y);
                wantFires.Add(key);
                if (_fires.ContainsKey(key)) continue;
                var go = new GameObject("fire_" + key);
                go.transform.SetParent(_layer, true);
                go.transform.position = GroundW(P(S, f.x, f.y), 0.1f);
                if (fx != null)
                {
                    var vis = Object.Instantiate(fx, go.transform, false);
                    vis.transform.localPosition = Vector3.zero;
                    vis.transform.localScale = Vector3.one * FireScale;
                }
                // the warm point — kept even without a VFX prefab (guarantees the identity)
                var lgo = new GameObject("firelight");
                lgo.transform.SetParent(go.transform, false);
                lgo.transform.localPosition = Vector3.up * FirelightLift;
                var light = lgo.AddComponent<Light>();
                light.type = LightType.Point; light.color = FirelightColor;
                light.intensity = FirelightIntensity; light.range = FirelightRange;
                _fires[key] = go;
            }
            Remove(_fires, wantFires);

            // --- chimney smoke: a hut within SmokeNearFireTiles of a burning fire is "lived-in"
            var wantSmoke = new HashSet<string>();
            var huts = S.huts ?? System.Array.Empty<WorldHut>();
            if (smokePf != null)
                foreach (var h in huts)
                {
                    if (!NearAnyFire(fires, h.x, h.y)) continue;
                    string key = Key(h.x, h.y);
                    wantSmoke.Add(key);
                    if (_smokes.ContainsKey(key)) continue;
                    var go = Object.Instantiate(smokePf, _layer, true);
                    go.name = "chimneysmoke_" + (h.owner ?? key);
                    go.transform.position = GroundW(P(S, h.x, h.y), SmokeRoofLift);
                    go.transform.localScale = Vector3.one * SmokeScale;
                    _smokes[key] = go;
                }
            Remove(_smokes, wantSmoke);

            Count = _fires.Count;
            SmokeCount = _smokes.Count;
        }

        public void Clear()
        {
            foreach (var go in _fires.Values) if (go != null) Object.Destroy(go);
            foreach (var go in _smokes.Values) if (go != null) Object.Destroy(go);
            _fires.Clear(); _smokes.Clear();
            Count = 0; SmokeCount = 0;
        }

        /// <summary>The dresser's rule, verbatim: Chebyshev distance in tile space.</summary>
        public static bool NearAnyFire(WorldFire[] fires, float hx, float hy)
        {
            if (fires == null) return false;
            foreach (var f in fires)
                if (Mathf.Abs(f.x - hx) <= SmokeNearFireTiles && Mathf.Abs(f.y - hy) <= SmokeNearFireTiles) return true;
            return false;
        }

        static void Remove(Dictionary<string, GameObject> have, HashSet<string> want)
        {
            List<string> gone = null;
            foreach (var kv in have)
                if (!want.Contains(kv.Key)) { (gone ??= new List<string>()).Add(kv.Key); }
            if (gone == null) return;
            foreach (var k in gone) { if (have[k] != null) Object.Destroy(have[k]); have.Remove(k); }
        }

        static GameObject FirstInCatalog(string[] names)
        {
            var cat = EmergenceAssetCatalog.Load();
            if (cat == null) return null;
            foreach (var n in names) { var pf = cat.Prefab(n); if (pf != null) return pf; }
            return null;
        }

        static string Key(float x, float y) => Mathf.RoundToInt(x) + "_" + Mathf.RoundToInt(y);
        static Vector3 P(WorldState S, float x, float y, float h = 0) => new Vector3(x * TileSize, h, (S.H - 1 - y) * TileSize);
        static Vector3 GroundW(Vector3 world, float lift = 0)
        {
            var t = Terrain.activeTerrain;
            if (t != null) world.y = t.SampleHeight(world) + t.transform.position.y;
            return world + Vector3.up * lift;
        }
    }
}
