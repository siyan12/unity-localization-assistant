[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ResultsPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ResultsPath)) {
    throw "Unity test results path does not exist: $ResultsPath"
}

$candidates = if (Test-Path -LiteralPath $ResultsPath -PathType Container) {
    Get-ChildItem -LiteralPath $ResultsPath -Filter '*.xml' -File -Recurse
} else {
    Get-Item -LiteralPath $ResultsPath
}

$testResult = $null
foreach ($candidate in $candidates) {
    try {
        [xml] $document = Get-Content -LiteralPath $candidate.FullName -Raw
        if ($document.DocumentElement.Name -in @('test-run', 'test-results')) {
            $testResult = @{
                Document = $document
                Path = $candidate.FullName
            }
            break
        }
    } catch {
        continue
    }
}

if ($null -eq $testResult) {
    throw "No NUnit test results XML was found under '$ResultsPath'."
}

$root = $testResult.Document.DocumentElement
$totalText = if ($root.HasAttribute('total')) {
    $root.GetAttribute('total')
} else {
    $root.GetAttribute('testcasecount')
}
$failedText = if ($root.HasAttribute('failed')) {
    $root.GetAttribute('failed')
} else {
    $root.GetAttribute('failures')
}

$total = 0
$failed = 0
if (-not [int]::TryParse($totalText, [ref] $total)) {
    throw "NUnit results have no valid test count: $($testResult.Path)"
}
if ($failedText -and -not [int]::TryParse($failedText, [ref] $failed)) {
    throw "NUnit results have an invalid failure count '$failedText': $($testResult.Path)"
}

if ($total -lt 1) {
    throw "Unity did not discover any tests: $($testResult.Path)"
}
if ($failed -gt 0 -or $root.GetAttribute('result') -eq 'Failed') {
    throw "Unity reported $failed failed test(s) out of ${total}: $($testResult.Path)"
}

Write-Host "Verified Unity test results: $total total, $failed failed ($($testResult.Path))."
