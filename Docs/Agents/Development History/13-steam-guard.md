# Implementation Plan: Steam Guard for SFSE Launch

## The Problem

Launching the game with SFSE (Starfield Script Extender) fails if Steam is
not running at that time: Steam is started automatically, but Starfield is 
then launched detached from SFSE. 

## The Solution

While this is a known problem, it can be mitigated by checking if Steam is
running, and informing the user in the application UI if it is not.

### Visual UI Indicator

To make it clear to users that Steam must be running for SFSE to function,
the "Play" button's behavior will be modified (this assumes that SFSE is 
installed):

1. Steam is running: The "Play" button functions as normal.
2. Steam is not running: The "Play" button is warning-colored and has a 
   warning icon. Hovering over it shows a tooltip:
   "Steam is not running. SFSE requires Steam to be open to function correctly."
3. Steam status cannot be checked: The "Play" button functions as normal.

> NOTE: The button status should update with each file modification check 
> (see the "Steam Process Detection").

### Failure Handling

The Steam process detection is non-critical, so it should not be intrusive.
It must not prevent users from launching the game if they choose to ignore 
the visual cues. 

### Only If Steam Is Installed

This feature is only relevant if the user has installed Starfield via Steam.
For users that have installed the game via other means (e.g., GOG, Epic Games 
Store), this check is unnecessary and should be bypassed entirely.

Whether the game is installed via Steam can be verified with the existing 
logic that checks for the Steam installation paths.

### Steam Process Detection

The Steam process detection can be added to the file modification check. 
It can set a flag indicating whether Steam is running to make the information
readily available - if the game is installed via Steam.

With a 3-second interval for file modification checks, this is sufficiently 
responsive without being too resource-intensive.

Implementation: The call `Process.GetProcessesByName("steam").Any()` should
work, because the process name for Steam is always "steam.exe".

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
