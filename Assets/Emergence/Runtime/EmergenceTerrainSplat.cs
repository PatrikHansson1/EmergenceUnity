// EMERGENCE — runtime terrain splat (D-891). TerrainData alphamaps set by script do NOT survive asset serialization
// into a build (they collapse to the base layer). But runtime SetAlphamaps works. So the dressed splat is baked to a
// .bytes TextAsset (dominant layer per cell — serializes reliably) and re-applied here at Awake, giving the built exe
// its roads / trodden ground / dirt back. Presentation-only.
using UnityEngine;

namespace Emergence.Runtime
{
    [RequireComponent(typeof(Terrain))]
    public class EmergenceTerrainSplat : MonoBehaviour
    {
        public TextAsset splat;
        public Terrain terrain;

        void Awake()
        {
            if (splat == null) return;
            var t = terrain != null ? terrain : GetComponent<Terrain>();
            if (t == null || t.terrainData == null) return;
            var b = splat.bytes;
            if (b.Length < 9) return;
            int W = System.BitConverter.ToInt32(b, 0), H = System.BitConverter.ToInt32(b, 4);
            var td = t.terrainData;
            if (td.alphamapWidth != W || td.alphamapHeight != H) { Debug.LogWarning("[TerrainSplat] size mismatch"); return; }
            int layers = td.alphamapLayers;
            var am = new float[H, W, layers];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int li = b[9 + y * W + x];
                    am[y, x, li < layers ? li : 0] = 1f;
                }
            td.SetAlphamaps(0, 0, am);
        }
    }
}
