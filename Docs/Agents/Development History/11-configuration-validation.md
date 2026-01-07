# Configuration Validation

Any of the two paths in the settings not being available anymore is a critical 
failure and must be handled correctly, even if something changes after startup. 
There are already a number of checks in place, but this must be extended.

## When To Check

The configuration validation should occur on every tick of the existing file
monitoring timer, and on every user action that relies on the configured paths.

> NOTE: The startup behavior does not change.

## How To Handle Invalid Configuration

The existing behavior is to disable UI elements when the configuration is invalid.
This is good, buit confusing because there is no visual indication of why the UI
is disabled. 

### The Configuration Error Banner

In addition to disabling the UI elements, I propose to add a visual banner in the
main window (like the existing version update banner), styled to indicate an error 
state. This banner informs the user that no operations can be performed due to 
invalid paths, and includes a button "Open settings" to quickly navigate to the 
settings window.

This banner has the following characteristics:

- Shown whenever either of the two configured paths is invalid.
- Hidden again if the configuration becomes valid on the next file monitoring tick.
- It cannot be dismissed by the user.
- Styled in Material Design v5 error style.
- If the version update banner is also visible, it is shown above it - they are stacked vertically.
- The error message reads: "Path configuration error, please review the configured paths."
- It has a button "Open settings" that opens the settings window when clicked.

## Invalid Configuration Behavior

### Main Window

In the main window, relevant menu items, buttons and other UI elements that rely on the
configured paths are already disabled, which is fine. In the other windows like the
Profile management window, error dialogs can be shown when the user tries to perform
actions that rely on the configured paths. 

### Secondary Windows

When an error occurs in any secondary window (e.g., profile management window, 
changes window) due to invalid configuration, the existing error dialogs must be
extended with a message informing the user about the error banner in the main window 
(which could be hidden behind a secondary window), as a configuration error is the 
most likely culprit of such errors.

It should show the following message, appended to the regular error message:

"The likely cause is that the current configuration is invalid. Please refer to the 
error message in the main window to fix this."

### Settings Window

Currently when opening the settings window during an invalid configuration state, 
there is no visual indicator of what is wrong. To improve this, the settings window 
should also be capable of showing a status banner, placed at the top.

#### The Status Banner

The idea is to have a permanent status banner in the settings window that indicates
both error and success states regarding the path configuration.

##### Error State

When either of the two configured paths is invalid, the banner shows an error state.
This banner has the following characteristics:

- It is styled in Material Design v5 error style.
- It has an error icon.
- It cannot be dismissed by the user.
- It clearly states which path(s) are invalid, e.g.:
  - "The game path is invalid."
  - "The app data path is invalid."
  - "Both the game path and app data path are invalid."
- It is shown whenever either of the two configured paths is invalid.

##### Success State

When both configured paths are valid, the banner shows a success state.

This banner has the following characteristics:

- It is styled in Material Design v5 success style.
- Is has a checkmark icon.
- It cannot be dismissed by the user.
- It states: "The configured paths are valid."
- It is shown whenever both of the two configured paths are valid.

#### Validation Logic

The validation logic is run when:

- The window is opened
- Whenever the user changes values (on input blur, not keystrokes)
- When the user clicks the "Save" button

### Error Status Caching

To avoid lots of I/O operations on invalid paths, while the banner is visible, 
an error status is stored internally between ticks of the file monitoring timer.
This guarantees that no I/O operations are attempted for any user-triggered
actions during this time, as they would likely fail anyway.

On the next tick of the file monitoring timer, the paths are re-checked, and
the error status is cleared if the configuration is valid again.

### Cached State Update Triggers

The cached error state updates on:

1. File monitoring timer tick
2. `Config` property changed (after settings save)
3. Settings dialog closes with valid configuration

This ensures user doesn't need to wait for timer tick after fixing paths.

## Rationale

Even if this is an edge case, it has several advantages:

1. It ensures that the user is immediately informed of any critical configuration issues.
2. When debugging error reports, it provides a clear indication of what went wrong.
3. All operations are gated with this check, ensuring that no further actions are taken with invalid paths.

## Implementation Guidelines

Refer to the [Application Description](./application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](./project-manifest.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

See [MVVM Architecture Overview](./impl-mvvm-architecture-overview.md) for an overview of the MVVM architecture
used in the application.

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
