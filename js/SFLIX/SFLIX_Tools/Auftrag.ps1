# Auftrag.ps1 - zentrale Auftragsverwaltung fuer coordination/ (SFLIX-20261112-254, Konzept SFLIX-20261112-253)
#
# Aufruf:  powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1 <Befehl> [Parameter]
# Befehle: erstellen | erledigt | status | aendern | abschliessen | liste [-Gruppen] | check [-Fix] | hilfe
# Gemeinsam: -Rolle (Pflicht bei schreibenden Befehlen), -Json, -WhatIf, -Eingabe <Datei>, -Help (Alias -Hilfe)
# Hilfe:   SFLIX_Tools\Auftrag.ps1 -Help  bzw.  SFLIX_Tools\Auftrag.ps1 <Befehl> -Help  (Bedienung: coordination/README.md Abschnitt 13)
# Exit-Codes: 0 ok | 1 Eingabe/Pflichtfeld | 2 Berechtigung | 3 Zustandskonflikt | 4 Sperre nicht erhalten
#             5 check hat Inkonsistenzen gefunden | 6 E/A- oder unerwarteter Fehler
#
# Dieses Skript ist absichtlich reines ASCII (PowerShell 5.1 liest .ps1 ohne BOM als ANSI).
# Umlaute und Sonderzeichen entstehen zur Laufzeit ueber UniText.
# Grenze: -Rolle und -Benutzerfreigabe sind Angaben des aufrufenden Chats (Konvention und Protokoll), keine Authentifizierung.

param(
    [Parameter(Position = 0)][string]$Befehl,
    [string]$Rolle,
    [switch]$Json,
    [switch]$WhatIf,
    [Alias('Hilfe')][switch]$Help,
    [string]$Eingabe,
    [string]$Id,
    # erstellen
    [string]$An, [string]$Titel, [string[]]$Prio, [string]$Aufgabe, [string]$Ergebnis,
    [string]$Uebergabegrund, [string]$Kontext, [string]$Dateien, [string]$Abhaengigkeiten,
    [string]$UX, [string]$Freigabe, [string]$Begruendung, [switch]$Trotzdem,
    # status / aendern / abschliessen
    [string]$Neu, [string]$Notiz, [string]$Frage, [string]$Benutzerfreigabe,
    [string]$Feld, [string]$Wert, [switch]$Anhaengen, [string]$Abschnitt, [switch]$Erzwingen,
    [string]$Arbeiten, [string]$Tests, [string]$Risiken, [string]$Folgeschritt, [string]$Entscheide,
    # liste
    [string]$Von, [string[]]$Status, [string]$Text, [int]$IdVon = 0, [int]$IdBis = 0, [string]$Seit, [string]$Bis, [switch]$MitDateien, [switch]$Gruppen,
    # check
    [switch]$Fix, [switch]$Details,
    # Test-/Betriebsparameter
    [string]$Wurzel, [string]$Haltepunkt, [int]$SperrZeitlimitSek = 30
)

$ErrorActionPreference = 'Stop'

function UniText([string]$s) { return [regex]::Unescape($s) }
function Norm([string]$s) {
    if ($null -eq $s) { return '' }
    $r = $s.ToLowerInvariant().Trim()
    $r = $r.Replace([string][char]0xE4, 'ae').Replace([string][char]0xF6, 'oe').Replace([string][char]0xFC, 'ue').Replace([string][char]0xDF, 'ss')
    return $r
}

# ---------------------------------------------------------------- Konstanten
$ND = [string][char]0x2013                  # Gedankenstrich
$DOT = ' ' + [string][char]0xB7 + ' '       # Trenner im Bearbeitungsverlauf
$script:Utf8 = New-Object System.Text.UTF8Encoding($false)

$FN = @{
    Erstellt    = 'Erstellt am'
    Absender    = 'Absender'
    Empfaenger  = (UniText 'Empf\u00e4nger')
    Prio        = (UniText 'Priorit\u00e4t')
    Status      = 'Status'
    Uebergabe   = (UniText '\u00dcbergabegrund')
    Aufgabe     = 'Aufgabenbeschreibung'
    Kontext     = 'Technischer Kontext'
    Dateien     = 'Betroffene Dateien'
    Doku        = 'Dokumentationsverweise'
    UX          = 'UX-Beteiligung'
    Freigabe    = 'Benutzerfreigabe'
    Abh         = (UniText 'Abh\u00e4ngigkeiten')
    Folge       = 'Folgeschritt'
    Arbeiten    = (UniText 'Durchgef\u00fchrte Arbeiten')
    Tests       = 'Tests und Ergebnisse'
    Fragen      = 'Offene Fragen'
    Risiken     = 'Verbleibende Risiken'
    Abgeschl    = 'Abgeschlossen am'
    Verlauf     = 'Bearbeitungsverlauf'
    Uebernahme  = (UniText '\u00dcbernahmebest\u00e4tigung')
    Ergebnis    = 'Erwartetes Ergebnis'
    ErgRueck    = (UniText 'Ergebnis und R\u00fcckmeldung')
    Entscheide  = 'Entscheidungen'
}
$ArbeitenUndTests = (UniText 'Durchgef\u00fchrte Arbeiten und Tests')

$script:Alias = @{}
foreach ($k in $FN.Keys) { $script:Alias[(Norm $FN[$k])] = $FN[$k] }
foreach ($v in @((UniText 'Tats\u00e4chlich durchgef\u00fchrte Arbeiten'), $ArbeitenUndTests, 'Durchgefuehrte Arbeiten')) { $script:Alias[(Norm $v)] = $FN.Arbeiten }
$script:Alias['tests'] = $FN.Tests
$script:BothKey = Norm $ArbeitenUndTests

# Fuenf Rollen (seit 2026-11-20, CLAUDE.md "Rollen"): Planung, Architekt, Entwicklung, Pruefung, Release
$RolePruefung = ('Pr' + [char]0xFC + 'fung')
$Roles = @(
    'Planung',
    'Architekt',
    'Entwicklung',
    $RolePruefung,
    'Release'
)

$InboxOf = @{
    'Planung'     = 'planung'
    'Architekt'   = 'architekt'
    'Entwicklung' = 'entwicklung'
    'Release'     = 'release'
}
$InboxOf[$RolePruefung] = 'pruefung'
# Dateipfade in "Betroffene Dateien" (Dublettenwarnung, liste -Gruppen)
$script:PathRx = '[A-Za-z0-9_./\\*-]+\.(?:js|html|css|cs|java|xml|gradle|ps1|bat|py|md|txt|json|svg|kt|kts|properties)'
$ActiveStatus = @('OPEN', 'IN_PROGRESS', 'BLOCKED')
$AllStatus = @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED', 'CANCELLED')
$CoreDup = @($FN.Status, $FN.Absender, $FN.Empfaenger, $FN.Prio, $FN.Erstellt)
$LastWins = @($FN.Uebernahme, $FN.Abgeschl, $FN.Verlauf)
$AppendOnly = @($FN.Verlauf, $FN.Arbeiten, $FN.Tests, $FN.Fragen)
$NotChangeable = @($FN.Status, $FN.Absender, $FN.Empfaenger, $FN.Erstellt, $FN.Uebernahme, $FN.Abgeschl)
$FieldRx = [regex]'^- (?:\*\*)?(?<n>[^:*\r]{2,60}?)(?:\*\*)?:(?:\*\*)?(?<v>.*)$'
$HeadRx = [regex]('^#{1,3}\s+(?<id>SFLIX-\d{8}-\d{3,})\s+[-' + [char]0x2013 + [char]0x2014 + ']\s+(?<t>.*?)\s*$')

# ---------------------------------------------------------------- Zustand
$script:Warn = New-Object System.Collections.Generic.List[string]
$script:Msg = New-Object System.Collections.Generic.List[string]
$script:Plan = New-Object System.Collections.Generic.List[string]
$script:Ids = New-Object System.Collections.Generic.List[string]
$script:Daten = $null
$script:LockFs = $null
$script:Testmodus = ($env:AUFTRAG_TESTMODUS -eq '1')
$script:Heute = Get-Date -Format 'yyyy-MM-dd'

if ($Wurzel) {
    if (-not $script:Testmodus) { Write-Output 'Fehler: -Wurzel nur im Testmodus.'; exit 1 }
    $script:Root = [IO.Path]::GetFullPath($Wurzel)
}
else {
    $script:Root = Join-Path (Split-Path -Parent $PSScriptRoot) 'coordination'
}
$script:Root = [IO.Path]::GetFullPath($script:Root).TrimEnd('\')
if (-not $script:Testmodus -and $SperrZeitlimitSek -lt 1) { $SperrZeitlimitSek = 30 }
$script:TasksDir = Join-Path $script:Root 'tasks'
$script:HistDir = Join-Path $script:Root '.history'

function Stop-Auftrag([int]$code, [string]$msg) {
    $e = New-Object System.Exception $msg
    $e.Data['code'] = $code
    throw $e
}
function Add-Msg([string]$s) { [void]$script:Msg.Add($s) }
function Add-Warnung([string]$s) { if (-not $script:Warn.Contains($s)) { [void]$script:Warn.Add($s) } }
function Add-Plan([string]$s) { [void]$script:Plan.Add($s) }
function Halt([string]$name) {
    if (-not ($script:Testmodus -and $Haltepunkt)) { return }
    if ($Haltepunkt -eq $name) { [Environment]::Exit(99) }
    if ($Haltepunkt -eq ("schlaf-" + $name)) { Start-Sleep -Seconds 5 }
}
function Test-IoEx($ex) {
    return (($ex -is [System.IO.IOException]) -or ($ex -is [System.UnauthorizedAccessException]) -or
        ($null -ne $ex.InnerException -and (($ex.InnerException -is [System.IO.IOException]) -or ($ex.InnerException -is [System.UnauthorizedAccessException]))))
}

function Assert-InRoot([string]$path) {
    $full = [IO.Path]::GetFullPath($path)
    if (-not $full.StartsWith($script:Root + '\', [StringComparison]::OrdinalIgnoreCase)) {
        Stop-Auftrag 1 "Pfad ausserhalb von coordination/: $path"
    }
    $d = $full
    while ($d.Length -gt $script:Root.Length) {
        if (Test-Path -LiteralPath $d) {
            if (([IO.File]::GetAttributes($d) -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Stop-Auftrag 1 "Junction/Symlink nicht erlaubt: $d" }
        }
        $d = [IO.Path]::GetDirectoryName($d)
        if ($null -eq $d) { break }
    }
}

# ---------------------------------------------------------------- Rollen
function Get-RoleName([string]$s) {
    $n = Norm $s
    if ($n -eq '') { return $null }

    # Kurz- und Altnamen (bis 2026-11-20 gab es 11 Rollen) werden der zustaendigen der fuenf Rollen zugeordnet
    if ($n -match '^(planung|planer|orchestrator|master orchestrator|technical product owner|product owner|project manager|projektmanager|po|pm)(\b|$)') { return 'Planung' }
    if ($n -match '^(architekt|software architect|softwarearchitekt|architect|architecture)(\b|$)') { return 'Architekt' }
    if ($n -match '^(entwicklung|entwickler|development|developer|dev|javascript developer|javascript|js developer|js|c#/.net developer|csharp developer|c# developer|dotnet developer|csharp|android developer|android|debugging specialist|debugger|debugging)(\b|$)') { return 'Entwicklung' }
    if ($n -match '^(pruefung|pruefer|qa engineer|qa|quality assurance|security auditor|security|performance engineer|performance)(\b|$)') { return $RolePruefung }
    if ($n -match '^(release|release manager|documentation assistant|documentation|dokumentation|doku)(\b|$)') { return 'Release' }
    if ($n -match '^benutzer(\b|$)') { return 'Benutzer' }

    return $null
}

# ---------------------------------------------------------------- Lesen / Parser
function Get-Sha([byte[]]$bytes) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash($bytes)) } finally { $sha.Dispose() }
}
function Read-Bytes([string]$path) {
    $last = $null
    for ($i = 0; $i -lt 8; $i++) {
        try { return , [IO.File]::ReadAllBytes($path) }
        catch { $last = $_; if (-not (Test-IoEx $_.Exception)) { throw }; Start-Sleep -Milliseconds 100 }
    }
    throw $last
}
function Read-FileInfo([string]$path) {
    $b = Read-Bytes $path
    $bom = ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
    $off = 0; if ($bom) { $off = 3 }
    $text = $script:Utf8.GetString($b, $off, $b.Length - $off)
    return [pscustomobject]@{ Text = $text; Bom = $bom; Hash = (Get-Sha $b); Size = $b.Length }
}

