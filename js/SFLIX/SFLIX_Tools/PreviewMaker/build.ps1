# Builds dist\PreviewMaker.exe with ffmpeg.exe embedded.
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
New-Item -ItemType Directory -Force "$PSScriptRoot\dist" | Out-Null
& $csc /nologo /optimize+ /platform:x64 /target:exe `
    /out:"$PSScriptRoot\dist\PreviewMaker.exe" `
    /reference:System.Drawing.dll `
    /resource:"$PSScriptRoot\tools\ffmpeg\ffmpeg.exe,ffmpeg.exe" `
    "$PSScriptRoot\src\PreviewMaker.cs"
