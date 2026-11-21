Add-Type -AssemblyName System.Xml.Linq

$script:SymbolIndex = $null

#region XAML and code indexes

function Get-XamlDocument {
    param([string]$Path)
    $text = (Read-TextFile $Path).Text
    [System.Xml.Linq.XDocument]::Parse($text, [System.Xml.Linq.LoadOptions]::SetLineInfo)
}

function Get-XmlLine {
    param($Node)
    $info = [System.Xml.IXmlLineInfo]$Node
    if ($info.HasLineInfo()) { return $info.LineNumber }
    return 0
}

function Get-XmlAttributeValue {
    param($Element, [string]$Name)
    foreach ($a in $Element.Attributes()) {
        if ($a.Name.LocalName -eq $Name -and $a.Name.NamespaceName -eq '') { return $a.Value }
    }
    return $null
}

# Returns the binding path of "{Binding Path}" / "{Binding Path=Path, ...}", '' for a bare or
# path-less binding, and $null when the value is not a binding at all.
function Get-BindingPath {
    param([string]$Value)
    if ($null -eq $Value) { return $null }
    $m = [regex]::Match($Value, '^\{\s*Binding(?:\s+(?:Path\s*=\s*)?(?<p>[^,\}\s=]+)(?=\s*[,\}]))?')
    if (-not $m.Success) { return $null }
    return $m.Groups['p'].Value
}

# Members the XAML may bind to, and the locale key behind every text property.
function Get-SymbolIndex {
    if ($null -ne $script:SymbolIndex) { return $script:SymbolIndex }

    $members = New-Object 'System.Collections.Generic.HashSet[string]'
    $classes = @{}

    $typeToken = '[\w<>\[\],\.\?]+'
    # Any identifier that is followed by an accessor block or an expression body is a member name;
    # types are deliberately not matched so that generic and tuple types need no special casing.
    $propertyRx = [regex]'\b(?<n>[A-Za-z_]\w*)\s*(?:\{\s*(?:get|set|init)\b|=>)'
    $relayRx = [regex]("\[RelayCommand[^\]]*\]\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|internal|protected)\s+)?(?:(?:static|async)\s+)*$typeToken\s+(?<n>\w+)\s*\(")
    $observableRx = [regex]("\[ObservableProperty[^\]]*\]\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|internal|protected)\s+)?$typeToken\s+_?(?<n>\w+)\s*(?:=|;)")
    $textRx = [regex]'(?m)public\s+string\s+(?<p>\w+)\s*=>\s*(?<e>[^;]+);'
    $keyRx = [regex]'GetString\(\s*"(?<s>[^"]+)"\s*,\s*"(?<k>[^"]+)"'
    $classRx = [regex]'\b(?:class|record)\s+(?<c>\w+)'

    foreach ($path in (Get-AppSourceFile -Extension '.cs')) {
        $text = (Read-TextFile $path).Text

        foreach ($m in $propertyRx.Matches($text)) { [void]$members.Add($m.Groups['n'].Value) }
        foreach ($m in $relayRx.Matches($text)) { [void]$members.Add(($m.Groups['n'].Value -replace 'Async$', '') + 'Command') }
        foreach ($m in $observableRx.Matches($text)) {
            $n = $m.Groups['n'].Value
            if ($n.Length -gt 0) { [void]$members.Add($n.Substring(0, 1).ToUpperInvariant() + $n.Substring(1)) }
        }

        $classMatch = $classRx.Match($text)
        if (-not $classMatch.Success) { continue }
        $className = $classMatch.Groups['c'].Value
        if (-not $classes.ContainsKey($className)) { $classes[$className] = @{} }

        foreach ($m in $textRx.Matches($text)) {
            $expr = $m.Groups['e'].Value.Trim()
            $k = $keyRx.Match($expr)
            if ($k.Success) {
                $classes[$className][$m.Groups['p'].Value] = @{ Key = ($k.Groups['s'].Value + '.' + $k.Groups['k'].Value) }
            }
            elseif ($expr -match '^(?:(?<o>[\w\.\?]+)\.)?(?<n>\w+)$') {
                $classes[$className][$m.Groups['p'].Value] = @{ Owner = $Matches['o']; Name = $Matches['n'] }
            }
        }
    }

    $script:SymbolIndex = [pscustomobject]@{ Members = $members; Classes = $classes }
    return $script:SymbolIndex
}

