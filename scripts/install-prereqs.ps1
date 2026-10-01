#requires -Version 5.1
. "$PSScriptRoot/common.ps1"
. "$PSScriptRoot/prereqs.ps1"
Install-UserPrerequisites
Write-Host ".NET SDK: $script:DotNetExe"
Write-Host "Node.js: $script:NodeExe"
Write-Host "npm: $script:NpmExe"
