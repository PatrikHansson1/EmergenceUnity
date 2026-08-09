// EMERGENCE — FAS 7 PROBE: THE C-LOSS WITNESS — a village loses knowledge and the body SEES it.
//
// D-182 (reviewer condition on TD-084/TD-086): condition C's LOSS half must be witnessed with
// CONTENT assertions — pop/maxGen/avgAge/crafts/knows against independent recomputation, not a
// smoke test that "parsed without crashing". The independent recomputation lives node-side
// (Tools/verify-closs.js -> Reports/fas7-closs-node.txt: own membership+aggregation code vs
// E.villageScope vs the exported fixture files — 20/20 OK, engine's own knowledgeLost narration
// quoted). THIS probe is the body half: the SAME canon numbers hard-asserted through the body's
// parse, the SAME Apply path, and the Almanac dossier the player actually reads.
//
// The story (seed 97013 — canon golden seed, lostEv=26, chosen from the D-183 sweep candidates):
//   y54: Torvhaven pop=10 maxGen=4 avgAge=17 crafts=34 (knows writing/tin/glassblowing/optics)
//   y55: Torvhaven pop=10 maxGen=4 avgAge=18 crafts=30 — the SAME ten souls, FOUR crafts dead
//        (writing, tin, glassblowing, optics); neighbor Bjornheim keeps all 34 — the villages
//        DISTINGUISH in what they KNOW (the review's F4: the old fixture showed zero difference).
//   Engine narration (node artifact): "In Torvhaven, Torv's script is lost…" ×4 (year+1 label law).
//
// Checks:
//   1. fixture canon: BOTH villages BOTH years — pop/maxGen/avgAge/crafts == node truth, contract
//      crafts==knows.length, lost-set EXACTLY {writing,tin,glassblowing,optics}, strict subset,
//      Bjornheim byte-unchanged (distinction);
//   2. PRE dossier (fixture through the SAME Fas3WorldRuntime.Apply under FixtureInjection, view on
//      SetStateFixture): name/pop/gen/crafts/knows == fixture, chips ELEMENT-WISE == knows[] (the
//      new VillageDossierChip seam), the four doomed crafts VISIBLE;
//   3. evidence PRE: the dossier with 34 chips (blankness-guarded, humanly looked at, D-008);
//   4. POST dossier: crafts 34->30, knows count 34->30, each lost craft GONE from the chips,
//      pop HOLDS at 10 (knowledge died, not people); Bjornheim dossier still 34 (distinction in
//      the view itself);
//   5. evidence POST: the shrunken dossier;
//   6. fixture silence: the chronicle witnessed NOTHING through both Applies (injection is
//      reconstruction, never witnessed history, D-161) — suppressed grew, entries unchanged;
//   7. clock honesty: tps untouched.
// Declared limit: the LIVE feed-witnessing of a loss (chronicle entry born in the running loop)
// awaits the motor lane's deep-tick order (D-149) — y55 live is producer-bound; the fixture school
// (D-152) buys the mechanism truth here.
// DONE key figures are stamped AT MEASUREMENT TIME (the R1 law, D-155).
// Menu: Emergence/Fas7/RUN C-LOSS WITNESS.  Headless: drop Reports/RUN_FAS7CLOSS.trigger.
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
    public static class Fas7CLossProbe
    {
        const long Seed = 8919;                 // live-loop seed (the fixtures replace state wholesale)
        const double Watchdog = 700.0;
        const int LiveYear = 3;

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS7CLOSS.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS7CLOSS_DONE.txt");
        const string Report   = "Reports/fas7-closs.txt";
        const string PngPre   = "Reports/fas7-closs-pre.png";
        const string PngPost  = "Reports/fas7-closs-post.png";
        const string GenesisPath = "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json";
        const string FixturePre  = "Assets/Emergence/WorldStates/world-97013-y54-e15.json";
        const string FixturePost = "Assets/Emergence/WorldStates/world-97013-y55-e15.json";
        const string KeyPending = "emg.fas7closs.pending", KeyStart = "emg.fas7closs.start", KeyReport = "emg.fas7closs.report";

        // node truth (Tools/verify-closs.js 97013 54 55 Torvhaven -> Reports/fas7-closs-node.txt, GREEN)
        const string Village = "Torvhaven", Neighbor = "Bjornheim";
        static readonly string[] Lost = { "writing", "tin", "glassblowing", "optics" };
        const int PrePop = 10, PreGen = 4, PreAge = 17, PreCrafts = 34;
        const int PostPop = 10, PostGen = 4, PostAge = 18, PostCrafts = 30;
        const int NbPop = 8, NbGen = 4, NbPreAge = 31, NbPostAge = 32, NbCrafts = 34;

        static int _frames, _phase;
        static Fas3Onboarding _onb;
        static Fas4ChronicleFeed _feed;
        static Fas5AlmanacView _view;
        static float _tpsBefore;
        static WorldState _pre, _post;
        static float _grabAskedAt;
        static int _feedBefore, _suppBefore;
        // measurement-time stamps (R1 law)
        static int _preChips = -1, _postChips = -1, _lostGone = -1, _nbCrafts = -1;
        static string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _n6 = "", _n7 = "";

        static Fas7CLossProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas7/RUN C-LOSS WITNESS")]
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
            sb.AppendLine("EMERGENCE — FAS 7 PROBE: the C-loss witness — Torvhaven loses writing/tin/glassblowing/optics and the dossier SEES it");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("data sources = world-97013-y54-e15.json + world-97013-y55-e15.json (REAL Engine 2.4.1 exports,");
            sb.AppendLine("driver-parity serializer over the StreamingAssets twin) through the SAME Fas3WorldRuntime.Apply path;");
            sb.AppendLine("independent recomputation = Tools/verify-closs.js -> Reports/fas7-closs-node.txt (node, own aggregation code)");
            sb.AppendLine();

            WorldDresser.Build(GenesisPath);
            foreach (var n in new[] { "CodexObjects", "Agents", "Huts", "Yards", "HutAge" })
            { var go = GameObject.Find(n); if (go != null) UnityEngine.Object.DestroyImmediate(go); }
            PresentationEventBus.Clear();
            PresentationEventBus.ResetSubscribers();
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
            var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
            onb.seed = Seed; onb.targetYear = -1;

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _phase = 0; _onb = null; _feed = null; _view = null;
            _tpsBefore = 0f; _pre = _post = null; _grabAskedAt = 0f;
            _feedBefore = _suppBefore = 0;
            _preChips = _postChips = _lostGone = _nbCrafts = -1;
            _n1 = _n2 = _n3 = _n4 = _n5 = _n6 = _n7 = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static WorldVillage Vil(WorldState S, string name)
        {
            if (S?.villages == null) return null;
            foreach (var v in S.villages) if (v.name == name) return v;
            return null;
        }

        static bool Has(string[] arr, string k) { return arr != null && Array.IndexOf(arr, k) >= 0; }

        // the view's sort law, recomputed independently: pop DESC, name ASC
        static int RowOf(WorldState S, string name)
        {
            var sorted = (WorldVillage[])S.villages.Clone();
            Array.Sort(sorted, (a, b) => b.pop != a.pop ? b.pop.CompareTo(a.pop) : string.CompareOrdinal(a.name ?? "", b.name ?? ""));
            for (int i = 0; i < sorted.Length; i++) if (sorted[i].name == name) return i;
            return -1;
        }

        static bool CanonOk(WorldVillage v, int pop, int gen, int age, int crafts, out string why)
        {
            why = v == null ? "village missing" :
                  v.pop != pop ? $"pop {v.pop}!={pop}" :
                  v.maxGen != gen ? $"maxGen {v.maxGen}!={gen}" :
                  v.avgAge != age ? $"avgAge {v.avgAge}!={age}" :
                  v.crafts != crafts ? $"crafts {v.crafts}!={crafts}" :
                  v.knows == null || v.knows.Length != crafts ? $"contract knows.len {(v.knows == null ? -1 : v.knows.Length)}!={crafts}" : "";
            return why.Length == 0;
        }

        static void Drive()
        {
            if (_phase == 0)
            {
                _onb = UnityEngine.Object.FindAnyObjectByType<Fas3Onboarding>();
                _feed = UnityEngine.Object.FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = UnityEngine.Object.FindAnyObjectByType<Fas5AlmanacView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.World == null
                    || _feed == null || _view == null) return;

                // 1. fixture canon vs node truth — content, not compilation properties (D-176)
                _pre = JsonUtility.FromJson<WorldState>(File.ReadAllText(Path.Combine(Application.dataPath, "..", FixturePre)));
                _post = JsonUtility.FromJson<WorldState>(File.ReadAllText(Path.Combine(Application.dataPath, "..", FixturePost)));
                if (_pre == null || _post == null || _pre.villages == null || _post.villages == null)
                { SafeFail("fixture parse"); return; }
                if (!(_pre.engineVersion ?? "").StartsWith("2.4") || !(_post.engineVersion ?? "").StartsWith("2.4"))
                { SafeFail("fixtures are not 2.4.x exports"); return; }

                var tvPre = Vil(_pre, Village); var tvPost = Vil(_post, Village);
                var nbPre = Vil(_pre, Neighbor); var nbPost = Vil(_post, Neighbor);
                bool cPre = CanonOk(tvPre, PrePop, PreGen, PreAge, PreCrafts, out string w1);
                bool cPost = CanonOk(tvPost, PostPop, PostGen, PostAge, PostCrafts, out string w2);
                bool cNbPre = CanonOk(nbPre, NbPop, NbGen, NbPreAge, NbCrafts, out string w3);
                bool cNbPost = CanonOk(nbPost, NbPop, NbGen, NbPostAge, NbCrafts, out string w4);
                int lostHeld = 0, lostGoneFx = 0; bool subset = true;
                foreach (var k in Lost)
                {
                    if (Has(tvPre.knows, k)) lostHeld++;
                    if (!Has(tvPost.knows, k)) lostGoneFx++;
                }
                foreach (var k in tvPost.knows) if (!Has(tvPre.knows, k)) subset = false;
                bool nbSame = nbPre.knows != null && nbPost.knows != null && nbPre.knows.Length == nbPost.knows.Length;
                if (nbSame) for (int i = 0; i < nbPre.knows.Length; i++) if (nbPre.knows[i] != nbPost.knows[i]) nbSame = false;
                bool ok1 = cPre && cPost && cNbPre && cNbPost && lostHeld == 4 && lostGoneFx == 4 && subset && nbSame
                        && tvPre.knows.Length - tvPost.knows.Length == 4;
                _n1 = $"fixture canon vs node truth: {Village} y54 {{10,4,17,34}} {(cPre ? "ok" : "FAIL " + w1)}, y55 {{10,4,18,30}} {(cPost ? "ok" : "FAIL " + w2)}; "
                    + $"{Neighbor} y54 {{8,4,31,34}} {(cNbPre ? "ok" : "FAIL " + w3)}, y55 {{8,4,32,34}} {(cNbPost ? "ok" : "FAIL " + w4)}; "
                    + $"lost set held@pre {lostHeld}/4, gone@post {lostGoneFx}/4, strict subset={subset}, neighbor knows byte-unchanged={nbSame} ({(ok1 ? "OK" : "FAIL")})";
                _phase = 1;
                return;
            }

            var w = _onb.World; var c = _onb.Clock;
            if (_onb.Driver.LastError.Length > 0) { SafeFail("driver: " + _onb.Driver.LastError); return; }

            if (_phase == 1)   // live to y3, then PRE through the SAME Apply path; dossier with 34 chips
            {
                if (w.LastState == null || w.LastAppliedYear < LiveYear) return;
                _tpsBefore = c.ticksPerSecond;
                c.paused = true;

                _feedBefore = _feed.Entries.Count; _suppBefore = _feed.SuppressedDuringFixture;
                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_pre); } finally { Fas3WorldRuntime.FixtureInjection = false; }

                _view.SetStateFixture(_pre);
                _view.OpenAlmanac();
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                int row = RowOf(_pre, Village);
                if (row < 0) { SafeFail("no " + Village + " row"); return; }
                _view.OpenVillageDossier(row);

                var tv = Vil(_pre, Village);
                bool idOk = _view.VillageDossierName == Village && _view.VillageDossierPop == PrePop
                         && _view.VillageDossierGen == PreGen && _view.VillageDossierCrafts == PreCrafts
                         && _view.VillageDossierKnows == PreCrafts;
                bool chipsOk = true; int held = 0;
                for (int i = 0; i < tv.knows.Length; i++) if (_view.VillageDossierChip(i) != tv.knows[i]) chipsOk = false;
                if (_view.VillageDossierChip(tv.knows.Length) != "") chipsOk = false;   // no phantom chips
                foreach (var k in Lost) { bool found = false; for (int i = 0; i < PreCrafts; i++) if (_view.VillageDossierChip(i) == k) { found = true; break; } if (found) held++; }
                _preChips = _view.VillageDossierKnows;   // stamped (R1)
                bool ok2 = idOk && chipsOk && held == 4;
                _n2 = $"PRE dossier '{_view.VillageDossierName}': pop {_view.VillageDossierPop}==10, gen {_view.VillageDossierGen}==4, hantverk {_view.VillageDossierCrafts}==34, chips {_preChips}==34 element-wise=={(chipsOk ? "fixture" : "MISMATCH")}, doomed crafts visible {held}/4 ({(ok2 ? "OK" : "FAIL")})";

                var g = new GameObject("Fas7CLossPreGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngPre; g.OnGrabbed = note => { _n3 = "evidence(pre) " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // wait pre grab -> POST through the SAME Apply path; the shrink
            {
                if (_n3.Length == 0 && Time.unscaledTime - _grabAskedAt < 16f) return;
                if (_n3.Length == 0) _n3 = "evidence(pre): FAIL (no grab within 16 s)";

                Fas3WorldRuntime.FixtureInjection = true;
                try { w.Apply(_post); } finally { Fas3WorldRuntime.FixtureInjection = false; }

                _view.SetStateFixture(_post);
                int row = RowOf(_post, Village);
                if (row < 0) { SafeFail("no " + Village + " row post"); return; }
                _view.SelectTab(Fas5AlmanacView.TabVillages);
                _view.OpenVillageDossier(row);

                var tv = Vil(_post, Village);
                bool idOk = _view.VillageDossierName == Village && _view.VillageDossierPop == PostPop
                         && _view.VillageDossierGen == PostGen && _view.VillageDossierCrafts == PostCrafts
                         && _view.VillageDossierKnows == PostCrafts;
                bool chipsOk = true;
                for (int i = 0; i < tv.knows.Length; i++) if (_view.VillageDossierChip(i) != tv.knows[i]) chipsOk = false;
                if (_view.VillageDossierChip(tv.knows.Length) != "") chipsOk = false;
                int gone = 0;
                foreach (var k in Lost) { bool found = false; for (int i = 0; i < PostCrafts; i++) if (_view.VillageDossierChip(i) == k) { found = true; break; } if (!found) gone++; }
                _postChips = _view.VillageDossierKnows; _lostGone = gone;   // stamped (R1)
                bool ok4 = idOk && chipsOk && gone == 4;
                _n4 = $"POST dossier '{_view.VillageDossierName}': pop {_view.VillageDossierPop}==10 (HOLDS — knowledge died, not people), hantverk {_view.VillageDossierCrafts} 34->30, chips {_postChips}==30 element-wise=={(chipsOk ? "fixture" : "MISMATCH")}, lost crafts GONE from chips {gone}/4 (writing/tin/glassblowing/optics) ({(ok4 ? "OK" : "FAIL")})";

                var g = new GameObject("Fas7CLossPostGrabber").AddComponent<Fas4NativeGrabber>();
                g.Path = PngPost; g.OnGrabbed = note => { _n5 = "evidence(post) " + note; };
                _grabAskedAt = Time.unscaledTime;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // wait post grab -> neighbor distinction in the VIEW, resume, honesty
            {
                if (_n5.Length == 0 && Time.unscaledTime - _grabAskedAt < 16f) return;
                if (_n5.Length == 0) _n5 = "evidence(post): FAIL (no grab within 16 s)";

                int nbRow = RowOf(_post, Neighbor);
                if (nbRow < 0) { SafeFail("no " + Neighbor + " row post"); return; }
                _view.OpenVillageDossier(nbRow);
                _nbCrafts = _view.VillageDossierCrafts;   // stamped (R1)
                bool ok6 = _view.VillageDossierName == Neighbor && _nbCrafts == NbCrafts && _nbCrafts != PostCrafts;
                _n6 = $"distinction in the VIEW: neighbor '{_view.VillageDossierName}' dossier hantverk {_nbCrafts}==34 while {Village} shows 30 — two villages, two knowledges ({(ok6 ? "OK" : "FAIL")})";

                _view.CloseAlmanac();
                _view.SetStateFixture(null);
                c.paused = false;
                bool silent = _feed.Entries.Count == _feedBefore && _feed.SuppressedDuringFixture > _suppBefore;
                bool tpsOk = Mathf.Approximately(c.ticksPerSecond, _tpsBefore);
                _n7 = $"honesty: chronicle SILENT through both Applies (entries {_feed.Entries.Count}=={_feedBefore}, suppressed +{_feed.SuppressedDuringFixture - _suppBefore}) — injection is reconstruction (D-161); resume tps {c.ticksPerSecond}=={_tpsBefore} ({(silent && tpsOk ? "OK" : "FAIL")})";
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
                sb.AppendLine("lane honesty: this closes condition C's LOSS half as a WITNESSED, content-asserted fact —");
                sb.AppendLine("the independent recomputation chain is Reports/fas7-closs-node.txt (own aggregation code vs");
                sb.AppendLine("E.villageScope vs the exported files, 20/20), and THIS report is the body's half: the same");
                sb.AppendLine("canon numbers through JsonUtility parse, the SAME Apply path, and the dossier the player reads.");
                sb.AppendLine("The engine narrated the loss itself (knowledgeLost x4, year+1 label law — quoted in the node");
                sb.AppendLine("artifact). Declared limit: LIVE feed-witnessing of a loss (a chronicle entry born in the running");
                sb.AppendLine("loop at y55) awaits the motor lane's deep-tick (D-149) — the fixture school (D-152) buys the");
                sb.AppendLine("mechanism truth; nothing here is a compilation property (D-176).");
                bool green = !overtime
                    && _n1.Contains("(OK)") && _n2.Contains("(OK)")
                    && _n3.Contains("OK") && !_n3.Contains("FAIL")
                    && _n4.Contains("(OK)")
                    && _n5.Contains("OK") && !_n5.Contains("FAIL")
                    && _n6.Contains("(OK)") && _n7.Contains("(OK)");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green
                    ? "GREEN — Torvhaven forgot writing, tin, glassblowing and optics; ten souls remain; Bjornheim remembers. The almanac saw it happen."
                    : "CHECK — see notes above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} preChips={_preChips}/34 postChips={_postChips}/30 lostGone={_lostGone}/4 nbCrafts={_nbCrafts}/34\nsee {Report} + Reports/fas7-closs-node.txt\n");   // measurement-time stamps (R1 law)
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
