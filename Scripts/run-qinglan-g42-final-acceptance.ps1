[CmdletBinding()]
param(
    [string]$Executable = 'Builds/WindowsRelease/AzureSword.exe',
    [string]$ProjectPath = '',
    [string]$LogPath = 'TestResults/QinglanDemo/G4.2-F/player-12m.log',
    [string]$ResultPath = 'TestResults/QinglanDemo/G4.2-F/player-12m.json',
    [string]$ScreenshotPath = 'TestResults/QinglanDemo/G4.2-F/Screenshots',
    [string]$SavePath = 'TestResults/QinglanDemo/G4.2-F/player-save',
    [int]$ScreenWidth = 1920,
    [int]$ScreenHeight = 1080,
    [int]$TimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-G42FinalPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}

$absoluteExecutable = Resolve-G42FinalPath $Executable
$absoluteLog = Resolve-G42FinalPath $LogPath
$absoluteResult = Resolve-G42FinalPath $ResultPath
$absoluteScreenshots = Resolve-G42FinalPath $ScreenshotPath
$absoluteSave = Resolve-G42FinalPath $SavePath
if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.2-F Release Player does not exist: $absoluteExecutable")
    exit 2
}
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteLog) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteResult) -Force | Out-Null
New-Item -ItemType Directory -Path $absoluteScreenshots -Force | Out-Null
New-Item -ItemType Directory -Path $absoluteSave -Force | Out-Null
foreach ($path in @($absoluteLog, $absoluteResult)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}

$expectedScreenshots = @(
    '00-enter-combat.png',
    '15-seconds.png',
    '30-seconds.png',
    '60-seconds.png',
    '03-minutes.png',
    '06-minutes.png',
    '09-minutes.png',
    '12-minutes.png',
    'accessibility-150-font.png',
    'accessibility-200-font.png',
    'accessibility-protanopia.png',
    'accessibility-deuteranopia.png',
    'accessibility-tritanopia.png',
    'accessibility-high-contrast.png',
    'accessibility-reduce-motion.png',
    'accessibility-no-damage-numbers.png',
    'review-grayscale.png'
)
foreach ($name in $expectedScreenshots) {
    $screenshot = Join-Path $absoluteScreenshots $name
    if (Test-Path -LiteralPath $screenshot) { Remove-Item -LiteralPath $screenshot -Force }
}

$previousResult = $env:QINGLAN_G42_FINAL_RESULT
$previousScreenshots = $env:QINGLAN_G42_FINAL_SCREENSHOT_DIR
$previousSave = $env:AZURESWORD_SAVE_ROOT
$process = $null
$windowSamples = 0
$respondingSamples = 0
try {
    $env:QINGLAN_G42_FINAL_RESULT = $absoluteResult
    $env:QINGLAN_G42_FINAL_SCREENSHOT_DIR = $absoluteScreenshots
    $env:AZURESWORD_SAVE_ROOT = $absoluteSave
    $process = Start-Process -FilePath $absoluteExecutable -ArgumentList @(
        '-screen-fullscreen', '0',
        '-screen-width', $ScreenWidth,
        '-screen-height', $ScreenHeight,
        '-qinglanG42FinalAcceptance',
        '-logFile', $absoluteLog
    ) -PassThru

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $process.HasExited -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 1000
        $process.Refresh()
        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            $windowSamples++
            if ($process.Responding) { $respondingSamples++ }
        }
    }
    if (-not $process.HasExited) {
        $process.Kill($true)
        [Console]::Error.WriteLine("G4.2-F Player timed out after $TimeoutSeconds seconds.")
        exit 7
    }
    $process.Refresh()
} finally {
    $env:QINGLAN_G42_FINAL_RESULT = $previousResult
    $env:QINGLAN_G42_FINAL_SCREENSHOT_DIR = $previousScreenshots
    $env:AZURESWORD_SAVE_ROOT = $previousSave
}

