// EMERGENCE — FAS 4 (D-148): the y120 CHRONICLE ARTIFACT rides the PLAYER VEHICLE.
//
// D-146 measured the editor cost of deep years (~12+0.6·y s/year) and ruled the full EA window
// out of the editor; this observer runs INSIDE the built player instead (D-138 pattern). The same
// onboarding composition (bufferMode, seed 8919) races ▶▶ across the FULL EA window (y0 -> y120),
// the SAME Fas4ChronicleFeed witnesses the whole run live, and the chronicle is exported as the
// artifact pair beside the exe. Assertions mirror the editor probe's recalibrated spine (D-146):
//   1. span: y0 entries exist, last witnessed year >= targetYear-5;
//   2. v0 turning-point spine WITH NAMES: named first child, the first hut, named first death
//      (>= 3 ★ — v0 salience defines exactly these three classes; deeper spines arrive with the
//      engine's writeHistory/salience delivery);
//   3. order non-decreasing + capacity honesty (DroppedOldest == 0);
//   4. artifact pair written beside the exe, entry counts match the feed;
//   5. evidence: the BOOK at y120, end-of-frame, blankness-guarded (D-142 law).
// D-078 r4: observes and exports through presentation APIs only; the sim is never touched.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas4ArtifactPlayerProof : MonoBehaviour
    {
        public long seed = 8919;
        public int targetYear = 120;
        public float watchdogSecs = 9000f;   // MEASURED (first run, D-148): deep-year ticks cost more as population grows —
                                             // ~18 s/yr @y23 rising ~0.55 s/yr² -> full window ≈ 4800 s producer-bound; 9000 = margin.
                                             // (The D-136 "19 t/s" was y0-4, the cheapest ticks — same lesson as D-146 in-editor.)

        string Root     => Path.Combine(Application.dataPath, "..");
        string OutPath  => Path.Combine(Root, "artifact-player.txt");
        string ArtTxt   => Path.Combine(Root, "chronicle-8919-y120.txt");
        string ArtHtml  => Path.Combine(Root, "chronicle-8919-y120.html");
        string PngBook  => Path.Combine(Root, "artifact-player-book.png");
        string BeatPath => Path.Combine(Root, "artifact-player-beat.txt");

        Fas3Onboarding _onb;
        Fas4ChronicleFeed _feed;
        Fas4ChronicleView _view;
        int _phase;
        float _grabAskedAt, _lastBeat;
        string _n1 = "", _n2 = "", _n3 = "", _n4 = "", _n5 = "", _eras = "";

        void Update()
        {
            if (_phase == 9) return;
            if (Time.realtimeSinceStartup > watchdogSecs)
            {
                // capacity honesty even in defeat: export what WAS witnessed before quitting —
                // a watchdog cut must never silently discard the ledger (D-142 school)
                if (_phase == 1 && _feed != null) { RunChecks(); WriteArtifacts(); }
                Finish("WATCHDOG");
                return;
            }

            if (_phase == 0)
            {
                _onb = FindAnyObjectByType<Fas3Onboarding>();
                _feed = FindAnyObjectByType<Fas4ChronicleFeed>();
                _view = FindAnyObjectByType<Fas4ChronicleView>();
                if (_onb == null || _onb.Driver == null || _onb.Clock == null || _onb.Controls == null || _feed == null || _view == null) return;
                _onb.Controls.SetSpeed(3);   // ▶▶ — presentation takes what the producer gives
                _phase = 1;
                return;
            }

            var d = _onb.Driver; var w = _onb.World; var c = _onb.Clock;
            if (d.LastError.Length > 0) { Finish("driverError=" + d.LastError.Replace('\n', ' ')); return; }

            if (_phase == 1)   // the long witness: y0 -> y120 at full pace
            {
                float t = Time.realtimeSinceStartup;
                if (t - _lastBeat > 15f)
                {
                    _lastBeat = t;
                    try { File.WriteAllText(BeatPath, $"y={w.LastAppliedYear}/{targetYear} entries={_feed.Entries.Count} t={t:F0}s buffered={d.BufferedYears}\n"); } catch {}
                }
                if (w.LastAppliedYear < targetYear) return;
                c.paused = true;   // freeze at the window's edge; the book is now complete
                RunChecks();
                WriteArtifacts();
                _view.SetFilter(1); _view.OpenBook(); _view.RefreshNow();
                _grabAskedAt = Time.unscaledTime;
                StartCoroutine(GrabBook());
                _phase = 2;
                return;
            }

            if (_phase == 2)
            {
                if (_n5.Length == 0 && Time.unscaledTime - _grabAskedAt < 25f) return;
                if (_n5.Length == 0) _n5 = "evidence=FAIL(no grab within 25s)";
                Finish("");
            }
        }

        void RunChecks()
        {
            var E = _feed.Entries;

            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var e in E) { if (e.year < minY) minY = e.year; if (e.year > maxY) maxY = e.year; }
            bool spanOk = minY == 0 && maxY >= targetYear - 5 && E.Count > 20;
            _n1 = $"span={(spanOk ? "OK" : "FAIL")}(y{minY}..y{maxY},{E.Count}entries)";

            int stars = 0; bool namedBirth = false, firstHut = false, namedDeath = false;
            foreach (var e in E)
                if (e.salience >= 3)
                {
                    stars++;
                    if (e.kind == "birth" && e.text.Contains("—")) namedBirth = true;
                    if (e.kind == "milestone" && e.text.StartsWith("the first hut")) firstHut = true;
                    if (e.kind == "death" && e.text.Contains("—")) namedDeath = true;
                }
            bool spineOk = stars >= 3 && namedBirth && firstHut && namedDeath;
            _n2 = $"spine={(spineOk ? "OK" : "FAIL")}({stars}stars,child{(namedBirth ? "+" : "-")},hut{(firstHut ? "+" : "-")},death{(namedDeath ? "+" : "-")})";

            bool ordered = true; int last = int.MinValue;
            foreach (var e in E) { if (e.year < last) { ordered = false; break; } last = e.year; }
            bool capOk = _feed.DroppedOldest == 0;
            _n3 = $"order={(ordered && capOk ? "OK" : "FAIL")}(nondecreasing {(ordered ? "yes" : "NO")},dropped{_feed.DroppedOldest}/{Fas4ChronicleFeed.Capacity})";

            var eras = new System.Collections.Generic.List<string>();
            foreach (var e in E) if (!string.IsNullOrEmpty(e.era) && !eras.Contains(e.era)) eras.Add(e.era);
            _eras = "eras=" + eras.Count + "(" + string.Join("->", eras) + ")";
        }

        void WriteArtifacts()
        {
            var E = _feed.Entries;
            try
            {
                // ---- txt: the saga, chronological ----
                var t = new StringBuilder();
                t.AppendLine("KRÖNIKAN — skriven av ingen, allt hände");
                t.AppendLine($"seed {seed} · y0..y{targetYear} · {E.Count} witnessed entries · exported {DateTime.Now:yyyy-MM-dd HH:mm} · player vehicle (D-148)");
                t.AppendLine(new string('-', 72));
                foreach (var e in E)
                    t.AppendLine($"y{e.year,3} [{e.era}] {(e.salience >= 3 ? "*" : e.salience == 2 ? "." : " ")} {e.text}");
                File.WriteAllText(ArtTxt, t.ToString());

                // ---- html: standalone, almanac palette (mirrors the editor artifact, D-146) ----
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
                 .Append("<div class=\"sub\">skriven av ingen — allt hände · y0..y").Append(targetYear)
                 .Append(" · ").Append(E.Count).Append(" poster · vittnad live i spelarens egen kropp (player build)</div><div class=\"card\">");
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
                _n4 = $"artifact={(ok ? "OK" : "FAIL")}({E.Count}entries)";
            }
            catch (Exception e) { _n4 = "artifact=FAIL(" + e.Message + ")"; }
        }

        IEnumerator GrabBook()
        {
            yield return new WaitForEndOfFrame();   // UI Toolkit has drawn — the grab sees the book
            DoGrab();
        }

        void DoGrab()
        {
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null) { _n5 = "evidence=FAIL(nullgrab)"; return; }
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
                _n5 = $"evidence={(ok ? "OK" : "FAIL(blank)")}(book@y{targetYear},nonwhite{(nw * 100f).ToString("F0", CultureInfo.InvariantCulture)}%)";
            }
            catch (Exception e) { _n5 = "evidence=FAIL(" + e.Message + ")"; }
        }

        void Finish(string error)
        {
            _phase = 9;
            var sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "artifact {0} {1} {2} {3} {4} {5} {6}\n",
                _n1.Length > 0 ? _n1 : "span=NEVER", _n2.Length > 0 ? _n2 : "spine=NEVER",
                _n3.Length > 0 ? _n3 : "order=NEVER", _n4.Length > 0 ? _n4 : "artifact=NEVER",
                _n5.Length > 0 ? _n5 : "evidence=NEVER", _eras.Length > 0 ? _eras : "eras=NEVER",
                error.Length > 0 ? "ERROR=" + error : "COMPLETE"));
            try { File.WriteAllText(OutPath, sb.ToString()); } catch { }
            Application.Quit();
        }
    }
}
