<#
.SYNOPSIS
    Helpers to drive StockRoom on an Android emulator or phone and take screenshots (docs/design/revision-3.md, S-last).

.DESCRIPTION
    Dot-source it, then call the functions:

        . .\scripts\emulator-screens.ps1
        Start-StockRoomEmulator              # headless AVD "stockroom" (created once, see revision-3.md)
        Install-StockRoom                    # newest artifacts/apk/*.apk, then launch
        Find-Ui 'Sign in'                    # centre of the first node whose text / content-desc / resource-id matches
        Tap-Ui 'UsernameEntry'; Send-Text 'olena'
        Save-Screen '01-login'               # docs/design/screenshots/r3/01-login.png, downscaled to 540 px wide
        Get-Crash                            # FATAL EXCEPTION / AndroidRuntime / mono-rt lines since the last clear
        Stop-StockRoomEmulator

    Nothing here installs SDK packages or accepts licences.
#>

$script:Sdk = if ($env:ANDROID_HOME) { $env:ANDROID_HOME } else { Join-Path $env:LOCALAPPDATA 'Android\Sdk' }
$script:Adb = Join-Path $script:Sdk 'platform-tools\adb.exe'
$script:Package = 'com.companyname.mobile_apps_l1_model_view'
$script:Repo = Split-Path $PSScriptRoot -Parent
if (-not $global:ScreenDir) { $global:ScreenDir = Join-Path $script:Repo 'docs\design\screenshots\r3' }

function Invoke-Adb { & $script:Adb @args }

# -Camera none reproduces "no camera" (the crash of revision-3 F1); emulated gives a test pattern preview.
# swiftshader_indirect is required: with -gpu host and -no-window, screencap returns a white picture
function Start-StockRoomEmulator {
    param([string]$Avd = 'stockroom', [ValidateSet('none', 'emulated')][string]$Camera = 'emulated')
    $emulator = Join-Path $script:Sdk 'emulator\emulator.exe'
    Start-Process $emulator -ArgumentList "-avd $Avd -no-window -no-audio -no-boot-anim -camera-back $Camera -camera-front none -gpu swiftshader_indirect" -WindowStyle Hidden
    Invoke-Adb wait-for-device
    while ((Invoke-Adb shell getprop sys.boot_completed) -notmatch '1') { Start-Sleep 3 }
    foreach ($scale in 'window_animation_scale', 'transition_animation_scale', 'animator_duration_scale') {
        Invoke-Adb shell settings put global $scale 0
    }
    Write-Host 'Emulator booted (animations off)'
}

function Stop-StockRoomEmulator { Invoke-Adb emu kill }

function Install-StockRoom {
    param([string]$Apk)
    if (-not $Apk) {
        $Apk = (Get-ChildItem (Join-Path $script:Repo 'artifacts\apk\*.apk') | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    }
    Invoke-Adb install -r $Apk
    Invoke-Adb logcat -c
    Invoke-Adb shell monkey -p $script:Package -c android.intent.category.LAUNCHER 1 | Out-Null
}

# Fresh data (seed only): clears the app's database and preferences
function Reset-StockRoom { Invoke-Adb shell pm clear $script:Package }

function Get-UiXml {
    Invoke-Adb shell uiautomator dump /sdcard/ui.xml | Out-Null
    [xml](Invoke-Adb exec-out cat /sdcard/ui.xml)
}

# Lists the visible nodes that have text, a description or an id, with their centres
function Show-Ui {
    (Get-UiXml).SelectNodes('//node') | Where-Object { $_.text -or $_.'content-desc' -or $_.'resource-id' } | ForEach-Object {
        $b = [regex]::Matches($_.bounds, '\d+') | ForEach-Object { [int]$_.Value }
        '{0,5},{1,-5} {2} | {3} | {4}' -f [int](($b[0] + $b[2]) / 2), [int](($b[1] + $b[3]) / 2), $_.text, $_.'content-desc', ($_.'resource-id' -replace '.*:id/', '')
    }
}

function Find-Ui {
    param([Parameter(Mandatory)][string]$Match, [int]$Index = 0)
    $nodes = @((Get-UiXml).SelectNodes('//node') | Where-Object {
        $_.text -eq $Match -or $_.'content-desc' -eq $Match -or ($_.'resource-id' -replace '.*:id/', '') -eq $Match })
    if ($nodes.Count -le $Index) { throw "No UI node '$Match'" }
    $b = [regex]::Matches($nodes[$Index].bounds, '\d+') | ForEach-Object { [int]$_.Value }
    [pscustomobject]@{ X = [int](($b[0] + $b[2]) / 2); Y = [int](($b[1] + $b[3]) / 2) }
}

function Tap { param([int]$X, [int]$Y) Invoke-Adb shell input tap $X $Y; Start-Sleep -Milliseconds 900 }
function Tap-Ui { param([string]$Match, [int]$Index = 0) $p = Find-Ui $Match $Index; Tap $p.X $p.Y }
# Single quotes keep the device shell from reading '#' as a comment (passwords such as Manager#2026)
function Send-Text { param([string]$Text) Invoke-Adb shell "input text '$($Text -replace ' ', '%s')'"; Start-Sleep -Milliseconds 600 }

# "System UI / StockRoom isn't responding" dialogs appear while a starved emulator settles; this taps Wait
function Skip-Anr { for ($i = 0; $i -lt 5; $i++) { try { Tap-Ui 'aerr_wait'; Start-Sleep 5 } catch { return } } }
function Send-Key { param([string]$Key) Invoke-Adb shell input keyevent $Key; Start-Sleep -Milliseconds 700 }
function Swipe { param([int]$X1, [int]$Y1, [int]$X2, [int]$Y2) Invoke-Adb shell input swipe $X1 $Y1 $X2 $Y2 300; Start-Sleep -Milliseconds 800 }

# Full-resolution capture, downscaled to $Width so the folder stays small
function Save-Screen {
    param([Parameter(Mandatory)][string]$Name, [int]$Width = 540)
    New-Item -ItemType Directory -Force $global:ScreenDir | Out-Null
    $raw = Join-Path $env:TEMP "stockroom-$Name-raw.png"
    Invoke-Adb shell screencap -p /sdcard/screen.png
    Invoke-Adb pull /sdcard/screen.png $raw | Out-Null
    Add-Type -AssemblyName System.Drawing
    $src = [System.Drawing.Image]::FromFile($raw)
    $height = [int]($src.Height * $Width / $src.Width)
    $dst = New-Object System.Drawing.Bitmap $Width, $height
    $g = [System.Drawing.Graphics]::FromImage($dst)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.DrawImage($src, 0, 0, $Width, $height)
    $out = Join-Path $global:ScreenDir "$Name.png"
    $dst.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $dst.Dispose(); $src.Dispose(); Remove-Item $raw
    $out
}

function Get-Crash {
    Invoke-Adb logcat -d | Select-String -Pattern 'FATAL EXCEPTION', 'AndroidRuntime', 'mono-rt', 'Unhandled', 'System\..*Exception' |
        Select-Object -First 60
}
