# Tools

Utility scripts for the Starfield Load Order Keeper project.

---

## Merge-MarkdownDocs.ps1

Merges multiple markdown files from documentation folders into single-page documents.

### Usage

**Merge both documentation folders (default):**
```powershell
.\Merge-MarkdownDocs.ps1
```

Outputs:
- `Project-Manifest-Complete.md`
- `Application-Description-Complete.md`

**Merge a specific folder:**
```powershell
.\Merge-MarkdownDocs.ps1 -FolderPath "..\Docs\Agents\Project Manifest"
```

**Merge with custom output filename:**
```powershell
.\Merge-MarkdownDocs.ps1 -FolderPath "..\Docs\Agents\Project Manifest" -OutputPath "my-output.md"
```

### How it Works

1. Reads `README.md` first (if exists)
2. Processes remaining `.md` files alphabetically
3. Separates files with horizontal rules (`---`)
4. Outputs to `Tools/` folder
5. Preserves UTF-8 encoding and formatting

### Output Location

All merged files are generated in the `Tools/` folder by default.

---

## .gitignore

The following files are ignored by Git:
- `*-Complete.md` (generated merged documentation files)

These files can be regenerated anytime by running the merge script.
