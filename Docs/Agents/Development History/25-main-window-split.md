# MainViewModel Refactoring Plan

## Overview
The MainViewModel has grown to over 1200 lines and handles too many responsibilities. This plan outlines a phased approach to split it into smaller, focused components while maintaining functionality and improving testability.

## Current State Analysis
- **Total Lines**: ~1200+
- **Key Responsibilities**:
  - Coordinator orchestration (7 coordinators)
  - Window lifecycle management (4 non-modal windows)
  - Command handling (20+ commands)
  - UI text properties (30+ menu/label strings)
  - Event wire-up and forwarding
  - File operations
  - Profile management
  - Reference management
  - Update checking
  - Configuration management

## Refactoring Goals
1. Reduce MainViewModel to ~400-500 lines
2. Improve testability through better separation of concerns
3. Maintain existing functionality without breaking changes
4. Keep UI bindings working seamlessly
5. Establish patterns for future feature additions

---

## Phase 1: Quick Wins (350-500 line reduction)

### Step 1.1: Extract Window Management Service
**Estimated Reduction**: ~150-200 lines

Create `Services/WindowManager.cs` to handle:
- Non-modal window singleton tracking
- Window activation/focus logic
- Window closed event handlers
- Window creation with proper Owner/DataContext setup

**Files to Create**:
- `Services/WindowManager.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Eliminates 4 window field declarations
- Removes repetitive window lifecycle code
- Centralizes window management logic
- Makes window behavior more testable

---

### Step 1.2: Extract Menu Properties to MenuViewModel
**Estimated Reduction**: ~100-150 lines

Create `ViewModels/MenuViewModel.cs` to contain:
- All menu text properties (FileMenuHeader, OpenPluginsMenuText, etc.)
- Menu-related commands that don't require MainViewModel state
- Static text constants for labels and messages

**Files to Create**:
- `ViewModels/MenuViewModel.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`
- `Views/MainWindow.xaml` (update bindings to use MenuViewModel property)

**Benefits**:
- Removes ~30 string property declarations
- Separates UI text from business logic
- Makes localization easier in the future
- Cleaner separation of concerns

---

### Step 1.3: Consolidate Coordinator Event Wiring
**Estimated Reduction**: ~100-150 lines

Create `Helpers/CoordinatorEventBinder.cs` to handle:
- Automatic property change forwarding from coordinators
- Event subscription setup
- Reduce boilerplate in constructor

**Files to Create**:
- `Helpers/CoordinatorEventBinder.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Eliminates repetitive PropertyChanged handlers
- Reduces constructor complexity
- Makes coordinator integration pattern reusable
- Easier to add new coordinators

---

### Phase 1 Summary
- **Total Estimated Reduction**: 350-500 lines
- **Expected Result**: MainViewModel at ~700-850 lines
- **Risk Level**: Low (minimal architectural changes)
- **Testing Focus**: Window opening, menu display, coordinator property updates

---

## Phase 2: Architectural Improvements (380-500 line reduction)

### Step 2.1: Extract File Operation Commands Service
**Estimated Reduction**: ~120-150 lines

Create `Services/FileOperationsService.cs` to handle:
- OpenPluginsFile, OpenReferenceFile
- OpenAppDataFolder, OpenGameFolder, OpenConfigFolder
- LaunchShellTarget helper
- File path validation

**Files to Create**:
- `Services/FileOperationsService.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Consolidates file/folder opening logic
- Makes file operations easily testable
- Reduces MainViewModel command count
- Better error handling centralization

---

### Step 2.2: Extract Reference Management Service
**Estimated Reduction**: ~150-200 lines

Create `Services/ReferenceManagementService.cs` to handle:
- CreateReferenceAsync logic
- DiscardChangesAsync logic
- Reference initialization
- Pending changes management
- Archive and rollback coordination

**Files to Create**:
- `Services/ReferenceManagementService.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Separates complex reference logic
- Makes reference operations testable
- Reduces cognitive load in MainViewModel
- Better encapsulation of reference workflow

---

### Step 2.3: Extract Initialization Logic Service
**Estimated Reduction**: ~80-100 lines

Create `Services/ViewModelInitializer.cs` to handle:
- LoadInitialStateAsync
- EnsureValidConfigurationAsync
- EnsureReferenceFileExistsAsync
- Initial coordinator configuration

