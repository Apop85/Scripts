# Baut dist\STEFFLIX.exe (STEFFLIX Desktop: Player-Fenster und Tools in einer EXE).
# Benötigt nur .NET Framework 4.x (auf jedem Windows 10/11 vorhanden), kein Visual Studio.
# Das WebView2-SDK (Player-Fenster) wird beim ersten Build von NuGet geladen, mit fester Version
# und Prüfsumme, und in die EXE eingebettet (eine einzige Datei für die Nutzer).
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

# 1. WebView2-SDK (Microsoft.Web.WebView2). Version auch in src\Desktop.cs (WebViewLibs.SdkVersion).
$sdkVersion = '1.0.4258.31'
$sdkSha256  = '56f7f4b8bf9aee4b8efefbbdd4f67d5f74ebd1b100ed0806da71bf76af481aa9'
$sdkDir = Join-Path $obj "webview2\$sdkVersion"
if (-not (Test-Path (Join-Path $sdkDir 'lib\net462\Microsoft.Web.WebView2.Core.dll'))) {
    $pkg = Join-Path $obj "webview2\microsoft.web.webview2.$sdkVersion.nupkg"
    New-Item -ItemType Directory -Force (Split-Path $pkg) | Out-Null
    if (-not (Test-Path $pkg) -or (Get-FileHash $pkg -Algorithm SHA256).Hash.ToLower() -ne $sdkSha256) {
        Write-Host "Lade WebView2-SDK $sdkVersion von NuGet ..."
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
        Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/$sdkVersion/microsoft.web.webview2.$sdkVersion.nupkg" -OutFile $pkg -UseBasicParsing
    }
    $hash = (Get-FileHash $pkg -Algorithm SHA256).Hash.ToLower()
    if ($hash -ne $sdkSha256) {
        Remove-Item $pkg -Force
        throw "WebView2-SDK: Prüfsumme stimmt nicht ($hash)"
    }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path $sdkDir) { Remove-Item $sdkDir -Recurse -Force }
    $zip = [IO.Compression.ZipFile]::OpenRead($pkg)
    try {
        foreach ($e in $zip.Entries) {
            if ($e.FullName -match '^(lib/net462/Microsoft\.Web\.WebView2\.(Core|WinForms)\.dll|build/native/(x64|x86|arm64)/WebView2Loader\.dll|LICENSE\.txt)$') {
                $target = Join-Path $sdkDir ($e.FullName -replace '/', '\')
                New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
                [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $target, $true)
            }
        }
    } finally { $zip.Dispose() }
}
$wvCore     = Join-Path $sdkDir 'lib\net462\Microsoft.Web.WebView2.Core.dll'
$wvWinForms = Join-Path $sdkDir 'lib\net462\Microsoft.Web.WebView2.WinForms.dll'

# 2. Icon und Kopfbild-Vorschau aus dem Logo erzeugen
& $csc /nologo /codepage:65001 /target:exe "/out:$obj\AssetGen.exe" /reference:System.Drawing.dll `
    "$PSScriptRoot\tools\AssetGen.cs" "$PSScriptRoot\src\Branding.cs"
if ($LASTEXITCODE -ne 0) { throw 'AssetGen konnte nicht gebaut werden' }
& "$obj\AssetGen.exe" $obj "v$version"
if ($LASTEXITCODE -ne 0) { throw 'Icon konnte nicht erzeugt werden' }

# 3. Programm bauen (WebView2-Bibliotheken als Ressourcen, siehe WebViewLibs in Desktop.cs)
$sources = Get-ChildItem "$PSScriptRoot\src\*.cs" | ForEach-Object { $_.FullName }
& $csc /nologo /codepage:65001 /optimize+ /platform:anycpu /target:winexe `
    "/out:$dist\STEFFLIX.exe" `
    "/win32icon:$obj\app.ico" `
    "/resource:$obj\app.ico,app.ico" `
    "/resource:$wvCore,webview2.Microsoft.Web.WebView2.Core.dll" `
    "/resource:$wvWinForms,webview2.Microsoft.Web.WebView2.WinForms.dll" `
    "/resource:$sdkDir\build\native\x64\WebView2Loader.dll,webview2.x64.WebView2Loader.dll" `
    "/resource:$sdkDir\build\native\x86\WebView2Loader.dll,webview2.x86.WebView2Loader.dll" `
    "/resource:$sdkDir\build\native\arm64\WebView2Loader.dll,webview2.arm64.WebView2Loader.dll" `
    /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll /reference:System.IO.Compression.dll `
    /reference:System.IO.Compression.FileSystem.dll `
    "/reference:$wvCore" "/reference:$wvWinForms" `
    $sources
if ($LASTEXITCODE -ne 0) { throw 'Build fehlgeschlagen' }

$hash = (Get-FileHash "$dist\STEFFLIX.exe" -Algorithm SHA256).Hash.ToLower()
Write-Host "Gebaut: $dist\STEFFLIX.exe  (Version $version)"
Write-Host "SHA256: $hash"
