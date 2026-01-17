# Internationalization & Localization

> Complete guide to the application's multi-language support system.

---

## Overview

The **Starfield Load Order Keeper** supports multiple languages through .NET's built-in resource system. The application automatically detects your Windows display language and shows the interface in your preferred language without any configuration required.

---

## Supported Languages

| Language | Code | Status |
|----------|------|--------|
| English | `en-US` | ? Default |
| French | `fr-FR` | ? Complete |
| German | `de-DE` | ? Complete |

---

## How It Works

### Automatic Language Detection

The application follows this language selection process:

1. **System Detection**: On startup, the application reads your Windows display language setting (`CultureInfo.CurrentUICulture`)
2. **Resource Matching**: If a matching resource file exists (e.g., `CommonResources.fr.resx` for French), that language is loaded
3. **Fallback**: If no matching resource file exists, the application falls back to English (default)
4. **No Configuration**: No user action required - it just works!

### What Gets Localized

All user-facing text is localized, including:

- **Window titles** and **menu items**
- **Button labels** (OK, Cancel, Save, Close, etc.)
- **Status messages** and **error messages**
- **Dialog boxes** and **confirmations**
- **About window** content
- **Tooltips** and **help text**

### What Stays the Same

Some elements remain in English for technical or practical reasons:

- **Mod file names** (e.g., `Fragile.esm`)
- **File paths** (e.g., `C:\Users\...`)
- **Debug/log messages**
- **GitHub repository links**

---

## Architecture

### Resource Files

The application uses standard .NET resource files (`.resx`) for localization:

```
Resources/
??? CommonResources.resx          # Default (English) - Shared UI strings
??? CommonResources.fr.resx       # French translation
??? CommonResources.de.resx       # German translation
??? AboutWindowResources.resx     # Default (English) - About window
??? AboutWindowResources.fr.resx  # French translation
??? AboutWindowResources.de.resx  # German translation
```

### Resource Categories

#### CommonResources
Shared UI strings used throughout the application:
- Button labels (OK, Cancel, Yes, No, Save, Close)
- Common messages and confirmations
- Error messages and warnings
- Validation messages

#### AboutWindowResources
About window specific content:
- Application name and description
- Version label and copyright
- Button labels (Homepage, Close)

### Satellite Assemblies

When the application is built, culture-specific satellite assemblies are generated:

```
bin/Debug/net9.0-windows/
??? StarfieldLoadOrderKeeper.exe
??? fr/
?   ??? StarfieldLoadOrderKeeper.resources.dll  # French resources
??? de/
    ??? StarfieldLoadOrderKeeper.resources.dll  # German resources
```

The .NET runtime automatically loads the correct satellite assembly based on the current culture.

---

## Technical Implementation

### LocalizationService

The `LocalizationService` manages culture selection and provides utilities for localization:

```csharp
public class LocalizationService
{
    public CultureInfo CurrentCulture { get; }
    public FlowDirection CurrentFlowDirection { get; }
    
    public void SetCulture(string cultureName);  // "fr-FR", "de-DE", or "auto"
    public event EventHandler? CultureChanged;
    
    // Pluralization support
    public string GetPlural(int count, string singularFormat, string pluralFormat);
}
```

**Access**: `App.LocalizationService` (singleton)

### Initialization Flow

1. `App.OnStartup()` creates `LocalizationService` instance
2. Loads user preference from `AppConfigModel.PreferredLanguage` (defaults to `"auto"`)
3. Calls `LocalizationService.SetCulture(cultureName)`:
   - Parses culture name or detects system culture
   - Sets `CurrentUICulture` and `CurrentCulture` on current thread
   - Fires `CultureChanged` event
4. ViewModels access localized strings via strongly-typed properties
5. Resource system automatically loads correct satellite assembly

### ViewModel Integration

ViewModels access resources through strongly-typed properties:

```csharp
public class AboutViewModel : ObservableObject
{
    // Resource properties - resolved at runtime based on current culture
    public string ApplicationName => AboutWindowResources.ApplicationName;
    public string Description => AboutWindowResources.Description;
    public string CloseButtonText => AboutWindowResources.CloseButtonText;
    
    // Subscribe to culture changes for dynamic updates
    public AboutViewModel()
    {
        App.LocalizationService.CultureChanged += OnCultureChanged;
    }
    
    private void OnCultureChanged(object? sender, EventArgs e)
    {
        // Refresh all localized properties
        OnPropertyChanged(nameof(ApplicationName));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(CloseButtonText));
    }
}
```

### Designer Files

Resource files generate strongly-typed accessor classes via `PublicResXFileCodeGenerator`:

```csharp
// Auto-generated from CommonResources.resx
public class CommonResources
{
    public static string ButtonOk => ResourceManager.GetString("ButtonOk", resourceCulture);
    public static string ButtonCancel => ResourceManager.GetString("ButtonCancel", resourceCulture);
    // ...
}
```

**Important**: Designer files must be regenerated after editing `.resx` files:
- Right-click `.resx` file in Visual Studio
- Select **"Run Custom Tool"**

---

## Culture Formatting

### Date and Time

The application uses culture-aware formatting for dates and times:

