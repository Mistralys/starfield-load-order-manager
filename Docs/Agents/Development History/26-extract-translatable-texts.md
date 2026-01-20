# Feature: Extract Translatable Texts

## The Problem

Currently, our view models contain hardcoded translatable texts. This makes it difficult 
to manage translations and update texts without modifying the code. Also, working on
feature branches that modify view models often leads to merge conflicts due to the code
being modified simultaneously.

## The Goal

To guarantee stability and ease of maintenance, we want to extract all translatable texts 
from the view models into separate resource files. This will allow translators to work
independently of the codebase and reduce merge conflicts.

## The Plan

We have already extracted the main window's texts, for example: `/ViewModels/MenuViewModel.cs`.
I would like to extend this to all windows, and to have one folder where all the texts live.

### Approach

We will implement this in two phases, with Phase 1 split into Work Packages (WP):

#### Phase 1: Extract Existing Texts - CODE CHANGES ONLY
Split into 10 Work Packages for manageable, atomic commits:

- **WP1:** Foundation Setup - Create ViewTexts folder, move existing text ViewModels
- **WP2:** Audit All ViewModels - Document all hardcoded strings (no code changes)
- **WP3:** Simple Dialogs - Extract ErrorDialog, CommentInput, ConfirmationDialog
- **WP4:** Settings Window - Extract SettingsViewModel
- **WP5:** Profile Management - Extract ManageProfiles, ProfileProperties, SwitchProfile
- **WP6:** Main Dialog - Extract DiffDialogViewModel
- **WP7:** History & Updates - Extract ReferenceHistory, UpdateOptions, ViewPendingChanges
- **WP8:** MainViewModel Cleanup - Extract remaining MainViewModel strings
- **WP9:** Services - Extract ReferenceManagementService and other services
- **WP10:** Final Verification - Build, test, verify language switching

**Goal:** All user-facing strings in code should reference resource files

#### Phase 2: Expand and Organize Resources - RESOURCE FILE CHANGES
- Create window-specific resource files for windows that don't have them yet (`.resx`)
- Identify shared/reusable strings across windows
- Expand `CommonResources.resx` with shared strings
- Create corresponding text ViewModels in `ViewTexts/` folder for new resource files
- Add translations (.de.resx, .fr.resx) for all new resource files
- **Goal:** Well-organized, maintainable resource structure

### Organization Structure

**Resource Files** (stay in existing `Resources/` folder):
```
Resources/
├── AboutWindowResources.resx (+ .de.resx, .fr.resx) [EXISTS]
├── CommonResources.resx (+ .de.resx, .fr.resx) [EXISTS]
├── MainWindowResources.resx (+ .de.resx, .fr.resx) [EXISTS]
├── CommentInputResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── ConfirmationDialogResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── DiffDialogResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── ErrorDialogResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── ManageProfilesResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── ProfilePropertiesResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── ReferenceHistoryResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── SettingsWindowResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── SwitchProfileResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
├── UpdateOptionsResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
└── ViewPendingChangesResources.resx (+ .de.resx, .fr.resx) [PHASE 2]
```

**Text ViewModels** (in new `ViewTexts/` folder):
```
ViewTexts/
├── MenuViewModel.cs [EXISTS - to be moved from ViewModels/]
├── AboutViewModel.cs [EXISTS - to be moved from ViewModels/]
├── CommentInputTexts.cs [PHASE 2]
├── ConfirmationDialogTexts.cs [PHASE 2]
├── DiffDialogTexts.cs [PHASE 2]
├── ErrorDialogTexts.cs [PHASE 2]
├── ManageProfilesTexts.cs [PHASE 2]
├── ProfilePropertiesTexts.cs [PHASE 2]
├── ReferenceHistoryTexts.cs [PHASE 2]
├── SettingsWindowTexts.cs [PHASE 2]
├── SwitchProfileTexts.cs [PHASE 2]
├── UpdateOptionsTexts.cs [PHASE 2]
└── ViewPendingChangesTexts.cs [PHASE 2]
```

---

## Phase 1 Work Packages (Detailed)

### WP1: Foundation Setup ⚡ Low Risk
**Goal:** Create ViewTexts folder structure and move existing text ViewModels

**Tasks:**
1. Create `ViewTexts/` folder in project root
2. Move `MenuViewModel.cs` from `ViewModels/` to `ViewTexts/`
3. Update `MenuViewModel.cs` namespace to `LoadOrderKeeper.ViewTexts`
4. Update all references to `MenuViewModel` (MainViewModel, etc.)
5. Move `AboutViewModel.cs` from `ViewModels/` to `ViewTexts/`
6. Update `AboutViewModel.cs` namespace to `LoadOrderKeeper.ViewTexts`
7. Update all references to `AboutViewModel`
8. Build and verify no errors

**Files Changed:** ~2-4 files  
**Commit Message:** `feat: Create ViewTexts folder and move text ViewModels`

---

### WP2: Audit All ViewModels 📋 Documentation Only
**Goal:** Document all hardcoded strings without making code changes