# Resolves a text property to the locale keys behind it. Owner and window hints narrow down the
# class when several classes define a property of the same name.
function Resolve-TextKey {
    param([string]$Name, [string]$OwnerHint, [string]$WindowHint, [int]$Depth = 0)

    if ($Depth -gt 4) { return @() }
    $index = Get-SymbolIndex

    $candidates = @($index.Classes.Keys | Where-Object { $index.Classes[$_].ContainsKey($Name) })
    if ($candidates.Count -gt 1 -and $OwnerHint) {
        $hint = $OwnerHint.TrimStart('_')
        $narrow = @($candidates | Where-Object { $_ -like "$hint*" })
        if ($narrow.Count -gt 0) { $candidates = $narrow }
    }
    if ($candidates.Count -gt 1 -and $WindowHint) {
        $narrow = @($candidates | Where-Object { $_ -like "*$WindowHint*" })
        if ($narrow.Count -gt 0) { $candidates = $narrow }
    }

    $keys = @()
    foreach ($c in $candidates) {
        $entry = $index.Classes[$c][$Name]
        if ($entry.ContainsKey('Key')) { $keys += $entry.Key }
        else { $keys += Resolve-TextKey -Name $entry.Name -OwnerHint $entry.Owner -WindowHint $WindowHint -Depth ($Depth + 1) }
    }
    return @($keys | Select-Object -Unique)
}

function Get-LocaleTable {
    if ($null -ne $script:LocaleTable) { return $script:LocaleTable }
    $table = [ordered]@{}
    foreach ($path in @(Get-RepoFile | Where-Object { $_ -match '^ViewTexts/Locales/[^/]+\.json$' })) {
        try { $table[[System.IO.Path]::GetFileNameWithoutExtension($path)] = ConvertFrom-LocaleJson -Json (Read-TextFile $path).Text } catch { }
    }
    $script:LocaleTable = $table
    return $table
}

#endregion

#region Menus

