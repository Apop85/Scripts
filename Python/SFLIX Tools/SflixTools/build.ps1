# Baut dist\updateStefflix.exe (STEFFLIX Updater).
# Benötigt nur .NET Framework 4.x (auf jedem Windows 10/11 vorhanden), kein Visual Studio.
#
# Aufruf:  powershell -ExecutionPolicy Bypass -File build.ps1
#
# Version erhöhen: in src\Program.cs AssemblyVersion und AssemblyFileVersion anpassen.
# Das Selbst-Update installiert nur Versionen, die höher sind als die laufende.

$ErrorActionPreference = 'Stop'
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }

$obj  = Join-Path $PSScriptRoot 'obj'
$dist = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $obj, $dist | Out-Null

$version = (Select-String -Path "$PSScriptRoot\src\Program.cs" -Pattern 'AssemblyFileVersion\("(\d+\.\d+\.\d+)').Matches[0].Groups[1].Value

# 1. Icon und Kopfbild-Vorschau aus dem Logo erzeugen
& $csc /nologo /codepage:65001 /target:exe "/out:$obj\AssetGen.exe" /reference:System.Drawing.dll `
    "$PSScriptRoot\tools\AssetGen.cs" "$PSScriptRoot\src\Branding.cs"
if ($LASTEXITCODE -ne 0) { throw 'AssetGen konnte nicht gebaut werden' }
& "$obj\AssetGen.exe" $obj "v$version"
if ($LASTEXITCODE -ne 0) { throw 'Icon konnte nicht erzeugt werden' }

# 2. Programm bauen
$sources = Get-ChildItem "$PSScriptRoot\src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /codepage:65001 /optimize+ /platform:anycpu /target:winexe `
    "/out:$dist\updateStefflix.exe" `
    "/win32icon:$obj\app.ico" `
    "/resource:$obj\app.ico,app.ico" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    $sources
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen' }

$hash = (Get-FileHash "$dist\updateStefflix.exe" -Algorithm SHA256).Hash.ToLower()
Write-Host "Gebaut: $dist\updateStefflix.exe  (Version $version)"
Write-Host "SHA256: $hash"
