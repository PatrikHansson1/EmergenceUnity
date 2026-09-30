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

        public readonly List<Hunch> Journal = new List<Hunch>();
        public Hunch Pending { get; private set; }
        public int Right { get; private set; }
        public int Wrong { get; private set; }
        public int Passed { get; private set; }
        public int Asked => Journal.Count;
        public bool JournalOpen { get; private set; }
        public string LastNote { get; private set; } = "";

        Fas3WorldRuntime _world; Fas3PresentationClock _clock; Fas3AudioDirector _audio;
        int _lastYear = -1, _cooldownUntil = 0;
        GUIStyle _head, _q, _row;

        Fas3WorldRuntime World() { if (_world == null) _world = FindAnyObjectByType<Fas3WorldRuntime>(); return _world; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }
        void Click() { if (_audio == null) _audio = FindAnyObjectByType<Fas3AudioDirector>(); if (_audio != null) _audio.PlayUIClick(); }

        public void Answer(int yes)
        {
            if (Pending == null || Pending.answer >= 0) return;
            Pending.answer = yes; Pending.answeredAt = Time.unscaledTime;
            LastNote = "answered " + (yes == 1 ? "YES" : "NO") + " to \"" + Pending.question + "\"";
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
                Recount();
                _cooldownUntil = Mathf.Min(_cooldownUntil, year);
            }
            _lastYear = year;
            if (Fas3WorldRuntime.FixtureInjection) return;   // a probe's injected fixture is not witnessed time

            Resolve(S, year);
            if (Pending != null && Pending.answer < 0 && year >= Pending.askedYear + PassAfterYears) LetPassSilently();
            if (Pending == null && year >= _cooldownUntil && year >= 2 && !jump) Offer(S, w.PrevState, year);
        }

        void LetPassSilently() { Pending.resolved = true; Pending.resolution = "let pass"; Passed++; Pending = null; }

        void Recount()
        {
            Right = Wrong = Passed = 0;
            foreach (var h in Journal) { if (!h.resolved) continue; if (h.answer < 0) Passed++; else if (h.outcome == (h.answer == 1)) Right++; else Wrong++; }
        }

        // ---------------- the questions (pure over applied state) ----------------

        void Offer(WorldState S, WorldState prev, int year)
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
                    { cands.Add(new Hunch { kind = "life", subject = a.id.ToString(), target = a.id, dueYear = year + 40, question = $"Will {a.name}, born this year, live to see year {year + 40}?" }); break; }
            // the people — will they double?
            if (pop >= 6)
                cands.Add(new Hunch { kind = "people", target = pop * 2, dueYear = year + 30, question = $"Will the people number {pop * 2} by year {year + 30}? ({pop} today)" });
            // roofs — will they double?
            if (huts >= 2)
                cands.Add(new Hunch { kind = "roofs", target = huts * 2, dueYear = year + 20, question = $"Will {huts * 2} roofs stand by year {year + 20}? ({huts} today)" });
            // crafts — will the most learned village hold three more?
            if (S.villages != null)
            {
                WorldVillage best = null; foreach (var v in S.villages) if (!string.IsNullOrEmpty(v.name) && (best == null || v.crafts > best.crafts)) best = v;
                if (best != null && best.crafts >= 2)
                    cands.Add(new Hunch { kind = "crafts", subject = best.name, target = best.crafts + 3, dueYear = year + 20, question = $"Will {best.name} hold {best.crafts + 3} crafts by year {year + 20}? ({best.crafts} today)" });
                // a leaderless village — will a voice rise?
                foreach (var v in S.villages)
                    if (!string.IsNullOrEmpty(v.name) && string.IsNullOrEmpty(v.leader) && v.pop >= 8)
                    { cands.Add(new Hunch { kind = "leader", subject = v.name, dueYear = year + 15, question = $"Will anyone speak for all of {v.name} by year {year + 15}?" }); break; }
            }
            if (cands.Count == 0) return;
            var h = cands[(int)(Hash(S.seed, year, 926) % (uint)cands.Count)];
            h.askedYear = year;
            Pending = h; Journal.Add(h);
            _cooldownUntil = year + CooldownYears;
            LastNote = $"y{year} asked: {h.question}";
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
                if (GUI.Button(new Rect(left, markY - 24f, 250f, 18f), tally, EmergenceUI.Button)) ToggleJournal();   // its own row above SETTINGS
            }

            // the card
            var p = Pending;
            if (p != null && !JournalOpen)
            {
                bool answered = p.answer >= 0;
                float fade = answered ? Mathf.Clamp01(1f - (Time.unscaledTime - p.answeredAt - 2.5f) / 1.5f) : 1f;
                if (answered && fade <= 0f) { /* card gone; the journal keeps it */ }
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
                    GUI.Label(new Rect(x, y + 16f, w - EmergenceUI.Sp5 - 12f, 56f), answered ? $"You said {(p.answer == 1 ? "YES" : "NO")}. The world will answer by year {p.dueYear}." : p.question, _q);
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
            sb.AppendLine($"hunches asked={Asked} right={Right} wrong={Wrong} passed={Passed} pending={(Pending != null ? Pending.question : "-")}");
            foreach (var h in Journal) sb.AppendLine($"  y{h.askedYear} due {h.dueYear} [{h.kind}] {h.question} answer={h.answer} resolved={h.resolved} {h.resolution}");
            return sb.ToString();
        }
    }
}
