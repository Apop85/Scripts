# Baut die STEFFLIX-App fuer Android.
# Fehlende Werkzeuge (JDK 17, Android SDK, Gradle) werden beim ersten Lauf nach
# %LOCALAPPDATA%\STEFFLIX-Android geladen. Ergebnis: STEFFLIX.apk im Release-Ordner.
#   -SetupOnly  nur Werkzeuge installieren
#   -Debug      Debug-APK nach dist\ (ohne Signaturschluessel)
param([switch]$SetupOnly, [switch]$Debug)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$release = (Resolve-Path (Join-Path $here '..\..')).Path
$tools   = Join-Path $env:LOCALAPPDATA 'STEFFLIX-Android'
$jdk     = Join-Path $tools 'jdk'
$sdk     = Join-Path $tools 'sdk'
$gradle  = Join-Path $tools 'gradle'

$jdkUrl    = 'https://github.com/adoptium/temurin17-binaries/releases/download/jdk-17.0.12%2B7/OpenJDK17U-jdk_x64_windows_hotspot_17.0.12_7.zip'
$sdkUrl    = 'https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip'
$gradleUrl = 'https://services.gradle.org/distributions/gradle-8.7-bin.zip'
$packages  = @('platforms;android-34', 'build-tools;34.0.0')

function Install-Zip([string]$url, [string]$target) {
    New-Item -ItemType Directory -Force $tools | Out-Null
    $zip = Join-Path $tools 'download.zip'
    $tmp = Join-Path $tools 'extract'
    Write-Host "Lade $url"
    Invoke-WebRequest $url -OutFile $zip -UseBasicParsing
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory -Force $tmp | Out-Null
    tar -xf $zip -C $tmp
    if ($LASTEXITCODE -ne 0) { throw "Entpacken fehlgeschlagen: $url" }
    $inner = Get-ChildItem $tmp | Select-Object -First 1
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
    Move-Item $inner.FullName $target
    Remove-Item $tmp -Recurse -Force
    Remove-Item $zip -Force
}

if (-not (Test-Path "$jdk\bin\java.exe")) { Install-Zip $jdkUrl $jdk }
if (-not (Test-Path "$gradle\bin\gradle.bat")) { Install-Zip $gradleUrl $gradle }
$sdkManager = "$sdk\cmdline-tools\latest\bin\sdkmanager.bat"
if (-not (Test-Path $sdkManager)) { Install-Zip $sdkUrl "$sdk\cmdline-tools\latest" }

$env:JAVA_HOME = $jdk
$env:ANDROID_HOME = $sdk
$env:GRADLE_USER_HOME = Join-Path $tools 'gradle-home'
$env:Path = "$jdk\bin;$env:Path"

if (-not (Test-Path "$sdk\platforms\android-34") -or -not (Test-Path "$sdk\build-tools\34.0.0")) {
    Write-Host 'Installiere Android-SDK-Pakete (Lizenzen werden bestaetigt) ...'
    $yes = Join-Path $tools 'yes.txt'
    [IO.File]::WriteAllText($yes, ("y`r`n" * 30))
    cmd /c "`"$sdkManager`" --sdk_root=`"$sdk`" --licenses < `"$yes`"" | Out-Null
    & $sdkManager "--sdk_root=$sdk" @packages
    if ($LASTEXITCODE -ne 0) { throw 'sdkmanager fehlgeschlagen' }
}
Write-Host "Werkzeuge bereit in $tools"
if ($SetupOnly) { return }

$task = 'assembleDebug'
if (-not $Debug) {
    $task = 'assembleRelease'
    $signDir = Join-Path $here 'signing'
    $props = Join-Path $signDir 'keystore.properties'
    if (-not (Test-Path $props)) {
        Write-Host ''
        Write-Host 'Erster Release-Build: Es wird ein Signaturschluessel erzeugt.'
        Write-Host 'Ordner "signing" gut sichern! Ohne ihn lassen sich keine Updates der App installieren.'
        $pw = Read-Host 'Passwort fuer den Schluessel (mind. 6 Zeichen)'
        if ($pw.Length -lt 6) { throw 'Passwort zu kurz' }
        New-Item -ItemType Directory -Force $signDir | Out-Null
        & "$jdk\bin\keytool.exe" -genkeypair -keystore "$signDir\stefflix.jks" -alias stefflix -keyalg RSA -keysize 2048 -validity 10000 -storepass $pw -keypass $pw -dname 'CN=STEFFLIX'
        if ($LASTEXITCODE -ne 0) { throw 'keytool fehlgeschlagen' }
        "storeFile=signing/stefflix.jks`nalias=stefflix`npassword=$pw`n" | Out-File $props -Encoding ascii
    }
}

# Splashscreen aus STEFFLIX in die App uebernehmen (wird sofort angezeigt, ohne Ladezeit)
Copy-Item (Join-Path $release 'sflix_sys\splash.svg') (Join-Path $here 'app\src\main\assets\splash.svg') -Force

& "$gradle\bin\gradle.bat" -p $here --no-daemon --console=plain $task
if ($LASTEXITCODE -ne 0) { throw 'Gradle-Build fehlgeschlagen' }

if ($Debug) {
    New-Item -ItemType Directory -Force (Join-Path $here 'dist') | Out-Null
    $out = Join-Path $here 'dist\STEFFLIX-debug.apk'
    Copy-Item "$here\app\build\outputs\apk\debug\app-debug.apk" $out -Force
} else {
    $out = Join-Path $release 'STEFFLIX.apk'
    Copy-Item "$here\app\build\outputs\apk\release\app-release.apk" $out -Force
}
Write-Host ''
Write-Host "Fertig: $out"
