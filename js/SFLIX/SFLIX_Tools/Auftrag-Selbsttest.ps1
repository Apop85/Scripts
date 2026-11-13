# Auftrag-Selbsttest.ps1 - SFLIX Selbsttest fuer Auftrag.ps1
#
# Arbeitet ausschliesslich auf Kopien von coordination/ bzw. auf kleinen Test-Wurzeln in einem Temp-Ordner.
# Der echte Bestand wird nur gelesen (Kopie) und nie veraendert.
# Aufruf: powershell -NoProfile -ExecutionPolicy Bypass -File tools\Auftrag-Selbsttest.ps1 [-Fall 1,2,...] [-Quelle <coordination>]
# Exit-Code: 0 alle Faelle bestanden, 1 mindestens ein Fall fehlgeschlagen.
# Reines ASCII (PowerShell 5.1); Sonderzeichen entstehen zur Laufzeit.

param(
    [string]$Fall,   # z. B. "1,8,13" (bei -File kommt eine Kommaliste als ein String an)
    [string]$Quelle,
    [string]$Arbeitsordner
)
$ErrorActionPreference = 'Stop'
$FallListe = @(); if ($Fall) { $FallListe = @($Fall -split '[,; ]+' | Where-Object { $_ } | ForEach-Object { [int]$_ }) }
$Script = Join-Path $PSScriptRoot 'Auftrag.ps1'
if (-not $Arbeitsordner) {
    $Arbeitsordner = Join-Path ([IO.Path]::GetTempPath()) (
        'sflix-auftrag-selbsttest-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    )
}

$Arbeitsordner = [IO.Path]::GetFullPath($Arbeitsordner)

if (-not (Test-Path -LiteralPath $PSScriptRoot -PathType Container)) {
    throw "Skriptverzeichnis nicht gefunden: $PSScriptRoot"
}

if (-not (Test-Path -LiteralPath $Script -PathType Leaf)) {
    throw "Auftrag.ps1 nicht gefunden: $Script"
}

[void](New-Item -ItemType Directory -Path $Arbeitsordner -Force)

Write-Output "SFLIX Selbsttest"
Write-Output "Skript: $Script"
Write-Output "Arbeitsordner: $Arbeitsordner"
Write-Output "Testdaten: ausschliesslich synthetisch"

[void](New-Item -ItemType Directory -Path $Arbeitsordner -Force)
$env:AUFTRAG_TESTMODUS = '1'
$Utf8 = New-Object System.Text.UTF8Encoding($false)
function U([string]$s) { return [regex]::Unescape($s) }

$script:Results = New-Object System.Collections.Generic.List[object]
$script:Cur = ''
$script:Counter = 0

function Check([string]$name, [bool]$ok, [string]$detail) {
    [void]$script:Results.Add([pscustomobject]@{ Fall = $script:Cur; Name = $name; Ok = $ok; Detail = $detail })
    if ($ok) { Write-Output ("  ok     {0}" -f $name) } else { Write-Output ("  FEHLER {0} -> {1}" -f $name, $detail) }
}
function Run([string]$root, [string[]]$a) {
    $all = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $Script) + $a + @('-Wurzel', $root)
    $out = & powershell @all
    return [pscustomobject]@{ Exit = $LASTEXITCODE; Out = ($out -join "`n") }
}
function RunJson([string]$root, [string[]]$a) {
    $r = Run $root ($a + @('-Json'))
    $j = $null
    try { $j = $r.Out | ConvertFrom-Json } catch { }
    return [pscustomobject]@{ Exit = $r.Exit; Out = $r.Out; J = $j }
}
function New-Wurzel([string]$name) {
    $w = Join-Path $Arbeitsordner $name
    if (Test-Path $w) { Remove-Item $w -Recurse -Force }
    foreach ($d in @('tasks\OPEN', 'tasks\IN_PROGRESS', 'tasks\BLOCKED', 'tasks\COMPLETED', 'inbox')) { [void](New-Item -ItemType Directory -Path (Join-Path $w $d) -Force) }
    return $w
}
function New-Kopie([string]$name) {
    $w = Join-Path $Arbeitsordner $name
    if (Test-Path $w) { Remove-Item $w -Recurse -Force }
    [void](New-Item -ItemType Directory -Path $w)
    foreach ($x in Get-ChildItem -LiteralPath $Quelle -Force) {
        if (@('archive', '.history', '.lock', '.audit.log') -contains $x.Name) { continue }
        Copy-Item -LiteralPath $x.FullName -Destination (Join-Path $w $x.Name) -Recurse -Force
    }
    return $w
}
function MkTask([string]$root, [string]$von, [string]$an, [string]$titel, [string]$prio) {
    if (-not $titel) { $titel = 'Testauftrag ' + (++$script:Counter) }
    if (-not $prio) { $prio = 'LOW' }
    $r = RunJson $root @('erstellen', '-Rolle', $von, '-An', $an, '-Titel', $titel, '-Prio', $prio, '-Aufgabe', 'Aufgabe', '-Ergebnis', 'Ergebnis')
    if ($r.Exit -ne 0) { throw "MkTask fehlgeschlagen: $($r.Out)" }
    return $r.J.ids[0]
}
function TaskFile([string]$root, [string]$id) {
    foreach ($f in @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')) {
        $p = Join-Path $root "tasks\$f\$id.md"
        if (Test-Path -LiteralPath $p) { return $p }
    }
    return $null
}
function ReadT([string]$path) { return [IO.File]::ReadAllText($path, $Utf8) }
function Snapshot([string]$root) {
    $h = @{}
    foreach ($f in Get-ChildItem -LiteralPath $root -Recurse -File -Force) {
        $h[$f.FullName.Substring($root.Length)] = (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash + '|' + [int]$f.Attributes + '|' + $f.Length
    }
    return $h
}
function Same-Snapshot($a, $b) {
    if ($a.Count -ne $b.Count) { return $false }
    foreach ($k in $a.Keys) { if (-not $b.ContainsKey($k) -or $a[$k] -ne $b[$k]) { return $false } }
    return $true
}
function Add-Bom([string]$w) {
    # Der echte Bestand enthaelt nach check -Fix keine BOM-Dateien mehr: in der Kopie eine BOM-Datei nachbilden.
    $x = Get-ChildItem (Join-Path $w 'tasks\COMPLETED') -File | Where-Object { $_.Name -match '^SFLIX-' } | Select-Object -First 1
    $ro = $x.IsReadOnly
    if ($ro) { Set-ItemProperty -LiteralPath $x.FullName -Name IsReadOnly -Value $false }
    $b = [IO.File]::ReadAllBytes($x.FullName)
    if (-not ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB)) { [IO.File]::WriteAllBytes($x.FullName, ([byte[]]@(0xEF, 0xBB, 0xBF) + $b)) }
    if ($ro) { Set-ItemProperty -LiteralPath $x.FullName -Name IsReadOnly -Value $true }
}
function Fall([int]$n, [string]$titel, [scriptblock]$body) {
    if ($FallListe.Count -gt 0 -and ($FallListe -notcontains $n)) { return }
    $script:Cur = "$n"
    Write-Output ("Fall {0}: {1}" -f $n, $titel)
    try { & $body }
    catch { Check 'Ausnahme im Testfall' $false $_.Exception.Message }
}

