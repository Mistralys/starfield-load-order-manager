# Load Order Management

[? Back to Overview](../README.md)

---

## Overview

Load Order Management is the core functionality of the application. It ensures that the mod load order in `Plugins.txt` remains stable once a save game has been created, while still allowing new mods to be added.

---

## Working Principle

1. **Reference Creation**: When the user is satisfied with their `Plugins.txt` file, they create a reference copy
2. **Automatic Sorting**: The tool loads the reference file and sorts all entries according to the reference order
3. **New Mod Handling**: Any new mod files are automatically appended at the end
4. **Periodic Monitoring**: Continuous background checking for external changes to the load order
5. **One-Click Restoration**: Quick fix button to restore the correct order instantly
6. **Status History**: Recent operations tracked and displayed for easy reference

---

## File Name Case Handling

Many mod manager tools lowercase all mod file names. While Starfield doesn't mind this, it will restore the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the application:
- Cross-references mod names with all `.esp` and `.esm` files in the game's `Data` folder
- Always uses the original file name case from disk
- Maintains consistency even if mod managers lowercase names
- Searches recursively through nested folders in the `Data` directory

---

## Handling Disabled Mods

All valid mod lines start with the character `*`. Any lines that do not start with this character are considered disabled by Starfield. **A disabled mod is functionally equivalent to a missing mod**.

From the application's point of view, to keep the logic as simple as possible, these lines are treated as if they did not exist.

> **NOTE**: This means that saving changes to the `Plugins.txt` file will remove any disabled mod lines from the file. This has no adverse effect on the game, as Starfield ignores these lines anyway.

---

## Related Features

- **[Change Detection](change-detection.md)** - How changes are detected and displayed
- **[Profile System](profile-system.md)** - Managing multiple load orders
- **[Reference History](reference-history.md)** - Tracking load order changes over time

---

## Technical Implementation

**Key Classes**:
- `LoadOrderManager` - Core load order manipulation logic
- `FileMonitoringCoordinator` - Monitors `Plugins.txt` for changes
- `DiffDialogViewModel` - Manages the diff window and change resolution

**File Operations**:
- All file reads/writes use UTF-8 encoding without BOM
- Case-sensitive mod name matching against game's `Data` folder
- Atomic file operations to prevent corruption

**See Also**:
- [File Handling](../file-handling.md) - File format details
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - FileMonitoringCoordinator architecture
