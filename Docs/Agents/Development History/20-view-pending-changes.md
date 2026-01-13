# View Pending Changes

## The Idea

When a user accepts changes, the current version is backed up into
the version history, and list of changes is stored in a list of
pending changes, to be archived with the next reference update.

These pending changes are currently not viewable in the UI. The idea
is to add a dialog that allows users to view the pending changes
(including the user comment) before they update the reference file.

## Pending Changes Menu Item

A new menu item "View Pending Changes" should be added to the main menu,
under the "Profile" menu. This menu item should open the Pending Changes 
dialog when clicked, even if there are no changes pending.

## The Pending Changes Dialog

This dialog shows a flat list of changes that are currently pending,
including the user comment. If the user comment is empty, a placeholder
text "(No comment entered)" should be shown instead. 

The dialog has two buttons:

- "Edit comment..." > Opens the edit comment dialog (same as when accepting
  changes) to modify the comment.
- "Close" > Closes the dialog.

An abstract text explains the role of the dialog:

"This shows all changes you have made since the last reference update. 
When you next update the reference file, these changes will be archived."

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
