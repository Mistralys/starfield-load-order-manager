# Shared helpers for the release check.
# Keep this file (and every other .ps1 in this folder) ASCII-only: Windows PowerShell 5.1
# reads BOM-less scripts as ANSI, so non-ASCII literals would be mangled. Build such
# characters with [char]0x... instead.

$script:StrictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)
$script:LenientUtf8 = New-Object System.Text.UTF8Encoding($false, $false)
$script:TextCache = @{}
$script:FileListCache = $null
$script:Allow = @()

#region Results

function New-CheckResult {
    param([string]$Id, [string]$Title)
    [pscustomobject]@{
        Id          = $Id
        Title       = $Title
        Status      = 'PASS'
        Findings    = (New-Object System.Collections.ArrayList)
        Suppressed  = 0
        DurationSec = 0
    }
}

# An entry in ReleaseCheck.allow.json suppresses a finding when its "check" equals the
# check id and its "match" regex matches the finding key.
function Test-AllowedFinding {
    param([string]$CheckId, [string]$Key)
    if ([string]::IsNullOrEmpty($Key)) { return $false }
    foreach ($rule in $script:Allow) {
        if ($rule.check -eq $CheckId -and $Key -match $rule.match) { return $true }
    }
    return $false
}

function Add-Finding {
    param(
        $Result,
        [ValidateSet('Fail', 'Warn', 'Info')][string]$Level,
        [string]$Message,
        [string[]]$Details = @(),
        [string]$Key = ''
    )

    if ($Level -ne 'Info' -and (Test-AllowedFinding -CheckId $Result.Id -Key $Key)) {
        $Result.Suppressed++
        return
    }

    [void]$Result.Findings.Add([pscustomobject]@{ Level = $Level; Message = $Message; Details = @($Details) })

    if ($Level -eq 'Fail') { $Result.Status = 'FAIL' }
    elseif ($Level -eq 'Warn' -and $Result.Status -ne 'FAIL') { $Result.Status = 'WARN' }
}

function Set-CheckSkipped {
    param($Result, [string]$Reason)
    $Result.Status = 'SKIP'
    [void]$Result.Findings.Add([pscustomobject]@{ Level = 'Info'; Message = $Reason; Details = @() })
}

# Limits a long list for display while keeping the true total visible.
function Limit-List {
    param([string[]]$Items, [int]$Max = 15)
    $list = @($Items)
    if ($list.Count -le $Max) { return $list }
    return @($list[0..($Max - 1)]) + @("... and $($list.Count - $Max) more")
}

# Reports a list of offending items as one finding. Each item can be suppressed on its own through
# ReleaseCheck.allow.json: the key it is matched against is "<KeyPrefix>:<item text>".
function Add-ListFinding {
    param($Result, [ValidateSet('Fail', 'Warn', 'Info')][string]$Level, [string]$Message, [string[]]$Items, [string]$KeyPrefix, [int]$Max = 15)

    $remaining = @()
    foreach ($item in @($Items)) {
        if (Test-AllowedFinding -CheckId $Result.Id -Key "${KeyPrefix}:$item") { $Result.Suppressed++ }
        else { $remaining += $item }
    }
    if ($remaining.Count -gt 0) { Add-Finding $Result $Level $Message (Limit-List $remaining $Max) }
}

#endregion

#region Native processes and git

function ConvertTo-ProcessArgument {
    param([string]$Value)
    if ($Value -eq '') { return '""' }
    if ($Value -notmatch '[\s"]') { return $Value }
    $escaped = $Value -replace '(\\*)"', '$1$1\"'
    $escaped = $escaped -replace '(\\+)$', '$1$1'
    return '"' + $escaped + '"'
}

