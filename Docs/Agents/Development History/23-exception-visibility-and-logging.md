# Feature: Exception Dialog and Error Logging

## The Problem

There are users reporting the same issue:

- "i still have the same problem, no matter what i click on it just closes down the programme. i really cant work out why it doing it."
- "I am having the same issue. I tried to open it via admin and regular, and no matter what, it closes seconds after it is opened, and anything in the app is pressed. "

## Possible Causes

I have been unable to reproduce this issue myself, even if I delete the 
`config.json` to simulate an invalid initial state. I believe this cause
can now be eliminated.

The next logical reason is that something causes an exception. Since there is no 
specific exception handling in place, from a user's perspective, it causes the 
app to close.

## The Solution

To address this, two things will be implemented to make it possible to identify
the root cause of the problem.

### 1. A global exception handler

This handler will capture any unhandled exceptions and display an error dialog 
to the user. This dialog will provide information about the error and possible 
steps to resolve it.

#### Exception Handler Scope

The global exception handler must capture all unhandled exceptions:

- **UI thread exceptions** via `Application.DispatcherUnhandledException`
- **All unhandled exceptions** via `AppDomain.CurrentDomain.UnhandledException`
- **Task exceptions** via `TaskScheduler.UnobservedTaskException`

This comprehensive approach ensures that any error, regardless of where it occurs,
is properly logged and reported to the user.

#### The Error Dialog

The dialog must be shown **AFTER** the error has been fully logged, so the
error log files are already available. It will contain the following elements:

- **Title**: "An unexpected error occurred"
- **Message**: A brief description of the error, including the exception message.
- **Action Buttons**:
  - **"Open Log Folder"**: Opens the application data folder in File Explorer where the error.log file is stored
  - **"Report Bug"**: Opens the GitHub issues page in the default browser
  - **"Exit"**: Closes the application immediately
  - **"Ignore (Unsafe)"**: Dismisses the dialog and attempts to continue running the application (with warning that app may be in unstable state)

> The Bug Report link is the following URL (place this in a constant):
> https://github.com/Mistralys/starfield-load-order-manager/issues

#### Button Layout

The buttons should be arranged to guide users toward safe actions:
- Primary action buttons: "Open Log Folder" and "Report Bug"
- Secondary action buttons: "Exit" (recommended) and "Ignore (Unsafe)" (discouraged)

The "Ignore (Unsafe)" button should be visually distinct to indicate it's not the recommended action.

#### Dialog Styling

The dialog must follow Material Design v5 guidelines like all other windows:
- Dark theme consistent with application
- Rounded corners and elevation shadows
- Proper spacing and typography
- Material Design icon for error/warning state

### 2. Error logging 

Whenever an exception occurs, a logging functionality will record the exception 
details to a log file, including all application state information that's
available at that point without causing further issues.

#### Log File Location and Management

- **Location**: Application data folder (same location as config.json)
- **Filename**: `error.log`
- **Lifecycle**: The log file is reset/cleared on each application launch
- **Format**: Plain text with structured sections for readability

#### Log File Contents

The error log must include:

1. **Timestamp**: When the error occurred
2. **Exception Details**:
   - Exception type
   - Exception message
   - Full stack trace
3. **Application State**: All debug information captured by the existing debug state service, including:
   - Application version
   - Current configuration (app data path, game path, active profile ID)
   - Whether Steam is installed
   - Whether Steam is detected as running
   - The total amount of changes detected
   - The current `Plugins.txt` contents
   - The current reference contents
   - The full current change list

This reuses the existing `DebugStateService` functionality to ensure consistency
and completeness of logged information.

#### Error Logging Service

A new service will be created to handle error logging:
- Initializes the log file on application startup (clears any previous content)
- Writes exception details and application state when an error occurs
- Handles logging failures gracefully (no recursive exceptions)
- Ensures all data is flushed to disk before showing the error dialog

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