function Convert-ToTask([string]$text, $forceCrlf) {
    $parts = $text.Split([char]10)
    $lines = New-Object 'System.Collections.Generic.List[string]'
    foreach ($p in $parts) { [void]$lines.Add($p) }
    if ($null -ne $forceCrlf) { $crlf = [bool]$forceCrlf }
    else {
        $c1 = ([regex]::Matches($text, "`r`n")).Count
        $c2 = ([regex]::Matches($text, "(?<!`r)`n")).Count
        $crlf = ($c1 -gt 0 -and $c1 -ge $c2)
    }
    $problems = New-Object System.Collections.Generic.List[string]
    $h = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -ne '') { $h = $i; break } }
    $id = $null; $title = $null
    if ($h -ge 0) {
        $m = $HeadRx.Match($lines[$h].TrimEnd("`r"))
        if ($m.Success) { $id = $m.Groups['id'].Value; $title = $m.Groups['t'].Value }
    }
    if ($null -eq $id) { [void]$problems.Add('Kopfzeile fehlt oder hat unbekanntes Format') }
    $restStart = $lines.Count
    if ($h -ge 0) {
        for ($i = $h + 1; $i -lt $lines.Count; $i++) { if ($lines[$i] -match '^#{1,6}\s') { $restStart = $i; break } }
    }
    $fields = New-Object System.Collections.Generic.List[object]
    if ($null -ne $id) {
        for ($i = $h + 1; $i -lt $restStart; $i++) {
            $m = $FieldRx.Match($lines[$i].TrimEnd("`r"))
            if ($m.Success) {
                $key = Norm $m.Groups['n'].Value
                if ($script:Alias.ContainsKey($key)) {
                    [void]$fields.Add([pscustomobject]@{ Name = $m.Groups['n'].Value.Trim(); Canon = $script:Alias[$key]; Key = $key; Start = $i; End = $i })
                }
            }
        }
        for ($k = 0; $k -lt $fields.Count; $k++) {
            $lim = $restStart - 1
            if ($k + 1 -lt $fields.Count) { $lim = $fields[$k + 1].Start - 1 }
            while ($lim -gt $fields[$k].Start -and $lines[$lim].Trim() -eq '') { $lim-- }
            $fields[$k].End = $lim
        }
        foreach ($c in $CoreDup) {
            $n = @($fields | Where-Object { $_.Canon -eq $c }).Count
            if ($n -gt 1) { [void]$problems.Add("Feld doppelt: $c") }
        }
    }
    $t = [pscustomobject]@{
        Lines = $lines; HeadIdx = $h; Id = $id; Title = $title; Fields = $fields; RestStart = $restStart
        Crlf = $crlf; Problems = $problems; Path = $null; Folder = $null; Bom = $false; Hash = $null
    }
    $st = Get-StatusWort $t
    if ($null -ne $id) {
        if ($null -eq (Get-Fld $t $FN.Status)) { [void]$problems.Add('Status-Zeile fehlt') }
        elseif ($null -eq $st) { [void]$problems.Add('Status unbekannt') }
    }
    return $t
}
function Get-Fld($t, [string]$canon) {
    $hit = $null
    foreach ($f in $t.Fields) {
        if ($f.Canon -eq $canon) {
            if ($LastWins -notcontains $canon) { return $f }
            $hit = $f
        }
    }
    return $hit
}
function Get-FldVal($t, $f) {
    if ($null -eq $f) { return '' }
    $first = $FieldRx.Match($t.Lines[$f.Start].TrimEnd("`r")).Groups['v'].Value
    $acc = New-Object System.Collections.Generic.List[string]
    [void]$acc.Add($first.Trim())
    for ($i = $f.Start + 1; $i -le $f.End; $i++) { [void]$acc.Add($t.Lines[$i].TrimEnd("`r").Trim()) }
    return (($acc -join "`n").Trim())
}
function Get-Val($t, [string]$canon) { return (Get-FldVal $t (Get-Fld $t $canon)) }
function Test-Leer([string]$v) {
    $x = ($v -replace '[\s\u2013\u2014-]', '')
    return ($x -eq '')
}
function Get-StatusWort($t) {
    $v = Get-Val $t $FN.Status
    $w = ($v -replace '\*', '').Trim()
    if ($w -eq '') { return $null }
    $w = ($w -split '[\s(]')[0].ToUpperInvariant()
    if ($AllStatus -contains $w) { return $w }
    return $null
}
function Get-Nummer([string]$id) {
    if ($id -match '^SFLIX-\d{8}-(\d{3,})$') { return [int]$Matches[1] }
    return -1
}

function Load-Task([string]$path) {
    $fi = Read-FileInfo $path
    $t = Convert-ToTask $fi.Text $null
    $t.Path = $path; $t.Folder = Split-Path -Leaf (Split-Path -Parent $path); $t.Bom = $fi.Bom; $t.Hash = $fi.Hash
    $name = [IO.Path]::GetFileNameWithoutExtension($path)
    if ($null -ne $t.Id -and $t.Id -ne $name) { [void]$t.Problems.Add("Kopf-ID $($t.Id) ungleich Dateiname") }
    return $t
}
function Assert-Eindeutig($t) {
    if ($t.Problems.Count -gt 0) { Stop-Auftrag 3 ("Auftrag mehrdeutig, nichts geaendert: " + ($t.Problems -join '; ')) }
}

function Get-TaskFiles([string[]]$folders) {
    $res = @()
    foreach ($f in $folders) {
        $d = Join-Path $script:TasksDir $f
        if (Test-Path -LiteralPath $d) {
            $res += @(Get-ChildItem -LiteralPath $d -File -Force | Where-Object { $_.Name -match '^SFLIX-\d{8}-\d{3,}\.md$' } | ForEach-Object { $_.FullName })
        }
    }
    return $res
}
function Find-TaskPath([string]$idIn) {
    if (-not $idIn) { Stop-Auftrag 1 '-Id fehlt' }
    $pattern = $null
    if ($idIn -match '^SFLIX-\d{8}-\d{3,}$') { $pattern = "$idIn.md" }
    elseif ($idIn -match '^\d{1,6}$') { $pattern = ('SFLIX-????????-{0:000}.md' -f [int]$idIn) }
    else { Stop-Auftrag 1 "Ungueltige -Id: $idIn" }
    $hits = @()
    foreach ($f in @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')) {
        $d = Join-Path $script:TasksDir $f
        if (Test-Path -LiteralPath $d) { $hits += @(Get-ChildItem -LiteralPath $d -File -Force -Filter $pattern | ForEach-Object { $_.FullName }) }
    }
    if ($hits.Count -eq 0) { Stop-Auftrag 1 "Auftrag nicht gefunden: $idIn" }
    if ($hits.Count -gt 1) { Stop-Auftrag 3 "Auftrag mehrfach vorhanden: $idIn ($($hits -join ', '))" }
    return $hits[0]
}

# ---------------------------------------------------------------- Mutationen (arbeiten auf $t.Lines, danach neu parsen)
function Mk($t, [string]$s) { if ($t.Crlf) { return $s + "`r" } else { return $s } }
function Reparse($t) {
    $n = Convert-ToTask ($t.Lines -join "`n") $t.Crlf
    $n.Path = $t.Path; $n.Folder = $t.Folder; $n.Bom = $t.Bom; $n.Hash = $t.Hash
    return $n
}
function Get-InsertPos($t) {
    $v = Get-Fld $t $FN.Verlauf
    if ($null -ne $v) { return $v.Start }
    if ($t.Fields.Count -gt 0) { return ($t.Fields[$t.Fields.Count - 1].End + 1) }
    return ($t.HeadIdx + 1)
}
function Format-FeldZeilen($t, [string]$name, [string]$value) {
    $vl = ($value -replace "`r`n", "`n") -split "`n"
    $out = New-Object System.Collections.Generic.List[string]
    [void]$out.Add((Mk $t ("- " + $name + ": " + $vl[0].TrimEnd())))
    for ($i = 1; $i -lt $vl.Count; $i++) { [void]$out.Add((Mk $t ("  " + $vl[$i].TrimEnd()))) }
    return , $out
}
function Add-NeuesFeld($t, [string]$name, [string]$value) {
    $pos = Get-InsertPos $t
    $z = Format-FeldZeilen $t $name $value
    $t.Lines.InsertRange($pos, [string[]]$z.ToArray())
    return (Reparse $t)
}
function Set-FeldWert($t, [string]$canon, [string]$value) {
    $f = Get-Fld $t $canon
    if ($null -eq $f) { return (Add-NeuesFeld $t $canon $value) }
    $line = $t.Lines[$f.Start].TrimEnd("`r")
    $m = [regex]::Match($line, '^(- (?:\*\*)?[^:]+?(?:\*\*)?:(?:\*\*)?)')
    $t.Lines[$f.Start] = Mk $t ($m.Groups[1].Value + ' ' + $value)
    if ($f.End -gt $f.Start) { $t.Lines.RemoveRange($f.Start + 1, $f.End - $f.Start) }
    return (Reparse $t)
}
function Add-FeldText($t, [string]$canon, [string]$value) {
    $f = Get-Fld $t $canon
    if ($null -eq $f) { return (Add-NeuesFeld $t $canon $value) }
    $vl = ($value -replace "`r`n", "`n") -split "`n"
    $new = New-Object System.Collections.Generic.List[string]
    foreach ($x in $vl) { [void]$new.Add((Mk $t ("  " + $x.TrimEnd()))) }
    $t.Lines.InsertRange($f.End + 1, [string[]]$new.ToArray())
    return (Reparse $t)
}
function Add-Verlauf($t, [string]$entry) {
    $f = Get-Fld $t $FN.Verlauf
    $entry = ($entry -replace "[\r\n]+", ' ').Trim()
    if ($null -eq $f) { return (Add-NeuesFeld $t $FN.Verlauf $entry) }
    if ($f.Start -eq $f.End) {
        $t.Lines[$f.Start] = (Mk $t ($t.Lines[$f.Start].TrimEnd("`r") + $DOT + $entry))
    }
    else {
        $t.Lines.Insert($f.End + 1, (Mk $t ("  " + $entry)))
    }
    return (Reparse $t)
}

