// EMERGENCE — FAS 6 PROBE: A2-POLISH — the body reads the WHOLE export (tempo law + social gaze).
//
// Boots the same self-composing opening as the other Fas 6 probes and asserts the D-159 polish:
//   1. the pure tempo law: TempoFor(band, act) = AgeGain × MoodSpeed over the FULL 9-verb engine
//      sayAct vocabulary (audited from the engine's speak() calls) — exact values, multiplicative,
//      null-safe, deterministic;
//   2. live honesty: after the opening's first applied years every soul's animator speed == the law
//      (whatever acts the sim exported — no drift between law and body);
//   3. fixture (D-131/D-152 school): seq-8919-y055 (REAL engine export, 22 souls rich in love/observe/
//      teach/small) through the SAME Fas3WorldRuntime.Apply path — every body at law tempo, several
//      non-neutral; social-act souls with a neighbor in radius carry the attend-gaze at the mapped
//      neighbor position (independently recomputed here);
//   4. the mood-gate fix: pre-D-159, SetMood was task-change-gated — a soul changing ONLY its sayAct
//      kept a stale tempo. Declared in-memory mechanism mutation (D-158 school): same task, new act
//      => tempo follows;
//   5. the cold branch, declared mechanism fixture (NO lying export bears sayAct "cold" — transient,
//      verified over the standing exports): world-4242-y120-dusk (1 fire, winter) with ONE soul set
//      cold beside the fire => attends the mapped fire position, yaw converges, tempo == cold law;
//   6. clock honesty: expression never touches tps; pause in/out untouched;
//   7. evidence PNG (raycast-picked angle, the mechanized D-131 canopy lesson), blankness-guarded
//      AND humanly looked at.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// Menu: Emergence/Fas6/RUN EXPRESSION PROBE.  Headless: drop Reports/RUN_FAS6EXPR.trigger.
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
    public static class Fas6ExpressionProbe
    {
        const long Seed = 8919;
        const double Watchdog = 420.0;
        const int LiveYear = 2;   // live law check early — fixtures carry the rich-act burden

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS6EXPR.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS6EXPR_DONE.txt");
        const string Report   = "Reports/fas6-expression.txt";
        const string Png      = "Reports/fas6-expression.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixtureRich = "Assets/Emergence/WorldStates/seq-8919-y055.json";        // real export: 22 souls, rich sayActs
        const string FixtureFire = "Assets/Emergence/WorldStates/world-4242-y120-dusk.json"; // real export: 1 fire + winter
        const string KeyPending = "emg.fas6expr.pending", KeyStart = "emg.fas6expr.start", KeyReport = "emg.fas6expr.report";

        static int _frames, _phase, _waitFrames;
        static Fas3Onboarding _onb;
        static float _tpsBefore;
        static WorldState _fx;
        static int _mutId = -1; static float _preMutSpeed = -1f; static string _mutBand = "";
        static int _coldId = -1; static Vector3 _firePos;
        static float _grabAskedAt;
        // measurement-time stamps (R1 law)
        static int _liveChecked = -1, _liveOk = -1, _fxChecked = -1, _fxOk = -1, _fxNonNeutral = -1, _socialChecked = -1, _socialOk = -1;
        static float _coldYawAtCheck = -1f, _coldSpeedAtCheck = -1f;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "";

        static Fas6ExpressionProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/RUN EXPRESSION PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 6 PROBE: A2-polish — full tempo law (age × mood, 9 verbs) + social attend-gaze");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data source = applied WorldState agents' sayAct/age + fires (read-only; presentation follows applied state)");
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
            _frames = 0; _phase = 0; _waitFrames = 0; _onb = null; _tpsBefore = 0f; _fx = null;
            _mutId = -1; _preMutSpeed = -1f; _mutBand = ""; _coldId = -1; _firePos = Vector3.zero; _grabAskedAt = 0f;
            _liveChecked = _liveOk = _fxChecked = _fxOk = _fxNonNeutral = _socialChecked = _socialOk = -1;
            _coldYawAtCheck = _coldSpeedAtCheck = -1f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        // sim -> world mapping, independent of the reconciler's private helpers (same published law)
        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        static AgentAnimator Find(int id)
        {
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer == null) return null;
            foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) if (aa.agentId == id) return aa;
            return null;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null) return;

                // 1. the pure tempo law — exact values over the audited 9-verb vocabulary
                bool moods = AgentAnimator.MoodSpeed("discovery") == 1.10f && AgentAnimator.MoodSpeed("love") == 1.07f
                          && AgentAnimator.MoodSpeed("teach") == 0.96f && AgentAnimator.MoodSpeed("observe") == 0.94f
                          && AgentAnimator.MoodSpeed("ritual") == 0.90f && AgentAnimator.MoodSpeed("fail") == 0.90f
                          && AgentAnimator.MoodSpeed("hungry") == 0.86f && AgentAnimator.MoodSpeed("cold") == 0.85f
                          && AgentAnimator.MoodSpeed("small") == 1f && AgentAnimator.MoodSpeed(null) == 1f;
                bool ages = AgentAnimator.AgeGain("child") == 1.06f && AgentAnimator.AgeGain("elder") == 0.92f && AgentAnimator.AgeGain("adult") == 1f;
                bool mult = Mathf.Approximately(AgentAnimator.TempoFor("child", "discovery"), 1.06f * 1.10f)
                         && Mathf.Approximately(AgentAnimator.TempoFor("elder", "cold"), 0.92f * 0.85f)
                         && AgentAnimator.TempoFor("adult", null) == 1f;
                bool social = AgentReconciler.SocialAct("teach") && AgentReconciler.SocialAct("love")
                           && AgentReconciler.SocialAct("small") && !AgentReconciler.SocialAct("observe") && !AgentReconciler.SocialAct("");
                _n1 = $"pure law: 9-verb mood table={moods}, age gains={ages}, multiplicative={mult}, social classifier={social} ({(moods && ages && mult && social ? "OK" : "FAIL")})";
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // live: every body at law tempo, whatever the sim exported
            {
                var S = w.LastState;
                if (S == null || w.LastAppliedYear < LiveYear) return;
                int checkedN = 0, okN = 0;
                var layer = GameObject.Find(AgentReconciler.LayerName);
                if (layer != null)
                    foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                    {
                        var an = aa.GetComponentInChildren<Animator>();
                        if (an == null) continue;
                        checkedN++;
                        if (Mathf.Abs(an.speed - AgentAnimator.TempoFor(aa.band, aa.sayAct)) < 0.001f) okN++;
                    }
                _liveChecked = checkedN; _liveOk = okN;   // stamped at the live measurement (R1)
                bool ok = checkedN > 0 && okN == checkedN;
                _n2 = $"live @y{w.LastAppliedYear}: bodies at law tempo {okN}/{checkedN} ({(ok ? "OK" : "FAIL")})";

                _tpsBefore = c.ticksPerSecond;
                c.paused = true;   // fixtures ride the SAME Apply path with nothing racing them
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureRich));
                // G-review r1 I2: injection is reconstruction, not witnessed history — chronicle stays silent
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _phase = 2;
                return;
            }

            if (_phase == 2)   // next frames: rich fixture — law tempo everywhere + social gaze
            {
                if (++_waitFrames < 5) return;   // spawned bodies need Start() to run
                int checkedN = 0, okN = 0, nonNeutral = 0, socialChecked = 0, socialOk = 0;
                foreach (var a in _fx.agents)
                {
                    var aa = Find(a.id);
                    if (aa == null) continue;
                    var an = aa.GetComponentInChildren<Animator>();
                    if (an == null) continue;
                    checkedN++;
                    float want = AgentAnimator.TempoFor(AgentReconciler.Band(a.age), a.sayAct);
                    if (Mathf.Abs(an.speed - want) < 0.001f) okN++;
                    if (want != 1f) nonNeutral++;

                    // social gaze: recompute the nearest-other independently and compare targets
                    if (AgentReconciler.SocialAct(a.sayAct ?? ""))
                    {
                        WorldAgent best = null; float bd = AgentReconciler.SocialRadius * AgentReconciler.SocialRadius;
                        foreach (var b in _fx.agents)
                        {
                            if (b.id == a.id) continue;
                            float dd = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
                            if (dd < bd) { bd = dd; best = b; }
                        }
                        socialChecked++;
                        if (best != null)
                        { if (aa.HasAttend && (aa.AttendTarget - Mapped(_fx, best.x, best.y)).magnitude < 0.05f) socialOk++; }
                        else
                        { if (!aa.HasAttend) socialOk++; }
                    }
                }
                _fxChecked = checkedN; _fxOk = okN; _fxNonNeutral = nonNeutral;   // stamped (R1)
                _socialChecked = socialChecked; _socialOk = socialOk;
                bool ok = checkedN >= 20 && okN == checkedN && nonNeutral > 0 && socialChecked > 0 && socialOk == socialChecked;
                _n3 = $"fixture y055 (riktig motor-export, samma Apply-väg): law tempo {okN}/{checkedN} (>=20), non-neutral={nonNeutral}>0, social gaze {socialOk}/{socialChecked}>0 at mapped neighbor ({(ok ? "OK" : "FAIL")})";

                // 4. the mood-gate fix: SAME task, new act — declared in-memory mechanism mutation (D-158 school)
                foreach (var a in _fx.agents)
                {
                    var aa = Find(a.id); var an = aa != null ? aa.GetComponentInChildren<Animator>() : null;
                    if (an == null) continue;
                    if ((a.sayAct ?? "") == "ritual") continue;
                    _mutId = a.id; _preMutSpeed = an.speed; _mutBand = AgentReconciler.Band(a.age);
                    a.sayAct = "ritual";   // task untouched
                    break;
                }
                Fas3WorldRuntime.FixtureInjection = true;   // I2: injection, chronicle silent
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // next frame: tempo followed the act although the task never changed
            {
                if (++_waitFrames < 2) return;
                var aa = Find(_mutId); var an = aa != null ? aa.GetComponentInChildren<Animator>() : null;
                float want = AgentAnimator.TempoFor(_mutBand, "ritual");
                float got = an != null ? an.speed : -1f;
                bool ok = an != null && Mathf.Abs(got - want) < 0.001f && Mathf.Abs(got - _preMutSpeed) > 0.0005f;
                _n4 = $"mood-gate fix (agent {_mutId}, task oförändrad, akt->ritual i minnet — deklarerad mekanism-mutation): speed {_preMutSpeed:F3}->{got:F3}==law {want:F3} ({(ok ? "OK" : "FAIL")})";

                // 5. the cold branch: declared mechanism fixture — no standing export bears sayAct "cold"
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(FixtureFire));
                var f0 = _fx.fires[0];
                var soul = _fx.agents[0];
                soul.sayAct = "cold"; soul.x = f0.x + 2f; soul.y = f0.y;   // beside the fire, task untouched
                _coldId = soul.id;
                _firePos = Mapped(_fx, f0.x, f0.y);
                Fas3WorldRuntime.FixtureInjection = true;   // I2: injection, chronicle silent
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // let the attend-slerp converge, then measure + evidence
            {
                var aa = Find(_coldId);
                if (aa == null) { SafeFail("cold soul not found in scene"); return; }
                if (aa.InTransit) { _waitFrames = 0; return; }   // the glide owns heading; gaze converges after arrival
                // convergence-honest wait: slerp rate is frame-pacing-dependent in a headless editor —
                // wait for the MECHANISM (yaw under threshold), bounded by its own frame watchdog
                if (++_waitFrames < 900 && aa.HasAttend && aa.AttendYawError() >= 12f) return;
                if (_waitFrames < 30) return;   // floor: let a few frames pass even if already converged
                var an = aa.GetComponentInChildren<Animator>();
                _coldYawAtCheck = aa.AttendYawError();                       // stamped (R1)
                _coldSpeedAtCheck = an != null ? an.speed : -1f;
                float wantSpeed = AgentAnimator.TempoFor(aa.band, "cold");
                bool attends = aa.HasAttend && (aa.AttendTarget - _firePos).magnitude < 0.05f;
                bool yawOk = _coldYawAtCheck < 12f;
                bool speedOk = Mathf.Abs(_coldSpeedAtCheck - wantSpeed) < 0.001f;
                _n5 = $"cold branch (4242-y120-dusk + EN själ satt cold intill elden — deklarerad mekanism-fixtur, ingen liggande export bär cold): attends mapped fire={attends}, yaw {_coldYawAtCheck:F1}<12°={yawOk}, tempo {_coldSpeedAtCheck:F3}==law {wantSpeed:F3}={speedOk} ({(attends && yawOk && speedOk ? "OK" : "FAIL")})";

                // evidence: PAIR-framing (first-run eye lesson: mid-point framing at fixed distance put
                // the soul outside the frame while the fire sat pretty in the middle — the subject of the
                // mechanism must be IN the picture). Perpendicular to the soul->fire axis, distance
                // proportional to the pair span, raycast against BOTH endpoints (the D-131 lesson).
                var soulPos = aa.transform.position;
                var mid = (soulPos + _firePos) * 0.5f + Vector3.up * 1.0f;
                var axis = _firePos - soulPos; axis.y = 0f;
                float span = Mathf.Max(axis.magnitude, 8f);
                var perp = Vector3.Cross(axis.normalized, Vector3.up);
                Vector3 pick = mid + perp * span * 1.2f + Vector3.up * 4f;
                bool found = false;
                foreach (var side in new[] { perp, -perp })
                {
                    foreach (var h in new[] { 3.5f, 6.5f })
                    {
                        var cand = mid + side * span * 1.2f + Vector3.up * h;
                        if (!Physics.Linecast(cand, soulPos + Vector3.up * 1f) && !Physics.Linecast(cand, _firePos + Vector3.up * 0.5f))
                        { pick = cand; found = true; break; }
                        pick = cand;
                    }
                    if (found) break;
                }
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(mid); }
                var g = new GameObject("Fas6ExprGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = Png; g.OnGrabbed = note => { _n6 = "evidence " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 5;
                return;
            }

            if (_phase == 5)   // wait for the grab, then resume
            {
                if (_n6.Length == 0 && Time.unscaledTime - _grabAskedAt < 10f) return;
                c.paused = false;
                _phase = 6;
                return;
            }

            if (_phase == 6)   // resume: expression never touched the clock
            {
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n7 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — expression never touches the clock";
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("lane honesty: this is the A2-polish WITHIN the current export (task/age/sayAct — D-131 interim deepened).");
                sb.AppendLine("Full emotion body-states (posture/gesture clips) are Väg-1/purchase work (R3); per-soul emotion export is");
                sb.AppendLine("motor-lane (R2). Fixture honesty: y055 + 4242-y120-dusk are REAL engine exports through the SAME Apply path;");
                sb.AppendLine("the ritual-mutation and the cold soul are DECLARED in-memory mechanism stagings (the D-158/Fas2GateProof school).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)")
                    && _n4.Contains("(OK)") && _n5.Contains("(OK)") && _n6.Contains("OK") && _n7.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the body reads the whole export: age in the gait, mood in the tempo, attention in the gaze"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} liveLaw={_liveOk}/{_liveChecked} fxLaw={_fxOk}/{_fxChecked} nonNeutral={_fxNonNeutral} socialGaze={_socialOk}/{_socialChecked} coldYaw={_coldYawAtCheck:F1} coldSpeed={_coldSpeedAtCheck:F3}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
