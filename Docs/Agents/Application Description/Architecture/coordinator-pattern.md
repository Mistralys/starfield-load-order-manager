# Coordinator Pattern

[? Back to Overview](../README.md)

---

## Overview

The application uses a **coordinator pattern** to separate concerns and improve maintainability. This architectural pattern extracts domain logic from ViewModels into specialized coordinator classes.

---

## What Are Coordinators?

Coordinators are specialized components that handle specific domain logic and state management. Each coordinator:

- Inherits from `CoordinatorBase` (provides `INotifyPropertyChanged` + `IDisposable`)
- Has a single, well-defined responsibility
- Communicates via events and property changes
- Is independently testable
- Can be reused across ViewModels

---

## Implemented Coordinators

### FileMonitoringCoordinator (~300 lines)

**Responsibilities**:
- Periodic file monitoring (3-second intervals)
- Change detection and counting
- Steam process detection and warnings
- Sorting recommendations

**Key Events**:
- `ChangesDetected` - Fires when `Plugins.txt` changes
- `SteamStateChanged` - Fires when Steam process starts/stops

**See Also**: [Change Detection](../Features/change-detection.md), [Steam Detection](../Features/steam-detection.md)

---

### StatusCoordinator (~80 lines)

**Responsibilities**:
- Status message management
- History tracking (last 3 messages)
- Timestamped message formatting

**Key Properties**:
- `CurrentMessage` - Most recent status message
- `StatusHistory` - ObservableCollection of recent messages

**See Also**: [UI Guidelines](../ui-guidelines.md#status-indicators)

---

### UpdateCheckCoordinator (~120 lines)

**Responsibilities**:
- Background and manual update checking
- Version comparison with caching
- Update notification management

**Key Properties**:
- `UpdateAvailable` - Boolean flag for new version availability
- `LatestVersion` - Latest version string from GitHub

**Key Methods**:
- `CheckForUpdatesAsync()` - Background check with caching
- `CheckForUpdatesManualAsync()` - Bypass cache for manual check

**See Also**: [Version Check](../Features/version-check.md)

---

### ProfileCoordinator (~150 lines)

**Responsibilities**:
- Active profile state management
- Profile switching coordination
- Profile change events

**Key Properties**:
- `ActiveProfileId` - Currently selected profile ID
- `ActiveProfileLabel` - Display name of active profile

**Key Events**:
- `ProfileChanged` - Fires when profile switches

**Key Methods**:
- `SwitchProfileAsync(string profileId)` - Switches to specified profile

**See Also**: [Profile System](../Features/profile-system.md)

---

### ConfigurationCoordinator (~180 lines)

**Responsibilities**:
- Configuration validation with caching
- Error banner state management
- Detailed validation results
- Invalid configuration overlay coordination

**Key Properties**:
- `IsConfigValid` - Boolean validation state
- `ValidationErrors` - List of specific error messages

**Key Events**:
- `ValidationChanged` - Fires when validation state changes

**Key Methods**:
- `ValidateConfiguration()` - Runs full validation check
- `GetValidationErrors()` - Returns detailed error list

**See Also**: [Configuration Validation](../Features/configuration-validation.md)

---

### GameLauncherCoordinator (~150 lines)

**Responsibilities**:
- SFSE (Script Extender) detection
- Game launching
- Dynamic play button text

**Key Properties**:
- `IsSfseInstalled` - Boolean SFSE detection result
- `PlayButtonText` - Dynamic label ("Play (SFSE)" or "Play (Vanilla)")

**Key Methods**:
- `LaunchGameAsync()` - Launches game with appropriate executable

**See Also**: [Game Integration](../Features/game-integration.md)

---

### WindowManager (~200 lines)

**Responsibilities**:
- Window lifecycle management
- Duplicate window prevention
- Instance tracking

**Key Methods**:
- `ShowWindow<T>()` - Shows window, prevents duplicates
- `CloseWindow<T>()` - Closes specific window type
- `IsWindowOpen<T>()` - Checks if window is open

**See Also**: [UI Guidelines](../ui-guidelines.md#window-types)

---

## Benefits

### Testability
Each coordinator can be unit tested independently with mocked dependencies.

### Maintainability
Changes are localized to specific coordinators, reducing risk of breaking other features.

### Reusability
Coordinators can be shared across multiple ViewModels for consistent behavior.

### Clarity
MainViewModel reduced from ~1300 lines to ~900 lines (31% reduction) by extracting domain logic.

### Scalability
New features can be added as new coordinators following established patterns.

---

## Communication Pattern

Coordinators communicate with ViewModels through:

1. **Properties**: Exposed for UI binding via pass-through properties in ViewModels
2. **Events**: Custom events (e.g., `ProfileChanged`, `ValidationChanged`) notify ViewModels of state changes
3. **Methods**: Public methods provide operations (e.g., `CheckForUpdatesAsync()`, `SwitchProfileAsync()`)

### Example Flow

```
User clicks "Check for Updates"
  ? MainViewModel.CheckForUpdatesCommand
    ? UpdateCheckCoordinator.CheckForUpdatesManualAsync()
      ? UpdateCheckCoordinator.UpdateAvailable property changes
        ? PropertyChanged event fires
          ? MainViewModel.OnPropertyChanged(nameof(UpdateAvailable))
            ? UI updates automatically
```

This architecture ensures clean separation of concerns while maintaining responsive UI updates through event-driven communication.

---

## CoordinatorBase

All coordinators inherit from `CoordinatorBase` which provides:

**INotifyPropertyChanged**:
- `OnPropertyChanged(string propertyName)` - Raises PropertyChanged event
- `SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName)` - Sets property with change notification

**IDisposable**:
- `Dispose()` - Releases resources (timers, event handlers, etc.)
- `Dispose(bool disposing)` - Protected virtual method for cleanup

**Benefits**:
- Consistent property change notification across all coordinators
- Proper resource cleanup to prevent memory leaks
- Reusable base functionality

---

## Related Documentation

- **[MVVM Structure](mvvm-structure.md)** - How coordinators fit into MVVM pattern
- **[All Features](../README.md#core-features)** - Features implemented using coordinators

---

## Design Guidelines

When creating new coordinators:

1. **Single Responsibility**: One coordinator per domain concern
2. **Event-Driven**: Use events for cross-cutting notifications
3. **Testable**: Design for dependency injection and mocking
4. **Disposable**: Implement proper cleanup in Dispose()
5. **Properties Over Methods**: Expose state as bindable properties when possible
6. **Coordinator Size**: Keep under 300 lines; split if growing too large
