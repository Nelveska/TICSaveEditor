<#
.SYNOPSIS
    Sanity-checks the JSON files under TICSaveEditor.Core/Resources/Nex/.

.DESCRIPTION
    Dev-only validator. Run after extract-bundled-game-data.ps1 (or any time
    a Nex JSON is hand-edited) to confirm the on-disk shape matches what the
    M7.x readers expect verbatim.

    Validations:
      - Each <lang>/<Table>.json is a single object with:
            type=="table", database==null, name=="<Table>-<lang>",
            withoutRowId==false, strict==false,
            columns=[{name,type},...], rows=[[...],[...]]
      - Every row's length equals columns.length.
      - Spec section11.3 spot-check: en/Job.json contains a row with Key=1 and Name="Squire".

    Exits 0 on success, nonzero on the first failure (early-exit).

.PARAMETER NexRoot
    Resources/Nex root. Defaults to TICSaveEditor.Core/Resources/Nex relative
    to repo root.
#>
[CmdletBinding()]
param(
    [string] $NexRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
if (-not $NexRoot) {
    $NexRoot = Join-Path $RepoRoot 'TICSaveEditor.Core\Resources\Nex'
}
if (-not (Test-Path $NexRoot)) {
    throw "NexRoot does not exist: $NexRoot"
}

$failures = 0
$checked = 0

function Fail([string] $message) {
    Write-Host "FAIL: $message" -ForegroundColor Red
    $script:failures++
}

function Validate-Shape([string] $path, [string] $expectedTableName) {
    $json = Get-Content -Raw -Path $path | ConvertFrom-Json
    foreach ($expected in @(
        @{ Property = 'type'; Value = 'table' }
        @{ Property = 'database'; Value = $null }
        @{ Property = 'name'; Value = $expectedTableName }
        @{ Property = 'withoutRowId'; Value = $false }
        @{ Property = 'strict'; Value = $false }
    )) {
        if ($json.$($expected.Property) -ne $expected.Value -and -not (
                # ConvertFrom-Json represents JSON null as $null, but PowerShell's -ne
                # against $null with non-null properties needs a property-existence check.
                $expected.Value -eq $null -and $null -eq $json.$($expected.Property)
            )) {
            Fail "$path : '$($expected.Property)' expected '$($expected.Value)', got '$($json.$($expected.Property))'"
            return
        }
    }
    if (-not $json.PSObject.Properties.Match('columns')) { Fail "$path : missing 'columns'"; return }
    if (-not $json.PSObject.Properties.Match('rows'))    { Fail "$path : missing 'rows'"; return }
    if ($json.columns.Count -lt 1) { Fail "$path : columns is empty"; return }

    $expectedColCount = $json.columns.Count
    $rowIndex = 0
    foreach ($row in $json.rows) {
        if ($row -isnot [System.Object[]]) {
            Fail "$path : row $rowIndex is not an array (got $($row.GetType().Name)) -- DB Browser positional-rows shape required"
            return
        }
        if ($row.Count -ne $expectedColCount) {
            Fail "$path : row $rowIndex has $($row.Count) cells; expected $expectedColCount (matching columns)"
            return
        }
        $rowIndex++
    }

    $script:checked++
    Write-Host "OK  : $path  ($expectedTableName, $rowIndex rows x $expectedColCount cols)"
}

function Spot-Check-EnJobSquire([string] $jobJsonPath) {
    if (-not (Test-Path $jobJsonPath)) {
        Write-Host "SKIP: en/Job.json not present at $jobJsonPath; spec section11.3 spot-check skipped"
        return
    }
    $json = Get-Content -Raw -Path $jobJsonPath | ConvertFrom-Json

    $colNameToIdx = @{}
    for ($i = 0; $i -lt $json.columns.Count; $i++) {
        $colNameToIdx[$json.columns[$i].name] = $i
    }
    if (-not $colNameToIdx.ContainsKey('Key') -or -not $colNameToIdx.ContainsKey('Name')) {
        Fail "en/Job.json : missing required Key/Name columns"
        return
    }

    $keyIdx = $colNameToIdx['Key']
    $nameIdx = $colNameToIdx['Name']
    $found = $false
    foreach ($row in $json.rows) {
        if ($row[$keyIdx] -eq 1) {
            $name = $row[$nameIdx]
            if ($name -ne 'Squire') {
                Fail "en/Job.json : Key=1 has Name='$name'; expected 'Squire' per spec section11.3"
            } else {
                Write-Host "SPOT: en/Job.json Key=1 -> Name='Squire'  (spec section11.3 spot-check OK)"
            }
            $found = $true
            break
        }
    }
    if (-not $found) {
        Fail "en/Job.json : no row with Key=1 found (spec section11.3 spot-check)"
    }
}

# Walk Nex/<lang>/*.json
$languages = Get-ChildItem -Path $NexRoot -Directory
foreach ($langDir in $languages) {
    $lang = $langDir.Name
    $jsonFiles = Get-ChildItem -Path $langDir.FullName -Filter '*.json'
    foreach ($jsonFile in $jsonFiles) {
        $tableBase = [System.IO.Path]::GetFileNameWithoutExtension($jsonFile.Name)
        Validate-Shape -path $jsonFile.FullName -expectedTableName "$tableBase-$lang"
    }
}

Spot-Check-EnJobSquire -jobJsonPath (Join-Path $NexRoot 'en\Job.json')

Write-Host ""
Write-Host "Validation complete: $checked file(s) checked, $failures failure(s)."

if ($failures -gt 0) {
    exit 1
}
exit 0
