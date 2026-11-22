<#
.SYNOPSIS
    Builds the MSI installer locally, mirroring the publish and installer steps of release.yml.

.DESCRIPTION
    1. Publishes the app (self-contained, single file, ReadyToRun) into installer/publish.
    2. Compiles installer/Product.wxs with the WiX v3 tools (candle, light); no MSBuild integration needed.
    3. Renames the result to StarfieldLoadOrderKeeper-<version>.msi in installer/bin/Release.

    The version is taken from the csproj <Version> element unless -Version is given.
    Nothing is committed, tagged or pushed.

.PARAMETER Version
    Version to build, such as 2.0.0 or 2.1.0-beta1. Defaults to the csproj <Version>.
#>
[CmdletBinding()]
param(
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $repoRoot 'Starfield Load Order Keeper.csproj'
$installerDir = Join-Path $repoRoot 'installer'
$publishDir = Join-Path $installerDir 'publish'

function Find-WixBin {
    # release.yml installs WiX v3 via Chocolatey. The installer ships the compiler tools (candle, light)
    # but no MSBuild integration for Visual Studio 2022, so the MSI is compiled with the tools directly.
    $roots = @()
    if ($env:WIX) { $roots += $env:WIX }
    $roots += Get-ChildItem -Path ${env:ProgramFiles(x86)} -Directory -Filter 'WiX Toolset v3*' -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending | ForEach-Object { $_.FullName }

    foreach ($root in $roots) {
        $bin = Join-Path $root 'bin'
        if ((Test-Path (Join-Path $bin 'candle.exe')) -and (Test-Path (Join-Path $bin 'light.exe'))) {
            return $bin
        }
    }

    throw 'WiX Toolset v3 not found. Install it first (as release.yml does): choco install wixtoolset -y'
}

# --- Resolve the version -------------------------------------------------------------------

if ([string]::IsNullOrWhiteSpace($Version)) {
    $match = Select-String -Path $csproj -Pattern '<Version>\s*([^<\s]+)\s*</Version>' | Select-Object -First 1
    if (-not $match) { throw "No <Version> element found in $csproj." }
    $Version = $match.Matches[0].Groups[1].Value
}

# Same parsing and MSI-safe fourth segment as release.yml.
$pattern = '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<pre>[0-9A-Za-z.-]+))?$'
$parsed = [regex]::Match($Version, $pattern)
if (-not $parsed.Success) {
    throw "Version '$Version' does not follow the expected semantic versioning pattern."
}

$build = 0
if ($parsed.Groups['pre'].Success) {
    $digits = [regex]::Match($parsed.Groups['pre'].Value, '\d+').Value
    $build = if ([string]::IsNullOrEmpty($digits)) { 1 } else { [Math]::Min([int]$digits, 65535) }
}
$msiVersion = '{0}.{1}.{2}.{3}' -f $parsed.Groups['major'].Value, $parsed.Groups['minor'].Value, $parsed.Groups['patch'].Value, $build

# --- Prerequisites -------------------------------------------------------------------------

$wixBin = Find-WixBin

Write-Host "Building MSI for $Version (MSI version $msiVersion)" -ForegroundColor Cyan
Write-Host "WiX: $wixBin"

# --- Publish -------------------------------------------------------------------------------

if (Test-Path $publishDir) {
    Remove-Item -Recurse -Force $publishDir
}

Write-Host ''
Write-Host 'Publishing application...' -ForegroundColor Cyan
& dotnet publish $csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o $publishDir `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    "-p:AssemblyVersion=$msiVersion" `
    "-p:FileVersion=$msiVersion" `
    "-p:InformationalVersion=$Version"
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# --- Installer -----------------------------------------------------------------------------

Write-Host ''
Write-Host 'Building MSI installer...' -ForegroundColor Cyan

$objDir = Join-Path $installerDir 'obj\Release\'
$outDir = Join-Path $installerDir 'bin\Release'
New-Item -ItemType Directory -Force -Path $objDir, $outDir | Out-Null

$wxs = Join-Path $installerDir 'Product.wxs'
$wixobj = Join-Path $objDir 'Product.wixobj'
$source = Join-Path $outDir 'StarfieldLoadOrderKeeper.msi'

& (Join-Path $wixBin 'candle.exe') -nologo -arch x64 `
    "-dProductVersion=$msiVersion" "-dPublishDir=$publishDir" `
    -out $wixobj $wxs
if ($LASTEXITCODE -ne 0) { throw 'WiX candle (compile) failed.' }

& (Join-Path $wixBin 'light.exe') -nologo -out $source $wixobj
if ($LASTEXITCODE -ne 0) { throw 'WiX light (link) failed.' }

if (-not (Test-Path $source)) { throw "Expected MSI not found: $source" }

$target = Join-Path $outDir "StarfieldLoadOrderKeeper-$Version.msi"
Move-Item -Path $source -Destination $target -Force

Write-Host ''
Write-Host "MSI created: $target" -ForegroundColor Green
