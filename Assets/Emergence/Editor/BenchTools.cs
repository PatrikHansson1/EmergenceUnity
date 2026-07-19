// EMERGENCE P1 — audition benches (TD-027): WATER (4 candidates, decides Tier-2 money) and
// BLACKSMITH forge-marker SEAM test (D-062 gap). Same-shot, magenta-checked, evidence to Dropbox.
// Nothing is "in" until the EP's eye passes it (D-075) — these are the shots for that judgment.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public static class BenchTools
    {
        const string EvidenceDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\audition-lightrig";

        static T Find<T>(string exact) where T : Object
        {
            string typ = typeof(GameObject).IsAssignableFrom(typeof(T)) ? "Prefab" : "Material";
            foreach (var g in AssetDatabase.FindAssets($"t:{typ} {exact}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == exact) return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            return null;
        }

        static Material Lit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            var m = new Material(sh); m.SetColor("_BaseColor", c); return m;
        }

        static Camera FreshScene(string camName, Vector3 pos, Vector3 euler, float fov = 60f)
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            var go = new GameObject(camName); go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos; cam.transform.rotation = Quaternion.Euler(euler); cam.fieldOfView = fov;
            cam.farClipPlane = 2000f;
            return cam;
        }

        // ---------------- WATER BENCH ----------------
        [MenuItem("Emergence/P1 Dressing/RUN WATER BENCH (4 candidates)")]
        public static void RunWaterBench()
        {
            // THE actual fix (TD-027 finding): stylized water is flat without depth + opaque textures.
            EnableDepthOpaque();

            // "Lake to the horizon" grazing shot — the reliable water-surface framing: camera near
            // water level looks ACROSS the water, so fresnel/reflection/transparency/wave-normals all
            // read. A flat floor 2.5m below shows through the near water (depth), sky reflects on the far.
            var cam = FreshScene("BenchCamera", new Vector3(0f, 1.6f, -40f), new Vector3(1.5f, 0f, 0f));

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "LakeFloor"; floor.transform.localScale = new Vector3(30f, 1f, 30f);
            floor.transform.position = new Vector3(0f, -2.5f, 40f);
            floor.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.22f, 0.30f, 0.20f)); // dark lakebed

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterSurface"; water.transform.localScale = new Vector3(24f, 1f, 24f); // 240m — reaches the horizon
            water.transform.position = new Vector3(0f, 0.0f, 60f);
            var waterRend = water.GetComponent<Renderer>();

            EmergenceLightRig.Apply("spring", "day");
            EmergencePostStack.Apply("day");

            waterRend.sharedMaterial = Lit(new Color(0.18f, 0.42f, 0.55f)); // flat baseline
            Cap("waterbench-0-baseline");
            TrySwap(waterRend, "M_Shader Water", "waterbench-1-polyone");
            TrySwap(waterRend, "Ocean Water",    "waterbench-2-verpha");
            TrySwap(waterRend, "example-water-01","waterbench-3-bitgem");
            TrySwap(waterRend, "water",          "waterbench-4-houidisoft");

            Debug.Log("[WaterBench] done (depth+opaque ON, sloped shore) — baseline + 4 in " + EvidenceDir);
        }

        // Stylized water samples _CameraDepthTexture (depth tint / shore foam) and _CameraOpaqueTexture
        // (refraction). Both are OFF by default on the URP asset — the reason TD-027's first bench was flat.
        static void EnableDepthOpaque()
        {
            var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            if (rp == null) { Debug.LogWarning("[WaterBench] no URP asset in GraphicsSettings"); return; }
            var so = new SerializedObject(rp);
            var d = so.FindProperty("m_RequireDepthTexture");
            var o = so.FindProperty("m_RequireOpaqueTexture");
            if (d != null) d.boolValue = true;
            if (o != null) o.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rp);
            Debug.Log($"[WaterBench] URP depth={(d!=null&&d.boolValue)} opaque={(o!=null&&o.boolValue)}");
        }

        static void TrySwap(Renderer r, string matName, string shot)
        {
            var m = Find<Material>(matName);
            if (m == null) { Debug.LogWarning($"[WaterBench] material '{matName}' not found — skipping {shot}"); return; }
            r.sharedMaterial = m;
            Cap(shot);
        }

        // ---------------- BLACKSMITH SEAM ----------------
        [MenuItem("Emergence/P1 Dressing/RUN BLACKSMITH SEAM (forge marker)")]
        public static void RunBlacksmithSeam()
        {
            var cam = FreshScene("BenchCamera", new Vector3(3.2f, 2.1f, -4.6f), new Vector3(10f, -28f, 0f), 52f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground"; ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.GetComponent<Renderer>().sharedMaterial = Lit(new Color(0.34f, 0.44f, 0.24f));

            // the Tidal Flask house — the seam neighbor the forge props must match
            var house = Find<GameObject>("P_BLD_house_01");
            if (house != null)
            {
                var h = (GameObject)PrefabUtility.InstantiatePrefab(house);
                h.transform.position = new Vector3(-3.0f, 0f, 3.5f);
                h.transform.rotation = Quaternion.Euler(0, 25, 0);
            }

            // the forge marker composed from the UpDraftArt sampler (anvil, coal pile, hammer, tongs)
            Place("Anvil",     new Vector3(0.0f, 0f, 0.5f),  0);
            Place("Coal Pile", new Vector3(1.4f, 0f, 1.2f),  40);
            Place("Small Hammer", new Vector3(-0.2f, 0.78f, 0.4f), 90); // resting on the anvil
            Place("Flat Tongs",   new Vector3(0.9f, 0.02f, -0.1f), 15);

            EmergenceLightRig.Apply("spring", "day");
            EmergencePostStack.Apply("day");
            Cap("blacksmith-seam-noon");

            EmergenceLightRig.Apply("spring", "dusk"); // forge glow test at blue hour
            EmergencePostStack.Apply("dusk");
            Cap("blacksmith-seam-dusk");

            Debug.Log("[Blacksmith] seam shots (noon+dusk) in " + EvidenceDir);
        }

        static void Place(string prefabName, Vector3 pos, float yaw)
        {
            var pf = Find<GameObject>(prefabName);
            if (pf == null) { Debug.LogWarning("[Blacksmith] missing prefab " + prefabName); return; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        // ---------------- capture (magenta-checked) ----------------
        static void Cap(string name)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[Bench] no Camera.main"); return; }
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
                ps.Simulate(3.0f, true, true);
            const int w = 2560, h = 1440;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllBytes(Path.Combine(EvidenceDir, $"{name}.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
            File.AppendAllText(Path.Combine(EvidenceDir, "capture-log.txt"), $"{name}.png magentaPixels={magenta}/{w * h}\n");
            Debug.Log($"[Bench] {name}.png magenta={magenta}");
        }
    }
}
#endif
