# Multilingual Support

## Overview

The Starfield Load Order Keeper provides complete interface localization in multiple languages with user-selectable language preference and automatic system language detection.

---

## Supported Languages

The application is fully translated into **5 languages**:

- ???? **English (en-US)** - Default language
- ???? **German (de-DE)** - Vollständige deutsche Übersetzung
- ???? **French (fr-FR)** - Traduction française complète
- ???? **Spanish (es-ES)** - Traducción española completa
- ???? **Italian (it-IT)** - Traduzione italiana completa

All 189+ user-facing text strings are translated, providing a native experience for each language.

---

## User Interface

### Language Selector in Settings

**New in v1.9.0**: Users can now select their preferred language directly in the Settings window.

**How to Change Language**:
1. Open **File ? Settings**
2. Locate the **Language** dropdown
3. Select your preferred language from the list:
   - **Automatic** - Uses system locale (default)
   - **English**
   - **Deutsch** (German)
   - **Français** (French)
   - **Español** (Spanish)
   - **Italiano** (Italian)
4. A yellow warning banner appears: "Language changes will take effect after restarting the application."
5. Click **Save**
6. **Restart the application**
7. Interface now displays in your selected language

**Language Dropdown Features**:
- Native language names (e.g., "Deutsch" not "German")
- Automatic option for system locale detection
- Clear restart notification banner
- Preference persists across application restarts

---

## Automatic Language Detection

### How It Works

When you first launch the application (or select "Automatic"):

1. **Detects Your System Language** - Reads your Windows display language setting
2. **Selects Best Match** - Chooses the closest supported language
3. **Applies Immediately** - Shows the interface in your language without configuration

### Language Mapping

The system intelligently maps language variants to the closest supported translation:

- **English Variants** (en-GB, en-AU, etc.) ? English (en-US)
- **German Variants** (de-AT, de-CH, etc.) ? German (de-DE)
- **French Variants** (fr-CA, fr-BE, etc.) ? French (fr-FR)
- **Spanish Variants** (es-MX, es-AR, etc.) ? Spanish (es-ES)
- **Italian Variants** (it-CH) ? Italian (it-IT)

### Fallback Behavior

If your system language isn't supported, the application defaults to English while remaining fully functional. This ensures everyone can use the application regardless of their language.

---

## What's Translated

### Complete Interface Coverage

Every part of the user interface is localized:

- **Window Titles** - All dialog and window titles
- **Menu Items** - File, Edit, Profile, Help menus with keyboard shortcuts
- **Button Labels** - All action buttons (Save, Cancel, Accept, etc.)
- **Status Messages** - Real-time feedback and notifications
- **Error Messages** - Clear error descriptions and guidance
- **Tooltips** - Helpful hints and warnings
- **Dialog Text** - Confirmation messages and explanations
- **Column Headers** - Table and list column names
- **Form Labels** - Input field descriptions

### Sections Covered

The localization system organizes translations into logical sections:

- Main Window
- Menu System
- About Dialog
- Error Dialogs
- Settings Window (including language selector)
- Profile Management
- Diff/Changes Window
- Reference History
- Confirmation Dialogs
- Status Messages
- And more...

---

## Technical Features

### Zero-Hardcoding Architecture

**New in v1.9.0**: The localization system uses a revolutionary **zero-hardcoding architecture**:

- **No Code Changes for New Languages**: Adding a language requires only a JSON file
- **Dynamic Discovery**: Application automatically finds and loads available locales
- **Metadata from JSON**: Language names and parent culture codes stored in locale files
- **Community-Friendly**: Translators can contribute without programming knowledge

### How It Works

Each locale file (`en-US.json`, `de-DE.json`, etc.) contains:

```json
{
  "LocaleName": "Deutsch",
  "ParentCulture": "de",
  "MainWindow": {
    "key": "translated value"
  }
}
```

- `LocaleName`: Native language name displayed in dropdown
- `ParentCulture`: ISO 639-1 code for automatic detection
- Translation sections: All UI strings organized by window/feature

**Benefits**:
- New languages appear in dropdown automatically
- No compilation required for translations
- Community can submit translations via pull requests
- Reduced maintenance burden

### Runtime Culture Switching

The localization system supports changing languages:

- Event-driven architecture notifies all UI components
- Requires application restart for full effect
- Maintains application state during restart
- Clear user notification about restart requirement

### Format String Support

Text with dynamic values is properly formatted:

- Version numbers: "Version {0} ist verfügbar!" (German)
- Counts: "Gérer l'ordre de chargement ({0} modifications)" (French)
- Parameters: "{0} réactivé" (French)

### Unicode Character Handling

Special characters are properly supported:

- **German**: Umlauts (ä, ö, ü, ß)
- **French**: Accents (é, è, à, ê, ç, î)
- **Spanish**: Ñ, accented vowels (á, é, í, ó, ú)
- **Italian**: Accented vowels (à, è, ì, ò, ù)
- **Symbols**: Copyright ©, trademark ™, bullet points •

