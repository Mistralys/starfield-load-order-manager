# Synthesis Report — Load Order Change Guidance
**Plan:** `2026-07-23-load-order-change-guidance`
**Date:** 2026-07-23
**Status:** COMPLETE — All 8 work packages delivered

---

## Executive Summary

This plan delivered a three-layer contextual guidance system integrated into the existing `DiffWindow` — the dialog that surfaces load order differences before the user commits to "Accept Changes." The feature set comprises:

1. **Per-item tooltips** on every change-type icon in the diff list (Added, Removed, Replaced, Moved, and the pre-existing Inserted), giving users in-context explanations on hover.
2. **A recommendations sidebar panel** (fixed 260 px, right of diff list) displaying color-coded `materialDesign:Card` elements — one per detected change type — with risk ratings, mod counts, and actionable guidance text. Cards appear and disappear dynamically as the diff content changes.
3. **A risk-gated confirmation dialog** on "Accept Changes": when removed or inserted mods are detected, the standard confirmation is replaced with a richer, singularly/plurally correct message that names the risks specific to that diff.

All user-facing strings are fully localized across all 8 supported locales (en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR) and all tooltip properties are registered in `OnCultureChanged()` for live locale switching.

---

## Metrics

| Work Package | Stages | Tests | Result |
|---|---|---|---|
| WP-001 — Locale Keys (en-US) | impl → qa → review → docs | 5/5 AC | PASS |
| WP-002 — ViewTexts + Brushes | impl → qa → review → docs | 464/466 tests¹ | PASS |
| WP-003 — Locale Translations (7 locales) | impl → qa → review → docs | 466/466 tests | PASS |
| WP-004 — ViewModel Properties | impl → qa → review (FAIL→rework) → review → docs | 492/492 tests² | PASS |
| WP-005 — Risk-Gated Confirmation Dialog | impl → qa → review → docs | 492/492 tests² | PASS |
| WP-006 — Per-Item Tooltips (XAML) | impl → qa → review → docs | 492/492 tests² | PASS |
| WP-007 — Sidebar XAML Layout | impl → qa → review → docs | 492/492 tests² | PASS |
| WP-008 — Documentation Consolidation | docs only | n/a | PASS |

¹ 2 expected failures (locale completeness tests before WP-003 ran); resolved to 466/466 by WP-003.
² 2 intentionally skipped confirmation-dialog tests throughout; not failures — documented limitation.

**Overall pipeline health:** 8/8 WPs with all active stages passing. 1 rework cycle (WP-004 code-review FAIL → PASS) — resolved a `Dispose()` null-guard defect in the ViewModel test-seam constructor.

### Final Test Suite State
- **492 passing** / **0 failing** / **2 intentionally skipped** (confirmation dialog tests — require MainViewModel in production constructor; test-seam barrier documented in `DiffDialogViewModelTests.cs`)

---

## Files Modified

