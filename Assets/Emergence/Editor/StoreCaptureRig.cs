// EMERGENCE — STORE CAPTURE RIG (Fas 7 ink. 4, D-183: "capture-riggens BILDER för de tre fröna").
//
// Captures the three Signature-Moment stills for the Steam page from REAL exported worlds
// (tick-precise driver-parity exports via Tools/export-world-tick.js — only real moments from
// real seeds, STEAM-CAMPAIGN C2; nothing staged, nothing moved):
//   A "first-fire"  world-900913-t2880-e15.json — Ask (64.49,14.40) one tile from the world's
//                   FIRST burning fire (64.98,13.50) — his own invention, twelve years on.
//   B "flame-chain" world-900913-t12892-e15.json — Torv II (82.9,20.0) & Embla II (83.1,21.5),
//                   the y90 taught-tick ("Torv II taught Embla II the secret of Ask's blaze").
//   C "rekindler"   world-1066-t13857-e15.json — Falk (52.5,35.3), 'experimenting', the
//                   rediscovery tick ("Falk the Rekindler has rediscovered Embla's the mural").
//
// Grammar: full WorldDresser dressing + the LOCKED look (EmergenceLightRig/EmergencePostStack,
// DUSK = the signature "blue world, one warm point", D-114/115b) + EYE-HEIGHT cinematic candidates
// (store shots are documentary stills, not probe evidence — the EvidenceFraming crane law is for
// proofs; here the camera walks a ring at human height and the human eye picks the frame, D-008).
// 8 compass directions x 2 distances per shot, 2560x1440, magenta-counted. No UI, no labels —
// captions are composited outside (the caption pattern owns the text layer).
// Headless: drop Reports/RUN_STORECAP.trigger  ->  Reports/store-cap/<shot>-<n>.png + manifest.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class StoreCaptureRig
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_STORECAP.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "STORECAP_DONE.txt");
        const string OutDir   = "Reports/store-cap";
        const int W = 2560, H = 1440;

        class Shot
        {
            public string name, json, season;
            public Vector2 subject;          // sim coords of the PRIMARY subject (the soul)
            public Vector2? warm;            // sim coords of the warm point (fire), if any
            public Vector2? second;          // second soul, if any
        }

        static readonly Shot[] Shots =
        {
            new Shot { name = "first-fire",  json = "Assets/Emergence/WorldStates/world-900913-t2880-e15.json",
                       subject = new Vector2(64.49f, 14.40f), warm = new Vector2(64.98f, 13.50f), season = "winter" },
            new Shot { name = "flame-chain", json = "Assets/Emergence/WorldStates/world-900913-t12892-e15.json",
                       subject = new Vector2(82.9f, 20.0f), second = new Vector2(83.1f, 21.5f), season = "summer" },
            new Shot { name = "rekindler",   json = "Assets/Emergence/WorldStates/world-1066-t13857-e15.json",
                       subject = new Vector2(52.5f, 35.3f), season = "spring" },
        };

        static StoreCaptureRig() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try { if (!File.Exists(Trigger)) return; File.Delete(Trigger); Run(); }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Marketing/RUN STORE CAPTURE RIG")]
        public static void RunMenu() => Run();

        static Vector3 Mapped(WorldState S, float x, float y)
        {
            var w = new Vector3(x * 8f, 0f, (S.H - 1 - y) * 8f);
            var t = Terrain.activeTerrain;
            if (t != null) w.y = t.SampleHeight(w) + t.transform.position.y;
            return w;
        }

        static void Run()
        {
            Directory.CreateDirectory(OutDir);
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — STORE CAPTURE RIG (three Signature Moments, real worlds, dusk look)");
            sb.AppendLine($"generated {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            int worstMagenta = 0; int totalPng = 0;

            foreach (var shot in Shots)
            {
                WorldDresser.Build(shot.json);
                var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(shot.json));
                string season = string.IsNullOrEmpty(S.season) ? shot.season : S.season;
                try { EmergenceLightRig.Apply(season, "dusk"); EmergencePostStack.Apply("dusk"); }
                catch (Exception e) { sb.AppendLine($"[{shot.name}] LOOK EXC: {e.Message}"); }

                // edit-mode particle honesty: fires/smoke are ParticleSystems and never advance in
                // edit mode (the D-158 flame was play-mode-proven) — Simulate() renders each system
                // at a lived-in moment so the warm point has a BODY in the still, not just a light.
                int simulated = 0;
                foreach (var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude))
                { ps.Simulate(4f, true, true); simulated++; }
                sb.AppendLine($"[{shot.name}] particle systems simulated: {simulated}");

                var subj = Mapped(S, shot.subject.x, shot.subject.y);
                Vector3 aim = subj + Vector3.up * 1.1f;
                Vector3 other = subj;
                if (shot.warm.HasValue) other = Mapped(S, shot.warm.Value.x, shot.warm.Value.y);
                if (shot.second.HasValue) other = Mapped(S, shot.second.Value.x, shot.second.Value.y);
                bool pair = shot.warm.HasValue || shot.second.HasValue;
                if (pair) aim = Vector3.Lerp(subj, other, 0.5f) + Vector3.up * 1.0f;

                var cam = Camera.main;
                if (cam == null) { var g = new GameObject("DocCamera") { tag = "MainCamera" }; cam = g.AddComponent<Camera>(); }
                cam.fieldOfView = 45f;

                int n = 0;
                var dirs = new[] { new Vector3(1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,1), new Vector3(-1,0,-1),
                                   new Vector3(1,0,0), new Vector3(-1,0,0), new Vector3(0,0,1), new Vector3(0,0,-1) };
                foreach (var d in dirs)
                {
                    foreach (var r in new[] { 9f, 14f })
                    {
                        // eye-height documentary stance: stand on the terrain like a person would
                        var pos = aim + d.normalized * r;
                        var t = Terrain.activeTerrain;
                        if (t != null) pos.y = t.SampleHeight(pos) + t.transform.position.y + 1.7f;
                        cam.transform.position = pos;
                        cam.transform.LookAt(aim);
                        int magenta = Capture(cam, Path.Combine(OutDir, $"{shot.name}-{n:00}.png"));
                        worstMagenta = Mathf.Max(worstMagenta, magenta); totalPng++;
                        sb.AppendLine($"[{shot.name}-{n:00}] dir=({d.x:0},{d.z:0}) r={r} magenta={magenta}");
                        n++;
                    }
                }
                sb.AppendLine($"[{shot.name}] world tick={S.tick} y(label)={S.years + 1} season={season} agents={S.agents.Length} fires={(S.fires != null ? S.fires.Length : 0)} — {n} candidates");
                sb.AppendLine();
            }

            sb.AppendLine($"worst magenta over {totalPng} captures: {worstMagenta} => {(worstMagenta == 0 ? "GREEN" : "CHECK")}");
            sb.AppendLine("The human eye picks the final frame per shot (D-008); captions composited outside the rig.");
            File.WriteAllText("Reports/store-cap-report.txt", sb.ToString());
            File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} pngs={totalPng} worstMagenta={worstMagenta}\nsee Reports/store-cap-report.txt\n");
            Debug.Log($"[StoreCapture] done pngs={totalPng} worstMagenta={worstMagenta}");
        }

        static int Capture(Camera cam, string path)
        {
            var prev = cam.targetTexture;
            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            cam.targetTexture = prev; RenderTexture.active = null;
            var px = tex.GetPixels32(); int magenta = 0;
            foreach (var c in px) if (c.r > 220 && c.b > 220 && c.g < 80) magenta++;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(rt);
            return magenta;
        }
    }
}
#endif
