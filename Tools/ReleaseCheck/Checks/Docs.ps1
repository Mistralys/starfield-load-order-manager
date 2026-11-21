function Get-FileTreeEntry {
    # Parses the code block of file-tree.md into repo-relative file paths.
    param([string]$Text)

    $box = '[\s' + [char]0x2502 + [char]0x251C + [char]0x2514 + [char]0x2500 + [char]0x00A0 + ']*'
    $paths = New-Object System.Collections.ArrayList
    $stack = @{}
    $inBlock = $false

    foreach ($line in ($Text -split "`r?`n")) {
        if ($line -match '^```') { $inBlock = -not $inBlock; continue }
        if (-not $inBlock) { continue }

        $m = [regex]::Match($line, "^(?<prefix>$box)(?<rest>.*)$")
        $prefix = $m.Groups['prefix'].Value
        $rest = $m.Groups['rest'].Value
        if ($rest.Trim() -eq '' -or $rest.Trim() -eq '.') { continue }

        $depth = [int][Math]::Floor($prefix.Length / 3)
        $nm = [regex]::Match($rest, '^(?<n>.+?)(?:\s{2,}|\s+\(|$)')
        $name = $nm.Groups['n'].Value.Trim()
        if ($name -eq '' -or $name.Contains('*')) { continue }

        $isDir = $name.EndsWith('/')
        $name = $name.TrimEnd('/')
        $stack[$depth] = $name
        foreach ($k in @($stack.Keys)) { if ($k -gt $depth) { $stack.Remove($k) } }

        if (-not $isDir) {
            $parts = @()
            for ($d = 0; $d -le $depth; $d++) { if ($stack.ContainsKey($d)) { $parts += $stack[$d] } }
            [void]$paths.Add(($parts -join '/'))
        }
    }
    return $paths.ToArray()
}