| File | WPs | Change |
|---|---|---|
| `ViewTexts/Locales/en-US.json` | WP-001 | +28 new DiffDialog locale keys |
| `ViewTexts/Locales/de-DE.json` | WP-003 | +28 translated keys |
| `ViewTexts/Locales/fr-FR.json` | WP-003 | +28 translated keys; fixed pre-existing typo (`différance` → `différence`) |
| `ViewTexts/Locales/es-ES.json` | WP-003 | +28 translated keys |
| `ViewTexts/Locales/it-IT.json` | WP-003 | +28 translated keys; fixed pre-existing typo (`carreggiamento` → `caricamento`, 4 keys) |
| `ViewTexts/Locales/zh-CN.json` | WP-003 | +28 translated keys (uses `「」` brackets — see Deferred Items) |
| `ViewTexts/Locales/ja-JP.json` | WP-003 | +28 translated keys |
| `ViewTexts/Locales/pt-BR.json` | WP-003 | +28 translated keys |
| `ViewTexts/DiffDialogTexts.cs` | WP-002 | +28 new read-only properties; expanded class XML doc |
| `Styles/DiffBrushes.xaml` | WP-002 | +5 `DiffBrush.Guidance*` brush resources |
| `ViewModels/DiffDialogViewModel.cs` | WP-004, WP-005 | +23 computed/pass-through properties; null-guard fix on `Dispose()`; risk-gated message logic in `UpdateReferenceWithConfirmationAsync()` |
| `Tests/.../DiffDialogViewModelTests.cs` | WP-004 | +26 unit tests covering all new ViewModel properties (new file) |
| `Views/DiffWindow.xaml` | WP-006, WP-007 | +4 tooltip `Setter` elements; full sidebar XAML (two-column Grid, 5 cards) |
| `Docs/Agents/project-manifest/localization.md` | WP-001, WP-003 | Plural forms section; tooling constraint for `「」` vs `U+201C/201D`; stale count fixes |
| `Docs/Agents/project-manifest/ui-design.md` | WP-002, WP-004, WP-007 | Brush table; Diff Window Layout section; sidebar column rationale |
| `Docs/Agents/project-manifest/api-surface.md` | WP-004, WP-008 | DiffDialogViewModel full property listing; DiffDialogTexts class entry |
| `Docs/Agents/project-manifest/constraints.md` | WP-004, WP-008 | Test-seam constructor pattern; sidebar informational-only constraint |
| `Docs/Agents/project-manifest/data-flows.md` | WP-005, WP-008 | Risk-gated confirmation flow; `UpdateDiffState()` sidebar property enumeration |
| `Docs/Agents/project-manifest/file-tree.md` | WP-004 | Added `DiffDialogViewModelTests.cs` entry |
| `changelog.md` | WP-006 | New v2.0.0 entry covering all 5 guidance features |
| `README.md` | WP-006 | Updated diff feature bullet to mention contextual risk guidance |

---

## Strategic Recommendations (Gold Nuggets)

### 1. Three-Layer Guidance Architecture Is Effective — Extend It
The "passive sidebar → hover tooltip → commit confirmation" pattern is well-suited to the domain. Users encountering risky changes at browse time (sidebar), investigation time (tooltip), and commit time (dialog) get defense-in-depth guidance. This pattern should be preserved and extended to other risk-bearing operations in the application.

### 2. Formalize the ViewModel Test-Seam Constructor Pattern
WP-004 introduced `internal + [EditorBrowsable(Never)] + null! + InternalsVisibleTo` as a test-seam mechanism for ViewModels with heavy coordinator dependencies. This pattern was documented in `constraints.md`. It is a reusable convention that should be applied proactively to any new ViewModel that wires coordinator subscriptions in its production constructor — adding the seam early avoids rework later.

### 3. `UpdateDiffState()` Notification Growth — Plan for Bulk Refresh
`DiffDialogViewModel.UpdateDiffState()` now emits 16+ individual `OnPropertyChanged` calls. The Reviewer noted that `OnPropertyChanged(string.Empty)` — a single call that invalidates all properties — is the simplest zero-dependency option for a bulk culture/state refresh. A `CommunityToolkit.Mvvm` `[ObservableProperty]` source-generator migration is the longer-term architectural win. Both options should be considered before the next major ViewModel expansion.

### 4. Centralize Risk-Color Foreground Brushes in `DiffBrushes.xaml`
Icon foreground hex literals (`#EF5350`, `#FF9800`, `#9C27B0`, `#66BB6A`) are duplicated across the sidebar card headers and the diff-list `DataTrigger` blocks in `DiffWindow.xaml`. If risk-color semantics change, a file-wide find-and-replace is needed. Extracting these to named `SolidColorBrush` resources (e.g., `DiffBrush.RemovedForeground`) in `DiffBrushes.xaml` would centralize ownership.

### 5. Tooling Constraint: No Typographic Quotes in JSON via `edit_file`
The `edit_file` tool silently transcribes Unicode typographic quotation marks (`U+201C`/`U+201D`) as ASCII 34, breaking JSON. This was documented in `localization.md` with `「」` (U+300C/U+300D) as the approved JSON-safe alternative for zh-CN and ja-JP. **This constraint applies to any future locale file edits** — all agents working on translations must be aware of it.

