# Steam Process Detection

[? Back to Overview](../README.md)

---

## Overview

The application includes intelligent Steam process detection to help prevent conflicts when managing load orders.

---

## Why Steam Detection Matters

Steam can interfere with load order management in several ways:
- May lock files when the game is running
- Can trigger automatic updates that modify game files
- Cloud sync features might conflict with load order changes
- Better to make changes when Steam is fully closed

---

## How It Works

### Automatic Detection

- Runs on the same 3-second interval as file monitoring
- Checks for Steam installation via Windows registry
- Detects if steam.exe process is currently running
- Updates warning state automatically when Steam starts/stops

### Visual Warning

- Persistent warning banner appears in main window when Steam is running
- Shows clear icon and warning message
- Provides helpful tooltip: "Steam is running. To prevent conflicts, it is recommended to close Steam before making changes to the load order."
- Warning automatically disappears when Steam closes

### Benefits

- Prevents potential file conflicts and corruption
- Reduces risk of lost changes due to Steam sync
- Clear visual feedback helps users avoid problems
- Non-intrusive—doesn't block operations, just warns

---

## Technical Details

**Detection Method**:
- Steam installation detected via registry keys (HKEY_CURRENT_USER and HKEY_LOCAL_MACHINE)
- Process detection uses `Process.GetProcessesByName("steam")` for efficiency
- Detection is part of the FileMonitoringCoordinator for optimal performance
- Warning state managed through event-driven architecture

---

## Related Features

- **[Change Detection](change-detection.md)** - Runs on same monitoring interval
- **[UI Guidelines](../ui-guidelines.md)** - Warning banner design patterns

---

## Technical Implementation

**Key Classes**:
- `FileMonitoringCoordinator` - Includes Steam detection logic
- `SteamHelper` - Steam installation and process detection
- `MainViewModel` - Displays warning banner in UI

**See Also**:
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - FileMonitoringCoordinator architecture
