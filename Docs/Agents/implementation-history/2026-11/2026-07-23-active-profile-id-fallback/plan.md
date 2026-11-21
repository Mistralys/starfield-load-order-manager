# Plan

## Plan Audit Cycles
- Audits: none - Plan Auditor v1.7.0
- Architectural Reviews: none - Plan Architect Reviewer v2.2.0

## Prior Project Context
Repository strategy emphasizes short-term robustness in load-order change handling and long-term maintainability through explicit agent documentation. This fix aligns directly by removing a brittle profile-state edge case that produces false runtime failures in the diff flow. Recent projects also reinforced adding focused regression tests for cross-step state transitions and keeping manifest documentation synchronized with behavior changes.

## Summary
Stabilize profile-to-reference resolution so the View changes flow never fails when configuration contains a stale ActiveProfileId (for example, new-profile after that profile no longer exists). The plan introduces deterministic active-profile normalization early in startup and at profile refresh boundaries, ensuring runtime file operations and UI state share the same canonical profile identity (default fallback), then validates the behavior with regression tests.

## Architectural Context
The application uses MVVM with coordinator orchestration and static services. Reference file paths are currently derived from `AppConfigModel.GetReferenceFilePath()` in [Models/AppConfigModel.cs](Models/AppConfigModel.cs#L69), which uses `ActiveProfileId` directly. Diff loading throws when that path is missing in [Services/FileService.cs](Services/FileService.cs#L170). Startup sequencing is centralized in [Services/ViewModelInitializer.cs](Services/ViewModelInitializer.cs#L35), and active profile display state is refreshed in [Coordinators/ProfileCoordinator.cs](Coordinators/ProfileCoordinator.cs#L55), but this refresh currently does not normalize stale config identity. The error is surfaced by ViewModel command handling in [ViewModels/MainViewModel.cs](ViewModels/MainViewModel.cs#L573).

## Approach / Architecture
Add a canonicalization step that guarantees `AppConfigModel.ActiveProfileId` always points to an existing profile ID (or default) before reference-dependent operations execute. Implement this by:
1. Reordering and extending startup initialization so active profile resolution runs before reference existence checks and can self-heal stale IDs.
2. Updating profile refresh logic to synchronize configuration identity with resolved profile identity when fallback occurs.
3. Keeping fallback semantics centralized in existing profile coordination/service paths (no new architectural subsystem).
4. Adding regression tests that cover stale-ID startup and refresh scenarios, plus unchanged behavior for valid custom profiles.

## Rationale
The defect is a state divergence issue: UI can show default while file operations still target a stale profile ID. Normalizing the config identity at startup and refresh boundaries fixes the root cause without introducing a new abstraction layer. This keeps behavior predictable across all consumers that currently rely on `ActiveProfileId` path composition and avoids broad invasive changes to every file operation call site.

## Considered Alternatives
| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Where to resolve stale active profile IDs | Normalize in startup sequence + profile refresh path | 1) Add fallback file existence checks in every file operation; 2) Add fallback logic directly inside `AppConfigModel.GetReferenceFilePath()` | Central normalization keeps a single source of truth for profile identity and avoids scattering I/O-dependent fallback logic across model/path helpers or many services. |
| Whether to add new public service API | Reuse/extend existing coordinator-service methods | Introduce a new public `ResolveActiveProfileId*` API in `ProfileService` | Avoids expanding public API surface for a one-concern fix and minimizes manifest API churn. |
| Persistence policy for corrected ID | Persist corrected canonical ID through existing settings flow during initialization path | Keep correction in-memory only | Persisting prevents repeated stale-config recurrences across launches and keeps debug-state output aligned with runtime behavior. |

## Pattern Alignment
- Follows coordinator-driven state management by applying profile normalization in startup/refresh orchestration rather than in UI event handlers, consistent with [Coordinators/ProfileCoordinator.cs](Coordinators/ProfileCoordinator.cs).
- Follows existing service responsibility boundaries by keeping profile identity logic in profile-related components (`ProfileService`/`ProfileCoordinator`) rather than diff/file services.
- Deliberate non-departure: avoid adding a new abstraction/interface because the only consumer is current startup/profile flow; additional abstraction would be speculative.

## Detailed Steps
1. Add active-profile canonicalization in profile refresh/startup flow so a non-existent `ActiveProfileId` is replaced with `default` before reference-dependent checks run.
2. Reorder initialization in [Services/ViewModelInitializer.cs](Services/ViewModelInitializer.cs) to refresh/canonicalize active profile before reference existence/creation logic executes.
3. Ensure corrected `ActiveProfileId` is persisted through existing settings persistence path when canonicalization changes it.
4. Keep valid custom profile behavior unchanged: no overwrite when the configured profile exists.
5. Add regression tests for stale active profile ID recovery in coordinator/service initialization paths and for subsequent reference path correctness.
6. Update manifest documentation sections that define profile and initialization invariants to reflect canonicalization guarantees.

