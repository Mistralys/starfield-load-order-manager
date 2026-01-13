# Logic Change: Store Comment in Pending Changes

## The Problem

Currently when a user accepts changes and enters a comment, the comment
is stored in the metadata of the version that is then archived in the version 
history. This is wrong, however: The comment describes the changes that were 
just accepted, and must therefore be stored in the pending changes that are 
archived when the user next updates the reference file.

## The Solution

Instead of storing the user comment in the version metadata when accepting
changes, the comment must be stored in the pending changes object. Then,
when the user next updates the reference file, the pending changes (including
the comment) are archived along with the new version.

## Implementation Guidelines

Refer to the [Application Description](../application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](../Project%20Manifest/README.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
