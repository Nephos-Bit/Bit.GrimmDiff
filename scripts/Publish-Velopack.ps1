param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+([\-+][0-9A-Za-z.-]+)?$')]
    [string]$Version,
    [string]$Runtime = "win-x64",
    [string]$VpkPath = "vpk",
    [string]$CodeSigningCertificateThumbprint
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root "artifacts\velopack-publish-$Runtime"
$releases = Join-Path $root "artifacts\Releases"
$application = Join-Path $publish "GrimmDiff.App.exe"
$icon = Join-Path $root "src\GrimmDiff.App\Assets\AppIcon.ico"
$repository = "https://github.com/Nephos-Bit/Bit.GrimmDiff"

Remove-Item -LiteralPath $publish, $releases -Recurse -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "src\GrimmDiff.App\GrimmDiff.App.csproj") -c Release -r $Runtime --self-contained true -p:PublishSingleFile=false -p:Version=$Version -p:ContinuousIntegrationBuild=true -o $publish

function Sign-File([string]$Path, [string]$Thumbprint) {
    $certificate = Get-ChildItem "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction Stop
    if (-not $certificate.HasPrivateKey) { throw "The selected code-signing certificate has no private key." }
    Set-AuthenticodeSignature -FilePath $Path -Certificate $certificate -TimestampServer "http://timestamp.digicert.com" | Out-Null
    if ((Get-AuthenticodeSignature -FilePath $Path).Status -ne "Valid") { throw "Code-signature verification failed for $Path." }
}

$isSigned = -not [string]::IsNullOrWhiteSpace($CodeSigningCertificateThumbprint)
if ($isSigned) { Sign-File $application $CodeSigningCertificateThumbprint }

New-Item -ItemType Directory -Force -Path $releases | Out-Null
try {
    & $VpkPath download github --repoUrl $repository --outputDir $releases
    if ($LASTEXITCODE -ne 0) { throw "No previous Velopack release is available." }
} catch {
    Write-Warning "Creating the first release without delta packages: $($_.Exception.Message)"
}

$packArguments = @(
    "pack", "--packId", "NephosBit.GrimmDiff", "--packVersion", $Version,
    "--packDir", $publish, "--mainExe", "GrimmDiff.App.exe", "--packTitle", "GrimmDiff",
    "--icon", $icon, "--outputDir", $releases, "--channel", "win", "--runtime", $Runtime
)
& $VpkPath @packArguments
if ($LASTEXITCODE -ne 0) { throw "Velopack packaging failed." }

if ($isSigned) {
    Get-ChildItem -LiteralPath $releases -Filter "*-Setup.exe" | ForEach-Object {
        Sign-File $_.FullName $CodeSigningCertificateThumbprint
    }
}

& (Join-Path $PSScriptRoot "New-Sbom.ps1") -OutputPath (Join-Path $releases "GrimmDiff-$Runtime.sbom.cdx.json") -Runtime $Runtime
Write-Output "Created Velopack release assets in $releases"
