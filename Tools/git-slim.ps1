# EMERGENCE - D-166 B2: slim the LOCAL (never-pushed) history. ASCII only, UTF-8 BOM.
# TD-038 committed ~3 GB purchased packs; GitHub rejects packs > 2 GB. Origin sits at TD-037
# and never saw these commits - rewriting is safe. Backup branch first. Packs stay ON DISK.
$log = "C:\Dev\EmergenceUnity\Logs\git-slim.log"
function L($m) { ("{0} {1}" -f (Get-Date -Format HH:mm:ss), $m) | Out-File -Append -Encoding utf8 $log }
Set-Location C:\Dev\EmergenceUnity
L "=== GIT-SLIM start ==="
$backup = "backup/pre-slim-2026-07-24"
if (git branch --list $backup) { L "backup branch already exists (pre-slim history is saved) - reusing it" }
else { git branch $backup HEAD }
L ("backup branch at " + (git rev-parse --short $backup))
# filter-branch demands a clean tree - a dirty tree means uncommitted work; refuse loudly
$dirty = git status --porcelain
if ($dirty) { L "ABORT: working tree not clean - commit first (RUN_GITCOMMIT)."; L ($dirty | Out-String); exit 1 }
$env:FILTER_BRANCH_SQUELCH_WARNING = "1"
$base = "180ab6667be19d5f0752444099b05dc11f5fd434"
$filter = 'git rm -rq --cached --ignore-unmatch "Assets/FANTASTIC - City Pack" "Assets/FANTASTIC - City Pack.meta" "Assets/Fantastic City Pack" "Assets/Fantastic City Pack.meta" "Assets/Fantastic Village Pack" "Assets/Fantastic Village Pack.meta" "Assets/FANTASTIC - Village Pack" "Assets/FANTASTIC - Village Pack.meta"'
L "filter-branch running..."
git filter-branch -f --index-filter $filter --prune-empty -- "$base..HEAD" 2>&1 | Out-File -Append -Encoding utf8 $log
L ("filter-branch exit: " + $LASTEXITCODE)
$first = git rev-list --reverse "$base..HEAD" | Select-Object -First 1
L ("first pending commit new-object load: " + (git rev-list --disk-usage --objects "$base..$first") + " bytes (was ~2.58 GB)")
L ("total pending push load: " + (git rev-list --disk-usage --objects "$base..HEAD") + " bytes")
L ("HEAD: " + (git log --oneline -1))
L "=== GIT-SLIM done - verify then drop RUN_GITPUSH_CHUNKED.trigger ==="
