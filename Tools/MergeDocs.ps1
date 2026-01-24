<#
.SYNOPSIS
    Merges multiple markdown files from a folder into a single consolidated document.

.DESCRIPTION
    This script reads all markdown files from a specified folder and combines them into
    a single markdown document. The README.md is always processed first, followed by
    other .md files in alphabetical order. Files are separated by horizontal rules.

.PARAMETER FolderPath
    The path to the folder containing markdown files to merge.
    If not specified, processes both documentation folders by default.

.PARAMETER OutputPath
    Optional. Custom output file path. If not specified, outputs to Tools folder
    with name based on folder name.

.EXAMPLE
    .\Merge-MarkdownDocs.ps1
    Processes both Project Manifest and Application Description folders

.EXAMPLE
    .\Merge-MarkdownDocs.ps1 -FolderPath "..\Docs\Agents\Project Manifest"
    Processes only the Project Manifest folder

.EXAMPLE
    .\Merge-MarkdownDocs.ps1 -FolderPath "..\Docs\Agents\Project Manifest" -OutputPath "custom-output.md"
    Processes Project Manifest with custom output filename
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$FolderPath,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath
)

function Merge-MarkdownFiles {
    param(
        [string]$SourceFolder,
        [string]$OutputFile
    )
    
    # Resolve full path
    $sourcePath = Resolve-Path -Path $SourceFolder -ErrorAction Stop
    Write-Host "Processing folder: $sourcePath" -ForegroundColor Cyan
    
    # Get all markdown files
    $allFiles = Get-ChildItem -Path $sourcePath -Filter "*.md" -File
    
    if ($allFiles.Count -eq 0) {
        Write-Warning "No markdown files found in $sourcePath"
        return
    }
    
    # Separate README and other files
    $readmeFile = $allFiles | Where-Object { $_.Name -eq "README.md" }
    $otherFiles = $allFiles | Where-Object { $_.Name -ne "README.md" } | Sort-Object Name
    
    # Build ordered file list
    $orderedFiles = @()
    if ($readmeFile) {
        $orderedFiles += $readmeFile
    }
    $orderedFiles += $otherFiles
    
    Write-Host "Found $($orderedFiles.Count) markdown file(s) to merge" -ForegroundColor Green
    
    # Create output content
    $output = @()
    
    foreach ($file in $orderedFiles) {
        Write-Host "  Adding: $($file.Name)" -ForegroundColor Gray
        
        # Read file content
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Add content
        $output += $content
        
        # Add separator between files (except for last file)
        if ($file -ne $orderedFiles[-1]) {
            $output += "`n`n---`n`n"
        }
    }
    
    # Write merged content
    $mergedContent = $output -join ""
    [System.IO.File]::WriteAllText($OutputFile, $mergedContent, [System.Text.Encoding]::UTF8)
    
    Write-Host "Created: $OutputFile" -ForegroundColor Green
    Write-Host "Size: $([math]::Round((Get-Item $OutputFile).Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host ""
}

# Main execution
try {
    # Get script directory
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptDir
    
    if ($FolderPath) {
        # Process single folder
        if (-not $OutputPath) {
            # Generate output filename from folder name
            $folderName = Split-Path -Leaf $FolderPath
            $outputName = ($folderName -replace '\s+', '-') + "-Complete.md"
            $OutputPath = Join-Path $scriptDir $outputName
        }
        
        Merge-MarkdownFiles -SourceFolder $FolderPath -OutputFile $OutputPath
    }
    else {
        # Process both default folders
        Write-Host "Merging documentation folders..." -ForegroundColor Cyan
        Write-Host ""
        
        # Project Manifest
        $projectManifestPath = Join-Path $repoRoot "Docs\Agents\Project Manifest"
        $projectManifestOutput = Join-Path $scriptDir "Project-Manifest-Complete.md"
        Merge-MarkdownFiles -SourceFolder $projectManifestPath -OutputFile $projectManifestOutput
        
        # Application Description
        $appDescPath = Join-Path $repoRoot "Docs\Agents\Application Description"
        $appDescOutput = Join-Path $scriptDir "Application-Description-Complete.md"
        Merge-MarkdownFiles -SourceFolder $appDescPath -OutputFile $appDescOutput

        # Sorting Scenarios
        $sortingScenariosPath = Join-Path $repoRoot "Docs\Agents\Sorting Scenarios"
        $sortingScenariosOutput = Join-Path $scriptDir "Sorting-Scenarios-Complete.md"
        Merge-MarkdownFiles -SourceFolder $sortingScenariosPath -OutputFile $sortingScenariosOutput
        
        Write-Host "All documentation merged successfully!" -ForegroundColor Green
    }
}
catch {
    Write-Error "Error: $_"
    exit 1
}
