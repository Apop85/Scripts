# checksums.json signieren (DEC-356): schreibt checksums.sig im Release-Ordner.
# Ruft release.ps1 nach create_checksums.py auf; einzeln:
#   powershell -NoProfile -ExecutionPolicy Bypass -File SFLIX_Tools\Release\sign_checksums.ps1
#
# Signatur: PKCS#1 v1.5 mit SHA-256 ueber die Bytes von checksums.json mit CRLF -> LF (wie die
# Pruefsummen der Textdateien), Base64 in einer Zeile, UTF-8 ohne BOM, LF.
# Exit 0 = signiert, 2 = privater Schluessel fehlt, 1 = anderer Fehler.
param(
    [string]$Root = (Join-Path $PSScriptRoot '..\..'),
    [string]$Key = (Join-Path $PSScriptRoot 'signing\update_key.xml')
)
$ErrorActionPreference = 'Stop'

$json = Join-Path $Root 'checksums.json'
$sig = Join-Path $Root 'checksums.sig'
if (-not (Test-Path $Key)) {
    Write-Host "Privater Schluessel fehlt: $Key (zuerst update_key.ps1 ausfuehren bzw. Sicherung zurueckholen)"
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

$rsa = New-Object Security.Cryptography.RSACryptoServiceProvider
try {
    $rsa.PersistKeyInCsp = $false
    $rsa.FromXmlString([IO.File]::ReadAllText((Resolve-Path $Key)))
    if ($rsa.PublicOnly) { Write-Host 'Die Schluesseldatei enthaelt keinen privaten Schluessel'; exit 1 }
    $sha = New-Object Security.Cryptography.SHA256CryptoServiceProvider
    $signature = $rsa.SignData($data, $sha)
    if (-not $rsa.VerifyData($data, $sha, $signature)) { Write-Host 'Signatur laesst sich nicht pruefen'; exit 1 }
} finally { $rsa.Dispose() }

[IO.File]::WriteAllText([IO.Path]::GetFullPath($sig), [Convert]::ToBase64String($signature) + "`n", (New-Object Text.UTF8Encoding($false)))
Write-Host "checksums.sig geschrieben ($($data.Length) Byte signiert)"
exit 0