---

## Deferred & Follow-Up Items

### Deferred (Intentionally Postponed)

| # | Source | Agent | Description | Priority |
|---|---|---|---|---|
| D-1 | WP-004 | Developer/Reviewer | **2 skipped confirmation-dialog unit tests** (`ConfirmationMessage_RiskyChanges_IncludesRemovedCount`, `ConfirmationMessage_SafeChanges_SkipsDialog`). Blocked by test-seam constructor no-op stub for `UpdateReferenceCommand`. Unblocked by injecting the message-building logic or mocking via interface. | Medium |
| D-2 | WP-004 | Reviewer | **`UpdateDiffState()` notification list** is growing (16+ calls). Defer to a `CommunityToolkit.Mvvm` migration or `OnPropertyChanged(string.Empty)` refactor. | Low |
| D-3 | WP-007 | Reviewer | **260 px sidebar column** is a magic number (no named resource). If the sidebar layout is adopted in a second view, extract to a shared resource to avoid drift. | Low |
| D-4 | WP-005 | Developer | **Confirmation-dialog tests** (same as D-1 above) need a future injection or interface mock to become testable without `MainViewModel`. | Medium |

### Out-of-Scope / Not Addressed (Flag for Next Cycle)

| # | Source | Agent | Description | Priority |
|---|---|---|---|---|
| O-1 | WP-003 | Reviewer → Documentation | **it-IT `DependentCause_Added` gender agreement** (`aggiunta` vs `aggiunto`). Key name could not be verified — no `DependentCause_*` key exists in any locale file. Reviewer note may contain an incorrect key name. Recommend investigation before fix. | Low |
| O-2 | WP-007 | Reviewer | **No Changes card** has no explicit `Background` brush (uses MD card default). Deliberate neutral-state design. A future `DiffBrush.GuidanceNoChangesBackground` would make the intent self-documenting via resources rather than an inline comment. | Low |
| O-3 | WP-007 | Reviewer | **Moved-only diff edge case**: when all diff entries are `DiffChangeType.Moved`, `HasDifferences=true` but all guidance cards are hidden, leaving the sidebar empty (title only). The sorting banner covers this scenario. A future `ShowMovedGuidance` card could close the gap cleanly. | Low |
| O-4 | WP-003 | Developer | **Other locale files** use flat key ordering with no structural grouping for `CountFormat`/`CountSingular` pairs. A future JSONC/YAML format with comment support could make the pairing relationships self-documenting. | Low |
| O-5 | WP-002 | Reviewer | **Other `ViewTexts` classes** carry similarly minimal one-line XML `<summary>` docs. A future pass could apply the `<summary>` + `<remarks>` pattern from `DiffDialogTexts.cs` across the entire `ViewTexts` namespace for consistency. | Low |

---

## Next Steps for the Planner

1. **Resolve D-1 / D-4 (confirmation-dialog test coverage):** Design a lightweight injection or interface mock for `UpdateReferenceCommand` in `DiffDialogViewModel` to unblock the two skipped tests. This is the highest-priority technical debt carried out of this cycle.

2. **Investigate O-1 (it-IT `DependentCause_Added`):** Verify the correct key name and grammatical context before fixing. May require a native Italian reviewer.

3. **Address O-3 (Moved-only sidebar gap):** Add `ShowMovedGuidance` and a corresponding card for completeness. Requires a new locale key group (title, body, count format/singular) and a ViewModel property — small scope, high polish value.

4. **Risk-color foreground brush consolidation (Rec. #4):** Extract `#EF5350`, `#FF9800`, `#9C27B0`, `#66BB6A` from `DiffWindow.xaml` into named `DiffBrush.*Foreground` resources in `DiffBrushes.xaml`. Small, safe, no behavioral change.

5. **`UpdateDiffState()` notification refactor (D-2 / Rec. #3):** Evaluate `OnPropertyChanged(string.Empty)` as a near-term fix for the growing notification list; defer the `CommunityToolkit.Mvvm` source-generator migration to a dedicated refactor plan if the full ViewModel restructuring is warranted.