function Invoke-Native {
    param(
        [string]$File,
        [string[]]$Arguments = @(),
        [string]$WorkingDirectory = $script:RepoRoot
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $File
    $psi.Arguments = (($Arguments | ForEach-Object { ConvertTo-ProcessArgument $_ }) -join ' ')
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.StandardOutputEncoding = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding = [System.Text.Encoding]::UTF8
    # Force English tool output so that summary lines can be parsed reliably.
    $psi.EnvironmentVariables['DOTNET_CLI_UI_LANGUAGE'] = 'en'
    $psi.EnvironmentVariables['VSLANG'] = '1033'
    $psi.EnvironmentVariables['DOTNET_NOLOGO'] = '1'
    $psi.EnvironmentVariables['DOTNET_SKIP_FIRST_TIME_EXPERIENCE'] = '1'
    $psi.EnvironmentVariables['GIT_TERMINAL_PROMPT'] = '0'

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi

    try {
        [void]$process.Start()
    }
    catch {
        return [pscustomobject]@{ ExitCode = -1; Text = ''; ErrorText = $_.Exception.Message; Output = @(); Started = $false }
    }

    $errorTask = $process.StandardError.ReadToEndAsync()
    $text = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    $errorText = $errorTask.Result

    [pscustomobject]@{
        ExitCode  = $process.ExitCode
        Text      = $text
        ErrorText = $errorText
        Output    = @($text -split "`r?`n" | Where-Object { $_ -ne '' })
        Started   = $true
    }
}

function Invoke-Git {
    param([string[]]$GitArgs)
    Invoke-Native -File 'git' -Arguments (@('-C', $script:RepoRoot, '-c', 'core.quotepath=false') + $GitArgs)
}

function Test-GitRepository {
    $r = Invoke-Git @('rev-parse', '--is-inside-work-tree')
    return ($r.Started -and $r.ExitCode -eq 0 -and ($r.Text.Trim() -eq 'true'))
}

#endregion

#region Files

$script:TextExtensions = @(
    '.cs', '.xaml', '.json', '.jsonl', '.md', '.yml', '.yaml', '.csproj', '.wxs', '.wixproj', '.props',
    '.targets', '.ps1', '.psm1', '.cmd', '.bat', '.txt', '.vdf', '.sln', '.xml', '.config', '.resx',
    '.editorconfig', '.gitignore', '.gitattributes'
)

function Get-RepoFile {
    # Tracked plus untracked-but-not-ignored files, as repo-relative paths with forward slashes.
    if ($null -ne $script:FileListCache) { return $script:FileListCache }

    $r = Invoke-Git @('ls-files', '-z', '--cached', '--others', '--exclude-standard')
    $paths = @($r.Text.Split([char]0) | Where-Object { $_ -ne '' } | Sort-Object -Unique)
    $script:FileListCache = @($paths | Where-Object { Test-Path -LiteralPath (Join-Path $script:RepoRoot $_) -PathType Leaf })
    return $script:FileListCache
}

function Test-TextPath {
    param([string]$RelPath)
    $name = [System.IO.Path]::GetFileName($RelPath).ToLowerInvariant()
    $ext = [System.IO.Path]::GetExtension($name)
    if ($ext -eq '' -and $name.StartsWith('.')) { $ext = $name }
    return ($script:TextExtensions -contains $ext)
}

function Read-TextFile {
    param([string]$RelPath)

    if ($script:TextCache.ContainsKey($RelPath)) { return $script:TextCache[$RelPath] }

    $full = Join-Path $script:RepoRoot $RelPath
    $bytes = [System.IO.File]::ReadAllBytes($full)
    $hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    $valid = $true

    try { $text = $script:StrictUtf8.GetString($bytes) }
    catch { $valid = $false; $text = $script:LenientUtf8.GetString($bytes) }

    if ($hasBom -and $text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }

    $file = [pscustomobject]@{ Path = $RelPath; HasBom = $hasBom; ValidUtf8 = $valid; Text = $text }
    $script:TextCache[$RelPath] = $file
    return $file
}

function Get-LineNumber {
    param([string]$Text, [int]$Index)
    if ($Index -le 0) { return 1 }
    $count = 1
    $pos = $Text.IndexOf("`n")
    while ($pos -ge 0 -and $pos -lt $Index) {
        $count++
        $pos = $Text.IndexOf("`n", $pos + 1)
    }
    return $count
}

# Files of the shipped application: excludes tests, build output, docs and tooling.
function Get-AppSourceFile {
    param([string[]]$Extension)
    Get-RepoFile | Where-Object {
        $p = $_
        ($Extension -contains [System.IO.Path]::GetExtension($p).ToLowerInvariant()) -and
        ($p -notmatch '^(Tests|Docs|Tools|installer|\.github|\.vs)/') -and
        ($p -notmatch '(^|/)(bin|obj)/')
    }
}

#endregion

#region Versions

function ConvertFrom-VersionText {
    param([string]$Text)
    $m = [regex]::Match($Text.Trim(), '^v?(?<maj>\d+)\.(?<min>\d+)\.(?<pat>\d+)(?:-(?<pre>[0-9A-Za-z.-]+))?$')
    if (-not $m.Success) { return $null }
    $pre = $null
    if ($m.Groups['pre'].Success) { $pre = $m.Groups['pre'].Value }
    [pscustomobject]@{
        Major = [int]$m.Groups['maj'].Value
        Minor = [int]$m.Groups['min'].Value
        Patch = [int]$m.Groups['pat'].Value
        Pre   = $pre
        Base  = ('{0}.{1}.{2}' -f $m.Groups['maj'].Value, $m.Groups['min'].Value, $m.Groups['pat'].Value)
        Text  = $Text.Trim().TrimStart('v')
    }
}

# Returns -1, 0 or 1. A pre-release sorts below its release.
function Compare-Version {
    param($A, $B)
    foreach ($part in 'Major', 'Minor', 'Patch') {
        if ($A.$part -lt $B.$part) { return -1 }
        if ($A.$part -gt $B.$part) { return 1 }
    }
    if ($null -eq $A.Pre -and $null -eq $B.Pre) { return 0 }
    if ($null -eq $A.Pre) { return 1 }
    if ($null -eq $B.Pre) { return -1 }

    $ai = $A.Pre.Split('.')
    $bi = $B.Pre.Split('.')
    $len = [Math]::Max($ai.Count, $bi.Count)
    for ($i = 0; $i -lt $len; $i++) {
        if ($i -ge $ai.Count) { return -1 }
        if ($i -ge $bi.Count) { return 1 }
        $x = $ai[$i]; $y = $bi[$i]
        $xn = $x -match '^\d+$'; $yn = $y -match '^\d+$'
        if ($xn -and $yn) { $c = ([int64]$x).CompareTo([int64]$y) }
        elseif ($xn) { $c = -1 }
        elseif ($yn) { $c = 1 }
        else { $c = [string]::CompareOrdinal($x, $y) }
        if ($c -ne 0) { return [Math]::Sign($c) }
    }
    return 0
}

# The three stable versions that are exactly one step ahead of the given version.
function Get-NextVersionCandidate {
    param($Last)
    [ordered]@{
        patch = ('{0}.{1}.{2}' -f $Last.Major, $Last.Minor, ($Last.Patch + 1))
        minor = ('{0}.{1}.0' -f $Last.Major, ($Last.Minor + 1))
        major = ('{0}.0.0' -f ($Last.Major + 1))
    }
}

# Classifies the step from $Last to $Target: 'patch', 'minor', 'major' (each with a
# "-prerelease" suffix for pre-release targets), 'release' (a pre-release is finalized),
# 'prerelease' (a later pre-release of the same base), or $null when it is not one step.
function Get-VersionStep {
    param($Last, $Target)

    $candidates = Get-NextVersionCandidate -Last $Last
    foreach ($kind in $candidates.Keys) {
        if ($Target.Base -eq $candidates[$kind]) {
            if ($null -ne $Target.Pre) { return "$kind-prerelease" }
            return $kind
        }
    }

    if ($null -ne $Last.Pre -and $Target.Base -eq $Last.Base) {
        if ($null -eq $Target.Pre) { return 'release' }
        if ((Compare-Version $Target $Last) -gt 0) { return 'prerelease' }
    }
    return $null
}

# Release tags are bare versions (1.2.3 or 1.2.3-beta.1, no "v" prefix), sorted ascending.
function Get-ReleaseTag {
    $r = Invoke-Git @('tag', '--list')
    $tags = New-Object System.Collections.ArrayList

    foreach ($t in $r.Output) {
        $tag = $t.Trim()
        if ($tag -match '^v') { continue }
        $v = ConvertFrom-VersionText $tag
        if ($null -eq $v) { continue }

        $entry = [pscustomobject]@{ Tag = $tag; Version = $v }
        $index = $tags.Count
        while ($index -gt 0 -and (Compare-Version $tags[$index - 1].Version $v) -gt 0) { $index-- }
        $tags.Insert($index, $entry)
    }
    return $tags.ToArray()
}

#endregion

#region Changelog

# Splits the changelog into entries. An entry starts at a "## " heading.
function ConvertFrom-Changelog {
    param([string[]]$Lines)

    $entries = New-Object System.Collections.ArrayList
    $current = $null

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]

        if ($line -match '^##\s') {
            $m = [regex]::Match($line, '^##\s+(?<v>v?)(?<ver>\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?)\b\s*(?:-\s*(?<title>.*?))?\s*$')
            $isWip = ($line -match '(?i)\bWIP\b')
            $title = ''
            if ($m.Success -and $m.Groups['title'].Success -and -not $isWip) { $title = $m.Groups['title'].Value }

            $current = [pscustomobject]@{
                Line      = $i + 1
                Heading   = $line
                Parsed    = $m.Success
                HasPrefix = ($m.Success -and $m.Groups['v'].Value -eq 'v')
                Version   = $(if ($m.Success) { $m.Groups['ver'].Value } else { '' })
                Title     = $title
                IsWip     = $isWip
                Body      = (New-Object System.Collections.ArrayList)
            }
            [void]$entries.Add($current)
        }
        elseif ($null -ne $current) {
            [void]$current.Body.Add($line)
        }
    }

    return $entries.ToArray()
}