All characters display correctly regardless of system encoding.

---

## Implementation Quality

### Translation Standards

All translations follow professional standards:

- **Native Speaker Quality** - Proper grammar and terminology
- **Context Appropriate** - Respects UI space constraints
- **Keyboard Shortcuts** - Preserved and functional (e.g., _F for File menu)
- **Consistency** - Same terms used throughout the interface
- **Cultural Sensitivity** - Respects language-specific conventions

### Testing & Validation

The localization system includes:

- **Automated Tests** - Verify all strings are present and valid
- **Structure Validation** - Ensures consistency across all languages
- **Format String Verification** - Confirms placeholders work correctly
- **Round-Trip Testing** - Validates encoding and display
- **Manual Testing Checklist** - 22 test cases covering all scenarios

---

## User Experience

### Seamless Integration

Users experience localization as:

- **Zero Configuration** - Works immediately on first launch
- **User Control** - Easy language selection in Settings
- **Natural Interface** - Feels like a native application
- **Consistent Experience** - All screens use the same language
- **Professional Quality** - No "machine translated" text
- **Clear Notifications** - Understands when restart is needed

### Benefits

- **Reduced Learning Curve** - Interface in familiar language
- **Fewer Errors** - Clear instructions in native language
- **Increased Confidence** - Better understanding of options
- **Broader Accessibility** - Usable by international community
- **User Choice** - Select preferred language regardless of system settings

---

## Contributing Translations

### Easy Addition

**New in v1.9.0**: The architecture makes adding new languages incredibly easy:

**To add a new language (e.g., Portuguese)**:

1. **Copy English template**:
   ```
   Copy ViewTexts/Locales/en-US.json to pt-BR.json
   ```

2. **Add metadata at root level**:
   ```json
   {
     "LocaleName": "Português (Brasil)",
     "ParentCulture": "pt",
     ...
   }
   ```

3. **Translate all string values** (keys remain in English)

4. **Save with UTF-8 encoding**

5. **Build application** (to include new content file)

6. **Done!** - Language appears in dropdown automatically

**No code changes. No compilation. No programming knowledge required.**

### Contribution Process

1. Fork the repository on GitHub
2. Add your locale JSON file
3. Test your translation
4. Submit a pull request
5. **No code review needed** - only JSON translation review

### Translation Tools

Built-in tools ensure translation quality:

- **JSON Normalizer** - Ensures proper UTF-8 encoding
- **Validation Tests** - Verifies structure and completeness
- **Format String Checker** - Validates placeholders ({0}, {1}, etc.)

---

## Future Languages

### Potential Additions

The community can easily add:

- Portuguese (pt-BR, pt-PT)
- Russian (ru-RU)
- Japanese (ja-JP)
- Chinese (zh-CN, zh-TW)
- Polish (pl-PL)
- Dutch (nl-NL)
- And more based on community contributions

**Each new language requires only a JSON file!**

---

## For Developers

### Translation Files

Translations are stored in JSON format:

- **Location**: `ViewTexts/Locales/{language-code}.json`
- **Format**: Hierarchical sections with key-value pairs
- **Encoding**: UTF-8 with full Unicode support
- **Structure**: Root metadata + translation sections

### Architecture

The localization system consists of:

- **`LocalizationService`** - Singleton managing translations
  - `GetAvailableCultures()` - Discovers locale files
  - `GetLocaleName()` - Reads native language names
  - `DetectSystemCulture()` - Automatic locale detection
  - `BuildParentCultureMap()` - Dynamic culture mapping
- **`LanguageOption`** - Model for dropdown items
- **`SettingsViewModel`** - Manages language selection
- **`ViewModelInitializer`** - Applies preference on startup

### Quality Tools

Built-in tools ensure translation quality:

- **JSON Normalizer** - Ensures proper encoding
- **Validation Tests** - Verifies structure and completeness
- **Format String Checker** - Validates placeholders

---

## Technical Documentation

For complete technical details, see:

- **[Project Manifest - Localization Architecture](../../Project%20Manifest/README.md#localization-architecture)**
- **[API Services - LocalizationService](../../Project%20Manifest/api-services.md#localization-services)**
- **[Implementation Plan](../../Implementation/language-preference-implementation.md)**
- **[Zero-Hardcoding Summary](../../Implementation/zero-hardcoding-localization-summary.md)**

---

## Summary

The multilingual support system provides:

- ? **5 Complete Translations** - English, German, French, Spanish, Italian
- ? **189+ Translated Strings** - Full interface coverage
- ? **User-Selectable Language** - Easy dropdown in Settings window
- ? **Automatic Detection** - Works out of the box
- ? **Zero-Hardcoding Architecture** - New languages require only JSON file
- ? **Professional Quality** - Native speaker translations
- ? **Proper Unicode Handling** - All special characters supported
- ? **Community-Friendly** - Easy translation contributions
- ? **Future-Ready** - Infinitely extensible without code changes

**The application speaks your language, making Starfield modding accessible to everyone!** ????
