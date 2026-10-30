# ===================================================================
#  STEFFLIX - Release bauen und bereitstellen
#
#  Start: Doppelklick auf build_release.bat (oder: powershell -ExecutionPolicy Bypass -File release.ps1)
#
#  Ordner: Diese Skripte liegen in <Release-Ordner>\SFLIX Tools\Release. Der Release-Ordner
#  (start.html, sflix_sys, die Programme, checksums.json, ffmpeg.json) liegt zwei Ebenen darüber,
#  das Updater-Projekt in <Release-Ordner>\SFLIX Tools\SflixTools.
#
#  Ablauf (mit Rückfragen, Enter übernimmt jeweils den Vorschlag in [eckigen Klammern]):
#   1. Updater    Hat sich der Quellcode im Unterordner "SFLIX Tools\SflixTools" seit dem letzten Release
#                 geändert, wird die Versionsnummer erhöht (Vorschlag: nächste Patch-Version,
#                 änderbar), updateStefflix.exe gebaut und hierher kopiert.
#   2. Android    Hat sich der Quellcode der App ("SFLIX Tools\Android") oder sflix_sys\splash.svg
#                 seit dem letzten Release geändert, wird die App-Version erhöht (versionName x.y.z
#                 und versionCode in app\build.gradle), STEFFLIX.apk gebaut und hierher kopiert.
#   3. ffmpeg     Gibt es eine neuere stabile ffmpeg-Version, kann ffmpeg.json aktualisiert werden.
#   4. STEFFLIX   Wurden Dateien der Weboberfläche geändert, kann die Version in version.js
#                 erhöht und ein Changelog-Eintrag in news.txt ergänzt werden. Format x.y.z
#                 (ab 2.5.0), version.js erhält zusätzlich die alte Zahl (siehe versioning.py).
#   5. Prüfen     check_release.py, create_news.py, create_checksums.py
#   6. Übersicht  welche Dateien sich geändert haben und gepusht werden müssen
#
#  Optionen:
#   -Auto           alle Vorschläge ohne Rückfrage übernehmen (STEFFLIX-Version bleibt unverändert)
#   -MarkReleased   nur merken, dass der aktuelle Quellcode von Updater und App bereitgestellt ist (ohne Bauen)
#   -SflixTools P   anderer Pfad zum Updater-Projekt
# ===================================================================

param(
    [switch]$Auto,
    [switch]$MarkReleased,
    [string]$SflixTools = ''
)

$ErrorActionPreference = 'Stop'
try { [Console]::OutputEncoding = New-Object Text.UTF8Encoding $false } catch { }
$env:PYTHONIOENCODING = 'utf-8'
$Scripts = $PSScriptRoot                                                   # SFLIX Tools\Release
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path               # Release-Ordner
if (-not $SflixTools) { $SflixTools = Join-Path $Root 'SFLIX Tools\SflixTools' }
# Python findet sflix_update.py im Skriptordner, auch wenn es im Release-Ordner läuft
$env:PYTHONPATH = $Scripts
$Utf8 = New-Object Text.UTF8Encoding $false
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

# ------------------------------------------------------------------
# Hilfsfunktionen
# ------------------------------------------------------------------

function Step([string]$text) {
    Write-Host ''
    Write-Host $text -ForegroundColor Cyan
}

function Info([string]$text) { Write-Host "   $text" }
function Warn([string]$text) { Write-Host "   $text" -ForegroundColor Yellow }
function Good([string]$text) { Write-Host "   $text" -ForegroundColor Green }

# Ja/Nein-Frage. Enter = Vorschlag.
function Ask-YesNo([string]$question, [bool]$default) {
    if ($Auto) { Info ("$question " + $(if ($default) { 'ja' } else { 'nein' }) + ' (automatisch)'); return $default }
    $hint = if ($default) { '[J/n]' } else { '[j/N]' }
    while ($true) {
        $a = ([string](Read-Host "   $question $hint")).Trim().ToLower()
        if ($a -eq '') { return $default }
        if ($a -in 'j', 'ja', 'y', 'yes') { return $true }
        if ($a -in 'n', 'nein', 'no') { return $false }
        Warn 'Bitte j oder n eingeben.'
    }
}

# Texteingabe mit Vorschlag und Prüfung per Regex
function Ask-Text([string]$question, [string]$default, [string]$pattern, [string]$formatHint) {
    if ($Auto) { Info "$question $default (automatisch)"; return $default }
    while ($true) {
        $a = ([string](Read-Host "   $question [$default]")).Trim()
        if ($a -eq '') { return $default }
        if ($a -match $pattern) { return $a }
        Warn "Ungültig. Erwartet: $formatHint"
    }
}

function Fail([string]$text) { throw $text }

