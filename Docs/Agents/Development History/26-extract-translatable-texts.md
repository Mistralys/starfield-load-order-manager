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

#### Phase 1: Foundation Setup - CODE STRUCTURE ONLY
Split into Work Packages for manageable, atomic commits:

- **WP1:** Foundation Setup - Create ViewTexts folder, move existing text ViewModels (MenuViewModel, AboutViewModel)
- **WP2:** Create ALL Text ViewModels - MainWindow, CommonResources, and all others
- **WP3:** Audit All ViewModels - Document all hardcoded strings (no code changes)

**Goal:** All windows have text ViewModels in ViewTexts/ folder, prepare for string extraction

#### Phase 2: Extract and Organize Resources - RESOURCE FILE CHANGES
Split into manageable work packages:

- **WP4:** Simple Dialogs - Create resources and extract ErrorDialog, CommentInput, ConfirmationDialog
- **WP5:** Settings Window - Create resources and extract SettingsViewModel
- **WP6:** Profile Management - Create resources and extract ManageProfiles, ProfileProperties, SwitchProfile
- **WP7:** Main Dialog - Create resources and extract DiffDialogViewModel
- **WP8:** History & Updates - Create resources and extract ReferenceHistory, UpdateOptions, ViewPendingChanges
- **WP9:** MainViewModel Cleanup - Extract remaining MainViewModel strings
- **WP10:** Services - Extract ReferenceManagementService and other services
- **WP11:** Final Verification - Build, test, verify language switching

