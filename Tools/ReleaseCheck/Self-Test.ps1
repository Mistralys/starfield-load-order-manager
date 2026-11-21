# Self-test for the release check. Pure helpers are tested directly; the checks are run against
# throw-away fixture folders (no git repository needed) to prove that they actually fail when
# they should. Usage: pwsh -File Tools\ReleaseCheck\Self-Test.ps1   (exit code 1 on any failure)

$ErrorActionPreference = 'Stop'
$checkRoot = $PSScriptRoot
$script:Options = [pscustomobject]@{ Offline = $true; Fetch = $false; AllowDirty = $true; IncludePublish = $false }

. (Join-Path $checkRoot 'Common.ps1')
foreach ($file in (Get-ChildItem -LiteralPath (Join-Path $checkRoot 'Checks') -Filter '*.ps1' | Sort-Object Name)) { . $file.FullName }

$script:Failures = New-Object System.Collections.ArrayList
$script:Passed = 0

function Assert-That {
    param([bool]$Condition, [string]$Name)
    if ($Condition) { $script:Passed++ }
    else { [void]$script:Failures.Add($Name); Write-Host "  FAILED: $Name" -ForegroundColor Red }
}

function Test-HasFinding {
    param($Result, [string]$Level, [string]$Pattern)
    return (@($Result.Findings | Where-Object { $_.Level -eq $Level -and ($_.Message -match $Pattern -or ($_.Details -join ' ') -match $Pattern) }).Count -gt 0)
}

#region Fixture plumbing

$script:FixtureRoot = $null
$script:FixtureTags = @()

