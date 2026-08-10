[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/compliance.json',
    [string]$LogPath = 'TestResults/QinglanDemo/G3.6/compliance.log'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
if ([string]::IsNullOrWhiteSpace($env:UNITY_PATH) -or
    -not (Test-Path -LiteralPath $env:UNITY_PATH -PathType Leaf)) {
    [Console]::Error.WriteLine('UNITY_PATH must point to the locked Unity executable.')
    exit 2
}
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
$absoluteOutput = Resolve-ProjectPath $OutputPath
$absoluteLog = Resolve-ProjectPath $LogPath
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteOutput) -Force | Out-Null
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteLog) -Force | Out-Null
foreach ($path in @($absoluteOutput, $absoluteLog)) {
    if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path -Force }
}

$previousOutput = $env:QINGLAN_G36_COMPLIANCE_RESULT
try {
    $env:QINGLAN_G36_COMPLIANCE_RESULT = $absoluteOutput
    $arguments = @(
        '-batchmode',
        '-projectPath', $projectRoot,
        '-executeMethod', 'Game.Editor.QinglanG36ComplianceCommand.Run',
        '-logFile', $absoluteLog
    )
    $process = Start-Process -FilePath $env:UNITY_PATH -ArgumentList $arguments `
        -PassThru -WindowStyle Hidden
    [void]$process.WaitForExit()
    $process.Refresh()
} finally {
    $env:QINGLAN_G36_COMPLIANCE_RESULT = $previousOutput
}
Write-Host "G3.6 compliance Unity exit code: $($process.ExitCode)"
if ($process.ExitCode -ne 0) { exit $process.ExitCode }
if (-not (Test-Path -LiteralPath $absoluteOutput -PathType Leaf)) {
    [Console]::Error.WriteLine("Compliance JSON is missing: $absoluteOutput")
    exit 4
}
try { $result = Get-Content -LiteralPath $absoluteOutput -Raw | ConvertFrom-Json }
catch { [Console]::Error.WriteLine("Compliance JSON is invalid: $($_.Exception.Message)"); exit 5 }
if ($result.status -ne 'PASS' -or [int]$result.issueCount -ne 0 -or
    $result.packId -ne 'qinglan.pack.demo' -or $result.packVersion -ne '0.10.0' -or
    -not [bool]$result.packOfficial -or [int]$result.definitionCount -ne 193 -or
    [int]$result.placeholderCount -ne 0 -or [int]$result.releaseAddressableEntryCount -le 0) {
    [Console]::Error.WriteLine('G3.6 compliance contract did not pass.')
    exit 5
}
if (-not (Select-String -LiteralPath $absoluteLog -SimpleMatch '[Qinglan G3.6 Compliance] PASS' -Quiet)) {
    [Console]::Error.WriteLine('Compliance log has no PASS marker.')
    exit 5
}
Write-Host "Qinglan G3.6 compliance: PASS ($absoluteOutput)"
exit 0
