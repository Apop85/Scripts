# build_test_apk.ps1 - Test-Build der STEFFLIX-App fuer die Rolle Pruefung (docs/TESTING.md).
# Gleiche Signatur wie die normale App (signing\keystore.properties), Ergebnis dist\STEFFLIX-test.apk;
# STEFFLIX.apk im Release-Ordner bleibt unveraendert. Nicht Teil des Fingerabdrucks von release.ps1.
#   -Install   danach in den laufenden Emulator installieren (adb install -r)
#   -Serial    Geraet fuer adb (z. B. emulator-5554), Standard: das einzige verbundene
# Reines ASCII (PowerShell 5.1).
param([switch]$Install, [string]$Serial = '')

$ErrorActionPreference = 'Stop'
$here  = Split-Path -Parent $MyInvocation.MyCommand.Path
$root  = (Resolve-Path (Join-Path $here '..\..')).Path
$tools = Join-Path $env:LOCALAPPDATA 'STEFFLIX-Android'
$jdk   = Join-Path $tools 'jdk'
$sdk   = Join-Path $tools 'sdk'
$gradle = Join-Path $tools 'gradle'

if (-not (Test-Path "$jdk\bin\java.exe") -or -not (Test-Path "$gradle\bin\gradle.bat") -or -not (Test-Path "$sdk\platforms\android-34")) {
    Write-Host 'Werkzeuge fehlen, installiere sie mit build.ps1 -SetupOnly ...'
    & (Join-Path $here 'build.ps1') -SetupOnly
}
if (-not (Test-Path (Join-Path $here 'signing\keystore.properties'))) {
    throw 'signing\keystore.properties fehlt: Der Benutzer muss einmal build_apk.bat ausfuehren (Signaturschluessel). Nie einen neuen Schluessel erzeugen.'
}

$env:JAVA_HOME = $jdk
$env:ANDROID_HOME = $sdk
$env:GRADLE_USER_HOME = Join-Path $tools 'gradle-home'
$env:Path = "$jdk\bin;$env:Path"

Copy-Item (Join-Path $root 'sflix_sys\splash.svg') (Join-Path $here 'app\src\main\assets\splash.svg') -Force
& "$gradle\bin\gradle.bat" -p $here --no-daemon --console=plain --quiet assembleRelease
if ($LASTEXITCODE -ne 0) { throw 'Gradle-Build fehlgeschlagen' }

$dist = Join-Path $here 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
$out = Join-Path $dist 'STEFFLIX-test.apk'
Copy-Item (Join-Path $here 'app\build\outputs\apk\release\app-release.apk') $out -Force
Write-Host "Fertig: $out"

if ($Install) {
    $adb = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
    if (-not (Test-Path $adb)) { $adb = Join-Path $sdk 'platform-tools\adb.exe' }
    if (-not (Test-Path $adb)) { throw 'adb nicht gefunden' }
    $adbArgs = @(); if ($Serial) { $adbArgs = @('-s', $Serial) }
    & $adb @adbArgs install -r $out
    if ($LASTEXITCODE -ne 0) { throw 'adb install fehlgeschlagen (Emulator gestartet? Andere Signatur installiert?)' }
    Write-Host 'Installiert.'
}
