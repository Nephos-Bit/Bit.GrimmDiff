param(
    [Parameter(Mandatory)]
    [string]$Path,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+([\-+][0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = "Stop"
$content = Get-Content -LiteralPath $Path -Raw
if ($content -notmatch "\A# GrimmDiff $([regex]::Escape($Version))\r?\n") {
    throw "Release notes must start with '# GrimmDiff $Version'."
}

$sections = [regex]::Matches($content, '(?m)^## (Deutsch|English)\s*$')
if ($sections.Count -ne 2 -or $sections[0].Groups[1].Value -ne 'Deutsch' -or $sections[1].Groups[1].Value -ne 'English') {
    throw "Release notes must contain one Deutsch and one English section in that order."
}

for ($index = 0; $index -lt $sections.Count; $index++) {
    $section = $sections[$index]
    $end = if ($index -eq $sections.Count - 1) { $content.Length } else { $sections[$index + 1].Index }
    $body = $content.Substring($section.Index + $section.Length, $end - ($section.Index + $section.Length))
    $bullets = [regex]::Matches($body, '(?m)^-\s+').Count
    if ($bullets -lt 3 -or $bullets -gt 6) { throw "Each language section must contain 3 to 6 bullet points." }
}
