# Auftrag-guard.ps1 - PreToolUse-Hook fuer .claude/settings.json (STEFFLIX, uebernommen aus Sync2PC, DEC-329 ff.)
#
# 1. PowerShell-Alias-Falle (CLAUDE.md, oberste Regel): Definiert ein PowerShell-Befehl oder eine geschriebene .ps1/.psm1-Datei
#    eine Funktion (function/filter), deren Name ein Alias oder Cmdlet ist -> Exit 2 (gesperrt). Aliase haben Vorrang vor
#    Funktionen; eine Funktion namens rd und danach "rd $p" ruft Remove-Item auf (Datenverlust).
#    Dazu: Array-Abflachung @(@('a','b')) mit genau einem Paar in schreibenden Befehlen -> Exit 2.
# 2. coordination/ (tasks, inbox, .history, TASK-INDEX.md, .audit.log, .lock): Edit/Write -> Exit 2; Shell-Befehle mit
#    Schreibverb auf diese Pfade (ausser SFLIX_Tools\Auftrag.ps1 und Selbsttest) -> Rueckfrage.
# 3. Zugriffsorte (CLAUDE.md "Zugriff nur im Projekt"):
#    - Netzlaufwerke (jedes Laufwerk ausser C:) und UNC-Pfade (\\Server\...; ausser \\127.0.0.1 und \\localhost) -> Exit 2,
#      bei allen Werkzeugen, auch nur lesend.
#    - Edit/Write/MultiEdit/NotebookEdit ausserhalb der Schreiborte -> Exit 2.
#    - Read/Glob/Grep ausserhalb der Leseorte -> Exit 2.
#    - PowerShell/Bash: absolute Pfade im Befehl (Umgebungsvariablen aufgeloest) und Arbeitsverzeichnis ausserhalb
#      -> Rueckfrage (heuristisch). Programme (.exe/.bat/.cmd/.com/.dll) zaehlen nicht als Ziel.
# 4. STEFFLIX.exe / updateStefflix.exe nur mit SFLIX_DIR auf .claude\testenv starten -> sonst Exit 2.
# Ein Fehler im Waechter blockiert nie (Exit 0). Grenze: haelt Versehen auf, keine absichtliche Umgehung.
# Reines ASCII (PowerShell 5.1). Selbsttest: SFLIX_Tools\Auftrag-guard-Selbsttest.ps1

$ErrorActionPreference = 'Stop'
try {
    $raw = [Console]::In.ReadToEnd()
    if (-not $raw -or -not $raw.Trim()) { exit 0 }
    $j = $raw | ConvertFrom-Json
}
catch { exit 0 }

$Meldung = 'Gesperrt: coordination/ (Auftraege, Index, Posteingaenge, .history, .audit.log, .lock) nur ueber SFLIX_Tools\Auftrag.ps1 aendern (coordination/README.md Abschnitt 13). Lesen: Auftrag.ps1 liste.'

