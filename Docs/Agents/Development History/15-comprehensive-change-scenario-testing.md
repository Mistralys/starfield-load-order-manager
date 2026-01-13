# Project: Comprehensive Change Scenario Testing

## Objective

In the folder [Sorting Scenarios](../Sorting%20Scenarios), a number of real world
examples have been collected to illustrate the application's load order management 
capabilities. This must be translated into a comprehensive suite of automated tests 
as foundation for verifying future changes do not break existing functionality.

## Implementation Steps

### 1. Verify Existing Scenarios

The scenarios must be checked for correctness and completeness to ensure that
they do not contain any logic fallacies or errors.

**Status**: ✅ Complete
- All 15 scenarios documented
- Scenario 06 title corrected
- Scenario 12 dependent changes fixed
- Consistent structure across all scenarios

### 2. Implement Test Infrastructure

Create the foundational test infrastructure to support scenario testing.

#### 2.1 Create Test Base Class

- Create `Tests/LoadOrderKeeper.Tests/ScenarioTestBase.cs`
- Implement helper methods for:
  - Setting up the standard 18-mod reference list
  - Creating scenario-specific current orders from arrays
  - Building expected diff result objects
  - Asserting change detection results
  - Verifying dependent changes lists
  - Comparing load orders after sorting

#### 2.2 Create Test Data Constants

- Define constant arrays for the standard 18-mod list
- Create helper methods to generate test file content
- Implement methods to create mock Data folder structures

#### 2.3 Create Assertion Helpers

Create strongly-typed assertion methods:
- `AssertModAdded(diffs, modName, position)`
- `AssertModRemoved(diffs, modName, refPosition)`
- `AssertModMoved(diffs, modName, fromPos, toPos)`
- `AssertModReplaced(diffs, oldMod, newMod, position)`
- `AssertDependentChanges(diff, expectedDependents)`
- `AssertNoChanges(diffs)`

### 3. Implement Scenario Tests

Create automated tests that cover all scenarios. Each scenario should be represented
as a separate test case, with clear setup, execution, and verification steps.

#### 3.1 Create Scenario Test File

- Create `Tests/LoadOrderKeeper.Tests/ScenarioTests.cs`
- Inherit from `ScenarioTestBase`
- Implement one test method per scenario

#### 3.2 Scenario Test Methods

Implement the following test methods:

**Basic Operations**
- `Scenario01_AddedNewMod_DetectsAddition()`
- `Scenario02_SortingModifiedExternally_RestoresOrder()`
- `Scenario03_InsertedNewMod_DetectsInsertion()`
- `Scenario04_DeletedMod_DetectsDeletion()`
- `Scenario05_ReplacedMod_DetectsReplacement()`

**Complex Operations**
- `Scenario06_CombinedChanges_DetectsMultipleChangeTypes()`
- `Scenario07_MultipleDeletedMods_DetectsAllDeletions()`
- `Scenario08_MultipleAddedMods_DetectsAllAdditions()`
- `Scenario09_MultipleReplacedMods_DetectsAllReplacements()`
- `Scenario10_MultipleMovedExternally_RestoresAllPositions()`
- `Scenario11_DisabledMods_TreatsAsRemoved()`
- `Scenario12_InsertedAndMovedCombination_DetectsBothChanges()`

**Edge Cases**
- `Scenario13_CaseSensitivity_IgnoresCaseChanges()`
- `Scenario14_AllModsReordered_RestoresCompleteOrder()`
- `Scenario15_WhitespaceAndComments_IgnoresFormatting()`

#### 3.3 Test Structure Pattern

Each test should follow this consistent pattern:

```csharp
[Fact]
public async Task ScenarioXX_Description_ExpectedOutcome()
{
    // Arrange
    using var context = new TestConfigContext();
    await SetupStandardReferenceAsync(context);
    await SetupScenarioCurrentOrderAsync(context, /* specific mods */);
    
    // Act - Detect Changes
    var diffs = await FileService.GetModDiffAsync(context.Config);
    
    // Assert - Change Detection
    AssertModAdded(diffs, "NewMod.esm", 19);
    AssertDependentChanges(diffs, "NewMod.esm", expectedDependents);
    
    // Act - Apply Sorting
    await FileService.ApplyLoadOrderAsync(context.Config);
    var postSortDiffs = await FileService.GetModDiffAsync(context.Config);
    
    // Assert - Post-Sort State
    AssertModAdded(postSortDiffs, "NewMod.esm", 19);
    // Verify other expectations...
}
```

### 4. Implement Additional Test Categories

Create specialized test files for specific concerns.

#### 4.1 Dependent Changes Tests

- Create `Tests/LoadOrderKeeper.Tests/DependentChangesTests.cs`
- Test cascade detection for:
  - Single deletion causing cascades
  - Multiple deletions causing cascades
  - Insertions causing cascades
  - Cascade boundary conditions (where cascades stop)
  - Complex multi-level cascades (scenario 06, 07, 12)

#### 4.2 Sorting Behavior Tests

- Create `Tests/LoadOrderKeeper.Tests/SortingBehaviorTests.cs`
- Test sorting logic for:
  - Preserving user-directed changes (replacements)
  - Moving additions to the end
  - Restoring moved mods to reference positions
  - Handling combined operations correctly
  - Maintaining relative order of new mods

#### 4.3 Edge Case Tests

- Create `Tests/LoadOrderKeeper.Tests/EdgeCaseTests.cs`
- Test edge conditions:
  - Empty load order (all mods removed)
  - Single mod scenarios
  - Large load orders (100+ mods)
  - Duplicate entries (error handling)
  - Malformed file content
  - Permission errors

### 5. Validation and Acceptance Criteria

#### 5.1 Test Coverage Metrics

- [ ] All 15 scenarios have corresponding test methods
- [ ] All change types are covered (Add, Remove, Move, Replace, Insert)
- [ ] All dependent change patterns are tested
- [ ] Edge cases are handled gracefully
- [ ] All scenarios include both detection and sorting verification

#### 5.2 Test Quality Checks

- [ ] All tests follow the consistent AAA (Arrange-Act-Assert) pattern
- [ ] Test names clearly describe the scenario being tested
- [ ] Assertions are specific and provide clear failure messages
- [ ] Test failures provide diagnostic information
- [ ] Tests are independent and can run in any order
- [ ] Tests clean up resources properly (using statements)

#### 5.3 Build Integration

- [ ] All tests pass in local environment
- [ ] Tests are included in CI/CD pipeline
- [ ] Test execution time is reasonable (<30 seconds for full suite)
- [ ] No flaky tests (consistent pass/fail results)
- [ ] Code coverage meets minimum threshold (>80%)

#### 5.4 Documentation

- [ ] Each test has XML documentation describing its purpose
- [ ] Test data setup is clearly documented
- [ ] Complex assertions are commented
- [ ] README updated with test execution instructions

## Success Criteria

The implementation is considered complete when:

1. All 15 scenario tests are implemented and passing
2. Test infrastructure provides reusable components
3. All acceptance criteria checkboxes are marked complete
4. Code review has been conducted and approved
5. Tests are integrated into the build pipeline
6. No regressions are introduced to existing functionality

## Notes

- Tests should use `TestConfigContext` for isolated test environments
- Mock file system operations should use temporary directories
- Tests should be deterministic and not depend on external state
- Consider parameterized tests for similar scenarios
- Document any assumptions or limitations in test comments
