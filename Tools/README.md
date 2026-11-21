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

## ReleaseCheck.ps1

Mechanical release gate. It only reads the repository: it never commits, tags, pushes or edits files.
Runs on Windows PowerShell 5.1 and PowerShell 7. The judgment half of the gate (changelog wording,
translation quality, risk review, go/no-go report) is the agent skill in
[`.github/skills/release-check`](../.github/skills/release-check/SKILL.md), which runs this script first.

```powershell
.\Tools\ReleaseCheck.ps1                    # everything, including build, tests and advisories
.\Tools\ReleaseCheck.ps1 -Quick -AllowDirty # fast checks only; uncommitted release prep is fine
.\Tools\ReleaseCheck.ps1 -Only version,localization
.\Tools\ReleaseCheck.ps1 -JsonPath report.json
```

| Flag | Effect |
|------|--------|
| `-Only` / `-Skip` | Run only / skip the listed check ids (`-List` shows them) |
| `-Quick` | Skip the slow checks: `build`, `tests`, `dependencies`, `publish` |
| `-Offline` | No network: skips the advisory scan and the remote tag lookup |
| `-Fetch` | `git fetch --tags` before comparing with the upstream branch (off by default; it updates local refs) |
| `-AllowDirty` | Uncommitted changes are a warning instead of a failure |
| `-IncludePublish` | Also run the self-contained publish from `release.yml` and inspect the output |
| `-Strict` | Warnings also fail the run |
| `-JsonPath` | Write the full report as JSON |

Exit code: `0` no blocking problem, `1` at least one failing check (or a warning with `-Strict`), `2` usage error.
The developer menu (`menu.cmd`) offers quick and full runs.

### Checks

| Id | Verifies |
|----|----------|
| `version` | Top changelog heading is well-formed, not WIP, exactly one step ahead of the last tag, has content and is not duplicated; shows the release notes that `release.yml` will extract; csproj `<Version>` drift |
| `git` | Clean tree, branch, upstream sync, commits since the last tag, tag not yet on origin |
| `unicode` | Every text file is valid UTF-8 without U+FFFD, mojibake, C1 controls, bidi overrides or NUL |
| `hygiene` | No conflict markers, secrets or tracked build output; flags TODOs, debug leftovers, user paths, mixed line endings |
| `localization` | Locale JSON valid, no duplicate keys, registered in the csproj, keys used in code exist, plural pairs, access keys, left-over English |
| `menus` | Every menu item has a resolvable Command, Click handler or submenu; no duplicate access keys per level and language |
| `hardcoded` | No literal user-visible text in XAML or `MessageBox.Show` |
| `docs` | `file-tree.md`, `api-surface.md`, `tech-stack.md` and `localization.md` cover the code; relative links resolve |
| `packaging` | csproj files exist, CI SDK matches the target framework, version fits the workflow pattern, installer packages the built exe and the locales, `UpgradeCode` stable |
| `build` | Clean Release build of the solution, warnings listed |
| `tests` | Full xUnit run in Release; fails on failures, no discovered tests or skipped tests |
| `dependencies` | NuGet advisories (critical/high fail) and deprecations |
| `publish` | Opt-in publish smoke test |

Key parity, empty values and placeholder parity between locales are enforced by
`LocalizationCompletenessTests`, which the `tests` check runs; `localization` covers the rest.

### Accepted findings

Reviewed false positives and accepted exceptions go into `Tools/ReleaseCheck/ReleaseCheck.allow.json`,
each with a reason. A rule suppresses a finding whose key matches its `match` regex:

```json
{ "rules": [ { "check": "hardcoded", "match": "Views/X\\.xaml:TextBlock\\.Text=\\(None found\\)", "reason": "design-time placeholder" } ] }
```

Keys are `<category>:<item text>` for list findings (the category is the last argument of the
`Add-ListFinding` call in `Tools/ReleaseCheck/Checks/*.ps1`) and `file:Element.Attribute=text` for `hardcoded`.
Suppressed findings are counted in the output.

### Self-test

```powershell
pwsh -File .\Tools\ReleaseCheck\Self-Test.ps1
```

Runs the version, changelog, access-key and binding helpers and each check against throw-away fixtures
to prove that the checks fail when they should. Run it after changing anything under `Tools/ReleaseCheck/`.
Script files must stay ASCII-only: Windows PowerShell 5.1 reads BOM-less scripts as ANSI.

---

## .gitignore

The following files are ignored by Git:
- `*-Complete.md` (generated merged documentation files)

These files can be regenerated anytime by running the merge script.
