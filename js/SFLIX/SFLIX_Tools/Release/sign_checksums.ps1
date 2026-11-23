# checksums.json signieren (DEC-356): schreibt checksums.sig im Release-Ordner.
# Ruft release.ps1 nach create_checksums.py auf; einzeln:
#   powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\sign_checksums.ps1
#
# Signatur: PKCS#1 v1.5 mit SHA-256 ueber die Bytes von checksums.json mit CRLF -> LF (wie die
# Pruefsummen der Textdateien), Base64 in einer Zeile, UTF-8 ohne BOM, LF.
#
# Schluessel (DEC-371): signing\update_key.dpapi, mit Windows-DPAPI (CurrentUser) verschluesselt;
# wird nur im Speicher entschluesselt. Anlegen: update_key.ps1 -Schuetzen. Eine Datei *.xml ueber
# -Key wird als unverschluesselter Schluessel gelesen (nur fuer Test-Schluessel).
# Exit 0 = signiert, 2 = privater Schluessel fehlt oder laesst sich nicht entschluesseln, 1 = anderer Fehler.
param(
    [string]$Root = (Join-Path $PSScriptRoot '..\..'),
    [string]$Key = (Join-Path $PSScriptRoot 'signing\update_key.dpapi')
)
$ErrorActionPreference = 'Stop'

$json = Join-Path $Root 'checksums.json'
$sig = Join-Path $Root 'checksums.sig'
if (-not (Test-Path $Key)) {
    $plain = [IO.Path]::ChangeExtension($Key, '.xml')
    if ($Key -like '*.dpapi' -and (Test-Path $plain)) {
        Write-Host "Verschluesselter Schluessel fehlt: $Key"
        Write-Host 'Es liegt noch der unverschluesselte Schluessel da: zuerst update_key.ps1 -Schuetzen ausfuehren (docs\RELEASE.md, Signierte Updates).'
    } else {
        Write-Host "Privater Schluessel fehlt: $Key (zuerst update_key.ps1 ausfuehren bzw. Sicherung zurueckholen und mit update_key.ps1 -Schuetzen ablegen)"
    }
    exit 2
}
if (-not (Test-Path $json)) { Write-Host "checksums.json fehlt: $json"; exit 1 }

$bytes = [IO.File]::ReadAllBytes((Resolve-Path $json))
$out = New-Object IO.MemoryStream
for ($i = 0; $i -lt $bytes.Length; $i++) {
    if ($bytes[$i] -eq 13 -and $i + 1 -lt $bytes.Length -and $bytes[$i + 1] -eq 10) { continue }
    $out.WriteByte($bytes[$i])
}
$data = $out.ToArray()

$keyPath = (Resolve-Path $Key).Path
if ($keyPath -like '*.xml') {
    $keyXml = [IO.File]::ReadAllText($keyPath)
} else {
    $keyBytes = $null
    try {
        Add-Type -AssemblyName System.Security
        $entropy = [Text.Encoding]::ASCII.GetBytes('STEFFLIX-update-key')
        $keyBytes = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath), $entropy, [Security.Cryptography.DataProtectionScope]::CurrentUser)
        $keyXml = [Text.Encoding]::UTF8.GetString($keyBytes)
    } catch {
        Write-Host "Der Schluessel laesst sich nicht entschluesseln: $keyPath"
        Write-Host 'Er ist nur fuer das Windows-Konto lesbar, das ihn abgelegt hat. Auf einem anderen Konto oder Rechner die Offline-Sicherung mit update_key.ps1 -Schuetzen neu ablegen.'
        exit 2
    } finally {
        if ($keyBytes) { [Array]::Clear($keyBytes, 0, $keyBytes.Length) }
    }
}

$rsa = New-Object Security.Cryptography.RSACryptoServiceProvider
try {
    $rsa.PersistKeyInCsp = $false
    $rsa.FromXmlString($keyXml)
    $keyXml = $null
    if ($rsa.PublicOnly) { Write-Host 'Die Schluesseldatei enthaelt keinen privaten Schluessel'; exit 1 }
    $sha = New-Object Security.Cryptography.SHA256CryptoServiceProvider
    $signature = $rsa.SignData($data, $sha)
    if (-not $rsa.VerifyData($data, $sha, $signature)) { Write-Host 'Signatur laesst sich nicht pruefen'; exit 1 }
} finally { $rsa.Dispose() }

[IO.File]::WriteAllText([IO.Path]::GetFullPath($sig), [Convert]::ToBase64String($signature) + "`n", (New-Object Text.UTF8Encoding($false)))
Write-Host "checksums.sig geschrieben ($($data.Length) Byte signiert)"
exit 0
