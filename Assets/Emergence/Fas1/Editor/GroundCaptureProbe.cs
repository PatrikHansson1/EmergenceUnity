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
        const double Watchdog = 420.0;   // D-217: 280 s cut the 30-year run off at year 19
        // D-217: 8 years was chosen to be quick, and it was — but it is a world where nobody has
        // died yet, so the window law had nothing to prove: 2 huts lit, 0 cold. A law that cannot be
        // observed failing has not been demonstrated. 45 years is one generation: there are huts,
        // there are villages, and there are owners who are no longer among the living. It also gives
        // step 1.6's eye-level pictures a village worth photographing instead of two houses.
        // 30 years overran the 420 s watchdog and the run was cut at 27. 22 still passes the first
        // huts, a real village and deaths among hut owners, and keeps the loop tight enough to
        // iterate on. A probe you avoid running because it is slow is a probe you stop running.
        const int Horizon = 22;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GROUNDCAP.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "GROUNDCAP_DONE.txt");
        const string Report   = "Reports/ground-capture.txt";
        const string KeyPending = "emg.groundcap.pending", KeyStart = "emg.groundcap.start";

        static int _frames, rendCount;
        static Vector3 _perfPos; static Quaternion _perfRot; static bool _perfPoseSet;
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

                    // WHAT IS ACTUALLY PAINTED. Three cycles of this pass were spent arguing about a
                    // large bare-earth area in the foreground while adjusting laws that turned out not
                    // to paint it. A layer's mean weight over the whole map settles that in one line:
                    // if Layer_Dirt reads 0,31 then a third of the world is dirt and the argument is over.
                    {
                        var am = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
                        int AW = td.alphamapWidth, AH = td.alphamapHeight, AL = td.alphamapLayers;
                        var mean = new float[AL];
                        for (int ay2 = 0; ay2 < AH; ay2++)
                            for (int ax2 = 0; ax2 < AW; ax2++)
                                for (int l2 = 0; l2 < AL; l2++) mean[l2] += am[ay2, ax2, l2];
                        var bits = new List<string>();
                        for (int l2 = 0; l2 < AL; l2++)
                        {
                            mean[l2] /= (AW * AH);
                            var nm = l2 < td.terrainLayers.Length && td.terrainLayers[l2] != null ? td.terrainLayers[l2].name : ("layer" + l2);
                            bits.Add(nm + "=" + (mean[l2] * 100f).ToString("F0") + "%");
                        }
                        sb.AppendLine("     the ground is painted: " + string.Join("  ", bits)
                                      + "   (alphamap " + AW + "x" + AH + ")");
                    }
                    Check(td.detailPrototypes.Length > 0, "the meadow has detail prototypes (" + td.detailPrototypes.Length + " grass/flower)");

                    // D-215b: the meadow was declared built and could not be seen at eye level. A
                    // prototype COUNT proves nothing — it says the recipe exists, not that a single
                    // blade was placed or that the terrain was told to draw one. Measure all three.
                    {
                        var terr0 = terrain;
                        long blades = 0;
                        for (int p = 0; p < td.detailPrototypes.Length; p++)
                        {
                            var map = td.GetDetailLayer(0, 0, td.detailWidth, td.detailHeight, p);
                            long bl = 0;
                            for (int yy = 0; yy < td.detailHeight; yy++)
                                for (int xx = 0; xx < td.detailWidth; xx++) bl += map[xx, yy];
                            blades += bl;
                            var pr = td.detailPrototypes[p].prototype;
                            bool hasMesh = pr != null && pr.GetComponentInChildren<MeshRenderer>(true) != null;
                            sb.AppendLine("       detail " + p + ": " + (pr != null ? pr.name : "NULL").PadRight(28)
                                          + " instances=" + bl
                                          + "  mesh=" + (hasMesh ? "yes" : "NO — nothing to draw")
                                          + "  mode=" + td.detailPrototypes[p].renderMode
                                          + "  instanced=" + td.detailPrototypes[p].useInstancing);
                        }
                        sb.AppendLine("       detail map " + td.detailWidth + "x" + td.detailHeight
                                      + ", terrain: distance=" + (terr0 != null ? terr0.detailObjectDistance : -1f)
                                      + " density=" + (terr0 != null ? terr0.detailObjectDensity : -1f)
                                      + " drawFoliage=" + (terr0 != null ? terr0.drawTreesAndFoliage.ToString() : "?"));
                        // the SECOND gate: quality level. A million planted blades draw nothing if this
                        // scale sits near zero, and nothing anywhere says so.
                        sb.AppendLine("       quality '" + QualitySettings.names[QualitySettings.GetQualityLevel()]
                                      + "': detailDensityScale=" + QualitySettings.terrainDetailDensityScale
                                      + " detailDistance=" + QualitySettings.terrainDetailDistance);
                        Check(QualitySettings.terrainDetailDensityScale > 0.4f,
                              "the quality level lets the meadow be drawn (densityScale="
                              + QualitySettings.terrainDetailDensityScale.ToString("F2") + ")");
                        Check(blades > 1000, "the meadow is actually PLANTED, not merely prototyped (" + blades + " instances)");
                    }

                    // what the land is MADE of. Four steps of this pass were spent arguing about a
                    // large bare area without anyone knowing which tile type painted it.
                    {
                        var S0 = world != null ? world.LastState : null;
                        if (S0 != null && !string.IsNullOrEmpty(S0.tileTypes))
                        {
                            var hist = new Dictionary<char, int>();
                            foreach (var ch in S0.tileTypes) { hist.TryGetValue(ch, out int c0); hist[ch] = c0 + 1; }
                            var parts = hist.OrderByDescending(k => k.Value)
                                            .Select(k => k.Key + "=" + k.Value + " (" + (100f * k.Value / S0.tileTypes.Length).ToString("F0") + "%)");
                            sb.AppendLine("     the map is made of: " + string.Join("  ", parts));
                        }
                    }
                    sb.AppendLine();
                    // VÅG 1.5: the water body, which the tile histogram says is 4% of the world
                    sb.AppendLine();
                    sb.AppendLine("3c. THE WATER (VÅG 1.5)");
                    sb.AppendLine("     " + (world != null && !string.IsNullOrEmpty(world.WaterNote) ? world.WaterNote : "(no water note)"));
                    {
                        var wroot = GameObject.Find("Water");
                        int surfaces = wroot != null ? wroot.transform.childCount : 0;
                        float wlo = float.MaxValue, whi = float.MinValue;
                        if (wroot != null)
                            for (int wi = 0; wi < surfaces; wi++)
                            {
                                float wy = wroot.transform.GetChild(wi).position.y;
                                if (wy < wlo) wlo = wy; if (wy > whi) whi = wy;
                            }
                        if (surfaces > 0)
                            sb.AppendLine("     surfaces stand at " + wlo.ToString("F1") + ".." + whi.ToString("F1")
                                          + " m (each body ONE level — the dresser gave every tile its own)");
                        Check(surfaces > 0, "the living world has water in it (" + surfaces + " bodies)");
                    }
                    sb.AppendLine();
                    sb.AppendLine("3b. THE NATURAL WORLD (trees, rocks, bushes — the rest of step 1.1)");
                    sb.AppendLine("     " + (world != null ? world.NatureNote : "(no world)"));
                    Check(world != null && world.NatureCount > 100, "the living world has a natural world in it ("
                          + (world != null ? world.NatureCount : 0) + " placed)");

                    // MEASURE THE NATURE'S SCALE against the human yardstick before touching a single
                    // multiplier. "The bushes look too big" is a feeling; "a bush is 0.9x a person"
                    // is a number you can correct and then check.
                    // the human yardstick, measured here because this block runs before section 5
                    float yard = 0f;
                    foreach (var rrY in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
                        if (rrY != null && rrY.name.StartsWith("char") && rrY.bounds.size.y > yard) yard = rrY.bounds.size.y;
                    var natRoot = GameObject.Find("Nature_Live");
                    if (natRoot != null && yard > 0.1f)
                    {
                        var cat = new Dictionary<string, List<float>>();
                        foreach (Transform child in natRoot.transform)
                        {
                            var rs2 = child.GetComponentsInChildren<Renderer>();
                            if (rs2.Length == 0) continue;
                            var bb5 = rs2[0].bounds;
                            for (int i = 1; i < rs2.Length; i++) bb5.Encapsulate(rs2[i].bounds);
                            string kind = child.name.Contains("TreeLarge") || child.name.Contains("Birch") ? "tree"
                                        : child.name.Contains("Rock") || child.name.Contains("stone") ? "rock"
                                        : child.name.Contains("Bush") ? "bush"
                                        : child.name.Contains("treetrunk") ? "trunk"
                                        : child.name.Contains("Grass") || child.name.Contains("Flower") ? "tuft" : "other";
                            if (!cat.TryGetValue(kind, out var l)) { l = new List<float>(); cat[kind] = l; }
                            l.Add(bb5.size.y);
                        }
                        // HONESTY ABOUT THE RULER: `yard` is an ANIMATED bounds, which spreads with the
                        // walk cycle and overstates a standing person by roughly a third. The design
                        // height is 1.75 m (D-215, measured at rest). Report against the design height
                        // so a ratio here means what a reader thinks it means.
                        const float DesignHeight = 1.75f;
                        sb.AppendLine("     scale against a person (design height " + DesignHeight.ToString("F2")
                                      + " m; animated bounds read " + yard.ToString("F2") + " m):");
                        foreach (var kv in cat.OrderBy(k => k.Key))
                        {
                            if (kv.Value.Count == 0) continue;
                            float mean = kv.Value.Average(), max = kv.Value.Max();
                            sb.AppendLine("       " + kv.Key.PadRight(7) + " n=" + kv.Value.Count.ToString().PadLeft(4)
                                          + "  mean " + mean.ToString("F1") + " m (" + (mean / DesignHeight).ToString("F2") + "x)"
                                          + "  max " + max.ToString("F1") + " m (" + (max / DesignHeight).ToString("F2") + "x)");
                        }
                        // a bush should reach a person's waist-to-chest, not overtop them
                        if (cat.TryGetValue("bush", out var bl) && bl.Count > 0)
                            Check(bl.Average() / DesignHeight < 1.0f, "a bush stands no taller than a person ("
                                  + (bl.Average() / DesignHeight).ToString("F2") + "x — over 1.0 it is a thicket, not a shrub)");
                    }
                    sb.AppendLine();
                }

                // ---------- 5. THE WORLD INVENTORY: scale, sinking, and materials ----------
                // Steps 1.2a / 1.2 / 1.3 measured in one pass, because all three are the same question
                // asked three ways: is what stands on this ground the right size, at the right height,
                // wearing the right skin? Patrik felt all three at once ("stones too big, trunks too
                // big and sliding under the ground") — so measure them together.
                sb.AppendLine("5. WHAT STANDS ON THE GROUND (scale / sinking / materials)");
                var rends = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
                float villagerH = 0f;
                var rows = new List<string>();
                int sunk = 0, defaultMat = 0, measured = 0, bedded = 0;
                var sinkers = new List<string>();
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
                        // D-215b - MEASURE THE RIGHT THING. Comparing the bounding box's lowest
                        // corner against the ground under its CENTRE is not a sinking test on sloped
                        // land: a box spanning uneven ground legitimately has a corner below the middle
                        // of that ground. That is what reported agent_3_Embla as sinking 0,09 m while
                        // her feet were exactly on the surface - the arms in her A-pose widen the box
                        // to 1,6 m, and 1,6 m of 11-degree bank is 0,16 m of fall. A thing sinks when
                        // its lowest point is under the LOWEST ground beneath its own footprint.
                        float gy = terrain.SampleHeight(b.center) + terrain.transform.position.y;
                        for (int cx = -1; cx <= 1; cx++)
                            for (int cz = -1; cz <= 1; cz++)
                            {
                                var pp = new Vector3(b.center.x + cx * b.extents.x, b.center.y, b.center.z + cz * b.extents.z);
                                float hh = terrain.SampleHeight(pp) + terrain.transform.position.y;
                                if (hh < gy) gy = hh;
                            }
                        sink = gy - b.min.y;          // >0 means the object's base is BELOW the ground
                            // THE LAW IS ABOUT BUILT THINGS AND PEOPLE, NOT BOULDERS. A rock formation
                        // half-bedded in the hillside is correct — geology, not a bug; a house or a
                        // villager sunk to the knee is neither. Natural scatter is therefore exempt,
                        // and the exemption is named here rather than quietly widening the threshold.
                        // walk the ancestry rather than trusting root: a single re-parent anywhere in
                        // the chain would otherwise turn a bedded boulder back into a "defect"
                        bool natural = IsNatural(rr.transform);   // by nature, not by parentage
                        if (sink > 0.05f && !natural)
                        {
                            sunk++;
                            // NAME IT. The shared row list is capped and shared with other flags, so a
                            // sinker could sit outside it and stay anonymous — which is exactly how the
                            // white ground hid for a day. Sinkers get their own list.
                            string chainS = rr.name; var pS = rr.transform.parent; int gS = 0;
                            while (pS != null && gS++ < 4) { chainS = pS.name + " / " + chainS; pS = pS.parent; }
                            sinkers.Add("       SINKS " + sink.ToString("F2") + " m  " + chainS
                                        + "  mat=" + (mat != null ? mat.name : "none"));
                            // D-241: THE MEASUREMENT, NOT A FIFTH FIX. Four interventions moved this
                            // number by exactly zero centimetres, which means it is not measuring what
                            // anyone assumed. So print, in the SAME frame, every number that goes into
                            // it — the transform, what the animator believes its feet are worth, the
                            // ground under the centre, the lowest ground under the footprint, and the
                            // bounds' own floor — plus the renderer TYPE, because a skinned mesh's box
                            // is not a mesh. One of these will not be what anyone expects.
                            {
                                var animC = rr.GetComponentInParent<Emergence.Runtime.AgentAnimator>();
                                float gCentre = terrain.SampleHeight(b.center) + terrain.transform.position.y;
                                float gRoot   = terrain.SampleHeight(rr.transform.position) + terrain.transform.position.y;
                                var root = animC != null ? animC.transform : rr.transform.root;
                                sinkers.Add("          why: rootY=" + root.position.y.ToString("F3")
                                    + " groundUnderRoot=" + gRoot.ToString("F3")
                                    + " groundUnderCentre=" + gCentre.ToString("F3")
                                    + " lowestUnderFootprint=" + gy.ToString("F3")
                                    + " boundsMinY=" + b.min.y.ToString("F3")
                                    + " boundsSizeXZ=" + b.size.x.ToString("F2") + "x" + b.size.z.ToString("F2"));
                                sinkers.Add("          who: renderer=" + rr.GetType().Name
                                    + (animC != null ? "  footBelief=" + animC.FootBelief.ToString("F3")
                                                     + " samplesLeft=" + animC.FootSamplesLeft
                                                     + " transit=" + animC.InTransit
                                                     + " state=" + animC.task
                                                     : "  (no AgentAnimator in parents)")
                                    + "  rootToBoundsFloor=" + (root.position.y - b.min.y).ToString("F3"));
                            }
                        }
                        else if (sink > 0.05f) bedded++;
                    }
                    if (b.size.y > tallest) { tallest = b.size.y; tallestName = go.name + " [" + (mat != null ? mat.name : "none") + "]"; }
                    if (rows.Count < 14 && (isDefault || (sink > 0.05f && !IsNatural(rr.transform)) || (villagerH > 0.1f && b.size.y > villagerH * 4f)))
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
                rendCount = measured;
                Check(measured > 0, "there is something standing on the ground at all (" + measured + " renderers)");
                Check(defaultMat == 0, "NOTHING wears Unity's default material (" + defaultMat + " do — that IS the grey checker Patrik saw)");
                foreach (var sk in sinkers.Take(6)) sb.AppendLine(sk);
                Check(sunk == 0, "no BUILT thing or person sinks through the ground (" + sunk + " do; " + bedded + " natural props bedded into it, which is correct)");
                // The old ceiling (8x the ANIMATED villager bounds) was written while the yardstick was
                // a 2.3 m giant, and it now fails on trees that are finally the right size — a real oak
                // IS about ten times a person. Judge against the design height (D-215) and put the
                // ceiling where "absurd" actually begins.
                Check(tallest < 1.75f * 18f, "nothing is absurdly out of scale (tallest = "
                      + (tallest / 1.75f).ToString("F1") + "x a person — a real tree is 8-14x)");
                sb.AppendLine();

                // ---------- the eye-level picture ----------
                sb.AppendLine("4. THE EYE-LEVEL PICTURE (what a person standing there would see)");
                int magenta = Capture("ground-eye-level", terrain, out string camNote);
                // remember the pose the cost is measured from, so section 6 is comparable run to run
                var pc0 = Camera.main;
                if (pc0 != null) { _perfPos = pc0.transform.position; _perfRot = pc0.transform.rotation; _perfPoseSet = true; }
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
                foreach (var rr2 in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
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
                foreach (var rr4 in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude))
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

                // ---- STEP 1.6: EYE-LEVEL, WHERE A PERSON WOULD ACTUALLY STAND ----
                // The close-ups above are diagnostic framing - a lens put deliberately on a suspect.
                // They are not what the game looks like. Acceptance for the environment pass is a
                // person's eye at 1,7 m looking at the two places the world is SUPPOSED to be worth
                // looking at: the water, and the village. Nothing is claimed here; the pictures are
                // the claim, and a human has to look at them (D-008).
                {
                    var wroot2 = GameObject.Find("Water");
                    if (wroot2 != null && wroot2.transform.childCount > 0)
                    {
                        Transform wbig = null; float bigA = 0f;
                        for (int i = 0; i < wroot2.transform.childCount; i++)
                        {
                            var ch = wroot2.transform.GetChild(i);
                            var rr5 = ch.GetComponentInChildren<Renderer>();
                            if (rr5 == null) continue;
                            float a5 = rr5.bounds.size.x * rr5.bounds.size.z;
                            if (a5 > bigA) { bigA = a5; wbig = ch; }
                        }
                        if (wbig != null)
                        {
                            var wb = wbig.GetComponentInChildren<Renderer>().bounds;
                            // stand on the shore, back from the edge, and look across the long axis
                            float reach = Mathf.Max(wb.extents.x, wb.extents.z) + 26f;
                            var eye = new Vector3(wb.center.x - reach, 0f, wb.center.z - reach * 0.35f);
                            eye.y = (terrain != null ? terrain.SampleHeight(eye) + terrain.transform.position.y : wb.center.y) + 1.7f;
                            var c5 = Camera.main;
                            c5.transform.position = eye;
                            c5.transform.LookAt(new Vector3(wb.center.x, wb.center.y + 1.0f, wb.center.z));
                            CaptureRaw("eye-at-the-water");
                            sb.AppendLine("     eye at the water: shore " + eye.ToString("F0") + " looking across "
                                          + wb.size.x.ToString("F0") + "x" + wb.size.z.ToString("F0") + " m -> Reports/eye-at-the-water.png");
                        }
                    }

                    // and the village: stand among the huts, not above them
                    var hutRoot = GameObject.Find(Emergence.Runtime.HutReconciler.LayerName);   // "Huts_Live" - "Huts" found nothing
                    if (hutRoot != null && hutRoot.transform.childCount > 0)
                    {
                        // ONE VILLAGE, NOT ALL OF THEM. At year 8 there were two huts and the
                        // bounds of "every hut" was a village. By year 19 the huts spanned 350 m
                        // across several settlements, the camera backed off to 194 m to fit them,
                        // and the "village picture" became an aerial of the whole map. Find the
                        // densest cluster instead: the hut with the most neighbours within 45 m,
                        // and frame it with those neighbours. Deterministic — ties go to the lowest
                        // child index, never to iteration order.
                        var hutBounds = new List<Bounds>();
                        for (int i = 0; i < hutRoot.transform.childCount; i++)
                        {
                            var rs8 = hutRoot.transform.GetChild(i).GetComponentsInChildren<Renderer>();
                            if (rs8.Length == 0) continue;
                            var b8 = rs8[0].bounds;
                            for (int k = 1; k < rs8.Length; k++) b8.Encapsulate(rs8[k].bounds);
                            hutBounds.Add(b8);
                        }
                        var acc = new Bounds(); bool any5 = false; int nh = 0;
                        int bestI = -1, bestN = -1;
                        for (int i = 0; i < hutBounds.Count; i++)
                        {
                            int n8 = 0;
                            for (int j = 0; j < hutBounds.Count; j++)
                                if (Vector3.Distance(hutBounds[i].center, hutBounds[j].center) < 45f) n8++;
                            if (n8 > bestN) { bestN = n8; bestI = i; }
                        }
                        if (bestI >= 0)
                            for (int j = 0; j < hutBounds.Count; j++)
                                if (Vector3.Distance(hutBounds[bestI].center, hutBounds[j].center) < 45f)
                                {
                                    if (!any5) { acc = hutBounds[j]; any5 = true; } else acc.Encapsulate(hutBounds[j]);
                                    nh++;
                                }
                        if (any5)
                        {
                            float back = Mathf.Clamp(acc.extents.magnitude * 1.1f, 18f, 60f);
                            var eye = new Vector3(acc.center.x - back * 0.75f, 0f, acc.center.z - back * 0.75f);
                            eye.y = (terrain != null ? terrain.SampleHeight(eye) + terrain.transform.position.y : acc.center.y) + 1.7f;
                            var c6 = Camera.main;
                            c6.transform.position = eye;
                            c6.transform.LookAt(new Vector3(acc.center.x, acc.center.y * 0.55f + eye.y * 0.45f, acc.center.z));
                            CaptureRaw("eye-in-the-village");
                            sb.AppendLine("     eye in the village: " + nh + " renderers over "
                                          + acc.size.x.ToString("F0") + "x" + acc.size.z.ToString("F0") + " m, standing "
                                          + back.ToString("F0") + " m out (densest cluster of "
                                          + hutBounds.Count + " huts) -> Reports/eye-in-the-village.png");

                            // VÅG 7.1: the same village at DUSK, which is the only condition under
                            // which the window law is visible. A hut whose owner is alive should glow;
                            // a hut whose owner has died should not. The claim is the picture.
                            Fas3LightRig.Apply("spring", "dusk");
                            CaptureRaw("village-at-dusk");
                            Fas3LightRig.Apply("spring", "day");
                            sb.AppendLine("     " + Emergence.Runtime.Fas3HearthGlow.Lit + " huts lit, "
                                          + Emergence.Runtime.Fas3HearthGlow.Unlit + " cold ("
                                          + Emergence.Runtime.Fas3HearthGlow.Panes
                                          + " panes switched on the last apply) -> Reports/village-at-dusk.png");
                            Check(Emergence.Runtime.Fas3HearthGlow.Lit + Emergence.Runtime.Fas3HearthGlow.Unlit > 0,
                                  "the window law ran over the huts ("
                                  + (Emergence.Runtime.Fas3HearthGlow.Lit + Emergence.Runtime.Fas3HearthGlow.Unlit) + " huts judged)");
                        }
                    }

                    // ---- and MEASURE the building, which no pass has done. A cottage is wider than
                    // it is tall; a tower is not. house-in-scene.png reads as a tower and the report
                    // has only ever printed its height, which cannot tell the two apart.
                    // measure the WHOLE HOUSE, not one renderer inside it. The first version of this
                    // took the tallest P_BLD* renderer, which is a wall MODULE - so it reported the
                    // body's proportions and not the building's, and a roof can change the answer.
                    Transform whole = null; float wholeH = 0f; Bounds wholeB = new Bounds();
                    if (hutRoot != null)
                        for (int i = 0; i < hutRoot.transform.childCount; i++)
                        {
                            var ch = hutRoot.transform.GetChild(i);
                            var rs7 = ch.GetComponentsInChildren<Renderer>();
                            if (rs7.Length == 0) continue;
                            var bb7 = rs7[0].bounds;
                            for (int k = 1; k < rs7.Length; k++) bb7.Encapsulate(rs7[k].bounds);
                            if (bb7.size.y > wholeH) { wholeH = bb7.size.y; whole = ch; wholeB = bb7; }
                        }
                    if (whole != null)
                    {
                        var hb = wholeB;
                        var tallB = whole;
                        float wide = Mathf.Max(hb.size.x, hb.size.z), narrow = Mathf.Min(hb.size.x, hb.size.z);
                        float ratio = hb.size.y / Mathf.Max(0.01f, wide);
                        sb.AppendLine("     the building measured: " + tallB.name + "  " + wide.ToString("F1") + " x "
                                      + narrow.ToString("F1") + " m footprint, " + hb.size.y.ToString("F1") + " m tall"
                                      + "  (height/width = " + ratio.ToString("F2") + "; a cottage is 0,5-0,9, a tower is >1,3)"
                                      + "  = " + (hb.size.y / 1.75f).ToString("F1") + "x a person");
                        Check(ratio < 1.30f, "the houses are cottages, not towers (height/width = " + ratio.ToString("F2") + ")");

                        // MEASURE THE WHOLE POOL, not the one that happened to be raised. The village
                        // picture shows a narrow A-frame spire beside a broad cottage, which means the
                        // pack's thirteen house variants are not one kind of building - so the fix is
                        // SELECTION, not a global multiplier that would shrink the good ones too.
                        var cat9 = EmergenceAssetCatalog.Load();
                        if (cat9 != null)
                        {
                            sb.AppendLine("     the pack's house pool, measured at the scale we raise them (" + 0.55f + "):");
                            for (int v = 1; v <= 14; v++)
                            {
                                var pf9 = cat9.Prefab("P_BLD_house_" + v.ToString("00"));
                                if (pf9 == null) { sb.AppendLine("       house_" + v.ToString("00") + ": absent"); continue; }
                                var inst = UnityEngine.Object.Instantiate(pf9);
                                inst.transform.position = new Vector3(0f, -5000f, 0f);
                                inst.transform.localScale = Vector3.one * 0.55f;
                                var rs9 = inst.GetComponentsInChildren<Renderer>();
                                if (rs9.Length > 0)
                                {
                                    var b9 = rs9[0].bounds;
                                    for (int k = 1; k < rs9.Length; k++) b9.Encapsulate(rs9[k].bounds);
                                    float w9 = Mathf.Max(b9.size.x, b9.size.z), n9 = Mathf.Min(b9.size.x, b9.size.z);
                                    float r9 = b9.size.y / Mathf.Max(0.01f, w9);
                                    sb.AppendLine("       house_" + v.ToString("00") + ": " + w9.ToString("F1") + " x " + n9.ToString("F1")
                                                  + " m, " + b9.size.y.ToString("F1") + " m tall, ratio " + r9.ToString("F2")
                                                  + "  = " + (b9.size.y / 1.75f).ToString("F1") + "x a person"
                                                  + (r9 > 1.30f ? "   <-- TOWER" : ""));
                                }
                                UnityEngine.Object.DestroyImmediate(inst);
                            }
                        }
                    }
                }

                // ---------- WHAT IT COSTS (D-223) ----------
                // The world went from 2 472 renderers to 8 717 in one pass and nobody measured it.
                // Every remaining item on the gap list ADDS to the scene — a windmill, a ruin, a
                // dollhouse interior — so the honest order is to price the world before extending it.
                // Editor Game-view numbers on the builder's machine are not a min-spec verdict and
                // are not claimed as one: they are the CALIBRATION ANCHOR and a trend we can watch.
                {
                    // TWO FAULTS IN THE FIRST VERSION OF THIS SECTION, both found by the number
                    // moving when the world had not:
                    //   (1) it sampled from WHATEVER POSE THE CAMERA HAPPENED TO END ON — the last
                    //       close-up, the shore, the village — so draw calls read 2 541 one run and
                    //       8 427 the next with an identical scene. A cost that depends on where the
                    //       lens is pointing is not a trend, it is noise wearing a number.
                    //   (2) it "averaged" sixty samples inside ONE frame, which is sixty copies of
                    //       the same reading. Stated honestly now as the single-frame reading it is.
                    // The pose is restored to the declared eye-level one so runs are comparable.
                    var pcam = Camera.main;
                    if (pcam != null && _perfPoseSet)
                    { pcam.transform.position = _perfPos; pcam.transform.rotation = _perfRot; pcam.Render(); }

                    long avgDc = UnityEditor.UnityStats.drawCalls;
                    long sp = UnityEditor.UnityStats.setPassCalls;
                    long tri = UnityEditor.UnityStats.triangles;
                    float ms = Time.smoothDeltaTime * 1000f;
                    sb.AppendLine();
                    sb.AppendLine("6. WHAT THE WORLD COSTS (one frame, from the DECLARED eye-level pose —");
                    sb.AppendLine("   an anchor on the builder's machine, never a min-spec verdict)");
                    sb.AppendLine("     renderers in scene: " + rendCount
                                  + "   draw calls: " + avgDc
                                  + "   set-pass: " + sp
                                  + "   triangles: " + (tri / 1000) + "k");
                    sb.AppendLine("     frame: " + ms.ToString("F1") + " ms ("
                                  + (ms > 0.01f ? (1000f / ms).ToString("F0") : "?") + " fps)"
                                  + "   quality '" + QualitySettings.names[QualitySettings.GetQualityLevel()] + "'");
                    // 2500 was the A6 budget set against a GTX 1660-class min spec. It is a threshold
                    // to WATCH, not a gate that should stop a pass — so it reports rather than fails,
                    // and says which it is out loud.
                    sb.AppendLine(avgDc <= 2500
                        ? "     within the A6 draw-call budget (2500, set against a GTX 1660-class min spec)"
                        : "     OVER the A6 draw-call budget of 2500 by " + (avgDc - 2500)
                          + " — batching/LOD work is owed before the scene grows again");
                    // WHAT THE NUMBER MEANS, so nobody has to re-derive it. 8 810 renderers producing
                    // 8 427 draw calls is ONE CALL PER RENDERER — that is not a heavy scene, it is an
                    // UNBATCHED one, and the 6 255 grass tufts are almost all of it. They were sown as
                    // individual objects precisely because the terrain detail system would not draw
                    // (D-215d), which fixed the picture and moved the cost here. The remedy is known
                    // and cheap: GPU instancing on the tuft materials, or merging tufts per tile into
                    // one mesh. It costs nothing at 2,8 ms on this machine and would cost a Steam Deck
                    // a great deal, so it is owed before min-spec, not before the next feature.
                    if (rendCount > 0)
                        sb.AppendLine("     " + (avgDc * 100 / Mathf.Max(1, rendCount)) + " draw calls per 100 renderers"
                                      + (avgDc > rendCount * 0.8f ? "  — essentially UNBATCHED (see the tufts)" : ""));
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
            // Shader carries these itself now; the UnityEditor.ShaderUtil pair is deprecated in
            // Unity 6 and its warning was one of thirteen that made the compile baseline flap.
            int count = m.shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
                if (m.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    var t = m.GetTexture(m.shader.GetPropertyName(i));
                    if (t != null) return t;
                }
            return null;
        }

        /// <summary>Is this a piece of the NATURAL world? Judged by WHAT IT IS, not by who hung it in
        /// the hierarchy. A boulder bedded into a hillside is geology whether the nature scatter, the
        /// codex reconciler or the dresser put it there; a house or a person sunk to the knee is a
        /// defect no matter how tidy its parent chain looks. Naming the exemption by ancestry was the
        /// narrower, more fragile version of the same idea — and it left one tree looking like a bug.</summary>
        static readonly string[] NaturalWords = { "tree", "bush", "rock", "cliff", "stone", "trunk", "log", "branch", "leaves", "foliage", "grass", "flower" };

        static bool IsNatural(Transform t)
        {
            for (var a = t; a != null; a = a.parent)
            {
                if (a.name == "Nature_Live") return true;
                var n = a.name.ToLowerInvariant();
                foreach (var w in NaturalWords) if (n.Contains(w)) return true;
            }
            return false;
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