function Run-Python([string]$script) {
    Push-Location $Root
    try {
        & python -X utf8 (Join-Path $Scripts $script)
        if ($LASTEXITCODE -ne 0) { Fail "$script ist fehlgeschlagen (Exit-Code $LASTEXITCODE)" }
    } finally { Pop-Location }
}

# Aktuelle Prüfsummen aller Release-Dateien, berechnet wie create_checksums.py (sflix_update.checksum)
function Get-CurrentChecksums {
    Push-Location $Root
    try {
        # Keine doppelten Anführungszeichen im Code: Windows PowerShell 5.1 gibt sie an Programme falsch weiter
        $code = 'import json,os;from sflix_update import FILES,checksum;print(json.dumps({p:checksum(p,open(p,''rb'').read()) for p in FILES if os.path.exists(p)}))'
        $out = & python -X utf8 -c $code
        if ($LASTEXITCODE -ne 0) { Fail 'Prüfsummen konnten nicht berechnet werden (ist requests installiert? pip install requests)' }
        return ($out | ConvertFrom-Json)
    } finally { Pop-Location }
}

function Get-FileVersion([string]$path) {
    if (-not (Test-Path $path)) { return $null }
    $v = (Get-Item $path).VersionInfo
    if ($v.FileMajorPart -eq 0 -and $v.FileMinorPart -eq 0 -and $v.FileBuildPart -eq 0) { return $null }
    return [version]"$($v.FileMajorPart).$($v.FileMinorPart).$($v.FileBuildPart)"
}

