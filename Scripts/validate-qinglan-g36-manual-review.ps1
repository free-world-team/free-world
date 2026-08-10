[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$CandidateCommit = 'qinglan-demo-g3.6-rc2',
    [string]$ReviewPath = 'TestResults/QinglanDemo/G3.6/External/manual-review.json',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/External/manual-review-validation.json',
    [string]$InstallEvidenceRoot = ''
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
    $valueUri = [Uri]::new([IO.Path]::GetFullPath($Value))
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($valueUri).ToString()).Replace('\', '/')
}
function Has-Text($Value) { return $null -ne $Value -and -not [string]::IsNullOrWhiteSpace([string]$Value) }

$reviewFile = Resolve-ProjectPath $ReviewPath
$output = Resolve-ProjectPath $OutputPath
$issues = [Collections.Generic.List[string]]::new()
$candidateExpression = "$CandidateCommit`^{commit`}"
$commit = (& git -C $projectRoot rev-parse $candidateExpression).Trim()
if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') {
    throw "Unable to resolve candidate commit: $CandidateCommit"
}
$review = $null
if (-not (Test-Path -LiteralPath $reviewFile -PathType Leaf)) {
    $issues.Add('manual-review.json is missing.')
} else {
    try { $review = Get-Content -LiteralPath $reviewFile -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { $issues.Add("manual-review.json is invalid JSON: $($_.Exception.Message)") }
}

$wasRun = $false
if ($null -ne $review) {
    if ([int]$review.schemaVersion -ne 2) { $issues.Add('schemaVersion must be 2.') }
    if ($review.candidateCommit -ne $commit) { $issues.Add('candidateCommit does not match the selected candidate.') }
    if (-not (Has-Text $review.candidateTag)) { $issues.Add('candidateTag is missing.') }
    else {
        $tagExpression = "$($review.candidateTag)`^{commit`}"
        $tagCommit = (& git -C $projectRoot rev-parse $tagExpression 2>$null).Trim()
        if ($LASTEXITCODE -ne 0 -or $tagCommit -ne $commit) {
            $issues.Add('candidateTag does not resolve to candidateCommit.')
        }
    }
    $reviewNames = @('visualReadability', 'buildDecisionDifference', 'audioMasking', 'rightsAndLicenses')
    $statuses = [Collections.Generic.List[string]]::new()
    foreach ($name in $reviewNames) {
        $item = $review.reviews.$name
        if ($null -eq $item) { $issues.Add("reviews.$name is missing."); continue }
        $status = [string]$item.status
        $statuses.Add($status)
        if ($status -notin @('PASS', 'FAIL', 'NOT_RUN')) { $issues.Add("reviews.$name.status is invalid.") }
        if ($status -eq 'NOT_RUN') { continue }
        $wasRun = $true
        if ($item.reviewer.kind -ne 'human') { $issues.Add("reviews.$name reviewer must be human.") }
        if (-not (Has-Text $item.reviewer.name)) { $issues.Add("reviews.$name reviewer name is missing.") }
        if (-not (Has-Text $item.reviewer.role)) { $issues.Add("reviews.$name reviewer role is missing.") }
        if (-not (Has-Text $item.notes)) { $issues.Add("reviews.$name notes are missing.") }
        if (@($item.evidence).Count -eq 0) { $issues.Add("reviews.$name evidence is empty.") }
        $reviewedAt = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse([string]$item.reviewedAtUtc, [ref]$reviewedAt)) {
            $issues.Add("reviews.$name reviewedAtUtc is invalid.")
        } elseif ($reviewedAt -gt [DateTimeOffset]::UtcNow.AddMinutes(5)) {
            $issues.Add("reviews.$name reviewedAtUtc is in the future.")
        }
        foreach ($evidence in @($item.evidence)) {
            if (-not (Has-Text $evidence.path) -or $evidence.sha256 -notmatch '^[0-9a-f]{64}$') {
                $issues.Add("reviews.$name contains invalid evidence metadata.")
                continue
            }
            $evidencePath = Resolve-ProjectPath ([string]$evidence.path)
            if (-not $evidencePath.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar,
                    [StringComparison]::OrdinalIgnoreCase) -or
                -not (Test-Path -LiteralPath $evidencePath -PathType Leaf)) {
                $issues.Add("reviews.$name evidence is missing or outside the project: $($evidence.path)")
                continue
            }
            $actualHash = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($actualHash -ne $evidence.sha256) {
                $issues.Add("reviews.$name evidence hash mismatch: $($evidence.path)")
            }
        }
    }
    $rights = $review.reviews.rightsAndLicenses
    if ($null -ne $rights -and $rights.status -ne 'NOT_RUN') {
        $rightsStatuses = [Collections.Generic.List[string]]::new()
        foreach ($name in @('commercialRights', 'steamAiDisclosure', 'notoOflDistribution', 'storeLegalText')) {
            $rightsStatus = [string]$rights.checks.$name
            $rightsStatuses.Add($rightsStatus)
            if ($rightsStatus -notin @('PASS', 'FAIL')) {
                $issues.Add("rightsAndLicenses.checks.$name must be PASS or FAIL after review.")
            }
        }
        $rightsAnyFail = @($rightsStatuses | Where-Object { $_ -eq 'FAIL' }).Count -gt 0
        if ($rights.status -eq 'PASS' -and @($rightsStatuses | Where-Object { $_ -ne 'PASS' }).Count -gt 0) {
            $issues.Add('rightsAndLicenses.status cannot PASS while a legal check is not PASS.')
        }
        if ($rights.status -eq 'FAIL' -and -not $rightsAnyFail) {
            $issues.Add('rightsAndLicenses.status is FAIL but no legal check is FAIL.')
        }
    }
    $allPass = $statuses.Count -eq 4 -and @($statuses | Where-Object { $_ -ne 'PASS' }).Count -eq 0
    $anyFail = @($statuses | Where-Object { $_ -eq 'FAIL' }).Count -gt 0
    if ($review.status -notin @('PASS', 'FAIL', 'NOT_RUN')) { $issues.Add('Top-level status is invalid.') }
    if ($allPass -and $review.status -ne 'PASS') { $issues.Add('Top-level status must be PASS when every review passes.') }
    if ($anyFail -and $review.status -ne 'FAIL') { $issues.Add('Top-level status must be FAIL when any review fails.') }
    if (-not $allPass -and -not $anyFail -and $review.status -ne 'NOT_RUN') {
        $issues.Add('Top-level status must be NOT_RUN while a review is not run.')
    }
    if ($review.status -ne 'NOT_RUN') {
        if (-not [bool]$review.attestation.signed) { $issues.Add('Human attestation is not signed.') }
        if ($review.attestation.signerKind -ne 'human') { $issues.Add('Attestation signer must be human.') }
        if (-not (Has-Text $review.attestation.signerName)) { $issues.Add('Attestation signerName is missing.') }
        $signedAt = [DateTimeOffset]::MinValue
        if (-not [DateTimeOffset]::TryParse([string]$review.attestation.signedAtUtc, [ref]$signedAt)) {
            $issues.Add('Attestation signedAtUtc is invalid.')
        }
        $expectedStatement = 'I personally reviewed the listed Qinglan G3.6 RC evidence and attest that these results are accurate.'
        if ($review.attestation.statement -ne $expectedStatement) { $issues.Add('Attestation statement was changed.') }
    }
}

