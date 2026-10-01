#requires -Version 5.1
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/prereqs.ps1"
Resolve-InstalledTools
$project = Join-Path $RepoRoot 'src/CustomerDashboard.Api/CustomerDashboard.Api.csproj'
$fixtures = Join-Path $RepoRoot 'src/CustomerDashboard.Infrastructure/MockData'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $RepoRoot
try {
    Invoke-Checked $script:DotNetExe @(
        'run', '--project', $project, '--no-launch-profile', '-c', 'Debug', '--',
        '--validate', "--fixtures=$fixtures"
    )
} finally { Pop-Location }
