function Invoke-PackagingCheck {
    $r = New-CheckResult 'packaging' 'Packaging and release pipeline'

    $csprojPath = 'Starfield Load Order Keeper.csproj'
    $wxsPath = 'installer/Product.wxs'
    $workflowPath = '.github/workflows/release.yml'

    foreach ($required in $csprojPath, $wxsPath, $workflowPath, 'installer/StarfieldLoadOrderKeeper.wixproj', 'LICENSE', 'README.md') {
        if (-not (Test-Path -LiteralPath (Join-Path $script:RepoRoot $required))) {
            Add-Finding $r Fail "Required release file is missing: $required."
        }
    }
    if ($r.Status -eq 'FAIL') { return $r }

    $csproj = (Read-TextFile $csprojPath).Text
    $wxs = (Read-TextFile $wxsPath).Text
    $workflow = (Read-TextFile $workflowPath).Text

    # Files the csproj points to.
    foreach ($m in [regex]::Matches($csproj, '<(?:ApplicationIcon|Content\s+Include|None\s+Include)(?:>|=")(?<p>[^<"]+)')) {
        $p = $m.Groups['p'].Value
        if ($p -match '[\*\$]') { continue }
        if (-not (Test-Path -LiteralPath (Join-Path $script:RepoRoot ($p -replace '\\', '/')))) {
            Add-Finding $r Fail "The csproj references '$p', which does not exist." @() "csproj-file:$p"
        }
    }

    # The CI runner must provide the SDK the project targets.
    $tfm = [regex]::Match($csproj, '<TargetFramework>net(?<v>\d+)\.')
    $sdk = [regex]::Match($workflow, 'dotnet-version:\s*''?(?<v>\d+)')
    if ($tfm.Success -and $sdk.Success -and $tfm.Groups['v'].Value -ne $sdk.Groups['v'].Value) {
        Add-Finding $r Fail "The project targets net$($tfm.Groups['v'].Value) but release.yml installs .NET $($sdk.Groups['v'].Value)."
    }

    # The version that release.yml would derive from the tag must be accepted by its own pattern.
    $top = Get-ChangelogTopVersion
    $pattern = [regex]::Match($workflow, '\$pattern\s*=\s*''(?<p>[^'']+)''')
    if ($top -and $pattern.Success) {
        if ($top -notmatch $pattern.Groups['p'].Value) {
            Add-Finding $r Fail "Tag $top would be rejected by the semantic version pattern in release.yml."
        }
    }
    if ($top -and $top -notmatch '^[0-9]') { Add-Finding $r Fail "Tag $top does not match the trigger glob of release.yml ('[0-9]*')." }

    # The installer must package the executable the project produces.
    $assembly = [regex]::Match($csproj, '<AssemblyName>(?<n>[^<]+)</AssemblyName>')
    $exe = [regex]::Match($wxs, 'Source="[^"]*[\\/](?<f>[^"\\/]+\.exe)"')
    if ($assembly.Success -and $exe.Success -and $exe.Groups['f'].Value -ne "$($assembly.Groups['n'].Value).exe") {
        Add-Finding $r Fail "The installer packages '$($exe.Groups['f'].Value)' but the project builds '$($assembly.Groups['n'].Value).exe'."
    }

    # The app reads its translations from ViewTexts/Locales next to the executable.
    $localeFiles = @(Get-RepoFile | Where-Object { $_ -match '^ViewTexts/Locales/[^/]+\.json$' })
    $shipped = @($localeFiles | Where-Object { $wxs -match [regex]::Escape([System.IO.Path]::GetFileName($_)) })
    if ($localeFiles.Count -gt 0 -and $shipped.Count -lt $localeFiles.Count -and $wxs -notmatch 'Locales') {
        Add-Finding $r Fail 'The MSI does not install the locale files.' @(
            'LocalizationService loads ViewTexts/Locales/*.json from the application directory, but Product.wxs only',
            'packages the executable. An MSI install would start without translations unless they are embedded.') 'msi-locales'
    }

    # Identifiers that must never change between releases, or upgrades break.
    $tags = @(Get-ReleaseTag)
    if ($tags.Count -gt 0) {
        $last = $tags[$tags.Count - 1]
        $old = Invoke-Git @('show', "$($last.Tag):$wxsPath")
        if ($old.ExitCode -eq 0) {
            $oldCode = [regex]::Match($old.Text, 'UpgradeCode="(?<g>[^"]+)"').Groups['g'].Value
            $newCode = [regex]::Match($wxs, 'UpgradeCode="(?<g>[^"]+)"').Groups['g'].Value
            if ($oldCode -and $newCode -and $oldCode -ne $newCode) {
                Add-Finding $r Fail "The installer UpgradeCode changed since $($last.Tag); existing installations would not be upgraded." @("$oldCode -> $newCode")
            }
            $oldComp = @([regex]::Matches($old.Text, '<Component\s+Id="(?<i>[^"]+)"\s+Guid="(?<g>[^"]+)"') | ForEach-Object { "$($_.Groups['i'].Value)=$($_.Groups['g'].Value)" })
            $newComp = @([regex]::Matches($wxs, '<Component\s+Id="(?<i>[^"]+)"\s+Guid="(?<g>[^"]+)"') | ForEach-Object { "$($_.Groups['i'].Value)=$($_.Groups['g'].Value)" })
            $changed = @($oldComp | Where-Object { $newComp -notcontains $_ })
            if ($changed.Count -gt 0) { Add-Finding $r Warn "Installer component GUIDs changed or were removed since $($last.Tag)." $changed 'component-guid' }
        }
    }

    return $r
}
