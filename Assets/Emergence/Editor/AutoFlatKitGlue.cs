// EMERGENCE — FlatKit glue-stack installer (DEMO-BYGGPLAN steg 4, VISUELL-TOTALPLAN §4a, D-875).
// Drop Reports/RUN_FLATKITGLUE.trigger. Idempotent. Does:
//  1. creates OUR versioned settings assets (L4 "light = versioned file"):
//       Assets/Emergence/Rendering/EmergenceFog.asset      (FlatKit.FogSettings)
//       Assets/Emergence/Rendering/EmergenceOutline.asset  (FlatKit.OutlineSettings)
//     — restrained glue baseline, NOT the FlatKit demo palette; tuned by eye later (D-008/D-064).
//  2. adds FlatKitFog + FlatKitOutline as Renderer Features on OUR renderer (Assets/Settings/PC_Renderer.asset),
//     exactly as the FlatKit manual prescribes ("do not swap the SRP asset — add the features to your renderer").
//  3. asserts PC_RPAsset flags the features need (Opaque + Depth texture ON, MSAA off → screen-space AA path).
// FlatKit types are resolved by REFLECTION on purpose: Assets/FlatKit/[Render Pipeline] URP/ is gitignored
// (D-874 F13), so this file must compile on a clone without the pack. Writes Reports/FLATKITGLUE_DONE.txt.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoFlatKitGlue
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FLATKITGLUE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FLATKITGLUE_DONE.txt");
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string RpPath       = "Assets/Settings/PC_RPAsset.asset";
        const string Folder       = "Assets/Emergence/Rendering";
        const string FogAsset     = Folder + "/EmergenceFog.asset";
        const string OutlineAsset = Folder + "/EmergenceOutline.asset";

        static AutoFlatKitGlue() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            if (!File.Exists(Trigger)) return;
            try { File.Delete(Trigger); } catch { }
            Directory.CreateDirectory(Path.GetDirectoryName(Done));
            File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            var rep = new StringBuilder();
            string verdict;
            try { verdict = Run(rep); }
            catch (Exception e) { rep.AppendLine("ERROR " + e); verdict = "ERROR " + e.Message; }
            File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " " + verdict + "\n" + rep);
            Debug.Log("[AutoFlatKitGlue] " + verdict);
        }

        static Type FindType(string fullName)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = a.GetType(fullName, false); } catch { }
                if (t != null) return t;
            }
            return null;
        }

        static string Run(StringBuilder rep)
        {
            var tFog = FindType("FlatKit.FlatKitFog"); var tOut = FindType("FlatKit.FlatKitOutline");
            var tFogS = FindType("FlatKit.FogSettings"); var tOutS = FindType("FlatKit.OutlineSettings");
            rep.AppendLine("types: FlatKitFog=" + (tFog != null) + " FlatKitOutline=" + (tOut != null) + " FogSettings=" + (tFogS != null) + " OutlineSettings=" + (tOutS != null)
                           + (tFog != null ? " asm=" + tFog.Assembly.GetName().Name : ""));
            if (tFog == null || tOut == null || tFogS == null || tOutS == null) return "FAIL: FlatKit URP render features not in project";

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RpPath);
            if (renderer == null || rp == null) return "FAIL: PC_Renderer/PC_RPAsset not found";

            // 1. our settings assets
            if (!AssetDatabase.IsValidFolder(Folder)) { AssetDatabase.CreateFolder("Assets/Emergence", "Rendering"); rep.AppendLine("created " + Folder); }
            var fogS = LoadOrCreate(tFogS, FogAsset, rep, so =>
            {
                B(so, "useDistance", true);
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(0.78f, 0.74f, 0.66f), 0f), new GradientColorKey(new Color(0.62f, 0.76f, 0.88f), 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.55f, 1f) });
                { var p = so.FindProperty("distanceGradient"); if (p != null) p.gradientValue = g; else Debug.LogWarning("[AutoFlatKitGlue] no distanceGradient"); }
                F(so, "near", 45f);
                F(so, "far", 260f);
                F(so, "distanceFogIntensity", 0.6f);
                B(so, "useHeight", false);     // ONE dis-layer (pack Distance Fade is the other — coordinated later)
                F(so, "distanceHeightBlend", 0f);
                B(so, "applyInSceneView", true);
            });
            var outS = LoadOrCreate(tOutS, OutlineAsset, rep, so =>
            {
                C(so, "edgeColor", new Color(0.13f, 0.10f, 0.08f, 0.55f)); // warm-dark ink, never pure black
                I(so, "thickness", 1);
                B(so, "resolutionInvariant", true);
                B(so, "fadeWithDistance", true);
                F(so, "fadeRangeStart", 15f);
                F(so, "fadeRangeEnd", 70f);
                B(so, "useDepth", true);
                F(so, "minDepthThreshold", 0.2f);
                F(so, "maxDepthThreshold", 0.6f);
                B(so, "useNormals", false);
                B(so, "useColor", false);
                B(so, "applyInSceneView", true);
            });

            // 2. renderer features on OUR renderer (idempotent by type)
            bool addedAny = false;
            addedAny |= EnsureFeature(renderer, tFog, "Emergence FlatKit Fog", fogS, rep);
            addedAny |= EnsureFeature(renderer, tOut, "Emergence FlatKit Outline", outS, rep);
            if (addedAny) { EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssets(); }

            // 3. RP asset flags
            var rso = new SerializedObject(rp);
            bool op = rso.FindProperty("m_RequireOpaqueTexture").boolValue, dp = rso.FindProperty("m_RequireDepthTexture").boolValue;
            int msaa = rso.FindProperty("m_MSAA").intValue;
            rep.AppendLine("PC_RPAsset: opaqueTex=" + op + " depthTex=" + dp + " msaa=" + msaa + (msaa > 1 ? "  (manual §Fog: use screen-space AA, not MSAA)" : ""));
            if (!op || !dp) return "FAIL: PC_RPAsset needs Opaque+Depth texture ON";

            // report final feature list
            var so2 = new SerializedObject(renderer); var list = so2.FindProperty("m_RendererFeatures");
            rep.AppendLine("PC_Renderer features (" + list.arraySize + "):");
            for (int i = 0; i < list.arraySize; i++)
            {
                var f = list.GetArrayElementAtIndex(i).objectReferenceValue;
                rep.AppendLine("  " + i + ": " + (f ? f.name + " <" + f.GetType().FullName + ">" : "(null)"));
            }
            return "OK fog+outline on PC_Renderer · settings " + FogAsset + " / " + OutlineAsset;
        }

        static void B(SerializedObject so, string n, bool v)  { var p = so.FindProperty(n); if (p != null) p.boolValue = v;  else Debug.LogWarning("[AutoFlatKitGlue] no field " + n); }
        static void F(SerializedObject so, string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; else Debug.LogWarning("[AutoFlatKitGlue] no field " + n); }
        static void I(SerializedObject so, string n, int v)   { var p = so.FindProperty(n); if (p != null) p.intValue = v;   else Debug.LogWarning("[AutoFlatKitGlue] no field " + n); }
        static void C(SerializedObject so, string n, Color v) { var p = so.FindProperty(n); if (p != null) p.colorValue = v; else Debug.LogWarning("[AutoFlatKitGlue] no field " + n); }

        static ScriptableObject LoadOrCreate(Type t, string path, StringBuilder rep, Action<SerializedObject> init)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null && t.IsInstanceOfType(existing)) { rep.AppendLine("kept " + path); return existing; }
            var so = ScriptableObject.CreateInstance(t);
            var ser = new SerializedObject(so); init(ser); ser.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(so, path);
            rep.AppendLine("created " + path);
            return so;
        }

        static bool EnsureFeature(ScriptableRendererData renderer, Type featureType, string name, ScriptableObject settings, StringBuilder rep)
        {
            var so = new SerializedObject(renderer);
            var list = so.FindProperty("m_RendererFeatures"); var map = so.FindProperty("m_RendererFeatureMap");
            for (int i = 0; i < list.arraySize; i++)
            {
                var f = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (f != null && featureType.IsInstanceOfType(f))
                {
                    var fs = new SerializedObject(f); var sp = fs.FindProperty("settings");
                    if (sp != null && sp.objectReferenceValue != settings) { sp.objectReferenceValue = settings; fs.ApplyModifiedProperties(); rep.AppendLine("relinked settings on existing " + f.name); return true; }
                    rep.AppendLine("already present: " + f.name); return false;
                }
            }
            var feature = ScriptableObject.CreateInstance(featureType) as ScriptableRendererFeature;
            feature.name = name;
            var fso = new SerializedObject(feature); var settingsProp = fso.FindProperty("settings");
            if (settingsProp != null) settingsProp.objectReferenceValue = settings;
            fso.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
            int n = list.arraySize;
            list.arraySize = n + 1; list.GetArrayElementAtIndex(n).objectReferenceValue = feature;
            map.arraySize = n + 1;  map.GetArrayElementAtIndex(n).longValue = localId;
            so.ApplyModifiedProperties();
            rep.AppendLine("added " + name + " (localId " + localId + ")");
            return true;
        }
    }
}
#endif
