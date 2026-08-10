[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$EvidenceRoot = 'TestResults/QinglanDemo/G3.6/Candidate',
    [ValidateSet('PASS', 'FAIL', 'NOT_RUN')]
    [string]$CiStatus = 'NOT_RUN',
    [string]$CiRunUrl = '',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/Candidate/release-candidate-summary.json'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
$root = Resolve-ProjectPath $EvidenceRoot
$output = Resolve-ProjectPath $OutputPath

function Read-Json([string]$Name) {
    $path = Join-Path $root $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    try { return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json }
    catch { return $null }
}
function Test-XmlPassed([string]$Name) {
    $path = Join-Path $root $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $false }
    try {
        [xml]$document = Get-Content -LiteralPath $path -Raw
        return $document.'test-run'.result -eq 'Passed' -and [int]$document.'test-run'.failed -eq 0
    } catch { return $false }
}
function Status([bool]$Passed, [bool]$WasRun = $true) {
    if (-not $WasRun) { return 'NOT_RUN' }
    if ($Passed) { return 'PASS' }
    return 'FAIL'
}
function Hash([string]$Name) {
    $path = Join-Path $root $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return '' }
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
}

$editPassed = Test-XmlPassed 'editmode.xml'
$playPassed = Test-XmlPassed 'playmode.xml'
$validationLog = Join-Path $root 'validation.log'
$validationPassed = (Test-Path -LiteralPath $validationLog -PathType Leaf) -and
    (Select-String -LiteralPath $validationLog -SimpleMatch '[Project Validation] PASS' -Quiet)
$compliance = Read-Json 'compliance.json'
$manifest = Read-Json 'release-build-manifest.json'
$player = Read-Json 'release-player.json'
$vertical = Read-Json 'vertical-slice.json'
$balance = Read-Json 'balance-freeze.json'
$cpu = Read-Json 'cpu-target.json'
$gpu = Read-Json 'target-player.json'
$cleanClone = Read-Json 'clean-clone-summary.json'
$manual = Read-Json 'manual-review.json'
$profilePath = Join-Path $root 'release-player-save/profile.json'
$head = (& git -C $projectRoot rev-parse HEAD).Trim()

$manifestPassed = $null -ne $manifest -and [int]$manifest.schemaVersion -eq 2 -and
    $manifest.result -eq 'Succeeded' -and $manifest.buildConfiguration -eq 'WindowsReleaseCandidate' -and
    -not [bool]$manifest.development -and [int]$manifest.placeholderCount -eq 0 -and
    [int]$manifest.unapprovedAssetCount -eq 0 -and [int]$manifest.formalContentPackCount -eq 1 -and
    $manifest.releaseValidator -eq 'PASS' -and $manifest.platformBackend -eq 'NullPlatformFacade' -and
    $manifest.git.commit -eq $head -and [bool]$manifest.git.workingTreeClean -and
    $manifest.contentPacks.Count -eq 1 -and $manifest.contentPacks[0].packId -eq 'qinglan.pack.demo' -and
    $manifest.contentPacks[0].version -eq '0.10.0' -and [bool]$manifest.contentPacks[0].official -and
    -not [bool]$manifest.contentPacks[0].placeholder
$playerPassed = $null -ne $player -and $player.status -eq 'PASS' -and
    [bool]$player.releaseContractPassed -and -not [bool]$player.debugBuild -and
    [bool]$player.nullPlatform -and [int]$player.contentPackCount -eq 1 -and
    [int]$player.contentDefinitionCount -eq 193 -and [bool]$player.formalVisualsLoaded -and
    [bool]$player.formalAudioLoaded -and [bool]$player.formalFontsLoaded -and
    [bool]$player.formalLocalizationResolved -and [bool]$player.saveCommitted -and
    [bool]$player.profileSavePresent -and [bool]$player.runRecoveryCleared -and
    [bool]$player.hubVisited -and [bool]$player.restartVisited -and
    [int]$player.activeViewsAfterHub -eq 0 -and (Test-Path -LiteralPath $profilePath -PathType Leaf)
$verticalPassed = $null -ne $vertical -and $vertical.status -eq 'PASS' -and
    [bool]$vertical.deterministicReplay -and [bool]$vertical.threeBuildRoutesDistinct -and
    [bool]$vertical.spawnFairness.passed
$balanceVictories = if ($null -ne $balance.totalVictories) {
    [int]$balance.totalVictories
} else { [int]$balance.matrix.victories }
$balanceDefeats = if ($null -ne $balance.totalDefeats) {
    [int]$balance.totalDefeats
} else { [int]$balance.matrix.defeats }
$relicCompatibilityPassed = if ($null -ne $balance.relicCompatibilityPassed) {
    [bool]$balance.relicCompatibilityPassed
} else { [bool]$balance.matrix.relicCompatibilityPassed }
$relicCompatibilityCases = if ($null -ne $balance.relicCompatibilityCases) {
    @($balance.relicCompatibilityCases).Count
} else { [int]$balance.matrix.relicCompatibilityCases }
$balancePassed = $null -ne $balance -and $balance.status -eq 'PASS' -and
    ($balanceVictories + $balanceDefeats) -eq 15 -and $balanceVictories -eq 12 -and
    $relicCompatibilityPassed -and $relicCompatibilityCases -eq 18
