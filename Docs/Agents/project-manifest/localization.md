# Localization Architecture

> Comprehensive guide to the zero-hardcoding localization system

---

## Table of Contents

1. [Zero-Hardcoding Design](#zero-hardcoding-design)
2. [Chinese and Japanese Translation Guidelines](#chinese-and-japanese-translation-guidelines)
3. [Locale File Structure](#locale-file-structure)
4. [Language Preference System](#language-preference-system)
5. [Adding a New Language](#adding-a-new-language)
6. [Testing LocalizationService](#testing-localizationservice)

---

## Zero-Hardcoding Design

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

### Supported Languages

- **Total Languages**: 8
- **Locale Codes**: en-US, de-DE, fr-FR, es-ES, it-IT, zh-CN, ja-JP, pt-BR
- **Total Strings**: 198 translated strings per locale 
- **User-Selectable**: Language preference dropdown in Settings
- **Auto-Detection**: Automatic system locale detection
- **Extensibility**: New languages require only JSON file (no code changes)

---

## Chinese and Japanese Translation Guidelines

**Critical formatting rules for Asian language translations** (zh-CN, ja-JP):

### 1. Menu Hotkeys Placement

- Place hotkeys at the **end** of menu items using the format `(_X)`
- Example (Chinese): `"FileMenuHeader": "文件(_F)"`
- Example (Japanese): `"FileMenuHeader": "ファイル(_F)"`
- **Do NOT** place space before hotkey parentheses in menu items

### 2. Aki Spacing (空き)

- **Always maintain spacing** between English words/numbers and Asian characters
- Correct: `版本 {0}` or `JSON 形式`
- Incorrect: `版本{0}` or `JSON形式`
- This improves scannability and follows modern UI typography standards

### 3. Quotation Marks

- Use **Chinese quotes** `" "` (zh-CN) when referring to folder names or UI labels
- Use **Japanese quotes** `「 」` (ja-JP) when referring to folder names or UI labels
- Example (Chinese): `"Profiles" 文件夹`
- Example (Japanese): `「Profiles」フォルダ`

### 4. Punctuation

- Use **full-width punctuation** for all descriptive text
- Chinese: `， 。 ： （ ）` instead of `, . : ( )`
- Japanese: Already uses full-width by default
- Example (Chinese): `列表中有很多更改，包括替换和移除。`
- Maintains visual consistency with Asian character blocks

### 5. Technical Terms

- Keep technical proper nouns (SFSE, Vanilla, GitHub, etc.) in English
- Surround with full-width parentheses when needed
- Example (Japanese): `プレイ（SFSE）`
- Example (Chinese): `开始游戏（SFSE）`

### Typography Anti-Patterns to Avoid

- ❌ Western punctuation in Chinese text: `列表中有很多更改, 包括替换和移除.`
- ❌ Missing spacing: `版本{0}` or `JSON格式`
- ❌ Western quotes: `'Profiles' 文件夹` (use `"Profiles"` instead)
- ❌ Space before hotkey: `文件 (_F)` (use `文件(_F)` instead)

---

## Locale File Structure

Each locale file (`en-US.json`, `de-DE.json`, etc.) contains:

### Root-Level Metadata

Used by `LocalizationService`:

- `LocaleName`: Native language name (e.g., "Deutsch", "Français")
- `ParentCulture`: Two-letter ISO 639-1 code (e.g., "de", "fr")

### Translation Sections

Used by ViewModels:

| Section | Purpose |
|---------|---------|
| `MainWindow` | Main window strings |
| `Menu` | Menu and status bar strings |
| `Settings` | Settings window strings |
| `ErrorDialog` | Error dialog strings |
| `About` | About window strings |
| `DiffDialog` | Load order changes dialog strings |
| `ManageProfiles` | Profile management window strings |
| `ProfileProperties` | Profile creation/editing strings |
| `SwitchProfile` | Profile switching dialog strings |
| `ReferenceHistory` | Reference version history strings |
| `UpdateOptions` | Update download options strings |
| `ViewPendingChanges` | Pending changes window strings |
| `CommentInput` | Comment input dialog strings |
| `ConfirmationDialog` | Generic confirmation dialog strings |
| `Common` | Shared strings across dialogs |
| `StatusCoordinator` | Status message strings |
| `FileMonitoring` | File monitoring warning strings |
| `ConfigInvalidOverlay` | Configuration overlay strings |
| `MainWindowStatus` | Main window status message strings |
| `ViewModelInitializerStatus` | Application initialization status strings |
| `ReferenceManagementStatus` | Reference file management status strings |

---

## Language Preference System

### User-Facing Features

- Language dropdown in Settings window (9 options: Automatic + 8 languages)
- Automatic system locale detection (when set to "Automatic")
- Persistence across application restarts
- Restart notification banner when language changes

### Configuration

- **Storage**: `config.json` as `PreferredLanguage` property
- **Values**: `"auto"` (default), `"en-US"`, `"de-DE"`, `"fr-FR"`, `"es-ES"`, `"it-IT"`, `"zh-CN"`, `"ja-JP"`, `"pt-BR"`
- **Application**: Applied on application startup via `ViewModelInitializer`

### Implementation Details

| Component | Purpose |
|-----------|---------|
| `LocalizationService.GetLocaleName()` | Reads native name from JSON |
| `LocalizationService.BuildParentCultureMap()` | Scans all files for parent mappings |
| `LocalizationService.DetectSystemCulture()` | Uses dynamic parent mapping |
| `SettingsViewModel.BuildLanguageList()` | Dynamically populates dropdown |
| `LanguageOption` model | Holds `Code` and `DisplayName` for dropdown |

---

## Adding a New Language

To add a new language (e.g., Portuguese):

### Step 1: Copy Template

Copy `en-US.json` to `pt-BR.json` in `ViewTexts\Locales\`

### Step 2: Set Metadata

```json
{
  "LocaleName": "Português (Brasil)",
  "ParentCulture": "pt",
  ...
}
```

### Step 3: Translate Strings

Translate all string values in each section (189 strings total)

### Step 4: Configure Build Action (**CRITICAL**)

Add to `.csproj`:

```xml
<ItemGroup>
  <Content Include="ViewTexts\Locales\pt-BR.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Step 5: Build Application

Build to include content file in output directory

### Step 6: Verify

Check that `bin/.../ViewTexts/Locales/pt-BR.json` exists after build

### Step 7: Test

Launch application - language appears in dropdown automatically

---

## Common Mistakes

### ⚠️ Forgetting Build Action Configuration

**Problem**: File won't be copied during build and language won't appear in dropdown

**Solution**: Always add `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>` to `.csproj`

### ⚠️ Invalid JSON Syntax

**Problem**: Application fails to load translations

**Solution**: Validate JSON syntax before committing (use JSON linter)

### ⚠️ Missing Translation Keys

**Problem**: UI shows placeholder text or throws errors

**Solution**: Ensure all 189 keys are present in the file (use en-US.json as reference)

### ⚠️ Incorrect Metadata

**Problem**: Language doesn't appear or automatic detection fails

**Solution**: Verify `LocaleName` and `ParentCulture` are set correctly

---

## Technical Architecture

### Services

| Service | Role |
|---------|------|
| `LocalizationService` | Singleton managing locale loading and culture detection |
| `LocalizationJsonNormalizer` | Validates and normalizes JSON files |

### ViewModels

15 specialized ViewModels provide localized strings:

- `MainWindowTexts`
- `MenuTexts`
- `SettingsTexts`
- `ErrorDialogTexts`
- `AboutTexts`
- `DiffDialogTexts`
- `ManageProfilesTexts`
- `ProfilePropertiesTexts`
- `SwitchProfileTexts`
- `ReferenceHistoryTexts`
- `UpdateOptionsTexts`
- `ViewPendingChangesTexts`
- `CommentInputTexts`
- `ConfirmationDialogTexts`
- `CommonTexts`
- `StatusCoordinatorTexts`
- `FileMonitoringTexts`
- `ConfigInvalidOverlayTexts`
- `MainWindowStatusTexts`
- `ViewModelInitializerStatusTexts`
- `ReferenceManagementStatusTexts`

### Data Flow

1. **Startup**: `ViewModelInitializer` loads preferred language from `config.json`
2. **Detection**: If set to "auto", `LocalizationService.DetectSystemCulture()` determines locale
3. **Loading**: `LocalizationService` loads JSON file and populates ViewModels
4. **Binding**: UI elements bind to ViewModel properties via XAML
5. **Runtime**: Language changes trigger ViewModel property updates and UI refresh

---

## No Code Changes Required

**Key principle**: Adding a new language requires **only a JSON file**. No C# code changes, no compilation needed for translations.

The architecture dynamically discovers, loads, and integrates new languages at runtime.

---

## Testing LocalizationService

`LocalizationService` is a **singleton with global mutable state** (`CurrentCulture`). Tests that call `SetCulture()` must isolate their culture changes to avoid corrupting other tests running concurrently or afterward.

### Test Fixture: EnglishLocaleFixture

`Tests/LoadOrderKeeper.Tests/Fixtures/EnglishLocaleFixture.cs` provides xUnit fixture support for locale-sensitive tests.

**What it does:**
- Snapshots `LocalizationService.Instance.CurrentCulture` on construction
- Calls `SetCulture("en-US")` to guarantee English strings for all tests in the consuming class
- Restores the original culture in `Dispose()`

**Required usage pattern — both mechanisms are mandatory:**

```csharp
[Collection(LocaleSequentialCollection.Name)]          // (1) Prevents parallel class execution
public sealed class MyTests : IClassFixture<EnglishLocaleFixture>  // (2) Per-class fixture lifecycle
{
    public MyTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US culture is active for the lifetime of this test class
        // ... remaining setup
    }
}
```

| Mechanism | Purpose |
|-----------|---------|
| `IClassFixture<EnglishLocaleFixture>` | Constructs/disposes the fixture once per test class (before first test, after last test). Ensures setup/teardown is bracketed around the class's full run. |
| `[Collection(LocaleSequentialCollection.Name)]` | Groups the class into `LocaleSequentialCollection` which has `DisableParallelization = true`. Prevents concurrent `SetCulture` calls from multiple locale-sensitive test classes running in parallel. |

**Why both are needed:** `IClassFixture<T>` alone does not prevent xUnit from running test *classes* in parallel. Without `[Collection]`, two classes each have their own fixture instance and can call `SetCulture` concurrently, corrupting the shared singleton and producing intermittent failures.

### Test Collection: LocaleSequentialCollection

`Tests/LoadOrderKeeper.Tests/Fixtures/LocaleSequentialCollection.cs` defines the xUnit collection used by all locale-sensitive test classes.

```csharp
[CollectionDefinition(Name, DisableParallelization = true)]
public class LocaleSequentialCollection
{
    public const string Name = "LocaleSequentialCollection";
}
```

`DisableParallelization = true` ensures all classes in the collection run sequentially. Use `LocaleSequentialCollection.Name` (not a magic string) in `[Collection]` attributes.

### Locale-Agnostic Tests

Tests that observe `LocalizationService` behavior without controlling the culture (e.g., verifying the startup culture detection contract) should **not** use `EnglishLocaleFixture`. These tests must pass on any system locale and should not be placed in `LocaleSequentialCollection`.

See `LocalizationServiceTests.CurrentCulture_DefaultsToSystemCulture` for the reference implementation: it reads `SessionStartCulture` (immutable after singleton construction) and asserts parseability without mutating singleton state.
