# Feature: State Debugging

## Overview

To be able to more easily debug real world scenarios, the current state
of the important data structures must be exportable. This will allow
users to share their current state with developers for easier debugging.

## Implementation

A debug feature will be created that can export the current state of the
application to JSON (prettified). At minimum, this will include:

- The application version
- The current configuration (app data path, game path, active profile ID)
- Whether Steam is installed
- Whether Steam is detected as running
- The total amount of changes detected
- The current `Plugins.txt` contents
- The current reference contents
- The full current change list

## Data Sanitization

Any file paths included in the exported state must be sanitized to remove
sensitive information. This includes replacing user-specific paths with
placeholders (e.g. `%USERPROFILE%`).

Even though the exported state is intended for debugging and not for posting
publicly, care must still be taken to protect sensitive information.

## Extensibility

New features or settings that affect the application's state should
be easily added to the debug feature's export functionality, to ensure
comprehensive state capture for debugging purposes.

## User Interface

A menu strip will be added to the DIFF window with the following items:

- File
	- Exit - Closes the DIFF window
- Edit
	- Sort mods - Same function as the "Sort mods" button
	- Accept changes - Same function as the "Accept changes" button
	- Separator
	- Discard all changes - Same function as the "Discard all changes" button
- Help
	- Copy Debug State - Copies the current debug state to the clipboard in JSON format

### Copy Feedback

When the user clicks "Copy Debug State", the state of the application is captured
at that exact time. 

A message box will appear to confirm the copy operation was successful. This has a 
single button to close the message box.

### Error Handling

If an error occurs during the export process, a message box will appear
to inform the user of the failure. The message box will display the error
message and have a single button to close the message box.

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
