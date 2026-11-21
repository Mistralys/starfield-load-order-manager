# Synthesis Report — `2026-07-23-load-order-change-guidance-rework-1`

_Generated: 2026-07-23 | Project Status: COMPLETE_

---

## Executive Summary

This rework plan hardened the load order change guidance feature delivered in the prior cycle. Five targeted improvements were delivered across testing, XAML resources, localization, ViewModel architecture, and locale hygiene — all within a single working session. Every work package passed all four pipeline stages (implementation → QA → code-review → documentation) on the first revision with zero blocking rework cycles.

**What was built:**

| WP | Title | Scope |
|----|-------|-------|
| WP-001 | Extract `BuildConfirmationMessage()` + unskip tests | Internal method extraction, 2 new unit tests, dead test cleanup |
| WP-002 | Consolidate foreground hex literals → named brush resources | 7 new `SolidColorBrush` resources, 9 `DynamicResource` references |
| WP-003 | Add Moved guidance sidebar card | Full card: localization (8 locales), ViewModel properties, XAML, 5 new unit tests |
| WP-004 | Remove dead `DependentCause_Added` locale key | Pure data deletion from 8 locale JSON files |
| WP-005 | Simplify `UpdateDiffState()` notification mechanism | Replace 24 `OnPropertyChanged(nameof(...))` calls with 1 `OnPropertyChanged(string.Empty)` |

---

## Metrics

### Test Suite

| WP | Tests at Start | New Tests Added | Tests at End | Failed | Skipped |
|----|---------------|-----------------|--------------|--------|---------|
| WP-001 | 492 + 2 skipped | +2 (replacing 2 skipped) | 494 | 0 | **0** (was 2) |
| WP-002 | 494 | 0 | 494 | 0 | 0 |
| WP-003 | 494 | +5 | 499 | 0 | 0 |
| WP-004 | 499 | 0 | 499 | 0 | 0 |
| WP-005 | 499 | 0 | 499 | 0 | 0 |

**Final suite: 499 tests — 499 passed, 0 failed, 0 skipped.**

Net new tests added this cycle: **7** (2 for `BuildConfirmationMessage`, 5 for Moved guidance).

### Build Health

All WPs delivered clean builds — **0 errors, 0 warnings** throughout.

### Pipeline Health

| Metric | Value |
|--------|-------|
| Total WPs | 5 |
| WPs with all stages passing | 5 (100%) |
| WPs requiring rework | 0 |
| Blocking issues | 0 |
| Fix-forwards applied by Reviewer | 1 (WP-001 Dispose() deduplication — non-behavioral) |
| Documentation-forwards addressed | 5 |
| Auto-cancelled pipelines | 1 (WP-004 implementation — MCP tooling error, recovery successful) |

---

## Strategic Recommendations (Gold Nuggets)

### 1. `OnPropertyChanged(string.Empty)` as the Standard Bulk-Refresh Pattern
**Source: WP-005 / Developer, Reviewer, QA**

Replacing the 24-call `OnPropertyChanged(nameof(...))` list in `UpdateDiffState()` with a single `OnPropertyChanged(string.Empty)` is the CommunityToolkit.Mvvm-documented convention for broadcasting a full property refresh. The result is a clean 3-line method that self-maintains — new computed properties are automatically covered without any developer action. This pattern should be adopted for any future ViewModel with a similar bulk-refresh trigger. The trade-off (harmless double-notification on `HasDifferences` from the `SetProperty` call) is an accepted and documented characteristic of the pattern.

### 2. Double-Gate Visibility Pattern is Defensively Superior
**Source: WP-003 / Reviewer**

The `ShowMovedGuidance => HasDifferences && MovedModCount > 0` compound guard is subtly better than a single `HasMovedMods` boolean. `HasDifferences` acts as a sentinel that prevents stale card visibility if the diff collection is independently cleared between notifications. All sidebar card `Show*` properties use this pattern consistently — it should be preserved as the canonical approach for any future cards or optional panels in the diff view.

### 3. `internal` Extract-Method > Interface Injection for Pure ViewModel Logic
**Source: WP-001 / Developer, Reviewer**

Extracting `BuildConfirmationMessage()` as an `internal string?`-returning method, tested via `InternalsVisibleTo`, is the correct tradeoff for a pure function of ViewModel state. The `string?` null-return contract (null = safe, non-null = message to show) is load-bearing for the confirmation dialog branch. This pattern — extract to `internal`, expose via `InternalsVisibleTo`, document the null contract with `<returns>` XML doc — is the established test-seam approach for this project and should be the default for future testability improvements.

### 4. Naming Asymmetry Deserves a Comment, Not Normalisation
**Source: WP-002 / Reviewer, Documentation**

The `DiffBrushes.xaml` foreground brush section has an intentional naming asymmetry: some sidebar cards reuse the plain `DiffBrush.{ChangeType}Foreground` brush, while others require a `DiffBrush.Guidance*Foreground` key because their sidebar icon colour differs from the diff-list icon colour. The documentation agent added an inline XML comment block explicitly warning contributors not to "fix" this asymmetry. This is the right practice for any non-obvious but intentional design decision in resource dictionaries — document it at the point of definition, not in a separate wiki.

### 5. Test Classes Should Be Described by Scope, Not by the WP That Wrote Them
**Source: WP-003 / Reviewer, Documentation**