# ------------------------------------------------------------------ Fall 1
Fall 1 'Rundlauf ueber synthetische SFLIX-Task-Dateien (byteidentisch ohne BOM)' {
    $w = New-Wurzel 'f1'

    # Vier synthetische Tickets erzeugen.
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Test OPEN'
    $b = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Test IN_PROGRESS'
    $c = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Test BLOCKED'
    $d = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Test COMPLETED'

    # In die jeweiligen Statusordner verschieben.
    $r = Run $w @(
        'status', '-Id', $b, '-Neu', 'IN_PROGRESS',
        '-Rolle', 'JavaScript Developer'
    )
    if ($r.Exit -ne 0) { throw $r.Out }

    $r = Run $w @(
        'status', '-Id', $c, '-Neu', 'BLOCKED',
        '-Frage', 'Synthetische Testfrage',
        '-Rolle', 'JavaScript Developer'
    )
    if ($r.Exit -ne 0) { throw $r.Out }

    $r = Run $w @(
        'status', '-Id', $d, '-Neu', 'IN_PROGRESS',
        '-Rolle', 'JavaScript Developer'
    )
    if ($r.Exit -ne 0) { throw $r.Out }

    $r = Run $w @(
        'abschliessen', '-Id', $d,
        '-Rolle', 'JavaScript Developer',
        '-Arbeiten', 'Synthetischer Test',
        '-Tests', 'Statuswechsel erfolgreich'
    )
    if ($r.Exit -ne 0) { throw $r.Out }

    # Exakt die vier gültigen Task-Dateien erfassen.
    $files = @()

    foreach ($folder in @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')) {
        $dir = Join-Path $w "tasks\$folder"

        $files += @(
            Get-ChildItem -LiteralPath $dir -File -Force |
            Where-Object {
                $_.Name -match '^SFLIX-\d{8}-\d{3,}\.md$'
            }
        )
    }

    Check 'vier synthetische Tickets angelegt' ($files.Count -eq 4) "Anzahl $($files.Count)"

    # BOM-Freiheit prüfen.
    $bomCount = 0

    foreach ($f in $files) {
        $bytes = [IO.File]::ReadAllBytes($f.FullName)

        if ($bytes.Length -ge 3 -and
            $bytes[0] -eq 0xEF -and
            $bytes[1] -eq 0xBB -and
            $bytes[2] -eq 0xBF) {
            $bomCount++
        }
    }

    Check 'alle Task-Dateien ohne UTF-8-BOM' ($bomCount -eq 0) "BOM-Dateien: $bomCount"

    # Rundlauf ausführen.
    $r = Run $w @('_rundlauf')

    Check 'rundlauf exit 0, keine Abweichung' (
        $r.Exit -eq 0 -and $r.Out -match '0 Abweichungen'
    ) $r.Out

    Check 'vier Dateien byteidentisch gelesen' (
        $r.Out -match 'rundlauf: 4 Dateien, 0 Abweichungen'
    ) $r.Out
}

# ------------------------------------------------------------------ Fall 2
Fall 2 'Parallel erstellen: 10 Prozesse x 5 Auftraege' {
    $w = New-Wurzel 'f2'
    [void](MkTask $w 'JavaScript Developer' 'QA Engineer' 'Start')
    $before = @(Get-ChildItem (Join-Path $w 'tasks') -Recurse -File).Count
    $jobs = @()
    foreach ($j in 1..10) {
        $jobs += Start-Job -ArgumentList $Script, $w, $j -ScriptBlock {
            param($s, $w, $j)
            foreach ($k in 1..5) {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $s erstellen -Wurzel $w -Rolle 'JavaScript Developer' -An 'QA Engineer' -Titel "Parallel $j-$k" -Prio LOW -Aufgabe 'x' -Ergebnis 'y' | Out-Null
                $LASTEXITCODE
            }
        }
    }
    $codes = @($jobs | Wait-Job | Receive-Job)
    $jobs | Remove-Job
    Check '50 Aufrufe mit Exit 0' (@($codes | Where-Object { $_ -ne 0 }).Count -eq 0 -and $codes.Count -eq 50) ("Codes: " + ($codes -join ','))
    $files = @(Get-ChildItem (Join-Path $w 'tasks\OPEN') -File | Where-Object { $_.Name -match '^SFLIX-' })
    Check '51 Dateien' ($files.Count -eq 51) "Anzahl $($files.Count)"
    $nums = @($files | ForEach-Object { [int]($_.BaseName -replace '^SFLIX-\d{8}-', '') } | Sort-Object)
    $seq = $true; for ($i = 0; $i -lt $nums.Count; $i++) { if ($nums[$i] -ne ($i + 1)) { $seq = $false } }
    Check 'Nummern lueckenlos und eindeutig' ($seq -and (@($nums | Select-Object -Unique).Count -eq $nums.Count)) ($nums -join ',')
    $idx = ReadT (Join-Path $w 'TASK-INDEX.md')
    $rows = @(($idx -split "`n") | Where-Object { $_ -match '^\| SFLIX-' }).Count
    Check 'Index enthaelt alle 51 Zeilen' ($rows -eq 51) "Zeilen $rows"
    $inb = ReadT (Join-Path $w 'inbox\qa.md')
    $rows2 = @(($inb -split "`n") | Where-Object { $_ -match '^\| SFLIX-' }).Count
    Check 'Posteingang QA enthaelt alle 51 Zeilen' ($rows2 -eq 51) "Zeilen $rows2"
    $c = Run $w @('check')
    Check 'check danach ohne Fehler' ($c.Exit -eq 0) $c.Out
}

