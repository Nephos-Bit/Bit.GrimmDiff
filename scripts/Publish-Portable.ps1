param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $root "artifacts\GrimmDiff-$Runtime"
$archive = Join-Path $root "artifacts\GrimmDiff-$Runtime.zip"

Remove-Item -LiteralPath $publish -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $archive -Force -ErrorAction SilentlyContinue
dotnet publish (Join-Path $root "src\GrimmDiff.App\GrimmDiff.App.csproj") -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -o $publish
Compress-Archive -Path (Join-Path $publish "*") -DestinationPath $archive
Write-Output "Created $archive"
