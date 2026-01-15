# Configuration Validation

[? Back to Overview](../README.md)

---

## Overview

The application provides comprehensive validation of configuration paths to prevent errors and guide users to correct setup with graceful error handling.

---

## Validation Order

The application validates configuration in a specific order to ensure all prerequisites are met:

1. **Paths Configured**: Both AppData and Game paths must be non-empty
2. **Paths Exist**: Both directories must exist on disk
3. **Data Folder Exists**: Game path must contain a `Data` subfolder
4. **Plugins.txt Exists**: AppData path must contain `Plugins.txt` file (cannot be auto-generated)
5. **Profiles Folder Writable**: Profiles folder must be creatable and writable

This order ensures efficient validation—each check depends on the previous ones being successful.

---

## Invalid Configuration Handling

The application uses a dual-layer approach to handle invalid configuration gracefully.

### Main Window Protection

- Non-dismissable error banner at top of main window when configuration becomes invalid
- Material Design v5 error styling with alert icon and clear error message
- "Open settings" button provides quick access to fix configuration
- Stacks above update notification banner when both present
- Automatically disappears when configuration becomes valid

### Secondary Window Protection

- All secondary windows include modal overlay protection:
  - DiffWindow
  - ManageProfilesWindow
  - ReferenceHistoryWindow
  - ViewPendingChangesWindow
  - SwitchProfileWindow
- `ConfigInvalidOverlay` user control displays when configuration invalid
- Semi-transparent dark background blocks all window interaction
- Centered Material Design card with alert icon and concise message
- Overlay automatically hides when configuration becomes valid again
- Windows remain open and preserve state during invalid configuration periods—no data loss
- `IsOperationInProgress` flag prevents overlay during active file operations

### User Experience Benefits

- Application remains open and accessible when configuration invalid
- Users can explore secondary windows even with invalid configuration
- Clear guidance directs users to main window settings to fix issues
- Automatic recovery when configuration becomes valid—seamless transition
- Non-destructive: all window state preserved throughout configuration changes

### Technical Implementation

- `ConfigurationCoordinator` tracks validation state and fires `ValidationChanged` event
- Secondary window ViewModels subscribe to validation changes via constructor injection
- `ShowOverlay` property computed as `!IsConfigValid && !IsOperationInProgress`
- Overlay uses `Grid.RowSpan` to span entire window with `Panel.ZIndex="1000"` for proper layering

---

## Main Window Error Banner

When either configured path becomes invalid, a non-dismissable error banner appears at the top of the main window.

### Characteristics

- Material Design v5 error styling (red background)
- Alert icon with clear error message
- "Open settings" button for quick access to configuration
- Stacks above update notification banner when both visible
- Automatically disappears when configuration becomes valid

### Error Message

"Path configuration error, please review the configured paths."

### Behavior

- Appears when either AppData or Game path becomes invalid
- Checks configuration on every timer tick (3-second intervals)
- Prevents operations that require valid paths while visible

---

## Settings Window Status Banner

The settings window features a permanent status banner that provides real-time validation feedback.

### Success State (green background)

- Checkmark icon
- Message: "The configured paths are valid."
- Displayed when all validation checks pass

### Error State (red background)

- Alert icon
- Specific error messages for each validation failure:
  - "The app data path is invalid."
  - "The game path is invalid."
  - "Both the game path and app data path are invalid."
  - "The game Data folder was not found."
  - "Plugins.txt not found in the app data folder."
  - "Access denied when creating the Profiles folder."
  - "The Profiles folder cannot be created or accessed."

### Validation Triggers

- When settings window opens
- When input fields lose focus (blur event)
- When user clicks "Save" button
- When user clicks auto-detected path link

### Benefits

- Immediate feedback prevents saving invalid configuration
- Clear guidance on what needs to be fixed
- No confusion about disabled UI elements
- Specific messages pinpoint the exact issue

---

## Plugins.txt Validation

The `Plugins.txt` file is a critical requirement that cannot be auto-generated.

### Why It's Required

- File created by Starfield on first game launch
- Contains the mod load order
- Application cannot function without it

### Validation Behavior

- Checked after basic path validation
- Must exist in the configured AppData path
- Configuration invalid if file missing

### User Guidance

- Error message: "Plugins.txt not found in the app data folder"
- Instructs user to run Starfield at least once
- Suggests verifying correct AppData path

### Impact When Missing