# ------------------------------------------------------------------ Fall 3
Fall 3 'Parallele Statuswechsel und Anhaenge: keine verlorene Verlaufszeile' {
    $w = New-Wurzel 'f3'
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Anhaenge'
    $r = Run $w @('status', '-Id', $a, '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer')
    Check 'Uebernahme' ($r.Exit -eq 0) $r.Out
    $bs = @(); foreach ($i in 1..5) { $bs += MkTask $w 'QA Engineer' 'JavaScript Developer' "Wechsel $i" }
    $jobs = @()
    foreach ($j in 1..6) {
        $jobs += Start-Job -ArgumentList $Script, $w, $j, $a -ScriptBlock {
            param($s, $w, $j, $a)
            foreach ($k in 1..3) {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $s aendern -Wurzel $w -Rolle 'QA Engineer' -Id $a -Feld 'Offene Fragen' -Wert "q$j-$k" -Notiz "n$j-$k" | Out-Null
                $LASTEXITCODE
            }
        }
    }
    foreach ($b in $bs) {
        $jobs += Start-Job -ArgumentList $Script, $w, $b -ScriptBlock {
            param($s, $w, $b)
            & powershell -NoProfile -ExecutionPolicy Bypass -File $s status -Wurzel $w -Rolle 'JavaScript Developer' -Id $b -Neu IN_PROGRESS | Out-Null
            $LASTEXITCODE
        }
    }
    $codes = @($jobs | Wait-Job | Receive-Job); $jobs | Remove-Job
    Check 'alle Aufrufe Exit 0' (@($codes | Where-Object { $_ -ne 0 }).Count -eq 0) ($codes -join ',')
    $txt = ReadT (TaskFile $w $a)
    $missing = @(); foreach ($j in 1..6) { foreach ($k in 1..3) { if (-not $txt.Contains("n$j-$k") -or -not $txt.Contains("q$j-$k")) { $missing += "$j-$k" } } }
    Check 'alle 18 Anhaenge und Verlaufszeilen vorhanden' ($missing.Count -eq 0) ($missing -join ',')
    $okb = $true; foreach ($b in $bs) { if (-not (Test-Path (Join-Path $w "tasks\IN_PROGRESS\$b.md"))) { $okb = $false } }
    Check 'parallele Statuswechsel verschiedener Auftraege' $okb ''
    $c = Run $w @('check')
    Check 'check danach ohne Fehler' ($c.Exit -eq 0) $c.Out
}

# ------------------------------------------------------------------ Fall 4
Fall 4 'Sperre: Zeitlimit (Exit 4) und sofortige Freigabe nach Prozessende' {
    $w = New-Wurzel 'f4'
    [void](MkTask $w 'JavaScript Developer' 'QA Engineer' 'Sperrtest')
    $lock = Join-Path $w '.lock'
    $fs = [IO.FileStream]::new($lock, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
    $n0 = @(Get-ChildItem (Join-Path $w 'tasks\OPEN')).Count
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $r = Run $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'blockiert', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y', '-SperrZeitlimitSek', '2')
    $sw.Stop()
    $fs.Dispose()
    Check 'Exit 4 nach Zeitlimit' ($r.Exit -eq 4) "Exit $($r.Exit) $($r.Out)"
    Check 'Wartezeit ungefaehr Zeitlimit' ($sw.Elapsed.TotalSeconds -ge 1.5) ("{0:N1} s" -f $sw.Elapsed.TotalSeconds)
    Check 'nichts geschrieben' (@(Get-ChildItem (Join-Path $w 'tasks\OPEN')).Count -eq $n0) ''
    # Halter-Prozess, der die Sperre haelt und hart beendet wird
    $holder = Join-Path $Arbeitsordner 'f4-halter.ps1'
    [IO.File]::WriteAllText($holder, '$f=[IO.FileStream]::new($args[0],[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None); Start-Sleep -Seconds 120', $Utf8)
    $pr = Start-Process powershell -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $holder, $lock) -PassThru -WindowStyle Hidden
    $held = $false
    foreach ($i in 1..40) {
        try { $t = [IO.FileStream]::new($lock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None); $t.Dispose(); Start-Sleep -Milliseconds 250 } catch { $held = $true; break }
    }
    Check 'Halter haelt die Sperre' $held ''
    $r2 = Run $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'waehrend Halter', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y', '-SperrZeitlimitSek', '2')
    Check 'waehrend der Halter lebt: Exit 4' ($r2.Exit -eq 4) $r2.Out
    Stop-Process -Id $pr.Id -Force
    $pr.WaitForExit()

    $pr.Refresh()
    $childExit = $pr.ExitCode

    Check 'Exit 3 bei erkannter Fremdaenderung' (
        $childExit -eq 3
    ) "Exit=[$childExit]; Prozess beendet=$($pr.HasExited); PID=$($pr.Id)"

    $r3 = Run $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'nach Halter', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y', '-SperrZeitlimitSek', '2')
    Check 'nach hartem Ende des Halters sofort frei (Exit 0)' ($r3.Exit -eq 0) $r3.Out
}

