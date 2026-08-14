// EMERGENCE — VÅG 7.1 (D-217): THE WINDOWS OBEY THE STATE.
//
// Patrik asked whether you can see inside the houses when something happens in them. The honest
// answer was no — the pack ships shells with no interiors. But the question's INTENT was not
// architecture: it was that the chronicle reports a birth, a feud, a death, and the village stands
// there silent. This is the cheapest half of the answer and it costs no new geometry at all.
//
// The pack already ships M_CLR_yellow_E: a deliberate window-glow material with _EMISSION on and a
// warm emissive colour. We FOUND it during the white-houses hunt, where it was the innocent
// explanation for thirteen renderer slots that carried no base texture (D-212). It has been sitting
// on every house since the first dressing pass, glowing at a constant strength, day and night,
// occupied and abandoned alike — which means it carries no information at all.
//
// A window that is always lit says nothing. A window that goes out says someone died.
//
// THE LAW. A hut's windows are lit when its owner is among the living souls of the applied snapshot,
// and dark when they are not. Nothing else. Not time of day (the living loop holds one light phase),
// not a schedule, not a random flicker — because every one of those would be a decoration, and a
// decoration on a surface the player will read as testimony is a lie by another route. When the
// chronicle says Ask died, Ask's window goes out in the same Apply. That is the whole feature.
//
// Presentation-only (D-078 r4): reads the applied snapshot, writes nothing back, consumes no sim
// RNG. Materials are cached instances — the imported asset is never touched, which is the same
// discipline GlbMaterialSanity uses on the villager bodies (D-215).
using System.Collections.Generic;
using UnityEngine;

namespace Emergence.Runtime
{
    public static class Fas3HearthGlow
    {
        /// <summary>Materials whose name carries this are the pack's window glow.</summary>
        public const string GlowMarker = "yellow_E";

        // The lit value is the pack's own authored emission, kept as authored — we are not
        // art-directing their window, only switching it. Dark is not black: an unlit pane at dusk
        // still catches the sky, and a pure black window reads as a hole punched in the wall.
        public static readonly Color Dark = new Color(0.055f, 0.060f, 0.075f, 1f);

        static readonly Dictionary<Material, Material> LitCache = new Dictionary<Material, Material>();
        static readonly Dictionary<Material, Material> DarkCache = new Dictionary<Material, Material>();

        public static int Lit, Unlit, Panes;

        public static void ResetCounters() { Lit = 0; Unlit = 0; Panes = 0; }

        /// <summary>Set every window pane under this hut. Cheap enough to call per Apply: it only
        /// touches renderers that actually carry the pack's glow material, of which a house has
        /// two or three.</summary>
        public static void SetLit(GameObject hut, bool lit)
        {
            if (hut == null) return;
            var rends = hut.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            for (int r = 0; r < rends.Length; r++)
            {
                var src = rends[r].sharedMaterials;
                if (src == null) continue;
                Material[] dst = null;
                for (int i = 0; i < src.Length; i++)
                {
                    var m = src[i];
                    if (m == null || m.name.IndexOf(GlowMarker, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var want = lit ? LitOf(m) : DarkOf(m);
                    if (want == m) continue;
                    if (dst == null) dst = (Material[])src.Clone();
                    dst[i] = want; any = true; Panes++;
                }
                if (dst != null) rends[r].sharedMaterials = dst;
            }
            if (any || rends.Length > 0) { if (lit) Lit++; else Unlit++; }
        }

        /// <summary>The pack's material as authored — but resolved through the same cache as the dark
        /// one, so switching back and forth never accumulates instances.</summary>
        static Material LitOf(Material m)
        {
            if (!IsDarkened(m)) return m;                     // already the original
            foreach (var kv in DarkCache) if (kv.Value == m) return kv.Key;
            return m;
        }

        static Material DarkOf(Material m)
        {
            if (IsDarkened(m)) return m;
            Material d;
            if (DarkCache.TryGetValue(m, out d) && d != null) return d;
            d = new Material(m) { name = m.name + " (cold)" };
            if (d.HasProperty("_EmissionColor")) d.SetColor("_EmissionColor", Dark);
            if (d.HasProperty("_BaseColor")) d.SetColor("_BaseColor", Dark);
            else if (d.HasProperty("_Color")) d.SetColor("_Color", Dark);
            d.DisableKeyword("_EMISSION");
            d.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            DarkCache[m] = d;
            return d;
        }

        static bool IsDarkened(Material m) => m != null && m.name.EndsWith("(cold)");
    }
}
