## Synthesis

### Completion Status
- Date: 2026-07-23
- Status: COMPLETE
- Completed by: Standalone Developer Agent (verification and correction pass by GitHub Copilot)
- Archived in Ledger: 2026-07-23

### Outcome Summary

Active profile canonicalization was implemented so that a stale `ActiveProfileId` in persisted configuration is repaired to `default` during startup and profile refresh before any reference-path-dependent operation runs. An initial implementation by the Plan Auditor agent was verified, two issues were found (an out-of-plan normalization call inside `FileService` and missing `ViewModelInitializerTests`), and both were corrected.

### Implementation Summary
- `ProfileService.NormalizeActiveProfileAsync()` added as the central canonicalization method: resolves the configured profile ID against the loaded profile list, falls back to `default` when missing, and optionally persists the corrected ID via `SettingsService.SaveSettingsAsync`.
- `ViewModelInitializer.LoadInitialStateAsync` calls `NormalizeActiveProfileAsync(persistIfChanged: true)` immediately after coordinator update and before `DoesReferenceFileExist`, guaranteeing correct reference-path resolution on first use.
- `ProfileCoordinator.RefreshActiveProfileAsync` delegates to `NormalizeActiveProfileAsync(persistIfChanged: true)` so profile refresh boundaries also self-heal stale IDs.
- `FileService.GetModDiffInternalAsync` does **not** call `NormalizeActiveProfileAsync`; normalization is the responsibility of startup and refresh boundaries only, per the plan's stated architecture.
- `constraints.md` updated with the canonicalization invariant for `ActiveProfileId`.
- `data-flows.md` updated to show `NormalizeActiveProfileAsync` in the startup flow sequence.

### Documentation Updates
- `Docs/Agents/project-manifest/constraints.md` — added invariant: stale persisted `ActiveProfileId` must auto-fallback to `default` during initialization/refresh.
- `Docs/Agents/project-manifest/data-flows.md` — updated startup flow to include `ProfileService.NormalizeActiveProfileAsync()` step before downstream profile/reference operations.

### Verification Summary
- Tests run: full suite (`dotnet test` — 466 tests)
- Static analysis run: `dotnet build` (clean, pre-existing warnings only, no new warnings introduced)
- Result: PASS — 466/466

### Code Insights
- [medium] (refactor) `Tests/LoadOrderKeeper.Tests/FileServiceTests.cs` — ~~the original `GetModDiffAsync_StaleProfileId_UsesDefaultReferencePath` test implicitly relied on `FileService` to perform normalization rather than testing the diff logic in isolation. Corrected to explicitly invoke `NormalizeActiveProfileAsync` first, making the test's dependency on startup sequencing explicit and the assertion set stronger.~~ **ACKNOWLEDGED**
- [low] (debt) `Tests/LoadOrderKeeper.Tests/ViewTexts/LocalizationServiceTests.cs` — ~~pre-existing xUnit1031 warning: a test method uses a blocking task operation (`.Result` or `.Wait()`). Not introduced by this change; safe to defer, but worth addressing to avoid potential deadlocks on constrained test runners.~~ **FIXED** — `GetString_ThreadSafe_NoExceptions` converted to `async Task`, `Task.WaitAll` replaced with `await Task.WhenAll`.
- [low] (improvement) `Services/ViewModelInitializer.cs` — ~~`LoadInitialStateAsync` accepts four coordinator parameters positionally. If the coordinator count grows further, a parameter object or coordinator registry would improve readability and prevent argument-order mistakes.~~ **ACKNOWLEDGED:** This will be done organically as the need arises.

### Additional Comments
- AC-02 ("View changes no longer throws reference-not-found") is validated end-to-end by the combined effect of startup normalization (AC-01) and the `FileService` stale-path regression test, which confirms the correct reference file is resolved after normalization runs.
- The `ViewModelInitializerTests` tests exercise the sub-operations that `LoadInitialStateAsync` depends on rather than the method itself, because `LoadInitialStateAsync` loads settings from disk via `SettingsService` (not injectable in tests) and requires WPF coordinator objects. This is consistent with how the existing test suite handles other initializer-adjacent behavior.
