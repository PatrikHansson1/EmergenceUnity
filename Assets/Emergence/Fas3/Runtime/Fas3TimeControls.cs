// EMERGENCE — FAS 3 increment 2 (D-134): TIME CONTROLS — the player's hand on presentation time.
//
// TIME LAW (D-133, proven): these buttons touch ONLY Fas3SimDriver.paused / ticksPerSecond — how
// many ticks the presentation may consume per real second. The sim's truth at tick T is pacing-
// independent by construction; pausing freezes the WATCH, never the world's logic.
//
// 1× = 24 ticks/s (the driver's documentary baseline: one sim year ≈ 9 s at YEAR≈216). 4× = 96.
// ▶▶ = uncapped (the driver becomes compute-bound; the HUD's measured ticks/s IS the cadence
// datapoint — in-editor Jint tops out near ~20 t/s, the increment-2 measurement).
// IMGUI overlay (no scene/canvas dependencies — works in any dressed world, evidence-friendly).
// Keys: Space = pause, 1/2/3 = 1×/4×/max (legacy Input, guarded like Fas3CameraRig).
using System;
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3TimeControls : MonoBehaviour
    {
        public const float BaseTps = 24f;   // 1× — one sim year ≈ 9 s (YEAR ≈ 216 ticks)
        public const float MaxTps = 999f;   // ▶▶ — compute-bound, the driver takes what Jint gives

        public Fas3SimDriver driver;        // auto-found if left empty
        public float EffectiveTps { get; private set; }   // measured, not requested — the honest number
        public int SpeedIndex { get; private set; } = 1;  // 0=paused 1=1× 2=4× 3=max

        int _lastTick; float _windowStart;

        public void SetPause(bool p)
        {
            var d = Driver(); if (d == null) return;
            d.paused = p;
            if (p) SpeedIndex = 0;
            else if (SpeedIndex == 0) SetSpeed(1);
        }

        /// <summary>1 = 1× (24 t/s), 2 = 4× (96 t/s), 3 = uncapped.</summary>
        public void SetSpeed(int idx)
        {
            var d = Driver(); if (d == null) return;
            d.paused = false;
            d.ticksPerSecond = idx == 3 ? MaxTps : idx == 2 ? BaseTps * 4f : BaseTps;
            SpeedIndex = Mathf.Clamp(idx, 1, 3);
        }

        Fas3SimDriver Driver()
        {
            if (driver == null) driver = FindAnyObjectByType<Fas3SimDriver>();
            return driver;
        }

        void Update()
        {
            var d = Driver(); if (d == null) return;
            // measured ticks/s over a 0.5 s window — what the machine actually delivers
            float t = Time.unscaledTime;
            if (t - _windowStart >= 0.5f)
            {
                if (_windowStart > 0f) EffectiveTps = (d.Tick - _lastTick) / (t - _windowStart);
                _windowStart = t; _lastTick = d.Tick;
            }
            try
            {
                if (Input.GetKeyDown(KeyCode.Space)) SetPause(!d.paused);
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(3);
            }
            catch (Exception) { /* new Input System only — buttons still work */ }
        }

        void OnGUI()
        {
            var d = Driver(); if (d == null) return;
            const int w = 332, h = 58;
            var r = new Rect(12, 12, w, h);
            GUI.Box(r, GUIContent.none);
            GUI.Label(new Rect(r.x + 10, r.y + 4, w - 20, 22),
                $"År {d.Year}   tick {d.Tick}   {EffectiveTps:F0} ticks/s" + (d.paused ? "   ❚❚ PAUS" : ""));
            string[] labels = { "❚❚", "1×", "4×", "▶▶" };
            for (int i = 0; i < 4; i++)
            {
                bool active = d.paused ? i == 0 : SpeedIndex == i;
                var br = new Rect(r.x + 10 + i * 78, r.y + 28, 70, 24);
                GUI.backgroundColor = active ? new Color(1f, 0.85f, 0.4f) : Color.white;
                if (GUI.Button(br, labels[i]))
                {
                    if (i == 0) SetPause(!d.paused);
                    else SetSpeed(i);
                }
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
