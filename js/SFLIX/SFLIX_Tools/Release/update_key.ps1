# Schluessel fuer signierte Updates erzeugen (DEC-356) bzw. geschuetzt ablegen (DEC-371).
# Fuehrt der Herausgeber aus, nie eine Rolle.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\update_key.ps1
#       Erzeugt zwei RSA-3072-Schluesselpaare:
#         signing\update_key.dpapi        Hauptschluessel, mit Windows-DPAPI (CurrentUser) verschluesselt;
#                                         signiert jeden Release (sign_checksums.ps1). Nie pushen.
#         signing\update_key.xml          derselbe Hauptschluessel unverschluesselt, nur fuer die
#                                         Offline-Sicherung: sichern, dann mit -Schuetzen -SicherungVorhanden loeschen.
#         signing\update_key_reserve.xml  Reserveschluessel (privat): sofort offline sichern (USB-Stick,
#                                         Passwortmanager) und hier loeschen. Er wird nur gebraucht, wenn der
#                                         Hauptschluessel verloren geht oder kompromittiert ist.
#       und schreibt beide oeffentlichen Teile nach SflixTools\src\UpdateKeys.cs (wird gepusht).
#       Danach den Desktop neu bauen: erst diese EXE prueft die Signatur.
#       Vorhandene Schluessel werden nur mit -Force ersetzt.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\update_key.ps1 -Schuetzen [-SicherungVorhanden]
#       Verschluesselt signing\update_key.xml nach signing\update_key.dpapi (Umstieg, neues Windows-Konto,
#       zurueckgeholte Sicherung), entschluesselt zur Probe, signiert und prueft damit. Erst mit
#       -SicherungVorhanden (die Offline-Sicherung ist vorhanden) wird update_key.xml danach geloescht.
#
# Gibt keine privaten Schluessel aus.
param(
    [switch]$Force,
    [switch]$Schuetzen,
    [switch]$SicherungVorhanden,
    [string]$Signing = (Join-Path $PSScriptRoot 'signing')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security

$main = Join-Path $Signing 'update_key.xml'
$protected = Join-Path $Signing 'update_key.dpapi'
$reserve = Join-Path $Signing 'update_key_reserve.xml'
$keysCs = Join-Path $PSScriptRoot '..\SflixTools\src\UpdateKeys.cs'
$utf8 = New-Object Text.UTF8Encoding($false)
$entropy = [Text.Encoding]::ASCII.GetBytes('STEFFLIX-update-key')
$scope = [Security.Cryptography.DataProtectionScope]::CurrentUser

# Schreibt den XML-Schluessel verschluesselt nach $protected (ueber eine Zwischendatei) und prueft:
# entschluesseln, signieren, mit dem oeffentlichen Teil des Originals pruefen. Ergebnis: $true/$false.
$writeProtected = {
    param([string]$xml)
    $plainBytes = $utf8.GetBytes($xml)
    $tmp = $protected + '.neu'
    try {
        [IO.File]::WriteAllBytes($tmp, [Security.Cryptography.ProtectedData]::Protect($plainBytes, $entropy, $scope))
    } finally { [Array]::Clear($plainBytes, 0, $plainBytes.Length) }

    $ok = $false
    $back = $null
    $orig = New-Object Security.Cryptography.RSACryptoServiceProvider
    $test = New-Object Security.Cryptography.RSACryptoServiceProvider
    try {
        $orig.PersistKeyInCsp = $false
        $test.PersistKeyInCsp = $false
        $orig.FromXmlString($xml)
        $back = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($tmp), $entropy, $scope)
        $test.FromXmlString($utf8.GetString($back))
        $probe = [Text.Encoding]::ASCII.GetBytes('STEFFLIX Probe ' + [DateTime]::UtcNow.Ticks)
        $sha = New-Object Security.Cryptography.SHA256CryptoServiceProvider
        $signature = $test.SignData($probe, $sha)
        $ok = (-not $test.PublicOnly) -and $orig.VerifyData($probe, $sha, $signature) -and ($test.ToXmlString($false) -eq $orig.ToXmlString($false))
    } finally {
        if ($back) { [Array]::Clear($back, 0, $back.Length) }
        $orig.Dispose(); $test.Dispose()
    }
    if (-not $ok) {
        Remove-Item -LiteralPath $tmp -Force
        return $false
    }
    Move-Item -LiteralPath $tmp -Destination $protected -Force
    return $true
}

