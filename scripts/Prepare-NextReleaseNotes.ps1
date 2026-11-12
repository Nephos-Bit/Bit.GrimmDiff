param(
    [Parameter(Mandatory)]
    [string]$Path,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$ReleasedVersion
)

$ErrorActionPreference = "Stop"
$parts = $ReleasedVersion.Split('.')
$nextVersion = "$($parts[0]).$($parts[1]).$([int]$parts[2] + 1)"
$content = "# GrimmDiff $nextVersion`n`n## Deutsch`n`n## English`n"

Set-Content -LiteralPath $Path -Value $content -NoNewline -Encoding utf8
Write-Output "Prepared release notes for $nextVersion."