# ------------------------------------------------------------------ Fall 5
Fall 5 'Abbruch an jedem Schritt: check meldet, -Fix stellt her, keine Datei geht verloren' {
    function CountAll([string]$w) { return @(Get-ChildItem (Join-Path $w 'tasks') -Recurse -File -Force | Where-Object { $_.Name -match '^SFLIX-' }).Count }
    # erstellen: nach-anlegen (leere Datei), nach-schreiben (Uebersicht fehlt)
    $w = New-Wurzel 'f5a'
    [void](MkTask $w 'JavaScript Developer' 'QA Engineer' 'Basis')
    $r = Run $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'abbruch', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y', '-Haltepunkt', 'nach-anlegen')
    Check 'erstellen nach-anlegen: Abbruch (99)' ($r.Exit -eq 99) "Exit $($r.Exit)"
    $leer = @(Get-ChildItem (Join-Path $w 'tasks\OPEN') | Where-Object { $_.Length -eq 0 })
    Check 'leere Datei bleibt liegen' ($leer.Count -eq 1) ''
    $c = Run $w @('check')
    Check 'check meldet unvollstaendig (Exit 5)' ($c.Exit -eq 5 -and $c.Out -match 'unvollstaendig') $c.Out
    $c2 = Run $w @('check', '-Fix', '-Rolle', 'JavaScript Developer')
    Check '-Fix loescht die leere Datei nicht' (@(Get-ChildItem (Join-Path $w 'tasks\OPEN') | Where-Object { $_.Length -eq 0 }).Count -eq 1) $c2.Out
    $r2 = RunJson $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'danach', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y')
    Check 'Nummer der leeren Datei bleibt vergeben' ($r2.Exit -eq 0 -and $r2.J.ids[0] -match '-003$') "$($r2.Out)"

    $w = New-Wurzel 'f5b'
    [void](MkTask $w 'JavaScript Developer' 'QA Engineer' 'Basis')
    $r = Run $w @('erstellen', '-Rolle', 'JavaScript Developer', '-An', 'QA Engineer', '-Titel', 'abbruch2', '-Prio', 'LOW', '-Aufgabe', 'x', '-Ergebnis', 'y', '-Haltepunkt', 'nach-schreiben')
    Check 'erstellen nach-schreiben: Abbruch' ($r.Exit -eq 99) ''
    $c = Run $w @('check')
    Check 'check meldet Uebersicht (Exit 5)' ($c.Exit -eq 5 -and $c.Out -match 'uebersicht') $c.Out
    $f = Run $w @('check', '-Fix', '-Rolle', 'JavaScript Developer')
    Check '-Fix stellt Uebersicht her (Exit 0)' ($f.Exit -eq 0) $f.Out
    Check 'keine Datei verloren (2 Auftraege)' ((CountAll $w) -eq 2) ''

    # Statuswechsel an jedem Haltepunkt
    foreach ($hp in @('nach-tmp', 'nach-replace', 'nach-move', 'nach-index')) {
        $w = New-Wurzel ('f5-' + $hp)
        $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Wechsel'
        $n0 = CountAll $w
        $r = Run $w @('status', '-Id', $a, '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer', '-Haltepunkt', $hp)
        Check "status Abbruch bei $hp (99)" ($r.Exit -eq 99) "Exit $($r.Exit) $($r.Out)"
        $c = Run $w @('check')
        $expect5 = ($hp -ne 'nach-tmp')
        $c1 = if ($hp -eq 'nach-tmp') { ($c.Exit -eq 5 -and $c.Out -match 'tmp') } else { ($c.Exit -eq 5) }
        Check "check meldet nach $hp" $c1 $c.Out
        $f = Run $w @('check', '-Fix', '-Rolle', 'JavaScript Developer')
        Check "-Fix nach ${hp}: check danach sauber" ($f.Exit -eq 0) $f.Out
        Check "keine Datei verloren nach $hp" ((CountAll $w) -eq $n0) ''
        if ($hp -ne 'nach-tmp') {
            Check "Status nach $hp in IN_PROGRESS" (Test-Path (Join-Path $w "tasks\IN_PROGRESS\$a.md")) ''
        }
        else {
            Check 'nach-tmp: Original unveraendert in OPEN' (Test-Path (Join-Path $w "tasks\OPEN\$a.md")) ''
            Check 'nach-tmp: Temp-Datei in .history\_verwaist' (@(Get-ChildItem (Join-Path $w '.history\_verwaist') -ErrorAction SilentlyContinue).Count -ge 1) ''
        }
    }
    # abschliessen: Abbruch nach dem Verschieben -> Schreibschutz fehlt
    $w = New-Wurzel 'f5-abschluss'
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Abschluss'
    [void](Run $w @('status', '-Id', $a, '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer'))
    $r = Run $w @('abschliessen', '-Id', $a, '-Rolle', 'JavaScript Developer', '-Arbeiten', 'a', '-Tests', 't', '-Haltepunkt', 'nach-move')
    Check 'abschliessen Abbruch nach Verschieben' ($r.Exit -eq 99) $r.Out
    $c = Run $w @('check')
    Check 'check meldet Schreibschutz' ($c.Exit -eq 5 -and $c.Out -match 'schreibschutz') $c.Out
    $f = Run $w @('check', '-Fix', '-Rolle', 'JavaScript Developer')
    Check '-Fix setzt Schreibschutz, check sauber' ($f.Exit -eq 0 -and (Get-Item (Join-Path $w "tasks\COMPLETED\$a.md")).IsReadOnly) $f.Out
    # Nummer nicht wiederverwenden, wenn ein Auftrag von Hand entfernt wurde (Ordner in .history zaehlt)
    $w = New-Wurzel 'f5-nummer'
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'A'
    [void](Run $w @('aendern', '-Id', $a, '-Rolle', 'JavaScript Developer', '-Feld', 'Technischer Kontext', '-Wert', 'x'))
    Remove-Item (TaskFile $w $a) -Force
    $b = MkTask $w 'QA Engineer' 'JavaScript Developer' 'B'
    Check 'entfernte Nummer wird nicht erneut vergeben' ($b -match '-002$') $b
}

