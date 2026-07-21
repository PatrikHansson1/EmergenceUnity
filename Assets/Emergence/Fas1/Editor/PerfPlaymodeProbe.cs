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
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

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

        // sampling accumulators (fresh statics after the single enter-playmode reload; valid until we exit)
        static int _frames, _samples, _dcMax, _spMax;
        static long _dcSum, _spSum, _triSum;
        static float _msSum, _msMax;

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
                        File.Delete(Trigger);
                        _frames = _samples = _dcMax = _spMax = 0;
                        _dcSum = _spSum = _triSum = 0; _msSum = _msMax = 0f;
                        SessionState.SetInt(KeyPending, 1);
                        SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditorApplication.EnterPlaymode();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;

            // hard watchdog regardless of play state (never leave the editor stuck)
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            bool overtime = EditorApplication.timeSinceStartup - start > 25.0;

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
                    if (_frames > 30) // warm-up: skip first ~30 frames (shader compile / first cull)
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
                sb.AppendLine();
                sb.AppendLine("metric                 avg        max        budget    verdict");
                sb.AppendLine($"draw calls             {dcAvg,-10:0}{_dcMax,-11}{BudgetDrawCalls,-10}{(dcAvg <= BudgetDrawCalls ? "OK" : "OVER")}");
                sb.AppendLine($"set-pass calls         {spAvg,-10:0}{_spMax,-11}{"-",-10}(info)");
                sb.AppendLine($"triangles (millions)   {triAvg,-10:0.0}{"-",-11}{8,-10}{(triAvg <= 8 ? "OK" : "OVER")}");
                sb.AppendLine($"frame ms / FPS         {msAvg,-10:0.0}{("max " + _msMax.ToString("0.0")),-11}{("FPS " + fps.ToString("0")),-10}{(fps >= TargetFps ? "OK" : "UNDER")}");
                sb.AppendLine();
                sb.AppendLine("NOTE: numbers reflect the editor Game view at its current resolution on the EP's GPU");
                sb.AppendLine("(4070 Ti SUPER reference; min-spec is GTX 1660-class). Use as the calibration anchor for A6;");
                sb.AppendLine("if draw calls are OVER, enforce LOD + culling + foliage instancing before Fas 2 adds agents.");
                if (dcAvg < 1f)
                    sb.AppendLine("WARNING: draw calls read ~0 — UnityStats needs a rendering Game view; re-run with the Game view visible.");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} samples={n} drawCallsAvg={dcAvg:0} triAvgM={triAvg:0.0} fps={fps:0}{(overtime ? " (watchdog)" : "")}\nsee Reports/perf-playmode.txt\n");
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
