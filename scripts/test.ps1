#requires -Version 5.1
param([switch]$E2E)
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/prereqs.ps1"
Resolve-InstalledTools
Push-Location $RepoRoot
try {
    Invoke-Checked $script:DotNetExe @('build', 'CustomerDashboard.slnx', '--no-restore', '-c', 'Debug')
    foreach ($project in @('CustomerDashboard.UnitTests', 'CustomerDashboard.ApiTests')) {
        Invoke-Checked $script:DotNetExe @((Join-Path $RepoRoot "tests/$project/bin/Debug/net10.0/$project.dll"))
    }
    Push-Location $UiRoot
    try {
        Invoke-Checked $script:NpmExe @('test', '--', '--watch=false')
        Invoke-Checked $script:NpmExe @('run', 'build')
        if ($E2E) { Invoke-Checked $script:NpxExe @('playwright', 'test') }
    } finally { Pop-Location }
} finally { Pop-Location }