The Reviewer flagged that `DiffDialogViewModelTests.cs` carried WP-number references in its class-level XML doc. The Documentation agent updated these to scope-based descriptions ("covering the guidance-sidebar computed properties..."). Going forward, test class comments should describe the _surface under test_, not the sprint/WP that created the tests. WP references age poorly and create confusion for contributors reading cold.

---

## Files Modified This Cycle

| File | WPs |
|------|-----|
| `ViewModels/DiffDialogViewModel.cs` | WP-001, WP-003, WP-005 |
| `Tests/LoadOrderKeeper.Tests/ViewModels/DiffDialogViewModelTests.cs` | WP-001, WP-003, WP-005 |
| `Styles/DiffBrushes.xaml` | WP-002, WP-003 |
| `Views/DiffWindow.xaml` | WP-002, WP-003 |
| `ViewTexts/DiffDialogTexts.cs` | WP-003 |
| `ViewTexts/Locales/en-US.json` | WP-003, WP-004 |
| `ViewTexts/Locales/de-DE.json` | WP-003, WP-004 |
| `ViewTexts/Locales/fr-FR.json` | WP-003, WP-004 |
| `ViewTexts/Locales/es-ES.json` | WP-003, WP-004 |
| `ViewTexts/Locales/it-IT.json` | WP-003, WP-004 |
| `ViewTexts/Locales/zh-CN.json` | WP-003, WP-004 |
| `ViewTexts/Locales/ja-JP.json` | WP-003, WP-004 |
| `ViewTexts/Locales/pt-BR.json` | WP-003, WP-004 |
| `Docs/Agents/project-manifest/api-surface.md` | WP-001 |

---

## Deferred & Follow-Up Items

No items were explicitly deferred from this plan. The following items were raised during the cycle as out-of-scope observations or noted for future consideration.

### Out-of-Scope Observations (Flagged for Future Cycles)

| # | Source | Agent | Description | Priority | Rationale |
|---|--------|-------|-------------|----------|-----------|
| 1 | WP-001 / QA | QA | `BuildConfirmationMessage()` iterates `DiffLines` twice (two `Count()` calls). A single-pass approach would reduce allocations. | Low | Negligible at current scale. Not a defect. Flag if performance profiling ever targets this path. |
| 2 | WP-002 / Developer | Developer | As `DiffBrushes.xaml` grows (now 18 entries), consider whether splitting into sub-dictionaries by concern (`DiffRowBrushes.xaml`, `DiffSidebarBrushes.xaml`, `DiffForegroundBrushes.xaml`) would aid navigation. | Low | Current single-file approach is adequate at this scale. Revisit if the file exceeds ~30–40 entries. |
| 3 | WP-003 / Developer | Developer | `DiffDialogTexts.cs` `OnCultureChanged` has grown to 69 explicit `OnPropertyChanged(nameof(...))` calls. WP-005 applied the `string.Empty` pattern to `UpdateDiffState()`, but the same improvement could be applied to `OnCultureChanged`. | Low | `OnCultureChanged` is a lower-frequency call path (only fires on culture switch). Apply in a future maintenance cycle if the list continues to grow. |
| 4 | WP-005 / QA, Reviewer | QA, Reviewer | `HasDifferences` receives two `PropertyChanged` notifications per `UpdateDiffState()` call — one from `SetProperty` and one from the `string.Empty` bulk broadcast. Harmless (WPF bindings are idempotent), but if micro-optimization is ever needed, the backing fields could be set directly before calling `OnPropertyChanged(string.Empty)` once. | Low | Documented CommunityToolkit.Mvvm trade-off. Not actionable unless performance profiling flags this path. |
| 5 | WP-001 / Reviewer | Reviewer | `BuildConfirmationMessage()` test asserts substring presence only (e.g., `'2 removed mod(s)'`), not the full formatted template from `ConfirmUpdateMessage_RiskyChanges`. | Low | Appropriately loose — avoids coupling tests to the full string. Locale completeness tests already guard all locale strings independently. |

---

## Next Steps for the Planner

The rework cycle is complete. All 5 items targeted from the prior synthesis have been delivered. The following areas warrant attention in a future cycle:

1. **`DiffDialogTexts.OnCultureChanged` notification simplification** — Apply the same `OnPropertyChanged(string.Empty)` pattern used in WP-005 to eliminate the 69-call explicit list. Low risk, straightforward change.

2. **Source-gen migration (`[ObservableProperty]`)** — The plan explicitly noted this as a larger refactor out of scope here. If the ViewModel continues to grow in computed properties, a dedicated migration plan from `ObservableObject` + manual properties to CommunityToolkit.Mvvm source generation would significantly reduce boilerplate. This was listed as a considered alternative to WP-005's approach.

3. **`DiffBrushes.xaml` dictionary split** — Low urgency. Re-evaluate at ~30–40 entries or when a new contributor reports difficulty navigating the file.

4. **Tooltip/confirmation dialog coverage gaps** (carried from prior synthesis) — The prior plan identified tooltip content and the confirmation dialog's visual presentation as out-of-scope. If user feedback indicates confusion around these elements, they remain the next natural feature area.

5. **Moved change-type edge cases** — The Moved card is now functional. If real-world usage surfaces edge cases (e.g., a mod that is simultaneously Moved and Replaced in a single diff), verify the card and ViewModel properties handle this gracefully.