# ------------------------------------------------------------------ Fall 6
Fall 6 'Aenderung am Skript vorbei zwischen Lesen und Ersetzen: Exit 3, nichts geschrieben' {

    $w = New-Wurzel 'f6'

    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Konflikttest'
    $p = TaskFile $w $a

    $stdout = Join-Path $w 'child-stdout.log'
    $stderr = Join-Path $w 'child-stderr.log'

    # Argumente sicher fuer Windows-Prozessstart quotieren.
    function Quote-Argument([string]$value) {
        $escaped = $value -replace '(\\*)"', '$1$1\"'
        $escaped = $escaped -replace '(\\+)$', '$1$1'
        return '"' + $escaped + '"'
    }

    $argsList = @(
        '-NoProfile'
        '-ExecutionPolicy'
        'Bypass'
        '-File'
        $Script
        'status'
        '-Id'
        $a
        '-Neu'
        'IN_PROGRESS'
        '-Rolle'
        'JavaScript Developer'
        '-Wurzel'
        $w
        '-Haltepunkt'
        'schlaf-nach-tmp'
    )

    $argumentLine = (
        $argsList | ForEach-Object {
            Quote-Argument ([string]$_)
        }
    ) -join ' '

    $exe = Join-Path $PSHOME 'powershell.exe'

    $pr = Start-Process `
        -FilePath $exe `
        -ArgumentList $argumentLine `
        -PassThru `
        -WindowStyle Hidden `
        -RedirectStandardOutput $stdout `
        -RedirectStandardError $stderr

    # Auf die Temp-Datei warten.
    $tmp = @()
    $watch = [Diagnostics.Stopwatch]::StartNew()

    while ($watch.Elapsed.TotalSeconds -lt 15) {

        $tmp = @(
            Get-ChildItem -LiteralPath (Join-Path $w 'tasks\OPEN') -Force |
            Where-Object { $_.Name -match '\.tmp-' }
        )

        if ($tmp.Count -gt 0 -or $pr.HasExited) {
            break
        }

        Start-Sleep -Milliseconds 100
    }

    $watch.Stop()

    if ($pr.HasExited) {
        $out = if (Test-Path $stdout) {
            Get-Content -LiteralPath $stdout -Raw
        } else { '' }

        $err = if (Test-Path $stderr) {
            Get-Content -LiteralPath $stderr -Raw
        } else { '' }

        throw "Kindprozess vor Temp-Datei beendet. Exit=$($pr.ExitCode)`nSTDOUT:`n$out`nSTDERR:`n$err"
    }

    Check 'Prozess hat Temp-Datei geschrieben und wartet' (
        $tmp.Count -gt 0
    ) "Temp-Dateien: $($tmp.Count)"

    if ($tmp.Count -eq 0) {
        Stop-Process -Id $pr.Id -Force
        throw 'Temp-Datei nicht innerhalb von 15 Sekunden erzeugt.'
    }

    # Fremdaenderung am Original simulieren.
    [IO.File]::AppendAllText(
        $p,
        "`r`nVon-Hand-Zeile`r`n",
        $Utf8
    )

    $pr.WaitForExit()

    Check 'Exit 3 bei erkannter Fremdaenderung' (
        $pr.ExitCode -eq 3
    ) "Exit $($pr.ExitCode)"

    $txt = ReadT $p

    Check 'Handaenderung bleibt, Status unveraendert OPEN' (
        $txt.Contains('Von-Hand-Zeile') -and
        $txt -match '- Status: OPEN'
    ) ''

    $remaining = @(
        Get-ChildItem -LiteralPath (Join-Path $w 'tasks\OPEN') -Force |
        Where-Object { $_.Name -match '\.tmp-' }
    )

    $orphanDir = Join-Path $w '.history\_verwaist'

    $orphans = @()

    if (Test-Path -LiteralPath $orphanDir) {
        $orphans = @(Get-ChildItem -LiteralPath $orphanDir -File -Force)
    }

    Check 'Temp-Datei nach .history\_verwaist verschoben' (
        $remaining.Count -eq 0 -and $orphans.Count -ge 1
    ) "Verblieben: $($remaining.Count); verwaist: $($orphans.Count)"

    $pr.Dispose()
}

# ------------------------------------------------------------------ Fall 7
Fall 7 'Berechtigungen: Wechsel x Rollen, mit und ohne -Benutzerfreigabe' {
    $w = New-Wurzel 'f7'
$roles = @(
        'Technical Product Owner',
        'Software Architect',
        'JavaScript Developer',
        'Debugging Specialist',
        'QA Engineer',
        'Security Auditor',
        'Performance Engineer',
        'C#/.NET Developer',
        'Android Developer',
        'Documentation Assistant',
        'Release Manager',
        'Benutzer'
    )
    foreach ($c in $cmds) {
        $r = Run $w ($c + @('-WhatIf'))
        Check ("-WhatIf $($c[0]): Exit 0/5 und Plan ausgegeben") (($r.Exit -eq 0 -or $r.Exit -eq 5) -and ($r.Out -match 'WHATIF' -or $c[0] -eq 'check')) "Exit $($r.Exit) $($r.Out)"
    }
    $after = Snapshot $w
    Check 'alle Dateien (Hash, Attribute, Groesse) unveraendert, keine neuen Dateien' (Same-Snapshot $before $after) ''
}

# ------------------------------------------------------------------ Fall 13
Fall 13 'Pfadpruefung: nichts ausserhalb von coordination/' {
    $w = New-Wurzel 'f13'
    $ausserhalb = Join-Path $Arbeitsordner 'f13-ausserhalb'
    [void](New-Item -ItemType Directory -Path $ausserhalb -Force)
    $cases = @(
        @{ n = 'absoluter Pfad ausserhalb'; p = (Join-Path $ausserhalb 'x.md'); e = 1 },
        @{ n = 'Eltern-Ordner ueber ..'; p = ($w + '\..\x.md'); e = 1 },
        @{ n = 'tasks\..\..'; p = ($w + '\tasks\..\..\x.md'); e = 1 },
        @{ n = 'Praefix-Ordner (coordination-x)'; p = ($w + '-x\tasks\a.md'); e = 1 },
        @{ n = 'gueltiger Pfad innerhalb'; p = ($w + '\tasks\OPEN\a.md'); e = 0 }
    )
    foreach ($c in $cases) {
        $r = Run $w @('_pfad', '-Wert', $c.p)
        Check ($c.n) ($r.Exit -eq $c.e) "Exit $($r.Exit) $($r.Out)"
    }
    # Junction innerhalb der Wurzel
    $jx = Join-Path $w 'tasks\OPEN\jx'
    cmd /c mklink /J "$jx" "$ausserhalb" | Out-Null
    $r = Run $w @('_pfad', '-Wert', (Join-Path $jx 'f.md'))
    Check 'Junction wird abgelehnt (Exit 1)' ($r.Exit -eq 1) "Exit $($r.Exit) $($r.Out)"
    cmd /c rmdir "$jx" | Out-Null
    Check 'Ziel der Junction unveraendert (leer)' (@(Get-ChildItem $ausserhalb -Force).Count -eq 0) ''
    $r = Run $w @('status', '-Id', '../x', '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer')
    Check 'manipulierte -Id abgelehnt (Exit 1)' ($r.Exit -eq 1) $r.Out
    $env:AUFTRAG_TESTMODUS = ''
    $r = Run $w @('liste')
    Check '-Wurzel ohne Testmodus abgelehnt (Exit 1)' ($r.Exit -eq 1) $r.Out
    $env:AUFTRAG_TESTMODUS = '1'
}

