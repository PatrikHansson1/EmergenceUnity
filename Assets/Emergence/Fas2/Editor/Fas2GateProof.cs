// EMERGENCE — FAS 2 GATE-PROOF (D-131): the grind-review's missing evidence, honestly labeled.
//
// The review found Work=0 and no carry. ROOT CAUSE (audited below): the ENGINE's exported task
// vocabulary contains no work/carry verbs in ANY owned snapshot — the presentation cannot honestly
// show work the sim never says. So this probe proves the MECHANISM with a FIXTURE: a copy of the
// y120 state where a handful of agents' `task` strings are edited to the engine-style work/carry
// phrases ("working the field", "chopping wood", "building a hut", "carrying clay home"). This is a
// presentation UNIT TEST — clearly labeled, never shipped; the product only ever reads real state
// (D-078 r4). It also writes the full TASK-VOCABULARY AUDIT (every task string in every owned
// snapshot -> its read) and scans evidence with the HARDENED magenta detector (catches ACES-
// tonemapped #FF00FF ≈ rgb(207,33,207) that the old r>220 threshold missed).
// Menu: Emergence/Fas2/RUN GATE PROOF.  Headless: drop Reports/RUN_FAS2GATE.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas2GateProof
    {
        const string World120 = "Assets/Emergence/WorldStates/world-8919-y120-newforces.json";
        const string StatesDir = "Assets/Emergence/WorldStates";

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS2GATE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS2GATE_DONE.txt");
        const string Report   = "Reports/fas2-gate-proof.txt";
        const string KeyPending = "emg.fas2gate.pending", KeyStart = "emg.fas2gate.start", KeyReport = "emg.fas2gate.report",
                     KeyWork = "emg.fas2gate.workids", KeyCarry = "emg.fas2gate.carryids";

        static int _frames, _magenta = -1, _magentaTone = -1;
        static string _invariant = "";

        static Fas2GateProof() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas2/RUN GATE PROOF")]
        public static void RunMenu() => EditPhase();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup >= _next)
            {
                _next = EditorApplication.timeSinceStartup + 0.5;
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
            bool overtime = EditorApplication.timeSinceStartup - start > 60.0;

            if (EditorApplication.isPlaying)
            {
                try
                {
                    _frames++;
                    if (_frames == 2) Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    EditorApplication.QueuePlayerLoopUpdate();

                    if (_frames == 60) _invariant = CheckInvariant();
                    if (_frames == 80) FrameOnIds(KeyWork, 8f);
                    if (_frames == 84) { var m = Capture("gate-work-live"); _magenta = m.classic; _magentaTone = m.tonemapped; }
                    if (_frames == 96) FrameOnIds(KeyCarry, 6f);
                    if (_frames == 100) { var m = Capture("gate-carry-live"); _magenta = Math.Max(_magenta, m.classic); _magentaTone = Math.Max(_magentaTone, m.tonemapped); }
                    if (_frames >= 120 || overtime) FinishPlay(overtime);
                }
                catch (Exception e) { SafeFail("play: " + e.Message); }
            }
            else if (overtime) SafeFail("play mode did not start within 60s");
        }

        static void EditPhase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 2 GATE-PROOF (D-131)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("FIXTURE-BASED MECHANISM PROOF — clearly labeled presentation unit test, never shipped.");
            sb.AppendLine();

            // ---- TASK-VOCABULARY AUDIT: every task string the engine has ever exported -> its read ----
            var vocab = new SortedDictionary<string, (int n, string read)>();
            foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "..", StatesDir.Replace("Assets/", "Assets/")), "*.json"))
            {
                WorldState s;
                try { s = JsonUtility.FromJson<WorldState>(File.ReadAllText(f)); } catch { continue; }
                if (s?.agents == null) continue;
                foreach (var a in s.agents)
                {
                    string read = AgentTaskRead.StateFor(a.task, true);
                    vocab[a.task ?? ""] = vocab.TryGetValue(a.task ?? "", out var v) ? (v.n + 1, read) : (1, read);
                }
            }
            sb.AppendLine("## TASK-VOCABULARY AUDIT (all owned snapshots)");
            foreach (var kv in vocab) sb.AppendLine($"  {kv.Value.n,4}x  \"{kv.Key}\" -> {kv.Value.read}");
            bool anyWork = vocab.Any(kv => kv.Value.read == "Work");
            sb.AppendLine($"  coverage: {vocab.Count} distinct strings, 100% classified; engine exports work-verbs: {(anyWork ? "YES" : "NO — engine-lane gap, flagged (Simulation-Architect)")}");
            sb.AppendLine();

            // ---- FIXTURE: y120 copy with work/carry phrases on a handful of adults ----
            WorldDresser.Build(World120);
            var sc = GameObject.Find("CodexObjects"); if (sc != null) UnityEngine.Object.DestroyImmediate(sc);
            var sa = GameObject.Find("Agents");       if (sa != null) UnityEngine.Object.DestroyImmediate(sa);
            var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(World120));
            PresentationEventBus.Clear();
            new LiveReconciler().Reconcile(S);

            var adults = S.agents.Where(a => a.age >= 20 && a.age <= 50).OrderBy(a => a.id).ToList();
            var workIds = new List<int>(); var carryIds = new List<int>();
            string[] workPhrases = { "working the field", "working the field", "chopping wood", "chopping wood", "building a hut", "working the mill" };
            for (int i = 0; i < workPhrases.Length && i < adults.Count; i++) { adults[i].task = workPhrases[i]; workIds.Add(adults[i].id); }
            for (int i = workPhrases.Length; i < workPhrases.Length + 3 && i < adults.Count; i++) { adults[i].task = "carrying clay home"; carryIds.Add(adults[i].id); }
            // cluster the fixture agents so one camera catches them (fixture may move fixture agents)
            if (workIds.Count > 0)
            {
                var anchor = adults[0];
                for (int i = 1; i < workIds.Count + carryIds.Count && i < adults.Count; i++)
                { adults[i].x = anchor.x + 1.1f * (i % 4) - 1.6f; adults[i].y = anchor.y + 1.1f * (i / 4) - 0.8f; }
            }

            var agents = new AgentReconciler();
            var d = agents.Reconcile(S, true);
            int props = GameObject.Find(AgentReconciler.LayerName).GetComponentsInChildren<Transform>(true).Count(t => t.name == "CarryProp_D131");
            sb.AppendLine("## FIXTURE (unit test — task strings edited on 9 adults, engine-style phrasing)");
            sb.AppendLine($"souls={agents.Count} ({d}); fixture: work={workIds.Count} agents [{string.Join(",", workIds)}], carry={carryIds.Count} [{string.Join(",", carryIds)}]");
            sb.AppendLine($"carry props attached (edit phase): {props}/{carryIds.Count}");
            try { EmergenceLightRig.Apply(string.IsNullOrEmpty(S.season) ? "spring" : S.season, "day"); EmergencePostStack.Apply("day"); }
            catch (Exception e) { Debug.LogWarning("[GateProof] look: " + e.Message); }

            SessionState.SetString(KeyReport, sb.ToString());
            SessionState.SetString(KeyWork, string.Join(",", workIds));
            SessionState.SetString(KeyCarry, string.Join(",", carryIds));
            SessionState.SetInt(KeyPending, 1);
            SessionState.SetFloat(KeyStart, (float)EditorApplication.timeSinceStartup);
            _frames = 0; _magenta = _magentaTone = -1; _invariant = "";
            File.WriteAllText(Done, "RUNNING (entering play mode) " + DateTime.Now.ToString("HH:mm:ss") + "\n");
            EditorApplication.EnterPlaymode();
        }

        static List<AgentAnimator> Ids(string key)
        {
            var ids = SessionState.GetString(key, "").Split(',').Where(s => s.Length > 0).Select(int.Parse).ToHashSet();
            var l = new List<AgentAnimator>();
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer != null) foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>()) if (ids.Contains(aa.agentId)) l.Add(aa);
            return l;
        }

        static string CheckInvariant()
        {
            int workOk = 0, workOff = 0, carryOk = 0, carryOff = 0, propOk = 0;
            foreach (var aa in Ids(KeyWork))
            {
                var an = aa.GetComponentInChildren<Animator>();
                bool ok = an != null && (an.GetCurrentAnimatorStateInfo(0).IsName("Work")
                       || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName("Work")));
                if (ok) workOk++; else workOff++;
            }
            foreach (var aa in Ids(KeyCarry))
            {
                var an = aa.GetComponentInChildren<Animator>();
                string expect = aa.InTransit ? "Walk" : AgentTaskRead.StateFor(aa.task, aa.canWork);
                bool ok = an != null && (an.GetCurrentAnimatorStateInfo(0).IsName(expect)
                       || (an.IsInTransition(0) && an.GetNextAnimatorStateInfo(0).IsName(expect)));
                if (ok) carryOk++; else carryOff++;
                bool prop = aa.GetComponentsInChildren<Transform>(true).Any(t => t.name == "CarryProp_D131");
                if (prop) propOk++;
            }
            // A2 spot check (D-131 interim → D-159 polish): tempo == the ONE law TempoFor(band, sayAct)
            int moodChecked = 0, moodOk = 0;
            var layer = GameObject.Find(AgentReconciler.LayerName);
            if (layer != null)
                foreach (var aa in layer.GetComponentsInChildren<AgentAnimator>())
                {
                    if (AgentAnimator.TempoFor(aa.band, aa.sayAct) == 1f) continue;
                    var an = aa.GetComponentInChildren<Animator>();
                    if (an == null) continue;
                    moodChecked++;
                    if (Mathf.Abs(an.speed - AgentAnimator.TempoFor(aa.band, aa.sayAct)) < 0.001f) moodOk++;
                }
            return $"WORK: {workOk} in Work state, {workOff} off | CARRY: {carryOk} read-right, {carryOff} off, props {propOk} | A2-mood tempo: {moodOk}/{moodChecked} applied";
        }

        static void FrameOnIds(string key, float dist)
        {
            var cam = Camera.main;
            if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
            var l = Ids(key);
            if (l.Count == 0) return;
            var c = Vector3.zero; foreach (var aa in l) c += aa.transform.position; c /= l.Count;
            cam.transform.position = c + new Vector3(dist * 0.55f, dist * 0.5f, -dist * 0.8f);
            cam.transform.LookAt(c + Vector3.up * 0.9f);
        }

        static (int classic, int tonemapped) Capture(string name)
        {
            var cam = Camera.main; if (cam == null) return (-1, -1);
            bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            cam.targetTexture = null; RenderTexture.active = null;
            RenderSettings.fog = fogWas;
            var px = tex.GetPixels32(); int classic = 0, tone = 0;
            foreach (var c in px)
            {
                if (c.r > 220 && c.b > 220 && c.g < 80) classic++;
                else if (Math.Abs(c.r - c.b) < 15 && c.r > 170 && c.g < c.r - 90) tone++;   // ACES-tonemapped #FF00FF
            }
            const string dir = @"C:\Users\patri\Dropbox\Emergence\45-UNITY\evidence\fas2";
            try { Directory.CreateDirectory(dir); File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG()); } catch {}
            UnityEngine.Object.Destroy(tex); UnityEngine.Object.Destroy(rt);
            return (classic, tone);
        }

        static void FinishPlay(bool overtime)
        {
            try
            {
                var sb = new StringBuilder(SessionState.GetString(KeyReport, ""));
                sb.AppendLine();
                sb.AppendLine($"## PLAY PHASE (frames={_frames}{(overtime ? ", WATCHDOG cut" : "")})");
                sb.AppendLine(_invariant.Length > 0 ? _invariant : "(invariant did not run)");
                sb.AppendLine($"magenta classic={_magenta}  TONEMAPPED={_magentaTone} (hardened detector, D-131)   evidence: gate-work-live.png, gate-carry-live.png");
                bool green = _invariant.Contains(" 0 off") && !_invariant.Contains("off | CARRY: 0") && _magenta == 0 && _magentaTone == 0 && !overtime
                          && _invariant.StartsWith("WORK:") && !_invariant.Contains("WORK: 0 in");
                sb.AppendLine();
                sb.AppendLine("verdict: " + (green ? "GREEN — the mechanism works: when the sim says work/carry, the body works/carries"
                                                   : "CHECK — see numbers above"));
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={(green ? "GREEN" : "CHECK")} {_invariant} magentaTone={_magentaTone}\nsee {Report}\n");
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
