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
        const double Watchdog = 52.0;
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
                        _yearA = _yearB = _agentsA = _agentsB = _appliedA = _appliedB = -1; _sampledA = _sampledB = false; _dYearA=_dYearB=_dTickA=_dTickB=_dBufA=_dBufB=_pYearA=_pYearB=-1; _dErr=""; _chronA=_chronB=-1; _cueB="";
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
                            if (dr!=null){_dYearA=dr.Year;_dTickA=dr.Tick;_dBufA=dr.BufferedYears;} if(ck!=null)_pYearA=ck.PresentationYear; if(fd!=null)_chronA=fd.Entries.Count; _sampledA = true; }
                        if (!_sampledB && t >= 44f) { _yearB = w.LastAppliedYear; _agentsB = w.AgentCount; _appliedB = w.AppliedCount;
                            if (dr!=null){_dYearB=dr.Year;_dTickB=dr.Tick;_dBufB=dr.BufferedYears;} if(ck!=null)_pYearB=ck.PresentationYear; if(fd!=null)_chronB=fd.Entries.Count; { var md=UnityEngine.Object.FindAnyObjectByType<Fas6MusicDirector>(); if(md!=null)_cueB=md.CurrentCue; } _sampledB = true; }
                    }
                    if (_sampledB || overtime) Finish(overtime, w != null);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within watchdog");
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
                sb.AppendLine("sample B (~44s): year=" + _yearB + " agents=" + _agentsB + " applied=" + _appliedB);
                sb.AppendLine("chronicle entries: A=" + _chronA + " -> B=" + _chronB + " (the emergent story writing itself)");
                sb.AppendLine("music cue @B: \"" + _cueB + "\" (the score engaged: era->ambient at genesis)");
                sb.AppendLine("DRIVER A: producedYear=" + _dYearA + " tick=" + _dTickA + " buffered=" + _dBufA + " | clock.PresentationYear=" + _pYearA);
                sb.AppendLine("DRIVER B: producedYear=" + _dYearB + " tick=" + _dTickB + " buffered=" + _dBufB + " | clock.PresentationYear=" + _pYearB);
                sb.AppendLine("driver.LastError: " + (_dErr.Length>0 ? _dErr : "(none)"));
                bool timeFlows = _yearA >= 0 && _yearB > _yearA;
                bool soulsLive = _agentsB > 0;
                bool applying  = _appliedB > _appliedA && _appliedA >= 0;
                bool chronicleLives = _chronB > 0;
                bool green = foundWorld && timeFlows && soulsLive && applying && chronicleLives && !overtime;
                sb.AppendLine("time flows: year " + _yearA + " -> " + _yearB + "  => " + timeFlows);
                sb.AppendLine("souls live: agents " + _agentsB + "  => " + soulsLive);
                sb.AppendLine("snapshots consumed: applied " + _appliedA + " -> " + _appliedB + "  => " + applying);
                sb.AppendLine("chronicle lives: entries " + _chronB + "  => " + chronicleLives);
                if (overtime) sb.AppendLine("WATCHDOG cut at " + Watchdog + "s");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the built scene LIVES: time advances, souls exist, snapshots consumed"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, "DONE " + DateTime.Now.ToString("HH:mm:ss") + " verdict=" + (green ? "GREEN" : "CHECK")
                    + " yearA=" + _yearA + " yearB=" + _yearB + " agentsA=" + _agentsA + " agentsB=" + _agentsB
                    + " applied=" + _appliedA + "->" + _appliedB + " chron=" + _chronA + "->" + _chronB + " cue=\"" + _cueB + "\"" + (overtime ? " WATCHDOG" : "") + "\nsee " + Report + "\n");
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
