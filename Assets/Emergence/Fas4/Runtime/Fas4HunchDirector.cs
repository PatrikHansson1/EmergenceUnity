// EMERGENCE — ANING / THE HUNCH (D-926, design verb C10 from the deep review 2026-09-29).
//
// The player of a civilisation documentary has nothing to DO between first-times — watching four souls walk
// between hut and stone at 18 s per year is an aquarium (speldesign law 2). The cheapest turn from watching
// into EXPECTING is a hunch: at a moment the world offers, the game asks one question about its own future
// ("Will Torvvik still stand in year 37?"), the player answers YES or NO or lets it pass, and years later the
// world answers back — in the chronicle, as a notable line, right or wrong. A journal keeps the score.
//
// Law (D-078 r4): the questions are a PURE function of the applied state sequence — same world, same
// questions in the same years; the choice among candidates is a salted hash of seed and year, never
// sim RNG, never wall-clock. The answers are the player's input and touch nothing but this journal.
// Resolution reads the applied state at the due year. A backward scrub (D-140) forgets every hunch asked
// after the year it lands in — the chronicle does the same. Presentation-only; the engine never knows.
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas4HunchDirector : MonoBehaviour
    {
        public sealed class Hunch
        {
            public int askedYear, dueYear;
            public string kind, question, subject;   // subject: village name / agent id / number
            public int target;
            public int answer = -1;                   // -1 unanswered, 0 NO, 1 YES
            public bool resolved; public bool outcome; public string resolution = "";
            public float answeredAt = -1f;            // realtime, for the short "noted" fade
        }

        public const int CooldownYears = 6;   // at most one question per ~six years — expectation needs room
        public const int PassAfterYears = 4;  // an unanswered question lets itself pass
        // D-935: the first hour measured (D-930) has fifteen minutes (years 10-25) with ONE notable line. The producer
        // sets that pace, not us - but a question is the one thing the presentation can put into a silence. When the
        // chronicle has had no notable world line (salience >= 2, hunch verdicts excluded) for QuietYears, the next
        // question may come QuietCooldownYears after the last instead of CooldownYears. Pure over the feed + year.
        public const int QuietYears = 3;
        public const int QuietCooldownYears = 3;
        public int QuietAsks { get; private set; }   // proof: questions that came by the quiet rule

        public readonly List<Hunch> Journal = new List<Hunch>();
        public Hunch Pending { get; private set; }
        public int Right { get; private set; }
        public int Wrong { get; private set; }
        public int Passed { get; private set; }
        public int Asked => Journal.Count;
        public bool JournalOpen { get; private set; }
        public string LastNote { get; private set; } = "";

        Fas3WorldRuntime _world; Fas3PresentationClock _clock; Fas3AudioDirector _audio; Fas4ChronicleFeed _feed;
        int _lastYear = -1, _cooldownUntil = 0, _lastAskYear = -100;
        Hunch _noted;   // D-935b: the answered card, kept only for its short NOTED fade — the journal owns the question
        GUIStyle _head, _q, _row;

        Fas3WorldRuntime World() { if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>(); return _world; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }
        Fas4ChronicleFeed Feed() { if (_feed == null) _feed = FindAnyObjectByType<Fas4ChronicleFeed>(); return _feed; }

        /// <summary>Years since the chronicle's last notable WORLD line at or before this year (hunch verdicts do not count).</summary>
        public int QuietSince(int year)
        {
            var f = Feed(); if (f == null) return 0;
            int last = -1;
            foreach (var e in f.Entries) if (e.salience >= 2 && e.kind != "hunch" && e.year <= year && e.year > last) last = e.year;
            return last < 0 ? year : year - last;
        }
        void Click() { if (_audio == null) _audio = FindAnyObjectByType<Fas3AudioDirector>(); if (_audio != null) _audio.PlayUIClick(); }

        public void Answer(int yes)
        {
            if (Pending == null || Pending.answer >= 0) return;
            Pending.answer = yes; Pending.answeredAt = Time.unscaledTime;
            LastNote = "answered " + (yes == 1 ? "YES" : "NO") + " to \"" + Pending.question + "\"";
            // D-935b (measured in the soak): an answered question stayed Pending until its due year — up to 40 years —
            // and Pending blocks every new offer, so an answering player got ONE hunch and then silence. The journal
            // keeps the open question and resolves it; the card only lingers for its fade. Pending is free again.
            _noted = Pending; Pending = null;
            Click();
        }
        public void LetPass()
        {
            if (Pending == null) return;
            Pending.resolved = true; Pending.resolution = "let pass"; Passed++; Pending = null; Click();
        }
        public void ToggleJournal() { JournalOpen = !JournalOpen; Click(); }
        public void CloseJournal() { JournalOpen = false; }

        void Update()
        {
            var w = World(); if (w == null) return;
            var S = w.LastState; if (S == null) return;
            int year = w.LastAppliedYear;
            if (year == _lastYear) return;
            var c = Clock();
            bool jump = c != null && c.ApplyingJump;
            if (year < _lastYear)
            {
                // a scrub backwards: the timeline has not lived those years — forget what was asked in them
                Journal.RemoveAll(h => h.askedYear > year);
                if (Pending != null && Pending.askedYear > year) Pending = null;
                if (_noted != null && _noted.askedYear > year) _noted = null;
                Recount();
                _cooldownUntil = Mathf.Min(_cooldownUntil, year);
                if (_lastAskYear > year) _lastAskYear = -100;
            }
            _lastYear = year;
            if (Fas3WorldRuntime.FixtureInjection) return;   // a probe's injected fixture is not witnessed time

            Resolve(S, year);
            if (Pending != null && Pending.answer < 0 && year >= Pending.askedYear + PassAfterYears) LetPassSilently();
            if (Pending == null && year >= 2 && !jump)
            {
                bool quiet = year < _cooldownUntil && year >= _lastAskYear + QuietCooldownYears && QuietSince(year) >= QuietYears;
                if (year >= _cooldownUntil || quiet) Offer(S, w.PrevState, year, quiet);
            }
        }

        void LetPassSilently() { Pending.resolved = true; Pending.resolution = "let pass"; Passed++; Pending = null; }

        void Recount()
        {
            Right = Wrong = Passed = 0;
            foreach (var h in Journal) { if (!h.resolved) continue; if (h.answer < 0) Passed++; else if (h.outcome == (h.answer == 1)) Right++; else Wrong++; }
        }

        // ---------------- the questions (pure over applied state) ----------------

        void Offer(WorldState S, WorldState prev, int year, bool quiet)
        {
            var cands = new List<Hunch>();
            int pop = S.agents != null ? S.agents.Length : 0;
            int huts = S.huts != null ? S.huts.Length : 0;

            // a village founded this year — will it hold?
            if (S.villages != null && prev != null && prev.villages != null && S.villages.Length > prev.villages.Length)
                foreach (var v in S.villages)
                {
                    bool isNew = true; foreach (var pv in prev.villages) if (pv.name == v.name) { isNew = false; break; }
                    if (isNew && !string.IsNullOrEmpty(v.name))
                    { cands.Add(new Hunch { kind = "village", subject = v.name, dueYear = year + 25, question = $"Will {v.name} still stand in year {year + 25}?" }); break; }
                }
            // a newborn — will they see forty?
            if (S.agents != null)
                foreach (var a in S.agents)
                    if (a.age < 1f && a.gen >= 1 && !string.IsNullOrEmpty(a.name))
                    { cands.Add(new Hunch { kind = "life", subject = a.id.ToString(), target = a.id, dueYear = year + 40, question = $"{a.name} was born this year. Will they see year {year + 40}?" }); break; }
            // D-935d: an elder - will they see ten more years? The oldest soul who has no open question. The soak's silence
            // (years 14-25) had every kind open; a life at its far end is a different question from a life at its start.
            if (S.agents != null)
            {
                WorldAgent old = null;
                foreach (var a in S.agents)
                    if (a.age > 55f && !string.IsNullOrEmpty(a.name) && (old == null || a.age > old.age))
                    {
                        bool open = false; foreach (var j in Journal) if (!j.resolved && j.kind == "elder" && j.target == a.id) { open = true; break; }
                        if (!open) old = a;
                    }
                if (old != null)
                    cands.Add(new Hunch { kind = "elder", subject = old.name, target = old.id, dueYear = year + 10, question = $"{old.name} is {Mathf.RoundToInt(old.age)}. Will they see year {year + 10}?" });
            }
            // the people — will they double?
            if (pop >= 6)
                cands.Add(new Hunch { kind = "people", target = pop * 2, dueYear = year + 30, question = $"They are {pop}. Will they be {pop * 2} by year {year + 30}?" });
            // roofs — will they double?
            if (huts >= 2)
                cands.Add(new Hunch { kind = "roofs", target = huts * 2, dueYear = year + 20, question = $"{huts} roofs stand. Will there be {huts * 2} by year {year + 20}?" });
            // crafts — will the most learned village hold three more?
            if (S.villages != null)
            {
                WorldVillage best = null; foreach (var v in S.villages) if (!string.IsNullOrEmpty(v.name) && (best == null || v.crafts > best.crafts)) best = v;
                if (best != null && best.crafts >= 2)
                    cands.Add(new Hunch { kind = "crafts", subject = best.name, target = best.crafts + 3, dueYear = year + 20, question = $"{best.name} holds {best.crafts} crafts. Three more by year {year + 20}?" });
                // a leaderless village — will a voice rise?
                foreach (var v in S.villages)
                    if (!string.IsNullOrEmpty(v.name) && string.IsNullOrEmpty(v.leader) && v.pop >= 8)
                    { cands.Add(new Hunch { kind = "leader", subject = v.name, dueYear = year + 15, question = $"Will anyone speak for all of {v.name} by year {year + 15}?" }); break; }
            }
            // D-935c (SEEN in the soak journal): with the card freed, the same question came twice while the first was still
            // open ("3 roofs stand. Will there be 6" in y13 and y16). One open question per kind - variety, not an echo.
            // ...except a life: two different children are two different questions (MEASURED, soak 5: kind-only dedupe left
            // years 14-25 without any question at all, because every open kind was still open and no village was named yet).
            cands.RemoveAll(c => { foreach (var j in Journal) if (!j.resolved && j.kind == c.kind && ((c.kind != "life" && c.kind != "elder") || j.subject == c.subject)) return true; return false; });
            if (cands.Count == 0) return;
            var h = cands[(int)(Hash(S.seed, year, 926) % (uint)cands.Count)];
            h.askedYear = year;
            Pending = h; Journal.Add(h);
            _cooldownUntil = year + CooldownYears; _lastAskYear = year;
            if (quiet) QuietAsks++;
            LastNote = $"y{year} asked{(quiet ? " (quiet stretch, " + QuietSince(year) + " y without a notable line)" : "")}: {h.question}";
        }

        void Resolve(WorldState S, int year)
        {
            foreach (var h in Journal)
            {
                if (h.resolved || year < h.dueYear) continue;
                bool o = Evaluate(h, S);
                h.resolved = true; h.outcome = o;
                string fact = Fact(h, S, o);
                if (h.answer < 0) { h.resolution = "no answer — " + fact; Passed++; }
                else
                {
                    bool right = o == (h.answer == 1);
                    if (right) Right++; else Wrong++;
                    h.resolution = (right ? "right — " : "wrong — ") + fact;
                }
                if (Pending == h) Pending = null;
                string line = h.answer < 0 ? "the question answered itself: " + fact
                            : (h.outcome == (h.answer == 1) ? "your hunch held: " : "your hunch failed: ") + fact;
                PresentationEventBus.Publish(new PresentationEvent(S.tick, year, S.eraName, PresentationEventType.Custom, "hunch:" + h.kind, -1, "hunch: " + line));
            }
        }

        static bool Evaluate(Hunch h, WorldState S)
        {
            switch (h.kind)
            {
                case "village": if (S.villages != null) foreach (var v in S.villages) if (v.name == h.subject) return true; return false;
                case "life":    if (S.agents != null) foreach (var a in S.agents) if (a.id == h.target) return true; return false;
                case "elder":   if (S.agents != null) foreach (var a in S.agents) if (a.id == h.target) return true; return false;
                case "people":  return S.agents != null && S.agents.Length >= h.target;
                case "roofs":   return S.huts != null && S.huts.Length >= h.target;
                case "crafts":  if (S.villages != null) foreach (var v in S.villages) if (v.name == h.subject) return v.crafts >= h.target; return false;
                case "leader":  if (S.villages != null) foreach (var v in S.villages) if (v.name == h.subject) return !string.IsNullOrEmpty(v.leader); return false;
            }
            return false;
        }

        static string Fact(Hunch h, WorldState S, bool o)
        {
            switch (h.kind)
            {
                case "village": return o ? $"{h.subject} stands in year {S.years}" : $"{h.subject} is gone by year {S.years}";
                case "life":    return o ? $"the child of year {h.askedYear} lives to see year {S.years}" : $"the child of year {h.askedYear} did not see year {S.years}";
                case "elder":   return o ? $"{h.subject} lives to see year {S.years}" : $"{h.subject} did not see year {S.years}";
                case "people":  return $"the people number {(S.agents != null ? S.agents.Length : 0)} in year {S.years} (asked: {h.target})";
                case "roofs":   return $"{(S.huts != null ? S.huts.Length : 0)} roofs stand in year {S.years} (asked: {h.target})";
                case "crafts":  { int c = 0; if (S.villages != null) foreach (var v in S.villages) if (v.name == h.subject) c = v.crafts; return $"{h.subject} holds {c} crafts in year {S.years} (asked: {h.target})"; }
                case "leader":  return o ? $"a voice speaks for all of {h.subject} by year {S.years}" : $"{h.subject} is still without a voice in year {S.years}";
            }
            return "";
        }

        static uint Hash(int x, int y, int salt) { unchecked { uint h = (uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791); h ^= h >> 13; h *= 2246822519; h ^= h >> 16; return h; } }

        // ---------------- the screen ----------------

        void OnGUI()
        {
            EmergenceUI.Begin();
            float W = EmergenceUI.W, H = EmergenceUI.H;
            if (_head == null)
            {
                _head = new GUIStyle(EmergenceUI.Meta);
                _q = new GUIStyle(EmergenceUI.Prose) { wordWrap = true, fontSize = 15 };
                _row = new GUIStyle(EmergenceUI.Meta) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            }
            float left = EmergenceUI.Sp6;
            float markY = H - EmergenceUI.Sp6 - 18f;   // the SETTINGS mark's row (EmergenceSettings)

            // the tally mark, beside SETTINGS
            if (Asked > 0 && !JournalOpen)
            {
                string tally = $"HUNCHES  {Right} right · {Wrong} wrong" + (Passed > 0 ? $" · {Passed} passed" : "");
                if (GUI.Button(new Rect(left, markY - 24f, 320f, 18f), tally, EmergenceUI.Button)) ToggleJournal();   // its own row above SETTINGS
            }

            // the card (the open question, or the just-answered one while its NOTED fade lasts)
            var p = Pending ?? _noted;
            if (p != null && !JournalOpen)
            {
                bool answered = p.answer >= 0;
                float fade = answered ? Mathf.Clamp01(1f - (Time.unscaledTime - p.answeredAt - 2.5f) / 1.5f) : 1f;
                if (answered && fade <= 0f) { _noted = null; /* card gone; the journal keeps it */ }
                else
                {
                    const float w = 360f, h = 118f;
                    var r = new Rect(left, markY - 24f - 10f - h, w, h);
                    var pc = GUI.color; GUI.color = new Color(1, 1, 1, fade);
                    var body = new Rect(r.x, r.y, r.width - 12f, r.height - 12f);
                    var fill = EmergenceUI.Surface1; fill.a = EmergenceUI.PanelAlpha * fade;
                    var c0 = GUI.color; GUI.color = fill; GUI.DrawTexture(body, Texture2D.whiteTexture); GUI.color = c0;
                    EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width, r.height - 12f), fill, fill.a, true, true, 12);
                    EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width - 12f, r.height), fill, fill.a, false, true, 12);
                    EmergenceUI.Bracket(body, EmergenceUI.Corner.TopLeft, EmergenceUI.Hairline);
                    EmergenceUI.Bracket(body, EmergenceUI.Corner.BottomRight, EmergenceUI.Hairline);
                    EmergenceUI.Bracket(new Rect(body.x + 3, body.y + 3, body.width, body.height), EmergenceUI.Corner.TopLeft, EmergenceUI.GoldLeaf);
                    float x = r.x + EmergenceUI.Sp4, y = r.y + EmergenceUI.Sp2;
                    GUI.Label(new Rect(x, y, 200, 16), answered ? "NOTED" : "A HUNCH  ·  year " + p.askedYear, _head);
                    GUI.Label(new Rect(x, y + 16f, w - EmergenceUI.Sp5 - 12f, 56f), answered ? $"You said {(p.answer == 1 ? "YES" : "NO")}. The world answers in year {p.dueYear}." : p.question, _q);
                    if (!answered)
                    {
                        float by = r.y + h - 12f - 26f;
                        if (GUI.Button(new Rect(x, by, 64, 20), "YES", EmergenceUI.ButtonOn)) Answer(1);
                        if (GUI.Button(new Rect(x + 72, by, 64, 20), "NO", EmergenceUI.ButtonOn)) Answer(0);
                        if (GUI.Button(new Rect(x + 160, by, 110, 20), "let it pass", EmergenceUI.Button)) LetPass();
                    }
                    GUI.color = pc;
                }
            }

            // the journal
            if (JournalOpen)
            {
                EmergenceUI.Wash(new Rect(0, 0, W, H), EmergenceUI.Surface0, 0.55f);
                const float w = 560f, h = 400f;
                var r = new Rect((W - w) * 0.5f, (H - h) * 0.5f, w, h);
                var body = new Rect(r.x, r.y, r.width - 12f, r.height - 12f);
                var fill = EmergenceUI.Surface1; fill.a = EmergenceUI.PanelAlpha;
                var pc = GUI.color; GUI.color = fill; GUI.DrawTexture(body, Texture2D.whiteTexture); GUI.color = pc;
                EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width, r.height - 12f), fill, fill.a, true, true, 12);
                EmergenceUI.FadeEdge(new Rect(r.x, r.y, r.width - 12f, r.height), fill, fill.a, false, true, 12);
                EmergenceUI.LayTooth(new Rect(r.x, r.y, r.width, r.height), EmergenceUI.ToothBody);
                EmergenceUI.Bracket(body, EmergenceUI.Corner.TopLeft, EmergenceUI.Hairline);
                EmergenceUI.Bracket(body, EmergenceUI.Corner.BottomRight, EmergenceUI.Hairline);
                EmergenceUI.Bracket(new Rect(body.x + 3, body.y + 3, body.width, body.height), EmergenceUI.Corner.TopLeft, EmergenceUI.GoldLeaf);
                EmergenceUI.PrickColumn(r.x + EmergenceUI.PrickInset, r.y + EmergenceUI.Sp5, body.height - EmergenceUI.Sp7, EmergenceUI.Hairline, 8);
                float x = r.x + EmergenceUI.Sp5 + 8f, y = r.y + EmergenceUI.Sp4, cw = w - EmergenceUI.Sp5 * 2f - 20f;
                GUI.Label(new Rect(x, y, cw, 30), "THE JOURNAL OF HUNCHES", new GUIStyle(EmergenceUI.Display) { fontSize = 22 });
                y += 30f;
                GUI.Label(new Rect(x, y, cw, 18), $"{Right} right · {Wrong} wrong · {Passed} passed", EmergenceUI.Dim);
                y += 20f; EmergenceUI.RuleH(x, y, cw, EmergenceUI.Hairline); y += EmergenceUI.Sp2;
                int shown = 0;
                for (int i = Journal.Count - 1; i >= 0 && shown < 9; i--, shown++)
                {
                    var hh = Journal[i];
                    string status = !hh.resolved ? (hh.answer < 0 ? "open" : "you said " + (hh.answer == 1 ? "YES" : "NO") + " — due " + hh.dueYear)
                                                 : hh.resolution;
                    GUI.Label(new Rect(x, y, 60, 16), "yr " + hh.askedYear, EmergenceUI.Meta);
                    GUI.Label(new Rect(x + 56, y, cw - 56, 16), hh.question, EmergenceUI.Label);
                    GUI.Label(new Rect(x + 56, y + 15, cw - 56, 16), status, hh.resolved && hh.answer >= 0 ? (hh.outcome == (hh.answer == 1) ? new GUIStyle(EmergenceUI.Meta) { normal = { textColor = EmergenceUI.Gold } } : EmergenceUI.Dim) : EmergenceUI.Dim);
                    y += 34f;
                }
                if (Journal.Count == 0) GUI.Label(new Rect(x, y, cw, 20), "No hunch yet — the world will ask when it has something to ask.", EmergenceUI.Dim);
                if (GUI.Button(new Rect(x + cw - 96, r.y + h - 12f - 30f, 96, 22), "CLOSE", EmergenceUI.ButtonOn)) { CloseJournal(); Click(); }
            }
            EmergenceUI.End();
        }

        /// <summary>Probe seam: the journal as text.</summary>
        public string Dump()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"hunches asked={Asked} quietAsks={QuietAsks} right={Right} wrong={Wrong} passed={Passed} pending={(Pending != null ? Pending.question : "-")}");
            foreach (var h in Journal) sb.AppendLine($"  y{h.askedYear} due {h.dueYear} [{h.kind}] {h.question} answer={h.answer} resolved={h.resolved} {h.resolution}");
            return sb.ToString();
        }
    }
}
