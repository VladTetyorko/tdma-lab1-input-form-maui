<#
.SYNOPSIS
    Builds an installable, debug-signed Release APK of StockRoom and optionally installs and starts it on a phone.

.DESCRIPTION
    Works in Windows PowerShell 5.1 and PowerShell 7, from any folder.
    The package is signed with the debug key of this machine (no release keystore, see docs/RELEASE.md).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\build-apk.ps1 -Install -Launch
#>
[CmdletBinding()]
param(
    [switch]$Install,
    [switch]$Launch,
    [string]$Serial,
    [string]$SdkPath,
    [string]$OutDir = "artifacts/apk"
)

$ErrorActionPreference = 'Stop'

$ApplicationId = 'com.companyname.mobile_apps_l1_model_view'
$Framework = 'net10.0-android'

function Stop-Script([string]$Message) {
    Write-Host "ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Test-AndroidSdk([string]$Path) {
    return $Path -and (Test-Path (Join-Path $Path 'platforms')) -and (Test-Path (Join-Path $Path 'build-tools'))
}

function Find-AndroidSdk {
    if ($SdkPath) {
        # An explicit path is never silently replaced by another SDK
        if (Test-AndroidSdk $SdkPath) { return (Resolve-Path $SdkPath).Path }
        Stop-Script "No Android SDK in '$SdkPath' (it must contain 'platforms' and 'build-tools')."
    }

    $candidates = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT)
    if ($env:LOCALAPPDATA) { $candidates += (Join-Path $env:LOCALAPPDATA 'Android\Sdk') }
    foreach ($candidate in $candidates) {
        if (Test-AndroidSdk $candidate) { return (Resolve-Path $candidate).Path }
    }

    Stop-Script "Android SDK not found. Pass -SdkPath, or set ANDROID_HOME, to a folder with 'platforms' and 'build-tools'."
}

function Invoke-Adb([string[]]$Arguments) {
    # adb writes progress to stderr; in 5.1 that must not become a terminating error
    $ErrorActionPreference = 'Continue'
    $output = & $script:Adb @Arguments 2>&1 | ForEach-Object { "$_" }
    return [pscustomobject]@{ Code = $LASTEXITCODE; Text = ($output -join [Environment]::NewLine) }
}

function Get-DeviceSerial {
    $result = Invoke-Adb @('devices')
    $devices = @($result.Text -split "\r?\n" |
        Where-Object { $_ -match '^(\S+)\s+device$' } |
        ForEach-Object { ($_ -split '\s+')[0] })

    if ($Serial) {
        if ($devices -notcontains $Serial) { Stop-Script "Device '$Serial' is not connected or not authorised." }
        return $Serial
    }
    if ($devices.Count -eq 0) {
        Stop-Script "No device. Connect the phone with USB debugging on, accept the prompt on the phone, then run again."
    }
    if ($devices.Count -gt 1) {
        Stop-Script "More than one device: $($devices -join ', '). Choose one with -Serial."
    }
    return $devices[0]
}

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'Mobile_apps_L1_Model_view\Mobile_apps_L1_Model_view.csproj'
if (-not (Test-Path $project)) { Stop-Script "Project not found: $project" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Stop-Script "'dotnet' is not on the PATH." }

$sdk = Find-AndroidSdk
Write-Host "Android SDK: $sdk"

$publishArguments = @(
    'publish', $project, '-f', $Framework, '-c', 'Release',
    '-p:AndroidPackageFormat=apk', "-p:AndroidSdkDirectory=$sdk")

if ($env:JAVA_HOME -and (Test-Path (Join-Path $env:JAVA_HOME 'bin'))) {
    Write-Host "JDK: $env:JAVA_HOME"
    $publishArguments += "-p:JavaSdkDirectory=$env:JAVA_HOME"
}
else {
    Write-Host "JAVA_HOME is not set or is not a JDK folder: the .NET Android SDK picks its default JDK."
}

# A package left by an earlier build must not be mistaken for this one
$publishDir = Join-Path $root "Mobile_apps_L1_Model_view\bin\Release\$Framework\publish"
if (Test-Path $publishDir) { Remove-Item (Join-Path $publishDir '*.apk') -Force -ErrorAction SilentlyContinue }

& dotnet @publishArguments
if ($LASTEXITCODE -ne 0) { Stop-Script "dotnet publish failed with exit code $LASTEXITCODE." }

# Only the signed package is installable; its unsigned twin is never copied
$signed = @(Get-ChildItem -Path $publishDir -Filter '*-Signed.apk' -ErrorAction SilentlyContinue)
if ($signed.Count -ne 1) { Stop-Script "Expected one signed APK in $publishDir, found $($signed.Count)." }

[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$version = @($projectXml.Project.PropertyGroup.ApplicationDisplayVersion | Where-Object { $_ })[0]
if (-not $version) { $version = '0' }

$commit = 'nogit'
if (Get-Command git -ErrorAction SilentlyContinue) {
    $sha = & git -C $root rev-parse --short HEAD
    if ($LASTEXITCODE -eq 0) {
        $commit = "$sha"
        # Mark a package that contains uncommitted changes to tracked files
        if (& git -C $root status --porcelain --untracked-files=no) { $commit = "$commit-dirty" }
    }
}

if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path $root $OutDir }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$apk = Join-Path (Resolve-Path $OutDir).Path "StockRoom-$version-$commit.apk"
Copy-Item -LiteralPath $signed[0].FullName -Destination $apk -Force

$size = (Get-Item -LiteralPath $apk).Length
Write-Host ""
Write-Host "APK:     $apk"
Write-Host ("Size:    {0:N1} MB ({1} bytes)" -f ($size / 1MB), $size)
Write-Host "SHA-256: $((Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash)"
Write-Host "Signed with this machine's debug key."

if (-not ($Install -or $Launch)) { exit 0 }

$script:Adb = Join-Path $sdk 'platform-tools\adb.exe'
if (-not (Test-Path $script:Adb)) { Stop-Script "adb not found: $script:Adb. Install 'platform-tools'." }
$device = Get-DeviceSerial

if ($Install) {
    Write-Host "Installing on $device ..."
    $result = Invoke-Adb @('-s', $device, 'install', '-r', $apk)
    Write-Host $result.Text
    if ($result.Text -match 'INSTALL_FAILED_UPDATE_INCOMPATIBLE') {
        Stop-Script ("An APK signed with another key (for example the CI artifact) is installed. Uninstall it first: " +
            "adb uninstall $ApplicationId . UNINSTALLING DELETES THE APP DATA ON THE PHONE, so this script never does it.")
    }
    if ($result.Code -ne 0 -or $result.Text -notmatch 'Success') { Stop-Script "adb install failed." }
}

if ($Launch) {
    $result = Invoke-Adb @('-s', $device, 'shell', 'monkey', '-p', $ApplicationId, '-c', 'android.intent.category.LAUNCHER', '1')
    if ($result.Code -ne 0 -or $result.Text -match 'No activities found') {
        Write-Host $result.Text
        Stop-Script "Could not start the app. Is it installed?"
    }
    Write-Host "Started $ApplicationId on $device."
}

exit 0
