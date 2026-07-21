// EMERGENCE — Fas 2 step 1 (D-123): BUILD THE SHARED VILLAGER ANIMATOR SET (path P, zero purchase).
//
// All 10 villager GLBs share ONE identical 24-joint skeleton (Hips/Spine/LeftUpLeg… — verified in the
// glTF node data), so a single controller + per-demographic clip overrides drives every body. This builder:
//   1) DUPLICATES each GLB's AnimationClip into standalone .anim assets (loop enabled — the imported
//      sub-asset clips are not loopable and would freeze on the last frame; duplicating also decouples
//      the controller from GLB reimports),
//   2) strips Hips root X/Z translation from walk clips (the sim owns the agent's position — a
//      translating walk cycle would slide agents off their sim-true spot, D-078 r4),
//   3) builds VillagerAnim.controller (states Idle/Walk/Work; script crossfades, no transition graph),
//   4) builds five AnimatorOverrideControllers (adult-f / child / child-f / elder / elder-f) so each
//      demographic keeps its own animation personality (the elder's Thoughtful_Walk matters).
//
// When a Väg-1 quality pack is bought (On Patrik), its humanoid set drops into this same architecture.
// Menu: Emergence/Fas2/BUILD VILLAGER ANIMATORS.  Headless: drop Reports/RUN_FAS2ANIM.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas2AnimatorBuild
    {
        const string CharDir = "Assets/Emergence/Models/characters/";
        const string OutDir  = "Assets/Emergence/Fas2/Anim";
        public const string ControllerPath = OutDir + "/VillagerAnim.controller";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2ANIM.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2ANIM_DONE.txt");

        static Fas2AnimatorBuild() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Build(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n" + e.StackTrace + "\n"); } catch {} }
        }

        // demographic → (idle glb, walk glb, work glb or null)
        static readonly (string key, string idle, string walk, string work)[] Demographics =
        {
            ("adult",   "villager",         "villager-walk",         "villager-work"),
            ("adult-f", "villager-f",       "villager-f-walk",       "villager-f-work"),
            ("child",   "villager-child",   "villager-child-walk",   null),
            ("child-f", "villager-child-f", "villager-child-f-walk", null),
            ("elder",   "villager-elder",   "villager-elder-walk",   null),
            ("elder-f", "villager-elder-f", "villager-elder-f-walk", null),
        };

        [MenuItem("Emergence/Fas2/BUILD VILLAGER ANIMATORS")]
        public static void Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 VILLAGER ANIMATOR BUILD (D-123)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            if (!AssetDatabase.IsValidFolder("Assets/Emergence/Fas2")) AssetDatabase.CreateFolder("Assets/Emergence", "Fas2");
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Emergence/Fas2", "Anim");

            // 1) duplicate clips (loop on; walk clips root-locked)
            var clips = new Dictionary<string, AnimationClip>();
            int missing = 0;
            foreach (var d in Demographics)
            {
                clips[d.key + ".idle"] = Dup(d.idle, d.key + "-idle", true, sb, ref missing);
                clips[d.key + ".walk"] = Dup(d.walk, d.key + "-walk", true, sb, ref missing, stripRootXZ: true);
                if (d.work != null) clips[d.key + ".work"] = Dup(d.work, d.key + "-work", true, sb, ref missing);
            }

            // 2) base controller from the adult male set
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = ctrl.layers[0].stateMachine;
            var stIdle = sm.AddState("Idle"); stIdle.motion = clips["adult.idle"];
            var stWalk = sm.AddState("Walk"); stWalk.motion = clips["adult.walk"];
            var stWork = sm.AddState("Work"); stWork.motion = clips["adult.work"];
            sm.defaultState = stIdle;
            sb.AppendLine($"controller: {ControllerPath} (Idle/Walk/Work, default Idle, crossfade from script)");

            // 3) per-demographic overrides (adult male IS the base controller)
            int overrides = 0;
            foreach (var d in Demographics)
            {
                if (d.key == "adult") continue;
                var oc = new AnimatorOverrideController(ctrl);
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>
                {
                    new(clips["adult.idle"], clips[d.key + ".idle"]),
                    new(clips["adult.walk"], clips[d.key + ".walk"]),
                    // bands without a work clip fall back to their own idle (guarded by canWork anyway)
                    new(clips["adult.work"], clips.TryGetValue(d.key + ".work", out var w) ? w : clips[d.key + ".idle"]),
                };
                oc.ApplyOverrides(pairs);
                string p = $"{OutDir}/Villager-{d.key}.overrideController";
                AssetDatabase.DeleteAsset(p);
                AssetDatabase.CreateAsset(oc, p);
                overrides++;
            }
            AssetDatabase.SaveAssets();
            sb.AppendLine($"overrides: {overrides} (adult-f/child/child-f/elder/elder-f)");
            sb.AppendLine($"clips duplicated: {clips.Count} (loop ON, walk Hips X/Z root-locked)  missing sources: {missing}");

            string verdict = missing == 0 ? "GREEN" : "CHECK (missing source clips)";
            sb.AppendLine($"verdict: {verdict}");
            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/fas2-anim-build.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} clips={clips.Count} overrides={overrides}\nsee Reports/fas2-anim-build.txt\n");
            Debug.Log($"[Fas2Anim] built — {verdict} clips={clips.Count} overrides={overrides}");
        }

        /// <summary>Controller for a band/sex — used by the dresser at placement.</summary>
        public static RuntimeAnimatorController ControllerFor(string band, bool female)
        {
            string key = band == "adult" ? (female ? "adult-f" : "adult") : band + (female ? "-f" : "");
            if (key == "adult") return AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            return AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{OutDir}/Villager-{key}.overrideController");
        }

        static AnimationClip Dup(string glbName, string outName, bool loop, StringBuilder sb, ref int missing, bool stripRootXZ = false)
        {
            AnimationClip src = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CharDir + glbName + ".glb"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview")) { src = c; break; }
            if (src == null) { sb.AppendLine($"  MISSING clip in {glbName}.glb"); missing++; return null; }

            var dup = UnityEngine.Object.Instantiate(src);
            dup.name = outName;
            var st = AnimationUtility.GetAnimationClipSettings(dup);
            st.loopTime = loop; st.loopBlendPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(dup, st);

            if (stripRootXZ)
            {
                // remove Hips localPosition X/Z curves — walk stays on the sim's spot (keep Y for the bob)
                foreach (var b in AnimationUtility.GetCurveBindings(dup))
                    if (b.propertyName != null && b.path != null && b.path.EndsWith("Hips")
                        && (b.propertyName == "m_LocalPosition.x" || b.propertyName == "m_LocalPosition.z"))
                    {
                        var curve = AnimationUtility.GetEditorCurve(dup, b);
                        float v = curve != null && curve.keys.Length > 0 ? curve.keys[0].value : 0f;
                        AnimationUtility.SetEditorCurve(dup, b, AnimationCurve.Constant(0, Mathf.Max(0.01f, dup.length), v));
                    }
            }

            string p = $"{OutDir}/{outName}.anim";
            AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(dup, p);
            return dup;
        }
    }
}
#endif
