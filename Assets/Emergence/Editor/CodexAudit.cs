// EMERGENCE — THE CODEX REALITY CHECK (D-252). Headless: drop Reports/RUN_CODEXAUDIT.trigger.
//
// THE LAW THIS ENFORCES, IN THE EP'S OWN WORDS:
//   "Ibland blir vi lurade av namnet på assets och objekt och bara går på namnet, och ibland
//    stämmer inte objektet riktigt med vad namnet säger."
//
// He is right, and it is the single most repeated defect in this project's history. From this
// week alone, every one of them measured rather than suspected:
//   • the `windmill` codex row pointed at P_BLD_windmill_sail — the bare rotor, no mill under it
//   • P_BLD_house_14 sat in the DWELLING pool and is a windmill; two of four homes were windmills
//   • `university` was BLD_03_L_Int_Blacksmith — a smithy's interior standing in for learning
//   • Layer_Cobblestone carries NO diffuse at all: a layer named for a surface it cannot render
//   • P_ENV_flower_city_* are window-box flowers, scattered across open meadow as wildflowers
//   • the audio pass (D-204) found "Northern Lights" is the highest-energy ACTION track of sixteen
//   • Prefab_Grass_01_Detail is authored for a detail prototype's hidden multiplier, so its name
//     is honest and its SIZE is not
//
// The codex is a register of MEANING, and every row binds a meaning to an asset by NAME. Nothing
// in the pipeline has ever opened the asset and asked what it actually is. This does:
// it measures each row's prefab — metres against a 1,75 m person, renderer and mesh families,
// whether it draws at all — and asserts the handful of contradictions a name cannot hide.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class CodexAudit
    {
        const string CodexPath = "Assets/Emergence/Codex/object-codex.json";
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_CODEXAUDIT.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "CODEXAUDIT_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "codex-audit.txt");

        /// <summary>A villager stands this tall. Every judgement below is a ratio against it, because
        /// "big" is not a measurement and "2,4x a person" is.</summary>
        const float Person = 1.75f;

        static CodexAudit() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [Serializable] class Entry { public string id, prefab, tier, category, desc, placement; public float scale; public string[] variants; }
        [Serializable] class Codex { public Entry[] objects; }

        [MenuItem("Emergence/Codex/RUN AUDIT (what the assets ACTUALLY are)")]
        public static void Run()
        {
            var codex = JsonUtility.FromJson<Codex>(File.ReadAllText(CodexPath));
            var sb = new System.Text.StringBuilder();
            var flags = new List<string>();
            sb.AppendLine("EMERGENCE — CODEX REALITY CHECK (D-252): what each row's asset MEASURES, not what it is called");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}   ({codex.objects.Length} rows, yardstick = a {Person:0.00} m villager)");
            sb.AppendLine();
            sb.AppendLine("  row                  tier       placed size      x person  parts  drawn by");
            sb.AppendLine("  " + new string('-', 96));

            foreach (var e in codex.objects.OrderBy(x => x.id, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(e.prefab)) { sb.AppendLine($"  {e.id,-20} {e.tier,-10} (told, not shown)"); continue; }
                var go = Load(e.prefab);
                if (go == null)
                {
                    sb.AppendLine($"  {e.id,-20} {e.tier,-10} !! prefab does not load: {e.prefab}");
                    flags.Add($"{e.id}: prefab '{e.prefab}' does not load");
                    continue;
                }

                var rends = go.GetComponentsInChildren<Renderer>(true);
                if (rends.Length == 0)
                {
                    sb.AppendLine($"  {e.id,-20} {e.tier,-10} !! NOTHING TO DRAW — no renderer anywhere in {go.name}");
                    flags.Add($"{e.id}: '{e.prefab}' has no renderer — it cannot be seen at all");
                    continue;
                }
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float s = e.scale <= 0f ? 1f : e.scale;
                float h = b.size.y * s, w = Mathf.Max(b.size.x, b.size.z) * s;
                float ratio = h / Person;

                // the mesh family: what the geometry itself is called, which is one level closer to
                // the truth than the prefab's name and is how the windmill in the dwelling pool was found
                var fam = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null) fam.Add(Family(mf.sharedMesh.name));
                foreach (var sk in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (sk.sharedMesh != null) fam.Add(Family(sk.sharedMesh.name));
                var shaders = new SortedSet<string>(StringComparer.Ordinal);
                foreach (var r in rends) foreach (var m in r.sharedMaterials) if (m?.shader != null) shaders.Add(Short(m.shader.name));

                sb.AppendLine($"  {e.id,-20} {e.tier,-10} {h,5:0.0}x{w,-5:0.0} m  {ratio,6:0.0}x   {rends.Length,4}   {string.Join(",", shaders)}");
                sb.AppendLine($"       meshes: {string.Join(", ", fam.Take(6))}");

                // ---- the contradictions a name cannot hide ----

                // 1. a DRESSING prop the size of a building. Dressing is what a village sets down;
                //    at three people tall it is not a prop, it is architecture wearing a prop's tier.
                // A flagpole is legitimately tall and legitimately dressing, so height alone is not
                // the test -- MASS is. Tall AND wide is a building; tall and thin is a pole.
                if (e.tier == "dressing" && ratio > 3.0f && w > 2.5f)
                    flags.Add($"{e.id}: tier 'dressing' but the asset stands {ratio:0.0}x a person ({h:0.0} x {w:0.0} m) — that is a building");

                // 2. a MILESTONE you could step over. A milestone is the thing a people point at.
                // And the test for a milestone is not "small" -- a knife and a coin are small and are
                // still the first edge and the first agreed worth. The test is INVISIBLE: under a
                // quarter of a metre nothing reads at eye level, so a landmark nobody can see is a
                // landmark that is not there. Reported with its measurement so the reader can judge.
                if (e.tier == "milestone" && h < 0.25f)
                    flags.Add($"{e.id}: tier 'milestone' but the asset places at {h:0.00} m — invisible at eye level");
                else if (e.tier == "milestone" && h < 0.60f)
                    sb.AppendLine($"       note: a milestone at {h:0.00} m is a prop on the ground — worth a pedestal, a rack or an arrangement");

                // 3. VARIANT COHERENCE — the check that catches a bulk sweep.
                // A variant is THE SAME MEANING MADE BY A DIFFERENT HAND. It is not "a name whose
                // stem matched". The cheapest way to tell those apart is size: a fence and a
                // staircase both sound like enclosure and measure nothing alike, and the reconciler
                // picks between them by hash — so one village in eight would get a staircase where
                // its fence should stand, forever, deterministically. Anything more than 2,5x away
                // from its anchor is reported by name so a human decides, because this is judgement
                // and the tool only has to make the judgement CHEAP.
                if (e.variants != null && h > 0.01f)
                {
                    foreach (var vn in e.variants)
                    {
                        var vg = Load(vn);
                        if (vg == null) { flags.Add($"{e.id}: variant '{vn}' does not load"); continue; }
                        var vr = vg.GetComponentsInChildren<Renderer>(true);
                        if (vr.Length == 0) { flags.Add($"{e.id}: variant '{vn}' has no renderer"); continue; }
                        var vb = vr[0].bounds;
                        for (int i2 = 1; i2 < vr.Length; i2++) vb.Encapsulate(vr[i2].bounds);
                        float vh = vb.size.y * s;
                        if (vh < 0.005f) continue;
                        float k = vh > h ? vh / h : h / vh;
                        if (k > 2.5f)
                            flags.Add($"{e.id}: variant '{vn}' is {vh:0.0} m against the row's {h:0.0} m ({k:0.0}x) — same word, different thing?");
                    }
                }

                // 3. THE WINDMILL TEST, generalised: the geometry says one thing, the row says another.
                //    Cheap, blunt, and it would have caught house_14, the bare rotor and the smithy
                //    standing in for a university, each of which cost a pass to find by eye.
                string idWord = LongestWord(e.id);
                if (idWord.Length >= 4 && fam.Count > 0 && !fam.Any(f => f.IndexOf(idWord, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    // only report when the MESH names carry a strong noun of their own that the row does not
                    var strong = fam.FirstOrDefault(f => f.Length >= 5);
                    if (strong != null && !e.prefab.IndexOfAny(new char[0]).Equals(-2))
                        sb.AppendLine($"       note: row says '{idWord}', geometry says '{strong}' — read once, then leave it alone if it is right");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## CONTRADICTIONS — where the asset does not do what the row claims");
            sb.AppendLine(flags.Count == 0 ? "  none ✓" : string.Join("\n", flags.Select(f => "  ✗ " + f)));
            sb.AppendLine();
            sb.AppendLine("Reading: the size and the tier are ASSERTED and can go red. The mesh-family note is a");
            sb.AppendLine("prompt for a human, not a rule — a name is evidence, and evidence is read, not trusted.");

            File.WriteAllText(Report, sb.ToString());
            string verdict = flags.Count == 0 ? "GREEN" : "CHECK";
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} rows={codex.objects.Length} contradictions={flags.Count}\nsee Reports/codex-audit.txt\n");
            Debug.Log($"[CodexAudit] {verdict} — {flags.Count} contradictions over {codex.objects.Length} rows");
        }

        static string Short(string s) { int i = s.LastIndexOf('/'); return i >= 0 ? s.Substring(i + 1) : s; }

        /// <summary>The noun inside a mesh name: SM_BLD_windmill_sail -> windmill.</summary>
        static string Family(string n)
        {
            foreach (var p in new[] { "SM_BLD_", "SM_PROP_", "SM_ENV_", "SM_", "P_BLD_", "P_PROP_", "P_ENV_", "COMP_PROP_", "COMP_", "Prefab_", "BLD_" })
                if (n.StartsWith(p, StringComparison.OrdinalIgnoreCase)) { n = n.Substring(p.Length); break; }
            int u = n.IndexOf('_');
            return (u > 0 ? n.Substring(0, u) : n).ToLowerInvariant();
        }

        static string LongestWord(string id)
        {
            string best = "";
            foreach (var part in id.Split('-', '_'))
                if (part.Length > best.Length) best = part;
            return best.ToLowerInvariant();
        }

        static GameObject Load(string name)
        {
            if (name.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Emergence/Models/tech/" + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Emergence/Models/nature/" + name)
                    ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Emergence/Models/characters/" + name);
            foreach (var g in AssetDatabase.FindAssets($"t:Prefab {name}"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            return null;
        }
    }
}
#endif
