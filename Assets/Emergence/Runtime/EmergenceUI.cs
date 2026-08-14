// EMERGENCE — SKÄRMBIBELN §11 STEG 2.1 (D-218): THE TOKEN LAYER.
//
// The Screen Bible names the single largest difference between software that looks shipped and
// software that looks like a hobby project, and it is not a font or a colour — it is the ABSENCE OF
// A TOKEN LAYER. Without one, every new view invents its own values: a heading at 21 px here and 23
// there, #1A1A1A in one panel and #181C22 in the next, a 6 px radius beside a 10 px radius. Nobody
// can point at any single element and call it wrong, and the whole thing looks amateur anyway. It is
// invisible in screenshots of individual components and obvious in a screenshot of the screen.
//
// This project has the disease in its purest form: three unrelated visual languages at once — an
// IMGUI debug readout top-left, a UI-Toolkit Almanac card top-right with real design care, and an
// unstyled text button bottom-right.
//
// THE COMPLICATION, AND WHY THIS IS C# AND NOT A .USS FILE. The Screen Bible assumes UI Toolkit and
// USS custom properties. Half of this screen is UI Toolkit (Fas4ChronicleView, Fas5AlmanacView) and
// half is IMGUI (the time controls, the feed overlay, the diagnostics). Rewriting the IMGUI half
// before EA is days of work against a proven, probe-covered layer, and USS tokens cannot reach IMGUI
// at all. So the token layer lives HERE, in C#, and both technologies read the same numbers. When
// the IMGUI half migrates, this file becomes the generator for the .uss and not one value changes.
// The alternative — tokens in USS now, hard-coded greys in IMGUI until the migration — would leave
// the screen looking exactly as it does today for the whole of Early Access.
//
// Every value below is the Screen Bible's, verbatim, including the measured ones. The panel alpha in
// particular is NOT a taste: 0,96 was derived by compositing a panel over the three brightest things
// this world can put behind it and re-measuring contrast. At 0,92 tertiary text falls below AA
// against a lit meadow. It is also why this game never needs a scrim.
using UnityEngine;

namespace Emergence.Runtime
{
    public static class EmergenceUI
    {
        // ---------- surfaces: the slate reading board ----------
        public static readonly Color Surface0 = Hex(0x0B0E12);   // scrim, deepest well
        public static readonly Color Surface1 = Hex(0x141A21);   // panel base — the board
        public static readonly Color Surface2 = Hex(0x1C242D);   // raised card, the page
        public static readonly Color Surface3 = Hex(0x263039);   // hover, selected row, input well
        public static readonly Color Hairline = Hex(0x37444F);   // 1 px separators and panel edge
        public static readonly Color HairlineWarm = Hex(0x4A4033); // the lit edge — top and left only

        /// <summary>MEASURED, not chosen. Below this, tertiary ink and the semantics fall under AA
        /// against a bright meadow. It is also the reason no surface in this game needs a scrim.</summary>
        public const float PanelAlpha = 0.96f;

        // ---------- ink: bone, never white ----------
        public static readonly Color Ink100 = Hex(0xF0E7D6);
        public static readonly Color Ink70  = Hex(0xB6AE9E);
        public static readonly Color Ink55  = Hex(0x979283);     // never on Surface3 — 4,32:1 there

        // ---------- the one warm point ----------
        // Not a fantasy reflex. The light rig locked a dusk identity: a blue world with exactly ONE
        // warm point. That is a compositional law, and an interface that breaks it reads as a second
        // author on the same painting. Gold is the only warm value the rig permits on screen.
        public static readonly Color Gold     = Hex(0xD9A441);
        public static readonly Color GoldDim  = Hex(0xB98B38);
        public static readonly Color OnGold   = Hex(0x14181C);

        // ---------- semantics: margin rules and marks ONLY, never body text ----------
        // And never alone: every semantic colour is accompanied by the word. Birth and conflict are
        // the classic deuteranopia confusion pair; they are separated here by lightness as a second
        // channel, but the WORD is the actual guarantee.
        public static readonly Color SemBirth     = Hex(0x9BC48D);
        public static readonly Color SemConflict  = Hex(0xD4795C);
        public static readonly Color SemLoss      = Hex(0x909CB0);
        public static readonly Color SemKnowledge = Hex(0xD9A441);   // deliberately the accent

        // ---------- spacing: base 4 px at 1280x800 ----------
        public const int Sp1 = 4, Sp2 = 8, Sp3 = 12, Sp4 = 16, Sp5 = 24, Sp6 = 32, Sp7 = 48, Sp8 = 64;

        // ---------- radius ----------
        public const int RPage = 0, RInner = 2, RCard = 4;

        // ---------- type, at the 1280x800 reference ----------
        public const int FsChronicle = 21, LhChronicle = 34;   // narrative lane's floor: 19 px is ~6 pt on a Deck
        public const int FsBody = 17, LhBody = 24;
        public const int FsCell = 16, LhCell = 24;
        public const int FsMeta = 15, LhMeta = 20;
        public const int FsMicro = 14;                          // ABSOLUTE FLOOR. Nothing smaller, anywhere.
        public const int MeasureChronicle = 600;                // px -> ~62 characters

        // ---------- motion ----------
        public const float DurPanelIn = 0.220f, DurPanelOut = 0.140f;
        public const float DurEntry = 0.320f, DurEntryStagger = 0.060f;
        public const float DurValue = 0.160f, DurValueDecay = 0.600f;
        public const float DurHunch = 0.520f;

