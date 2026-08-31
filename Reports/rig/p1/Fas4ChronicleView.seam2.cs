// EMERGENCE — FAS 4 native CHRONICLE VIEW (per FAS4-NATIVE-CHRONICLE-VIEW-SKISS 2026-07-22):
// the story's first native face. UI Toolkit (UIDocument) replacing the IMGUI v0 panel, styled
// after the almanac reference (emergence-almanac.html — dark cards, gold year badges, the
// subtitle "skriven av ingen — allt hände").
//
// Two modes, one truth:
//   FEED — docked right edge during play: the latest witnessed entries, salience filter,
//          newest at the BOTTOM (the running feed reads downward, as in v0).
//   BOOK — fullscreen READ view: newest FIRST (the reference's order), year badges, the full
//          scrollable history, and the WHY-EXPANDER — no longer a stub: since 2026-08-13 the feed
//          carries the engine's own resolved causes[] (FAS4-PROSE-DIRECTOR-ORDER §1/§2), and an
//          expanded row asks Fas4ProseDirector to phrase them. With useProse OFF (the default) that
//          is the deterministic rule-based line and the answer is instant; with it ON the same line
//          comes from the local model, cached per entry, and the row shows "…" until it lands.
//          Opening the book PAUSES the presentation clock;
//          closing restores the prior pause state. The view touches the clock in NO other way.
//
// Data source: the SAME Fas4ChronicleFeed (no new truth). The view is a pure reader — it
// polls the feed's counters and rebuilds only when something changed. The filter state IS
// feed.minSalience (shared with the v0 panel semantics). No RNG, no state writes (D-078 r4).
//
// Styling lives in code (inline styles), not a USS asset: one moving part fewer in the player
// build (no catalog/Resources styling dependency), and the almanac palette is a dozen constants.
// PanelSettings + runtime theme are real assets (Assets/Emergence/Resources, built once by
// Fas4UIAssetsBuild) because a runtime UIDocument requires them; if they are missing the view
// disarms itself and the IMGUI v0 panel stays — the chronicle never goes dark.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Emergence.Fas4;   // Fas4ProseDirector — the why-line service (presentation-only, A5)

namespace Emergence.Runtime
{
    public sealed class Fas4ChronicleView : MonoBehaviour
    {
        public const int FeedMaxRows = 14;
        public const string PanelSettingsResource = "Fas4PanelSettings";

        // ---- almanac palette (emergence-almanac.html :root) ----
        // D-218 (Skärmbibeln §2 steg 2.1): THE PALETTE MOVED TO THE TOKEN LAYER.
        //
        // These constants were hand-picked here and hand-picked again, differently, in the other
        // view — 0x141B28 against 0x141A21, a COOL blue-white ink (0xE8EEF8) against the bone the
        // Screen Bible specifies (0xF0E7D6), 0xC9A227 gold against 0xD9A441. Nobody could point at
        // either and call it wrong; the screen looked amateur anyway. That is exactly the drift a
        // token layer exists to end, so the names stay and the VALUES now come from one place.
        static readonly Color ColPanel  = WithA(EmergenceUI.Surface1, EmergenceUI.PanelAlpha);
        static readonly Color ColPanel2 = EmergenceUI.Surface2;
        static readonly Color ColBookBg = WithA(EmergenceUI.Surface0, 0.97f);
        static readonly Color ColLine   = EmergenceUI.Hairline;
        static readonly Color ColInk    = EmergenceUI.Ink100;
        static readonly Color ColSub    = EmergenceUI.Ink70;
        static readonly Color ColGold   = EmergenceUI.Gold;
        static readonly Color ColRowLine= EmergenceUI.Hairline;
        static readonly Color ColRowTxt = EmergenceUI.Ink100;
        static Color WithA(Color c, float a) { c.a = a; return c; }

        /// <summary>Probe seam (Fas4 gate review 2026-07-22, villkor 2): a non-empty value replaces the
        /// PanelSettings resource name so the missing-assets DISARM branch can be proven without
        /// touching the real assets. Empty in production — the shipped path is unchanged.</summary>
        public string panelSettingsResourceOverride = "";

