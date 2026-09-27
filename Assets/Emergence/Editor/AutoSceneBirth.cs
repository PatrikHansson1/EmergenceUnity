// EMERGENCE — DEMO-BYGGPLAN steg 5: THE FLOOR SCENE, born from the pack's own demo (VISUELL-TOTALPLAN L2 "demoscenen är
// golvet", L4 "ljuset är en versionerad fil", D-878). Drop Reports/RUN_SCENEBIRTH.trigger. Optional body: a world-state json
// (project-relative) to dress into the floor for the eye still; default = the codex demo world.
//
// What it does, in order:
//  1. RP-guard: pins Assets/Settings/PC_RPAsset (FlatKit demos swap it — D-875 fynd 1).
//  2. Opens demoscene_village_day READ-ONLY (never saved) and RECORDS the look: the directional sun (rotation, colour,
//     intensity, shadows), RenderSettings (skybox, ambient mode + colours, fog values), and the global Volume profile.
//  3. Copies the pack's day post profile to Assets/Emergence/Rendering/EmergenceLook_day.asset (ours, versioned) and
//     disables ONLY the Lift Gamma Gain override on the copy (D-069). The pack asset is untouched.
//  4. Writes the floor's fog colour/range into Assets/Emergence/Rendering/EmergenceFog.asset (FlatKit Fog = the ONE haze
//     layer; built-in fog stays OFF in the floor scene).
//  5. Builds Assets/Emergence/Scenes/EmergenceFloor_day.unity: Sun (+ child marker "FLOOR:demoscene_village_day"),
//     RenderSettings copied (fog off), global Volume "EmergenceLook" -> EmergenceLook_day. Nothing else. Saved.
//  6. Dresses the chosen world INTO the floor (WorldDresser.Build opens the floor when it exists), measures the Nature tree
//     heights (L6 measuring stick 1.75 m), captures the doc gaze + one eye-level frame, writes the report. Scene NOT saved.
// Evidence: 45-UNITY/evidence/scene-birth/<yyyy-MM-dd>/ ; Reports/SCENEBIRTH_DONE.txt.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoSceneBirth
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_SCENEBIRTH.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "SCENEBIRTH_DONE.txt");
        const string DemoScene   = "Assets/Fantastic Village Pack/scenes/demoscene_village_day.unity";
        const string FloorScene  = WorldDresser.FloorScenePath;
        const string LookAsset   = "Assets/Emergence/Rendering/EmergenceLook_day.asset";
        const string FogAsset    = "Assets/Emergence/Rendering/EmergenceFog.asset";
        const string Marker      = "FLOOR:demoscene_village_day";
        // D-879: default = a FULL export (Tools/export-world-full.js) of the canonical v25b engine — tiles + footfall (pathUse).
        // world-codex-demo.json (2026-07-20 vintage, no pathUse, no era) stays as the codex-coverage fixture.
        const string DefaultWorld = "Assets/Emergence/WorldStates/world-8919-y120-full.json";
        const string RpPath      = "Assets/Settings/PC_RPAsset.asset";
        const float FogNear = 120f, FogFar = 700f, FogIntensity = 0.55f;   // our camera scale (see it.2 note)
        const string NatureRoot = "Assets/Fantastic Nature Pack";

        static AutoSceneBirth() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var rep = new StringBuilder();
            rep.AppendLine("SCENE BIRTH — " + DateTime.Now.ToString("s") + " (D-878, steg 5)");
            string verdict;
            try { verdict = Run(string.IsNullOrEmpty(body) ? DefaultWorld : body, rep); }
            catch (Exception e) { rep.AppendLine("ERROR " + e); verdict = "ERROR " + e.Message; }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoSceneBirth] " + verdict);
        }

        // ---- the recorded look ----
        class Look
        {
            public Quaternion sunRot; public Color sunColor; public float sunIntensity; public LightShadows sunShadows; public float sunShadowStrength, sunBias, sunNormalBias; public string sunName;
            public AmbientMode ambientMode; public Color ambientSky, ambientEquator, ambientGround, ambientLight; public float ambientIntensity;
            public Material skybox; public Color subtractive;
            public bool fog; public FogMode fogMode; public Color fogColor; public float fogStart, fogEnd, fogDensity;
            public VolumeProfile profile; public float volumeWeight;
        }

        static void RpGuard(StringBuilder rep)
        {
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RpPath); if (pc == null) { rep.AppendLine("RPGUARD: PC_RPAsset missing"); return; }
            var q = QualitySettings.renderPipeline; var d = GraphicsSettings.defaultRenderPipeline;
            if (q != pc || d != pc) { QualitySettings.renderPipeline = pc; GraphicsSettings.defaultRenderPipeline = pc; rep.AppendLine("RPGUARD: DRIFT quality=" + (q ? q.name : "null") + " default=" + (d ? d.name : "null") + " -> pinned PC_RPAsset"); }
            else rep.AppendLine("RPGUARD: ok (PC_RPAsset)");
        }

        static string Run(string worldJson, StringBuilder rep)
        {
            RpGuard(rep);
            // 2. read the demo (never saved)
            if (!File.Exists(DemoScene)) return "FAIL: demo scene missing " + DemoScene;
            EditorSceneManager.OpenScene(DemoScene, OpenSceneMode.Single);
            RpGuard(rep);
            var L = new Look();
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude).Where(l => l.type == LightType.Directional).OrderByDescending(l => l.intensity).ToArray();
            if (lights.Length == 0) return "FAIL: no directional light in demo";
            var sun = lights[0];
            L.sunName = sun.gameObject.name; L.sunRot = sun.transform.rotation; L.sunColor = sun.color; L.sunIntensity = sun.intensity; L.sunShadows = sun.shadows;
            L.sunShadowStrength = sun.shadowStrength; L.sunBias = sun.shadowBias; L.sunNormalBias = sun.shadowNormalBias;
            L.ambientMode = RenderSettings.ambientMode; L.ambientSky = RenderSettings.ambientSkyColor; L.ambientEquator = RenderSettings.ambientEquatorColor; L.ambientGround = RenderSettings.ambientGroundColor;
            L.ambientLight = RenderSettings.ambientLight; L.ambientIntensity = RenderSettings.ambientIntensity; L.skybox = RenderSettings.skybox; L.subtractive = RenderSettings.subtractiveShadowColor;
            L.fog = RenderSettings.fog; L.fogMode = RenderSettings.fogMode; L.fogColor = RenderSettings.fogColor; L.fogStart = RenderSettings.fogStartDistance; L.fogEnd = RenderSettings.fogEndDistance; L.fogDensity = RenderSettings.fogDensity;
            var vols = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude).Where(v => v.isGlobal && v.sharedProfile != null).OrderByDescending(v => v.priority).ToArray();
            if (vols.Length > 0) { L.profile = vols[0].sharedProfile; L.volumeWeight = vols[0].weight; }
            var e = L.sunRot.eulerAngles;
            rep.AppendLine($"DEMO sun '{L.sunName}': rot=({e.x:0.#},{e.y:0.#},{e.z:0.#}) color=({L.sunColor.r:0.###},{L.sunColor.g:0.###},{L.sunColor.b:0.###}) I={L.sunIntensity} shadows={L.sunShadows} strength={L.sunShadowStrength} bias={L.sunBias}/{L.sunNormalBias}");
            rep.AppendLine($"DEMO ambient: mode={L.ambientMode} sky=({L.ambientSky.r:0.##},{L.ambientSky.g:0.##},{L.ambientSky.b:0.##}) eq=({L.ambientEquator.r:0.##},{L.ambientEquator.g:0.##},{L.ambientEquator.b:0.##}) gnd=({L.ambientGround.r:0.##},{L.ambientGround.g:0.##},{L.ambientGround.b:0.##}) I={L.ambientIntensity} skybox={(L.skybox ? L.skybox.name : "none")}");
            rep.AppendLine($"DEMO fog: on={L.fog} mode={L.fogMode} color=({L.fogColor.r:0.##},{L.fogColor.g:0.##},{L.fogColor.b:0.##}) {L.fogStart}-{L.fogEnd} density={L.fogDensity}");
            rep.AppendLine("DEMO post: " + (L.profile ? AssetDatabase.GetAssetPath(L.profile) + " weight=" + L.volumeWeight : "none"));

            // 3. our look profile (copy of the pack's, LGG off)
            if (L.profile != null)
            {
                var src = AssetDatabase.GetAssetPath(L.profile);
                if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(LookAsset) != null) AssetDatabase.DeleteAsset(LookAsset);
                if (!AssetDatabase.CopyAsset(src, LookAsset)) return "FAIL: could not copy profile to " + LookAsset;
                var look = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LookAsset);
                int off = 0;
                foreach (var c in look.components) if (c != null && c.GetType().Name == "LiftGammaGain" && c.active) { c.active = false; EditorUtility.SetDirty(c); off++; }
                EditorUtility.SetDirty(look); AssetDatabase.SaveAssets();
                rep.AppendLine($"LOOK: {LookAsset} = copy of {Path.GetFileName(src)} ({look.components.Count} overrides; LGG disabled on {off}, D-069)");
            }
            else rep.AppendLine("LOOK: demo has no global volume — no profile copied");

            // 4. FlatKit fog takes the floor's haze
            // Iteration 2 (SEEN 2026-09-27 it.1: the demo's 28–210 m haze at 0.8 buried the whole diorama from the 55 m doc camera):
            // the floor gives the COLOUR; the RANGE follows our camera scale (world 800×560 m, doc gaze 55 m up). Deliberate deviation, logged.
            var fog = AssetDatabase.LoadAssetAtPath<ScriptableObject>(FogAsset);
            if (fog != null && L.fog)
            {
                var so = new SerializedObject(fog);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(L.fogColor, 0f), new GradientColorKey(L.fogColor, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 1f) });
                var pg = so.FindProperty("distanceGradient"); if (pg != null) pg.gradientValue = g;
                var pn = so.FindProperty("near"); if (pn != null) pn.floatValue = FogNear;
                var pf = so.FindProperty("far"); if (pf != null) pf.floatValue = FogFar;
                var pi = so.FindProperty("distanceFogIntensity"); if (pi != null) pi.floatValue = FogIntensity;
                so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(fog); AssetDatabase.SaveAssets();
                rep.AppendLine($"FOG: EmergenceFog.asset <- floor colour ({L.fogColor.r:0.##},{L.fogColor.g:0.##},{L.fogColor.b:0.##}), range {FogNear}-{FogFar} intensity {FogIntensity} (demo had {L.fogStart}-{L.fogEnd}; built-in fog OFF in floor)");
            }
            CoordinateHaze(L, rep);

            // 5. the floor scene
            var floor = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sunGo = new GameObject("Sun"); var sl = sunGo.AddComponent<Light>();
            sl.type = LightType.Directional; sl.color = L.sunColor; sl.intensity = L.sunIntensity; sl.shadows = L.sunShadows; sl.shadowStrength = L.sunShadowStrength; sl.shadowBias = L.sunBias; sl.shadowNormalBias = L.sunNormalBias;
            sunGo.transform.rotation = L.sunRot;
            var marker = new GameObject(Marker); marker.transform.SetParent(sunGo.transform, false);
            RenderSettings.sun = sl;
            RenderSettings.ambientMode = L.ambientMode; RenderSettings.ambientSkyColor = L.ambientSky; RenderSettings.ambientEquatorColor = L.ambientEquator; RenderSettings.ambientGroundColor = L.ambientGround;
            RenderSettings.ambientLight = L.ambientLight; RenderSettings.ambientIntensity = L.ambientIntensity; RenderSettings.skybox = L.skybox; RenderSettings.subtractiveShadowColor = L.subtractive;
            RenderSettings.fog = false; RenderSettings.fogMode = L.fogMode; RenderSettings.fogColor = L.fogColor; RenderSettings.fogStartDistance = L.fogStart; RenderSettings.fogEndDistance = L.fogEnd; // values kept, fog OFF (FlatKit Fog is the haze)
            var lookGo = new GameObject("EmergenceLook"); var vol = lookGo.AddComponent<Volume>(); vol.isGlobal = true; vol.priority = 1f; vol.weight = 1f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LookAsset);
            Directory.CreateDirectory(Path.GetDirectoryName(FloorScene));
            if (!EditorSceneManager.SaveScene(floor, FloorScene)) return "FAIL: floor scene save failed";
            rep.AppendLine("FLOOR saved: " + FloorScene + " (Sun + " + Marker + ", RenderSettings inherited, fog off, Volume EmergenceLook)");

            // 6. dress a world into the floor + measure + capture
            var evDir = Path.Combine(@"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\scene-birth", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(evDir);
            if (File.Exists(worldJson))
            {
                WorldDresser.Build(worldJson);
                EmergencePostStack.Apply("day");
                rep.AppendLine("WORLD dressed: " + worldJson + " floor=" + EmergenceLightRig.IsFloor());
                MeasureTrees(rep);
                TerrainDiag(rep);
                bool prevAsync = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    // D-881 THE WASH (steg 6): when Patrik's switch is on, every URP/Lit material on the dressed instances is swapped
                    // for its FlatKit Stylized Surface twin before the evidence is captured. Off = pack shaders as authored.
                    if (AutoWash.Enabled) AutoWash.Apply(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(), rep);
                    else rep.AppendLine("WASH: switch off (" + AutoWash.SwitchFile + " absent) — pack shaders as authored");
                    var cam = Camera.main; if (cam == null) return "FAIL: no Camera.main after dressing";
                    Capture(cam, Path.Combine(evDir, "birth-doc-gaze.png"), rep);
                    // eye level (D-064 gaze 2): 1.75 m at the first hut, looking along the settlement
                    var hut = GameObject.Find("Huts"); Transform h0 = hut != null && hut.transform.childCount > 0 ? hut.transform.GetChild(0) : null;
                    var savedPos = cam.transform.position; var savedRot = cam.transform.rotation;
                    if (h0 != null)
                    {
                        // EvidenceFraming law (D-164): pick the bearing whose line of sight to the hut is least obstructed —
                        // it.7 SEEN: a fixed 35° bearing put a tree trunk across the whole frame.
                        var target = h0.position + Vector3.up * 1.6f; float bestClear = -1f; Vector3 bestPos = target + Vector3.back * 14f + Vector3.up * 1.75f;
                        for (int b = 0; b < 360; b += 30)
                        {
                            var dir = Quaternion.Euler(0, b, 0) * Vector3.back; var pos = h0.position + dir * 14f + Vector3.up * 1.75f;
                            var ray = new Ray(pos, (target - pos).normalized); float clear = 14f;
                            if (Physics.Raycast(ray, out var hit, 14f) && hit.transform.root != h0.root) clear = hit.distance;
                            if (clear > bestClear) { bestClear = clear; bestPos = pos; }
                        }
                        cam.transform.position = bestPos; cam.transform.LookAt(target);
                        rep.AppendLine($"eye-level bearing chosen: clear line {bestClear:0.0} m of 14");
                        Capture(cam, Path.Combine(evDir, "birth-eye-level.png"), rep);
                    }
                    else rep.AppendLine("eye-level: no Huts in this world — skipped");
                    cam.transform.position = savedPos; cam.transform.rotation = savedRot;
                }
                finally { ShaderUtil.allowAsyncCompilation = prevAsync; }
                // pink audit of the dressed scene (the family gate: 0 expected now that Dreamscape is out)
                int pink = 0; var pinkNames = new System.Collections.Generic.HashSet<string>();
                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                    foreach (var m in r.sharedMaterials) if (m != null && m.shader != null && (m.shader.name == "Hidden/InternalErrorShader" || !m.shader.isSupported)) { pink++; pinkNames.Add(m.name); }
                rep.AppendLine("PINK materials in dressed scene: " + pink + (pink > 0 ? " [" + string.Join(", ", pinkNames.Take(12)) + "]" : ""));
            }
            else rep.AppendLine("WORLD: " + worldJson + " not found — floor only");
            File.WriteAllText(Path.Combine(evDir, "scene-birth-report.txt"), rep.ToString());
            RpGuard(rep);
            AssetDatabase.SaveAssets();
            return "OK floor born from demoscene_village_day → " + FloorScene;
        }

        // ONE haze layer (PACK-DOCS §4 öppen punkt c): the FANTASTIC foliage shaders carry their own Distance Fade
        // (cyan by default, 26–515 m at 0.8 — SEEN it.1 as cyan bushes across the whole diorama). Coordinate it with our
        // fog: same colour, same range, same strength — so the foliage fades INTO the haze instead of into cyan.
        // Reversible: every original value is written to the evidence folder as JSON (haze-originals.json).
        static void CoordinateHaze(Look L, StringBuilder rep)
        {
            var guids = AssetDatabase.FindAssets("t:Material", new[] { NatureRoot });
            var log = new StringBuilder("{\n");
            int n = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || !m.HasProperty("_DistanceFadeColor")) continue;
                var oc = m.GetColor("_DistanceFadeColor");
                float os = m.HasProperty("_DistanceFadeStart") ? m.GetFloat("_DistanceFadeStart") : -1, oe = m.HasProperty("_DistanceFadeEnd") ? m.GetFloat("_DistanceFadeEnd") : -1, oo = m.HasProperty("_DistanceFadeOpacity") ? m.GetFloat("_DistanceFadeOpacity") : -1;
                log.Append($"  \"{path}\": {{\"color\":[{oc.r},{oc.g},{oc.b},{oc.a}],\"start\":{os},\"end\":{oe},\"opacity\":{oo}}},\n");
                m.SetColor("_DistanceFadeColor", L.fogColor);
                if (os >= 0) m.SetFloat("_DistanceFadeStart", FogNear);
                if (oe >= 0) m.SetFloat("_DistanceFadeEnd", FogFar);
                if (oo >= 0) m.SetFloat("_DistanceFadeOpacity", FogIntensity);
                EditorUtility.SetDirty(m); n++;
                rep.AppendLine($"  HAZE {Path.GetFileName(path)}: distance fade was ({oc.r:0.##},{oc.g:0.##},{oc.b:0.##}) {os}-{oe} @{oo} -> fog colour {FogNear}-{FogFar} @{FogIntensity}");
            }
            var ls = log.ToString().TrimEnd(); if (ls.EndsWith(",")) ls = ls.Substring(0, ls.Length - 1); log = new StringBuilder(ls); log.Append("\n}\n");
            var evDir = Path.Combine(@"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\scene-birth", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(evDir);
            var orig = Path.Combine(evDir, "haze-originals.json");
            if (!File.Exists(orig)) File.WriteAllText(orig, log.ToString().Replace("\\", "/"));   // first run keeps the true originals
            AssetDatabase.SaveAssets();
            rep.AppendLine($"HAZE coordinated on {n} Nature materials (originals: {orig})");
        }

        static void TerrainDiag(StringBuilder rep)
        {
            var t = Terrain.activeTerrain; if (t == null) { rep.AppendLine("TERRAIN: none"); return; }
            var d = t.terrainData; var ls = d.terrainLayers;
            rep.AppendLine($"TERRAIN layers={ls.Length} alphaRes={d.alphamapResolution} material={(t.materialTemplate ? t.materialTemplate.shader.name : "null")} trees={d.treeInstanceCount} protos={d.treePrototypes.Length} detailProtos={d.detailPrototypes.Length}");
            var am = d.GetAlphamaps(0, 0, d.alphamapWidth, d.alphamapHeight);
            var dom = new int[ls.Length];
            for (int y = 0; y < d.alphamapHeight; y++) for (int x = 0; x < d.alphamapWidth; x++) { int bi = 0; float bv = -1; for (int l = 0; l < ls.Length; l++) if (am[y, x, l] > bv) { bv = am[y, x, l]; bi = l; } dom[bi]++; }
            for (int l = 0; l < ls.Length; l++) rep.AppendLine($"  LAYER {l}: {(ls[l] ? ls[l].name : "null")} tex={(ls[l] && ls[l].diffuseTexture ? ls[l].diffuseTexture.name : "none")} tile={(ls[l] ? ls[l].tileSize.x.ToString("0.#") : "?")} dominant={dom[l]} px ({100.0 * dom[l] / (d.alphamapWidth * d.alphamapHeight):0.0}%)");
        }

        static void MeasureTrees(StringBuilder rep)
        {
            // L6 measuring stick: report authored heights of the Nature trees actually placed (bounds of the LOD0 renderers)
            var nature = GameObject.Find("Nature"); if (nature == null) return;
            var seen = new System.Collections.Generic.Dictionary<string, float>();
            foreach (Transform c in nature.transform)
            {
                var src = PrefabUtility.GetCorrespondingObjectFromSource(c.gameObject); var key = src != null ? src.name : c.name;
                if (seen.ContainsKey(key)) continue;
                var rs = c.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                seen[key] = b.size.y / Mathf.Max(0.001f, c.localScale.y);
            }
            foreach (var kv in seen.OrderBy(k => k.Key)) rep.AppendLine($"  MEASURE {kv.Key}: height {kv.Value:0.0} m at scale 1 (villager 1.75 m)");
        }

        static void Capture(Camera cam, string file, StringBuilder rep)
        {
            const int w = 2560, h = 1440;
            var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt; var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            var px = tex.GetPixels32(); int magenta = 0; long lum = 0;
            foreach (var c in px) { if (c.r > 220 && c.b > 220 && c.g < 80) magenta++; lum += c.r + c.g + c.b; }
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            rep.AppendLine($"CAPTURE {Path.GetFileName(file)} magenta={magenta}/{w * h} meanLum={lum / (3L * px.Length)} cam=({cam.transform.position.x:0},{cam.transform.position.y:0},{cam.transform.position.z:0})");
        }
    }
}
#endif
