# Plan

## Plan Audit Cycles
- Audits: 1 - Plan Auditor v1.7.0
- Architectural Reviews: none - Plan Architect Reviewer v2.2.0

## Prior Project Context
- The repository's long-term strategy is a stable application that is straightforward to maintain.
- `Gameloop.Vdf` is the source of the recorded vulnerable transitive `System.Net.Http` and `System.Text.RegularExpressions` package chain, and the repository notes report no newer package release.
- Existing repository testing practice favors direct unit tests plus end-to-end integration scenarios when behavior crosses an abstraction boundary.
- Test results recorded in the repository and prior audit output are not treated as the current baseline; capture a fresh pre-change result immediately before implementation and compare post-change failures against it.

## Summary
Replace the direct `Gameloop.Vdf` dependency with a small project-owned parser dedicated to the Steam `libraryfolders.vdf` shape used by `SettingsService`. The parser will operate on text, return typed library-entry data, and leave AppID selection, Windows path normalization, installation validation, and the existing silent multi-level fallback chain in `SettingsService`. Remove the package from both application and test project files, add focused parser tests alongside the existing Steam discovery tests, and update the canonical agent manifest.

## Architectural Context
`SettingsService.TryGetDefaultSteamPath()` is the public entry point for automatic Starfield path detection. It finds Steam through Windows registry keys, calls the private `TryFindStarfieldInSteamLibraries(string steamInstallPath)`, and falls back to the main Steam installation and then the Program Files x86 location. The private method currently reads `steamapps/libraryfolders.vdf`, deserializes it with `Gameloop.Vdf`, searches nested library objects for AppID `1716740`, skips entries with a missing or blank path, missing or non-object `apps`, or no matching AppID, combines the library path with `steamapps/common/Starfield`, and requires a `Data` directory.

The application uses .NET 9 standard APIs, nullable reference types, static stateless services, xUnit tests, temporary filesystem fixtures, and an existing `InternalsVisibleTo` relationship from the application to `LoadOrderKeeper.Tests`. No public API or UI layer needs to change.

## Approach / Architecture
1. Add `Helpers/SteamLibraryVdfParser.cs` as an internal, stateless parser with no file I/O. Its test-visible internal contract should expose `Parse(string)` returning an `IReadOnlyList<SteamLibraryEntry>`, where each entry has a nullable `Path` and nullable `AppIds` set. A missing or non-scalar `path` produces `Path = null`; a missing or non-object `apps` produces `AppIds = null`; an empty apps object produces an empty set. Only object-valued children of the `libraryfolders` object become entries, and their source order is preserved. Keep the tokenizer, cursor, object traversal, and result types internal/private to the helper rather than adding a public API.
2. Implement the exact supported grammar required by the observed Steam file: quoted keys and values, values that are either quoted tokens or nested objects, whitespace, and `//` comments between tokens or at the end of a line. Comments are not recognized inside quoted values. Decode only `\\`, `\"`, `\n`, `\r`, and `\t`; reject unknown or unterminated escapes. Require exactly one top-level `libraryfolders` key whose value is an object, reject duplicate keys in any object and extra top-level pairs, ignore scalar children under `libraryfolders`, and require every key to have a value. Throw a bounded `FormatException` for malformed text, unbalanced braces, unterminated quotes, missing values, duplicate keys, or trailing tokens so the owning service can preserve its current fail-safe behavior.
3. Refactor `Services/SettingsService.cs` to read the file as text and delegate parsing to the helper. Keep the existing iteration order, AppID constant, `Path.Combine` layout, forward-slash normalization, `Data` directory check, and broad failure-to-`null` behavior. Do not move installation policy into the parser.
4. Remove `Gameloop.Vdf` from `Starfield Load Order Keeper.csproj` and `Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj`. Restore the solution and confirm no package or assembly reference remains in the effective dependency graph.
5. Add a test-project item in `Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj` that links `Docs/Agents/example-steam-library.vdf` into a `Fixtures` output subdirectory with `CopyToOutputDirectory=PreserveNewest`. Add direct parser tests under `Tests/LoadOrderKeeper.Tests/Helpers/SteamLibraryVdfParserTests.cs` that load that copied fixture through `AppContext.BaseDirectory` and cover the exact result semantics, escaped Windows paths, nested app maps, source-order preservation, missing optional sections, comments/whitespace, duplicate keys, and malformed input. Keep the existing `SettingsServiceTests` integration tests and extend them only where needed to prove the service still handles corrupted files, missing sections, first-match behavior, and path normalization through the new parser.
6. Update `Docs/Agents/project-manifest/tech-stack.md`, `file-tree.md`, `data-flows.md`, `file-formats.md`, and `constraints.md` to remove the external parser reference, list the new helper/test file, describe the built-in parsing step, document the supported `libraryfolders.vdf` subset and text handling, and record parser bounds, duplicate-key, and fail-safe invariants. Leave `api-surface.md` unchanged because the public `SettingsService` signatures remain the same.