        public bool Ready { get; private set; }
        public bool BookOpen { get; private set; }
        public int FeedRowCount { get; private set; }
        public int BookRowCount => _bookRows.Count;
        public string LastError { get; private set; } = "";

        Fas4ChronicleFeed _feed;
        Fas3PresentationClock _clock;
        UIDocument _doc;
        VisualElement _feedPanel, _bookRoot;
        Label _feedHead;
        ScrollView _bookScroll;
        readonly List<Button> _filterButtons = new List<Button>();

        sealed class BookRow { public int year; public int salience; public string key; public Label stub; public string text; public string[] causes; public int voiceTier; }
        readonly List<BookRow> _bookRows = new List<BookRow>();
        readonly List<int> _feedRowSaliences = new List<int>();
        readonly HashSet<string> _expandedKeys = new HashSet<string>();   // survives rebuilds — the reader's open pages
        // The why-lines already answered, keyed by entry — survives rebuilds AND never re-asks the
        // model for a page the reader has already opened (the director caches too; this keeps the UI
        // honest across a rebuild, which drops the element the answer was written into).
        readonly Dictionary<string, string> _whyCache = new Dictionary<string, string>();
        sealed class PendingWhy { public string key; public System.Threading.Tasks.Task<string> task; }
        readonly List<PendingWhy> _pendingWhy = new List<PendingWhy>();
        Fas4ProseDirector _prose;
        // P1 (D-611): THE SPAN — the interval report from the presentation layer, shown at the top of the book.
        Label _spanLabel; Fas3WorldRuntime _rt;
        Fas3WorldRuntime Rt() { if (_rt == null) _rt = FindAnyObjectByType<Fas3WorldRuntime>(); return _rt; }
        void UpdateSpan()
        {
            if (_spanLabel == null) return;
            var rt = Rt(); var txt = (rt != null && rt.LastState != null) ? rt.LastState.intervalReport : null;
            if (string.IsNullOrEmpty(txt)) { _spanLabel.style.display = DisplayStyle.None; return; }
            _spanLabel.text = txt; _spanLabel.style.display = DisplayStyle.Flex;
        }
        /// <summary>Proof seam: why-lines answered from the director (rule-based or model).</summary>
        public int WhyResolvedCount { get; private set; }
        public string LastWhy { get; private set; } = "";
        const string WhyWaiting = "…";
        Fas4ProseDirector Prose() { if (_prose == null) _prose = FindAnyObjectByType<Fas4ProseDirector>(); return _prose; }

        bool _pausedBefore;
        long _lastSig = long.MinValue;

        Fas4ChronicleFeed Feed() { if (_feed == null) _feed = FindAnyObjectByType<Fas4ChronicleFeed>(); return _feed; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }

        void Start()
        {
            string resName = string.IsNullOrEmpty(panelSettingsResourceOverride) ? PanelSettingsResource : panelSettingsResourceOverride;
            var ps = Resources.Load<PanelSettings>(resName);
            if (ps == null)
            {
                LastError = "PanelSettings resource '" + resName + "' missing — run Emergence/Fas4/BUILD UI ASSETS; keeping IMGUI v0 panel";
                Debug.LogWarning("[Fas4ChronicleView] " + LastError);
                enabled = false;
                return;
            }
            _doc = gameObject.GetComponent<UIDocument>();
            if (_doc == null) _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = ps;
            BuildStatic();
            var f = Feed();
            if (f != null) f.showUI = false;   // the native face replaces the IMGUI panel
            Ready = true;
        }

        void Update()
        {
            if (!Ready) return;
            DrainPendingWhy();
            var f = Feed(); if (f == null) return;
            long sig = ((long)f.Entries.Count << 24) ^ ((long)f.TrimCount << 16) ^ ((long)f.DroppedOldest << 8)
                       ^ (long)f.minSalience ^ ((long)PresYear() << 32) ^ (BookOpen ? 1L << 60 : 0L);
            if (sig == _lastSig) return;
            _lastSig = sig;
            RefreshNow();
        }

        int PresYear() { var c = Clock(); return c != null ? c.PresentationYear : 0; }

        // ---------------- public surface (probe + future game UI) ----------------

