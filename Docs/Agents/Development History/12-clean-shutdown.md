# Clean Application Shutdown

## Issue Description

A user mentioned not being able to manually delete the folder of the 
application. Even after closing the application, the Windows Explorer 
complained, saying that the file `d3dcompiler_47_cor3.dll` (which is 
part of the bundled files) is still in use.

## Investigation

This is a classic "zombie process" or "resource handle" issue. When 
Windows Explorer reports that d3dcompiler_47_cor3.dll is in use, it 
means a process still has an open handle to that library. Since that 
specific DLL is a core component of the WPF (Windows Presentation 
Foundation) rendering pipeline used for hardware acceleration, it 
points directly to how the application or the .NET runtime is shutting 
down.

## Root Cause

If this file is locked after you "close" the app, it usually means one 
of three things:

1. The Process is still alive: The main window is gone, but the 
   process is still visible in Task Manager because a background thread 
   (likely the update checker or a file watcher) is still running.
2. The Render Thread hung: WPF uses a separate thread for rendering.
   Occasionally, if there's a driver conflict or a heavy UI transition 
   happening exactly during shutdown, the render thread fails to release 
   its hooks into the DirectX DLLs.
3. External Hooking: An antivirus or a "game overlay" (like Discord or 
   Steam) has hooked into SLOK's DirectX pipeline and hasn't let go even 
   though the parent process is trying to die.

## Resolution Steps

### 1. Implement IDisposable Pattern in MainViewModel

Added proper resource cleanup to `MainViewModel`:

```csharp
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _shutdownCts = new();

    public void Dispose()
    {
        _pluginsMonitorTimer?.Stop();
        _shutdownCts?.Cancel();
        _shutdownCts?.Dispose();
        
        // Close non-modal windows if open
        _diffWindow?.Close();
        _manageProfilesWindow?.Close();
        _referenceHistoryWindow?.Close();
    }
}
```

### 2. Add CancellationToken Support to Background Operations

Updated `UpdateCheckService` to support cancellation:

```csharp
public static async Task<UpdateCheckResult> CheckForUpdatesAsync(
    bool bypassCache = false, 
    CancellationToken cancellationToken = default)
{
    try
    {
        // ...
        var latestRelease = await FetchLatestReleaseAsync(cancellationToken);
        // ...
    }
    catch (OperationCanceledException)
    {
        // Silent cancellation
        return new UpdateCheckResult(false, VersionService.GetApplicationVersion(), null, null);
    }
}
```

Background update check now respects cancellation:

```csharp
private async Task CheckForUpdatesBackgroundAsync()
{
    var result = await UpdateCheckService.CheckForUpdatesAsync(
        bypassCache: false, 
        _shutdownCts.Token);
}
```

### 3. Handle MainWindow Closing Event

Added `OnClosing` handler to trigger cleanup:

```csharp
protected override void OnClosing(CancelEventArgs e)
{
    // Allow MainViewModel to clean up
    if (DataContext is IDisposable disposable)
    {
        disposable.Dispose();
    }
    base.OnClosing(e);
}
```

### 4. Implement Graceful Shutdown in App.OnExit

Added graceful shutdown with forced exit as fallback:

```csharp
protected override void OnExit(System.Windows.ExitEventArgs e)
{
    // Dispose MainViewModel to trigger cleanup
    _mainViewModel?.Dispose();
    
    base.OnExit(e);
    
    // Force exit after brief grace period to ensure all resources released
    // This is necessary for WPF render thread cleanup
    Task.Delay(500).ContinueWith(_ => Environment.Exit(0));
}
```

## Implementation Summary

The implementation follows a multi-layered cleanup approach:

1. **Graceful Cancellation**: Background tasks (HTTP requests) are cancelled via `CancellationToken`
2. **Resource Disposal**: Timer stopped, windows closed, tokens disposed
3. **Window Cleanup**: MainWindow triggers disposal on close
4. **Application Cleanup**: App.OnExit ensures disposal happens
5. **Forced Exit**: `Environment.Exit(0)` after 500ms grace period ensures render thread cleanup

This approach tries to clean up gracefully first, but ensures the process 
terminates completely even if the WPF render thread hangs or external hooks 
prevent normal shutdown.

## Testing

To verify the fix:

1. Start the application
2. Let it run long enough for a background update check to occur
3. Close the main window
4. Verify in Task Manager that the process terminates completely
5. Attempt to delete the application folder - should succeed immediately

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
