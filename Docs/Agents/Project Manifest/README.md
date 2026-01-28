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
5. **[Localization](localization.md)** - Zero-hardcoding localization architecture and translation guidelines

### API Reference

6. **[Coordinators API](api-coordinators.md)** - Public signatures for all coordinators and their events
7. **[Models API](api-models.md)** - Data models, enums, and constants
8. **[Services API](api-services.md)** - Static service classes and methods
9. **[ViewModels API](api-viewmodels.md)** - ViewModel signatures and commands
10. **[Views & Converters API](api-views.md)** - Window classes and value converters

---

## Quick Reference

- **Target Framework**: .NET 9
- **UI Framework**: WPF with MaterialDesign v5
- **Architecture**: MVVM + Coordinator Pattern + Instance Services
- **Localization**: See **[Localization Guide](localization.md)** for complete documentation
  - **Supported Languages**: 8 (English, German, French, Spanish, Italian, Simplified Chinese, Japanese, Portuguese)
  - **Locale Codes**: en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR
  - **Total Strings**: 189 translated strings per locale
  - **Zero-Hardcoding**: New languages require only JSON file (no code changes)
- **Key Coordinators**: FileMonitoring, Status, UpdateCheck, Profile, Configuration, GameLauncher, WindowManager
- **Static Services**: Settings, File, Profile, Diff, ReferenceHistory, UpdateCheck, Version, DateTimeFormatting, ErrorLogging, DebugState
- **Instance Services**: FileOperations, ReferenceManagement, WindowLifecycle, ViewModelInitializer
- **Localization Services**: LocalizationService (singleton), LocalizationJsonNormalizer
- **Helper Classes**: CoordinatorEventBinder, MenuViewModel, LanguageOption
- **Text ViewModels**: 15 ViewModels providing localized strings for UI

---

## Adding a New Language

To add a new language (e.g., Portuguese):

1. **Copy** `en-US.json` to `pt-BR.json`
2. **Set metadata**:
   ```json
   {
     "LocaleName": "Português (Brasil)",
     "ParentCulture": "pt",
     ...
   }
   ```
3. **Translate** all string values
4. **Configure build action** in `.csproj` (**CRITICAL - DO NOT SKIP**):
   ```xml
   <ItemGroup>
     <Content Include="ViewTexts\Locales\pt-BR.json">
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
     </Content>
   </ItemGroup>
   ```
5. **Build** application (to include content file)
6. **Verify**: Check `bin/.../ViewTexts/Locales/pt-BR.json` exists after build
7. **Done** - Language appears in dropdown automatically

**No code changes required. No compilation needed for translations.**

**⚠️ Common Mistake**: Forgetting step 4 means the file won't be copied during build and the language won't appear in the dropdown, even though all code supports it.

----------

## Services Overview

### Static Services

- `SettingsService`: configuration persistence and default path discovery (includes Steam library detection)
- `FileService`: plugins/reference file operations plus diff helpers
- `DiffService`: diff line construction for the UI
- `ProfileService`: profile discovery, CRUD, switching, and file scaffolding
- `VersionService`: centralized application version retrieval
- `UpdateCheckService`: GitHub API integration for version checking with caching
- `ReferenceHistoryService`: version history management, archiving, rollback, and pending changes tracking
- `DateTimeFormattingService`: user-friendly date/time formatting utilities
- `ErrorLoggingService`: exception logging with user privacy protection (path sanitization)
- `DebugStateService`: application state capture for debugging with sanitized paths and full status message history