        public void SetFilter(int minSalience)
        {
            var f = Feed(); if (f == null) return;
            f.minSalience = Mathf.Clamp(minSalience, 1, 3);
        }

        public void OpenBook()
        {
            if (BookOpen) return;
            var c = Clock();
            if (c != null) { _pausedBefore = c.paused; c.paused = true; }   // the ONLY clock touch
            BookOpen = true;
            RefreshNow();
        }

        public void CloseBook()
        {
            if (!BookOpen) return;
            var c = Clock();
            if (c != null) c.paused = _pausedBefore;
            BookOpen = false;
            RefreshNow();
        }

        public int BookRowYear(int i) => i >= 0 && i < _bookRows.Count ? _bookRows[i].year : -1;
        public int FeedRowSalience(int i) => i >= 0 && i < _feedRowSaliences.Count ? _feedRowSaliences[i] : -1;

        /// <summary>Why-expander: toggles the why-line under book row i; true if now visible. Opening a
        /// row for the first time asks Fas4ProseDirector for the line — instantly when prose is off
        /// (rule-based, no model, no await), otherwise "…" until the model answers (DrainPendingWhy).</summary>
        public bool ExpandBookRow(int i)
        {
            if (i < 0 || i >= _bookRows.Count) return false;
            var row = _bookRows[i];
            bool show = !_expandedKeys.Contains(row.key);
            if (show) _expandedKeys.Add(row.key); else _expandedKeys.Remove(row.key);
            row.stub.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show) AskWhy(row);
            return show;
        }

