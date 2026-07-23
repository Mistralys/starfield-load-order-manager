param()

$ErrorActionPreference = 'Stop'

function Write-Menu {
    Clear-Host
    Write-Host 'Starfield Load Order Keeper' -ForegroundColor Cyan
    Write-Host ''
    Write-Host '[d] Build (Debug)' -ForegroundColor Green
    Write-Host '[r] Run (Debug)' -ForegroundColor Cyan
    Write-Host '[m] Merge Docs' -ForegroundColor Yellow
    Write-Host '[q] Quit' -ForegroundColor DarkGray
    Write-Host ''
    Write-Host 'Press a key to continue.'
}

function Invoke-BuildDebug {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $solutionPath = Join-Path $repoRoot 'Starfield Load Order Keeper.sln'

    Write-Host ''
    Write-Host 'Running Debug build...' -ForegroundColor Green
    & dotnet build $solutionPath --configuration Debug

    if ($LASTEXITCODE -ne 0) {
        throw 'Debug build failed.'
    }

    Write-Host ''
    Write-Host 'Build completed successfully.' -ForegroundColor Green
}

function Invoke-RunDebug {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $projectPath = Join-Path $repoRoot 'Starfield Load Order Keeper.csproj'

    Write-Host ''
    Write-Host 'Running Debug build...' -ForegroundColor Cyan
    & dotnet run --project $projectPath

    if ($LASTEXITCODE -ne 0) {
        throw 'Debug run failed.'
    }

    Write-Host ''
    Write-Host 'Run completed successfully.' -ForegroundColor Green
}

function Invoke-MergeDocs {
    $mergeDocsScript = Join-Path $PSScriptRoot 'MergeDocs.ps1'

    Write-Host ''
    Write-Host 'Merging docs...' -ForegroundColor Yellow
    & powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File $mergeDocsScript

    if ($LASTEXITCODE -ne 0) {
        throw 'Merge Docs failed.'
    }

    Write-Host ''
    Write-Host 'Docs merged successfully.' -ForegroundColor Green
}

while ($true) {
    Write-Menu

    $key = [System.Console]::ReadKey($true).KeyChar.ToString().ToLowerInvariant()

    try {
        switch ($key) {
            'd' {
                Invoke-BuildDebug
            }
            'r' {
                Invoke-RunDebug
            }
            'm' {
                Invoke-MergeDocs
            }
            'q' {
                return
            }
            default {
                Write-Host ''
                Write-Host "Unknown option: $key" -ForegroundColor Red
            }
        }
    }
    catch {
        Write-Host ''
        Write-Host $_.Exception.Message -ForegroundColor Red
    }

    Write-Host ''
    Write-Host 'Press any key to return to the menu.'
    [void][System.Console]::ReadKey($true)
}