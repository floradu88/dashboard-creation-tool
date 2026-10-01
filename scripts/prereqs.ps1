#requires -Version 5.1
# Installs a missing .NET 10 SDK and Node.js 24 into the current user's profile.
# No administrator account is required. An already suitable install is left alone.

$script:NodeVersion = '24.15.0'
$script:DotNetExe = $null
$script:NodeExe = $null
$script:NpmExe = $null
$script:NpxExe = $null

function Add-UserPath {
    param([string]$Directory)
    if (-not $Directory) { return }
    $userPath = [Environment]::GetEnvironmentVariable('Path', 'User')
    $parts = @()
    if ($userPath) { $parts = @($userPath -split ';' | Where-Object { $_ -and ($_.TrimEnd('\') -ne $Directory.TrimEnd('\')) }) }
    [Environment]::SetEnvironmentVariable('Path', ((@($Directory) + $parts) -join ';'), 'User')
    Add-ProcessPath $Directory
}

function Add-ProcessPath {
    param([string]$Directory)
    if (-not $Directory) { return }
    $trimmed = $Directory.TrimEnd('\')
    foreach ($part in @($env:Path -split ';')) {
        if ($part -and $part.TrimEnd('\') -eq $trimmed) { return }
    }
    $env:Path = "$Directory;$env:Path"
}

function Invoke-Quiet {
    param([string]$Executable, [string[]]$Arguments)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { return & $Executable @Arguments 2>&1 }
    finally { $ErrorActionPreference = $previous }
}

function Test-DotNetSdk {
    param([string]$Executable)
    if (-not $Executable -or -not (Test-Path -LiteralPath $Executable)) { return $false }
    Push-Location $script:RepoRoot
    try {
        $output = Invoke-Quiet $Executable @('--version')
        return $LASTEXITCODE -eq 0 -and (($output | Out-String).Trim() -like '10.*')
    } finally { Pop-Location }
}

function Test-NodeSdk {
    param([string]$Executable)
    if (-not $Executable -or -not (Test-Path -LiteralPath $Executable)) { return $false }
    $output = Invoke-Quiet $Executable @('--version')
    if ($LASTEXITCODE -ne 0) { return $false }
    $parsed = [version](($output | Out-String).Trim().TrimStart('v'))
    return $parsed.Major -eq 24 -and $parsed -ge [version]$script:NodeVersion
}

function Find-DotNetExe {
    $candidates = @()
    $existing = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($existing) { $candidates += $existing.Source }
    $candidates += (Join-Path $env:USERPROFILE '.dotnet\dotnet.exe')
    foreach ($candidate in $candidates) {
        if (-not (Test-DotNetSdk $candidate)) { continue }
        $toolHome = Split-Path -Parent $candidate
        Add-ProcessPath $toolHome
        $env:DOTNET_ROOT = $toolHome
        return $candidate
    }
    return $null
}

function Get-NodeCandidates {
    $seen = @{}
    $paths = @()
    $existing = Get-Command node -ErrorAction SilentlyContinue
    if ($existing) { $paths += $existing.Source }
    $paths += (Join-Path $env:LOCALAPPDATA 'Programs\nodejs\node.exe')
    $chosen = @()
    foreach ($path in $paths) {
        if (-not $path) { continue }
        $key = $path.TrimEnd('\').ToLowerInvariant()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true
        $chosen += $path
    }
    return $chosen
}

function Test-Npm12 {
    param([string]$ToolHome)
    $npm = Join-Path $ToolHome 'npm.cmd'
    if (-not (Test-Path -LiteralPath $npm)) { return $false }
    $npmVersion = Invoke-Quiet $npm @('--version')
    return $LASTEXITCODE -eq 0 -and (($npmVersion | Out-String).Trim() -like '12.*')
}

function Test-DirectoryWritable {
    param([string]$Directory)
    if (-not $Directory -or -not (Test-Path -LiteralPath $Directory)) { return $false }
    $probe = Join-Path $Directory ('.write-probe-' + [guid]::NewGuid().ToString('N'))
    try {
        [System.IO.File]::WriteAllText($probe, '')
        Remove-Item -LiteralPath $probe -Force
        return $true
    } catch { return $false }
}

function Find-NodeExe {
    foreach ($candidate in @(Get-NodeCandidates)) {
        if (-not (Test-NodeSdk $candidate)) { continue }
        $toolHome = Split-Path -Parent $candidate
        if (-not (Test-Npm12 $toolHome)) { continue }
        Add-ProcessPath $toolHome
        return $candidate
    }
    return $null
}

function Resolve-InstalledTools {
    $script:DotNetExe = Find-DotNetExe
    $script:NodeExe = Find-NodeExe
    if ($script:NodeExe) {
        $toolHome = Split-Path -Parent $script:NodeExe
        $script:NpmExe = Join-Path $toolHome 'npm.cmd'
        $script:NpxExe = Join-Path $toolHome 'npx.cmd'
    }
    if (-not $script:DotNetExe -or -not $script:NodeExe) { throw 'Dependencies missing. Run scripts\setup.cmd' }
}

function Install-MissingDotNetSdk {
    $found = Find-DotNetExe
    if ($found) { Write-Host "Using .NET SDK at $found"; return }
    $installDir = Join-Path $env:USERPROFILE '.dotnet'
    $installed = Join-Path $installDir 'dotnet.exe'
    Write-Host "The .NET 10 SDK from global.json is not available. Downloading it into $installDir (no administrator rights)."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    New-Item -ItemType Directory -Force -Path $script:LocalRoot | Out-Null
    $installer = Join-Path $script:LocalRoot 'dotnet-install.ps1'
    $progress = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try { Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer -UseBasicParsing }
    finally { $ProgressPreference = $progress }
    Unblock-File -LiteralPath $installer
    $globalJson = Join-Path $script:RepoRoot 'global.json'
    try { & $installer -JSonFile $globalJson -InstallDir $installDir -NoPath }
    catch { throw "The .NET SDK install failed. $($_.Exception.Message)" }
    if (-not (Test-DotNetSdk $installed)) { throw "The .NET SDK install did not produce a usable dotnet.exe in $installDir." }
    Add-UserPath $installDir
    [Environment]::SetEnvironmentVariable('DOTNET_ROOT', $installDir, 'User')
    $env:DOTNET_ROOT = $installDir
    Write-Host "Installed the .NET SDK into $installDir."
}

function Update-BundledNpm {
    param([string]$InstallDir)
    $node = Join-Path $InstallDir 'node.exe'
    $npmCli = Join-Path $InstallDir 'node_modules\npm\bin\npm-cli.js'
    if (-not (Test-Path -LiteralPath $npmCli)) { throw "The Node.js archive did not contain npm." }
    Write-Host "Installing npm 12.0.0 into $InstallDir (no administrator rights)."
    & $node $npmCli install --global --prefix $InstallDir npm@12.0.0
    if ($LASTEXITCODE -ne 0) { throw "npm 12.0.0 was not installed into $InstallDir." }
    $npm = Join-Path $InstallDir 'npm.cmd'
    $npmVersion = Invoke-Quiet $npm @('--version')
    if ($LASTEXITCODE -ne 0 -or (($npmVersion | Out-String).Trim() -notlike '12.*')) { throw "npm 12.0.0 was not installed into $InstallDir." }
}

function Install-MissingNode {
    $found = Find-NodeExe
    if ($found) { Write-Host "Using Node.js at $found"; return }
    foreach ($candidate in @(Get-NodeCandidates)) {
        if (-not (Test-NodeSdk $candidate)) { continue }
        $toolHome = Split-Path -Parent $candidate
        if (-not (Test-DirectoryWritable $toolHome)) { continue }
        Write-Host "Node.js at $candidate is usable, but npm is older than 12."
        try {
            Update-BundledNpm $toolHome
            $found = Find-NodeExe
            if ($found) { Write-Host "Using Node.js at $found"; return }
        } catch {
            Write-Host "Could not update npm in $toolHome. A user-local Node.js install will be used instead."
        }
    }
    $arch = $env:PROCESSOR_ARCHITECTURE
    $wow = [Environment]::GetEnvironmentVariable('PROCESSOR_ARCHITEW6432')
    if ($wow) { $arch = $wow }
    if ($arch -eq 'AMD64') { $arch = 'x64' }
    if ($arch -ne 'x64' -and $arch -ne 'arm64') { throw "Node.js setup supports x64 and arm64 Windows. This process is $arch." }
    $folder = "node-v$($script:NodeVersion)-win-$arch"
    $installDir = Join-Path $env:LOCALAPPDATA 'Programs\nodejs'
    $zipName = "$folder.zip"
    $url = "https://nodejs.org/dist/v$($script:NodeVersion)/$zipName"
    Write-Host "Node.js $script:NodeVersion is not available. Downloading $url into $installDir (no administrator rights)."
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    New-Item -ItemType Directory -Force -Path $script:LocalRoot | Out-Null
    $zip = Join-Path $script:LocalRoot $zipName
    $sums = Join-Path $script:LocalRoot 'SHASUMS256.txt'
    $progress = $ProgressPreference
    $ProgressPreference = 'SilentlyContinue'
    try {
        Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
        Invoke-WebRequest -Uri "https://nodejs.org/dist/v$($script:NodeVersion)/SHASUMS256.txt" -OutFile $sums -UseBasicParsing
    } finally { $ProgressPreference = $progress }
    $expected = $null
    foreach ($line in Get-Content -LiteralPath $sums) {
        $parts = @($line -split '\s+', 2)
        if ($parts.Length -eq 2 -and $parts[1] -eq $zipName) { $expected = $parts[0].ToLowerInvariant() }
    }
    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
    if (-not $expected -or $actual -ne $expected) { throw 'The Node.js download checksum did not match SHASUMS256.txt.' }
    $extract = Join-Path $script:LocalRoot 'node-extract'
    if (Test-Path -LiteralPath $extract) { Remove-Item -LiteralPath $extract -Recurse -Force }
    Expand-Archive -LiteralPath $zip -DestinationPath $extract
    $unpacked = Join-Path $extract $folder
    if (-not (Test-Path -LiteralPath $unpacked)) { throw "The Node.js archive did not contain $folder." }
    if (Test-Path -LiteralPath $installDir) { Remove-Item -LiteralPath $installDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $installDir) | Out-Null
    Move-Item -LiteralPath $unpacked -Destination $installDir
    Get-ChildItem -LiteralPath $installDir -Recurse -File | Unblock-File
    Update-BundledNpm $installDir
    $installed = Join-Path $installDir 'node.exe'
    if (-not (Test-NodeSdk $installed)) { throw "The Node.js install did not produce a usable node.exe in $installDir." }
    Add-UserPath $installDir
    Write-Host "Installed Node.js $script:NodeVersion into $installDir."
}

function Install-UserPrerequisites {
    Install-MissingDotNetSdk
    Install-MissingNode
    Resolve-InstalledTools
}
