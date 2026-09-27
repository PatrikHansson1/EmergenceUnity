// EMERGENCE — trigger-file pack verification (PACK-IMPORT-VERIFICATION-PLAN rows 0/2/3/4/6, headless-from-bridge).
// Drop Reports/RUN_PACKVERIFY.trigger. Optional body: comma-separated pack root folders (project-relative);
// empty = the default owned-pack roots. Writes Reports/PACKVERIFY_DONE.txt + evidence to
// 45-UNITY/evidence/pack-verify/<yyyy-MM-dd>/ : environment, zero-pink scan, one capture per demo scene
// (magenta pixel count), texture budget. Never saves a scene, never edits a pack asset (D-874 F10, ASSET-INTAKE).
// RP GUARD (D-875): FlatKit demo scenes carry FlatKit.AutoLoadPipelineAsset ([ExecuteAlways]) which swaps
// QualitySettings.renderPipeline / GraphicsSettings.defaultRenderPipeline on scene open — run 2 left the project on
// Valley-URP/Wanderer-URP (cascades 4, shadowDist 100) and SaveAssets persisted it. The guard pins PC_RPAsset before
// the environment read, after every capture and at exit, and reports which RP each scene was captured under.
// Body token "reimport" additionally force-reimports every InternalErrorShader material (+ its shader) and rescans.
// Body token "rp=<asset path>" captures every scene under that RenderPipelineAsset (transient — the guard restores
// PC_RPAsset afterwards; nothing is saved to the asset). Token "suffix=_x" appends _x to capture filenames.
// Use: ceiling test of a pack under the pipeline config it was authored for (D-065) without touching ours.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoPackVerify
    {
        static double _next;
        static string _rpOverride, _suffix = "";
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_PACKVERIFY.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "PACKVERIFY_DONE.txt");
        static readonly string[] DefaultRoots = {
            "Assets/Fantastic Village Pack", "Assets/Fantastic City Pack", "Assets/Fantastic Nature Pack",
            "Assets/FlatKit", "Assets/Quaternius", "Assets/Vefects", "Assets/msVFX_Free Smoke Effects Pack",
            "Assets/Polyart", "Assets/PolyOne", "Assets/UpDraftArt"
        };

        static AutoPackVerify() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            string body = "";
            try { body = File.ReadAllText(Trigger).Trim(); File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var tokens = body.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
            bool reimport = tokens.RemoveAll(t => t.Equals("reimport", StringComparison.OrdinalIgnoreCase)) > 0;
            string rpOverride = tokens.FirstOrDefault(t => t.StartsWith("rp=", StringComparison.OrdinalIgnoreCase)); tokens.RemoveAll(t => t.StartsWith("rp=", StringComparison.OrdinalIgnoreCase));
            string suffix = tokens.FirstOrDefault(t => t.StartsWith("suffix=", StringComparison.OrdinalIgnoreCase)); tokens.RemoveAll(t => t.StartsWith("suffix=", StringComparison.OrdinalIgnoreCase));
            _rpOverride = rpOverride != null ? rpOverride.Substring(3).Trim() : null; _suffix = suffix != null ? suffix.Substring(7).Trim() : "";
            var roots = (tokens.Count == 0 ? DefaultRoots : tokens.ToArray()).Where(AssetDatabase.IsValidFolder).ToArray();
            var evDir = Path.Combine(@"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\pack-verify", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(evDir);
            var rep = new StringBuilder();
            rep.AppendLine("PACK VERIFY — " + DateTime.Now.ToString("s"));
            try { RpGuard(rep, "start"); } catch (Exception e) { rep.AppendLine("RPGUARD ERROR " + e.Message); }
            try { Environment(rep); } catch (Exception e) { rep.AppendLine("ENV ERROR " + e.Message); }
            int broken = -1;
            try { broken = ZeroPink(roots, rep, reimport); } catch (Exception e) { rep.AppendLine("ZEROPINK ERROR " + e.Message); }
            var caps = new List<string>();
            try { caps = CaptureScenes(roots, evDir, rep); } catch (Exception e) { rep.AppendLine("CAPTURE ERROR " + e.Message); }
            try { Textures(roots, rep); } catch (Exception e) { rep.AppendLine("TEX ERROR " + e.Message); }
            try { RpGuard(rep, "end"); AssetDatabase.SaveAssets(); } catch (Exception e) { rep.AppendLine("RPGUARD ERROR " + e.Message); }
            var verdict = (broken == 0 ? "ZERO-PINK PASS" : broken < 0 ? "ZERO-PINK NOT RUN" : "ZERO-PINK FAIL (" + broken + ")")
                          + " · scenes captured=" + caps.Count;
            rep.AppendLine("VERDICT: " + verdict);
            File.WriteAllText(Path.Combine(evDir, "packverify-report.txt"), rep.ToString());
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoPackVerify] " + verdict);
        }

        const string ProjectRp = "Assets/Settings/PC_RPAsset.asset"; // the project's pipeline (D-118: 35u / 1 cascade)

        // Pins the project pipeline (quality-level override + graphics default) and reports any drift it found.
        static void RpGuard(StringBuilder rep, string when)
        {
            var pc = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(ProjectRp);
            if (pc == null) { rep.AppendLine("RPGUARD " + when + ": " + ProjectRp + " NOT FOUND — no pin"); return; }
            var q = QualitySettings.renderPipeline; var d = GraphicsSettings.defaultRenderPipeline;
            bool drift = q != pc || d != pc;
            if (drift)
            {
                rep.AppendLine("RPGUARD " + when + ": DRIFT quality=" + (q ? q.name : "null") + " default=" + (d ? d.name : "null") + " -> pinned " + pc.name);
                QualitySettings.renderPipeline = pc; GraphicsSettings.defaultRenderPipeline = pc;
            }
            else rep.AppendLine("RPGUARD " + when + ": ok (" + pc.name + ")");
        }

        static void Environment(StringBuilder rep)
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            rep.AppendLine("Unity " + Application.unityVersion + " · RP asset: " + (rp ? rp.name : "Built-in!") + " · colorSpace=" + PlayerSettings.colorSpace);
            var urp = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.render-pipelines.universal");
            rep.AppendLine("URP package: " + (urp != null ? urp.version : "NOT FOUND"));
            // URP asset flags FlatKit Fog/Outline + pack water need (Opaque/Depth texture) — read via SerializedObject, pipeline-agnostic
            if (rp != null)
            {
                var so = new SerializedObject(rp);
                var op = so.FindProperty("m_RequireOpaqueTexture"); var dp = so.FindProperty("m_RequireDepthTexture");
                var ms = so.FindProperty("m_MSAA"); var sc = so.FindProperty("m_ShadowCascadeCount"); var sd = so.FindProperty("m_ShadowDistance");
                rep.AppendLine("URP asset: opaqueTex=" + (op != null ? op.boolValue.ToString() : "?") + " depthTex=" + (dp != null ? dp.boolValue.ToString() : "?")
                               + " msaa=" + (ms != null ? ms.intValue.ToString() : "?") + " cascades=" + (sc != null ? sc.intValue.ToString() : "?")
                               + " shadowDist=" + (sd != null ? sd.floatValue.ToString("0") : "?"));
            }
        }

        static int ZeroPink(string[] roots, StringBuilder rep, bool reimport)
        {
            var guids = AssetDatabase.FindAssets("t:Material", roots);
            var bad = Scan(guids);
            rep.AppendLine("ZERO-PINK: materials=" + guids.Length + " broken=" + bad.Count + " roots=" + string.Join(" | ", roots));
            foreach (var b in bad) rep.AppendLine("  BROKEN " + b.Key + " -> " + b.Value);
            if (reimport && bad.Count > 0)
            {
                // Cheap check (D-874): a material can land on InternalErrorShader when its shader compiled after the material
                // was imported. Force-reimport shader (by the material's stored shader guid) + material, then rescan.
                foreach (var b in bad)
                {
                    try
                    {
                        var txt = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), b.Key));
                        var m = System.Text.RegularExpressions.Regex.Match(txt, @"m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32})");
                        if (m.Success)
                        {
                            var sp = AssetDatabase.GUIDToAssetPath(m.Groups[2].Value);
                            rep.AppendLine("  REIMPORT shader " + (string.IsNullOrEmpty(sp) ? "(guid " + m.Groups[2].Value + " NOT IN PROJECT)" : sp));
                            if (!string.IsNullOrEmpty(sp)) AssetDatabase.ImportAsset(sp, ImportAssetOptions.ForceUpdate);
                        }
                        AssetDatabase.ImportAsset(b.Key, ImportAssetOptions.ForceUpdate);
                    }
                    catch (Exception e) { rep.AppendLine("  REIMPORT ERROR " + b.Key + ": " + e.Message); }
                }
                AssetDatabase.Refresh();
                var bad2 = Scan(bad.Select(b => AssetDatabase.AssetPathToGUID(b.Key)).ToArray());
                rep.AppendLine("ZERO-PINK after reimport: broken=" + bad2.Count + " (of " + bad.Count + " retried)");
                foreach (var b in bad2) rep.AppendLine("  STILL BROKEN " + b.Key + " -> " + b.Value);
                return bad2.Count;
            }
            return bad.Count;
        }

        static List<KeyValuePair<string, string>> Scan(string[] guids)
        {
            var bad = new List<KeyValuePair<string, string>>();
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null) { bad.Add(new KeyValuePair<string, string>(path, "(null)")); continue; }
                if (mat.shader.name == "Hidden/InternalErrorShader" || !mat.shader.isSupported) bad.Add(new KeyValuePair<string, string>(path, mat.shader.name));
            }
            return bad;
        }

        static List<string> CaptureScenes(string[] roots, string evDir, StringBuilder rep)
        {
            var done = new List<string>();
            var startScene = SceneManager_ActivePath();
            var guids = AssetDatabase.FindAssets("t:Scene", roots);
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
                             .Where(p => { var n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                                           return n.Contains("demo") || n.Contains("valley") || n.Contains("wanderer") || n.Contains("showcase") || n.Contains("sample"); })
                             .Distinct().OrderBy(p => p).ToList();
            rep.AppendLine("SCENES found=" + paths.Count);
            bool prevAsync = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false; // TD-PLAYBOOK: cold variants render empty in one-shot captures
            try
            {
                foreach (var p in paths)
                {
                    try
                    {
                        EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                        if (!string.IsNullOrEmpty(_rpOverride))
                        {
                            var ov = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(_rpOverride);
                            if (ov != null) { QualitySettings.renderPipeline = ov; GraphicsSettings.defaultRenderPipeline = ov; }
                            else rep.AppendLine("  RP OVERRIDE NOT FOUND: " + _rpOverride);
                        }
                        var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
                        bool madeCam = false;
                        if (cam == null)
                        {
                            var go = new GameObject("VerifyCam"); cam = go.AddComponent<Camera>(); madeCam = true;
                            var rs = UnityEngine.Object.FindObjectsByType<Renderer>();
                            if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                                float ext = Mathf.Max(b.extents.magnitude, 5f);
                                cam.transform.position = b.center + new Vector3(ext * 0.2f, ext * 0.5f, -ext * 1.2f); cam.transform.LookAt(b.center); cam.farClipPlane = ext * 10f + 1000f; }
                        }
                        const int w = 2560, h = 1440;
                        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt;
                        cam.Render(); cam.Render(); // render twice — TD-PLAYBOOK capture-flake mitigation
                        RenderTexture.active = rt;
                        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                        cam.targetTexture = null; RenderTexture.active = null;
                        var px = tex.GetPixels32(); int magenta = 0; long lum = 0;
                        foreach (var c in px) { if (c.r > 220 && c.b > 220 && c.g < 80) magenta++; lum += c.r + c.g + c.b; }
                        var name = Path.GetFileNameWithoutExtension(p);
                        var file = Path.Combine(evDir, name + _suffix + ".png");
                        File.WriteAllBytes(file, tex.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
                        if (madeCam) UnityEngine.Object.DestroyImmediate(cam.gameObject);
                        var rpNow = GraphicsSettings.currentRenderPipeline;
                        rep.AppendLine("  SCENE " + p + " -> " + Path.GetFileName(file) + " magentaPixels=" + magenta + "/" + (w * h) + " meanLum=" + (lum / (3L * px.Length)) + " cam=" + (madeCam ? "auto" : cam.name) + " rp=" + (rpNow ? rpNow.name : "Built-in!"));
                        done.Add(name);
                    }
                    catch (Exception e) { rep.AppendLine("  SCENE " + p + " ERROR " + e.Message); }
                    finally { try { RpGuard(rep, "after " + Path.GetFileNameWithoutExtension(p)); } catch { } }
                }
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                try { if (!string.IsNullOrEmpty(startScene) && File.Exists(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
                      else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single); } catch { }
            }
            return done;
        }

        static string SceneManager_ActivePath()
        {
            try { return UnityEngine.SceneManagement.SceneManager.GetActiveScene().path; } catch { return null; }
        }

        static void Textures(string[] roots, StringBuilder rep)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", roots);
            long bytes = 0; int count = 0, over2k = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); if (tex == null) continue;
                count++; if (Mathf.Max(tex.width, tex.height) > 2048) over2k++;
                var fi = new FileInfo(Path.Combine(Path.GetDirectoryName(Application.dataPath), path)); if (fi.Exists) bytes += fi.Length;
            }
            rep.AppendLine("TEXTURES: count=" + count + " sourceMB=" + (bytes / (1024 * 1024)) + " over2K=" + over2k + " (native res as authored, VISUAL-QUALITY-BAR)");
        }
    }
}
#endif
