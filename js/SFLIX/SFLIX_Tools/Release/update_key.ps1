# Schluessel fuer signierte Updates erzeugen (DEC-356). Fuehrt der Herausgeber einmal aus.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\update_key.ps1
#
# Erzeugt zwei RSA-3072-Schluesselpaare:
#   signing\update_key.xml          Hauptschluessel (privat, signiert jeden Release; nie pushen)
#   signing\update_key_reserve.xml  Reserveschluessel (privat): sofort offline sichern (USB-Stick,
#                                   Passwortmanager) und hier loeschen. Er wird nur gebraucht, wenn der
#                                   Hauptschluessel verloren geht oder kompromittiert ist.
# und schreibt beide oeffentlichen Teile nach SflixTools\src\UpdateKeys.cs (wird gepusht).
# Danach den Desktop neu bauen: erst diese EXE prueft die Signatur.
# Gibt keine privaten Schluessel aus. Vorhandene Schluessel werden nur mit -Force ersetzt.
param([switch]$Force)
$ErrorActionPreference = 'Stop'

$signing = Join-Path $PSScriptRoot 'signing'
$main = Join-Path $signing 'update_key.xml'
$reserve = Join-Path $signing 'update_key_reserve.xml'
$keysCs = Join-Path $PSScriptRoot '..\SflixTools\src\UpdateKeys.cs'
$utf8 = New-Object Text.UTF8Encoding($false)

if ((Test-Path $main) -and -not $Force) {
    Write-Host "Es gibt schon einen Hauptschluessel ($main). Ein neuer Schluessel macht alle bisherigen Programme"
    Write-Host 'update-unfaehig, sobald mit ihm signiert wird. Nur mit -Force ersetzen (Schluesselwechsel: docs\RELEASE.md).'
    exit 1
}
New-Item -ItemType Directory -Force $signing | Out-Null

$publicKeys = @()
foreach ($file in @($main, $reserve)) {
    $rsa = New-Object Security.Cryptography.RSACryptoServiceProvider(3072)
    try {
        $rsa.PersistKeyInCsp = $false
        [IO.File]::WriteAllText($file, $rsa.ToXmlString($true), $utf8)
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
Write-Host "  $main"
Write-Host "  $reserve  <- jetzt offline sichern und hier loeschen"
Write-Host "Oeffentliche Teile in $keysCs eingetragen. Desktop neu bauen."