# ------------------------------------------------------------------ Fall 14
Fall 14 'Index-Defekt (Leerzeile, fehlende und ueberzaehlige Zeile) wird durch die Erzeugung behoben' {
    $w = New-Wurzel 'f14'
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Eins'
    $b = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Zwei'
    $c = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Drei'
    $ip = Join-Path $w 'TASK-INDEX.md'
    $lines = @((ReadT $ip) -split "`r?`n")
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($l in $lines) {
        if ($l -match [regex]::Escape($b)) { continue }                                  # Zeile fehlt
        if ($l -match [regex]::Escape($c)) { [void]$out.Add('') }                         # Leerzeile vor Zeile
        [void]$out.Add($l)
    }
    [void]$out.Add('| SFLIX-20990101-999 | Erfundene Zeile | X | Y | LOW | OPEN | tasks/OPEN/SFLIX-20990101-999.md |')
    [IO.File]::WriteAllText($ip, ($out -join "`r`n"), $Utf8)
    $chk = Run $w @('check')
    Check 'check erkennt den Defekt (Exit 5, fehlt/ueberzaehlig)' ($chk.Exit -eq 5 -and $chk.Out -match 'fehlt' -and $chk.Out -match 'berz') $chk.Out
    [void](MkTask $w 'QA Engineer' 'JavaScript Developer' 'Vier')
    $idx = ReadT $ip
    $tab = @(($idx -split "`n") | Where-Object { $_ -match '^\|' })
    Check 'Tabelle ohne Leerzeile, 4 Aufgabenzeilen + Kopf' ($tab.Count -eq 6) "Zeilen $($tab.Count)"
    Check 'erfundene Zeile entfernt' (-not $idx.Contains('20990101-999')) ''
    Check 'fehlende Zeile wieder vorhanden' ($idx.Contains($b)) ''
    $chk = Run $w @('check')
    Check 'check danach sauber' ($chk.Exit -eq 0) $chk.Out
    Check 'Zaehler Letzte vergebene Nummer = 004' ($idx -match 'Letzte vergebene Nummer: 004') ''
}

