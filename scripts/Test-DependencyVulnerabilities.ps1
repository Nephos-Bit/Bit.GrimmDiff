$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$result = dotnet list (Join-Path $root "GrimmDiff.sln") package --vulnerable --include-transitive --format json | ConvertFrom-Json
$vulnerabilities = [System.Collections.Generic.List[object]]::new()

function Find-Vulnerabilities($value) {
    if ($null -eq $value) { return }
    if ($value -is [System.Collections.IEnumerable] -and $value -isnot [string]) {
        foreach ($item in $value) { Find-Vulnerabilities $item }
        return
    }
    foreach ($property in $value.PSObject.Properties) {
        if ($property.Name -eq "vulnerabilities" -and $property.Value.Count -gt 0) {
            $vulnerabilities.Add($property.Value)
        }
        Find-Vulnerabilities $property.Value
    }
}

Find-Vulnerabilities $result
if ($vulnerabilities.Count -gt 0) { throw "Dependency vulnerability scan found $($vulnerabilities.Count) vulnerable package entries." }
Write-Output "Dependency vulnerability scan found no vulnerable packages."
