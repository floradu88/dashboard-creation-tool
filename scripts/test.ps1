#requires -Version 7.0
param([switch]$E2E)
. "$PSScriptRoot/common.ps1"
Push-Location $RepoRoot
try {
    Invoke-Checked dotnet @('build', 'CustomerDashboard.slnx', '--no-restore', '-c', 'Debug')
    foreach ($project in @('CustomerDashboard.UnitTests', 'CustomerDashboard.ApiTests')) {
        Invoke-Checked dotnet @((Join-Path $RepoRoot "tests/$project/bin/Debug/net10.0/$project.dll"))
    }
    Push-Location $UiRoot
    try {
        Invoke-Checked npm @('test', '--', '--watch=false')
        Invoke-Checked npm @('run', 'build')
        if ($E2E) { Invoke-Checked npx @('playwright', 'test') }
    } finally { Pop-Location }
} finally { Pop-Location }


