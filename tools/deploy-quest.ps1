<#
.SYNOPSIS
    Installs WintryVR on a connected Quest and launches it, reporting why it did not start if it did not.

.DESCRIPTION
    "I tap the icon and nothing happens" is the least informative failure a headset can give you: the launcher
    reports nothing, and the app is gone from the task list before you have taken the headset off. Everything
    that explains it is in logcat, so this script installs, launches, and then says in one line whether the
    activity actually came up — and prints the reason when it did not.

    The old package is removed first on purpose. Installing over the top keeps whatever the previous build
    registered, and a stale launcher entry pointing at an activity class the new APK no longer contains fails
    in exactly the silent way this script exists to explain.

.PARAMETER Apk
    APK to install. Defaults to dist/WintryVR.apk next to this repository.

.PARAMETER Keep
    Install over the existing app instead of uninstalling it first, keeping its saved settings and pushed
    config files.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools/deploy-quest.ps1
#>
[CmdletBinding()]
param(
    [string] $Apk,
    [switch] $Keep
)

$ErrorActionPreference = 'Stop'
$Package  = 'com.wintry.wintryvr'
$Activity = "$Package/com.unity3d.player.UnityPlayerActivity"

function Find-Adb {
    $onPath = Get-Command adb -ErrorAction SilentlyContinue
    if ($onPath) { return $onPath.Source }

    $candidates = @()
    # Unity ships a complete SDK with the Android Build Support module; the editor root varies per install.
    foreach ($root in @("$env:ProgramFiles\Unity\Hub\Editor", 'D:\', 'C:\Unity')) {
        if (Test-Path $root) {
            $candidates += Get-ChildItem -Path $root -Filter 'adb.exe' -Recurse -Depth 8 -ErrorAction SilentlyContinue |
                           Select-Object -ExpandProperty FullName
        }
    }
    $candidates += "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
    return $null
}

$adb = Find-Adb
if (-not $adb) {
    throw "adb not found. Install Android Build Support in Unity Hub, or add platform-tools to PATH."
}
Write-Host "adb: $adb"

if (-not $Apk) {
    $Apk = Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\WintryVR.apk'
}
if (-not (Test-Path $Apk)) {
    throw "APK not found at $Apk. Build one first (WintryVR -> Build -> Build Quest APK)."
}
$size = [math]::Round((Get-Item $Apk).Length / 1MB, 1)
Write-Host "apk: $Apk ($size MB)"

# A headset that is asleep, locked, or has not had "Allow USB debugging" accepted shows up as unauthorized or
# offline rather than as nothing at all, so say which of those it is.
$devices = & $adb devices | Select-Object -Skip 1 | Where-Object { $_.Trim() }
if (-not $devices) {
    throw "No device. Connect the Quest by USB, put it on, and accept the 'Allow USB debugging' prompt inside the headset."
}
foreach ($d in $devices) {
    if ($d -match 'unauthorized') { throw "Device is unauthorized: accept the USB debugging prompt inside the headset." }
    if ($d -match 'offline')      { throw "Device is offline: unplug, wake the headset, plug back in." }
}
Write-Host "device: $($devices[0])"

if (-not $Keep) {
    Write-Host "Removing any previous $Package ..."
    & $adb uninstall $Package 2>&1 | Out-Null   # fails harmlessly when it was never installed
}

Write-Host "Installing ..."
$install = & $adb install -r -g $Apk 2>&1
if ($LASTEXITCODE -ne 0 -or ($install -match 'Failure')) {
    Write-Host ($install -join "`n")
    if ($install -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE|signatures do not match') {
        throw "The installed copy was signed with a different key. Run again without -Keep so it is uninstalled first."
    }
    throw "Install failed."
}
Write-Host "Installed."

# Clear the log, launch, and read back only what the launch produced.
& $adb logcat -c 2>&1 | Out-Null
$start = & $adb shell am start -n $Activity 2>&1
Write-Host ($start -join "`n")

if ($start -match 'does not exist') {
    Write-Host ""
    Write-Host "The launcher activity is not in the APK." -ForegroundColor Red
    Write-Host "Player Settings -> Android -> Other Settings -> Application Entry Point must be 'Activity',"
    Write-Host "not 'GameActivity'. Run WintryVR -> Setup -> Configure Player Settings, then rebuild."
    exit 1
}
if ($start -match 'Error|Exception') { Write-Host "Launch reported an error above." -ForegroundColor Red; exit 1 }

Start-Sleep -Seconds 6
$running = & $adb shell pidof $Package 2>&1
if ($running -and ($running -join '').Trim()) {
    Write-Host ""
    Write-Host "WintryVR is running (pid $($running -join ' ')). Put the headset on." -ForegroundColor Green
    Write-Host "Follow its log with:  adb logcat -s Unity"
    exit 0
}

Write-Host ""
Write-Host "The app started and then stopped. What the device said:" -ForegroundColor Red
& $adb logcat -d -b crash -t 80
& $adb logcat -d -t 200 | Select-String -Pattern 'Unity|AndroidRuntime|XR|Oculus|VrApi|FATAL' | Select-Object -Last 40
exit 1
