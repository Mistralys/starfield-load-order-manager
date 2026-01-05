# Starfield Load Order Keeper - Application Description

## Table of Contents
- [The Problem](#the-problem)
- [The Solution](#the-solution)
- [Technology Stack](#technology-stack)
- [Core Features](#core-features)
  - [Load Order Management](#load-order-management)
  - [Profile System](#profile-system)
  - [Change Detection](#change-detection)
  - [Dependent Change Tracking](#dependent-change-tracking)
  - [Game Integration](#game-integration)
  - [Version Check](#version-check)
- [Configuration](#configuration)
- [File Handling](#file-handling)
- [User Interface](#user-interface)

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
3. **One-Click Fix**: Restores the correct load order while preserving new mods
4. **Profile Support**: Manages multiple load orders for different characters or playthroughs
5. **Visual Diff**: Shows exactly what changed with options to accept or revert changes
6. **Dependent Change Tracking**: Intelligently groups cascading position changes for clarity
7. **Smart Confirmations**: Warns when destructive changes are about to be made
8. **Automatic Updates**: Checks for new versions and provides easy download options

---

## Technology Stack

The application is built as a **WPF .NET 9** desktop application using:

- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Design**: Material Design v5 theme with dark mode
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm
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

### Change Detection

#### Automatic Change Detection

The application periodically checks the `Plugins.txt` file on disk for changes:
- **Configurable Interval**: Default every 5 seconds, customizable in settings
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

**Status Messages**:
- Timestamped updates shown at bottom of window
- Success, warning, and error messages color-coded
- Indicates when no new changes detected

**Sorting Recommendations**:
- Special banner appears when inserted mods detected
- Warns to sort first before other changes
- Explains that inserted mods should be at the end

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

## Configuration

### Required Settings

The application requires two essential configuration settings:

1. **Starfield AppData Path**: Where `Plugins.txt` is located
2. **Starfield Game Path**: Where the `Data` folder with mods is located

### Optional Settings

3. **Change Detection Interval**: How often to check for changes (default: 5 seconds)
4. **Active Profile ID**: Currently selected profile (default: "default")

### Configuration Validation

The application validates configuration on startup:
- Paths are not empty
- Directories exist on disk
- Game `Data` folder exists

**Configuration Enforcement**: App prompts for settings and shuts down if invalid after settings dialog.

**Status Messages**: Multiple status history entries shown when settings are invalid or folders are missing.

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

**Change Badge**:
- Shows total changes including dependent changes
- Updates in real-time
- Button text: "Manage load order" or "Manage load order (X changes)"

**Sorting Recommendation**:
- Prominent banner when inserted mods detected
- Warning icon and colored text
- Explains need to sort first

### Window Types

**Modal Windows** (block parent until closed):
- Settings Window
- Switch Profile Window  
- Profile Properties Window
- Confirmation Dialog
- About Window

**Non-Modal Windows** (allow parent interaction):
- Diff Window (tracks changes, prevents duplicates)
- Manage Profiles Window (tracks instance, prevents duplicates)

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
- v1.3.0: Settings helper, dependent change grouping, confirmations, dark theme dialogs
- v1.2.0: Status message history
- v1.1.0: About dialog, always-open diff window
- v1.0.0: Initial release with profile switching
