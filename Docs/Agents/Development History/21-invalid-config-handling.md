# Invalid Configuration Handling

## The Problem

There are currently several edge cases under which conditions the application
can automatically close without direct user interaction, in combination with
the configuration validity checks.

## The Solution

Instead of closing the application automatically when an invalid configuration
is detected, we only ensure that the user is informed about the invalid state
(which is already handled via an error banner in the main window).

All existing code paths that lead to automatic closure of the application
must be modified to allow the application to remain open, while ensuring
that no operations that depend on a valid configuration can be performed.

## Application Startup

### First Run (No Configuration)

When the application starts with no configuration or invalid configuration,
the settings dialog is shown automatically. If the user saves a valid 
configuration, the application continues normally. If the user cancels or
closes the settings dialog without saving valid configuration, the application
remains open but displays the configuration error banner.

### Configuration Validation During Startup

The profile folder validation check that currently shows an error dialog with
an option to shutdown must be modified to only show the error message and
offer to open settings. The automatic shutdown option is removed.

## Open Secondary Windows

Secondary windows must be allowed to remain open when the configuration
is invalid, but because many operations depend on the configuration, they
must no longer be editable.

### Two-Tier Protection Strategy

**Tier 1 - Prevention (Before Operations Start):**
A modal overlay prevents interaction with the window when configuration is
invalid and no operation is currently in progress.

**Tier 2 - Completion (During Operations):**
Operations that are already in progress are allowed to complete. The modal
overlay only appears after the operation finishes.

### Operation Tracking

Each window that depends on configuration tracks whether a critical operation
is currently in progress. The modal overlay visibility is determined by both
configuration validity AND operation status:

- Config valid: No overlay
- Config invalid + No operation in progress: Show overlay
- Config invalid + Operation in progress: Allow completion, then show overlay

### Configuration Re-validation

Before committing any changes that depend on configuration, operations must
re-validate the configuration. If configuration became invalid during the
operation (unlikely but possible), the operation is cancelled with a clear
error message explaining that the configuration changed.

### Modal Overlay Implementation

A reusable user control is created for the modal overlay to avoid code
duplication across windows. The overlay includes:

- Semi-transparent dark background blocking all interaction
- Centered message card with error icon
- Clear explanation that configuration must be fixed in main window
- Reassurance that the window will remain open and become usable when config is fixed

### Windows Requiring Overlay

The following windows depend on valid configuration and require the overlay:

- **Diff Window**: All operations (sort, accept, discard) require config
- **Manage Profiles Window**: Profile operations require config
- **Reference History Window**: Rollback and history operations require config  
- **View Pending Changes Window**: Editing comments requires config

### Excluded Windows

The following windows do not depend on configuration and are exempt:

- **About Window**: Informational only, no config dependency
- **Settings Window**: Used to fix configuration, must always be accessible
- **Update Options Dialog**: Informational only, provides download links
- **Comment Input Dialog**: Generic utility, used within larger operations
- **Confirmation Dialog**: Generic utility, used for confirmations

### Modal Dialog Protection

When a modal dialog is open (such as the comment input dialog during the
accept changes operation), the parent window is blocked by WPF's modal
behavior. This naturally prevents the user from accessing the main window
settings to invalidate the configuration. The configuration re-validation
check handles the edge case where configuration might change through external
means (remote desktop session, manual file deletion, etc.).

### Implementation Per Window

Each window requiring overlay protection needs:

1. **ViewModel Properties**: 
   - Configuration validity flag
   - Operation in progress flag  
   - Computed overlay visibility property

2. **Event Subscription**: 
   - Subscribe to ConfigurationCoordinator.ValidationChanged event
   - Update configuration validity flag when event fires

3. **Operation Wrapping**:
   - Set operation flag before starting critical operations
   - Clear operation flag in finally block
   - Re-validate configuration before committing changes
   - Handle cancellation gracefully if config became invalid

4. **XAML Addition**:
   - Add reusable ConfigInvalidOverlay control
   - Bind visibility to computed property
   - Set high Z-index to overlay all content

This approach keeps the code minimal (approximately 10-15 lines per window)
while providing robust protection against invalid configuration states.

## Benefits

- **No Automatic Shutdowns**: Application remains open, reducing user frustration
- **Clear Feedback**: Users understand exactly what action is needed
- **Non-Destructive**: Window state and position preserved  
- **Data Safety**: In-progress operations can complete or be safely cancelled
- **Automatic Recovery**: Overlay disappears immediately when config is fixed
- **Minimal Code**: Reusable overlay component keeps implementation simple
- **Consistent UX**: Behavior is predictable and follows modern application patterns

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
