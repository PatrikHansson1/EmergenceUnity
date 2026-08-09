// EMERGENCE — FAS 7 PROBE: R2 ink. 1 CONSUMED — the body reads the engine's verb + era canon (TD-078).
//
// Engine 2.3.2 (TD-076) additively exports `eraName` (era canon moved INTO the engine) and
// `agents[].verb` (15 canonical work verbs). This probe proves the body consumes both under
// THE TWO NEW LAWS and that backward compatibility holds:
//   1. LIVE era canon: after a few live years the applied state bears eraName != "" and
//      WorldEras.Name(S) returns the ENGINE name ("The First Morning"), never the interim
//      name ("dawn") and never the season;
//   2. the feed shows the engine name: every chronicle entry's era string is the engine's,
//      none empty, none interim;
//   3. the almanac tile shows the engine name (recorder sampled eraName at apply time);
//   4. LIVE verb law: every exported soul bears a non-empty verb and every settled body's
//      ACTUAL animator state == AgentTaskRead.StateFor(verb, task, canWork);
//   5. FIXTURE world-8919-y006-r2ink1 (REAL 2.3.x export bearing the new fields) through the
//      SAME Fas3WorldRuntime.Apply path: every body's animator state == independently recomputed
//      from the verb; the gather-soul PROVES the verb drives (task law alone would say Walk,
//      the verb says Work);
//   6. BACKWARD fixture seq-8919-y055 (pre-R2 export, NO eraName/verb): verbs land empty, every
//      body falls back to the task classification, and the era-name law still yields the
//      non-empty interim name — no consumer can ever show an empty era string;
//   7. clock honesty: fixtures ride a paused clock; tps untouched on resume;
//   8. evidence PNG via the SHARED framing law (subjects[0] = PRIMARY = the non-idle-verb soul),
//      blankness-guarded AND humanly looked at.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// TempoFor (D-159) and the attend-gaze law are UNTOUCHED by R2 ink. 1 — regression rides Fas6Expr.
// Menu: Emergence/Fas7/RUN R2 BODY PROBE.  Headless: drop Reports/RUN_FAS7R2.trigger.
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
    public static class Fas7R2BodyProbe
    {
        const long Seed = 8919;
        const double Watchdog = 420.0;
        const int LiveYear = 3;          // live era/verb checks early — fixtures carry the field burden

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7R2.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS7R2_DONE.txt");
        const string Report   = "Reports/fas7-r2body.txt";
        const string Png      = "Reports/fas7-r2body.png";
        const string PngEra   = "Reports/fas7-r2body-era.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixtureR2   = "Assets/Emergence/WorldStates/world-8919-y006-r2ink1.json";  // real 2.3.x export WITH eraName+verb
        const string FixtureOld  = "Assets/Emergence/WorldStates/seq-8919-y055.json";           // real pre-R2 export WITHOUT the fields
        const string KeyPending = "emg.fas7r2.pending", KeyStart = "emg.fas7r2.start", KeyReport = "emg.fas7r2.report";

        static int _frames, _phase, _waitFrames;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas5AlmanacView _view;
        static float _tpsBefore;
        static WorldState _fx;
        static float _settleStart = -1f, _grabAskedAt;
        // measurement-time stamps (R1 law)
        static string _eraLive = "", _tileEra = "";
        static int _liveVerbs = -1, _liveTotal = -1, _liveStateOk = -1, _liveStateChecked = -1;
        static int _feedOk = -1, _feedTotal = -1;
        static int _fxOk = -1, _fxChecked = -1, _fxNonIdle = -1, _fxVerbCarried = -1;
        static int _oldOk = -1, _oldChecked = -1, _oldVerbEmpty = -1;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n6b = "", _n7 = "", _n8 = "";

        static Fas7R2BodyProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN R2 BODY PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 7 PROBE: R2 ink. 1 consumed — engine era canon (eraName) + verb-driven animation");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = applied WorldState (eraName, agents[].verb) — read-only; presentation follows applied state");
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
            _frames = 0; _phase = 0; _waitFrames = 0; _onb = null; _feed = null; _view = null;
            _tpsBefore = 0f; _fx = null; _settleStart = -1f; _grabAskedAt = 0f;
            _eraLive = _tileEra = "";
            _liveVerbs = _liveTotal = _liveStateOk = _liveStateChecked = _feedOk = _feedTotal = -1;
            _fxOk = _fxChecked = _fxNonIdle = _fxVerbCarried = -1;
            _oldOk = _oldChecked = _oldVerbEmpty = -1;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n6b = _n7 = _n8 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static AgentAnimator Find(int id)
        {
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer == null) return null;
            foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) if (aa.agentId == id) return aa;
            return null;
        }

        /// <summary>Settled = not gliding and the animator is not mid-crossfade (the state IS the state).</summary>
        static bool Settled(AgentAnimator aa, out Animator an)
        {
            an = aa != null ? aa.GetComponentInChildren<Animator>() : null;
            return aa != null && an != null && !aa.InTransit && !an.IsInTransition(0);
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null || _feed == null || _view == null) return;
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // LIVE: era canon in state/feed/almanac + verb law on settled bodies
            {
                var S = w.LastState;
                if (S == null || w.LastAppliedYear < LiveYear) return;

                // need a few settled bodies for the honest animator-state read (glides come and go live)
                int total = 0, verbs = 0, stateChecked = 0, stateOk = 0;
                var layer = GameObject.Find(AgentReconciler.LayerName);
                if (layer != null)
                    foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                    {
                        total++;
                        if (aa.verb != "") verbs++;
                        if (Settled(aa, out var an))
                        {
                            stateChecked++;
                            if (an.GetCurrentAnimatorStateInfo(0).IsName(AgentTaskRead.StateFor(aa.verb, aa.task, aa.canWork))) stateOk++;
                        }
                    }
                if (stateChecked < 3 && ++_waitFrames < 600) return;   // bounded wait for a settled sample

                // 1. era canon live: engine name in the applied state, never interim, never season
                _eraLive = S.eraName ?? "";
                string interim = WorldEras.Name(S.era);
                bool eraOk = _eraLive.Length > 0 && WorldEras.Name(S) == _eraLive && _eraLive != interim && _eraLive != S.season;
                _n1 = $"live era canon @y{w.LastAppliedYear}: state eraName='{_eraLive}' non-empty, WorldEras.Name(S)=='{WorldEras.Name(S)}', != interim '{interim}', != season '{S.season}' ({(eraOk ? "OK" : "FAIL")})";

                // 2. the feed shows ENGINE names — no entry empty, none interim (entries keep the era
                // they were STAMPED in, so earlier engine names may coexist; interim names may not)
                int fTotal = 0, fEngine = 0, fEmpty = 0, fInterim = 0;
                foreach (var e in _feed.Entries)
                {
                    fTotal++;
                    if (string.IsNullOrEmpty(e.era)) fEmpty++;
                    else if (e.era == _eraLive) fEngine++;
                    for (int i = 0; i < 7; i++) if (e.era == WorldEras.Name(i)) { fInterim++; break; }
                }
                _feedOk = fEngine; _feedTotal = fTotal;   // stamped (R1)
                bool feedOk = fTotal > 0 && fEmpty == 0 && fInterim == 0 && fEngine > 0;
                var fdump = new StringBuilder();
                foreach (var e in _feed.Entries) fdump.Append($" [y{e.year}|{e.era}|{e.kind}]");
                _n2 = $"feed: {fEngine}/{fTotal} entries carry the current engine name '{_eraLive}', empty={fEmpty}, interim-named={fInterim} ({(feedOk ? "OK" : "FAIL")}) —{fdump}";

                // 3. the almanac tile shows the engine name (recorder sampled eraName at apply time)
                _tpsBefore = c.ticksPerSecond;
                c.paused = true;   // freeze — fixtures ride the SAME Apply path with nothing racing them
                _view.OpenAlmanac(); _view.RefreshNow();
                _tileEra = _view.TileEra;   // stamped (R1)
                bool tileOk = _tileEra == _eraLive && _tileEra != interim && _tileEra != S.season;
                _view.CloseAlmanac();
                _n3 = $"almanac tile: '{_tileEra}' == engine '{_eraLive}', never interim/season ({(tileOk ? "OK" : "FAIL")})";

                // 4. live verb law: every exported soul bears a verb; settled bodies sit in the verb state
                _liveVerbs = verbs; _liveTotal = total; _liveStateOk = stateOk; _liveStateChecked = stateChecked;   // stamped (R1)
                bool verbOk = total > 0 && verbs == total && stateChecked > 0 && stateOk == stateChecked;
                _n4 = $"live verb law: verbs {verbs}/{total} non-empty, settled bodies in StateFor(verb,task,canWork) {stateOk}/{stateChecked} ({(verbOk ? "OK" : "FAIL")})";

                // FIXTURE with the new fields through the SAME Apply path (injection: chronicle silent)
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureR2));
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0; _settleStart = Time.unscaledTime;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // R2 fixture: every body's state == independently recomputed from the verb
            {
                if (++_waitFrames < 5) return;   // spawned bodies need Start() to run
                bool allSettled = true;
                foreach (var a in _fx.agents) { if (!Settled(Find(a.id), out _)) { allSettled = false; break; } }
                if (!allSettled && Time.unscaledTime - _settleStart < 75f) return;   // glides are bounded (MaxGlide/speed)

                int checkedN = 0, okN = 0, nonIdle = 0, verbCarried = 0;
                string proofSoul = "";
                AgentAnimator primary = null;
                foreach (var a in _fx.agents)
                {
                    var aa = Find(a.id);
                    if (!Settled(aa, out var an)) continue;
                    checkedN++;
                    bool adult = AgentReconciler.Band(a.age) == "adult";
                    string want = AgentTaskRead.StateFor(a.verb, a.task, adult);   // independent recompute
                    if (an.GetCurrentAnimatorStateInfo(0).IsName(want)) okN++;
                    if (aa.verb == (a.verb ?? "")) verbCarried++;
                    if ((a.verb ?? "") != "idle" && (a.verb ?? "") != "" && (a.verb ?? "") != "grow")
                    {
                        nonIdle++;
                        if (primary == null)
                        {
                            primary = aa;   // PRIMARY evidence subject: a soul with a non-idle verb
                            string taskLaw = AgentTaskRead.StateFor(a.task, adult);
                            proofSoul = $"'{a.name}' verb='{a.verb}' task='{a.task}' -> state {want} (task law alone would say {taskLaw})";
                        }
                    }
                }
                _fxChecked = checkedN; _fxOk = okN; _fxNonIdle = nonIdle; _fxVerbCarried = verbCarried;   // stamped (R1)
                bool ok = checkedN == _fx.agents.Length && okN == checkedN && verbCarried == checkedN && nonIdle > 0;
                _n5 = $"fixture r2ink1 (riktig 2.3.x-export, samma Apply-väg, {_fx.agents.Length} själar): state==recomputed-from-verb {okN}/{checkedN} (alla), verb carried {verbCarried}/{checkedN}, non-idle verbs={nonIdle}>0; verb DRIVES: {proofSoul} ({(ok ? "OK" : "FAIL")})";

                // evidence: the SHARED framing law — subjects[0] = PRIMARY = the non-idle-verb soul.
                // SINGLE subject deliberately: the claim under proof is the verb IN THE BODY (the work
                // pose), so the close 8-candidate framing carries the evidence — a pair-frame against a
                // distant neighbor shrinks the soul to a dot (first-run eye lesson, D-008 school).
                if (primary == null) { SafeFail("no non-idle-verb soul found for evidence"); return; }
                var soulPos = primary.transform.position;
                Vector3 lookAt;
                Vector3 pick = Emergence.Runtime.EvidenceFraming.FrameSubjects(out lookAt, soulPos);
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(lookAt); }
                // TD-078-review condition: TWO honest frames — the almanac modal dims the world, so
                // the work POSE and the READABLE era name cannot share one image without lying.
                // Frame 1 (Png): the pose, almanac closed. Frame 2 (PngEra): almanac open, the
                // engine era name readable in UI. The report claims exactly what each frame shows.
                var g = new GameObject("Fas7R2Grabber").AddComponent<Fas4NativeGrabber>();
                g.Path = Png; g.OnGrabbed = note =>
                {
                    _n6 = "evidence(pose) " + note;
                    try
                    {
                        _view.OpenAlmanac(); _view.RefreshNow();
                        var g2 = new GameObject("Fas7R2GrabberEra").AddComponent<Fas4NativeGrabber>();
                        g2.Path = PngEra; g2.OnGrabbed = note2 => { _n6b = "evidence(era-UI) " + note2; try { _view.CloseAlmanac(); } catch { } };
                    }
                    catch (Exception e) { _n6b = "evidence(era-UI) FAIL: " + e.Message; }
                };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // wait for the grab, then the BACKWARD fixture
            {
                if ((_n6.Length == 0 || _n6b.Length == 0) && Time.unscaledTime - _grabAskedAt < 16f) return;
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureOld));
                Fas3WorldRuntime.FixtureInjection = true;   // injection: chronicle silent
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0; _settleStart = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // pre-R2 fixture: fallback law — task classification + interim era name
            {
                if (++_waitFrames < 5) return;
                bool allSettled = true;
                foreach (var a in _fx.agents) { if (!Settled(Find(a.id), out _)) { allSettled = false; break; } }
                if (!allSettled && Time.unscaledTime - _settleStart < 90f) return;

                int checkedN = 0, okN = 0, verbEmpty = 0;
                foreach (var a in _fx.agents)
                {
                    var aa = Find(a.id);
                    if (!Settled(aa, out var an)) continue;
                    checkedN++;
                    if (aa.verb == "") verbEmpty++;
                    bool adult = AgentReconciler.Band(a.age) == "adult";
                    if (an.GetCurrentAnimatorStateInfo(0).IsName(AgentTaskRead.StateFor(a.task, adult))) okN++;   // OLD law
                }
                _oldChecked = checkedN; _oldOk = okN; _oldVerbEmpty = verbEmpty;   // stamped (R1)
                var S = w.LastState;
                string eraFallback = WorldEras.Name(S);   // the ONE law every UI reads through
                bool eraOk = S != null && string.IsNullOrEmpty(S.eraName) && eraFallback == WorldEras.Name(S.era) && eraFallback.Length > 0;
                bool ok = checkedN >= 20 && okN == checkedN && verbEmpty == checkedN && eraOk;
                _n7 = $"backward y055 (pre-R2-export utan fälten, {_fx.agents.Length} själar): verbs empty {verbEmpty}/{checkedN}, state==task-law {okN}/{checkedN} (>=20), era law falls back to interim '{eraFallback}' (non-empty, eraName absent) ({(ok ? "OK" : "FAIL")})";
                _phase = 5;
                return;
            }

            if (_phase == 5)   // resume: the probe never touched the clock's tempo
            {
                c.paused = false;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n8 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — R2 consumption never touches the clock";
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n6b, _n7, _n8 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("lane honesty: the body CONSUMES Engine 2.3.2's additive R2 ink. 1 export (eraName + agents[].verb).");
                sb.AppendLine("Verb law: idle/rest/eat/grow->Idle, move->Walk, gather/hunt/fish/carry->Work-for-adults (no Carry clip —");
                sb.AppendLine("the true carry cycle is Väg-1/R3 purchase work; the D-131 basket prop stays task-driven), work/harvest/");
                sb.AppendLine("trade/fight->Work-for-adults; social/ritual keep the D-159 expression paths; empty/unknown -> task fallback.");
                sb.AppendLine("Era law: a non-empty engine eraName IS the name; empty falls back to the interim names — no UI can show an");
                sb.AppendLine("empty era. TempoFor and attend (D-159) are untouched — their regression rides Fas6Expr. Fixture honesty:");
                sb.AppendLine("r2ink1 + y055 are REAL engine exports through the SAME Apply path (FixtureInjection, chronicle silent).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)") && _n4.Contains("(OK)")
                    && _n5.Contains("(OK)") && _n6.Contains("OK") && !_n6.Contains("FAIL") && _n6b.Contains("OK") && !_n6b.Contains("FAIL")
                    && _n7.Contains("(OK)") && _n8.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the engine names the age and picks the step; the body follows, and old worlds still play"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} liveEra='{_eraLive}' tile='{_tileEra}' feedEra={_feedOk}/{_feedTotal} liveVerbs={_liveVerbs}/{_liveTotal} liveState={_liveStateOk}/{_liveStateChecked} fxState={_fxOk}/{_fxChecked} fxVerbCarried={_fxVerbCarried} fxNonIdle={_fxNonIdle} oldState={_oldOk}/{_oldChecked} oldVerbEmpty={_oldVerbEmpty}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
