// EMERGENCE — reading-panel hotkeys (D-911): B toggles the chronicle BOOK, M the ALMANAC, Esc closes
// either. The on-screen buttons already exist; this adds the keys players expect. The two fullscreen
// panels are mutually exclusive (opening one closes the other). Presentation-only, legacy Input guarded
// like the camera rig — never touches sim state. Keys chosen to avoid the camera's WASD/Space/1-2-3.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class EmergenceHotkeys : MonoBehaviour
    {
        Fas4ChronicleView _book;
        Fas5AlmanacView _alm;
        Fas3AudioDirector _audio;
        EmergenceSettings _settings;
        Fas4HunchDirector _hunch;
        Fas4HunchDirector Hunch() { if (_hunch == null) _hunch = FindAnyObjectByType<Fas4HunchDirector>(); return _hunch; }
        EmergenceSettings Settings() { if (_settings == null) _settings = FindAnyObjectByType<EmergenceSettings>(); return _settings; }
        void Click() { if (_audio == null) _audio = FindAnyObjectByType<Fas3AudioDirector>(); if (_audio != null) _audio.PlayUIClick(); }
        Fas4ChronicleView Book() { if (_book == null) _book = FindAnyObjectByType<Fas4ChronicleView>(); return _book; }
        Fas5AlmanacView Alm()   { if (_alm  == null) _alm  = FindAnyObjectByType<Fas5AlmanacView>();  return _alm; }

        void Update()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
                    var b = Book(); if (b == null) return;
                    if (b.BookOpen) b.CloseBook();
                    else { var a = Alm(); if (a != null && a.AlmanacOpen) a.CloseAlmanac(); b.OpenBook(); }
                    Click();
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    var a = Alm(); if (a == null) return;
                    if (a.AlmanacOpen) a.CloseAlmanac();
                    else { var b = Book(); if (b != null && b.BookOpen) b.CloseBook(); a.OpenAlmanac(); }
                    Click();
                }
                else if (Input.GetKeyDown(KeyCode.H))
                {
                    // D-926: H opens the journal of hunches (Y / N answer the open question while the card shows)
                    var hj = Hunch(); if (hj != null) hj.ToggleJournal();
                }
                else if (Input.GetKeyDown(KeyCode.Y)) { var hj = Hunch(); if (hj != null && hj.Pending != null && hj.Pending.answer < 0) hj.Answer(1); }
                else if (Input.GetKeyDown(KeyCode.N)) { var hj = Hunch(); if (hj != null && hj.Pending != null && hj.Pending.answer < 0) hj.Answer(0); }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // D-925: Esc closes what is open; with nothing open it opens SETTINGS (the volume's on-screen path)
                    var b = Book(); var a = Alm(); var s = Settings(); var hj = Hunch();
                    bool closed = false;
                    if (b != null && b.BookOpen) { b.CloseBook(); closed = true; }
                    if (a != null && a.AlmanacOpen) { a.CloseAlmanac(); closed = true; }
                    if (s != null && s.Open) { s.Close(); closed = true; }
                    if (hj != null && hj.JournalOpen) { hj.CloseJournal(); closed = true; }
                    if (!closed && s != null) s.Toggle();
                    Click();
                }

                // audio accessibility (Steam-review factor, audio-skill §7): master volume + mute.
                // Master via AudioListener.volume (global, read every frame — robust). Music-separate
                // control waits for a real settings UI (Patriks öga). Keys avoid the camera/speed keys.
                // D-925: keys and the settings panel share one law (persisted master level, one mute state)
                if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
                    EmergenceSettings.SetMaster(AudioListener.volume + 0.1f);
                else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
                    EmergenceSettings.SetMaster(AudioListener.volume - 0.1f);
                else if (Input.GetKeyDown(KeyCode.Backslash))
                {
                    var s = Settings();
                    if (s != null) s.ToggleMute();
                    else EmergenceSettings.SetMaster(AudioListener.volume > 0f ? 0f : 0.8f);
                }
            }
            catch { /* legacy Input disabled in some setups — disarm, never break the frame */ }
        }
    }
}
