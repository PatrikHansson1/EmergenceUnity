// EMERGENCE — FAS 7 increment 1: SAVE/LOAD IN A REAL PLAYER — the proof observer (D-138 school).
//
// The editor probe proved the mechanism; this component proves the SAME sequence inside a built
// player: continuous run to the save year -> Save -> full teardown (worker waited out, checkpoint
// grid WIPED) -> cold boot (startPaused) + Fas7LoadBoot -> SHA proof (resimulated == continuous ==
// save anchor) -> mode restored -> the loaded world lives on (next year applies). Writes
// saveload-player.txt beside the exe, saves an evidence frame through the SHARED framing law
// (EvidenceFraming.FrameSubjects — runtime since Fas 7 ink. 1), magenta-scanned + blank-guarded,
// quits. It COMPOSES the boots itself (the scene ships wilderness + camera + this observer only).
// D-078 r4: presentation APIs only; the sim is never touched.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas7SaveLoadPlayerProof : MonoBehaviour
    {
        public long seed = 8919;
        public int saveYear = 6;
        public float watchdogSecs = 300f;

        int _phase; int _waitFrames; float _waitAnchor;
        Fas3Onboarding _onb;
        Fas3SimDriver _oldDriver;
        Fas7LoadBoot _boot;
        Fas7SaveData _saved;
        string _shaCont = "", _shaLoad = "";
        int _chkDeleted = -1, _feedNew = -1, _liveOnYear = -1;
        int _magenta = -1, _magentaTone = -1;
        string _nSave = "", _nWipe = "", _nSha = "", _nWorld = "", _nMode = "", _nFeed = "", _nLive = "", _nEvid = "", _nEvidHud = "";

        string OutPath => Path.Combine(Application.dataPath, "..", "saveload-player.txt");
        string PngPath => Path.Combine(Application.dataPath, "..", "saveload-player.png");
        string PngHudPath => Path.Combine(Application.dataPath, "..", "saveload-player-hud.png");

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs) { Finish("WATCHDOG(phase" + _phase + ")"); return; }

            if (_phase == 0)   // boot 1: compose the game's own opening
            {
                Application.runInBackground = true;
                var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
                onb.seed = seed; onb.targetYear = -1;
                _onb = onb;
                _phase = 1;
                return;
            }

            if (_phase == 1)   // ride the producer wall to the save year
            {
                if (_onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
                if (_onb.Driver.LastError.Length > 0) { Finish("driverError=" + _onb.Driver.LastError.Replace('\n', ' ')); return; }
                _onb.Clock.ticksPerSecond = Fas3TimeControls.MaxTps;
                _phase = 2;
                return;
            }

            if (_phase == 2)   // save at the witnessed year, stamp the continuous SHA
            {
                var w = _onb.World; var c = _onb.Clock; var d = _onb.Driver;
                if (d.LastError.Length > 0) { Finish("driverError=" + d.LastError.Replace('\n', ' ')); return; }
                if (w.LastAppliedYear < saveYear) return;
                c.ticksPerSecond = Fas3TimeControls.BaseTps;
                c.paused = false;
                _saved = Fas7SaveLoad.Save(d, c, w, out var err);
                if (_saved == null) { Finish("saveError=" + err); return; }
                try { _shaCont = EmergenceJintHost.Sha256Hex(File.ReadAllText(Fas7SaveLoad.CheckpointPath(d, _saved.year))); }
                catch (Exception e) { Finish("contSha=" + e.Message); return; }
                bool ok = File.Exists(Fas7SaveLoad.PathFor(seed)) && _saved.stateSha == _shaCont;
                _nSave = $"save={(ok ? "OK" : "FAIL")}(y{_saved.year},anchor=={_shaCont.Substring(0, 8)})";
                _oldDriver = d;
                _waitFrames = 0;
                _phase = 3;
                return;
            }

            if (_phase == 3)   // teardown + grid wipe (worker waited out)
            {
                if (_waitFrames == 0)
                {
                    _waitAnchor = Time.realtimeSinceStartup;
                    _oldDriver.StopWorker();   // explicit stop — Destroy's OnDestroy is frame-deferred
                    _onb.World.ResetWorld();
                    foreach (var t in new[] { typeof(Fas3Onboarding), typeof(Fas3WorldRuntime), typeof(Fas3SimDriver),
                        typeof(Fas3PresentationClock), typeof(Fas3TimeControls), typeof(Fas3AudioDirector),
                        typeof(Fas6EraAmbience), typeof(Fas6StateAmbience), typeof(Fas4ChronicleFeed),
                        typeof(Fas4ChronicleView), typeof(Fas5MetricsRecorder), typeof(Fas5AlmanacView) })
                    { var o = FindAnyObjectByType(t) as Component; if (o != null) Destroy(o.gameObject); }
                }
                _waitFrames++;
                if (_oldDriver.WorkerAlive)
                { if (Time.realtimeSinceStartup - _waitAnchor > 45f) Finish("workerNeverStopped(45s)"); return; }
                int deleted = 0;
                try
                {
                    foreach (var f in Directory.GetFiles(_oldDriver.CheckpointDir, $"seq-{seed}-y*.json"))
                    { File.Delete(f); deleted++; }
                }
                catch (Exception e) { Finish("gridWipe=" + e.Message); return; }
                _chkDeleted = deleted;
                _nWipe = $"resim={(deleted > 0 ? "OK" : "FAIL")}(grid wiped {deleted} files)";
                PresentationEventBus.Clear();
                PresentationEventBus.ResetSubscribers();
                _waitFrames = 0;
                _phase = 4;
                return;
            }

            if (_phase == 4)   // boot 2: cold load
            {
                if (++_waitFrames < 3) return;
                var onb2 = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
                onb2.seed = seed; onb2.targetYear = -1; onb2.startPaused = true;
                _boot = new GameObject("Fas7LoadBoot").AddComponent<Fas7LoadBoot>();
                _boot.savePath = Fas7SaveLoad.PathFor(seed);
                _onb = onb2;
                _waitFrames = 0;
                _phase = 5;
                return;
            }

            if (_phase == 5)   // restorer verdict + world/mode/feed checks
            {
                if (!_boot.Done) return;
                if (!_boot.Ok) { Finish("loadFail=" + _boot.Note.Replace('\n', ' ')); return; }
                var w = _onb.World; var c = _onb.Clock;
                _shaLoad = _boot.LoadedSha;
                bool shaMatch = _shaLoad == _shaCont && _shaLoad == _saved.stateSha;
                _nSha = $"shaMatch={(shaMatch ? "OK" : "FAIL")}({_shaLoad.Substring(0, 8)})";
                var S = w.LastState;
                bool world = w.LastAppliedYear == _saved.year && S != null
                          && w.AgentCount == S.agents.Length && w.HutCount == S.huts.Length;
                _nWorld = $"loaded={(world ? "OK" : "FAIL")}(y{w.LastAppliedYear},souls{w.AgentCount},huts{w.HutCount})";
                bool mode = !c.paused && Mathf.Approximately(c.ticksPerSecond, _saved.ticksPerSecond);
                _nMode = $"mode={(mode ? "OK" : "FAIL")}(paused={c.paused},tps={c.ticksPerSecond.ToString("F0", CultureInfo.InvariantCulture)})";
                var feed = FindAnyObjectByType<Fas4ChronicleFeed>();
                _feedNew = feed != null ? feed.Entries.Count : -1;
                _nFeed = $"feedNew={(_feedNew == 0 ? "OK" : "FAIL")}({_feedNew})";
                _waitFrames = 0; _waitAnchor = Time.realtimeSinceStartup;
                _phase = 6;
                return;
            }

            if (_phase == 6)   // the loaded world lives on
            {
                var w = _onb.World;
                if (w.LastAppliedYear <= _saved.year) { if (Time.realtimeSinceStartup - _waitAnchor > 30f) { _nLive = "liveOn=FAIL(30s)"; Finish(""); } return; }
                _liveOnYear = w.LastAppliedYear;
                _nLive = $"liveOn=OK(y{_liveOnYear})";

                // evidence through the SHARED framing law (runtime since ink. 1) — subjects must be a
                // frameable CLUSTER: the hut + the 2 souls nearest it (the editor probe's eye lesson)
                var S = w.LastState;
                var subjects = new System.Collections.Generic.List<Vector3>();
                if (S != null && S.huts.Length > 0)
                {
                    var h0 = S.huts[0];
                    subjects.Add(Mapped(S, h0.x, h0.y));
                    var byDist = new System.Collections.Generic.List<WorldAgent>(S.agents);
                    byDist.Sort((a, b) => ((a.x - h0.x) * (a.x - h0.x) + (a.y - h0.y) * (a.y - h0.y))
                                .CompareTo((b.x - h0.x) * (b.x - h0.x) + (b.y - h0.y) * (b.y - h0.y)));
                    for (int i = 0; i < byDist.Count && subjects.Count < 3; i++) subjects.Add(Mapped(S, byDist[i].x, byDist[i].y));
                }
                else if (S != null)
                    for (int i = 0; i < S.agents.Length && subjects.Count < 2; i++) subjects.Add(Mapped(S, S.agents[i].x, S.agents[i].y));
                Vector3 pick = EvidenceFraming.FrameSubjects(out var lookAt, subjects.ToArray());
                var cam = Camera.main;
                if (cam != null) { cam.transform.position = pick; cam.transform.LookAt(lookAt); }
                _waitFrames = 0;
                _phase = 7;
                return;
            }

            if (_phase == 7)   // let a frame render at the picked angle, then grab + finish
            {
                if (++_waitFrames < 5) return;
                CaptureFrame(PngPath);
                // I5 (grind-review runda 1): the framed RT capture never sees IMGUI — grab the
                // backbuffer END-OF-FRAME too, so the evidence bears the HUD (year/tick/tps),
                // the D-142 school (Fas3OnboardPlayerProof.GrabHud).
                StartCoroutine(CaptureHudThenFinish());
                _phase = 8;
                return;
            }
        }

        System.Collections.IEnumerator CaptureHudThenFinish()
        {
            yield return new WaitForEndOfFrame();   // IMGUI (time HUD) has drawn by now
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) { _nEvidHud = "evidenceHud=FAIL(null grab)"; }
                else
                {
                    var px = tex.GetPixels32();
                    int nonWhite = 0, dark = 0;
                    foreach (var p in px)
                    {
                        if (!(p.r > 245 && p.g > 245 && p.b > 245)) nonWhite++;
                        if (p.r < 90 && p.g < 90 && p.b < 90) dark++;
                    }
                    float nw = nonWhite / (float)px.Length;
                    bool ok = nw > 0.10f && dark > px.Length / 1000;   // D-142 blank-guard — never a white sheet again
                    File.WriteAllBytes(PngHudPath, tex.EncodeToPNG());
                    Destroy(tex);
                    _nEvidHud = $"evidenceHud={(ok ? "OK" : "FAIL(blank)")}(backbuffer incl. IMGUI, nonwhite {(nw * 100f).ToString("F0", CultureInfo.InvariantCulture)}%)";
                }
            }
            catch (Exception e) { _nEvidHud = "evidenceHud=FAIL(" + e.Message + ")"; }
            Finish("");
        }

        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        void CaptureFrame(string path)
        {
            try
            {
                var cam = Camera.main; if (cam == null) { _nEvid = "evidence=FAIL(no camera)"; return; }
                bool fogWas = RenderSettings.fog; RenderSettings.fog = false;
                const int pw = 1600, ph = 900;
                var rt = new RenderTexture(pw, ph, 24);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
                cam.targetTexture = null; RenderTexture.active = null;
                RenderSettings.fog = fogWas;
                var px = tex.GetPixels32(); int mag = 0, tone = 0, nonBlack = 0;
                foreach (var p in px)
                {
                    if (p.r > 220 && p.b > 220 && p.g < 80) mag++;
                    else if (Math.Abs(p.r - p.b) < 15 && p.r > 170 && p.g < p.r - 90) tone++;
                    if (p.r + p.g + p.b > 30) nonBlack++;
                }
                _magenta = mag; _magentaTone = tone;
                bool blank = nonBlack < px.Length / 10;   // blankness guard — the eye stays the last word
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex); Destroy(rt);
                _nEvid = $"evidence={(blank ? "FAIL(blank)" : "OK")}(framed by shared law)";
            }
            catch (Exception e) { _nEvid = "evidence=FAIL(" + e.Message + ")"; }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "saveload {0} {1} {2} {3} {4} {5} {6} {7} {8} magenta={9}/{10} {11}\n",
                N(_nSave), N(_nWipe), N(_nSha), N(_nWorld), N(_nMode), N(_nFeed), N(_nLive), N(_nEvid), N(_nEvidHud),
                _magenta, _magentaTone, error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }

        static string N(string s) => s.Length > 0 ? s : "NEVER-REACHED";
    }
}
