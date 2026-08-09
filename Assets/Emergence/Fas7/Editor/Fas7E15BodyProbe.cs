// EMERGENCE — FAS 7 PROBE: E1.5 CONSUMED — the body eats the drama (Engine 2.4.1, TD-080..082).
//
// Engine 2.4.1 additively exports agents[].wealth, villages[].leader, villages[].gift and six new
// drama sayActs (steal/raid/feud/mourn/submit/gift). This probe proves the body CONSUMES all of it:
//   1. the EXACT extended tempo law: the six new MoodSpeed arms (raid 1.12 / feud 1.08 / gift 1.04 /
//      steal 0.92 / submit 0.88 / mourn 0.82), the D-159 register UNTOUCHED, multiplicative with
//      AgeGain, null-safe, unknown acts ('hail') at 1f; social classifier gains gift/submit/mourn;
//   2. FIXTURE world-4242-y120-e15 (REAL 2.4.1 export, driver-parity serializer, engine twin) through
//      the SAME Fas3WorldRuntime.Apply path under FixtureInjection: drama souls at law tempo — the
//      export bears REAL steal/gift sayActs at this tick; missing acts are DECLARED in-memory
//      mechanism mutations (D-158/Fas2GateProof school, counted in the report);
//   3. attend law: gift/submit/mourn attend the nearest soul (independently recomputed target);
//      steal/raid/feud stay unsocial (no attend — the thief does not telegraph);
//   4. publication mechanism + fixture silence: the reconciler put "sayAct: <act>" AgentActivity
//      events and the runtime put village "leader:"/"giftway:" Custom events on the bus DURING the
//      fixture Apply, while the chronicle stayed silent (SuppressedDuringFixture grew, entries
//      unchanged) — injection is reconstruction, never witnessed history (D-161);
//   5. feed classification on WITNESSED events (probe-published, same shapes the reconciler emits,
//      real fixture ids so names resolve from applied state): feud=★3, raid=★3, steal=2, mourn=2,
//      gift=2, leader=2 (+village name), giftway=2; submit deliberately silent (body-only, v0);
//      metrics recorder counts steal/raid/feud (trim-honest per-year counters);
//   6. Almanac souls: WEALTH sort (the ONE comparator, independently recomputed against the
//      export's real wealth values), dossier shows wealth;
//   7. Almanac villages: dossier bears LEDARE + GÅVO-SED when the engine has them;
//   8. Almanac society: first honest view — top-wealth list, leader per village, witnessed
//      violence counters == recorder truth;
//   9. mourn-fire fallback: a mourner with NO soul in social reach turns to the nearest hearth
//      (dusk fixture world-4242-y120-dusk, 1 fire — DECLARED mechanism staging like the D-159
//      cold branch); yaw converges, tempo == mourn law;
//  10. evidence: TWO honest frames via the SHARED framing law (subjects[0] = primary) — the
//      mourner at the fire (pose) + the almanac society view (UI); blankness-guarded AND humanly
//      looked at (D-008);
//  11. clock honesty: tps untouched through it all.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// Menu: Emergence/Fas7/RUN E15 BODY PROBE.  Headless: drop Reports/RUN_FAS7E15.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas7E15BodyProbe
    {
        const long Seed = 8919;
        const double Watchdog = 700.0;
        const int LiveYear = 3;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7E15.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS7E15_DONE.txt");
        const string Report   = "Reports/fas7-e15body.txt";
        const string PngPose  = "Reports/fas7-e15body-pose.png";
        const string PngAlm   = "Reports/fas7-e15body-almanac.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixtureE15  = "Assets/Emergence/WorldStates/world-4242-y120-e15.json";   // REAL 2.4.1 export (driver-parity, engine twin)
        const string FixtureFire = "Assets/Emergence/WorldStates/world-4242-y120-dusk.json";  // real export: 1 fire (mourn-fallback stage)
        const string KeyPending = "emg.fas7e15.pending", KeyStart = "emg.fas7e15.start", KeyReport = "emg.fas7e15.report";

        static readonly string[] DramaActs = { "steal", "raid", "feud", "mourn", "submit", "gift" };

        static int _frames, _phase, _waitFrames;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas5AlmanacView _view;
        static Fas5MetricsRecorder _rec;
        static float _tpsBefore;
        static WorldState _fx;
        static float _settleStart = -1f, _grabAskedAt;
        static readonly Dictionary<string, int> _actSoul = new Dictionary<string, int>();   // act -> chosen agent id
        static int _mutations;                       // how many acts needed a declared mutation
        static int _feedBefore, _suppBefore, _busBefore;
        static int _stealBefore, _raidBefore, _feudBefore;
        static int _mournFireId = -1; static Vector3 _firePos;
        // measurement-time stamps (R1 law)
        static int _fxTempoOk = -1, _fxAttendSocOk = -1, _fxAttendNoneOk = -1;
        static int _busSayActs = -1, _busLeader = -1, _busGiftway = -1, _feedGained = -1;
        static string _topName = ""; static int _topWealth = -1;
        static string _dosLeader = "", _dosGift = "";
        static int _socSteal = -1, _socRaid = -1, _socFeud = -1, _socLed = -1;
        static float _mournYaw = -1f, _mournSpeed = -1f;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8 = "", _n8b = "", _n9 = "", _n10 = "", _n11 = "";

        static Fas7E15BodyProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN E15 BODY PROBE")]
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
            Fas4UIAssetsBuild.Ensure();

            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 7 PROBE: E1.5 consumed — drama tempo/attend, ★-salience, wealth/leader/gift in the almanac");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data sources = applied WorldState (read-only) + world-4242-y120-e15.json (REAL Engine 2.4.1 export,");
            sb.AppendLine("driver-parity serializer over the StreamingAssets engine twin) + world-4242-y120-dusk.json (fire stage)");
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
            _frames = 0; _phase = 0; _waitFrames = 0; _onb = null; _feed = null; _view = null; _rec = null;
            _tpsBefore = 0f; _fx = null; _settleStart = -1f; _grabAskedAt = 0f;
            _actSoul.Clear(); _mutations = 0;
            _feedBefore = _suppBefore = _busBefore = 0; _stealBefore = _raidBefore = _feudBefore = 0;
            _mournFireId = -1; _firePos = Vector3.zero;
            _fxTempoOk = _fxAttendSocOk = _fxAttendNoneOk = -1;
            _busSayActs = _busLeader = _busGiftway = _feedGained = -1;
            _topName = ""; _topWealth = -1; _dosLeader = ""; _dosGift = "";
            _socSteal = _socRaid = _socFeud = _socLed = -1;
            _mournYaw = _mournSpeed = -1f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8 = _n8b = _n9 = _n10 = _n11 = "";
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

        static bool Settled(AgentAnimator aa, out Animator an)
        {
            an = aa != null ? aa.GetComponentInChildren<Animator>() : null;
            return aa != null && an != null && !aa.InTransit && !an.IsInTransition(0);
        }

        static WorldAgent Agent(WorldState S, int id)
        {
            foreach (var a in S.agents) if (a.id == id) return a;
            return null;
        }

        static WorldAgent NearestOther(WorldState S, WorldAgent a, float radius)
        {
            WorldAgent best = null; float bd = radius * radius;
            foreach (var b in S.agents)
            {
                if (b.id == a.id) continue;
                float dd = (b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y);
                if (dd < bd) { bd = dd; best = b; }
            }
            return best;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>();
                _rec = UnityEngine.Object.FindAnyObjectByType<Fas5MetricsRecorder>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null
                    || _feed == null || _view == null || _rec == null) return;

                // 1. the pure extended law — exact values; the D-159 register untouched
                bool newArms = AgentAnimator.MoodSpeed("raid") == 1.12f && AgentAnimator.MoodSpeed("feud") == 1.08f
                            && AgentAnimator.MoodSpeed("gift") == 1.04f && AgentAnimator.MoodSpeed("steal") == 0.92f
                            && AgentAnimator.MoodSpeed("submit") == 0.88f && AgentAnimator.MoodSpeed("mourn") == 0.82f;
                bool oldArms = AgentAnimator.MoodSpeed("discovery") == 1.10f && AgentAnimator.MoodSpeed("love") == 1.07f
                            && AgentAnimator.MoodSpeed("teach") == 0.96f && AgentAnimator.MoodSpeed("observe") == 0.94f
                            && AgentAnimator.MoodSpeed("ritual") == 0.90f && AgentAnimator.MoodSpeed("fail") == 0.90f
                            && AgentAnimator.MoodSpeed("hungry") == 0.86f && AgentAnimator.MoodSpeed("cold") == 0.85f
                            && AgentAnimator.MoodSpeed("small") == 1f && AgentAnimator.MoodSpeed(null) == 1f
                            && AgentAnimator.MoodSpeed("hail") == 1f;   // unknown acts stay null-tempo
                bool mult = Mathf.Approximately(AgentAnimator.TempoFor("elder", "mourn"), 0.92f * 0.82f)
                         && Mathf.Approximately(AgentAnimator.TempoFor("child", "gift"), 1.06f * 1.04f)
                         && Mathf.Approximately(AgentAnimator.TempoFor("adult", "raid"), 1.12f)
                         && AgentAnimator.TempoFor("adult", null) == 1f;
                bool social = AgentReconciler.SocialAct("gift") && AgentReconciler.SocialAct("submit")
                           && AgentReconciler.SocialAct("mourn") && AgentReconciler.SocialAct("teach")
                           && AgentReconciler.SocialAct("love") && AgentReconciler.SocialAct("small")
                           && !AgentReconciler.SocialAct("steal") && !AgentReconciler.SocialAct("raid")
                           && !AgentReconciler.SocialAct("feud") && !AgentReconciler.SocialAct("observe")
                           && !AgentReconciler.SocialAct("");
                bool drama = AgentReconciler.DramaAct("steal") && AgentReconciler.DramaAct("raid")
                          && AgentReconciler.DramaAct("feud") && AgentReconciler.DramaAct("mourn")
                          && AgentReconciler.DramaAct("submit") && AgentReconciler.DramaAct("gift")
                          && !AgentReconciler.DramaAct("love") && !AgentReconciler.DramaAct("");
                _n1 = $"pure law: six new arms exact={newArms}, D-159 register untouched (incl. 'hail'->1f)={oldArms}, multiplicative={mult}, social gift/submit/mourn={social}, drama classifier={drama} ({(newArms && oldArms && mult && social && drama ? "OK" : "FAIL")})";
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // live to y3, then the REAL 2.4.1 fixture through the SAME Apply path
            {
                if (w.LastState == null || w.LastAppliedYear < LiveYear) return;
                _tpsBefore = c.ticksPerSecond;
                c.paused = true;

                string fixAbs = Path.Combine(Application.dataPath, "..", FixtureE15);
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(fixAbs));
                if (_fx == null || _fx.agents == null || _fx.agents.Length < 20 || _fx.villages == null)
                { SafeFail("fixture parse: " + FixtureE15); return; }
                if (string.IsNullOrEmpty(_fx.engineVersion) || !_fx.engineVersion.StartsWith("2.4"))
                { SafeFail("fixture is not a 2.4.x export: '" + _fx.engineVersion + "'"); return; }

                // choose one soul per drama act: the export's OWN drama souls first (real 2.4.1 acts —
                // 4242-y120 bears live steal + gift at this tick); missing acts become DECLARED
                // in-memory mechanism mutations (D-158 school) on souls with a neighbor in social
                // reach (so the attend law is measurable). Deterministic ascending-id scans, no RNG.
                foreach (var act in DramaActs)
                    foreach (var a in _fx.agents)
                        if ((a.sayAct ?? "") == act && !_actSoul.ContainsValue(a.id)) { _actSoul[act] = a.id; break; }
                foreach (var act in DramaActs)
                {
                    if (_actSoul.ContainsKey(act)) continue;
                    foreach (var a in _fx.agents)
                    {
                        if (_actSoul.ContainsValue(a.id)) continue;
                        if (AgentReconciler.DramaAct(a.sayAct ?? "")) continue;   // never overwrite real drama
                        if (NearestOther(_fx, a, AgentReconciler.SocialRadius) == null) continue;
                        a.sayAct = act; _actSoul[act] = a.id; _mutations++; break;
                    }
                }
                if (_actSoul.Count < DramaActs.Length) { SafeFail("could not stage all six drama acts in the fixture"); return; }

                _feedBefore = _feed.Entries.Count; _suppBefore = _feed.SuppressedDuringFixture;
                _busBefore = PresentationEventBus.Count;
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0; _settleStart = Time.unscaledTime;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // drama tempo + attend on the settled bodies
            {
                if (++_waitFrames < 5) return;
                bool allSettled = true;
                foreach (var id in _actSoul.Values) if (!Settled(Find(id), out _)) { allSettled = false; break; }
                if (!allSettled && Time.unscaledTime - _settleStart < 90f) return;

                int tempoOk = 0, attendSocOk = 0, attendSocN = 0, attendNoneOk = 0, attendNoneN = 0;
                var detail = new StringBuilder();
                foreach (var act in DramaActs)
                {
                    int id = _actSoul[act];
                    var a = Agent(_fx, id);
                    var aa = Find(id);
                    var an = aa != null ? aa.GetComponentInChildren<Animator>() : null;
                    if (a == null || an == null) { detail.Append($" [{act}:MISSING]"); continue; }
                    float want = AgentAnimator.TempoFor(AgentReconciler.Band(a.age), act);
                    bool tOk = Mathf.Abs(an.speed - want) < 0.001f;
                    if (tOk) tempoOk++;
                    detail.Append($" [{act}='{a.name}' speed {an.speed:F3}=={want:F3} {(tOk ? "ok" : "FAIL")}]");
                    if (AgentReconciler.SocialAct(act))
                    {
                        attendSocN++;
                        var best = NearestOther(_fx, a, AgentReconciler.SocialRadius);
                        if (best != null ? aa.HasAttend && (aa.AttendTarget - Mapped(_fx, best.x, best.y)).magnitude < 0.05f
                                         : !aa.HasAttend) attendSocOk++;   // e15 fixture has 0 fires — mourn w/o neighbor clears
                    }
                    else
                    {
                        attendNoneN++;
                        if (!aa.HasAttend) attendNoneOk++;
                    }
                }
                _fxTempoOk = tempoOk; _fxAttendSocOk = attendSocOk; _fxAttendNoneOk = attendNoneOk;   // stamped (R1)
                bool ok2 = tempoOk == DramaActs.Length;
                _n2 = $"fixture e15 (riktig 2.4.1-export, samma Apply-väg, {_fx.agents.Length} själar, {_mutations} deklarerade mutationer): drama tempo==lag {tempoOk}/{DramaActs.Length} ({(ok2 ? "OK" : "FAIL")}) —{detail}";
                bool ok3 = attendSocN == 3 && attendSocOk == 3 && attendNoneN == 3 && attendNoneOk == 3;
                _n3 = $"attend law: social (gift/submit/mourn) at recomputed nearest-soul {attendSocOk}/{attendSocN}, unsocial (steal/raid/feud) no attend {attendNoneOk}/{attendNoneN} ({(ok3 ? "OK" : "FAIL")})";

                // 4. publication mechanism + fixture silence (the bus heard it; the book did not)
                int sayActs = 0, leaderEv = 0, giftwayEv = 0;
                var log = PresentationEventBus.Log;
                for (int i = _busBefore; i < log.Count; i++)
                {
                    var e = log[i];
                    if (e.Type == PresentationEventType.AgentActivity && e.Data.StartsWith("sayAct: ")) sayActs++;
                    else if (e.Type == PresentationEventType.Custom && e.Data.StartsWith("leader: ")) leaderEv++;
                    else if (e.Type == PresentationEventType.Custom && e.Data.StartsWith("giftway: ")) giftwayEv++;
                }
                _busSayActs = sayActs; _busLeader = leaderEv; _busGiftway = giftwayEv;   // stamped (R1)
                bool silent = _feed.Entries.Count == _feedBefore && _feed.SuppressedDuringFixture > _suppBefore;
                bool ok4 = sayActs >= DramaActs.Length && leaderEv >= 1 && giftwayEv >= 1 && silent;
                _n4 = $"bus during fixture Apply: sayAct events {sayActs}>=6, village leader events {leaderEv}>=1, giftway events {giftwayEv}>=1; chronicle SILENT (entries {_feed.Entries.Count}=={_feedBefore}, suppressed +{_feed.SuppressedDuringFixture - _suppBefore}) ({(ok4 ? "OK" : "FAIL")})";

                // 5. feed classification on WITNESSED events — probe-published, the same shapes the
                // reconciler emits, with REAL fixture ids so NameOf resolves from the applied state
                _stealBefore = _rec.TotalSteal; _raidBefore = _rec.TotalRaid; _feudBefore = _rec.TotalFeud;
                _feedBefore = _feed.Entries.Count;
                string era = WorldEras.Name(_fx);
                int leaderVi = -1, giftVi = -1;
                for (int i = 0; i < _fx.villages.Length; i++)
                {
                    if (leaderVi < 0 && !string.IsNullOrEmpty(_fx.villages[i].leader)) leaderVi = i;
                    if (giftVi < 0 && !string.IsNullOrEmpty(_fx.villages[i].gift)) giftVi = i;
                }
                if (leaderVi < 0 || giftVi < 0) { SafeFail("fixture bears no leader/gift village — regenerate"); return; }
                foreach (var act in DramaActs)
                    PresentationEventBus.Publish(new PresentationEvent(
                        _fx.tick, _fx.years, era, PresentationEventType.AgentActivity, "agent-" + _actSoul[act], -1, "sayAct: " + act));
                PresentationEventBus.Publish(new PresentationEvent(
                    _fx.tick, _fx.years, era, PresentationEventType.Custom, "village:" + _fx.villages[leaderVi].name, leaderVi, "leader: " + _fx.villages[leaderVi].leader));
                PresentationEventBus.Publish(new PresentationEvent(
                    _fx.tick, _fx.years, era, PresentationEventType.Custom, "village:" + _fx.villages[giftVi].name, giftVi, "giftway: " + _fx.villages[giftVi].gift));

                int gained = _feed.Entries.Count - _feedBefore;
                _feedGained = gained;   // stamped (R1)
                int sal(string kind) { for (int i = _feed.Entries.Count - 1; i >= _feedBefore - 0; i--) if (i >= 0 && i < _feed.Entries.Count && _feed.Entries[i].kind == kind) return _feed.Entries[i].salience; return -1; }
                string txt(string kind) { for (int i = _feed.Entries.Count - 1; i >= 0; i--) if (_feed.Entries[i].kind == kind) return _feed.Entries[i].text; return ""; }
                bool classOk = gained == 7
                    && sal("feud") == 3 && sal("raid") == 3 && sal("steal") == 2
                    && sal("mourn") == 2 && sal("gift") == 2 && sal("leader") == 2 && sal("giftway") == 2;
                bool namesOk = txt("feud").Contains(Agent(_fx, _actSoul["feud"]).name)
                    && txt("leader").Contains(_fx.villages[leaderVi].leader) && txt("leader").Contains(_fx.villages[leaderVi].name)
                    && txt("giftway").Contains(_fx.villages[giftVi].gift);
                bool recOk = _rec.TotalSteal == _stealBefore + 1 && _rec.TotalRaid == _raidBefore + 1 && _rec.TotalFeud == _feudBefore + 1;
                _socSteal = _rec.TotalSteal; _socRaid = _rec.TotalRaid; _socFeud = _rec.TotalFeud;   // stamped (R1)
                bool ok5 = classOk && namesOk && recOk;
                _n5 = $"feed classification (witnessed injection, real ids): +{gained}==7 entries — feud=★{sal("feud")} raid=★{sal("raid")} steal={sal("steal")} mourn={sal("mourn")} gift={sal("gift")} leader={sal("leader")} giftway={sal("giftway")}, submit SILENT by design; names+village in text={namesOk}; recorder steal/raid/feud +1 each -> {_socSteal}/{_socRaid}/{_socFeud} ({(ok5 ? "OK" : "FAIL")}) — feud: \"{txt("feud")}\"";
                _phase = 3;
                return;
            }

            if (_phase == 3)   // almanac: wealth sort + leader/gift dossier + society, on the SAME fixture
            {
                _view.SetStateFixture(_fx);
                _view.OpenAlmanac();

                // 6. souls: the ONE wealth comparator, independently recomputed over the export's real values
                var sorted = (WorldAgent[])_fx.agents.Clone();
                Array.Sort(sorted, Fas5AlmanacView.WealthOrder);
                _view.SelectTab(Fas5AlmanacView.TabSouls);
                _view.OpenSoulDossier(0);
                int expRows = Mathf.Min(Fas5AlmanacView.SoulRowCap, _fx.agents.Length);
                bool orderOk = _view.SoulRowCount == expRows;
                for (int i = 0; i < 5 && i < expRows; i++) if (_view.SoulRowName(i) != sorted[i].name) orderOk = false;
                _topName = _view.SoulRowName(0); _topWealth = _view.SoulDossierWealth;   // stamped (R1)
                bool dosOk = _view.SoulDossierName == sorted[0].name && _view.SoulDossierWealth == Mathf.RoundToInt(sorted[0].wealth);
                bool ok6 = orderOk && dosOk && sorted[0].wealth > 0f;
                _n6 = $"almanac souls: rows {_view.SoulRowCount}=={expRows}, WEALTH order top5 vs recompute {(orderOk ? "OK" : "FAIL")}, rikaste '{_topName}' rikedom {_topWealth}=={Mathf.RoundToInt(sorted[0].wealth)} (>0 — real 2.4.1 wealth) ({(ok6 ? "OK" : "FAIL")})";

                // 7. villages: dossier bears LEDARE + GÅVO-SED (view sort: pop DESC, name ASC — recomputed)
                var vSorted = (WorldVillage[])_fx.villages.Clone();
                Array.Sort(vSorted, (x, y) => y.pop != x.pop ? y.pop.CompareTo(x.pop) : string.CompareOrdinal(x.name ?? "", y.name ?? ""));
                int row = -1; WorldVillage led = null;
                for (int i = 0; i < vSorted.Length; i++)
                    if (!string.IsNullOrEmpty(vSorted[i].leader) && !string.IsNullOrEmpty(vSorted[i].gift)) { row = i; led = vSorted[i]; break; }
                if (row < 0) { SafeFail("no village with both leader+gift in fixture"); return; }
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                _view.OpenVillageDossier(row);
                _dosLeader = _view.VillageDossierLeader; _dosGift = _view.VillageDossierGift;   // stamped (R1)
                bool ok7 = _view.VillageDossierName == led.name && _dosLeader == led.leader && _dosGift == led.gift;
                _n7 = $"almanac village dossier '{_view.VillageDossierName}': LEDARE '{_dosLeader}'=='{led.leader}', GÅVO-SED '{_dosGift}'=='{led.gift}' ({(ok7 ? "OK" : "FAIL")})";

                // 8. society: first honest view — top wealth, leaders, witnessed violence == recorder truth
                int expLed = 0; foreach (var v in _fx.villages) if (!string.IsNullOrEmpty(v.leader)) expLed++;
                _view.SelectTab(Fas5AlmanacView.TabSociety);
                _socLed = _view.SocietyLedVillages;   // stamped (R1)
                bool ok8 = _view.SocietyTopName == sorted[0].name && _view.SocietyTopWealth == Mathf.RoundToInt(sorted[0].wealth)
                        && _view.SocietyWealthRows == Mathf.Min(10, _fx.agents.Length)
                        && _view.SocietyVillageRows == _fx.villages.Length && _socLed == expLed && expLed > 0
                        && _view.SocietySteal == _rec.TotalSteal && _view.SocietyRaid == _rec.TotalRaid && _view.SocietyFeud == _rec.TotalFeud
                        && _view.SocietySteal > 0 && _view.SocietyRaid > 0 && _view.SocietyFeud > 0
                        && !Fas5AlmanacView.TabIsStub(Fas5AlmanacView.TabSociety);
                _n8 = $"almanac society (stubben ersatt): topp '{_view.SocietyTopName}' ({_view.SocietyTopWealth}), wealth rows {_view.SocietyWealthRows}, byar {_view.SocietyVillageRows} varav {_socLed} med ledare (=={expLed}), bevittnat våld {_view.SocietySteal}/{_view.SocietyRaid}/{_view.SocietyFeud}==recorder ({(ok8 ? "OK" : "FAIL")})";

                var g = new GameObject("Fas7E15AlmGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngAlm; g.OnGrabbed = note => { _n8b = "evidence(almanac) " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // wait almanac grab -> mourn-fire fallback stage
            {
                if (_n8b.Length == 0 && Time.unscaledTime - _grabAskedAt < 16f) return;
                if (_n8b.Length == 0) _n8b = "evidence(almanac): FAIL (no grab within 16 s)";
                _view.CloseAlmanac();
                _view.SetStateFixture(null);

                // 9. mourn-fire fallback: dusk fixture (1 fire), ONE soul set to mourn beside the fire
                // with NO soul in social reach — DECLARED mechanism staging (the D-159 cold school).
                string fixAbs = Path.Combine(Application.dataPath, "..", FixtureFire);
                _fx = JsonUtility.FromJson<WorldState>(File.ReadAllText(fixAbs));
                if (_fx == null || _fx.fires == null || _fx.fires.Length == 0) { SafeFail("fire fixture parse: " + FixtureFire); return; }
                var f0 = _fx.fires[0];
                var soul = _fx.agents[0];
                // find a spot near the fire (within FireRadius) with no OTHER soul within SocialRadius
                bool placed = false;
                foreach (var off in new[] { new Vector2(2, 0), new Vector2(0, 2), new Vector2(-2, 0), new Vector2(0, -2),
                                            new Vector2(3, 3), new Vector2(-3, 3), new Vector2(3, -3), new Vector2(-3, -3) })
                {
                    float nx = f0.x + off.x, ny = f0.y + off.y;
                    bool clear = true;
                    foreach (var b in _fx.agents)
                    {
                        if (b.id == soul.id) continue;
                        if ((b.x - nx) * (b.x - nx) + (b.y - ny) * (b.y - ny) < AgentReconciler.SocialRadius * AgentReconciler.SocialRadius)
                        { clear = false; break; }
                    }
                    if (clear) { soul.x = nx; soul.y = ny; placed = true; break; }
                }
                if (!placed) { SafeFail("no isolated spot beside the fire — staging impossible"); return; }
                soul.sayAct = "mourn";
                _mournFireId = soul.id;
                _firePos = Mapped(_fx, f0.x, f0.y);
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_fx); } finally { Fas3WorldRuntime.FixtureInjection = false; }
                _waitFrames = 0; _settleStart = Time.unscaledTime;
                _phase = 5;
                return;
            }

            if (_phase == 5)   // converge, measure the mourner, evidence pose
            {
                var aa = Find(_mournFireId);
                if (aa == null) { SafeFail("mourner not found in scene"); return; }
                if (aa.InTransit) { _waitFrames = 0; return; }
                if (++_waitFrames < 900 && aa.HasAttend && aa.AttendYawError() >= 12f) return;
                if (_waitFrames < 30) return;
                var an = aa.GetComponentInChildren<Animator>();
                _mournYaw = aa.AttendYawError(); _mournSpeed = an != null ? an.speed : -1f;   // stamped (R1)
                float wantSpeed = AgentAnimator.TempoFor(aa.band, "mourn");
                bool attends = aa.HasAttend && (aa.AttendTarget - _firePos).magnitude < 0.05f;
                bool yawOk = _mournYaw < 12f;
                bool speedOk = Mathf.Abs(_mournSpeed - wantSpeed) < 0.001f;
                _n9 = $"mourn-fire fallback (dusk-fixtur + EN själ satt mourn intill elden, isolerad — deklarerad mekanism-staging): attends mapped fire={attends}, yaw {_mournYaw:F1}<12°={yawOk}, tempo {_mournSpeed:F3}==lag {wantSpeed:F3}={speedOk} ({(attends && yawOk && speedOk ? "OK" : "FAIL")})";

                var soulPos = aa.transform.position;
                Vector3 lookAt;
                Vector3 pick = EvidenceFraming.FrameSubjects(out lookAt, soulPos, _firePos);   // subjects[0] = PRIMARY = the mourner
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(lookAt); }
                var g = new GameObject("Fas7E15PoseGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngPose; g.OnGrabbed = note => { _n10 = "evidence(pose) " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 6;
                return;
            }

            if (_phase == 6)   // wait pose grab, resume, clock honesty
            {
                if (_n10.Length == 0 && Time.unscaledTime - _grabAskedAt < 16f) return;
                if (_n10.Length == 0) _n10 = "evidence(pose): FAIL (no grab within 16 s)";
                c.paused = false;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n11 = $"resume: tps {c.ticksPerSecond}=={_tpsBefore} ({(tpsOk ? "OK" : "FAIL")}) — E1.5 consumption never touches the clock";
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8, _n8b, _n9, _n10, _n11 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("lane honesty: the body CONSUMES Engine 2.4.1's additive E1.5 export (wealth/leader/gift + six drama");
                sb.AppendLine("sayActs). Tempo: ONE multiplicative law, D-159 values untouched, director-chosen new arms (raid 1.12 /");
                sb.AppendLine("feud 1.08 / gift 1.04 / steal 0.92 / submit 0.88 / mourn 0.82); unknown acts stay null-tempo. Attend:");
                sb.AppendLine("gift/submit/mourn social (nearest soul), mourn falls back to the hearth (cold mechanics as pattern);");
                sb.AppendLine("steal/raid/feud deliberately unsocial. Chronicle: feud/raid=★, steal/mourn/gift/leader/giftway notable;");
                sb.AppendLine("submit body-only (v0-lagen). Tribute events stay engine-side history — no tribute data reaches the");
                sb.AppendLine("export, so the body says nothing (honest wait, R2/E-next). Fixture honesty: world-4242-y120-e15 is a");
                sb.AppendLine("REAL 2.4.1 export (driver-parity serializer over the StreamingAssets twin) through the SAME Apply path;");
                sb.AppendLine("declared mutations counted above; the mourner's staging follows the D-159 cold-branch school.");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)") && _n4.Contains("(OK)")
                    && _n5.Contains("(OK)") && _n6.Contains("(OK)") && _n7.Contains("(OK)") && _n8.Contains("(OK)")
                    && _n8b.Contains("OK") && !_n8b.Contains("FAIL")
                    && _n9.Contains("(OK)") && _n10.Contains("OK") && !_n10.Contains("FAIL") && _n11.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the drama reaches the body: the thief skulks, the mourner turns to the fire, the book stars the feud, the almanac knows who is rich and who leads"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} fxTempo={_fxTempoOk}/6 attendSoc={_fxAttendSocOk}/3 attendNone={_fxAttendNoneOk}/3 busSayActs={_busSayActs} busLeader={_busLeader} busGiftway={_busGiftway} feedGained={_feedGained}/7 top='{_topName}'({_topWealth}) dossier='{_dosLeader}'/'{_dosGift}' societyViolence={_socSteal}/{_socRaid}/{_socFeud} ledVillages={_socLed} mournYaw={_mournYaw:F1} mournSpeed={_mournSpeed:F3} mutations={_mutations}\nsee {Report}\n");   // measurement-time stamps (R1 law)
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