**Files to Create**:
- `Services/ViewModelInitializer.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Separates startup concerns
- Makes initialization sequence testable
- Easier to modify startup behavior
- Cleaner MainViewModel constructor

---

### Step 2.4: Extract Profile Command Handler
**Estimated Reduction**: ~50-80 lines

Create `Services/ProfileCommandService.cs` to handle:
- SwitchProfileAsync
- ManageProfilesAsync
- ShowReferenceHistory
- ViewPendingChanges
- HandleRollbackRequestAsync

**Files to Create**:
- `Services/ProfileCommandService.cs`

**Files to Modify**:
- `ViewModels/MainViewModel.cs`

**Benefits**:
- Groups related profile operations
- Reduces MainViewModel command count
- Makes profile workflows testable
- Better separation of concerns

---

### Phase 2 Summary
- **Total Estimated Reduction**: 380-500 lines
- **Expected Result**: MainViewModel at ~300-400 lines
- **Risk Level**: Medium (requires careful service interface design)
- **Testing Focus**: Reference operations, profile management, file operations, initialization

---

## Phase 3: Optional Advanced Refactoring

### Step 3.1: Create Specialized ViewModels (Future)
For even better separation, consider:
- `StatusBarViewModel` - status messages and history
- `ToolbarViewModel` - button states and toolbar actions
- `UpdateNotificationViewModel` - update banner logic

**Note**: This is optional and should only be pursued if Phase 2 results are insufficient.

---

## Implementation Strategy

### Iteration Approach
1. Implement one step at a time
2. Run full test suite after each step
3. Verify UI functionality manually
4. Commit after each successful step
5. Address any issues before moving to next step

### Testing Strategy
- **Unit Tests**: Test new services in isolation
- **Integration Tests**: Verify coordinator interactions
- **Manual Testing**: Verify UI functionality after each phase
- **Regression Testing**: Ensure existing features still work

### Rollback Plan
- Each step is committed separately
- Easy to revert individual changes if issues arise
- Feature branch protects main codebase

---

## Risk Assessment

### Low Risk
- Window Management extraction
- Menu properties extraction
- Event wiring consolidation

### Medium Risk
- File operations service (command integration)
- Reference management service (complex workflow)
- Initialization service (startup dependencies)

### Mitigation Strategies
- Maintain existing public API in MainViewModel
- Use facade pattern to preserve command interfaces
- Extensive testing at each step
- Keep changes focused and atomic

---

## Success Criteria

### Phase 1 Complete
- [ ] MainViewModel reduced to ~700-850 lines
- [ ] All windows open correctly
- [ ] Menu items display properly
- [ ] Coordinator property updates work
- [ ] No regressions in functionality

### Phase 2 Complete
- [ ] MainViewModel reduced to ~300-400 lines
- [ ] All file operations work
- [ ] Reference management works
- [ ] Profile operations work
- [ ] Initialization completes successfully
- [ ] All tests pass

### Overall Success
- [ ] Code is more maintainable
- [ ] Test coverage improved
- [ ] Future features easier to add
- [ ] No performance degradation
- [ ] Documentation updated

---

## Timeline Estimates

### Phase 1
- Step 1.1: 2-3 hours
- Step 1.2: 2-3 hours
- Step 1.3: 1-2 hours
- Testing/Fixes: 1-2 hours
- **Total**: 6-10 hours

### Phase 2
- Step 2.1: 2-3 hours
- Step 2.2: 3-4 hours
- Step 2.3: 2-3 hours
- Step 2.4: 2-3 hours
- Testing/Fixes: 2-3 hours
- **Total**: 11-16 hours

### Grand Total
- **Phase 1 + 2**: 17-26 hours
- **Recommended**: Start with Phase 1, evaluate results, then proceed to Phase 2

---

## Recommendation

**Start with Phase 1** as it provides:
- Significant line reduction (350-500 lines)
- Low risk of breaking changes
- Quick wins that demonstrate value
- Foundation for Phase 2

After Phase 1 completion:
- Evaluate if further refactoring is needed
- Assess code maintainability improvements
- Decide if Phase 2 is warranted

**Phase 2 should be pursued** if:
- MainViewModel is still difficult to maintain after Phase 1
- Additional features will be added soon
- Team agrees architectural improvements are worth the effort

---

## Notes

- All service classes should follow existing patterns in the codebase
- Maintain dependency injection compatibility
- Preserve existing command names for UI bindings
- Keep coordinators as primary architectural pattern
- Services should be thin wrappers, not new business logic layers
- Focus on organization and testability, not reimplementation
