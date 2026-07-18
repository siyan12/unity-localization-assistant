[CmdletBinding()]
param(
    [Parameter()]
    [string] $UnityEditorPath = $env:UNITY_EDITOR_PATH,

    [Parameter()]
    [string] $ProjectPath = (Join-Path $PSScriptRoot '..\Tests\UnityProject'),

    [Parameter()]
    [string] $ArtifactsPath = (Join-Path $PSScriptRoot '..\artifacts\unity')
)

$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw 'PowerShell 7 or newer is required so Unity arguments with spaces can be passed safely.'
}

function Resolve-UnityEditorPath {
    param([string] $RequestedPath)

    if ($RequestedPath) {
        $resolvedRequestedPath = (Resolve-Path -LiteralPath $RequestedPath).Path
        if (-not (Test-Path -LiteralPath $resolvedRequestedPath -PathType Leaf)) {
            throw "Unity Editor executable was not found at '$RequestedPath'."
        }

        return $resolvedRequestedPath
    }

    if ($IsWindows -or $env:OS -eq 'Windows_NT') {
        $hubRoot = 'C:\Program Files\Unity\Hub\Editor'
        if (Test-Path -LiteralPath $hubRoot -PathType Container) {
            $candidate = Get-ChildItem -LiteralPath $hubRoot -Directory |
                Where-Object { $_.Name -like '2022.3.*' } |
                Sort-Object Name -Descending |
                ForEach-Object { Join-Path $_.FullName 'Editor\Unity.exe' } |
                Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
                Select-Object -First 1

            if ($candidate) {
                return $candidate
            }
        }
    }

    throw 'Unity 2022.3 was not found. Pass -UnityEditorPath or set UNITY_EDITOR_PATH.'
}

function Invoke-Unity {
    param(
        [string] $UnityPath,
        [string[]] $Arguments,
        [string] $Phase
    )

    Write-Host "Running Unity $Phase..."
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $UnityPath
    $startInfo.UseShellExecute = $false
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Unity $Phase could not be started."
    }

    $process.WaitForExit()
    if ($process.ExitCode -ne 0) {
        throw "Unity $Phase exited with code $($process.ExitCode)."
    }
}

function Assert-TestResults {
    param([string] $ResultsPath)

    if (-not (Test-Path -LiteralPath $ResultsPath -PathType Leaf)) {
        throw "Unity did not create the expected test results file: $ResultsPath"
    }

    [xml] $results = Get-Content -LiteralPath $ResultsPath -Raw
    $root = $results.DocumentElement
    if ($null -eq $root) {
        throw 'Unity test results XML has no document element.'
    }

    $totalText = if ($root.HasAttribute('total')) { $root.GetAttribute('total') } else { $root.GetAttribute('testcasecount') }
    $failedText = if ($root.HasAttribute('failed')) { $root.GetAttribute('failed') } else { $root.GetAttribute('failures') }

    $total = 0
    $failed = 0
    if (-not [int]::TryParse($totalText, [ref] $total)) {
        throw "Unity test results XML has no valid test count on <$($root.Name)>."
    }
    if ($failedText -and -not [int]::TryParse($failedText, [ref] $failed)) {
        throw "Unity test results XML has an invalid failure count '$failedText'."
    }

    if ($total -lt 1) {
        throw 'Unity completed without discovering any tests.'
    }
    if ($failed -gt 0 -or $root.GetAttribute('result') -eq 'Failed') {
        throw "Unity reported $failed failed test(s) out of $total."
    }

    Write-Host "Unity EditMode tests passed: $total total, $failed failed."
}

$unityPath = Resolve-UnityEditorPath -RequestedPath $UnityEditorPath
$resolvedProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$resolvedArtifactsPath = [System.IO.Path]::GetFullPath($ArtifactsPath)
New-Item -ItemType Directory -Path $resolvedArtifactsPath -Force | Out-Null

$importLog = Join-Path $resolvedArtifactsPath 'import.log'
$testLog = Join-Path $resolvedArtifactsPath 'editmode.log'
$testResults = Join-Path $resolvedArtifactsPath 'editmode-results.xml'

Invoke-Unity -UnityPath $unityPath -Phase 'package import and compilation' -Arguments @(
    '-batchmode',
    '-nographics',
    '-quit',
    '-projectPath', $resolvedProjectPath,
    '-logFile', $importLog
)

Invoke-Unity -UnityPath $unityPath -Phase 'EditMode tests' -Arguments @(
    '-batchmode',
    '-nographics',
    '-projectPath', $resolvedProjectPath,
    '-runTests',
    '-testPlatform', 'EditMode',
    '-testResults', $testResults,
    '-logFile', $testLog
)

Assert-TestResults -ResultsPath $testResults