**Goal:** All user-facing strings extracted to resource files with proper organization

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
├── MenuViewModel.cs [PHASE 1 WP1 - EXISTS, to be moved from ViewModels/]
├── AboutViewModel.cs [PHASE 1 WP1 - EXISTS, to be moved from ViewModels/]
├── MainWindowTexts.cs [PHASE 1 WP2 - wrap MainWindowResources]
├── CommonTexts.cs [PHASE 1 WP2 - wrap CommonResources]
├── CommentInputTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── ConfirmationDialogTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── DiffDialogTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── ErrorDialogTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── ManageProfilesTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── ProfilePropertiesTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── ReferenceHistoryTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── SettingsWindowTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── SwitchProfileTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
├── UpdateOptionsTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
└── ViewPendingChangesTexts.cs [PHASE 1 WP2 - created with hardcoded current strings]
```

**Note:** In Phase 1, text ViewModels for windows without resource files will be created with temporary placeholder strings or empty properties. In Phase 2, these will be connected to their respective resource files once created.

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

### WP2: Create ALL Text ViewModels ⚙️ Medium Risk
**Goal:** Create text ViewModels for ALL windows, establishing complete ViewTexts structure

**Tasks:**
1. Create text ViewModels for existing resource files:
   - Create `MainWindowTexts.cs` wrapping `MainWindowResources.resx`
   - Create `CommonTexts.cs` wrapping `CommonResources.resx`
2. Create text ViewModels for windows WITHOUT resource files (with hardcoded strings):
   - Create `CommentInputTexts.cs`, `ConfirmationDialogTexts.cs`, `ErrorDialogTexts.cs`
   - Create `DiffDialogTexts.cs`, `ManageProfilesTexts.cs`, `ProfilePropertiesTexts.cs`
   - Create `ReferenceHistoryTexts.cs`, `SettingsWindowTexts.cs`, `SwitchProfileTexts.cs`
   - Create `UpdateOptionsTexts.cs`, `ViewPendingChangesTexts.cs`
   - These will contain hardcoded string properties (extracted from their ViewModels)
3. Use `INotifyPropertyChanged` pattern for all text ViewModels
4. Update ALL ViewModels to use text ViewModels from `ViewTexts/` instead of hardcoded strings
5. Register all text ViewModels in DI container
6. Build and verify no errors
7. Test all windows and language switching

**Files Changed:** ~15-25 files (13 new text VMs + updates to all consuming ViewModels)
**Commit Message:** `feat: Create all text ViewModels and centralize text access`

---

### WP3: Audit All ViewModels 📋 Documentation Only
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

## Phase 2 Implementation Steps

### WP4: Simple Dialogs ⚡ Low Risk (PHASE 2)
**Goal:** Create resource files and extract strings from simple, isolated dialogs

**ViewModels:**
- ErrorDialogViewModel (BugReportUrl, status messages)
- CommentInputViewModel (placeholder, button texts)
- ConfirmationDialogViewModel (button texts - may already use properties)

**Tasks:**
1. Create `ErrorDialogResources.resx` with .de.resx and .fr.resx translations
2. Create `CommentInputResources.resx` with .de.resx and .fr.resx translations
3. Create `ConfirmationDialogResources.resx` with .de.resx and .fr.resx translations
4. Create corresponding text ViewModels in `ViewTexts/` folder
5. Extract hardcoded strings to new resource files
6. Update ViewModels to use text ViewModels
7. Build and test each ViewModel

**Files Changed:** ~9-15 files (3 resource files × 3 languages + 3 text VMs + 3 ViewModels)
**Commit Message:** `feat: Extract strings from simple dialog ViewModels`

---

### WP5: Settings Window ⚙️ Medium Risk (PHASE 2)
**Goal:** Create resource files and extract strings from SettingsViewModel

**Strings to Extract:**
- Window titles
- Button texts
- Status banner messages
- Validation error messages
- Label texts

**Tasks:**
1. Create `SettingsWindowResources.resx` with .de.resx and .fr.resx translations
2. Create `SettingsWindowTexts.cs` in `ViewTexts/` folder
3. Extract hardcoded strings to new resource file
4. Update SettingsViewModel to use text ViewModel
5. Build and test settings window

**Files Changed:** ~5 files (1 resource file × 3 languages + 1 text VM + 1 ViewModel)
**Commit Message:** `feat: Extract strings from SettingsViewModel`

---

### WP6: Profile Management 👥 Medium Risk (PHASE 2)
**Goal:** Create resource files and extract strings from profile-related ViewModels

**ViewModels:**
- ManageProfilesViewModel
- ProfilePropertiesViewModel
- SwitchProfileViewModel

**Tasks:**
1. Create `ManageProfilesResources.resx` with .de.resx and .fr.resx translations
2. Create `ProfilePropertiesResources.resx` with .de.resx and .fr.resx translations
3. Create `SwitchProfileResources.resx` with .de.resx and .fr.resx translations
4. Create corresponding text ViewModels in `ViewTexts/` folder
5. Extract hardcoded strings to new resource files
6. Update ViewModels to use text ViewModels
7. Build and test profile management features

**Files Changed:** ~12-15 files (3 resource files × 3 languages + 3 text VMs + 3 ViewModels)
**Commit Message:** `feat: Extract strings from profile ViewModels`

---

### WP7: Main Dialog - DiffDialog ⚠️ High Risk, Complex (PHASE 2)
**Goal:** Create resource files and extract strings from DiffDialogViewModel (30+ strings)

**Strings to Extract:**
- Window title, descriptions
- Button texts (many)
- Menu items
- Status messages
- Confirmation messages
- Help messages

**Tasks:**
1. Create `DiffDialogResources.resx` with .de.resx and .fr.resx translations
2. Create `DiffDialogTexts.cs` in `ViewTexts/` folder
3. Extract hardcoded strings to new resource file
4. Update DiffDialogViewModel to use text ViewModel
5. Thoroughly test diff dialog functionality

**Files Changed:** ~5 files (1 resource file × 3 languages + 1 text VM + 1 ViewModel)
**Commit Message:** `feat: Extract strings from DiffDialogViewModel`

---

### WP8: History & Updates 📜 Medium Risk (PHASE 2)
**Goal:** Create resource files and extract strings from history and update-related ViewModels

**ViewModels:**
- ReferenceHistoryViewModel
- UpdateOptionsViewModel
- ViewPendingChangesViewModel

**Tasks:**
1. Create `ReferenceHistoryResources.resx` with .de.resx and .fr.resx translations
2. Create `UpdateOptionsResources.resx` with .de.resx and .fr.resx translations
3. Create `ViewPendingChangesResources.resx` with .de.resx and .fr.resx translations
4. Create corresponding text ViewModels in `ViewTexts/` folder
5. Extract hardcoded strings to new resource files
6. Update ViewModels to use text ViewModels
7. Build and test history/update features

**Files Changed:** ~12-15 files (3 resource files × 3 languages + 3 text VMs + 3 ViewModels)
**Commit Message:** `feat: Extract strings from history and update ViewModels`

---

### WP9: MainViewModel Cleanup 🧹 Low Risk (PHASE 2)
**Goal:** Extract remaining hardcoded strings from MainViewModel

**Tasks:**
1. Identify any remaining hardcoded strings in MainViewModel
2. Determine if they should go to `MainWindowResources.resx` or `CommonResources.resx`
3. Add strings to appropriate resource files (including .de.resx and .fr.resx)
4. Update MainViewModel to reference resources via existing text ViewModels
5. Build and test main window functionality

**Files Changed:** ~4 files (resource files + translations + ViewModel)
**Commit Message:** `feat: Extract remaining strings from MainViewModel`

---

### WP10: Services 🔧 Medium Risk (PHASE 2)
**Goal:** Extract user-facing strings from services

**Services:**
- ReferenceManagementService (status messages)
- Other services with user-facing strings

**Tasks:**
1. Audit services for user-facing strings
2. Determine appropriate resource file location (likely `CommonResources.resx` or service-specific)
3. Add strings to resource files (including .de.resx and .fr.resx)
4. Update services to reference resources
5. Build and test affected functionality

**Files Changed:** ~3-6 files (resource files + translations + services)
**Commit Message:** `feat: Extract strings from services`

---

### WP11: Final Verification ✅ Testing (PHASE 2)
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

**Current Phase:** Phase 1 - Foundation Setup (Code Structure Only)  
**Current Work Package:** ✅ Phase 1 Complete  
**Overall Progress:** 3/11 Work Packages Complete (27%)

### Phase 1 Progress (Work Packages)

| WP | Name | Status | Files | Risk | Notes |
|----|------|--------|-------|------|-------|
| WP1 | Foundation Setup | ✅ Complete | 4 | ⚡ Low | ViewTexts created, MenuViewModel & AboutViewModel moved |
| WP2 | Create ALL Text ViewModels | ✅ Complete | 20 | ⚙️ Medium | All text VMs created, ViewModels updated |
| WP3 | Audit ViewModels | ✅ Complete | 1 | 📋 Docs | 64+ hardcoded strings documented |

### Phase 2 Progress (Work Packages)

| WP | Name | Status | Files | Risk | Notes |
|----|------|--------|-------|------|-------|
| WP4 | Simple Dialogs | ✅ Complete | 9-15 | ⚡ Low | Error, Comment, Confirmation + resources |
| WP5 | Settings Window | ✅ Complete | 5 | ⚙️ Medium | SettingsViewModel + resources |
| WP6 | Profile Management | ✅ Complete | 12-15 | 👥 Medium | 3 ViewModels + resources |
| WP7 | DiffDialog | ✅ Complete | 5 | ⚠️ High | 30+ strings + resources |
| WP8 | History & Updates | ✅ Complete | 12-15 | 📜 Medium | 3 ViewModels + resources |
| WP9 | MainViewModel Cleanup | ✅ Complete | 4 | 🧹 Low | Remaining strings to existing resources |
| WP10 | Services | ⏳ Pending | 3-6 | 🔧 Medium | ReferenceManagement + resources |
| WP11 | Final Verification | ⏳ Pending | 1 | ✅ Test | Build, test, verify |

### Completed Work

#### Pre-Phase 1
- ✅ MenuViewModel extraction (exists, ready to move to ViewTexts/)
- ✅ AboutViewModel extraction (exists, ready to move to ViewTexts/)
- ✅ MainWindow partial resource references
- ✅ Plan document updated with Work Packages

### Phase 1 Progress ✅ **COMPLETE**
- ✅ **WP1 Complete** - ViewTexts/ folder created, MenuViewModel and AboutViewModel moved from ViewModels/ to ViewTexts/, namespaces updated to `LoadOrderKeeper.ViewTexts`, MainViewModel and AboutWindow.xaml.cs updated with new namespace references, build successful with zero errors
- ✅ **WP2 Complete** - Created MainWindowTexts (wraps MainWindowResources), CommonTexts (wraps CommonResources), and text ViewModels for all windows: ErrorDialogTexts, CommentInputTexts, ConfirmationDialogTexts, SettingsWindowTexts, DiffDialogTexts, ManageProfilesTexts, ProfilePropertiesTexts, SwitchProfileTexts, ReferenceHistoryTexts, UpdateOptionsTexts, ViewPendingChangesTexts. Updated MainViewModel to expose MainWindowTexts and CommonTexts properties. Updated all ViewModels to use their text ViewModels (CommentInputViewModel, ErrorDialogViewModel, ConfirmationDialogViewModel, UpdateOptionsViewModel, ProfilePropertiesViewModel, ManageProfilesViewModel, ReferenceHistoryViewModel, ViewPendingChangesViewModel, SwitchProfileViewModel). Fixed XAML bindings in MainWindow for configuration error banner. Build successful with zero errors.
- ✅ **WP3 Complete** - Comprehensive audit completed. Created audit report documenting 64+ remaining hardcoded strings across SettingsViewModel (9), DiffDialogViewModel (25+), MainViewModel (10+), StatusCoordinator (3), and FileMonitoringCoordinator (3). All services and helpers verified clean. Report saved to `27-phase1-audit-report.md`.

**Phase 1 Summary**: Foundation established with 13 text ViewModels created, pattern proven, 64+ strings documented for Phase 2 extraction.

### Phase 2 Progress
- ⚠️ **REVISED**: Switched from .resx to JSON-based localization (see `31-phase2-revised-json-plan.md`)
- 📄 **Old Plan**: `28-phase2-resource-mapping.md` (resx-based, deprecated)
- 📄 **New Plan**: `31-phase2-revised-json-plan.md` (JSON-based, active)
- ✅ **WP4 Complete**: JSON infrastructure created (LocalizationService + 3 JSON files + tests)
- ✅ **WP5 Complete**: MenuViewModel & AboutViewModel migrated (32 strings)
- ✅ **WP6 Complete**: MainWindowTexts & CommonTexts migrated (19 strings)
- ✅ **WP7 Complete**: Simple Dialogs migrated - ErrorDialog, CommentInput, ConfirmationDialog (22 strings)
- ✅ **WP8 Complete**: SettingsWindowTexts migrated with all validation messages (19 strings)
- ✅ **WP9 Complete**: Profile Management - ManageProfiles, ProfileProperties, SwitchProfile (26 strings)
- 📊 **Progress**: 118 strings migrated, 11/17 sections complete (65%)
- ✅ All 45 .resx files will be DELETED after JSON migration complete
- ⏳ Ready to begin WP10 (DiffDialog - 30+ strings, most complex ViewModel)