$cpuPassed = $null -ne $cpu -and $cpu.status -eq 'PASS' -and
    [int]$cpu.configuration.tickCount -ge 54000 -and $cpu.simulationTick.p99Milliseconds -lt 33.33 -and
    [long]$cpu.gc.hotPathManagedAllocationBytes -eq 0
$gpuPassed = $null -ne $gpu -and $gpu.status -eq 'PASS' -and
    [int]$gpu.environment.screenWidth -eq 1920 -and [int]$gpu.environment.screenHeight -eq 1080 -and
    [bool]$gpu.budgets.averageFpsWithinBudget -and [bool]$gpu.budgets.onePercentLowWithinBudget -and
    [bool]$gpu.budgets.gpuP99WithinBudget -and [bool]$gpu.budgets.tickP99WithinBudget -and
    [bool]$gpu.budgets.zeroHotPathManagedAllocation -and [bool]$gpu.budgets.noGcCollections
$compliancePassed = $null -ne $compliance -and $compliance.status -eq 'PASS' -and
    [int]$compliance.issueCount -eq 0 -and [int]$compliance.placeholderCount -eq 0
$cleanClonePassed = $null -ne $cleanClone -and $cleanClone.status -eq 'PASS' -and
    $cleanClone.commit -eq $head
$manualPassed = $null -ne $manual -and $manual.status -eq 'PASS' -and
    $manual.visualReadability -eq 'PASS' -and $manual.buildDecisionDifference -eq 'PASS' -and
    $manual.audioMasking -eq 'PASS' -and $manual.rightsAndLicenses -eq 'PASS'

$dod = [ordered]@{
    'DOD-01' = Status ($playPassed -and $playerPassed)
    'DOD-02' = Status ($editPassed -and $playPassed -and $playerPassed)
    'DOD-03' = Status $balancePassed
    'DOD-04' = Status $balancePassed
    'DOD-05' = Status ($playPassed -and $verticalPassed)
    'DOD-06' = Status ($playPassed -and $verticalPassed)
    'DOD-07' = Status ($editPassed -and $playPassed -and $playerPassed)
    'DOD-08' = Status ($editPassed -and $playPassed -and $verticalPassed -and $playerPassed)
    'DOD-09' = Status ($gpuPassed -and $manualPassed)
    'DOD-10' = if ($CiStatus -eq 'NOT_RUN') { 'NOT_RUN' } else {
        Status ($editPassed -and $playPassed -and $validationPassed -and $cpuPassed -and
            $gpuPassed -and $manifestPassed -and $playerPassed -and $compliancePassed -and
            $cleanClonePassed -and $manualPassed -and $CiStatus -eq 'PASS')
    }
}
$allPassed = @($dod.Values | Where-Object { $_ -ne 'PASS' }).Count -eq 0
$result = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    status = if ($allPassed) { 'PASS' } else { 'FAIL' }
    decision = if ($allPassed) { 'GO' } else { 'NO-GO' }
    commit = $head
    branch = (& git -C $projectRoot rev-parse --abbrev-ref HEAD).Trim()
    ci = [ordered]@{ status = $CiStatus; runUrl = $CiRunUrl }
    gates = [ordered]@{
        editMode = Status $editPassed
        playMode = Status $playPassed
        validation = Status $validationPassed
        verticalSlice = Status $verticalPassed
        balance = Status $balancePassed
        cpuTarget = Status $cpuPassed
        gpuTarget = Status $gpuPassed
        releaseManifest = Status $manifestPassed
        releasePlayer = Status $playerPassed
        compliance = Status $compliancePassed
        cleanClone = Status $cleanClonePassed
        manualReview = Status $manualPassed
        ci = $CiStatus
    }
    dod = $dod
    hashes = [ordered]@{
        editMode = Hash 'editmode.xml'
        playMode = Hash 'playmode.xml'
        validation = Hash 'validation.log'
        verticalSlice = Hash 'vertical-slice.json'
        balance = Hash 'balance-freeze.json'
        cpuTarget = Hash 'cpu-target.json'
        gpuTarget = Hash 'target-player.json'
        compliance = Hash 'compliance.json'
        releaseManifest = Hash 'release-build-manifest.json'
        releasePlayer = Hash 'release-player.json'
        cleanClone = Hash 'clean-clone-summary.json'
        manualReview = Hash 'manual-review.json'
    }
    knownIssues = @(
        [ordered]@{ id = 'G3.5-EXT-2000'; status = 'FAIL'; releaseBlocking = $false;
            summary = 'The advisory 2000-enemy extension is CPU-bound below the frame target.' },
        [ordered]@{ id = 'G3.5-MIN-SPEC'; status = 'NOT_RUN'; releaseBlocking = $false;
            summary = 'Minimum-spec certification is not available; target evidence is RTX 3060 Ti / i7-12700F.' }
    )
}
New-Item -ItemType Directory -Path (Split-Path -Parent $output) -Force | Out-Null
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $output -Encoding utf8
Write-Host "Qinglan G3.6 Release Candidate: $($result.decision) ($output)"
if (-not $allPassed) { exit 1 }
exit 0