# Version of the newest changelog entry, or $null when the changelog is missing or unreadable.
function Get-ChangelogTopVersion {
    if (-not (Test-Path -LiteralPath (Join-Path $script:RepoRoot 'changelog.md'))) { return $null }
    $entries = @(ConvertFrom-Changelog @((Read-TextFile 'changelog.md').Text -split "`r?`n"))
    if ($entries.Count -eq 0 -or -not $entries[0].Parsed) { return $null }
    return $entries[0].Version
}

# Mirrors the "Extract changelog entry" step of .github/workflows/release.yml so that the
# check sees what the release will publish.
function Get-ReleaseNotesEntry {
    param([string[]]$Lines, [string]$Tag)

    $pattern = '^## v' + [regex]::Escape($Tag) + '\b'
    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match $pattern) { $start = $i; break }
    }
    if ($start -lt 0) { return [pscustomobject]@{ Found = $false; Title = ''; Lines = @() } }

    $title = ''
    $m = [regex]::Match($Lines[$start], '^## v' + [regex]::Escape($Tag) + '\s*-\s*(.+)$')
    if ($m.Success) { $title = $m.Groups[1].Value.Trim() }

    $end = $Lines.Count
    for ($j = $start + 1; $j -lt $Lines.Count; $j++) {
        if ($Lines[$j] -match '^## ') { $end = $j; break }
    }

    $body = @()
    if ($end - 1 -ge $start + 1) {
        $body = @($Lines[($start + 1)..($end - 1)] | Where-Object { $_.Trim() -ne '' })
    }
    [pscustomobject]@{ Found = $true; Title = $title; Lines = $body }
}

#endregion

#region Menu and locale helpers

# Returns the access key (mnemonic) of a WPF header, e.g. "_File" -> "f", "File (_F)" -> "f".
function Get-AccessKey {
    param([string]$Header)
    if ([string]::IsNullOrEmpty($Header)) { return $null }
    $clean = $Header.Replace('__', '')
    $m = [regex]::Match($clean, '_(?<k>\S)')
    if (-not $m.Success) { return $null }
    return $m.Groups['k'].Value.ToLowerInvariant()
}

# Flattens a locale file into "Section.Key" -> value. Root-level scalars (LocaleName,
# ParentCulture) are returned under "$meta.Name".
function ConvertFrom-LocaleJson {
    param([string]$Json)

    $root = $Json | ConvertFrom-Json
    $flat = [ordered]@{}
    foreach ($section in $root.PSObject.Properties) {
        if ($section.Value -is [System.Management.Automation.PSCustomObject]) {
            foreach ($entry in $section.Value.PSObject.Properties) {
                $flat["$($section.Name).$($entry.Name)"] = [string]$entry.Value
            }
        }
        else {
            $flat["`$meta.$($section.Name)"] = [string]$section.Value
        }
    }
    return $flat
}

#endregion
