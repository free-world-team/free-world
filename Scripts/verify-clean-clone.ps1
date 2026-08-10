[CmdletBinding()]
param(
    [string]$SourceRepository = '',
    [string]$Branch = '',
    [string]$WorkRoot = '',
    [string]$EvidenceOutput = '',
    [switch]$KeepClone
)

$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($SourceRepository)) { $SourceRepository = $sourceRoot }
if ([string]::IsNullOrWhiteSpace($Branch)) {
    $Branch = (& git -C $sourceRoot rev-parse --abbrev-ref HEAD).Trim()
}
if ([string]::IsNullOrWhiteSpace($WorkRoot)) {
    $WorkRoot = Join-Path $env:TEMP ('fw-g36-' + [Guid]::NewGuid().ToString('N'))
}
if ([string]::IsNullOrWhiteSpace($EvidenceOutput)) {
    $EvidenceOutput = Join-Path $sourceRoot 'TestResults/QinglanDemo/G3.6/CleanClone'
}
if ([string]::IsNullOrWhiteSpace($env:UNITY_PATH) -or
    -not (Test-Path -LiteralPath $env:UNITY_PATH -PathType Leaf)) {
    throw 'UNITY_PATH must point to the locked Unity executable.'
}
$resolvedWorkRoot = [IO.Path]::GetFullPath($WorkRoot)
if (Test-Path -LiteralPath $resolvedWorkRoot) {
    throw "Clean-clone target already exists: $resolvedWorkRoot"
}
$cloneRoot = Join-Path $resolvedWorkRoot 'repo'
New-Item -ItemType Directory -Path $resolvedWorkRoot | Out-Null

