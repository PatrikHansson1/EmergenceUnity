// EMERGENCE — Fas 2 step 4 (D-128): BUILD THE ANIMAL ANIMATOR SET (zero purchase).
//
// The owned deer.glb/wolf.glb are STATIC (0 skins, 0 clips — verified in the glTF), but the owned
// Quaternius FBX set is fully rigged (Idle/Idle_2/*HeadLow/Eating/Walk/Gallop). Mirror of the D-123
// villager build:
//   1) duplicate the needed FBX sub-asset clips into standalone .anim assets (loop ON; locomotion
//      clips root-locked in X/Z — the sim owns the animal's position, D-078 r4),
//   2) AnimalAnim-deer.controller (states Idle/Idle2/Sniff/Graze/Walk/Gallop; script crossfades),
//   3) AnimalAnim-wolf.overrideController swaps in the wolf clip set (same state names).
// Attack/Death clips are NOT wired (legibility law: no read without a sim event to justify it).
// Menu: Emergence/Fas2/BUILD ANIMAL ANIMATORS.  Headless: drop Reports/RUN_ANIMALANIM.trigger.
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
    public static class AnimalAnimBuild
    {
        const string FbxDir = "Assets/Quaternius/FBX/";
        const string OutDir = "Assets/Emergence/Fas2/Anim";
        public const string ControllerPath = OutDir + "/AnimalAnim-deer.controller";
        public const string WolfOverridePath = OutDir + "/AnimalAnim-wolf.overrideController";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_ANIMALANIM.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "ANIMALANIM_DONE.txt");

        static AnimalAnimBuild() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Build(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n" + e.StackTrace + "\n"); } catch {} }
        }

        // state -> clip-name fragment per species (importer names clips "AnimalArmature|<Name>")
        static readonly (string state, string deer, string wolf, bool rootLock)[] Clips =
        {
            ("Idle",   "|Idle",         "|Idle",           false),
            ("Idle2",  "|Idle_2",       "|Idle_2",         false),
            ("Sniff",  "|Idle_Headlow", "|Idle_2_HeadLow", false),
            ("Graze",  "|Eating",       "|Eating",         false),
            ("Walk",   "|Walk",         "|Walk",           true),
            ("Gallop", "|Gallop",       "|Gallop",         true),
        };

        [MenuItem("Emergence/Fas2/BUILD ANIMAL ANIMATORS")]
        public static void Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 ANIMAL ANIMATOR BUILD (D-128)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            if (!AssetDatabase.IsValidFolder("Assets/Emergence/Fas2")) AssetDatabase.CreateFolder("Assets/Emergence", "Fas2");
            if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Emergence/Fas2", "Anim");

            int missing = 0;
            var deer = new Dictionary<string, AnimationClip>();
            var wolf = new Dictionary<string, AnimationClip>();
            foreach (var c in Clips)
            {
                deer[c.state] = Dup("Deer", c.deer, $"animal-deer-{c.state.ToLower()}", c.rootLock, sb, ref missing);
                wolf[c.state] = Dup("Wolf", c.wolf, $"animal-wolf-{c.state.ToLower()}", c.rootLock, sb, ref missing);
            }

            // base controller = deer
            AssetDatabase.DeleteAsset(ControllerPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var sm = ctrl.layers[0].stateMachine;
            foreach (var c in Clips)
            {
                var st = sm.AddState(c.state);
                st.motion = deer[c.state];
                if (c.state == "Idle") sm.defaultState = st;
            }
            sb.AppendLine($"controller: {ControllerPath} (Idle/Idle2/Sniff/Graze/Walk/Gallop, default Idle)");

            // wolf override (same state names, wolf clip set)
            var oc = new AnimatorOverrideController(ctrl);
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var c in Clips)
                if (deer[c.state] != null && wolf[c.state] != null)
                    pairs.Add(new(deer[c.state], wolf[c.state]));
            oc.ApplyOverrides(pairs);
            AssetDatabase.DeleteAsset(WolfOverridePath);
            AssetDatabase.CreateAsset(oc, WolfOverridePath);
            AssetDatabase.SaveAssets();
            sb.AppendLine($"override: {WolfOverridePath} ({pairs.Count} clip swaps)");
            sb.AppendLine($"missing source clips: {missing}");

            string verdict = missing == 0 ? "GREEN" : "CHECK (missing source clips)";
            sb.AppendLine($"verdict: {verdict}");
            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/animal-anim-build.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} missing={missing}\nsee Reports/animal-anim-build.txt\n");
            Debug.Log($"[AnimalAnim] built — {verdict}");
        }

        /// <summary>Controller for an animal type — used by the dresser at placement.</summary>
        public static RuntimeAnimatorController ControllerFor(string type) =>
            type == "wolf"
                ? (RuntimeAnimatorController)AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(WolfOverridePath)
                : AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        static AnimationClip Dup(string fbx, string nameFrag, string outName, bool rootLock, StringBuilder sb, ref int missing)
        {
            AnimationClip src = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(FbxDir + fbx + ".fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview") && c.name.EndsWith(nameFrag)) { src = c; break; }
            if (src == null) { sb.AppendLine($"  MISSING clip *{nameFrag} in {fbx}.fbx"); missing++; return null; }

            var dup = UnityEngine.Object.Instantiate(src);
            dup.name = outName;
            var st = AnimationUtility.GetAnimationClipSettings(dup);
            st.loopTime = true; st.loopBlendPositionXZ = false;
            AnimationUtility.SetAnimationClipSettings(dup, st);

            if (rootLock)
            {
                // lock X/Z position on root-level bones (path depth <= 1) — the clip may not move the animal
                foreach (var b in AnimationUtility.GetCurveBindings(dup))
                    if (b.path != null && b.path.Split('/').Length <= 2
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
