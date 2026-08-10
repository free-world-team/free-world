[CmdletBinding()]
param(
    [string]$ProjectPath = '',
    [string]$CandidateCommit = '9984bcc5582bd827372768993953645d754cc463',
    [string]$CandidateTag = 'qinglan-demo-g3.6-rc2',
    [Parameter(Mandatory = $true)]
    [string]$PerformanceExecutable,
    [Parameter(Mandatory = $true)]
    [Alias('Executable')]
    [string]$ReleaseExecutable,
    [Parameter(Mandatory = $true)]
    [string]$OperatorName,
    [Parameter(Mandatory = $true)]
    [string]$OperatorRole,
    [Parameter(Mandatory = $true)]
    [switch]$AttestPhysicalHardware,
    [string]$Notes = '',
    [string]$OutputPath = 'TestResults/QinglanDemo/G3.6/MinimumSpec/minimum-spec-hardware.json'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = Split-Path -Parent $PSScriptRoot }
$projectRoot = (Resolve-Path -LiteralPath $ProjectPath).Path
function Resolve-ProjectPath([string]$Value) {
    if ([IO.Path]::IsPathRooted($Value)) { return [IO.Path]::GetFullPath($Value) }
    return [IO.Path]::GetFullPath((Join-Path $projectRoot $Value))
}
function Require-Text([string]$Value, [string]$Name) {
    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Name is required." }
}

Require-Text $OperatorName 'OperatorName'
Require-Text $OperatorRole 'OperatorRole'
if (-not $AttestPhysicalHardware) { throw 'AttestPhysicalHardware is required.' }
$candidateExpression = "$CandidateCommit`^{commit`}"
$resolvedCommit = (& git -c "safe.directory=$($projectRoot.Replace('\', '/'))" -C $projectRoot rev-parse $candidateExpression).Trim()
if ($LASTEXITCODE -ne 0 -or $resolvedCommit -notmatch '^[0-9a-f]{40}$') {
    throw "Unable to resolve candidate commit: $CandidateCommit"
}
$resolvedTagCommit = (& git -c "safe.directory=$($projectRoot.Replace('\', '/'))" -C $projectRoot rev-parse "$CandidateTag`^{commit`}").Trim()
if ($LASTEXITCODE -ne 0 -or $resolvedTagCommit -ne $resolvedCommit) {
    throw "Candidate tag $CandidateTag does not resolve to $resolvedCommit."
}
$absolutePerformanceExecutable = Resolve-ProjectPath $PerformanceExecutable
$absoluteReleaseExecutable = Resolve-ProjectPath $ReleaseExecutable
if (-not (Test-Path -LiteralPath $absolutePerformanceExecutable -PathType Leaf)) {
    throw "Performance executable does not exist: $absolutePerformanceExecutable"
}
if (-not (Test-Path -LiteralPath $absoluteReleaseExecutable -PathType Leaf)) {
    throw "Release executable does not exist: $absoluteReleaseExecutable"
}

$computerSystem = Get-CimInstance -ClassName Win32_ComputerSystem
$operatingSystem = Get-CimInstance -ClassName Win32_OperatingSystem
$processors = @(Get-CimInstance -ClassName Win32_Processor)
$graphicsAdapters = @(Get-CimInstance -ClassName Win32_VideoController | Where-Object {
        $_.Name -and $_.Name -notmatch 'Remote|Basic Display|Virtual'
    } | Sort-Object -Property @{ Expression = { [uint64]$_.AdapterRAM }; Descending = $true })
if ($processors.Count -eq 0) { throw 'No physical processor was returned by CIM.' }
if ($graphicsAdapters.Count -eq 0) { throw 'No physical graphics adapter was returned by CIM.' }
$selectedGraphics = $graphicsAdapters[0]
$processorNames = @($processors | ForEach-Object { [string]$_.Name }) -join ' + '
$physicalCores = [int](($processors | Measure-Object -Property NumberOfCores -Sum).Sum)
$logicalProcessors = [int](($processors | Measure-Object -Property NumberOfLogicalProcessors -Sum).Sum)

$capture = [ordered]@{
    schemaVersion = 2
    candidateCommit = $resolvedCommit
    candidateTag = $CandidateTag
    physicalHardware = $true
    captureMethod = 'CIM_WMI_LOCAL'
    capturedAtUtc = [DateTime]::UtcNow.ToString('O')
    operator = [ordered]@{
        kind = 'human'
        name = $OperatorName.Trim()
        role = $OperatorRole.Trim()
        attested = $true
        statement = 'I witnessed this certification on the physical hardware recorded in this file.'
    }
    computerSystem = [ordered]@{
        manufacturer = [string]$computerSystem.Manufacturer
        model = [string]$computerSystem.Model
        totalPhysicalMemoryBytes = [uint64]$computerSystem.TotalPhysicalMemory
    }
    operatingSystem = [ordered]@{
        caption = [string]$operatingSystem.Caption
        version = [string]$operatingSystem.Version
        architecture = [string]$operatingSystem.OSArchitecture
    }
    processor = [ordered]@{
        name = $processorNames.Trim()
        physicalCores = $physicalCores
        logicalProcessors = $logicalProcessors
    }
    graphicsAdapter = [ordered]@{
        name = [string]$selectedGraphics.Name
        adapterRamBytes = [uint64]$selectedGraphics.AdapterRAM
        driverVersion = [string]$selectedGraphics.DriverVersion
    }
    executables = [ordered]@{
        performance = [ordered]@{
            path = $absolutePerformanceExecutable.Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $absolutePerformanceExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        release = [ordered]@{
            path = $absoluteReleaseExecutable.Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $absoluteReleaseExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    notes = $Notes.Trim()
}
$absoluteOutput = Resolve-ProjectPath $OutputPath
New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteOutput) -Force | Out-Null
$json = ($capture | ConvertTo-Json -Depth 6).Replace("`r`n", "`n") + "`n"
[IO.File]::WriteAllText($absoluteOutput, $json, [Text.UTF8Encoding]::new($false))
Write-Host "Qinglan G3.6 minimum-spec hardware capture: $absoluteOutput"
