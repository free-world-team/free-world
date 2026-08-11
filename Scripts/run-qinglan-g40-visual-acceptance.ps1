[CmdletBinding()]
param(
    [string]$Executable = 'Builds/WindowsDevelopmentG40/AzureSword.exe',
    [string]$ProjectPath = '',
    [string]$LogPath = 'TestResults/QinglanDemo/G4.0/player-60s.log',
    [string]$ResultPath = 'TestResults/QinglanDemo/G4.0/player-60s.json',
    [string]$ScreenshotPath = 'TestResults/QinglanDemo/G4.0/Screenshots',
    [string]$SavePath = 'TestResults/QinglanDemo/G4.0/player-save',
    [int]$TimeoutSeconds = 150
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-G40Path([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
$absoluteExecutable = Resolve-G40Path $Executable
$absoluteLog = Resolve-G40Path $LogPath
$absoluteResult = Resolve-G40Path $ResultPath
$absoluteScreenshots = Resolve-G40Path $ScreenshotPath
$absoluteSave = Resolve-G40Path $SavePath
if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.0 Development Player does not exist: $absoluteExecutable")
    exit 2
}
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteLog) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteResult) -Force | Out-Null
New-Item -ItemType Directory -Path $absoluteScreenshots -Force | Out-Null
New-Item -ItemType Directory -Path $absoluteSave -Force | Out-Null
foreach ($path in @($absoluteLog, $absoluteResult)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
foreach ($name in @('00-enter-combat.png', '15-seconds.png', '30-seconds.png', '45-seconds.png', '60-seconds.png')) {
    $screenshot = Join-Path $absoluteScreenshots $name
    if (Test-Path -LiteralPath $screenshot) { Remove-Item -LiteralPath $screenshot -Force }
}

$previousResult = $env:QINGLAN_G40_VISUAL_RESULT
$previousScreenshots = $env:QINGLAN_G40_SCREENSHOT_DIR
$previousSave = $env:AZURESWORD_SAVE_ROOT
try {
    $env:QINGLAN_G40_VISUAL_RESULT = $absoluteResult
    $env:QINGLAN_G40_SCREENSHOT_DIR = $absoluteScreenshots
    $env:AZURESWORD_SAVE_ROOT = $absoluteSave
    $process = Start-Process -FilePath $absoluteExecutable -ArgumentList @(
        '-screen-fullscreen', '0',
        '-screen-width', '1920',
        '-screen-height', '1080',
        '-qinglanG40VisualAcceptance',
        '-logFile', $absoluteLog
    ) -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill($true)
        [Console]::Error.WriteLine("G4.0 Player timed out after $TimeoutSeconds seconds.")
        exit 7
    }
    $process.Refresh()
} finally {
    $env:QINGLAN_G40_VISUAL_RESULT = $previousResult
    $env:QINGLAN_G40_SCREENSHOT_DIR = $previousScreenshots
    $env:AZURESWORD_SAVE_ROOT = $previousSave
}
Write-Host "G4.0 Player exit code: $($process.ExitCode)"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if (-not (Test-Path -LiteralPath $absoluteResult -PathType Leaf)) {
    [Console]::Error.WriteLine("G4.0 Player result is missing: $absoluteResult")
    exit 4
}
try {
    $result = Get-Content -LiteralPath $absoluteResult -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("G4.0 Player result is invalid: $($_.Exception.Message)")
    exit 5
}
if ($result.status -ne 'PASS' -or -not [bool]$result.passedAutomaticGate -or
    [double]$result.wallClockSeconds -lt 60 -or [int]$result.screenshotCount -ne 5 -or
    [int]$result.distinctEnemyProfileCount -lt 3 -or [int]$result.maxActorViews -lt 4 -or
    [int]$result.maxProjectileViews -le 0 -or [int]$result.maxHeldWeaponViews -le 0 -or
    [long]$result.projectileTrailSpawnCount -le 0 -or
    [long]$result.directionalAnimationFrameChangeCount -le 0 -or
    [long]$result.totalHitRequestCount -le 0 -or [long]$result.totalDeathRequestCount -le 0 -or
    [long]$result.formalVfxSpawnCount -le 0 -or -not [bool]$result.usesTiltedOrthographicCamera -or
    -not [bool]$result.usesXzGroundPlane -or [int]$result.raisedMapGeometryCount -le 0 -or
    [int]$result.mapGroundShadowCount -le 0 -or [int]$result.formalMapGroundTileCount -le 0 -or
    [int]$result.formalMapPropCount -le 0 -or [int]$result.realCardClicks -le 0) {
    [Console]::Error.WriteLine('G4.0 Player did not satisfy the sixty-second automatic visual gate.')
    exit 5
}
foreach ($name in @('00-enter-combat.png', '15-seconds.png', '30-seconds.png', '45-seconds.png', '60-seconds.png')) {
    $screenshot = Join-Path $absoluteScreenshots $name
    if (-not (Test-Path -LiteralPath $screenshot -PathType Leaf) -or
        (Get-Item -LiteralPath $screenshot).Length -le 0) {
        [Console]::Error.WriteLine("G4.0 screenshot is missing or empty: $screenshot")
        exit 5
    }
}
if (-not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Qinglan G4.0 Visual Acceptance] PASS' -Quiet)) {
    [Console]::Error.WriteLine("G4.0 Player log has no PASS marker: $absoluteLog")
    exit 5
}
Write-Host "Qinglan G4.0 sixty-second Player gate: PASS ($absoluteResult)"
exit 0