$sourceHash = if (Test-Path -LiteralPath $reviewFile -PathType Leaf) {
    (Get-FileHash -LiteralPath $reviewFile -Algorithm SHA256).Hash.ToLowerInvariant()
} else { '' }
$hasReviewFailure = $null -ne $review -and $review.status -eq 'FAIL'
$status = if ($issues.Count -gt 0) { 'FAIL' } elseif (-not $wasRun -or $review.status -eq 'NOT_RUN') {
    'NOT_RUN'
} elseif ($hasReviewFailure) { 'FAIL' } else { 'PASS' }
$result = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    status = $status
    candidateCommit = $commit
    sourcePath = Relative-ProjectPath $reviewFile
    sourceSha256 = $sourceHash
    issueCount = $issues.Count
    issues = $issues.ToArray()
}
New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
$json = ($result | ConvertTo-Json -Depth 8).Replace("`r`n", "`n") + "`n"
[IO.File]::WriteAllText($output, $json, [Text.UTF8Encoding]::new($false))
Write-Host "Qinglan G3.6 manual review validation: $status ($output)"
if ($status -eq 'PASS') {
    if (-not [string]::IsNullOrWhiteSpace($InstallEvidenceRoot)) {
        $installRoot = Resolve-ProjectPath $InstallEvidenceRoot
        New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
        Copy-Item -LiteralPath $reviewFile -Destination (Join-Path $installRoot 'manual-review.json') -Force
        Copy-Item -LiteralPath $output -Destination (Join-Path $installRoot 'manual-review-validation.json') -Force
        Write-Host "Validated manual review installed: $installRoot"
    }
    exit 0
}
if ($status -eq 'NOT_RUN') { exit 2 }
exit 1
