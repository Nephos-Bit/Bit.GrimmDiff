param(
    [Parameter(Mandatory)]
    [string]$OutputPath,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$components = @(
    @{ type = "application"; name = "GrimmDiff"; version = "1.0.0"; purl = "pkg:generic/grimmdiff@1.0.0" },
    @{ type = "application"; name = "GrimmDiff.Gui"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.gui@1.0.0" },
    @{ type = "library"; name = "GrimmDiff.Gui.Abstracts"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.gui.abstracts@1.0.0" },
    @{ type = "library"; name = "GrimmDiff.Logic"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.logic@1.0.0" },
    @{ type = "library"; name = "GrimmDiff.Logic.Abstracts"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.logic.abstracts@1.0.0" },
    @{ type = "library"; name = "GrimmDiff.Data"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.data@1.0.0" },
    @{ type = "library"; name = "GrimmDiff.Data.Abstracts"; version = "1.0.0"; purl = "pkg:generic/grimmdiff.data.abstracts@1.0.0" },
    @{ type = "framework"; name = ".NET"; version = "11.0"; purl = "pkg:generic/dotnet@11.0"; properties = @(@{ name = "grimmdiff:runtime"; value = $Runtime }) }
)
$bom = [ordered]@{
    bomFormat = "CycloneDX"; specVersion = "1.5"; serialNumber = "urn:uuid:$([guid]::NewGuid())"; version = 1
    metadata = @{ timestamp = [DateTime]::UtcNow.ToString("o"); component = $components[0]; tools = @(@{ vendor = "GrimmDiff"; name = "New-Sbom.ps1"; version = "1.0" }) }
    components = $components[1..($components.Count - 1)]
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
$bom | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $OutputPath -Encoding utf8