## Rationale
The parser is deliberately scoped to the one Steam file and one current consumer. A typed result keeps `SettingsService` independent from a dynamic third-party object graph while avoiding a speculative general-purpose VDF AST or serializer. Keeping file I/O and installation checks in `SettingsService` preserves the current ownership boundary and makes parser tests deterministic. Parsing text with standard .NET APIs removes the vulnerable package chain without adding a replacement dependency.

The parser should be strict about syntax but forgiving at the application boundary: malformed or unavailable Steam metadata remains a normal detection miss, and `TryGetDefaultSteamPath()` continues through its existing fallbacks. Returning nullable path/app sections makes the parser's syntactically valid but incomplete entry behavior explicit while preserving the current service skip policy. This provides a clear maintenance contract without changing user-visible behavior.

## Considered Alternatives

| Decision | Chosen Shape | Alternatives Considered | Trade-Off Summary |
|----------|--------------|-------------------------|-------------------|
| Parser ownership | Internal `Helpers/SteamLibraryVdfParser.cs` | Keep parser private inside `SettingsService`; add a public service/interface | A separate internal helper enables focused grammar tests and keeps the settings service readable. A public abstraction has no current consumer and would expand the supported contract unnecessarily. |
| Parser result | Ordered typed library entries with path and app IDs | Dynamic objects; `Dictionary<string, object>`; full generic VDF AST | Typed entries expose exactly what the current consumer needs and avoid dynamic/runtime shape failures. A full AST would be more code and surface area for a single read-only file. |
| Parser input boundary | `string` input, with file access in `SettingsService` | Parser owns file paths and `File.ReadAllText`; use another parsing package | Text input makes syntax tests cheap and keeps filesystem/error policy in the existing service. Standard APIs meet the requirement without another dependency. |
| Malformed input policy | Parser throws `FormatException`; `SettingsService` converts all discovery failures to `null` | Silently return partial results; expose parser errors to the UI | Strict parsing prevents unsafe partial interpretation, while the existing service boundary preserves the current fail-safe UX and fallback order. |
| Fixture access | Copy the repository fixture to test output through the test project file | Resolve a repository-relative path at runtime; replace the fixture with inline content | An explicit linked content item makes the acceptance test deterministic from any test working directory without adding a production path convention. |

## Pattern Alignment
- Follows the repository's stateless utility pattern used by `Services/FileService.cs` and other static services; the new parser has no mutable application state.
- Follows the existing test-access pattern established by `InternalsVisibleTo` in `Starfield Load Order Keeper.csproj`; no public API is introduced for testing.
- Follows the existing Steam detection flow in `Services/SettingsService.cs`; only the parser implementation behind the private method changes.
- Extends the existing `Helpers/` directory with a domain-specific helper. This is a small, justified addition because the parser is pure and has a single narrow consumer.
- Follows the current xUnit temporary-directory approach in `Tests/LoadOrderKeeper.Tests/SettingsServiceTests.cs`, while adding pure string tests for the parser grammar.

