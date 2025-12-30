# DIFF Detection System – Technical Overview

This document explains the current DIFF detection pipeline so agents can extend or reuse it when building new features.

## Objectives

- Compare `Plugins.txt` against `Plugins.reference.txt` whenever monitoring detects changes or a user explicitly opens the DIFF window.
- Categorize mods as added, removed, moved, or replaced (removed+added in same slot) using numbered load-order semantics.
- Provide structured diff data for UI surfaces (DIFF window, status badges, sorting recommendations).

## Core Responsibilities

| Area | Description |
| --- | --- |
| Data acquisition | `FileService.GetModDiffAsync(AppConfigModel)` reads both files, normalizes casing, filters to enabled mods, and builds `ModDiffModel` entries with reference/current line numbers. |
| Diff classification | `DiffService.GetPluginsDiffAsync(AppConfigModel)` transforms raw `ModDiffModel` data into user-friendly `DiffLineModel` records (Added/Removed/Moved/Replaced). |
| Replacement detection | Matches `removed` and `added` mods sharing the same line number to flag “replaced” entries, preventing duplicate notifications. |
| UI readiness | Returns immutable lists used by `MainViewModel` and DIFF dialogs to render change summaries, command availability, and contextual help. |
| Error handling | Validates configuration, ensures both files exist, and throws descriptive exceptions consumed by calling layers for user messaging. |

## Key Components & Interactions

1. **`FileService`**
   - `GetModDiffAsync(AppConfigModel config)` (async):  
     - Reads reference/current files (`UTF-8`, enabled lines only).  
     - Assigns 1-based load-order numbers to each mod per file.  
     - Builds `ModDiffModel` objects containing `FileName`, `ReferenceNumber`, `CurrentNumber`, with convenience flags (`IsNew`, `IsRemoved`, `IsMoved`).

2. **`DiffService`**
   - `GetPluginsDiffAsync(AppConfigModel config)` (async):  
     - Guards configuration paths and file presence.  
     - Invokes `FileService.GetModDiffAsync`.  
     - Calls `DetectReplacements` to pair removals with additions that occupy the same position.  
     - Projects each diff into a `DiffLineModel` describing the change textually, tagging with `DiffChangeType` (`Added`, `Removed`, `Moved`, `Replaced`).

3. **`DetectReplacements` helper**
   - Builds a dictionary of additions keyed by their current line number.  
   - Iterates removals and pairs them with additions sharing the same numbered slot, marking those additions as “matched” so they aren’t reported twice.  
   - Returns a map of removed→replacement plus the set of matched additions.

4. **View Model Consumption**
   - `MainViewModel.UpdateChangeCountDisplayAsync()` and `ShowDiffAsync()` call `DiffService.GetPluginsDiffAsync`.  
   - Results feed the DIFF window’s ListView, change counters, and tooltips.  
   - Combined with `FileService.WouldSortingChangeDiffsAsync` to show sorting recommendations when necessary.

## Typical Flow

1. **Trigger**  
   - Periodic monitor detects file signature change or user clicks “Manage changes”.

2. **Diff Retrieval**  
   - `DiffService.GetPluginsDiffAsync` validates config and reads both files via `FileService`.

3. **Classification**  
   - `ModDiffModel` instances are processed:  
     - Removed + matching added ⇒ `DiffChangeType.Replaced`.  
     - Removed only ⇒ `DiffChangeType.Removed`.  
     - Added only ⇒ `DiffChangeType.Added`.  
     - Different numbered positions ⇒ `DiffChangeType.Moved`.

4. **Delivery**  
   - Resulting `IReadOnlyList<DiffLineModel>` returned to caller for UI display and actionable context menus.

## Extension Points

- **Additional change types**: Introduce new `DiffChangeType` values (e.g., “Duplicate”) by extending `DiffService` classification logic.
- **Metadata enrichment**: Augment `ModDiffModel` with file hashes or timestamps for advanced diagnostics.
- **Selective diffing**: Allow callers to request diff scopes (e.g., only additions) to optimize specific UI workflows.
- **Localization**: Move change description strings to resource files to support translations.

## Implementation Notes

- Keep `DiffService` stateless so it remains easy to test and thread-safe.
- Ensure `FileService` continues honoring casing restoration rules when constructing `ModDiffModel` instances.
- All file I/O runs on background threads (`ConfigureAwait(false)`), while calling view models marshal results to the UI thread for property updates.
- When extending diff logic, update both `DiffLineModel` and DIFF window rendering to maintain consistent user messaging.