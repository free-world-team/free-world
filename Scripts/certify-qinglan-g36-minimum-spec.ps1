[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$CandidateCommit = '9984bcc5582bd827372768993953645d754cc463',
    [string]$CandidateTag = 'qinglan-demo-g3.6-rc2',
    [Parameter(Mandatory = $true)]
    [string]$PerformanceExecutable,
    [Parameter(Mandatory = $true)]
    [string]$ReleaseExecutable,
    [Parameter(Mandatory = $true)]
    [string]$DevelopmentManifest,
    [Parameter(Mandatory = $true)]
    [string]$ReleaseManifest,
    [Parameter(Mandatory = $true)]
    [string]$OperatorName,
    [Parameter(Mandatory = $true)]
    [string]$OperatorRole,
    [Parameter(Mandatory = $true)]
    [switch]$AttestPhysicalHardware,
    [string]$Notes = '',
    [string]$OutputDirectory = 'TestResults/QinglanDemo/G3.6/MinimumSpec',
    [string]$InstallEvidenceRoot = 'Docs/DemoDevelopment/Assets/G3.6/Final'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}

$absolutePerformanceExecutable = Resolve-ProjectPath $PerformanceExecutable
$absoluteReleaseExecutable = Resolve-ProjectPath $ReleaseExecutable
$absoluteDevelopmentManifest = Resolve-ProjectPath $DevelopmentManifest
$absoluteManifest = Resolve-ProjectPath $ReleaseManifest
$absoluteOutput = Resolve-ProjectPath $OutputDirectory
$installRoot = if ([string]::IsNullOrWhiteSpace($InstallEvidenceRoot)) { '' } else { Resolve-ProjectPath $InstallEvidenceRoot }
if (-not (Test-Path -LiteralPath $absolutePerformanceExecutable -PathType Leaf)) { throw "Performance executable does not exist: $absolutePerformanceExecutable" }
if (-not (Test-Path -LiteralPath $absoluteReleaseExecutable -PathType Leaf)) { throw "Release executable does not exist: $absoluteReleaseExecutable" }
if (-not (Test-Path -LiteralPath $absoluteDevelopmentManifest -PathType Leaf)) { throw "Development manifest does not exist: $absoluteDevelopmentManifest" }
if (-not (Test-Path -LiteralPath $absoluteManifest -PathType Leaf)) { throw "Release manifest does not exist: $absoluteManifest" }
New-Item -ItemType Directory -Path $absoluteOutput -Force | Out-Null

$hardwarePath = Join-Path $absoluteOutput 'minimum-spec-hardware.json'
$performancePath = Join-Path $absoluteOutput 'target-player.json'
$releasePlayerPath = Join-Path $absoluteOutput 'release-player.json'
$releasePlayerLog = Join-Path $absoluteOutput 'release-player.log'
$releasePlayerSave = Join-Path $absoluteOutput 'release-player-save'
$developmentManifestPath = Join-Path $absoluteOutput 'development-build-manifest.json'
$manifestPath = Join-Path $absoluteOutput 'release-build-manifest.json'
$reviewPath = Join-Path $absoluteOutput 'minimum-spec-review.json'
$validationPath = Join-Path $absoluteOutput 'minimum-spec-validation.json'
foreach ($staleFile in @(
        $hardwarePath, $performancePath, (Join-Path $absoluteOutput 'target-player.log'),
        $releasePlayerPath, $releasePlayerLog, $developmentManifestPath, $manifestPath,
        $reviewPath, $validationPath
    )) {
    if (Test-Path -LiteralPath $staleFile -PathType Leaf) { Remove-Item -LiteralPath $staleFile -Force }
}
Copy-Item -LiteralPath $absoluteDevelopmentManifest -Destination $developmentManifestPath -Force
Copy-Item -LiteralPath $absoluteManifest -Destination $manifestPath -Force