```csharp
// DateTimeFormattingService respects current culture
var timestamp = DateTimeFormattingService.FormatFriendly(dateTime);
// English: "Today 14:56" or "Jan 15 14:56"
// French: "Aujourd'hui 14:56" or "15 janv. 14:56"
// German: "Heute 14:56" or "15. Jan. 14:56"
```

### Numbers

Numbers are automatically formatted according to culture conventions:
- **English**: `1,234.56` (comma thousands separator, period decimal)
- **French**: `1 234,56` (space thousands separator, comma decimal)
- **German**: `1.234,56` (period thousands separator, comma decimal)

---

## Future Expansion

### Adding New Languages

To add a new language (e.g., Spanish):

1. Create culture-specific resource files:
   - `Resources/CommonResources.es.resx`
   - `Resources/AboutWindowResources.es.resx`

2. Copy all entries from the default `.resx` files

3. Translate the `<value>` elements (keep `<data name>` unchanged)

4. Build the project to generate satellite assemblies

5. Test by changing Windows display language to Spanish

### Right-to-Left (RTL) Languages

The application is prepared for RTL languages (Arabic, Hebrew):

- `LocalizationService.CurrentFlowDirection` property returns `LeftToRight` or `RightToLeft`
- `AboutWindow` binds `FlowDirection` to this property
- Other windows can follow the same pattern when RTL translations are added

```xaml
<Window FlowDirection="{Binding FlowDirection}">
    <!-- Content automatically flows right-to-left for RTL languages -->
</Window>
```

---

## Contributing Translations

### Prerequisites

- Visual Studio 2022 or later (for ResX Editor)
- OR any text editor (`.resx` files are XML)
- Familiarity with the application

### Translation Workflow

1. **Fork the Repository**: Create a fork on GitHub

2. **Create Resource Files**: Copy the default `.resx` files and rename with culture suffix:
   ```
   CommonResources.resx ? CommonResources.es.resx
   AboutWindowResources.resx ? AboutWindowResources.es.resx
   ```

3. **Translate Strings**: Open `.resx` files and translate the `<value>` elements:
   ```xml
   <!-- English (default) -->
   <data name="ButtonOk" xml:space="preserve">
     <value>OK</value>
   </data>
   
   <!-- Spanish translation -->
   <data name="ButtonOk" xml:space="preserve">
     <value>Aceptar</value>
   </data>
   ```

4. **Test Locally**: 
   - Build the project
   - Change Windows display language to your target language
   - Run the application and verify translations

5. **Submit Pull Request**: Create a PR with your translation files

### Translation Guidelines

- **Keep formatting placeholders**: `{0}`, `{1}` must remain in the same positions
- **Maintain keyboard shortcuts**: Menu items with `_` (e.g., `_File`) should keep the shortcut
- **Test in context**: Some strings are used in multiple places - ensure translations work everywhere
- **Be concise**: UI space is limited, especially for button labels
- **Use formal tone**: Professional language appropriate for a technical tool
- **Test with long text**: Some languages (German) tend to be longer than English

### Translation Checklist

- [ ] All strings in `CommonResources` translated
- [ ] All strings in `AboutWindowResources` translated
- [ ] Formatting placeholders (`{0}`, `{1}`) preserved
- [ ] Keyboard shortcuts maintained in menu items
- [ ] Tested in application UI (no truncation or wrapping issues)
- [ ] Date/time examples checked for culture-specific formatting
- [ ] Error messages are clear and actionable

---

## Troubleshooting

### Language Not Changing

**Problem**: Application still shows English after changing Windows language.

**Solutions**:
1. Verify Windows display language is changed (Settings ? Time & Language ? Language)
2. Sign out and sign back in (or restart Windows)
3. Check if satellite assembly exists: `bin/Debug/net9.0-windows/{culture}/StarfieldLoadOrderKeeper.resources.dll`
4. Try forcing the culture in code for testing:
   ```csharp
   // In App.xaml.cs OnStartup
   _localizationService.SetCulture("fr-FR");  // Force French
   ```

### Missing Translations

**Problem**: Some text shows in English despite using another language.

**Causes**:
- Translation missing from `.resx` file
- Designer file not regenerated after editing `.resx`
- ViewModel not accessing resources (hardcoded strings)

**Solutions**:
1. Open the `.resx` file and verify translation exists
2. Right-click `.resx` file ? "Run Custom Tool" to regenerate Designer
3. Check ViewModel code - ensure using resource properties, not hardcoded strings

### Designer File Errors

**Problem**: Build fails with "CommonResources does not exist" errors.

**Cause**: Designer files not generated or have wrong visibility.

**Solution**:
1. Check `.csproj` has `PublicResXFileCodeGenerator`:
   ```xml
   <EmbeddedResource Update="Resources\CommonResources.resx">
     <Generator>PublicResXFileCodeGenerator</Generator>
   </EmbeddedResource>
   ```
2. Right-click `.resx` files ? "Run Custom Tool"
3. Clean and rebuild solution

---

## Related Documentation

- **[Application Description](README.md)** - High-level application overview
- **[Project Manifest - Localization Constraints](../Project%20Manifest/constraints-invariants.md#localization)** - Technical constraints
- **[Project Manifest - LocalizationService API](../Project%20Manifest/api-services.md#localizationservice)** - Service API reference
- **[Implementation Guidelines](../implementation-guidelines.md)** - Coding standards

---

[? Back to Application Description](README.md)