## Detailed Steps
1. Capture a fresh pre-change baseline with the focused Steam discovery filter and the full test project. Record the test count and every failure name/message; do not classify any failure as pre-existing unless this command reproduces it immediately before implementation.
2. Define the internal parser contract and result type in `Helpers/SteamLibraryVdfParser.cs`, including nullable `Path`/`AppIds`, source-order preservation, scalar-root-child handling, duplicate-key rejection, exact escape decoding, and comment placement.
3. Implement token scanning with explicit cursor advancement and bounds checks; decode the specified VDF escapes, parse nested objects, preserve child order, require the expected root, reject duplicate keys and trailing data, and fail on malformed syntax.
4. Replace the `Gameloop.Vdf` imports and dynamic traversal in `Services/SettingsService.cs` with the helper call. Preserve `StarfieldAppId`, first-match behavior, path normalization, `Data` validation, and catch-to-`null` behavior.
5. Remove the package references from both application/test `.csproj` files and add the linked fixture-copy item to the test project. Restore the solution so stale package assets are regenerated rather than hand-edited.
6. Add parser unit tests for the exact supported grammar and result semantics, loading the copied repository fixture through `AppContext.BaseDirectory`. Keep or adjust `SettingsServiceTests` so integration tests exercise actual temporary `libraryfolders.vdf` files and verify first match, missing sections, corrupted files, path normalization, and installation validation.
7. Review the unchanged `SettingsService.TryGetDefaultSteamPath()` body to verify registry lookup, main-library fallback, Program Files x86 fallback, and public signatures remain intact. Treat this as the AC-05 verification obligation; the private-method integration tests must not be described as coverage of the public registry/fallback chain.
8. Update the five canonical manifest files and verify their dependency, file-tree, data-flow, format, and constraint statements match the implementation.
9. Run focused parser/service tests, then the full test project, build, and dependency/security verification. Compare post-change results with the freshly captured baseline and report only newly introduced failures as regressions.

## Dependencies
- Standard .NET 9 APIs only; no new NuGet package.
- Existing `InternalsVisibleTo` relationship from the application project to `LoadOrderKeeper.Tests`.
- Existing `Docs/Agents/example-steam-library.vdf` fixture, copied to test output by the modified test project, and temporary filesystem test conventions.

## Required Components
- **New:** `Helpers/SteamLibraryVdfParser.cs` - internal parser and typed internal result representation.
- **Modified:** `Services/SettingsService.cs` - delegate VDF parsing while retaining detection policy.
- **Modified:** `Starfield Load Order Keeper.csproj` - remove `Gameloop.Vdf`.
- **Modified:** `Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj` - remove `Gameloop.Vdf` and copy the linked repository fixture to test output.
- **New:** `Tests/LoadOrderKeeper.Tests/Helpers/SteamLibraryVdfParserTests.cs` - direct grammar and result tests.
- **Modified as needed:** `Tests/LoadOrderKeeper.Tests/SettingsServiceTests.cs` - integration regression coverage.
- **Modified documentation:** `Docs/Agents/project-manifest/tech-stack.md`, `file-tree.md`, `data-flows.md`, `file-formats.md`, and `constraints.md`.

## Assumptions
- The supported input is Steam's text `libraryfolders.vdf`, not binary VDF and not arbitrary Valve files.
- Current Steam files use the quoted nested-object form shown in `Docs/Agents/example-steam-library.vdf`; only whitespace, `//` inter-token comments, and the explicitly listed escape sequences are supported.
- The parser returns one entry per object-valued child under `libraryfolders` in source order, uses null for missing/non-scalar `path`, null for missing/non-object `apps`, and an empty set for an empty apps object; scalar root children are ignored and duplicate keys are rejected.
- Returning the first matching installed library in source order remains the desired behavior.
- The current public `SettingsService` signatures and user-facing fallback behavior must remain unchanged.
- The test project will load the checked-in fixture from its output directory rather than relying on the process working directory.
- Baseline status is determined by a fresh pre-change test run; no fixed failure is assumed.