Write-Host "G4.2-F Player exit code: $($process.ExitCode)"
Write-Host "G4.2-F window responding samples: $respondingSamples/$windowSamples"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if ($windowSamples -lt 600 -or $respondingSamples -ne $windowSamples) {
    [Console]::Error.WriteLine('G4.2-F Player window was missing or stopped responding during sampling.')
    exit 5
}
if (-not (Test-Path -LiteralPath $absoluteResult -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.2-F Player result is missing: $absoluteResult")
    exit 4
}
try {
    $result = Get-Content -LiteralPath $absoluteResult -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("G4.2-F Player result is invalid: $($_.Exception.Message)")
    exit 5
}

if ([int]$result.schemaVersion -ne 4 -or $result.milestone -ne 'G4.2-F' -or
    $result.status -ne 'PASS' -or -not [bool]$result.passedAutomaticGate -or
    [double]$result.wallClockSeconds -lt 720 -or [double]$result.simulationSeconds -lt 720 -or
    [math]::Abs([double]$result.acceptanceSimulationScale - 1.0) -gt 0.0001 -or
    [int]$result.screenWidth -ne $ScreenWidth -or [int]$result.screenHeight -ne $ScreenHeight -or
    [int]$result.screenshotCount -ne 8 -or [int]$result.accessibilityScreenshotCount -ne 8 -or
    [int]$result.grayscaleReviewScreenshotCount -ne 1 -or
    [bool]$result.accessibilityTextOverflowObserved -or
    [int]$result.maxBossPhaseViews -le 0 -or [int]$result.maximumObservedBossPhase -lt 2 -or
    [int]$result.bossPhaseObservationSamples -le 0 -or
    -not [bool]$result.renderRecordersAvailable -or [long]$result.renderRecorderSamples -lt 10000 -or
    [double]$result.averageFps -lt 30 -or [double]$result.onePercentLowFps -lt 20 -or
    [int]$result.gpuFrameSampleCount -le 0 -or [double]$result.gpuFrameP99Milliseconds -gt 33.34 -or
    [long]$result.managedMemoryGrowthBytes -gt 268435456 -or
    [long]$result.finalTotalAllocatedMemoryBytes -gt 1610612736 -or
    [int]$result.generation2Collections -ge 120 -or
    [int]$result.majorSkillNonColorSignatureCount -ne 6 -or
    [long]$result.begunStagedVfxSequenceCount -le 0 -or
    [long]$result.completedStagedVfxSequenceCount -le 0 -or
    [long]$result.anticipationVfxStageCount -le 0 -or [long]$result.launchVfxStageCount -le 0 -or
    [long]$result.travelVfxStageCount -le 0 -or [long]$result.impactVfxStageCount -le 0 -or
    [long]$result.residueVfxStageCount -le 0 -or
    [long]$result.droppedCriticalVfxRequestCount -ne 0 -or
    [long]$result.droppedCriticalStagedVfxSequenceCount -ne 0 -or
    [long]$result.droppedCriticalAudioRequestCount -ne 0 -or
    [int]$result.maxActorViews -lt 103 -or [int]$result.maxPickupViews -lt 268 -or
    [bool]$result.humanVisualSignoff) {
    [Console]::Error.WriteLine('G4.2-F Player did not satisfy the 12-minute 1x automatic gate.')
    exit 5
}

foreach ($name in $expectedScreenshots) {
    $screenshot = Join-Path $absoluteScreenshots $name
    if (-not (Test-Path -LiteralPath $screenshot -PathType Leaf) -or
        (Get-Item -LiteralPath $screenshot).Length -le 0) {
        [Console]::Error.WriteLine("G4.2-F screenshot is missing or empty: $screenshot")
        exit 5
    }
}
if (-not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Qinglan G4.2-F Final Acceptance] PASS' -Quiet) -or
    -not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Bootstrap] Loaded content:' -Quiet)) {
    [Console]::Error.WriteLine("G4.2-F Player log is missing its PASS or Bootstrap marker: $absoluteLog")
    exit 5
}

Write-Host "Qinglan G4.2-F 12-minute 1x Player gate: PASS ($absoluteResult)"
Write-Host 'Independent human, physical minimum-spec, 4K and legal signoff: NOT RUN'
exit 0
