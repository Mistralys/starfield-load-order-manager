# Project Manifest – Starfield Load Order Keeper

> Source-of-truth overview for future AI agents. Do not infer behavior beyond what is documented here.

---

## Domain Context

Starfield uses a line-based text file called `Plugins.txt` to define which mods load and in what order. Once a save game exists, the order of existing lines must not change — internal object references depend on it (e.g., equipped items added by mods). Both the game itself and mod manager tools frequently rearrange the file, which can corrupt save games. This application monitors `Plugins.txt`, detects unauthorized changes, and restores the correct load order while safely appending new mods.

---

## Navigation

This manifest is split into logical sections for easier maintenance:

### Core Documentation

1. **[Tech Stack & Patterns](tech-stack.md)** - Runtime, libraries, frameworks, and architectural patterns
2. **[File Tree](file-tree.md)** - Logical overview of project structure
3. **[Data Flows](data-flows.md)** - Key application workflows and coordinator interactions
4. **[Constraints & Invariants](constraints.md)** - Rules, guarantees, and system constraints
5. **[Localization](localization.md)** - Zero-hardcoding localization architecture and translation guidelines
6. **[File Formats & Handling](file-formats.md)** - Encoding rules, file format specs, and I/O conventions
7. **[UI Design System](ui-design.md)** - Visual design conventions, component taxonomy, and interaction patterns

### API Reference

8. **[API Surface](api-surface.md)** - Public signatures for all coordinators, models, services, ViewModels, views, and converters

---

## Quick Reference

- **Target Framework**: .NET 9
- **UI Framework**: WPF with MaterialDesign v5
- **Architecture**: MVVM + Coordinator Pattern + Instance Services
- **Localization**: See **[Localization Guide](localization.md)** for complete documentation
  - **Locale Codes**: en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR
  - **Zero-Hardcoding**: New languages require only JSON file (no code changes)
- **Key Coordinators**: FileMonitoring, Status, UpdateCheck, Profile, Configuration, GameLauncher, WindowManager
- **Static Services**: Settings, File, Profile, Diff, ReferenceHistory, UpdateCheck, Version, DateTimeFormatting, ErrorLogging, DebugState
- **Instance Services**: FileOperations, ReferenceManagement, WindowLifecycle, ViewModelInitializer
- **Localization Services**: LocalizationService (singleton), LocalizationJsonNormalizer
- **Helper Classes**: CoordinatorEventBinder, MenuViewModel, LanguageOption
- **Text ViewModels**: Per-window text ViewModels in `ViewTexts/`; see `tech-stack.md` for the full list


