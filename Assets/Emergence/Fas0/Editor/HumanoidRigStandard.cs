// EMERGENCE — Fas 0 (D-107 A1/A3): the Humanoid rig standard.
//
// A1 locks ONE rig for everything that walks: Unity Humanoid (Mecanim), so a single animation set
// retargets to every character we buy. A3 gates purchases on it: "a character is not bought if it
// cannot map to Humanoid" — that check is exactly this file's validator.
//
// Design choice (conservative, so we do NOT disturb the working dress pipeline): the standard applies
// to a NEW, dedicated intake folder — Assets/Emergence/Characters — where the Fas-2 Väg-1 purchase
// lands. Any model imported there is forced to Humanoid automatically. The current Quaternius
// placeholder villagers under Models/characters are left as-is (Fas 2 decides their fate); the
// validator still REPORTS their rig status for reference.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    public sealed class HumanoidRigStandard : AssetPostprocessor
    {
        public const string CharacterRoot = "Assets/Emergence/Characters";   // the rig-standard intake folder
        public const string LegacyRoot    = "Assets/Emergence/Models/characters"; // placeholders (reported only)

        // Fires on import of any model under the rig-standard folder → force Humanoid.
        void OnPreprocessModel()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.StartsWith(CharacterRoot, StringComparison.OrdinalIgnoreCase)) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
                Debug.Log($"[HumanoidRig] enforced Humanoid on {assetPath}");
            }
        }

        [MenuItem("Emergence/Fas0/Validate Humanoid Rig Standard")]
        public static void Validate()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("EMERGENCE — HUMANOID RIG STANDARD validation (Fas 0, A1/A3)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"standard folder: {CharacterRoot}  (imports here are forced Humanoid)");
            sb.AppendLine();

            int conform = 0, nonconform = 0;
            AppendGroup(sb, "RIG-STANDARD FOLDER (gate: must be Humanoid)", CharacterRoot, true, ref conform, ref nonconform);
            AppendGroup(sb, "LEGACY PLACEHOLDERS (reported only — Fas 2 decides)", LegacyRoot, false, ref conform, ref nonconform);

            sb.Insert(0, $"verdict: {(nonconform == 0 ? "GREEN — all rig-standard characters are Humanoid" : $"RED — {nonconform} non-Humanoid in the standard folder")}\n\n");
            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/humanoid-rig-report.txt", sb.ToString());
            Debug.Log($"[HumanoidRig] validated — conform={conform} nonconform={nonconform}  (see Reports/humanoid-rig-report.txt)");
        }

        static void AppendGroup(System.Text.StringBuilder sb, string title, string root, bool gated, ref int conform, ref int nonconform)
        {
            sb.AppendLine("## " + title);
            if (!AssetDatabase.IsValidFolder(root)) { sb.AppendLine("  (folder empty / not present)"); sb.AppendLine(); return; }
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { root });
            if (guids.Length == 0) { sb.AppendLine("  (no models yet)"); sb.AppendLine(); return; }
            foreach (var g in guids.Distinct())
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                bool human = mi != null && mi.animationType == ModelImporterAnimationType.Human;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var avatar = go != null ? go.GetComponentInChildren<Animator>()?.avatar : null;
                bool validAvatar = avatar != null && avatar.isValid && avatar.isHuman;
                bool ok = human && (validAvatar || avatar == null); // avatar may be null until reimported
                if (gated) { if (ok) conform++; else nonconform++; }
                sb.AppendLine($"  [{(ok ? "ok" : "!!")}] {Path.GetFileName(path)}  animationType={(mi != null ? mi.animationType.ToString() : "?")}  avatar={(validAvatar ? "human-valid" : (avatar == null ? "none" : "not-human"))}");
            }
            sb.AppendLine();
        }
    }
}
#endif