# Fingerabdruck des Updater-Quellcodes. Die Versionszeilen zählen nicht, sonst wäre der Code
# nach jedem Hochzählen "geändert".
function Get-SourceFingerprint {
    $files = @(Get-ChildItem (Join-Path $SflixTools 'src') -Filter *.cs | Sort-Object Name)
    $files += Get-Item (Join-Path $SflixTools 'tools\AssetGen.cs')
    $files += Get-Item (Join-Path $SflixTools 'build.ps1')
    $sha = [Security.Cryptography.SHA256]::Create()
    $sb = New-Object Text.StringBuilder
    foreach ($f in $files) {
        $text = [IO.File]::ReadAllText($f.FullName) -replace "`r`n", "`n"
        $text = ($text -split "`n" | Where-Object { $_ -notmatch 'Assembly(File)?Version\(' }) -join "`n"
        [void]$sb.Append($f.Name).Append("`0").Append($text).Append("`0")
    }
    return (($sha.ComputeHash($Utf8.GetBytes($sb.ToString())) | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-ProgramVersion([string]$programCs) {
    $m = [regex]::Match([IO.File]::ReadAllText($programCs), 'AssemblyFileVersion\("(\d+)\.(\d+)\.(\d+)')
    if (-not $m.Success) { Fail "Keine AssemblyFileVersion in $programCs gefunden" }
    return [version]"$($m.Groups[1].Value).$($m.Groups[2].Value).$($m.Groups[3].Value)"
}

function Set-ProgramVersion([string]$programCs, [version]$v) {
    $text = [IO.File]::ReadAllText($programCs)
    $full = "$($v.Major).$($v.Minor).$($v.Build).0"
    $text = [regex]::Replace($text, 'AssemblyVersion\("[\d.]+"\)', "AssemblyVersion(`"$full`")")
    $text = [regex]::Replace($text, 'AssemblyFileVersion\("[\d.]+"\)', "AssemblyFileVersion(`"$full`")")
    [IO.File]::WriteAllText($programCs, $text, $Utf8)
}

function Short([version]$v) { "$($v.Major).$($v.Minor).$($v.Build)" }

# Android-App: Version steht in app\build.gradle (versionName 'x.y.z', versionCode = x*10000 + y*100 + z)
$AndroidDir = Join-Path $Root 'SFLIX Tools\Android'
$androidGradle = Join-Path $AndroidDir 'app\build.gradle'
$androidStateFile = Join-Path $AndroidDir 'release_state.json'

# Fingerabdruck des App-Quellcodes inkl. splash.svg (wird beim Bauen in die App kopiert).
# Die Versionszeilen zählen nicht, die Kopie von splash.svg in assets auch nicht (zählt über das Original).
function Get-AndroidFingerprint {
    $files = @(Get-ChildItem (Join-Path $AndroidDir 'app\src') -Recurse -File |
        Where-Object { $_.FullName -notlike '*\assets\splash.svg' })
    foreach ($n in 'build.gradle', 'settings.gradle', 'gradle.properties', 'build.ps1', 'app\build.gradle') {
        $p = Join-Path $AndroidDir $n
        if (Test-Path $p) { $files += Get-Item $p }
    }
    $files += Get-Item (Join-Path $Root 'sflix_sys\splash.svg')
    $files = $files | Sort-Object FullName
    $sha = [Security.Cryptography.SHA256]::Create()
    $sb = New-Object Text.StringBuilder
    foreach ($f in $files) {
        $bytes = [IO.File]::ReadAllBytes($f.FullName)
        if ($f.FullName -eq $androidGradle) {
            $text = ([IO.File]::ReadAllText($f.FullName) -replace "`r`n", "`n" -split "`n" |
                Where-Object { $_ -notmatch '^\s*version(Code|Name)\b' }) -join "`n"
            $bytes = $Utf8.GetBytes($text)
        }
        $hash = ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString('x2') }) -join ''
        [void]$sb.Append($f.Name).Append("`0").Append($hash).Append("`0")
    }
    return (($sha.ComputeHash($Utf8.GetBytes($sb.ToString())) | ForEach-Object { $_.ToString('x2') }) -join '')
}

function Get-AndroidVersion {
    $m = [regex]::Match([IO.File]::ReadAllText($androidGradle), "versionName\s+'(\d+)\.(\d+)\.(\d+)'")
    if (-not $m.Success) { Fail "Kein versionName 'x.y.z' in $androidGradle gefunden" }
    return [version]"$($m.Groups[1].Value).$($m.Groups[2].Value).$($m.Groups[3].Value)"
}

function Set-AndroidVersion([version]$v) {
    $text = [IO.File]::ReadAllText($androidGradle)
    $code = $v.Major * 10000 + $v.Minor * 100 + $v.Build
    $text = [regex]::Replace($text, "versionName\s+'[\d.]+'", "versionName '$(Short $v)'")
    $text = [regex]::Replace($text, 'versionCode\s+\d+', "versionCode $code")
    [IO.File]::WriteAllText($androidGradle, $text, $Utf8)
}

function Save-AndroidState([string]$version, [string]$fingerprint) {
    @{ version = $version; sourceHash = $fingerprint; built = (Get-Date).ToString('s') } |
        ConvertTo-Json | Set-Content $androidStateFile -Encoding UTF8
}

# STEFFLIX-Versionen (gleiche Regeln wie versioning.py): ab 2.5.0 x.y.z, dazu die alte Zahl
# x + y/100 + z/10000 für Installationen bis 2.04 ("2.5.0" -> "2.05", "2.5.1" -> "2.0501")
function Convert-LegacyVersion([string]$text) {
    $parts = $text.Trim().Split('.')
    if ($parts.Count -ge 3) { return "$([int]$parts[0]).$([int]$parts[1]).$([int]$parts[2])" }
    $dec = if ($parts.Count -gt 1) { $parts[1] } else { '' }
    if ($dec.Length -le 2) { $minor = [int]($dec.PadRight(2, '0')); $patch = 0 }
    elseif ($dec.Length -eq 3) { $minor = [int]$dec.Substring(0, 2); $patch = [int]$dec.Substring(2, 1) }
    else { $minor = [int]$dec.Substring(0, 2); $patch = [int]$dec.Substring(2, 2) }
    return "$([int]$parts[0]).$minor.$patch"
}

function Get-WebVersionName([string]$vjs) {
    $n = [regex]::Match($vjs, '\bversionName\s*=\s*["'']([\d.]+)["'']')
    if ($n.Success) { return Convert-LegacyVersion $n.Groups[1].Value }
    $l = [regex]::Match($vjs, '\bversion\s*=\s*([\d.]+)')
    if ($l.Success) { return Convert-LegacyVersion $l.Groups[1].Value }
    return $null
}

function Get-LegacyNumber([string]$name) {
    $v = [version]$name
    $s = "$($v.Major)." + $v.Minor.ToString('00')
    if ($v.Build -gt 0) { $s += $v.Build.ToString('00') }
    return $s
}

function Get-VersionJsText([string]$name) {
    "// Alte Zahl zuerst: STEFFLIX und Updater bis 2.04 lesen nur diese Zeile`n" +
    "var version = $(Get-LegacyNumber $name);`n" +
    "var versionName = `"$name`";`n"
}

# Nur HTML-Sonderzeichen kodieren, Umlaute bleiben wie in den bisherigen Einträgen direkt stehen
function Html([string]$s) { $s.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;') }

# ------------------------------------------------------------------
# Start
# ------------------------------------------------------------------

$programCs = Join-Path $SflixTools 'src\Program.cs'
$stateFile = Join-Path $SflixTools 'release_state.json'

function Save-State([string]$version, [string]$fingerprint) {
    @{ version = $version; sourceHash = $fingerprint; built = (Get-Date).ToString('s') } |
        ConvertTo-Json | Set-Content $stateFile -Encoding UTF8
}

if ($MarkReleased) {
    $v = Short (Get-ProgramVersion $programCs)
    Save-State $v (Get-SourceFingerprint)
    Write-Host "Gemerkt: Quellcode von Version $v gilt als bereitgestellt ($stateFile)."
    if (Test-Path $androidGradle) {
        $av = Short (Get-AndroidVersion)
        Save-AndroidState $av (Get-AndroidFingerprint)
        Write-Host "Gemerkt: Android-App $av gilt als bereitgestellt ($androidStateFile)."
    }
    exit 0
}
$programCsBackup = $null
$androidGradleBackup = $null
$released = @()   # Beschreibung der Änderungen für die Übersicht

try {
    Write-Host '==================================================================='
    Write-Host ' STEFFLIX - Release bauen und bereitstellen'
    Write-Host '==================================================================='

    if (-not (Get-Command python -ErrorAction SilentlyContinue)) { Fail 'Python wurde nicht gefunden.' }
    if (-not (Test-Path (Join-Path $Root 'start.html'))) { Fail "Kein STEFFLIX-Release-Ordner: $Root" }

    # Ausgangslage merken, um am Ende die geänderten Dateien zu zeigen
    $before = Get-CurrentChecksums
    $published = @{}
    if (Test-Path (Join-Path $Root 'checksums.json')) {
        (Get-Content (Join-Path $Root 'checksums.json') -Raw -Encoding UTF8 | ConvertFrom-Json).PSObject.Properties |
            ForEach-Object { $published[$_.Name] = $_.Value }
    }

    # --------------------------------------------------------------
    Step '[1/6] Updater (updateStefflix.exe)'
    # --------------------------------------------------------------
    $releaseExe = Join-Path $Root 'updateStefflix.exe'
    if (-not (Test-Path $programCs)) {
        Warn "Updater-Projekt nicht gefunden: $SflixTools"
        Warn 'updateStefflix.exe bleibt unverändert.'
    } else {
        $sourceVersion = Get-ProgramVersion $programCs
        $releaseVersion = Get-FileVersion $releaseExe
        $fingerprint = Get-SourceFingerprint
        $state = $null
        if (Test-Path $stateFile) { $state = Get-Content $stateFile -Raw -Encoding UTF8 | ConvertFrom-Json }
        $changed = ($state -eq $null) -or ($state.sourceHash -ne $fingerprint)

        Info ("Version im Release-Ordner: " + $(if ($releaseVersion) { Short $releaseVersion } else { 'keine' }) +
              "   im Quellcode: " + (Short $sourceVersion))
        if ($changed) { Info 'Der Quellcode wurde seit dem letzten Release geändert.' }
        else { Info 'Der Quellcode ist seit dem letzten Release unverändert.' }

        if (Ask-YesNo 'Updater neu bauen und bereitstellen?' $changed) {
            # Vorschlag: Wurde die Version im Quellcode schon erhöht (höher als die bereitgestellte),
            # gilt sie. Sonst die nächste Patch-Version nach der höheren der beiden.
            if (-not $releaseVersion -or $sourceVersion -gt $releaseVersion) {
                $suggest = $sourceVersion
                Info "Vorschlag aus dem Quellcode: $(Short $sourceVersion)."
            } else {
                $suggest = [version]"$($releaseVersion.Major).$($releaseVersion.Minor).$($releaseVersion.Build + 1)"
            }
            Info 'Enter übernimmt den Vorschlag, eine andere Nummer kann eingegeben werden.'
            Info 'Tipp: 3.0.5 = kleine Änderung, 3.1.0 = neue Funktionen, 4.0.0 = grosser Umbau.'
            $newVersion = [version](Ask-Text 'Neue Versionsnummer' (Short $suggest) '^\d+\.\d+\.\d+$' 'z.B. 3.0.5')
            if ($releaseVersion -and $newVersion -le $releaseVersion) {
                Warn "$(Short $newVersion) ist nicht höher als die bereitgestellte Version $(Short $releaseVersion)."
                Warn 'Installationen würden diese Version beim Selbst-Update NICHT übernehmen.'
                if (-not (Ask-YesNo 'Trotzdem fortfahren?' $false)) { Fail 'Abgebrochen.' }
            }

            $programCsBackup = [IO.File]::ReadAllText($programCs)
            Set-ProgramVersion $programCs $newVersion
            Info "Baue Version $(Short $newVersion) ..."
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $SflixTools 'build.ps1') | ForEach-Object { Info $_ }
            if ($LASTEXITCODE -ne 0) { Fail 'Build fehlgeschlagen.' }
            $built = Join-Path $SflixTools 'dist\updateStefflix.exe'
            if ((Get-FileVersion $built) -ne $newVersion) { Fail 'Die gebaute EXE hat nicht die erwartete Version.' }
            $programCsBackup = $null   # Build erfolgreich, Version bleibt

            Copy-Item $built $releaseExe -Force
            Save-State (Short $newVersion) $fingerprint
            Good "updateStefflix.exe $(Short $newVersion) bereitgestellt."
            $released += "Updater $(Short $newVersion)"
        } else {
            Info 'updateStefflix.exe bleibt unverändert.'
        }
    }

    # --------------------------------------------------------------
    Step '[2/6] Android-App (STEFFLIX.apk)'
    # --------------------------------------------------------------
    $releaseApk = Join-Path $Root 'STEFFLIX.apk'
    $androidReleased = $false
    if (-not (Test-Path $androidGradle)) {
        Warn "App-Projekt nicht gefunden: $AndroidDir"
        Warn 'STEFFLIX.apk bleibt unverändert.'
    } else {
        $appSourceVersion = Get-AndroidVersion
        $appFingerprint = Get-AndroidFingerprint
        $appState = $null
        if (Test-Path $androidStateFile) { $appState = Get-Content $androidStateFile -Raw -Encoding UTF8 | ConvertFrom-Json }
        $appReleased = if ($appState) { [version]$appState.version } else { $null }
        # Vor diesem Schritt von Hand gebaute APK (ohne release_state.json): gilt als Version im Quellcode
        if (-not $appReleased -and (Test-Path $releaseApk)) { $appReleased = $appSourceVersion }
        $appChanged = ($appState -eq $null) -or ($appState.sourceHash -ne $appFingerprint)

        Info ("Zuletzt bereitgestellt: " + $(if ($appReleased) { Short $appReleased } else { 'keine' }) +
              "   im Quellcode: " + (Short $appSourceVersion))
        if ($appChanged) { Info 'Der Quellcode der App wurde seit dem letzten Release geändert.' }
        else { Info 'Der Quellcode der App ist seit dem letzten Release unverändert.' }

        if (Ask-YesNo 'Android-App neu bauen und bereitstellen?' $appChanged) {
            # Vorschlag: Version aus dem Quellcode, wenn sie höher als die bereitgestellte ist (schon
            # erhöht oder allererstes Release). Sonst die nächste Patch-Version.
            if (-not $appReleased -or $appSourceVersion -gt $appReleased) {
                $appSuggest = $appSourceVersion
                Info "Vorschlag aus dem Quellcode: $(Short $appSourceVersion)."
            } else {
                $appSuggest = [version]"$($appReleased.Major).$($appReleased.Minor).$($appReleased.Build + 1)"
            }
            Info 'Enter übernimmt den Vorschlag, eine andere Nummer kann eingegeben werden.'
            Info 'Tipp: 1.0.1 = kleine Änderung, 1.1.0 = neue Funktionen (y und z unter 100).'
            $newApp = [version](Ask-Text 'Neue App-Version' (Short $appSuggest) '^\d+\.\d{1,2}\.\d{1,2}$' 'z.B. 1.0.1')
            if ($appReleased -and $newApp -le $appReleased) {
                Warn "$(Short $newApp) ist nicht höher als die bereitgestellte Version $(Short $appReleased)."
                Warn 'Android würde die neue APK nicht als Update über die alte installieren.'
                if (-not (Ask-YesNo 'Trotzdem fortfahren?' $false)) { Fail 'Abgebrochen.' }
            }

            $androidGradleBackup = [IO.File]::ReadAllText($androidGradle)
            Set-AndroidVersion $newApp
            Info "Baue Android-App $(Short $newApp) (beim ersten Mal wird nach dem Passwort des Signaturschlüssels gefragt) ..."
            $apkBefore = if (Test-Path $releaseApk) { (Get-Item $releaseApk).LastWriteTimeUtc } else { [datetime]::MinValue }
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $AndroidDir 'build.ps1')
            if ($LASTEXITCODE -ne 0) { Fail 'Build der Android-App fehlgeschlagen.' }
            if (-not (Test-Path $releaseApk) -or (Get-Item $releaseApk).LastWriteTimeUtc -le $apkBefore) {
                Fail 'STEFFLIX.apk wurde nicht neu erstellt.'
            }
            $androidGradleBackup = $null   # Build erfolgreich, Version bleibt

            Save-AndroidState (Short $newApp) (Get-AndroidFingerprint)
            Good "STEFFLIX.apk $(Short $newApp) bereitgestellt."
            $released += "Android-App $(Short $newApp)"
            $androidReleased = $true
        } else {
            Info 'STEFFLIX.apk bleibt unverändert.'
        }
    }

    # --------------------------------------------------------------
    Step '[3/6] ffmpeg (ffmpeg.json)'
    # --------------------------------------------------------------
    $ffmpegJson = Join-Path $Root 'ffmpeg.json'
    $current = $null
    if (Test-Path $ffmpegJson) { $current = Get-Content $ffmpegJson -Raw -Encoding UTF8 | ConvertFrom-Json }
    Info ("Vorgegeben: ffmpeg " + $(if ($current) { $current.version } else { '(keine ffmpeg.json)' }))
    $latest = $null
    try {
        $all = Invoke-RestMethod 'https://api.github.com/repos/GyanD/codexffmpeg/releases?per_page=30' -Headers @{ 'User-Agent' = 'STEFFLIX-release' }
        foreach ($r in $all) { if ($r.tag_name -notmatch 'git' -and -not $r.prerelease) { $latest = $r; break } }
    } catch {
        Warn "Neueste ffmpeg-Version nicht abrufbar ($($_.Exception.Message)). ffmpeg.json bleibt unverändert."
    }
    if ($latest) {
        $asset = $latest.assets | Where-Object { $_.name -like '*essentials_build.zip' } | Select-Object -First 1
        if (-not $current -or $current.version -ne $latest.tag_name) {
            Info "Neueste stabile Version: ffmpeg $($latest.tag_name)"
            if (-not $asset -or -not $asset.digest) {
                Warn 'Für diese Version gibt es keinen essentials-Build mit Prüfsumme. ffmpeg.json bleibt unverändert.'
            } elseif (Ask-YesNo "ffmpeg.json auf $($latest.tag_name) aktualisieren?" (-not $current)) {
                $sha = $asset.digest -replace '^sha256:', ''
                $json = "{`n  `"version`": `"$($latest.tag_name)`",`n  `"url`": `"$($asset.browser_download_url)`",`n  `"sha256`": `"$sha`"`n}`n"
                [IO.File]::WriteAllText($ffmpegJson, $json, $Utf8)
                Good "ffmpeg.json: ffmpeg $($latest.tag_name)"
                $released += "ffmpeg $($latest.tag_name)"
            }
        } else {
            Info 'ffmpeg ist aktuell.'
        }
    }

    # --------------------------------------------------------------
    Step '[4/6] STEFFLIX-Version (version.js und Changelog)'
    # --------------------------------------------------------------
    $versionJs = Join-Path $Root 'sflix_sys\version.js'
    $newsTxt = Join-Path $Root 'sflix_sys\news.txt'
    $vjs = [IO.File]::ReadAllText($versionJs)
    $webVersion = Get-WebVersionName $vjs
    if (-not $webVersion) { Fail 'Version in version.js nicht gefunden.' }

    # Geänderte Dateien der Weboberfläche seit dem letzten Release (checksums.json)
    $webChanged = @()
    foreach ($p in $before.PSObject.Properties) {
        if ($p.Name -like '*.exe' -or $p.Name -eq 'ffmpeg.json') { continue }
        if ($published[$p.Name] -ne $p.Value) { $webChanged += $p.Name }
    }
    Info "Aktuelle Version: $webVersion"
    if ($webChanged.Count -gt 0) {
        Info 'Seit dem letzten Release geänderte Dateien der Weboberfläche:'
        $webChanged | ForEach-Object { Info "  - $_" }
    } else {
        Info 'Die Dateien der Weboberfläche sind seit dem letzten Release unverändert.'
    }

    $bumpWeb = $false
    if ($Auto) { Info 'Automatischer Modus: Version und Changelog bleiben unverändert.' }
    else { $bumpWeb = Ask-YesNo 'STEFFLIX-Version erhöhen und Changelog ergänzen?' ($webChanged.Count -gt 0 -and $webChanged -notcontains 'sflix_sys/version.js') }
    if ($bumpWeb) {
        # Vorschlag: nächste Minor-Version (2.5.0 -> 2.6.0)
        $cur = [version]$webVersion
        $suggestWeb = "$($cur.Major).$($cur.Minor + 1).0"
        Info 'Tipp: 2.5.1 = kleine Korrektur, 2.6.0 = neue Funktionen, 3.0.0 = grosser Umbau (y und z unter 100).'
        $newWeb = Ask-Text 'Neue STEFFLIX-Version' $suggestWeb '^\d+\.\d{1,2}\.\d{1,2}$' 'z.B. 2.6.0'
        $newWeb = Short ([version]$newWeb)
        if ([version]$newWeb -le $cur) {
            Warn "$newWeb ist nicht höher als $webVersion."
            if (-not (Ask-YesNo 'Trotzdem fortfahren?' $false)) { Fail 'Abgebrochen.' }
        }

        # Changelog-Einträge sammeln
        $types = [ordered]@{
            'f' = @('FEATURE', '<span style="color: red">FEATURE</span>')
            'b' = @('BUGFIX', '<span style="color: darkgreen">BUGFIX</span>')
            'o' = @('OPTIMIERUNG', 'OPTIMIERUNG')
            'i' = @('INTERFACE', '<span style="color: blue">INTERFACE</span>')
            'h' = @('HOTFIX', '<span style="color: darkorange">HOTFIX</span>')
        }
        $entries = @()
        if (-not $Auto) {
            Info 'Changelog-Einträge eingeben, ein Eintrag pro Zeile. Leere Zeile = fertig.'
            Info 'Typ als Kürzel davor: f: FEATURE  b: BUGFIX  o: OPTIMIERUNG  i: INTERFACE  h: HOTFIX'
            Info 'Beispiel:  b: Vorschaubilder werden wieder angezeigt.'
            while ($true) {
                $line = ([string](Read-Host '   Eintrag')).Trim()
                if ($line -eq '') { break }
                $tm = [regex]::Match($line, '^([a-zA-Z])\s*:\s*(.+)$')
                if (-not $tm.Success -or -not $types.Contains($tm.Groups[1].Value.ToLower())) {
                    Warn 'Bitte mit Typ-Kürzel beginnen, z.B. "f: Neue Funktion ..."'
                    continue
                }
                $t = $types[$tm.Groups[1].Value.ToLower()]
                $entries += "    <li>[$($t[1])] $(Html $tm.Groups[2].Value.Trim())</li>"
                Good "  + [$($t[0])] $($tm.Groups[2].Value.Trim())"
            }
        }
        if ($entries.Count -eq 0) {
            Warn 'Kein Changelog-Eintrag eingegeben.'
            if ($Auto -or -not (Ask-YesNo 'Version trotzdem ohne Einträge erhöhen?' $false)) { Fail 'Abgebrochen: ohne Changelog-Eintrag wird die Version nicht erhöht.' }
        }

        # news.txt (CRLF wie in der Datei): Gibt es schon einen Block für diese Version, kommen die
        # Einträge an dessen Ende. Sonst wird ein neuer Block vor der bisher obersten Version eingefügt.
        $news = [IO.File]::ReadAllText($newsTxt)
        $nl = if ($news -match "`r`n") { "`r`n" } else { "`n" }
        $pos = $news.IndexOf('<b>Version ')
        if ($pos -lt 0) { Fail 'In news.txt wurde kein Eintrag "<b>Version ...</b>" gefunden.' }
        $lines = ($entries | ForEach-Object { $_ + $nl }) -join ''
        $existing = [regex]::Match($news, '<b>Version\s+' + [regex]::Escape($newWeb) + '\s*</b>')
        if ($existing.Success) {
            $end = $news.IndexOf('</ul>', $existing.Index)
            if ($end -lt 0) { Fail "Der Block für Version $newWeb in news.txt hat kein </ul>." }
            if ($entries.Count -gt 0) { [IO.File]::WriteAllText($newsTxt, $news.Insert($end, $lines), $Utf8) }
            Info "news.txt hat schon einen Block für $newWeb, die Einträge wurden dort angehängt."
        } else {
            $block = "<b>Version $newWeb</b>:<br>$nl<ul>$nl" + $lines + "</ul>$nl"
            [IO.File]::WriteAllText($newsTxt, $news.Insert($pos, $block), $Utf8)
        }

        # version.js: alte Zahl zuerst (für Installationen bis 2.04), dann x.y.z
        [IO.File]::WriteAllText($versionJs, (Get-VersionJsText $newWeb), $Utf8)
        Good "STEFFLIX-Version $newWeb, $($entries.Count) Changelog-Eintrag/Einträge"
        $released += "STEFFLIX $newWeb"
    } else {
        Info 'Version bleibt unverändert.'
    }

    # --------------------------------------------------------------
    Step '[5/6] Prüfen, news.json und checksums.json erzeugen'
    # --------------------------------------------------------------
    Run-Python 'check_release.py'
    # Ändert sich news.json beim Erzeugen, wurde der Changelog (news.txt) bearbeitet
    $newsJson = Join-Path $Root 'sflix_sys\news.json'
    $newsBefore = if (Test-Path $newsJson) { (Get-FileHash $newsJson).Hash } else { '' }
    Run-Python 'create_news.py'
    $newsChanged = $newsBefore -ne (Get-FileHash $newsJson).Hash
    Run-Python 'create_checksums.py'

    # versions.json: neueste Versionen von Updater und ffmpeg für die Hilfe und den Update-Hinweis
    # in STEFFLIX (wird nur von GitHub gelesen, der Updater lädt sie nicht herunter)
    $versionsJson = Join-Path $Root 'versions.json'
    $versionsBefore = if (Test-Path $versionsJson) { [IO.File]::ReadAllText($versionsJson, $Utf8) } else { '' }
    $exeVersion = Get-FileVersion (Join-Path $Root 'updateStefflix.exe')
    $updaterVersion = if ($exeVersion) { Short $exeVersion } else { '' }
    $ffmpegVersion = ''
    $ffmpegManifest = Join-Path $Root 'ffmpeg.json'
    if (Test-Path $ffmpegManifest) { $ffmpegVersion = [string](Get-Content $ffmpegManifest -Raw -Encoding UTF8 | ConvertFrom-Json).version }
    # Android-App (ab 1.1.0): Version und SHA256 der STEFFLIX.apk. Die App prüft damit selbst auf
    # Updates und kontrolliert die heruntergeladene Datei vor der Installation.
    $appVersion = ''
    $appSha = ''
    $apkFile = Join-Path $Root 'STEFFLIX.apk'
    if (Test-Path $apkFile) {
        # Version der zuletzt gebauten APK (release_state.json), sonst die im Quellcode
        if (Test-Path $androidStateFile) {
            $appVersion = [string](Get-Content $androidStateFile -Raw -Encoding UTF8 | ConvertFrom-Json).version
        }
        if (-not $appVersion -and (Test-Path $androidGradle)) { $appVersion = Short (Get-AndroidVersion) }
        $appSha = (Get-FileHash $apkFile -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $versionsText = "{`n  `"updater`": `"$updaterVersion`",`n  `"ffmpeg`": `"$ffmpegVersion`""
    if ($appVersion) {
        $versionsText += ",`n  `"android`": {`n    `"version`": `"$appVersion`",`n    `"sha256`": `"$appSha`"`n  }"
    }
    $versionsText += "`n}`n"
    if ($versionsText -ne $versionsBefore) {
        [IO.File]::WriteAllText($versionsJson, $versionsText, $Utf8)
        Good "versions.json: Updater $updaterVersion, ffmpeg $ffmpegVersion$(if ($appVersion) { ", Android-App $appVersion" })"
    }
    $versionsChanged = $versionsText -ne $versionsBefore

    # --------------------------------------------------------------
    Step '[6/6] Übersicht'
    # --------------------------------------------------------------
    $after = Get-Content (Join-Path $Root 'checksums.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $toPush = @()
    foreach ($p in $after.PSObject.Properties) {
        if ($published[$p.Name] -ne $p.Value) { $toPush += $p.Name }
    }
    if ($toPush.Count -gt 0 -or $released.Count -gt 0) { $toPush += 'checksums.json' }
    if ($newsChanged -or $toPush -contains 'sflix_sys/version.js' -or $released -match '^STEFFLIX') {
        $toPush += 'sflix_sys/news.txt'
        $toPush += 'sflix_sys/news.json'
    }
    if ($versionsChanged) { $toPush += 'versions.json' }
    if ($androidReleased) {
        # Nicht in checksums.json (der Updater lädt die App nicht), darum hier ergänzen
        $toPush += 'STEFFLIX.apk'
        $toPush += 'SFLIX Tools/Android/ (Quellcode der App, inkl. release_state.json)'
    }
    $toPush = $toPush | Select-Object -Unique

    Write-Host ''
    Write-Host '==================================================================='
    if ($released.Count -gt 0) { Write-Host " FERTIG - bereitgestellt: $($released -join ', ')" -ForegroundColor Green }
    else { Write-Host ' FERTIG - geprüft, nichts Neues bereitgestellt' -ForegroundColor Green }
    Write-Host '==================================================================='
    if ($toPush.Count -gt 0) {
        Write-Host ' Diese Dateien zusammen pushen (EXE und checksums.json gehören zusammen):'
        $toPush | ForEach-Object { Write-Host "   $_" }
    } else {
        Write-Host ' Gegenüber dem letzten Release hat sich nichts geändert.'
    }
    $exitCode = 0
}
catch {
    if ($programCsBackup) {
        [IO.File]::WriteAllText($programCs, $programCsBackup, $Utf8)
        Warn 'Versionsnummer im Quellcode wurde zurückgesetzt.'
    }
    if ($androidGradleBackup) {
        [IO.File]::WriteAllText($androidGradle, $androidGradleBackup, $Utf8)
        Warn 'Versionsnummer der Android-App wurde zurückgesetzt.'
    }
    Write-Host ''
    Write-Host '===================================================================' -ForegroundColor Red
    Write-Host " ABBRUCH - das Release ist NICHT fertig." -ForegroundColor Red
    Write-Host " $($_.Exception.Message)" -ForegroundColor Red
    Write-Host '===================================================================' -ForegroundColor Red
    $exitCode = 1
}
finally {
    Remove-Item (Join-Path $Scripts '__pycache__') -Recurse -Force -ErrorAction SilentlyContinue
}
exit $exitCode
