[CmdletBinding()]
param(
    [ValidateSet('Cpu', 'Quick', 'Target', 'Extension')]
    [string]$Mode = 'Quick',
    [string]$ProjectPath = '',
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe',
    [string]$Executable = 'Builds/WindowsDevelopment/AzureSword.exe',
    [string]$OutputDirectory = 'TestResults/QinglanDemo/G3.5',
    [int]$QuickTicks = 300,
    [int]$QuickEnemies = 120,
    [int]$QuickProjectiles = 90,
    [int]$QuickPickups = 50,
    [int]$QuickVfx = 20,
    [int]$QuickWarmupTicks = 30
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Split-Path -Parent $PSScriptRoot
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
function Restore-Environment([string]$Name, [string]$Value) {
    [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
}

$absoluteOutput = Resolve-ProjectPath $OutputDirectory
New-Item -ItemType Directory -Path $absoluteOutput -Force | Out-Null
if ($Mode -eq 'Cpu') {
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        [Console]::Error.WriteLine("Unity executable does not exist: $UnityPath")
        exit 2
    }
    $resultPath = Join-Path $absoluteOutput 'cpu-target.json'
    $logPath = Join-Path $absoluteOutput 'cpu-target.log'
    foreach ($path in @($resultPath, $logPath)) {
        if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
    }
    $oldOutput = $env:M10_PERFORMANCE_OUTPUT
    $oldEnemy = $env:M10_ENEMY_ID
    $oldTicks = $env:M10_TICK_COUNT
    $oldEnemies = $env:M10_ENEMY_COUNT
    $oldProjectiles = $env:M10_PROJECTILE_COUNT
    $oldPickups = $env:M10_PICKUP_COUNT
    $oldVfx = $env:M10_VFX_COUNT
    $oldWarmup = $env:M10_WARMUP_TICKS
    try {
        $env:M10_PERFORMANCE_OUTPUT = $resultPath
        $env:M10_ENEMY_ID = 'qinglan.enemy.grass_spirit'
        $env:M10_TICK_COUNT = '54000'
        $env:M10_ENEMY_COUNT = '1500'
        $env:M10_PROJECTILE_COUNT = '3000'
        $env:M10_PICKUP_COUNT = '5000'
        $env:M10_VFX_COUNT = '200'
        $env:M10_WARMUP_TICKS = '300'
        $editorProcess = Start-Process -FilePath $UnityPath -ArgumentList @(
            '-batchmode', '-projectPath', $projectRoot,
            '-executeMethod', 'Game.Editor.M10PerformanceCommand.Run', '-logFile', $logPath
        ) -PassThru -Wait -WindowStyle Hidden
        $exitCode = $editorProcess.ExitCode
    } finally {
        Restore-Environment 'M10_PERFORMANCE_OUTPUT' $oldOutput
        Restore-Environment 'M10_ENEMY_ID' $oldEnemy
        Restore-Environment 'M10_TICK_COUNT' $oldTicks
        Restore-Environment 'M10_ENEMY_COUNT' $oldEnemies
        Restore-Environment 'M10_PROJECTILE_COUNT' $oldProjectiles
        Restore-Environment 'M10_PICKUP_COUNT' $oldPickups
        Restore-Environment 'M10_VFX_COUNT' $oldVfx
        Restore-Environment 'M10_WARMUP_TICKS' $oldWarmup
    }
    if ($exitCode -ne 0) { exit $exitCode }
    if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
        [Console]::Error.WriteLine("G3.5 CPU result is missing: $resultPath")
        exit 4
    }
    $cpu = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
    if ($cpu.status -ne 'PASS' -or $cpu.configuration.enemyId -ne 'qinglan.enemy.grass_spirit' -or
        [int]$cpu.configuration.tickCount -ne 54000 -or [int]$cpu.configuration.enemies -ne 1500 -or
        [int]$cpu.configuration.projectiles -ne 3000 -or [int]$cpu.configuration.pickups -ne 5000 -or
        -not $cpu.budgets.tickP99WithinBudget -or -not $cpu.budgets.zeroHotPathManagedAllocation -or
        -not $cpu.budgets.noGcCollections -or -not $cpu.budgets.noSustainedMemoryGrowth) {
        [Console]::Error.WriteLine('G3.5 formal-content CPU target gate failed.')
        exit 5
    }
    Write-Host "Qinglan G3.5 CPU target result: PASS ($resultPath)"
    exit 0
}

$absoluteExecutable = Resolve-ProjectPath $Executable
if (-not (Test-Path -LiteralPath $absoluteExecutable -PathType Leaf)) {
    [Console]::Error.WriteLine("Development executable does not exist: $absoluteExecutable")
    exit 2
}
$profile = $Mode.ToLowerInvariant()
$resultPath = Join-Path $absoluteOutput "$profile-player.json"
$logPath = Join-Path $absoluteOutput "$profile-player.log"
foreach ($path in @($resultPath, $logPath)) {
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}
$gitSha = (& git -c "safe.directory=$($projectRoot.Replace('\', '/'))" -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $gitSha -notmatch '^[0-9a-f]{40}$') {
    [Console]::Error.WriteLine('Unable to resolve the Git SHA for G3.5 provenance.')
    exit 3
}