function Get-AliasKonflikt([string]$code) {
    $res = New-Object System.Collections.Generic.List[string]
    if (-not $code -or $code -notmatch '(?i)\b(function|filter)\s') { return , $res }
    foreach ($m in [regex]::Matches($code, '(?i)(?<![\w$.-])(?:function|filter)\s+(?:global:|script:|local:|private:)?([A-Za-z][\w-]*)')) {
        $n = $m.Groups[1].Value
        $a = Get-Alias -Name $n -ErrorAction SilentlyContinue
        if ($a) { [void]$res.Add("$n (Alias fuer $($a.Definition))"); continue }
        if (Get-Command -Name $n -CommandType Cmdlet -ErrorAction SilentlyContinue) { [void]$res.Add("$n (Cmdlet)") }
    }
    return , $res
}
function Remove-AnfuehrungsText([string]$code) {
    return [regex]::Replace($code, '(?s)@''.*?''@|@".*?"@|''[^'']*''|"(?:[^"`]|`.)*"', "''")
}
function Test-AusfuehrtText([string]$cmd) {
    return [bool]($cmd -match '(?i)(\s-c(ommand)?\s|\s-enc(odedcommand)?\s|\biex\b|Invoke-Expression|\[scriptblock\]|::Create\(|Add-Type\b)')
}
function Stop-Guard([string]$text) {
    [Console]::Error.WriteLine($text)
    exit 2
}
function Stop-AliasKonflikt($treffer) {
    Stop-Guard ('Abgelehnt (PowerShell-Alias-Falle, CLAUDE.md): Funktionsname belegt: ' + (($treffer | Select-Object -Unique) -join ', ') +
        '. Aliase haben Vorrang vor Funktionen, ein Aufruf fuehrt das Cmdlet aus (z. B. rd = Remove-Item, Datenverlust). ' +
        'Eindeutigen Verb-Nomen-Namen waehlen (z. B. Read-FileText, Set-LsText) oder einen Skriptblock in einer Variablen nutzen ($teil = { ... }; & $teil).')
}
function Send-GuardFrage([string]$grund) {
    $o = [ordered]@{ hookSpecificOutput = [ordered]@{
            hookEventName            = 'PreToolUse'
            permissionDecision       = 'ask'
            permissionDecisionReason = $grund
        }
    }
    Write-Output (ConvertTo-Json $o -Depth 4 -Compress)
    exit 0
}
function Resolve-GuardPfad([string]$p, [string]$cwd) {
    $p = $p.Trim().Trim('"', "'").Replace('/', '\')
    if ($p -match '^\\[A-Za-z]\\') { $p = $p.Substring(1, 1) + ':' + $p.Substring(2) }
    $p = ($p -split '[*?]')[0]
    if (-not $p) { return $null }
    try {
        if ($p.StartsWith('\\')) { return $p.TrimEnd('\') }
        if (-not [IO.Path]::IsPathRooted($p)) { $p = Join-Path $cwd $p }
        return [IO.Path]::GetFullPath($p).TrimEnd('\')
    }
    catch { return $null }
}
function Test-GuardOrt([string]$voll, $liste) {
    foreach ($o in $liste) {
        if ($voll.Equals($o, [StringComparison]::OrdinalIgnoreCase) -or $voll.StartsWith($o + '\', [StringComparison]::OrdinalIgnoreCase)) { return $true }
    }
    return $false
}
# Netzlaufwerk oder fremder Rechner: jedes Laufwerk ausser C: sowie UNC-Pfade (ausser dem lokalen SMB-Testserver)
function Test-NetzPfad([string]$voll) {
    if (-not $voll) { return $false }
    if ($voll -match '^(?i)\\\\(127\.0\.0\.1|localhost|\?|\.)(\\|$)') { return $false }
    if ($voll.StartsWith('\\')) { return $true }
    if ($voll -match '^([A-Za-z]):' -and $Matches[1].ToUpperInvariant() -ne 'C') { return $true }
    return $false
}
$NetzMeldung = 'Gesperrt: Netzlaufwerke und fremde Rechner (CLAUDE.md "Netzlaufwerke"). Zugriff nur, wenn der Benutzer den Ordner ausdruecklich nennt; dann fragt er selbst nach. Nie Freigaben durchsuchen oder auflisten.'

try {
    $tool = [string]$j.tool_name
    $ti = $j.tool_input
    $root = Split-Path -Parent $PSScriptRoot
    $base = [IO.Path]::GetFullPath((Join-Path $root 'coordination')).TrimEnd('\')
    $cwd = [string]$j.cwd; if (-not $cwd) { $cwd = $root }

    # Schreiborte (CLAUDE.md "Zugriff nur im Projekt"; Aenderung nur mit dem Regelwerk)
    $Orte = New-Object System.Collections.Generic.List[string]
    foreach ($o in @(
            $root,                                                                    # Projekt (Rollengrenzen: CLAUDE.md)
            'C:\Users\rbald\AppData\Local\Temp', (Join-Path $env:LOCALAPPDATA 'Temp'), # Temp fest (TMP kann auf einen Unterordner zeigen)
            [IO.Path]::GetTempPath(), $env:TMP, $env:TEMP,                            # Scratchpad
            (Join-Path $env:USERPROFILE '.claude'),                                   # Gedaechtnis, Ausgaben, Plaene von Claude Code
            (Join-Path $env:LOCALAPPDATA 'STEFFLIX'),                                 # ffmpeg, WebView2, Downloads der EXE (Tests)
            (Join-Path $env:LOCALAPPDATA 'STEFFLIX-Android'),                         # JDK, Gradle, Build-Werkzeuge der App
            (Join-Path $env:LOCALAPPDATA 'STEFFLIX-TestShare'),                       # venv und Betrieb des SMB-Testservers
            (Join-Path $env:LOCALAPPDATA 'Android\Sdk'),                              # adb, Emulator, Systemabbilder
            (Join-Path $env:USERPROFILE '.android'))) {                               # AVDs, adb-Schluessel
        if ($o) { try { [void]$Orte.Add([IO.Path]::GetFullPath($o).TrimEnd('\')) } catch {} }
    }
    # Zusaetzlich nur lesend
    $LeseOrte = New-Object System.Collections.Generic.List[string]
    foreach ($o in $Orte) { [void]$LeseOrte.Add($o) }
    foreach ($o in @('C:\Windows\Microsoft.NET', 'C:\Program Files\Android', 'C:\Program Files (x86)\Microsoft\EdgeWebView')) { [void]$LeseOrte.Add($o) }
    $OrtHinweis = 'Erlaubt sind: Projektordner, %TEMP% (Scratchpad), %USERPROFILE%\.claude, %LOCALAPPDATA%\STEFFLIX, \STEFFLIX-Android, \STEFFLIX-TestShare, \Android\Sdk, %USERPROFILE%\.android (CLAUDE.md "Zugriff nur im Projekt"). Einen erlaubten Ort waehlen; nur zulassen, wenn der Auftrag diesen Ort wirklich braucht.'

    # ------------------------------------------------------------ Datei-Werkzeuge (schreibend)
    if (@('Edit', 'Write', 'MultiEdit', 'NotebookEdit') -contains $tool) {
        $p = [string]$ti.file_path
        if (-not $p) { $p = [string]$ti.notebook_path }
        if (-not $p) { exit 0 }
        if ($p -match '(?i)\.psm?1$') {
            $code = [string]$ti.content
            if ($tool -eq 'Edit') { $code = [string]$ti.new_string }
            if ($tool -eq 'MultiEdit' -and $ti.edits) { $code = (@($ti.edits | ForEach-Object { [string]$_.new_string }) -join "`n") }
            $k = Get-AliasKonflikt $code
            if ($k.Count -gt 0) { Stop-AliasKonflikt $k }
        }
        $full = Resolve-GuardPfad $p $cwd
        if (-not $full) { exit 0 }
        if (Test-NetzPfad $full) { Stop-Guard ($NetzMeldung + ' Pfad: ' + $full) }
        if ($full.StartsWith($base + '\', [StringComparison]::OrdinalIgnoreCase)) {
            $rel = $full.Substring($base.Length + 1)
            if ($rel -match '^(?i)(tasks|inbox|\.history)(\\|$)' -or $rel -match '^(?i)(TASK-INDEX\.md|\.audit\.log|\.lock)$') { Stop-Guard $Meldung }
        }
        if (-not (Test-GuardOrt $full $Orte)) { Stop-Guard ('Gesperrt: Schreiben ausserhalb der erlaubten Orte: ' + $full + '. ' + $OrtHinweis) }
        exit 0
    }

    # ------------------------------------------------------------ Datei-Werkzeuge (lesend)
    if (@('Read', 'Glob', 'Grep', 'NotebookRead') -contains $tool) {
        $kand = @()
        foreach ($f in @('file_path', 'path', 'notebook_path')) { $v = [string]$ti.$f; if ($v) { $kand += $v } }
        if ($tool -eq 'Glob') { $pt = [string]$ti.pattern; if ($pt -and ($pt -match '^([A-Za-z]:|\\\\|/[A-Za-z]/)')) { $kand += $pt } }
        foreach ($c in $kand) {
            $full = Resolve-GuardPfad $c $cwd
            if (-not $full) { continue }
            if (Test-NetzPfad $full) { Stop-Guard ($NetzMeldung + ' Pfad: ' + $full) }
            if (-not (Test-GuardOrt $full $LeseOrte)) { Stop-Guard ('Gesperrt: Lesen ausserhalb der erlaubten Orte: ' + $full + '. ' + $OrtHinweis) }
        }
        exit 0
    }

    # ------------------------------------------------------------ Shell
    if (@('PowerShell', 'Bash') -contains $tool) {
        $cmd = [string]$ti.command
        if (-not $cmd) { exit 0 }
        $verben = @(
            '(?i)\b(Set-Content|Add-Content|Out-File|Clear-Content|New-Item|Remove-Item|Move-Item|Copy-Item|Rename-Item|Set-Item|Set-ItemProperty|Clear-Item|Tee-Object|Expand-Archive|Compress-Archive|Export-Csv|Export-Clixml)\b',
            '(?i)(^|[\s;|&(])(sc|ac|ni|ri|rm|rmdir|del|erase|rd|mi|mv|move|cpi|cp|copy|ren|rni|md|mkdir|attrib|icacls|tee|touch|truncate|xcopy|robocopy)(\s|$)',
            '(?i)\[(System\.)?IO\.(File|Directory)\]::(Write\w*|Append\w*|Delete|Move|Copy|Replace|Create\w*|SetAttributes|OpenWrite)',
            '(?i)\.(WriteAllText|WriteAllBytes|WriteAllLines|AppendAllText|MoveTo|CopyTo|Delete)\(',
            '(?i)(\s-OutFile\b|\bsed\s+-i|\bpull\s)',
            '(?<![-0-9=<])>{1,2}(?![&=])'
        )
        $schreibt = $false
        foreach ($v in $verben) { if ($cmd -match $v) { $schreibt = $true; break } }
        $textSkripte = '(?i)(Auftrag(-Selbsttest|-guard-Selbsttest)?\.ps1|decisions\.py|news\.py)'

        # 1. Alias-Falle: bei Aufrufen der Text-Skripte und reinen Lesebefehlen nur Code ausserhalb von Anfuehrungszeichen
        if ($tool -eq 'PowerShell') {
            $pruef = $cmd
            if (($cmd -match $textSkripte -or -not $schreibt) -and -not (Test-AusfuehrtText $cmd)) { $pruef = Remove-AnfuehrungsText $cmd }
            $k = Get-AliasKonflikt $pruef
            if ($k.Count -gt 0) { Stop-AliasKonflikt $k }
        }

        # 4. STEFFLIX.exe nur mit SFLIX_DIR auf der Testumgebung starten
        $exeRx = '(STEFFLIX|updateStefflix)\.exe'
        $startRx = @(
            ('(?i)Start-Process\b[^;|\r\n]*' + $exeRx),
            ('(?i)(^|[;|{(\r\n])\s*&\s*[^;|\r\n]*' + $exeRx),
            ('(?i)(^|[;|{(\r\n])\s*(?:["''][^"''\r\n]*|[.\w:\\/-]*)\\' + $exeRx),
            ('(?i)(^|[;|{(\r\n])\s*\.[\\/]' + $exeRx),
            ('(?i)FileName\s*=\s*[^;\r\n]*' + $exeRx),
            ('(?i)(CreateProcess|Invoke-Item|\bii\b|cmd(\.exe)?\s+/c)[^;|\r\n]*' + $exeRx)
        )
        $startet = $false
        foreach ($s in $startRx) { if ($cmd -match $s) { $startet = $true; break } }
        if ($startet -and $cmd -notmatch '(?i)SFLIX_DIR[^;\r\n]*\.claude[\\/]+testenv') {
            Stop-Guard ('Gesperrt: STEFFLIX.exe/updateStefflix.exe nur mit der Testumgebung starten (CLAUDE.md "Testumgebung"): ' +
                'im selben Befehl $env:SFLIX_DIR = ''C:\Users\rbald\Documents\SFLIX\.claude\testenv'' setzen (Pfad ausgeschrieben). Nie produktive STEFFLIX-Ordner.')
        }

        # 1b. Array-Abflachung (nur schreibende PowerShell-Befehle)
        if ($tool -eq 'PowerShell' -and $schreibt -and $cmd -notmatch $textSkripte -and
            $cmd -match '@\(\s*@?\(\s*(?:''[^'']*''|"[^"]*")\s*,\s*(?:''[^'']*''|"[^"]*")\s*\)\s*\)') {
            Stop-Guard ('Abgelehnt (PowerShell-Array-Abflachung, CLAUDE.md): @(@(''a'',''b'')) mit nur einem Paar ergibt zwei Zeichenketten ' +
                'statt eines Paares; ein Ersetzen ueber diese Liste ersetzt einzelne Zeichen. Paar mit fuehrendem Komma (@(,@(''a'',''b''))) ' +
                'oder besser das Edit-Werkzeug nutzen.')
        }

        # 2. coordination/
        $schutz = '(?i)(coordination[\\/]+(tasks|inbox|\.history)([\\/"''\s]|$)|TASK-INDEX\.md|\.audit\.log|coordination[\\/]+\.lock)'
        if ($schreibt -and $cmd -notmatch '(?i)Auftrag(-Selbsttest)?\.ps1' -and $cmd -match $schutz) {
            Send-GuardFrage ('Befehl schreibt moeglicherweise direkt in coordination/. ' + $Meldung)
        }

        # 3. Pfade: Zuweisungen an Umgebungsvariablen sind kein Ziel; "adb ... shell"-Argumente laufen auf dem Geraet
        $t = [regex]::Replace($cmd, '(?i)\$\{?env:[\w()]+\}?\s*[+]?=(?:"[^"]*"|''[^'']*''|[^;\r\n"''])*', ' ')
        $t = [regex]::Replace($t, '(?i)(?<![\w-])shell\s+(?:"[^"]*"|''[^'']*''|[^;\r\n"''>|])*', ' ')
        $t = [regex]::Replace($t, '(?i)\$\{?env:([A-Za-z_][\w()]*)\}?', [Text.RegularExpressions.MatchEvaluator] {
                param($m) $w = [Environment]::GetEnvironmentVariable($m.Groups[1].Value); if ($w) { $w } else { $m.Value } })
        $t = [regex]::Replace($t, '%([A-Za-z_][\w()]*)%', [Text.RegularExpressions.MatchEvaluator] {
                param($m) $w = [Environment]::GetEnvironmentVariable($m.Groups[1].Value); if ($w) { $w } else { $m.Value } })
        $t = [regex]::Replace($t, '(?i)(?<![\w$])(\$HOME\b|~(?=[\\/]))', $env:USERPROFILE.Replace('$', '$$'))
        $teilPat = '("[^"]*"|''[^'']*''|[^\s;|(){}"'']+)'
        $t = [regex]::Replace($t, '(?i)\bJoin-Path\s+(?:-Path\s+)?' + $teilPat + '\s+(?:-ChildPath\s+)?' + $teilPat, [Text.RegularExpressions.MatchEvaluator] {
                param($m) '"' + $m.Groups[1].Value.Trim('"', "'").TrimEnd('\', '/') + '\' + $m.Groups[2].Value.Trim('"', "'").TrimStart('\', '/') + '"' })
        $t = [regex]::Replace($t, '(?i)("[A-Za-z]:[\\/][^"]*"|''[A-Za-z]:[\\/][^'']*''|(?<![\w"''$:])[A-Za-z]:[\\/][^\s"'';|<>(){},`+]*)\s*\+\s*("[^"]*"|''[^'']*'')', [Text.RegularExpressions.MatchEvaluator] {
                param($m) '"' + $m.Groups[1].Value.Trim('"', "'").TrimEnd('\', '/') + '\' + $m.Groups[2].Value.Trim('"', "'").TrimStart('\', '/') + '"' })
        #    Text-Argumente von Auftrag.ps1, decisions.py und news.py sind kein Ziel: Text in Anfuehrungszeichen bis zum
        #    Anweisungsende leeren (Umleitungen und Folgeanweisungen bleiben geprueft)
        if (-not (Test-AusfuehrtText $cmd)) {
            $agRx = [regex]::new($textSkripte)
            $agTreffer = $agRx.Match($t)
            while ($agTreffer.Success) {
                $agSb = New-Object System.Text.StringBuilder
                $agEnde = $agTreffer.Index + $agTreffer.Length
                $agPos = $agEnde
                $agQuote = [char]0
                while ($agPos -lt $t.Length) {
                    $agZ = $t[$agPos]
                    if ($agQuote -ne [char]0) {
                        if ($agQuote -eq [char]'"' -and $agZ -eq [char]'`') { $agPos += 2; continue }
                        if ($agZ -eq $agQuote) { $agQuote = [char]0; [void]$agSb.Append($agZ) }
                        $agPos++; continue
                    }
                    if ($agZ -eq [char]'"' -or $agZ -eq [char]"'") { $agQuote = $agZ; [void]$agSb.Append($agZ); $agPos++; continue }
                    if ($agZ -eq [char]';' -or $agZ -eq [char]'|' -or $agZ -eq [char]'>' -or $agZ -eq [char]"`n" -or $agZ -eq [char]"`r") { break }
                    [void]$agSb.Append($agZ); $agPos++
                }
                $t = $t.Substring(0, $agEnde) + $agSb.ToString() + $t.Substring([Math]::Min($agPos, $t.Length))
                $agTreffer = $agRx.Match($t, $agEnde + $agSb.Length)
            }
        }
        $start = '(?:[A-Za-z]:[\\/]|\\\\[\w.$-]+\\)'
        $kand = New-Object System.Collections.Generic.List[string]
        $qPat = '(["''])(' + $start + '[^"''\r\n]*)\1'
        foreach ($m in [regex]::Matches($t, $qPat)) { [void]$kand.Add($m.Groups[2].Value) }
        $rest = [regex]::Replace($t, $qPat, ' ')
        foreach ($m in [regex]::Matches($rest, '(?<![\w"''$:])(' + $start + '[^\s"'';|<>(){},`]*)')) { [void]$kand.Add($m.Groups[1].Value) }

        $liste = $LeseOrte; if ($schreibt) { $liste = $Orte }
        $draussen = New-Object System.Collections.Generic.List[string]
        foreach ($c in $kand) {
            $full = Resolve-GuardPfad $c $cwd
            if (-not $full) { continue }
            if (Test-NetzPfad $full) { Stop-Guard ($NetzMeldung + ' Pfad: ' + $full) }
            if ($full -match '^(?i)\\\\(127\.0\.0\.1|localhost)(\\|$)') { continue }     # lokaler SMB-Testserver
            if ($c -match '(?i)\.(exe|bat|cmd|com|dll)$') { continue }
            if (-not (Test-GuardOrt $full $liste) -and -not $draussen.Contains($full)) { [void]$draussen.Add($full) }
        }
        $cwdVoll = Resolve-GuardPfad $cwd $root
        if ($cwdVoll -and (Test-NetzPfad $cwdVoll)) { Stop-Guard ($NetzMeldung + ' Arbeitsverzeichnis: ' + $cwdVoll) }
        if ($cwdVoll -and -not (Test-GuardOrt $cwdVoll $liste)) { [void]$draussen.Add('Arbeitsverzeichnis ' + $cwdVoll) }
        if ($draussen.Count -gt 0) {
            $art = 'greift'; if ($schreibt) { $art = 'schreibt' }
            Send-GuardFrage ('Befehl ' + $art + ' moeglicherweise ausserhalb der erlaubten Orte zu: ' + ($draussen -join ', ') + '. ' + $OrtHinweis)
        }
        exit 0
    }
}
catch { exit 0 }
exit 0
