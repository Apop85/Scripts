# smb-testshare.ps1 - lokaler SMB-Testserver fuer die STEFFLIX-App im Emulator (docs/TESTING.md, nach Sync2PC)
#   SFLIX_Tools\Test\smb-testshare.ps1            -> Start (venv beim ersten Mal)
#   SFLIX_Tools\Test\smb-testshare.ps1 -Stop      -> Server beenden, adb reverse entfernen
#   SFLIX_Tools\Test\smb-testshare.ps1 -Status    -> laeuft der Server, ist adb reverse aktiv
#   -ThrottleKBps <n> / -DropAfterBytes <n> [-DropCount <k>]  -> Datenrate bremsen bzw. Verbindung trennen (nur beim Start)
# Freigabe ist IMMER .claude\testenv (nur lesend), nie ein produktiver STEFFLIX-Ordner.
# Ablauf: venv mit impacket in %LOCALAPPDATA%\STEFFLIX-TestShare (einmalig, PyPI) -> Server auf 127.0.0.1:47123 ->
# "adb reverse tcp:445 tcp:47123" (nur fuer den STEFFLIX-Emulator). In der App: Quelle \\127.0.0.1\STEFFLIX\, Benutzer stefflix,
# Passwort stefflix-test-1 (oeffentliches Testpasswort nur dieser lokalen Testumgebung). Windows belegt Port 445 selbst.
# Port 47123 (DEC-340): deutlich getrennt von den Ports anderer Projekte (z. B. 4445/4446 von Sync2PC). Ist der Port belegt,
# startet der Server nicht.
# adb schreibt Fehler nach stderr: in PowerShell 5.1 mit ErrorActionPreference Stop wuerde das Skript abbrechen, darum
# laufen alle adb-Aufrufe ueber $adbRun (lokal Continue) und werten $LASTEXITCODE aus.
# Reines ASCII (PowerShell 5.1).
param(
    [switch]$Stop,
    [switch]$Status,
    [string]$Share = 'STEFFLIX',
    [string]$User = 'stefflix',
    [string]$Password = 'stefflix-test-1',
    [int]$Port = 47123,
    [string]$Serial = '',
    [int]$ThrottleKBps = 0,
    [int]$DropAfterBytes = 0,
    [int]$DropCount = 1
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$shareDir = Join-Path $root '.claude\testenv'
$work = Join-Path $env:LOCALAPPDATA 'STEFFLIX-TestShare'
$venv = Join-Path $work 'venv'
$py = Join-Path $venv 'Scripts\python.exe'
$pidFile = Join-Path $work 'smb-testshare.pid'
$log = Join-Path $work 'smb-testshare.log'
$adb = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
if (-not (Test-Path $adb)) { $adb = 'adb' }
# adb ohne Abbruch bei Meldungen auf stderr; Ergebnis in $LASTEXITCODE
$adbRun = {
    $ErrorActionPreference = 'Continue'
    & $adb @args 2>$null
}
# Ohne -Serial: den laufenden STEFFLIX-Emulator suchen (fremde Emulatoren anderer Projekte nie anfassen)
if (-not $Serial) {
    foreach ($line in (& $adbRun devices | Select-Object -Skip 1)) {
        if ($line -match '^(emulator-\d+)\s+device') {
            # Serial sichern: das naechste -match ueberschreibt $Matches
            $candidate = $Matches[1]
            $n = @(& $adbRun -s $candidate emu avd name)
            if ($n.Count -gt 0 -and ([string]$n[0]).Trim() -match '^STEFFLIX_') { $Serial = $candidate; break }
        }
    }
}
$adbArgs = @(); if ($Serial) { $adbArgs = @('-s', $Serial) }
if (-not $Serial -and -not $Status -and -not $Stop) { Write-Warning 'Kein STEFFLIX-Emulator gefunden: Server startet, adb reverse wird uebersprungen (zuerst SFLIX_Tools\Test\avd.ps1 -Start).' }

$getServer = {
    if (Test-Path $pidFile) {
        $id = [int](Get-Content $pidFile -Raw)
        return Get-Process -Id $id -ErrorAction SilentlyContinue
    }
    return $null
}

if ($Status) {
    $p = & $getServer
    if ($p) { "Server laeuft (PID $($p.Id)), Freigabe $shareDir" } else { 'Server laeuft nicht' }
    if ($Serial) { "Emulator $Serial, adb reverse:"; & $adbRun @adbArgs reverse --list } else { 'Kein STEFFLIX-Emulator verbunden.' }
    return
}

if ($Stop) {
    $p = & $getServer
    if ($p) { Stop-Process -Id $p.Id -Force; 'Server beendet' } else { 'Server lief nicht' }
    if (Test-Path $pidFile) { [IO.File]::Delete($pidFile) }
    if ($Serial) { & $adbRun @adbArgs reverse --remove tcp:445 | Out-Null }
    return
}

if (-not (Test-Path (Join-Path $shareDir 'start.html'))) { throw "Testumgebung fehlt oder unvollstaendig: $shareDir" }

# 1) venv + impacket (einmalig)
if (-not (Test-Path $py)) {
    Write-Host 'Erstelle venv und installiere impacket (einmalig, PyPI) ...'
    New-Item -ItemType Directory -Force $work | Out-Null
    python -m venv $venv
    & $py -m pip install --quiet impacket
    if ($LASTEXITCODE -ne 0) { throw 'pip install impacket fehlgeschlagen' }
}

# 2) Server starten (falls nicht schon aktiv)
if (& $getServer) {
    'Server laeuft bereits'
} else {
    # Port darf nicht von einem anderen Programm belegt sein (z. B. Testserver anderer Projekte)
    $belegt = @(Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue)
    if ($belegt.Count -gt 0) {
        throw ("Port $Port ist schon belegt (PID " + (($belegt | ForEach-Object { $_.OwningProcess } | Select-Object -Unique) -join ', ') +
            '), vermutlich ein anderer Testserver. Nichts gestartet; anderen Port mit -Port waehlen, nie den eines anderen Projekts.')
    }
    $srvArgs = @((Join-Path $PSScriptRoot 'smb-testshare.py'), '--dir', $shareDir, '--share', $Share, '--user', $User,
        '--password', $Password, '--port', $Port, '--readonly')
    if ($ThrottleKBps -gt 0) { $srvArgs += @('--throttle-kbps', $ThrottleKBps) }
    if ($DropAfterBytes -gt 0) { $srvArgs += @('--drop-after-bytes', $DropAfterBytes, '--drop-count', $DropCount) }
    $quoted = @($srvArgs | ForEach-Object { '"' + ([string]$_).Replace('"', '\"') + '"' }) -join ' '
    $proc = Start-Process -FilePath $py -ArgumentList $quoted -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $log -RedirectStandardError "$log.err"
    [IO.File]::WriteAllText($pidFile, [string]$proc.Id)
    Start-Sleep -Seconds 2
    if ($proc.HasExited) { throw "Server beendet sich sofort, siehe $log.err" }
}

# 3) Emulator: 445 -> Host:$Port (Port 445 braucht im Emulator Root: bei Fehler "adb root", nur google_apis-Abbilder)
if ($Serial) {
    & $adbRun @adbArgs reverse tcp:445 "tcp:$Port" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        & $adbRun @adbArgs root | Out-Null
        Start-Sleep -Seconds 3
        & $adbRun @adbArgs wait-for-device | Out-Null
        & $adbRun @adbArgs reverse tcp:445 "tcp:$Port" | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Warning 'adb reverse auf Port 445 fehlgeschlagen (kein Root im Abbild?)' }
    }
    "Emulator: $Serial (adb immer mit -s $Serial)"
}

"Bereit. Quelle in der App: \\127.0.0.1\$($Share.ToUpper())\, Benutzer $User, Passwort $Password"
"Freigabeordner (nur lesend): $shareDir"
