# Reference File Version History

## Problem Statement

Currently, only a single load order reference file is used per profile. When the user chooses
to update the reference file with new changes, there is no way to roll that change back, nor 
to see what changed between versions.

Because the reference file is crucial for determining load order changes, a history of changes
should be maintained, allowing users to revert to previous versions if needed, as well as to
see what changes were made over time.

## Proposed Solution

### Versioned History for Reference Files

Every time the user updates the load order reference file, the current reference file is
backed up to a history folder. The user can choose to give an optional comment describing the 
changes, which can help document the evolution of the reference file over time.

Metadata is stored along with the archived version:

- Version number (incremented automatically starting at 1)
- A timestamp
- An optional user comment
- A list of changes in the version, including:
	- List of removed mod names
	- List of added mod names
	- Total number of mods changed

### Rollback Functionality

To leverage all the existing functionality including the DIFF window, rolling back to a previous 
version replaces the current `Plugins.txt` with the selected version. This allows the user to
review the changes in the DIFF viewer before confirming the rollback by updating the reference file.

This way, if the user accepts the changes, the current reference file is archived before being 
replaced, effectively creating a new version in the history. Simple, effective and consistent.

#### Forks and Linear History

There is no branching or forking of history versions. If the user updates the reference file after
a rollback, it is treated exactly like any other changes. The relevant information in the end is
the list of changes between versions.

#### Error Handling

Should any errors occur during the backup or rollback process (e.g., file access issues), appropriate 
error messages will be displayed to the user, and the operation will be safely aborted to prevent 
data corruption.

Any archived versions that cannot be accessed or read, or whose metadata is missing or corrupted will
be silently ignored when loading them from disk.

#### Import/Export Considerations

No import or export functionality is planned at this stage.

#### Storage Limits & Management

To prevent unbounded growth of the history folder, a maximum number of `16` versions to keep will 
be enforced, primarily to keep the UI uncluttered. Older versions beyond this limit will be 
automatically deleted whenever new versions are added to manage disk space effectively.

Because these text files and metadata are relatively small, there are no additional storage
considerations needed beyond this version limit.

## UI Implementation

### History Viewer Window

A window dedicated to listing all archived versions of the reference file for the active profile.
For each version, the following information is displayed:

- Version number
- Date and time of creation
- Total number of mods changed
- Change Summary (computed, see "Change Summary" section)

The user can select a version to switch to by selecting it, which enables the "Rollback..." button.
The user can also double-click a version to roll back to it.

The window also includes controls to delete individual versions or clear the entire history, as well
as to close the window.

### Rollback Confirmation Dialog

The user will be prompted to confirm the rollback action to prevent accidental changes. The dialog
will display the version number, timestamp, and change summary of the selected version, and explain
that the current `Plugins.txt` will be replaced so the changes can be reviewed before accepting them.

### Change Summary

Using the version metadata stored alongside each archived reference file, a summary of changes is 
computed and displayed. This allows for quick viewing of the history without needing to re-compute 
diffs on the fly. Example of what an entry's summary text might look like:

- "{USER_COMMENT} - Added new mod X".
- "Removed mod X and Y, added 5 new mods". (Threshold for using numbers: 3)
- "Added 4 mods, removed 1 mod".

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