$names = @(
    'QINGLAN_G35_OUTPUT', 'QINGLAN_G35_PROFILE', 'QINGLAN_G35_GIT_SHA',
    'QINGLAN_G35_PACK_VERSION', 'QINGLAN_G35_PACK_HASH', 'QINGLAN_G35_TICKS',
    'QINGLAN_G35_ENEMIES', 'QINGLAN_G35_PROJECTILES', 'QINGLAN_G35_PICKUPS',
    'QINGLAN_G35_VFX', 'QINGLAN_G35_WARMUP_TICKS'
)
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    $env:QINGLAN_G35_OUTPUT = $resultPath
    $env:QINGLAN_G35_PROFILE = $profile
    $env:QINGLAN_G35_GIT_SHA = $gitSha
    $env:QINGLAN_G35_PACK_VERSION = '0.10.0'
    $env:QINGLAN_G35_PACK_HASH = '8900fedffde84c2d014c260d50bff1833a1a98f378a4b22ac08ea4a3ec40d21f'
    if ($Mode -eq 'Quick') {
        $env:QINGLAN_G35_TICKS = $QuickTicks.ToString([Globalization.CultureInfo]::InvariantCulture)
        $env:QINGLAN_G35_ENEMIES = $QuickEnemies.ToString([Globalization.CultureInfo]::InvariantCulture)
        $env:QINGLAN_G35_PROJECTILES = $QuickProjectiles.ToString([Globalization.CultureInfo]::InvariantCulture)
        $env:QINGLAN_G35_PICKUPS = $QuickPickups.ToString([Globalization.CultureInfo]::InvariantCulture)
        $env:QINGLAN_G35_VFX = $QuickVfx.ToString([Globalization.CultureInfo]::InvariantCulture)
        $env:QINGLAN_G35_WARMUP_TICKS = $QuickWarmupTicks.ToString([Globalization.CultureInfo]::InvariantCulture)
    }
    $playerArguments = @(
        '-force-d3d11', '-screen-fullscreen', '0', '-screen-width', '1920', '-screen-height', '1080',
        '-qinglanG35Performance', '-logFile', $logPath
    )
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $absoluteExecutable
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $false
    foreach ($argument in $playerArguments) { [void]$startInfo.ArgumentList.Add($argument) }
    $playerProcess = [Diagnostics.Process]::new()
    $playerProcess.StartInfo = $startInfo
    [void]$playerProcess.Start()
    $playerProcess.WaitForExit()
    $exitCode = $playerProcess.ExitCode
} finally {
    foreach ($name in $names) { Restore-Environment $name $previous[$name] }
}
if ($exitCode -ne 0) { exit $exitCode }
if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    [Console]::Error.WriteLine("G3.5 Player result is missing: $resultPath")
    exit 4
}
try {
    $result = Get-Content -LiteralPath $resultPath -Raw | ConvertFrom-Json
} catch {
    [Console]::Error.WriteLine("G3.5 Player result is invalid: $($_.Exception.Message)")
    exit 5
}
if ($result.status -ne 'PASS' -or $result.configuration.profile -ne $profile -or
    -not $result.budgets.configurationMatchesProfile -or -not $result.budgets.exactEntityCounts -or
    -not $result.budgets.formalAssetsResolved -or -not $result.budgets.averageFpsWithinBudget -or
    -not $result.budgets.onePercentLowWithinBudget -or -not $result.budgets.gpuP99WithinBudget -or
    -not $result.budgets.tickP99WithinBudget -or -not $result.budgets.zeroHotPathManagedAllocation -or
    -not $result.budgets.zeroProfilerGcAllocation -or -not $result.budgets.noGcCollections -or
    -not $result.budgets.noSustainedMemoryGrowth -or -not $result.budgets.renderRecordersAvailable -or
    -not $result.budgets.noPoolExpansionOrDrops -or -not $result.budgets.correctResolution -or
    -not $result.budgets.correctGraphicsApi -or -not $result.budgets.correctQuality -or
    -not $result.budgets.uninterruptedFocus -or -not $result.budgets.physicalDisplayAdapter -or
    -not $result.budgets.provenanceRecorded -or -not $result.budgets.noFrameSampleOverflow) {
    [Console]::Error.WriteLine("G3.5 $profile Player gate failed: $($result.failureReason)")
    exit 5
}
if ($Mode -eq 'Target' -and (-not $result.configuration.certificationEligible -or
    [int]$result.configuration.tickCount -ne 54000 -or [int]$result.configuration.enemies -ne 1200)) {
    [Console]::Error.WriteLine('G3.5 target result is not certification eligible.')
    exit 5
}
if ($Mode -eq 'Extension' -and ([int]$result.configuration.tickCount -ne 9000 -or
    [int]$result.configuration.enemies -ne 2000)) {
    [Console]::Error.WriteLine('G3.5 extension result has the wrong pressure configuration.')
    exit 5
}
if (-not (Select-String -LiteralPath $logPath -SimpleMatch '[Qinglan G3.5 Performance] PASS' -Quiet)) {
    [Console]::Error.WriteLine("G3.5 Player log has no PASS marker: $logPath")
    exit 5
}
Write-Host "Qinglan G3.5 $profile Player result: PASS ($resultPath)"
exit 0
