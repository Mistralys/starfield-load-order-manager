function New-UnicodePattern {
    param([string]$Pattern)
    New-Object System.Text.RegularExpressions.Regex($Pattern)
}

function Get-MatchLocation {
    param([string]$Path, [string]$Text, [int]$Index, [int]$Length = 1)
    $line = Get-LineNumber -Text $Text -Index $Index
    $start = [Math]::Max(0, $Index - 12)
    $len = [Math]::Min($Text.Length - $start, $Length + 24)
    $snippet = ($Text.Substring($start, $len) -replace '[\r\n\t]+', ' ')
    # Escape non-ASCII so that the console cannot mangle the evidence a second time.
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $snippet.ToCharArray()) {
        $code = [int]$ch
        if ($code -lt 32 -or $code -gt 126) { [void]$sb.AppendFormat('<U+{0:X4}>', $code) }
        else { [void]$sb.Append($ch) }
    }
    return ('{0}:{1}  {2}' -f $Path, $line, $sb.ToString())
}

function Invoke-UnicodeCheck {
    $r = New-CheckResult 'unicode' 'Encoding and unicode integrity'

    $mojibake = New-UnicodePattern (
        '\u00C3[\u0080-\u00BF\u0152\u0153\u0160\u0161\u0178\u017D\u017E\u02C6\u02DC\u2013\u2014\u2018-\u201E\u2020-\u2022\u2026\u2030\u2039\u203A\u20AC\u2122]' +
        '|\u00E2(?:\u20AC|\u0080)[\u0090-\u00BF\u0152\u0153\u0161\u0178\u017E\u02DC\u2013\u2014\u2018-\u201E\u2020-\u2022\u2026\u2030\u2039\u203A\u2122]' +
        '|\u00C2[\u00A0-\u00BF]' +
        '|\u00EF\u00BB\u00BF' +
        '|\u00E3\u20AC')
    $c1 = New-UnicodePattern '[\u0080-\u009F]'
    $bidi = New-UnicodePattern '[\u202A-\u202E\u2066-\u2069]'
    $zeroWidth = New-UnicodePattern '[\u200B\u200C\u200E\u200F\u2060\uFEFF]'
    $nul = New-UnicodePattern '\u0000'

    $invalid = @(); $replacement = @(); $moji = @(); $control = @(); $bidiHits = @(); $zero = @(); $nulHits = @(); $bom = @()
    $scanned = 0

    foreach ($path in (Get-RepoFile)) {
        if (-not (Test-TextPath $path)) { continue }
        $scanned++
        $file = Read-TextFile $path
        $text = $file.Text

        if (-not $file.ValidUtf8) {
            $i = $text.IndexOf([char]0xFFFD)
            $where = $(if ($i -ge 0) { "first invalid byte near line $(Get-LineNumber $text $i)" } else { 'invalid byte sequence' })
            $invalid += "$path ($where)"
            continue
        }

        $i = $text.IndexOf([char]0xFFFD)
        if ($i -ge 0) { $replacement += Get-MatchLocation $path $text $i }

        $m = $mojibake.Match($text)
        if ($m.Success) { $moji += Get-MatchLocation $path $text $m.Index $m.Length }

        $m = $c1.Match($text)
        if ($m.Success) { $control += Get-MatchLocation $path $text $m.Index }

        $m = $bidi.Match($text)
        if ($m.Success) { $bidiHits += Get-MatchLocation $path $text $m.Index }

        $m = $zeroWidth.Match($text)
        if ($m.Success) { $zero += Get-MatchLocation $path $text $m.Index }

        $m = $nul.Match($text)
        if ($m.Success) { $nulHits += Get-MatchLocation $path $text $m.Index }

        if ($file.HasBom -and ($path -match '^ViewTexts/Locales/.+\.json$' -or $path -match '^\.github/workflows/')) {
            $bom += $path
        }
    }

    Add-ListFinding $r Fail 'Files that are not valid UTF-8 (probably saved in a legacy code page).' $invalid 'invalid-utf8'
    Add-ListFinding $r Fail 'Files containing the U+FFFD replacement character (a character was already lost).' $replacement 'fffd'
    Add-ListFinding $r Fail 'Probable mojibake (UTF-8 text that was decoded as a legacy code page and saved again).' $moji 'mojibake'
    Add-ListFinding $r Fail 'Files containing C1 control characters (U+0080-U+009F), usually a mojibake remnant.' $control 'c1'
    Add-ListFinding $r Fail 'Files containing bidirectional override characters.' $bidiHits 'bidi'
    Add-ListFinding $r Fail 'Text files containing NUL characters.' $nulHits 'nul'
    Add-ListFinding $r Warn 'Invisible zero-width characters or stray byte order marks inside the text.' $zero 'zero-width'
    Add-ListFinding $r Warn 'Locale or workflow files saved with a byte order mark.' $bom 'bom'

    if ($r.Status -eq 'PASS') { Add-Finding $r Info "$scanned text files scanned; all are valid UTF-8 without damaged characters." }
    return $r
}

