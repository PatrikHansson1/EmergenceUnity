// EMERGENCE — CODEX COVERAGE TOOL (D-121, per OBJECT-CODEX-SPEC §3: the anti-orphan guarantee).
// Scans every prefab/model in Assets/** and diffs against object-codex.json:
//   • DANGLING  — a codex entry whose prefab name matches no asset (a broken pointer). Must be 0.
//   • ORPHAN    — an asset NOBODY claims: not the codex, not a runtime placer, not a building kit.
//   • COVERAGE% — of the codex's OWN domain, plus a named owner for everything set aside.
// Makes an un-indexed asset impossible to forget. Read-only. Headless: drop Reports/RUN_COVERAGE.trigger.
//
// D-244 — WHY THE 1339 WAS FOUR BACKLOGS WEARING ONE NUMBER.
// The tool had one question ("is this asset in the codex?") and therefore one answer for four
// different situations. It reported the fourteen houses the game raises in EVERY village as
// un-indexed content; it reported the meadow's trees, which Fas3NatureScatter places by name, as
// un-indexed content; it reported particle effects, which no village will ever "place", the same
// way. A number that calls the placed and the unplaceable by one name cannot tell anyone what to
// do next, which is exactly what a backlog is for.
// So the question is now: WHO CLAIMS THIS ASSET? The codex, a named runtime placer (the asset
// catalog is that placer's own list, so the claim is evidence and not an assertion), the building
// kit, the nature scatter, the agent bodies — or nobody. Only nobody is an orphan.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;   // D-137: world model moved to Runtime

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class CodexCoverage
    {
        const string CodexPath = "Assets/Emergence/Codex/object-codex.json";
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_COVERAGE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "COVERAGE_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "codex-coverage.txt");

        static CodexCoverage() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [Serializable] class Entry { public string id, prefab, tier; public Part[] arrangement; public string[] variants; }
        [Serializable] class Part  { public string prefab; }
        [Serializable] class Codex { public Entry[] objects; }

        // Every domain that is NOT the codex names the system that places it. A set-aside without an
        // owner is just a smaller number; a set-aside with an owner is a division of labour. If this
        // table ever has to guess, the guess belongs HERE, in the open, and not spread through a report.
        const string OwnKit    = "the building kit — assembled into a whole, never placed alone (arrangement templates)";
        const string OwnPlaced = "a runtime placer, by name — EmergenceAssetCatalog is that placer's own list";
        const string OwnFx     = "the effect systems (FireReconciler, expression) — a particle is not a thing a village sets down";
        const string OwnNature = "Fas3NatureScatter / FoliageInstancer — the world's own growth, not a village's making";
        const string OwnAgent  = "AgentReconciler / the animal intake — bodies, not objects";

        [MenuItem("Emergence/Codex/RUN COVERAGE (orphans/dangling)")]
        public static void Run()
        {
            var codex = JsonUtility.FromJson<Codex>(File.ReadAllText(CodexPath));
            // referenced prefab base-names (strip .glb; empty = told-not-shown, ignored)
            var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toldNotShown = 0;
            // the stems the codex already carries a meaning for — the backlog is split against these
            var codexStems = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in codex.objects)
            {
                // D-242: an arrangement's parts are REFERENCED assets too. Counting only the anchor
                // would let a whole quietly point at a prefab that does not exist and still report
                // 0 dangling — the anti-orphan guarantee has to cover every name the codex can place.
                if (e.variants != null)
                    foreach (var vn in e.variants)
                        if (!string.IsNullOrWhiteSpace(vn)) { referenced.Add(Base(vn)); AddStem(codexStems, vn, e.id); }
                if (e.arrangement != null)
                    foreach (var pt in e.arrangement)
                        if (pt != null && !string.IsNullOrWhiteSpace(pt.prefab))
                            referenced.Add(Base(pt.prefab));
                if (string.IsNullOrWhiteSpace(e.prefab)) { toldNotShown++; continue; }
                referenced.Add(Base(e.prefab));
                AddStem(codexStems, e.prefab, e.id);
            }

            // the names a runtime placer asks for. Not a folder rule and not my opinion: this asset is
            // reached at runtime because some placer put its name in the catalog build's own list.
            var placedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cat = AssetDatabase.LoadAssetAtPath<EmergenceAssetCatalog>(
                "Assets/Emergence/Resources/" + EmergenceAssetCatalog.ResourcesName + ".asset");
            if (cat != null)
            {
                foreach (var p in cat.prefabs) if (!string.IsNullOrWhiteSpace(p.name)) placedNames.Add(Base(p.name));
                foreach (var m in cat.mossPrefabs) if (m != null) placedNames.Add(m.name);
            }

            var guids = AssetDatabase.FindAssets("t:GameObject");
            var assetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var domain = new Dictionary<string, int>();
            var perPack = new Dictionary<string, (int total, int refd)>();
            var orphans = new List<string>();
            int codexRefd = 0, codexTotal = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (!path.StartsWith("Assets/")) continue;
                if (!(path.EndsWith(".prefab") || path.EndsWith(".glb") || path.EndsWith(".fbx"))) continue;
                var name = Path.GetFileNameWithoutExtension(path);
                assetNames.Add(name);

                string dom;
                bool isRef = referenced.Contains(name);
                if (isRef) dom = "codex";
                else if (IsKit(name, path)) dom = "kit";
                else if (placedNames.Contains(name)) dom = "placed";
                else if (IsFx(path)) dom = "fx";
                else if (IsNature(path)) dom = "nature";
                else if (IsAgent(path)) dom = "agent";
                else dom = "orphan";
                domain[dom] = domain.TryGetValue(dom, out var dc) ? dc + 1 : 1;
                if (dom != "codex" && dom != "orphan") continue;

                // the codex's own domain: what it indexes, plus what nobody else will ever place
                codexTotal++; if (isRef) codexRefd++;
                var parts = path.Split('/');
                var pack = parts.Length > 1 ? parts[1] : "(root)";
                var cur = perPack.TryGetValue(pack, out var v) ? v : (total: 0, refd: 0);
                perPack[pack] = (cur.total + 1, cur.refd + (isRef ? 1 : 0));
                if (!isRef) orphans.Add($"{pack}/{name}");
            }

            // dangling = referenced names that match no asset
            var dangling = referenced.Where(r => !assetNames.Contains(r)).OrderBy(x => x).ToList();

            // THE BACKLOG, GROUPED — the half of the job a flat list of 445 names cannot do. An orphan
            // whose meaning the codex already carries is a VARIANT: it needs a name in an existing row
            // and no new gate at all. An orphan whose meaning is new needs an authored row, a discovery
            // that unlocks it and a sentence. Two piles, two kinds of work, two different afternoons.
            var variantOf = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var newMeaning = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in orphans)
            {
                var n = o.Substring(o.IndexOf('/') + 1);
                var s = Stem(n);
                var bucket = codexStems.ContainsKey(s) ? variantOf : newMeaning;
                if (!bucket.TryGetValue(s, out var l)) bucket[s] = l = new List<string>();
                l.Add(n);
            }
            int nVariant = variantOf.Values.Sum(l => l.Count), nNew = newMeaning.Values.Sum(l => l.Count);

            int allAssets = domain.Values.Sum();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("EMERGENCE — CODEX COVERAGE (D-121, anti-orphan; D-244, one question per domain)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"codex entries: {codex.objects.Length}  ({referenced.Count} unique prefabs referenced, {toldNotShown} told-not-shown)");
            sb.AppendLine($"project assets (prefab/glb/fbx): {allAssets}");
            sb.AppendLine($"CODEX COVERAGE: {codexRefd}/{codexTotal} = {(codexTotal > 0 ? 100f * codexRefd / codexTotal : 0):0.0}%  |  ORPHANS: {orphans.Count}  |  DANGLING: {dangling.Count}");
            sb.AppendLine();
            sb.AppendLine("## WHO CLAIMS WHAT (every asset is claimed by exactly one, or it is an orphan)");
            sb.AppendLine($"  {Get(domain,"codex"),5}  codex      — indexed: a village places it when its discovery is true");
            sb.AppendLine($"  {Get(domain,"orphan"),5}  ORPHAN     — nobody. THIS is the backlog, and it is the only one.");
            sb.AppendLine($"  {Get(domain,"kit"),5}  kit        — {OwnKit}");
            sb.AppendLine($"  {Get(domain,"placed"),5}  placed     — {OwnPlaced}");
            sb.AppendLine($"  {Get(domain,"fx"),5}  fx         — {OwnFx}");
            sb.AppendLine($"  {Get(domain,"nature"),5}  nature     — {OwnNature}");
            sb.AppendLine($"  {Get(domain,"agent"),5}  agent      — {OwnAgent}");
            if (cat == null)
                sb.AppendLine("  !! EmergenceAssetCatalog did not load — 'placed' is 0 and its assets fell into ORPHAN. Run RUN_CATALOG.");
            sb.AppendLine();
            sb.AppendLine($"## DANGLING (codex → missing asset) — MUST be 0");
            sb.AppendLine(dangling.Count == 0 ? "  none ✓" : string.Join("\n", dangling.Select(d => "  ✗ " + d)));
            sb.AppendLine();
            sb.AppendLine("## CODEX-DOMAIN COVERAGE PER PACK (referenced / total)");
            foreach (var kv in perPack.OrderByDescending(k => k.Value.total))
                sb.AppendLine($"  {kv.Value.refd,4}/{kv.Value.total,-5} {(kv.Value.total > 0 ? 100f * kv.Value.refd / kv.Value.total : 0),5:0.0}%  {kv.Key}");
            sb.AppendLine();
            sb.AppendLine($"## THE BACKLOG, GROUPED — {orphans.Count} orphans = {nVariant} variants of {variantOf.Count} meanings the codex HAS");
            sb.AppendLine($"##                          + {nNew} assets carrying {newMeaning.Count} meanings it does NOT");
            sb.AppendLine();
            sb.AppendLine("### A — VARIANT of an indexed meaning (name it in that row's variants[]; no new gate)");
            foreach (var kv in variantOf.OrderByDescending(k => k.Value.Count))
                sb.AppendLine($"  {kv.Value.Count,4}  {kv.Key,-24} → codex [{string.Join(", ", codexStems[kv.Key])}]   {Sample(kv.Value)}");
            sb.AppendLine();
            sb.AppendLine("### B — NEW MEANING (needs an authored row: a discovery that unlocks it, and a sentence)");
            foreach (var kv in newMeaning.OrderByDescending(k => k.Value.Count).ThenBy(k => k.Key))
                sb.AppendLine($"  {kv.Value.Count,4}  {kv.Key,-24} {Sample(kv.Value)}");
            sb.AppendLine();
            sb.AppendLine("Reading: dangling MUST be 0 (broken codex pointers). Pile A is an afternoon of names;");
            sb.AppendLine("pile B is authoring, and every row in it must earn its place by a discovery — never by density.");

            File.WriteAllText(Report, sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} coverage={(codexTotal>0?100f*codexRefd/codexTotal:0):0.0}% orphans={orphans.Count} (variant={nVariant} new={nNew}) dangling={dangling.Count}\nsee Reports/codex-coverage.txt\n");
            Debug.Log($"[CodexCoverage] {codexRefd}/{codexTotal} = {(codexTotal>0?100f*codexRefd/codexTotal:0):0.0}% codex domain | orphans={orphans.Count} dangling={dangling.Count}");
        }

        static int Get(Dictionary<string,int> d, string k) => d.TryGetValue(k, out var v) ? v : 0;
        static string Base(string p) => Path.GetFileNameWithoutExtension(p);
        static string Sample(List<string> l) { l.Sort(StringComparer.OrdinalIgnoreCase); return "e.g. " + string.Join(", ", l.Take(3)); }
        static void AddStem(Dictionary<string, SortedSet<string>> d, string prefab, string id)
        {
            var s = Stem(Base(prefab));
            if (!d.TryGetValue(s, out var set)) d[s] = set = new SortedSet<string>(StringComparer.Ordinal);
            set.Add(id);
        }

        // The meaning under a name. The packs write the same thing five ways — P_PROP_crate_01,
        // COMP_PROP_crate_city_03 — and the difference between those two is a hand, not a meaning.
        // Strip the pack's grammar and what is left is what the thing IS. Deliberately blunt: it is a
        // grouping aid for a human reader, never a gate, and it is allowed to be wrong out loud.
        static readonly Regex RxPrefix = new Regex(@"^(COMP_PROP_|P_PROP_|P_BLD_|P_ENV_|COMP_|SM_|P_|Prefab_)", RegexOptions.IgnoreCase);
        static readonly Regex RxPackSuffix = new Regex(@"(_city|_v)(?=(_\d+)?$)", RegexOptions.IgnoreCase);
        static readonly Regex RxNum = new Regex(@"[_ ]\d+$");
        static readonly Regex RxSize = new Regex(@"_(deco|small|big|large|medium|old|new)$", RegexOptions.IgnoreCase);
        public static string Stem(string n)
        {
            n = Path.GetFileNameWithoutExtension(n);
            n = RxPrefix.Replace(n, "");
            n = RxPackSuffix.Replace(n, "");
            n = RxNum.Replace(n, "");
            n = RxSize.Replace(n, "");
            return n.ToLowerInvariant();
        }

        // A "kit" asset exists so a WHOLE can be assembled, or is a state of a whole we already index.
        // Naming is the packs' own: _Ext_/_Int_ are building modules, SM_ is a raw static mesh,
        // COMP_Base is a composition base, _LOD is a level of detail, and -walk/-work are animation
        // variants of a villager the codex already knows. The FOLDER rule was the missing half: the
        // City Pack keeps its modular kit in Modular/ and the Village Pack in buildings_modules/, and
        // 674 wall, window, roof and stair modules were being counted as content a village forgot to
        // build. They become codex rows only through arrangement templates.
        static bool IsKit(string n, string path)
        {
            // FIRST, and the order is the point: the level-3 test has to run BEFORE the name rules.
            // A complete building is called BLD_03_16x8_Ext_01 or BLD_03_L_Int_Blacksmith -- so the
            // _Ext_/_Int_ rules, written for wall modules, swallowed all forty-three of them and the
            // count did not move a single asset when the path test was added below them. The number
            // refusing to move named the fault again; the fix is one line in the right place.
            if (path.IndexOf("03_BLD_COMPLETE", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (n.StartsWith("SM_", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.StartsWith("COMP_Base", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.IndexOf("_LOD", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("_Ext_", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("_Int_", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.EndsWith("-walk", StringComparison.OrdinalIgnoreCase)) return true;
            if (n.EndsWith("-work", StringComparison.OrdinalIgnoreCase)) return true;
            var p = path.ToLowerInvariant();
            // D-251 — THE VENDOR'S OWN THREE LEVELS, instead of our one word for all of them. From
            // "Documentation - FANTASTIC City Pack", chapter "Prefabs and Nested Prefabs":
            //     "level 1: Parts - individual modular elements, baseline prefabs + collision
            //      level 2: Comps - compositions of individual parts
            //      level 3: Complete Buildings - combinations of Comps and Parts to a full building"
            // Levels 1 and 2 are the kit. LEVEL 3 IS NOT: a complete building is the most placeable
            // thing in the entire pack, and sweeping the whole of Modular/ into "kit" hid FORTY-THREE
            // finished buildings -- 26 exteriors, 7 interiors and 10 themed by trade -- from the very
            // backlog that exists to make un-indexed content impossible to forget. The codex already
            // names three of them (temple, manor, university), which is the proof they belong here.
            return p.Contains("/modular/") || p.Contains("buildings_modules") || p.Contains("/collision/");
        }

        // Measured, not guessed: the asset is opened and asked whether it is a particle system.
        // Only the few hundred that reach this test are loaded, so the pass stays under a second.
        static bool IsFx(string path)
        {
            var p = path.ToLowerInvariant();
            if (p.Contains("vfx") || p.Contains("/effects/") || p.Contains("/particles/") || p.Contains("/fx/"))
                return true;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return go != null && go.GetComponentInChildren<ParticleSystem>(true) != null;
        }

        // Deliberately NARROW. The meadow pack keeps barrels, benches and a tent in the same Props/
        // folder as its tree stumps, and a barrel is a thing a village makes — sweeping that folder
        // into "nature" would have hidden two dozen placeable props behind the word. Foliage, rock,
        // grass, tree, water and terrain folders only; everything else stays visible as backlog.
        static bool IsNature(string path)
        {
            var p = path.ToLowerInvariant();
            return p.Contains("/foliage/") || p.Contains("/grass/") || p.Contains("/trees/")
                || p.Contains("/rocks/") || p.Contains("/water/") || p.Contains("/water urp/")
                || p.Contains("/terrain/");
        }

        static bool IsAgent(string path)
        {
            var p = path.ToLowerInvariant();
            return p.Contains("/models/characters/") || p.Contains("/models/nature/") || p.Contains("/quaternius/");
        }
    }
}
#endif