# ------------------------------------------------------------------ Fall 15
Fall 15 '.history-Obergrenze, liste mit allen Filtern, COMPLETED gegen CANCELLED, BLOCKED-Hinweis' {
    $w = New-Wurzel 'f15'
    $a = MkTask $w 'QA Engineer' 'JavaScript Developer' 'Historie'
    foreach ($i in 1..24) { [void](Run $w @('aendern', '-Id', $a, '-Rolle', 'JavaScript Developer', '-Feld', 'Technischer Kontext', '-Wert', "v$i")) }
    $n = @(Get-ChildItem (Join-Path $w ".history\$a") -File).Count
    Check 'Obergrenze 20 Fassungen' ($n -eq 20) "Fassungen $n"
    $newest = ReadT (TaskFile $w $a)
    Check 'neueste Fassung ist die aktuelle (v24)' ($newest.Contains('v24')) ''
    $oldest = @(Get-ChildItem (Join-Path $w ".history\$a") -File | Sort-Object Name)[0]
    Check 'aelteste Sicherung enthaelt v4 (v1..v4 entfernt, nur in .history)' ((ReadT $oldest.FullName).Contains('v4') -or (ReadT $oldest.FullName).Contains('v3')) $oldest.Name
    Check 'Task-Datei selbst nie entfernt' (Test-Path (TaskFile $w $a)) ''

}
Fall 15 'liste (Fortsetzung)' {
    $w = New-Wurzel 'f15b'
    $mk = {
        param($von, $an, $titel, $prio)
        $a = @('erstellen', '-Rolle', $von, '-An', $an, '-Titel', $titel, '-Prio', $prio, '-Aufgabe', 'A', '-Ergebnis', 'E')
        if ($prio -eq 'HIGH') { $a += @('-Begruendung', 'weil') }
        $r = RunJson $w $a
        if ($r.Exit -ne 0) { throw $r.Out }
        return $r.J.ids[0]
    }
    $t1 = & $mk 'QA Engineer' 'JavaScript Developer' 'Alpha Suchwort' 'HIGH'
    $t2 = & $mk 'QA Engineer' 'JavaScript Developer' 'Beta' 'LOW'
    $t3 = & $mk 'Software Architect' 'JavaScript Developer' 'Gamma' 'MEDIUM'
    $t4 = & $mk 'Software Architect' 'Documentation Assistant' 'Delta' 'LOW'
    $t5 = & $mk 'Documentation Assistant' 'JavaScript Developer' 'Epsilon' 'LOW'
    [void](Run $w @('status', '-Id', $t2, '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer'))
    [void](Run $w @('status', '-Id', $t3, '-Neu', 'BLOCKED', '-Frage', 'q', '-Rolle', 'JavaScript Developer'))
    [void](Run $w @('status', '-Id', $t4, '-Neu', 'IN_PROGRESS', '-Rolle', 'Documentation Assistant'))
    [void](Run $w @('abschliessen', '-Id', $t4, '-Rolle', 'Documentation Assistant', '-Arbeiten', 'a', '-Tests', 't'))
    [void](Run $w @('status', '-Id', $t5, '-Neu', 'CANCELLED', '-Notiz', 'weg', '-Rolle', 'Documentation Assistant'))
    $ids = { param($r) @($r.J.daten | ForEach-Object { $_.id }) }
    $r = RunJson $w @('liste', '-Rolle', 'JavaScript Developer')
    $l = @(& $ids $r)
    Check 'Standard mit -Rolle (Posteingang): OPEN, IN_PROGRESS und BLOCKED der Rolle' ($l.Count -eq 3 -and $l -contains $t1 -and $l -contains $t2 -and $l -contains $t3) ($l -join ',')
    $r = RunJson $w @('liste', '-An', 'JavaScript Developer')
    $l = @(& $ids $r)
    Check 'Standard ohne -Rolle: OPEN und IN_PROGRESS' ($l.Count -eq 2 -and $l -contains $t1 -and $l -contains $t2) ($l -join ',')
    Check 'Hinweis "1 BLOCKED ausgeblendet"' ((@($r.J.meldungen) -join ' ') -match '1 BLOCKED ausgeblendet') $r.Out
    $r = Run $w @('liste', '-Rolle', 'JavaScript Developer', '-MitDateien')
    Check '-MitDateien: Exit 0' ($r.Exit -eq 0) $r.Out
    $r = RunJson $w @('liste', '-Rolle', 'JavaScript Developer', '-Status', 'BLOCKED'); $l = @(& $ids $r)
    Check '-Status BLOCKED' ($l.Count -eq 1 -and $l[0] -eq $t3) ($l -join ',')
    $r = RunJson $w @('liste', '-Rolle', 'JavaScript Developer', '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-Status Alle (Rolle JavaScript Developer: 1,2,3 und CANCELLED 5)' ($l.Count -eq 4) ($l -join ',')
    $r = RunJson $w @('liste', '-Status', 'COMPLETED'); $l = @(& $ids $r)
    Check 'COMPLETED ohne CANCELLED' ($l.Count -eq 1 -and $l[0] -eq $t4) ($l -join ',')
    $r = RunJson $w @('liste', '-Status', 'CANCELLED'); $l = @(& $ids $r)
    Check 'CANCELLED ohne COMPLETED' ($l.Count -eq 1 -and $l[0] -eq $t5) ($l -join ',')
    $r = RunJson $w @('liste', '-Status', 'OPEN,IN_PROGRESS'); $l = @(& $ids $r)
    Check 'mehrere Status' ($l.Count -eq 2) ($l -join ',')
    $r = RunJson $w @('liste', '-An', 'JavaScript Developer', '-Von', 'Software Architect', '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-An und -Von' ($l.Count -eq 1 -and $l[0] -eq $t3) ($l -join ',')
    $r = RunJson $w @('liste', '-Prio', 'HIGH'); $l = @(& $ids $r)
    Check '-Prio' ($l.Count -eq 1 -and $l[0] -eq $t1) ($l -join ',')
    $r = RunJson $w @('liste', '-Text', 'suchwort', '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-Text (ohne Gross-/Kleinschreibung)' ($l.Count -eq 1 -and $l[0] -eq $t1) ($l -join ',')
    $n2 = [int]($t2 -replace '^SFLIX-\d{8}-', ''); $n3 = [int]($t3 -replace '^SFLIX-\d{8}-', '')
    $r = RunJson $w @('liste', '-IdVon', "$n2", '-IdBis', "$n3", '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-IdVon/-IdBis' ($l.Count -eq 2) ($l -join ',')
    $heute = Get-Date -Format 'yyyy-MM-dd'
    $r = RunJson $w @('liste', '-Seit', $heute, '-Bis', $heute, '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-Seit/-Bis (heute): alle 5' ($l.Count -eq 5) ($l -join ',')
    $r = RunJson $w @('liste', '-Seit', '2099-01-01', '-Status', 'Alle'); $l = @(& $ids $r)
    Check '-Seit Zukunft: leer' ($l.Count -eq 0) ($l -join ',')
    $r = RunJson $w @('liste', '-Status', 'KAPUTT')
    Check 'unbekannter Status: Exit 1' ($r.Exit -eq 1) $r.Out
}

# ------------------------------------------------------------------ Fall 16
Fall 16 'Nummer 999 wird zu 1000' {
    $w = New-Wurzel 'f16'
    $id = 'SFLIX-20260101-999'
    $txt = "# $id - Altauftrag`r`n- Erstellt am: 2026-01-01`r`n- Absender: QA Engineer`r`n- Empf" + [char]0xE4 + "nger: JavaScript Developer`r`n- Priorit" + [char]0xE4 + "t: LOW`r`n- Status: OPEN`r`n- Aufgabenbeschreibung: x`r`n- Erwartetes Ergebnis: y`r`n"
    [IO.File]::WriteAllText((Join-Path $w "tasks\OPEN\$id.md"), $txt, $Utf8)
    $r = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Tausend', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E')
    Check 'naechste Nummer 1000' ($r.Exit -eq 0 -and $r.J.ids[0] -match '-1000$') $r.Out
    $r2 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Tausendeins', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E')
    Check 'danach 1001' ($r2.Exit -eq 0 -and $r2.J.ids[0] -match '-1001$') $r2.Out
    $c = Run $w @('check')
    Check 'check akzeptiert vierstellige Nummern' ($c.Exit -eq 0) $c.Out
    $a = Run $w @('status', '-Id', $r.J.ids[0], '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer')
    Check 'Statuswechsel mit vierstelliger ID' ($a.Exit -eq 0) $a.Out
    $a = Run $w @('status', '-Id', '1001', '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer')
    Check 'Kurzform -Id 1001' ($a.Exit -eq 0) $a.Out
    $idx = ReadT (Join-Path $w 'TASK-INDEX.md')
    Check 'Zaehler 1001' ($idx -match 'Letzte vergebene Nummer: 1001') ''
}

# ------------------------------------------------------------------ Fall 17
Fall 17 '-Eingabe ohne stillen Verlust, -Help, Pfade mit * in der Dublettenpruefung (SFLIX-20261112-259)' {
    $w = New-Wurzel 'f17'
    $vorher = Snapshot $w
    $ein = Join-Path $Arbeitsordner 'f17-fremd.txt'
    [IO.File]::WriteAllText($ein, "- An: JavaScript Developer`r`n- Titel: T`r`n- Prio: LOW`r`n- Aufgabe: A`r`n- Schritt 1: Spalte 0`r`n- Ergebnis: E`r`n", $Utf8)
    $r = Run $w @('erstellen', '-Rolle', 'Software Architect', '-Eingabe', $ein)
    Check 'unbekanntes Feld: Exit 1' ($r.Exit -eq 1 -and $r.Out -match 'Schritt 1') $r.Out
    $ein2 = Join-Path $Arbeitsordner 'f17-kopf.txt'
    [IO.File]::WriteAllText($ein2, "Vorlauf`n- An: JavaScript Developer`n- Titel: T`n- Prio: LOW`n- Aufgabe: A`n## Abschnitt`n- Ergebnis: E`n", $Utf8)
    $r = Run $w @('erstellen', '-Rolle', 'Software Architect', '-Eingabe', $ein2)
    Check 'Ueberschrift und Vorlauf: Exit 1' ($r.Exit -eq 1 -and $r.Out -match 'Ueberschrift' -and $r.Out -match 'Vorlauf') $r.Out
    Check 'abgelehnt: nichts geschrieben' (Same-Snapshot $vorher (Snapshot $w)) ''
    $ein3 = Join-Path $Arbeitsordner 'f17-gut.txt'
    [IO.File]::WriteAllText($ein3, ("- An: JavaScript Developer`n- Titel: T`n- Prio: HIGH`n- " + ('Begr' + [char]0xFC + 'ndung') + ": B`n- Aufgabe: A`n  - Schritt 1: eingerueckt`n- Erwartetes Ergebnis: E`n"), $Utf8)
    $r = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-Eingabe', $ein3)
    Check 'eingerueckte Liste und Langnamen: Exit 0' ($r.Exit -eq 0) $r.Out
    if ($r.Exit -eq 0) { Check 'eingerueckte Zeile bleibt im Feld' ((ReadT (TaskFile $w $r.J.ids[0])).Contains('  - Schritt 1: eingerueckt')) '' }
    $id = $r.J.ids[0]
    $a = Run $w @('status', '-Id', $id, '-Neu', 'IN_PROGRESS', '-Rolle', 'JavaScript Developer')
    $ein4 = Join-Path $Arbeitsordner 'f17-abschluss.txt'
    [IO.File]::WriteAllText($ein4, "- Arbeiten: X`n- Tests: Y`n- Dateien: Z`n", $Utf8)
    $r = Run $w @('abschliessen', '-Id', $id, '-Rolle', 'JavaScript Developer', '-Eingabe', $ein4)
    Check 'abschliessen: fremder Schluessel Exit 1' ($a.Exit -eq 0 -and $r.Exit -eq 1 -and $r.Out -match 'Dateien') $r.Out
    $ein5 = Join-Path $Arbeitsordner 'f17-abschnitt.txt'
    [IO.File]::WriteAllText($ein5, "## Unterpunkt`n- Frei: Text`nZeile", $Utf8)
    $r = Run $w @('aendern', '-Id', $id, '-Rolle', 'JavaScript Developer', '-Abschnitt', 'Bericht', '-Eingabe', $ein5)
    Check 'aendern -Abschnitt bleibt frei' ($r.Exit -eq 0 -and (ReadT (TaskFile $w $id)).Contains('- Frei: Text')) $r.Out
    $h = Run $w @('-Help')
    Check '-Help: Exit 0 mit Befehlsliste' ($h.Exit -eq 0 -and $h.Out -match 'abschliessen' -and $h.Out -match 'Exit-Codes') $h.Out
    $h = Run $w @('aendern', '-Hilfe')
    Check '<Befehl> -Hilfe: Exit 0' ($h.Exit -eq 0 -and $h.Out -match '-Abschnitt') $h.Out
    $h = Run $w @('foo', '-Help')
    Check 'unbekannter Befehl -Help: Exit 1' ($h.Exit -eq 1) $h.Out
    $p1 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Pfad eins', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E', '-Dateien', '.claude/skills/*/SKILL.md')
    $p2 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Anderes Thema', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E', '-Dateien', 'docs/SKILL.md, tools/x/SKILL.md', '-WhatIf')
    Check 'kein Fehlalarm aus Pfad mit *' ($p1.Exit -eq 0 -and $p2.Exit -eq 0 -and -not (@($p2.J.warnungen) -match 'Gleiche Dateien')) $p2.Out
    $p3 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Drittes Thema', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E', '-Dateien', '.claude/skills/*/SKILL.md', '-WhatIf')
    Check 'gleicher Pfad mit * wird erkannt' ($p3.Exit -eq 0 -and (@($p3.J.warnungen) -match 'skills/\*/skill\.md')) $p3.Out
    $m1 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Markdown eins', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E', '-Dateien', 'app/SyncEngine.kt')
    $m2 = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-An', 'JavaScript Developer', '-Titel', 'Ganz anders', '-Prio', 'LOW', '-Aufgabe', 'A', '-Ergebnis', 'E', '-Dateien', '**app/SyncEngine.kt**', '-WhatIf')
    Check 'Markdown-Sternchen um Pfad: Dublette erkannt' ($m1.Exit -eq 0 -and (@($m2.J.warnungen) -match 'app/syncengine\.kt')) $m2.Out
    $k = Run $w @('aendern', '-Id', $m1.J.ids[0], '-Rolle', 'Software Architect', '-Feld', 'Dateien', '-Wert', 'app/Neu.kt')
    Check 'aendern mit Kurzname Dateien' ($k.Exit -eq 0 -and (ReadT (TaskFile $w $m1.J.ids[0])).Contains('- Betroffene Dateien: app/Neu.kt')) $k.Out
    $k = Run $w @('aendern', '-Id', $m1.J.ids[0], '-Rolle', 'Software Architect', '-Feld', 'Gibtsnicht', '-Wert', 'x')
    Check 'aendern unbekanntes Feld nennt gueltige Namen' ($k.Exit -eq 1 -and $k.Out -match 'erlaubt:' -and $k.Out -match 'Kurznamen') $k.Out
    $ein6 = Join-Path $Arbeitsordner 'f17-status.txt'
    [IO.File]::WriteAllText($ein6, "- Neu: BLOCKED`n- Frage: F`n- Grund: x`n", $Utf8)
    $k = Run $w @('status', '-Id', $m1.J.ids[0], '-Rolle', 'JavaScript Developer', '-Eingabe', $ein6)
    Check 'status -Eingabe mit unbekanntem Schluessel: Exit 1' ($k.Exit -eq 1 -and $k.Out -match 'Grund') $k.Out
    $ein7 = Join-Path $Arbeitsordner 'f17-bom.txt'
    [IO.File]::WriteAllBytes($ein7, ([byte[]]@(0xEF, 0xBB, 0xBF) + $Utf8.GetBytes("- **An:** JavaScript Developer`r`n- Titel: Bom`r`n- Prio: LOW`r`n- Aufgabe: A`r`n- Ergebnis: E`r`n")))
    $k = RunJson $w @('erstellen', '-Rolle', 'Software Architect', '-Eingabe', $ein7)
    Check 'BOM, CRLF und Fett-Feldname in -Eingabe' ($k.Exit -eq 0) $k.Out
    $c = Run $w @('check')
    $cd = Run $w @('check', '-Details')
    Check 'check fasst Hinweise zusammen, -Details einzeln' ($c.Exit -eq 0 -and $cd.Exit -eq 0 -and -not ($c.Out -match 'Hinweis  \[') -and ($c.Out -match 'check: \d+ Task-Dateien')) ($c.Out + ' | ' + $cd.Out)
    $l = RunJson $w @('liste', '-Rolle', 'JavaScript Developer', '-MitDateien')
    $hit = @($l.J.daten | Where-Object { $_.id -eq $m1.J.ids[0] })
    Check 'liste -MitDateien: Dateien in Ausgabe und JSON' ($l.Exit -eq 0 -and $hit.Count -eq 1 -and $hit[0].betroffeneDateien -eq 'app/Neu.kt' -and ((@($l.J.meldungen) -join "`n") -match '    Dateien: app/Neu\.kt')) $l.Out
}

# ------------------------------------------------------------------ Ergebnis
$fail = @($script:Results | Where-Object { -not $_.Ok })
Write-Output ''
Write-Output ("Ergebnis: {0} Pruefungen, {1} bestanden, {2} fehlgeschlagen. Arbeitsordner: {3}" -f $script:Results.Count, ($script:Results.Count - $fail.Count), $fail.Count, $Arbeitsordner)
$cases = @($script:Results | Group-Object Fall | ForEach-Object { $g = $_; "Fall $($g.Name): " + $(if (@($g.Group | Where-Object { -not $_.Ok }).Count -eq 0) { 'bestanden' } else { 'FEHLER' }) })
$cases | ForEach-Object { Write-Output $_ }
if ($fail.Count -gt 0) { exit 1 }
exit 0