try {
    & git clone --no-local --single-branch --branch $Branch $SourceRepository $cloneRoot
    if ($LASTEXITCODE -ne 0) { throw "git clone failed with exit code $LASTEXITCODE" }
    $lockedLine = Get-Content -LiteralPath (Join-Path $cloneRoot 'ProjectSettings/ProjectVersion.txt') `
        | Select-String -Pattern '^m_EditorVersion:' | Select-Object -First 1
    $lockedVersion = ($lockedLine.Line -split ':', 2)[1].Trim()
    $editorDirectory = Split-Path -Parent $env:UNITY_PATH
    $installedVersion = Split-Path -Leaf (Split-Path -Parent $editorDirectory)
    if ($installedVersion -ne $lockedVersion) {
        throw "Unity mismatch: project=$lockedVersion installed=$installedVersion"
    }

    $evidence = Join-Path $cloneRoot 'TestResults/QinglanDemo/G3.6/CleanClone'
    & (Join-Path $cloneRoot 'Scripts/test.ps1') -Platform EditMode `
        -ProjectPath $cloneRoot -ResultsDirectory $evidence
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone EditMode failed.' }
    & (Join-Path $cloneRoot 'Scripts/test.ps1') -Platform PlayMode `
        -ProjectPath $cloneRoot -ResultsDirectory $evidence
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone PlayMode failed.' }
    & (Join-Path $cloneRoot 'Scripts/validate.ps1') -ProjectPath $cloneRoot `
        -LogPath (Join-Path $evidence 'validation.log')
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone validation failed.' }
    $oldG28 = $env:QINGLAN_G28_OUTPUT
    try {
        $env:QINGLAN_G28_OUTPUT = Join-Path $evidence 'vertical-slice.json'
        $process = Start-Process -FilePath $env:UNITY_PATH -ArgumentList @(
            '-batchmode', '-projectPath', $cloneRoot,
            '-executeMethod', 'Game.Editor.QinglanG28VerticalSliceCommand.Run',
            '-logFile', (Join-Path $evidence 'vertical-slice.log')) `
            -PassThru -Wait -WindowStyle Hidden
    } finally { $env:QINGLAN_G28_OUTPUT = $oldG28 }
    if ($process.ExitCode -ne 0) { throw 'Clean-clone vertical slice failed.' }
    $oldG34 = $env:QINGLAN_G34_OUTPUT
    try {
        $env:QINGLAN_G34_OUTPUT = Join-Path $evidence 'balance-report.json'
        $process = Start-Process -FilePath $env:UNITY_PATH -ArgumentList @(
            '-batchmode', '-projectPath', $cloneRoot,
            '-executeMethod', 'Game.Editor.QinglanG34BalanceCommand.Run',
            '-logFile', (Join-Path $evidence 'balance.log')) `
            -PassThru -Wait -WindowStyle Hidden
    } finally { $env:QINGLAN_G34_OUTPUT = $oldG34 }
    if ($process.ExitCode -ne 0) { throw 'Clean-clone balance matrix failed.' }
    & (Join-Path $cloneRoot 'Scripts/run-qinglan-g35-performance.ps1') -Mode Cpu `
        -ProjectPath $cloneRoot -UnityPath $env:UNITY_PATH -OutputDirectory $evidence
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone formal CPU target failed.' }
    & (Join-Path $cloneRoot 'Scripts/audit-qinglan-g36-compliance.ps1') `
        -ProjectPath $cloneRoot -OutputPath (Join-Path $evidence 'compliance.json') `
        -LogPath (Join-Path $evidence 'compliance.log')
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone compliance failed.' }
    & (Join-Path $cloneRoot 'Scripts/build-windows.ps1') -ProjectPath $cloneRoot `
        -OutputPath 'Builds/WindowsDevelopment/AzureSword.exe' `
        -LogPath (Join-Path $evidence 'build-development.log') `
        -EvidenceRoot $evidence
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone Development build failed.' }
    & (Join-Path $cloneRoot 'Scripts/build-windows-release.ps1') -ProjectPath $cloneRoot `
        -OutputPath 'Builds/WindowsRelease/AzureSword.exe' `
        -LogPath (Join-Path $evidence 'build-release.log') `
        -EvidenceRoot $evidence
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone Release build failed.' }
    & (Join-Path $cloneRoot 'Scripts/run-player-smoke.ps1') -ProjectPath $cloneRoot `
        -Executable 'Builds/WindowsRelease/AzureSword.exe' `
        -LogPath (Join-Path $evidence 'release-player.log') `
        -ResultPath (Join-Path $evidence 'release-player.json') `
        -SavePath (Join-Path $evidence 'release-player-save')
    if ($LASTEXITCODE -ne 0) { throw 'Clean-clone Release player smoke failed.' }

    New-Item -ItemType Directory -Path $EvidenceOutput -Force | Out-Null
    Copy-Item -Path (Join-Path $evidence '*') -Destination $EvidenceOutput -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $cloneRoot 'Builds/WindowsDevelopment/BuildManifest.json') `
        -Destination (Join-Path $EvidenceOutput 'development-build-manifest.json') -Force
    Copy-Item -LiteralPath (Join-Path $cloneRoot 'Builds/WindowsRelease/BuildManifest.json') `
        -Destination (Join-Path $EvidenceOutput 'release-build-manifest.json') -Force
    $dirtyTracked = & git -C $cloneRoot diff --name-only
    $dirtyStaged = & git -C $cloneRoot diff --cached --name-only
    $untracked = & git -C $cloneRoot ls-files --others --exclude-standard
    if ($dirtyTracked -or $dirtyStaged -or $untracked) {
        throw 'Clean-clone gates left source-controlled or untracked project files behind.'
    }
    $commit = (& git -C $cloneRoot rev-parse HEAD).Trim()
    $summary = [ordered]@{
        schemaVersion = 1
        generatedAtUtc = [DateTime]::UtcNow.ToString('O')
        status = 'PASS'
        commit = $commit
        branch = (& git -C $cloneRoot rev-parse --abbrev-ref HEAD).Trim()
        unityVersion = $lockedVersion
        gates = [ordered]@{
            editMode = 'PASS'; playMode = 'PASS'; validation = 'PASS'
            verticalSlice = 'PASS'; balance = 'PASS'; cpuTarget = 'PASS'
            compliance = 'PASS'; developmentBuild = 'PASS'; releaseBuild = 'PASS'
            releasePlayer = 'PASS'; sourceTreeClean = 'PASS'
        }
    }
    $summary | ConvertTo-Json -Depth 5 | Set-Content `
        -LiteralPath (Join-Path $EvidenceOutput 'clean-clone-summary.json') -Encoding utf8
    Write-Host "Qinglan G3.6 clean clone: PASS ($cloneRoot)"
} finally {
    if ($KeepClone) {
        Write-Host "Clean clone retained: $cloneRoot"
    } else {
        Write-Host "Clean clone retained for audit; remove explicitly after reviewing: $cloneRoot"
    }
}
