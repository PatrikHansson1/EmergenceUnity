// EMERGENCE P1 — free-assets audition runner (TD-025): sky + fire + smoke + post, same-shot A/B.
// Two menu items, each fully automated: dress a world, then capture noon & dusk, post OFF vs ON.
// The "before" the EP compares against is the TD-024 captures (gray horizon, no post) already in
// evidence/pack-verify/. Here every shot carries the new sky/fire/smoke; the post OFF/ON pair
// isolates the post stack's contribution. Particle systems only animate in play mode, so we
// ParticleSystem.Simulate() every system before each capture to make Vefects fire/msVFX smoke render.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class AuditionRunner
    {
        const string EvidenceDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\audition-lightrig";
        static string WS(string f) => Path.Combine(Application.dataPath, "Emergence", "WorldStates", f);

        [MenuItem("Emergence/P1 Dressing/RUN AUDITION - Noon world (777-y120)")]
        public static void RunNoonWorld() => Run(WS("world-777-y120.json"), "spring", "noon777");

        [MenuItem("Emergence/P1 Dressing/RUN AUDITION - Dusk one-warm-point (4242)")]
        public static void RunDuskWorld() => Run(WS("world-4242-y120-dusk.json"), "winter", "dusk4242", starDome: true);

        // TD-028: ground-level closeup to actually SEE the villagers + tech anchors (the doc camera
        // is too high — 1.7m people are ~2px at 55m). Stand near the biggest village, eye height.
        [MenuItem("Emergence/P1 Dressing/RUN PEOPLE CLOSEUP (villagers + tech)")]
        public static void RunPeopleCloseup()
        {
            var json = WS("world-777-y120.json");
            if (!File.Exists(json)) { Debug.LogError("[Closeup] missing " + json); return; }
            WorldDresser.Build(json);
            var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(json));
            if (S.villages == null || S.villages.Length == 0) { Debug.LogError("[Closeup] no villages"); return; }
            var v = S.villages[0];
            float ts = WorldDresser.TileSize;
            var t = Terrain.activeTerrain;
            // aim at the villagers, not the village centre (the well sits there). Find the agent
            // nearest the village centre and frame HIM — people are the subject of this shot.
            WorldAgent near = null; float best = float.MaxValue;
            foreach (var a in S.agents)
            {
                float d = (a.x - v.x) * (a.x - v.x) + (a.y - v.y) * (a.y - v.y);
                if (d < best) { best = d; near = a; }
            }
            float ax = near != null ? near.x : v.x, ay = near != null ? near.y : v.y;
            Vector3 look = new Vector3(ax * ts, 0f, (S.H - 1 - ay) * ts);
            if (t != null) look.y = t.SampleHeight(look) + t.transform.position.y;
            var cam = Camera.main;
            Vector3 pos = look + new Vector3(3.5f, 0f, -9f);   // ~10 m away, human eye height
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y + 1.7f;
            cam.transform.position = pos;
            cam.transform.LookAt(look + Vector3.up * 0.9f);    // aim at torso height
            cam.fieldOfView = 50f;

            EmergencePostStack.Remove();
            EmergenceLightRig.Apply("spring", "day");
            Cap("people-closeup-noon-nopost");
            EmergencePostStack.Apply("day");
            Cap("people-closeup-noon");
            EmergenceLightRig.Apply("spring", "dusk");
            EmergencePostStack.Apply("dusk");
            Cap("people-closeup-dusk");
            Debug.Log("[Closeup] villager/tech closeups written");
        }

        static void Run(string jsonPath, string season, string tag, bool starDome = false)
        {
            if (!File.Exists(jsonPath)) { Debug.LogError($"[Audition] missing {jsonPath}"); return; }
            WorldDresser.Build(jsonPath);

            EmergencePostStack.Remove();
            EmergenceLightRig.Apply(season, "day");
            Cap($"{tag}-noon-nopost");
            EmergencePostStack.Apply("day");
            Cap($"{tag}-noon-post");

            EmergencePostStack.Remove();
            EmergenceLightRig.Apply(season, "dusk");
            Cap($"{tag}-dusk-nopost");
            EmergencePostStack.Apply("dusk");            // fable-5 retune: dusk lifted, no longer crushes
            Cap($"{tag}-dusk-post");

            // EP directive 2026-07-19: the star dome must READ. Pack night sky (M_ENV_SKYBOX_night)
            // carries moon + stars in the same painterly hand (fable-5 sky bench). Tilt the camera
            // UP over the one warm point so the village sits low and the stars fill the dome.
            if (starDome)
            {
                EmergenceLightRig.Apply(season, "night");
                StarDomeCamera();
                EmergencePostStack.Apply("night");
                Cap($"{tag}-night-stardome");
                Debug.Log($"[Audition] {tag}: 5 captures (incl. star dome) written to {EvidenceDir}");
            }
            else Debug.Log($"[Audition] {tag}: 4 captures written to {EvidenceDir}");
        }

        // stand just south of the one warm point, low, looking gently up — the fire/lit village
        // anchors the lower frame, the star dome fills the top (EP's "look up and see the stars").
        static void StarDomeCamera()
        {
            var cam = Camera.main; if (cam == null) return;
            var fire = GameObject.Find("firelight");
            Vector3 f = fire != null ? fire.transform.position : new Vector3(583f, 2f, 98f);
            var t = Terrain.activeTerrain;
            var pos = new Vector3(f.x, 0f, f.z - 48f);
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y + 2.2f;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Euler(-3f, 0f, 0f); // near-level, slight up
            cam.fieldOfView = 62f;
        }

        // SKY BENCH (EP 2026-07-19: "packet vi köpte har en skybox — gratis kan vara sämre").
        // Ground-level camera at the biggest village, horizon in frame, post ON — the dome
        // actually shows here. Same shot: pack sky vs Staggart per phase. AllSky excluded
        // by register (photoreal vs painterly — sobriety filter), noted in the verdict.
        [MenuItem("Emergence/P1 Dressing/RUN SKY BENCH - pack vs Staggart (ground shot)")]
        public static void RunSkyBench()
        {
            var json = WS("world-777-y120.json");
            if (!File.Exists(json)) { Debug.LogError("[SkyBench] missing " + json); return; }
            WorldDresser.Build(json);
            EmergencePostStack.Apply();
            GroundCamera();

            EmergenceLightRig.Apply("spring", "day");   // pack day (first choice)
            Cap("skybench-noon-packday");
            SetSky("Sky_Noon");                          // Staggart challenger
            Cap("skybench-noon-staggart");

            EmergenceLightRig.Apply("spring", "dusk");  // Staggart dusk (pack has none)
            Cap("skybench-dusk-staggart");
            SetSky("M_ENV_SKYBOX_night");                // pack night as dusk stand-in
            Cap("skybench-dusk-packnight");

            Debug.Log("[SkyBench] 4 ground-level sky shots written");
        }

        static void GroundCamera()
        {
            var cam = Camera.main; if (cam == null) return;
            // stand at the heartland's southern edge, eye height, looking gently up past the village
            var t = Terrain.activeTerrain;
            var pos = new Vector3(400f, 0f, 180f);
            if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y + 1.7f;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Euler(-6f, 10f, 0f); // horizon + dome in frame
        }

        static void SetSky(string exact)
        {
            foreach (var g in AssetDatabase.FindAssets($"t:Material {exact}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == exact)
                { RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(p); return; }
            }
            Debug.LogWarning("[SkyBench] sky not found: " + exact);
        }

        static void Cap(string name)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[Audition] no Camera.main — dress first"); return; }
            // force particle systems (Vefects fire, msVFX smoke) to a mid-animation frame — they
            // do not animate in edit mode, so a raw capture would show empty emitters.
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                ps.Simulate(3.0f, true, true);

            const int w = 2560, h = 1440;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;

            var px = tex.GetPixels32();
            int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;

            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllBytes(Path.Combine(EvidenceDir, $"{name}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
            File.AppendAllText(Path.Combine(EvidenceDir, "capture-log.txt"), $"{name}.png magentaPixels={magenta}/{w * h}\n");
            Debug.Log($"[Audition] {name}.png magenta={magenta}");
        }
    }
}
#endif
