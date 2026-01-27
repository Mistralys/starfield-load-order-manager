# Status Message History

## Overview

The application maintains a comprehensive status message history that displays all operations, warnings, errors, and informational messages throughout the current session. This provides users with complete visibility into application activity and helps with troubleshooting.

---

## User Interface

### Main Window Status Area

Located at the bottom of the main window, the status message area displays:

- **Recent Status Messages** header (appears when messages exist)
- **Scrollable message list** with timestamps and color-coded message types
- **Automatic scrollbar** when messages exceed visible area (150px height)
- **Most recent messages at top** for immediate visibility

### Message Display Format

Each status message includes:
- **Timestamp**: Formatted as `[HH:mm:ss]` for quick reference
- **Message text**: Descriptive text explaining the operation or event
- **Color coding**: Visual distinction by message type
  - **Info** (Blue): General information and status updates
  - **Success** (Green): Successful operations
  - **Warning** (Orange): Non-critical issues requiring attention
  - **Error** (Red): Failed operations or critical problems

---

## Technical Implementation

### Architecture

**StatusCoordinator** manages all status message functionality:
- Maintains two separate collections:
  - **Display History** (`StatusMessageHistory`): Unlimited scrollable UI collection
  - **Internal Log** (`_allMessages`): Complete session log for debugging
- Inserts new messages at the beginning (most recent first)
- No artificial limits on message count
- Thread-safe operations with disposal pattern

### Message Model

**StatusMessageModel** contains:
- `Message`: The status text
- `Timestamp`: DateTime when message was created
- `Type`: Enum value (Info, Success, Warning, Error)
- `FormattedTimestamp`: User-friendly time string
- `DisplayText`: Combined timestamp and message for UI binding

### UI Rendering

**MainWindow.xaml** implementation:
- `ScrollViewer` with `MaxHeight="150"` for consistent area
- `ItemsControl` bound to `StatusMessageHistory` observable collection
- Material Design styling with dynamic foreground colors
- Automatic visibility toggle based on message count

---

## Message Types

### Info Messages (Blue)
Default type for general information:
- "Initializing application..."
- "Configuration updated successfully."
- "Checking for updates..."
- "Ready. Configuration is valid."

### Success Messages (Green)
Confirm successful operations:
- "Reference file created successfully."
- "Load order changes accepted."
- "Profile switched to: [Profile Name]"
- "Changes discarded successfully."

### Warning Messages (Orange)
Alert to non-critical issues:
- "Plugins.txt was modified by an external program."
- "Steam is running. Close Steam before making changes."
- "Custom editor launch failed. Using default application."

### Error Messages (Red)
Critical failures requiring attention:
- "ERROR: Failed to read Plugins.txt: [details]"
- "ERROR: Failed to create reference file: [details]"
- "Configuration is required. Please set paths in the Settings window."

---

## Use Cases

### Normal Operations
Users can review:
- Recent file operations and their results
- Configuration changes and validations
- Profile switches and reference updates
- Game launch attempts

### Troubleshooting
Status history helps diagnose:
- When and why operations failed
- Sequence of events leading to errors
- External modifications to files
- Configuration validation issues

### Debug State Export
The complete internal log is included in debug state exports:
- Accessible via Debug menu ? "Copy Debug State"
- Includes all messages from session start
- Chronological order with timestamps and types
- Used for bug reports and support requests

---

## History Management

### Session Scope
- Messages persist for the entire application session
- Cleared only when application closes
- No automatic pruning or rotation

### Manual Clearing
- `StatusCoordinator.ClearHistory()` removes all messages
- Used primarily for testing and initialization
- Not exposed in user interface

### Memory Considerations
- Messages are lightweight (string + DateTime + enum)
- Typical session: 50-200 messages (~10-40 KB)
- No practical memory concern for normal usage

---

## Comparison to Previous Implementation

### Before (v1.2.0 - v1.8.1)
- **Limit**: Only last 3 messages displayed
- **Scrolling**: Not available
- **Older messages**: Lost when new messages arrived
- **Visibility**: Limited context for troubleshooting

### After (v1.8.2+)
- **Limit**: Unlimited message history
- **Scrolling**: Full session history accessible
- **Older messages**: Preserved for entire session
- **Visibility**: Complete operational context

---

## Integration with Other Features

### Configuration Validation
Invalid configuration triggers error messages:
- "Configuration is required. Please set paths in the Settings window."
- Displayed in red with Error type
- Remains visible until configuration fixed

### File Monitoring
Change detection generates status messages:
- "Detected changes at [timestamp]"
- "No new differences detected ([timestamp])"
- Helps users understand monitoring activity

### Profile System
Profile operations logged:
- "Profile switched to: [Profile Name]"
- "Profile [Name] created successfully."
- Confirms successful profile changes

### Reference Management
Reference operations tracked:
- "Reference file created successfully."
- "Reference updated from Plugins.txt"
- "Rollback to version [timestamp] successful"

### Exception Handling
Errors logged before error dialog appears:
- "ERROR: [Exception message]"
- Provides context in status history
- Available for review after dialog dismissal

---

## Developer Notes

### Adding Status Messages

From any ViewModel or Service:
```csharp
StatusCoordinator.AddStatusMessage("Operation completed", StatusMessageType.Success);
```

Message types available:
- `StatusMessageType.Info` (default)
- `StatusMessageType.Success`
- `StatusMessageType.Warning`
- `StatusMessageType.Error`

### Accessing Complete Log

For debugging or export:
```csharp
IReadOnlyList<StatusMessageModel> allMessages = StatusCoordinator.GetAllMessages();
```

Returns messages in chronological order (oldest first).

### Testing

Unit tests verify:
- Unlimited message storage
- Message ordering (most recent first)
- Type preservation
- Timestamp accuracy
- Clear history functionality

---

## Future Enhancements

Potential improvements:
- **Filtering**: Show only specific message types
- **Search**: Find messages by text content
- **Export**: Save session history to text file
- **Persistence**: Optionally preserve history across sessions
- **Categorization**: Group messages by feature area

---

## Related Documentation

- **[User Interface Guidelines](../ui-guidelines.md)** - Status indicator design
- **[Exception Handling](exception-handling.md)** - Error message integration
- **[Configuration Validation](configuration-validation.md)** - Validation message types
- **[Coordinator Pattern](../Architecture/coordinator-pattern.md)** - StatusCoordinator architecture

---

[<< Back to Application Description](../README.md)
