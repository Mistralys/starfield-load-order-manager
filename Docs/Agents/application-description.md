# Starfield Load Order Keeper - Application Description

## Table of Contents
- [The Problem](#the-problem)
- [The Solution](#the-solution)
- [Technology Stack](#technology-stack)
- [Core Features](#core-features)
  - [Load Order Management](#load-order-management)
  - [Profile System](#profile-system)
  - [Change Detection](#change-detection)
  - [Game Integration](#game-integration)
- [Configuration](#configuration)
- [File Handling](#file-handling)

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

---

## Technology Stack

The application is built as a **WPF .NET 9** desktop application using:

- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Design**: Material Design theme with dark mode
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm
- **Testing**: xUnit for unit tests

---

## Core Features

### Load Order Management

#### Working Principle

1. **Reference Creation**: When the user is satisfied with their `Plugins.txt` file, they create a reference copy
2. **Automatic Sorting**: The tool loads the reference file and sorts all entries according to the reference order
3. **New Mod Handling**: Any new mod files are automatically appended at the end
4. **Periodic Monitoring**: Continuous background checking for external changes to the load order
5. **One-Click Restoration**: Quick fix button to restore the correct order instantly

#### File Name Case Handling

Many mod manager tools lowercase all mod file names. While Starfield doesn't mind this, it will restore 
the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the application:
- Cross-references mod names with all `.esp` and `.esm` files in the game's `Data` folder
- Always uses the original file name case from disk
- Maintains consistency even if mod managers lowercase names

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

- **ID**: Auto-generated from label (lowercase, dash-separated, ASCII-only)
- **Label**: User-facing name (2-30 characters, unique, case-insensitive)
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
- Positioned between "File" and "Settings"
- Quick access to switch and manage profiles

**Switch Profile Window**:
- Card-based interface with visual feedback
- Hover effects for easy selection
- Active profile indicated with checkmark icon
- One-click switching

**Manage Profiles Window**:
- ListView showing all custom profiles
- Add, edit, delete, and copy operations
- Context menu for quick actions
- Double-click to edit

**Profile Properties Window**:
- Create or edit profile metadata
- Real-time validation
- Material Design error display
- Shared between create and edit modes

---

### Change Detection

#### Automatic Change Detection

The application periodically checks the `Plugins.txt` file on disk for changes:
- **Configurable Interval**: Default every 5 seconds
- **Signature Tracking**: Detects changes without re-reading the entire file
- **Profile-Aware**: Automatically uses the active profile's reference file

#### Types of Changes Detected

1. **Moved Mods**: Position in load order has changed
2. **Added Mods**: New mods not present in reference file
3. **Removed Mods**: Mods in reference but missing from current file

Each mod is assigned a numerical position based on its line number (starting at 1). The application 
compares both mod names and positions to determine the type of change.

#### Managing Changes: The DIFF Window

A dedicated window shows and manages detected changes:

**Features**:
- Visual diff showing all changes with color coding
- Line-by-line comparison with reference numbers
- Sorting recommendation when order changes detected
- Action buttons:
  - **Update Reference**: Accept current state as new reference
  - **Fix Load Order**: Restore correct order from reference
  - **Discard Changes**: Revert `Plugins.txt` to reference state

**Change Resolution**:
- Re-enable removed mods
- Remove new mods
- Replace old mods with new equivalents
- Real-time diff updates as changes are resolved

---

### Game Integration

#### Play Button

Launches the game directly from the application:
- **SFSE Detection**: Automatically uses Starfield Script Extender if installed
- **Fallback**: Uses vanilla executable if SFSE not found
- **Smart Detection**: Checks for `sfse_loader.exe` presence

#### Utility Functions

Quick access to important files and folders:
- Open `Plugins.txt`
- Open reference file
- Open AppData folder
- Open game installation folder

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

**Status Messages**: Shown when settings are invalid or folders are missing

**Auto-Discovery**: Common installation paths are checked and pre-filled when found:
- Steam: `C:\Program Files (x86)\Steam\steamapps\common\Starfield`
- AppData: `%LOCALAPPDATA%\Starfield`

> **NOTE**: Installations vary between gaming platforms (Steam, GOG, etc.), so auto-discovery 
> may not always work. Users can manually browse to the correct folders.

---

## File Handling

### Example `Plugins.txt`

See the file [example-plugins.txt](./example-plugins.txt) for an example of a `Plugins.txt` file.

### File Encoding

The `Plugins.txt` file must be encoded in **UTF-8 without BOM** (Byte Order Mark):
- Application reads and writes in this format
- BOM causes Starfield to ignore the first line of the file

### Whitespace Handling

**Reading**:
- Leading and trailing whitespace on each line is ignored
- Empty lines at the end of file are ignored

**Writing**:
- No leading or trailing whitespace is added
- UTF-8 without BOM encoding

### Reference Files

**Legacy System** (pre-profiles):
- Single `Plugins.reference.txt` in AppData folder

**Profile System** (current):
- Each profile has its own `reference.txt` in `Profiles/{profile-id}/`
- Automatically created from `main.txt` when missing
- Used for change detection and sort order

### Profile Storage

**Profile Metadata** (`profile.json`):
```json
{
  "label": "My Character",
  "description": "Main playthrough character"
}
```

**Note**: Profile ID is not stored in JSON - it's derived from the folder name to prevent sync issues.
