# Periodic Change Checking System – Technical Overview

This document summarizes the intended implementation of the periodic change checking system so agents can extend the feature set without re-deriving its foundations.

## Objectives

- Detect external modifications to `Plugins.txt` at a configurable interval (default: 5 s).
- Surface differences between `Plugins.txt` and `Plugins.reference.txt` immediately to keep the DIFF window current.
- Maintain “signature tracking” so repeated notifications occur only for new changes.

## Core Responsibilities

| Area | Description |
| --- | --- |
| Scheduling | Uses a recurring timer (e.g., `DispatcherTimer` or `System.Threading.PeriodicTimer`) tied to the configured interval. Pauses when configuration is invalid or application is busy with load-order operations. |
| File signature tracking | Stores lightweight signatures (timestamp, size, or hash) for both `Plugins.txt` and the reference file. Prevents redundant diff calculations when files stay unchanged between ticks. |
| Change detection | When signatures differ, re-parses both files (filtered to enabled mods only) and produces a change model (moved/added/removed mods). |
| Notification | Raises observable state so the DIFF window can prompt the user with actionable options (apply fix or accept changes). |
| Error handling | If file reads fail, the timer suppresses notifications and reports status messages until valid paths/files return. |

## Key Components & Interactions

1. **Configuration Gate**
   - Checks `AppConfigModel.IsValid()` before every tick. Invalid config ⇒ timer still runs but short-circuits detection to avoid I/O noise.

2. **Watcher/Ticker Service (proposed)**
   - Encapsulates timer lifecycle (`Start`, `Stop`, `UpdateInterval`).
   - Exposes events or `IObservable<ChangeDetectionResult>` to consumers (e.g., DIFF view model).

3. **Signature Cache**
   - Maintains last-known `FileSignature` per monitored file.
   - Signature structure can contain `Path`, `LastWriteTimeUtc`, `Length`, and optional hash for extra safety.

4. **Diff Engine**
   - Reuses parsing logic (enabled-only mods, case-normalized filenames).
   - Produces a result object categorizing Added/Removed/Moved entries with their load-order positions.

5. **UI Integration**
   - Main view model (or dedicated DIFF view model) subscribes to change notifications.
   - Updates status message (“Changes detected in Plugins.txt”) and opens/refreshes DIFF window automatically when new differences appear.

## Typical Flow

1. **Timer Tick**
   - Validate configuration + reference file presence.
   - Gather current signatures for target and reference files.
   - Compare with cached signatures; exit early if identical.

2. **Change Analysis**
   - Read both files (UTF-8, enabled lines only, no disabled entries).
   - Generate diff summary, including recommendations for sorting fix if load order differs.

3. **State Update**
   - Cache new signatures.
   - Publish change result to UI layer for user action.
   - Reset notification flag after user accepts/reverts changes so next external modification triggers another alert.

## Extension Points

- **Dynamic Interval Adjustment:** Hook into settings changes to reconfigure the timer without restarting the app.
- **Manual Refresh:** Allow user-triggered “Check now” command that reuses the same detection pipeline as the timer.
- **Advanced Signatures:** Optionally calculate file hashes to guard against timestamp-only changes.
- **Telemetry/Logging:** Record change events for debugging or future analytics.

## Implementation Notes

- Ensure timer execution is marshaled onto the UI thread only when updating bound properties; heavy I/O stays on background threads.
- Guard against concurrent file operations (e.g., disable timer while `ApplyLoadOrderAsync` runs or lock around file access).
- Respect UTF-8 without BOM and whitespace rules already established for file parsing/writing.
- Keep disabled mods excluded throughout diff calculations to maintain consistency with existing logic.

# Periodic Change Checking Implementation Details

## Core Classes
- `MainViewModel`
  - Owns `_pluginsMonitorTimer` (`DispatcherTimer`), `_isCheckingPluginsFile`, `_lastObservedPluginsSignature`.
  - Signals UI state: `PluginsFileChangedExternally`, `SortingRecommendationActive`, `StatusMessage`, `IsBusy`.

- `FileService`
  - Provides file comparison and signature utilities accessed by the view model:
    - `ComparePluginsWithReferenceAsync(AppConfigModel config)`
    - `WouldSortingChangeDiffsAsync(AppConfigModel config)`
    - `CreateReferenceFileAsync`, `ApplyLoadOrderAsync`, `DiscardChangesAsync`

- `DiffService`
  - Supplies diff payloads for UI consumption:
    - `GetPluginsDiffAsync(AppConfigModel config)`

## Key Methods in `MainViewModel`
1. **Timer Lifecycle**
   - `MainViewModel()` constructor → creates `_pluginsMonitorTimer`, hooks `OnPluginsMonitorTick`.
   - `ConfigurePluginsMonitor()` → sets interval (`GetMonitorInterval()`), starts/stops timer based on `Config.IsValid()` and `RefExists`.

2. **Periodic Check**
   - `OnPluginsMonitorTick(object? sender, EventArgs e)`
     - Calls `CheckPluginsFileAsync()` and keeps all logic off UI thread except property updates.

3. **Change Detection**
   - `CheckPluginsFileAsync()`
     - Guards reentrancy (`_isCheckingPluginsFile`, `IsBusy`).
     - Verifies configuration/reference presence.
     - Invokes `FileService.ComparePluginsWithReferenceAsync` to get `ChangeDetectionResult` plus a `PluginsSignature`.
     - Compares signature to `_lastObservedPluginsSignature`; exits early when unchanged.
     - On diff:
       - Updates `PluginsFileChangedExternally`, `StatusMessage`.
       - Uses `FileService.WouldSortingChangeDiffsAsync` to set `SortingRecommendationActive`.
       - Calls `UpdateChangeCountDisplayAsync()` (which invokes `DiffService.GetPluginsDiffAsync`) to refresh “Manage changes (n)”.
       - Refreshes open diff dialog via `_activeDiffDialog?.RefreshDiffAsync()`.

4. **User Actions Feeding Back Into Monitoring**
   - `FixLoadOrderAsync()`, `CreateReferenceAsync()`, `DiscardChangesAsync()` all end with `await CheckPluginsFileAsync()` to resync signatures after manual operations.
   - `ShowDiffAsync()` uses `DiffService.GetPluginsDiffAsync` before presenting the DIFF window; it also clears `PluginsFileChangedExternally` when the user accepts/reverts changes.

5. **Status/Config Updates**
   - `LoadInitialStateAsync()` and `OpenSettings()` ensure `Config` and `RefExists` changes trigger `ConfigurePluginsMonitor()` so the timer reflects new settings.

## Data Contracts
- `ChangeDetectionResult` (returned by `ComparePluginsWithReferenceAsync`)
  - Lists added/removed/moved mods and carries the latest `PluginsSignature`.
- `PluginsSignature`
  - Contains at least last write time and file length for `Plugins.txt`; can be extended with hashes.

These components collectively provide the periodic timer, signature caching, diff computation, and UI notification loop described in the technical overview.