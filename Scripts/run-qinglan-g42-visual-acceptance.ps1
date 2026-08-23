[CmdletBinding()]
param(
    [string]$Executable = 'Builds/WindowsDevelopmentG42/AzureSword.exe',
    [string]$ProjectPath = '',
    [string]$LogPath = 'TestResults/QinglanDemo/G4.2-A/player-90s.log',
    [string]$ResultPath = 'TestResults/QinglanDemo/G4.2-A/player-90s.json',
    [string]$ScreenshotPath = 'TestResults/QinglanDemo/G4.2-A/Screenshots',
    [string]$SavePath = 'TestResults/QinglanDemo/G4.2-A/player-save',
    [int]$ScreenWidth = 1920,
    [int]$ScreenHeight = 1080,
    [int]$TimeoutSeconds = 210
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-G42Path([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}

$absoluteExecutable = Resolve-G42Path $Executable
$absoluteLog = Resolve-G42Path $LogPath
$absoluteResult = Resolve-G42Path $ResultPath
$absoluteScreenshots = Resolve-G42Path $ScreenshotPath
$absoluteSave = Resolve-G42Path $SavePath
if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.2-A Development Player does not exist: $absoluteExecutable")
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
    '45-seconds.png',
    '60-seconds.png',
    '90-seconds.png',
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

$previousResult = $env:QINGLAN_G42_VISUAL_RESULT
$previousScreenshots = $env:QINGLAN_G42_SCREENSHOT_DIR
$previousSave = $env:AZURESWORD_SAVE_ROOT
$process = $null
$windowSamples = 0
$respondingSamples = 0
try {
    $env:QINGLAN_G42_VISUAL_RESULT = $absoluteResult
    $env:QINGLAN_G42_SCREENSHOT_DIR = $absoluteScreenshots
    $env:AZURESWORD_SAVE_ROOT = $absoluteSave
    $process = Start-Process -FilePath $absoluteExecutable -ArgumentList @(
        '-screen-fullscreen', '0',
        '-screen-width', $ScreenWidth,
        '-screen-height', $ScreenHeight,
        '-qinglanG42VisualAcceptance',
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
        [Console]::Error.WriteLine("G4.2-A Player timed out after $TimeoutSeconds seconds.")
        exit 7
    }
    $process.Refresh()
} finally {
    $env:QINGLAN_G42_VISUAL_RESULT = $previousResult
    $env:QINGLAN_G42_SCREENSHOT_DIR = $previousScreenshots
    $env:AZURESWORD_SAVE_ROOT = $previousSave
}

Write-Host "G4.2-A Player exit code: $($process.ExitCode)"
Write-Host "G4.2-A window responding samples: $respondingSamples/$windowSamples"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if ($windowSamples -lt 10 -or $respondingSamples -ne $windowSamples) {
    [Console]::Error.WriteLine('G4.2-A Player window was missing or stopped responding during sampling.')
    exit 5
}
if (-not (Test-Path -LiteralPath $absoluteResult -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.2-A Player result is missing: $absoluteResult")
    exit 4
}
try {
    $result = Get-Content -LiteralPath $absoluteResult -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("G4.2-A Player result is invalid: $($_.Exception.Message)")
    exit 5
}

if ($result.status -ne 'PASS' -or -not [bool]$result.passedAutomaticGate -or
    [double]$result.wallClockSeconds -lt 90 -or [int]$result.screenshotCount -ne 6 -or
    [int]$result.screenWidth -ne $ScreenWidth -or [int]$result.screenHeight -ne $ScreenHeight -or
    [int]$result.accessibilityScreenshotCount -ne 8 -or [bool]$result.accessibilityTextOverflowObserved -or
    [int]$result.grayscaleReviewScreenshotCount -ne 1 -or
    [int]$result.maxActorViews -lt 103 -or [int]$result.maxPickupViews -lt 268 -or
    [int]$result.maxActiveVfx -lt 42 -or -not [bool]$result.playerOutlineObserved -or
    -not [bool]$result.playerRimObserved -or -not [bool]$result.densePickupPresentationObserved -or
    [int]$result.maxDensityGroupedPickupViews -le 0 -or
    [int]$result.maxDensityEmphasisPickupViews -le 0 -or
    [int]$result.centralArenaTransitionCount -ne 14 -or
    -not [bool]$result.usesTiltedOrthographicCamera -or -not [bool]$result.usesXzGroundPlane -or
    [int]$result.formalMapGroundTileCount -le 0 -or [int]$result.formalMapPropCount -le 0 -or
    [bool]$result.humanVisualSignoff) {
    [Console]::Error.WriteLine('G4.2-A Player did not satisfy the ninety-second automatic visual gate.')
    exit 5
}

foreach ($name in $expectedScreenshots) {
    $screenshot = Join-Path $absoluteScreenshots $name
    if (-not (Test-Path -LiteralPath $screenshot -PathType Leaf) -or
        (Get-Item -LiteralPath $screenshot).Length -le 0) {
        [Console]::Error.WriteLine("G4.2-A screenshot is missing or empty: $screenshot")
        exit 5
    }
}
if (-not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Qinglan G4.2-A Visual Acceptance] PASS' -Quiet) -or
    -not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Bootstrap] Loaded content:' -Quiet)) {
    [Console]::Error.WriteLine("G4.2-A Player log is missing its PASS or Bootstrap marker: $absoluteLog")
    exit 5
}

Write-Host "Qinglan G4.2-A ninety-second Player gate: PASS ($absoluteResult)"
Write-Host 'Independent human visual signoff: NOT RUN'
exit 0