if ($Schuetzen) {
    if (-not (Test-Path $main)) {
        if (Test-Path $protected) { Write-Host "Der Schluessel ist schon geschuetzt abgelegt ($protected); update_key.xml liegt nicht mehr da." ; exit 0 }
        Write-Host "Unverschluesselter Schluessel fehlt: $main (Offline-Sicherung dorthin kopieren)"
        exit 2
    }
    $xml = [IO.File]::ReadAllText((Resolve-Path $main).Path)
    $check = New-Object Security.Cryptography.RSACryptoServiceProvider
    try {
        $check.PersistKeyInCsp = $false
        $check.FromXmlString($xml)
        if ($check.PublicOnly) { Write-Host "$main enthaelt keinen privaten Schluessel"; exit 1 }
        $publicXml = $check.ToXmlString($false)
    } finally { $check.Dispose() }

    if (-not (& $writeProtected $xml)) {
        $xml = $null
        Write-Host 'Probe fehlgeschlagen: Der verschluesselte Schluessel liefert nicht dieselbe Signatur. Nichts geaendert.'
        exit 1
    }
    $xml = $null
    Write-Host "Geschuetzt abgelegt und geprueft: $protected"
    if ((Test-Path $keysCs) -and -not ([IO.File]::ReadAllText((Resolve-Path $keysCs).Path).Contains($publicXml))) {
        Write-Host 'WARNUNG: Der oeffentliche Teil dieses Schluessels steht nicht in SflixTools\src\UpdateKeys.cs.'
    }
    if ($SicherungVorhanden) {
        Remove-Item -LiteralPath $main -Force
        Write-Host "Unverschluesselter Schluessel geloescht: $main"
    } else {
        Write-Host "$main liegt noch unverschluesselt da. Ist er offline gesichert (USB-Stick, Passwortmanager),"
        Write-Host 'mit -Schuetzen -SicherungVorhanden erneut ausfuehren: dann wird er hier geloescht.'
    }
    if (Test-Path $reserve) { Write-Host "Hinweis: $reserve gehoert offline gesichert und hier geloescht." }
    exit 0
}

if (((Test-Path $main) -or (Test-Path $protected)) -and -not $Force) {
    Write-Host "Es gibt schon einen Hauptschluessel (in $Signing). Ein neuer Schluessel macht alle bisherigen Programme"
    Write-Host 'update-unfaehig, sobald mit ihm signiert wird. Nur mit -Force ersetzen (Schluesselwechsel: docs\RELEASE.md).'
    exit 1
}
New-Item -ItemType Directory -Force $Signing | Out-Null

$publicKeys = @()
foreach ($file in @($main, $reserve)) {
    $rsa = New-Object Security.Cryptography.RSACryptoServiceProvider(3072)
    try {
        $rsa.PersistKeyInCsp = $false
        $xml = $rsa.ToXmlString($true)
        [IO.File]::WriteAllText($file, $xml, $utf8)
        if ($file -eq $main -and -not (& $writeProtected $xml)) { Write-Host 'Der Hauptschluessel liess sich nicht geschuetzt ablegen.'; exit 1 }
        $xml = $null
        $publicKeys += $rsa.ToXmlString($false)
    } finally { $rsa.Dispose() }
}

$lines = New-Object Collections.Generic.List[string]
$lines.Add('// ' + [char]0xD6 + 'ffentliche Schl' + [char]0xFC + 'ssel f' + [char]0xFC + 'r signierte Updates (DEC-356, seit Desktop 4.1.0).')
$lines.Add('//')
$lines.Add('// Geschrieben von SFLIX_Tools\Release\update_key.ps1 am ' + (Get-Date -Format 'dd.MM.yyyy') + ': Hauptschl' + [char]0xFC + 'ssel, Reserveschl' + [char]0xFC + 'ssel.')
$lines.Add('// Nur die ' + [char]0xF6 + 'ffentlichen Teile; die privaten Schl' + [char]0xFC + 'ssel liegen in SFLIX_Tools\Release\signing\ (nie pushen) bzw. offline.')
$lines.Add('')
$lines.Add('namespace Sflix')
$lines.Add('{')
$lines.Add('    static class UpdateKeys')
$lines.Add('    {')
$lines.Add('        public static readonly string[] PublicKeys = {')
foreach ($k in $publicKeys) { $lines.Add('            "' + $k + '",') }
$lines.Add('        };')
$lines.Add('    }')
$lines.Add('}')
[IO.File]::WriteAllText((Resolve-Path $keysCs), (($lines -join "`r`n") + "`r`n"), $utf8)

Write-Host 'Schluessel erzeugt:'
Write-Host "  $protected  (verschluesselt, signiert die Releases)"
Write-Host "  $main  <- offline sichern, dann update_key.ps1 -Schuetzen -SicherungVorhanden (loescht ihn hier)"
Write-Host "  $reserve  <- jetzt offline sichern und hier loeschen"
Write-Host "Oeffentliche Teile in $keysCs eingetragen. Desktop neu bauen."
