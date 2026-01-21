# Multilingual Support

## Overview

The Starfield Load Order Keeper provides complete interface localization in multiple languages, automatically detecting your system language and displaying the entire user interface in your preferred language.

---

## Supported Languages

The application is fully translated into **3 languages**:

- **English (en-US)** - Default language
- **German (de-DE)** - Vollständige deutsche Übersetzung
- **French (fr-FR)** - Traduction française complète

All 189 user-facing text strings are translated, providing a native experience for each language.

---

## Automatic Language Detection

### How It Works

When you first launch the application, it automatically:

1. **Detects Your System Language** - Reads your Windows display language setting
2. **Selects Best Match** - Chooses the closest supported language
3. **Applies Immediately** - Shows the interface in your language without configuration

### Language Mapping

The system intelligently maps language variants to the closest supported translation:

- **French Variants** (fr-CA, fr-BE, etc.) ? French (fr-FR)
- **German Variants** (de-AT, de-CH, etc.) ? German (de-DE)
- **English Variants** (en-GB, en-AU, etc.) ? English (en-US)

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
- Settings Window
- Profile Management
- Diff/Changes Window
- Reference History
- Confirmation Dialogs
- Status Messages
- And more...

---

## Manual Language Selection

### Configuration Option

While automatic detection works for most users, you can manually override the language:

1. The application stores a `PreferredLanguage` setting in its configuration
2. Default value is `"auto"` for automatic detection
3. Can be changed to a specific language code (`"en-US"`, `"de-DE"`, `"fr-FR"`)

### Future Enhancement

The Settings window may include a language selector in future versions, allowing easy language switching without editing configuration files.

---

## Technical Features

### Runtime Culture Switching

The localization system supports changing languages without restarting:

- Event-driven architecture notifies all UI components
- Instant UI refresh when language changes
- Maintains application state during switch

### Format String Support

Text with dynamic values is properly formatted:

- Version numbers: "Version {0} ist verfügbar!" (German)
- Counts: "Gérer l'ordre de chargement ({0} modifications)" (French)
- Parameters: "{0} réactivé" (French)

### Unicode Character Handling

Special characters are properly supported:

- **German**: Umlauts (ä, ö, ü, ß)
- **French**: Accents (é, è, à, ê, ô, ç)
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

---

## User Experience

### Seamless Integration

Users experience localization as:

- **Zero Configuration** - Works immediately on first launch
- **Natural Interface** - Feels like a native application
- **Consistent Experience** - All screens use the same language
- **Professional Quality** - No "machine translated" text

### Benefits

- **Reduced Learning Curve** - Interface in familiar language
- **Fewer Errors** - Clear instructions in native language
- **Increased Confidence** - Better understanding of options
- **Broader Accessibility** - Usable by international community

---

## Future Languages

### Easy Addition

The architecture supports adding new languages easily:

- JSON-based translation files
- No code changes required
- Automated validation tools
- Community contribution friendly

### Potential Additions

Future versions may include:

- Spanish (es-ES)
- Italian (it-IT)
- Russian (ru-RU)
- Japanese (ja-JP)
- And more based on community demand

---

## For Developers

### Translation Files

Translations are stored in JSON format:

- Location: `ViewTexts/Locales/{language-code}.json`
- Format: Hierarchical sections with key-value pairs
- Encoding: UTF-8 with Unicode support

### Adding Translations

To add a new language:

1. Copy English JSON file as template
2. Translate all values (keys remain in English)
3. Configure build to include the file
4. Test with normalization tool

### Quality Tools

Built-in tools ensure translation quality:

- **JSON Normalizer** - Ensures proper encoding
- **Validation Tests** - Verifies structure and completeness
- **Format String Checker** - Validates placeholders

---

## Summary

The multilingual support system provides:

- ? **3 Complete Translations** - English, German, French
- ? **189 Translated Strings** - Full interface coverage
- ? **Automatic Detection** - Works out of the box
- ? **Professional Quality** - Native speaker translations
- ? **Proper Unicode Handling** - All special characters supported
- ? **Future-Ready** - Easy to add more languages

**The application speaks your language, making Starfield modding accessible to everyone!** ??
