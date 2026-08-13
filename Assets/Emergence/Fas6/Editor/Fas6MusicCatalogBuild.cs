// EMERGENCE — FAS 6: build the MUSIC CATALOG asset from the two owned packs.
// Menu: Emergence/Fas6/BUILD MUSIC CATALOG.  Headless: drop Reports/RUN_MUSICCAT.trigger.
//
// The role split (ambient bed vs action cue) is judged BY TITLE and every viking entry is stamped
// earCheck=true, because a title is not a listening. Nothing here is canon until a human has heard
// it; the flag is how the catalog says so out loud.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Emergence.Runtime;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas6MusicCatalogBuild
    {
        const string AssetPath = "Assets/Emergence/Resources/Fas6MusicCatalog.asset";
        const string VikingDir = "Assets/Viking Music Pack/Loops";
        const string MedievalDir = "Assets/10 Fantasy Medieval Ambient Tracks/Loops";
        const string Report = "Reports/fas6-music-catalog.txt";

        // ---- THE MEASURED SPLIT (2026-08-13) ----
        // The first pass assigned action/ambient BY TITLE. That was a guess, and it was wrong twice.
        // Every loop was then measured (RMS loudness, crest factor, 40-200 Hz share, onset density)
        // and ranked by a drive index = z(loudness) + z(low-band) - z(crest): how loud, how much
        // drum weight, how compressed. The four highest are the action pool.
        //
        // The measurement DISAGREED with the title on two tracks, and the measurement wins here:
        //   "Northern Lights"     — the highest low-band energy of all 16 (21.4%). Not a calm night.
        //   "Frostbound Horizons" — second loudest of all 16 (-19.1 dBFS). Not a cold bed.
        //   "Throne of the Fjords"— the title's most action-sounding name, and it measures 4 dB
        //                           quieter with half the drum weight. It drops OUT of the pool.
        // Those three carry earCheck: they are exactly the tracks where a human ear is worth more
        // than either the title or the number, so they are flagged and nothing else is.
        //
        // Honest limit of the method: the onset-density metric came out nearly flat (222-281 per
        // minute across all 16), so it discriminated nothing and does not enter the index. Loudness,
        // low-band share and crest factor did all the work.
        static readonly string[] VikingAction = { "Echoes of Valhalla", "Frostbound Horizons", "Northern Lights", "Saga of the Sea Wolves" };
        /// <summary>Tracks where the measurement and the title disagree — a human ear settles these.</summary>
        static readonly string[] EarCheck = { "Northern Lights", "Frostbound Horizons", "Throne of the Fjords" };

        /// <summary>Measured RMS loudness per track, and the dB to add to reach the shared target
        /// (-24 dBFS). Measured from the middle 90 s of each loop, 2026-08-13.</summary>
        public const float TargetDbfs = -24f;
        static readonly Dictionary<string, float> GainDb = new Dictionary<string, float>
        {
            { "Echoes of Valhalla",   -4.4f },   // -19.6 dBFS  low 17.5%  drive  4.21
            { "Frostbound Horizons",  -4.9f },   // -19.1       low 18.8   drive  4.17
            { "Northern Lights",      -3.5f },   // -20.5       low 21.4   drive  3.60
            { "Saga of the Sea Wolves", -2.6f }, // -21.4       low 15.5   drive  1.79
            { "Throne of the Fjords",  -0.3f },  // -23.7       low 10.7   drive  1.31
            { "Winds of Valor",        2.0f },   // -26.0       low 16.7   drive  0.50
            { "Emberlight",            3.5f },   // -27.5       low 10.4   drive  0.21
            { "Elven Dawn",            3.6f },   // -27.6       low  8.3   drive -0.43
            { "Moonspire",             9.0f },   // -33.0       low 20.5   drive -0.63
            { "Sorrow’s Edge",   -1.0f },   // -23.0       low  0.2   drive -1.02
            { "Darkwood Path",         5.7f },   // -29.7       low 15.0   drive -1.05
            { "Throne of Storms",      3.4f },   // -27.4       low  5.7   drive -1.84
            { "Odin's Whisper",        8.4f },   // -32.4       low  8.7   drive -2.26
            { "Silverbrook",           0.2f },   // -24.2       low  2.5   drive -2.42
            { "Mystic Grove",          2.2f },   // -26.2       low  6.3   drive -3.00
            { "Frostbound",            5.3f },   // -29.3       low  1.7   drive -3.13
        };

        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_MUSICCAT.trigger");

        static Fas6MusicCatalogBuild() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas6/BUILD MUSIC CATALOG")]
        public static void RunMenu() { Debug.Log(Build()); }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                string body = Build();
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Report, body);
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "Reports", "MUSICCAT_DONE.txt"),
                                  "DONE " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Debug.Log("[Fas6MusicCatalogBuild] -> " + Report);
            }
            catch (Exception e) { Debug.LogWarning("[Fas6MusicCatalogBuild] " + e.Message); }
        }

        /// <summary>"Loops/3. Saga of the Sea Wolves.wav" -> "Saga of the Sea Wolves".</summary>
        public static string KeyOf(string assetPath)
        {
            string n = Path.GetFileNameWithoutExtension(assetPath);
            int dot = n.IndexOf('.');
            if (dot >= 0 && dot <= 2) n = n.Substring(dot + 1);
            return n.Trim();
        }

        public static string Build()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EMERGENCE — FAS 6 music catalog");
            sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("source: the two OWNED packs' Loops folders (loops, not one-shot tracks — a score must not end)");
            sb.AppendLine();

            var cat = AssetDatabase.LoadAssetAtPath<Fas6MusicCatalog>(AssetPath);
            if (cat == null)
            {
                cat = ScriptableObject.CreateInstance<Fas6MusicCatalog>();
                Directory.CreateDirectory("Assets/Emergence/Resources");
                AssetDatabase.CreateAsset(cat, AssetPath);
            }
            cat.entries.Clear();

            Action<string, string> scan = (dir, pack) =>
            {
                if (!Directory.Exists(dir)) { sb.AppendLine("MISSING pack folder: " + dir); return; }
                var files = Directory.GetFiles(dir, "*.wav").Concat(Directory.GetFiles(dir, "*.ogg"))
                                     .Concat(Directory.GetFiles(dir, "*.mp3")).OrderBy(f => f, StringComparer.Ordinal).ToArray();
                foreach (var f in files)
                {
                    string ap = f.Replace('\\', '/');
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ap);
                    string key = KeyOf(ap);
                    bool action = Array.IndexOf(VikingAction, key) >= 0;
                    float gdb; if (!GainDb.TryGetValue(key, out gdb)) gdb = 0f;
                    cat.entries.Add(new Fas6MusicCatalog.Entry
                    {
                        key = key, pack = pack,
                        role = action ? Fas6MusicCatalog.Role.Action : Fas6MusicCatalog.Role.Ambient,
                        earCheck = Array.IndexOf(EarCheck, key) >= 0,   // measurement vs title disagreed here
                        gainDb = gdb,
                        clip = clip
                    });
                    sb.AppendLine(string.Format("  {0,-10} {1,-8} {2,-24} {3,6} dB  {4}{5}", pack, action ? "ACTION" : "ambient",
                                                key, gdb.ToString("F1"),
                                                clip != null ? "clip OK (" + clip.length.ToString("F0") + " s)" : "CLIP MISSING",
                                                Array.IndexOf(EarCheck, key) >= 0 ? "   << EAR CHECK" : ""));
                }
            };
            scan(VikingDir, "viking");
            scan(MedievalDir, "medieval");

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Fas6MusicCatalog.Invalidate();

            sb.AppendLine();
            sb.AppendLine("entries: " + cat.Count + "   action: " + cat.Pool(Fas6MusicCatalog.Role.Action).Count +
                          "   ambient: " + cat.Pool(Fas6MusicCatalog.Role.Ambient).Count +
                          "   awaiting the ear: " + cat.EarCheckCount);
            sb.AppendLine();
            sb.AppendLine("ERA TABLE (the director's declared opinion — every name must exist above):");
            for (int i = 0; i < Fas6MusicDirector.EraTrack.Length; i++)
            {
                string t = Fas6MusicDirector.EraTrack[i];
                sb.AppendLine("  era " + i + " (" + WorldEras.Name(i) + ")  -> " + t + (cat.Clip(t) != null ? "   [found]" : "   [!! NOT IN CATALOG]"));
            }
            sb.AppendLine("  season winter          -> " + Fas6MusicDirector.WinterTrack + (cat.Clip(Fas6MusicDirector.WinterTrack) != null ? "   [found]" : "   [!! NOT IN CATALOG]"));
            sb.AppendLine();
            sb.AppendLine("LEVEL MATCH: every track is gained to " + TargetDbfs + " dBFS. The two packs are mastered 14 dB");
            sb.AppendLine("  apart, so without this the score would jump audibly the moment drama pulled a viking cue in.");
            sb.AppendLine();
            sb.AppendLine("ON PATRIK (ear check, THREE tracks only): the split is now MEASURED, not guessed — but the");
            sb.AppendLine("  measurement disagreed with the title on these, and an ear is worth more than either:");
            foreach (var k in EarCheck) sb.AppendLine("    - " + k + (Array.IndexOf(VikingAction, k) >= 0 ? "  (now ACTION; the title reads ambient)" : "  (now AMBIENT; the title reads action)"));
            sb.AppendLine("  Move one and rebuild — the law does not change, only the pool.");
            return sb.ToString();
        }
    }
}
#endif
