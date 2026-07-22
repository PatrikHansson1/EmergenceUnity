// EMERGENCE — FAS 3 increment 8 PROBE (D-141): the AUDIO LANE v1 — the bus speaks, verified.
//
// Boots the same self-composing opening (genesis wilderness + Fas3Onboarding — which now raises
// Fas3AudioDirector) and asserts the EAR: ambience is PLAYING from frame one, the first birth
// plays the soft tone, the first hut's milestone plays the chime — all counted by the director
// (bus -> PlayOneShot is the mechanism under test; speaker output needs Patrik's ears, logged).
// Menu: Emergence/Fas3/RUN AUDIO PROBE.  Headless: drop Reports/RUN_FAS3AUDIO.trigger.
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
    public static class Fas3AudioProbe
    {
        const long Seed = 8919;
        const double Watchdog = 220.0;
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS3AUDIO.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS3AUDIO_DONE.txt");
        const string Report   = "Reports/fas3-audio.txt";
        const string KeyPending = "emg.fas3aud.pending", KeyStart = "emg.fas3aud.start", KeyReport = "emg.fas3aud.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas3AudioDirector _audio;
        static bool _hutBeat;
        static int _childBusYear = -1;   // gate-review fix: the bus's own first-child year — the tone must match IT
        static string _ambNote = "", _birthNote = "", _hutNote = "", _arriveNote = "";

        static Fas3AudioProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas3/RUN AUDIO PROBE")]
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
                        Directory.CreateDirectory(Path.GetDirectoryName(Done));
                        File.WriteAllText(Done, "RUNNING (edit phase) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                        EditPhase();
                        return;
                    }
                }
                catch (Exception e) { SafeFail("arm: " + e.Message); }
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
                    Drive();
                    if (_phase == 99 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within watchdog");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 3 AUDIO PROBE (D-141): the bus speaks (procedural v0)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("owned audio assets: 3 Vefects fire loops only -> v0 is SYNTHESIZED (wind ambience + chimes), zero purchases");
            sb.AppendLine();

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _onb = null; _audio = null; _hutBeat = false; _childBusYear = -1;
            _ambNote = _birthNote = _hutNote = _arriveNote = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void OnBusEvent(PresentationEvent e)
        {
            if (e.Type == PresentationEventType.Milestone && e.Data == "the first hut") _hutBeat = true;
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a child is born" && _childBusYear < 0) _childBusYear = e.Year;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _audio = UnityEngine.Object.FindAnyObjectByType<Fas3AudioDirector>();
                if (_onb == null || _onb.Driver == null || _audio == null) return;
                PresentationEventBus.OnEvent += OnBusEvent;
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World;
            if (d.LastError.Length > 0) { SafeFail("driver: " + d.LastError); return; }

            if (_ambNote.Length == 0 && _frames > 10)
                _ambNote = $"ambience: {( _audio.AmbiencePlaying ? "PLAYING from frame one (OK)" : "SILENT (FAIL)")}";

            // genesis arrivals (y0) -> soft tone, counted APART from births (arrival ≠ birth, D-135 honesty)
            if (_arriveNote.Length == 0 && _audio.ArrivalTonesPlayed > 0)
                _arriveNote = $"genesis arrival tone: played x{_audio.ArrivalTonesPlayed} at applied y{w.LastAppliedYear} — arrival, NOT birth ({(w.LastAppliedYear == 0 ? "OK" : "FAIL: expected y0")})";

            // first REAL birth -> soft tone; the year must match the bus's own first-child year
            if (_birthNote.Length == 0 && _audio.BirthTonesPlayed > 0)
            {
                bool yearOk = _childBusYear >= 1 && w.LastAppliedYear == _childBusYear;
                _birthNote = $"birth tone: played x{_audio.BirthTonesPlayed} at applied y{w.LastAppliedYear}, bus first-child y{_childBusYear} ({(yearOk ? "OK" : "FAIL: tone/bus year mismatch")})";
            }

            // first hut milestone -> chime
            if (_hutBeat && _hutNote.Length == 0 && _audio.StingersPlayed > 0)
                _hutNote = $"milestone chime: played x{_audio.StingersPlayed} by first-hut year y{w.LastAppliedYear} (OK)";

            if (_ambNote.Length > 0 && _birthNote.Length > 0 && _hutNote.Length > 0 && _arriveNote.Length > 0) _phase = 99;
            if (w.LastAppliedYear >= 10) _phase = 99;   // safety horizon — report what came
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                PresentationEventBus.OnEvent -= OnBusEvent;
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine(_ambNote.Length > 0 ? _ambNote : "ambience: never evaluated (FAIL)");
                sb.AppendLine(_arriveNote.Length > 0 ? _arriveNote : "genesis arrival tone: NEVER PLAYED (FAIL)");
                sb.AppendLine(_birthNote.Length > 0 ? _birthNote : "birth tone: NEVER PLAYED (FAIL)");
                sb.AppendLine(_hutNote.Length > 0 ? _hutNote : "milestone chime: NEVER PLAYED (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: PlayOneShot/isPlaying is the mechanism proof; SPEAKER truth (levels, taste) needs Patrik's ears — logged On Patrik.");
                bool green = _ambNote.Contains("OK") && _birthNote.Contains("(OK)") && _hutNote.Contains("OK") && _arriveNote.Contains("(OK)") && !overtime;
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the Fas 0 bus finally speaks: wind under the wilderness, a tone for a birth, a chime for the first hut"
                                                   : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} stingers={( _audio != null ? _audio.StingersPlayed : -1)} births={( _audio != null ? _audio.BirthTonesPlayed : -1)}\nsee {Report}\n");
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
            try { PresentationEventBus.OnEvent -= OnBusEvent; } catch {}
            try { File.WriteAllText(Done, "ERROR " + msg + " — " + DateTime.Now.ToString("HH:mm:ss") + "\n"); } catch {}
            SessionState.SetInt(KeyPending, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
#endif
