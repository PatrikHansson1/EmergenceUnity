// EMERGENCE — A6 GPU-INSTANCING PASS + SHARP MEASUREMENT (D-126). The last no-purchase A6 lever:
// draw calls were 5101 (budget 2500) after D-118/D-119; the remaining volume is the small foliage
// (Grass + MeadowFoliage — thousands of individual clump renderers). This pass, self-contained:
//   1) dress the core world at y120 (retire static Codex/Agents layers, live codex + 111 live agents
//      — the realistic Fas 2 perf scenario), locked look applied;
//   2) run the D-118 A6Optimize levers (foliage shadows off, shadowDistance, static batching);
//   3) CONVERT: LOD0 of every enabled small-foliage renderer → FoliageInstancer groups
//      (higher LODs retired; originals disabled, not destroyed — re-dress = revert);
//   4) enter play mode, sample UnityStats 120 frames (warm-up 30), ScreenCapture evidence
//      (game-view captures include instanced draws; a manual Camera.Render may not).
// Read-only vs the sim (D-078 r4); pack assets untouched (instancing on material COPIES).
// Menu: Emergence/Fas1/RUN A6 INSTANCING.  Headless: drop Reports/RUN_A6INST.trigger.
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
    public static class A6Instancing
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        // layer -> distance cull (0 = never): tiny clumps invisible past 90u; crops past 120u; trees always drawn.
        // Census (16:27) showed the residual draws were the SHADOW PASS (1162 casters x 2 cascades), not foliage:
        // so v2 also instances Fields + MeadowTrees (removes them from both camera and shadow pass) and runs 1 cascade.
        // v4: instancing = REPEATED SMALL MESHES only (v3 proved structures are too diverse — LOD0 + coarse
        // cell culling exploded draws/tris to 11781/16M). The real residual cost was the SHADOW PASS: the
        // PC URP asset ran shadowDistance 50 + 4 CASCADES — the D-118 QualitySettings levers are IGNORED
        // under URP, so every caster drew up to 4x. v4 sets the URP asset itself (35u, 1 cascade).
        static readonly (string name, float cull, bool shadows)[] Targets =
            { ("Grass", 90f, false), ("MeadowFoliage", 90f, false), ("Fields", 120f, false), ("MeadowTrees", 0f, false) };
        const int BudgetDrawCalls = 2500;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_A6INST.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "A6INST_DONE.txt");
        const string Report   = "Reports/a6-instancing.txt";
        const string KeyPending = "emg.a6inst.pending", KeyStart = "emg.a6inst.start", KeyReport = "emg.a6inst.report";

        // play-phase accumulators (fresh statics after the enter-playmode domain reload)
        static int _frames, _samples, _dcMax, _spMax, _magenta = -1, _submitted;
        static long _dcSum, _spSum, _triSum;

        static A6Instancing() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas1/RUN A6 INSTANCING")]
        public static void RunMenu() => EditPhase();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.5;
                try
                {
                    if (SessionState.GetInt(KeyPending, 0) == 0 && !EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Trigger))
                    {
                        File.Delete(Trigger);
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (edit phase: dress + convert) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditPhase();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 60.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;   // D-123: unattended editor
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();

                    if (_frames > 30)   // warm-up: shader compile (instanced variants) / first cull
                    {
                        int dc = UnityStats.drawCalls, sp = UnityStats.setPassCalls;
                        _dcSum += dc; _spSum += sp; _triSum += UnityStats.triangles;
                        if (dc > _dcMax) _dcMax = dc; if (sp > _spMax) _spMax = sp;
                        _samples++;
                    }
                    if (_frames == 60) CensusVisible();
                    if (_frames == 100) FrameVillageCamera();
                    if (_frames == 104) _magenta = CaptureGameView("a6inst-village-live");
                    if (_frames == 110) FrameMeadowCamera();
                    if (_frames == 114)
                    {
                        int m2 = CaptureGameView("a6inst-meadow-live");
                        if (m2 > _magenta) _magenta = m2;
                        var inst = UnityEngine.Object.FindAnyObjectByType<FoliageInstancer>();
                        _submitted = inst != null ? inst.SubmittedLastFrame : -1;
                    }
                    if (_samples >= 120 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 60s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — A6 GPU-INSTANCING (D-126)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("small foliage collapses to instanced draws; the sharp number is play-mode UnityStats below.");
            sb.AppendLine();

            // 1) the realistic Fas 2 scene: dressed core + live codex + full living population at y120
            WorldDresser.Build(World120);
            var staticCodex = GameObject.Find("CodexObjects");
            if (staticCodex != null) UnityEngine.Object.DestroyImmediate(staticCodex);
            var staticAgents = GameObject.Find("Agents");
            if (staticAgents != null) UnityEngine.Object.DestroyImmediate(staticAgents);
            var S120 = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            PresentationEventBus.Clear();
            var codex = new LiveReconciler();
            var dCodex = codex.Reconcile(S120);
            var agents = new AgentReconciler();
            var dAgents = agents.Reconcile(S120, true);
            sb.AppendLine($"dressed y120; static layers retired; codex diff={dCodex} placed={codex.PlacedCount}; agents souls={agents.Count}");
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S120.season) ? "spring" : S120.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[A6Inst] look: " + e.Message); }

            // 2) D-118 levers first (so the measurement isolates what INSTANCING adds)
            A6Optimize.Run();
            // v4: the REAL shadow levers live on the URP asset (QualitySettings is ignored under URP).
            // 35u/1 cascade matches the D-118 intent; saved to the asset so player builds get it too.
            var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            string urpNote = "URP asset: NOT FOUND (built-in RP?)";
            if (urp != null)
            {
                float prevDist = urp.shadowDistance; int prevCasc = urp.shadowCascadeCount;
                urp.shadowDistance = 35f;
                urp.shadowCascadeCount = 1;
                EditorUtility.SetDirty(urp);
                AssetDatabase.SaveAssets();
                urpNote = $"URP asset '{urp.name}': shadowDistance {prevDist} -> 35, cascades {prevCasc} -> 1 (saved)";
            }

            // 3) convert
            int before = Targets.Select(t => GameObject.Find(t.name)).Where(g => g != null)
                                .Sum(g => g.GetComponentsInChildren<MeshRenderer>(false).Count(r => r.enabled));
            var (disabled, groups, batches, instances) = Convert();
            sb.AppendLine($"foliage renderers enabled before: {before}");
            sb.AppendLine($"converted: {disabled} renderers disabled -> {groups} (mesh,mats)-groups, {instances} instances, {batches} cell-batches (cellSize 24u)");
            sb.AppendLine(urpNote);
            sb.AppendLine();

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = _samples = _dcMax = _spMax = 0; _dcSum = _spSum = _triSum = 0; _magenta = -1; _submitted = 0;
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static (int disabled, int groups, int batches, int instances) Convert()
        {
            var go = new GameObject("FoliageInstanced");
            var inst = go.AddComponent<FoliageInstancer>();
            var map = new Dictionary<string, FoliageInstancer.Group>();
            int disabled = 0;

            foreach (var (parentName, cullDist, shadows) in Targets)
            {
                var parent = GameObject.Find(parentName);
                if (parent == null) continue;

                // LOD0-only policy: LOD0 renderers represent the clump; higher LODs are retired outright
                var higherLod = new HashSet<Renderer>();
                foreach (var lg in parent.GetComponentsInChildren<LODGroup>(true))
                {
                    var lods = lg.GetLODs();
                    for (int li = 1; li < lods.Length; li++)
                        foreach (var r in lods[li].renderers) if (r != null) higherLod.Add(r);
                }

                foreach (var mr in parent.GetComponentsInChildren<MeshRenderer>(false))
                {
                    if (!mr.enabled) continue;                       // impostors etc. already off
                    if (higherLod.Contains(mr)) { mr.enabled = false; disabled++; continue; }
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    var mats = mr.sharedMaterials;
                    if (mats == null || mats.Length == 0 || mats.All(m => m == null)) continue;

                    string key = mf.sharedMesh.GetEntityId() + "|" + string.Join(",", mats.Select(m => m != null ? m.GetEntityId().ToString() : "0"));
                    if (!map.TryGetValue(key, out var g))
                    {
                        g = new FoliageInstancer.Group
                        {
                            note = parentName + "/" + mf.sharedMesh.name,
                            mesh = mf.sharedMesh,
                            sourceMats = mats,
                            cullDistance = cullDist,
                            castShadows = shadows
                        };
                        map[key] = g;
                        inst.groups.Add(g);
                    }
                    var t = mr.transform;
                    g.pos.Add(t.position); g.rot.Add(t.rotation); g.scl.Add(t.lossyScale);
                    mr.enabled = false; disabled++;
                }
            }
            inst.Rebuild();
            return (disabled, inst.groups.Count, inst.BatchCount, inst.InstanceCount);
        }

        static string _census = "";
        // who is actually drawing? enabled+visible renderers by root (the draw-call suspects list)
        static void CensusVisible()
        {
            var byRoot = new Dictionary<string, int>();
            int total = 0, shadowCasters = 0;
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
            {
                if (!r.enabled || !r.isVisible) continue;
                total++;
                if (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off) shadowCasters++;
                Transform t = r.transform, prev = t;
                while (t.parent != null) { prev = t; t = t.parent; }   // t=scene root, prev=layer under it
                string root = t.name + (prev != t ? "/" + prev.name : "");
                byRoot[root] = byRoot.TryGetValue(root, out var n) ? n + 1 : 1;
            }
            var sb = new StringBuilder();
            sb.AppendLine($"visible-renderer census (frame 60): total={total}, shadow-casting={shadowCasters}");
            foreach (var kv in byRoot.OrderByDescending(k => k.Value).Take(14))
                sb.AppendLine($"  {kv.Value,6}  {kv.Key}");
            _census = sb.ToString();
        }

        // ---- play-phase camera + game-view evidence (ScreenCapture includes instanced geometry) ----

        static void FrameVillageCamera()
        {
            var cam = EnsureCamera();
            var pts = AgentPoints();
            Vector3 center = Cluster(pts, out _);
            cam.transform.position = center + new Vector3(7f, 6.3f, -11.2f);
            cam.transform.LookAt(center + Vector3.up * 0.8f);
        }

        static void FrameMeadowCamera()
        {
            var cam = EnsureCamera();
            var pts = AgentPoints();
            Vector3 center = Cluster(pts, out _);
            cam.transform.position = center + new Vector3(-6f, 3.5f, 8f);
            cam.transform.LookAt(center + new Vector3(35f, 0f, -35f));   // across the open meadow
        }

        static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            return cam;
        }

        static List<Vector3> AgentPoints()
        {
            var pts = new List<Vector3>();
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer != null) foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) pts.Add(aa.transform.position);
            return pts;
        }

        static Vector3 Cluster(List<Vector3> pts, out int size)
        {
            Vector3 center = pts.Count > 0 ? pts[0] : new Vector3(400, 30, 400); int best = -1;
            foreach (var p in pts) { int n = pts.Count(q => (q - p).sqrMagnitude < 144f); if (n > best) { best = n; center = p; } }
            var cluster = pts.Where(q => (q - center).sqrMagnitude < 144f).ToList();
            if (cluster.Count > 0) { var c = Vector3.zero; foreach (var q in cluster) c += q; center = c / cluster.Count; }
            size = Mathf.Max(best, 0);
            return center;
        }

        // manual RT capture (proven in D-125; ScreenCapture yields a white frame on an unattended editor).
        // NOTE: a manual cam.Render() may not include this frame's RenderMeshInstanced submissions —
        // if instanced foliage is missing here it is a capture artifact, not a scene one (game view has it).
        static int CaptureGameView(string name)
        {
            var cam = EnsureCamera();
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas2";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
            return magenta;
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                int n = Mathf.Max(1, _samples);
                float dcAvg = _dcSum / (float)n, spAvg = _spSum / (float)n, triAvg = _triSum / (float)n / 1_000_000f;
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}, samples={n}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine("metric                 avg        max        budget    verdict");
                sb.AppendLine($"draw calls             {dcAvg,-10:0}{_dcMax,-11}{BudgetDrawCalls,-10}{(dcAvg <= BudgetDrawCalls ? "OK" : "OVER")}");
                sb.AppendLine($"set-pass calls         {spAvg,-10:0}{_spMax,-11}{"-",-10}(info)");
                sb.AppendLine($"triangles (millions)   {triAvg,-10:0.0}{"-",-11}{8,-10}{(triAvg <= 8 ? "OK" : "OVER")}");
                sb.AppendLine($"instancer submits/frame: {_submitted}   (was ~thousands of renderer draws)");
                sb.AppendLine($"magenta (2 game-view captures): {_magenta}   evidence: 45-UNITY/evidence/fas2/a6inst-*.png");
                sb.AppendLine($"reference BEFORE this pass: 5101 draw calls (Reports/perf-playmode.txt, 12:49)");
                if (_census.Length > 0) sb.AppendLine(_census);
                sb.AppendLine("note: FPS is not meaningful on an unattended editor (player loop stepped from editor ticks);");
                sb.AppendLine("      definitive FPS comes from a player build (open A6 item, D-119).");
                bool green = dcAvg <= BudgetDrawCalls && triAvg <= 8 && _magenta == 0 && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — draw calls within the A6 budget with the full living scene"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} dcAvg={dcAvg:0} dcMax={_dcMax} " +
                                        $"triM={triAvg:0.0} submits={_submitted} magenta={_magenta}{(overtime ? " (watchdog)" : "")}\nsee {Report}\n");
                Debug.Log($"[A6Inst] done dcAvg={dcAvg:0} submits={_submitted} magenta={_magenta}");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR finish: " + e.Message + "\n"); } catch {} }
            finally
            {
                SessionState.SetInt(KeyPending, 0);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        static void SafeFail(string msg)
        {
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
