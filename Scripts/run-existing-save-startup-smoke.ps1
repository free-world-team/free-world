[CmdletBinding()]
param(
    [string]$Executable = 'Builds/WindowsRelease/AzureSword.exe',
    [string]$ProjectPath = '',
    [string]$SavePath = '',
    [string]$LogPath = 'TestResults/QinglanDemo/G4.1/existing-save-startup.log',
    [string]$ResultPath = 'TestResults/QinglanDemo/G4.1/existing-save-startup.json',
    [int]$TimeoutSeconds = 30,
    [int]$RequiredStableSamples = 10
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
$absoluteSave = if ([string]::IsNullOrWhiteSpace($SavePath)) {
    [IO.Path]::GetFullPath((Join-Path $projectRoot 'TestResults/QinglanDemo/G4.1/existing-save'))
} elseif ([IO.Path]::IsPathRooted($SavePath)) {
    [IO.Path]::GetFullPath($SavePath)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectRoot $SavePath))
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

if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("Release executable does not exist: $absoluteExecutable")
    exit 2
}
foreach ($name in @('settings.json', 'profile.json')) {
    $path = Join-Path $absoluteSave $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        [Console]::Error.WriteLine("Existing-save startup gate requires $path")
        exit 3
    }
}
if ($TimeoutSeconds -lt 5 -or $RequiredStableSamples -lt 2) {
    [Console]::Error.WriteLine('TimeoutSeconds must be at least 5 and RequiredStableSamples at least 2.')
    exit 2
}

New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteLog) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteResult) -Force | Out-Null
foreach ($path in @($absoluteLog, $absoluteResult)) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}

$settingsPath = Join-Path $absoluteSave 'settings.json'
$profilePath = Join-Path $absoluteSave 'profile.json'
$settingsHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $settingsPath).Hash
$profileHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $profilePath).Hash
$previousSave = $env:AZURESWORD_SAVE_ROOT
$process = $null
$status = 'FAIL'
$errorMessage = ''
$bootstrapObserved = $false
$stableSamples = 0
$maximumWorkingSetBytes = 0L
$startedAt = Get-Date

try {
    $env:AZURESWORD_SAVE_ROOT = $absoluteSave
    $process = Start-Process `
        -FilePath $absoluteExecutable `
        -WorkingDirectory (Split-Path -Parent $absoluteExecutable) `
        -ArgumentList @(
            '-screen-fullscreen', '0',
            '-screen-width', '1600',
            '-screen-height', '900',
            '-logFile', $absoluteLog) `
        -PassThru

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        $process.Refresh()
        if ($process.HasExited) {
            $errorMessage = "Player exited before startup completed (exit $($process.ExitCode))."
            break
        }
        if ($process.WorkingSet64 -gt $maximumWorkingSetBytes) {
            $maximumWorkingSetBytes = $process.WorkingSet64
        }
        if (-not $bootstrapObserved -and (Test-Path -LiteralPath $absoluteLog -PathType Leaf)) {
            $bootstrapObserved = Select-String `
                -LiteralPath $absoluteLog `
                -SimpleMatch '[Bootstrap] Loaded content:' `
                -Quiet
        }
        if ($bootstrapObserved -and $process.Responding -and $process.MainWindowHandle -ne 0) {
            $stableSamples++
            if ($stableSamples -ge $RequiredStableSamples) {
                $status = 'PASS'
                break
            }
        } else {
            $stableSamples = 0
        }
    }
    if ($status -ne 'PASS' -and [string]::IsNullOrWhiteSpace($errorMessage)) {
        $errorMessage = 'Player did not reach a stable responsive window before the timeout.'
    }
} catch {
    $errorMessage = $_.Exception.GetType().Name + ': ' + $_.Exception.Message
} finally {
    $env:AZURESWORD_SAVE_ROOT = $previousSave
    if ($null -ne $process) {
        $process.Refresh()
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            $process.WaitForExit()
        }
    }
}

$settingsHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $settingsPath).Hash
$profileHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $profilePath).Hash
$savesUnchanged = $settingsHashBefore -eq $settingsHashAfter -and $profileHashBefore -eq $profileHashAfter
if (-not $savesUnchanged) {
    $status = 'FAIL'
    $errorMessage = 'Direct startup changed settings.json or profile.json before user interaction.'
}

$result = [ordered]@{
    schemaVersion = 1
    status = $status
    error = $errorMessage
    generatedAtUtc = [DateTime]::UtcNow.ToString('O')
    executable = $absoluteExecutable
    savePath = $absoluteSave
    wallClockSeconds = [Math]::Round(((Get-Date) - $startedAt).TotalSeconds, 3)
    bootstrapObserved = $bootstrapObserved
    stableResponsiveWindowSamples = $stableSamples
    requiredStableResponsiveWindowSamples = $RequiredStableSamples
    maximumWorkingSetBytes = $maximumWorkingSetBytes
    settingsHashBefore = $settingsHashBefore
    settingsHashAfter = $settingsHashAfter
    profileHashBefore = $profileHashBefore
    profileHashAfter = $profileHashAfter
    savesUnchanged = $savesUnchanged
}
$result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $absoluteResult -Encoding UTF8

if ($status -ne 'PASS') {
    [Console]::Error.WriteLine("Existing-save direct startup gate: FAIL - $errorMessage")
    exit 7
}
Write-Host "Existing-save direct startup gate: PASS ($absoluteResult)"
exit 0
