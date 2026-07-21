// EMERGENCE — push current branch from the Windows side (VM mount has no network)
#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Emergence.Editor
{
    [InitializeOnLoad]
    public static class PushBranch
    {
        // D-127: headless git — the VM mount cannot even DELETE files (stale index.lock proved it),
        // so commits must run Windows-side. Drop Reports/RUN_GITCOMMIT.trigger (message in
        // Logs/commitmsg-current.txt) -> add -A, commit, push; result in Logs/git-commitpush.log.
        static double _next;
        static string TriggerPath => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GITCOMMIT.trigger");
        static string DonePath    => Path.Combine(Application.dataPath, "..", "Reports", "GITCOMMIT_DONE.txt");

        static PushBranch() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (!File.Exists(TriggerPath)) return;
                File.Delete(TriggerPath);
                var psi = new ProcessStartInfo("cmd.exe",
                    "/c cd /d C:\\Dev\\EmergenceUnity && (if exist .git\\index.lock del /f .git\\index.lock) && " +
                    "git add -A & git commit -F Logs\\commitmsg-current.txt & git push -u origin HEAD " +   // unconditional: push retries even when nothing new to commit
                    "> Logs\\git-commitpush.log 2>&1 & git log --oneline -1 >> Logs\\git-commitpush.log 2>&1")
                { CreateNoWindow = true, UseShellExecute = false };
                Process.Start(psi);
                File.WriteAllText(DonePath, "STARTED " + System.DateTime.Now.ToString("HH:mm:ss") + " — see Logs/git-commitpush.log\n");
                UnityEngine.Debug.Log("[PushBranch] headless commit+push started");
            }
            catch (System.Exception e) { try { File.WriteAllText(DonePath, "ERROR " + e.Message + "\n"); } catch {} }
        }

        [MenuItem("Emergence/Tools/Git Push Current Branch")]
        public static void Run()
        {
            var psi = new ProcessStartInfo("cmd.exe",
                "/c cd /d C:\\Dev\\EmergenceUnity && git push -u origin HEAD > Logs\\git-push.log 2>&1")
            { CreateNoWindow = true, UseShellExecute = false };
            Process.Start(psi);
            UnityEngine.Debug.Log("[PushBranch] push started (see Logs/git-push.log)");
        }

        // TD-034: mount-side git commit times out (slow bridge FS) — commit from Windows.
        // Message comes from a FILE (Logs/commitmsg-current.txt) so cmd.exe never parses <>()
        // in the trailer as shell redirection (that silently ate the first attempt).
        [MenuItem("Emergence/Tools/Git Commit ALL + Push (Windows-side)")]
        public static void CommitAllAndPush()
        {
            var psi = new ProcessStartInfo("cmd.exe",
                "/c cd /d C:\\Dev\\EmergenceUnity && git add -A && git commit -F Logs\\commitmsg-current.txt && git push -u origin HEAD > Logs\\git-commitpush.log 2>&1")
            { CreateNoWindow = true, UseShellExecute = false };
            Process.Start(psi);
            UnityEngine.Debug.Log("[PushBranch] commit+push started (see Logs/git-commitpush.log)");
        }
    }
}
#endif