- All operations disabled
- File monitoring won't start
- Profile operations blocked
- Reference file operations blocked

---

## Profiles Folder Validation

The Profiles folder is required for storing profile data and must be writable.

### Validation Process

- Checked after Plugins.txt validation
- Attempts to create folder if it doesn't exist
- Tests writability with temporary file
- Cleans up test file automatically

### Error Scenarios

**Access Denied**: Insufficient permissions
- Message: "Access denied when creating the Profiles folder"
- Guidance: May need administrator rights or different location

**Creation Failed**: Other I/O errors
- Message: "The Profiles folder cannot be created or accessed"
- Guidance: Check permissions or select different AppData path

### Startup Validation

- Profiles folder validated via `ProfileService.EnsureProfilesFolderExists()` on startup
- Error dialog shown if folder cannot be created with option to open settings
- Profile operations validate folder existence before proceeding

### Operation-Level Validation

- Profile creation calls `ProfileService.EnsureProfilesFolderExists()`
- Profile copying validates folder before proceeding
- Consistent error handling across all profile operations

---

## Secondary Window Error Handling

When operations fail in secondary windows due to invalid configuration:

### Enhanced Error Messages

- Original error message displayed
- Appended guidance: "The likely cause is that the current configuration is invalid. Please refer to the error message in the main window to fix this."
- Centralized message stored in `Constants/UserMessages.cs` for easy maintenance

### Benefits

- Users immediately understand root cause
- Directed to main window's error banner for resolution
- Consistent messaging across all windows

---

## Validation Caching

To optimize performance and prevent excessive file system operations:

### Cached State

- `ConfigurationCoordinator` maintains validation state
- Updated only on timer ticks (every 3 seconds) and configuration changes
- Prevents I/O operations when paths known to be invalid

### Cache Update Triggers

1. File monitoring timer tick (every 3 seconds)
2. `Config` property changed (after settings save)
3. Settings dialog closes with valid configuration

### Benefits

- Prevents repeated file system checks on invalid paths
- User doesn't wait for timer tick after fixing paths
- Efficient resource usage
- Smooth UI experience

---

## Technical Implementation

### Validation in AppConfigModel.IsValid()

```
1. Check paths are non-empty strings
2. Check directories exist on disk
3. Check Game/Data folder exists
4. Check Plugins.txt exists
5. Try to create Profiles folder
6. Test Profiles folder writability
7. Return true only if all checks pass
```

### Validation in SettingsViewModel

- Builds list of specific error messages
- Shows cumulative errors in status banner
- Follows same validation order as AppConfigModel
- Provides immediate feedback on each path change

### Error Recovery

- Settings window must be used to correct invalid paths
- Application continues running with disabled features
- Clear visual feedback guides user to resolution
- Automatic recovery when paths become valid again

### Centralized Messages

All error messages stored in `Constants/UserMessages.cs`:
- `ConfigInvalidGuidance`: General configuration error guidance
- `ProfilesFolderRequired`: Profiles folder creation failure explanation
- `ProfilesFolderAccessDenied`: Permission-specific error message
- `PluginsTxtRequired`: Missing Plugins.txt guidance

---

## First-Time User Experience

For users who have never run Starfield:

### Expected Behavior

1. User installs and launches application
2. Paths auto-detected (if possible)
3. Validation fails: "Plugins.txt not found"
4. Clear error message in settings window
5. User instructed to run Starfield first
6. After running Starfield once, file created
7. Validation passes, application fully functional

### Why This Is Correct

- Application genuinely cannot function without Plugins.txt
- Clear guidance prevents user confusion
- Prevents partial functionality and obscure errors
- Ensures proper setup before first use

---

## Related Features

- **[Configuration](../configuration.md)** - Full configuration details
- **[UI Guidelines](../ui-guidelines.md)** - Error banner and overlay design patterns

---

## Technical Implementation

**Key Classes**:
- `ConfigurationCoordinator` - Validation logic and state management
- `AppConfigModel` - Configuration data model with validation
- `SettingsViewModel` - Settings window UI with real-time validation
- `ConfigInvalidOverlay` - Reusable overlay user control

**Key Components**:
- `ValidationChanged` event - Notifies ViewModels of validation state changes
- `ShowOverlay` computed property - Controls overlay visibility
- Error banner binding - Reactive UI updates based on validation state

**See Also**:
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - ConfigurationCoordinator details
- [MVVM Structure](../Architecture/mvvm-structure.md) - Event-driven validation pattern