function Test-MenuContainer {
    param($Container, [string]$File, [string]$CodeBehind, [string]$WindowHint, $Result, $Stats)

    $items = @($Container.Elements() | Where-Object { $_.Name.LocalName -eq 'MenuItem' })
    $index = Get-SymbolIndex
    $locales = Get-LocaleTable
    $accessKeys = @{}

    foreach ($item in $items) {
        $Stats.Items++
        $line = Get-XmlLine $item
        $where = "${File}:$line"

        $header = Get-XmlAttributeValue $item 'Header'
        $command = Get-XmlAttributeValue $item 'Command'
        $click = Get-XmlAttributeValue $item 'Click'
        $checked = (Get-XmlAttributeValue $item 'Checked') -or (Get-XmlAttributeValue $item 'Unchecked')
        $itemsSource = (Get-XmlAttributeValue $item 'ItemsSource') -or @($item.Elements() | Where-Object { $_.Name.LocalName -eq 'MenuItem.ItemsSource' }).Count -gt 0
        $children = @($item.Elements() | Where-Object { $_.Name.LocalName -eq 'MenuItem' })
        $setters = @($item.Descendants() | Where-Object {
                $_.Name.LocalName -eq 'Setter' -and (Get-XmlAttributeValue $_ 'Property') -eq 'Command' })
        $eventSetters = @($item.Descendants() | Where-Object {
                $_.Name.LocalName -eq 'EventSetter' -and (Get-XmlAttributeValue $_ 'Event') -eq 'Click' })
        if ($null -eq $header) {
            $headerElement = @($item.Elements() | Where-Object { $_.Name.LocalName -eq 'MenuItem.Header' })
            if ($headerElement.Count -gt 0) { $header = $headerElement[0].Value }
        }
        $label = $(if ($header) { $header } else { '(no header)' })

        # Header: a literal is reported by the hard-coded text check; here it only has to resolve.
        $path = Get-BindingPath $header
        if ($null -eq $header) {
            Add-Finding $Result Warn "Menu item without a header at $where." @() "menu-header:${where}"
        }
        elseif ($path) {
            $leaf = $path.Split('.')[-1]
            if (-not $index.Members.Contains($leaf)) {
                Add-Finding $Result Fail "Menu header binds to '$path', which is not declared anywhere in the code ($where)." @() "menu-header:${where}"
            }
        }

        # Action: a command, a click handler or a submenu.
        $isSubmenu = ($children.Count -gt 0) -or $itemsSource
        if ($command) {
            $cmdPath = Get-BindingPath $command
            if ($cmdPath) {
                $leaf = $cmdPath.Split('.')[-1]
                if (-not $index.Members.Contains($leaf)) {
                    Add-Finding $Result Fail "Menu item '$label' binds Command to '$cmdPath', which is not declared anywhere in the code ($where)." @() "menu-command:${where}"
                }
            }
        }
        elseif ($click) {
            if ($CodeBehind -and $CodeBehind -notmatch ('\bvoid\s+' + [regex]::Escape($click) + '\s*\(')) {
                Add-Finding $Result Fail "Menu item '$label' has Click handler '$click', which is missing from the code-behind ($where)." @() "menu-click:${where}"
            }
        }
        elseif ($setters.Count -gt 0 -or $eventSetters.Count -gt 0 -or $checked) {
            foreach ($s in $setters) {
                $v = Get-BindingPath (Get-XmlAttributeValue $s 'Value')
                if ($v -and -not $index.Members.Contains($v.Split('.')[-1])) {
                    Add-Finding $Result Fail "Menu item '$label' sets Command to '$v' in a style, which is not declared anywhere in the code ($where)." @() "menu-command:${where}"
                }
            }
        }
        elseif ($isSubmenu) {
            if ($itemsSource) {
                Add-Finding $Result Warn "Dynamic submenu '$label' has no Command or Click on its generated items ($where)." @() "menu-dynamic:${where}"
            }
        }
        else {
            Add-Finding $Result Fail "Menu item '$label' has no action: no Command, Click or submenu ($where)." @() "menu-action:${where}"
        }

        # Access keys of the entries of one menu level must be distinct in every language.
        if ($path) {
            $owner = $null
            $parts = $path.Split('.')
            if ($parts.Count -gt 1) { $owner = $parts[$parts.Count - 2] }
            $keys = @(Resolve-TextKey -Name $parts[-1] -OwnerHint $owner -WindowHint $WindowHint)
            if ($keys.Count -eq 1) {
                foreach ($locale in $locales.Keys) {
                    if (-not $locales[$locale].Contains($keys[0])) { continue }
                    $ak = Get-AccessKey $locales[$locale][$keys[0]]
                    if (-not $ak) { continue }
                    $slot = "$locale|$ak"
                    if (-not $accessKeys.ContainsKey($slot)) { $accessKeys[$slot] = @() }
                    $accessKeys[$slot] += "$($locales[$locale][$keys[0]])   [$($keys[0])]"
                }
            }
        }

        if ($children.Count -gt 0) { Test-MenuContainer $item $File $CodeBehind $WindowHint $Result $Stats }
    }

    foreach ($slot in $accessKeys.Keys) {
        if ($accessKeys[$slot].Count -gt 1) {
            $locale, $ak = $slot.Split('|')
            $line = Get-XmlLine $Container
            Add-Finding $Result Warn "Access key '_$ak' is used twice on one menu level in $locale ($File near line $line)." @($accessKeys[$slot]) "accesskey:${File}:$slot"
        }
    }
}

function Invoke-MenuCheck {
    $r = New-CheckResult 'menus' 'Menu consistency'
    $stats = @{ Items = 0; Menus = 0 }

    foreach ($path in (Get-AppSourceFile -Extension '.xaml')) {
        $text = (Read-TextFile $path).Text
        if ($text -notmatch '<(Menu|ContextMenu)\b') { continue }

        try { $doc = Get-XamlDocument $path }
        catch { Add-Finding $r Fail "XAML file is not well-formed: $path." @($_.Exception.Message) "xaml:$path"; continue }

        $codeBehind = $null
        $cbPath = "$path.cs"
        if (Test-Path -LiteralPath (Join-Path $script:RepoRoot $cbPath)) { $codeBehind = (Read-TextFile $cbPath).Text }

        $windowHint = [System.IO.Path]::GetFileNameWithoutExtension($path) -replace '(Window|Dialog)$', ''

        foreach ($menu in @($doc.Descendants() | Where-Object { $_.Name.LocalName -in 'Menu', 'ContextMenu' })) {
            $stats.Menus++
            Test-MenuContainer $menu $path $codeBehind $windowHint $r $stats
        }
    }

    if ($stats.Menus -eq 0) { Add-Finding $r Warn 'No Menu or ContextMenu found in any XAML file; is the scan looking in the right place?' }
    else { Add-Finding $r Info "$($stats.Menus) menus with $($stats.Items) items inspected." }
    return $r
}

