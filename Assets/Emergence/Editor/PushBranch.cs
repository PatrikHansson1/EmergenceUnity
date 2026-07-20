// EMERGENCE — push current branch from the Windows side (VM mount has no network)
#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;

namespace Emergence.Editor
{
    public static class PushBranch
    {
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
