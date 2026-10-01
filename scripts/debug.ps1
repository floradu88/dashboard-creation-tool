#requires -Version 5.1
param([ValidateSet('Mock')][string]$Mode = 'Mock', [int]$ApiPort = 5080, [int]$UiPort = 4200, [switch]$Detach)
& "$PSScriptRoot/run.ps1" -Mode $Mode -ApiPort $ApiPort -UiPort $UiPort -Configuration Debug -Detach:$Detach

