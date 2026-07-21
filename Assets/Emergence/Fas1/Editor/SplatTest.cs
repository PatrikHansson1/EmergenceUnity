// EMERGENCE — D-115 step 1: EMPIRICAL TERRAIN SPLAT TEST.
//
// The ground/paths use mesh-decal quads (the "plates") on the claim that URP won't render the procedural
// terrain's splat beyond base grass. The terrain-diag proves the alphamap IS stored (paths painted) — but
// never proves it doesn't RENDER. This runner builds the dressed core world with the decal ground OFF
// (WorldDresser.GroundDecals=false) and captures the bare terrain: if the painted dirt paths/fields show,
// the plates are an unnecessary workaround and we switch to painted terrain.
//
// Headless: drop Reports/RUN_SPLATTEST.trigger. Read the two PNGs + the terrain-diag.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class SplatTest
    {
        const string World = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string EvidenceDir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\reconciler";
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_SPLATTEST.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "SPLATTEST_DONE.txt");

        static SplatTest() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Fas1/RUN SPLAT TEST (terrain paths, no decals)")]
        public static void RunMenu() => Run();

        static void Run()
        {
            Directory.CreateDirectory("Reports");
            string season = "spring";
            try { var s = JsonUtility.FromJson<WorldState>(File.ReadAllText(World)); if (!string.IsNullOrEmpty(s.season)) season = s.season; } catch {}

            // BARE TERRAIN — decals off: is the painted splat (dirt paths / field soil) visible on its own?
            WorldDresser.GroundDecals = false;
            WorldDresser.Build(World);
            ApplyLook(season);
            Cap("splattest-1-NODECALS");

            // CONTROL — decals on (the current 'plates' look) for A/B.
            WorldDresser.GroundDecals = true;
            WorldDresser.Build(World);
            ApplyLook(season);
            Cap("splattest-2-DECALS");

            string diag = "";
            try { diag = File.ReadAllText("Logs/terrain-diag.txt"); } catch {}
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss}\nA/B: {EvidenceDir}\\splattest-1-NODECALS.png (terrain splat only) vs splattest-2-DECALS.png (plates)\n\n{diag}\n");
            Debug.Log("[SplatTest] done — compare splattest-1-NODECALS vs splattest-2-DECALS.");
        }

        static void ApplyLook(string season)
        {
            try { EmergenceLightRig.Apply(season, "day"); EmergencePostStack.Apply("day"); } catch (Exception e) { Debug.LogWarning("[SplatTest] look: " + e.Message); }
        }

        static void Cap(string name)
        {
            var cam = Camera.main;
            if (cam == null) return;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = prev; RenderTexture.active = null;
            try { Directory.CreateDirectory(EvidenceDir); File.WriteAllBytes(Path.Combine(EvidenceDir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
#endif
