#requires -Version 5.1
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/prereqs.ps1"
Push-Location $RepoRoot
try {
    Install-UserPrerequisites
    Invoke-Checked $script:DotNetExe @('restore', 'CustomerDashboard.slnx', '--locked-mode')
    Push-Location $UiRoot
    try { Invoke-Checked $script:NpmExe @('ci') } finally { Pop-Location }
    Write-Host 'Setup complete. Run: scripts\run.cmd'
} finally { Pop-Location }
