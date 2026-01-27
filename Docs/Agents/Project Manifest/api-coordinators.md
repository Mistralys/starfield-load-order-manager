# Coordinators API

> Public API signatures for all coordinators and their event arguments.

---

## Base Interfaces

### `LoadOrderKeeper.Coordinators.ICoordinator`

```csharp
public interface ICoordinator : IDisposable
{
    void Initialize();
}
```

### `LoadOrderKeeper.Coordinators.CoordinatorBase`

```csharp
public abstract class CoordinatorBase : ObservableObject, ICoordinator
{
    public virtual void Initialize();
    public void Dispose();
    protected virtual void Dispose(bool disposing);
    protected virtual void OnDisposing();
    protected void ThrowIfDisposed();
}
```

---

## Coordinator Implementations

### `LoadOrderKeeper.Coordinators.FileMonitoringCoordinator`

```csharp
public sealed class FileMonitoringCoordinator : CoordinatorBase
{
    // Properties
    public bool PluginsFileChangedExternally { get; }
    public int ChangeCount { get; }
    public string SortingRecommendationMessage { get; }
    public bool SortingRecommendationActive { get; }
    public bool ShowSteamWarning { get; }
    public string SteamWarningTooltip { get; }
    public bool IsSteamInstalled { get; }
    public bool IsSteamRunning { get; }
    
    // Events
    public event EventHandler<ChangeDetectedEventArgs>? ChangeDetected;
    public event EventHandler<SortingRecommendationChangedEventArgs>? SortingRecommendationChanged;
    public event EventHandler<SteamWarningChangedEventArgs>? SteamWarningChanged;
    
    // Methods
    public void UpdateState(AppConfigModel config, bool refExists, bool isBusy, bool configIsInvalid);
    public Task CheckPluginsFileAsync();
}
```

**Event Firing Behavior:**
- `ChangeDetected` fires when `PluginsFileChangedExternally` state changes (false?true or true?false) **OR** when file signature changes.
- This enables both main window state updates and automatic refresh of open diff windows.
- Multiple subscribers can listen simultaneously (e.g., `MainViewModel` and `DiffDialogViewModel`).

### `LoadOrderKeeper.Coordinators.StatusCoordinator`

```csharp
public sealed class StatusCoordinator : CoordinatorBase
{
    // Properties
    public string StatusMessage { get; }
    public ObservableCollection<StatusMessageModel> StatusMessageHistory { get; }
    
    // Methods
    public void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info);
    public IReadOnlyList<StatusMessageModel> GetAllMessages();
    public string GetReadyStatusMessage(bool configValid);
    public void ClearHistory();
}
```

**Internal Logging Behavior:**
- Maintains two separate collections:
  - **Display History** (`StatusMessageHistory`): Rolling window of last 3 messages for UI display (most recent first)
  - **Internal Log** (`_allMessages`): Unlimited storage of all messages logged during the session (chronological order)
- `AddStatusMessage()` stores messages in both collections
- `GetAllMessages()` returns read-only view of complete internal log for debugging purposes
- Used by `DebugStateService` to include full status history in application state exports

### `LoadOrderKeeper.Coordinators.UpdateCheckCoordinator`

```csharp
public sealed class UpdateCheckCoordinator : CoordinatorBase
{
    // Properties
    public bool UpdateAvailable { get; }
    public string UpdateMessage { get; }
    public bool UpdateInfoBarVisible { get; }
    
    // Methods
    public Task<UpdateCheckResult> CheckForUpdatesBackgroundAsync();
    public Task<UpdateCheckResult> CheckForUpdatesManualAsync();
    public void DismissUpdateNotification();
    public string? GetLatestVersion();
}
```

### `LoadOrderKeeper.Coordinators.ProfileCoordinator`

```csharp
public sealed class ProfileCoordinator : CoordinatorBase
{
    // Properties
    public ProfileModel ActiveProfile { get; }
    public string ActiveProfileLabel { get; }
    
    // Events
    public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
    
    // Methods
    public void UpdateConfiguration(AppConfigModel? config);
    public Task RefreshActiveProfileAsync();
    public Task<bool> SwitchProfileAsync(string targetProfileId);
    public bool IsActiveProfile(string profileId);
}
```

### `LoadOrderKeeper.Coordinators.ConfigurationCoordinator`

```csharp
public sealed class ConfigurationCoordinator : CoordinatorBase
{
    // Properties
    public bool IsConfigValid { get; }
    public bool ShowErrorBanner { get; }
    
    // Events
    public event EventHandler<ConfigValidationChangedEventArgs>? ValidationChanged;
    
    // Methods
    public void UpdateConfiguration(AppConfigModel? config);
    public void ValidateConfiguration();
    public ValidationResult GetValidationResult();
}

public sealed class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }
    
    public static ValidationResult Success();
    public static ValidationResult Failed(string errorMessage);
}
```

### `LoadOrderKeeper.Coordinators.GameLauncherCoordinator`

```csharp
public sealed class GameLauncherCoordinator : CoordinatorBase
{
    // Properties
    public string PlayButtonText { get; }
    public bool HasSfseInstalled { get; }
    
    // Methods
    public void UpdateGamePath(string? gamePath);
    public void UpdateConfiguration(AppConfigModel? config);
    public bool LaunchGame();
    public string? GetExecutablePath();
}
```

### `LoadOrderKeeper.Coordinators.WindowManager`

```csharp
public sealed class WindowManager : CoordinatorBase
{
    // Methods (examples - full API in WindowManager documentation)
    public bool IsWindowOpen<T>() where T : Window;
    public void RegisterWindow<T>(T window) where T : Window;
    public void UnregisterWindow<T>() where T : Window;
    public void BringToFront<T>() where T : Window;
}
```

---

## Coordinator Event Arguments

### `LoadOrderKeeper.Coordinators.Events.ChangeDetectedEventArgs`

```csharp
public sealed class ChangeDetectedEventArgs : EventArgs
{
    public bool HasChanges { get; }
    public int ChangeCount { get; }
}
```

### `LoadOrderKeeper.Coordinators.Events.SteamWarningChangedEventArgs`

```csharp
public sealed class SteamWarningChangedEventArgs : EventArgs
{
    public bool ShowWarning { get; }
    public string Tooltip { get; }
}
```

### `LoadOrderKeeper.Coordinators.Events.SortingRecommendationChangedEventArgs`

```csharp
public sealed class SortingRecommendationChangedEventArgs : EventArgs
{
    public bool RecommendSorting { get; }
    public string Message { get; }
}
```

### `LoadOrderKeeper.Coordinators.Events.ProfileChangedEventArgs`

```csharp
public sealed class ProfileChangedEventArgs : EventArgs
{
    public ProfileModel OldProfile { get; }
    public ProfileModel NewProfile { get; }
}
```

### `LoadOrderKeeper.Coordinators.Events.ConfigValidationChangedEventArgs`

```csharp
public sealed class ConfigValidationChangedEventArgs : EventArgs
{
    public bool WasValid { get; }
    public bool IsValid { get; }
    public bool StateChanged { get; }
}
```

---

[<< Back to Index](README.md)
