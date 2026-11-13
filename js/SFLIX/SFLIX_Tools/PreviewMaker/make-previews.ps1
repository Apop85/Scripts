# Creates a 480x270 JPEG (quality 80) preview from the middle of every video.
# Output: <video folder>\Preview\<video name>.jpg  -- existing previews are skipped.
# Usage:  powershell -ExecutionPolicy Bypass -File make-previews.ps1 [-Root F:\]

param(
    [string]$Root = 'F:\',
    [string[]]$Extensions = @('.mp4', '.m4v', '.webm', '.ogv'),
    [int]$Width = 480,
    [int]$Height = 270,
    [long]$Quality = 80
)

$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.Drawing

$ffmpeg  = Join-Path $PSScriptRoot 'tools\ffmpeg\ffmpeg.exe'
$ffprobe = Join-Path $PSScriptRoot 'tools\ffmpeg\ffprobe.exe'
$logFile = Join-Path $PSScriptRoot 'make-previews.log'
$tmpBmp  = Join-Path $env:TEMP 'preview-frame.bmp'

$jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$encParams = New-Object System.Drawing.Imaging.EncoderParameters 1
$encParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality, $Quality)

"=== Start $(Get-Date -Format s), root $Root ===" | Out-File $logFile -Append -Encoding utf8

$created = 0; $skipped = 0; $failed = 0

$videos = Get-ChildItem -LiteralPath $Root -Recurse -File -Force -ErrorAction SilentlyContinue |
    Where-Object { $Extensions -contains $_.Extension.ToLower() -and
                   $_.FullName -notmatch '\\(\$RECYCLE\.BIN|System Volume Information)\\' }

foreach ($v in $videos) {
    $previewDir = Join-Path $v.DirectoryName 'Preview'
    $target = Join-Path $previewDir ($v.BaseName + '.jpg')
    if (Test-Path -LiteralPath $target) { $skipped++; continue }

    try {
        # Duration in seconds; fall back to the start if unknown.
        $dur = & $ffprobe -v error -show_entries format=duration -of default=nw=1:nk=1 "$($v.FullName)"
        $mid = 0.0
        if ([double]::TryParse(($dur | Select-Object -First 1), [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$mid)) { $mid = $mid / 2 } else { $mid = 0 }
        $ss = $mid.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)

        # Fill 480x270 and crop the overflow (centered).
        $vf = "scale=${Width}:${Height}:force_original_aspect_ratio=increase,crop=${Width}:${Height},setsar=1"
        if (Test-Path $tmpBmp) { Remove-Item $tmpBmp -Force }
        & $ffmpeg -v error -y -ss $ss -i "$($v.FullName)" -frames:v 1 -vf $vf $tmpBmp
        if (-not (Test-Path $tmpBmp)) {
            # Seek can fail near the end of broken files; retry from the start.
            & $ffmpeg -v error -y -i "$($v.FullName)" -frames:v 1 -vf $vf $tmpBmp
        }
        if (-not (Test-Path $tmpBmp)) { throw 'ffmpeg produced no frame' }

        if (-not (Test-Path -LiteralPath $previewDir)) { New-Item -ItemType Directory -Path $previewDir | Out-Null }
        $img = [System.Drawing.Image]::FromFile($tmpBmp)
        try { $img.Save($target, $jpegCodec, $encParams) } finally { $img.Dispose() }

        $created++
        Write-Host "OK    $($v.FullName)"
    } catch {
        $failed++
        $msg = "FAIL  $($v.FullName) :: $($_.Exception.Message)"
        Write-Host $msg
        $msg | Out-File $logFile -Append -Encoding utf8
    }
}

if (Test-Path $tmpBmp) { Remove-Item $tmpBmp -Force }
$summary = "Done. Created: $created  Skipped (already existed): $skipped  Failed: $failed"
Write-Host $summary
$summary | Out-File $logFile -Append -Encoding utf8
