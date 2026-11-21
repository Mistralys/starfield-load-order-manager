function New-TempWorkDirectory {
    $path = Join-Path ([System.IO.Path]::GetTempPath()) ('release-check-' + [guid]::NewGuid().ToString('N'))
    [void](New-Item -ItemType Directory -Path $path)
    return $path
}

function Invoke-BuildCheck {
    $r = New-CheckResult 'build' 'Release build'

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Add-Finding $r Fail 'dotnet is not on the PATH.'
        return $r
    }

    $solution = 'Starfield Load Order Keeper.sln'
    # --no-incremental: an incremental build skips compilation and would hide warnings; CI builds from scratch.
    $run = Invoke-Native -File 'dotnet' -Arguments @('build', $solution, '--configuration', 'Release', '--no-incremental', '--nologo', '-v', 'minimal', '-clp:Summary')
    $lines = @($run.Output)

    $errors = @($lines | Where-Object { $_ -match ': error ' } | Select-Object -Unique)
    $warnings = @($lines | Where-Object { $_ -match ': warning ' } | ForEach-Object { ($_ -replace '\s*\[[^\]]+\]\s*$', '').Trim() } | Select-Object -Unique)

    if ($run.ExitCode -ne 0 -or $errors.Count -gt 0) {
        $detail = $(if ($errors.Count -gt 0) { Limit-List $errors 10 } else { Limit-List @($lines | Select-Object -Last 15) 15 })
        Add-Finding $r Fail "dotnet build failed (exit code $($run.ExitCode))." $detail
        return $r
    }

    if ($warnings.Count -gt 0) {
        Add-Finding $r Warn 'The Release build produced warnings.' (Limit-List $warnings 12) 'build-warning'
    }
    return $r
}

function Invoke-TestCheck {
    $r = New-CheckResult 'tests' 'Unit tests'

    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Add-Finding $r Fail 'dotnet is not on the PATH.'
        return $r
    }

    $project = 'Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj'
    $work = New-TempWorkDirectory
    try {
        $run = Invoke-Native -File 'dotnet' -Arguments @('test', $project, '--configuration', 'Release', '--nologo',
            '--logger', 'trx;LogFileName=release-check.trx', '--results-directory', $work)

        $trx = Join-Path $work 'release-check.trx'
        if (-not (Test-Path -LiteralPath $trx)) {
            Add-Finding $r Fail "dotnet test produced no result file (exit code $($run.ExitCode))." (Limit-List @($run.Output | Select-Object -Last 20) 20)
            return $r
        }

        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($trx)
        $counters = $xml.SelectSingleNode('//*[local-name()="Counters"]')
        $total = [int]$counters.GetAttribute('total')
        $passed = [int]$counters.GetAttribute('passed')
        $failed = [int]$counters.GetAttribute('failed')
        $skipped = $total - [int]$counters.GetAttribute('executed')

        if ($total -eq 0) { Add-Finding $r Fail 'No tests were discovered.' }

        if ($failed -gt 0 -or $run.ExitCode -ne 0) {
            $names = @()
            foreach ($result in $xml.SelectNodes('//*[local-name()="UnitTestResult"][@outcome="Failed"]')) {
                $message = $result.SelectSingleNode('.//*[local-name()="Message"]')
                $first = $(if ($message) { ($message.InnerText -split "`r?`n")[0].Trim() } else { '' })
                $names += "$($result.GetAttribute('testName'))  $first"
            }
            if ($names.Count -eq 0) { $names = @($run.Output | Select-Object -Last 15) }
            Add-Finding $r Fail "$failed test(s) failed (exit code $($run.ExitCode))." (Limit-List $names 15)
        }

        if ($skipped -gt 0) {
            $names = @($xml.SelectNodes('//*[local-name()="UnitTestResult"][@outcome="NotExecuted"]') | ForEach-Object { $_.GetAttribute('testName') })
            Add-Finding $r Warn "$skipped test(s) were skipped." (Limit-List $names 10) 'skipped-test'
        }

        if ($r.Status -eq 'PASS' -or $r.Status -eq 'WARN') { Add-Finding $r Info "$passed of $total tests passed." }
    }
    finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
    return $r
}

