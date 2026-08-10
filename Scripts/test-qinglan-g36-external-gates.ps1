[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$OutputDirectory = 'TestResults/QinglanDemo/G3.6/ExternalGateTests'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
function Write-Utf8Json([string]$Path, $Value, [int]$Depth = 12) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
    $json = ($Value | ConvertTo-Json -Depth $Depth).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}
function Invoke-Validator([string]$Root) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'validate-qinglan-g36-minimum-spec.ps1') `
        -ProjectPath $projectRoot `
        -HardwarePath (Join-Path $Root 'hardware.json') `
        -PerformancePath (Join-Path $Root 'performance.json') `
        -ReleasePlayerPath (Join-Path $Root 'player.json') `
        -DevelopmentManifestPath (Join-Path $Root 'development-manifest.json') `
        -ReleaseManifestPath (Join-Path $Root 'manifest.json') `
        -OutputPath (Join-Path $Root 'review.json') `
        -ValidationOutputPath (Join-Path $Root 'validation.json') | Out-Host
    return $LASTEXITCODE
}

$root = Resolve-ProjectPath $OutputDirectory
New-Item -ItemType Directory -Path $root -Force | Out-Null
$candidateCommit = '9984bcc5582bd827372768993953645d754cc463'
$candidateTag = 'qinglan-demo-g3.6-rc2'
$developmentManifestSource = Join-Path $projectRoot 'TestResults/QinglanDemo/G3.6/Candidate/development-build-manifest.json'
$manifestSource = Join-Path $projectRoot 'Docs/DemoDevelopment/Assets/G3.6/Final/release-build-manifest.json'
$playerSource = Join-Path $projectRoot 'Docs/DemoDevelopment/Assets/G3.6/Final/release-player.json'
$performanceSource = Join-Path $projectRoot 'Docs/DemoDevelopment/Assets/G3.6/Final/target-player.json'
$developmentManifest = Get-Content -LiteralPath $developmentManifestSource -Raw -Encoding UTF8 | ConvertFrom-Json
$manifest = Get-Content -LiteralPath $manifestSource -Raw -Encoding UTF8 | ConvertFrom-Json
$player = Get-Content -LiteralPath $playerSource -Raw -Encoding UTF8 | ConvertFrom-Json
$performance = Get-Content -LiteralPath $performanceSource -Raw -Encoding UTF8 | ConvertFrom-Json
$performance.environment.operatingSystem = 'Windows 10 (10.0.19045) 64bit'
$performance.environment.processor = 'Synthetic Four Core CPU'
$performance.environment.processorCount = 8
$performance.environment.systemMemoryMegabytes = 8192
$performance.environment.graphicsDevice = 'Synthetic Minimum Spec GPU'
$performance.environment.graphicsMemoryMegabytes = 2048
$performance.environment.gitSha = $candidateCommit
$performance.environment.packVersion = '0.10.0'
$performance.environment.packHash = '8900fedffde84c2d014c260d50bff1833a1a98f378a4b22ac08ea4a3ec40d21f'
$hardware = [ordered]@{
    schemaVersion = 2
    candidateCommit = $candidateCommit
    candidateTag = $candidateTag
    physicalHardware = $true
    captureMethod = 'CIM_WMI_LOCAL'
    capturedAtUtc = [DateTime]::UtcNow.ToString('O')
    operator = [ordered]@{
        kind = 'human'
        name = 'Synthetic Regression Fixture'
        role = 'Automated validator test only'
        attested = $true
        statement = 'I witnessed this certification on the physical hardware recorded in this file.'
    }
    computerSystem = [ordered]@{ manufacturer = 'Fixture'; model = 'Fixture'; totalPhysicalMemoryBytes = [uint64](8 * 1GB) }
    operatingSystem = [ordered]@{ caption = 'Microsoft Windows 10 Pro'; version = '10.0.19045'; architecture = '64-bit' }
    processor = [ordered]@{ name = 'Synthetic Four Core CPU'; physicalCores = 4; logicalProcessors = 8 }
    graphicsAdapter = [ordered]@{ name = 'Synthetic Minimum Spec GPU'; adapterRamBytes = [uint64](2 * 1GB); driverVersion = 'fixture' }
    executables = [ordered]@{
        performance = [ordered]@{ path = 'fixture/Development/AzureSword.exe'; sha256 = [string]$developmentManifest.artifacts.sha256 }
        release = [ordered]@{ path = 'fixture/Release/AzureSword.exe'; sha256 = [string]$manifest.artifacts.sha256 }
    }
    notes = 'Synthetic fixture. Never valid for release installation.'
}

