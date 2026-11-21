function Invoke-VersionCheck {
    $r = New-CheckResult 'version' 'Version and changelog'

    if (-not (Test-Path -LiteralPath (Join-Path $script:RepoRoot 'changelog.md'))) {
        Add-Finding $r Fail 'changelog.md not found.'
        return $r
    }

    $lines = @((Read-TextFile 'changelog.md').Text -split "`r?`n")
    $entries = @(ConvertFrom-Changelog $lines)

    if ($entries.Count -eq 0) {
        Add-Finding $r Fail 'changelog.md has no "## v{version}" entries.'
        return $r
    }

    $top = $entries[0]

    # Every heading must be machine-readable, otherwise release.yml cannot find its entry.
    foreach ($entry in $entries) {
        if (-not $entry.Parsed) {
            $level = $(if ($entry -eq $top) { 'Fail' } else { 'Warn' })
            Add-Finding $r $level "Changelog heading is not '## v{major}.{minor}.{patch} - {Title}' (line $($entry.Line))." @($entry.Heading) "heading:$($entry.Line)"
        }
        elseif (-not $entry.HasPrefix) {
            $level = $(if ($entry -eq $top) { 'Fail' } else { 'Warn' })
            Add-Finding $r $level "Changelog heading lacks the 'v' prefix that release.yml matches on (line $($entry.Line))." @($entry.Heading) "heading:$($entry.Line)"
        }
    }

    $parsed = @($entries | Where-Object { $_.Parsed })
    if (-not $top.Parsed) { return $r }

    $targetText = $top.Version
    $target = ConvertFrom-VersionText $targetText

    # Duplicate versions and ordering.
    $seen = @{}
    foreach ($entry in $parsed) {
        if ($seen.ContainsKey($entry.Version)) {
            Add-Finding $r Fail "Version $($entry.Version) appears more than once in the changelog (lines $($seen[$entry.Version]) and $($entry.Line))." @() "duplicate:$($entry.Version)"
        }
        else { $seen[$entry.Version] = $entry.Line }
    }
    for ($i = 1; $i -lt $parsed.Count; $i++) {
        $newer = ConvertFrom-VersionText $parsed[$i - 1].Version
        $older = ConvertFrom-VersionText $parsed[$i].Version
        if ((Compare-Version $newer $older) -le 0 -and $parsed[$i - 1].Version -ne $parsed[$i].Version) {
            Add-Finding $r Warn "Changelog entries are not in descending order: $($parsed[$i - 1].Version) (line $($parsed[$i - 1].Line)) is listed above $($parsed[$i].Version)." @() "order:$($parsed[$i].Version)"
        }
    }

    # WIP marker: the release entry must be finalized.
    if ($top.IsWip) {
        Add-Finding $r Fail "Top changelog entry v$targetText is still marked WIP; finalize it before tagging." @($top.Heading)
    }

    # One step ahead of the last tagged release.
    $tags = @(Get-ReleaseTag)
    if ($tags.Count -eq 0) {
        Add-Finding $r Warn 'No release tags found; cannot verify that the changelog is one step ahead of the last release.'
    }
    else {
        $last = $tags[$tags.Count - 1]
        $cmp = Compare-Version $target $last.Version

        if ($cmp -eq 0) {
            Add-Finding $r Fail "Top changelog entry v$targetText equals the last tag $($last.Tag); there is no entry for the next release."
        }
        elseif ($cmp -lt 0) {
            Add-Finding $r Fail "Top changelog entry v$targetText is older than the last tag $($last.Tag)."
        }
        else {
            $step = Get-VersionStep -Last $last.Version -Target $target
            if ($null -eq $step) {
                $c = Get-NextVersionCandidate -Last $last.Version
                Add-Finding $r Fail "Top changelog entry v$targetText is not exactly one step ahead of the last tag $($last.Tag)." @(
                    "Valid next versions: patch $($c.patch), minor $($c.minor), major $($c.major)")
            }
            else {
                Add-Finding $r Info "v$targetText is a $step step ahead of the last tag $($last.Tag). Confirm the bump size matches the changes (breaking = major, features = minor, fixes = patch)."
            }
        }

        $existing = @($tags | Where-Object { (Compare-Version $_.Version $target) -eq 0 })
        if ($existing.Count -gt 0) {
            Add-Finding $r Fail "A tag for v$targetText already exists ($($existing[0].Tag))."
        }
    }

    # What release.yml would publish for this version.
    $notes = Get-ReleaseNotesEntry -Lines $lines -Tag $targetText
    if (-not $notes.Found) {
        Add-Finding $r Fail "release.yml would not find a '## v$targetText' heading and would publish 'No changelog entry found.'"
    }
    else {
        if ([string]::IsNullOrWhiteSpace($notes.Title) -and -not $top.IsWip) {
            Add-Finding $r Warn "Entry v$targetText has no title ('## v$targetText - Title'); the GitHub release name loses its suffix."
        }
        if ($notes.Lines.Count -eq 0) {
            Add-Finding $r Fail "Entry v$targetText has no content; the release notes would be empty."
        }
        else {
            $preview = @($notes.Lines | Select-Object -First 3)
            Add-Finding $r Info "Release notes preview for v${targetText}: '$($notes.Title)'" $preview
        }
    }

    # House style of the bullet lines in the top entry: "- Area: text".
    $badBullets = @($top.Body | Where-Object { $_ -match '^\s*-\s' -and $_ -notmatch '^\s*-\s[^:\[\]]{2,40}:\s+\S' })
    if ($badBullets.Count -gt 0) {
        Add-Finding $r Warn "Changelog bullets in v$targetText that do not follow '- Area: text'." (Limit-List $badBullets) 'bullets'
    }

    # The csproj <Version> is informational (the tag overrides it at publish), but should not rot.
    $csproj = 'Starfield Load Order Keeper.csproj'
    if (Test-Path -LiteralPath (Join-Path $script:RepoRoot $csproj)) {
        $m = [regex]::Match((Read-TextFile $csproj).Text, '<Version>\s*([^<\s]+)\s*</Version>')
        if ($m.Success -and $m.Groups[1].Value -ne $targetText) {
            Add-Finding $r Warn "The csproj <Version> is $($m.Groups[1].Value) but the changelog says $targetText." @(
                'release.yml overrides the version from the tag at publish time, so this only affects local builds,',
                'but AGENTS.md states that the version is tracked in the csproj. Keep it in step or correct AGENTS.md.') 'csproj-version'
        }
    }

    return $r
}
