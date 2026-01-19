# Exception Handling

[? Back to Overview](../README.md)

---

## Overview

The application implements comprehensive global exception handling to ensure that unhandled exceptions are properly logged and presented to users in a helpful, actionable way. This feature addresses user reports of the application "closing down unexpectedly" by capturing all exceptions, logging detailed diagnostics, and providing users with clear next steps.

---

## The Problem

Users reported that the application would "just close down" when clicking on things, with no indication of what went wrong:

> "i still have the same problem, no matter what i click on it just closes down the programme. i really cant work out why it doing it."

Without proper exception handling, unhandled exceptions cause the application to terminate abruptly, leaving users confused and unable to report the issue effectively.

---

## The Solution

A three-tier approach to exception handling:

1. **Comprehensive Exception Capture** - Catch all unhandled exceptions from any source
2. **Detailed Error Logging** - Log full diagnostic information with privacy protection
3. **User-Friendly Error Dialog** - Present clear actions and next steps to users

---

## Exception Capture

The application registers three global exception handlers in `App.xaml.cs` to ensure comprehensive coverage:

### UI Thread Exceptions
```csharp
Application.DispatcherUnhandledException
```
Captures exceptions thrown on the main WPF UI thread (e.g., button clicks, property changes).

### Non-UI Thread Exceptions
```csharp
AppDomain.CurrentDomain.UnhandledException
```
Captures exceptions thrown on background threads or non-UI contexts.

### Async Task Exceptions
```csharp
TaskScheduler.UnobservedTaskException
```
Captures exceptions in async/await code that would otherwise go unobserved.

### Exception Handling Flow

1. Exception occurs anywhere in the application
2. Appropriate global handler captures it
3. Exception is logged to `error.log` with full diagnostic information
4. `ErrorDialog` is displayed to the user
5. User chooses an action (open logs, report bug, exit, or ignore)

---

## Error Logging

All exceptions are logged to `error.log` in the application data folder (`%LOCALAPPDATA%\LoadOrderKeeper\error.log`).

### Log File Lifecycle

- **Created on startup**: Log file is cleared at application launch to keep logs focused on current session
- **Appended on error**: Each exception appends detailed information to the log
- **Persistent location**: Always in the same location for easy access

### Log Contents

Each logged exception includes:

#### 1. Timestamp
```
ERROR OCCURRED AT: 2024-01-18 14:30:45
```

#### 2. Exception Details
- **Type**: Full exception type name (e.g., `System.InvalidOperationException`)
- **Message**: Exception message text
- **Stack Trace**: Full call stack for debugging

#### 3. Inner Exception (if present)
- Type, message, and stack trace of inner exception

#### 4. Application State
Full diagnostic snapshot via `DebugStateService`:
- Application version
- Configuration paths (sanitized)
- Active profile ID
- Steam installation/running status
- Current change count
- `Plugins.txt` contents
- Reference file contents
- Current change list with all detected differences

### Privacy Protection

All user-specific paths are automatically sanitized to protect user privacy:

**Before sanitization:**
```
C:\Users\JohnDoe\AppData\Local\Starfield
at LoadOrderKeeper.Services.FileService.ReadFile(String path) in C:\Users\JohnDoe\Documents\Projects\App\FileService.cs:line 42
```

**After sanitization:**
```
%USERPROFILE%\AppData\Local\Starfield
at LoadOrderKeeper.Services.FileService.ReadFile(String path) in %USERPROFILE%\Documents\Projects\App\FileService.cs:line 42
```

This ensures users can safely share error logs when reporting bugs without exposing personal information.

---

## Error Dialog

When an exception occurs, the application displays a Material Design styled error dialog with comprehensive user actions.

### Dialog Design

- **Title**: "An Unexpected Error Occurred"
- **Icon**: Alert circle (48x48) in error color
- **Message**: Exception message in readable text
- **Details Section**: Scrollable area showing exception type and message
- **Size**: 600x420 pixels, centered on screen, non-resizable
- **Style**: Material Design v5 dark theme consistent with application

### User Actions

The dialog provides four action buttons arranged in two rows:

#### Primary Actions (Top Row)

**"Open Log Folder"**
- Opens the application data folder in File Explorer
- Provides direct access to `error.log` file
- Useful for examining full diagnostic information

**"Report Bug"**
- Opens GitHub issues page in default browser
- URL: `https://github.com/Mistralys/starfield-load-order-manager/issues`
- Allows users to report the issue with developers

#### Secondary Actions (Bottom Row)

**"Exit" (Recommended)**
- Immediately shuts down the application
- Recommended action after an unhandled exception
- Styled as raised button to indicate it's the primary choice

**"Ignore (Unsafe)"**
- Dismisses the dialog and continues running
- Styled in warning color (flat button) to indicate risk
- Tooltip warns that application may be in unstable state
- Should only be used if user understands the risks

### Button Layout Rationale

The layout guides users toward safe actions:
1. Primary row encourages investigation (logs) and reporting
2. Exit button is prominent as the safest option
3. Ignore button is visually de-emphasized to discourage use

---

## Services

### ErrorLoggingService

