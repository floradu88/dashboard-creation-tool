#requires -Version 7.0
. "$PSScriptRoot/common.ps1"
$project = Join-Path $RepoRoot 'src/CustomerDashboard.Api/CustomerDashboard.Api.csproj'
$fixtures = Join-Path $RepoRoot 'src/CustomerDashboard.Infrastructure/MockData'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $RepoRoot
try {
    Invoke-Checked dotnet @(
        'run', '--project', $project, '--no-launch-profile', '-c', 'Debug', '--',
        '--validate', "--fixtures=$fixtures"
    )
} finally { Pop-Location }
