# avd.ps1 - eigene Android-Emulatoren fuer STEFFLIX (docs/TESTING.md)
#   -Anlegen [-Api 29|35]   AVD STEFFLIX_API<n> aus dem installierten Systemabbild (google_apis, x86_64) anlegen
#   -Start   [-Api 29|35]   Emulator starten (ohne Fenster mit -OhneFenster) und warten, bis er bereit ist
#   -Stop                   nur die laufenden STEFFLIX-Emulatoren beenden (fremde Emulatoren bleiben)
#   -Status                 AVDs und laufende Emulatoren (mit AVD-Namen) anzeigen
# Laufen mehrere Emulatoren (auch aus anderen Projekten), adb immer mit -s <Serial> des STEFFLIX-Emulators aufrufen.
# Nutzt das Android-SDK von Android Studio (%LOCALAPPDATA%\Android\Sdk). AVDs liegen in %USERPROFILE%\.android\avd.
# Getrennt von den Emulatoren anderer Projekte. Reines ASCII (PowerShell 5.1).
param([switch]$Anlegen, [switch]$Start, [switch]$Stop, [switch]$Status, [ValidateSet(29, 35)][int]$Api = 35, [switch]$OhneFenster)

$ErrorActionPreference = 'Stop'
$sdk = Join-Path $env:LOCALAPPDATA 'Android\Sdk'
$adb = Join-Path $sdk 'platform-tools\adb.exe'
$emu = Join-Path $sdk 'emulator\emulator.exe'
$avdm = Join-Path $sdk 'cmdline-tools\latest\bin\avdmanager.bat'
$name = 'STEFFLIX_API' + $Api
$image = 'system-images;android-' + $Api + ';google_apis;x86_64'

if (-not $env:JAVA_HOME -or -not (Test-Path (Join-Path $env:JAVA_HOME 'bin\java.exe'))) {
    foreach ($j in @((Join-Path $env:LOCALAPPDATA 'STEFFLIX-Android\jdk'), 'C:\Program Files\Android\Android Studio\jbr')) {
        if (Test-Path (Join-Path $j 'bin\java.exe')) { $env:JAVA_HOME = $j; break }
    }
}
$env:ANDROID_HOME = $sdk
$env:ANDROID_SDK_ROOT = $sdk

# adb ohne Abbruch bei Meldungen auf stderr (PowerShell 5.1 mit Stop bricht sonst ab); Ergebnis in $LASTEXITCODE
$adbRun = {
    $ErrorActionPreference = 'Continue'
    & $adb @args 2>$null
}

# Laufende Emulatoren mit AVD-Namen (andere Projekte koennen eigene Emulatoren offen haben: nie anfassen)
$getEmulators = {
    $res = @()
    foreach ($line in (& $adbRun devices | Select-Object -Skip 1)) {
        if ($line -match '^(emulator-\d+)\s+device') {
            $s = $Matches[1]
            $n = @(& $adbRun -s $s emu avd name)
            $avdName = ''; if ($n.Count -gt 0) { $avdName = ([string]$n[0]).Trim() }
            $res += [pscustomobject]@{ Serial = $s; Avd = $avdName }
        }
    }
    return , $res
}

if ($Status -or -not ($Anlegen -or $Start -or $Stop)) {
    Write-Output 'AVDs:'
    & $emu -list-avds | Where-Object { $_ -match '^STEFFLIX_' } | ForEach-Object { Write-Output ('  ' + $_) }
    Write-Output 'Laufende Emulatoren:'
    foreach ($e in (& $getEmulators)) {
        $mark = ''; if ($e.Avd -notmatch '^STEFFLIX_') { $mark = '  (fremd, nicht anfassen)' }
        Write-Output ('  ' + $e.Serial + '  ' + $e.Avd + $mark)
    }
    return
}

if ($Anlegen) {
    if ((& $emu -list-avds) -contains $name) { Write-Output "$name gibt es schon."; return }
    if (-not (Test-Path (Join-Path $sdk ('system-images\android-' + $Api + '\google_apis\x86_64')))) { throw "Systemabbild fehlt: $image (in Android Studio installieren)" }
    if (-not (Test-Path $avdm)) { throw 'avdmanager fehlt (Android Studio: SDK Tools > Command-line Tools)' }
    $no = Join-Path ([IO.Path]::GetTempPath()) 'stefflix-avd-no.txt'
    [IO.File]::WriteAllText($no, "no`r`n")
    cmd /c "`"$avdm`" create avd -n $name -k `"$image`" -d pixel_6 < `"$no`""
    if ($LASTEXITCODE -ne 0) { throw 'avdmanager create avd fehlgeschlagen' }
    Write-Output "Angelegt: $name"
    return
}

if ($Start) {
    if (-not ((& $emu -list-avds) -contains $name)) { throw "$name fehlt: zuerst -Anlegen -Api $Api" }
    $laeuft = @((& $getEmulators) | Where-Object { $_.Avd -eq $name })
    if ($laeuft.Count -gt 0) { Write-Output ("$name laeuft schon: " + $laeuft[0].Serial); return }
    $emuArgs = @('-avd', $name, '-no-snapshot-save', '-no-boot-anim')
    if ($OhneFenster) { $emuArgs += '-no-window' }
    Start-Process -FilePath $emu -ArgumentList $emuArgs -WindowStyle Minimized | Out-Null
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $serial = ''
    while ($sw.Elapsed.TotalSeconds -lt 240) {
        Start-Sleep -Seconds 3
        if (-not $serial) {
            $e = @((& $getEmulators) | Where-Object { $_.Avd -eq $name })
            if ($e.Count -gt 0) { $serial = $e[0].Serial } else { continue }
        }
        $b = (& $adbRun -s $serial shell getprop sys.boot_completed)
        if ($b -and ([string]$b).Trim() -eq '1') { Write-Output "$name bereit als $serial ($([int]$sw.Elapsed.TotalSeconds) s). adb immer mit -s $serial."; return }
    }
    throw "$name nicht innerhalb von 240 s gestartet"
}

if ($Stop) {
    $eigene = @((& $getEmulators) | Where-Object { $_.Avd -match '^STEFFLIX_' })
    foreach ($e in $eigene) { & $adbRun -s $e.Serial emu kill | Out-Null }
    if ($eigene.Count -eq 0) { Write-Output 'Kein STEFFLIX-Emulator lief.' }
    else { Write-Output ('Beendet: ' + (($eigene | ForEach-Object { $_.Serial + ' (' + $_.Avd + ')' }) -join ', ')) }
}