        /// <summary>Resolve one row's why-line. Cache first, then the director; never throws, never blocks.</summary>
        void AskWhy(BookRow row)
        {
            string cached;
            if (_whyCache.TryGetValue(row.key, out cached)) { row.stub.text = cached; return; }
            foreach (var p in _pendingWhy) if (p.key == row.key) { row.stub.text = WhyWaiting; return; }

            var d = Prose();
            if (d == null)
            {
                // no director in the scene: the same deterministic rule-based line, straight from the
                // service's static law — the reader is never shown an empty page.
                Answer(row.key, Fas4ProseDirector.RuleBasedWhy(row.text, row.causes, row.voiceTier));
                row.stub.text = _whyCache[row.key];
                return;
            }

            System.Threading.Tasks.Task<string> t;
            try { t = d.WhyProse(row.text, row.causes, row.voiceTier); }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Fas4ChronicleView] why: " + e.Message);
                Answer(row.key, Fas4ProseDirector.RuleBasedWhy(row.text, row.causes, row.voiceTier));
                row.stub.text = _whyCache[row.key]; return;
            }

            if (t == null) { Answer(row.key, Fas4ProseDirector.RuleBasedWhy(row.text, row.causes, row.voiceTier)); row.stub.text = _whyCache[row.key]; return; }
            if (t.IsCompleted) { Answer(row.key, Safe(t, row)); row.stub.text = _whyCache[row.key]; return; }
            row.stub.text = WhyWaiting;
            _pendingWhy.Add(new PendingWhy { key = row.key, task = t });
        }

        /// <summary>Main-thread drain of model answers — UI Toolkit is touched from Update only.</summary>
        void DrainPendingWhy()
        {
            for (int i = _pendingWhy.Count - 1; i >= 0; i--)
            {
                var p = _pendingWhy[i];
                if (p.task == null) { _pendingWhy.RemoveAt(i); continue; }
                if (!p.task.IsCompleted) continue;
                _pendingWhy.RemoveAt(i);
                BookRow row = null;
                foreach (var r in _bookRows) if (r.key == p.key) { row = r; break; }
                Answer(p.key, Safe(p.task, row));
                if (row != null) row.stub.text = _whyCache[p.key];
            }
        }

        string Safe(System.Threading.Tasks.Task<string> t, BookRow row)
        {
            try
            {
                if (t.IsFaulted || t.IsCanceled) throw new System.Exception(t.Exception != null ? t.Exception.Message : "cancelled");
                var r = t.Result;
                if (!string.IsNullOrEmpty(r)) return r;
            }
            catch (System.Exception e) { Debug.LogWarning("[Fas4ChronicleView] why: " + e.Message); }
            return Fas4ProseDirector.RuleBasedWhy(row != null ? row.text : "", row != null ? row.causes : null, row != null ? row.voiceTier : Fas4ProseDirector.TierWritten);
        }

        void Answer(string key, string line)
        {
            _whyCache[key] = line; LastWhy = line; WhyResolvedCount++;
        }

        /// <summary>Probe seam: the why-line standing under an entry key ("" if never opened).</summary>
        public string WhyFor(string key) { string v; return _whyCache.TryGetValue(key, out v) ? v : ""; }

        /// <summary>Store/probe seam (trailer round, slot 5): scrolls the BOOK so the first row with
        /// year &lt;= yearTarget sits at the top of the viewport. Rows are newest-first, so this finds
        /// the newest entry at-or-before that year. Pure presentation — touches no feed/clock state.
        /// Layout must exist: call at least one frame after RefreshNow(). Returns the row index or -1.</summary>
        public int ScrollBookToYear(int yearTarget)
        {
            for (int i = 0; i < _bookRows.Count; i++)
                if (_bookRows[i].year <= yearTarget)
                {
                    var el = _bookScroll.contentContainer[i];
                    _bookScroll.scrollOffset = new Vector2(0f, el.layout.y);
                    return i;
                }
            return -1;
        }

        /// <summary>Rebuild both surfaces from the feed NOW (probe-friendly: no frame-order races).</summary>
        public void RefreshNow()
        {
            var f = Feed(); if (f == null || !Ready) return;
            RebuildFeedRows(f);
            RebuildFilterButtons(f);
            _bookRoot.style.display = BookOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _feedPanel.style.display = BookOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (BookOpen) RebuildBookRows(f);
        }

        // ---------------- construction ----------------

        void BuildStatic()
        {
            var root = _doc.rootVisualElement;
            // D-221: the two faces. UI Toolkit inherits font down the tree, so setting it on the
            // root dresses every row, header and cell in one line. The Chronicle is TESTIMONY and
            // takes the serif; measurement surfaces override back to the sans where they need
            // tabular figures. A missing font falls back to Unity's built-in — it must cost the
            // voice, never the record.
            if (EmergenceUI.Serif != null)
                root.style.unityFontDefinition = UnityEngine.UIElements.FontDefinition.FromFont(EmergenceUI.Serif);

            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            // ---- FEED panel (docked right) ----
            _feedPanel = new VisualElement();
            Card(_feedPanel, ColPanel, ColLine, 12);
            _feedPanel.style.position = Position.Absolute;
            _feedPanel.style.right = 12; _feedPanel.style.top = 12;
            _feedPanel.style.width = 372;
            root.Add(_feedPanel);

            var head = new VisualElement(); RowFlex(head);
            _feedHead = new Label("THE CHRONICLE");
            _feedHead.style.color = ColInk; _feedHead.style.unityFontStyleAndWeight = FontStyle.Bold;
            _feedHead.style.fontSize = 13; _feedHead.style.letterSpacing = 1;
            head.Add(_feedHead);
            var spacer = new VisualElement(); spacer.style.flexGrow = 1; head.Add(spacer);
            var bookBtn = MakeButton("BOOK", () => OpenBook());
            head.Add(bookBtn);
            _feedPanel.Add(head);

            var filters = new VisualElement(); RowFlex(filters); filters.style.marginTop = 6;
            string[] labels = { "all", "notable", "turning points" };
            for (int i = 0; i < 3; i++)
            {
                int sal = i + 1;
                var b = MakeButton(labels[i], () => { SetFilter(sal); RefreshNow(); });
                b.style.flexGrow = 1;
                _filterButtons.Add(b);
                filters.Add(b);
            }
            _feedPanel.Add(filters);

            var feedRows = new VisualElement { name = "feed-rows" };
            feedRows.style.marginTop = 6;
            _feedPanel.Add(feedRows);

            // ---- BOOK (fullscreen READ) ----
            _bookRoot = new VisualElement();
            _bookRoot.style.position = Position.Absolute;
            _bookRoot.style.left = 0; _bookRoot.style.right = 0; _bookRoot.style.top = 0; _bookRoot.style.bottom = 0;
            _bookRoot.style.backgroundColor = ColBookBg;
            _bookRoot.style.alignItems = Align.Center;
            _bookRoot.style.justifyContent = Justify.Center;
            _bookRoot.style.display = DisplayStyle.None;
            root.Add(_bookRoot);

            var card = new VisualElement();
            Card(card, ColPanel, ColLine, 12);
            card.style.width = 720; card.style.maxHeight = Length.Percent(86);
            _bookRoot.Add(card);

            var bh = new VisualElement(); RowFlex(bh);
            var title = new Label("The Chronicle");
            title.style.color = ColInk; title.style.fontSize = 23; title.style.unityFontStyleAndWeight = FontStyle.Bold;
            bh.Add(title);
            var bsp = new VisualElement(); bsp.style.flexGrow = 1; bh.Add(bsp);
            bh.Add(MakeButton("✕  close", () => CloseBook()));
            card.Add(bh);

            // the game's central promise, and it must read in the language the game ships in
            var sub = new Label("written by no one — all of it happened");
            sub.style.color = ColSub; sub.style.fontSize = 13; sub.style.marginBottom = 8;
            card.Add(sub);

            // P1 (D-611): the span — what the last century kept. Read from WorldState.intervalReport; hidden when empty.
            _spanLabel = new Label("");
            _spanLabel.style.color = ColInk; _spanLabel.style.fontSize = 14; _spanLabel.style.whiteSpace = WhiteSpace.Normal;
            _spanLabel.style.marginBottom = 10; _spanLabel.style.paddingBottom = 8;
            _spanLabel.style.borderBottomWidth = 1; _spanLabel.style.borderBottomColor = ColLine;
            _spanLabel.style.display = DisplayStyle.None;
            card.Add(_spanLabel);

            var bookFilters = new VisualElement(); RowFlex(bookFilters); bookFilters.style.marginBottom = 6;
            for (int i = 0; i < 3; i++)
            {
                int sal = i + 1;
                var b = MakeButton(labels[i], () => { SetFilter(sal); RefreshNow(); });
                _filterButtons.Add(b);
                bookFilters.Add(b);
            }
            card.Add(bookFilters);

            _bookScroll = new ScrollView(ScrollViewMode.Vertical);
            _bookScroll.style.flexGrow = 1;
            card.Add(_bookScroll);
        }

        void RebuildFeedRows(Fas4ChronicleFeed f)
        {
            var rowsHost = _feedPanel.Q<VisualElement>("feed-rows");
            rowsHost.Clear();
            _feedRowSaliences.Clear();
            _feedHead.text = "THE CHRONICLE   yr " + PresYear() + "   (" + f.Entries.Count + " entries)";

            // newest at the bottom — the running feed reads downward (v0 semantics kept)
            var tail = new List<Fas4ChronicleFeed.Entry>();
            for (int i = f.Entries.Count - 1; i >= 0 && tail.Count < FeedMaxRows; i--)
                if (f.Entries[i].salience >= f.minSalience) tail.Add(f.Entries[i]);
            tail.Reverse();

            foreach (var e in tail)
            {
                var l = new Label("y" + e.year + "  " + Mark(e.salience) + " " + e.text);
                l.style.color = e.salience >= 3 ? ColInk : ColRowTxt;
                l.style.fontSize = 12; l.style.whiteSpace = WhiteSpace.Normal;
                l.style.marginBottom = 2;
                rowsHost.Add(l);
                _feedRowSaliences.Add(e.salience);
            }
            FeedRowCount = tail.Count;
        }

        void RebuildBookRows(Fas4ChronicleFeed f)
        {
            UpdateSpan(); // P1 (D-611)
            _bookScroll.Clear();
            _bookRows.Clear();

            // newest FIRST — the reference's reading order for the full book
            for (int i = f.Entries.Count - 1; i >= 0; i--)
            {
                var e = f.Entries[i];
                if (e.salience < f.minSalience) continue;

                var row = new VisualElement();
                row.style.borderTopWidth = _bookRows.Count > 0 ? 1 : 0;
                row.style.borderTopColor = ColRowLine;
                row.style.paddingTop = 7; row.style.paddingBottom = 7;

                var line = new VisualElement(); RowFlex(line);
                var y = new Label("yr " + e.year);
                y.style.color = ColGold; y.style.unityFontStyleAndWeight = FontStyle.Bold;
                y.style.fontSize = 13; y.style.width = 64; y.style.flexShrink = 0;
                line.Add(y);
                var t = new Label(Mark(e.salience) + " " + e.text);
                t.style.color = e.salience >= 3 ? ColInk : ColRowTxt;
                t.style.fontSize = 13; t.style.whiteSpace = WhiteSpace.Normal; t.style.flexGrow = 1;
                line.Add(t);
                row.Add(line);

                // why-expander — the engine's causes, phrased by the prose director (rule-based by
                // default). A page the reader already opened keeps its answered line across rebuilds.
                string why; if (!_whyCache.TryGetValue(e.key, out why)) why = WhyWaiting;
                var stub = new Label(why);
                stub.style.color = ColSub; stub.style.fontSize = 12;
                stub.style.marginLeft = 64; stub.style.marginTop = 3;
                stub.style.backgroundColor = ColPanel2;
                stub.style.paddingLeft = 8; stub.style.paddingRight = 8; stub.style.paddingTop = 4; stub.style.paddingBottom = 4;
                stub.style.display = _expandedKeys.Contains(e.key) ? DisplayStyle.Flex : DisplayStyle.None;
                row.Add(stub);

                int idx = _bookRows.Count;
                row.RegisterCallback<ClickEvent>(_ => ExpandBookRow(idx));
                _bookRows.Add(new BookRow { year = e.year, salience = e.salience, key = e.key, stub = stub, text = e.text, causes = e.causes, voiceTier = e.voiceTier });
                _bookScroll.Add(row);
            }
        }

        void RebuildFilterButtons(Fas4ChronicleFeed f)
        {
            string[] labels = { "all", "notable", "turning points" };
            for (int i = 0; i < _filterButtons.Count; i++)
            {
                int sal = (i % 3) + 1;
                bool on = f.minSalience == sal;
                var b = _filterButtons[i];
                b.text = labels[i % 3];
                b.style.backgroundColor = on ? ColPanel2 : ColPanel;
                b.style.color = on ? ColInk : ColSub;
                SetBorderColor(b, on ? ColGold : ColLine);
            }
        }

        // ---------------- style helpers ----------------

        static string Mark(int s) => s >= 3 ? "★" : s == 2 ? "•" : "·";

        static void RowFlex(VisualElement v)
        {
            v.style.flexDirection = FlexDirection.Row;
            v.style.alignItems = Align.Center;
        }

        static void Card(VisualElement v, Color bg, Color line, int radius)
        {
            v.style.backgroundColor = bg;
            SetBorderColor(v, line);
            v.style.borderTopWidth = 1; v.style.borderBottomWidth = 1; v.style.borderLeftWidth = 1; v.style.borderRightWidth = 1;
            v.style.borderTopLeftRadius = radius; v.style.borderTopRightRadius = radius;
            v.style.borderBottomLeftRadius = radius; v.style.borderBottomRightRadius = radius;
            v.style.paddingLeft = 14; v.style.paddingRight = 14; v.style.paddingTop = 10; v.style.paddingBottom = 10;
        }

        static void SetBorderColor(VisualElement v, Color c)
        {
            v.style.borderTopColor = c; v.style.borderBottomColor = c;
            v.style.borderLeftColor = c; v.style.borderRightColor = c;
        }

        Button MakeButton(string text, System.Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.backgroundColor = ColPanel;
            b.style.color = ColSub;
            SetBorderColor(b, ColLine);
            b.style.borderTopWidth = 1; b.style.borderBottomWidth = 1; b.style.borderLeftWidth = 1; b.style.borderRightWidth = 1;
            b.style.borderTopLeftRadius = 7; b.style.borderTopRightRadius = 7;
            b.style.borderBottomLeftRadius = 7; b.style.borderBottomRightRadius = 7;
            b.style.paddingLeft = 10; b.style.paddingRight = 10; b.style.paddingTop = 4; b.style.paddingBottom = 4;
            b.style.fontSize = 12;
            b.style.marginRight = 4;
            return b;
        }
    }
}
