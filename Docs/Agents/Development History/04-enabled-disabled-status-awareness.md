# Enabled/Disabled Status Awareness

Until now, the character `*` at the beginning of each line in the `Plugin.txt` was implied. 
However, Starfield uses this to toggle the enabled/disabled status of mods. Therefore, the 
application must now be aware of this status and handle it appropriately.

Important consideration: **A disabled mod is functionally equivalent to a missing mod**. 
Thus, when a mod is disabled, it should be treated as if it is not present in the load order.

## Read and handle the disabled Status

When reading the `Plugins.txt` and the reference file, the application must now also check
for the presence of `*` at the beginning of each line. If this character is missing, the mod 
is considered disabled, and can be skipped entirely in further processing. It must be as if
the line does not exist.

# Agent Implementation Plan – Enabled/Disabled Status Awareness

## 1. Understand Requirements
- Mods now store enabled state via leading `*` in `Plugins.txt`.
- Missing `*` ⇒ mod disabled ⇒ treat as absent everywhere.
- Disabled mods must not affect load order, diffing, or reporting—they’re equivalent to missing lines.

## 2. Update Data Model
1. Adjust `ModEntryModel`:
   - Add `bool IsEnabled` (read-only) derived from parsed line.
   - Ensure constructor strips `*` but retains the original filename for equality.
   - Keep equality/hash based on the normalized filename only.

## 3. File Parsing Changes
1. Extend `FileService.ReadFileAsync`:
   - When iterating lines, detect leading `*`.
   - Set `IsEnabled`.
   - Skip creating entries when `IsEnabled == false`.
   - Preserve existing numbering logic (line numbers should reflect enabled-entry positions only).
2. Ensure case restoration, diffing, and load-order logic use the filtered enabled list.

## 4. Reference Handling
- `CreateReferenceFileAsync` still copies raw file unchanged (disabled lines stay in file).
- Reference comparison logic must ignore disabled entries just like current file reading.

## 5. Load Order Application
1. `ApplyLoadOrderAsync`:
   - `referenceMods` and `currentMods` already filtered to enabled entries.
   - Final list should contain only enabled mods (all prefixed with `*`).
   - When writing output:
     - Prepend `*` to every line.
     - Maintain UTF-8 encoding.
2. Disabled mods remain in file untouched? Requirement says disabled treated as missing; output should not include them. Confirm: final write should include all enabled lines only.

## 6. Unit/Integration Tests (if applicable)
- Add/extend tests:
  - Parsing lines with/without `*`.
  - Ensuring disabled lines don’t appear in results.
  - Reordering logic ignores disabled entries.
- If no test suite yet, capture manual test steps.

## 7. Documentation
- Update relevant docs (maybe `Docs/Agents/04-enabled-disabled-status-awareness.md`) to reflect actual implementation status.
- Mention that disabled mods get ignored during processing and output.

## 8. UI/Status Messaging
- If UI displays counts/status, ensure messaging clarifies disabled mods are ignored (optional if no UI change needed).

## 9. Validation
- Manual validation steps:
  1. Create sample `Plugins.txt` with mix of `*mod.esm` and `mod.esm`.
  2. Run app:
     - Disabled mods should not appear in load order operations.
     - Reference creation unaffected.
     - Output `Plugins.txt` contains only enabled mods prefixed with `*`.
  3. Confirm case restoration still applies.

## 10. Rollout Notes
- No schema changes to config.
- Ensure backward compatibility: files without disabled lines behave exactly as before.
