[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$CandidateCommit = '9984bcc5582bd827372768993953645d754cc463',
    [string]$CandidateTag = 'qinglan-demo-g3.6-rc2',
    [string]$HardwarePath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/minimum-spec-hardware.json',
    [string]$PerformancePath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/target-player.json',
    [string]$ReleasePlayerPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/release-player.json',
    [string]$DevelopmentManifestPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/development-build-manifest.json',
    [string]$ReleaseManifestPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/release-build-manifest.json',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/minimum-spec-review.json',
    [string]$ValidationOutputPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/minimum-spec-validation.json',
    [string]$InstallEvidenceRoot = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
function Write-Utf8Json([string]$Path, $Value, [int]$Depth = 10) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
    $json = ($Value | ConvertTo-Json -Depth $Depth).Replace("`r`n", "`n") + "`n"
    [IO.File]::WriteAllText($Path, $json, [Text.UTF8Encoding]::new($false))
}
function Hash-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}
function Read-Json([string]$Path, [string]$Label) {
    try { return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json }
    catch { Add-Issue "$Label is not valid JSON: $($_.Exception.Message)"; return $null }
}
function Add-Issue([string]$Message) { $script:issues.Add($Message) | Out-Null }
function Check([string]$Name, [bool]$Passed, [string]$Failure) {
    $script:checks[$Name] = $Passed
    if (-not $Passed) { Add-Issue $Failure }
}

$hardwareFile = Resolve-ProjectPath $HardwarePath
$performanceFile = Resolve-ProjectPath $PerformancePath
$releasePlayerFile = Resolve-ProjectPath $ReleasePlayerPath
$developmentManifestFile = Resolve-ProjectPath $DevelopmentManifestPath
$releaseManifestFile = Resolve-ProjectPath $ReleaseManifestPath
$outputFile = Resolve-ProjectPath $OutputPath
$validationFile = Resolve-ProjectPath $ValidationOutputPath
$candidateExpression = "$CandidateCommit`^{commit`}"
$resolvedCommit = (& git -c "safe.directory=$($projectRoot.Replace('\', '/'))" -C $projectRoot rev-parse $candidateExpression).Trim()
if ($LASTEXITCODE -ne 0 -or $resolvedCommit -notmatch '^[0-9a-f]{40}$') {
    throw "Unable to resolve candidate commit: $CandidateCommit"
}
$resolvedTagCommit = (& git -c "safe.directory=$($projectRoot.Replace('\', '/'))" -C $projectRoot rev-parse "$CandidateTag`^{commit`}").Trim()
if ($LASTEXITCODE -ne 0 -or $resolvedTagCommit -ne $resolvedCommit) {
    throw "Candidate tag $CandidateTag does not resolve to $resolvedCommit."
}

$sourceFiles = [ordered]@{
    hardware = $hardwareFile
    performance = $performanceFile
    releasePlayer = $releasePlayerFile
    developmentManifest = $developmentManifestFile
    releaseManifest = $releaseManifestFile
}
$missing = @($sourceFiles.GetEnumerator() | Where-Object {
        -not (Test-Path -LiteralPath $_.Value -PathType Leaf)
    } | ForEach-Object { $_.Key })
$issues = [Collections.Generic.List[string]]::new()
$checks = [ordered]@{}
$sourceHashes = [ordered]@{}
foreach ($entry in $sourceFiles.GetEnumerator()) { $sourceHashes[$entry.Key] = Hash-File $entry.Value }

if ($missing.Count -gt 0) {
    foreach ($name in $missing) { Add-Issue "Required evidence is missing: $name" }
    $review = [ordered]@{
        schemaVersion = 2
        status = 'NOT_RUN'
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        commit = $resolvedCommit
        candidateTag = $CandidateTag
        physicalHardware = $false
        sourceEvidence = $sourceHashes
        hardware = $null
        performance = $null
        checks = $checks
        issueCount = $issues.Count
        issues = @($issues)
    }
    Write-Utf8Json $outputFile $review
    $reviewHash = Hash-File $outputFile
    $validation = [ordered]@{
        schemaVersion = 1
        status = 'NOT_RUN'
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        candidateCommit = $resolvedCommit
        candidateTag = $CandidateTag
        sourceSha256 = $reviewHash
        evidenceSha256 = $sourceHashes
        issueCount = $issues.Count
    }
    Write-Utf8Json $validationFile $validation
    Write-Host "Qinglan G3.6 minimum-spec validation: NOT_RUN ($outputFile)"
    exit 2
}