$hardwarePath = Join-Path $root 'hardware.json'
Write-Utf8Json $hardwarePath $hardware
Write-Utf8Json (Join-Path $root 'performance.json') $performance
Write-Utf8Json (Join-Path $root 'player.json') $player
Write-Utf8Json (Join-Path $root 'development-manifest.json') $developmentManifest
Write-Utf8Json (Join-Path $root 'manifest.json') $manifest

$passExit = Invoke-Validator $root
$passReview = Get-Content -LiteralPath (Join-Path $root 'review.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($passExit -ne 0 -or $passReview.status -ne 'PASS' -or [int]$passReview.issueCount -ne 0) {
    throw "Valid minimum-spec fixture was not accepted (exit=$passExit, status=$($passReview.status))."
}

$hardware.processor.physicalCores = 12
Write-Utf8Json $hardwarePath $hardware
$highSpecExit = Invoke-Validator $root
$highSpecReview = Get-Content -LiteralPath (Join-Path $root 'review.json') -Raw -Encoding UTF8 | ConvertFrom-Json
if ($highSpecExit -ne 1 -or $highSpecReview.status -ne 'FAIL' -or -not ($highSpecReview.issues -match 'exactly four physical cores')) {
    throw "Out-of-envelope fixture was not rejected (exit=$highSpecExit, status=$($highSpecReview.status))."
}

$hardware.processor.physicalCores = 4
Write-Utf8Json $hardwarePath $hardware
$finalExit = Invoke-Validator $root
if ($finalExit -ne 0) { throw 'Unable to restore the valid fixture before tamper testing.' }
$reviewPath = Join-Path $root 'review.json'
$validationPath = Join-Path $root 'validation.json'
$validation = Get-Content -LiteralPath $validationPath -Raw -Encoding UTF8 | ConvertFrom-Json
$review = Get-Content -LiteralPath $reviewPath -Raw -Encoding UTF8 | ConvertFrom-Json
$review.issueCount = 99
Write-Utf8Json $reviewPath $review
$tamperedHash = (Get-FileHash -LiteralPath $reviewPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($validation.sourceSha256 -eq $tamperedHash) { throw 'Tampered review unexpectedly retained its validation hash.' }

$restoredExit = Invoke-Validator $root
if ($restoredExit -ne 0) { throw 'Unable to restore validated evidence for candidate aggregation testing.' }
$integrationRoot = Join-Path $root 'CandidateEvidence'
New-Item -ItemType Directory -Path $integrationRoot -Force | Out-Null
Copy-Item -Path (Join-Path $projectRoot 'TestResults/QinglanDemo/G3.6/Candidate/*') -Destination $integrationRoot -Recurse -Force
Copy-Item -LiteralPath $reviewPath -Destination (Join-Path $integrationRoot 'minimum-spec-review.json') -Force
Copy-Item -LiteralPath $validationPath -Destination (Join-Path $integrationRoot 'minimum-spec-validation.json') -Force
$summaryPath = Join-Path $integrationRoot 'external-gate-summary.json'
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify-qinglan-g36-release-candidate.ps1') `
    -ProjectPath $projectRoot -EvidenceRoot $integrationRoot -CandidateCommit $candidateCommit `
    -CiStatus PASS -OutputPath $summaryPath | Out-Host
$summaryExit = $LASTEXITCODE
$summary = Get-Content -LiteralPath $summaryPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($summaryExit -ne 1 -or $summary.gates.minimumSpec -ne 'PASS' -or $summary.gates.manualReview -ne 'NOT_RUN') {
    throw "Candidate aggregator did not accept validated minimum-spec evidence (exit=$summaryExit, minimum=$($summary.gates.minimumSpec))."
}
$integrationReviewPath = Join-Path $integrationRoot 'minimum-spec-review.json'
$integrationReview = Get-Content -LiteralPath $integrationReviewPath -Raw -Encoding UTF8 | ConvertFrom-Json
$integrationReview.issueCount = 7
Write-Utf8Json $integrationReviewPath $integrationReview
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'verify-qinglan-g36-release-candidate.ps1') `
    -ProjectPath $projectRoot -EvidenceRoot $integrationRoot -CandidateCommit $candidateCommit `
    -CiStatus PASS -OutputPath $summaryPath | Out-Host
$tamperSummaryExit = $LASTEXITCODE
$tamperSummary = Get-Content -LiteralPath $summaryPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ($tamperSummaryExit -ne 1 -or $tamperSummary.gates.minimumSpec -ne 'NOT_RUN') {
    throw 'Candidate aggregator accepted minimum-spec evidence after its validated review was altered.'
}

Write-Host 'Qinglan G3.6 external gate regression: PASS'
Write-Host '  valid synthetic minimum-spec fixture: PASS'
Write-Host '  out-of-envelope high-spec fixture rejection: PASS'
Write-Host '  post-validation tamper detection: PASS'
Write-Host '  candidate aggregation and hash pairing: PASS'
exit 0
