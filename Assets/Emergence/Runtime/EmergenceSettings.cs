// EMERGENCE — SETTINGS (D-925): the on-screen path to volume. The sprint review (D-917) found that −/+/\ were
// the ONLY way to set the volume and that no screen ever said so; a Steam player who cannot find the volume
// writes the review about that. This panel is the smallest honest answer: master + music sliders, mute, the
// key map, all in the Screen Bible's own vocabulary (EmergenceUI — pricked corner, no closed box), opened
// by Esc when no reading panel is up, or by the small SETTINGS mark at the bottom-left. Volumes persist in
// PlayerPrefs so the next session opens at the level the player chose. Presentation-only: reads no sim state,
// pauses nothing, touches no bus.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceSettings : MonoBehaviour
    {
        public const string KeyMaster = "emg.vol.master", KeyMusic = "emg.vol.music";
        public bool Open { get; private set; }

        Fas6MusicDirector _music;
        Fas3AudioDirector _audio;
        Fas4ChronicleView _book;
        Fas5AlmanacView _alm;
        float _preMute = -1f;
        GUIStyle _head, _key;

        Fas6MusicDirector Music() { if (_music == null) _music = FindAnyObjectByType<Fas6MusicDirector>(); return _music; }

        void Awake()
        {
            // restore the player's levels before the first frame sounds
            try
            {
                if (PlayerPrefs.HasKey(KeyMaster)) AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMaster));
                var m = Music();
                if (m != null && PlayerPrefs.HasKey(KeyMusic)) m.SetVolume(Mathf.Clamp01(PlayerPrefs.GetFloat(KeyMusic)));
            }
            catch { }
        }

        public void Toggle() { Open = !Open; Click(); }
        public void Close() { Open = false; }

        /// <summary>Master level, persisted. The hotkeys (−/+) route through here so a key press and a slider agree.</summary>
        public static void SetMaster(float v)
        {
            AudioListener.volume = Mathf.Clamp01(v);
            try { PlayerPrefs.SetFloat(KeyMaster, AudioListener.volume); PlayerPrefs.Save(); } catch { }
        }

        public void SetMusic(float v)
        {
            var m = Music(); if (m == null) return;
            m.SetVolume(Mathf.Clamp01(v));
            try { PlayerPrefs.SetFloat(KeyMusic, m.volume); PlayerPrefs.Save(); } catch { }
        }

        public void ToggleMute()
        {
            if (_preMute >= 0f) { SetMaster(_preMute); _preMute = -1f; }
            else { _preMute = AudioListener.volume; SetMaster(0f); }
        }
        public bool Muted => _preMute >= 0f;

        void Click() { if (_audio == null) _audio = FindAnyObjectByType<Fas3AudioDirector>(); if (_audio != null) _audio.PlayUIClick(); }

        bool ReadingPanelUp()
        {
            if (_book == null) _book = FindAnyObjectByType<Fas4ChronicleView>();
            if (_alm == null) _alm = FindAnyObjectByType<Fas5AlmanacView>();
            return (_book != null && _book.BookOpen) || (_alm != null && _alm.AlmanacOpen);
        }

        void OnGUI()
        {
            EmergenceUI.Begin();
            float W = EmergenceUI.W, H = EmergenceUI.H;
            if (_head == null)
            {
                _head = new GUIStyle(EmergenceUI.Display) { fontSize = 22 };
                _key = new GUIStyle(EmergenceUI.Meta) { wordWrap = true, alignment = TextAnchor.UpperLeft };
            }

            // the mark that says the panel exists — bottom-left, out of the world's way
            var mark = new Rect(EmergenceUI.Sp6, H - EmergenceUI.Sp6 - 18f, 90f, 18f);
            if (!Open && !ReadingPanelUp())
            {
                if (GUI.Button(mark, "SETTINGS", EmergenceUI.Button)) Toggle();
            }

            if (Open)
            {
                // scrim + the panel, centred, pricked corner (D-220: no closed rectangle)
                EmergenceUI.Wash(new Rect(0, 0, W, H), EmergenceUI.Surface0, 0.55f);
                const float w = 460f, h = 336f;
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

                float x = r.x + EmergenceUI.Sp5 + 8f, y = r.y + EmergenceUI.Sp4;
                float cw = w - EmergenceUI.Sp5 * 2f - 20f;
                GUI.Label(new Rect(x, y, 200, 30), "SETTINGS", _head); y += 34f;
                EmergenceUI.RuleH(x, y, cw, EmergenceUI.Hairline); y += EmergenceUI.Sp3;

                // master
                GUI.Label(new Rect(x, y, 200, 16), "MASTER VOLUME", EmergenceUI.Meta);
                GUI.Label(new Rect(x + cw - 60, y, 60, 16), Mathf.RoundToInt(AudioListener.volume * 100f) + " %", EmergenceUI.Dim);
                y += 18f;
                float mv = EmergenceUI.Slider(new Rect(x + 6, y, cw - 12, 12), AudioListener.volume, 0f, 1f);
                if (Mathf.Abs(mv - AudioListener.volume) > 0.001f) { _preMute = -1f; SetMaster(mv); }
                y += EmergenceUI.Sp5;

                // music
                var m = Music();
                float cur = m != null ? m.volume : 0f;
                GUI.Label(new Rect(x, y, 120, 16), "MUSIC", EmergenceUI.Meta);
                GUI.Label(new Rect(x + cw - 60, y, 60, 16), m != null ? Mathf.RoundToInt(cur * 100f) + " %" : "—", EmergenceUI.Dim);
                y += 18f;
                if (m != null)
                {
                    float nv = EmergenceUI.Slider(new Rect(x + 6, y, cw - 12, 12), cur, 0f, 1f);
                    if (Mathf.Abs(nv - cur) > 0.001f) SetMusic(nv);
                }
                y += EmergenceUI.Sp5;

                // mute + keys
                if (GUI.Button(new Rect(x, y, 96, 22), Muted ? "UNMUTE" : "MUTE", Muted ? EmergenceUI.ButtonOn : EmergenceUI.Button)) { ToggleMute(); Click(); }
                if (GUI.Button(new Rect(x + cw - 96, y, 96, 22), "RESUME", EmergenceUI.ButtonOn)) { Close(); Click(); }
                y += 30f;
                EmergenceUI.RuleH(x, y, cw, EmergenceUI.Hairline); y += EmergenceUI.Sp2;
                GUI.Label(new Rect(x, y, cw, 110),
                    "KEYS\n" +
                    "Space  pause     1 / 2 / 3  speed     Esc  settings / close\n" +
                    "W A S D  move     Q / E  turn     O  orbit\n" +
                    "drag  look     scroll  zoom\n" +
                    "B  the book     M  the almanac     − / +  volume     \\  mute",
                    _key);
            }
            EmergenceUI.End();
        }
    }
}
