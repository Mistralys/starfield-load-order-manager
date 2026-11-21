# Complements Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationCompletenessTests.cs, which already
# proves key parity, non-empty values and placeholder parity between locales. This check covers
# what those tests cannot see: usage in code, build registration, plural contracts and metadata.

function Invoke-LocalizationCheck {
    $r = New-CheckResult 'localization' 'Localization'

    $baseName = 'en-US'
    $localeFiles = @(Get-RepoFile | Where-Object { $_ -match '^ViewTexts/Locales/[^/]+\.json$' })
    if ($localeFiles.Count -eq 0) {
        Add-Finding $r Fail 'No locale files found in ViewTexts/Locales.'
        return $r
    }

    $locales = [ordered]@{}
    foreach ($path in $localeFiles) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($path)
        $file = Read-TextFile $path
        try { $locales[$name] = ConvertFrom-LocaleJson -Json $file.Text }
        catch { Add-Finding $r Fail "Locale $name is not valid JSON." @($_.Exception.Message) "json:$name" }
    }

    if (-not $locales.Contains($baseName)) {
        Add-Finding $r Fail "Base locale $baseName.json is missing or unreadable."
        return $r
    }
    $base = $locales[$baseName]

    # Duplicate keys: JSON parsers silently keep one of them.
    foreach ($path in $localeFiles) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($path)
        $section = ''
        $seenSections = @{}
        $seenKeys = @{}
        $dupes = @()
        $lineNo = 0
        foreach ($line in ((Read-TextFile $path).Text -split "`r?`n")) {
            $lineNo++
            $m = [regex]::Match($line, '^\s{2}"([^"]+)"\s*:\s*\{')
            if ($m.Success) {
                $section = $m.Groups[1].Value
                if ($seenSections.ContainsKey($section)) { $dupes += "$name.json:$lineNo  section '$section'" }
                $seenSections[$section] = $true
                continue
            }
            $m = [regex]::Match($line, '^\s{4}"([^"]+)"\s*:')
            if ($m.Success -and $section -ne '') {
                $id = "$section.$($m.Groups[1].Value)"
                if ($seenKeys.ContainsKey($id)) { $dupes += "$name.json:$lineNo  key '$id'" }
                $seenKeys[$id] = $true
            }
        }
        if ($dupes.Count -gt 0) { Add-Finding $r Fail 'Duplicate keys in a locale file.' (Limit-List $dupes) "dupe:$name" }
    }

    # Metadata sanity.
    $localeNames = @{}
    foreach ($name in $locales.Keys) {
        $culture = $null
        try { $culture = [System.Globalization.CultureInfo]::GetCultureInfo($name) } catch { }
        if ($null -eq $culture -or $culture.Name -ne $name) {
            Add-Finding $r Warn "File name '$name.json' is not a valid culture name." @() "culture:$name"
        }
        elseif ($locales[$name].Contains('$meta.ParentCulture') -and $locales[$name]['$meta.ParentCulture'] -ne $culture.TwoLetterISOLanguageName) {
            Add-Finding $r Warn "ParentCulture of $name is '$($locales[$name]['$meta.ParentCulture'])', expected '$($culture.TwoLetterISOLanguageName)'." @() "parent:$name"
        }

        $display = $locales[$name]['$meta.LocaleName']
        if ($display) {
            if ($localeNames.ContainsKey($display)) { Add-Finding $r Warn "LocaleName '$display' is used by both $($localeNames[$display]) and $name." @() "localename:$name" }
            $localeNames[$display] = $name
        }
    }

    # Every locale must be copied to the output directory, or it silently disappears from the app.
    $csproj = 'Starfield Load Order Keeper.csproj'
    if (Test-Path -LiteralPath (Join-Path $script:RepoRoot $csproj)) {
        $registered = @{}
        $wildcard = $false
        try {
            $xml = New-Object System.Xml.XmlDocument
            $xml.LoadXml((Read-TextFile $csproj).Text)
            foreach ($item in $xml.SelectNodes('//*[local-name()="None" or local-name()="Content"]')) {
                $spec = $item.GetAttribute('Update')
                if (-not $spec) { $spec = $item.GetAttribute('Include') }
                if ($spec -notmatch '^ViewTexts[\\/]Locales[\\/]') { continue }
                $copy = $item.SelectSingleNode('*[local-name()="CopyToOutputDirectory"]')
                if ($null -eq $copy -or $copy.InnerText -eq 'Never') { continue }
                if ($spec -match '\*') { $wildcard = $true }
                else { $registered[[System.IO.Path]::GetFileNameWithoutExtension($spec)] = $true }
            }
        }
        catch { Add-Finding $r Fail "Could not parse $csproj." @($_.Exception.Message) }

        if (-not $wildcard) {
            foreach ($name in $locales.Keys) {
                if (-not $registered.ContainsKey($name)) {
                    Add-Finding $r Fail "Locale $name.json is not copied to the output directory by the csproj." @('Add <None Update="ViewTexts\Locales\' + $name + '.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>.') "csproj:$name"
                }
            }
            foreach ($name in $registered.Keys) {
                if (-not $locales.Contains($name)) { Add-Finding $r Warn "The csproj registers $name.json, which does not exist." @() "csproj-stale:$name" }
            }
        }
    }

    # Code versus en-US: keys used in code but missing from the base locale, and the reverse.
    $used = @{}
    $computed = @{}
    $dynamic = @()
    $call = [regex]'[Ll]ocali[sz]ation\w*(?:\.Instance)?\.GetString\(\s*(?<a>"[^"]*"|[^,)]+?)\s*,\s*(?<b>"[^"]*"|[^,)]+?)\s*[,)]'
    foreach ($path in (Get-AppSourceFile -Extension '.cs')) {
        if ($path -eq 'ViewTexts/LocalizationService.cs') { continue }
        $text = (Read-TextFile $path).Text
        foreach ($m in $call.Matches($text)) {
            $a = $m.Groups['a'].Value; $b = $m.Groups['b'].Value
            $loc = "${path}:$(Get-LineNumber $text $m.Index)"
            if ($a.StartsWith('"') -and $b.StartsWith('"')) {
                $id = $a.Trim('"') + '.' + $b.Trim('"')
                if (-not $used.ContainsKey($id)) { $used[$id] = $loc }
            }
            else {
                $dynamic += "$loc  GetString($a, $b)"
                # A computed key usually picks between string literals in the same file.
                if ($a.StartsWith('"')) {
                    if (-not $computed.ContainsKey($path)) { $computed[$path] = @{} }
                    $computed[$path][$a.Trim('"')] = $true
                }
            }
        }
    }

    foreach ($path in $computed.Keys) {
        $text = (Read-TextFile $path).Text
        foreach ($m in [regex]::Matches($text, '"([A-Za-z_][A-Za-z0-9_]*)"')) {
            foreach ($section in $computed[$path].Keys) {
                $id = "$section.$($m.Groups[1].Value)"
                if ($base.Contains($id) -and -not $used.ContainsKey($id)) { $used[$id] = "$path (computed key)" }
            }
        }
    }

    $missing = @($used.Keys | Where-Object { -not $base.Contains($_) } | Sort-Object | ForEach-Object { "$_  (used at $($used[$_]))" })
    Add-ListFinding $r Fail "Keys used in code but missing from $baseName.json (the UI would show a placeholder)." $missing 'missing-key'

    $unused = @($base.Keys | Where-Object { -not $_.StartsWith('$meta.') -and -not $used.ContainsKey($_) } | Sort-Object)
    $note = $(if ($dynamic.Count -gt 0) { " Some may be reached through the $($dynamic.Count) lookup(s) with computed keys." } else { '' })
    Add-ListFinding $r Warn "Keys in $baseName.json that no code references.$note" $unused 'unused-key' 20
    if ($dynamic.Count -gt 0) { Add-Finding $r Info 'Lookups with computed keys (not verifiable statically).' (Limit-List $dynamic 8) }

    # Plural contract: "{0}" in the plural form, never in the singular form.
    $pairs = @()
    foreach ($key in $base.Keys) {
        if ($key -match '^(.+)CountFormat$') { $pairs += , @($key, "$($Matches[1])CountSingular") }
    }
    $pairs += , @('DiffDialog.ConfirmRiskyDetail_Removals', 'DiffDialog.ConfirmRiskyDetail_RemovedSingular')
    $pairs += , @('DiffDialog.ConfirmRiskyDetail_Insertions', 'DiffDialog.ConfirmRiskyDetail_InsertedSingular')

    foreach ($pair in $pairs) {
        $plural = $pair[0]; $singular = $pair[1]
        if (-not $base.Contains($plural)) { continue }
        foreach ($name in $locales.Keys) {
            $values = $locales[$name]
            if ($values.Contains($plural) -and $values[$plural] -notmatch '\{0') {
                Add-Finding $r Fail "Plural form lacks the {0} placeholder: $name $plural." @() "plural:${name}:$plural"
            }
            if (-not $values.Contains($singular)) {
                Add-Finding $r Fail "Singular partner key is missing: $name $singular." @() "singular:${name}:$singular"
            }
            elseif ($values.Contains($singular) -and $values[$singular] -match '\{0') {
                Add-Finding $r Fail "Singular form must not contain {0}: $name $singular." @() "singular:${name}:$singular"
            }
        }
    }

    # Mnemonics: a menu entry with an access key in English must keep one in every language.
    foreach ($name in $locales.Keys) {
        if ($name -eq $baseName) { continue }
        $lost = @()
        foreach ($key in $base.Keys) {
            if ($key.StartsWith('$meta.')) { continue }
            if ((Get-AccessKey $base[$key]) -and $locales[$name].Contains($key) -and -not (Get-AccessKey $locales[$name][$key])) { $lost += $key }
        }
        Add-ListFinding $r Warn "$name lost the access key (_X) of entries that have one in English." $lost "accesskey:$name" 15
    }

    # Left-over English: multi-word values identical to the base locale.
    foreach ($name in $locales.Keys) {
        if ($name -eq $baseName -or $name -like 'en-*') { continue }
        $same = @()
        foreach ($key in $base.Keys) {
            if ($key.StartsWith('$meta.') -or -not $locales[$name].Contains($key)) { continue }
            $value = $base[$key]
            $probe = $value.Replace('Starfield Load Order Keeper', '')
            if ($value -and $value -ceq $locales[$name][$key] -and $probe -match '[A-Za-z]{3,}\s+[A-Za-z]{2,}' -and $probe.Length -ge 8) { $same += "$key = $value" }
        }
        Add-ListFinding $r Warn "$name has values identical to English (untranslated?)." $same "english:$name" 8
    }

    return $r
}