## Constraints
- No third-party VDF/parser dependency may remain in either project.
- No direct edits to generated `bin/` or `obj/` package assets.
- Parser errors, missing files, invalid paths, and absent `Data` folders must continue to produce a detection miss and allow fallback detection.
- Parser loops must make progress and enforce bounds so malformed Steam metadata cannot hang startup path detection.
- The parser must reject duplicate keys, unknown escapes, extra top-level pairs, and trailing tokens; the service must convert those parse failures to a detection miss.
- Do not add UI, localization, registry changes, or unrelated refactors.

## Out of Scope
- Writing or serializing VDF files.
- Supporting binary VDF formats or every possible Valve Data Format extension.
- Replacing JSON parsing or changing any other file format in the application.
- Changing Steam registry detection, Starfield AppID, installation directory layout, or fallback precedence.
- Introducing a public VDF API or dependency-injection abstraction without a second consumer.
- Fixing the unrelated pre-existing profile test encoding issue.

## Acceptance Criteria
- AC-01: The application and test project contain no direct or transitive `Gameloop.Vdf` package/assembly reference after restore, and both projects build using only the remaining declared dependencies.
- AC-02: The internal parser test loads the copied `Docs/Agents/example-steam-library.vdf` fixture from `AppContext.BaseDirectory` and extracts ordered library paths and nested app IDs including `1716740`.
- AC-03: The parser correctly handles quoted tokens, escaped Windows backslashes, forward-slash paths, nested objects, whitespace, and `//` comments between tokens, decodes only `\\`, `\"`, `\n`, `\r`, and `\t`, and rejects unknown escapes, duplicate keys, extra top-level pairs, trailing tokens, unterminated quotes, missing values, and unbalanced braces with a bounded `FormatException`.
- AC-04: `SettingsService.TryFindStarfieldInSteamLibraries` retains first-match behavior, `Data` folder validation, path normalization, and conversion of parser/file failures to `null`; parser entries with null path/app sections do not produce a Starfield match.
- AC-05: A review of the unchanged `SettingsService.TryGetDefaultSteamPath()` implementation confirms its registry lookup, main-library fallback, Program Files x86 fallback order, and public signatures remain unchanged.
- AC-06: Focused parser/service tests pass, and the full post-change test run introduces no failure not present in the freshly captured pre-change baseline.
- AC-07: The canonical project manifest accurately describes the built-in parser, removed package, linked fixture test asset, new files, updated data flow, supported VDF subset, and parser/service constraints.

## Testing Strategy
Capture the baseline before implementation, then test the parser as a pure grammar component first and the owning service with actual temporary `libraryfolders.vdf` files and directory layouts. This separates syntax defects from installation-policy defects while still validating the full parser-to-service path. Load the supplied fixture from the test output asset for representative Steam data and use small inline strings for malformed and edge cases. Verify the unchanged public fallback method by focused code review, then run restore/build and package graph/security checks to prove the dependency removal is complete.

