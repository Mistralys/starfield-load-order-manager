<#
.SYNOPSIS
    Mechanical pre-release gate for Starfield Load Order Keeper.

.DESCRIPTION
    Runs every check that can be decided by a script: changelog and version progression, git
    state, file encoding, locale consistency, menu wiring, hard-coded UI text, manifest sync,
    packaging, build, tests and dependency advisories. Judgment calls (changelog wording,
    translation quality, risk review) belong to the release-check skill in
    .github/skills/release-check, which runs this script first.

    The script only reads the repository. It never commits, tags, pushes or edits files.
    Exit code: 0 = no blocking problem, 1 = at least one FAIL (or WARN with -Strict), 2 = usage error.

.PARAMETER Only
    Run only these checks (ids, see -List).

.PARAMETER Skip
    Run everything except these checks.

.PARAMETER Quick
    Skip the slow checks: build, tests, dependencies and publish.

.PARAMETER Offline
    Skip every check step that needs the network (advisory database, remote tag lookup).

.PARAMETER Fetch
    Run "git fetch --tags" before comparing with the upstream branch. Off by default because it
    updates local refs.

.PARAMETER AllowDirty
    Report uncommitted changes as a warning instead of a failure (useful while preparing the release).

.PARAMETER IncludePublish
    Also run the self-contained publish of release.yml and inspect its output (slow).

.PARAMETER Strict
    Treat warnings as failures for the exit code.

.PARAMETER JsonPath
    Write the full report as JSON to this path (for the release-check skill or CI).

.PARAMETER List
    List the available checks and exit.

.EXAMPLE
    .\Tools\ReleaseCheck.ps1 -Quick -AllowDirty

.EXAMPLE
    .\Tools\ReleaseCheck.ps1 -Only version,localization
#>
[CmdletBinding()]
param(
    [string[]]$Only = @(),
    [string[]]$Skip = @(),
    [switch]$Quick,
    [switch]$Offline,
    [switch]$Fetch,
    [switch]$AllowDirty,
    [switch]$IncludePublish,
    [switch]$Strict,
    [string]$JsonPath = '',
    [switch]$List,
    [string]$RepoRoot = ''
)

$ErrorActionPreference = 'Stop'

# "powershell -File" passes -Only a,b as one string, so accept comma-separated values too.
$Only = @($Only | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$Skip = @($Skip | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

if ($RepoRoot -eq '') { $RepoRoot = Split-Path -Parent $PSScriptRoot }
$rootPath = (Resolve-Path -LiteralPath $RepoRoot).Path
# The check library keeps its state in script-scope variables (script:RepoRoot, script:Options, ...),
# which share the scope of this script's parameters. Set them only after the parameters are consumed.
$script:RepoRoot = $rootPath
$script:Options = [pscustomobject]@{
    Offline        = [bool]$Offline
    Fetch          = [bool]$Fetch
    AllowDirty     = [bool]$AllowDirty
    IncludePublish = [bool]$IncludePublish
}

$libraryRoot = Join-Path $PSScriptRoot 'ReleaseCheck'
. (Join-Path $libraryRoot 'Common.ps1')
foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $libraryRoot 'Checks') -Filter '*.ps1' | Sort-Object Name)) { . $file.FullName }
$registry = @(
    @{ Id = 'version'; Function = 'Invoke-VersionCheck'; Slow = $false; Title = 'Version and changelog' },
    @{ Id = 'git'; Function = 'Invoke-GitCheck'; Slow = $false; Title = 'Git state' },
    @{ Id = 'unicode'; Function = 'Invoke-UnicodeCheck'; Slow = $false; Title = 'Encoding and unicode integrity' },
    @{ Id = 'hygiene'; Function = 'Invoke-HygieneCheck'; Slow = $false; Title = 'Repository hygiene' },
    @{ Id = 'localization'; Function = 'Invoke-LocalizationCheck'; Slow = $false; Title = 'Localization' },
    @{ Id = 'menus'; Function = 'Invoke-MenuCheck'; Slow = $false; Title = 'Menu consistency' },
    @{ Id = 'hardcoded'; Function = 'Invoke-HardcodedTextCheck'; Slow = $false; Title = 'Hard-coded UI text' },
    @{ Id = 'docs'; Function = 'Invoke-DocsCheck'; Slow = $false; Title = 'Documentation and manifest' },
    @{ Id = 'packaging'; Function = 'Invoke-PackagingCheck'; Slow = $false; Title = 'Packaging and release pipeline' },
    @{ Id = 'build'; Function = 'Invoke-BuildCheck'; Slow = $true; Title = 'Release build' },
    @{ Id = 'tests'; Function = 'Invoke-TestCheck'; Slow = $true; Title = 'Unit tests' },
    @{ Id = 'dependencies'; Function = 'Invoke-DependencyCheck'; Slow = $true; Title = 'NuGet dependencies' },
    @{ Id = 'publish'; Function = 'Invoke-PublishCheck'; Slow = $true; Title = 'Publish smoke test' }
)

if ($List) {
    foreach ($c in $registry) { '{0,-13} {1}{2}' -f $c.Id, $c.Title, $(if ($c.Slow) { '  (slow)' } else { '' }) }
    return
}

$known = @($registry | ForEach-Object { $_.Id })
foreach ($id in @($Only) + @($Skip)) {
    if ($id -and $known -notcontains $id) {
        Write-Host "Unknown check '$id'. Known checks: $($known -join ', ')" -ForegroundColor Red
        exit 2
    }
}

