// EMERGENCE — headless compile driver (removes the per-iteration "focus Unity + eyeball the Console").
//
// The window-click was only ever needed because Unity's Directory Monitoring DEFERS importing NEW .cs
// files until the editor regains focus. A resident, already-compiled editor script can force it:
// on Reports/RUN_COMPILE.trigger this calls AssetDatabase.Refresh(ForceUpdate) + RequestScriptCompilation(),
// which imports + compiles WITHOUT focus, then captures every compiler error/warning into
// Reports/compile-report.txt and a verdict into Reports/COMPILE_DONE.txt. The Studio Director reads
// those over the bridge — so nobody has to click or watch the Console again.
//
// Lives in its OWN assembly (Emergence.AutoCompile.Editor.asmdef) so a compile error in the main
// editor assembly can't disable the very runner that recompiles the fix.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Emergence.AutoCompileTool
{
    [InitializeOnLoad]
    public static class AutoCompile
    {
        // N2 (Fas 6 G-review r2, D-163): the DECLARED warning baseline — 16 pre-existing
        // third-party/legacy warnings (15 Polyart CS0414/CS0618/UAC1001 + 1 Fas2MoveProbe CS0414),
        // audited 2026-07-23. A full recompile reports all 16; an incremental one may report fewer.
        // Deviations ABOVE baseline are findings; update the constant only with a fresh audit.
        const int WarningsBaseline = 16;

        static double _next, _armedAt;
        static bool _armed, _sawFinish;
        static readonly List<string> _errors = new List<string>();
        static readonly List<string> _warnings = new List<string>();

        static string Trigger => Path.Combine(Application.dataPath, "..", "Reports", "RUN_COMPILE.trigger");
        static string Done    => Path.Combine(Application.dataPath, "..", "Reports", "COMPILE_DONE.txt");
        static string Report  => Path.Combine(Application.dataPath, "..", "Reports", "compile-report.txt");

        static AutoCompile()
        {
            EditorApplication.update += Tick;
            CompilationPipeline.assemblyCompilationFinished += OnAsm;
            CompilationPipeline.compilationFinished += OnDone;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            try
            {
                if (!_armed && File.Exists(Trigger))
                {
                    File.Delete(Trigger);
                    _armed = true; _sawFinish = false; _armedAt = EditorApplication.timeSinceStartup;
                    _errors.Clear(); _warnings.Clear();
                    Directory.CreateDirectory(Path.GetDirectoryName(Done));
                    File.WriteAllText(Done, "RUNNING " + DateTime.Now.ToString("HH:mm:ss") + "\n");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);   // import new/changed .cs without focus
                    CompilationPipeline.RequestScriptCompilation();          // force the compile
                    return;
                }
                // "nothing changed" path: no compile fires => no domain reload, so these statics survive to time out
                if (_armed && !_sawFinish && !EditorApplication.isCompiling
                    && EditorApplication.timeSinceStartup - _armedAt > 5.0)
                    Finalize("CLEAN (no compilation needed)");
            }
            catch (Exception e) { try { File.WriteAllText(Done, "ERROR " + e.Message + "\n"); } catch {} _armed = false; }
        }

        static void OnAsm(string asmPath, CompilerMessage[] msgs)
        {
            if (!_armed || msgs == null) return;
            foreach (var m in msgs)
            {
                var line = $"{Path.GetFileName(asmPath)}: {m.file}({m.line}) {m.message}";
                if (m.type == CompilerMessageType.Error) _errors.Add(line);
                else if (m.type == CompilerMessageType.Warning) _warnings.Add(line);
            }
        }

        static void OnDone(object _)
        {
            if (!_armed) return;
            _sawFinish = true;
            Finalize(_errors.Count == 0 ? "CLEAN" : "FAIL");
        }

        static void Finalize(string verdict)
        {
            _armed = false;
            var sb = new StringBuilder();
            sb.AppendLine($"COMPILE {verdict} — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"errors={_errors.Count} warnings={_warnings.Count} warningsBaseline={WarningsBaseline}");
            if (_warnings.Count > WarningsBaseline)
                sb.AppendLine($"NOTE: warning count EXCEEDS the declared baseline ({WarningsBaseline}) — inspect ## WARNINGS before accepting (N2, D-163). (An incremental compile honestly reports fewer.)");
            sb.AppendLine();
            if (_errors.Count > 0)   { sb.AppendLine("## ERRORS");   foreach (var e in _errors)   sb.AppendLine("  " + e); sb.AppendLine(); }
            if (_warnings.Count > 0) { sb.AppendLine("## WARNINGS"); foreach (var w in _warnings) sb.AppendLine("  " + w); }
            try { File.WriteAllText(Report, sb.ToString()); } catch {}
            try { File.WriteAllText(Done, $"DONE {DateTime.Now:HH:mm:ss} verdict={verdict} errors={_errors.Count} warnings={_warnings.Count}\nsee Reports/compile-report.txt\n"); } catch {}
            Debug.Log($"[AutoCompile] {verdict} errors={_errors.Count} warnings={_warnings.Count}");
        }
    }
}
#endif
