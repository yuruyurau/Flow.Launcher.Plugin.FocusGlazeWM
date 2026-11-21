$ErrorActionPreference = "Stop"

# 1. Publish
dotnet publish Flow.Launcher.Plugin.FocusGlazeWM -c Debug -r win-x64 --no-self-contained

$AppDataFolder = [Environment]::GetFolderPath("ApplicationData")
$pluginDest = "$AppDataFolder\FlowLauncher\Plugins\FocusGlazeWM"
$publishSource = "Flow.Launcher.Plugin.FocusGlazeWM\bin\Debug\win-x64\publish"
$flowLauncherExe = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"

if (-not (Test-Path $flowLauncherExe)) {
    Write-Host "Flow.Launcher.exe not found at: $flowLauncherExe" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $publishSource)) {
    Write-Host "Publish folder not found: $publishSource" -ForegroundColor Red
    exit 1
}

# 2. Stop Flow
Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# 3. Replace plugin folder (contents only)
if (Test-Path $pluginDest) {
    Remove-Item -Recurse -Force $pluginDest
}
New-Item -ItemType Directory -Path $pluginDest -Force | Out-Null
Copy-Item "$publishSource\*" $pluginDest -Recurse -Force

# 4. Restart
Start-Sleep -Seconds 1
Start-Process $flowLauncherExe

Write-Host "Deployed to $pluginDest and restarted Flow Launcher." -ForegroundColor Green

