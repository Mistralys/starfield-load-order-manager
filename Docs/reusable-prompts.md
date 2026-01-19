# Reusable Prompts

## Generate BBCODE from README

```Markdown
Convert the `README.md` Markdown document to BBCODE and save it as `README.bbcode`, using the following rules:

- Inline code must be converted to [font=Courier New] tags, not  [CODE].
- Horizontal separators (---) are not supported in BBCODE and must be removed.
- Headings must be converted as follows:
  - `# ` to [size=3][b][/b][/size]
  - `## ` to [size=2][b][/b][/size]
  - `### ` to [size=1][b][/b][/size]
```

## Update the application manifest

```Markdown
Please update the Project Manifest for our current application in the file `/Docs/Agents/project-manifest.md` with the current state of the application. 

This document is intended to be the 'Source of Truth' for future AI agent sessions to understand the codebase without reading every line.

**The Manifest must include:**

1. **Tech Stack & Patterns:** Runtime (.NET 9/WPF), Libraries (CommunityToolkit.Mvvm), and architectural patterns (Static Services, MVVM, etc.).
2. **File Tree:** A visual directory structure of the current project.
3. **The 'Public API' (Signatures Only):** For every Service, Model, and ViewModel, list only the public properties, methods (signatures only), and constructors. **Do not include the implementation logic.**
4. **Key Data Flows:** Briefly describe how the UI interacts with the Services (e.g., 'MainViewModel calls FileService.ApplyLoadOrderAsync').
5. **Current Constraints:** List established rules like 'All file I/O must be async' or 'Case restoration requires checking the /Data folder'.
```

## Review a feature plan

```Markdown
I have provided a project specification. I want you to act as a Senior Lead Developer. 
Read my specification and, instead of starting the work, ask me **up to three clarifying questions** 
about edge cases or technical constraints I might have missed.
```

## Update the application description

> Open the [README.md](Docs/Agents/Application Description/README.md) for reference.

```Markdown
Please also update the high-leve, human-readable Application Description 
to reflect the current state of the application.
```
