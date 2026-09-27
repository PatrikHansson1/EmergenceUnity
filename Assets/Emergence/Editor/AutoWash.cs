// EMERGENCE — the WASH (D-873 §4a "allt tvättas genom limmet", DEMO-BYGGPLAN steg 6): every URP/Lit material on a
// dressed instance is re-authored as a FlatKit "Stylized Surface" material that keeps the pack's hand-painted albedo
// (Texture Maps → Albedo, Multiply, Impact 1) and adds ONE shading language on top — cel Single, one shaded tone,
// Unity shadows tinted "the blue world". The pack assets are NEVER edited: washed copies live under
// Assets/Emergence/Materials/Washed/<name>__SS.mat and are swapped onto scene instances (an instance override).
// Drop Reports/RUN_WASH.trigger on a dressed scene (RUN_SCENEBIRTH first). Optional body "on"/"off" writes/clears the
// switch file Assets/Emergence/Materials/Washed/WASH-ON.txt that WorldDresser/SceneBirth consult (Patrik's eye decides).
// Evidence: 45-UNITY/evidence/wash/<date>/wash-eye-before.png, wash-eye-after.png (same frame), wash-doc-after.png, report.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoWash
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_WASH.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "WASH_DONE.txt");
        public const string WashedDir = "Assets/Emergence/Materials/Washed";
        public const string SwitchFile = WashedDir + "/WASH-ON.txt";
        const string UrpLit = "Universal Render Pipeline/Lit";
        const string FlatKitShader = "FlatKit/Stylized Surface";

        // THE LOOK (one language for three sources) — starting values, Patrik's eye tunes them at the 3-source image.
        static readonly Color ShadedTone   = new Color(0.66f, 0.70f, 0.82f, 1f); // _ColorDim: the shaded side leans blue
        static readonly Color ShadowColour = new Color(0.58f, 0.64f, 0.80f, 1f); // _UnityShadowColor: "den blå världen"
        const float SelfShadingSize = 0.5f, ShadowEdge = 0.06f, Flatness = 1.0f, LightContribution = 0.5f;

        static AutoWash() { EditorApplication.update += Tick; }

        public static bool Enabled => File.Exists(Path.Combine(Path.GetDirectoryName(Application.dataPath), SwitchFile));

        static Action _pending; static double _pendingAt;

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (_pending != null && EditorApplication.timeSinceStartup >= _pendingAt) { var p = _pending; _pending = null; p(); return; }
            if (_pending != null) return;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger).Trim().ToLowerInvariant(); File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var rep = new StringBuilder(); rep.AppendLine("WASH — " + DateTime.Now.ToString("s"));
            string verdict;
            try
            {
                var abs = Path.Combine(Path.GetDirectoryName(Application.dataPath), SwitchFile);
                if (body == "on") { Directory.CreateDirectory(Path.GetDirectoryName(abs)); File.WriteAllText(abs, "wash ON " + DateTime.Now.ToString("s") + "\n"); AssetDatabase.Refresh(); verdict = "SWITCH ON"; }
                else if (body == "off") { if (File.Exists(abs)) File.Delete(abs); AssetDatabase.Refresh(); verdict = "SWITCH OFF"; }
                else verdict = RunAB(rep);
            }
            catch (Exception e) { verdict = "ERROR " + e.Message; rep.AppendLine(verdict + "\n" + e.StackTrace); }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoWash] " + verdict);
        }

        // A/B on the current (dressed) scene: capture eye-level, wash, capture again from the same camera.
        static string RunAB(StringBuilder rep)
        {
            var evDir = Path.Combine(@"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\wash", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(evDir);
            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (cam == null) return "FAIL: no camera — RUN_SCENEBIRTH first";
            var savedPos = cam.transform.position; var savedRot = cam.transform.rotation;
            bool prevAsync = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
            try
            {
                var hut = GameObject.Find("Huts"); Transform h0 = hut != null && hut.transform.childCount > 0 ? hut.transform.GetChild(0) : null;
                if (h0 != null) { FrameEye(cam, h0, rep); Capture(cam, Path.Combine(evDir, "wash-eye-before.png"), rep); }
                var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                int swapped = Apply(roots, rep);
                // it.2 SEEN: freshly created materials rendered NOTHING in the same tick (outline + shadow only, no forward pass);
                // reloaded from disk two seconds later they rendered fine → the after-captures run on a later tick.
                var eyePos = cam.transform.position; var eyeRot = cam.transform.rotation;
                _pendingAt = EditorApplication.timeSinceStartup + 3.0;
                _pending = () =>
                {
                    bool pa = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
                    try
                    {
                        if (h0 != null) { cam.transform.position = eyePos; cam.transform.rotation = eyeRot; Capture(cam, Path.Combine(evDir, "wash-eye-after.png"), rep); }
                        cam.transform.position = savedPos; cam.transform.rotation = savedRot;
                        Capture(cam, Path.Combine(evDir, "wash-doc-after.png"), rep);
                        File.WriteAllText(Path.Combine(evDir, "wash-report.txt"), rep.ToString());
                        var v = $"OK washed renderers={swapped} switch={(Enabled ? "ON" : "OFF")} dir={evDir}";
                        File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + v + "\n" + rep);
                        Debug.Log("[AutoWash] " + v);
                    }
                    catch (Exception e) { File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " ERROR(after) " + e.Message + "\n" + rep); }
                    finally { ShaderUtil.allowAsyncCompilation = pa; cam.transform.position = savedPos; cam.transform.rotation = savedRot; }
                };
                return "PHASE1 washed renderers=" + swapped + " — after-captures pending";
            }
            finally { ShaderUtil.allowAsyncCompilation = prevAsync; }
        }

        // Swap every URP/Lit material under the roots for its washed twin. Returns renderers touched. Idempotent.
        public static int Apply(GameObject[] roots, StringBuilder rep)
        {
            var fk = Shader.Find(FlatKitShader);
            if (fk == null) { rep?.AppendLine("WASH: shader '" + FlatKitShader + "' not found — nothing done"); return 0; }
            if (!AssetDatabase.IsValidFolder("Assets/Emergence/Materials")) AssetDatabase.CreateFolder("Assets/Emergence", "Materials");
            if (!AssetDatabase.IsValidFolder(WashedDir)) AssetDatabase.CreateFolder("Assets/Emergence/Materials", "Washed");
            var cache = new Dictionary<Material, Material>();
            int renderers = 0, mats = 0, created = 0, skippedClip = 0, skippedOther = 0;
            foreach (var root in roots)
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    var arr = r.sharedMaterials; bool touched = false;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        var m = arr[i];
                        if (m == null || m.shader == null) continue;
                        if (m.shader.name != UrpLit) { if (m.shader != fk) skippedOther++; continue; }
                        if (m.IsKeywordEnabled("_ALPHATEST_ON") || m.GetFloat("_Surface") > 0.5f) { skippedClip++; continue; } // cutout/transparent: keep the pack shader for now
                        if (AssetDatabase.GetAssetPath(m).StartsWith(WashedDir)) continue;
                        if (!cache.TryGetValue(m, out var w)) { w = Washed(m, fk, out bool made); cache[m] = w; if (made) created++; }
                        arr[i] = w; touched = true; mats++;
                    }
                    if (touched) { r.sharedMaterials = arr; renderers++; }
                }
            rep?.AppendLine($"WASH: renderers={renderers} materialSlots={mats} distinct={cache.Count} newlyCreated={created} keptCutout/Transparent={skippedClip} keptPackShaders={skippedOther}");
            AssetDatabase.SaveAssets();
            return renderers;
        }

        static Material Washed(Material src, Shader fk, out bool made)
        {
            var path = WashedDir + "/" + Sanitize(src.name) + "__SS.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            made = false;
            if (existing != null) return existing;
            var m = new Material(fk) { name = Path.GetFileNameWithoutExtension(path) };
            // keep the pack's painted albedo
            if (src.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", src.GetTexture("_BaseMap")); m.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap")); m.SetTextureOffset("_BaseMap", src.GetTextureOffset("_BaseMap")); }
            m.SetColor("_BaseColor", src.HasProperty("_BaseColor") ? src.GetColor("_BaseColor") : Color.white);
            m.SetFloat("_TextureImpact", 1f);
            if (src.HasProperty("_Cull") && m.HasProperty("_Cull")) m.SetFloat("_Cull", src.GetFloat("_Cull")); // A/B it.1 SEEN: the drying hide (two-sided, _Cull 0) vanished from behind
            if (src.HasProperty("_BumpMap") && src.GetTexture("_BumpMap") != null && m.HasProperty("_BumpMap")) m.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
            // ONE shading language
            m.SetFloat("_CelPrimaryMode", 1f); m.EnableKeyword("_CELPRIMARYMODE_SINGLE");
            m.SetColor("_ColorDim", ShadedTone); m.SetFloat("_SelfShadingSize", SelfShadingSize); m.SetFloat("_ShadowEdgeSize", ShadowEdge); m.SetFloat("_Flatness", Flatness);
            m.SetFloat("_UnityShadowMode", 2f); m.EnableKeyword("_UNITYSHADOWMODE_COLOR"); m.SetColor("_UnityShadowColor", ShadowColour);
            m.SetFloat("_LightContribution", LightContribution);
            m.enableInstancing = true; // §8.1: Stylized Surface has no SRP Batcher — instancing carries it
            AssetDatabase.CreateAsset(m, path);
            made = true;
            return m;
        }

        static string Sanitize(string n) { foreach (var c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_'); return n; }

        static void FrameEye(Camera cam, Transform h0, StringBuilder rep)
        {
            var target = h0.position + Vector3.up * 1.6f; float bestClear = -1f; Vector3 bestPos = target + Vector3.back * 14f + Vector3.up * 1.75f;
            for (int b = 0; b < 360; b += 30)
            {
                var dir = Quaternion.Euler(0, b, 0) * Vector3.back; var pos = h0.position + dir * 14f + Vector3.up * 1.75f;
                var ray = new Ray(pos, (target - pos).normalized); float clear = 14f;
                if (Physics.Raycast(ray, out var hit, 14f) && hit.transform.root != h0.root) clear = hit.distance;
                if (clear > bestClear) { bestClear = clear; bestPos = pos; }
            }
            cam.transform.position = bestPos; cam.transform.LookAt(target);
            rep.AppendLine($"eye frame: hut {h0.name}, clear {bestClear:0.0} m of 14");
            // ground diagnostic (D-881 it.2: the same hut stood on trodden dirt at 12:45 and on grass at 12:51 — which is the data?)
            var t = Terrain.activeTerrain;
            if (t != null && t.terrainData != null)
            {
                var td = t.terrainData; var tp = t.transform.position;
                float u = Mathf.Clamp01((h0.position.x - tp.x) / td.size.x), v = Mathf.Clamp01((h0.position.z - tp.z) / td.size.z);
                int ax = Mathf.Clamp((int)(u * (td.alphamapWidth - 1)), 0, td.alphamapWidth - 1), ay = Mathf.Clamp((int)(v * (td.alphamapHeight - 1)), 0, td.alphamapHeight - 1);
                var a = td.GetAlphamaps(ax, ay, 1, 1); var sb = new StringBuilder();
                for (int l = 0; l < td.alphamapLayers; l++) sb.Append(td.terrainLayers[l] != null ? td.terrainLayers[l].name : "?").Append('=').Append(a[0, 0, l].ToString("0.00")).Append(' ');
                rep.AppendLine($"ground at hut: alphamap cell ({ax},{ay}) of {td.alphamapWidth} → {sb} · terrainData={AssetDatabase.GetAssetPath(td)} basemapDist={t.basemapDistance} material={(t.materialTemplate ? t.materialTemplate.shader.name : "none")} splatTex={(td.alphamapTextureCount > 0 && td.alphamapTextures[0] != null ? td.alphamapTextures[0].width.ToString() : "?")}");
            }
            else rep.AppendLine("ground at hut: no active terrain");
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
            rep.AppendLine($"CAPTURE {Path.GetFileName(file)} magenta={magenta}/{w * h} meanLum={lum / (3L * px.Length)}");
        }
    }
}
#endif
