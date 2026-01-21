# Starfield Load Order Keeper - Application Description

## Overview

The **Starfield Load Order Keeper** is a WPF .NET 9 desktop application designed to protect and manage Starfield's mod load order. This documentation provides a comprehensive guide to the application's features, architecture, and implementation.

---

## The Problem

The game Starfield uses a line-based text file called `Plugins.txt` in which each line contains the file name of a mod to load when starting the game. This is typically referred to as the "Load Order". Once a save game has been created, it is crucial that the order of existing lines in the file is not modified: New lines can be added, but all previously enabled mod lines must preserve their order.

Changing the load order in the middle of a save game can cause all manner of issues. Internal object references depend on the load order. For example, this means that if you are wearing a spacesuit that is added by a mod and that mod's position in the load order changes, you will lose that spacesuit.

The problem is that the game itself, as well as mod manager tools, tend to change the order around—hence the need for a small application to observe and fix these changes.

---

## The Solution

The application provides automated load order protection and management through:

1. **Reference File System**: Creates and maintains a reference copy of a known-good `Plugins.txt` file
2. **Automatic Detection**: Periodically monitors for unauthorized changes to the load order
3. **Steam Process Guard**: Detects when Steam is running and warns users (required for SFSE functionality)
4. **One-Click Fix**: Restores the correct load order while preserving new mods
5. **Profile Support**: Manages multiple load orders for different characters or playthroughs
6. **Visual Diff**: Shows exactly what changed with options to accept or revert changes
7. **Dependent Change Tracking**: Intelligently groups cascading position changes for clarity
8. **Version History**: Tracks reference file changes with versioning, rollback, and user comments
9. **Pending Changes View**: Shows upcoming changes before they're archived in version history
10. **Smart Confirmations**: Warns when destructive changes are about to be made
11. **Automatic Updates**: Checks for new versions and provides easy download options
12. **Configuration Validation**: Real-time validation with clear visual feedback and graceful error handling
13. **Modal Overlay Protection**: Secondary windows remain accessible with invalid configuration; overlay blocks operations until fixed
14. **Global Exception Handling**: Comprehensive error logging with user-friendly dialogs and privacy protection
15. **Debug State Export**: Captures application state for troubleshooting (includes sanitized paths for privacy)
16. **Multilingual Support**: Full interface localization in English, German, and French with automatic language detection
17. **Modular Architecture**: Coordinator pattern ensures maintainability and testability

---

## Technology Stack

The application is built as a **WPF .NET 9** desktop application using:

- **Framework**: .NET 9
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Design**: Material Design v5 theme with dark mode
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm with Coordinator pattern
- **Localization**: JSON-based internationalization supporting English, German, and French
- **Coordinators**: Modular domain logic handlers for file monitoring, status, updates, profiles, configuration, and game launching
- **Testing**: xUnit for unit tests
- **Dialogs**: Custom Material Design confirmation dialogs

---

## Documentation Structure

### Core Features
Detailed documentation for each major feature:

- **[Load Order Management](Features/load-order-management.md)** - Core load order protection and sorting functionality
- **[Profile System](Features/profile-system.md)** - Multiple load order profiles for different characters
- **[Reference History](Features/reference-history.md)** - Version history and rollback capabilities
- **[Change Detection](Features/change-detection.md)** - Automatic change detection and diff window
- **[Steam Process Detection](Features/steam-detection.md)** - Steam process monitoring and warnings
- **[Game Integration](Features/game-integration.md)** - Play button, file/folder utilities, About dialog
- **[Version Check](Features/version-check.md)** - Automatic update checking system
- **[Configuration Validation](Features/configuration-validation.md)** - Path validation and error handling
- **[Exception Handling](Features/exception-handling.md)** - Global error logging and user-friendly error dialogs
- **[Multilingual Support](Features/multilingual-support.md)** - Full localization in English, German, and French

### Architecture & Design
Technical architecture documentation:

- **[Coordinator Pattern](Architecture/coordinator-pattern.md)** - Coordinator-based architecture details
- **[MVVM Structure](Architecture/mvvm-structure.md)** - MVVM pattern implementation

### Supporting Documentation

- **[User Interface Guidelines](ui-guidelines.md)** - Design principles, dialogs, buttons, status indicators
- **[File Handling](file-handling.md)** - File formats, encoding, storage structure
- **[Configuration](configuration.md)** - Configuration settings and path auto-discovery

---

## Quick Navigation

### By Use Case

**I want to understand...**
- How load order protection works >> [Load Order Management](Features/load-order-management.md)
- How to manage multiple characters >> [Profile System](Features/profile-system.md)
- How version history works >> [Reference History](Features/reference-history.md)
- How the application detects changes >> [Change Detection](Features/change-detection.md)
- How configuration validation works >> [Configuration Validation](Features/configuration-validation.md)
- How exceptions are handled >> [Exception Handling](Features/exception-handling.md)
- How multilingual support works >> [Multilingual Support](Features/multilingual-support.md)

**I want to learn about...**
- The coordinator pattern >> [Coordinator Pattern](Architecture/coordinator-pattern.md)
- MVVM architecture >> [MVVM Structure](Architecture/mvvm-structure.md)
- UI design principles >> [User Interface Guidelines](ui-guidelines.md)
- File storage formats >> [File Handling](file-handling.md)

---

## Version History

The application maintains semantic versioning:
- Version displayed in window title and About dialog
- Retrieved from assembly attributes via `VersionService`
- Commit hashes stripped for clean display (e.g., "1.3.0" not "1.3.0+abc123")
- GitHub Actions automatically creates releases when version tags are pushed

**Recent Major Features**:
- v1.8.0: Complete localization system with English, German, and French translations (189 strings per language)
- v1.7.1: Invalid configuration handling improvements with graceful recovery and modal overlays
- v1.7.0: View Pending Changes dialog and comment storage flow fixes
- v1.6.1: Settings window improvements and debug menu for testing
- v1.6.0: Steam process detection, debug state export, and improved diff window with sorting intelligence
- v1.5.0: Configuration validation with error banners, real-time feedback, and improved error handling
- v1.4.0: Reference history with versioning, rollback, comment support, and automatic update checking
- v1.3.0: Settings helper with auto-detection, dependent change grouping, and Material Design dark theme dialogs
- v1.2.0: Status message history
- v1.1.0: About dialog and always-open diff window
- v1.0.0: Initial release with profile switching

---

## Related Documentation

- **[Project Manifest](../Project%20Manifest/README.md)** - Technical API documentation and constraints
- **[Development History](../Development%20History/)** - Feature implementation history
- **[Implementation Guidelines](../implementation-guidelines.md)** - Coding standards and practices
