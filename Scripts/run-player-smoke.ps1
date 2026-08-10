[CmdletBinding()]
param(
    [Alias('PlayerPath')]
    [string]$Executable = 'Builds/WindowsRelease/AzureSword.exe',
    [string]$ProjectPath = '',
    [string]$LogPath = 'TestResults/QinglanDemo/G3.6/release-player.log',
    [Alias('OutputPath')]
    [string]$ResultPath = 'TestResults/QinglanDemo/G3.6/release-player.json',
    [string]$SavePath = 'TestResults/QinglanDemo/G3.6/release-player-save',
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
$absoluteExecutable = if ([IO.Path]::IsPathRooted($Executable)) {
    [IO.Path]::GetFullPath($Executable)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $Executable))
}
$absoluteLog = if ([IO.Path]::IsPathRooted($LogPath)) {
    [IO.Path]::GetFullPath($LogPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $LogPath))
}
$absoluteResult = if ([IO.Path]::IsPathRooted($ResultPath)) {
    [IO.Path]::GetFullPath($ResultPath)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $ResultPath))
}
$absoluteSave = if ([IO.Path]::IsPathRooted($SavePath)) {
    [IO.Path]::GetFullPath($SavePath)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $SavePath))
}
if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("Release executable does not exist: $absoluteExecutable")
    exit 2
}
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteLog) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteResult) -Force | Out-Null
New-Item -ItemType Directory -Path $absoluteSave -Force | Out-Null
foreach ($path in @($absoluteLog, $absoluteResult)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}
foreach ($name in @('settings.json', 'settings.json.bak', 'settings.json.tmp',
        'profile.json', 'profile.json.bak', 'profile.json.tmp',
        'run_recovery.json', 'run_recovery.json.bak', 'run_recovery.json.tmp')) {
    $saveFile = Join-Path $absoluteSave $name
    if (Test-Path -LiteralPath $saveFile -PathType Leaf) { Remove-Item -LiteralPath $saveFile -Force }
}

$previousResult = $env:QINGLAN_G36_RELEASE_PLAYER_RESULT
$previousSave = $env:AZURESWORD_SAVE_ROOT
try {
    $env:QINGLAN_G36_RELEASE_PLAYER_RESULT = $absoluteResult
    $env:AZURESWORD_SAVE_ROOT = $absoluteSave
    $process = Start-Process -FilePath $absoluteExecutable `
        -ArgumentList @('-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080', '-qinglanG36ReleaseSmoke', '-logFile', $absoluteLog) `
        -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        $process.Kill($true)
        [Console]::Error.WriteLine("Release Player timed out after $TimeoutSeconds seconds.")
        exit 7
    }
    $process.Refresh()
} finally {
    $env:QINGLAN_G36_RELEASE_PLAYER_RESULT = $previousResult
    $env:AZURESWORD_SAVE_ROOT = $previousSave
}
Write-Host "Release player exit code: $($process.ExitCode)"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if (-not (Test-Path -LiteralPath $absoluteResult -PathType Leaf)) {
    [Console]::Error.WriteLine("Release smoke result is missing: $absoluteResult")
    exit 4
}
try {
    $result = Get-Content -LiteralPath $absoluteResult -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("Release smoke result is invalid: $($_.Exception.Message)")
    exit 5
}
if ($result.status -ne 'PASS' -or -not $result.releaseCandidateRequested -or
    -not $result.releaseContractPassed -or [bool]$result.debugBuild -or
    -not $result.nullPlatform -or [int]$result.contentPackCount -ne 1 -or
    [int]$result.contentDefinitionCount -ne 193 -or
    -not $result.formalVisualsLoaded -or -not $result.formalAudioLoaded -or
    -not $result.formalFontsLoaded -or -not $result.formalLocalizationResolved -or
    -not $result.localeCyclePassed -or -not $result.layoutScalePassed -or
    -not $result.activeRunVisited -or -not $result.pauseResumeVisited -or
    -not $result.upgradeVisited -or -not $result.resultVisited -or
    -not $result.saveCommitted -or -not $result.profileSavePresent -or
    -not $result.runRecoveryCleared -or -not $result.hubVisited -or
    -not $result.restartVisited -or [int]$result.activeViewsAfterHub -ne 0 -or
    [int]$result.inputOwnerCount -ne 1) {
    [Console]::Error.WriteLine('Release Player did not complete the formal Demo lifecycle contract.')
    exit 5
}
if (-not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Qinglan G3.6 Release Player] PASS' -Quiet)) {
    [Console]::Error.WriteLine("Release player log has no PASS marker: $absoluteLog")
    exit 5
}
if (-not (Test-Path -LiteralPath (Join-Path $absoluteSave 'profile.json') -PathType Leaf)) {
    [Console]::Error.WriteLine('Release Player profile save sample is missing.')
    exit 5
}
Write-Host "Qinglan G3.6 Release Player result: PASS ($absoluteResult)"
exit 0
