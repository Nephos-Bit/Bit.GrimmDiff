param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$CodeSigningCertificateThumbprint
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root "artifacts\GrimmDiff-$Runtime"
$archive = Join-Path $root "artifacts\GrimmDiff-$Runtime.zip"
$checksum = "$archive.sha256"
$sbom = Join-Path $root "artifacts\GrimmDiff-$Runtime.sbom.cdx.json"

Remove-Item -LiteralPath $publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $archive, $checksum, $sbom -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "src\GrimmDiff.Gui\GrimmDiff.Gui.csproj") -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:ContinuousIntegrationBuild=true -o $publish

$application = Join-Path $publish "GrimmDiff.exe"
$isSigned = -not [string]::IsNullOrWhiteSpace($CodeSigningCertificateThumbprint)
if ($isSigned) {
    $certificate = Get-ChildItem "Cert:\CurrentUser\My\$CodeSigningCertificateThumbprint" -ErrorAction Stop
    if (-not $certificate.HasPrivateKey) { throw "The selected code-signing certificate has no private key." }
    Set-AuthenticodeSignature -FilePath $application -Certificate $certificate -TimestampServer "http://timestamp.digicert.com" | Out-Null
    if ((Get-AuthenticodeSignature -FilePath $application).Status -ne "Valid") { throw "Code-signature verification failed." }
}

& (Join-Path $PSScriptRoot "New-Sbom.ps1") -OutputPath $sbom -Runtime $Runtime
Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $archive
(Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash | Set-Content -LiteralPath $checksum -NoNewline
if ($isSigned) {
    Write-Output "Created signed release: $archive"
} else {
    Write-Output "Created unsigned release: $archive"
}
Write-Output "Created checksum: $checksum"
Write-Output "Created SBOM: $sbom"
