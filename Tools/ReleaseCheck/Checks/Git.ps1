function Invoke-GitCheck {
    $r = New-CheckResult 'git' 'Git state'

    if (-not (Test-GitRepository)) {
        Add-Finding $r Fail 'Not inside a git work tree.'
        return $r
    }

    # Working tree.
    $status = Invoke-Git @('status', '--porcelain')
    $dirty = @($status.Output)
    if ($dirty.Count -gt 0) {
        $level = $(if ($script:Options.AllowDirty) { 'Warn' } else { 'Fail' })
        Add-Finding $r $level 'The working tree has uncommitted or untracked changes.' (Limit-List $dirty 12)
    }

    # Branch.
    $branch = (Invoke-Git @('branch', '--show-current')).Text.Trim()
    if ($branch -eq '') {
        Add-Finding $r Warn 'HEAD is detached; tag releases from a branch.'
    }
    else {
        $default = ''
        $ref = Invoke-Git @('symbolic-ref', '--short', 'refs/remotes/origin/HEAD')
        if ($ref.ExitCode -eq 0 -and $ref.Text.Trim() -ne '') { $default = $ref.Text.Trim() -replace '^origin/', '' }
        $expected = @($(if ($default) { $default } else { 'main'; 'master' }))

        if ($expected -notcontains $branch) {
            Add-Finding $r Warn "Releasing from branch '$branch', not from $($expected -join '/')." @('Merge the work first, unless this is a deliberate release from a branch.')
        }
    }

    # Upstream sync. Fetching updates local refs, so it only happens on request; without
    # -Fetch the comparison uses the last known state of the remote-tracking branch.
    if ($script:Options.Fetch -and -not $script:Options.Offline) {
        [void](Invoke-Git @('fetch', '--quiet', '--tags', 'origin'))
    }

    $upstream = Invoke-Git @('rev-parse', '--abbrev-ref', '--symbolic-full-name', '@{u}')
    if ($upstream.ExitCode -ne 0) {
        Add-Finding $r Warn "Branch '$branch' has no upstream; the commits may not be pushed."
    }
    else {
        $counts = Invoke-Git @('rev-list', '--left-right', '--count', '@{u}...HEAD')
        $parts = $counts.Text.Trim() -split '\s+'
        if ($counts.ExitCode -eq 0 -and $parts.Count -eq 2) {
            $behind = [int]$parts[0]
            $ahead = [int]$parts[1]
            if ($behind -gt 0) { Add-Finding $r Fail "Branch is $behind commit(s) behind $($upstream.Text.Trim()); pull before releasing." }
            if ($ahead -gt 0) { Add-Finding $r Warn "Branch is $ahead commit(s) ahead of $($upstream.Text.Trim()); push so that CI builds what you tagged." }
        }
    }

    # Something to release.
    $tags = @(Get-ReleaseTag)
    if ($tags.Count -gt 0) {
        $last = $tags[$tags.Count - 1]
        $count = (Invoke-Git @('rev-list', '--count', "$($last.Tag)..HEAD")).Text.Trim()
        if ($count -eq '0') {
            Add-Finding $r Fail "HEAD has no commits since the last tag $($last.Tag); there is nothing to release."
        }
        else {
            Add-Finding $r Info "$count commit(s) since the last tag $($last.Tag)."
        }

        $atHead = @((Invoke-Git @('tag', '--points-at', 'HEAD')).Output)
        if ($atHead.Count -gt 0) {
            Add-Finding $r Warn "HEAD already carries the tag(s): $($atHead -join ', ')."
        }
    }

    # The remote must not know the release tag yet.
    $topVersion = Get-ChangelogTopVersion
    if (-not $script:Options.Offline -and $topVersion) {
        $remote = Invoke-Git @('ls-remote', '--tags', 'origin', "refs/tags/$topVersion")
        if ($remote.ExitCode -eq 0 -and $remote.Text.Trim() -ne '') {
            Add-Finding $r Fail "Tag $topVersion already exists on origin."
        }
        elseif ($remote.ExitCode -ne 0) {
            Add-Finding $r Warn 'Could not query origin for existing tags.' @($remote.ErrorText.Trim())
        }
    }

    return $r
}
