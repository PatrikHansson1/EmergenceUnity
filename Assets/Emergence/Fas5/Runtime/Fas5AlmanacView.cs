// EMERGENCE — FAS 5 (per FAS5-KICKOFF-BRIEF 2026-07-22): the ALMANAC's native face — ANALYZE
// begins to show its patterns. Same UI Toolkit school as Fas4ChronicleView (D-145): almanac
// palette in code, PanelSettings REUSED from the Fas4 build (one resource, one seam), and the
// same honest pause law — opening the almanac pauses the clock, closing restores the prior mode,
// tps is never touched. If the UI assets are missing the view disarms itself (villkor-2 school:
// the probe proves that branch through panelSettingsResourceOverride).
//
// v0 (D-151) = the Overview tab (B2): headline tiles + population curve + era strip.
// v1 (this file) = the TAB SKELETON against the almanac reference's seven tabs, with:
//   - VILLAGES live: rows + dossier straight from the applied snapshot's villages[] (name/pop/
//     maxGen/avgAge/crafts/cosmos/knows/beliefs — the state has carried these since TD-033);
//   - SOULS base: the 30 OLDEST souls from agents[] (name/task/age/gen) — the reference sorts by
//     wealth, but wealth/roles/traits are engine metrics (R2 order), so the base sort is age;
//   - SOCIETY / TECH & MEMORY / DYNASTY as honest stubs — they NAME what they await (R2);
//   - CHRONICLE = a handoff to the Fas 4 book (link, never rebuild — the brief's law).
// DETERMINISM (D-078 r4): a pure READ of Fas5MetricsRecorder + the applied WorldState — never
// writes anything back. Scrub honesty is inherited by construction: villages/souls render the
// PRESENTED snapshot, and the recorder's series obey the trim law (D-144).
// PROBE SEAM: SetStateFixture(WorldState) feeds a real engine snapshot through the SAME rebuild
// path so the populated-villages branch can be proven without an 8-minute live sim (fixture-proven
// mechanism, D-131 school). Production never calls it.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Emergence.Runtime
{
    public sealed class Fas5AlmanacView : MonoBehaviour
    {
        // ---- almanac palette (emergence-almanac.html :root — same constants as the chronicle view) ----
        static readonly Color ColPanel  = new Color32(0x14, 0x1B, 0x28, 0xF0);
        static readonly Color ColPanel2 = new Color32(0x1A, 0x22, 0x33, 0xFF);
        static readonly Color ColBookBg = new Color32(0x0B, 0x0F, 0x17, 0xF7);
        static readonly Color ColLine   = new Color32(0x27, 0x30, 0x47, 0xFF);
        static readonly Color ColInk    = new Color32(0xE8, 0xEE, 0xF8, 0xFF);
        static readonly Color ColSub    = new Color32(0x88, 0x96, 0xB2, 0xFF);
        static readonly Color ColGold   = new Color32(0xC9, 0xA2, 0x27, 0xFF);

        /// <summary>Probe seam (villkor-2 school): non-empty replaces the PanelSettings resource name
        /// so the missing-assets DISARM branch can be proven. Empty in production.</summary>
        public string panelSettingsResourceOverride = "";

        public bool Ready { get; private set; }
        public bool AlmanacOpen { get; private set; }
        public string LastError { get; private set; } = "";

        // ---- tabs (reference order, emergence-almanac.html nav) ----
        public const int TabOverview = 0, TabSociety = 1, TabTech = 2, TabVillages = 3, TabSouls = 4, TabDynasty = 5, TabChronicle = 6;
        public static readonly string[] TabNames = { "Översikt", "Samhälle", "Teknik & minne", "Byar", "Själar", "Dynastier & tid", "Krönika" };
        public static bool TabIsStub(int t) => t == TabSociety || t == TabTech || t == TabDynasty;
        public int ActiveTab { get; private set; }

        // probe-readable rendered truth (set at rebuild, straight from the labels' sources)
        public int TileYear { get; private set; }
        public string TileEra { get; private set; } = "";
        public int TilePop { get; private set; }
        public int TileBirths { get; private set; }
        public int TileDeaths { get; private set; }
        public int TileHuts { get; private set; }
        public int CurvePointCount { get; private set; }
        public int CurveLastYear { get; private set; }

        // villages truth
        public int VillageRowCount { get; private set; }
        public string VillageRowName(int i) => _sortedVillages != null && i >= 0 && i < _sortedVillages.Length ? _sortedVillages[i].name : "";
        public int VillageRowPop(int i) => _sortedVillages != null && i >= 0 && i < _sortedVillages.Length ? _sortedVillages[i].pop : -1;
        public string VillageDossierName { get; private set; } = "";
        public int VillageDossierPop { get; private set; }
        public int VillageDossierGen { get; private set; }
        public int VillageDossierCrafts { get; private set; }
        public int VillageDossierKnows { get; private set; }

        // souls truth
        public const int SoulRowCap = 30;   // the reference's "30 mest förmögna"; base = 30 oldest (wealth awaits R2)
        public int SoulRowCount { get; private set; }
        public string SoulRowName(int i) => _sortedSouls != null && i >= 0 && i < _sortedSouls.Length ? _sortedSouls[i].name : "";
        public string SoulDossierName { get; private set; } = "";
        public int SoulDossierAge { get; private set; }
        public int SoulDossierGen { get; private set; }
        public string SoulDossierTask { get; private set; } = "";

        Fas5MetricsRecorder _rec;
        Fas3PresentationClock _clock;
        Fas3WorldRuntime _worldRt;
        UIDocument _doc;
        VisualElement _openBtnPanel, _root, _tilesRow, _eraStrip, _curveHost;
        VisualElement _tabsBar, _villagesHost, _soulsHost;
        readonly VisualElement[] _tabBodies = new VisualElement[7];
        readonly List<Button> _tabButtons = new List<Button>();
        Label _sub;
        readonly List<Label> _tileValues = new List<Label>();
        Fas5MetricsRecorder.YearRecord[] _curve = new Fas5MetricsRecorder.YearRecord[0];

        WorldState _fixture;                 // probe seam — production never sets this
        WorldVillage[] _sortedVillages;
        WorldAgent[] _sortedSouls;
        int _villageDossier = -1, _soulDossier = -1;

        bool _pausedBefore;
        long _lastSig = long.MinValue;

        Fas5MetricsRecorder Rec() { if (_rec == null) _rec = FindAnyObjectByType<Fas5MetricsRecorder>(); return _rec; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }
        Fas3WorldRuntime WorldRt() { if (_worldRt == null) _worldRt = FindAnyObjectByType<Fas3WorldRuntime>(); return _worldRt; }

        void Start()
        {
            string resName = string.IsNullOrEmpty(panelSettingsResourceOverride) ? Fas4ChronicleView.PanelSettingsResource : panelSettingsResourceOverride;
            var ps = Resources.Load<PanelSettings>(resName);
            if (ps == null)
            {
                LastError = "PanelSettings resource '" + resName + "' missing — run Emergence/Fas4/BUILD UI ASSETS; almanac stays dark, WATCH/READ unaffected";
                Debug.LogWarning("[Fas5AlmanacView] " + LastError);
                enabled = false;
                return;
            }
            _doc = gameObject.GetComponent<UIDocument>();
            if (_doc == null) _doc = gameObject.AddComponent<UIDocument>();
            _doc.panelSettings = ps;
            BuildStatic();
            Ready = true;
        }

        void Update()
        {
            if (!Ready) return;
            var r = Rec(); if (r == null) return;
            long sig = ((long)r.RecordCount << 20) ^ ((long)r.TrimCount << 12) ^ ((long)PresYear() << 32) ^ (AlmanacOpen ? 1L << 60 : 0L);
            if (sig == _lastSig) return;
            _lastSig = sig;
            RefreshNow();
        }

        int PresYear() { var c = Clock(); return c != null ? c.PresentationYear : 0; }

        WorldState PresentedState()
        {
            if (_fixture != null) return _fixture;
            var w = WorldRt(); return w != null ? w.LastState : null;
        }

        /// <summary>Honest time label for the state-rendering tabs (review I4): when a fixture is
        /// applied the header must carry the FIXTURE's year with a visible marker — never the
        /// presentation clock's year next to fixture data.</summary>
        string StateWhen() => _fixture != null ? "FIXTUR y" + _fixture.years + " (riktig motor-export)" : "år " + TileYear;

        // ---------------- public surface (probe + future game UI) ----------------

        public void OpenAlmanac()
        {
            if (AlmanacOpen) return;
            var c = Clock();
            if (c != null) { _pausedBefore = c.paused; c.paused = true; }   // the ONLY clock touch (D-145 law)
            AlmanacOpen = true;
            RefreshNow();
        }

        public void CloseAlmanac()
        {
            if (!AlmanacOpen) return;
            var c = Clock();
            if (c != null) c.paused = _pausedBefore;
            AlmanacOpen = false;
            RefreshNow();
        }

        /// <summary>Select a tab by index (TabChronicle hands off to the Fas 4 book instead).</summary>
        public void SelectTab(int t)
        {
            if (t < 0 || t >= TabNames.Length) return;
            if (t == TabChronicle) { GoToChronicle(); return; }
            ActiveTab = t;
            _villageDossier = -1; _soulDossier = -1;
            RefreshNow();
        }

        /// <summary>The Chronicle tab IS the Fas 4 book (brief's law: link, never rebuild). Closing the
        /// almanac restores the prior pause mode; the book then applies its own identical pause law —
        /// the chain stays honest. Returns false when no chronicle view exists (nothing is touched).</summary>
        public bool GoToChronicle()
        {
            var chron = FindAnyObjectByType<Fas4ChronicleView>();
            if (chron == null || !chron.enabled) return false;
            CloseAlmanac();
            chron.OpenBook();
            return true;
        }

        /// <summary>Probe seam (D-131 fixture school): feed a REAL engine snapshot through the same
        /// rebuild path so populated villages/souls branches are provable without an 8-minute live
        /// sim. Read-only — the fixture is rendered, never written. Pass null to return to runtime state.</summary>
        public void SetStateFixture(WorldState s)
        {
            _fixture = s;
            _villageDossier = -1; _soulDossier = -1;
            RefreshNow();
        }

        public void OpenVillageDossier(int row) { _villageDossier = row; RefreshNow(); }
        public void CloseVillageDossier() { _villageDossier = -1; RefreshNow(); }
        public void OpenSoulDossier(int row) { _soulDossier = row; RefreshNow(); }
        public void CloseSoulDossier() { _soulDossier = -1; RefreshNow(); }

        /// <summary>Rebuild the surface from the recorder NOW (probe-friendly: no frame-order races).</summary>
        public void RefreshNow()
        {
            var r = Rec(); if (r == null || !Ready) return;
            _root.style.display = AlmanacOpen ? DisplayStyle.Flex : DisplayStyle.None;
            _openBtnPanel.style.display = AlmanacOpen ? DisplayStyle.None : DisplayStyle.Flex;
            if (!AlmanacOpen) return;

            _curve = r.Series();
            var last = r.Latest();
            TileYear = PresYear();
            TileEra = WorldEras.Name(last.era);
            TilePop = last.pop;
            TileBirths = r.TotalBirths;
            TileDeaths = r.TotalDeaths;
            TileHuts = r.HutCount;
            CurvePointCount = _curve.Length;
            CurveLastYear = r.LatestYear;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                _tabButtons[i].style.color = i == ActiveTab ? ColGold : ColSub;
                _tabButtons[i].style.unityFontStyleAndWeight = i == ActiveTab ? FontStyle.Bold : FontStyle.Normal;
            }
            for (int i = 0; i < _tabBodies.Length; i++)
                if (_tabBodies[i] != null) _tabBodies[i].style.display = i == ActiveTab ? DisplayStyle.Flex : DisplayStyle.None;

            var S = PresentedState();
            switch (ActiveTab)
            {
                case TabOverview:
                    _sub.text = "år " + TileYear + " · " + TileEra + " · statistik med kausalitet — korrelationerna väntar på motorns metrics (R2)";
                    string[] vals = { TilePop.ToString(), TileBirths.ToString(), TileDeaths.ToString(), TileHuts.ToString(), TileEra };
                    for (int i = 0; i < _tileValues.Count && i < vals.Length; i++) _tileValues[i].text = vals[i];
                    RebuildEraStrip();
                    _curveHost.MarkDirtyRepaint();
                    break;
                case TabVillages:
                    _sub.text = StateWhen() + " · byarna som världen själv har grundat — klicka för dossier";
                    RebuildVillages(S);
                    break;
                case TabSouls:
                    _sub.text = StateWhen() + " · de " + SoulRowCap + " äldsta själarna — roller · rikedom · egenskaper väntar på motorns metrics (R2)";
                    RebuildSouls(S);
                    break;
                default:
                    _sub.text = "år " + TileYear + " · " + TileEra;
                    break;
            }
        }

        // ---------------- construction ----------------

        void BuildStatic()
        {
            var root = _doc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 0; root.style.bottom = 0;
            root.pickingMode = PickingMode.Ignore;

            // ---- docked opener (below the chronicle feed panel's corner) ----
            _openBtnPanel = new VisualElement();
            _openBtnPanel.style.position = Position.Absolute;
            _openBtnPanel.style.right = 12; _openBtnPanel.style.bottom = 12;
            var open = MakeButton("ALMANACKEN", OpenAlmanac);
            _openBtnPanel.Add(open);
            root.Add(_openBtnPanel);

            // ---- fullscreen ANALYZE ----
            _root = new VisualElement();
            _root.style.position = Position.Absolute;
            _root.style.left = 0; _root.style.right = 0; _root.style.top = 0; _root.style.bottom = 0;
            _root.style.backgroundColor = ColBookBg;
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;
            _root.style.display = DisplayStyle.None;
            root.Add(_root);

            var card = new VisualElement();
            Card(card, ColPanel, ColLine, 12);
            card.style.width = 840; card.style.maxHeight = Length.Percent(88);
            _root.Add(card);

            var bh = new VisualElement(); RowFlex(bh);
            var title = new Label("Almanacken");
            title.style.color = ColInk; title.style.fontSize = 23; title.style.unityFontStyleAndWeight = FontStyle.Bold;
            bh.Add(title);
            var bsp = new VisualElement(); bsp.style.flexGrow = 1; bh.Add(bsp);
            bh.Add(MakeButton("✕  stäng", CloseAlmanac));
            card.Add(bh);

            _sub = new Label("");
            _sub.style.color = ColSub; _sub.style.fontSize = 13; _sub.style.marginBottom = 8;
            card.Add(_sub);

            // ---- tab bar (reference nav) ----
            _tabsBar = new VisualElement(); RowFlex(_tabsBar); _tabsBar.style.marginBottom = 10; _tabsBar.style.flexWrap = Wrap.Wrap;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var b = MakeButton(TabNames[i], () => SelectTab(idx));
                b.style.marginRight = 4; b.style.marginBottom = 2;
                _tabButtons.Add(b);
                _tabsBar.Add(b);
            }
            card.Add(_tabsBar);

            // ---- tab bodies ----
            // Overview
            var ov = new VisualElement();
            _tabBodies[TabOverview] = ov;
            card.Add(ov);

            _tilesRow = new VisualElement(); RowFlex(_tilesRow); _tilesRow.style.marginBottom = 12;
            string[] heads = { "BEFOLKNING", "FÖDDA", "DÖDA", "HYDDOR", "ERA" };
            for (int i = 0; i < heads.Length; i++)
            {
                var tile = new VisualElement();
                Card(tile, ColPanel2, ColLine, 10);
                tile.style.flexGrow = 1;
                tile.style.marginRight = i < heads.Length - 1 ? 8 : 0;
                var h = new Label(heads[i]);
                h.style.color = ColSub; h.style.fontSize = 10; h.style.letterSpacing = 1;
                tile.Add(h);
                var v = new Label("—");
                v.style.color = i == heads.Length - 1 ? ColGold : ColInk;
                v.style.fontSize = 20; v.style.unityFontStyleAndWeight = FontStyle.Bold;
                tile.Add(v);
                _tileValues.Add(v);
                _tilesRow.Add(tile);
            }
            ov.Add(_tilesRow);

            var curveHead = new Label("BEFOLKNING ÖVER TID");
            curveHead.style.color = ColSub; curveHead.style.fontSize = 10; curveHead.style.letterSpacing = 1;
            curveHead.style.marginBottom = 4;
            ov.Add(curveHead);

            _curveHost = new VisualElement();
            Card(_curveHost, ColPanel2, ColLine, 10);
            _curveHost.style.height = 220;
            _curveHost.generateVisualContent += PaintCurve;
            ov.Add(_curveHost);

            _eraStrip = new VisualElement(); RowFlex(_eraStrip);
            _eraStrip.style.height = 22; _eraStrip.style.marginTop = 6;
            ov.Add(_eraStrip);

            // Villages / Souls — scrollable lists rebuilt on refresh
            _tabBodies[TabVillages] = MakeScrollBody(card, out _villagesHost);
            _tabBodies[TabSouls] = MakeScrollBody(card, out _soulsHost);

            // Honest stubs — they NAME what they await (never fake data)
            _tabBodies[TabSociety] = MakeStub(card, "Samhälle väntar på motorns metrics-export (R2): Gini · konflikt · handel · tro.");
            _tabBodies[TabTech] = MakeStub(card, "Teknik & minne väntar på motorns metrics-export (R2): tech lost/rediscovered · Minnesmotorns kurvor.");
            _tabBodies[TabDynasty] = MakeStub(card, "Dynastier & tid väntar på motorns metrics-export (R2): generationsträd · tidslinjens skalor.");
            // Chronicle body never shows — SelectTab hands off to the Fas 4 book.
            _tabBodies[TabChronicle] = MakeStub(card, "");

            for (int i = 0; i < _tabBodies.Length; i++)
                if (_tabBodies[i] != null) _tabBodies[i].style.display = i == TabOverview ? DisplayStyle.Flex : DisplayStyle.None;
        }

        VisualElement MakeScrollBody(VisualElement card, out VisualElement host)
        {
            var body = new VisualElement();
            var sv = new ScrollView(ScrollViewMode.Vertical);
            sv.style.maxHeight = 420;
            host = sv.contentContainer;
            body.Add(sv);
            card.Add(body);
            return body;
        }

        VisualElement MakeStub(VisualElement card, string text)
        {
            var body = new VisualElement();
            if (text.Length > 0)
            {
                var box = new VisualElement();
                Card(box, ColPanel2, ColLine, 10);
                var l = new Label(text);
                l.style.color = ColSub; l.style.fontSize = 13; l.style.whiteSpace = WhiteSpace.Normal;
                box.Add(l);
                body.Add(box);
            }
            card.Add(body);
            return body;
        }

        // ---------------- villages ----------------

        void RebuildVillages(WorldState S)
        {
            _villagesHost.Clear();
            var src = S != null && S.villages != null ? S.villages : new WorldVillage[0];
            _sortedVillages = (WorldVillage[])src.Clone();
            System.Array.Sort(_sortedVillages, (a, b) => b.pop != a.pop ? b.pop.CompareTo(a.pop) : string.CompareOrdinal(a.name ?? "", b.name ?? ""));
            VillageRowCount = _sortedVillages.Length;

            if (_villageDossier >= VillageRowCount) _villageDossier = -1;
            VillageDossierName = ""; VillageDossierPop = 0; VillageDossierGen = 0; VillageDossierCrafts = 0; VillageDossierKnows = 0;

            if (VillageRowCount == 0)
            {
                var empty = new Label("inga byar ännu — själarna bor ännu spridda; världen grundar sina byar själv");
                empty.style.color = ColSub; empty.style.fontSize = 13; empty.style.whiteSpace = WhiteSpace.Normal;
                _villagesHost.Add(empty);
                return;
            }

            if (_villageDossier >= 0)
            {
                var v = _sortedVillages[_villageDossier];
                VillageDossierName = v.name ?? "";
                VillageDossierPop = v.pop; VillageDossierGen = v.maxGen; VillageDossierCrafts = v.crafts;
                VillageDossierKnows = v.knows != null ? v.knows.Length : 0;
                _villagesHost.Add(BuildVillageDossier(v));
            }

            for (int i = 0; i < _sortedVillages.Length; i++)
            {
                var v = _sortedVillages[i];
                bool law = v.beliefs != null && System.Array.IndexOf(v.beliefs, "harm") >= 0;
                string meta = v.pop + " själar · " + v.crafts + " hantverk · gen " + v.maxGen + (string.IsNullOrEmpty(v.cosmos) ? "" : " · 🌌 " + v.cosmos);
                _villagesHost.Add(MakeClickRow((v.name ?? "?") + (law ? " ⚖️" : ""), meta, i, OpenVillageDossier));
            }
        }

        VisualElement BuildVillageDossier(WorldVillage v)
        {
            var d = new VisualElement();
            Card(d, ColPanel2, ColGold, 10);
            d.style.marginBottom = 8;

            var head = new VisualElement(); RowFlex(head);
            var nm = new Label(v.name ?? "?");
            nm.style.color = ColInk; nm.style.fontSize = 16; nm.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.Add(nm);
            var sp = new VisualElement(); sp.style.flexGrow = 1; head.Add(sp);
            head.Add(MakeButton("✕", CloseVillageDossier));
            d.Add(head);

            bool law = v.beliefs != null && System.Array.IndexOf(v.beliefs, "harm") >= 0;
            AddKv(d, "Befolkning", v.pop + " själar");
            AddKv(d, "Generationer", v.maxGen.ToString());
            AddKv(d, "Medelålder", v.avgAge + " år");
            AddKv(d, "Hantverk", v.crafts.ToString());
            if (!string.IsNullOrEmpty(v.cosmos)) AddKv(d, "Himmelstro", "🌌 " + v.cosmos);
            if (law) AddKv(d, "Lag", "⚖️ Peace of Kin");

            var kh = new Label("KAN (HANTVERK & KUNSKAP)");
            kh.style.color = ColSub; kh.style.fontSize = 10; kh.style.letterSpacing = 1; kh.style.marginTop = 8; kh.style.marginBottom = 4;
            d.Add(kh);
            var chips = new VisualElement(); RowFlex(chips); chips.style.flexWrap = Wrap.Wrap;
            if (v.knows != null)
                foreach (var k in v.knows) chips.Add(MakeChip(k));
            d.Add(chips);
            return d;
        }

        // ---------------- souls ----------------

        void RebuildSouls(WorldState S)
        {
            _soulsHost.Clear();
            var src = S != null && S.agents != null ? S.agents : new WorldAgent[0];
            var all = (WorldAgent[])src.Clone();
            System.Array.Sort(all, (a, b) => b.age != a.age ? b.age.CompareTo(a.age) : a.id.CompareTo(b.id));
            int n = Mathf.Min(SoulRowCap, all.Length);
            _sortedSouls = new WorldAgent[n];
            System.Array.Copy(all, _sortedSouls, n);
            SoulRowCount = n;

            if (_soulDossier >= SoulRowCount) _soulDossier = -1;
            SoulDossierName = ""; SoulDossierAge = 0; SoulDossierGen = 0; SoulDossierTask = "";

            if (SoulRowCount == 0)
            {
                var empty = new Label("inga levande själar i det presenterade året");
                empty.style.color = ColSub; empty.style.fontSize = 13;
                _soulsHost.Add(empty);
                return;
            }

            if (_soulDossier >= 0)
            {
                var s = _sortedSouls[_soulDossier];
                SoulDossierName = s.name ?? "";
                SoulDossierAge = Mathf.RoundToInt(s.age); SoulDossierGen = s.gen; SoulDossierTask = s.task ?? "";
                _soulsHost.Add(BuildSoulDossier(s));
            }

            for (int i = 0; i < _sortedSouls.Length; i++)
            {
                var s = _sortedSouls[i];
                string meta = (s.task ?? "—") + " · " + Mathf.RoundToInt(s.age) + " år · gen " + s.gen;
                _soulsHost.Add(MakeClickRow(s.name ?? ("själ " + s.id), meta, i, OpenSoulDossier));
            }
        }

        VisualElement BuildSoulDossier(WorldAgent s)
        {
            var d = new VisualElement();
            Card(d, ColPanel2, ColGold, 10);
            d.style.marginBottom = 8;

            var head = new VisualElement(); RowFlex(head);
            var nm = new Label(s.name ?? "?");
            nm.style.color = ColInk; nm.style.fontSize = 16; nm.style.unityFontStyleAndWeight = FontStyle.Bold;
            head.Add(nm);
            var sp = new VisualElement(); sp.style.flexGrow = 1; head.Add(sp);
            head.Add(MakeButton("✕", CloseSoulDossier));
            d.Add(head);

            AddKv(d, "Ålder", Mathf.RoundToInt(s.age) + " år");
            AddKv(d, "Generation", s.gen.ToString());
            AddKv(d, "Syssla", s.task ?? "—");
            var note = new Label("egenskaper · band · rikedom · dråp väntar på motorns metrics-export (R2)");
            note.style.color = ColSub; note.style.fontSize = 11; note.style.marginTop = 8; note.style.whiteSpace = WhiteSpace.Normal;
            d.Add(note);
            return d;
        }

        // ---------------- shared row/chip/kv helpers ----------------

        VisualElement MakeClickRow(string name, string meta, int index, System.Action<int> onClick)
        {
            var row = new VisualElement(); RowFlex(row);
            Card(row, ColPanel2, ColLine, 8);
            row.style.marginBottom = 4;
            var left = new VisualElement();
            var nm = new Label(name);
            nm.style.color = ColInk; nm.style.fontSize = 14; nm.style.unityFontStyleAndWeight = FontStyle.Bold;
            left.Add(nm);
            var mt = new Label(meta);
            mt.style.color = ColSub; mt.style.fontSize = 11;
            left.Add(mt);
            row.Add(left);
            var sp = new VisualElement(); sp.style.flexGrow = 1; row.Add(sp);
            var arrow = new Label("›");
            arrow.style.color = ColSub; arrow.style.fontSize = 16;
            row.Add(arrow);
            row.RegisterCallback<ClickEvent>(_ => onClick(index));
            return row;
        }

        VisualElement MakeChip(string text)
        {
            var c = new Label(text);
            c.style.color = ColSub; c.style.fontSize = 11;
            c.style.backgroundColor = ColPanel;
            SetBorderColor(c, ColLine);
            c.style.borderTopWidth = 1; c.style.borderBottomWidth = 1; c.style.borderLeftWidth = 1; c.style.borderRightWidth = 1;
            c.style.borderTopLeftRadius = 9; c.style.borderTopRightRadius = 9;
            c.style.borderBottomLeftRadius = 9; c.style.borderBottomRightRadius = 9;
            c.style.paddingLeft = 8; c.style.paddingRight = 8; c.style.paddingTop = 2; c.style.paddingBottom = 2;
            c.style.marginRight = 4; c.style.marginBottom = 4;
            return c;
        }

        void AddKv(VisualElement parent, string k, string v)
        {
            var row = new VisualElement(); RowFlex(row);
            row.style.marginBottom = 2;
            var kl = new Label(k);
            kl.style.color = ColSub; kl.style.fontSize = 12; kl.style.width = 120;
            row.Add(kl);
            var vl = new Label(v);
            vl.style.color = ColInk; vl.style.fontSize = 12;
            row.Add(vl);
            parent.Add(row);
        }

        // ---------------- overview painters (unchanged from v0) ----------------

        void PaintCurve(MeshGenerationContext ctx)
        {
            if (_curve.Length == 0) return;
            var rect = ctx.visualElement.contentRect;
            if (rect.width < 10f || rect.height < 10f) return;

            int maxPop = 1, maxYear = 1;
            foreach (var p in _curve) { if (p.pop > maxPop) maxPop = p.pop; if (p.year > maxYear) maxYear = p.year; }

            var painter = ctx.painter2D;
            painter.strokeColor = ColGold;
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int i = 0; i < _curve.Length; i++)
            {
                float x = rect.xMin + rect.width * (_curve[i].year / (float)maxYear);
                float y = rect.yMax - rect.height * 0.9f * (_curve[i].pop / (float)maxPop) - rect.height * 0.05f;
                if (i == 0) painter.MoveTo(new Vector2(x, y)); else painter.LineTo(new Vector2(x, y));
            }
            painter.Stroke();
        }

        void RebuildEraStrip()
        {
            _eraStrip.Clear();
            if (_curve.Length == 0) return;

            int spanStart = 0;
            for (int i = 1; i <= _curve.Length; i++)
            {
                bool close = i == _curve.Length || _curve[i].era != _curve[spanStart].era;
                if (!close) continue;
                int years = _curve[i - 1].year - _curve[spanStart].year + 1;
                var seg = new VisualElement();
                seg.style.flexGrow = Mathf.Max(1, years);
                seg.style.backgroundColor = ColPanel2;
                seg.style.borderLeftWidth = spanStart > 0 ? 1 : 0;
                seg.style.borderLeftColor = ColGold;
                seg.style.justifyContent = Justify.Center;
                var l = new Label(WorldEras.Name(_curve[spanStart].era));
                l.style.color = ColSub; l.style.fontSize = 9; l.style.letterSpacing = 1;
                l.style.paddingLeft = 4;
                seg.Add(l);
                _eraStrip.Add(seg);
                spanStart = i;
            }
        }

        // ---------------- small helpers (same shapes as the chronicle view) ----------------

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

        static void RowFlex(VisualElement v)
        {
            v.style.flexDirection = FlexDirection.Row;
            v.style.alignItems = Align.Center;
        }

        static Button MakeButton(string text, System.Action onClick)
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
            return b;
        }
    }
}
