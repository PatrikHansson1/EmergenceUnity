// EMERGENCE — D-215: THE VILLAGERS WERE LAMPS, NOT PEOPLE.
//
// Evidence first. Reports/villager-in-scene.png showed a glossy grey-blue blob with boots: no face,
// no folds, no shading. Eight things could have caused that, so instead of guessing we opened the
// source. villager.glb declares, verbatim:
//
//   "emissiveFactor": [1, 1, 1], "emissiveTexture": {"index": 0},
//   "extensions": { "KHR_materials_specular": { "specularColorFactor": [2.0, 2.0, 2.0] } },
//   "pbrMetallicRoughness": { "baseColorTexture": {"index": 1}, "roughnessFactor": 0.41 }
//
// Three faults in three lines. The body re-emits its own albedo at FULL strength, which floods every
// shadow and flattens the mesh to a silhouette. The specular factor is DOUBLE the physical neutral
// of 1.0, which is the sheen. And 0.41 roughness is polished leather, not homespun wool. The texture
// atlas itself is fine — we extracted and looked at it: slate-blue tunics, brown leather, real faces.
// Nothing was missing. The pack simply ships an authoring choice that reads as plastic under URP.
//
// Presentation-only (D-078 r4). This never touches the imported asset: it caches ONE corrected copy
// per source material and hands that to the renderers, so a reimport cannot be poisoned and the sim
// is not consulted. The property names are DISCOVERED from the shader rather than assumed — the
// glTFast Shader Graph does not use _BaseMap/_EmissionColor, and this file exists partly because
// GroundCaptureProbe lied twice by assuming exactly that (D-213).
//
// The tint is the second half of the fix. One atlas dresses every soul, so a village was fifty
// people in the identical slate tunic. Six variants, chosen by hash(agentId) — hash, never sim RNG,
// and six shared materials rather than one instance per agent so batching survives.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emergence.Runtime
{
    public static class GlbMaterialSanity
    {
        public const int TintCount = 6;

        // warm/cool and light/dark, kept narrow: one atlas carries skin as well as cloth, so a wide
        // hue swing would turn faces green. +-8% is enough to break the clone army at eye level.
        static readonly Color[] Tints =
        {
            new Color(1.00f, 1.00f, 1.00f, 1f),   // as authored
            new Color(1.06f, 1.01f, 0.93f, 1f),   // sun-faded
            new Color(0.93f, 0.95f, 1.04f, 1f),   // cold-washed
            new Color(0.90f, 0.88f, 0.86f, 1f),   // older, dirtier
            new Color(1.04f, 0.97f, 0.92f, 1f),   // ochre-dyed
            new Color(0.95f, 0.99f, 0.94f, 1f),   // moss-dyed
        };

        // keyed by the source material itself. The obvious key would be its instance id, but that
        // accessor is deprecated in Unity 6 and this project compiles CS0619 as an ERROR — which
        // is the right setting and stays. The object reference is a better key anyway.
        static readonly Dictionary<Material, Material[]> Cache = new Dictionary<Material, Material[]>();
        public static int Repaired, Inspected, Skipped;
        public static string LastNote = "";

        /// <summary>Hash-based, deterministic, sim-free: the same soul always wears the same cloth.</summary>
        public static int TintFor(int agentId)
        {
            uint h = (uint)agentId * 2654435761u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            return (int)(h % TintCount);
        }

        /// <summary>Correct every glTF material under this root, and dye the cloth by tint slot.</summary>
        public static void Apply(GameObject root, int tint)
        {
            if (root == null) return;
            var rends = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < rends.Length; r++)
            {
                var src = rends[r].sharedMaterials;
                if (src == null || src.Length == 0) continue;
                Material[] dst = null;
                for (int i = 0; i < src.Length; i++)
                {
                    var fixedMat = Repair(src[i], tint);
                    if (fixedMat == src[i]) continue;
                    if (dst == null) dst = (Material[])src.Clone();
                    dst[i] = fixedMat;
                }
                if (dst != null) rends[r].sharedMaterials = dst;
            }
        }

        static Material Repair(Material m, int tint)
        {
            if (m == null) return null;
            var sh = m.shader;
            if (sh == null) return m;
            // scope: only the bought glTF bodies. Polyart's own shaders are authored correctly and
            // must not be touched — 2178 of the scene's 2435 renderers wear them.
            if (sh.name.IndexOf("glTF", System.StringComparison.OrdinalIgnoreCase) < 0) { Skipped++; return m; }

            int slot = Mathf.Abs(tint) % TintCount;
            Material[] byTint;
            if (!Cache.TryGetValue(m, out byTint)) { byTint = new Material[TintCount]; Cache[m] = byTint; }
            if (byTint[slot] != null) return byTint[slot];

            Inspected++;
            var copy = new Material(m) { name = m.name + " (lit, not lamp) t" + tint };
            int n = sh.GetPropertyCount();
            int touched = 0;
            for (int i = 0; i < n; i++)
            {
                string p = sh.GetPropertyName(i);
                var t = sh.GetPropertyType(i);
                string lp = p.ToLowerInvariant();
                bool isCol = t == ShaderPropertyType.Color || t == ShaderPropertyType.Vector;
                bool isNum = t == ShaderPropertyType.Float || t == ShaderPropertyType.Range;

                if (lp.Contains("emissi"))
                {
                    // the whole point: a person is lit, they do not glow
                    if (isCol) { copy.SetColor(p, Color.black); touched++; }
                    else if (isNum) { copy.SetFloat(p, 0f); touched++; }
                }
                else if (lp.Contains("specular"))
                {
                    if (isCol) { copy.SetColor(p, new Color(0.5f, 0.5f, 0.5f, 1f)); touched++; }
                    else if (isNum) { copy.SetFloat(p, Mathf.Min(m.GetFloat(p), 0.5f)); touched++; }
                }
                else if (lp.Contains("roughness"))
                {
                    if (isNum) { copy.SetFloat(p, Mathf.Max(m.GetFloat(p), 0.85f)); touched++; }   // wool, not lacquer
                }
                else if (lp.Contains("smoothness"))
                {
                    if (isNum) { copy.SetFloat(p, Mathf.Min(m.GetFloat(p), 0.12f)); touched++; }
                }
                else if (lp.Contains("metallic"))
                {
                    if (isNum) { copy.SetFloat(p, 0f); touched++; }
                }
                else if (isCol && (lp.Contains("basecolorfactor") || lp == "_basecolor" || lp == "_color"))
                {
                    var c = m.GetColor(p); var k = Tints[slot];
                    copy.SetColor(p, new Color(c.r * k.r, c.g * k.g, c.b * k.b, c.a));
                    touched++;
                }
            }
            copy.DisableKeyword("_EMISSION");
            copy.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            if (touched == 0) { byTint[slot] = m; return m; }   // nothing to correct: keep the original
            Repaired++;
            LastNote = copy.name + ": " + touched + " properties corrected on " + sh.name;
            byTint[slot] = copy;
            return copy;
        }
    }
}