function Invoke-DocsCheck {
    $r = New-CheckResult 'docs' 'Documentation and manifest'
    $manifest = 'Docs/Agents/project-manifest'

    $required = 'README.md', 'tech-stack.md', 'file-tree.md', 'data-flows.md', 'constraints.md', 'localization.md', 'file-formats.md', 'api-surface.md', 'ui-design.md'
    $missing = @($required | Where-Object { -not (Test-Path -LiteralPath (Join-Path $script:RepoRoot "$manifest/$_")) })
    if ($missing.Count -gt 0) {
        Add-Finding $r Fail 'Project manifest documents are missing.' $missing
        return $r
    }

    $trackedAll = @(Get-RepoFile)

    # file-tree.md versus the files on disk.
    $treeText = (Read-TextFile "$manifest/file-tree.md").Text
    $documented = @(Get-FileTreeEntry $treeText | Where-Object { $_ -match '\.(cs|xaml|json|csproj)$' })
    $scopeDirs = 'Constants', 'Coordinators', 'Models', 'Services', 'Helpers', 'ViewModels', 'Views', 'Controls', 'Converters', 'Styles', 'ViewTexts', 'Tests'
    $onDisk = @($trackedAll | Where-Object {
            $_ -match '\.(cs|xaml|json|csproj)$' -and $_ -notmatch '(^|/)(bin|obj)/' -and $_ -notmatch '\.g(\.i)?\.cs$' -and
            ($scopeDirs -contains $_.Split('/')[0] -or $_ -notmatch '/')
        })
    $docSet = @{}; foreach ($d in $documented) { $docSet[$d.ToLowerInvariant()] = $true }
    $diskSet = @{}; foreach ($d in $onDisk) { $diskSet[$d.ToLowerInvariant()] = $true }

    $undocumented = @($onDisk | Where-Object { -not $docSet.ContainsKey($_.ToLowerInvariant()) })
    $stale = @($documented | Where-Object { -not $diskSet.ContainsKey($_.ToLowerInvariant()) -and -not (Test-Path -LiteralPath (Join-Path $script:RepoRoot $_)) })
    Add-ListFinding $r Warn 'Files that file-tree.md does not list.' $undocumented 'tree-missing' 20
    Add-ListFinding $r Warn 'Files listed in file-tree.md that no longer exist.' $stale 'tree-stale' 20

    # api-surface.md must mention every public type.
    $api = (Read-TextFile "$manifest/api-surface.md").Text
    $typeRx = [regex]'(?m)^\s*public\s+(?:(?:static|sealed|abstract|partial|readonly)\s+)*(?:class|interface|record|enum|struct)\s+(?<n>[A-Za-z_]\w*)'
    $unlisted = @()
    foreach ($path in (Get-AppSourceFile -Extension '.cs')) {
        if ($path -match '\.g(\.i)?\.cs$' -or $path -eq 'AssemblyInfo.cs') { continue }
        foreach ($m in $typeRx.Matches((Read-TextFile $path).Text)) {
            $name = $m.Groups['n'].Value
            if ($api -notmatch ('\b' + [regex]::Escape($name) + '\b')) { $unlisted += "$name  ($path)" }
        }
    }
    Add-ListFinding $r Warn 'Public types that api-surface.md does not mention (AGENTS.md: new public API needs a manifest entry).' $unlisted 'api-missing' 20

    # tech-stack.md must name every dependency.
    $tech = (Read-TextFile "$manifest/tech-stack.md").Text
    $undeclared = @()
    foreach ($proj in ($trackedAll | Where-Object { $_ -like '*.csproj' -and $_ -notmatch '^Tests/' })) {
        foreach ($m in [regex]::Matches((Read-TextFile $proj).Text, '<PackageReference\s+Include="(?<n>[^"]+)"')) {
            $name = $m.Groups['n'].Value
            if ($tech -notmatch [regex]::Escape($name) -and $undeclared -notcontains $name) { $undeclared += $name }
        }
    }
    Add-ListFinding $r Warn 'NuGet packages that tech-stack.md does not mention.' $undeclared 'tech-missing'

    # localization.md must list every locale.
    $locDoc = (Read-TextFile "$manifest/localization.md").Text
    $unlistedLocales = @($trackedAll | Where-Object { $_ -match '^ViewTexts/Locales/(?<c>[^/]+)\.json$' } | ForEach-Object { $Matches['c'] } | Where-Object { $locDoc -notmatch [regex]::Escape($_) })
    Add-ListFinding $r Warn 'Locales that localization.md does not mention.' $unlistedLocales 'locale-doc'

    # Relative markdown links must resolve.
    $docFiles = @($trackedAll | Where-Object {
            $_ -match '\.md$' -and $_ -notmatch '^Docs/Agents/(implementation-history|insights|plans|research)/' -and
            ($_ -match '^(README|AGENTS|CLAUDE)\.md$' -or $_ -match '^Docs/Agents/(project-manifest|Sorting Scenarios)/' -or $_ -match '^(Tools|Tests)/.*README\.md$')
        })
    $broken = @()
    foreach ($doc in $docFiles) {
        $dir = Split-Path -Parent (Join-Path $script:RepoRoot $doc)
        $text = (Read-TextFile $doc).Text
        $text = [regex]::Replace($text, '(?s)```.*?```', '')
        foreach ($m in [regex]::Matches($text, '\]\((?<t>[^)]+)\)')) {
            $target = $m.Groups['t'].Value.Trim().Trim('<', '>')
            if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#') -or $target -match '\s"') { continue }
            $target = ($target -split '#')[0]
            if ($target -eq '') { continue }
            $target = [System.Uri]::UnescapeDataString($target)
            $full = Join-Path $dir $target
            if (-not (Test-Path -LiteralPath $full)) { $broken += "${doc}:$(Get-LineNumber $text $m.Index)  $($m.Groups['t'].Value)" }
        }
    }
    Add-ListFinding $r Warn 'Broken relative links in the documentation.' $broken 'broken-link' 20

    return $r
}
