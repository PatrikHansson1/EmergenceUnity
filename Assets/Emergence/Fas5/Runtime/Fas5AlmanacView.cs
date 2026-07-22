// EMERGENCE — FAS 5 v0 (per FAS5-KICKOFF-BRIEF 2026-07-22): the ALMANAC's native face — ANALYZE
// begins to show its patterns. Same UI Toolkit school as Fas4ChronicleView (D-145): almanac
// palette in code, PanelSettings REUSED from the Fas4 build (one resource, one seam), and the
// same honest pause law — opening the almanac pauses the clock, closing restores the prior mode,
// tps is never touched. If the UI assets are missing the view disarms itself (villkor-2 school:
// the probe proves that branch through panelSettingsResourceOverride).
//
// v0 = the Overview tab only (B2): headline tiles (år · era · befolkning · födda · döda · hyddor)
// + the population curve (generateVisualContent painter) + the era strip. The other five tabs
// wait for the engine's metrics/event export (R2 order); the Chronicle tab IS the Fas 4 book.
// DETERMINISM (D-078 r4): a pure READ of Fas5MetricsRecorder — never writes anything back.
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

        // probe-readable rendered truth (set at rebuild, straight from the labels' sources)
        public int TileYear { get; private set; }
        public string TileEra { get; private set; } = "";
        public int TilePop { get; private set; }
        public int TileBirths { get; private set; }
        public int TileDeaths { get; private set; }
        public int TileHuts { get; private set; }
        public int CurvePointCount { get; private set; }
        public int CurveLastYear { get; private set; }

        Fas5MetricsRecorder _rec;
        Fas3PresentationClock _clock;
        UIDocument _doc;
        VisualElement _openBtnPanel, _root, _tilesRow, _eraStrip, _curveHost;
        Label _sub;
        readonly List<Label> _tileValues = new List<Label>();
        Fas5MetricsRecorder.YearRecord[] _curve = new Fas5MetricsRecorder.YearRecord[0];

        bool _pausedBefore;
        long _lastSig = long.MinValue;

        Fas5MetricsRecorder Rec() { if (_rec == null) _rec = FindAnyObjectByType<Fas5MetricsRecorder>(); return _rec; }
        Fas3PresentationClock Clock() { if (_clock == null) _clock = FindAnyObjectByType<Fas3PresentationClock>(); return _clock; }

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

            _sub.text = "år " + TileYear + " · " + TileEra + " · statistik med kausalitet — korrelationerna väntar på motorns metrics (R2)";
            string[] vals = { TilePop.ToString(), TileBirths.ToString(), TileDeaths.ToString(), TileHuts.ToString(), TileEra };
            for (int i = 0; i < _tileValues.Count && i < vals.Length; i++) _tileValues[i].text = vals[i];

            RebuildEraStrip();
            _curveHost.MarkDirtyRepaint();
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
            _sub.style.color = ColSub; _sub.style.fontSize = 13; _sub.style.marginBottom = 10;
            card.Add(_sub);

            // ---- headline tiles ----
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
            card.Add(_tilesRow);

            // ---- population curve (the first pattern: the Memory Engine's shape arrives with R2) ----
            var curveHead = new Label("BEFOLKNING ÖVER TID");
            curveHead.style.color = ColSub; curveHead.style.fontSize = 10; curveHead.style.letterSpacing = 1;
            curveHead.style.marginBottom = 4;
            card.Add(curveHead);

            _curveHost = new VisualElement();
            Card(_curveHost, ColPanel2, ColLine, 10);
            _curveHost.style.height = 220;
            _curveHost.generateVisualContent += PaintCurve;
            card.Add(_curveHost);

            // ---- era strip under the curve ----
            _eraStrip = new VisualElement(); RowFlex(_eraStrip);
            _eraStrip.style.height = 22; _eraStrip.style.marginTop = 6;
            card.Add(_eraStrip);
        }

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
