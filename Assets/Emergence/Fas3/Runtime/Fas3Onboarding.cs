// EMERGENCE — FAS 3 increment 6 (D-139): the ONBOARDING COMPOSITION — the game's actual start.
//
// Existence condition A in its sharp form: the player lands IN THE BODY, no menu wall. One
// component in the start scene composes the whole opening: honest genesis wilderness (dressed
// from the D-135 cold-start export), the buffer driver ticking LIVE from year 0 (D-136/137 —
// genesis is the first queued snapshot, so the world's truth stands from frame one), the
// presentation clock at 1× (documentary pace; the producer races underneath), the time controls
// (the player's hand from second one), and the gaze (the eye is TAKEN to the first hut, the
// first child — "titta, något föddes", D-134).
//
// This component IS the game start: probes only observe it; a player build ships it as-is.
// D-078 r4: composes readers. Nothing here writes into the sim.
using UnityEngine;

namespace Emergence.Runtime
{
    public sealed class Fas3Onboarding : MonoBehaviour
    {
        public long seed = 8919;
        [Tooltip("-1 = endless (the game). Probes may set a horizon.")]
        public int targetYear = -1;
        public int lookaheadYears = 16;

        public Fas3SimDriver Driver { get; private set; }
        public Fas3WorldRuntime World { get; private set; }
        public Fas3PresentationClock Clock { get; private set; }
        public Fas3TimeControls Controls { get; private set; }

        void Start()
        {
            Application.runInBackground = true;

            World = new GameObject("Fas3WorldRuntime").AddComponent<Fas3WorldRuntime>();

            var dgo = new GameObject("Fas3SimDriver");
            Driver = dgo.AddComponent<Fas3SimDriver>();
            Driver.seed = seed; Driver.bufferMode = true; Driver.targetYear = targetYear; Driver.lookaheadYears = lookaheadYears;

            var cgo = new GameObject("Fas3PresentationClock");
            Clock = cgo.AddComponent<Fas3PresentationClock>();
            Clock.driver = Driver; Clock.world = World;
            Clock.ticksPerSecond = Fas3TimeControls.BaseTps;   // 1× — the documentary opening pace

            var ugo = new GameObject("Fas3TimeControls");
            Controls = ugo.AddComponent<Fas3TimeControls>();
            Controls.driver = Driver; Controls.clock = Clock;

            // the ear (D-141): the bus's first consumer — procedural v0 ambience + milestone chimes
            if (FindAnyObjectByType<Fas3AudioDirector>() == null)
                new GameObject("Fas3AudioDirector").AddComponent<Fas3AudioDirector>();

            // the story (Fas 4 v0): consumer #3 — the chronicle feed, READ's first organ
            if (FindAnyObjectByType<Fas4ChronicleFeed>() == null)
                new GameObject("Fas4ChronicleFeed").AddComponent<Fas4ChronicleFeed>();

            // the eye: rig + gaze on the main camera (idempotent — the scene may already carry them)
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<Fas3CameraRig>() == null) cam.gameObject.AddComponent<Fas3CameraRig>();
                if (cam.GetComponent<Fas3GazeDirector>() == null) cam.gameObject.AddComponent<Fas3GazeDirector>();
            }
        }
    }
}