function Invoke-DependencyCheck {
    $r = New-CheckResult 'dependencies' 'NuGet dependencies'

    if ($script:Options.Offline) {
        Set-CheckSkipped $r 'Skipped in offline mode; the advisory database needs network access.'
        return $r
    }

    $solution = 'Starfield Load Order Keeper.sln'
    $vulnerable = Invoke-Native -File 'dotnet' -Arguments @('list', $solution, 'package', '--vulnerable', '--include-transitive')
    $text = $vulnerable.Text + "`n" + $vulnerable.ErrorText

    if ($vulnerable.ExitCode -ne 0 -or $text -match '(?i)unable to load the service index|failed to retrieve|no network|error NU1301') {
        Add-Finding $r Warn 'Could not query the NuGet advisory database.' (Limit-List @($text -split "`r?`n" | Where-Object { $_ -match '(?i)error|unable|fail' } | Select-Object -First 5) 5)
    }
    else {
        $rows = @($vulnerable.Output | Where-Object { $_ -match '^\s*>\s+\S+.*\b(Critical|High|Moderate|Low)\b' })
        $severe = @($rows | Where-Object { $_ -match '\b(Critical|High)\b' })
        $minor = @($rows | Where-Object { $_ -notmatch '\b(Critical|High)\b' })
        if ($severe.Count -gt 0) { Add-Finding $r Fail 'Packages with critical or high severity advisories.' @($severe | ForEach-Object { $_.Trim() }) }
        if ($minor.Count -gt 0) { Add-Finding $r Warn 'Packages with moderate or low severity advisories.' @($minor | ForEach-Object { $_.Trim() }) }
    }

    $deprecated = Invoke-Native -File 'dotnet' -Arguments @('list', $solution, 'package', '--deprecated')
    $rows = @($deprecated.Output | Where-Object { $_ -match '^\s*>\s+\S+' })
    if ($rows.Count -gt 0) { Add-Finding $r Warn 'Deprecated packages.' @($rows | ForEach-Object { $_.Trim() }) }

    return $r
}

# Mirrors the "Publish WPF application" step of release.yml, then checks what the installer and
# the ZIP would pick up. Slow, so it only runs on request.
function Invoke-PublishCheck {
    $r = New-CheckResult 'publish' 'Publish smoke test'

    if (-not $script:Options.IncludePublish) {
        Set-CheckSkipped $r 'Only runs with -IncludePublish (it repeats the slow self-contained publish of release.yml).'
        return $r
    }

    $work = New-TempWorkDirectory
    try {
        $run = Invoke-Native -File 'dotnet' -Arguments @('publish', 'Starfield Load Order Keeper.csproj', '-c', 'Release', '-r', 'win-x64',
            '--self-contained', 'true', '-o', $work, '-p:PublishSingleFile=true', '-p:PublishReadyToRun=true', '--nologo', '-v', 'minimal')
        if ($run.ExitCode -ne 0) {
            Add-Finding $r Fail "dotnet publish failed (exit code $($run.ExitCode))." (Limit-List @($run.Output | Select-Object -Last 15) 15)
            return $r
        }

        $exe = Join-Path $work 'StarfieldLoadOrderKeeper.exe'
        if (-not (Test-Path -LiteralPath $exe)) { Add-Finding $r Fail 'The publish output has no StarfieldLoadOrderKeeper.exe.'; return $r }

        $expected = @(Get-RepoFile | Where-Object { $_ -match '^ViewTexts/Locales/[^/]+\.json$' } | ForEach-Object { [System.IO.Path]::GetFileName($_) })
        $localeDir = Join-Path $work 'ViewTexts/Locales'
        $present = @()
        if (Test-Path -LiteralPath $localeDir) { $present = @(Get-ChildItem -LiteralPath $localeDir -Filter *.json | ForEach-Object { $_.Name }) }
        $absent = @($expected | Where-Object { $present -notcontains $_ })
        if ($absent.Count -gt 0) { Add-Finding $r Fail 'Locale files are missing from the publish output.' $absent }

        Add-Finding $r Info ("Published executable: {0:N1} MB." -f ((Get-Item -LiteralPath $exe).Length / 1MB))
    }
    finally {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
    return $r
}
