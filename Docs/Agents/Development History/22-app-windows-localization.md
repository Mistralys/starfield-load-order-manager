# Feature: App Windows Localization

## The Localization System

Localization support has been implemented in the application. 

The About window has been translated, and a common resources file 
has been added for global texts. 

See the Project Manifest for details on how the localization is set up.

## Next Steps

The other windows of the application must be translated to complete 
the localization effort. They will be done incrementally over time,
in this order:

1. Main Window
1. Settings Window
1. Manage Profiles Window
1. Profile Properties Window
1. Switch Profile Window
1. Reference History Window
1. Diff Window
1. View Pending Changes Window
1. Comment Input Dialog
1. Confirmation Dialog
1. Update Options Dialog

## **IMPORTANT**

This project is NOT ABOUT THE REPLACEMENT LOGIC. It is uniquely focused
on the localization of windows and dialogs.

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
