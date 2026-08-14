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

        // ---------- ORNAMENT (D-220) ----------
        //
        // Patrik saw the finished token pass and said the HUD looked boring and boxy — "stela och
        // fyrkantiga", stiff and square, couldn't they be ornamented. He was right, and the art
        // director's amendment named the fault precisely: the material spec was all PROHIBITIONS
        // (no glass, no wood, no parchment, no scrim, no glow) and its total positive craft was a
        // fill, a border, a radius and a shadow. That is not a material, it is a PRIMITIVE. A
        // fiction was named — an archivist's reading board — and a rounded rectangle was handed to
        // the builder. Quiet and characterless are different things and we built the wrong one.
        //
        // Three specific omissions, all one family:
        //   - no TERMINALS. Every rule simply stopped. A rule that stops is a border; a rule that
        //     ends in a mark is ornament. Two quads, and it is the whole distance between an
        //     engineering drawing and something made by a hand.
        //   - no CORNERS. A radius is a way of not deciding a corner.
        //   - SYMMETRY was never forbidden, and symmetry is the actual disease. Four identically
        //     treated edges IS a box; there is no way to draw one that is not.
        //
        // THE VOCABULARY IS THE ILLUMINATED MANUSCRIPT, and not for the obvious reason. Every
        // ornamental vocabulary implies a MAKER, and the maker has to be someone who plausibly
        // exists. Carved timber implies a joiner in a village — but villages differ and can LOSE
        // knowledge, so a carved frame makes a claim the simulation can contradict, and its texture
        // would come from the same diffuse as the cottages (the style seam, by the fastest route).
        // Cloth cannot hold a hairline rule. Iron reads fortress, which is a COMMAND verb, and iron
        // is what a raid is made of. Manuscript ornament is SCRIBAL: it belongs to the recorder, the
        // one entity in this fiction already allowed to stand outside the world and look in. And
        // "illuminated" is literally our lighting law — gold applied so a dark page catches light,
        // in a blue world with exactly one warm point.
        //
        // NO FLOWERS, and the reason is worth keeping: a flower is a SPECIFIC PLANT, specific plants
        // belong to specific places, and this world's flora is generated. A rose is a claim about a
        // world that has not made it. A vine terminal — an abstraction of growth — is not.
        //
        // Everything below is drawn from rects. No imported art, no import pipeline, crisper at Deck
        // scale, and deterministic by construction.

        /// <summary>Ornamental gold. NOT the accent: present, never luminous. Only ONE element on
        /// screen may carry full Gold — if ornamental gold ever reads as a hue rather than as a lamp,
        /// the pass has gone wrong.</summary>
        public static readonly Color GoldLeaf = new Color(0xD9 / 255f, 0xA4 / 255f, 0x41 / 255f, 0.38f);

        /// <summary>Muted leather. Book mode only, and its meaning is carried by POSITION (it marks
        /// the last-read place), never by colour.</summary>
        public static readonly Color Ribbon = Hex(0x7A4F3C);

        // measured tooth budget, in sRGB levels of +- variation:
        //   8  wherever Ink55 appears (it bottoms at 4,54:1 there)
        //  10  text block carrying only Ink100/Ink70
        //  14  margins and edges with no text at all
        public const int ToothText = 8, ToothBody = 10, ToothMargin = 14;

        public const int PrickPitch = 32;      // MUST equal LhChronicle — the dots set the ruling
        public const int PrickInset = 12;
        public const int BracketH = 20, BracketV = 14;
        public const int Lozenge = 5;

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

        // ---------- THE CURSOR (D-223) ----------
        //
        // Windows' white arrow in a dusk world is a seam, and it is in EVERY screenshot we will ever
        // take — including the store page. The gap sweep ranked it fourth, and it is step 1 of the
        // production ladder: drawn in code, so it is deterministic, free, carries no licence question
        // and cannot drift from the palette, because it IS the palette.
        //
        // A pointer has one job and taste does not get a vote: it must read as a pointer at a glance
        // on any background. So the shape stays the conventional arrow — bone, with a one-pixel dark
        // keyline so it survives over a lit meadow as well as over a slate panel — and the only thing
        // the visual language contributes is the colour.
        static Texture2D _cursor;
        static bool _cursorSet;

        public static void EnsureCursor()
        {
            if (_cursorSet) return;
            _cursorSet = true;
            const int N = 24;
            _cursor = new Texture2D(N, N, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            _cursor.filterMode = FilterMode.Point;
            var px = new Color32[N * N];
            var ink = (Color32)Ink100;
            var edge = new Color32(11, 14, 18, 235);          // Surface0, near-opaque keyline

            // the classic arrow, expressed as a per-row run so there is no art asset to import:
            // a 45-degree left edge, a vertical-ish right edge, and a tail that steps back in.
            for (int y = 0; y < N; y++)
            {
                int fill;
                if (y <= 12) fill = y;                         // the head widens with each row
                else if (y <= 16) fill = 12 - (y - 12) * 2;    // the notch under the head
                else fill = 0;
                if (y > 16) continue;
                for (int x = 0; x <= fill; x++)
                {
                    // the tail: rows below the notch keep only a narrow shaft
                    if (y > 12 && x > 5 && x < fill - 1) continue;
                    px[(N - 1 - y) * N + x] = ink;
                }
            }
            // keyline: any transparent texel orthogonally touching ink becomes edge
            var outp = (Color32[])px.Clone();
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    if (px[y * N + x].a > 0) continue;
                    bool near = (x > 0 && px[y * N + x - 1].a > 0) || (x < N - 1 && px[y * N + x + 1].a > 0)
                             || (y > 0 && px[(y - 1) * N + x].a > 0) || (y < N - 1 && px[(y + 1) * N + x].a > 0);
                    if (near) outp[y * N + x] = edge;
                }
            _cursor.SetPixels32(outp); _cursor.Apply();
            Cursor.SetCursor(_cursor, new Vector2(1f, 1f), CursorMode.Auto);
        }

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

        // ---------- THE TWO FACES (D-221) ----------
        //
        // The luckesvep found that this project contained NO FONTS AT ALL. The Screen Bible
        // prescribes a screen-serif for the Chronicle and a neutral sans for measurement, and
        // neither existed — every surface in the game rendered in Unity's built-in face. That is the
        // single largest identity gap in the build, and it was free to close.
        //
        // The argument is not "serif is classy". The Chronicle and the Almanac make DIFFERENT TRUTH
        // CLAIMS and the type has to say which one you are reading before you read a word. The
        // Chronicle's promise is "everything below happened" — testimony, in prose — and serif is the
        // typographic signal for A THING SOMEONE WROTE DOWN. It should not look generated; it should
        // look transcribed. The Almanac claims the opposite: this is MEASUREMENT. A sans with
        // tabular figures says "nobody's voice, just the number". Setting the Almanac in serif would
        // make data feel authored, which is a lie; setting the Chronicle in sans would make testimony
        // feel like telemetry, which is a worse one.
        //
        // Literata (serif) and Inter (sans). Both SIL OFL 1.1 — and the licence text was READ and
        // committed beside the fonts, not assumed. Literata was commissioned for e-reading: large
        // x-height, low stroke contrast, sturdy at 21 px, which is what a 7 inch Deck panel needs.
        // EB Garamond was considered and rejected: beautiful, and its hairlines fall apart at this
        // size on that screen. Refuse a third face — a monospace for the Almanac's columns is the
        // obvious temptation and it would be a third visual language on a screen being cured of
        // exactly that disease.
        public const string SerifResource = "Fonts/Literata-Variable";
        public const string SansResource  = "Fonts/Inter-Variable";

        static Font _serif, _sans;
        static bool _fontsTried;

        static void EnsureFonts()
        {
            if (_fontsTried) return;
            _fontsTried = true;
            _serif = Resources.Load<Font>(SerifResource);
            _sans  = Resources.Load<Font>(SansResource);
        }

        /// <summary>The Chronicle's face. Null falls back to Unity's built-in — a missing font must
        /// cost the voice, never the record.</summary>
        public static Font Serif { get { EnsureFonts(); return _serif; } }
        public static Font Sans  { get { EnsureFonts(); return _sans; } }
        public static string FontNote => (Serif != null ? "serif=Literata" : "serif=MISSING")
                                       + " " + (Sans != null ? "sans=Inter" : "sans=MISSING");

        // ---------- IMGUI bridge ----------
        // IMGUI cannot read tokens, so it is handed them. One place, so the day the IMGUI half
        // migrates to UI Toolkit these styles are deleted and nothing else changes.
        static Texture2D _panelTex, _cardTex, _goldTex, _hairTex;
        static GUIStyle _panel, _label, _labelDim, _meta, _btn, _btnOn, _display, _serifLabel;

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
            if (Sans != null) _label.font = Sans;
            _labelDim = new GUIStyle(_label) { normal = { textColor = Ink70 } };
            _meta = new GUIStyle(_label) { fontSize = FsMeta, normal = { textColor = Ink55 } };
            _display = new GUIStyle(_label) { fontSize = 34, fontStyle = FontStyle.Bold, normal = { textColor = Ink100 } };
            // the Latest Line carries chronicle prose, so it carries the chronicle's face
            _serifLabel = new GUIStyle(_label) { fontSize = FsChronicle };
            if (Serif != null) _serifLabel.font = Serif;
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
        public static GUIStyle Display { get { EnsureStyles(); return _display; } }
        /// <summary>Chronicle voice: prose that someone wrote down.</summary>
        public static GUIStyle Prose   { get { EnsureStyles(); return _serifLabel; } }
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

        // ================= ORNAMENT PRIMITIVES =================
        // Every one of these is rects. The only curve in the whole language is the vine terminal,
        // and at most ONE of those may be visible at a time — that cap is the entire discipline
        // preventing a fantasy-book look, so it is asserted rather than suggested.

        static Texture2D _white;
        static Texture2D White { get { if (_white == null) _white = Solid(Color.white); return _white; } }

        static void Fill(Rect r, Color c)
        {
            var prev = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = prev;
        }

        /// <summary>A rule that ENDS IN A MARK. A rule that stops is a border; this is ornament, and
        /// the difference costs two quads.</summary>
        public static void RuleH(float x, float y, float len, Color c, bool terminals = true, int loz = 0)
        {
            Fill(new Rect(x, y, len, 1f), c);
            if (!terminals) return;
            int s = loz > 0 ? loz : Lozenge;
            LozengeAt(x, y + 0.5f, s, c);
            LozengeAt(x + len, y + 0.5f, s, c);
        }

        public static void RuleV(float x, float y, float len, Color c, bool terminals = true, int loz = 0)
        {
            Fill(new Rect(x, y, 1f, len), c);
            if (!terminals) return;
            int s = loz > 0 ? loz : Lozenge;
            LozengeAt(x + 0.5f, y, s, c);
            LozengeAt(x + 0.5f, y + len, s, c);
        }

        /// <summary>A rotated square, drawn as a stack of rows so it needs no texture and no rotation
        /// matrix — which also means it stays crisp at any panel scale.</summary>
        public static void LozengeAt(float cx, float cy, int size, Color c)
        {
            int half = Mathf.Max(1, size / 2);
            for (int i = -half; i <= half; i++)
            {
                float w = (half - Mathf.Abs(i)) * 2f + 1f;
                Fill(new Rect(cx - w * 0.5f, cy + i, w, 1f), c);
            }
        }

        /// <summary>A corner bracket. NEVER all four: at least one edge of a surface must differ, or
        /// it is a box and there is no way to draw one that is not.</summary>
        public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

        public static void Bracket(Rect r, Corner k, Color c, int inset = 0)
        {
            float x = k == Corner.TopLeft || k == Corner.BottomLeft ? r.x + inset : r.xMax - inset - BracketH;
            float y = k == Corner.TopLeft || k == Corner.TopRight ? r.y + inset : r.yMax - inset - 1f;
            Fill(new Rect(x, y, BracketH, 1f), c);
            float vx = k == Corner.TopLeft || k == Corner.BottomLeft ? r.x + inset : r.xMax - inset - 1f;
            float vy = k == Corner.TopLeft || k == Corner.TopRight ? r.y + inset : r.yMax - inset - BracketV;
            Fill(new Rect(vx, vy, 1f, BracketV), c);
        }

        /// <summary>The pricking column: the marks a scribe made to SET the ruling. It reads as a
        /// prepared surface, and it breaks a vertical edge with rhythm instead of with a line.</summary>
        public static void PrickColumn(float x, float y, float h, Color c, int pitch = 0, int dot = 1)
        {
            int p = pitch > 0 ? pitch : 8;
            for (float yy = y; yy <= y + h; yy += p) Fill(new Rect(x, yy, dot, dot), c);
        }

        /// <summary>An edge that DISSOLVES instead of ending. Two of a surface's four edges get this
        /// and two get brackets; that asymmetry is what makes the shape unreadable as a rectangle.</summary>
        public static void FadeEdge(Rect r, Color c, float alpha, bool horizontal, bool towardMax, int band = 12)
        {
            for (int i = 0; i < band; i++)
            {
                float t = 1f - i / (float)band;
                var cc = c; cc.a = alpha * t;
                if (horizontal)
                {
                    float x = towardMax ? r.xMax - band + i : r.x + band - 1 - i;
                    Fill(new Rect(x, r.y, 1f, r.height), cc);
                }
                else
                {
                    float y = towardMax ? r.yMax - band + i : r.y + band - 1 - i;
                    Fill(new Rect(r.x, y, r.width, 1f), cc);
                }
            }
        }

        /// <summary>A vertical wash: the world darkens INTO a line of writing instead of being
        /// interrupted by a box. This is what replaces a panel where there is no page.
        ///
        /// First version stacked one-pixel quads and BANDED visibly once the reference-space matrix
        /// scaled them — the stripes read as a scanline artefact, which is worse than the box it
        /// replaced. A generated 1x128 gradient stretched by the GPU is smooth at any scale and is
        /// one draw call instead of a hundred.</summary>
        static Texture2D _washTex;
        static Texture2D WashTex
        {
            get
            {
                if (_washTex != null) return _washTex;
                const int N = 128;
                _washTex = new Texture2D(1, N, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _washTex.wrapMode = TextureWrapMode.Clamp; _washTex.filterMode = FilterMode.Bilinear;
                var px = new Color32[N];
                for (int i = 0; i < N; i++)
                {
                    float t = i / (float)(N - 1);
                    // smootherstep in from both ends: no hard start, no hard finish
                    float a = t < 0.5f ? t * 2f : (1f - t) * 2f;
                    a = a * a * (3f - 2f * a);
                    px[N - 1 - i] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
                _washTex.SetPixels32(px); _washTex.Apply();
                return _washTex;
            }
        }

        public static void Wash(Rect r, Color c, float peakAlpha)
        {
            var prev = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, peakAlpha);
            GUI.DrawTexture(r, WashTex);
            GUI.color = prev;
        }

        /// <summary>A slider drawn from the same vocabulary: a ruled track with marks at its ends and
        /// a lozenge for a handle. Unity's own slider was the last un-styled control on the screen,
        /// and a grey capsule with a round grip is a web widget, not a scribe's mark.</summary>
        static GUIStyle _sliderBg, _sliderThumb;
        static Texture2D _thumbTex, _trackTex;

        public static float Slider(Rect r, float value, float min, float max)
        {
            EnsureStyles();
            if (_sliderBg == null)
            {
                _trackTex = Solid(Hairline);
                const int T = 11;
                _thumbTex = new Texture2D(T, T, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[T * T];
                int half = T / 2;
                var gold = (Color32)Gold;
                for (int y = 0; y < T; y++)
                    for (int x = 0; x < T; x++)
                    {
                        bool inside = Mathf.Abs(x - half) + Mathf.Abs(y - half) <= half;
                        px[y * T + x] = inside ? gold : new Color32(0, 0, 0, 0);
                    }
                _thumbTex.SetPixels32(px); _thumbTex.Apply();
                _sliderBg = new GUIStyle { fixedHeight = 1f, normal = { background = _trackTex } };
                _sliderThumb = new GUIStyle { fixedWidth = T, fixedHeight = T, normal = { background = _thumbTex } };
            }
            LozengeAt(r.x, r.y + r.height * 0.5f, Lozenge, Hairline);
            LozengeAt(r.xMax, r.y + r.height * 0.5f, Lozenge, Hairline);
            return GUI.HorizontalSlider(r, value, min, max, _sliderBg, _sliderThumb);
        }

        /// <summary>The pause mark: two bars, so the control is DISCOVERABLE. A tally alone tells a
        /// player how fast the world runs and never tells them they may stop it.</summary>
        public static void PauseMark(float x, float baseline, bool active, Color on, Color off, Color mark)
        {
            var c = active ? on : off;
            Fill(new Rect(x, baseline - 9f, 2f, 9f), c);
            Fill(new Rect(x + 4f, baseline - 9f, 2f, 9f), c);
            if (active) Fill(new Rect(x - 1f, baseline + 3f, 8f, 1f), mark);
        }

        /// <summary>The scribe's tally. Four vertical strokes of RISING HEIGHT — so speed is carried
        /// by height as well as by fill, and survives colour-blindness and a glance without a legend.
        /// Four identical squares is the boxiest possible widget and it was what we had.</summary>
        public static void Tally(float x, float baseline, int active, bool paused, Color on, Color off, Color mark)
        {
            int[] hgt = { 6, 9, 12 };
            for (int i = 0; i < 3; i++)
            {
                float bx = x + i * 7f;
                bool lit = !paused && i == active;
                Fill(new Rect(bx, baseline - hgt[i], 2f, hgt[i]), paused ? off : (i <= active ? on : off));
                if (lit) Fill(new Rect(bx - 2f, baseline + 3f, 6f, 1f), mark);
            }
        }

        /// <summary>Page tooth. Generated, never imported — deterministic hash noise, so the same
        /// surface is the same surface in every build and there is no import pipeline to drift.
        /// Amplitude is the CALLER's choice against the measured budget above.</summary>
        static Texture2D _tooth;
        public static Texture2D Tooth
        {
            get
            {
                if (_tooth != null) return _tooth;
                const int N = 128;
                _tooth = new Texture2D(N, N, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                _tooth.wrapMode = TextureWrapMode.Repeat; _tooth.filterMode = FilterMode.Bilinear;
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                        h ^= h >> 13; h *= 2246822519u; h ^= h >> 16;
                        float v = (h & 0xFFFF) / 65535f;
                        // ~0.3% rarer, darker flecks: the hair side of a skin
                        float a = ((h >> 20) & 0x3FF) < 3 ? 1f : v * 0.55f;
                        px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                _tooth.SetPixels32(px); _tooth.Apply();
                return _tooth;
            }
        }

        /// <summary>Lay tooth over a rect at an amplitude in sRGB levels. Capped at the measured
        /// budget so ornament can never cost legibility.</summary>
        public static void LayTooth(Rect r, int levels)
        {
            levels = Mathf.Clamp(levels, 0, ToothMargin);
            if (levels == 0) return;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, levels / 255f * 4f);
            GUI.DrawTextureWithTexCoords(r, Tooth, new Rect(r.x / 128f, r.y / 128f, r.width / 128f, r.height / 128f));
            GUI.color = prev;
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
