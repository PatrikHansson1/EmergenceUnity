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
        // TD-070 session: GitHub 500s on the FULL 34-commit push (multi-MB evidence blobs) — the
        // size hypothesis. This trigger walks the remote forward in 4-commit chunks instead, one
        // small pack at a time; log in Logs/git-push-chunked.log.
        static string ChunkTriggerPath => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GITPUSH_CHUNKED.trigger");
        static string ChunkDonePath    => Path.Combine(Application.dataPath, "..", "Reports", "GITPUSH_CHUNKED_DONE.txt");
        // D-166 B6: run the Dropbox canon cleanup (CLEANUP-MOVES.cmd) Windows-side.
        static string CleanupTriggerPath => Path.Combine(Application.dataPath, "..", "Reports", "RUN_CLEANUP.trigger");
        static string CleanupDonePath    => Path.Combine(Application.dataPath, "..", "Reports", "CLEANUP_DONE.txt");
        // D-166 B2: slim local history (strip purchased packs that block the 2 GB push limit) — Tools/git-slim.ps1.
        static string SlimTriggerPath => Path.Combine(Application.dataPath, "..", "Reports", "RUN_GITSLIM.trigger");
        static string SlimDonePath    => Path.Combine(Application.dataPath, "..", "Reports", "GITSLIM_DONE.txt");

        static PushBranch() { EditorApplication.update += Tick; }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 2.0;
            try
            {
                if (File.Exists(TriggerPath))
                {
                File.Delete(TriggerPath);
                var psi = new ProcessStartInfo("cmd.exe",
                    "/c cd /d C:\\Dev\\EmergenceUnity && (if exist .git\\index.lock del /f .git\\index.lock) && " +
                    // N1 (Fas 7 G-review r2, D-187): TerrainData_generated.asset is a regenerable
                    // WorldDresser.Build byproduct (no committed scene references it) that mutated
                    // silently in TD-089 — untrack once (idempotent), .gitignore keeps it out.
                    "git rm -r -q --cached --ignore-unmatch Assets\\Emergence\\Scenes\\TerrainData_generated.asset Assets\\Emergence\\Scenes\\TerrainData_generated.asset.meta & " +
                    "git add -A & git commit -F Logs\\commitmsg-current.txt & git push -u origin HEAD " +   // unconditional: push retries even when nothing new to commit
                    "> Logs\\git-commitpush.log 2>&1 & git log --oneline -1 >> Logs\\git-commitpush.log 2>&1")
                { CreateNoWindow = true, UseShellExecute = false };
                Process.Start(psi);
                File.WriteAllText(DonePath, "STARTED " + System.DateTime.Now.ToString("HH:mm:ss") + " — see Logs/git-commitpush.log\n");
                UnityEngine.Debug.Log("[PushBranch] headless commit+push started");
                }
            }
            catch (System.Exception e) { try { File.WriteAllText(DonePath, "ERROR " + e.Message + "\n"); } catch {} }

            try
            {
                if (File.Exists(ChunkTriggerPath))
                {
                File.Delete(ChunkTriggerPath);
                // walk origin forward 4 commits at a time (oldest first), then a final full push;
                // -ErrorAction silently ignored by git output — everything lands in the log.
                string ps =
                    "cd C:\\Dev\\EmergenceUnity; " +
                    "'CHUNKED PUSH ' + (Get-Date -Format HH:mm:ss) | Out-File -Append Logs\\git-push-chunked.log; " +
                    "$b = 'audition/free-assets-2026-07-19'; " +
                    "$c = @(git rev-list --reverse (\"origin/$b..HEAD\")); " +
                    "('commits ahead: ' + $c.Count) | Out-File -Append Logs\\git-push-chunked.log; " +
                    "for ($i = 3; $i -lt $c.Count; $i += 4) { " +
                    "  ('chunk -> ' + $c[$i]) | Out-File -Append Logs\\git-push-chunked.log; " +
                    "  git push origin ($c[$i] + ':refs/heads/' + $b) 2>&1 | Out-File -Append Logs\\git-push-chunked.log; " +
                    "  if ($LASTEXITCODE -ne 0) { ('CHUNK FAILED exit ' + $LASTEXITCODE) | Out-File -Append Logs\\git-push-chunked.log; break } " +
                    "}; " +
                    "git push -u origin HEAD 2>&1 | Out-File -Append Logs\\git-push-chunked.log; " +
                    "('final exit ' + $LASTEXITCODE + ' ' + (Get-Date -Format HH:mm:ss)) | Out-File -Append Logs\\git-push-chunked.log";
                var psi2 = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -Command \"" + ps.Replace("\"", "\\\"") + "\"")
                { CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = "C:\\Dev\\EmergenceUnity" };
                Process.Start(psi2);
                File.WriteAllText(ChunkDonePath, "STARTED " + System.DateTime.Now.ToString("HH:mm:ss") + " — see Logs/git-push-chunked.log\n");
                UnityEngine.Debug.Log("[PushBranch] chunked push started");
                }
            }
            catch (System.Exception e) { try { File.WriteAllText(ChunkDonePath, "ERROR " + e.Message + "\n"); } catch {} }

            try
            {
                if (File.Exists(CleanupTriggerPath))
                {
                    File.Delete(CleanupTriggerPath);
                    // no quotes, no outer redirects: the script self-logs, and cmd.exe's first/last-quote
                    // stripping mangled the quoted form (the 20:44 empty-log lesson)
                    var p = new ProcessStartInfo("cmd.exe",
                        "/c C:\\Users\\patri\\Dropbox\\Emergence\\CLEANUP-MOVES.cmd")
                    { CreateNoWindow = true, UseShellExecute = false };
                    Process.Start(p);
                    File.WriteAllText(CleanupDonePath, "STARTED " + System.DateTime.Now.ToString("HH:mm:ss") + " — see Logs/cleanup.log\n");
                }
            }
            catch (System.Exception e) { try { File.WriteAllText(CleanupDonePath, "ERROR " + e.Message + "\n"); } catch {} }

            try
            {
                if (File.Exists(SlimTriggerPath))
                {
                    File.Delete(SlimTriggerPath);
                    var p = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -ExecutionPolicy Bypass -File \"C:\\Dev\\EmergenceUnity\\Tools\\git-slim.ps1\"")
                    { CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = "C:\\Dev\\EmergenceUnity" };
                    Process.Start(p);
                    File.WriteAllText(SlimDonePath, "STARTED " + System.DateTime.Now.ToString("HH:mm:ss") + " — see Logs/git-slim.log\n");
                }
            }
            catch (System.Exception e) { try { File.WriteAllText(SlimDonePath, "ERROR " + e.Message + "\n"); } catch {} }
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