# ---------------------------------------------------------------- Schreiben
function Get-Zeitstempel { return (Get-Date -Format 'yyyyMMdd-HHmmss-fff') }
function Write-Fresh([string]$path, [string]$text) {
    Assert-InRoot $path
    $bytes = $script:Utf8.GetBytes($text)
    $fs = [IO.FileStream]::new($path, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $fs.Write($bytes, 0, $bytes.Length); $fs.Flush($true) } finally { $fs.Dispose() }
}
function Move-Verwaist([string]$path, [string]$why) {
    $d = Join-Path $script:HistDir '_verwaist'
    Assert-InRoot $d
    if (-not (Test-Path -LiteralPath $d)) { [void](New-Item -ItemType Directory -Path $d) }
    $dest = Join-Path $d ((Split-Path -Leaf $path).TrimStart('.') + '.' + (Get-Zeitstempel))
    Move-Item -LiteralPath $path -Destination $dest
    return $dest
}
function Backup-Datei([string]$path, [string]$id) {
    $d = Join-Path $script:HistDir $id
    Assert-InRoot $d
    if (-not (Test-Path -LiteralPath $d)) { [void](New-Item -ItemType Directory -Path $d) }
    $dest = Join-Path $d ((Get-Zeitstempel) + '.md')
    $k = 1
    while (Test-Path -LiteralPath $dest) { $dest = Join-Path $d ((Get-Zeitstempel) + "-$k.md"); $k++ }
    Copy-Item -LiteralPath $path -Destination $dest
    [IO.File]::SetAttributes($dest, [IO.FileAttributes]::Normal)
    # Obergrenze 20 Fassungen je ID: einzige Loeschung, nur die aelteste Fassung innerhalb von .history
    $all = @(Get-ChildItem -LiteralPath $d -File | Sort-Object Name)
    $over = $all.Count - 20
    for ($i = 0; $i -lt $over; $i++) {
        [IO.File]::SetAttributes($all[$i].FullName, [IO.FileAttributes]::Normal)
        [IO.File]::Delete($all[$i].FullName)
    }
}
function Save-Task($t, [string]$newText) {
    $path = $t.Path
    Assert-InRoot $path
    $dir = Split-Path -Parent $path
    $tmp = Join-Path $dir ('.' + (Split-Path -Leaf $path) + '.tmp-' + $PID)
    Write-Fresh $tmp $newText
    Halt 'nach-tmp'
    $cur = Get-Sha (Read-Bytes $path)
    if ($cur -ne $t.Hash) {
        [void](Move-Verwaist $tmp 'konflikt')
        Stop-Auftrag 3 "Auftrag $($t.Id) wurde seit dem Lesen ausserhalb des Skripts geaendert; nichts geschrieben. Aufruf wiederholen."
    }
    Backup-Datei $path $t.Id
    $ro = (Get-Item -LiteralPath $path -Force).IsReadOnly
    if ($ro) { [IO.File]::SetAttributes($path, [IO.FileAttributes]::Normal) }
    $ok = $false; $last = $null
    for ($i = 0; $i -lt 6 -and -not $ok; $i++) {
        try { [IO.File]::Replace($tmp, $path, [NullString]::Value); $ok = $true }
        catch { $last = $_; if (-not (Test-IoEx $_.Exception)) { throw }; Start-Sleep -Milliseconds 150 }
    }
    if ($ro) { [IO.File]::SetAttributes($path, [IO.FileAttributes]::ReadOnly) }
    if (-not $ok) { Stop-Auftrag 6 "Ersetzen fehlgeschlagen: $($last.Exception.Message)" }
    $t.Hash = Get-Sha (Read-Bytes $path)
    Halt 'nach-replace'
}
function Move-Task($t, [string]$folder) {
    $dest = Join-Path (Join-Path $script:TasksDir $folder) (Split-Path -Leaf $t.Path)
    Assert-InRoot $dest
    if (Test-Path -LiteralPath $dest) { Stop-Auftrag 3 "Zieldatei existiert bereits: $dest" }
    if (-not (Test-Path -LiteralPath (Split-Path -Parent $dest))) { [void](New-Item -ItemType Directory -Path (Split-Path -Parent $dest)) }
    [IO.File]::Move($t.Path, $dest)
    $t.Path = $dest; $t.Folder = $folder
    Halt 'nach-move'
}
function Set-Schreibschutz([string]$path, [bool]$ro) {
    Assert-InRoot $path
    if ($ro) { [IO.File]::SetAttributes($path, ([IO.File]::GetAttributes($path) -bor [IO.FileAttributes]::ReadOnly)) }
    else { [IO.File]::SetAttributes($path, ([IO.File]::GetAttributes($path) -band (-bnot [IO.FileAttributes]::ReadOnly))) }
}
function Add-Audit([string]$cmd, [string]$id, [string]$detail, [string]$res) {
    $p = Join-Path $script:Root '.audit.log'
    Assert-InRoot $p
    $r = if ($Rolle) { $Rolle } else { '-' }
    $line = ((Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fff') + " | $PID | $r | $cmd | $id | " + ($detail -replace "[\r\n]+", ' ') + " | $res")
    if ($Benutzerfreigabe) { $line += ' | Benutzerfreigabe: ' + ($Benutzerfreigabe -replace "[\r\n]+", ' ') }
    $bytes = $script:Utf8.GetBytes($line + "`r`n")
    $fs = [IO.FileStream]::new($p, [IO.FileMode]::Append, [IO.FileAccess]::Write, [IO.FileShare]::Read)
    try { $fs.Write($bytes, 0, $bytes.Length); $fs.Flush($true) } finally { $fs.Dispose() }
}

# ---------------------------------------------------------------- Sperre
function Enter-Lock {
    $p = Join-Path $script:Root '.lock'
    Assert-InRoot $p
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $rnd = New-Object System.Random
    while ($true) {
        try {
            $script:LockFs = [IO.FileStream]::new($p, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            return
        }
        catch {
            if (-not (Test-IoEx $_.Exception)) { throw }
            if ($sw.Elapsed.TotalSeconds -ge $SperrZeitlimitSek) { Stop-Auftrag 4 "Sperre coordination/.lock nach $SperrZeitlimitSek s nicht erhalten; Aufruf wiederholen." }
            Start-Sleep -Milliseconds (200 + $rnd.Next(0, 150))
        }
    }
}
function Exit-Lock {
    if ($null -ne $script:LockFs) { try { $script:LockFs.Dispose() } catch {}; $script:LockFs = $null }
}

# ---------------------------------------------------------------- Uebersicht (Index / Posteingaenge), erzeugt
function Get-MaxNummer {
    $max = 0
    if (Test-Path -LiteralPath $script:TasksDir) {
        foreach ($f in Get-ChildItem -LiteralPath $script:TasksDir -Directory -Force) {
            foreach ($x in Get-ChildItem -LiteralPath $f.FullName -File -Force) {
                if ($x.Name -match '^SFLIX-\d{8}-(\d{3,})\.md$') { $n = [int]$Matches[1]; if ($n -gt $max) { $max = $n } }
            }
        }
    }
    if (Test-Path -LiteralPath $script:HistDir) {
        foreach ($x in Get-ChildItem -LiteralPath $script:HistDir -Directory -Force) {
            if ($x.Name -match '^SFLIX-\d{8}-(\d{3,})$') { $n = [int]$Matches[1]; if ($n -gt $max) { $max = $n } }
        }
    }
    return $max
}
function Get-Record($t) {
    $st = Get-StatusWort $t
    $von = Get-RoleName (Get-Val $t $FN.Absender); if (-not $von) { $von = (Get-Val $t $FN.Absender) }
    $an = Get-RoleName (Get-Val $t $FN.Empfaenger); if (-not $an) { $an = (Get-Val $t $FN.Empfaenger) }
    $pr = ((Get-Val $t $FN.Prio) -replace '\*', '').Trim(); if ($pr) { $pr = ($pr -split '[\s(]')[0].ToUpperInvariant() }
    return [pscustomobject]@{
        Id = $t.Id; Num = (Get-Nummer $t.Id); Titel = $t.Title; Von = $von; An = $an; Prio = $pr; Status = $st
        Erstellt = ((Get-Val $t $FN.Erstellt) -split '[\s(,]')[0]; Folder = $t.Folder; File = (Split-Path -Leaf $t.Path)
    }
}
function Get-AllRecords([string[]]$folders) {
    $recs = @()
    foreach ($p in (Get-TaskFiles $folders)) {
        try {
            $t = Load-Task $p
            if ($t.Problems.Count -gt 0) { Add-Warnung ("{0}: {1}" -f (Split-Path -Leaf $p), ($t.Problems -join '; ')); continue }
            $recs += Get-Record $t
        }
        catch { Add-Warnung ("{0}: nicht lesbar ({1})" -f (Split-Path -Leaf $p), $_.Exception.Message) }
    }
    return , ($recs | Sort-Object Num, Id)
}
function Format-Zelle([string]$s) { return (($s -replace "[\r\n]+", ' ') -replace '\|', '\|') }
function Get-Eol([string]$text) {
    if (-not $text) { return "`r`n" }
    $c1 = ([regex]::Matches($text, "`r`n")).Count
    $c2 = ([regex]::Matches($text, "(?<!`r)`n")).Count
    if ($c2 -gt $c1) { return "`n" }
    return "`r`n"
}

function Build-IndexText($recs, [string]$existing) {
    $eol = Get-Eol $existing
    $max = Get-MaxNummer
    $head = New-Object System.Collections.Generic.List[string]
    $found = $false
    if ($existing) {
        foreach ($l in ($existing -split "`n")) {
            $x = $l.TrimEnd("`r")
            if ($x -match '^\*\*Letzte vergebene Nummer') { $found = $true; break }
            [void]$head.Add($x)
        }
    }
    if (-not $found) {
        $head.Clear()
        [void]$head.Add('# TASK-INDEX ' + $ND + ' aktive Auftr' + [char]0xE4 + 'ge')
    }
    while ($head.Count -gt 0 -and ($head[$head.Count - 1].Trim() -eq '' -or $head[$head.Count - 1] -match '^<!-- Erzeugt von')) { $head.RemoveAt($head.Count - 1) }
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($l in $head) { [void]$out.Add($l) }
    [void]$out.Add('')
    [void]$out.Add('<!-- Erzeugt von SFLIX_Tools/Auftrag.ps1, nicht von Hand bearbeiten -->')
    [void]$out.Add('')
    [void]$out.Add(('**Letzte vergebene Nummer: {0:000}** (Stand {1}). N' -f $max, $script:Heute) + [char]0xE4 + ('chste ID: `SFLIX-YYYYMMDD-{0:000}`.' -f ($max + 1)))
    [void]$out.Add('')
    [void]$out.Add('| ID | Kurzbeschreibung | Von | An | Prio | Status | Datei |')
    [void]$out.Add('|---|---|---|---|---|---|---|')
    foreach ($r in $recs) {
        if ($ActiveStatus -contains $r.Status) {
            [void]$out.Add(('| {0} | {1} | {2} | {3} | {4} | {5} | tasks/{6}/{7} |' -f $r.Id, (Format-Zelle $r.Titel), (Format-Zelle $r.Von), (Format-Zelle $r.An), $r.Prio, $r.Status, $r.Folder, $r.File))
        }
    }
    return (($out -join $eol) + $eol)
}
function Build-InboxText([string]$role, $recs, [string]$existing) {
    $eol = Get-Eol $existing
    $head = New-Object System.Collections.Generic.List[string]
    $found = $false
    if ($existing) {
        foreach ($l in ($existing -split "`n")) {
            $x = $l.TrimEnd("`r")
            if ($x -match '^\|\s*ID\s*\|') { $found = $true; break }
            [void]$head.Add($x)
        }
    }
    if (-not $found) {
        $head.Clear()
        [void]$head.Add('# Posteingang ' + $ND + ' ' + $role)
        [void]$head.Add('')
        [void]$head.Add('Nur Verweise auf aktive, an ' + $role + ' adressierte Auftr' + [char]0xE4 + 'ge (Details in der Task-Datei). Erledigte Eintr' + [char]0xE4 + 'ge werden entfernt. Regeln: `../README.md`.')
        [void]$head.Add('')
    }
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($l in $head) { [void]$out.Add($l) }
    [void]$out.Add('| ID | Von | Kurzbeschreibung | Prio | Status | Datei |')
    [void]$out.Add('|---|---|---|---|---|---|')
    foreach ($r in $recs) {
        if (($ActiveStatus -contains $r.Status) -and $r.An -eq $role) {
            [void]$out.Add(('| {0} | {1} | {2} | {3} | {4} | ../tasks/{5}/{6} |' -f $r.Id, (Format-Zelle $r.Von), (Format-Zelle $r.Titel), $r.Prio, $r.Status, $r.Folder, $r.File))
        }
    }
    return (($out -join $eol) + $eol)
}
function Get-ViewTargets($recs) {
    $list = New-Object System.Collections.Generic.List[object]
    $ip = Join-Path $script:Root 'TASK-INDEX.md'
    $ex = $null; if (Test-Path -LiteralPath $ip) { $ex = (Read-FileInfo $ip).Text }
    [void]$list.Add([pscustomobject]@{ Path = $ip; Text = (Build-IndexText $recs $ex); Existing = $ex; Name = 'TASK-INDEX.md' })
    foreach ($role in $Roles) {
        $p = Join-Path (Join-Path $script:Root 'inbox') ($InboxOf[$role] + '.md')
        $ex = $null; if (Test-Path -LiteralPath $p) { $ex = (Read-FileInfo $p).Text }
        [void]$list.Add([pscustomobject]@{ Path = $p; Text = (Build-InboxText $role $recs $ex); Existing = $ex; Name = ('inbox/' + $InboxOf[$role] + '.md') })
    }
    return , $list
}
function Test-ViewGleich($v) {
    if ($null -eq $v.Existing) { return $false }
    $a = ($v.Existing -replace '\(Stand \d{4}-\d{2}-\d{2}\)', '(Stand X)')
    $b = ($v.Text -replace '\(Stand \d{4}-\d{2}-\d{2}\)', '(Stand X)')
    return ($a -ceq $b)
}
function Update-Views {
    $recs = Get-AllRecords @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')
    $targets = Get-ViewTargets $recs
    foreach ($v in $targets) {
        if (Test-ViewGleich $v) { continue }
        Assert-InRoot $v.Path
        $dir = Split-Path -Parent $v.Path
        if (-not (Test-Path -LiteralPath $dir)) { [void](New-Item -ItemType Directory -Path $dir) }
        $tmp = Join-Path $dir ('.' + (Split-Path -Leaf $v.Path) + '.tmp-' + $PID)
        Write-Fresh $tmp $v.Text
        if (Test-Path -LiteralPath $v.Path) {
            $ok = $false; $last = $null
            for ($i = 0; $i -lt 6 -and -not $ok; $i++) {
                try { [IO.File]::Replace($tmp, $v.Path, [NullString]::Value); $ok = $true }
                catch { $last = $_; if (-not (Test-IoEx $_.Exception)) { throw }; Start-Sleep -Milliseconds 150 }
            }
            if (-not $ok) { Stop-Auftrag 6 "Ersetzen fehlgeschlagen ($($v.Name)): $($last.Exception.Message)" }
        }
        else { [IO.File]::Move($tmp, $v.Path) }
        if ($v.Name -eq 'TASK-INDEX.md') { Halt 'nach-index' }
    }
}

# ---------------------------------------------------------------- Eingabedatei
$script:InFelder = $null
$script:InNamen = New-Object System.Collections.Generic.List[string]
$script:InProbleme = New-Object System.Collections.Generic.List[string]
function Read-Eingabe {
    $d = [ordered]@{}
    $text = ''
    if ($Eingabe) {
        if (-not (Test-Path -LiteralPath $Eingabe)) { Stop-Auftrag 1 "Eingabedatei nicht gefunden: $Eingabe" }
        $text = (Read-FileInfo $Eingabe).Text
        $cur = $null; $acc = $null
        foreach ($l0 in ($text -split "`n")) {
            $l = $l0.TrimEnd("`r")
            $m = [regex]::Match($l, '^- (?:\*\*)?(?<n>[^:*]{2,60}?)(?:\*\*)?:(?:\*\*)?\s?(?<v>.*)$')
            if ($m.Success) {
                if ($null -ne $cur) { $d[$cur] = ($acc -join "`n").Trim() }
                $cur = Norm $m.Groups['n'].Value; $acc = New-Object System.Collections.Generic.List[string]
                [void]$script:InNamen.Add($m.Groups['n'].Value.Trim())
                [void]$acc.Add($m.Groups['v'].Value)
            }
            else {
                if ($l -match '^#{1,6}\s') { [void]$script:InProbleme.Add('Ueberschrift: ' + $l.Trim()) }
                elseif ($null -eq $cur -and $l.Trim() -ne '') { [void]$script:InProbleme.Add('Text vor dem ersten Feld: ' + $l.Trim()) }
                if ($null -ne $cur) { [void]$acc.Add($l.TrimStart()) }
            }
        }
        if ($null -ne $cur) { $d[$cur] = ($acc -join "`n").Trim() }
    }
    $script:InFelder = $d
    $script:InText = $text.Trim()
}
# Feldbasierte Befehle (erstellen, status, abschliessen): nichts aus -Eingabe darf still verloren gehen.
function Assert-Eingabe([string[]]$keys) {
    if (-not $Eingabe) { return }
    $ok = @($keys | ForEach-Object { Norm $_ })
    $fremd = @($script:InNamen | Where-Object { $ok -notcontains (Norm $_) } | Sort-Object -Unique)
    $msg = @()
    if ($fremd.Count -gt 0) { $msg += ('unbekannte Felder: ' + ($fremd -join ', ') + ' (erlaubt: ' + ($keys -join ', ') + '; Folgezeilen mit zwei Leerzeichen einruecken)') }
    if ($script:InProbleme.Count -gt 0) { $msg += (($script:InProbleme | Select-Object -First 3) -join '; ') + ' (Abschnitte nur mit aendern -Abschnitt)' }
    if ($msg.Count -gt 0) { Stop-Auftrag 1 ('-Eingabe abgelehnt, nichts geschrieben: ' + ($msg -join ' | ')) }
}
function Get-In([string[]]$keys) {
    foreach ($k in $keys) { $n = Norm $k; if ($script:InFelder.Contains($n)) { return $script:InFelder[$n] } }
    return $null
}
function Pick([string]$param, [string[]]$keys) {
    if ($param) { return $param }
    $v = Get-In $keys
    if ($v) { return $v }
    return ''
}

# ---------------------------------------------------------------- Befehl: erstellen
$script:ErstellenKeys = @('An', 'Titel', 'Prio', 'Aufgabe', 'Ergebnis', 'Begruendung', 'Uebergabegrund', 'Kontext', 'Dateien', 'Abhaengigkeiten', 'UX', 'Freigabe', 'Folgeschritt',
    'Empfaenger', 'Prioritaet', 'Aufgabenbeschreibung', 'Erwartetes Ergebnis', 'Technischer Kontext', 'Betroffene Dateien', 'UX-Beteiligung', 'Benutzerfreigabe')
$script:ErledigtModus = $false
function Invoke-Erstellen {
    if (-not $script:RoleN) { Stop-Auftrag 1 '-Rolle fehlt oder ist ungueltig' }
    if (-not $script:ErledigtModus) {
        Assert-Eingabe $script:ErstellenKeys
    }
    $an0 = Pick $An @('an', 'empfaenger')
    $titel = (Pick $Titel @('titel')) -replace "[\r\n]+", ' '
    $prioIn = ''; if ($Prio) { $prioIn = ($Prio -join '') } else { $prioIn = Pick '' @('prio', 'prioritaet') }
    $aufgabe = Pick $Aufgabe @('aufgabe', 'aufgabenbeschreibung')
    $erg = Pick $Ergebnis @('ergebnis', 'erwartetes ergebnis')
    $ugr = Pick $Uebergabegrund @('uebergabegrund')
    $ktx = Pick $Kontext @('kontext', 'technischer kontext')
    $dat = Pick $Dateien @('dateien', 'betroffene dateien')
    $abh = Pick $Abhaengigkeiten @('abhaengigkeiten')
    $ux = Pick $UX @('ux', 'ux-beteiligung')
    $frg = Pick $Freigabe @('freigabe', 'benutzerfreigabe')
    $fol = Pick $Folgeschritt @('folgeschritt')
    $beg = Pick $Begruendung @('begruendung')
    $miss = @()
    if (-not $an0) { $miss += '-An' }
    if (-not $titel) { $miss += '-Titel' }
    if (-not $prioIn) { $miss += '-Prio' }
    if (-not $aufgabe) { $miss += '-Aufgabe' }
    if (-not $erg) { $miss += '-Ergebnis (Erwartetes Ergebnis)' }
    if ($miss.Count -gt 0) { Stop-Auftrag 1 ('Pflichtfelder fehlen: ' + ($miss -join ', ')) }
    $anR = Get-RoleName $an0
    if (-not $anR -or $anR -eq 'Benutzer') { Stop-Auftrag 1 "Empfaenger unbekannt: $an0 (erlaubt: $($Roles -join ', '))" }
    $pr = $prioIn.Trim().ToUpperInvariant()
    if (@('LOW', 'MEDIUM', 'HIGH', 'CRITICAL') -notcontains $pr) { Stop-Auftrag 1 "Prioritaet ungueltig: $prioIn (LOW, MEDIUM, HIGH, CRITICAL)" }
    if (($pr -eq 'HIGH' -or $pr -eq 'CRITICAL') -and -not $beg) { Stop-Auftrag 1 "Begruendung (-Begruendung) bei $pr Pflicht" }
    if ($anR -eq $script:RoleN -and -not $script:ErledigtModus -and $anR -ne 'Planung') { Add-Warnung 'Empfaenger ist die eigene Rolle: eigene Arbeit mit "erledigt" erfassen (README 3).' }
    if ($anR -eq 'Planung' -and $script:RoleN -ne 'Planung' -and $script:RoleN -ne 'Benutzer') { Add-Warnung 'Auftrag an die Planung nur bei Koordinationsbedarf oder fuer Ideen (README 3).' }
    if ($beg) { $ugr = (($ugr + ' Begruendung ' + $pr + ': ' + $beg).Trim()) }

    # Dublettenpruefung: aehnlicher Titel mit mindestens zwei gemeinsamen Woertern -> Abbruch vor dem Schreiben (Exit 3,
    # ausser -Trotzdem); sonst nur Warnung. In -Abhaengigkeiten genannte Vorgaenger sind ausgenommen (erwartete Ueberschneidung).
    $abhIds = @([regex]::Matches($abh, 'SFLIX-\d{8}-\d{3,}') | ForEach-Object { $_.Value.ToUpperInvariant() })
    $dubl = New-Object System.Collections.Generic.List[string]
    $words = { param($s) @(([regex]::Split((Norm $s), '[^a-z0-9]+')) | Where-Object { $_.Length -ge 3 } | Sort-Object -Unique) }
    $wN = & $words $titel
    # '*' gehoert zum Pfad (z. B. .claude/skills/*/SKILL.md), sonst bliebe nur das Endstueck /skill.md (Fehlalarm)
    $pathRx = $script:PathRx
    # fuehrende Markdown-Sternchen (**Datei.kt**) abschneiden, Platzhalter wie */SKILL.md behalten
    $pN = @([regex]::Matches($dat, $pathRx) | ForEach-Object { ($_.Value.Replace('\', '/').ToLowerInvariant()) -replace '^\*+(?!/)', '' } | Sort-Object -Unique)
    foreach ($p in (Get-TaskFiles @('OPEN', 'IN_PROGRESS', 'BLOCKED'))) {
        try {
            $o = Load-Task $p
            if ($abhIds -contains $o.Id) { continue }
            $ow = & $words $o.Title
            $inter = @($wN | Where-Object { $ow -contains $_ }).Count
            $uni = @($wN + $ow | Sort-Object -Unique).Count
            if ($uni -gt 0 -and ($inter / $uni) -ge 0.6) {
                if ($inter -ge 2) { [void]$dubl.Add(("{0} {1}" -f $o.Id, $o.Title)) }
                else { Add-Warnung ("Moegliche Dublette (aehnlicher Titel): {0} {1}" -f $o.Id, $o.Title) }
            }
            $oP = @([regex]::Matches((Get-Val $o $FN.Dateien), $pathRx) | ForEach-Object { ($_.Value.Replace('\', '/').ToLowerInvariant()) -replace '^\*+(?!/)', '' } | Sort-Object -Unique)
            $gem = @($pN | Where-Object { $oP -contains $_ })
            if ($gem.Count -gt 0) { Add-Warnung ("Gleiche Dateien in aktivem Auftrag {0}: {1}" -f $o.Id, ($gem -join ', ')) }
        }
        catch { }
    }
    if ($dubl.Count -gt 0) {
        if (-not $Trotzdem) { Stop-Auftrag 3 ('Moegliche Dublette, nichts angelegt: ' + ($dubl -join '; ') + '. Bestehenden Auftrag ergaenzen (aendern) oder bewusst mit -Trotzdem anlegen; Vorgaenger unter -Abhaengigkeiten sind ausgenommen.') }
        foreach ($x in $dubl) { Add-Warnung ("Moegliche Dublette (mit -Trotzdem angelegt): $x") }
    }

    $build = {
        param($id)
        $L = New-Object System.Collections.Generic.List[string]
        [void]$L.Add("# $id $ND $titel")
        $pairs = @(
            @($FN.Erstellt, $script:Heute), @($FN.Absender, $script:RoleN), @($FN.Empfaenger, $anR), @($FN.Prio, $pr), @($FN.Status, 'OPEN'),
            @($FN.Uebergabe, $ugr), @($FN.Aufgabe, $aufgabe), @($FN.Kontext, $ktx), @($FN.Dateien, $dat), @($FN.UX, $ux),
            @($FN.Freigabe, $frg), @($FN.Abh, $abh), @($FN.Folge, $fol), @($FN.Ergebnis, $erg),
            @($FN.Verlauf, ($script:Heute + ' ' + $script:RoleN + ' OPEN')), @($FN.Uebernahme, $ND)
        )
        foreach ($pr2 in $pairs) {
            $v = [string]$pr2[1]
            if ($v.Trim() -eq '') { continue }
            $vl = ($v -replace "`r`n", "`n") -split "`n"
            [void]$L.Add('- ' + $pr2[0] + ': ' + $vl[0].TrimEnd())
            for ($i = 1; $i -lt $vl.Count; $i++) { [void]$L.Add('  ' + $vl[$i].TrimEnd()) }
        }
        return (($L -join "`r`n") + "`r`n")
    }

    $datum = (Get-Date -Format 'yyyyMMdd')
    if ($WhatIf) {
        $n = (Get-MaxNummer) + 1
        $id = ('SFLIX-{0}-{1:000}' -f $datum, $n)
        Add-Plan "erstellen: $id (An $anR, $pr): $titel"
        Add-Plan ((& $build $id) -replace "`r`n", "`n")
        [void]$script:Ids.Add($id)
        return 0
    }
    Enter-Lock
    $n = (Get-MaxNummer) + 1
    $fs = $null; $id = $null; $path = $null
    for ($try = 0; $try -lt 50; $try++) {
        $id = ('SFLIX-{0}-{1:000}' -f $datum, $n)
        $path = Join-Path (Join-Path $script:TasksDir 'OPEN') ($id + '.md')
        Assert-InRoot $path
        if (-not (Test-Path -LiteralPath (Split-Path -Parent $path))) { [void](New-Item -ItemType Directory -Path (Split-Path -Parent $path)) }
        try { $fs = [IO.FileStream]::new($path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None); break }
        catch { if (-not (Test-IoEx $_.Exception)) { throw }; $fs = $null; $n++ }
    }
    if ($null -eq $fs) { Stop-Auftrag 6 'Keine freie Auftragsnummer gefunden (50 Versuche).' }
    try {
        Halt 'nach-anlegen'
        $bytes = $script:Utf8.GetBytes((& $build $id))
        $fs.Write($bytes, 0, $bytes.Length); $fs.Flush($true)
    }
    finally { $fs.Dispose() }
    Halt 'nach-schreiben'
    Invoke-Nachlauf $id { Update-Views; Add-Audit 'erstellen' $id ("An $anR, $pr, $titel") 'ok' }
    [void]$script:Ids.Add($id)
    Add-Msg "Erstellt: $id (An $anR, $pr)"
    return 0
}

# ---------------------------------------------------------------- Berechtigung, Verlauf
function Test-Recht([string]$recht, $t) {
    $emp = Get-RoleName (Get-Val $t $FN.Empfaenger)
    $abs = Get-RoleName (Get-Val $t $FN.Absender)
    $ok = $false
    if ($recht -eq 'Empfaenger') { $ok = ($script:RoleN -eq $emp) }
    elseif ($recht -eq 'AbsenderOderBenutzer') { $ok = ($script:RoleN -eq $abs -or $script:RoleN -eq 'Benutzer') }
    elseif ($recht -eq 'Alle') { $ok = $true }
    if ($ok) { return '' }
    if ($Benutzerfreigabe) { return (' (Benutzerfreigabe: ' + ($Benutzerfreigabe -replace "[\r\n]+", ' ').Trim() + ')') }
    $wer = if ($recht -eq 'Empfaenger') { "Empfaenger ($emp)" } else { "Absender ($abs) oder Benutzer" }
    Stop-Auftrag 2 ("Berechtigung: diese Aktion ist nur durch $wer vorgesehen, -Rolle ist $($script:RoleN). Ausnahme nur mit -Benutzerfreigabe <Grund>.")
}
function Get-TargetFolder([string]$st) {
    if ($st -eq 'COMPLETED' -or $st -eq 'CANCELLED') { return 'COMPLETED' }
    return $st
}
function Invoke-Nachlauf([string]$id, [scriptblock]$sb) {
    try { & $sb }
    catch {
        if ($null -ne $_.Exception.Data['code']) { $c = [int]$_.Exception.Data['code'] } else { $c = 6 }
        [void]$script:Ids.Add($id)
        Stop-Auftrag $c ("Auftrag $id ist geschrieben, aber Uebersicht/Audit fehlgeschlagen: $($_.Exception.Message). Nicht erneut ausfuehren, sondern check -Fix aufrufen.")
    }
}
function Finish-Write($t, [string]$cmd, [string]$detail) {
    # Reihenfolge README 10: Datei ersetzt -> verschieben -> Schreibschutz -> Index -> Posteingaenge -> Audit
    $st = Get-StatusWort $t
    $target = Get-TargetFolder $st
    if ($t.Folder -ne $target) { Move-Task $t $target }
    if ($st -eq 'COMPLETED' -or $st -eq 'CANCELLED') { Set-Schreibschutz $t.Path $true }
    Invoke-Nachlauf $t.Id { Update-Views; Add-Audit $cmd $t.Id $detail 'ok' }
}

# ---------------------------------------------------------------- Befehl: status
function Invoke-Status {
    if (-not $script:RoleN) { Stop-Auftrag 1 '-Rolle fehlt oder ist ungueltig' }
    Assert-Eingabe @('Neu', 'Status', 'Notiz', 'Frage')
    $neu = (Pick $Neu @('neu', 'status')).Trim().ToUpperInvariant()
    $notiz = Pick $Notiz @('notiz')
    $frage = Pick $Frage @('frage')
    if (@('IN_PROGRESS', 'BLOCKED', 'CANCELLED') -notcontains $neu) {
        if ($neu -eq 'COMPLETED') { Stop-Auftrag 1 'COMPLETED nur ueber den Befehl abschliessen' }
        Stop-Auftrag 1 '-Neu muss IN_PROGRESS, BLOCKED oder CANCELLED sein'
    }
    if ($neu -eq 'BLOCKED' -and -not $frage) { Stop-Auftrag 1 'Bei BLOCKED ist -Frage Pflicht (wird unter Offene Fragen eingetragen)' }
    if ($neu -eq 'CANCELLED' -and -not $notiz) { Stop-Auftrag 1 'Bei CANCELLED ist -Notiz (Begruendung) Pflicht' }
    if (-not $WhatIf) { Enter-Lock }
    $path = Find-TaskPath $Id
    $t = Load-Task $path
    Assert-Eindeutig $t
    $alt = Get-StatusWort $t
    $trans = @{ 'OPEN' = @('IN_PROGRESS', 'BLOCKED', 'CANCELLED'); 'IN_PROGRESS' = @('BLOCKED', 'CANCELLED'); 'BLOCKED' = @('IN_PROGRESS', 'CANCELLED') }
    if ($alt -eq 'COMPLETED' -or $alt -eq 'CANCELLED') { Stop-Auftrag 3 "Auftrag $($t.Id) ist bereits abgeschlossen ($alt); kein Statuswechsel. Neue Arbeit als Folgeauftrag (README 6)." }
    if (-not $trans.ContainsKey($alt) -or ($trans[$alt] -notcontains $neu)) { Stop-Auftrag 3 "Unzulaessiger Statuswechsel: $alt -> $neu (README 6)" }
    $recht = if ($neu -eq 'CANCELLED') { 'AbsenderOderBenutzer' } else { 'Empfaenger' }
    $fr = Test-Recht $recht $t
    $entry = $script:Heute + ' ' + $script:RoleN + ' ' + $neu
    if ($notiz) { $entry += ' (' + ($notiz -replace "[\r\n]+", ' ').Trim() + ')' }
    $entry += $fr
    if ($WhatIf) { Add-Plan "status: $($t.Id) $alt -> $neu; Verlauf: $entry"; [void]$script:Ids.Add($t.Id); return 0 }
    $n = Set-FeldWert $t $FN.Status $neu
    if ($neu -eq 'IN_PROGRESS') {
        $uv = Get-Val $n $FN.Uebernahme
        if ($alt -eq 'OPEN' -or (Test-Leer $uv)) { $n = Set-FeldWert $n $FN.Uebernahme ($script:Heute + ', ' + $script:RoleN) }
    }
    if ($neu -eq 'BLOCKED') { $n = Add-FeldText $n $FN.Fragen ($script:Heute + ' ' + $script:RoleN + ': ' + $frage) }
    if ($neu -eq 'CANCELLED') { $n = Set-FeldWert $n $FN.Abgeschl $script:Heute }
    $n = Add-Verlauf $n $entry
    Assert-Eindeutig $n
    Save-Task $n ($n.Lines -join "`n")
    Finish-Write $n 'status' "$alt -> $neu"
    [void]$script:Ids.Add($n.Id)
    Add-Msg "$($n.Id): $alt -> $neu"
    return 0
}

# ---------------------------------------------------------------- Befehl: abschliessen
# -Entscheide: "keine" oder DEC-IDs aus docs/DECISIONS.md (Pflicht beim Abschluss, CLAUDE.md "Entscheidungen festhalten")
function Get-EntscheideWert([string]$v) {
    $v = ($v -replace "[\r\n]+", ' ').Trim()
    if ($v -eq '') { return '' }
    if ((Norm $v) -match '^keine?\b') { return $v }
    $ids = @([regex]::Matches($v, '(?i)\bDEC-\d{3,}\b') | ForEach-Object { $_.Value.ToUpperInvariant() })
    if ($ids.Count -eq 0) { Stop-Auftrag 1 ('-Entscheide braucht DEC-IDs (z. B. "DEC-329, DEC-330") oder "keine": ' + $v) }
    return $v
}

function Invoke-Abschliessen {
    if (-not $script:RoleN) { Stop-Auftrag 1 '-Rolle fehlt oder ist ungueltig' }
    if (-not $script:ErledigtModus) {
        Assert-Eingabe @('Arbeiten', 'Tests', 'Risiken', 'Folgeschritt', 'Notiz', 'Entscheide', 'Entscheidungen', 'Durchgefuehrte Arbeiten', 'Tests und Ergebnisse', 'Verbleibende Risiken')
    }
    $arb = Pick $Arbeiten @('arbeiten', 'durchgefuehrte arbeiten')
    $tst = Pick $Tests @('tests', 'tests und ergebnisse')
    $rsk = Pick $Risiken @('risiken', 'verbleibende risiken')
    $fol = Pick $Folgeschritt @('folgeschritt')
    $notiz = Pick $Notiz @('notiz')
    $ent = Get-EntscheideWert (Pick $Entscheide @('entscheide', 'entscheidungen'))
    if (-not $WhatIf -and -not $script:ErledigtModus) { Enter-Lock }
    $path = Find-TaskPath $Id
    $t = Load-Task $path
    Assert-Eindeutig $t
    $alt = Get-StatusWort $t
    if ($alt -eq 'COMPLETED' -or $alt -eq 'CANCELLED') { Stop-Auftrag 3 "Auftrag $($t.Id) ist bereits abgeschlossen ($alt); nichts geaendert." }
    if ($alt -ne 'IN_PROGRESS') { Stop-Auftrag 3 "Abschluss nur aus IN_PROGRESS moeglich (aktuell $alt)" }
    $fr = Test-Recht 'Empfaenger' $t
    $n = $t
    if ($arb) { $n = Add-FeldText $n $FN.Arbeiten $arb }
    if ($tst) { $n = Add-FeldText $n $FN.Tests $tst }
    if ($rsk) { $n = Add-FeldText $n $FN.Risiken $rsk }
    if ($fol) { $n = Add-FeldText $n $FN.Folge $fol }
    if ($ent) { $n = Add-FeldText $n $FN.Entscheide $ent }
    $fa = Get-Fld $n $FN.Arbeiten
    $both = ($null -ne $fa -and $fa.Key -eq $script:BothKey)
    $fv = Get-Fld $n $FN.Tests
    $miss = @()
    if ($null -eq $fa -or (Test-Leer (Get-FldVal $n $fa))) { $miss += $FN.Arbeiten }
    if (-not $both -and ($null -eq $fv -or (Test-Leer (Get-FldVal $n $fv)))) { $miss += $FN.Tests }
    if (Test-Leer (Get-Val $n $FN.Entscheide)) { $miss += ($FN.Entscheide + ' (-Entscheide "DEC-..." oder "keine")') }
    if ($miss.Count -gt 0) { Stop-Auftrag 1 ('Pflichtfelder fuer den Abschluss fehlen oder sind leer: ' + ($miss -join ', ')) }
    $entry = $script:Heute + ' ' + $script:RoleN + ' COMPLETED'
    if ($notiz) { $entry += ' (' + ($notiz -replace "[\r\n]+", ' ').Trim() + ')' }
    $entry += $fr
    if ($WhatIf) { Add-Plan "abschliessen: $($t.Id) IN_PROGRESS -> COMPLETED; Verlauf: $entry"; [void]$script:Ids.Add($t.Id); return 0 }
    $n = Set-FeldWert $n $FN.Status 'COMPLETED'
    $n = Set-FeldWert $n $FN.Abgeschl $script:Heute
    $n = Add-Verlauf $n $entry
    Assert-Eindeutig $n
    Save-Task $n ($n.Lines -join "`n")
    Finish-Write $n 'abschliessen' 'IN_PROGRESS -> COMPLETED'
    [void]$script:Ids.Add($n.Id)
    Add-Msg "$($n.Id): COMPLETED"
    return 0
}

# ---------------------------------------------------------------- Befehl: erledigt
# Eigene Arbeit in einem Aufruf erfassen (CLAUDE.md "Tickets"): erstellen (An = eigene Rolle) -> IN_PROGRESS -> COMPLETED,
# unter einer Sperre. Pflicht: -Titel, -Aufgabe, -Arbeiten, -Tests, -Entscheide; -Prio Standard LOW.
function Invoke-Erledigt {
    if (-not $script:RoleN -or $script:RoleN -eq 'Benutzer') { Stop-Auftrag 1 'erledigt braucht -Rolle (eigene Rolle, nicht Benutzer)' }
    $script:ErledigtModus = $true
    Assert-Eingabe ($script:ErstellenKeys + @('Arbeiten', 'Tests', 'Risiken', 'Notiz', 'Entscheide', 'Entscheidungen', 'Durchgefuehrte Arbeiten', 'Tests und Ergebnisse', 'Verbleibende Risiken'))
    $an0 = Pick $An @('an', 'empfaenger')
    if ($an0 -and (Get-RoleName $an0) -ne $script:RoleN) { Stop-Auftrag 1 'erledigt gilt nur fuer die eigene Rolle; fuer andere Rollen erstellen verwenden' }
    $script:An = $script:RoleN
    if (-not $Prio -and -not (Get-In @('prio', 'prioritaet'))) { $script:Prio = @('LOW') }
    if (-not (Pick $Ergebnis @('ergebnis', 'erwartetes ergebnis'))) { $script:Ergebnis = 'Erledigt in derselben Sitzung (siehe Durchgefuehrte Arbeiten)' }
    $miss = @()
    if (-not (Pick $Arbeiten @('arbeiten', 'durchgefuehrte arbeiten'))) { $miss += '-Arbeiten' }
    if (-not (Pick $Tests @('tests', 'tests und ergebnisse'))) { $miss += '-Tests' }
    if (-not (Pick $Entscheide @('entscheide', 'entscheidungen'))) { $miss += '-Entscheide ("DEC-..." oder "keine")' }
    if ($miss.Count -gt 0) { Stop-Auftrag 1 ('Pflichtfelder fehlen, nichts angelegt: ' + ($miss -join ', ')) }
    [void](Get-EntscheideWert (Pick $Entscheide @('entscheide', 'entscheidungen')))
    $r = Invoke-Erstellen
    if ($WhatIf) { Add-Plan 'danach: IN_PROGRESS und COMPLETED (erledigt)'; return $r }
    $neueId = $script:Ids[$script:Ids.Count - 1]
    $t = Load-Task (Find-TaskPath $neueId)
    $n = Set-FeldWert $t $FN.Status 'IN_PROGRESS'
    $n = Set-FeldWert $n $FN.Uebernahme ($script:Heute + ', ' + $script:RoleN)
    $n = Add-Verlauf $n ($script:Heute + ' ' + $script:RoleN + ' IN_PROGRESS (erledigt)')
    Save-Task $n ($n.Lines -join "`n")
    $script:Id = $neueId
    return (Invoke-Abschliessen)
}

# ---------------------------------------------------------------- Befehl: aendern
function Invoke-Aendern {
    if (-not $script:RoleN) { Stop-Auftrag 1 '-Rolle fehlt oder ist ungueltig' }
    $notiz = Pick $Notiz @('notiz')
    # Abgeschlossene Auftraege sind gesperrt; Nachfuehrung nur ausnahmsweise mit -Erzwingen und Begruendung (nur Anhaengen)
    if ($Erzwingen -and -not $notiz) { Stop-Auftrag 1 '-Erzwingen braucht -Notiz "<Grund der Nachfuehrung>"' }
    $abs = $Abschnitt
    $feld = $Feld
    $wert = $Wert
    if ($abs) {
        if ($Feld) { Stop-Auftrag 1 '-Feld und -Abschnitt schliessen sich aus' }
        if (-not $script:InText) { Stop-Auftrag 1 '-Abschnitt braucht den Text in -Eingabe' }
        $wert = $script:InText
    }
    else {
        if (-not $feld) { Stop-Auftrag 1 '-Feld oder -Abschnitt fehlt' }
        $key = Norm $feld
        # Kurznamen wie bei erstellen (nur fuer aendern, nicht fuer den Parser der Task-Dateien)
        $kurz = @{ 'aufgabe' = 'aufgabenbeschreibung'; 'ergebnis' = 'erwartetes ergebnis'; 'dateien' = 'betroffene dateien'; 'kontext' = 'technischer kontext'
            'ux' = 'ux-beteiligung'; 'freigabe' = 'benutzerfreigabe'; 'prio' = 'prioritaet'; 'arbeiten' = 'durchgefuehrte arbeiten'; 'risiken' = 'verbleibende risiken'
            'fragen' = 'offene fragen'; 'verlauf' = 'bearbeitungsverlauf'; 'entscheide' = 'entscheidungen'
        }
        if (-not $script:Alias.ContainsKey($key) -and $kurz.ContainsKey($key)) { $key = $kurz[$key] }
        if (-not $script:Alias.ContainsKey($key)) { Stop-Auftrag 1 ("Unbekanntes Feld: $feld (erlaubt: " + (($FN.Values | Sort-Object -Unique) -join ', ') + '; Kurznamen: ' + (($kurz.Keys | Sort-Object) -join ', ') + ')') }
        $canon = $script:Alias[$key]
        if ($canon -eq $FN.Prio -and @('LOW', 'MEDIUM', 'HIGH', 'CRITICAL') -notcontains $wert.Trim().ToUpperInvariant() -and $Wert) { Stop-Auftrag 1 'Prioritaet ungueltig (LOW, MEDIUM, HIGH, CRITICAL)' }
        if ($NotChangeable -contains $canon) { Stop-Auftrag 1 "Feld nicht aenderbar: $canon (ID, Titel, Status, Absender, Empfaenger, Erstellt am, Uebernahmebestaetigung, Abgeschlossen am)" }
        if (-not $wert) {
            $wert = Get-In @($feld, $canon)
            if (-not $wert -and $script:InText) { $wert = $script:InText }
        }
        if (-not $wert) { Stop-Auftrag 1 '-Wert oder -Eingabe fehlt' }
    }
    if (-not $WhatIf) { Enter-Lock }
    $path = Find-TaskPath $Id
    $t = Load-Task $path
    Assert-Eindeutig $t
    $st = Get-StatusWort $t
    $abgeschl = ($st -eq 'COMPLETED' -or $st -eq 'CANCELLED')
    if ($abgeschl -and -not $Erzwingen) {
        $am = Get-Val $t $FN.Abgeschl
        $am = if (Test-Leer $am) { '' } else { ', ' + $am.Trim() }
        Stop-Auftrag 3 ("Auftrag $($t.Id) ist bereits abgeschlossen ($st$am); nichts geaendert. Neue Arbeit als Folgeauftrag. " +
            'Nachfuehrung nur ausnahmsweise: -Erzwingen -Notiz "<Grund>" (nur Anhaengen).')
    }
    if ($Erzwingen -and -not $abgeschl) { Stop-Auftrag 1 "-Erzwingen nur fuer abgeschlossene Auftraege ($($t.Id) ist $st); ohne -Erzwingen aufrufen." }
    $erz = ''
    if ($Erzwingen) { $erz = ' [erzwungen: ' + ($notiz -replace "[\r\n]+", ' ').Trim() + ']' }
    $n = $t
    if ($abs) {
        $hadTail = ($n.Lines.Count -gt 0 -and $n.Lines[$n.Lines.Count - 1] -eq '')
        if ($hadTail) { $n.Lines.RemoveAt($n.Lines.Count - 1) }
        $n.Lines.Add((Mk $n '')); $n.Lines.Add((Mk $n ('## ' + $abs.Trim())))
        foreach ($x in (($wert -replace "`r`n", "`n") -split "`n")) { $n.Lines.Add((Mk $n $x.TrimEnd())) }
        if ($hadTail) { $n.Lines.Add('') }
        $n = Add-Verlauf (Reparse $n) ($script:Heute + ' ' + $script:RoleN + ' ' + (UniText 'ge\u00e4ndert') + ': Abschnitt ' + $abs.Trim() + $erz)
        $what = "Abschnitt $abs"
    }
    elseif ($canon -eq $FN.Verlauf) {
        $n = Add-Verlauf $n ($script:Heute + ' ' + $script:RoleN + ': ' + ($wert -replace "[\r\n]+", ' ').Trim() + $erz)
        $what = $canon
    }
    else {
        $doAppend = ($Anhaengen -or ($AppendOnly -contains $canon))
        if ($abgeschl -and -not $doAppend) { Stop-Auftrag 3 "Abgeschlossener Auftrag ($st): auch mit -Erzwingen nur Anhaengen (-Anhaengen), kein Ersetzen." }
        if ($doAppend) { $n = Add-FeldText $n $canon $wert } else {
            if ($null -ne (Get-Fld $n $canon)) { $n = Set-FeldWert $n $canon (($wert -replace "`r`n", "`n") -split "`n")[0] ; $rest = (($wert -replace "`r`n", "`n") -split "`n") | Select-Object -Skip 1; if ($rest) { $n = Add-FeldText $n $canon ($rest -join "`n") } }
            else { $n = Add-NeuesFeld $n $canon $wert }
        }
        $verb = if ($doAppend) { (UniText 'erg\u00e4nzt') } else { (UniText 'ge\u00e4ndert') }
        $e = $script:Heute + ' ' + $script:RoleN + ' ' + $verb + ': ' + $canon
        if ($erz) { $e += $erz } elseif ($notiz) { $e += ' (' + ($notiz -replace "[\r\n]+", ' ').Trim() + ')' }
        $n = Add-Verlauf $n $e
        $what = $canon
    }
    Assert-Eindeutig $n
    if ($WhatIf) { Add-Plan "aendern: $($t.Id) $what"; [void]$script:Ids.Add($t.Id); return 0 }
    Save-Task $n ($n.Lines -join "`n")
    Invoke-Nachlauf $n.Id { Update-Views; Add-Audit 'aendern' $n.Id $what 'ok' }
    [void]$script:Ids.Add($n.Id)
    Add-Msg "$($n.Id): $what"
    return 0
}

# ---------------------------------------------------------------- Befehl: liste
# Gruppenvorschlag (liste -Gruppen): Auftraege mit mindestens einer gemeinsamen Datei bilden eine Gruppe (in einem Durchgang
# bearbeiten, Dateien nur einmal lesen). Einzelne Auftraege im selben Ordner wie eine Gruppe erscheinen als "passt dazu".
# Reihenfolge: hoechste Prioritaet in der Gruppe, dann kleinste Nummer. BLOCKED wird nicht gruppiert.
function Get-PfadSchluessel([string]$dateien) {
    return @([regex]::Matches($dateien, $script:PathRx) | ForEach-Object { ($_.Value.Replace('\', '/').ToLowerInvariant()) -replace '^\*+(?!/)', '' -replace '^\./', '' } | Where-Object { $_ -notmatch '\*' } | Sort-Object -Unique)
}
function Add-GruppenVorschlag($recs) {
    if ($recs.Count -lt 2) { return }
    $rang = @{ 'CRITICAL' = 0; 'HIGH' = 1; 'MEDIUM' = 2; 'LOW' = 3 }
    $wurzel = @{}
    foreach ($r in $recs) { $wurzel[$r.Id] = $r.Id }
    $finde = { param($x) while ($wurzel[$x] -ne $x) { $x = $wurzel[$x] }; return $x }
    $pfade = @{}
    foreach ($r in $recs) { $pfade[$r.Id] = @(Get-PfadSchluessel ([string]$r.Dateien)) }
    $besitzer = @{}
    foreach ($r in $recs) {
        foreach ($p in $pfade[$r.Id]) {
            if ($besitzer.ContainsKey($p)) {
                $a = & $finde $besitzer[$p]; $b = & $finde $r.Id
                if ($a -ne $b) { $wurzel[$b] = $a }
            }
            else { $besitzer[$p] = $r.Id }
        }
    }
    $gruppen = @{}
    foreach ($r in $recs) {
        $w = & $finde $r.Id
        if (-not $gruppen.ContainsKey($w)) { $gruppen[$w] = New-Object System.Collections.Generic.List[object] }
        [void]$gruppen[$w].Add($r)
    }
    $liste = @()
    foreach ($k in $gruppen.Keys) {
        $g = $gruppen[$k]
        $best = 9; foreach ($r in $g) { $x = 9; if ($rang.ContainsKey([string]$r.Prio)) { $x = $rang[[string]$r.Prio] }; if ($x -lt $best) { $best = $x } }
        $minNum = ($g | Measure-Object -Property Num -Minimum).Minimum
        $liste += [pscustomobject]@{ Mitglieder = $g; Rang = $best; Num = $minNum }
    }
    $mehr = @($liste | Where-Object { $_.Mitglieder.Count -gt 1 } | Sort-Object Rang, Num)
    $einzel = @($liste | Where-Object { $_.Mitglieder.Count -eq 1 } | Sort-Object Rang, Num)
    if ($mehr.Count -eq 0) { Add-Msg ''; Add-Msg 'Gruppen: keine (keine gemeinsamen Dateien).'; return }
    Add-Msg ''
    Add-Msg 'Gruppen (gemeinsame Dateien, in einem Durchgang bearbeiten):'
    $i = 0
    foreach ($g in $mehr) {
        $i++
        $ids = @($g.Mitglieder | Sort-Object Num | ForEach-Object { $_.Id })
        $gem = @{}
        foreach ($m in $g.Mitglieder) { foreach ($p in $pfade[$m.Id]) { if ($gem.ContainsKey($p)) { $gem[$p]++ } else { $gem[$p] = 1 } } }
        $geteilt = @($gem.Keys | Where-Object { $gem[$_] -gt 1 } | Sort-Object)
        $prioTxt = @('CRITICAL', 'HIGH', 'MEDIUM', 'LOW', '?')[[Math]::Min($g.Rang, 4)]
        Add-Msg ('  Gruppe {0} [{1}]: {2}' -f $i, $prioTxt, ($ids -join ', '))
        Add-Msg ('    gemeinsam: ' + ($geteilt -join ', '))
        $ordner = @($geteilt | ForEach-Object { if ($_ -match '/') { $_.Substring(0, $_.LastIndexOf('/')) } else { '' } } | Sort-Object -Unique)
        $dazu = @()
        foreach ($e in $einzel) {
            $r = $e.Mitglieder[0]
            foreach ($p in $pfade[$r.Id]) {
                $o = ''; if ($p -match '/') { $o = $p.Substring(0, $p.LastIndexOf('/')) }
                if ($o -and $ordner -contains $o) { $dazu += $r.Id; break }
            }
        }
        if ($dazu.Count -gt 0) { Add-Msg ('    passt dazu (gleicher Ordner): ' + (($dazu | Sort-Object -Unique) -join ', ')) }
    }
    $rest = @($einzel | ForEach-Object { $_.Mitglieder[0].Id })
    if ($rest.Count -gt 0) { Add-Msg ('  Einzeln: ' + ($rest -join ', ')) }
}

function Invoke-Liste {
    $want = @()
    if ($Status) {
        foreach ($s in $Status) { foreach ($p in ($s -split '[,;\s]+')) { if ($p) { $want += $p.ToUpperInvariant() } } }
        if ($want -contains 'ALLE') { $want = $AllStatus }
        foreach ($w in $want) { if ($AllStatus -notcontains $w) { Stop-Auftrag 1 "Unbekannter Status: $w" } }
    }
    elseif ($script:RoleN -and $script:RoleN -ne 'Benutzer' -and -not $An) { $want = @('OPEN', 'IN_PROGRESS', 'BLOCKED') }   # eigener Posteingang
    else { $want = @('OPEN', 'IN_PROGRESS') }
    $folders = @('OPEN', 'IN_PROGRESS', 'BLOCKED')
    if ($want -contains 'COMPLETED' -or $want -contains 'CANCELLED') { $folders += 'COMPLETED' }
    $anF = $null
    if ($An) { $anF = Get-RoleName $An; if (-not $anF) { Stop-Auftrag 1 "Unbekannte Rolle: $An" } }
    elseif ($script:RoleN -and $script:RoleN -ne 'Benutzer') { $anF = $script:RoleN }
    $vonF = $null
    if ($Von) { $vonF = Get-RoleName $Von; if (-not $vonF) { Stop-Auftrag 1 "Unbekannte Rolle: $Von" } }
    $prF = @(); if ($Prio) { foreach ($p in $Prio) { $prF += ($p -split '[,;\s]+') | Where-Object { $_ } | ForEach-Object { $_.ToUpperInvariant() } } }
    $recs = @()
    foreach ($p in (Get-TaskFiles $folders)) {
        try {
            $t = Load-Task $p
            if ($t.Problems.Count -gt 0) { Add-Warnung ("{0}: {1}" -f (Split-Path -Leaf $p), ($t.Problems -join '; ')); continue }
            $r = Get-Record $t
            if ($MitDateien -or $Gruppen) { $r | Add-Member -NotePropertyName Dateien -NotePropertyValue (((Get-Val $t $FN.Dateien) -replace '[\r\n]+', ' ').Trim()) }
            if ($Gruppen -and $r.Status -eq 'BLOCKED') {
                $fq = @(((Get-Val $t $FN.Fragen) -replace "`r", '') -split "`n" | Where-Object { $_.Trim() -ne '' })
                $lf = ''; if ($fq.Count -gt 0) { $lf = $fq[$fq.Count - 1].Trim() }
                $r | Add-Member -NotePropertyName Frage -NotePropertyValue $lf
            }
            if ($Text) {
                $full = ($t.Lines -join "`n")
                if ($full.IndexOf($Text, [StringComparison]::OrdinalIgnoreCase) -lt 0) { continue }
            }
            $recs += $r
        }
        catch { Add-Warnung ("{0}: nicht lesbar" -f (Split-Path -Leaf $p)) }
    }
    $f = @($recs | Where-Object {
            ((-not $anF) -or $_.An -eq $anF) -and ((-not $vonF) -or $_.Von -eq $vonF) -and
            ($prF.Count -eq 0 -or $prF -contains $_.Prio) -and
            ($IdVon -le 0 -or $_.Num -ge $IdVon) -and ($IdBis -le 0 -or $_.Num -le $IdBis) -and
            ((-not $Seit) -or $_.Erstellt -ge $Seit) -and ((-not $Bis) -or $_.Erstellt -le $Bis)
        } | Sort-Object Num, Id)
    $shown = @($f | Where-Object { $want -contains $_.Status })
    $hidden = 0
    if ($want -notcontains 'BLOCKED') { $hidden = @($f | Where-Object { $_.Status -eq 'BLOCKED' }).Count }
    foreach ($r in $shown) {
        Add-Msg ('{0}  {1} -> {2}  {3,-8}  {4,-11}  {5}' -f $r.Id, $r.Von, $r.An, $r.Prio, $r.Status, $r.Titel)
        if (($MitDateien -or $Gruppen) -and $r.Dateien) {
            $d = $r.Dateien; if ($d.Length -gt 300) { $d = $d.Substring(0, 300) + ' ...' }
            Add-Msg ('    Dateien: ' + $d)
        }
        if ($Gruppen -and $r.Status -eq 'BLOCKED' -and $r.Frage) { Add-Msg ('    Offene Frage: ' + $r.Frage) }
    }
    if ($shown.Count -eq 0) { Add-Msg 'Keine passenden Auftraege.' }
    if ($Gruppen -and $shown.Count -gt 1) { Add-GruppenVorschlag @($shown | Where-Object { $_.Status -ne 'BLOCKED' }) }
    if ($hidden -gt 0) { Add-Msg "$hidden BLOCKED ausgeblendet (-Status BLOCKED oder -Status Alle)" }
    $script:Daten = @($shown | ForEach-Object {
            $e = [ordered]@{ id = $_.Id; von = $_.Von; an = $_.An; prio = $_.Prio; status = $_.Status; titel = $_.Titel; erstellt = $_.Erstellt; datei = ('tasks/{0}/{1}' -f $_.Folder, $_.File) }
            if ($MitDateien) { $e['betroffeneDateien'] = $_.Dateien }
            $e
        })
    foreach ($r in $shown) { [void]$script:Ids.Add($r.Id) }
    return 0
}

# ---------------------------------------------------------------- Befehl: check
function Get-CheckFindings {
    $fin = New-Object System.Collections.Generic.List[object]
    $add = { param($art, $code, $datei, $text, $fix) [void]$fin.Add([pscustomobject]@{ Art = $art; Code = $code; Datei = $datei; Text = $text; Fix = $fix }) }
    $seenNum = @{}
    $okFolders = @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')
    if (Test-Path -LiteralPath $script:TasksDir) {
        foreach ($d in Get-ChildItem -LiteralPath $script:TasksDir -Force) {
            if (-not $d.PSIsContainer) { & $add 'Fehler' 'fremd' $d.Name 'Datei direkt unter tasks/' $false; continue }
            if ($okFolders -notcontains $d.Name) { & $add 'Fehler' 'fremd' $d.Name 'unbekannter Ordner unter tasks/' $false; continue }
            foreach ($x in Get-ChildItem -LiteralPath $d.FullName -Force) {
                $rel = $d.Name + '/' + $x.Name
                if ($x.PSIsContainer) { & $add 'Fehler' 'fremd' $rel 'Unterordner' $false; continue }
                if ($x.Name -match '^\..*\.tmp-\d+$') { & $add 'Fehler' 'tmp' $rel 'verwaiste Temp-Datei' $true; continue }
                if ($x.Name -notmatch '^SFLIX-\d{8}-(\d{3,})\.md$') { & $add 'Fehler' 'fremd' $rel 'Datei ohne Namensmuster' $false; continue }
                $num = [int]$Matches[1]
                if ($seenNum.ContainsKey($num)) { & $add 'Fehler' 'doppelt' $rel ("Nummer {0:000} doppelt (auch {1}); aeltere behalten, dem Benutzer melden" -f $num, $seenNum[$num]) $false }
                else { $seenNum[$num] = $rel }
                if ($x.Length -eq 0) { & $add 'Fehler' 'unvollstaendig' $rel 'leere Datei (unvollstaendig angelegt); nichts wird geloescht oder ergaenzt' $false; continue }
                try { $t = Load-Task $x.FullName } catch { & $add 'Fehler' 'unlesbar' $rel $_.Exception.Message $false; continue }
                if ($t.Problems.Count -gt 0) { & $add 'Fehler' 'mehrdeutig' $rel ($t.Problems -join '; ') $false; continue }
                if ($t.Bom) { & $add 'Fehler' 'bom' $rel 'UTF-8-BOM' $true }
                foreach ($g in ($t.Fields | Group-Object Canon)) { if ($g.Count -gt 1 -and ($CoreDup -notcontains $g.Name)) { $gl = if ($LastWins -contains $g.Name) { 'letztes gilt' } else { 'erstes gilt' }; & $add 'Hinweis' 'mehrfach' $rel ("Feld mehrfach (Altformat, $gl): $($g.Name)") $false } }
                foreach ($req in @($FN.Erstellt, $FN.Absender, $FN.Empfaenger, $FN.Prio, $FN.Status)) {
                    if ($null -eq (Get-Fld $t $req)) { & $add 'Fehler' 'pflichtfeld' $rel "Feld fehlt: $req" $false }
                }
                foreach ($req in @($FN.Aufgabe, $FN.Ergebnis, $FN.Verlauf, $FN.Uebernahme)) {
                    if ($null -eq (Get-Fld $t $req)) { & $add 'Hinweis' 'pflichtfeld' $rel "Feld fehlt (Altformat): $req" $false }
                }
                $st = Get-StatusWort $t
                if ($st) {
                    $exp = Get-TargetFolder $st
                    if ($exp -ne $d.Name) { & $add 'Fehler' 'ordner' $rel "Status $st gehoert nach $exp" $true }
                    if (($st -eq 'COMPLETED' -or $st -eq 'CANCELLED') -and -not $x.IsReadOnly) { & $add 'Fehler' 'schreibschutz' $rel 'abgeschlossen, aber nicht schreibgeschuetzt' $true }
                }
                if (-not (Get-RoleName (Get-Val $t $FN.Empfaenger)) -or (Get-RoleName (Get-Val $t $FN.Empfaenger)) -eq 'Benutzer') { & $add 'Fehler' 'empfaenger' $rel ('Empfaenger nicht zuordenbar: ' + (Get-Val $t $FN.Empfaenger)) $false }
                elseif (-not (Get-RoleName (Get-Val $t $FN.Absender))) { & $add 'Hinweis' 'absender' $rel ('Absender nicht zuordenbar: ' + (Get-Val $t $FN.Absender)) $false }
            }
        }
    }
    foreach ($dir in @($script:Root, (Join-Path $script:Root 'inbox'))) {
        if (Test-Path -LiteralPath $dir) {
            foreach ($x in Get-ChildItem -LiteralPath $dir -File -Force) {
                if ($x.Name -match '^\..*\.tmp-\d+$') { & $add 'Fehler' 'tmp' $x.FullName.Substring($script:Root.Length + 1) 'verwaiste Temp-Datei' $true }
            }
        }
    }
    # Index und Posteingaenge gegen die Erzeugung aus den Task-Dateien
    $before = $script:Warn.Count
    $recs = Get-AllRecords @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')
    foreach ($v in (Get-ViewTargets $recs)) {
        if ($null -eq $v.Existing) { & $add 'Fehler' 'uebersicht' $v.Name 'Datei fehlt (wird erzeugt)' $true; continue }
        if (-not (Test-ViewGleich $v)) {
            $exIds = @([regex]::Matches($v.Existing, 'SFLIX-\d{8}-\d{3,}') | ForEach-Object { $_.Value } | Sort-Object -Unique)
            $newIds = @(($v.Text -split "`n") | Where-Object { $_ -match '^\|\s*SFLIX-' } | ForEach-Object { ([regex]::Match($_, 'SFLIX-\d{8}-\d{3,}')).Value } | Sort-Object -Unique)
            $exTab = @(($v.Existing -split "`n") | Where-Object { $_ -match '^\|\s*SFLIX-' } | ForEach-Object { ([regex]::Match($_, 'SFLIX-\d{8}-\d{3,}')).Value })
            $miss = @($newIds | Where-Object { $exTab -notcontains $_ -and $v.Name -ne 'x' })
            $extra = @($exTab | Where-Object { $newIds -notcontains $_ })
            $d = ''
            if ($miss.Count) { $d += ' fehlt: ' + ($miss -join ', ') + ';' }
            if ($extra.Count) { $d += ' ueberzaehlig: ' + ($extra -join ', ') + ';' }
            if (-not $d) {
                $exRows = @(($v.Existing -split "`n") | Where-Object { $_ -match '^\|\s*SFLIX-' } | ForEach-Object { $_.TrimEnd("`r") })
                $newRows = @(($v.Text -split "`n") | Where-Object { $_ -match '^\|\s*SFLIX-' } | ForEach-Object { $_.TrimEnd("`r") })
                $sameSet = (@($exRows | Where-Object { $newRows -notcontains $_ }).Count -eq 0 -and @($newRows | Where-Object { $exRows -notcontains $_ }).Count -eq 0)
                if ($sameSet -and ($exRows -join "`n") -ne ($newRows -join "`n")) { $d = ' Zeilenreihenfolge weicht ab (Soll: nach Nummer sortiert)' }
                elseif (-not $sameSet) { $d = ' Zeileninhalt weicht ab (Kurzbeschreibung, Von/An, Prio, Status oder Datei von Hand geaendert)' }
                else { $d = ' Kopf, Zaehler, Leerzeilen oder Zeilenenden weichen ab' }
            }
            & $add 'Fehler' 'uebersicht' $v.Name ('weicht von der Erzeugung ab:' + $d) $true
        }
    }
    if (Test-Path -LiteralPath $script:HistDir) {
        $sz = (Get-ChildItem -LiteralPath $script:HistDir -Recurse -File -Force | Measure-Object Length -Sum).Sum
        if ($sz -gt 50MB) { & $add 'Hinweis' 'history' '.history' ('Groesse {0:N1} MB' -f ($sz / 1MB)) $false }
    }
    return , $fin
}
function Invoke-Check {
    $fin = Get-CheckFindings
    $fixed = @()
    if ($Fix) {
        if (-not $script:RoleN) { Stop-Auftrag 1 '-Rolle ist fuer check -Fix Pflicht' }
        $todo = @($fin | Where-Object { $_.Fix })
        if ($todo.Count -gt 0) {
            if (-not $WhatIf) { Enter-Lock; $fin = Get-CheckFindings; $todo = @($fin | Where-Object { $_.Fix }) }
            foreach ($f in $todo) {
                if ($f.Code -eq 'uebersicht') { continue }
                $full = if ($f.Code -eq 'tmp' -and $f.Datei -match '^(OPEN|IN_PROGRESS|BLOCKED|COMPLETED)/') { Join-Path $script:TasksDir $f.Datei } else { Join-Path $(if ($f.Code -eq 'tmp') { $script:Root } else { $script:TasksDir }) $f.Datei }
                if (-not ($WhatIf) -and -not (Test-Path -LiteralPath $full)) { continue }
                if ($f.Code -eq 'tmp') {
                    if ($WhatIf) { Add-Plan "Fix tmp: $($f.Datei) -> .history/_verwaist"; continue }
                    $dest = Move-Verwaist $full 'check'
                    Add-Audit 'check-fix' '-' ("tmp -> $dest") 'ok'; $fixed += "tmp: $($f.Datei)"
                }
                elseif ($f.Code -eq 'ordner') {
                    if ($WhatIf) { Add-Plan "Fix ordner: $($f.Datei)"; continue }
                    $t = Load-Task $full
                    $st = Get-StatusWort $t
                    Move-Task $t (Get-TargetFolder $st)
                    if ($st -eq 'COMPLETED' -or $st -eq 'CANCELLED') { Set-Schreibschutz $t.Path $true }
                    Add-Audit 'check-fix' $t.Id ("verschoben nach " + $t.Folder) 'ok'; $fixed += "ordner: $($f.Datei)"
                }
                elseif ($f.Code -eq 'schreibschutz') {
                    if ($WhatIf) { Add-Plan "Fix schreibschutz: $($f.Datei)"; continue }
                    Set-Schreibschutz $full $true
                    Add-Audit 'check-fix' ([IO.Path]::GetFileNameWithoutExtension($full)) 'Schreibschutz gesetzt' 'ok'; $fixed += "schreibschutz: $($f.Datei)"
                }
                elseif ($f.Code -eq 'bom') {
                    if ($WhatIf) { Add-Plan "Fix bom: $($f.Datei)"; continue }
                    $t = Load-Task $full
                    Save-Task $t ($t.Lines -join "`n")
                    Add-Audit 'check-fix' $t.Id 'BOM entfernt' 'ok'; $fixed += "bom: $($f.Datei)"
                }
            }
            if (-not $WhatIf) {
                Update-Views
                Add-Audit 'check-fix' '-' 'Index und Posteingaenge erzeugt' 'ok'
                $fin = Get-CheckFindings
            }
        }
    }
    $err = @($fin | Where-Object { $_.Art -eq 'Fehler' })
    $hin = @($fin | Where-Object { $_.Art -eq 'Hinweis' })
    foreach ($f in $err) { Add-Msg ("FEHLER   [{0}] {1}: {2}{3}" -f $f.Code, $f.Datei, $f.Text, $(if ($f.Fix) { ' (mit -Fix behebbar)' } else { '' })) }
    if ($Details) { foreach ($f in $hin) { Add-Msg ("Hinweis  [{0}] {1}: {2}" -f $f.Code, $f.Datei, $f.Text) } }
    elseif ($hin.Count -gt 0) {
        # Hinweise (Altformat, nur Information) zusammengefasst, um Ausgabe und Token zu sparen; Einzelheiten mit -Details oder -Json
        foreach ($g in ($hin | Group-Object Code | Sort-Object Name)) { Add-Msg ("Hinweise [{0}]: {1}" -f $g.Name, $g.Count) }
        Add-Msg 'Hinweise sind Altformat-Meldungen ohne Handlungsbedarf; Einzelheiten: check -Details'
    }
    foreach ($x in $fixed) { Add-Msg "Repariert: $x" }
    $total = @(Get-TaskFiles @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED')).Count
    Add-Msg ("check: {0} Task-Dateien, {1} Fehler, {2} Hinweise" -f $total, $err.Count, $hin.Count)
    $script:Daten = [ordered]@{ dateien = $total; fehler = @($err | ForEach-Object { [ordered]@{ code = $_.Code; datei = $_.Datei; text = $_.Text; fix = $_.Fix } }); hinweise = @($hin | ForEach-Object { [ordered]@{ code = $_.Code; datei = $_.Datei; text = $_.Text } }); repariert = $fixed }
    if ($err.Count -gt 0) { return 5 }
    return 0
}

# ---------------------------------------------------------------- Testbefehle (nur Testmodus)
function Invoke-TestRundlauf {
    $bad = 0; $n = 0
    foreach ($p in (Get-TaskFiles @('OPEN', 'IN_PROGRESS', 'BLOCKED', 'COMPLETED'))) {
        $b = Read-Bytes $p
        $fi = Read-FileInfo $p
        $t = Convert-ToTask $fi.Text $null
        $out = $script:Utf8.GetBytes(($t.Lines -join "`n"))
        $off = 0; if ($fi.Bom) { $off = 3 }
        $same = ($out.Length -eq ($b.Length - $off))
        if ($same) { for ($i = 0; $i -lt $out.Length; $i++) { if ($out[$i] -ne $b[$i + $off]) { $same = $false; break } } }
        $n++
        if (-not $same) { $bad++; Add-Msg ("ABWEICHUNG: " + (Split-Path -Leaf $p)) }
    }
    Add-Msg "rundlauf: $n Dateien, $bad Abweichungen"
    if ($bad -gt 0) { return 5 }
    return 0
}

# ---------------------------------------------------------------- Hilfe (-Help, -Hilfe, Befehl hilfe)
$script:HilfeText = @{
    '' = @'
Auftrag.ps1 - Auftragsverwaltung fuer coordination/ (Bedienung und Beispiele: coordination/README.md Abschnitt 13)
Aufruf:  powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag.ps1 <Befehl> [Parameter]
Hilfe zu einem Befehl:  ... SFLIX_Tools\Auftrag.ps1 <Befehl> -Help

Befehle:
  erstellen     neuen Auftrag anlegen (Absender)
  erledigt      eigene Arbeit in einem Aufruf erfassen: anlegen, uebernehmen, abschliessen
  status        Statuswechsel IN_PROGRESS | BLOCKED | CANCELLED
  abschliessen  IN_PROGRESS -> COMPLETED (Empfaenger)
  aendern       Feld setzen oder anhaengen, Abschnitt anhaengen
  liste         Auftraege anzeigen und filtern (nur lesend), -Gruppen schlaegt Gruppen vor
  check         Konsistenz pruefen (nur lesend), -Fix repariert sicher

Gemeinsame Parameter:
  -Rolle <R>         eigene Rolle: Planung, Architekt, Entwicklung, Pruefung, Release, Benutzer
                     (Pflicht bei erstellen, erledigt, status, abschliessen, aendern, check -Fix)
  -Id <ID|Nummer>    z. B. SFLIX-20261120-001 oder 1
  -Eingabe <Datei>   Werte aus UTF-8-Datei im Format "- Schluessel: Wert", Folgezeilen 2 Leerzeichen eingerueckt;
                     Datei im Scratchpad, nie unter coordination/. Parameter haben Vorrang.
  -WhatIf            zeigt Plan, Inhalt und Warnungen, schreibt nichts
  -Json              Ausgabe als JSON (nur fuer maschinelle Auswertung)
  -Benutzerfreigabe <Grund>
                     protokollierte Ausnahme von der Rollenpruefung (Exit 2), nur mit ausdruecklicher
                     Erlaubnis des Benutzers im Chat. Nicht der Freigabe-Vermerk (dafuer -Freigabe bzw. aendern).
  -Help, -Hilfe      diese Hilfe

Nach Exit 0 nichts nachlesen (Task-Datei, Index, Posteingang); WARNUNG-Zeilen beachten.
Exit-Codes: 0 ok | 1 Eingabe/Pflichtfeld: korrigieren | 2 Berechtigung: nicht umgehen | 3 Zustandskonflikt
  oder moegliche Dublette: Stand pruefen | 4 Sperre: wiederholen | 5 check fand Fehler: check -Fix | 6 E/A-Fehler (bei "ist geschrieben,
  aber ...": nicht wiederholen, check -Fix)
'@
    'erstellen' = @'
erstellen -Rolle <R> -An <Rolle> -Titel "..." -Prio LOW|MEDIUM|HIGH|CRITICAL -Aufgabe "..." -Ergebnis "..."
  Pflicht:   -An, -Titel, -Prio, -Aufgabe, -Ergebnis; -Begruendung bei HIGH und CRITICAL
  Optional:  -Uebergabegrund, -Kontext (Technischer Kontext), -Dateien (Betroffene Dateien), -Abhaengigkeiten,
             -UX (UX-Beteiligung), -Freigabe (Vermerk Benutzerfreigabe), -Folgeschritt, -Trotzdem (Dublette bewusst)
  Das Skript vergibt die ID, setzt Erstellt am, Absender (= -Rolle), Status OPEN, Verlauf und erzeugt Index und
  Posteingaenge. Dublette: aehnlicher Titel mit mindestens zwei gemeinsamen Woertern -> Exit 3, nichts angelegt
  (bestehenden Auftrag mit aendern ergaenzen oder bewusst -Trotzdem). Nur Warnung: aehnlicher kurzer Titel,
  gleiche Datei in aktivem Auftrag, Empfaenger = eigene Rolle (dafuer erledigt), Auftrag an die Planung.
  Unter -Abhaengigkeiten genannte Vorgaenger sind von der Dublettenpruefung ausgenommen.
  Folgeauftrag vollstaendig formulieren: noetige Erkenntnisse in -Kontext, damit der Empfaenger die
  Vorgaenger-Datei nicht lesen muss. -Dateien mit Pfaden ab der Projektwurzel (fuer liste -Gruppen).
  -Eingabe-Schluessel: An, Titel, Prio, Aufgabe, Ergebnis, Begruendung, Uebergabegrund, Kontext, Dateien,
  Abhaengigkeiten, UX, Freigabe, Folgeschritt (oder die Feldnamen der Vorlage). Unbekannte Schluessel,
  #-Ueberschriften und Text vor dem ersten Feld: Exit 1. Abschnitte danach mit aendern -Abschnitt.
Beispiel:
  ... erstellen -Rolle Entwicklung -An Pruefung -Prio MEDIUM -Titel "Pruefung Download (aus -012)" -Aufgabe "Download pruefen" -Ergebnis "Pruefbericht" -Dateien "SFLIX_Tools/SflixTools/src/DownloadTask.cs" -Abhaengigkeiten "SFLIX-20261120-012"
  ... erstellen -Rolle Architekt -Eingabe C:\...\scratchpad\auftrag.md
'@
    'erledigt' = @'
erledigt -Rolle <R> -Titel "..." -Aufgabe "..." -Arbeiten "..." -Tests "..." -Entscheide "DEC-...|keine" [-Prio ...] [-Dateien "..."]
  Fuer eigene Arbeit, die keine andere Rolle braucht (auch kleine Aenderungen): legt den Auftrag an die eigene Rolle an,
  setzt IN_PROGRESS und schliesst ihn ab, alles unter einer Sperre. Jede Aufgabe bekommt so ein Ticket.
  Pflicht:   -Titel, -Aufgabe, -Arbeiten, -Tests, -Entscheide; -Prio Standard LOW, -Ergebnis optional.
  Optional:  wie erstellen (-Kontext, -Dateien, -Abhaengigkeiten, -Freigabe) und -Risiken, -Folgeschritt, -Notiz.
  -An darf nur die eigene Rolle sein. Dublettenpruefung wie bei erstellen.
Beispiel:
  ... erledigt -Rolle Entwicklung -Titel "Tippfehler in der Hilfe" -Aufgabe "Hilfe Abschnitt Player korrigieren" -Dateien "start.html" -Arbeiten "start.html: Tippfehler korrigiert" -Tests "Vorschau-Server: Hilfe geprueft" -Entscheide keine
'@
    'status' = @'
status -Rolle <R> -Id <ID> -Neu IN_PROGRESS|BLOCKED|CANCELLED [-Notiz "..."] [-Frage "..."]
  IN_PROGRESS  Uebernahme (setzt Uebernahmebestaetigung) oder Rueckkehr aus BLOCKED; nur Empfaenger
  BLOCKED      -Frage Pflicht (wird unter Offene Fragen eingetragen); nur Empfaenger
  CANCELLED    -Notiz Pflicht (Begruendung); nur Absender oder Benutzer
  Zulaessig: OPEN -> IN_PROGRESS|BLOCKED|CANCELLED, IN_PROGRESS -> BLOCKED|CANCELLED,
             BLOCKED -> IN_PROGRESS|CANCELLED. COMPLETED nur ueber abschliessen bzw. erledigt.
  -Notiz erscheint in der Verlaufszeile. -Eingabe-Schluessel: Neu, Notiz, Frage
Beispiele:
  ... status -Rolle Pruefung -Id 12 -Neu IN_PROGRESS
  ... status -Rolle Pruefung -Id 12 -Neu BLOCKED -Frage "Welche App-Version ist installiert?"
  ... status -Rolle Entwicklung -Id 12 -Neu CANCELLED -Notiz "Dublette von SFLIX-20261120-010"
'@
    'abschliessen' = @'
abschliessen -Rolle <R> -Id <ID> -Arbeiten "..." -Tests "..." -Entscheide "DEC-...|keine" [-Risiken "..."] [-Folgeschritt "..."] [-Notiz "..."]
  Nur aus IN_PROGRESS, nur Empfaenger. Die Texte werden an Durchgefuehrte Arbeiten, Tests und Ergebnisse,
  Entscheidungen, Verbleibende Risiken und Folgeschritt angehaengt; Arbeiten, Tests und Entscheidungen duerfen
  danach nicht leer sein. -Entscheide nennt die in docs/DECISIONS.md festgehaltenen Entscheide (DEC-IDs, mit
  SFLIX_Tools\Doku\decisions.py neu erfasst) oder "keine".
  Setzt Status COMPLETED und Abgeschlossen am, verschiebt nach tasks/COMPLETED/, setzt den Schreibschutz.
  Folgeauftraege vorher erstellen und unter -Folgeschritt nennen. Danach gesperrt (aendern nur mit -Erzwingen).
  -Eingabe-Schluessel: Arbeiten, Tests, Entscheide, Risiken, Folgeschritt, Notiz
Beispiel:
  ... abschliessen -Rolle Pruefung -Id 12 -Arbeiten "Download geprueft" -Tests "Testumgebung: 3 Faelle bestanden; Emulator nicht geprueft: kein Bezug" -Entscheide keine -Folgeschritt "SFLIX-20261120-013 an Entwicklung"
'@
    'aendern' = @'
aendern -Rolle <R> -Id <ID> -Feld "<Feldname>" -Wert "..." [-Anhaengen] [-Notiz "..."] [-Erzwingen]
aendern -Rolle <R> -Id <ID> -Feld "<Feldname>" -Eingabe <Datei>     (Wert des Felds in der Datei, sonst ganzer Inhalt)
aendern -Rolle <R> -Id <ID> -Abschnitt "<Titel>" -Eingabe <Datei>   (ganzer Inhalt als "## <Titel>" ans Ende)
  Ersetzt den Feldwert; fehlt das Feld, wird es eingefuegt. Immer angehaengt: Bearbeitungsverlauf,
  Durchgefuehrte Arbeiten, Tests und Ergebnisse, Offene Fragen; -Anhaengen haengt auch an andere Felder an.
  Nicht aenderbar: ID/Titel, Status, Absender, Empfaenger, Erstellt am, Uebernahmebestaetigung, Abgeschlossen am.
  COMPLETED/CANCELLED sind gesperrt (Exit 3 "bereits abgeschlossen"); neue Arbeit als Folgeauftrag.
  Nur ausnahmsweise: -Erzwingen -Notiz "<Grund>" erlaubt Anhaengen und -Abschnitt (Verlauf: [erzwungen: Grund]),
  nie Ersetzen. Jede Aenderung schreibt eine Verlaufszeile;
  -Feld Bearbeitungsverlauf -Wert "x" ergibt "<Datum> <Rolle>: x". Feldnamen mit Umlaut oder ae/oe/ue;
  Kurznamen: Aufgabe, Ergebnis, Dateien, Kontext, UX, Freigabe, Prio, Arbeiten, Risiken, Fragen, Verlauf, Entscheide.
Beispiele:
  ... aendern -Rolle Architekt -Id 5 -Feld Bearbeitungsverlauf -Wert "Konzept erstellt"
  ... aendern -Rolle Architekt -Id 5 -Feld Benutzerfreigabe -Wert "erteilt 2026-11-20, Variante B, Chat Architekt"
  ... aendern -Rolle Entwicklung -Id 12 -Feld "Offene Fragen" -Wert "2026-11-20 Antwort: 4.0.6"
  ... aendern -Rolle Architekt -Id 5 -Abschnitt "Konzept" -Eingabe C:\...\scratchpad\konzept.md
'@
    'liste' = @'
liste [-Rolle <R>] [-An <Rolle>] [-Von <Rolle>] [-Status <S>[,<S>...]|Alle] [-Prio <P>[,<P>]] [-Text "..."]
      [-IdVon <n>] [-IdBis <n>] [-Seit yyyy-MM-dd] [-Bis yyyy-MM-dd] [-MitDateien] [-Gruppen]
  Mit -Rolle (ohne -An): eigener Posteingang, Standard-Status OPEN,IN_PROGRESS,BLOCKED.
  Ohne -Rolle, mit -Rolle Benutzer oder mit -An: alle bzw. die genannte Rolle, Standard-Status OPEN,IN_PROGRESS
  (BLOCKED wird dann ausgeblendet und gezaehlt). Alle = auch COMPLETED/CANCELLED.
  -Text sucht im ganzen Auftrag (z. B. Dateiname). -MitDateien zeigt je Auftrag zusaetzlich die Betroffenen
  Dateien (zum Gruppieren, ohne Task-Dateien zu lesen). -Gruppen (schliesst -MitDateien ein) schlaegt Gruppen vor:
  Auftraege mit gemeinsamen Dateien in einem Durchgang, Einzelne im gleichen Ordner als "passt dazu", Reihenfolge
  nach hoechster Prioritaet; BLOCKED mit der letzten offenen Frage, nicht gruppiert.
  Ausgabe je Zeile: ID  Von -> An  Prio  Status  Titel
Beispiele:
  ... liste -Rolle Pruefung -Gruppen                          (eigener Posteingang beim Rollenstart)
  ... liste -Status BLOCKED                                  (alle blockierten Auftraege)
  ... liste -Text "player.js"                                (laufende Arbeit an einer Datei)
'@
    'check' = @'
check [-Fix -Rolle <R>] [-WhatIf] [-Details] [-Json]
  Prueft: Ordner passend zum Status, Schreibschutz COMPLETED/CANCELLED, BOM, Temp-Dateien, Pflichtfelder,
  doppelte Nummern, Namensmuster, Index und Posteingaenge gegen die Erzeugung. Exit 5 bei Fehlern.
  Fehler werden einzeln gemeldet, Hinweise (Altformat, ohne Handlungsbedarf) nur gezaehlt; einzeln mit -Details.
  -Fix behebt: Ordner, Schreibschutz, BOM, verwaiste Temp-Dateien, Index und Posteingaenge.
  Nur gemeldet (dem Benutzer vorlegen): doppelte Nummer, leere oder mehrdeutige Datei, unbekannter Empfaenger,
  fehlendes Pflichtfeld, fremde Datei unter tasks/.
'@
}
function Show-Hilfe([string]$cmd) {
    $k = ''
    if ($cmd) { $k = $cmd.ToLowerInvariant() }
    if (@('hilfe', 'help') -contains $k) { $k = '' }
    if (-not $script:HilfeText.ContainsKey($k)) { Stop-Auftrag 1 "Unbekannter Befehl: $cmd (Uebersicht: -Help)" }
    foreach ($l in ($script:HilfeText[$k] -split "`r?`n")) { Add-Msg $l }
    return 0
}

# ---------------------------------------------------------------- Hauptablauf
function Invoke-Main {
    if ($Help -or ($Befehl -and @('hilfe', 'help', '-?', '/?') -contains $Befehl.ToLowerInvariant())) { return (Show-Hilfe $Befehl) }
    $script:RoleN = $null
    if ($Rolle) {
        $script:RoleN = Get-RoleName $Rolle
        if (-not $script:RoleN) { Stop-Auftrag 1 "Ungueltige -Rolle: $Rolle (erlaubt: $($Roles -join ', '), Benutzer)" }
        $Rolle = $script:RoleN
    }
    Read-Eingabe
    if (-not $Befehl) { Stop-Auftrag 1 'Befehl fehlt: erstellen | erledigt | status | aendern | abschliessen | liste | check (Hilfe: -Help)' }
    $b = $Befehl.ToLowerInvariant()
    if ($b.StartsWith('_') -and -not $script:Testmodus) { Stop-Auftrag 1 'Testbefehle nur im Testmodus' }
    if (-not (Test-Path -LiteralPath $script:Root)) { Stop-Auftrag 6 "coordination-Ordner nicht gefunden: $script:Root" }
    switch ($b) {
        'erstellen' { return (Invoke-Erstellen) }
        'status' { return (Invoke-Status) }
        'aendern' { return (Invoke-Aendern) }
        'abschliessen' { return (Invoke-Abschliessen) }
        'erledigt' { return (Invoke-Erledigt) }
        'liste' { return (Invoke-Liste) }
        'check' { return (Invoke-Check) }
        '_rundlauf' { return (Invoke-TestRundlauf) }
        '_pfad' { Assert-InRoot $Wert; Add-Msg 'pfad ok'; return 0 }
        default { Stop-Auftrag 1 "Unbekannter Befehl: $Befehl (Hilfe: -Help)" }
    }
}

$exitCode = 0
try {
    try { [Console]::OutputEncoding = $script:Utf8 } catch { }
    $exitCode = Invoke-Main
}
catch {
    $c = $null
    try { $c = $_.Exception.Data['code'] } catch { }
    if ($null -ne $c) { $exitCode = [int]$c; $script:Fehler = $_.Exception.Message }
    else { $exitCode = 6; $script:Fehler = 'Unerwarteter Fehler: ' + $_.Exception.Message + ' | ' + $_.InvocationInfo.PositionMessage + ' | ' + $_.ScriptStackTrace }
}
finally { Exit-Lock }

if ($Json) {
    $fe = @(); if ($script:Fehler) { $fe = @($script:Fehler) }
    $o = [ordered]@{
        ok = ($exitCode -eq 0); exit = $exitCode; befehl = $Befehl; ids = @($script:Ids); meldungen = @($script:Msg)
        warnungen = @($script:Warn); plan = @($script:Plan); fehler = $fe; daten = $script:Daten
    }
    Write-Output (ConvertTo-Json $o -Depth 6)
}
else {
    foreach ($m in $script:Msg) { Write-Output $m }
    foreach ($p in $script:Plan) { Write-Output ('WHATIF: ' + $p) }
    foreach ($w in $script:Warn) { Write-Output ('WARNUNG: ' + $w) }
    if ($script:Fehler) { Write-Output ('FEHLER: ' + $script:Fehler) }
}
exit $exitCode