function Invoke-HygieneCheck {
    $r = New-CheckResult 'hygiene' 'Repository hygiene'

    $mixed = @(); $markers = @(); $todos = @(); $debug = @(); $paths = @(); $secrets = @()
    $secretPatterns = @(
        '-----BEGIN (?:RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----',
        'gh[pousr]_[A-Za-z0-9]{36,}',
        'github_pat_[A-Za-z0-9_]{40,}',
        'AKIA[0-9A-Z]{16}'
    )
    $appFiles = @(Get-AppSourceFile -Extension '.cs', '.xaml')

    foreach ($path in (Get-RepoFile)) {
        if (-not (Test-TextPath $path)) { continue }
        $file = Read-TextFile $path
        if (-not $file.ValidUtf8) { continue }
        $text = $file.Text

        $crlf = ([regex]::Matches($text, "\r\n")).Count
        $lf = ([regex]::Matches($text, "(?<!\r)\n")).Count
        if ($crlf -gt 0 -and $lf -gt 0) { $mixed += "$path (CRLF $crlf, LF $lf)" }

        $m = [regex]::Match($text, '(?m)^(<{7}|>{7})(\s.*)?$')
        if ($m.Success) { $markers += "${path}:$(Get-LineNumber $text $m.Index)" }

        foreach ($pattern in $secretPatterns) {
            $m = [regex]::Match($text, $pattern)
            if ($m.Success) { $secrets += "${path}:$(Get-LineNumber $text $m.Index)  (matches $pattern)" }
        }

        if ($path -notmatch '^Docs/Agents/(implementation-history|insights|plans|research)/' -and $path -notmatch '^Tools/ReleaseCheck/') {
            # Placeholder names (username, Example, ...) are documentation, not leaked paths.
            $m = [regex]::Match($text, '(?i)[A-Za-z]:\\Users\\(?!(?:username|example|user|yourname|your_name|name|public|default)\b)[^\\\s"''<>%]+')
            if ($m.Success) { $paths += "${path}:$(Get-LineNumber $text $m.Index)" }
        }

        if ($appFiles -contains $path) {
            foreach ($m in [regex]::Matches($text, '\b(TODO|FIXME|HACK|XXX)\b')) {
                $todos += "${path}:$(Get-LineNumber $text $m.Index)  $($m.Value)"
            }
            if ($path -like '*.cs') {
                foreach ($m in [regex]::Matches($text, 'Debugger\.(Break|Launch)\s*\(|\bConsole\.Write(Line)?\s*\(')) {
                    $debug += "${path}:$(Get-LineNumber $text $m.Index)  $($m.Value)"
                }
            }
        }
    }

    $tracked = @((Invoke-Git @('ls-files')).Output)
    $junk = @($tracked | Where-Object { $_ -match '\.(tmp|bak|orig|rej|suo|user|swp)$' -or $_ -match '(^|/)(bin|obj)/' -or $_ -match '~$' })

    Add-ListFinding $r Fail 'Unresolved merge conflict markers.' $markers 'conflict'
    Add-ListFinding $r Fail 'Possible secrets or private keys.' $secrets 'secret'
    Add-ListFinding $r Fail 'Build output or temporary files are tracked by git.' $junk 'junk'
    Add-ListFinding $r Warn 'Debug leftovers in application code (Debugger calls, Console output).' $debug 'debug'
    Add-ListFinding $r Warn 'TODO / FIXME / HACK markers in application code.' $todos 'todo'
    Add-ListFinding $r Warn 'Hard-coded user profile paths in tracked files.' $paths 'local-path'
    Add-ListFinding $r Warn 'Files with mixed line endings.' $mixed 'line-endings'

    return $r
}