## Test Plan
- Fresh pre-change baseline command - run the focused Steam discovery filter and the full test project before implementation, recording counts and failure names/messages - establishes the comparison required by AC-06.
- `Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj` - link `Docs/Agents/example-steam-library.vdf` into `Fixtures/example-steam-library.vdf` and copy it to output - makes AC-02 deterministic.
- `Tests/LoadOrderKeeper.Tests/Helpers/SteamLibraryVdfParserTests.cs` - load the copied fixture and parse nested library/app objects in source order, including AppID `1716740` - covers AC-02.
- `Tests/LoadOrderKeeper.Tests/Helpers/SteamLibraryVdfParserTests.cs` - assert nullable path/apps results, empty apps behavior, ignored scalar root children, duplicate-key rejection, exact escape decoding, and comments/whitespace handling - covers AC-03 and AC-04.
- `Tests/LoadOrderKeeper.Tests/Helpers/SteamLibraryVdfParserTests.cs` - reject missing root/value, unknown or unterminated escapes, extra top-level pairs, trailing tokens, unterminated quotes, and unbalanced/misplaced braces without hanging - covers AC-03.
- `Tests/LoadOrderKeeper.Tests/SettingsServiceTests.cs` - retain first matching library, missing AppID, missing VDF, missing `Data`, malformed VDF, forward-slash normalization, and missing/non-object `apps` integration cases through the refactored parser - covers AC-04.
- `Services/SettingsService.cs` review - confirm `TryGetDefaultSteamPath()` retains registry lookup, main-library fallback, Program Files x86 fallback, and public signatures; do not count private-method tests as public fallback coverage - covers AC-05.
- Solution verification command - `dotnet restore "Starfield Load Order Keeper.sln"` followed by `dotnet build "Starfield Load Order Keeper.sln" --configuration Debug` - covers AC-01.
- Test verification command - `dotnet test "Tests/LoadOrderKeeper.Tests/LoadOrderKeeper.Tests.csproj" --configuration Debug` - covers AC-06; compare against the fresh pre-change baseline and do not assume a specific pre-existing failure.
- Dependency verification command - `dotnet list "Starfield Load Order Keeper.sln" package --include-transitive` and `dotnet list "Starfield Load Order Keeper.sln" package --vulnerable --include-transitive` - covers AC-01 and confirms the Gameloop-originated package chain is absent.

## Documentation Updates
- `Docs/Agents/project-manifest/tech-stack.md` - remove `Gameloop.Vdf` and describe the internal Steam library VDF helper using standard .NET APIs.
- `Docs/Agents/project-manifest/file-tree.md` - add `Helpers/SteamLibraryVdfParser.cs` and the parser test file to the logical tree.
- `Docs/Agents/project-manifest/data-flows.md` - replace the Gameloop-specific parsing step with the built-in parser step while retaining AppID, validation, normalization, and fallback descriptions.
- `Docs/Agents/project-manifest/file-formats.md` - document `libraryfolders.vdf` as a read-only external input, its supported quoted/nested subset, exact escape/comment handling, linked fixture loading, and fail-safe treatment of malformed input.
- `Docs/Agents/project-manifest/constraints.md` - record bounded parser progress, supported grammar limits, duplicate-key and trailing-data rejection, nullable incomplete-entry semantics, and conversion of parser failures to a detection miss.
- `Docs/Agents/project-manifest/api-surface.md` - no change required; verify the public `SettingsService` surface remains identical.

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| **Steam adds a valid construct outside the supported subset.** | Base the grammar on the checked-in fixture and real current files, include comments and escape handling, and preserve silent fallback when parsing fails. Keep parser scope explicit so future extensions have a clear test location. |
| **Incorrect VDF unescaping produces invalid Windows paths.** | Add exact escaped-path tests using the same strings already generated by `SettingsServiceTests`, then retain service-level normalization and `Directory.Exists` checks. |
| **A malformed file causes partial interpretation or a parser loop.** | Use a cursor with bounds checks, require a complete root/object parse, throw `FormatException` for malformed structure, and add malformed-input tests. |
| **Parser behavior for incomplete entries drifts from the current service policy.** | Specify and test null path/app sections, empty apps, ignored scalar root children, and strict duplicate-key behavior before implementation; keep installation policy in `SettingsService`. |
| **The fixture test depends on the process working directory or is unavailable in test output.** | Add an explicit linked content item to the test project and load `Fixtures/example-steam-library.vdf` from `AppContext.BaseDirectory`. |
| **The package remains through a second direct or transitive reference.** | Remove both direct references, restore from the project files, inspect the transitive package list, and verify the built output does not contain `Gameloop.Vdf.dll`. |
| **Baseline results are stale or misclassified.** | Run the focused and full test commands immediately before implementation, record exact failure names/messages, and compare the post-change run against that fresh snapshot rather than a fixed historical assumption. |

## Recommended Workflow
- **Workflow:** standalone
- **Rationale:** This is a focused dependency replacement confined to one private service path, two project files, one helper, and targeted tests, with no public API or cross-module architecture change.
