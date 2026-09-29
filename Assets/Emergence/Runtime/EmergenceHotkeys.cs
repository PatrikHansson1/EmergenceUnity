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
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    var a = Alm(); if (a == null) return;
                    if (a.AlmanacOpen) a.CloseAlmanac();
                    else { var b = Book(); if (b != null && b.BookOpen) b.CloseBook(); a.OpenAlmanac(); }
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    var b = Book(); if (b != null && b.BookOpen) b.CloseBook();
                    var a = Alm();  if (a != null && a.AlmanacOpen) a.CloseAlmanac();
                }
            }
            catch { /* legacy Input disabled in some setups — disarm, never break the frame */ }
        }
    }
}
