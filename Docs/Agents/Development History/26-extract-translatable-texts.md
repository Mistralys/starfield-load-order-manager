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

We will implement this in two phases:

#### Phase 1: Extract Existing Texts (Current Phase) - CODE CHANGES ONLY
- Create a dedicated `ViewTexts/` folder for text-providing ViewModels
- Move text-providing ViewModels (like `MenuViewModel`) to `ViewTexts/` folder
- Extract all hardcoded user-facing strings from ALL ViewModels to resource files
- Extract all user-facing strings from Services
- Future-proof all translations by extracting all strings (even if not currently translated)
- **Goal:** All user-facing strings in code should reference resource files via text ViewModels

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

### Implementation Steps - Phase 1

#### Step 1: Setup ViewTexts Folder Structure
1. Create `ViewTexts/` folder in project root
2. Move `MenuViewModel.cs` from `ViewModels/` to `ViewTexts/`
3. Update `MenuViewModel.cs` namespace to `LoadOrderKeeper.ViewTexts`
4. Update all references to `MenuViewModel` in other ViewModels
5. Move `AboutViewModel.cs` from `ViewModels/` to `ViewTexts/`
6. Update `AboutViewModel.cs` namespace to `LoadOrderKeeper.ViewTexts`
7. Update all references to `AboutViewModel` in other files

#### Step 2: Audit All ViewModels
Document all hardcoded strings in each ViewModel:
- ✅ MenuViewModel (already extracted - to be moved to ViewTexts/)
- ✅ AboutViewModel (already extracted - to be moved to ViewTexts/)
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
- MainViewModel (partial - check for remaining strings)

#### Step 3: Temporary Extraction Strategy
For ViewModels without resource files yet, temporarily add their strings to:
- `CommonResources.resx` for truly shared strings (buttons, common messages)
- `MainWindowResources.resx` for window-specific strings (as temporary holding)
- Document which strings belong to which window for Phase 2 separation

#### Step 4: Update ViewModels
- Extract hardcoded strings to resource files
- Update each ViewModel to reference resources instead of hardcoded strings
- Test each change individually
- Commit atomically to reduce merge conflicts

#### Step 5: Extract Services
- ReferenceManagementService (status messages → MainWindowResources or CommonResources)
- Other services with user-facing strings

#### Step 6: Verification
- Build and test all changes
- Verify language switching still works
- Ensure NO hardcoded user-facing strings remain in code
- Update this document with completion status

### Implementation Steps - Phase 2

#### Step 1: Create Window-Specific Resource Files
For each window/dialog without a resource file:
1. Create `.resx` file in `Resources/` folder
2. Move strings from temporary locations to appropriate resource files
3. Create `.de.resx` and `.fr.resx` translation files
4. Create corresponding text ViewModel in `ViewTexts/` folder (e.g., `ErrorDialogTexts.cs`)
5. Update ViewModels to reference new text ViewModels

#### Step 2: Identify and Extract Common Strings
- Analyze all resource files for duplicates
- Move common strings to `CommonResources.resx`
- Update text ViewModels to use common resources

#### Step 3: Final Verification
- Build and test all changes
- Verify all translations work correctly
- Update documentation

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

## Status

**Current Phase:** Phase 1 - Code-Facing Text Extraction  
**Progress:** Planning complete, ready to start implementation

### Phase 1 Progress

#### Completed
- ✅ MenuViewModel extraction (exists, needs to be moved to ViewTexts/)
- ✅ AboutViewModel extraction (exists, needs to be moved to ViewTexts/)
- ✅ MainWindow partial resource references
- ✅ Plan document updated

#### In Progress
- 🔄 Setting up ViewTexts/ folder structure

#### Pending
- ⏳ ErrorDialogViewModel audit and extraction
- ⏳ CommentInputViewModel audit and extraction
- ⏳ ConfirmationDialogViewModel audit and extraction
- ⏳ SettingsViewModel audit and extraction
- ⏳ ManageProfilesViewModel audit and extraction
- ⏳ DiffDialogViewModel audit and extraction
- ⏳ ReferenceHistoryViewModel audit and extraction
- ⏳ UpdateOptionsViewModel audit and extraction
- ⏳ SwitchProfileViewModel audit and extraction
- ⏳ ProfilePropertiesViewModel audit and extraction
- ⏳ ViewPendingChangesViewModel audit and extraction
- ⏳ MainViewModel remaining strings audit
- ⏳ Services extraction

### Phase 2 Progress
- ⏳ Not started - will begin after Phase 1 completion