**Tasks:**
1. Create audit document (e.g., `26-strings-audit.md`)
2. For each ViewModel, list:
   - ViewModel name
   - All hardcoded user-facing strings
   - String category (button, message, title, etc.)
   - Proposed resource file (CommonResources or MainWindowResources)
3. Count total strings to extract
4. Identify any patterns or common strings

**ViewModels to Audit:**
- ErrorDialogViewModel
- CommentInputViewModel
- ConfirmationDialogViewModel
- SettingsViewModel
- ManageProfilesViewModel
- DiffDialogViewModel
- ReferenceHistoryViewModel
- UpdateOptionsViewModel
- SwitchProfileViewModel
- ProfilePropertiesViewModel
- ViewPendingChangesViewModel
- MainViewModel (remaining strings)

**Files Changed:** 1 documentation file  
**Commit Message:** `docs: Audit hardcoded strings in ViewModels`

---

### WP3: Simple Dialogs ⚡ Low Risk
**Goal:** Extract strings from simple, isolated dialogs

**ViewModels:**
- ErrorDialogViewModel (BugReportUrl, status messages)
- CommentInputViewModel (placeholder, button texts)
- ConfirmationDialogViewModel (button texts - may already use properties)

**Tasks:**
1. Add strings to `CommonResources.resx` (buttons) or `MainWindowResources.resx` (messages)
2. Update each ViewModel to reference resources
3. Build and test each ViewModel
4. Document temporary resource location for Phase 2

**Files Changed:** ~3-5 files  
**Commit Message:** `feat: Extract strings from simple dialog ViewModels`

---

### WP4: Settings Window ⚙️ Medium Risk
**Goal:** Extract strings from SettingsViewModel

**Strings to Extract:**
- Window titles
- Button texts
- Status banner messages
- Validation error messages
- Label texts

**Tasks:**
1. Add strings to `MainWindowResources.resx` (temporary location)
2. Update SettingsViewModel to reference resources
3. Build and test settings window
4. Document strings for Phase 2 migration

**Files Changed:** ~2-3 files  
**Commit Message:** `feat: Extract strings from SettingsViewModel`

---

### WP5: Profile Management 👥 Medium Risk
**Goal:** Extract strings from profile-related ViewModels

**ViewModels:**
- ManageProfilesViewModel
- ProfilePropertiesViewModel
- SwitchProfileViewModel

**Tasks:**
1. Add strings to `MainWindowResources.resx` (temporary location)
2. Update each ViewModel to reference resources
3. Build and test profile management features
4. Document strings for Phase 2 migration

**Files Changed:** ~4-6 files  
**Commit Message:** `feat: Extract strings from profile ViewModels`

---

### WP6: Main Dialog - DiffDialog ⚠️ High Risk, Complex
**Goal:** Extract strings from DiffDialogViewModel (30+ strings)

**Strings to Extract:**
- Window title, descriptions
- Button texts (many)
- Menu items
- Status messages
- Confirmation messages
- Help messages

**Tasks:**
1. Add strings to `MainWindowResources.resx` (temporary location)
2. Update DiffDialogViewModel to reference resources
3. Thoroughly test diff dialog functionality
4. Document strings for Phase 2 migration

**Files Changed:** ~2-3 files  
**Commit Message:** `feat: Extract strings from DiffDialogViewModel`

---

### WP7: History & Updates 📜 Medium Risk
**Goal:** Extract strings from history and update-related ViewModels

**ViewModels:**
- ReferenceHistoryViewModel
- UpdateOptionsViewModel
- ViewPendingChangesViewModel

**Tasks:**
1. Add strings to `MainWindowResources.resx` (temporary location)
2. Update each ViewModel to reference resources
3. Build and test history/update features
4. Document strings for Phase 2 migration

**Files Changed:** ~4-6 files  
**Commit Message:** `feat: Extract strings from history and update ViewModels`

---

### WP8: MainViewModel Cleanup 🧹 Low Risk
**Goal:** Extract remaining hardcoded strings from MainViewModel

**Tasks:**
1. Identify any remaining hardcoded strings in MainViewModel
2. Add strings to `MainWindowResources.resx`
3. Update MainViewModel to reference resources
4. Build and test main window functionality

**Files Changed:** ~2-3 files  
**Commit Message:** `feat: Extract remaining strings from MainViewModel`

---

### WP9: Services 🔧 Medium Risk
**Goal:** Extract user-facing strings from services

**Services:**
- ReferenceManagementService (status messages)
- Other services with user-facing strings

**Tasks:**
1. Audit services for user-facing strings
2. Add strings to `MainWindowResources.resx` or `CommonResources.resx`
3. Update services to reference resources
4. Build and test affected functionality

**Files Changed:** ~2-4 files  
**Commit Message:** `feat: Extract strings from services`

---

### WP10: Final Verification ✅ Testing
**Goal:** Ensure all changes work correctly

**Tasks:**
1. Build entire solution - verify no errors
2. Test all windows/dialogs
3. Verify language switching works (English, German, French)
4. Search codebase for remaining hardcoded strings
5. Update this document with completion status
6. Document any findings or issues