#endregion

#region Hard-coded text

function Invoke-HardcodedTextCheck {
    $r = New-CheckResult 'hardcoded' 'Hard-coded UI text'

    $textAttributes = @('Header', 'Content', 'Text', 'ToolTip', 'Title', 'Watermark', 'Label', 'HintAssist.Hint', 'HintAssist.HelperText', 'AutomationProperties.Name', 'AutomationProperties.HelpText')
    $hits = @()
    $dataElements = @('Color', 'Double', 'Int32', 'Boolean', 'Thickness', 'CornerRadius', 'GridLength', 'FontFamily', 'Duration', 'TimeSpan', 'SolidColorBrush')

    foreach ($path in (Get-AppSourceFile -Extension '.xaml')) {
        if ($path -match '^Styles/') { continue }
        try { $doc = Get-XamlDocument $path }
        catch { Add-Finding $r Fail "XAML file is not well-formed: $path." @($_.Exception.Message) "xaml:$path"; continue }

        foreach ($el in $doc.Descendants()) {
            $elName = $el.Name.LocalName
            if ($elName.Contains('.')) { continue }
            $line = Get-XmlLine $el

            foreach ($a in $el.Attributes()) {
                $ns = $a.Name.NamespaceName
                if ($ns -match 'blend/2008|markup-compatibility') { continue }
                if ($a.Name.LocalName -notin $textAttributes) { continue }
                $value = $a.Value
                if ($value -match '^\{\}') { $value = $value.Substring(2) }
                elseif ($value.StartsWith('{')) { continue }
                if ($value -match '\p{L}') {
                    $attrLine = Get-XmlLine $a
                    $hits += [pscustomobject]@{ Key = "${path}:${elName}.$($a.Name.LocalName)=$value"; Text = "${path}:$attrLine  <$elName $($a.Name.LocalName)=`"$value`">" }
                }
            }

            if ($elName -eq 'Setter') {
                $prop = Get-XmlAttributeValue $el 'Property'
                $value = Get-XmlAttributeValue $el 'Value'
                if ($prop -in $textAttributes -and $value -and -not $value.StartsWith('{') -and $value -match '\p{L}') {
                    $hits += [pscustomobject]@{ Key = "${path}:Setter.${prop}=$value"; Text = "${path}:$line  <Setter $prop=`"$value`">" }
                }
            }

            # Element content is text only for text-like elements; colors, sizes and fonts are data.
            if ($elName -in $dataElements) { continue }
            foreach ($node in $el.Nodes()) {
                if ($node -is [System.Xml.Linq.XText]) {
                    $value = $node.Value.Trim()
                    if ($value -match '\p{L}' -and -not $value.StartsWith('{') -and $value -notmatch '^#[0-9A-Fa-f]{3,8}$') {
                        $hits += [pscustomobject]@{ Key = "${path}:${elName}#text=$value"; Text = "${path}:$line  <$elName>$value</$elName>" }
                    }
                }
            }
        }
    }

    foreach ($path in (Get-AppSourceFile -Extension '.cs')) {
        $text = (Read-TextFile $path).Text
        foreach ($m in [regex]::Matches($text, 'MessageBox\.Show\(\s*"([^"]*)"')) {
            $line = Get-LineNumber $text $m.Index
            $hits += [pscustomobject]@{ Key = "${path}:MessageBox=$($m.Groups[1].Value)"; Text = "${path}:$line  MessageBox.Show(`"$($m.Groups[1].Value)`")" }
        }
    }

    $reported = @()
    foreach ($hit in $hits) {
        if (Test-AllowedFinding -CheckId $r.Id -Key $hit.Key) { $r.Suppressed++ } else { $reported += $hit.Text }
    }
    if ($reported.Count -gt 0) {
        Add-Finding $r Fail 'User-visible text is hard-coded; AGENTS.md requires it in the locale files and text ViewModels.' (Limit-List $reported 25)
    }
    return $r
}

#endregion
