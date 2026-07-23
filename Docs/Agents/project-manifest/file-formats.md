# File Formats & Handling

> Encoding rules, file format specifications, and I/O conventions for all application-managed files.

---

## Plugins.txt Format

The game's load order file, located in `StarfieldAppDataPath`.

### Encoding

- **UTF-8 without BOM** — BOM causes Starfield to ignore the first line of the file.
- All application writes use `new UTF8Encoding(false)` explicitly.

### Line Syntax

- Lines starting with `*` are **enabled** mods (e.g., `*MyMod.esm`).
- Lines without `*` are **disabled** — treated as non-existent by both Starfield and this application.
- Lines starting with `#` are **comments** — ignored on read.
- Leading/trailing whitespace per line is trimmed on read.
- Trailing empty lines are ignored on read.

### Write Behavior

- Only enabled mods are written; disabled lines and comments are removed on save.
- No leading or trailing whitespace is added.

### Case Restoration

- Mod filenames are cross-referenced with `.esm` / `.esp` files in `StarfieldGamePath/Data` to restore original disk casing.

---

## Configuration File

**Location**: `%LOCALAPPDATA%/StarfieldLoadOrderKeeper/appsettings.json`

```json
{
  "appDataPath": "C:\\Users\\...\\AppData\\Local\\Starfield",
  "gamePath": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Starfield",
  "activeProfileId": "my-character",
  "preferredLanguage": "auto"
}
```

- `preferredLanguage` defaults to `"auto"` (system locale detection); accepts specific culture codes (`"en-US"`, `"de-DE"`, etc.).

---

## Profile Storage

### Folder Layout

```
%LOCALAPPDATA%/Starfield/
  └─ Profiles/
      ├─ default/
      │   ├─ main.txt
      │   ├─ reference.txt
      │   ├─ pending-changes.json
      │   └─ History/
      │       ├─ reference_v1.txt
      │       ├─ reference_v1.json
      │       └─ ...
      └─ my-character/
          ├─ profile.json
          ├─ main.txt
          ├─ reference.txt
          ├─ pending-changes.json
          └─ History/
              └─ ...
```

### File Descriptions

| File | Encoding | Purpose |
|------|----------|---------|
| `main.txt` | UTF-8 no BOM | Snapshot of `Plugins.txt` for this profile; updated on switch-away, restored on switch-to. |
| `reference.txt` | UTF-8 no BOM | Known-good reference state for change detection; updated when user accepts changes. |
| `profile.json` | UTF-8 (BOM optional) | Label and description only. Profile ID is derived from folder name, not stored in JSON. |
| `pending-changes.json` | UTF-8 (BOM optional) | Comment and mod change lists for the next version archive. |
| `History/reference_vX.txt` | UTF-8 no BOM | Archived reference snapshot for rollback. |
| `History/reference_vX.json` | UTF-8 (BOM optional) | Version metadata (number, timestamp, comment, added/removed mods). |

### Profile Metadata (`profile.json`)

```json
{
  "label": "My Character",
  "description": "Main playthrough character"
}
```

The default profile has no `profile.json`; it is virtual and auto-created.

### Pending Changes (`pending-changes.json`)

```json
{
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp"],
  "removedMods": ["ModY.esp"]
}
```

Archived with the next reference update — the comment describes the changes being accepted.

### Version Metadata (`reference_vX.json`)

```json
{
  "versionNumber": 2,
  "timestamp": "2025-01-05T14:30:00.123",
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp", "ModY.esp"],
  "removedMods": []
}
```

- Maximum 16 versions per profile; oldest pruned automatically.

### Profile ID Generation

- Transliterated from label (accented chars → ASCII equivalents).
- Lowercase, dash-separated.
- Numeric suffix for uniqueness (`my-profile`, `my-profile-1`).
- Falls back to `profile` if label yields only non-ASCII chars.

---

## Steam Library Folders

**Location**: `{Steam install}/steamapps/libraryfolders.vdf`

- Read-only external Steam metadata used by `SettingsService` to discover Starfield installations.
- `SteamLibraryVdfParser` supports quoted keys and values, nested objects, whitespace, and `//` comments between tokens or at line ends.
- The parser decodes only `\\`, `\"`, `\n`, `\r`, and `\t`; unsupported or unterminated escapes, duplicate keys, missing values, unbalanced braces, extra root pairs, and trailing tokens produce `FormatException`.
- Exactly one top-level `libraryfolders` object is accepted. Object-valued children become ordered library entries; scalar children are ignored. Missing/non-scalar `path` and missing/non-object `apps` are represented as null, while an empty `apps` object is an empty set.
- `SettingsService` treats unavailable, invalid, or malformed metadata as a detection miss and preserves its normal Steam-location fallbacks.
- `Docs/Agents/example-steam-library.vdf` is linked to `Fixtures/example-steam-library.vdf` in the test output so parser tests load it through `AppContext.BaseDirectory`.

---

## Update Check Cache

**Location**: `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\update-check-cache.json`

- Stores last check timestamp and result.
- 24-hour expiration; survives application restarts.

---

## Error Log

**Location**: `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\error.log`

- Reset on each application startup.
- Appended on unhandled exception with timestamp, exception details, and full application state.
- All user paths sanitized with `%USERPROFILE%` placeholder.

---

[<< Back to Index](README.md)
