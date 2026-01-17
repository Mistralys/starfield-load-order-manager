# Game Integration

[? Back to Overview](../README.md)

---

## Overview

The application provides seamless integration with Starfield through a play button, file/folder utilities, and an About dialog.

---

## Play Button

Launches the game directly from the application with intelligent Script Extender detection.

### Features

- **SFSE Detection**: Automatically uses Starfield Script Extender if installed
- **Fallback**: Uses vanilla executable if SFSE not found
- **Smart Detection**: Checks for `sfse_loader.exe` presence
- **Dynamic Label**: Shows "Play (SFSE)" or "Play (Vanilla)" based on detection

### Behavior

- Button disabled when configuration invalid
- Launches game in separate process
- Application remains open after launching game

---

## Utility Functions

Quick access to important files and folders via File menu.

### Available Utilities

**Open Plugins.txt**:
- Opens current plugins file in default text editor
- Disabled when configuration invalid

**Open Reference File**:
- Opens active profile's reference file
- Disabled when configuration invalid

**Open AppData Folder**:
- Opens Starfield's AppData directory in File Explorer
- Disabled when configuration invalid

**Open Game Folder**:
- Opens game installation directory in File Explorer
- Disabled when configuration invalid

---

## About Dialog

Material Design styled modal dialog displaying application information.

### Content

- Application name and version (clean semantic versioning)
- Copyright information with dynamic year
- Application description
- Link to project homepage on GitHub

### Design

- Material Design v5 styled with dark theme
- Rounded corners and elevation shadows
- Clickable GitHub link opens in default browser
- Keyboard navigation support (Escape to close)

---

## Related Features

- **[Configuration Validation](configuration-validation.md)** - Controls utility button enablement

---

## Technical Implementation

**Key Classes**:
- `GameLauncherCoordinator` - SFSE detection and game launching
- `AboutViewModel` - About dialog content and actions
- `MainViewModel` - File/folder utility commands

**SFSE Detection**:
- Checks for `sfse_loader.exe` in game folder
- Falls back to `Starfield.exe` if SFSE not found
- Detection runs on application startup and configuration change

**See Also**:
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - GameLauncherCoordinator details
- [UI Guidelines](../ui-guidelines.md) - Modal dialog patterns
