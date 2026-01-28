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
- **Localization**: JSON-based with zero-hardcoding architecture
  - **Supported Languages**: 8 (English, German, French, Spanish, Italian, Simplified Chinese, Japanese, Portuguese)
  - **Locale Codes**: en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR
  - **Total Strings**: 189 translated strings per locale
  - **User-Selectable**: Language preference dropdown in Settings
  - **Auto-Detection**: Automatic system locale detection
  - **Extensibility**: New languages require only JSON file (no code changes)
- **Key Coordinators**: FileMonitoring, Status, UpdateCheck, Profile, Configuration, GameLauncher, WindowManager
- **Static Services**: Settings, File, Profile, Diff, ReferenceHistory, UpdateCheck, Version, DateTimeFormatting, ErrorLogging, DebugState
- **Instance Services**: FileOperations, ReferenceManagement, WindowLifecycle, ViewModelInitializer
- **Localization Services**: LocalizationService (singleton), LocalizationJsonNormalizer
- **Helper Classes**: CoordinatorEventBinder, MenuViewModel, LanguageOption
- **Text ViewModels**: 15 ViewModels providing localized strings for UI

---

## Localization Architecture

### Zero-Hardcoding Design

The localization system uses a **zero-hardcoding architecture** where adding a new language requires **only a JSON file**:

```json
{
  "LocaleName": "Português (Brasil)",
  "ParentCulture": "pt",
  "MainWindow": { "key": "translated value" },
  "Settings": { "key": "translated value" }
}
```

No C# code changes, no compilation, no hardcoded mappings. The system automatically:
- Discovers new locale files via file system scanning
- Reads display names from `LocaleName` property
- Maps parent cultures via `ParentCulture` property
- Populates language dropdown dynamically
- Supports automatic system locale detection

### Chinese and Japanese Translation Guidelines

**Critical formatting rules for Asian language translations** (zh-CN, ja-JP):

1. **Menu Hotkeys Placement**
   - Place hotkeys at the **end** of menu items using the format `(_X)`
   - Example (Chinese): `"FileMenuHeader": "文件(_F)"`
   - Example (Japanese): `"FileMenuHeader": "ファイル(_F)"`
   - **Do NOT** place space before hotkey parentheses in menu items

2. **Aki Spacing (空き)**
   - **Always maintain spacing** between English words/numbers and Asian characters
   - Correct: `版本 {0}` or `JSON 形式`
   - Incorrect: `版本{0}` or `JSON形式`
   - This improves scannability and follows modern UI typography standards

3. **Quotation Marks**
   - Use **Chinese quotes** `" "` (zh-CN) when referring to folder names or UI labels
   - Use **Japanese quotes** `「 」` (ja-JP) when referring to folder names or UI labels
   - Example (Chinese): `"Profiles" 文件夹`
   - Example (Japanese): `「Profiles」フォルダ`

4. **Punctuation**
   - Use **full-width punctuation** for all descriptive text
   - Chinese: `， 。 ： （ ）` instead of `, . : ( )`
   - Japanese: Already uses full-width by default
   - Example (Chinese): `列表中有很多更改，包括替换和移除。`
   - Maintains visual consistency with Asian character blocks

5. **Technical Terms**
   - Keep technical proper nouns (SFSE, Vanilla, GitHub, etc.) in English
   - Surround with full-width parentheses when needed
   - Example (Japanese): `プレイ（SFSE）`
   - Example (Chinese): `开始游戏（SFSE）`

**Typography Anti-Patterns to Avoid**:
- ❌ Western punctuation in Chinese text: `列表中有很多更改, 包括替换和移除.`
- ❌ Missing spacing: `版本{0}` or `JSON格式`
- ❌ Western quotes: `'Profiles' 文件夹` (use `"Profiles"` instead)
- ❌ Space before hotkey: `文件 (_F)` (use `文件(_F)` instead)

### Locale File Structure

Each locale file (`en-US.json`, `de-DE.json`, etc.) contains:

**Root-level metadata** (used by LocalizationService):
- `LocaleName`: Native language name (e.g., "Deutsch", "Français")
- `ParentCulture`: Two-letter ISO 639-1 code (e.g., "de", "fr")

**Translation sections** (used by ViewModels):
- `MainWindow`: Main window strings
- `Menu`: Menu and status bar strings
- `Settings`: Settings window strings
- `ErrorDialog`: Error dialog strings
- `About`: About window strings
- `DiffDialog`: Load order changes dialog strings
- `ManageProfiles`: Profile management window strings
- `ProfileProperties`: Profile creation/editing strings
- `SwitchProfile`: Profile switching dialog strings
- `ReferenceHistory`: Reference version history strings
- `UpdateOptions`: Update download options strings
- `ViewPendingChanges`: Pending changes window strings
- `CommentInput`: Comment input dialog strings
- `ConfirmationDialog`: Generic confirmation dialog strings
- `Common`: Shared strings across dialogs
- `StatusCoordinator`: Status message strings
- `FileMonitoring`: File monitoring warning strings
- `MainWindowStatus`: Main window status message strings
- `ViewModelInitializerStatus`: Application initialization status strings
- `ReferenceManagementStatus`: Reference file management status strings

### Language Preference System

**User-facing features**:
- Language dropdown in Settings window (9 options: Automatic + 8 languages)
- Automatic system locale detection (when set to "Automatic")
- Persistence across application restarts
- Restart notification banner when language changes

**Configuration**:
- Stored in `config.json` as `PreferredLanguage` property
- Values: `"auto"` (default), `"en-US"`, `"de-DE"`, `"fr-FR"`, `"es-ES"`, `"it-IT"`, `"zh-CN"`, `"ja-JP"`, `"pt-BR"`
- Applied on application startup via `ViewModelInitializer`

**Implementation details**:
- `LocalizationService.GetLocaleName()`: Reads native name from JSON
- `LocalizationService.BuildParentCultureMap()`: Scans all files for parent mappings
- `LocalizationService.DetectSystemCulture()`: Uses dynamic parent mapping
- `SettingsViewModel.BuildLanguageList()`: Dynamically populates dropdown
- `LanguageOption` model: Holds `Code` and `DisplayName` for dropdown

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

**?? Common Mistake**: Forgetting step 4 means the file won't be copied during build and the language won't appear in the dropdown, even though all code supports it.

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
