// EMERGENCE — VÅG 1.1 EVIDENCE: DOES THE LIVING WORLD HAVE GROUND?
//
// Patrik's field report (2026-08-14): the environment felt wrong — flat, angular, props sinking,
// lakes odd, "it doesn't feel like we applied the environment we bought". The studio looked at real
// screenshots and found the living world at year 120 was a flat green plane with one villager on
// it, while the store rig's shots looked like a game. Root cause (D-209): the entire WorldDresser
// sits behind #if UNITY_EDITOR, so it could never run in a build, and the living loop never called
// it at all.
//
// This probe is the acceptance test for the fix. It boots the SAME self-composing opening the
// player gets (Fas3Onboarding — no dresser, no capture rig, no editor-only anything), lets it run,
// and asks the ground three questions:
//
//   1. Did the terrain get raised AT ALL, from the living loop's own applied state?
//   2. Does it have RELIEF — is the land actually rolling, measured in metres, or still a plane?
//   3. Does it have REAL GROUND MATERIALS — the pack's terrain layers, not a flat colour?
//
// And then it takes an EYE-LEVEL picture, from a person's height looking across the land, because
// probe-framed close-ups are exactly how a flat green world got shipped past everyone for weeks.
// Menu: Emergence/Fas1/RUN GROUND CAPTURE.  Headless: drop Reports/RUN_GROUNDCAP.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class GroundCaptureProbe
    {
        const long Seed = 8919;
        const double Watchdog = 280.0;
        const int Horizon = 8;              // far enough for huts, close enough to be quick

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GROUNDCAP.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "GROUNDCAP_DONE.txt");
        const string Report   = "Reports/ground-capture.txt";
        const string KeyPending = "emg.groundcap.pending", KeyStart = "emg.groundcap.start";

        static int _frames;
        static Fas3Onboarding _onb;

        static GroundCaptureProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas1/RUN GROUND CAPTURE")]
        public static void RunMenu() => EditPhase();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.25;
                try
                {
                    if (SessionState.GetInt(KeyPending, 0) == 0 && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Trigger))
                    {
                        File.Delete(Trigger);
                        Directory.CreateDirectory("Reports");
                        File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditPhase();
                        return;
                    }
                }
                catch (Exception e) { Fail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > Watchdog;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    if (_onb == null) _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                    var clock = UnityEngine.Object.FindAnyObjectByType<Fas3PresentationClock>();
                    if (overtime || (clock != null && clock.PresentationYear >= Horizon && _frames > 60))
                        Finish(overtime);
                }
                catch (Exception e) { Fail("play: " + e.Message); }
            }
            else if (overtime) Fail("play mode did not start within watchdog");
        }

        static void EditPhase()
        {
            // A BARE scene on purpose: no WorldDresser, no capture rig. Whatever ground appears must
            // have been raised by the living loop itself, or the fix did not work.
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                UnityEditor.SceneManagement.NewSceneMode.Single);
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();

            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("MainCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            cam.farClipPlane = 3000f;

            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;

            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _onb = null;
            EditorApplication.EnterPlaymode();
        }

        static void Finish(bool overtime)
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            Action<bool, string> Check = (ok, m) => { if (ok) pass++; else fail++; sb.AppendLine((ok ? "  PASS  " : "  FAIL  ") + m); };

            sb.AppendLine("EMERGENCE — VÅG 1.1: does the LIVING world have ground?");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + (overtime ? "   (WATCHDOG cut)" : ""));
            sb.AppendLine("scene = the self-composing opening ONLY. No WorldDresser, no capture rig, no editor-only path.");
            sb.AppendLine();

            try
            {
                var world = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>();
                var clock = UnityEngine.Object.FindAnyObjectByType<Fas3PresentationClock>();
                sb.AppendLine("1. WAS THE GROUND RAISED, BY THE LIVING LOOP ITSELF?");
                Check(world != null, "the living world runtime exists");
                if (world != null)
                {
                    Check(world.GroundBuilt, "ground build attempted from an applied snapshot");
                    sb.AppendLine("     note: " + world.GroundNote);
                }
                var terrain = UnityEngine.Object.FindAnyObjectByType<Terrain>();
                Check(terrain != null, "a Terrain EXISTS in the living scene" + (terrain != null ? " (" + terrain.name + ")" : " — THE WORLD IS STILL A PLANE"));
                sb.AppendLine();

                if (terrain != null)
                {
                    var td = terrain.terrainData;
                    sb.AppendLine("2. IS THE LAND ACTUALLY ROLLING? (the flat-plane test, in metres)");
                    int r = td.heightmapResolution;
                    var hs = td.GetHeights(0, 0, r, r);
                    float lo = 1f, hi = 0f, sum = 0f;
                    for (int y = 0; y < r; y++) for (int x = 0; x < r; x++) { float v = hs[y, x]; if (v < lo) lo = v; if (v > hi) hi = v; sum += v; }
                    float reliefM = (hi - lo) * td.size.y;
                    sb.AppendLine("     terrain " + td.size.x + " x " + td.size.z + " m, height span " + td.size.y + " m");
                    sb.AppendLine("     relief actually built: " + reliefM.ToString("F1") + " m   (low " + (lo * td.size.y).ToString("F1") + " m, high " + (hi * td.size.y).ToString("F1") + " m)");
                    Check(reliefM > 8f, "the land ROLLS — " + reliefM.ToString("F1") + " m of relief (a plane would be ~0)");
                    // steepness: the thing Patrik felt as "too steep down to the lake"
                    float maxSlope = 0f; double slopeSum = 0; int n = 0;
                    for (int i = 0; i <= 60; i++)
                        for (int j = 0; j <= 60; j++)
                        {
                            float sx = i / 60f, sy = j / 60f;
                            float st = td.GetSteepness(sx, sy);
                            if (st > maxSlope) maxSlope = st;
                            slopeSum += st; n++;
                        }
                    sb.AppendLine("     slope: mean " + (slopeSum / n).ToString("F1") + " deg, max " + maxSlope.ToString("F1") + " deg");
                    Check(maxSlope < 55f, "no cliff faces — max slope " + maxSlope.ToString("F1") + " deg (a wall would be 80+)");
                    sb.AppendLine();

                    sb.AppendLine("3. IS IT REAL GROUND, OR A FLAT COLOUR?");
                    var names = td.terrainLayers.Select(l => l != null ? l.name : "null").ToArray();
                    sb.AppendLine("     layers: " + string.Join(", ", names));
                    Check(td.terrainLayers.Length >= 4, "at least four ground materials (" + td.terrainLayers.Length + ")");
                    int textured = td.terrainLayers.Count(l => l != null && l.diffuseTexture != null && l.diffuseTexture != Texture2D.whiteTexture);
                    // NAME EVERY LAYER AND ITS TEXTURE. The checkered mounds sit ON the terrain and the
                    // inventory above skips Terrain by design — so a textureless GROUND layer could
                    // never be found by it. This is where it would hide.
                    for (int li = 0; li < td.terrainLayers.Length; li++)
                    {
                        var L = td.terrainLayers[li];
                        sb.AppendLine("       layer " + li + ": " + (L != null ? L.name : "NULL").PadRight(22)
                                      + " diffuse=" + (L != null && L.diffuseTexture != null ? L.diffuseTexture.name + " " + L.diffuseTexture.width + "px" : "*** NONE — THIS RENDERS AS FLAT/CHECKER ***")
                                      + (L != null ? "  tile=" + L.tileSize : ""));
                    }
                    Check(textured >= 5, textured + " of " + td.terrainLayers.Length + " layers carry a REAL texture (a fallback layer is flat white tinted)");
                    Check(td.detailPrototypes.Length > 0, "the meadow has detail prototypes (" + td.detailPrototypes.Length + " grass/flower)");
                    sb.AppendLine();
                }

                // ---------- 5. THE WORLD INVENTORY: scale, sinking, and materials ----------
                // Steps 1.2a / 1.2 / 1.3 measured in one pass, because all three are the same question
                // asked three ways: is what stands on this ground the right size, at the right height,
                // wearing the right skin? Patrik felt all three at once ("stones too big, trunks too
                // big and sliding under the ground") — so measure them together.
                sb.AppendLine("5. WHAT STANDS ON THE GROUND (scale / sinking / materials)");
                var rends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                float villagerH = 0f;
                var rows = new List<string>();
                int sunk = 0, defaultMat = 0, measured = 0;
                float tallest = 0f; string tallestName = "";
                var byShader = new Dictionary<string, int>();

                // the human yardstick first — everything is judged against a person's height
                foreach (var rr in rends)
                {
                    if (rr == null || !rr.enabled) continue;
                    var t = rr.transform;
                    bool isAgent = t.root != null && (t.root.name.Contains("agent") || t.root.name.Contains("Agent") || t.root.name.Contains("villager"));
                    if (isAgent && rr.bounds.size.y > villagerH) villagerH = rr.bounds.size.y;
                }
                sb.AppendLine("     the yardstick: tallest villager renderer = " + villagerH.ToString("F2") + " m");

                foreach (var rr in rends)
                {
                    if (rr == null || !rr.enabled) continue;
                    if (rr is ParticleSystemRenderer) continue;
                    var go = rr.gameObject;
                    if (go.GetComponent<Terrain>() != null) continue;
                    var b = rr.bounds;
                    if (b.size.y <= 0.001f) continue;
                    measured++;

                    var mat = rr.sharedMaterial;
                    string shader = mat != null && mat.shader != null ? mat.shader.name : "NO MATERIAL";
                    byShader[shader] = byShader.TryGetValue(shader, out var c0) ? c0 + 1 : 1;
                    // Unity's default material is what renders as the grey CHECKER — the placeholder look
                    bool isDefault = mat == null || mat.name.StartsWith("Default-") || mat.name == "Lit";
                    if (isDefault) defaultMat++;

                    float sink = 0f;
                    if (terrain != null)
                    {
                        float gy = terrain.SampleHeight(b.center) + terrain.transform.position.y;
                        sink = gy - b.min.y;          // >0 means the object's base is BELOW the ground
                        if (sink > 0.05f) sunk++;
                    }
                    if (b.size.y > tallest) { tallest = b.size.y; tallestName = go.name + " [" + (mat != null ? mat.name : "none") + "]"; }
                    if (rows.Count < 14 && (isDefault || sink > 0.05f || (villagerH > 0.1f && b.size.y > villagerH * 4f)))
                        rows.Add("       " + go.name.PadRight(26).Substring(0, Math.Min(26, go.name.Length)).PadRight(26)
                                 + " h=" + b.size.y.ToString("F1") + "m"
                                 + (villagerH > 0.1f ? " (" + (b.size.y / villagerH).ToString("F1") + "x villager)" : "")
                                 + (sink > 0.05f ? "  SINKS " + sink.ToString("F2") + "m" : "")
                                 + (isDefault ? "  DEFAULT MATERIAL" : "  " + (mat != null ? mat.name : "-")));
                }
                sb.AppendLine("     renderers measured: " + measured);
                sb.AppendLine("     shaders in use: " + string.Join(" | ", byShader.OrderByDescending(k => k.Value).Take(6).Select(k => k.Key + " x" + k.Value)));
                sb.AppendLine("     tallest object: " + tallestName + " = " + tallest.ToString("F1") + " m"
                              + (villagerH > 0.1f ? " (" + (tallest / villagerH).ToString("F1") + "x a villager)" : ""));
                foreach (var r2 in rows) sb.AppendLine(r2);

                // NAME EVERYTHING. Six hypotheses missed the white objects because every check was
                // conditional — only flagged rows were printed, and the offenders passed every flag.
                // So: list the biggest things in the world unconditionally, with their full parent
                // chain, so an unidentified object can never hide behind a clean bill of health again.
                sb.AppendLine();
                sb.AppendLine("     THE TEN LARGEST OBJECTS (unconditional — nothing hides here):");
                var big = new List<(float vol, string line)>();
                foreach (var rr3 in rends)
                {
                    if (rr3 == null || !rr3.enabled || rr3 is ParticleSystemRenderer) continue;
                    if (rr3.gameObject.GetComponent<Terrain>() != null) continue;
                    var bb3 = rr3.bounds;
                    if (bb3.size.y <= 0.001f) continue;
                    float vol = bb3.size.x * bb3.size.y * bb3.size.z;
                    string chain = rr3.name;
                    var p3 = rr3.transform.parent;
                    int guard = 0;
                    while (p3 != null && guard++ < 4) { chain = p3.name + " / " + chain; p3 = p3.parent; }
                    var m3 = rr3.sharedMaterial;
                    Texture t3 = null;
                    // NOTE: glTFast's Shader Graph does not use _BaseMap/_MainTex — reading only those
                    // reported our own villager GLBs as textureless, which the source files disprove
                    // (they carry a baseColorTexture and TEXCOORD_0). Ask the material what it actually has.
                    if (m3 != null) t3 = FirstTexture(m3);
                    big.Add((vol, "       " + bb3.size.x.ToString("F1") + "x" + bb3.size.y.ToString("F1") + "x" + bb3.size.z.ToString("F1") + " m  "
                                  + chain + "   mat=" + (m3 != null ? m3.name : "none")
                                  + "  tex=" + (t3 != null ? t3.name : "NONE")
                                  + "  slots=" + (rr3.sharedMaterials != null ? rr3.sharedMaterials.Length : 0)
                                  + "  at " + bb3.center.ToString("F0")));
                }
                foreach (var e3 in big.OrderByDescending(x => x.vol).Take(10)) sb.AppendLine(e3.line);
                Check(measured > 0, "there is something standing on the ground at all (" + measured + " renderers)");
                Check(defaultMat == 0, "NOTHING wears Unity's default material (" + defaultMat + " do — that IS the grey checker Patrik saw)");
                Check(sunk == 0, "NOTHING sinks through the ground (" + sunk + " do)");
                if (villagerH > 0.1f)
                    Check(tallest < villagerH * 8f, "nothing is absurdly out of scale (tallest = " + (tallest / villagerH).ToString("F1") + "x a villager)");
                sb.AppendLine();

                // ---------- the eye-level picture ----------
                sb.AppendLine("4. THE EYE-LEVEL PICTURE (what a person standing there would see)");
                int magenta = Capture("ground-eye-level", terrain, out string camNote);
                // A/B the light phase: the store shots were all taken at DUSK, and the day rig runs
                // sun 1.3 + fill 0.6 + trilight ambient. If the pack's light textures are blowing out
                // to white in daylight, dusk will show them correctly — and that, not a missing
                // material, is what the grey "checker" is.
                Fas3LightRig.Apply("spring", "dusk");
                Capture("ground-eye-level-dusk", terrain, out _);
                Fas3LightRig.Apply("spring", "day");

                // THE CLOSE-UP. The isolation shot proved the house renders perfectly on its own
                // (teal shingles, timber, plaster). So the fault is scene-level — and the fastest way
                // to corner it is the same object at the same range, but IN the living scene.
                var hut = GameObject.Find("Huts");
                Renderer nearest = null; float bestSize = 0f;
                foreach (var rr2 in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (rr2 == null || rr2.gameObject.GetComponent<Terrain>() != null) continue;
                    if (!rr2.name.StartsWith("P_BLD")) continue;
                    if (rr2.bounds.size.y > bestSize) { bestSize = rr2.bounds.size.y; nearest = rr2; }
                }
                if (nearest != null)
                {
                    var bb2 = nearest.bounds;
                    float d2 = bb2.size.magnitude * 1.1f;
                    cam2Note = "close-up on " + nearest.name + " (" + bb2.size.y.ToString("F1") + " m) from " + d2.ToString("F0") + " m";
                    var c2 = Camera.main;
                    c2.transform.position = bb2.center + new Vector3(d2 * 0.7f, d2 * 0.45f, d2 * 0.7f);
                    c2.transform.LookAt(bb2.center);
                    bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
                    CaptureRaw("house-in-scene");
                    RenderSettings.fog = fogWas;
                    sb.AppendLine("     " + cam2Note + " -> Reports/house-in-scene.png");
                }
                // and the same treatment for a VILLAGER — the ten-largest list named char1 as the
                // suspect, so put it in front of the lens under the same conditions as the house.
                Renderer soul = null; float soulSize = 0f;
                foreach (var rr4 in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (rr4 == null || !rr4.name.StartsWith("char")) continue;
                    if (rr4.bounds.size.y > soulSize) { soulSize = rr4.bounds.size.y; soul = rr4; }
                }
                if (soul != null)
                {
                    var b4 = soul.bounds;
                    float d4 = Mathf.Max(3.5f, b4.size.magnitude * 1.4f);
                    var c4 = Camera.main;
                    c4.transform.position = b4.center + new Vector3(d4 * 0.7f, d4 * 0.25f, d4 * 0.7f);
                    c4.transform.LookAt(b4.center);
                    bool fw = RenderSettings.fog; RenderSettings.fog = false;
                    CaptureRaw("villager-in-scene");
                    RenderSettings.fog = fw;
                    var mt = soul.sharedMaterial;
                    sb.AppendLine("     close-up on " + soul.name + " (" + b4.size.y.ToString("F1") + " m) mat=" + (mt != null ? mt.name : "none")
                                  + " shader=" + (mt != null && mt.shader != null ? mt.shader.name : "-")
                                  + " tex=" + (mt != null && FirstTexture(mt) != null ? FirstTexture(mt).name : "NONE")
                                  + " -> Reports/villager-in-scene.png");
                }
                sb.AppendLine("     " + camNote);
                Check(magenta >= 0, "capture written");
                Check(magenta == 0, "no missing-material magenta in frame (" + magenta + " px)");
                sb.AppendLine("     year at capture: " + (clock != null ? clock.PresentationYear : -1));
            }
            catch (Exception e) { fail++; sb.AppendLine("  FAIL  exception: " + e); }

            sb.AppendLine();
            sb.AppendLine("VERDICT: " + (fail == 0 ? "GREEN" : "RED") + "  (" + pass + "/" + (pass + fail) + ")");
            sb.AppendLine("declared: this proves the GROUND is there and rolling. Prop scale, the pivot sinking and");
            sb.AppendLine("  the water body are steps 1.2-1.5 and are NOT claimed here. The picture is the real test —");
            sb.AppendLine("  a human (or the builder) must LOOK at it, per D-008.");

            try
            {
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, (fail == 0 ? "GREEN" : "RED") + " " + pass + "/" + (pass + fail) + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            }
            catch { }
            SessionState.SetInt(KeyPending, 0);
            EditorApplication.ExitPlaymode();
            Debug.Log("[GroundCaptureProbe] -> " + Report);
        }

        /// <summary>Stand a camera at a person's height on the land and look across it. Manual RT capture
        /// (D-125: ScreenCapture yields a white frame on an unattended editor).</summary>
        /// <summary>The first real texture on a material, whatever the shader calls it. A probe that
        /// only knows URP's names will call every glTF material textureless and send the hunt sideways.</summary>
        static Texture FirstTexture(Material m)
        {
            if (m == null || m.shader == null) return null;
            foreach (var n in new[] { "_BaseMap", "_MainTex", "baseColorTexture", "_baseColorTexture", "_BaseColorTexture" })
                if (m.HasProperty(n)) { var t = m.GetTexture(n); if (t != null) return t; }
            int count = UnityEditor.ShaderUtil.GetPropertyCount(m.shader);
            for (int i = 0; i < count; i++)
                if (UnityEditor.ShaderUtil.GetPropertyType(m.shader, i) == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    var t = m.GetTexture(UnityEditor.ShaderUtil.GetPropertyName(m.shader, i));
                    if (t != null) return t;
                }
            return null;
        }

        static string cam2Note = "";

        /// <summary>Grab whatever the camera is currently looking at, without re-framing.</summary>
        static void CaptureRaw(string name)
        {
            var cam = Camera.main; if (cam == null) return;
            const int w = 1280, h = 720;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            try { File.WriteAllBytes("Reports/" + name + ".png", tex.EncodeToPNG()); } catch { }
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
        }

        static int Capture(string name, Terrain terrain, out string note)
        {
            note = "";
            var cam = Camera.main; if (cam == null) return -1;

            // put the eye where the people are, not 55 m up on a map camera (D-131's lesson)
            var world = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>();
            Vector3 look = Vector3.zero;
            var S = world != null ? world.LastState : null;
            if (S?.agents != null && S.agents.Length > 0)
                look = new Vector3(S.agents[0].x * Fas3TerrainBuilder.TileSize, 0f, S.agents[0].y * Fas3TerrainBuilder.TileSize);
            else if (terrain != null)
                look = terrain.transform.position + new Vector3(terrain.terrainData.size.x * 0.5f, 0f, terrain.terrainData.size.z * 0.5f);

            float gy = terrain != null ? terrain.SampleHeight(look) + terrain.transform.position.y : 0f;
            look.y = gy + 1.0f;
            // stand back and slightly above eye height so the LAND is the subject, not a face
            var eye = look + new Vector3(-34f, 0f, -34f);
            float ey = terrain != null ? terrain.SampleHeight(eye) + terrain.transform.position.y : 0f;
            eye.y = ey + 2.4f;
            cam.transform.position = eye;
            cam.transform.LookAt(look);
            cam.fieldOfView = 55f;
            note = "eye at " + eye.ToString("F1") + " (ground " + ey.ToString("F1") + " m), looking at " + look.ToString("F1");

            const int w = 1920, h = 1080;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;

            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            try { Directory.CreateDirectory("Reports"); File.WriteAllBytes("Reports/" + name + ".png", tex.EncodeToPNG()); } catch { }
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
            return magenta;
        }

        static void Fail(string why)
        {
            try { Directory.CreateDirectory("Reports"); File.WriteAllText(Done, "RED " + why + " " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch { }
            SessionState.SetInt(KeyPending, 0);
            Debug.LogWarning("[GroundCaptureProbe] " + why);
        }
    }
}
#endif