& (Join-Path $PSScriptRoot 'capture-qinglan-g36-minimum-spec-hardware.ps1') `
    -ProjectPath $projectRoot -CandidateCommit $CandidateCommit -CandidateTag $CandidateTag `
    -PerformanceExecutable $absolutePerformanceExecutable -ReleaseExecutable $absoluteReleaseExecutable `
    -OperatorName $OperatorName -OperatorRole $OperatorRole `
    -AttestPhysicalHardware:$AttestPhysicalHardware -Notes $Notes -OutputPath $hardwarePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$hardware = Get-Content -LiteralPath $hardwarePath -Raw -Encoding UTF8 | ConvertFrom-Json
$preflightPassed = $hardware.operatingSystem.caption -match 'Windows 10' -and
    $hardware.operatingSystem.architecture -match '64' -and
    [int]$hardware.processor.physicalCores -eq 4 -and
    [int]$hardware.processor.logicalProcessors -ge 4 -and [int]$hardware.processor.logicalProcessors -le 8 -and
    [uint64]$hardware.computerSystem.totalPhysicalMemoryBytes -ge [uint64](7.5 * 1GB) -and
    [uint64]$hardware.computerSystem.totalPhysicalMemoryBytes -le [uint64](10 * 1GB) -and
    [uint64]$hardware.graphicsAdapter.adapterRamBytes -ge [uint64](1.75 * 1GB) -and
    [uint64]$hardware.graphicsAdapter.adapterRamBytes -le [uint64](3 * 1GB)
if (-not $preflightPassed) {
    & (Join-Path $PSScriptRoot 'validate-qinglan-g36-minimum-spec.ps1') `
        -ProjectPath $projectRoot -CandidateCommit $CandidateCommit -CandidateTag $CandidateTag `
        -HardwarePath $hardwarePath -PerformancePath $performancePath `
        -ReleasePlayerPath $releasePlayerPath -DevelopmentManifestPath $developmentManifestPath `
        -ReleaseManifestPath $manifestPath -OutputPath $reviewPath `
        -ValidationOutputPath $validationPath
    [Console]::Error.WriteLine('This machine is outside the locked Windows 10 x64 / 4-core / 8 GB RAM / 2 GB VRAM certification envelope. The 30-minute run was not started.')
    exit 3
}

& (Join-Path $PSScriptRoot 'run-qinglan-g35-performance.ps1') `
    -Mode Target -ProjectPath $projectRoot -Executable $absolutePerformanceExecutable `
    -OutputDirectory $absoluteOutput -CandidateCommit $CandidateCommit
$performanceExitCode = $LASTEXITCODE

& (Join-Path $PSScriptRoot 'run-player-smoke.ps1') `
    -ProjectPath $projectRoot -Executable $absoluteReleaseExecutable -LogPath $releasePlayerLog `
    -ResultPath $releasePlayerPath -SavePath $releasePlayerSave
$playerExitCode = $LASTEXITCODE

$validatorArguments = @{
    ProjectPath = $projectRoot
    CandidateCommit = $CandidateCommit
    CandidateTag = $CandidateTag
    HardwarePath = $hardwarePath
    PerformancePath = $performancePath
    ReleasePlayerPath = $releasePlayerPath
    DevelopmentManifestPath = $developmentManifestPath
    ReleaseManifestPath = $manifestPath
    OutputPath = $reviewPath
    ValidationOutputPath = $validationPath
}
if (-not [string]::IsNullOrWhiteSpace($installRoot)) { $validatorArguments.InstallEvidenceRoot = $installRoot }
& (Join-Path $PSScriptRoot 'validate-qinglan-g36-minimum-spec.ps1') @validatorArguments
$validationExitCode = $LASTEXITCODE
if ($performanceExitCode -ne 0 -or $playerExitCode -ne 0 -or $validationExitCode -ne 0) {
    [Console]::Error.WriteLine("Minimum-spec certification failed (performance=$performanceExitCode, player=$playerExitCode, validation=$validationExitCode).")
    exit 1
}
Write-Host "Qinglan G3.6 minimum-spec certification: PASS ($reviewPath)"
exit 0
