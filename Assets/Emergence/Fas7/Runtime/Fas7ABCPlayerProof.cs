// EMERGENCE — FAS 7 increment 3: THE A/B/C SIMULTANEITY PROOF (technical half of the gate).
//
// The gate's wording (BUILD-PLAN-TO-EA, Fas 7): "de tre existensvillkoren A/B/C samtidigt bevisade
// i en enda ostörd genomspelning". This observer runs ONE undisturbed player session (D-138 school,
// the soak's composition: the observer composes the game's own opening and never touches time) and
// witnesses all three at once:
//
//   A — the opening's beats in order, no pause, no jump (D-140's checks WITHOUT the scrub half):
//       genesis lands (souls in wilderness, 0 huts) -> first child -> first hut, bus events + gaze,
//       years applied 0,1,2,... unbroken, pause/jump counters must stay ZERO through the window.
//   B — the chronicle artifact (txt + html, Fas4ArtifactPlayerProof pattern) exported from the SAME
//       run: ★-spine (named first child, the first hut, named first death) + the E1.5 drama entries
//       that fall out in the window. Declared window (node recon on the same 2.4.1 engine twin,
//       year-boundary sampling — exactly what the body witnesses): seed 8919 y0->y56 carries
//       leader y41 (Torv), leader-gone y43, leader y45 (Embla), leader-gone y53, gift y54 (Vidar),
//       first death y55 (Signe); submit y41/y45 stay body-only by the v0 law (declared, not hidden).
//   C — the SAME run's world diverges from a reference seed: a short reference producer (seed 4242,
//       flat-out to y8, headless — never applied, never witnessed) runs BEFORE the main witness in
//       the same player; at the same year the two checkpoint exports must differ in SHA and DNA.
//       The knowledge-loss/ruin half is DECLARED, not faked: no loss falls out on 8919 <= y80
//       (node recon), and structurally the live driver export carries no village knows/pop —
//       the codex ruin gates (requiresTech/minPop) can never fire in the live loop, so an
//       onLoss->toRuin event is unwitnessable in ANY live window today (engine-lane export order;
//       D-111/D-112 own the loss proof on checkpoint-built worlds). A witness hook still listens —
//       if a ruin/hut-loss DOES appear it is recorded, honesty over prediction.
//
// Undisturbed = no pause, no scrub, no JumpToYear during the window; the clock stays at the 1×
// opening pace (the producer wall owns the pace beyond it, soak lesson: deep years ~15-20 s/year).
// The window CLOSES at windowYears; only then does the proof pause to export the artifact and grab
// book/world evidence (post-window, declared).
// Evidence: (1) the first-hut beat as the GAZE holds it (the mechanism's own framing, D-140 school),
// (2) the end world through the SHARED framing law (crane shot, primary subject first — hut + 2
// nearest souls), (3) the open book. All magenta-scanned/blank-guarded; the human eye stays the
// last word (D-008). R1 law: every note is stamped at its measurement.
// Writes abc-player.txt (one line per condition + COMPLETE) + chronicle artifact pair beside the exe.
// D-078 r4: observes and exports through presentation APIs only; the sim is never touched.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas7ABCPlayerProof : MonoBehaviour
    {
        public long seed = 8919;
        public long refSeed = 4242;
        public int refYear = 8;            // divergence measured at this year in BOTH worlds
        public int windowYears = 56;       // declared window — see recon note above
        public int expectedGenesisSouls = -1;   // baked by the probe from the genesis export
        public float watchdogSecs = 2600f; // window + margin (soak: y0->40 = 714 s; deep years ~15-20 s/yr)
        public float softLockSecs = 120f;

        int _phase; int _waitFrames; float _waitAnchor;
        GameObject _refGo; Fas3SimDriver _refDriver;
        string _refSha = "", _refDna = "";
        Fas3Onboarding _onb;
        Fas3GazeDirector _gaze;
        Fas4ChronicleFeed _feed;
        Fas4ChronicleView _view;
        float _t0 = -1f;

        // A witnesses
        bool _hutBeat, _childBeat;
        int _hutYear = -1, _childYear = -1;
        float _hutGazeAt = -1f, _childGazeAt = -1f;
        string _nGenesis = "", _nChild = "", _nHut = "", _nBeats = "", _nUndist = "", _nSpan = "";
        int _lastYear = -1; float _lastYearAt;
        int _orderBreaks, _paceViolations, _pausedFrames, _jumpFrames;
        string _firstPauseNote = "";

        // B witnesses
        string _nBSpan = "", _nSpine = "", _nDrama = "", _nArtifact = "", _nBook = "";
        readonly List<string> _dramaSeen = new List<string>();   // "y41 leader:Torv" ... (bus, at witness time)
        int _submitSeen;                                          // body-only acts, declared not chronicled

        // C witnesses
        string _nDiverge = "", _nLoss = "";
        readonly List<string> _lossSeen = new List<string>();     // ruin/hut-loss events, if any fall out

        int _magenta = -1, _magentaTone = -1;
        string _nEvid = "";
        bool _subscribed;

        string Root => Path.Combine(Application.dataPath, "..");
        string OutPath => Path.Combine(Root, "abc-player.txt");
        string ArtTxt => Path.Combine(Root, $"chronicle-{seed}-y{windowYears:000}-abc.txt");
        string ArtHtml => Path.Combine(Root, $"chronicle-{seed}-y{windowYears:000}-abc.html");
        string PngHut => Path.Combine(Root, "abc-a-firsthut.png");
        string PngWorld => Path.Combine(Root, "abc-world.png");
        string PngBook => Path.Combine(Root, "abc-book.png");
        string BeatPath => Path.Combine(Root, "abc-beat.txt");
        string CheckpointDir => Path.Combine(Application.persistentDataPath, "Emergence", "checkpoints");

        void OnDestroy() { if (_subscribed) PresentationEventBus.OnEvent -= OnBus; }

        void OnBus(PresentationEvent e)
        {
            // A beats
            if (e.Type == PresentationEventType.Milestone && e.Data == "the first hut" && !_hutBeat) { _hutBeat = true; _hutYear = e.Year; }
            else if (e.Type == PresentationEventType.AgentActivity && e.Data == "a child is born" && !_childBeat) { _childBeat = true; _childYear = e.Year; }
            // B drama (E1.5) — recorded at witness time (R1)
            if (e.Type == PresentationEventType.AgentActivity && e.Data.StartsWith("sayAct: "))
            {
                string act = e.Data.Substring(8);
                if (act == "feud" || act == "raid" || act == "steal" || act == "mourn" || act == "gift")
                    _dramaSeen.Add($"y{e.Year} {act}");
                else if (act == "submit") _submitSeen++;   // body-only by the v0 law — declared below
            }
            else if (e.Type == PresentationEventType.Custom &&
                     (e.Data.StartsWith("leader: ") || e.Data.StartsWith("leader-gone: ") || e.Data.StartsWith("giftway: ")))
                _dramaSeen.Add($"y{e.Year} {e.Data.Replace(": ", ":")}");
            // C loss/ruin hook — listens honestly even though recon says none can fire live (see header)
            if (e.Type == PresentationEventType.AssetRemoved &&
                (e.Data.Contains("toRuin") || (e.Id != null && e.Id.StartsWith("hut:"))))
                _lossSeen.Add($"y{e.Year} {e.Id} {e.Data}");
        }

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs)
            {
                // capacity honesty in defeat (D-142 school): export what WAS witnessed before quitting
                if (_phase == 3 && _feed != null) { StampB(); WriteArtifacts(); }
                Finish($"WATCHDOG(phase{_phase},y{_lastYear})");
                return;
            }

            if (_phase == 0)   // fresh grid + the REFERENCE producer (headless, never witnessed)
            {
                Application.runInBackground = true;
                try
                {
                    if (Directory.Exists(CheckpointDir))
                        foreach (var f in Directory.GetFiles(CheckpointDir, "seq-*.json")) File.Delete(f);
                }
                catch (Exception e) { Finish("gridWipe=" + e.Message); return; }
                _refGo = new GameObject("Fas7ABCRefDriver");
                _refDriver = _refGo.AddComponent<Fas3SimDriver>();
                _refDriver.seed = refSeed; _refDriver.bufferMode = true; _refDriver.targetYear = refYear;
                _refDriver.lookaheadYears = refYear + 2;   // whole ref window fits — the producer never stalls
                _waitAnchor = Time.realtimeSinceStartup;
                _phase = 1;
                return;
            }

            if (_phase == 1)   // ref finished -> stamp its SHA + DNA, tear it down (worker waited out)
            {
                if (_refDriver.LastError.Length > 0) { Finish("refDriver=" + _refDriver.LastError.Replace('\n', ' ')); return; }
                if (!_refDriver.Finished) { if (Time.realtimeSinceStartup - _waitAnchor > 600f) Finish("refNeverFinished(600s)"); return; }
                if (_refSha.Length == 0)
                {
                    try
                    {
                        string chk = Path.Combine(CheckpointDir, $"seq-{refSeed}-y{refYear:000}.json");
                        string json = File.ReadAllText(chk);
                        _refSha = EmergenceJintHost.Sha256Hex(json);
                        _refDna = Fingerprint(json);
                        if (_refSha != _refDriver.FinalHash) { Finish("refShaMismatch(file!=FinalHash)"); return; }
                    }
                    catch (Exception e) { Finish("refSha=" + e.Message); return; }
                    _refDriver.StopWorker();
                    Destroy(_refGo);
                    _waitAnchor = Time.realtimeSinceStartup;
                }
                // NOTE: managed reference on purpose — after Destroy the Unity fake-null would skip
                // this wait; the C# object (and its thread flag) outlives the component (D-164 school).
                if ((object)_refDriver != null && _refDriver.WorkerAlive)
                { if (Time.realtimeSinceStartup - _waitAnchor > 45f) Finish("refWorkerNeverStopped(45s)"); return; }
                _phase = 2;
                return;
            }

            if (_phase == 2)   // boot the game's own opening — the ONE undisturbed session begins
            {
                PresentationEventBus.Clear();
                if (!_subscribed) { PresentationEventBus.OnEvent += OnBus; _subscribed = true; }
                var onb = new GameObject("Fas3Onboarding").AddComponent<Fas3Onboarding>();
                onb.seed = seed; onb.targetYear = -1;   // endless, as the game ships; the window is the observer's
                _onb = onb;
                _phase = 3;
                return;
            }

            if (_onb.Driver == null || _onb.Clock == null || _onb.World == null) return;
            var d = _onb.Driver; var c = _onb.Clock; var w = _onb.World;
            if (d.LastError.Length > 0) { Finish("driverError=" + d.LastError.Replace('\n', ' ')); return; }

            if (_phase == 3)   // THE WINDOW: witness A beats + drama + divergence, guard undisturbedness
            {
                if (_t0 < 0f)
                {
                    _t0 = Time.realtimeSinceStartup; _lastYearAt = _t0;
                    _gaze = Camera.main != null ? Camera.main.GetComponent<Fas3GazeDirector>() : null;
                    _feed = FindAnyObjectByType<Fas4ChronicleFeed>();
                    _view = FindAnyObjectByType<Fas4ChronicleView>();
                }
                float now = Time.realtimeSinceStartup;

                // undisturbedness ledger — these must all stay zero through the window
                if (c.paused)
                {
                    _pausedFrames++;
                    if (_firstPauseNote.Length == 0)
                        _firstPauseNote = $"firstPause@y{_lastYear},t{now - _t0:F0}s";   // diagnosis: WHO paused, WHEN
                }
                if (c.ApplyingJump) _jumpFrames++;
                if (c.PresentationYear > d.Year) _paceViolations++;

                int y = w.LastAppliedYear;
                if (y > _lastYear)
                {
                    if (y != _lastYear + 1 && !(_lastYear == -1 && y == 0)) _orderBreaks++;
                    _lastYear = y; _lastYearAt = now;
                    if (now - _t0 > 10f || y >= 1)
                        try { File.WriteAllText(BeatPath, $"y={y}/{windowYears} entries={(_feed != null ? _feed.Entries.Count : -1)} t={now - _t0:F0}s pausedFrames={_pausedFrames}{(_firstPauseNote.Length > 0 ? " " + _firstPauseNote : "")}\n"); } catch { }
                }
                else if (now - _lastYearAt > softLockSecs && !d.Finished)
                { Finish($"SOFTLOCK(no year for {(now - _lastYearAt):F0}s at y{_lastYear})"); return; }

                // A: genesis (stamped once, at first applied year — R1)
                if (_nGenesis.Length == 0 && w.LastAppliedYear >= 0)
                {
                    bool ok = w.LastAppliedYear == 0 && w.AgentCount == expectedGenesisSouls && w.HutCount == 0;
                    _nGenesis = $"genesis={(ok ? "OK" : "FAIL")}(y{w.LastAppliedYear},souls{w.AgentCount}/{expectedGenesisSouls},huts{w.HutCount})";
                }

                // A: first child — gaze taken (D-140 checks, honest cooldown fallback)
                if (_childBeat && _nChild.Length == 0)
                {
                    if (_gaze != null && _gaze.HasTarget && (_gaze.TargetLabel.Contains("born") || _gaze.TargetLabel.Contains("arrives")))
                    {
                        if (_childGazeAt < 0f) _childGazeAt = now + 1.2f;
                        else if (now >= _childGazeAt)
                        {
                            var cam = Camera.main;
                            float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                            _nChild = $"firstChild={(ang < 15f ? "OK" : "OK-gazeoff")}(y{_childYear},gaze{ang.ToString("F1", CultureInfo.InvariantCulture)}deg)";
                        }
                    }
                    else if (w.LastAppliedYear >= _childYear + 2)
                        _nChild = $"firstChild=OK(y{_childYear},beat; gaze within cooldown — design D-134)";
                }

                // A: first hut — gaze taken + the beat frame AS THE GAZE HOLDS IT (mechanism evidence)
                if (_hutBeat && _nHut.Length == 0)
                {
                    if (_gaze != null && _gaze.HasTarget && _gaze.TargetLabel.Contains("hut"))
                    {
                        if (_hutGazeAt < 0f) _hutGazeAt = now + 1.2f;
                        else if (now >= _hutGazeAt)
                        {
                            var cam = Camera.main;
                            float ang = cam != null ? Vector3.Angle(cam.transform.forward, (_gaze.Target + Vector3.up * 0.8f) - cam.transform.position) : 99f;
                            _nHut = $"firstHut={(ang < 15f ? "OK" : "FAIL")}(y{_hutYear},gaze{ang.ToString("F1", CultureInfo.InvariantCulture)}deg)";
                            CaptureFrame(PngHut);
                        }
                    }
                    else if (w.LastAppliedYear >= _hutYear + 2)
                        _nHut = $"firstHut=FAIL(y{_hutYear},gaze-never-took)";
                }

                // C: divergence — stamped the moment the main run's refYear checkpoint is witnessed (R1)
                if (_nDiverge.Length == 0 && w.LastAppliedYear >= refYear)
                {
                    try
                    {
                        string json = File.ReadAllText(Path.Combine(CheckpointDir, $"seq-{seed}-y{refYear:000}.json"));
                        string mainSha = EmergenceJintHost.Sha256Hex(json);
                        string mainDna = Fingerprint(json);
                        // The export's dna FIELD is unusable for comparison: ExportJs writes
                        // ''+E.computeDNA(S) and computeDNA returns an OBJECT -> every live checkpoint
                        // carries "[object Object]" (first ABC run's finding, declared to the engine-lane
                        // export order). The world FINGERPRINT (population, huts, the souls' own names)
                        // carries the same divergence truth readably; the SHA carries it exactly.
                        bool div = mainSha != _refSha && mainDna.Length > 0 && _refDna.Length > 0 && mainDna != _refDna;
                        _nDiverge = $"divergence={(div ? "OK" : "FAIL")}(y{refYear}: sha {mainSha.Substring(0, 8)}!={_refSha.Substring(0, 8)}; world[{mainDna}]!=[{_refDna}]; dna-field='[object Object]' stringify-quirk declared->engine-lane)";
                    }
                    catch (Exception e) { _nDiverge = "divergence=FAIL(" + e.Message.Replace('\n', ' ') + ")"; }
                }

                if (_lastYear >= windowYears)   // the window CLOSES — stamp A/C, pause (post-window), do B
                {
                    float secs = now - _t0;
                    _nSpan = $"span=OK(y0->y{_lastYear} at 1x/producer wall, {secs:F0}s — declared window; full 120y WAITS on deep-tick D-148)";
                    bool beatsOk = _childBeat && _hutBeat && _childYear > 0 && _childYear < _hutYear;
                    _nBeats = $"beatsOrder={(beatsOk ? "OK" : "FAIL")}(genesis y0 -> child y{_childYear} -> hut y{_hutYear})";
                    int scrubJumps = _onb.Controls != null ? _onb.Controls.ScrubJumps : -1;
                    bool undist = _pausedFrames == 0 && _jumpFrames == 0 && scrubJumps == 0 && _orderBreaks == 0 && _paceViolations == 0;
                    _nUndist = $"undisturbed={(undist ? "OK" : "FAIL")}(pausedFrames={_pausedFrames},jumpFrames={_jumpFrames},scrubJumps={scrubJumps},orderBreaks={_orderBreaks},paceViolations={_paceViolations}{(_firstPauseNote.Length > 0 ? "," + _firstPauseNote : "")})";
                    if (_nChild.Length == 0) _nChild = _childBeat ? $"firstChild=OK(y{_childYear},beat)" : "firstChild=FAIL(never)";
                    if (_nHut.Length == 0) _nHut = _hutBeat ? $"firstHut=FAIL(y{_hutYear},gaze unresolved)" : "firstHut=FAIL(never)";

                    // C loss half — witnessed if it fell out, DECLARED otherwise (see header: structurally
                    // unwitnessable live today; the divergence + D-111/D-112 checkpoint proofs carry C)
                    _nLoss = _lossSeen.Count > 0
                        ? $"loss=WITNESSED({_lossSeen.Count}: {string.Join("; ", _lossSeen)})"
                        : "loss=DECLARED-ABSENT(none falls out on 8919<=y80 by recon; live export carries no village knows/pop so codex ruin gates cannot fire — engine-lane order; loss half rests on divergence + D-111/D-112)";

                    c.paused = true;   // the window is complete — everything after this is export, not witness
                    StampB();
                    WriteArtifacts();

                    // end-world evidence through the SHARED law: crane on the primary subject
                    // (first hut + 2 nearest souls — the frameable-cluster lesson, D-164)
                    var S = w.LastState;
                    var subjects = new List<Vector3>();
                    if (S != null && S.huts.Length > 0)
                    {
                        var h0 = S.huts[0];
                        subjects.Add(Mapped(S, h0.x, h0.y));
                        var byDist = new List<WorldAgent>(S.agents);
                        byDist.Sort((a, b) => ((a.x - h0.x) * (a.x - h0.x) + (a.y - h0.y) * (a.y - h0.y))
                                    .CompareTo((b.x - h0.x) * (b.x - h0.x) + (b.y - h0.y) * (b.y - h0.y)));
                        for (int i = 0; i < byDist.Count && subjects.Count < 3; i++) subjects.Add(Mapped(S, byDist[i].x, byDist[i].y));
                    }
                    else if (S != null)
                        for (int i = 0; i < S.agents.Length && subjects.Count < 2; i++) subjects.Add(Mapped(S, S.agents[i].x, S.agents[i].y));
                    Vector3 pick = EvidenceFraming.FrameSubjects(out var lookAt, subjects.ToArray());
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        var g = mainCam.GetComponent<Fas3GazeDirector>();
                        if (g != null) g.enabled = false;   // post-window: the crane owns the frame now
                        mainCam.transform.position = pick; mainCam.transform.LookAt(lookAt);
                    }
                    _waitFrames = 0;
                    _phase = 4;
                }
                return;
            }

            if (_phase == 4)   // let the crane frame render, grab, then open the book
            {
                if (++_waitFrames < 5) return;
                CaptureFrame(PngWorld);
                if (_view != null) { _view.SetFilter(1); _view.OpenBook(); _view.RefreshNow(); }
                _waitAnchor = Time.realtimeSinceStartup;
                StartCoroutine(GrabBook());
                _phase = 5;
                return;
            }

            if (_phase == 5)
            {
                if (_nBook.Length == 0 && Time.realtimeSinceStartup - _waitAnchor < 25f) return;
                if (_nBook.Length == 0) _nBook = "book=FAIL(no grab within 25s)";
                Finish("");
            }
        }

        void StampB()
        {
            var E = _feed.Entries;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var e in E) { if (e.year < minY) minY = e.year; if (e.year > maxY) maxY = e.year; }
            bool spanOk = E.Count > 20 && minY == 0 && maxY >= windowYears - 2;
            _nBSpan = $"span={(spanOk ? "OK" : "FAIL")}(y{minY}..y{maxY},{E.Count}entries,dropped{_feed.DroppedOldest}/{Fas4ChronicleFeed.Capacity})";

            int stars = 0; bool namedBirth = false, firstHut = false, namedDeath = false; bool ordered = true; int last = int.MinValue;
            foreach (var e in E)
            {
                if (e.year < last) ordered = false; last = e.year;
                if (e.salience >= 3)
                {
                    stars++;
                    if (e.kind == "birth" && e.text.Contains("—")) namedBirth = true;
                    if (e.kind == "milestone" && e.text.StartsWith("the first hut")) firstHut = true;
                    if (e.kind == "death" && e.text.Contains("—")) namedDeath = true;
                }
            }
            bool spineOk = stars >= 3 && namedBirth && firstHut && namedDeath && ordered && _feed.DroppedOldest == 0;
            _nSpine = $"spine={(spineOk ? "OK" : "FAIL")}({stars}stars,child{(namedBirth ? "+" : "-")},hut{(firstHut ? "+" : "-")},death{(namedDeath ? "+" : "-")},ordered={(ordered ? "yes" : "NO")})";

            // drama presence: entries in the BOOK (leader/giftway/gift/steal/raid/feud/mourn kinds)
            int dramaEntries = 0; var kinds = new List<string>();
            foreach (var e in E)
                if (e.kind == "leader" || e.kind == "giftway" || e.kind == "gift" || e.kind == "steal"
                 || e.kind == "raid" || e.kind == "feud" || e.kind == "mourn")
                { dramaEntries++; kinds.Add($"y{e.year} {e.kind}"); }
            bool dramaOk = dramaEntries >= 1 && _dramaSeen.Count >= 1;
            _nDrama = $"drama={(dramaOk ? "OK" : "FAIL")}(bookEntries={dramaEntries}[{string.Join(",", kinds)}],busSeen={_dramaSeen.Count},submitBodyOnly={_submitSeen})";
        }

        void WriteArtifacts()
        {
            var E = _feed.Entries;
            try
            {
                int wMaxY = 0; foreach (var e in E) if (e.year > wMaxY) wMaxY = e.year;
                string spanTruth = _lastYear >= windowYears
                    ? $"y0..y{_lastYear} · COMPLETE"
                    : $"y0..y{_lastYear} of target y{windowYears} · WATCHDOG-CUT (partial)";

                var t = new StringBuilder();
                t.AppendLine("KRÖNIKAN — skriven av ingen, allt hände");
                t.AppendLine($"seed {seed} · {spanTruth} · {E.Count} witnessed entries · exported {DateTime.Now:yyyy-MM-dd HH:mm} · A/B/C-samtidighetsproben (Fas 7 ink. 3, player vehicle)");
                t.AppendLine(new string('-', 72));
                foreach (var e in E)
                    t.AppendLine($"y{e.year,3} [{e.era}] {(e.salience >= 3 ? "*" : e.salience == 2 ? "." : " ")} {e.text}");
                File.WriteAllText(ArtTxt, t.ToString());

                var h = new StringBuilder();
                h.Append("<!doctype html><meta charset=\"utf-8\"><title>Krönikan — seed ").Append(seed).Append("</title><style>")
                 .Append("body{margin:0;background:#0b0f17;color:#e8eef8;font:14px/1.5 -apple-system,Segoe UI,Roboto,sans-serif}")
                 .Append(".wrap{max-width:760px;margin:0 auto;padding:26px 16px}")
                 .Append("h1{margin:0;font-size:23px}h1 span{color:#c9a227}.sub{color:#8896b2;font-size:13px;margin-bottom:14px}")
                 .Append(".card{background:#141b28;border:1px solid #273047;border-radius:12px;padding:16px 18px}")
                 .Append(".e{padding:7px 0;border-top:1px solid #1e2740;font-size:13px;color:#cdd7ea;display:flex}")
                 .Append(".e:first-child{border-top:0}.y{color:#c9a227;font-weight:600;width:64px;flex-shrink:0}")
                 .Append(".star{color:#e8eef8;font-weight:600}</style><div class=\"wrap\">")
                 .Append("<h1>Krönikan <span>— seed ").Append(seed).Append("</span></h1>")
                 .Append("<div class=\"sub\">skriven av ingen — allt hände · ").Append(spanTruth)
                 .Append(" · ").Append(E.Count).Append(" poster · vittnad live i EN ostörd spelarkörning (A/B/C-proben)</div><div class=\"card\">");
                foreach (var e in E)
                {
                    string mark = e.salience >= 3 ? "★ " : e.salience == 2 ? "• " : "· ";
                    h.Append("<div class=\"e\"><div class=\"y\">år ").Append(e.year).Append("</div><div")
                     .Append(e.salience >= 3 ? " class=\"star\"" : "").Append(">").Append(mark)
                     .Append(System.Net.WebUtility.HtmlEncode(e.text)).Append("</div></div>");
                }
                h.Append("</div></div>");
                File.WriteAllText(ArtHtml, h.ToString());

                bool ok = File.Exists(ArtTxt) && File.Exists(ArtHtml) && E.Count > 0;
                _nArtifact = $"artifact={(ok ? "OK" : "FAIL")}({E.Count}entries,txt+html)";
            }
            catch (Exception e) { _nArtifact = "artifact=FAIL(" + e.Message + ")"; }
        }

        IEnumerator GrabBook()
        {
            yield return new WaitForEndOfFrame();   // UI Toolkit has drawn — the grab sees the book
            DoGrabBook();
        }

        void DoGrabBook()
        {
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) { _nBook = "book=FAIL(nullgrab)"; return; }
                var px = tex.GetPixels32();
                int nonWhite = 0, dark = 0;
                foreach (var p in px)
                {
                    if (!(p.r > 245 && p.g > 245 && p.b > 245)) nonWhite++;
                    if (p.r < 90 && p.g < 90 && p.b < 90) dark++;
                }
                float nw = nonWhite / (float)px.Length;
                bool ok = nw > 0.10f && dark > px.Length / 1000;   // D-142 blank-guard
                File.WriteAllBytes(PngBook, tex.EncodeToPNG());
                Destroy(tex);
                _nBook = $"book={(ok ? "OK" : "FAIL(blank)")}(open@y{_lastYear},nonwhite{(nw * 100f).ToString("F0", CultureInfo.InvariantCulture)}%)";
            }
            catch (Exception e) { _nBook = "book=FAIL(" + e.Message + ")"; }
        }

        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        /// <summary>Semantic world fingerprint of a checkpoint export — a human-readable divergence
        /// witness beside the exact SHA. Second ABC run's lesson: NAMES are the same across seeds
        /// (the founder pool is canonical: Eira/Ask/Embla/Torv/Liv/... in fixed order) and small
        /// counts coincide — but WHERE things stand diverges decisively (hut tiles, the souls'
        /// centroid), so the fingerprint is positional.</summary>
        static string Fingerprint(string json)
        {
            try
            {
                var S = JsonUtility.FromJson<WorldState>(json);
                if (S == null || S.agents == null) return "";
                float cx = 0f, cy = 0f;
                foreach (var a in S.agents) { cx += a.x; cy += a.y; }
                if (S.agents.Length > 0) { cx /= S.agents.Length; cy /= S.agents.Length; }
                string hut0 = S.huts != null && S.huts.Length > 0
                    ? string.Format(CultureInfo.InvariantCulture, "hut0({0:F1};{1:F1})", S.huts[0].x, S.huts[0].y)
                    : "hut0(none)";
                return string.Format(CultureInfo.InvariantCulture, "pop{0},huts{1},{2},souls@({3:F1};{4:F1})",
                    S.agents.Length, S.huts != null ? S.huts.Length : 0, hut0, cx, cy);
            }
            catch { return ""; }
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
                _magenta = Math.Max(_magenta, mag); _magentaTone = Math.Max(_magentaTone, tone);   // worst frame
                bool blank = nonBlack < px.Length / 10;
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex); Destroy(rt);
                _nEvid = $"evidence={(blank ? "FAIL(blank)" : "OK")}({Path.GetFileName(path)} latest)";
            }
            catch (Exception e) { _nEvid = "evidence=FAIL(" + e.Message + ")"; }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            sb.AppendLine("A " + N(_nGenesis) + " " + N(_nChild) + " " + N(_nHut) + " " + N(_nBeats) + " " + N(_nUndist) + " " + N(_nSpan));
            sb.AppendLine("B " + N(_nBSpan) + " " + N(_nSpine) + " " + N(_nDrama) + " " + N(_nArtifact) + " " + N(_nBook));
            sb.AppendLine("C " + N(_nDiverge) + " " + N(_nLoss));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "abc {0} magenta={1}/{2} {3}",
                N(_nEvid), _magenta, _magentaTone, error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }

        static string N(string s) => s.Length > 0 ? s : "NEVER-REACHED";
    }
}
