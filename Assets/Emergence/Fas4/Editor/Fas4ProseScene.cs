// EMERGENCE — FAS 4 PROSE SCENE SETUP (FAS4-PROSE-DIRECTOR-ORDER §3).
//
// Raises the LLM half of the prose wiring in the live scene: an LLM node bound to the studio's
// model, one LLMCharacter, and Fas4ProseDirector pointed at it with useProse ON. Kept OUT of
// Fas3Onboarding on purpose: the onboarding raises the DIRECTOR always (it costs nothing without a
// model), but a multi-gigabyte model must never load itself just because someone pressed play.
// This is the deliberate switch — a menu item, or a headless RUN_FAS4PROSESCENE trigger.
//
// Model discovery: LLMUnity keeps downloaded models in its own global store
// (%APPDATA%/LLMUnity/models), NOT in the project — so nothing here depends on a gguf living in
// StreamingAssets, and the 3.9 GB backend stays gitignored (D-167). If no model is found the setup
// reports it honestly and leaves useProse OFF; the rule-based why-line stands and nothing breaks.
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using LLMUnity;
using Emergence.Fas4;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class Fas4ProseScene
    {
        static double _next;
        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_FAS4PROSESCENE.trigger");
        const string Report = "Reports/fas4-prose-scene.txt";

        /// <summary>The studio's chronicle model (D-198). Any 1-3B instruct gguf works; this is the one chosen.</summary>
        public const string ModelMatch = "llama-3.2-1b";

        static Fas4ProseScene() { EditorApplication.update += Tick; }

        [MenuItem("Emergence/Fas4/SETUP PROSE SCENE (LLM)")]
        public static void RunMenu() { var sb = new StringBuilder(); Setup(sb, true); Debug.Log(sb.ToString()); }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 0.25;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Trigger)) return;
                File.Delete(Trigger);
                var sb = new StringBuilder();
                sb.AppendLine("EMERGENCE — FAS 4 prose scene setup (order §3)");
                sb.AppendLine("generated " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Setup(sb, true);
                Directory.CreateDirectory("Reports");
                File.WriteAllText(Report, sb.ToString());
                File.WriteAllText(Path.Combine(Application.dataPath, "..", "Reports", "FAS4PROSESCENE_DONE.txt"),
                                  "DONE " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                Debug.Log("[Fas4ProseScene] -> " + Report);
            }
            catch (Exception e) { Debug.LogWarning("[Fas4ProseScene] " + e.Message); }
        }

        /// <summary>Find the studio's model in LLMUnity's global store. Returns null when none is present.</summary>
        public static string FindModel()
        {
            try
            {
                string dir = LLMUnitySetup.modelDownloadPath;
                if (!Directory.Exists(dir)) return null;
                var all = Directory.GetFiles(dir, "*.gguf", SearchOption.AllDirectories);
                var chosen = all.FirstOrDefault(f => Path.GetFileName(f).ToLowerInvariant().Contains(ModelMatch));
                return chosen ?? all.FirstOrDefault();
            }
            catch { return null; }
        }

        /// <summary>Raise (or find) LLM + LLMCharacter + Fas4ProseDirector and bind them. Idempotent.</summary>
        public static Fas4ProseDirector Setup(StringBuilder sb, bool turnProseOn)
        {
            var director = UnityEngine.Object.FindAnyObjectByType<Fas4ProseDirector>();
            if (director == null)
                director = new GameObject("Fas4ProseDirector").AddComponent<Fas4ProseDirector>();

            string model = FindModel();
            sb.AppendLine("model store : " + LLMUnitySetup.modelDownloadPath);
            sb.AppendLine("model found : " + (model ?? "NONE — useProse stays OFF, the rule-based why-line stands"));
            if (model == null) { director.useProse = false; return director; }

            var llm = UnityEngine.Object.FindAnyObjectByType<LLM>();
            if (llm == null) llm = new GameObject("EmergenceLLM").AddComponent<LLM>();
            if (string.IsNullOrEmpty(llm.model)) llm.SetModel(model);

            var ch = UnityEngine.Object.FindAnyObjectByType<LLMCharacter>();
            if (ch == null) ch = new GameObject("ChronicleNarrator").AddComponent<LLMCharacter>();
            ch.llm = llm;
            ch.systemPrompt = Fas4ProseDirector.SystemPrompt;
            ch.temperature = 0f;          // greedy — the reproducibility law (D-198 §3)
            ch.numPredict = director.maxTokens;
            ch.save = "";                 // no chat history on disk: every entry stands alone
            // v3.0.3 note: the director already calls Chat(..., addToHistory:false), so one
            // entry's narration can never colour the next — the book has no memory of itself.

            director.character = ch;
            director.useProse = turnProseOn;

            sb.AppendLine("LLM node    : " + llm.gameObject.name + "  model=" + Path.GetFileName(llm.model));
            sb.AppendLine("character   : " + ch.gameObject.name + "  temp=" + ch.temperature + " numPredict=" + ch.numPredict + " save='" + ch.save + "'");
            sb.AppendLine("director    : useProse=" + director.useProse + "  character bound=" + (director.character != null));
            return director;
        }
    }
}
#endif
