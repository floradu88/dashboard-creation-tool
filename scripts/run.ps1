#requires -Version 5.1
param(
    [ValidateSet('Mock')][string]$Mode = 'Mock',
    [ValidateRange(1024,65535)][int]$ApiPort = 5080,
    [ValidateRange(1024,65535)][int]$UiPort = 4200,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [switch]$Detach
)
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/prereqs.ps1"
Resolve-InstalledTools
if ($ApiPort -eq $UiPort) { throw 'API and UI require different ports.' }
New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
$lock = [System.IO.File]::Open((Join-Path $LocalRoot 'startup.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
$started = $false
try {
    if (Test-Path -LiteralPath $StatePath) {
        $existing = Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
        if (@($existing.Processes | Where-Object { Test-OwnedProcess $_ }).Count) { throw 'Workspace is already running. Use stop.ps1 first.' }
        Remove-Item -LiteralPath $StatePath
    }
    foreach ($port in @($ApiPort, $UiPort)) {
        $probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        try { $probe.Start() } catch { throw "Port $port is occupied. Use -ApiPort / -UiPort." } finally { $probe.Stop() }
    }
    if (-not (Test-Path (Join-Path $UiRoot 'node_modules/@angular/cli/bin/ng.js'))) { throw 'Dependencies missing. Run scripts\setup.cmd' }
    Push-Location $RepoRoot
    try { Invoke-Checked $script:DotNetExe @('build', 'src/CustomerDashboard.Api/CustomerDashboard.Api.csproj', '--no-restore', '-c', $Configuration) } finally { Pop-Location }
    $proxyPath = Join-Path $LocalRoot 'proxy.json'
    Set-Utf8File $proxyPath ((@{ '/api/**' = @{ target = "http://127.0.0.1:$ApiPort"; secure = $false; changeOrigin = $true } } | ConvertTo-Json -Depth 5))
    $entries = [System.Collections.Generic.List[object]]::new()
    function Save-State {
        Set-Utf8File $StatePath ((@{ Root = $RepoRoot; ApiPort = $ApiPort; UiPort = $UiPort; Processes = @($entries.ToArray()) } | ConvertTo-Json -Depth 5))
    }
    function Start-Owned([string]$Executable, [string[]]$Arguments, [string]$Name, [string]$WorkingDirectory) {
        $quoted = @($Arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }) -join ' '
        $options = @{ FilePath = $Executable; ArgumentList = $quoted; WorkingDirectory = $WorkingDirectory; WindowStyle = 'Hidden'; PassThru = $true; RedirectStandardOutput = (Join-Path $LocalRoot "$Name.log"); RedirectStandardError = (Join-Path $LocalRoot "$Name.error.log") }
        $process = Start-Process @options
        $entries.Add([pscustomobject]@{ Name = $Name; Id = $process.Id; StartTicks = $process.StartTime.ToUniversalTime().Ticks.ToString(); Executable = $Executable })
        Save-State
        return $process.Id
    }
    $dll = Join-Path $RepoRoot "src/CustomerDashboard.Api/bin/$Configuration/net10.0/CustomerDashboard.Api.dll"
    $apiId = Start-Owned $script:DotNetExe @($dll, '--urls', "http://127.0.0.1:${ApiPort};http://[::1]:${ApiPort}", '--environment', 'Development') 'api' $RepoRoot
    $started = $true
    Wait-Endpoint "http://127.0.0.1:$ApiPort/health/ready"
    $uiId = Start-Owned $script:NodeExe @((Join-Path $UiRoot 'node_modules/@angular/cli/bin/ng.js'), 'serve', '--host', 'localhost', '--port', "$UiPort", '--proxy-config', $proxyPath) 'ui' $UiRoot
    Wait-Endpoint "http://localhost:$UiPort" 120
    Write-Host "UI: http://localhost:$UiPort | API: http://127.0.0.1:$ApiPort | MCP: http://127.0.0.1:$ApiPort/mcp"
    Write-Host "API PID: $apiId | UI PID: $uiId | Logs: $LocalRoot"
    Write-Host 'Attach the .NET debugger to the API PID; browser configuration is in .vscode/launch.json.'
    $lock.Dispose()
    if ($Detach) { Write-Host 'Running in background. Stop with scripts/stop.ps1.'; return }
    Write-Host 'Press Ctrl+C to stop both services.'
    while ((Get-Process -Id $apiId -ErrorAction SilentlyContinue) -and (Get-Process -Id $uiId -ErrorAction SilentlyContinue)) { Start-Sleep -Seconds 1 }
    throw 'A service stopped unexpectedly. Inspect .local logs.'
} catch {
    if ($started) { Stop-WorkspaceProcesses }
    throw
} finally {
    $lock.Dispose()
    if ($started -and -not $Detach) { Stop-WorkspaceProcesses }
}