# The checks ask git for the file list and the tags; point them at the fixture instead.
function Get-RepoFile {
    @(Get-ChildItem -LiteralPath $script:FixtureRoot -Recurse -File | ForEach-Object {
            $_.FullName.Substring($script:FixtureRoot.Length).TrimStart('\', '/').Replace('\', '/')
        } | Sort-Object)
}

function Get-ReleaseTag {
    $list = @()
    foreach ($t in $script:FixtureTags) { $list += [pscustomobject]@{ Tag = $t; Version = (ConvertFrom-VersionText $t) } }
    return $list
}

function New-Fixture {
    param([hashtable]$Files)

    if ($script:FixtureRoot -and (Test-Path -LiteralPath $script:FixtureRoot)) { Remove-Item -LiteralPath $script:FixtureRoot -Recurse -Force }
    $script:FixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('release-check-selftest-' + [guid]::NewGuid().ToString('N'))
    [void](New-Item -ItemType Directory -Path $script:FixtureRoot)
    $script:RepoRoot = $script:FixtureRoot
    $script:TextCache = @{}
    $script:FileListCache = $null
    $script:SymbolIndex = $null
    $script:LocaleTable = $null

    $utf8 = New-Object System.Text.UTF8Encoding($false)
    foreach ($name in $Files.Keys) {
        $full = Join-Path $script:FixtureRoot $name
        [void](New-Item -ItemType Directory -Force -Path (Split-Path -Parent $full))
        $content = $Files[$name]
        if ($content -is [byte[]]) { [System.IO.File]::WriteAllBytes($full, $content) }
        else { [System.IO.File]::WriteAllText($full, $content, $utf8) }
    }
}

#endregion

Write-Host 'Version helpers' -ForegroundColor Cyan
$v19 = ConvertFrom-VersionText '1.9.0'
Assert-That ((Get-VersionStep $v19 (ConvertFrom-VersionText '1.9.1')) -eq 'patch') 'patch step'
Assert-That ((Get-VersionStep $v19 (ConvertFrom-VersionText '1.10.0')) -eq 'minor') 'minor step'
Assert-That ((Get-VersionStep $v19 (ConvertFrom-VersionText '2.0.0')) -eq 'major') 'major step'
Assert-That ($null -eq (Get-VersionStep $v19 (ConvertFrom-VersionText '1.9.2'))) 'skipping a patch is not one step'
Assert-That ($null -eq (Get-VersionStep $v19 (ConvertFrom-VersionText '1.11.0'))) 'skipping a minor is not one step'
Assert-That ($null -eq (Get-VersionStep $v19 (ConvertFrom-VersionText '2.1.0'))) 'major bump must reset the minor'
Assert-That ((Get-VersionStep $v19 (ConvertFrom-VersionText '2.0.0-beta.1')) -eq 'major-prerelease') 'pre-release of the next major'
Assert-That ((Get-VersionStep (ConvertFrom-VersionText '2.0.0-beta.1') (ConvertFrom-VersionText '2.0.0')) -eq 'release') 'finalizing a pre-release'
Assert-That ((Compare-Version (ConvertFrom-VersionText '1.0.0-rc.1') (ConvertFrom-VersionText '1.0.0')) -lt 0) 'pre-release sorts below its release'
Assert-That ((Compare-Version (ConvertFrom-VersionText '1.10.0') (ConvertFrom-VersionText '1.9.0')) -gt 0) 'numeric, not lexical, comparison'
Assert-That ($null -eq (ConvertFrom-VersionText 'not-a-version')) 'garbage is not a version'

Write-Host 'Changelog helpers' -ForegroundColor Cyan
$sample = @('# Changelog', '', '## v1.2.0 - Newest', '', '- Area: one', '- Area: two', '', '## v1.1.0 - Older', '- Area: old') -join "`n"
$lines = $sample -split "`n"
$notes = Get-ReleaseNotesEntry -Lines $lines -Tag '1.2.0'
Assert-That ($notes.Found -and $notes.Title -eq 'Newest' -and $notes.Lines.Count -eq 2) 'release notes extraction mirrors release.yml'
Assert-That (-not (Get-ReleaseNotesEntry -Lines $lines -Tag '9.9.9').Found) 'missing entry is reported'
$entries = @(ConvertFrom-Changelog $lines)
Assert-That ($entries.Count -eq 2 -and $entries[0].Version -eq '1.2.0' -and $entries[0].HasPrefix) 'changelog entries are parsed'
$wip = @(ConvertFrom-Changelog @('## v1.3.0 - **WIP; UNRELEASED**'))
Assert-That ($wip[0].IsWip -and $wip[0].Version -eq '1.3.0') 'WIP marker is detected'

Write-Host 'Menu and binding helpers' -ForegroundColor Cyan
Assert-That ((Get-AccessKey '_File') -eq 'f') 'access key at the start'
Assert-That ((Get-AccessKey 'E_xit') -eq 'x') 'access key in the middle'
$cjk = [string][char]0x6587 + [char]0x4EF6 + [char]0xFF08 + '_F' + [char]0xFF09
Assert-That ((Get-AccessKey $cjk) -eq 'f') 'CJK access key in full-width parentheses'
Assert-That ($null -eq (Get-AccessKey 'Plain')) 'no access key'
Assert-That ($null -eq (Get-AccessKey 'Snake__case')) 'escaped underscore is not an access key'
Assert-That ((Get-BindingPath '{Binding Menu.FileMenuHeader}') -eq 'Menu.FileMenuHeader') 'binding path'
Assert-That ((Get-BindingPath '{Binding Path=Foo, Mode=OneWay}') -eq 'Foo') 'explicit Path='
Assert-That ((Get-BindingPath '{Binding RelativeSource={RelativeSource Self}}') -eq '') 'binding without a path'
Assert-That ($null -eq (Get-BindingPath 'Plain text')) 'literal is not a binding'

Write-Host 'Version check against fixtures' -ForegroundColor Cyan
$csproj = '<Project><PropertyGroup><Version>1.9.0</Version></PropertyGroup></Project>'
function Test-VersionFixture {
    param([string]$Changelog, [string[]]$Tags = @('1.8.0', '1.9.0'))
    New-Fixture @{ 'changelog.md' = $Changelog; 'Starfield Load Order Keeper.csproj' = $csproj }
    $script:FixtureTags = $Tags
    return (Invoke-VersionCheck)
}

$r = Test-VersionFixture "# Changelog`n`n## v1.9.1 - Fix`n- Area: fixed`n`n## v1.9.0 - Old`n- Area: old"
Assert-That ($r.Status -ne 'FAIL') 'next patch passes'
$r = Test-VersionFixture "# Changelog`n`n## v1.9.0 - Same`n- Area: x"
Assert-That (Test-HasFinding $r 'Fail' 'equals the last tag') 'entry equal to the last tag fails'
$r = Test-VersionFixture "# Changelog`n`n## v1.9.3 - Jump`n- Area: x"
Assert-That (Test-HasFinding $r 'Fail' 'not exactly one step') 'skipped version fails'
$r = Test-VersionFixture "# Changelog`n`n## v2.0.0 - **WIP; UNRELEASED**`n- Area: x"
Assert-That (Test-HasFinding $r 'Fail' 'WIP') 'WIP marker fails'
$r = Test-VersionFixture "# Changelog`n`n## 2.0.0 - No prefix`n- Area: x"
Assert-That (Test-HasFinding $r 'Fail' "'v' prefix") 'missing v prefix fails'
$r = Test-VersionFixture "# Changelog`n`n## v2.0.0 - Empty`n`n## v1.9.0 - Old`n- Area: old"
Assert-That (Test-HasFinding $r 'Fail' 'no content') 'empty entry fails'
$r = Test-VersionFixture "# Changelog`n`n## v1.9.1 - A`n- Area: a`n`n## v1.9.1 - B`n- Area: b"
Assert-That (Test-HasFinding $r 'Fail' 'more than once') 'duplicate heading fails'
$r = Test-VersionFixture "# Changelog`n`n## v1.9.1 - A`n- no area prefix here"
Assert-That (Test-HasFinding $r 'Warn' 'Area: text') 'bullet style is flagged'
$r = Test-VersionFixture "# Changelog`n`n## v2.0.0-beta.1 - Preview`n- Area: a"
Assert-That ($r.Status -ne 'FAIL') 'pre-release of the next major passes'
$r = Test-VersionFixture "# Changelog`n`n## v1.9.1 - Fix`n- Area: a" @()
Assert-That (Test-HasFinding $r 'Warn' 'No release tags') 'missing tags are reported'

Write-Host 'Unicode check against fixtures' -ForegroundColor Cyan
$accents = [string][char]0x00E9 + [char]0x00FC + [char]0x4E2D
$mojibake = 'caf' + [char]0x00C3 + [char]0x00A9
New-Fixture @{
    'good.md'    = "Valid text: $accents`n"
    'latin1.cs'  = [byte[]](0x63, 0x61, 0x66, 0xE9, 0x0A)
    'moji.md'    = "$mojibake`n"
    'fffd.md'    = ("lost " + [char]0xFFFD + "`n")
    'bidi.cs'    = ("// " + [char]0x202E + " evil`n")
    'zero.md'    = ("a" + [char]0x200B + "b`n")
}
$r = Invoke-UnicodeCheck
Assert-That (Test-HasFinding $r 'Fail' 'not valid UTF-8') 'invalid UTF-8 is detected'
Assert-That (Test-HasFinding $r 'Fail' 'latin1.cs') 'invalid file is named'
Assert-That (Test-HasFinding $r 'Fail' 'mojibake') 'mojibake is detected'
Assert-That (Test-HasFinding $r 'Fail' 'replacement') 'U+FFFD is detected'
Assert-That (Test-HasFinding $r 'Fail' 'bidirectional') 'bidi override is detected'
Assert-That (Test-HasFinding $r 'Warn' 'zero-width') 'zero-width character is flagged'
Assert-That (-not (Test-HasFinding $r 'Fail' 'good\.md')) 'valid accented text is not flagged'

New-Fixture @{ 'ok.md' = "Plain $accents text`n"; 'ok.cs' = "class A { }`n" }
$r = Invoke-UnicodeCheck
Assert-That ($r.Status -eq 'PASS') 'clean fixture passes'

Write-Host 'Menu and hard-coded text checks against fixtures' -ForegroundColor Cyan
$xaml = @'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        Title="Hard coded title">
  <Menu>
    <MenuItem Header="{Binding FileHeader}" d:Header="_File">
      <MenuItem Header="{Binding GoText}" Command="{Binding GoCommand}" />
      <MenuItem Header="{Binding GoText}" Command="{Binding TypoCommand}" />
      <MenuItem Header="{Binding GoText}" />
      <MenuItem Header="{Binding GoText}" Click="OnMissing" />
      <Separator />
      <MenuItem Header="Literal text" Command="{Binding GoCommand}" />
    </MenuItem>
  </Menu>
</Window>
'@
New-Fixture @{
    'Views/Sample.xaml'    = $xaml
    'Views/Sample.xaml.cs' = 'public partial class Sample { }'
    'Models/Vm.cs'         = 'public class Vm { public IRelayCommand GoCommand { get; } public string FileHeader => "x"; public string GoText => "y"; }'
}
$r = Invoke-MenuCheck
Assert-That (Test-HasFinding $r 'Fail' "TypoCommand") 'command that does not exist fails'
Assert-That (Test-HasFinding $r 'Fail' 'no action') 'menu item without an action fails'
Assert-That (Test-HasFinding $r 'Fail' "OnMissing") 'missing click handler fails'
Assert-That (-not (Test-HasFinding $r 'Fail' 'GoCommand')) 'existing command passes'
$r = Invoke-HardcodedTextCheck
Assert-That (Test-HasFinding $r 'Fail' 'Literal text') 'literal header is reported'
Assert-That (Test-HasFinding $r 'Fail' 'Hard coded title') 'literal window title is reported'
Assert-That (-not (Test-HasFinding $r 'Fail' '"_File"')) 'design-time (d:) text is ignored'

Write-Host 'Localization check against fixtures' -ForegroundColor Cyan
$en = '{ "LocaleName": "English", "ParentCulture": "en", "Dlg": { "ItemCountFormat": "{0} items", "ItemCountSingular": "1 item", "Used": "Used text", "Unused": "Unused text", "Menu_File": "_File" } }'
$de = '{ "LocaleName": "Deutsch", "ParentCulture": "de", "Dlg": { "ItemCountFormat": "{0} Elemente", "ItemCountSingular": "{0} Element", "Used": "Benutzt", "Unused": "Unbenutzt", "Menu_File": "Datei" } }'
$csprojLoc = '<Project><ItemGroup><None Update="ViewTexts\Locales\en-US.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None></ItemGroup></Project>'
New-Fixture @{
    'Starfield Load Order Keeper.csproj' = $csprojLoc
    'ViewTexts/Locales/en-US.json'       = $en
    'ViewTexts/Locales/de-DE.json'       = $de
    'Views/Code.cs'                      = 'class C { void M() { _localization.GetString("Dlg", "Used"); _localization.GetString("Dlg", "Missing"); } }'
}
$r = Invoke-LocalizationCheck
Assert-That (Test-HasFinding $r 'Fail' 'Dlg\.Missing') 'key used in code but absent from en-US fails'
Assert-That (Test-HasFinding $r 'Warn' 'Dlg\.Unused') 'unused key is flagged'
Assert-That (Test-HasFinding $r 'Fail' 'Singular form must not contain') 'singular with {0} fails'
Assert-That (Test-HasFinding $r 'Fail' 'de-DE\.json is not copied|Locale de-DE\.json') 'locale missing from the csproj fails'
Assert-That (Test-HasFinding $r 'Warn' 'lost the access key') 'lost access key is flagged'

Write-Host 'Allow-list' -ForegroundColor Cyan
New-Fixture @{ 'bad.md' = [byte[]](0x63, 0x61, 0x66, 0xE9, 0x0A); 'worse.md' = [byte[]](0x78, 0xE9, 0x0A) }
$script:Allow = @([pscustomobject]@{ check = 'unicode'; match = 'invalid-utf8:bad\.md'; reason = 'self-test' })
$r = Invoke-UnicodeCheck
Assert-That ($r.Suppressed -eq 1) 'allow-list suppresses exactly the matching item'
Assert-That (Test-HasFinding $r 'Fail' 'worse\.md') 'items outside the rule are still reported'
Assert-That (-not (Test-HasFinding $r 'Fail' 'bad\.md')) 'suppressed item is no longer reported'
$script:Allow = @()

# Cleanup.
if ($script:FixtureRoot -and (Test-Path -LiteralPath $script:FixtureRoot)) { Remove-Item -LiteralPath $script:FixtureRoot -Recurse -Force }

Write-Host ''
if ($script:Failures.Count -eq 0) {
    Write-Host ("Self-test passed ({0} assertions)." -f $script:Passed) -ForegroundColor Green
    exit 0
}
Write-Host ("Self-test FAILED: {0} of {1} assertions." -f $script:Failures.Count, ($script:Passed + $script:Failures.Count)) -ForegroundColor Red
exit 1
