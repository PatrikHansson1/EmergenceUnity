// EMERGENCE — Fas 1 increment 3: SHARP A6 PLAY-MODE PROBE.
//
// The static census counts every renderer unculled (worst case). The real budget number is the play-mode
// draw-call/SetPass/triangle count AFTER frustum culling + SRP batching. This probe enters play mode on the
// currently open scene (the dressed core scene), samples UnityStats over ~120 frames, writes the calibrated
// numbers, and EXITS play mode on its own. It is defensive: a hard watchdog force-exits play mode if the
// sample overruns, so a headless run can never leave the editor stuck in play mode.
//
// It only READS render stats — never the sim (D-078 r4). Golden master untouched.
//
// Menu: Emergence/Fas1/RUN A6 PLAY-MODE PROBE.  Headless: drop Reports/RUN_PERFPLAY.trigger.
//
// D-923 (R4, sprint review): an EMPTY trigger measures the open scene as it boots (year 0-1, the old
// number). A trigger whose body is an integer N measures the world AT MATURITY: after genesis is applied
// the probe scrubs to year N through the SAME path the player's timeline uses (Fas3PresentationClock
// .JumpToYear -> persisted checkpoint; the checkpoint is seeded from Assets/Emergence/WorldStates when
// the persistent dir lacks it), waits 30 frames, then samples. The report adds the live census
// (huts/agents/fences/soil/work-marks/trodden/codex) so draw calls can be read against what is on screen.
// Still read-only toward the sim: a checkpoint is applied state, never ticked here (D-078 r4).
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class PerfPlaymodeProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_PERFPLAY.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "PERFPLAY_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "perf-playmode.txt");

        const string KeyPending = "emg.perfplay.pending";   // survives the enter-playmode domain reload
        const string KeyStart   = "emg.perfplay.start";
        const string KeyYear    = "emg.perfplay.year";    // D-923: target year from the trigger body (0 = none)
        const string KeyHide    = "emg.perfplay.hide";    // D-923: "hide=A,B" — roots to deactivate after the jump (draw-call attribution)

        // sampling accumulators (fresh statics after the single enter-playmode reload; valid until we exit)
        static int _frames, _samples, _dcMax, _spMax;
        static long _dcSum, _spSum, _triSum;
        static float _msSum, _msMax;
        static int _jumpFrame = -1; static float _jumpMs; static string _jumpNote = "";

        // provisional A6 budget (from PerfSampler / D-107)
        const int BudgetDrawCalls = 2500, TargetFps = 60;

        static PerfPlaymodeProbe() { EditorApplication.update += Tick; }

        static void Tick()
        {
            // ARM (edit mode): trigger present, not already running, not mid-transition
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.5;
                try
                {
                    if (SessionState.GetInt(KeyPending, 0) == 0 && !EditorApplication.isPlayingOrWillChangePlaymode
                        && File.Exists(Trigger))
                    {
                        int year = 0; string hide = "";
                        try
                        {
                            // body: "<year> [hide=Root1,Root2]" — e.g. "120 hide=LiveFields" measures year 120 without the fence layer
                            foreach (var tok in File.ReadAllText(Trigger).Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (tok.StartsWith("hide=")) hide = tok.Substring(5);
                                else if (int.TryParse(tok, out int y)) year = y;
                            }
                        }
                        catch { year = 0; hide = ""; }
                        File.Delete(Trigger);
                        SessionState.SetInt(KeyYear, Mathf.Max(0, year));
                        SessionState.SetString(KeyHide, hide);
                        _frames = _samples = _dcMax = _spMax = 0;
                        _dcSum = _spSum = _triSum = 0; _msSum = _msMax = 0f; _jumpFrame = -1; _jumpMs = 0f; _jumpNote = "";
                        SessionState.SetInt(KeyPending, 1);
                        SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (entering play mode" + (year > 0 ? ", target year " + year : "") + ") " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditorApplication.EnterPlaymode();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;

            // hard watchdog regardless of play state (never leave the editor stuck)
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            int targetYear = SessionState.GetInt(KeyYear, 0);
            double elapsed = EditorApplication.timeSinceStartup - start;
            bool overtime = elapsed > (targetYear > 0 ? 120.0 : 25.0);

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    // D-123: on an unattended editor (focus lost/screen locked) the player loop never
                    // steps unless runInBackground is on — without this the probe hangs at frame 1.
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                    // D-923: scrub to the target year once genesis stands applied (same path as the player's timeline)
                    if (targetYear > 0 && _jumpFrame < 0 && _frames > 5) TryJump(targetYear, elapsed);
                    bool warm = _frames > 30 && (targetYear <= 0 || (_jumpFrame >= 0 && _frames > _jumpFrame + 30));
                    if (warm) // warm-up: skip first ~30 frames (shader compile / first cull), and 30 after a jump
                    {
                        int dc = UnityStats.drawCalls, sp = UnityStats.setPassCalls;
                        long tri = UnityStats.triangles;
                        float ms = Time.unscaledDeltaTime * 1000f;
                        _dcSum += dc; _spSum += sp; _triSum += tri; _msSum += ms;
                        if (dc > _dcMax) _dcMax = dc; if (sp > _spMax) _spMax = sp; if (ms > _msMax) _msMax = ms;
                        _samples++;
                    }
                    if (_samples >= 120 || overtime) Finish(overtime);
                }
                catch (Exception e) { SafeFail("sample: " + e.Message); }
            }
            else if (overtime)
            {
                // armed but never entered play mode → give up cleanly
                SafeFail("play mode did not start within 25s");
            }
        }

        static void TryJump(int year, double elapsed)
        {
            var w = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>();
            var d = UnityEngine.Object.FindAnyObjectByType<Fas3SimDriver>();
            var c = UnityEngine.Object.FindAnyObjectByType<Fas3PresentationClock>();
            if (w == null || d == null || c == null) { if (elapsed > 60) { _jumpFrame = _frames; _jumpNote = "NO JUMP: rig not found (world/driver/clock)"; } return; }
            if (w.LastAppliedYear < 0 || string.IsNullOrEmpty(d.CheckpointDir)) { if (elapsed > 60) { _jumpFrame = _frames; _jumpNote = "NO JUMP: genesis never applied within 60s"; } return; }
            string name = $"seq-{d.seed}-y{year:000}.json";
            string dst = Path.Combine(d.CheckpointDir, name);
            // seed the persistent grid from the repo: a current full export (world-<seed>-y<N>-full.json, engine of
            // today) wins over an old seq- checkpoint; a newer repo file overwrites a stale persistent copy.
            string ws = Path.Combine(Application.dataPath, "Emergence", "WorldStates");
            string srcFull = Path.Combine(ws, $"world-{d.seed}-y{year}-full.json"), srcSeq = Path.Combine(ws, name);
            string src = File.Exists(srcFull) ? srcFull : File.Exists(srcSeq) ? srcSeq : null;
            string srcNote = "persistent checkpoint";
            if (src != null && (!File.Exists(dst) || File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst))) { File.Copy(src, dst, true); srcNote = Path.GetFileName(src); }
            if (!File.Exists(dst)) { _jumpFrame = _frames; _jumpNote = "NO JUMP: no checkpoint for year " + year + " (persistent dir or Assets/Emergence/WorldStates)"; return; }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool ok = c.JumpToYear(year);
            sw.Stop();
            _jumpFrame = _frames; _jumpMs = (float)sw.Elapsed.TotalMilliseconds;
            _jumpNote = (ok ? $"jumped to year {year} at frame {_frames} (apply {_jumpMs:0} ms, applied year now {w.LastAppliedYear}, source {srcNote})"
                           : "JUMP FAILED: " + c.LastError);
            string hide = SessionState.GetString(KeyHide, "");
            if (hide.Length > 0)
            {
                var hidden = new StringBuilder();
                foreach (var nm in hide.Split(',')) { var go = GameObject.Find(nm.Trim()); if (go != null) { go.SetActive(false); hidden.Append(nm.Trim()).Append(' '); } else hidden.Append(nm.Trim()).Append("(not found) "); }
                _jumpNote += " | hidden: " + hidden.ToString().Trim();
            }
            Debug.Log("[PerfPlayProbe] " + _jumpNote);
        }

        static string Census()
        {
            var w = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>();
            if (w == null) return "world: (no Fas3WorldRuntime in scene)";
            return $"world: appliedYear={w.LastAppliedYear} huts={w.HutCount} agents={w.AgentCount} fences={w.FenceCount} soilTexels={w.SoilTexels} " +
                   $"workMarks={w.WorkMarkCount} codexPlaced={w.CodexPlacedCount} fires={w.FireCount} nature={w.NatureCount} " +
                   $"trodden=\"{Fas3TroddenPainter.LastNote}\"" + RootStats("LiveFields") + RootStats("Huts_Live") + RootStats("LiveWorkMarks");
        }

        // renderers / distinct meshes / combined (static-batched) meshes under a live root — shows whether batching took
        static string RootStats(string root)
        {
            var go = GameObject.Find(root); if (go == null) return "";
            var mfs = go.GetComponentsInChildren<MeshFilter>(false);
            var meshes = new System.Collections.Generic.HashSet<Mesh>(); int combined = 0;
            foreach (var mf in mfs) { if (mf.sharedMesh == null) continue; if (meshes.Add(mf.sharedMesh) && mf.sharedMesh.name.StartsWith("Combined Mesh")) combined++; }
            int rend = go.GetComponentsInChildren<Renderer>(false).Length;
            return $"\n  {root}: renderers={rend} meshFilters={mfs.Length} distinctMeshes={meshes.Count} combinedMeshes={combined}";
        }

        static void Finish(bool overtime)
        {
            try
            {
                int n = Mathf.Max(1, _samples);
                float dcAvg = _dcSum / (float)n, spAvg = _spSum / (float)n;
                float triAvg = _triSum / (float)n / 1_000_000f, msAvg = _msSum / n, fps = msAvg > 0.01f ? 1000f / msAvg : 0f;
                var sb = new StringBuilder();
                sb.AppendLine("EMERGENCE — A6 PLAY-MODE PROBE (Fas 1, increment 3)");
                sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("real render stats in play mode (after frustum culling + SRP batching) on the open scene —");
                sb.AppendLine("the sharp A6 number vs the static census. Read-only (D-078 r4).");
                sb.AppendLine($"samples={n}  (warm-up 30 frames skipped){(overtime ? "  [WATCHDOG cut]" : "")}");
                string census = ""; try { census = Census(); } catch (Exception e) { census = "world: census failed: " + e.Message; }
                if (_jumpNote.Length > 0) sb.AppendLine("maturity (D-923): " + _jumpNote);
                sb.AppendLine(census);
                sb.AppendLine();
                sb.AppendLine("metric                 avg        max        budget    verdict");
                sb.AppendLine($"draw calls             {dcAvg,-10:0}{_dcMax,-11}{BudgetDrawCalls,-10}{(dcAvg <= BudgetDrawCalls ? "OK" : "OVER")}");
                sb.AppendLine($"set-pass calls         {spAvg,-10:0}{_spMax,-11}{"-",-10}(info)");
                sb.AppendLine($"triangles (millions)   {triAvg,-10:0.0}{"-",-11}{8,-10}{(triAvg <= 8 ? "OK" : "OVER")}");
                sb.AppendLine($"frame ms / FPS         {msAvg,-10:0.0}{("max " + _msMax.ToString("0.0")),-11}{("FPS " + fps.ToString("0")),-10}{(fps >= TargetFps ? "OK" : "UNDER")}");
                // D-923: under the SRP Batcher a "draw call" is cheap (see set-pass) — the GPU time is what scales to
                // min-spec. Reference GPU 4070 Ti SUPER vs GTX 1660-class ≈ 3.7× raster throughput (ASSUMED, not measured).
                float minSpecMs = msAvg * 3.7f, minSpecFps = minSpecMs > 0.01f ? 1000f / minSpecMs : 0f;
                sb.AppendLine($"min-spec estimate      {minSpecMs,-10:0.0}{"(x3.7)",-11}{("FPS " + minSpecFps.ToString("0")),-10}{(minSpecFps >= TargetFps ? "OK (est.)" : "UNDER (est.)")}");
                sb.AppendLine();
                sb.AppendLine("NOTE: numbers reflect the editor Game view at its current resolution on the EP's GPU");
                sb.AppendLine("(4070 Ti SUPER reference; min-spec is GTX 1660-class). Use as the calibration anchor for A6;");
                sb.AppendLine("if draw calls are OVER, enforce LOD + culling + foliage instancing before Fas 2 adds agents.");
                if (dcAvg < 1f)
                    sb.AppendLine("WARNING: draw calls read ~0 — UnityStats needs a rendering Game view; re-run with the Game view visible.");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} samples={n} drawCallsAvg={dcAvg:0} dcMax={_dcMax} triAvgM={triAvg:0.0} fps={fps:0}{(overtime ? " (watchdog)" : "")}{(_jumpNote.Length > 0 ? " | " + _jumpNote : "")}\n{census}\nsee Reports/perf-playmode.txt\n");
                Debug.Log($"[PerfPlayProbe] done dcAvg={dcAvg:0} fps={fps:0} samples={n}");
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
