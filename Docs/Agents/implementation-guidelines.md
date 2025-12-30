# Implementation guidelines for Agents

## Target platform and technology

- Coding: C#
- Framework: WPF with .NET 9
- Patterns: MVVM pattern (using CommunityToolkit.Mvvm)

## Text conventions

- Always use bindings for text displayed in the UI.

## UI Implementation Guidelines

- Use Material Design principles for UI design.
- The application uses a dark mode theme by default.
- Do not use flat button styles; use outlined buttons for secondary actions and raised buttons for primary actions.
- Use consistent spacing and padding throughout the UI.

## Additional Resources

### Application Description

The [Application Description](./application-description.md) contains a high-level description of the application's
goals, features and architecture.

### Implementation Details

- The [Periodic Change Checking System](./impl-periodic-change-checking.md).
- The [DIFF Detection System](./impl-diff-detection-overview.md).
- The [MVVM Architecture Overview](./impl-mvvm-architecture-overview.md).

### Development history

The folder [Development History](./Development%20History) contains documents that describe the incremental development 
history of various features of the application (they are numbered to document their implementation order). 
These documents can provide useful context and guidelines for implementing new features or modifying existing ones.
