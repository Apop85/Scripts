# Auftrag-guard-Selbsttest.ps1 - prueft SFLIX_Tools\Auftrag-guard.ps1 mit simulierten Hook-Eingaben (STEFFLIX, nach Sync2PC)
# Aufruf: powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Auftrag-guard-Selbsttest.ps1
# Schreibt nichts; jeder Fall startet den Waechter mit einer JSON-Eingabe und vergleicht das Ergebnis:
#   frei = Exit 0 ohne Ausgabe, frage = Rueckfrage (ask), sperre = Exit 2. Reines ASCII (PowerShell 5.1).

$guard = Join-Path $PSScriptRoot 'Auftrag-guard.ps1'
$root = Split-Path -Parent $PSScriptRoot
$tmp = [IO.Path]::GetTempPath().TrimEnd('\')
$prof = $env:USERPROFILE
$mem = Join-Path $prof ('.claude\projects\' + ($root -replace '[:\\/]', '-') + '\memory')
$adb = Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'
$testenv = Join-Path $root '.claude\testenv'
$fn = 'func' + 'tion'          # Schluesselwort zerlegt, damit der Waechter diese Datei nicht selbst sperrt
$ok = 0; $fehler = 0

$pruefe = {
    param([string]$name, [string]$tool, [string]$wert, [string]$erwartet, [string]$cwd)
    if (-not $cwd) { $cwd = $root }
    $ti = @{}
    if ($tool -eq 'PowerShell' -or $tool -eq 'Bash') { $ti.command = $wert }
    elseif ($tool -eq 'Glob' -or $tool -eq 'Grep') { $ti.path = $wert; $ti.pattern = '*.md' }
    else { $ti.file_path = $wert; $ti.content = 'x' }
    $json = ConvertTo-Json @{ tool_name = $tool; tool_input = $ti; cwd = $cwd } -Depth 4 -Compress
    $aus = ($json | powershell -NoProfile -ExecutionPolicy Bypass -File $guard 2>&1 | Out-String)
    $code = $LASTEXITCODE
    $ist = 'frei'
    if ($code -eq 2) { $ist = 'sperre' }
    elseif ($aus -match '"permissionDecision":"ask"') { $ist = 'frage' }
    elseif ($code -ne 0 -or $aus.Trim()) { $ist = "unerwartet (Exit $code)" }
    if ($ist -eq $erwartet) { $script:ok++ ; Write-Output ("OK      " + $name) }
    else { $script:fehler++; Write-Output ("FEHLER  " + $name + ": erwartet " + $erwartet + ", ist " + $ist + " | " + $aus.Trim()) }
}

# Datei-Werkzeuge schreibend
& $pruefe 'Write im Projekt'                 'Write' (Join-Path $root 'docs\x.md') 'frei'
& $pruefe 'Write tief im Projekt'            'Write' (Join-Path $root 'sflix_sys\js\player.js') 'frei'
& $pruefe 'Write .claude im Projekt'         'Write' (Join-Path $root '.claude\skills\rolle-x\SKILL.md') 'frei'
& $pruefe 'Write Nachbarordner SFLIX-alt'    'Write' ($root + '-alt\a.md') 'sperre'
& $pruefe 'Write Sync2PC'                    'Write' 'C:\Users\rbald\Documents\Sync2PC\a.md' 'sperre'
& $pruefe 'Write in %TMP%'                   'Write' (Join-Path $tmp 'claude\scratch\a.md') 'frei'
& $pruefe 'Write Memory'                     'Write' (Join-Path $mem 'x.md') 'frei'
& $pruefe 'Write Desktop'                    'Write' (Join-Path $prof 'Desktop\x.txt') 'sperre'
& $pruefe 'Write STEFFLIX-TestShare'         'Write' (Join-Path $env:LOCALAPPDATA 'STEFFLIX-TestShare\a.txt') 'frei'
& $pruefe 'Write AVD'                        'Write' (Join-Path $prof '.android\avd\STEFFLIX_API35.ini') 'frei'
& $pruefe 'Write LOCALAPPDATA sonst'         'Write' (Join-Path $env:LOCALAPPDATA 'Anderes\a.txt') 'sperre'
& $pruefe 'Write coordination/tasks'         'Write' (Join-Path $root 'coordination\tasks\OPEN\x.md') 'sperre'
& $pruefe 'Write coordination/README'        'Write' (Join-Path $root 'coordination\README.md') 'frei'
& $pruefe 'Write Netzlaufwerk M:'            'Write' 'M:\Claude\a.txt' 'sperre'
& $pruefe 'Write D:'                         'Write' 'D:\a.txt' 'sperre'
& $pruefe 'Write UNC'                        'Write' '\\nas\share\a.txt' 'sperre'
& $pruefe 'Write .ps1 mit Alias-Funktion'    'Write' (Join-Path $tmp 'a.ps1') 'frei'

# Datei-Werkzeuge lesend
& $pruefe 'Read im Projekt'                  'Read' (Join-Path $root 'start.html') 'frei'
& $pruefe 'Read Testumgebung'                'Read' (Join-Path $testenv 'start.html') 'frei'
& $pruefe 'Read Sync2PC'                     'Read' 'C:\Users\rbald\Documents\Sync2PC\CLAUDE.md' 'sperre'
& $pruefe 'Read Netzlaufwerk'                'Read' 'Z:\Filme\a.mp4' 'sperre'
& $pruefe 'Read Tool-Ergebnis Claude'        'Read' (Join-Path $prof '.claude\projects\x\tool-results\a.txt') 'frei'
& $pruefe 'Read csc-Ordner'                  'Read' 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll' 'frei'
& $pruefe 'Read C:\Windows sonst'            'Read' 'C:\Windows\win.ini' 'sperre'
& $pruefe 'Glob Projekt'                     'Glob' $root 'frei'
& $pruefe 'Glob Benutzerordner'              'Glob' 'C:\Users\rbald\Documents' 'sperre'
& $pruefe 'Grep NAS'                         'Grep' 'Y:\' 'sperre'

# PowerShell
& $pruefe 'Lesen im Projekt'                 'PowerShell' 'Get-Content -Encoding UTF8 start.html' 'frei'
& $pruefe 'Lesen ausserhalb'                 'PowerShell' 'Get-Content -Encoding UTF8 C:\Windows\win.ini' 'frage'
& $pruefe 'Lesen Netzlaufwerk'               'PowerShell' 'Get-ChildItem M:\Claude' 'sperre'
& $pruefe 'Lesen UNC'                        'PowerShell' 'Get-ChildItem \\nas\Filme' 'sperre'
& $pruefe 'Lokaler SMB-Testserver als Text'  'PowerShell' 'Write-Output "\\127.0.0.1\STEFFLIX"' 'frei'
& $pruefe 'New-Item im Projekt (relativ)'    'PowerShell' 'New-Item -ItemType Directory docs\neu' 'frei'
& $pruefe 'New-Item ausserhalb'              'PowerShell' 'New-Item -ItemType Directory C:\Users\rbald\Neu' 'frage'
& $pruefe 'relativ, cwd ausserhalb'          'PowerShell' 'New-Item x.txt' 'frage' (Join-Path $prof 'Desktop')
& $pruefe 'csc mit Verweisen'                'PowerShell' '& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:library /reference:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.dll /out:C:\Users\rbald\AppData\Local\Temp\x.dll src\a.cs' 'frei'
& $pruefe 'adb-Screenshot ins Projekt'       'PowerShell' ('& "' + $adb + '" exec-out screencap -p > .claude\testenv\a.png') 'frei'
& $pruefe 'adb shell input text UNC'         'PowerShell' ('& "' + $adb + '" shell ''input text "\\\\nas\\Fotos"''') 'frei'
& $pruefe 'Join-Path TestShare'              'PowerShell' 'New-Item -ItemType Directory (Join-Path $env:LOCALAPPDATA ''STEFFLIX-TestShare\venv'')' 'frei'
& $pruefe 'Join-Path ausserhalb'             'PowerShell' 'Copy-Item a.txt (Join-Path $env:LOCALAPPDATA ''Anderes\a.txt'')' 'frage'
& $pruefe 'WriteAllText $env:TEMP'           'PowerShell' '[IO.File]::WriteAllText("$env:TEMP\x.txt", ''a'', [Text.UTF8Encoding]::new($false))' 'frei'
& $pruefe 'WriteAllText %USERPROFILE%'       'PowerShell' '[IO.File]::WriteAllText("$env:USERPROFILE\x.txt", ''a'')' 'frage'
& $pruefe 'Auftrag.ps1, Pfad in -Risiken'    'PowerShell' 'powershell -NoProfile -File SFLIX_Tools\Auftrag.ps1 abschliessen -Rolle Pruefung -Id 7 -Arbeiten "x" -Tests "ok" -Entscheide keine -Risiken "liegt noch unter C:\Users\rbald\Desktop\x"' 'frei'
& $pruefe 'decisions.py, Pfad im Text'       'PowerShell' 'python SFLIX_Tools\Doku\decisions.py neu --titel "Pfad" --entscheidung "liegt unter D:\x"' 'frei'
& $pruefe 'Auftrag.ps1 + Folgeanweisung'     'PowerShell' 'powershell -File SFLIX_Tools\Auftrag.ps1 liste -Rolle Pruefung; Remove-Item "C:\Neu\x.txt"' 'frage'
& $pruefe 'coordination direkt'              'PowerShell' 'Remove-Item coordination\tasks\OPEN\x.md' 'frage'
& $pruefe 'Alias-Funktion'                   'PowerShell' ($fn + ' gc { 1 }') 'sperre'
& $pruefe 'Verb-Nomen-Funktion'              'PowerShell' ($fn + ' Read-FileText { 1 }') 'frei'
& $pruefe 'Alias-Name nur als Suchtext'      'PowerShell' ('Select-String -Path a.txt -Pattern ''' + $fn + ' rd''') 'frei'
& $pruefe 'Alias-Funktion mit Aufruf'        'PowerShell' ($fn + ' rd { Remove-Item $args }; rd x') 'sperre'
& $pruefe 'Funktion in -Command-Text'        'PowerShell' ('powershell -Command "' + $fn + ' rd { 1 }"') 'sperre'
$paar1 = '$p = @(@(''a'',''b'')); $t = [IO.File]::ReadAllText("$env:TEMP\x.txt"); foreach ($q in $p) { $t = $t.Replace($q[0], $q[1]) }; [IO.File]::WriteAllText("$env:TEMP\x.txt", $t)'
& $pruefe 'Abflachung: ein Paar'             'PowerShell' $paar1 'sperre'
& $pruefe 'Abflachung: Komma-Schreibweise'   'PowerShell' ($paar1.Replace('@(@(''a'',''b''))', '@(,@(''a'',''b''))')) 'frei'

# STEFFLIX.exe nur mit Testumgebung
& $pruefe 'EXE ohne SFLIX_DIR (&)'           'PowerShell' '& .\STEFFLIX.exe --headless data' 'sperre'
& $pruefe 'EXE ohne SFLIX_DIR (Start-Process)' 'PowerShell' 'Start-Process "SFLIX_Tools\SflixTools\dist\STEFFLIX.exe" -ArgumentList "--tools"' 'sperre'
& $pruefe 'EXE direkt am Zeilenanfang'       'PowerShell' 'SFLIX_Tools\SflixTools\dist\STEFFLIX.exe --headless data' 'sperre'
& $pruefe 'EXE mit SFLIX_DIR testenv'        'PowerShell' ('$env:SFLIX_DIR = ''' + $testenv + '''; & "SFLIX_Tools\SflixTools\dist\STEFFLIX.exe" --headless data') 'frei'
& $pruefe 'EXE mit SFLIX_DIR anderswo'       'PowerShell' '$env:SFLIX_DIR = ''C:\Users\rbald\Videos\Stefflix''; & .\STEFFLIX.exe --headless data' 'sperre'
& $pruefe 'EXE nur lesen (Hash)'             'PowerShell' 'Get-FileHash STEFFLIX.exe; (Get-Item SFLIX_Tools\SflixTools\dist\STEFFLIX.exe).VersionInfo' 'frei'
& $pruefe 'EXE kopieren'                     'PowerShell' 'Copy-Item SFLIX_Tools\SflixTools\dist\STEFFLIX.exe STEFFLIX.exe' 'frei'

Write-Output ''
Write-Output ("Ergebnis: " + $ok + " OK, " + $fehler + " Fehler")
if ($fehler -gt 0) { exit 1 }
exit 0