## Dependencies
- Existing profile folder/file scaffolding in `ProfileService.EnsureDefaultProfileFilesAsync`.
- Existing settings persistence API in `SettingsService`.
- Existing test infrastructure (`TestConfigContext`) for temporary valid Starfield layout setup.

## Required Components
- [Services/ViewModelInitializer.cs](Services/ViewModelInitializer.cs)
- [Coordinators/ProfileCoordinator.cs](Coordinators/ProfileCoordinator.cs)
- [Services/ProfileService.cs](Services/ProfileService.cs) (if needed for shared fallback helper behavior)
- [Tests/LoadOrderKeeper.Tests/Coordinators/ProfileCoordinatorTests.cs](Tests/LoadOrderKeeper.Tests/Coordinators/ProfileCoordinatorTests.cs)
- [Tests/LoadOrderKeeper.Tests/Services](Tests/LoadOrderKeeper.Tests/Services)
- [Docs/Agents/project-manifest/constraints.md](Docs/Agents/project-manifest/constraints.md)
- [Docs/Agents/project-manifest/data-flows.md](Docs/Agents/project-manifest/data-flows.md)

## Assumptions
- Valid configuration implies Profiles directory is writable and default profile files can be created.
- Stale IDs are recoverable by default fallback; no user prompt is required.
- Startup initialization remains the authoritative place to repair persisted configuration state.

## Constraints
- Preserve default profile invariants documented in the manifest.
- Preserve current user-facing error handling patterns (status + confirmation dialogs) for genuine file failures.
- Do not introduce breaking changes to existing public API signatures unless strictly required.

## Out of Scope
- Refactoring all file path helpers across the entire codebase to include independent fallback logic.
- Profile UX redesign or new profile-management commands.
- Fixing unrelated known test encoding issue KN-0003.

## Acceptance Criteria
- AC-01: When `ActiveProfileId` references a missing profile folder, startup canonicalizes active profile identity to `default` before diff/reference checks.
- AC-02: Clicking View changes with a stale persisted active profile ID no longer surfaces "Reference file not found" when default reference exists.
- AC-03: When `ActiveProfileId` already points to an existing custom profile, initialization preserves it without fallback.
- AC-04: Corrected fallback identity is persisted so subsequent launches do not retain stale profile IDs.
- AC-05: Regression tests cover stale-ID recovery and valid-profile non-regression paths.

## Testing Strategy
Use targeted unit/integration-style tests around startup initialization and profile refresh behavior with temporary profile directories, validating both configuration state and resulting reference-path behavior. Keep assertions locale-neutral and deterministic.

## Test Plan
- [Tests/LoadOrderKeeper.Tests/Coordinators/ProfileCoordinatorTests.cs](Tests/LoadOrderKeeper.Tests/Coordinators/ProfileCoordinatorTests.cs) - add test: stale `ActiveProfileId` refresh resolves to default and synchronizes config identity - covers AC-01, AC-05.
- [Tests/LoadOrderKeeper.Tests/Services/ViewModelInitializerTests.cs](Tests/LoadOrderKeeper.Tests/Services/ViewModelInitializerTests.cs) - add test: initializer canonicalizes stale active profile before reference checks and persists corrected ID - covers AC-01, AC-04, AC-05.
- [Tests/LoadOrderKeeper.Tests/Services/ViewModelInitializerTests.cs](Tests/LoadOrderKeeper.Tests/Services/ViewModelInitializerTests.cs) - add test: existing valid custom active profile remains unchanged after initialization - covers AC-03, AC-05.
- [Tests/LoadOrderKeeper.Tests/Services/FileServiceTests.cs](Tests/LoadOrderKeeper.Tests/Services/FileServiceTests.cs) - add regression scenario ensuring diff path resolution succeeds post-canonicalization and does not throw reference-missing for stale-ID startup case - covers AC-02, AC-05.

## Documentation Updates
- [Docs/Agents/project-manifest/constraints.md](Docs/Agents/project-manifest/constraints.md) - document canonicalization invariant: stale persisted `ActiveProfileId` must auto-fallback to `default` during initialization/refresh.
- [Docs/Agents/project-manifest/data-flows.md](Docs/Agents/project-manifest/data-flows.md) - update startup flow sequence to show profile canonicalization before reference checks.

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Canonicalization timing still occurs after a reference check in one path | Explicitly reorder initializer steps and add tests that assert ordering-sensitive outcomes. |
| Persisting corrected profile ID introduces unexpected settings-write side effects in tests | Isolate persistence assertions with temporary config contexts and keep test setup deterministic. |
| Hidden callers rely on stale ID behavior | Add non-regression tests for valid custom profiles and preserve existing API signatures/usage patterns. |

## Recommended Workflow
- Workflow: standalone
- Rationale: This is a bounded bug fix within existing profile/startup patterns affecting a small set of modules and tests, suitable for a single developer session with focused validation.
