# Starfield Load Order Keeper - Application Description

## Table of Contents
- [The Problem](#the-problem)
- [The Solution](#the-solution)
- [Technology Stack](#technology-stack)
- [Core Features](#core-features)
  - [Load Order Management](#load-order-management)
  - [Profile System](#profile-system)
  - [Reference History](#reference-history)
  - [Change Detection](#change-detection)
  - [Steam Process Detection](#steam-process-detection)
  - [Dependent Change Tracking](#dependent-change-tracking)
  - [Game Integration](#game-integration)
  - [Version Check](#version-check)
  - [Configuration Validation](#configuration-validation)
- [Configuration](#configuration)
- [File Handling](#file-handling)
- [User Interface](#user-interface)
- [Architecture & Design](#architecture--design)

---

## The Problem

The game Starfield uses a line-based text file called `Plugins.txt` in which each line contains
the file name of a mod to load when starting the game. This is typically referred to as the 
"Load Order". Once a save game has been created, it is crucial that the order of existing lines 
in the file is not modified: New lines can be added, but all previously enabled mod lines must
preserve their order.

Changing the load order in the middle of a save game can cause all manner of issues. Internal 
object references depend on the load order. For example, this means that if you are wearing a 
spacesuit that is added by a mod and that mod's position in the load order changes, you will 
lose that spacesuit.

The problem is that the game itself, as well as mod manager tools, tend to change the order 
around - hence the need for a small application to observe and fix these changes.

---

## The Solution

The application provides automated load order protection and management through:

1. **Reference File System**: Creates and maintains a reference copy of a known-good `Plugins.txt` file
2. **Automatic Detection**: Periodically monitors for unauthorized changes to the load order
3. **Steam Process Guard**: Detects when Steam is running and warns users to prevent conflicts
4. **One-Click Fix**: Restores the correct load order while preserving new mods
5. **Profile Support**: Manages multiple load orders for different characters or playthroughs
6. **Visual Diff**: Shows exactly what changed with options to accept or revert changes
7. **Dependent Change Tracking**: Intelligently groups cascading position changes for clarity
8. **Smart Confirmations**: Warns when destructive changes are about to be made
9. **Automatic Updates**: Checks for new versions and provides easy download options
10. **Configuration Validation**: Real-time validation with clear visual feedback to prevent errors
11. **Modular Architecture**: Coordinator pattern ensures maintainability and testability

---

## Technology Stack

The application is built as a **WPF .NET 9** desktop application using:

- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Design**: Material Design v5 theme with dark mode
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm with Coordinator pattern
- **Coordinators**: Modular domain logic handlers for file monitoring, status, updates, profiles, configuration, and game launching
- **Testing**: xUnit for unit tests
- **Dialogs**: Custom Material Design confirmation dialogs

---

## Core Features

### Load Order Management

#### Working Principle

1. **Reference Creation**: When the user is satisfied with their `Plugins.txt` file, they create a reference copy
2. **Automatic Sorting**: The tool loads the reference file and sorts all entries according to the reference order
3. **New Mod Handling**: Any new mod files are automatically appended at the end
4. **Periodic Monitoring**: Continuous background checking for external changes to the load order
5. **One-Click Restoration**: Quick fix button to restore the correct order instantly
6. **Status History**: Recent operations tracked and displayed for easy reference

#### File Name Case Handling

Many mod manager tools lowercase all mod file names. While Starfield doesn't mind this, it will restore 
the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the application:
- Cross-references mod names with all `.esp` and `.esm` files in the game's `Data` folder
- Always uses the original file name case from disk
- Maintains consistency even if mod managers lowercase names
- Searches recursively through nested folders in the `Data` directory

#### Handling Disabled Mods

All valid mod lines start with the character `*`. Any lines that do not start with this character 
are considered disabled by Starfield. **A disabled mod is functionally equivalent to a missing mod**.

From the application's point of view, to keep the logic as simple as possible, these lines are treated 
as if they did not exist.

> **NOTE**: This means that saving changes to the `Plugins.txt` file will remove any disabled mod lines 
> from the file. This has no adverse effect on the game, as Starfield ignores these lines anyway.

---

### Profile System

The profile system allows users to maintain separate load orders for different characters or playthroughs.

#### Overview

- **Default Profile**: Always available, automatically created, cannot be deleted
- **Custom Profiles**: Create unlimited profiles with unique labels and descriptions
- **Quick Switching**: One-click profile switching with state preservation
- **Profile Management**: Add, edit, delete, and copy profiles through dedicated UI

#### Profile Structure

Each profile maintains its own isolated state:

**Folder Structure**:
```
AppData/Starfield/Profiles/
  ├── default/
  │   ├── main.txt          (current state)
  │   └── reference.txt     (known-good state)
  ├── my-character/
  │   ├── profile.json      (metadata)
  │   ├── main.txt
  │   └── reference.txt
  └── another-character/
      ├── profile.json
      ├── main.txt
      └── reference.txt
```

**Profile Files**:
1. **`profile.json`**: Stores profile metadata (label, description)
2. **`main.txt`**: Current `Plugins.txt` state for this profile
3. **`reference.txt`**: Known-good reference state for change detection

#### Profile Properties

- **ID**: Auto-generated from label (lowercase, dash-separated, ASCII-only with numeric suffixes for uniqueness)
- **Label**: User-facing name (2-30 characters, unique, case-insensitive, cannot be "Default")
- **Description**: Optional description (max 500 characters)

#### Profile Switching

When switching profiles:
1. Current `Plugins.txt` is backed up to the active profile's `main.txt`
2. Target profile's `main.txt` is copied to `Plugins.txt`
3. Active profile ID is updated in configuration
4. Change detection automatically uses the new profile's reference file

**No data loss**: The current state is always preserved, even if there are unsaved changes.

#### Default Profile

- **Virtual Profile**: No `profile.json` file, but otherwise behaves like any profile
- **Always Available**: Automatically created on first use
- **Immutable Properties**: Cannot rename or change description
- **Cannot Delete**: Ensures users always have at least one profile
- **Auto-Recreation**: Automatically restored if manually deleted

#### Profile UI Features

**Active Profile Display**:
- Shown below the menu bar in the main window
- Clickable to open profile switcher

**Profile Menu**:
- Positioned between "File" and "Edit"
- Quick access to switch and manage profiles

**Switch Profile Window**:
- Card-based interface with visual feedback
- Hover effects for easy selection
- Active profile indicated with checkmark icon
- One-click switching

**Manage Profiles Window**:
- Non-modal window allowing interaction with main application
- ListView showing all custom profiles
- Add, edit, delete, and copy operations
- Context menu for quick actions
- Double-click to edit

**Profile Properties Window**:
- Create or edit profile metadata
- Real-time validation with Material Design error display
- Shared between create and edit modes
- Prevents duplicate labels

---

### Reference History

The application maintains a version history of reference file updates, allowing users to track changes over time and rollback to previous states if needed.

#### Overview

- **Automatic Versioning**: Each reference update creates a new version
- **Change Tracking**: Records what mods were added/removed in each version
- **User Comments**: Optional comments to describe what changed
- **Rollback Support**: Restore previous reference states
- **Per-Profile History**: Each profile maintains its own independent history
- **Version Limit**: Keeps last 16 versions, automatically pruning older ones

#### Version Storage

Each version is stored in the profile's `History` folder:

**Folder Structure**:
```
Profiles/{profileId}/
  ├── main.txt
  ├── reference.txt
  ├── pending-changes.json
  └── History/
      ├── reference_v1.txt       (archived reference)
      ├── reference_v1.json      (version metadata)
      ├── reference_v2.txt
      ├── reference_v2.json
      └── ...
```

**Version Metadata** (`reference_vX.json`):
```json
{
  "versionNumber": 2,
  "timestamp": "2025-01-05T14:30:00",
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp", "ModY.esp"],
  "removedMods": []
}
```

#### How It Works

**Pending Changes System**:
1. User makes changes to load order
2. User accepts changes → current changes stored as "pending"
3. Reference file updated to match current state
4. On next update, previous pending changes are recorded in history
5. Current changes become new pending changes

**Example Flow**:
```
Update 1: Add ModX
  → Archive: Version 1 "Initial version" (no changes)
  → Store pending: {Added: [ModX]}

Update 2: Add ModY
  → Archive: Version 2 "Added ModX" 
  → Store pending: {Added: [ModY]}

Update 3: Remove ModX
  → Archive: Version 3 "Added ModY"
  → Store pending: {Removed: [ModX]}
```

This approach ensures each version accurately describes what changed when creating that version.

#### Version Information

Each version displays:
- **Version Number**: Sequential numbering starting at 1
- **Date & Time**: User-friendly timestamps
  - Today: "Today 14:56"
  - Yesterday: "Yesterday 16:41"
  - This year: "Jan 15 14:56"
  - Previous years: "Dec 25, 2023 14:56"
- **Changes**: Total number of mods added + removed
- **Summary**: Human-readable change description

**Summary Display**:
- **User comment** (italic, optional): Personal notes about the update
- **Change details**: Lists of added/removed mods
  - ≤3 mods: Shows names (e.g., "Added ModX and ModY")
  - \>3 mods: Shows count (e.g., "Added 5 mods")
- Text wraps for long content
- Multiple lines for readability

#### Reference History Window

A dedicated non-modal window for managing version history:

**Features**:
- DataGrid showing all versions (newest first)
- Sortable columns with enhanced headers
- Text wrapping in summary column
- Horizontal grid lines for clarity
- Context menu for quick actions
- Real-time updates when new versions created

**Actions**:
- **Rollback** (button + context menu + double-click):
  - Replaces `Plugins.txt` with archived version
  - Opens diff window to review before accepting
  - Shows confirmation with version details
- **Edit Comment** (context menu):
  - Modify version comments after creation
  - Opens dialog with existing comment pre-filled
  - Updates immediately in history
- **Delete Version** (context menu):
  - Removes specific version from history
  - Shows confirmation warning
  - Cannot be undone
- **Clear All History** (menu + button):
  - Deletes entire version history
  - Shows confirmation warning
  - Does not affect current reference

**Menu Bar**:
- **File** → Exit: Close window
- **Edit** → Clear all history: Delete all versions

**Window Behavior**:
- Non-modal: Can interact with main window while open
- Single instance: Prevents duplicate windows
- Auto-refresh: Updates when new versions created
- Dynamic updates: Reflects changes from main window

#### Comment Dialog

Optional comment input when updating reference:

**Features**:
- Multi-line text input (max 500 characters)
- Material Design styled with proper text colors
- OK/Cancel buttons with proper event handling
- Reusable for creating and editing comments

**Behavior**:
- Cancel aborts the reference update (no version created)
- Empty comment allowed (defaults to "Initial version" for first update)
- Comment appears in italic in history window
- Editable after creation via context menu

#### Automatic Migration

For existing installations without history:

**On-Demand Creation**:
- When history is empty and no pending changes exist
- Creates "Initial version" automatically
- Archives current reference state
- Transparent to user (no special indication)
- Works per-profile independently

**Benefits**:
- Seamless upgrade experience
- No manual migration needed
- Handles external file changes
- Restores history if manually deleted

#### Version Limits

- **Maximum Versions**: 16 per profile
- **Auto-Pruning**: Oldest versions deleted when limit exceeded
- **Prune Timing**: After each new version created
- **Sort Order**: Oldest versions (lowest numbers) pruned first

#### Technical Details

**File Encoding**:
- All files stored in UTF-8 without BOM
- Consistent with main file handling

**Storage Location**:
- Per-profile: `Profiles/{profileId}/History/`
- Profile-specific: Independent histories per profile
- Pending changes: `Profiles/{profileId}/pending-changes.json`

**Error Handling**:
- Archive failures show warning but continue update
- Load failures return empty history
- Corrupted files silently ignored
- Missing folders automatically created

---

### Change Detection

#### Automatic Change Detection

The application periodically checks the `Plugins.txt` file on disk for changes:
- **Fixed Interval**: Checks every 3 seconds (foundational value optimized through testing)
- **Signature Tracking**: Detects changes without re-reading the entire file
- **Profile-Aware**: Automatically uses the active profile's reference file
- **Smart Updates**: Only refreshes diff window when actual changes detected

#### Types of Changes Detected

1. **Moved Mods**: Position in load order has changed
2. **Added Mods**: New mods not present in reference file, appended at the end
3. **Inserted Mods**: New mods added in the middle of the load order
4. **Removed Mods**: Mods in reference but missing from current file
5. **Replaced Mods**: New mod replacing a removed mod's position
6. **Unchanged Mods**: Mods at correct positions

Each mod is assigned a numerical position based on its line number (starting at 1). The application 
compares both mod names and positions to determine the type of change.

#### Change Count Display

- Main window shows total number of changes (including dependent changes)
- Badge updates automatically as changes are detected or resolved
- Button text dynamically displays change count: "Manage load order (X changes)"

---

### Steam Process Detection

The application includes intelligent Steam process detection to help prevent conflicts when managing load orders.

#### Why Steam Detection Matters

Steam can interfere with load order management in several ways:
- May lock files when the game is running
- Can trigger automatic updates that modify game files
- Cloud sync features might conflict with load order changes
- Better to make changes when Steam is fully closed

#### How It Works

**Automatic Detection**:
- Runs on the same 3-second interval as file monitoring
- Checks for Steam installation via Windows registry
- Detects if steam.exe process is currently running
- Updates warning state automatically when Steam starts/stops

**Visual Warning**:
- Persistent warning banner appears in main window when Steam is running
- Shows clear icon and warning message
- Provides helpful tooltip: "Steam is running. To prevent conflicts, it is recommended to close Steam before making changes to the load order."
- Warning automatically disappears when Steam closes

**Benefits**:
- Prevents potential file conflicts and corruption
- Reduces risk of lost changes due to Steam sync
- Clear visual feedback helps users avoid problems
- Non-intrusive—doesn't block operations, just warns

**Technical Details**:
- Steam installation detected via registry keys (HKEY_CURRENT_USER and HKEY_LOCAL_MACHINE)
- Process detection uses `Process.GetProcessesByName("steam")` for efficiency
- Detection is part of the FileMonitoringCoordinator for optimal performance
- Warning state managed through event-driven architecture

---

### Dependent Change Tracking

When a mod is removed or inserted in the middle of the load order, all mods below it shift positions.
The application intelligently groups these cascading changes to avoid clutter.

#### How It Works

**Detection**:
- Removed and inserted mods are identified as "parent" changes
- All subsequent "Moved" mods are tracked as dependent changes
- Grouping stops when a non-moved mod is encountered

**Display**:
- Collapsed by default with summary: "+ X mod positions affected by this change"
- Click summary to expand/collapse dependent changes
- Visual hierarchy shows relationship between parent and dependent changes
- Dependent changes are indented and visually distinct

**Benefits**:
- Reduces visual noise in diff window
- Quickly identifies root cause of position changes
- Easy to understand cascading effects
- Improves decision-making when reviewing changes

**Example**:
```
- ModA (Removed from line 5)
  + 10 mod positions affected by this change
    [Click to expand/collapse]
```

When expanded:
```
- ModA (Removed from line 5)
  ~ ModB (5→4)
  ~ ModC (6→5)
  ~ ModD (7→6)
  ...
```

---

### Managing Changes: The DIFF Window

A dedicated non-modal window shows and manages detected changes:

**Features**:
- Visual diff showing all changes with color coding and prefixes
- Line-by-line comparison with reference and current position numbers
- Sorting recommendation banner when order changes detected
- Real-time diff updates as changes are resolved
- Collapsible dependent change groups
- Scrolls to first change automatically

**Action Buttons**:
- **Update Reference**: Accept current state as new reference
  - Shows ellipsis ("...") to indicate confirmation dialog
  - Warns when removed or inserted mods detected
  - Shows total affected mods including dependent changes
- **Fix Load Order**: Restore correct order from reference
- **Discard Changes**: Revert `Plugins.txt` to reference state
  - Shows ellipsis ("...") to indicate confirmation dialog
  - Displays detailed breakdown of changes to be discarded
  - Warns that action cannot be undone

**Change Resolution**:
- **Re-enable removed mods**: Right-click context menu on removed mod
- **Remove new mods**: Right-click context menu on added mod
- **Replace old with new**: Right-click on removed mod to replace with added mod
- **Expand/collapse dependent changes**: Click on dependency summary line

**Replacement Workflow Notes**:
When working with multiple mod replacements in a single session, be aware that the application compares the current state against the reference file on disk. This means:

- **Option 1**: Click "Accept changes" after each replacement to make it permanent before making the next replacement
- **Option 2**: Make all your replacements together, then click "Accept changes" once to accept all changes

The app shows an informational banner when multiple removals or replacements are detected to remind you of these options. This is the intended behavior—replacements are temporary change resolution actions until explicitly accepted.

**Status Messages**:
- Timestamped updates shown at bottom of window
- Success, warning, and error messages color-coded
- Indicates when no new changes detected

**Sorting Recommendations**:
- Prominent banner to show when there are mods that have shifted position but only under the condition that they are not part of any dependent change lists (because those are not affected by sorting anyway).
- Warning icon and colored text
- Explains need to sort first

---

### Game Integration

#### Play Button

Launches the game directly from the application:
- **SFSE Detection**: Automatically uses Starfield Script Extender if installed
- **Fallback**: Uses vanilla executable if SFSE not found
- **Smart Detection**: Checks for `sfse_loader.exe` presence
- **Dynamic Label**: Shows "Play (SFSE)" or "Play (Vanilla)" based on detection

#### Utility Functions

Quick access to important files and folders via File menu:
- **Open Plugins.txt**: Opens current plugins file in default text editor
- **Open Reference File**: Opens active profile's reference file
- **Open AppData Folder**: Opens Starfield's AppData directory
- **Open Game Folder**: Opens game installation directory

#### About Dialog

- Shows application name and version (clean semantic versioning)
- Copyright information with dynamic year
- Application description
- Link to project homepage on GitHub
- Material Design styled with dark theme

---

### Version Check

The application automatically checks for updates and notifies users when new versions are available.

#### Automatic Update Check

**Background Check**:
- Runs silently when application starts
- Checks GitHub API for latest release
- Compares current version with latest stable release
- Uses 24-hour caching to avoid excessive API calls
- Fails silently if network unavailable

**Smart Version Comparison**:
- Uses semantic versioning (Major.Minor.Patch)
- Ignores pre-release versions (beta, rc, etc.)
- Only notifies for newer stable releases
- Handles version downgrades correctly (no notification)

**Update Notification**:
- Non-intrusive info bar appears at top of main window
- Shows update message: "Version X.X.X is available!"
- Provides "Download options..." button
- Can be dismissed for current session
- Reappears on next app launch if update still available

#### Manual Update Check

**Check for Updates Menu**:
- Located in Help menu for easy access
- Bypasses 24-hour cache for immediate check
- Shows success dialog if already on latest version
- Displays download options dialog if update available

**Download Options Dialog**:
- Material Design styled with prominent buttons
- Shows current and latest version numbers
- Two download sources with clickable buttons:
  - **Nexusmods**: Primary distribution platform
  - **GitHub Releases**: Alternative source
- Opens selected download page in default browser
- Closes automatically after selection

#### Error Handling

**Network Failures**:
- Background check fails silently (no user disruption)
- Manual check shows download options dialog
- Explains inability to check automatically
- Still provides access to download pages

**GitHub API Rate Limits**:
- Cached results prevent hitting rate limits
- 24-hour cache duration balances freshness and API limits
- Unauthenticated requests (no token required)
- Suitable for small to medium user base

#### Technical Details

**GitHub API Integration**:
- Queries: `https://api.github.com/repos/Mistralys/starfield-load-order-manager/releases/latest`
- 10-second timeout for network requests
- Parses release tag name for version number
- Checks `prerelease` flag to filter beta versions

**Caching System**:
- Cache file: `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\update-check-cache.json`
- Stores timestamp and last check result
- 24-hour expiration
- Survives application restarts

**Version Source**:
- Current version from assembly attributes via `VersionService`
- Latest version from GitHub release tag name
- Strips commit hashes and extra metadata
- Clean semantic version format (e.g., "1.4.0")

**Download Locations**:
- **Nexusmods**: https://www.nexusmods.com/starfield/mods/15786
- **GitHub Releases**: https://github.com/Mistralys/starfield-load-order-manager/releases
- URLs stored as constants for easy maintenance
- Both options presented equally in download dialog

---

### Configuration Validation

The application provides comprehensive validation of configuration paths to prevent errors and guide users to correct setup.

#### Validation Order

The application validates configuration in a specific order to ensure all prerequisites are met:

1. **Paths Configured**: Both AppData and Game paths must be non-empty
2. **Paths Exist**: Both directories must exist on disk
3. **Data Folder Exists**: Game path must contain a `Data` subfolder
4. **Plugins.txt Exists**: AppData path must contain `Plugins.txt` file (cannot be auto-generated)
5. **Profiles Folder Writable**: Profiles folder must be creatable and writable

This order ensures efficient validation—each check depends on the previous ones being successful.

#### Main Window Error Banner

When either configured path becomes invalid, a non-dismissable error banner appears at the top of the main window:

**Characteristics**:
- Material Design v5 error styling (red background)
- Alert icon with clear error message
- "Open settings" button for quick access to configuration
- Stacks above update notification banner when both visible
- Automatically disappears when configuration becomes valid

**Error Message**:
- "Path configuration error, please review the configured paths."

**Behavior**:
- Appears when either AppData or Game path becomes invalid
- Checks configuration on every timer tick (3-second intervals)
- Prevents operations that require valid paths while visible
- UI elements disabled when paths invalid

#### Settings Window Status Banner

The settings window features a permanent status banner that provides real-time validation feedback:

**Success State** (green background):
- Checkmark icon
- Message: "The configured paths are valid."
- Displayed when all validation checks pass

**Error State** (red background):
- Alert icon
- Specific error messages for each validation failure:
  - "The app data path is invalid."
  - "The game path is invalid."
  - "Both the game path and app data path are invalid."
  - "The game Data folder was not found."
  - "Plugins.txt not found in the app data folder."
  - "Access denied when creating the Profiles folder."
  - "The Profiles folder cannot be created or accessed."

**Validation Triggers**:
- When settings window opens
- When input fields lose focus (blur event)
- When user clicks "Save" button
- When user clicks auto-detected path link

**Benefits**:
- Immediate feedback prevents saving invalid configuration
- Clear guidance on what needs to be fixed
- No confusion about disabled UI elements
- Specific messages pinpoint the exact issue

#### Plugins.txt Validation

The `Plugins.txt` file is a critical requirement that cannot be auto-generated:

**Why It's Required**:
- File created by Starfield on first game launch
- Contains the mod load order
- Application cannot function without it

**Validation Behavior**:
- Checked after basic path validation
- Must exist in the configured AppData path
- Configuration invalid if file missing

**User Guidance**:
- Error message: "Plugins.txt not found in the app data folder"
- Instructs user to run Starfield at least once
- Suggests verifying correct AppData path

**Impact When Missing**:
- All operations disabled
- File monitoring won't start
- Profile operations blocked
- Reference file operations blocked

#### Profiles Folder Validation

The Profiles folder is required for storing profile data and must be writable:

**Validation Process**:
- Checked after Plugins.txt validation
- Attempts to create folder if it doesn't exist
- Tests writability with temporary file
- Cleans up test file automatically

**Error Scenarios**:
1. **Access Denied**: Insufficient permissions
   - Message: "Access denied when creating the Profiles folder"
   - Guidance: May need administrator rights or different location
2. **Creation Failed**: Other I/O errors
   - Message: "The Profiles folder cannot be created or accessed"
   - Guidance: Check permissions or select different AppData path

**Startup Validation**:
- Profiles folder validated via `ProfileService.EnsureProfilesFolderExists()` on startup
- Error dialog shown if folder cannot be created with option to open settings or shutdown
- Profile operations validate folder existence before proceeding

**Operation-Level Validation**:
- Profile creation calls `ProfileService.EnsureProfilesFolderExists()`
- Profile copying validates folder before proceeding
- Consistent error handling across all profile operations

#### Secondary Window Error Handling

When operations fail in secondary windows (Profile Management, Changes Window, etc.) due to invalid configuration:

**Enhanced Error Messages**:
- Original error message displayed
- Appended guidance: "The likely cause is that the current configuration is invalid. Please refer to the error message in the main window to fix this."
- Centralized message stored in `Constants/UserMessages.cs` for easy maintenance

**Benefits**:
- Users immediately understand root cause
- Directed to main window's error banner for resolution
- Consistent messaging across all windows

#### Validation Caching

To optimize performance and prevent excessive file system operations:

**Cached State**:
- `MainViewModel` maintains `_configIsInvalid` field
- Updated only on timer ticks (every 3 seconds)
- Prevents I/O operations when paths known to be invalid

**Cache Update Triggers**:
1. File monitoring timer tick (every 3 seconds)
2. `Config` property changed (after settings save)
3. Settings dialog closes with valid configuration

**Benefits**:
- Prevents repeated file system checks on invalid paths
- User doesn't wait for timer tick after fixing paths
- Efficient resource usage
- Smooth UI experience

#### Technical Implementation

**Validation in AppConfigModel.IsValid()**:
```
1. Check paths are non-empty strings
2. Check directories exist on disk
3. Check Game/Data folder exists
4. Check Plugins.txt exists
5. Try to create Profiles folder
6. Test Profiles folder writability
7. Return true only if all checks pass
```

**Validation in SettingsViewModel**:
- Builds list of specific error messages
- Shows cumulative errors in status banner
- Follows same validation order as AppConfigModel
- Provides immediate feedback on each path change

**Error Recovery**:
- Settings window must be used to correct invalid paths
- Application continues running with disabled features
- Clear visual feedback guides user to resolution
- Automatic recovery when paths become valid again

**Centralized Messages**:
All error messages stored in `Constants/UserMessages.cs`:
- `ConfigInvalidGuidance`: General configuration error guidance
- `ProfilesFolderRequired`: Profiles folder creation failure explanation
- `ProfilesFolderAccessDenied`: Permission-specific error message
- `PluginsTxtRequired`: Missing Plugins.txt guidance

#### First-Time User Experience

For users who have never run Starfield:

**Expected Behavior**:
1. User installs and launches application
2. Paths auto-detected (if possible)
3. Validation fails: "Plugins.txt not found"
4. Clear error message in settings window
5. User instructed to run Starfield first
6. After running Starfield once, file created
7. Validation passes, application fully functional

**Why This Is Correct**:
- Application genuinely cannot function without Plugins.txt
- Clear guidance prevents user confusion
- Prevents partial functionality and obscure errors
- Ensures proper setup before first use

---

## Configuration

### Required Settings

The application requires two essential configuration settings:

1. **Starfield AppData Path**: Where `Plugins.txt` is located
2. **Starfield Game Path**: Where the `Data` folder with mods is located

### Optional Settings

3. **Active Profile ID**: Currently selected profile (default: "default")

### Configuration Validation

The application validates configuration on startup:
- Paths are not empty
- Directories exist on disk
- Game `Data` folder exists
- **Plugins.txt file exists** (required, cannot be auto-generated by application)
- **Profiles folder can be created and is writable** (tested with temporary file)
- Continuous validation during runtime via timer ticks
- Real-time feedback in settings window

**Validation Order**: paths configured → paths exist → Data folder exists → Plugins.txt exists → Profiles folder writable

**Configuration Enforcement**: App prompts for settings and shuts down if invalid after settings dialog.

**Visual Feedback**:
- Error banner in main window when paths invalid
- Status banner in settings window with success/error states
- Disabled UI elements when configuration invalid
- Specific error messages for each validation failure

**Status Messages**: Multiple status history entries shown when settings are invalid or folders are missing.

**Startup Validation**:
- Profiles folder validated via `ProfileService.EnsureProfilesFolderExists()` on startup
- Error dialog shown if folder cannot be created with option to open settings or shutdown
- Profile operations validate folder existence before proceeding

### Auto-Discovery

Common installation paths are checked and pre-filled when found:
- **Steam**: Auto-detected using Steam library folders
- **AppData**: `%LOCALAPPDATA%\Starfield`
- Detected paths shown as clickable links in settings window

> **NOTE**: Installations vary between gaming platforms (Steam, GOG, Microsoft Store, etc.), so auto-discovery 
> may not always work. Users can manually browse to the correct folders using the "Browse..." buttons.

#### Steam Library Detection

The application includes intelligent Steam library detection to locate Starfield installations:

**How It Works**:
1. Detects the main Steam installation path from Windows registry
2. Reads Steam's `libraryfolders.vdf` configuration file
3. Searches all configured Steam library folders for Starfield (AppID: `1716740`)
4. Returns the first valid installation found

**Benefits**:
- Automatically finds Starfield even when installed in non-default Steam libraries
- No manual configuration needed for most Steam users
- Handles multiple Steam library folders seamlessly
- Validates installation by checking for the `Data` folder

**Fallback Behavior**:
- If library detection fails, checks default Steam installation location
- Falls back to default Program Files location if needed
- Silent failure ensures no disruption to user experience

**Technical Details**:
- Uses Valve Data Format (VDF) parser (Gameloop.Vdf library)
- Parses numeric library keys (0, 1, 2, ...) from `libraryfolders.vdf`
- Checks each library's `apps` property for Starfield's AppID
- Constructs path: `{library-path}/steamapps/common/Starfield`
- Normalizes paths (converts forward slashes to backslashes)
- Validates installation completeness before returning path

This feature significantly improves the first-run experience for Steam users who have Starfield installed in custom library locations.

---

## File Handling

### Example `Plugins.txt`

See the file [example-plugins.txt](./example-plugins.txt) for an example of a `Plugins.txt` file.

### File Encoding

The `Plugins.txt` file must be encoded in **UTF-8 without BOM** (Byte Order Mark):
- Application reads and writes in this format
- BOM causes Starfield to ignore the first line of the file
- All file writes use UTF-8 without BOM explicitly

### Whitespace Handling

**Reading**:
- Leading and trailing whitespace on each line is trimmed
- Empty lines at the end of file are ignored
- Comment lines (starting with `#`) are ignored

**Writing**:
- No leading or trailing whitespace is added
- UTF-8 without BOM encoding
- Only enabled mods are written (no disabled lines or comments)

### Reference Files

**Legacy System** (pre-profiles):
- Single `Plugins.reference.txt` in AppData folder (no longer used)

**Profile System** (current):
- Each profile has its own `reference.txt` in `Profiles/{profile-id}/`
- Automatically created from `main.txt` when missing
- Used for change detection and sort order
- Copied raw (preserving comments) when created

### Profile Storage

**Profile Metadata** (`profile.json`):
```json
{
  "label": "My Character",
  "description": "Main playthrough character"
}
```

**Note**: Profile ID is not stored in JSON - it's derived from the folder name to prevent sync issues.

**Profile ID Generation**:
- Transliterated from label (accented chars → ASCII equivalents)
- Lowercase, dash-separated
- Numeric suffix added if duplicate (`my-profile`, `my-profile-1`, `my-profile-2`)
- Falls back to `profile` if label contains only non-ASCII chars

**Pending Changes** (`pending-changes.json`):
```json
{
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp"],
  "removedMods": ["ModY.esp"]
}
```

**Note**: Stores comment and changes made since last reference update. The comment describes the changes being accepted and is archived with the next version when the reference is updated again.

### Version History Storage

**History Structure**:
```
Profiles/{profile-id}/History/
  ├── reference_v1.txt      # Archived reference file
  ├── reference_v1.json     # Version metadata
  ├── reference_v2.txt
  ├── reference_v2.json
  └── ...
```

**Version Metadata** (`reference_vX.json`):
```json
{
  "versionNumber": 2,
  "timestamp": "2025-01-05T14:30:00.123",
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp", "ModY.esp"],
  "removedMods": []
}
```

**Metadata Properties**:
- `versionNumber`: Sequential version number (starts at 1)
- `timestamp`: ISO 8601 format timestamp
- `comment`: Optional user comment (null if empty)
- `addedMods`: Mods added when creating this version
- `removedMods`: Mods removed when creating this version

**Storage Rules**:
- Maximum 16 versions per profile
- Oldest versions automatically pruned
- All files UTF-8 without BOM encoding
- Per-profile isolation (independent histories)

---

## User Interface

### Design Principles

The application follows **Material Design v5** guidelines with:
- **Dark mode theme** by default (Lime primary, LightGreen secondary)
- **Semantic color brushes** for theme consistency
- **Elevated surfaces** with shadows for depth
- **Rounded corners** (8px) for modern aesthetic
- **Responsive layouts** with proper spacing and padding

### Dialogs & Confirmations

**Custom Material Design Dialogs**:
- Replaced all system `MessageBox` calls with custom styled dialogs
- Support for multiple icon types (Information, Question, Warning, Error)
- Color-coded icons (Blue, Purple, Orange, Red)
- Multiple button configurations (OK, OKCancel, YesNo, YesNoCancel)
- Transparent backgrounds with rounded corners and elevation shadows
- Scrollable message area for long text

**Confirmation Patterns**:
- Destructive actions show warning icon and detailed impact
- Default to "No" for safety
- Ellipsis in button labels indicates dialog will appear
- Clear action descriptions with bullet-point summaries

### Button Conventions

- **Raised buttons**: Primary actions (Save, Create, Update)
- **Outlined buttons**: Secondary actions (Cancel, Browse, Close)
- **Flat buttons**: Tertiary/inline actions (detected paths)
- **Ellipsis suffix**: Indicates dialog or additional interaction

### Status Indicators

**Main Window**:
- Current status message with icon/color coding
- Last 3 status messages in history list
- Timestamps for all status entries
- Color coding: Info (Primary), Success (Tertiary), Warning (Secondary), Error (Error)
- Configuration error banner when paths invalid
- Update notification info bar when new version available
- Steam process warning banner when Steam is running

**Settings Window**:
- Status banner showing validation state
- Error state: Specific messages about invalid paths
- Success state: Confirmation that paths are valid
- Cannot be dismissed (always visible for feedback)

**Change Badge**:
- Shows total changes including dependent changes
- Updates in real-time
- Button text: "Manage load order" or "Manage load order (X changes)"

**Sorting Recommendation**:
- Prominent banner to show when there are mods that have shifted position but only under the condition that they are not part of any dependent change lists (because those are not affected by sorting anyway).
- Warning icon and colored text
- Explains need to sort first

**Steam Warning**:
- Shows when Steam process is detected running
- Orange warning banner with helpful tooltip
- Explains potential conflicts
- Automatically disappears when Steam closes

### Window Types

**Modal Windows** (block parent until closed):
- Settings Window
- Switch Profile Window  
- Profile Properties Window
- Confirmation Dialog
- About Window
- Update Options Dialog
- Comment Input Dialog

**Non-Modal Windows** (allow parent interaction):
- Diff Window (tracks changes, prevents duplicates)
- Manage Profiles Window (tracks instance, prevents duplicates)
- Reference History Window (tracks instance, prevents duplicates, auto-refreshes)

### Accessibility Features

- Keyboard navigation support (`IsCancel`, `IsDefault` properties)
- Clear visual hierarchy with color and typography
- Hover effects on interactive elements
- Descriptive button labels and tooltips
- Design-time attributes for XAML designer support

---

## Version History

The application maintains semantic versioning:
- Version displayed in window title and About dialog
- Retrieved from assembly attributes via `VersionService`
- Commit hashes stripped for clean display (e.g., "1.3.0" not "1.3.0+abc123")
- Copyright year updates automatically

**Recent Major Features**:
- v1.5.0: Configuration validation with error banners and real-time feedback
- v1.4.0: Reference history with versioning, rollback, and comment support
- v1.3.0: Settings helper, dependent change grouping, confirmations, dark theme dialogs
- v1.2.0: Status message history
- v1.1.0: About dialog, always-open diff window
- v1.0.0: Initial release with profile switching

---

## Architecture & Design

### Coordinator Pattern

The application uses a **coordinator pattern** to separate concerns and improve maintainability:

#### What Are Coordinators?

Coordinators are specialized components that handle specific domain logic and state management. Each coordinator:
- Inherits from `CoordinatorBase` (provides `INotifyPropertyChanged` + `IDisposable`)
- Has a single, well-defined responsibility
- Communicates via events and property changes
- Is independently testable
- Can be reused across ViewModels

#### Implemented Coordinators

**FileMonitoringCoordinator** (~300 lines):
- Periodic file monitoring (3-second intervals)
- Change detection and counting
- Steam process detection and warnings
- Sorting recommendations

**StatusCoordinator** (~80 lines):
- Status message management
- History tracking (last 3 messages)
- Timestamped message formatting

**UpdateCheckCoordinator** (~120 lines):
- Background and manual update checking
- Version comparison with caching
- Update notification management

**ProfileCoordinator** (~150 lines):
- Active profile state management
- Profile switching coordination
- Profile change events

**ConfigurationCoordinator** (~180 lines):
- Configuration validation with caching
- Error banner state management
- Detailed validation results

**GameLauncherCoordinator** (~150 lines):
- SFSE (Script Extender) detection
- Game launching
- Dynamic play button text

**WindowManager** (~200 lines):
- Window lifecycle management
- Duplicate window prevention
- Instance tracking

#### Benefits

**Testability**: Each coordinator can be unit tested independently with mocked dependencies.

**Maintainability**: Changes are localized to specific coordinators, reducing risk of breaking other features.

**Reusability**: Coordinators can be shared across multiple ViewModels for consistent behavior.

**Clarity**: MainViewModel reduced from ~1300 lines to ~900 lines (31% reduction) by extracting domain logic.

**Scalability**: New features can be added as new coordinators following established patterns.

#### Communication Pattern

Coordinators communicate with ViewModels through:
1. **Properties**: Exposed for UI binding via pass-through properties in ViewModels
2. **Events**: Custom events (e.g., `ProfileChanged`, `ValidationChanged`) notify ViewModels of state changes
3. **Methods**: Public methods provide operations (e.g., `CheckForUpdatesAsync()`, `SwitchProfileAsync()`)

Example flow:
```
User clicks "Check for Updates"
  → MainViewModel.CheckForUpdatesCommand
    → UpdateCheckCoordinator.CheckForUpdatesManualAsync()
      → UpdateCheckCoordinator.UpdateAvailable property changes
        → PropertyChanged event fires
          → MainViewModel.OnPropertyChanged(nameof(UpdateAvailable))
            → UI updates automatically
```

This architecture ensures clean separation of concerns while maintaining responsive UI updates through event-driven communication.
