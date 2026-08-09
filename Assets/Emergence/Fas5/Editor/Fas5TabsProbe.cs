// EMERGENCE — FAS 5 PROBE: the TAB SKELETON — Villages live + Souls base, verified.
//
// Boots the same self-composing opening as the v0 probe and asserts the almanac's tab skeleton
// against the applied state AND a real engine snapshot fixture:
//   1. tab bar = the reference's seven tabs, Overview active by default, view READY;
//   2. VILLAGES live (empty branch): rows == state.villages count at the frozen year (the young
//      world honestly shows "inga byar ännu" — never fake rows);
//   3. SOULS live: rows == min(30, agents), sorted by the E1.5 wealth law (wealth DESC, tie age
//      DESC, id ASC — recomputed independently); dossier row 0 verbatim (name/age/gen/task/wealth);
//   4. VILLAGES populated branch (D-131 fixture school): seq-8919-y055.json (a REAL engine
//      export, 2 villages) fed through the SAME rebuild path via SetStateFixture — rows ==
//      fixture count, sorted pop DESC, dossier == the fixture village verbatim (incl. knows);
//   5. fixture cleared -> the tab returns to runtime state truth;
//   6. CHRONICLE tab = handoff to the Fas 4 book (link, never rebuild): almanac closes, book
//      opens, clock stays paused, tps untouched through the whole chain;
//   7+8. evidence PNGs (souls live, villages fixture) END-OF-FRAME with the blankness guard;
//   9. Overview regression: tiles still == recorder truth after all tab traffic.
// Menu: Emergence/Fas5/RUN TABS PROBE.  Headless: drop Reports/RUN_FAS5TABS.trigger.
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
    public static class Fas5TabsProbe
    {
        const long Seed = 8919;
        const double Watchdog = 320.0;
        const int Horizon = 10;   // first child y1, first hut y5 at seed 8919 — margin included
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixturePath = "Assets/Emergence/WorldStates/seq-8919-y055.json";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS5TABS.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS5TABS_DONE.txt");
        const string Report   = "Reports/fas5-tabs.txt";
        const string PngSouls = "Reports/fas5-souls.png";
        const string PngVill  = "Reports/fas5-villages.png";
        const string KeyPending = "emg.fas5tabs.pending", KeyStart = "emg.fas5tabs.start", KeyReport = "emg.fas5tabs.report";

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas5MetricsRecorder _rec;
        static Fas5AlmanacView _view;
        static WorldState _fix;
        static int _liveVillageCount = -1;
        static int _soulsRowsLive = -1, _villRowsFixture = -1;   // R1 (Fas 5 review): DONE key figures stamped AT MEASUREMENT TIME, never read from view state at finish
        static float _tpsBefore, _grabAskedAt;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "", _n8 = "", _n9 = "";

        static Fas5TabsProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas5/RUN TABS PROBE")]
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
            sb.AppendLine("EMERGENCE — FAS 5 PROBE: the tab skeleton — Villages live + Souls base");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data sources = applied WorldState (read-only) + fixture seq-8919-y055.json (a real engine export, D-131 fixture school)");
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
            _frames = 0; _phase = 0; _onb = null; _rec = null; _view = null; _fix = null;
            _liveVillageCount = -1; _soulsRowsLive = -1; _villRowsFixture = -1; _tpsBefore = 0f; _grabAskedAt = 0f;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = _n8 = _n9 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _rec = UnityEngine.Object.FindAnyObjectByType<Fas5MetricsRecorder>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _rec == null || _view == null) return;
                if (!_view.enabled && _view.LastError.Length > 0) { SafeFail("view disarmed: " + _view.LastError); return; }
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // witness live until the opening beats exist (first hut ~y5)
            {
                var feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                bool hut = false, birth = false;
                if (feed != null)
                    foreach (var e in feed.Entries)
                    {
                        if (e.kind == "milestone" && e.text.StartsWith("the first hut")) hut = true;
                        else if (e.kind == "birth") birth = true;
                    }
                if (!(hut && birth))
                {
                    if (w.LastAppliedYear >= Horizon) SafeFail($"beats missing by y{w.LastAppliedYear} (hut={hut} birth={birth})");
                    return;
                }
                c.paused = true;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // open + live-branch assertions + souls evidence grab
            {
                _view.OpenAlmanac(); _view.RefreshNow();
                bool tabsOk = Fas5AlmanacView.TabNames.Length == 7 && _view.ActiveTab == Fas5AlmanacView.TabOverview && _view.Ready;
                _n1 = $"skeleton: {Fas5AlmanacView.TabNames.Length} tabs (reference nav), default tab {_view.ActiveTab}==Overview, READY={_view.Ready} ({(tabsOk ? "OK" : "FAIL")})";

                var S = w.LastState;
                int sv = S != null && S.villages != null ? S.villages.Length : 0;
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                _liveVillageCount = _view.VillageRowCount;
                bool villLiveOk = _view.VillageRowCount == sv;
                _n2 = $"villages LIVE (empty branch): rows {_view.VillageRowCount}==state {sv} @y{w.LastAppliedYear} — the young world hides nothing, fakes nothing ({(villLiveOk ? "OK" : "FAIL")})";

                // first soul by the view's ONE sort law (E1.5: wealth DESC, tie age DESC, id ASC —
                // recomputed independently through the same published comparator)
                WorldAgent top = null;
                if (S != null && S.agents != null)
                    foreach (var a in S.agents)
                        if (top == null || Fas5AlmanacView.WealthOrder(a, top) < 0) top = a;
                int expRows = S != null && S.agents != null ? Mathf.Min(Fas5AlmanacView.SoulRowCap, S.agents.Length) : 0;
                _view.SelectTab(Fas5AlmanacView.TabSouls);
                _view.OpenSoulDossier(0);
                bool soulsOk = _view.SoulRowCount == expRows && top != null
                            && _view.SoulRowName(0) == top.name
                            && _view.SoulDossierName == top.name
                            && _view.SoulDossierAge == Mathf.RoundToInt(top.age)
                            && _view.SoulDossierGen == top.gen
                            && _view.SoulDossierTask == (top.task ?? "")
                            && _view.SoulDossierWealth == Mathf.RoundToInt(top.wealth);
                _n3 = $"souls LIVE: rows {_view.SoulRowCount}=={expRows}, wealth-first '{_view.SoulRowName(0)}'=='{(top != null ? top.name : "?")}' (rikedom {_view.SoulDossierWealth}), dossier {_view.SoulDossierName}/{_view.SoulDossierAge} år/gen {_view.SoulDossierGen}/'{_view.SoulDossierTask}' ({(soulsOk ? "OK" : "FAIL")})";
                _soulsRowsLive = _view.SoulRowCount;   // stamped at the live measurement (review I1)

                var g = new GameObject("Fas5SoulsGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngSouls; g.OnGrabbed = note => { _n7 = "souls " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // wait souls grab -> fixture branch + villages evidence grab
            {
                if (_n7.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n7.Length == 0) _n7 = "souls evidence: FAIL (no grab within 20 s)";

                string fixAbs = Path.Combine(Application.dataPath, "..", FixturePath);
                _fix = JsonUtility.FromJson<WorldState>(File.ReadAllText(fixAbs));
                if (_fix == null || _fix.villages == null || _fix.villages.Length == 0) { SafeFail("fixture parse: no villages in " + FixturePath); return; }

                // expected order: pop DESC, name ASC tiebreak (the view's sort law)
                var exp = (WorldVillage[])_fix.villages.Clone();
                Array.Sort(exp, (a, b) => b.pop != a.pop ? b.pop.CompareTo(a.pop) : string.CompareOrdinal(a.name ?? "", b.name ?? ""));

                _view.SetStateFixture(_fix);
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                _view.OpenVillageDossier(0);
                bool orderOk = true;
                for (int i = 0; i < exp.Length; i++)
                    if (_view.VillageRowName(i) != exp[i].name || _view.VillageRowPop(i) != exp[i].pop) orderOk = false;
                var e0 = exp[0];
                bool dossOk = _view.VillageDossierName == e0.name && _view.VillageDossierPop == e0.pop
                           && _view.VillageDossierGen == e0.maxGen && _view.VillageDossierCrafts == e0.crafts
                           && _view.VillageDossierKnows == (e0.knows != null ? e0.knows.Length : 0);
                bool countOk = _view.VillageRowCount == _fix.villages.Length;
                _villRowsFixture = _view.VillageRowCount;   // stamped at the fixture measurement (review I1/R1)
                _n4 = $"villages FIXTURE (y055, real export): rows {_view.VillageRowCount}=={_fix.villages.Length}, pop-DESC order {(orderOk ? "OK" : "FAIL")}, dossier '{_view.VillageDossierName}' pop {_view.VillageDossierPop} gen {_view.VillageDossierGen} crafts {_view.VillageDossierCrafts} knows {_view.VillageDossierKnows} ({(countOk && orderOk && dossOk ? "OK" : "FAIL")})";

                var g = new GameObject("Fas5VillGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngVill; g.OnGrabbed = note => { _n8 = "villages " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // wait villages grab -> clear fixture + chronicle handoff + overview regression
            {
                if (_n8.Length == 0 && Time.unscaledTime - _grabAskedAt < 20f) return;
                if (_n8.Length == 0) _n8 = "villages evidence: FAIL (no grab within 20 s)";

                _view.SetStateFixture(null);
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                bool clearOk = _view.VillageRowCount == _liveVillageCount;
                _n5 = $"fixture cleared: rows back to runtime truth {_view.VillageRowCount}=={_liveVillageCount} ({(clearOk ? "OK" : "FAIL")})";

                var chron = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleView>();
                _tpsBefore = c.ticksPerSecond;
                _view.SelectTab(Fas5AlmanacView.TabChronicle);   // handoff, never a rebuild
                bool handOk = chron != null && chron.BookOpen && !_view.AlmanacOpen && c.paused;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                if (chron != null && chron.BookOpen) chron.CloseBook();
                bool chainOk = c.paused;   // prior mode was PAUSED — the whole chain must preserve it
                _n6 = $"chronicle tab = handoff to the Fas 4 book: almanac closed, book opened={handOk}, tps {_tpsBefore}->{c.ticksPerSecond} ({(tpsOk ? "OK" : "FAIL")}), pause preserved through chain={chainOk} ({(handOk && tpsOk && chainOk ? "OK" : "FAIL")})";

                _view.OpenAlmanac();
                _view.SelectTab(Fas5AlmanacView.TabOverview);
                var latest = _rec.Latest();
                bool ovOk = _view.TilePop == latest.pop && _view.TileBirths == _rec.TotalBirths
                         && _view.TileDeaths == _rec.TotalDeaths && _view.CurvePointCount == _rec.RecordCount
                         && _view.TileYear == c.PresentationYear;
                _n9 = $"overview regression after tab traffic: pop {_view.TilePop}/{latest.pop} births {_view.TileBirths}/{_rec.TotalBirths} deaths {_view.TileDeaths}/{_rec.TotalDeaths} curve {_view.CurvePointCount}/{_rec.RecordCount} år {_view.TileYear}/{c.PresentationYear} ({(ovOk ? "OK" : "FAIL")})";
                _view.CloseAlmanac();
                _phase = 99;
            }
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                foreach (var n in new[] { _n1, _n2, _n3, _n4, _n5, _n6, _n7, _n8, _n9 })
                    sb.AppendLine(n.Length > 0 ? n : "check never reached (FAIL)");
                sb.AppendLine();
                sb.AppendLine("caveat: Souls sorts by WEALTH since E1.5 (agents[].wealth; all-zero old exports degrade to the age tie law);");
                sb.AppendLine("roles/traits/bonds await the engine metrics (R2); Society carries its E1.5 first honest view (wealth/leaders/");
                sb.AppendLine("witnessed violence — proven by Fas7E15BodyProbe); Tech&Memory/Dynasty are honest stubs naming what they await;");
                sb.AppendLine("the populated-villages branch is fixture-proven");
                sb.AppendLine("(seq-8919-y055.json, a real engine export through the SAME rebuild path — D-131 school; villages emerge y30-y55 at this seed,");
                sb.AppendLine("an 8-minute live sim buys no additional mechanism truth).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)") && _n3.Contains("(OK)")
                    && _n4.Contains("(OK)") && _n5.Contains("(OK)") && _n6.Contains("(OK)")
                    && _n7.Contains("OK") && !_n7.Contains("FAIL")
                    && _n8.Contains("OK") && !_n8.Contains("FAIL")
                    && _n9.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — the almanac has its skeleton: Villages live, Souls base, honest stubs, the book one tab away"
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} villagesFixtureRows={_villRowsFixture} soulsRowsLive={_soulsRowsLive}\nsee {Report}\n");   // key figures are measurement-time stamps, a mirror of the report's OK lines (review R1)
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