        // ---------- the reference resolution, and why IMGUI needs it stated ----------
        //
        // Every number in this file is the Screen Bible's, given at its 1280x800 reference. UI
        // Toolkit scales a panel for you; IMGUI does not — it draws in raw pixels, so a 17 px label
        // on a 2560x1440 capture is half the intended size and the first HUD screenshot showed
        // exactly that. So the IMGUI surfaces draw through a scale matrix and work in REFERENCE
        // space, which has the pleasant side effect of making the Screen Bible's numbers literally
        // true rather than approximately true.
        //
        // Matched on HEIGHT (the Bible's own choice): on a 16:9 screen the extra width becomes
        // gutter rather than text, which is what a reading column wants.
        public const float RefHeight = 800f;
        public static float Scale => Mathf.Max(1f, Screen.height / RefHeight);
        /// <summary>Screen width in REFERENCE pixels — use instead of Screen.width inside Begin/End.</summary>
        public static float W => Screen.width / Scale;
        /// <summary>Screen height in REFERENCE pixels.</summary>
        public static float H => Screen.height / Scale;

        static Matrix4x4 _prevMatrix;
        public static void Begin()
        {
            _prevMatrix = GUI.matrix;
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        }
        public static void End() { GUI.matrix = _prevMatrix; }

        // ---------- the diagnostics gate ----------
        // The debug readouts are not restyled, they are REMOVED from what a player sees. A frame
        // counter in a Steam screenshot is worth real wishlists. Off by default; a probe or an
        // editor menu turns it on, and it never survives into a fresh session unless asked for.
        const string DiagKey = "emergence.ui.diagnostics";
        static int _diag = -1;
        public static bool Diagnostics
        {
            get { if (_diag < 0) _diag = PlayerPrefs.GetInt(DiagKey, 0); return _diag == 1; }
            set { _diag = value ? 1 : 0; PlayerPrefs.SetInt(DiagKey, _diag); }
        }

        // ---------- IMGUI bridge ----------
        // IMGUI cannot read tokens, so it is handed them. One place, so the day the IMGUI half
        // migrates to UI Toolkit these styles are deleted and nothing else changes.
        static Texture2D _panelTex, _cardTex, _goldTex, _hairTex;
        static GUIStyle _panel, _label, _labelDim, _meta, _btn, _btnOn;

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c); t.Apply();
            t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Point;
            return t;
        }

        static void EnsureStyles()
        {
            if (_panel != null && _panelTex != null) return;
            var p = Surface1; p.a = PanelAlpha;
            var c = Surface2; c.a = 1f;
            _panelTex = Solid(p); _cardTex = Solid(c); _goldTex = Solid(Gold); _hairTex = Solid(Hairline);

            _panel = new GUIStyle { normal = { background = _panelTex }, border = new RectOffset(1, 1, 1, 1) };
            _label = new GUIStyle(GUIStyle.none) { fontSize = FsBody, normal = { textColor = Ink100 }, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip };
            _labelDim = new GUIStyle(_label) { normal = { textColor = Ink70 } };
            _meta = new GUIStyle(_label) { fontSize = FsMeta, normal = { textColor = Ink55 } };
            _btn = new GUIStyle
            {
                fontSize = FsBody, alignment = TextAnchor.MiddleCenter,
                normal = { background = _cardTex, textColor = Ink70 },
                hover  = { background = _cardTex, textColor = Ink100 },
                active = { background = _cardTex, textColor = Ink100 },
                border = new RectOffset(1, 1, 1, 1)
            };
            _btnOn = new GUIStyle(_btn)
            {
                normal = { background = _goldTex, textColor = OnGold },
                hover  = { background = _goldTex, textColor = OnGold },
                active = { background = _goldTex, textColor = OnGold },
                fontStyle = FontStyle.Bold
            };
        }

        public static GUIStyle Panel   { get { EnsureStyles(); return _panel; } }
        public static GUIStyle Label   { get { EnsureStyles(); return _label; } }
        public static GUIStyle Dim     { get { EnsureStyles(); return _labelDim; } }
        public static GUIStyle Meta    { get { EnsureStyles(); return _meta; } }
        public static GUIStyle Button  { get { EnsureStyles(); return _btn; } }
        public static GUIStyle ButtonOn{ get { EnsureStyles(); return _btnOn; } }

        /// <summary>A panel: nearly-opaque slate with the 1 px hairline that is the single highest
        /// value line in the whole language. Muddiness over a bright world is an EDGE failure, not a
        /// fill failure — give the eye a hard boundary and it disappears.</summary>
        public static void DrawPanel(Rect r)
        {
            EnsureStyles();
            GUI.DrawTexture(r, _panelTex);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), _hairTex);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), _hairTex);
            GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), _hairTex);
            GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), _hairTex);
        }

        /// <summary>A gold rule under a value that just changed. The whole event: this changed, look
        /// here, it is over. Numbers never roll, tick or count up — a rolling counter is a slot
        /// machine, and a slot machine implies you did something.</summary>
        public static void DrawChangeRule(Rect r, float age)
        {
            if (age >= DurValueDecay) return;
            EnsureStyles();
            var c = GoldDim; c.a = 1f - age / DurValueDecay;
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.yMax - 2f, r.width, 2f), _goldTex);
            GUI.color = prev;
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
