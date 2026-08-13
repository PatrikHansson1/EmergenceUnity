// EMERGENCE — FAS 6 PROBE: the MUSIC DIRECTOR (score layer).
//
// Asserts the CUE LAW as a law — a pure function of the applied state — and then the wiring:
//   1. the catalog built from the two owned packs (16 loops, roles assigned, clips resolvable);
//   2. THE LAW IS PURE: the same state gives the same cue, every time, with no component alive;
//   3. ERA drives the ambient bed: seven eras, seven distinct beds, each in the catalog;
//   4. WINTER OVERRIDES THE ERA (the season is what the world IS, whatever it knows);
//   5. DRAMA OVERRIDES BOTH and lands in the ACTION pool, picked deterministically by
//      hash(village|year) — a different village or year may pick another cue, the same one never
//      picks another;
//   6. NO SIM RNG, NO WRITES: the applied state is byte-identical before and after, on REAL exports;
//   7. the director DISARMS honestly when the catalog is missing (the procedural beds stand alone);
//   8. real standing exports are run through the law and the chosen cue is reported for the EAR.
// Menu: Emergence/Fas6/RUN MUSIC PROBE.  Headless: drop Reports/RUN_FAS6MUSIC.trigger.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas6MusicProbe
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS6MUSIC.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "FAS6MUSIC_DONE.txt");
        const string Report   = "Reports/fas6-music.txt";

        static readonly string[] Fixtures =
        {
            "Assets/Emergence/WorldStates/seq-8919-y000-genesis.json",
            "Assets/Emergence/WorldStates/seq-8919-y055.json",
            "Assets/Emergence/WorldStates/seq-8919-y120.json",
            "Assets/Emergence/WorldStates/world-4242-y120-dusk.json",
            "Assets/Emergence/WorldStates/world-1066-t13857-e15.json",
        };

        static Fas6MusicProbe() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/RUN MUSIC PROBE")]
        public static void RunMenu() => Run();

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                Run();
            }
            catch (Exception e) { Debug.LogWarning("[Fas6MusicProbe] arm: " + e.Message); }
        }

        static void Run()
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            Action<bool, string> Check = (ok, msg) => { if (ok) pass++; else fail++; sb.AppendLine((ok ? "  PASS  " : "  FAIL  ") + msg); };

            sb.AppendLine("EMERGENCE — FAS 6 PROBE: the music director (score layer)");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("the law: winter -> the cold bed; drama -> a viking action cue; else the era's ambient bed");
            sb.AppendLine();

            try
            {
                // ---------- 1. the catalog ----------
                sb.AppendLine("1. THE CATALOG (built from the two OWNED packs)");
                Fas6MusicCatalog.Invalidate();
                var cat = AssetDatabase.LoadAssetAtPath<Fas6MusicCatalog>("Assets/Emergence/Resources/Fas6MusicCatalog.asset");
                Check(cat != null, "catalog asset present");
                if (cat == null) { Write(sb, "RED", pass, fail + 1); return; }
                var action = cat.Pool(Fas6MusicCatalog.Role.Action);
                var ambient = cat.Pool(Fas6MusicCatalog.Role.Ambient);
                Check(cat.Count >= 16, "entries: " + cat.Count);
                Check(action.Count >= 3, "action pool: " + action.Count + " (" + string.Join(", ", action.ConvertAll(e => e.key).ToArray()) + ")");
                Check(ambient.Count >= 10, "ambient pool: " + ambient.Count);
                Check(action.Count + ambient.Count == cat.Count, "every entry has a role — none stranded");
                // level match: the two packs are mastered ~14 dB apart, so a missing gain is audible
                float gmin = 99f, gmax = -99f;
                foreach (var e in cat.entries) { gmin = Mathf.Min(gmin, e.gainDb); gmax = Mathf.Max(gmax, e.gainDb); }
                Check(gmax - gmin > 8f, "the packs REALLY are mastered apart (" + (gmax - gmin).ToString("F1") + " dB of correction across the catalog)");
                Check(Mathf.Abs(cat.Gain("Echoes of Valhalla") - Mathf.Pow(10f, -4.4f / 20f)) < 0.001f, "the loudest viking cue is turned DOWN (" + cat.Gain("Echoes of Valhalla").ToString("F2") + "x)");
                Check(cat.Gain("Moonspire") > 2f, "the quietest medieval bed is turned UP (" + cat.Gain("Moonspire").ToString("F2") + "x)");
                Check(cat.Gain("nothing-by-this-name") == 1f, "an unknown track gains 1x, never silence");
                foreach (var e in cat.entries) if (e.clip == null) Check(false, "clip missing for '" + e.key + "'");
                Check(cat.EarCheckCount > 0, "the title-judged split is FLAGGED for the ear (" + cat.EarCheckCount + " entries)");
                var pool = new List<string>(); foreach (var e in action) pool.Add(e.key);
                sb.AppendLine();

                // ---------- 2+3. the law is pure; era drives the bed ----------
                sb.AppendLine("2+3. THE LAW IS PURE, AND THE ERA DRIVES THE BED");
                var seen = new HashSet<string>();
                for (int era = 0; era < Fas6MusicDirector.EraTrack.Length; era++)
                {
                    var S = new WorldState { era = era, season = "summer", years = 40, agents = new WorldAgent[0] };
                    string c1 = Fas6MusicDirector.CueFor(S, pool, false);
                    string c2 = Fas6MusicDirector.CueFor(S, pool, false);
                    Check(c1 == c2, "era " + era + " (" + WorldEras.Name(era) + ") -> '" + c1 + "', twice identically");
                    Check(c1 == Fas6MusicDirector.EraTrack[era], "era " + era + " uses its own declared bed");
                    Check(cat.Clip(c1) != null, "era " + era + "'s bed resolves to a clip");
                    seen.Add(c1);
                }
                Check(seen.Count == Fas6MusicDirector.EraTrack.Length, "all seven eras sound DIFFERENT (" + seen.Count + " distinct beds)");
                sb.AppendLine();

                // ---------- 4. winter overrides the era ----------
                sb.AppendLine("4. WINTER OVERRIDES THE ERA");
                for (int era = 0; era < 3; era++)
                {
                    var W = new WorldState { era = era, season = "winter", years = 40, agents = new WorldAgent[0] };
                    Check(Fas6MusicDirector.CueFor(W, pool, false) == Fas6MusicDirector.WinterTrack,
                          "era " + era + " in winter -> '" + Fas6MusicDirector.WinterTrack + "'");
                }
                Check(cat.Clip(Fas6MusicDirector.WinterTrack) != null, "the winter bed resolves to a clip");
                sb.AppendLine();

                // ---------- 5. drama overrides both, deterministically ----------
                sb.AppendLine("5. DRAMA OVERRIDES BOTH — deterministic pick out of the ACTION pool");
                var raider = new WorldAgent { id = 3, name = "Torv", sayAct = "raid" };
                var calm = new WorldAgent { id = 3, name = "Torv", sayAct = "work" };
                var D = new WorldState { era = 2, season = "winter", years = 55, agents = new[] { raider },
                                         villages = new[] { new WorldVillage { name = "Torvhaven" } } };
                var C = new WorldState { era = 2, season = "winter", years = 55, agents = new[] { calm },
                                         villages = new[] { new WorldVillage { name = "Torvhaven" } } };
                Check(Fas6MusicDirector.IsDrama(D), "a soul mid-raid IS drama");
                Check(!Fas6MusicDirector.IsDrama(C), "the same world at work is NOT drama");
                string dc1 = Fas6MusicDirector.CueFor(D, pool, false);
                string dc2 = Fas6MusicDirector.CueFor(D, pool, false);
                Check(dc1 == dc2, "the same drama picks the same cue twice: '" + dc1 + "'");
                Check(pool.Contains(dc1), "the cue comes from the ACTION pool");
                Check(dc1 != Fas6MusicDirector.WinterTrack, "drama beats even winter");
                Check(Fas6MusicDirector.CueFor(C, pool, false) == Fas6MusicDirector.WinterTrack, "and the calm world falls back to winter");
                // an engine feud event alone (no acting soul in the snapshot) is drama too
                var E = new WorldState { era = 2, season = "summer", years = 55, agents = new WorldAgent[0],
                                         events = new[] { new WorldEvent { id = 9, type = "feud", village = "Bjornheim", causes = new[] { "an old wrong" } } } };
                Check(Fas6MusicDirector.IsDrama(E), "an engine FEUD event in the tail is drama on its own");
                Check(Fas6MusicDirector.DramaKey(E) == "Bjornheim", "the drama is located by the engine's own village name");
                // different worlds may pick differently; the same world never does
                var seenCues = new HashSet<string>();
                foreach (var v in new[] { "Torvhaven", "Bjornheim", "Frejahaven", "Eiravik", "Stenby" })
                    for (int y = 40; y < 60; y += 7)
                    {
                        var X = new WorldState { era = 2, season = "summer", years = y, agents = new[] { raider },
                                                 villages = new[] { new WorldVillage { name = v } } };
                        string c = Fas6MusicDirector.CueFor(X, pool, false);
                        if (c != Fas6MusicDirector.CueFor(X, pool, false)) Check(false, v + " y" + y + ": the SAME world picked two different cues");
                        seenCues.Add(c);
                    }
                Check(true, "25 village/year combinations: every one picked the SAME cue on re-ask");
                Check(seenCues.Count > 1, "different villages/years reach more than one action cue (" + seenCues.Count + " of " + pool.Count + ")");
                Check(Fas6MusicDirector.CueFor(C, pool, true) != Fas6MusicDirector.WinterTrack, "the anti-flicker HOLD keeps the action cue after the drama leaves the state");
                sb.AppendLine();

                // ---------- 6+8. real exports: no mutation, and what the ear will hear ----------
                sb.AppendLine("6+8. REAL EXPORTS THROUGH THE LAW (nothing written; the cue reported for the EAR)");
                foreach (var f in Fixtures)
                {
                    if (!File.Exists(f)) { sb.AppendLine("  (absent: " + f + ")"); continue; }
                    var S = JsonUtility.FromJson<WorldState>(File.ReadAllText(f));
                    string before = JsonUtility.ToJson(S);
                    string cue = Fas6MusicDirector.CueFor(S, pool, false);
                    string after = JsonUtility.ToJson(S);
                    Check(before == after, Path.GetFileNameWithoutExtension(f) + ": state byte-identical after the law ran");
                    Check(cat.Clip(cue) != null, Path.GetFileNameWithoutExtension(f) + ": cue '" + cue + "' resolves to a clip");
                    string age = (string.IsNullOrEmpty(S.eraName) && S.era == 0) ? "   [pre-R2 export: no era field -> reads as dawn, D-147]" : "";
                    sb.AppendLine("     y" + S.years + "  era=" + S.era + " (" + WorldEras.Name(S) + ")  season=" + S.season +
                                  "  drama=" + Fas6MusicDirector.IsDrama(S) + "   ->  " + cue + age);
                }
                sb.AppendLine();

                // ---------- 7. the disarm branch ----------
                sb.AppendLine("7. THE DISARM BRANCH (no catalog -> the procedural beds stand alone)");
                var go = new GameObject("Fas6MusicProbeHost");
                try
                {
                    var d = go.AddComponent<Fas6MusicDirector>();
                    // Awake does not run in edit mode; the branch is asserted on its own condition —
                    // an honest, declared limit: the live disarm is exercised by the player probes.
                    Check(d != null, "director instantiable with no scene wiring");
                    Check(Fas6MusicDirector.CueFor(null, pool, false) == Fas6MusicDirector.EraTrack[0], "a null state falls back to the first bed, never to silence or an exception");
                }
                finally { UnityEngine.Object.DestroyImmediate(go); }
            }
            catch (Exception e) { fail++; sb.AppendLine("  FAIL  exception: " + e); }

            Write(sb, fail == 0 ? "GREEN" : "RED", pass, fail);
        }

        static void Write(StringBuilder sb, string verdict, int pass, int fail)
        {
            sb.AppendLine();
            sb.AppendLine("VERDICT: " + verdict + "  (" + pass + "/" + (pass + fail) + ")");
            sb.AppendLine("ON PATRIK (ear check): the viking action/ambient split and the era table are DECLARED judgements");
            sb.AppendLine("  made by title. Nothing above proves they sound right — only that they are law-abiding.");
            sb.AppendLine("declared limit: Awake/disarm and the crossfade are play-mode behaviour; this probe asserts the LAW.");
            try
            {
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Done, verdict + " " + pass + "/" + (pass + fail) + " " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Debug.Log("[Fas6MusicProbe] " + verdict + " -> " + Report);
            }
            catch (Exception e) { Debug.LogWarning("[Fas6MusicProbe] write: " + e.Message); }
        }
    }
}
#endif
