[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$CandidateCommit = 'qinglan-demo-g3.6-rc2',
    [string]$CandidateTag = 'qinglan-demo-g3.6-rc2',
    [string]$EvidenceRoot = 'Docs/DemoDevelopment/Assets/G3.6/Final',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/External/manual-review.json',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
function Relative-ProjectPath([string]$Value) {
    $rootWithSeparator = $projectRoot.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $rootUri = [Uri]::new($rootWithSeparator)
    $valueUri = [Uri]::new((Resolve-Path -LiteralPath $Value).Path)
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($valueUri).ToString()).Replace('\', '/')
}
function Evidence([string]$Value) {
    $path = Resolve-ProjectPath $Value
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Review evidence is missing: $Value" }
    return [ordered]@{
        path = Relative-ProjectPath $path
        sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

$output = Resolve-ProjectPath $OutputPath
if ((Test-Path -LiteralPath $output) -and -not $Force) {
    throw "Manual review already exists; use -Force only before a human has edited it: $output"
}
$candidateExpression = "$CandidateCommit`^{commit`}"
$commit = (& git -C $projectRoot rev-parse $candidateExpression).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') {
    throw "Unable to resolve candidate commit: $CandidateCommit"
}
$tagExpression = "$CandidateTag`^{commit`}"
$tagCommit = (& git -C $projectRoot rev-parse $tagExpression).Trim()
if ($LASTEXITCODE -ne 0 -or $tagCommit -ne $commit) {
    throw "Candidate tag does not resolve to the candidate commit: $CandidateTag"
}

$templatePath = Join-Path $projectRoot 'Templates/QINGLAN_G36_MANUAL_REVIEW_TEMPLATE.json'
$review = Get-Content -LiteralPath $templatePath -Raw -Encoding UTF8 | ConvertFrom-Json
$review.candidateCommit = $commit
$review.candidateTag = $CandidateTag
$review.reviews.visualReadability.evidence = @(
    Evidence 'Docs/DemoDevelopment/Assets/G3.6/Final/target-player.json'
    Evidence 'Docs/Reports/2026-08-10-g3-1-formal-visual-final-integration.md'
)
$review.reviews.buildDecisionDifference.evidence = @(
    Evidence 'Docs/DemoDevelopment/Assets/G3.6/Final/balance-freeze.json'
    Evidence 'Docs/Reports/2026-08-10-g3-4-balance-freeze-final-integration.md'
)
$review.reviews.audioMasking.evidence = @(
    Evidence 'Docs/DemoDevelopment/Assets/G3.6/Final/audio-masking-review.json'
    Evidence 'Docs/Reports/2026-08-10-g3-2-formal-audio-final-integration.md'
)
$review.reviews.rightsAndLicenses.evidence = @(
    Evidence 'Docs/DemoDevelopment/Assets/G3.6/Final/compliance.json'
    Evidence 'Docs/DemoDevelopment/Assets/G0_4_ASSET_MANIFEST.csv'
    Evidence 'Docs/Reports/2026-08-09-g3-1-formal-asset-governance.md'
    Evidence 'THIRD_PARTY_NOTICES.md'
    Evidence 'Assets/ThirdParty/Fonts/NotoCJKSC/FONT-002/LICENSE.txt'
)

New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
$json = ($review | ConvertTo-Json -Depth 12).Replace("`r`n", "`n") + "`n"
[IO.File]::WriteAllText($output, $json, [Text.UTF8Encoding]::new($false))
Write-Host "Qinglan G3.6 manual review prepared: $output"