$hardware = Read-Json $hardwareFile 'Hardware capture'
$performance = Read-Json $performanceFile 'Performance result'
$releasePlayer = Read-Json $releasePlayerFile 'Release Player result'
$developmentManifest = Read-Json $developmentManifestFile 'Development manifest'
$manifest = Read-Json $releaseManifestFile 'Release manifest'
if ($null -eq $hardware -or $null -eq $performance -or $null -eq $releasePlayer -or
    $null -eq $developmentManifest -or $null -eq $manifest) {
    $reviewStatus = 'FAIL'
} else {
    $reviewStatus = 'PASS'

    $attestation = 'I witnessed this certification on the physical hardware recorded in this file.'
    Check 'hardwareSchema' ([int]$hardware.schemaVersion -eq 2) 'Hardware capture schemaVersion must be 2.'
    Check 'candidateCommit' ($hardware.candidateCommit -eq $resolvedCommit) 'Hardware capture candidate commit does not match.'
    Check 'candidateTag' ($hardware.candidateTag -eq $CandidateTag) 'Hardware capture candidate tag does not match.'
    Check 'physicalHardware' ([bool]$hardware.physicalHardware -and $hardware.captureMethod -eq 'CIM_WMI_LOCAL') 'Certification must be captured from local physical hardware through CIM/WMI.'
    $capturedAt = [DateTime]::MinValue
    Check 'captureTime' ([DateTime]::TryParse([string]$hardware.capturedAtUtc, [ref]$capturedAt) -and $capturedAt.ToUniversalTime() -le [DateTime]::UtcNow.AddMinutes(5)) 'Hardware capture time is missing or invalid.'
    Check 'humanOperator' ($hardware.operator.kind -eq 'human' -and -not [string]::IsNullOrWhiteSpace([string]$hardware.operator.name) -and -not [string]::IsNullOrWhiteSpace([string]$hardware.operator.role)) 'A named human operator and role are required.'
    Check 'operatorAttestation' ([bool]$hardware.operator.attested -and $hardware.operator.statement -eq $attestation) 'The physical-hardware attestation is incomplete or altered.'
    Check 'windows10X64' ($hardware.operatingSystem.caption -match 'Windows 10' -and $hardware.operatingSystem.architecture -match '64') 'The certification OS must be Windows 10 x64.'
    Check 'fourPhysicalCores' ([int]$hardware.processor.physicalCores -eq 4) 'The certification CPU must have exactly four physical cores.'
    Check 'logicalProcessorEnvelope' ([int]$hardware.processor.logicalProcessors -ge 4 -and [int]$hardware.processor.logicalProcessors -le 8) 'Logical processor count must be between 4 and 8.'
    $minimumRam = [uint64](7.5 * 1GB)
    $maximumRam = [uint64](10 * 1GB)
    Check 'memoryEnvelope' ([uint64]$hardware.computerSystem.totalPhysicalMemoryBytes -ge $minimumRam -and [uint64]$hardware.computerSystem.totalPhysicalMemoryBytes -le $maximumRam) 'Physical memory must be within the 8 GB certification envelope (7.5-10 GiB).'
    $minimumVram = [uint64](1.75 * 1GB)
    $maximumVram = [uint64](3 * 1GB)
    Check 'vramEnvelope' ([uint64]$hardware.graphicsAdapter.adapterRamBytes -ge $minimumVram -and [uint64]$hardware.graphicsAdapter.adapterRamBytes -le $maximumVram) 'GPU memory must be within the 2 GB certification envelope (1.75-3 GiB).'
    Check 'executableHashShape' ([string]$hardware.executables.performance.sha256 -match '^[0-9a-f]{64}$' -and [string]$hardware.executables.release.sha256 -match '^[0-9a-f]{64}$') 'Captured executable SHA-256 values are invalid.'

    Check 'developmentManifestSchema' ([int]$developmentManifest.schemaVersion -eq 2) 'Development manifest schemaVersion must be 2.'
    Check 'developmentManifestSucceeded' ($developmentManifest.result -eq 'Succeeded' -and $developmentManifest.buildConfiguration -eq 'WindowsDevelopment' -and [bool]$developmentManifest.development) 'Manifest is not a successful Windows Development build.'
    Check 'developmentManifestCandidate' ($developmentManifest.git.commit -eq $resolvedCommit -and $developmentManifest.git.tag -eq $CandidateTag -and [bool]$developmentManifest.git.workingTreeClean) 'Development manifest candidate provenance does not match the locked commit and tag.'
    $developmentQinglanPack = @($developmentManifest.contentPacks | Where-Object {
            $_.packId -eq 'qinglan.pack.demo' -and $_.version -eq '0.10.0' -and
            $_.contentHash -eq '8900fedffde84c2d014c260d50bff1833a1a98f378a4b22ac08ea4a3ec40d21f'
        })
    Check 'developmentManifestContent' ($developmentQinglanPack.Count -eq 1) 'Development manifest does not contain the frozen Qinglan performance pack.'
    Check 'samePerformanceExecutable' ($developmentManifest.artifacts.sha256 -eq $hardware.executables.performance.sha256) 'The physical performance executable is not the manifest-locked Development executable.'

    Check 'manifestSchema' ([int]$manifest.schemaVersion -eq 2) 'Release manifest schemaVersion must be 2.'
    Check 'manifestSucceeded' ($manifest.result -eq 'Succeeded' -and $manifest.buildConfiguration -eq 'WindowsReleaseCandidate' -and -not [bool]$manifest.development) 'Manifest is not a successful Windows Release Candidate build.'
    Check 'manifestCandidate' ($manifest.git.commit -eq $resolvedCommit -and $manifest.git.tag -eq $CandidateTag -and [bool]$manifest.git.workingTreeClean) 'Manifest candidate provenance does not match the locked commit and tag.'
    Check 'manifestReleaseContent' ([int]$manifest.placeholderCount -eq 0 -and [int]$manifest.unapprovedAssetCount -eq 0 -and [int]$manifest.formalContentPackCount -eq 1 -and $manifest.releaseValidator -eq 'PASS') 'Manifest does not satisfy formal release-content gates.'
    Check 'sameReleaseExecutable' ($manifest.artifacts.sha256 -eq $hardware.executables.release.sha256) 'The physical Release Player executable is not the manifest-locked Release Candidate executable.'

    Check 'performanceSchema' ([int]$performance.schemaVersion -eq 1) 'Performance result schemaVersion must be 1.'
    Check 'performancePassed' ($performance.status -eq 'PASS') 'Minimum-spec target performance result is not PASS.'
    Check 'targetProfile' ($performance.configuration.profile -eq 'target' -and [bool]$performance.configuration.certificationEligible -and [int]$performance.configuration.tickCount -eq 54000 -and [int]$performance.configuration.enemies -eq 1200 -and [int]$performance.configuration.projectiles -eq 900 -and [int]$performance.configuration.pickups -eq 500 -and [int]$performance.configuration.vfx -eq 200) 'Performance result is not the full 30-minute target certification profile.'
    Check 'performanceCandidate' ($performance.environment.gitSha -eq $resolvedCommit -and $performance.environment.packVersion -eq '0.10.0' -and $performance.environment.packHash -eq '8900fedffde84c2d014c260d50bff1833a1a98f378a4b22ac08ea4a3ec40d21f') 'Performance provenance does not match the locked candidate.'
    Check 'performanceHardwareIdentity' ($performance.environment.processorCount -ge 4 -and $performance.environment.processorCount -le 8 -and $performance.environment.systemMemoryMegabytes -ge 7168 -and $performance.environment.systemMemoryMegabytes -le 10240 -and $performance.environment.graphicsMemoryMegabytes -ge 1792 -and $performance.environment.graphicsMemoryMegabytes -le 3072 -and $performance.environment.graphicsDevice -eq $hardware.graphicsAdapter.name) 'Player-reported hardware does not match the minimum-spec envelope or captured adapter.'
    Check 'performanceDisplay' ([int]$performance.environment.screenWidth -eq 1920 -and [int]$performance.environment.screenHeight -eq 1080 -and $performance.environment.graphicsDeviceType -eq 'Direct3D11' -and -not [bool]$performance.environment.remoteDisplayAdapter) 'Performance run must use 1920x1080, Direct3D 11, and a physical display adapter.'
    $requiredBudgetNames = @(
        'configurationMatchesProfile', 'exactEntityCounts', 'formalAssetsResolved',
        'averageFpsWithinBudget', 'onePercentLowWithinBudget', 'gpuP99WithinBudget',
        'tickP99WithinBudget', 'gpuSamplesAvailable', 'zeroHotPathManagedAllocation',
        'zeroProfilerGcAllocation', 'noGcCollections', 'noSustainedMemoryGrowth',
        'renderRecordersAvailable', 'noPoolExpansionOrDrops', 'correctResolution',
        'correctGraphicsApi', 'correctQuality', 'uninterruptedFocus',
        'physicalDisplayAdapter', 'provenanceRecorded', 'noFrameSampleOverflow'
    )
    $budgetFailures = @($requiredBudgetNames | Where-Object { -not [bool]$performance.budgets.$_ })
    Check 'performanceBudgets' ($budgetFailures.Count -eq 0) "Performance budgets failed: $($budgetFailures -join ', ')"
    Check 'performanceGc' ([long]$performance.gc.hotPathManagedAllocationBytes -eq 0 -and [long]$performance.gc.profilerAllocatedBytes -eq 0 -and [int]$performance.gc.generation0Collections -eq 0 -and [int]$performance.gc.generation1Collections -eq 0 -and [int]$performance.gc.generation2Collections -eq 0) 'Performance run recorded managed allocation or GC collections.'

    Check 'releasePlayerSchema' ([int]$releasePlayer.schemaVersion -eq 3) 'Release Player result schemaVersion must be 3.'
    Check 'releasePlayerContract' ($releasePlayer.status -eq 'PASS' -and [bool]$releasePlayer.releaseCandidateRequested -and [bool]$releasePlayer.releaseContractPassed -and -not [bool]$releasePlayer.debugBuild -and [bool]$releasePlayer.nullPlatform) 'Release Player contract did not pass on the minimum-spec machine.'
    Check 'releasePlayerFormalContent' ([bool]$releasePlayer.formalVisualsLoaded -and [bool]$releasePlayer.formalAudioLoaded -and [bool]$releasePlayer.formalFontsLoaded -and [bool]$releasePlayer.formalLocalizationResolved) 'Release Player did not load all formal content.'
    Check 'releasePlayerLifecycle' ([bool]$releasePlayer.saveCommitted -and [bool]$releasePlayer.profileSavePresent -and [bool]$releasePlayer.runRecoveryCleared -and [bool]$releasePlayer.hubVisited -and [bool]$releasePlayer.restartVisited -and [int]$releasePlayer.activeViewsAfterHub -eq 0) 'Release Player lifecycle or persistence verification failed.'
}