**Files Changed:** 1 documentation file  
**Commit Message:** `docs: Complete Phase 1 - text extraction verification`

---

## Phase 2 Implementation Steps

### Step 1: Create Window-Specific Resource Files
For each window/dialog without a resource file:
1. Create `.resx` file in `Resources/` folder
2. Move strings from temporary locations (MainWindowResources) to appropriate resource files
3. Create `.de.resx` and `.fr.resx` translation files
4. Create corresponding text ViewModel in `ViewTexts/` folder (e.g., `ErrorDialogTexts.cs`)
5. Update ViewModels to reference new text ViewModels

### Step 2: Identify and Extract Common Strings
- Analyze all resource files for duplicates
- Move common strings to `CommonResources.resx`
- Update text ViewModels to use common resources

### Step 3: Final Verification
- Build and test all changes
- Verify all translations work correctly
- Update documentation

---

## Guidelines

### Naming Conventions

- **Resource Keys**: Use descriptive names like `WindowTitle`, `ButtonText_Save`, `Message_ConfirmDelete`
- **Format Strings**: Use .NET format strings for dynamic content (e.g., `"Version {0} is available!"`)
- **Comments**: Always add comments to resource entries explaining context for translators
- **Text ViewModels**: Name them `*Texts.cs` for new ones (e.g., `ErrorDialogTexts.cs`) to distinguish from regular ViewModels
- **Existing Text ViewModels**: Keep current names (`MenuViewModel.cs`, `AboutViewModel.cs`) for backward compatibility

### What NOT to Extract

- Debug strings
- Internal error messages not shown to users
- Developer-facing logs
- Exception messages for developers

### Translation Management

- Using ResX format for all resource files
- Resource files stay in `Resources/` folder
- Text-providing ViewModels live in `ViewTexts/` folder
- Translations managed via ResXManager or similar tools
- Language switching logic already exists and is working

### Work Package Best Practices

- **One WP at a time** - Complete and commit before moving to next
- **Test after each WP** - Verify functionality works
- **Document as you go** - Note any issues or decisions
- **Atomic commits** - Each WP = one commit with clear message
- **Easy rollback** - Can revert individual WP if needed

---

## Status Tracking

**Current Phase:** Phase 1 - Code-Facing Text Extraction  
**Current Work Package:** WP1 - Foundation Setup  
**Overall Progress:** 0/10 Work Packages Complete (0%)

### Phase 1 Progress (Work Packages)

| WP | Name | Status | Files | Risk | Notes |
|----|------|--------|-------|------|-------|
| WP1 | Foundation Setup | ⏳ Pending | 2-4 | ⚡ Low | Create ViewTexts, move existing |
| WP2 | Audit ViewModels | ⏳ Pending | 1 | 📋 Docs | Document strings only |
| WP3 | Simple Dialogs | ⏳ Pending | 3-5 | ⚡ Low | Error, Comment, Confirmation |
| WP4 | Settings Window | ⏳ Pending | 2-3 | ⚙️ Medium | SettingsViewModel |
| WP5 | Profile Management | ⏳ Pending | 4-6 | 👥 Medium | 3 profile ViewModels |
| WP6 | DiffDialog | ⏳ Pending | 2-3 | ⚠️ High | 30+ strings, complex |
| WP7 | History & Updates | ⏳ Pending | 4-6 | 📜 Medium | 3 ViewModels |
| WP8 | MainViewModel Cleanup | ⏳ Pending | 2-3 | 🧹 Low | Remaining strings |
| WP9 | Services | ⏳ Pending | 2-4 | 🔧 Medium | ReferenceManagement, etc. |
| WP10 | Final Verification | ⏳ Pending | 1 | ✅ Test | Build, test, verify |

### Completed Work

#### Pre-Phase 1
- ✅ MenuViewModel extraction (exists, ready to move to ViewTexts/)
- ✅ AboutViewModel extraction (exists, ready to move to ViewTexts/)
- ✅ MainWindow partial resource references
- ✅ Plan document updated with Work Packages

### Phase 2 Progress
- ⏳ Not started - will begin after Phase 1 completion (all 10 WPs done)

---

## Notes & Decisions

### Temporary Resource Location Strategy
During Phase 1, strings will be temporarily added to existing resource files:
- **CommonResources.resx** - For truly shared strings (OK, Cancel, Yes, No, Close, etc.)
- **MainWindowResources.resx** - For window-specific strings as temporary holding location

In Phase 2, these strings will be moved to their proper window-specific resource files.

### Risk Assessment
- **Low Risk (⚡)**: Small, isolated changes, easy to test
- **Medium Risk (⚙️)**: Multiple files, moderate complexity
- **High Risk (⚠️)**: Many strings, complex logic, requires thorough testing
- **Documentation (📋)**: No code changes, safe

### Success Criteria
Phase 1 is complete when:
- ✅ All 10 Work Packages committed
- ✅ Zero hardcoded user-facing strings in ViewModels
- ✅ Zero hardcoded user-facing strings in Services
- ✅ All builds succeed without errors
- ✅ Language switching works correctly
- ✅ All tests pass


