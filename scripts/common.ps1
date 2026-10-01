#requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path $PSScriptRoot -Parent
$script:UiRoot = Join-Path $script:RepoRoot 'src/customer-dashboard-ui'
$script:LocalRoot = Join-Path $script:RepoRoot '.local'
$script:StatePath = Join-Path $script:LocalRoot 'processes.json'
function Invoke-Checked {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command exited with code $LASTEXITCODE." }
}
function Set-Utf8File {
    param([string]$Path, [string]$Value)
    $encoding = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($Path, $Value, $encoding)
}
function Get-ProcessPath {
    param([int]$ProcessId)
    $instance = Get-CimInstance Win32_Process -Filter "ProcessId = $ProcessId" -ErrorAction SilentlyContinue
    if ($instance -and $instance.ExecutablePath) { return [string]$instance.ExecutablePath }
    return ''
}
function Test-OwnedProcess {
    param($Entry)
    $candidate = Get-Process -Id $Entry.Id -ErrorAction SilentlyContinue
    if (-not $candidate) { return $false }
    return $candidate.StartTime.ToUniversalTime().Ticks.ToString() -eq [string]$Entry.StartTicks -and (Get-ProcessPath $Entry.Id) -eq [string]$Entry.Executable
}
function Stop-WorkspaceProcesses {
    if (-not (Test-Path -LiteralPath $script:StatePath)) { return }
    $state = Get-Content -LiteralPath $script:StatePath -Raw | ConvertFrom-Json
    if ($state.Root -ne $script:RepoRoot) { throw 'Process file belongs to another workspace.' }
    foreach ($entry in @($state.Processes)) {
        if (Test-OwnedProcess $entry) {
            $children = [System.Collections.Generic.List[object]]::new()
            function Find-Children([int]$Parent) {
                foreach ($child in @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $Parent")) {
                    $process = Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
                    if ($process) {
                        $children.Add([pscustomobject]@{ Id = $process.Id; StartTicks = $process.StartTime.ToUniversalTime().Ticks.ToString(); Executable = (Get-ProcessPath $process.Id) })
                        Find-Children $process.Id
                    }
                }
            }
            Find-Children $entry.Id
            $process = Get-Process -Id $entry.Id -ErrorAction SilentlyContinue
            if ($process) { [void]$process.CloseMainWindow() }
            if (Test-OwnedProcess $entry) { Stop-Process -Id $entry.Id -ErrorAction SilentlyContinue }
            foreach ($child in $children) {
                if (Test-OwnedProcess $child) { Stop-Process -Id $child.Id -ErrorAction SilentlyContinue }
            }
        }
    }
    Remove-Item -LiteralPath $script:StatePath
}
function Wait-Endpoint {
    param([string]$Url, [int]$Seconds = 90)
    $deadline = [DateTime]::UtcNow.AddSeconds($Seconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec 2 -UseBasicParsing
            if ($response.StatusCode -eq 200) { return }
        } catch { }
        Start-Sleep -Milliseconds 500
    }
    throw "Timed out waiting for $Url. See .local logs."
}

