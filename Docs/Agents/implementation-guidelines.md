# Implementation guidelines for Agents

## Target platform and technology

- Coding: C#
- Framework: WPF with .NET 9
- Patterns: MVVM pattern (using CommunityToolkit.Mvvm)
- UI Library: Material Design v5 in XAML Toolkit (MaterialDesignThemes)

## Text conventions

- Always use bindings for text displayed in the UI.

## UI Implementation Guidelines

- Use Material Design **v5** principles for UI design.
- Always verify brush and style names that were renamed in Material Design v5.
- The application uses a dark mode theme by default.
- Do not use flat button styles; use outlined buttons for secondary actions and raised buttons for primary actions.
- Use consistent spacing and padding throughout the UI.

## Color Guidelines

- Always use semantic brushes instead of hardcoded hex values to ensure proper theme support.
- Avoid using deprecated Material Design brushes like `MaterialDesignValidationSuccessBrush`, which have been renamed in v5.
- Use proper Material Design semantic brushes for status indicators:
	- `{DynamicResource MaterialDesign.Brush.Error}` for error messages.
	- `{DynamicResource MaterialDesign.Brush.Tertiary}` for success messages.
	- `{DynamicResource MaterialDesign.Brush.Primary}` for informational messages.
	- `{DynamicResource MaterialDesign.Brush.Secondary}` for warning messages.
- For status messages, prefer colored foreground text over colored backgrounds for better readability and visual clarity.

## Additional Resources

### Application Description

The [Application Description](application-description.md) contains a high-level description of the application's
goals, features and architecture.

### Implementation Details

The [Project Manifest](Project%20Manifest/README.md) document is the Source-of-truth overview for AI agents, 
detailing the tech stack, file tree, architecture, MVVM patterns and key components of the application.

### Real World Sorting Examples

The [Sorting Scenarios](Sorting%20Scenarios/real-world-sorting-scenarios.md) document provides practical examples 
of sorting use cases and handling of deletions, insertions, replacements and reorderings.

### Development history

The folder [Development History](Development%20History) contains documents that describe the incremental development 
history of various features of the application (they are numbered to document their implementation order). 
These documents can provide useful context and guidelines for implementing new features or modifying existing ones.
