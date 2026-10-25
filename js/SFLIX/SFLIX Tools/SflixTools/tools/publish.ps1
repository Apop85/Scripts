# Bereitet eine neue Version im lokalen Klon des GitHub-Repositorys vor.
#
#   powershell -ExecutionPolicy Bypass -File tools\publish.ps1 -Repo "C:\...\Scripts\js\SFLIX"
#
# Was passiert:
#   1. dist\updateStefflix.exe wird ins Repository kopiert.
#   2. ffmpeg.json wird erzeugt: neueste stabile ffmpeg-Version von gyan.dev (GitHub-Release
#      GyanD/codexffmpeg) samt SHA256 aus den Release-Daten. Mit -FfmpegTag eine bestimmte
#      Version wählen, mit -KeepFfmpeg die vorhandene ffmpeg.json unverändert lassen.
#   3. In checksums.json werden die Einträge für updateStefflix.exe und ffmpeg.json
#      aktualisiert. Alle anderen Einträge bleiben, wie sie sind.
# Danach im Repository committen und pushen.
#
# Wichtig: updateData.exe und updater.exe (und ihre Einträge in checksums.json) noch eine
# Weile im Repository lassen. Ältere Installationen laden diese Dateien beim Umstieg.

param(
    [Parameter(Mandatory = $true)][string]$Repo,
    [string]$FfmpegTag = '',
    [switch]$KeepFfmpeg
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$exe = Join-Path $root 'dist\updateStefflix.exe'
if (-not (Test-Path $exe)) { throw "Zuerst bauen: $exe fehlt" }
if (-not (Test-Path (Join-Path $Repo 'checksums.json'))) { throw "Keine checksums.json in $Repo" }
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$utf8 = New-Object Text.UTF8Encoding $false

function Sha256([string]$path) { (Get-FileHash $path -Algorithm SHA256).Hash.ToLower() }

# 1. Programm
Copy-Item $exe (Join-Path $Repo 'updateStefflix.exe') -Force
$version = (Get-Item $exe).VersionInfo.FileVersion
Write-Host "updateStefflix.exe $version kopiert"

# 2. ffmpeg.json
$ffmpegJson = Join-Path $Repo 'ffmpeg.json'
if (-not $KeepFfmpeg) {
    $headers = @{ 'User-Agent' = 'STEFFLIX-publish' }
    if ($FfmpegTag) {
        $release = Invoke-RestMethod "https://api.github.com/repos/GyanD/codexffmpeg/releases/tags/$FfmpegTag" -Headers $headers
    } else {
        # Stabile Versionen heissen z.B. "9.0.2", tägliche Builds enthalten "git"
        $all = Invoke-RestMethod 'https://api.github.com/repos/GyanD/codexffmpeg/releases?per_page=30' -Headers $headers
        $release = $null
        foreach ($r in $all) { if ($r.tag_name -notmatch 'git' -and -not $r.prerelease) { $release = $r; break } }
        if (-not $release) { throw 'Keine stabile ffmpeg-Version gefunden' }
    }
    $asset = $release.assets | Where-Object { $_.name -like '*essentials_build.zip' } | Select-Object -First 1
    if (-not $asset -or -not $asset.digest) { throw "Kein essentials-Build mit Prüfsumme in Release $($release.tag_name)" }
    $sha = $asset.digest -replace '^sha256:', ''
    $json = "{`n  `"version`": `"$($release.tag_name)`",`n  `"url`": `"$($asset.browser_download_url)`",`n  `"sha256`": `"$sha`"`n}`n"
    [IO.File]::WriteAllText($ffmpegJson, $json, $utf8)
    Write-Host "ffmpeg.json: ffmpeg $($release.tag_name)"
}

# 3. checksums.json (Reihenfolge und übrige Einträge bleiben erhalten)
$checksumFile = Join-Path $Repo 'checksums.json'
$sums = Get-Content $checksumFile -Raw -Encoding UTF8 | ConvertFrom-Json
$new = [ordered]@{}
foreach ($p in $sums.PSObject.Properties) { $new[$p.Name] = $p.Value }
$new['updateStefflix.exe'] = Sha256 (Join-Path $Repo 'updateStefflix.exe')
if (Test-Path $ffmpegJson) {
    # Textdatei: Prüfsumme über LF-Zeilenenden (wie in sflix_update.py / Net.Checksum)
    $bytes = [IO.File]::ReadAllBytes($ffmpegJson)
    $text = $utf8.GetString($bytes) -replace "`r`n", "`n"
    $sha = [Security.Cryptography.SHA256]::Create()
    $new['ffmpeg.json'] = (($sha.ComputeHash($utf8.GetBytes($text)) | ForEach-Object { $_.ToString('x2') }) -join '')
}
$lines = @()
foreach ($k in $new.Keys) { $lines += "  `"$k`": `"$($new[$k])`"" }
[IO.File]::WriteAllText($checksumFile, "{`n" + ($lines -join ",`n") + "`n}`n", $utf8)
Write-Host "checksums.json aktualisiert"
Write-Host ""
Write-Host "Jetzt im Repository committen und pushen."
