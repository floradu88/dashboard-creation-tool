#requires -Version 5.1
. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    foreach ($tool in @('dotnet', 'node', 'npm')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { throw "Install prerequisite: $tool. See docs/RUNNING.md." }
    }
    $sdk = & dotnet --version
    if ($sdk -notlike '10.*') { throw 'The pinned .NET 10 SDK is required.' }
    $nodeVersion = [version]((& node --version).TrimStart('v'))
    if ($nodeVersion.Major -ne 24 -or $nodeVersion -lt [version]'24.15.0') { throw 'Use Node 24.15.0 or a newer Node 24 patch.' }
    Invoke-Checked dotnet @('restore', 'CustomerDashboard.slnx', '--locked-mode')
    Push-Location $UiRoot
    try { Invoke-Checked npm @('ci') } finally { Pop-Location }
    Write-Host 'Setup complete. Run: powershell -File scripts/run.ps1'
} finally { Pop-Location }