Static service responsible for error logging operations.

**Key Methods:**
- `InitializeErrorLog()` - Clears log file on startup
- `LogExceptionAsync()` - Logs exception with full diagnostic information
- `SanitizeText()` - Replaces user profile paths with `%USERPROFILE%`

**Features:**
- Async file operations to avoid blocking
- Graceful failure handling (no recursive exceptions)
- Comprehensive path sanitization for privacy

### DebugStateService

Static service for capturing application state snapshots.

**Key Methods:**
- `CaptureDebugStateAsync()` - Captures full application state as JSON
- `SanitizePath()` - Sanitizes individual paths
- `ReadFileContentsAsync()` - Reads file contents for diagnostics

**Captured Information:**
- Application version
- Configuration (paths sanitized)
- Steam installation/running status
- Total changes detected
- Full file contents (`Plugins.txt`, reference file)
- Complete change list

---

## Testing

A test exception menu item is provided in the Debug menu for validation:

**Menu**: Debug ? Throw Test Exception

**Action**: Throws a test `InvalidOperationException` with a descriptive message

**Purpose**:
- Verify exception handling works correctly
- Test error dialog appearance and functionality
- Validate error logging with sanitized paths
- Ensure user actions work as expected

---

## Error Dialog ViewModel

### ErrorDialogViewModel

**Properties:**
- `ErrorMessage` - Exception message text
- `ErrorDetails` - Exception type and message for details section

**Commands:**
- `OpenLogFolderCommand` - Opens app data folder in File Explorer
- `ReportBugCommand` - Opens GitHub issues in browser
- `ExitCommand` - Shuts down application
- `IgnoreCommand` - Closes dialog and continues

**Events:**
- `CloseRequested` - Raised when Ignore button clicked
- `ExitRequested` - Raised when Exit button clicked (triggers shutdown)

---

## Implementation Details

### Exception Handler Registration

Registered in `App.OnStartup()` before any other initialization:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    // Initialize error log (clear previous content)
    ErrorLoggingService.InitializeErrorLog();
    
    // Register global exception handlers
    RegisterExceptionHandlers();
    
    // Continue with normal startup...
}
```

### Handler Behavior

**DispatcherUnhandledException:**
- Sets `e.Handled = true` to prevent default crash
- Logs exception and shows dialog
- Continues execution or exits based on user choice

**UnhandledException:**
- Always fatal (CLR limitation)
- Logs exception and shows dialog
- Calls `Environment.Exit(1)` after user acknowledgment

**UnobservedTaskException:**
- Sets `e.SetObserved()` to prevent default termination
- Logs exception and shows dialog
- Continues execution or exits based on user choice

---

## User Benefits

### For End Users

1. **Clarity**: Clear explanation of what went wrong
2. **Actionable**: Multiple clear actions to take
3. **Empowerment**: Direct access to logs and bug reporting
4. **Safety**: Recommended actions prevent data corruption

### For Developers

1. **Diagnostics**: Full application state at time of error
2. **Reproducibility**: Complete context for reproducing issues
3. **Privacy**: Sanitized logs safe for public bug reports
4. **Testing**: Debug menu item for validation

---

## Best Practices

### When Exceptions Occur

1. **For Users:**
   - Click "Open Log Folder" to view `error.log`
   - Click "Report Bug" to file an issue on GitHub
   - Include relevant portions of `error.log` (already sanitized for privacy)
   - Click "Exit" to safely close the application

2. **For Developers:**
   - Review `error.log` for full diagnostic information
   - Examine application state snapshot (configuration, files, changes)
   - Check stack trace for exact error location
   - Use test exception menu item to verify error handling works

### Development

- Use Debug ? Throw Test Exception to verify error handling
- Check that `error.log` contains sanitized paths
- Verify error dialog displays correctly
- Test all four action buttons
- Ensure log file resets on each application launch

---

## Related Features

- **[Configuration Validation](configuration-validation.md)** - Prevents configuration errors that might cause exceptions
- **[Game Integration](game-integration.md)** - Debug menu where test exception is available

---

## Technical Notes

### Path Sanitization Algorithm

1. Get user profile path: `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)`
2. Search text (case-insensitive) for user profile path occurrences
3. Replace all occurrences with `%USERPROFILE%` placeholder
4. Applied to:
   - Exception messages
   - Stack traces
   - Inner exception messages and stack traces
   - Configuration paths in debug state
   - Any text that might contain file paths

### Log File Location

Always at: `%LOCALAPPDATA%\LoadOrderKeeper\error.log`

This is the same folder as `config.json` for consistency.

### Dialog Owner

Error dialog has no owner window since it can occur at any time, even during application startup. It's always centered on screen.

---

## Future Enhancements

Potential improvements for future versions:

1. **Automatic Bug Reporting**: Option to submit error reports automatically
2. **Error Statistics**: Track most common errors for prioritization
3. **Log Rotation**: Keep multiple log files instead of clearing on startup
4. **Telemetry**: Optional anonymous error reporting
5. **Recovery Actions**: Automatic recovery for known error scenarios

---

[? Back to Overview](../README.md)