# Optional allow-list for accepted findings.
$allowFile = Join-Path $libraryRoot 'ReleaseCheck.allow.json'
if (Test-Path -LiteralPath $allowFile) {
    try {
        $allow = Get-Content -LiteralPath $allowFile -Raw -Encoding UTF8 | ConvertFrom-Json
        $script:Allow = @($allow.rules | Where-Object { $_ })
        foreach ($rule in $script:Allow) { [void][regex]::new($rule.match) }
    }
    catch {
        Write-Host "Cannot read $allowFile : $($_.Exception.Message)" -ForegroundColor Red
        exit 2
    }
}

function Write-Status {
    param([string]$Status)
    switch ($Status) {
        'PASS' { Write-Host ' PASS ' -ForegroundColor Black -BackgroundColor Green -NoNewline }
        'WARN' { Write-Host ' WARN ' -ForegroundColor Black -BackgroundColor Yellow -NoNewline }
        'FAIL' { Write-Host ' FAIL ' -ForegroundColor White -BackgroundColor Red -NoNewline }
        default { Write-Host ' SKIP ' -ForegroundColor Black -BackgroundColor Gray -NoNewline }
    }
}

# Header.
$topVersion = Get-ChangelogTopVersion
$tags = @(Get-ReleaseTag)
$lastTag = $(if ($tags.Count -gt 0) { $tags[$tags.Count - 1].Tag } else { 'none' })
Write-Host ''
Write-Host 'Starfield Load Order Keeper - release check' -ForegroundColor Cyan
Write-Host ("Release candidate: {0}   Last tag: {1}   Repository: {2}" -f $(if ($topVersion) { "v$topVersion" } else { 'unknown' }), $lastTag, $script:RepoRoot) -ForegroundColor DarkGray
Write-Host ''

# Select and run.
$selected = @($registry | Where-Object {
        ($Only.Count -eq 0 -or $Only -contains $_.Id) -and
        ($Skip -notcontains $_.Id) -and
        -not ($Quick -and $_.Slow -and $Only.Count -eq 0)
    })

$results = New-Object System.Collections.ArrayList
$position = 0
foreach ($check in $selected) {
    $position++
    Write-Host ("[{0}/{1}] {2}..." -f $position, $selected.Count, $check.Title) -ForegroundColor DarkGray
    $watch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $result = & $check.Function
    }
    catch {
        $result = New-CheckResult $check.Id $check.Title
        Add-Finding $result Fail "The check crashed: $($_.Exception.Message)" @($_.ScriptStackTrace -split "`r?`n" | Select-Object -First 4)
    }
    $watch.Stop()
    $result.DurationSec = [Math]::Round($watch.Elapsed.TotalSeconds, 1)
    [void]$results.Add($result)

    Write-Status $result.Status
    Write-Host (' {0}' -f $result.Title) -NoNewline
    Write-Host ('  ({0}s)' -f $result.DurationSec) -ForegroundColor DarkGray

    foreach ($finding in $result.Findings) {
        switch ($finding.Level) {
            'Fail' { $color = 'Red'; $tag = 'FAIL' }
            'Warn' { $color = 'Yellow'; $tag = 'WARN' }
            default { $color = 'DarkGray'; $tag = 'info' }
        }
        Write-Host ("       {0}  {1}" -f $tag, $finding.Message) -ForegroundColor $color
        foreach ($detail in $finding.Details) { Write-Host ("              {0}" -f $detail) -ForegroundColor DarkGray }
    }
    if ($result.Suppressed -gt 0) { Write-Host ("       info  {0} finding(s) suppressed by ReleaseCheck.allow.json" -f $result.Suppressed) -ForegroundColor DarkGray }
    Write-Host ''
}

# Summary.
$failed = @($results | Where-Object { $_.Status -eq 'FAIL' })
$warned = @($results | Where-Object { $_.Status -eq 'WARN' })

Write-Host 'Summary' -ForegroundColor Cyan
foreach ($result in $results) {
    Write-Host '  ' -NoNewline
    Write-Status $result.Status
    Write-Host (' {0,-13} {1}' -f $result.Id, $result.Title)
}
Write-Host ''

$blocked = ($failed.Count -gt 0) -or ($Strict -and $warned.Count -gt 0)
if ($blocked) {
    Write-Host ('NOT READY: {0} blocking check(s) failed, {1} with warnings.' -f $failed.Count, $warned.Count) -ForegroundColor Red
}
elseif ($warned.Count -gt 0) {
    Write-Host ('READY for the agent review: no blocking problems, {0} check(s) with warnings to read.' -f $warned.Count) -ForegroundColor Yellow
}
else {
    Write-Host 'READY for the agent review: all mechanical checks passed.' -ForegroundColor Green
}
Write-Host 'Mechanical checks do not cover changelog wording, translation quality or risk review; see .github/skills/release-check.' -ForegroundColor DarkGray

if ($JsonPath -ne '') {
    $report = [pscustomobject]@{
        generated        = (Get-Date).ToString('s')
        releaseCandidate = $topVersion
        lastTag          = $lastTag
        ready            = (-not $blocked)
        checks           = @($results | ForEach-Object {
                [pscustomobject]@{
                    id          = $_.Id
                    title       = $_.Title
                    status      = $_.Status
                    durationSec = $_.DurationSec
                    suppressed  = $_.Suppressed
                    findings    = @($_.Findings | ForEach-Object { [pscustomobject]@{ level = $_.Level; message = $_.Message; details = @($_.Details) } })
                }
            })
    }
    $json = $report | ConvertTo-Json -Depth 6
    $target = $(if ([System.IO.Path]::IsPathRooted($JsonPath)) { $JsonPath } else { Join-Path (Get-Location).Path $JsonPath })
    [System.IO.File]::WriteAllText($target, $json, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Report written to $target" -ForegroundColor DarkGray
}

if ($blocked) { exit 1 }
exit 0
