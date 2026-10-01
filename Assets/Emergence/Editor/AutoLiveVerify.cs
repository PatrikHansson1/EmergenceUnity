// EMERGENCE — LIVE VERIFY (D-897): does the BUILT live scene actually live? READ-ONLY (D-078 r4).
// Drop Reports/RUN_LIVEVERIFY.trigger. Opens EmergenceLive.unity, enters play mode, lets the scene's
// OWN rig run (Fas3SimDriver worker + Fas3PresentationClock consumes year snapshots -> Fas3WorldRuntime),
// and samples Fas3WorldRuntime.{LastAppliedYear, AgentCount, AppliedCount} at ~5s and ~18s. GREEN when the
// year advances (time flows), souls exist, and snapshots are being consumed. 32 s watchdog always exits
// play mode. Writes Reports/LIVEVERIFY_DONE.txt + Reports/live-verify.txt. Reads only; motor untouched.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class AutoLiveVerify
    {
        const string LiveScene = "Assets/Emergence/Scenes/EmergenceLive.unity";
        const double Watchdog = 75.0;   // D-936c: window 64 s + slack
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_LIVEVERIFY.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "LIVEVERIFY_DONE.txt");
        const string Report   = "Reports/live-verify.txt";
        const string KeyPending = "emg.liveverify.pending", KeyStart = "emg.liveverify.start";
        static int _yearA = -1, _yearB = -1, _agentsA = -1, _agentsB = -1, _appliedA = -1, _appliedB = -1;
        static bool _sampledA, _sampledB;
        static int _dYearA=-1,_dYearB=-1,_dTickA=-1,_dTickB=-1,_dBufA=-1,_dBufB=-1,_pYearA=-1,_pYearB=-1;
        static string _dErr="";
        static int _chronA=-1,_chronB=-1;
        static string _cueB="";
        static int _gazeB=-1;
        // R2 (D-921, review D-917): 110 964 InvalidOperationExceptions hid behind GREEN for weeks because no probe
        // counted them. Now every Exception/Error the play window logs from Emergence code is a RED, and the input
        // backend is asserted (legacy Input code + New-only setting = every key silently dead).
        static int _exceptions = 0; static string _firstException = ""; static bool _logHooked;

        static AutoLiveVerify() { EditorApplication.update += Tick; }

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
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (opening scene) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditorSceneManager.OpenScene(LiveScene, OpenSceneMode.Single);
                        _yearA = _yearB = _agentsA = _agentsB = _appliedA = _appliedB = -1; _sampledA = _sampledB = false; _dYearA=_dYearB=_dTickA=_dTickB=_dBufA=_dBufB=_pYearA=_pYearB=-1; _dErr=""; _chronA=_chronB=-1; _cueB=""; _gazeB=-1; _exceptions=0; _firstException=""; _logHooked=false;
                        SessionState.SetInt(KeyPending, 1);
                        SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
                        File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditorApplication.EnterPlaymode();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
            }

            if (SessionState.GetInt(KeyPending, 0) != 1) return;
            float start = SessionState.GetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            float t = (float)EditorApplication.timeSinceStartup - start;
            bool overtime = t > Watchdog;

            if (EditorApplication.isPlaying)
            {
                if (!_logHooked) { _logHooked = true; Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog; }
                try
                {
                    Application.runInBackground = true; EditorApplication.isPaused = false; EditorApplication.QueuePlayerLoopUpdate();
                    var w = UnityEngine.Object.FindAnyObjectByType<Fas3WorldRuntime>();
                    if (w != null)
                    {
                        var dr = UnityEngine.Object.FindAnyObjectByType<Fas3SimDriver>();
                        var ck = UnityEngine.Object.FindAnyObjectByType<Fas3PresentationClock>();
                        var fd = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                        if (dr != null && dr.LastError != null && dr.LastError.Length > 0) _dErr = dr.LastError;
                        if (!_sampledA && t >= 5f)  { _yearA = w.LastAppliedYear; _agentsA = w.AgentCount; _appliedA = w.AppliedCount;
                            if (dr!=null){_dYearA=dr.Year;_dTickA=dr.Tick;_dBufA=dr.BufferedYears;} if(ck!=null)_pYearA=ck.PresentationYear; if(fd!=null)_chronA=fd.Entries.Count; _sampledA = true;
                            CaptureRaw("live-verify-opening"); }   // D-936: what the player SEES at ~5 s (the founders frame) — evidence, not a claim
                        // D-936c: was 44 s — MEASURED a race with editor production (year 1 lands ~40 s cold; CHECK 2 of 3 runs 13:18–13:21), one year needs 6 s to play
                        if (!_sampledB && t >= 64f) { _yearB = w.LastAppliedYear; _agentsB = w.AgentCount; _appliedB = w.AppliedCount;
                            CaptureRaw("live-verify-end");
                            CaptureLakeShore("live-verify-lake");   // D-936c: the lake from its own shore, eye height — the ravine test in the BUILD scene
                            if (dr!=null){_dYearB=dr.Year;_dTickB=dr.Tick;_dBufB=dr.BufferedYears;} if(ck!=null)_pYearB=ck.PresentationYear; if(fd!=null)_chronB=fd.Entries.Count; { var md=UnityEngine.Object.FindAnyObjectByType<Fas6MusicDirector>(); if(md!=null)_cueB=md.CurrentCue; var gz=UnityEngine.Object.FindAnyObjectByType<Fas3GazeDirector>(); if(gz!=null)_gazeB=gz.GazeCount; } _sampledB = true; }
                    }
                    if (_sampledB || overtime) Finish(overtime, w != null);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within watchdog");
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error) return;
            string all = condition + "\n" + stackTrace;
            if (all.IndexOf("Emergence", StringComparison.Ordinal) < 0 && all.IndexOf("InvalidOperationException", StringComparison.Ordinal) < 0) return;
            if (all.IndexOf("Package Manager", StringComparison.Ordinal) >= 0) return;   // Unity ID / PM auth noise, not the game
            _exceptions++;
            if (_firstException.Length == 0) _firstException = condition.Length > 160 ? condition.Substring(0, 160) : condition;
        }

        /// <summary>0 = legacy, 1 = New-only, 2 = Both. New-only with legacy Input code in the project = dead keys (D-917).</summary>
        static int InputHandler()
        {
            try
            {
                var txt = File.ReadAllText(Path.Combine(Application.dataPath, "..", "ProjectSettings", "ProjectSettings.asset"));
                var m = System.Text.RegularExpressions.Regex.Match(txt, @"activeInputHandler:\s*(\d)");
                return m.Success ? int.Parse(m.Groups[1].Value) : -1;
            }
            catch { return -1; }
        }

        /// <summary>D-936: render the main camera to Reports/NAME.png (1280x720), the GroundCaptureProbe way.</summary>
        static void CaptureRaw(string name)
        {
            try
            {
                var cam = Camera.main; if (cam == null) return;
                const int w = 1280, h = 720;
                var rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "Reports", name + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
            }
            catch (Exception e) { Debug.LogWarning("[AutoLiveVerify] capture " + name + " failed: " + e.Message); }
        }

        /// <summary>D-936c: a temporary camera on the biggest water body's shore, 1.7 m up, looking across — the same
        /// framing as GroundCaptureProbe's eye-at-the-water.png, but in the LIVE scene the build ships.</summary>
        static void CaptureLakeShore(string name)
        {
            try
            {
                var wroot = GameObject.Find("Water"); if (wroot == null) return;
                Renderer big = null; float bigA = 0f;
                foreach (var r in wroot.GetComponentsInChildren<Renderer>())
                { float a = r.bounds.size.x * r.bounds.size.z; if (a > bigA) { bigA = a; big = r; } }
                if (big == null) return;
                var wb = big.bounds; var terrain = Terrain.activeTerrain;
                float reach = Mathf.Max(wb.extents.x, wb.extents.z) + 22f;
                var eye = new Vector3(wb.center.x - reach, 0f, wb.center.z - reach * 0.35f);
                eye.y = (terrain != null ? terrain.SampleHeight(eye) + terrain.transform.position.y : wb.center.y) + 1.7f;
                var go = new GameObject("TMP_lakeCam"); var cam = go.AddComponent<Camera>();
                cam.CopyFrom(Camera.main); cam.targetTexture = null; cam.fieldOfView = 55f;
                go.transform.position = eye; go.transform.LookAt(new Vector3(wb.center.x, wb.center.y + 0.5f, wb.center.z));
                const int w = 1280, h = 720;
                var rt = new RenderTexture(w, h, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(Application.dataPath, "..", "Reports", name + ".png"), tex.EncodeToPNG());
                UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(go);
            }
            catch (Exception e) { Debug.LogWarning("[AutoLiveVerify] lake capture failed: " + e.Message); }
        }

        static void Finish(bool overtime, bool foundWorld)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("EMERGENCE — LIVE VERIFY (D-897): does the built live scene actually live?");
                sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("world runtime found: " + foundWorld);
                sb.AppendLine("sample A (~5s):  year=" + _yearA + " agents=" + _agentsA + " applied=" + _appliedA);
                sb.AppendLine("sample B (~64s): year=" + _yearB + " agents=" + _agentsB + " applied=" + _appliedB);
                sb.AppendLine("chronicle entries: A=" + _chronA + " -> B=" + _chronB + " (the emergent story writing itself)");
                sb.AppendLine("music cue @B: \"" + _cueB + "\" (the score engaged: era->ambient at genesis)");
                sb.AppendLine("gaze dives @B: " + _gazeB + " (the eye noticed life — 0 is fine in a 1-year window)");
                sb.AppendLine("DRIVER A: producedYear=" + _dYearA + " tick=" + _dTickA + " buffered=" + _dBufA + " | clock.PresentationYear=" + _pYearA);
                sb.AppendLine("DRIVER B: producedYear=" + _dYearB + " tick=" + _dTickB + " buffered=" + _dBufB + " | clock.PresentationYear=" + _pYearB);
                sb.AppendLine("driver.LastError: " + (_dErr.Length>0 ? _dErr : "(none)"));
                bool timeFlows = _yearA >= 0 && _yearB > _yearA;
                bool soulsLive = _agentsB > 0;
                bool applying  = _appliedB > _appliedA && _appliedA >= 0;
                bool chronicleLives = _chronB > 0;
                Application.logMessageReceived -= OnLog;
                int ih = InputHandler();
                bool noExceptions = _exceptions == 0;
                bool inputOk = ih != 1;   // Both (2) or legacy (0) keep the keys alive; New-only (1) kills them silently
                bool green = foundWorld && timeFlows && soulsLive && applying && chronicleLives && !overtime && noExceptions && inputOk;
                sb.AppendLine("time flows: year " + _yearA + " -> " + _yearB + "  => " + timeFlows);
                sb.AppendLine("souls live: agents " + _agentsB + "  => " + soulsLive);
                sb.AppendLine("snapshots consumed: applied " + _appliedA + " -> " + _appliedB + "  => " + applying);
                sb.AppendLine("chronicle lives: entries " + _chronB + "  => " + chronicleLives);
                sb.AppendLine("no exceptions (R2): " + _exceptions + " from Emergence code in the play window  => " + noExceptions + (noExceptions ? "" : "   FIRST: " + _firstException));
                sb.AppendLine("input backend (R2): activeInputHandler=" + ih + " (0 legacy, 1 NEW-ONLY = keys dead, 2 both)  => " + inputOk);
                if (overtime) sb.AppendLine("WATCHDOG cut at " + Watchdog + "s");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the built scene LIVES: time advances, souls exist, snapshots consumed"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " verdict=" + (green ? "GREEN" : "CHECK")
                    + " yearA=" + _yearA + " yearB=" + _yearB + " agentsA=" + _agentsA + " agentsB=" + _agentsB
                    + " applied=" + _appliedA + "->" + _appliedB + " chron=" + _chronA + "->" + _chronB + " cue=\"" + _cueB + "\" gaze=" + _gazeB + " exc=" + _exceptions + " input=" + (inputOk ? "ok" : "NEW-ONLY") + (overtime ? " WATCHDOG" : "") + "\nsee " + Report + "\n");
                Debug.Log("[AutoLiveVerify] " + (green ? "GREEN" : "CHECK") + " y" + _yearA + "->" + _yearB + " agents" + _agentsB);
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