if ($issues.Count -gt 0) { $reviewStatus = 'FAIL' }
$hardwareSummary = if ($null -eq $hardware) { $null } else { [ordered]@{
        capturedAtUtc = $hardware.capturedAtUtc
        operator = $hardware.operator
        computerSystem = $hardware.computerSystem
        operatingSystem = $hardware.operatingSystem
        processor = $hardware.processor
        graphicsAdapter = $hardware.graphicsAdapter
        executables = $hardware.executables
    } }
$performanceSummary = if ($null -eq $performance) { $null } else { [ordered]@{
        generatedAtUtc = $performance.generatedAtUtc
        configuration = $performance.configuration
        environment = $performance.environment
        wallFrame = $performance.wallFrame
        gpuFrame = $performance.gpuFrame
        simulationTick = $performance.simulationTick
        gc = $performance.gc
    } }
$review = [ordered]@{
    schemaVersion = 2
    status = $reviewStatus
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    commit = $resolvedCommit
    candidateTag = $CandidateTag
    physicalHardware = $null -ne $hardware -and [bool]$hardware.physicalHardware
    sourceEvidence = $sourceHashes
    hardware = $hardwareSummary
    performance = $performanceSummary
    checks = $checks
    issueCount = $issues.Count
    issues = @($issues)
}
Write-Utf8Json $outputFile $review
$reviewHash = Hash-File $outputFile
$validation = [ordered]@{
    schemaVersion = 1
    status = $reviewStatus
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    candidateCommit = $resolvedCommit
    candidateTag = $CandidateTag
    sourceSha256 = $reviewHash
    evidenceSha256 = $sourceHashes
    checkCount = $checks.Count
    issueCount = $issues.Count
}
Write-Utf8Json $validationFile $validation

if (-not [string]::IsNullOrWhiteSpace($InstallEvidenceRoot)) {
    if ($reviewStatus -ne 'PASS') { throw 'Only a PASS minimum-spec certification can be installed as candidate evidence.' }
    $installRoot = Resolve-ProjectPath $InstallEvidenceRoot
    New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
    $installFiles = [ordered]@{
        $hardwareFile = 'minimum-spec-hardware.json'
        $performanceFile = 'minimum-spec-target-player.json'
        $releasePlayerFile = 'minimum-spec-release-player.json'
        $developmentManifestFile = 'minimum-spec-development-manifest.json'
        $releaseManifestFile = 'minimum-spec-release-manifest.json'
        $outputFile = 'minimum-spec-review.json'
        $validationFile = 'minimum-spec-validation.json'
    }
    foreach ($entry in $installFiles.GetEnumerator()) {
        Copy-Item -LiteralPath $entry.Key -Destination (Join-Path $installRoot $entry.Value) -Force
    }
}

Write-Host "Qinglan G3.6 minimum-spec validation: $reviewStatus ($outputFile)"
if ($reviewStatus -ne 'PASS') { exit 1 }
exit 0
