# Project Manifest – Starfield Load Order Keeper

> Source-of-truth overview for future AI agents. Do not infer behavior beyond what is documented here.

---

## Navigation

This manifest is split into logical sections for easier maintenance:

### Core Documentation

1. **[Tech Stack & Patterns](tech-stack.md)** - Runtime, libraries, frameworks, and architectural patterns
2. **[File Tree](file-tree.md)** - Logical overview of project structure
3. **[Data Flows](data-flows.md)** - Key application workflows and coordinator interactions
4. **[Constraints & Invariants](constraints-invariants.md)** - Rules, guarantees, and system constraints

### API Reference

5. **[Coordinators API](api-coordinators.md)** - Public signatures for all coordinators and their events
6. **[Models API](api-models.md)** - Data models, enums, and constants
7. **[Services API](api-services.md)** - Static service classes and methods
8. **[ViewModels API](api-viewmodels.md)** - ViewModel signatures and commands
9. **[Views & Converters API](api-views.md)** - Window classes and value converters

---

## Quick Reference

- **Target Framework**: .NET 9
- **UI Framework**: WPF with MaterialDesign v5
- **Architecture**: MVVM + Coordinator Pattern + Instance Services
- **Localization**: JSON-based, 5 languages (en-US, de-DE, fr-FR, es-ES, it-IT), 189 strings
- **Key Coordinators**: FileMonitoring, Status, UpdateCheck, Profile, Configuration, GameLauncher, WindowManager
- **Static Services**: Settings, File, Profile, Diff, ReferenceHistory, UpdateCheck, Version, DateTimeFormatting, ErrorLogging, DebugState
- **Instance Services**: FileOperations, ReferenceManagement, WindowLifecycle, ViewModelInitializer
- **Localization Services**: LocalizationService (singleton), LocalizationJsonNormalizer
- **Helper Classes**: CoordinatorEventBinder, MenuViewModel
- **Text ViewModels**: 15 ViewModels providing localized strings for UI
