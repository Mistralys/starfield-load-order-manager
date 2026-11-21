---
name: release-check
description: Gate a Starfield Load Order Keeper release. Runs Tools/ReleaseCheck.ps1 for every mechanical check, then does the review a script cannot - changelog accuracy and version bump size, translation quality of new strings, manifest and README accuracy, upgrade and compatibility risk, UI sanity - and ends with a go/no-go report. Use when asked to prepare, verify, sign off or "check" a release, a tag, or a changelog entry before publishing.
---

# Release check

You are the release gate. A script already decides everything that can be decided mechanically; your job is the rest. Never repeat what the script proves, and never skip the script.

**Read-only.** Do not commit, tag, push, publish, bump versions or edit the changelog unless the user explicitly asks. Propose exact text instead. The user performs the release.

## 1. Run the script first

```powershell
pwsh -NoProfile -File Tools\ReleaseCheck.ps1 -JsonPath "$env:TEMP\release-check.json"
```

`powershell -File ...` (Windows PowerShell 5.1) works too. Useful flags: `-AllowDirty` (uncommitted release prep is expected), `-Quick` (skip build, tests, advisories), `-Offline`, `-Only a,b`, `-Skip a,b`, `-IncludePublish` (slow self-contained publish as in `release.yml`), `-Fetch` (updates local refs; only with the user's consent), `-List`.

Read the console output and the JSON report. Exit code 0 means no blocking problem, 1 means at least one FAIL.

| Check id | What the script already proves | If it is not clean |
|----------|--------------------------------|--------------------|
| `version` | Top changelog heading parses, is not WIP, is exactly one step ahead of the last tag, has content, is not a duplicate; release notes preview as `release.yml` extracts them | Fix the changelog; judge bump size yourself (section 3A) |
| `git` | Clean tree, branch, upstream sync, commits since last tag, tag not already on origin | Report; do not fix |
| `unicode` | Every text file is valid UTF-8; no U+FFFD, mojibake, C1 controls or bidi overrides | Report file and line; re-saving as UTF-8 is a fix to propose |
| `hygiene` | No conflict markers, secrets or tracked build output; flags TODOs, debug leftovers, user paths, mixed line endings | Triage the warnings |
| `localization` | Locale JSON valid, no duplicate keys, registered in the csproj, every key used in code exists, plural-pair and `{0}` contract, access keys kept, left-over English | Key parity, empty values and placeholder parity are enforced by `LocalizationCompletenessTests` (run by `tests`) |
| `menus` | Every menu item has a Command, Click or submenu that resolves in code; no duplicate access keys per menu level and language | Wiring only - not whether the label matches the action |
| `hardcoded` | No literal user-visible text in XAML or `MessageBox.Show` | Move into text ViewModels and all locale files |
| `docs` | `file-tree.md` matches the files, `api-surface.md` names every public type, `tech-stack.md` names every package, relative links resolve | Presence only - not accuracy (section 3C) |
| `packaging` | csproj files exist, CI SDK matches the target framework, version fits the workflow pattern, installer packages the built exe, `UpgradeCode` unchanged | An MSI that does not ship the locales is a blocker until disproved |
| `build`, `tests`, `dependencies` | Release build, full xUnit run, advisories and deprecations | Interpret advisories (section 3D) |

A heuristic can be wrong. For a confirmed false positive, propose an entry for `Tools/ReleaseCheck/ReleaseCheck.allow.json` with a reason; never suppress silently:

```json
{ "rules": [ { "check": "hardcoded", "match": "ManageProfilesWindow\\.xaml:MenuItem\\.Header=Edit", "reason": "why this is acceptable" } ] }
```

The `match` regex is tested against the finding key: `<category>:<item text>` for list findings (for example `invalid-utf8:Tests/.../ProfileModelTests.cs`, `unused-key:DiffDialog.SomeKey`, `todo:Services/X.cs:12`), `file:Element.Attribute=text` for `hardcoded` (for example `Views/X.xaml:TextBlock.Text=(None found)`). The category prefixes are the last argument of the `Add-ListFinding` / `Add-Finding` calls in `Tools/ReleaseCheck/Checks/*.ps1`. Suppressions are counted and printed, so they stay visible.

## 2. Establish the release range

Release tags are bare versions (`1.9.0`); changelog headings carry a `v` (`## v1.9.0`).

```powershell
$last = git describe --tags --abbrev=0
git log "$last..HEAD" --no-merges --pretty=format:"%h %s"
git diff --stat "$last..HEAD"
```

Everything below is judged against this range. Consult the manifest (`Docs/Agents/project-manifest/`) before reading source, per `AGENTS.md`.

## 3. Review what a script cannot

Spend effort in proportion to what changed; skip areas the range does not touch and say so.

### A. Changelog accuracy and bump size
- Compare the top entry with the commits and the diff. Every user-visible change appears; nothing appears that is not in the range; internal work (refactors, tests, tooling, docs) does not appear.
- Style: `- Area: sentence.` bullets with areas consistent with existing entries (Changes Window, Main Window, Profiles, Translation, Debug, General), user language, no typos, a meaningful title, an accurate optional bold summary paragraph, issue links in the existing format.
- Bump size: removed or breaking behavior, `config.json` or profile format changes users must care about, or a large rework = major; new user-visible capability = minor; fixes only = patch. State whether the script's "confirm the bump size" note holds.
- Offer corrected text as a diff, not as an edit.

### B. Translation quality of new and changed strings
- List the changed keys: `git diff "$last..HEAD" -U0 -- ViewTexts/Locales/en-US.json`. For each, compare the other seven locales: meaning preserved, terminology consistent with existing translations of the same terms (mod, load order, reference, profile), placeholders used sensibly, plural pairs grammatical, length plausible for buttons and menus.
- zh-CN and ja-JP follow `localization.md`: full-width punctuation (zh), spacing between Latin and CJK text, corner brackets for UI names, never typographic double quotes, hotkey format as the existing entries use it.
- Proper nouns (SFSE, Steam, Starfield, Plugins.txt) stay in English.
- Report concerns as `locale / key / issue / suggestion`. Do not rewrite translations you cannot judge; flag them for a native reader.

### C. Documentation accuracy
- Using the table in `AGENTS.md` section 2, check that each kind of change in the range has its manifest update and that the update is correct: signatures in `api-surface.md` match the code, new events appear in `data-flows.md`, new invariants in `constraints.md`, UI conventions in `ui-design.md`.
- `README.md` claims (features, languages, requirements, screenshots) still hold, and agree with the changelog.

### D. Compatibility and risk in the diff
- **Persistence:** new, removed or renamed fields in `AppConfigModel`, profile files (`profile.json`, `main.txt`, `reference.txt`), reference history. Old data must still load; defaults must be safe.
- **Invariants from `constraints.md`:** `Plugins.txt` stays UTF-8 without BOM; profile switching guarantees; diff semantics; no coordinator state mutated from outside; the debug export never leaks user paths or localized text.
- **Threading and I/O:** async file work, UI-thread access, errors surfaced through `ConfirmationDialog` and `StatusCoordinator`.
- **Update check:** `UpdateCheckService` still understands the new tag format and version.
- **Installer and CI:** `UpgradeCode` and component GUIDs unchanged, `MajorUpgrade` behavior, locale files shipped, workflow changes. Treat the script's `msi-locales` finding as real until a clean MSI install shows translated text.
- **Advisories:** for each package the `dependencies` check lists, decide whether it is reachable from this app and whether it blocks the release.
- **New user-visible strings in C#** that the regex cannot see: status messages, exception text shown to users, `UserMessages` constants.

### E. UI sanity of changed XAML
- Follows `ui-design.md` and Material Design v5 patterns; secondary windows keep the `ConfigInvalidOverlay` behavior; keyboard access and tab order are sensible.
- Menu labels match what their commands do (the script checks wiring, not meaning); tooltips are accurate.
- If you cannot run the app, say so and write a short manual smoke test tailored to the diff: start the app, switch language, open each changed window, the happy path of each changed feature, a rollback or discard path.

### F. Test adequacy
- New behavior in the range has tests; sorting and diff changes are reflected in `Docs/Agents/Sorting Scenarios/` and `ScenarioTests`. Where a path is untested, add a test recommendation (write tests only when asked).

## 4. Report

End with exactly this structure, short and specific (file paths, keys, line numbers):

```
## Release check: v{version}  ({last tag} -> {version})

**Verdict:** GO | GO WITH CONDITIONS | NO-GO

### Blocking
- {script FAILs and review findings that must be fixed first}

### Should fix before release
- ...

### Notes
- {warnings accepted, areas not reviewed and why}

### Manual smoke test
- ...

### For you to run once the verdict is GO
git tag {version}
git push origin {version}
```

Verdict rules: any script FAIL or blocking finding = NO-GO. Only accepted warnings or non-blocking findings = GO WITH CONDITIONS, with the conditions listed. Otherwise GO. Never claim an area is clean that you did not review.
