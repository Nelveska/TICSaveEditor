<#
.SYNOPSIS
    Regenerates TICSaveEditor.Core/Resources/Nex/<lang>/<Table>.json from FFT:TIC
    .nxd files via FF16Tools.CLI + the local NexJsonExporter helper tool.

.DESCRIPTION
    Dev-only orchestrator. Replaces the manual M7 recipe (FF16Tools.CLI +
    SQLiteStudio GUI export). Never invoked by the editor at runtime; never
    shipped to end users. See memory: decisions_build_pipeline_automation.md.

    Pipeline stages:
      1. (optional) Run FF16Tools.CLI unpack-all-packs against -GameRoot to
         produce a staging directory with `nxd/` files. Skipped if -NxdDir is
         supplied (the typical fast path).
      2. Run FF16Tools.CLI nxd-to-sqlite to convert nxd files into a SQLite DB.
      3. For each (Table, Lang) pair in -Tables x -Languages, invoke
         tools/NexJsonExporter to write Resources/Nex/<lang>/<Table>.json in
         the exact DB Browser positional-rows shape that the Nex readers
         expect verbatim.

.PARAMETER GameRoot
    FFT:TIC install root. Auto-detected from Steam libraryfolders.vdf if
    omitted. Required only if the unpack step actually runs (i.e. -NxdDir is
    not supplied or doesn't exist yet).

.PARAMETER NxdDir
    Pre-unpacked directory containing the .nxd files (output of
    FF16Tools.CLI unpack-all-packs). Skips stage 1 entirely. Default loaded
    from scripts/_config.local.ps1 if present.

.PARAMETER FF16ToolsPath
    Path to FF16Tools.CLI.exe. Default loaded from scripts/_config.local.ps1.

.PARAMETER WorkDir
    Staging directory for the SQLite DB. Default: an OS temp directory.

.PARAMETER OutputRoot
    Resources/Nex output root. Defaults to the in-repo
    TICSaveEditor.Core/Resources/Nex directory.

.PARAMETER Tables
    Logical table names (without -lang suffix). Default: the v0.1 manifest.

.PARAMETER Languages
    Languages to extract per table. Default: en, fr, ja, de.

.PARAMETER NoLangSuffix
    Logical table names that are NOT localized (one .nxd per table, no -lang).
    Currently empty (all v0.1 tables are localized).

.PARAMETER Configuration
    NexJsonExporter build configuration. Default: Release.

.EXAMPLE
    # Fast path with pre-unpacked nxd files at a known location:
    pwsh -File scripts/extract-bundled-game-data.ps1 `
        -NxdDir D:\Repos\Modding\FFT\UnpackedGameFiles\nxd `
        -FF16ToolsPath D:\Downloads\Firefox\FF16Tools.CLI-win-x64\win-x64\FF16Tools.CLI.exe

.EXAMPLE
    # Full pipeline starting from a Steam install:
    pwsh -File scripts/extract-bundled-game-data.ps1 `
        -GameRoot 'C:\Program Files (x86)\Steam\steamapps\common\Final Fantasy Tactics The Ivalice Chronicles' `
        -FF16ToolsPath D:\Downloads\Firefox\FF16Tools.CLI-win-x64\win-x64\FF16Tools.CLI.exe
#>
[CmdletBinding()]
param(
    [string] $GameRoot,
    [string] $NxdDir,
    [string] $FF16ToolsPath,
    [string] $WorkDir,
    [string] $OutputRoot,
    [string[]] $Tables = @('Job', 'Item', 'Ability', 'JobCommand', 'CharaName', 'UIStatusEffect'),
    [string[]] $Languages = @('en', 'fr', 'ja', 'de'),
    [string[]] $NoLangSuffix = @(),
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Split any comma-bearing single-string entries -- accommodates invocation from
# bash/cmd where '-Tables Job,Item,...' arrives as one element instead of an array.
$Tables = @($Tables | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$Languages = @($Languages | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$NoLangSuffix = @($NoLangSuffix | ForEach-Object { $_ -split ',' } | Where-Object { $_ })

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$ConfigLocal = Join-Path $PSScriptRoot '_config.local.ps1'
if (Test-Path $ConfigLocal) {
    Write-Verbose "Loading $ConfigLocal"
    . $ConfigLocal
    if (-not $NxdDir -and (Get-Variable -Name DefaultNxdDir -ErrorAction SilentlyContinue)) {
        $NxdDir = $DefaultNxdDir
    }
    if (-not $FF16ToolsPath -and (Get-Variable -Name DefaultFF16ToolsPath -ErrorAction SilentlyContinue)) {
        $FF16ToolsPath = $DefaultFF16ToolsPath
    }
    if (-not $GameRoot -and (Get-Variable -Name DefaultGameRoot -ErrorAction SilentlyContinue)) {
        $GameRoot = $DefaultGameRoot
    }
}

if (-not $OutputRoot) {
    $OutputRoot = Join-Path $RepoRoot 'TICSaveEditor.Core\Resources\Nex'
}
if (-not $WorkDir) {
    $WorkDir = Join-Path ([System.IO.Path]::GetTempPath()) "TICSaveEditor-extract-$([System.IO.Path]::GetRandomFileName())"
}

# ------------------------------------------------------------------ helpers ---

function Find-FfttIvcRoot {
    param([Parameter(Mandatory)] [string] $LibraryFoldersVdfPath)

    if (-not (Test-Path $LibraryFoldersVdfPath)) {
        return $null
    }
    # libraryfolders.vdf is line-oriented; "path" lines hold each library root.
    # Naive parser is sufficient -- Valve's VDF schema is stable and we only need
    # the path values.
    $libraries = @()
    foreach ($line in Get-Content $LibraryFoldersVdfPath) {
        if ($line -match '^\s*"path"\s+"(.+?)"\s*$') {
            $libraries += $matches[1] -replace '\\\\', '\'
        }
    }
    foreach ($lib in $libraries) {
        $candidate = Join-Path $lib 'steamapps\common\Final Fantasy Tactics The Ivalice Chronicles'
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

function Resolve-GameRoot {
    if ($script:GameRoot) {
        if (-not (Test-Path $script:GameRoot)) {
            throw "GameRoot does not exist: $script:GameRoot"
        }
        return (Resolve-Path $script:GameRoot).Path
    }
    $candidatePaths = @(
        "${env:ProgramFiles(x86)}\Steam\steamapps\libraryfolders.vdf"
        "$env:ProgramFiles\Steam\steamapps\libraryfolders.vdf"
    )
    foreach ($vdf in $candidatePaths) {
        $found = Find-FfttIvcRoot -LibraryFoldersVdfPath $vdf
        if ($found) {
            Write-Host "Auto-detected GameRoot via $vdf : $found"
            return $found
        }
    }
    throw "GameRoot not specified and Steam libraryfolders.vdf scan didn't find FFT:TIC. Pass -GameRoot explicitly."
}

function Invoke-FF16Tools {
    param([Parameter(Mandatory)][string[]] $Args)
    if (-not $script:FF16ToolsPath) {
        throw "FF16ToolsPath not specified. Pass -FF16ToolsPath or set DefaultFF16ToolsPath in scripts/_config.local.ps1."
    }
    if (-not (Test-Path $script:FF16ToolsPath)) {
        throw "FF16Tools.CLI not found at: $script:FF16ToolsPath"
    }
    Write-Host "  > FF16Tools.CLI $($Args -join ' ')"
    & $script:FF16ToolsPath @Args
    if ($LASTEXITCODE -ne 0) {
        throw "FF16Tools.CLI exited with code $LASTEXITCODE"
    }
}

# ----------------------------------------------------------------- pipeline ---

Write-Host "TICSaveEditor build-pipeline extraction"
Write-Host "  RepoRoot:    $RepoRoot"
Write-Host "  OutputRoot:  $OutputRoot"
Write-Host "  WorkDir:     $WorkDir"
Write-Host "  Tables:      $($Tables -join ', ')"
Write-Host "  Languages:   $($Languages -join ', ')"
Write-Host ""

New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null

# Stage 1: locate the nxd directory, unpacking from -GameRoot if missing.
if (-not $NxdDir -or -not (Test-Path $NxdDir)) {
    $resolvedGameRoot = Resolve-GameRoot
    $unpackOut = Join-Path $WorkDir 'unpacked'
    $sourcePacs = Join-Path $resolvedGameRoot 'FFTIVC\data\enhanced'
    if (-not (Test-Path $sourcePacs)) {
        throw "Expected pack directory not found: $sourcePacs"
    }
    Write-Host "[1/3] unpacking $sourcePacs -> $unpackOut"
    Invoke-FF16Tools @('unpack-all-packs', '-i', $sourcePacs, '-o', $unpackOut, '-g', 'fft')
    $NxdDir = Join-Path $unpackOut 'nxd'
} else {
    Write-Host "[1/3] using pre-unpacked nxd directory: $NxdDir  (skipping unpack-all-packs)"
}

if (-not (Test-Path $NxdDir)) {
    throw "Expected nxd directory not found after unpack: $NxdDir"
}

# Stage 2: nxd -> SQLite.
$dbPath = Join-Path $WorkDir 'fft_tables.db'
if (Test-Path $dbPath) { Remove-Item $dbPath -Force }
Write-Host "[2/3] nxd-to-sqlite $NxdDir -> $dbPath"
Invoke-FF16Tools @('nxd-to-sqlite', '-i', $NxdDir, '-o', $dbPath, '-g', 'fft')
if (-not (Test-Path $dbPath)) {
    throw "FF16Tools didn't produce expected SQLite DB at $dbPath"
}

# Stage 3: SQLite -> JSON via NexJsonExporter.
$exporterProj = Join-Path $RepoRoot 'tools\NexJsonExporter\NexJsonExporter.csproj'
if (-not (Test-Path $exporterProj)) {
    throw "NexJsonExporter project not found at $exporterProj"
}

# Build once up front so subsequent invocations skip restore/build.
Write-Host "[3/3] building NexJsonExporter ($Configuration) ..."
& dotnet build $exporterProj -c $Configuration --nologo -v quiet | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "NexJsonExporter build failed (exit $LASTEXITCODE)"
}

$exporterDll = Join-Path $RepoRoot "tools\NexJsonExporter\bin\$Configuration\net10.0\NexJsonExporter.dll"
if (-not (Test-Path $exporterDll)) {
    throw "NexJsonExporter assembly not produced at $exporterDll"
}

$totalJobs = 0
$succeeded = @()
$missing = @()

foreach ($table in $Tables) {
    foreach ($lang in $Languages) {
        $totalJobs++
        $sqliteName = if ($NoLangSuffix -contains $table) { $table } else { "$table-$lang" }
        $outDir = Join-Path $OutputRoot $lang
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
        $outPath = Join-Path $outDir "$table.json"

        Write-Host "  $sqliteName -> $outPath"
        & dotnet $exporterDll --db $dbPath --table $sqliteName --out $outPath
        if ($LASTEXITCODE -eq 0) {
            $succeeded += [pscustomobject]@{ Table = $table; Lang = $lang; Path = $outPath }
        } else {
            $missing += [pscustomobject]@{ Table = $table; Lang = $lang; SqliteName = $sqliteName }
        }
    }
}

Write-Host ""
Write-Host "Summary: $($succeeded.Count)/$totalJobs JSON files written."
if ($missing.Count -gt 0) {
    Write-Host "Missing tables (table not found in SQLite DB; not necessarily an error if locale unavailable):"
    foreach ($m in $missing) {
        Write-Host "  - $($m.SqliteName)"
    }
}
Write-Host ""
Write-Host "Done. Run scripts/validate-bundled-game-data.ps1 to sanity-check the output."
