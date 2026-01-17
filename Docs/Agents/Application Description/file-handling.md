# File Handling

[? Back to Overview](README.md)

---

## Overview

The application handles various file formats for configuration, profiles, and load order data. All files use UTF-8 encoding without BOM for consistency.

---

## Example `Plugins.txt`

See the file [example-plugins.txt](../../example-plugins.txt) for an example of a `Plugins.txt` file.

---

## File Encoding

### UTF-8 without BOM

The `Plugins.txt` file must be encoded in **UTF-8 without BOM** (Byte Order Mark):
- Application reads and writes in this format
- BOM causes Starfield to ignore the first line of the file
- All file writes use UTF-8 without BOM explicitly

**Implementation**:
```csharp
var encoding = new UTF8Encoding(false); // false = no BOM
File.WriteAllText(path, content, encoding);
```

---

## Whitespace Handling

### Reading

- Leading and trailing whitespace on each line is trimmed
- Empty lines at the end of file are ignored
- Comment lines (starting with `#`) are ignored

### Writing

- No leading or trailing whitespace is added
- UTF-8 without BOM encoding
- Only enabled mods are written (no disabled lines or comments)

---

## Reference Files

### Legacy System (pre-profiles)

- Single `Plugins.reference.txt` in AppData folder (no longer used)
- Maintained for backward compatibility during migration

### Profile System (current)

- Each profile has its own `reference.txt` in `Profiles/{profile-id}/`
- Automatically created from `main.txt` when missing
- Used for change detection and sort order
- Copied raw (preserving comments) when created

---

## Profile Storage

### Profile Metadata (`profile.json`)

```json
{
  "label": "My Character",
  "description": "Main playthrough character"
}
```

**Note**: Profile ID is not stored in JSON—it's derived from the folder name to prevent sync issues.

### Profile ID Generation

- Transliterated from label (accented chars ? ASCII equivalents)
- Lowercase, dash-separated
- Numeric suffix added if duplicate (`my-profile`, `my-profile-1`, `my-profile-2`)
- Falls back to `profile` if label contains only non-ASCII chars

**Examples**:
- "My Character" ? `my-character`
- "Jön's Profile" ? `jons-profile`
- "Profile" ? `profile`
- "Profile" (duplicate) ? `profile-1`

### Pending Changes (`pending-changes.json`)

```json
{
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp"],
  "removedMods": ["ModY.esp"]
}
```

**Note**: Stores comment and changes made since last reference update. The comment describes the changes being accepted and is archived with the next version when the reference is updated again.

---

## Version History Storage

### History Structure

```
Profiles/{profile-id}/History/
  ??? reference_v1.txt      # Archived reference file
  ??? reference_v1.json     # Version metadata
  ??? reference_v2.txt
  ??? reference_v2.json
  ??? ...
```

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

### Metadata Properties

- `versionNumber`: Sequential version number (starts at 1)
- `timestamp`: ISO 8601 format timestamp
- `comment`: Optional user comment (null if empty)
- `addedMods`: Mods added when creating this version
- `removedMods`: Mods removed when creating this version

### Storage Rules

- Maximum 16 versions per profile
- Oldest versions automatically pruned
- All files UTF-8 without BOM encoding
- Per-profile isolation (independent histories)

---

## Folder Structure

### Complete Profile Layout

```
%LOCALAPPDATA%/Starfield/
  ??? Profiles/
      ??? default/
      ?   ??? main.txt
      ?   ??? reference.txt
      ?   ??? pending-changes.json
      ?   ??? History/
      ?       ??? reference_v1.txt
      ?       ??? reference_v1.json
      ?       ??? ...
      ??? my-character/
          ??? profile.json
          ??? main.txt
          ??? reference.txt
          ??? pending-changes.json
          ??? History/
              ??? reference_v1.txt
              ??? reference_v1.json
              ??? ...
```

### File Purposes

**`main.txt`**: Current state of `Plugins.txt` for this profile
- Updated when switching away from profile
- Restored when switching to profile
- UTF-8 without BOM

**`reference.txt`**: Known-good reference state
- Used for change detection
- Updated when user accepts changes
- UTF-8 without BOM

**`profile.json`**: Profile metadata
- Label and description only
- UTF-8 with or without BOM (JSON parser handles both)

**`pending-changes.json`**: Next version's changes
- Comment and mod lists
- Archived on next reference update
- UTF-8 with or without BOM

**`History/reference_vX.txt`**: Archived reference file
- Snapshot of reference at version X
- Used for rollback functionality
- UTF-8 without BOM

**`History/reference_vX.json`**: Version metadata
- Version info, timestamp, comment, changes
- UTF-8 with or without BOM

---

## Configuration Storage

### Application Configuration (`appsettings.json`)

```json
{
  "appDataPath": "C:\\Users\\Username\\AppData\\Local\\Starfield",
  "gamePath": "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Starfield",
  "activeProfileId": "my-character"
}
```

**Location**: `%LOCALAPPDATA%/StarfieldLoadOrderKeeper/appsettings.json`

**Properties**:
- `appDataPath`: Path to Starfield's AppData folder (contains `Plugins.txt`)
- `gamePath`: Path to Starfield installation (contains `Data` folder)
- `activeProfileId`: Currently selected profile ID

---

## Update Check Cache

### Cache File (`update-check-cache.json`)

```json
{
  "timestamp": "2025-01-05T14:30:00.123",
  "latestVersion": "1.5.0",
  "downloadUrl": "https://github.com/..."
}
```

**Location**: `%LOCALAPPDATA%/StarfieldLoadOrderKeeper/update-check-cache.json`

**Properties**:
- `timestamp`: When check was performed (ISO 8601)
- `latestVersion`: Latest version from GitHub
- `downloadUrl`: Release page URL

**Cache Duration**: 24 hours

---

## File Operations

### Atomic Writes

To prevent corruption:
1. Write to temporary file
2. Verify write success
3. Rename temp file to target (atomic operation)
4. Delete temp file if rename fails

**Implementation**:
```csharp
var tempPath = path + ".tmp";
File.WriteAllText(tempPath, content, encoding);
File.Move(tempPath, path, overwrite: true);
```

### Error Handling

**File Not Found**:
- Check if file exists before reading
- Create parent directories if needed
- Provide clear error messages to user

**Access Denied**:
- Detect permission issues
- Guide user to check file/folder permissions
- Suggest running as administrator if needed

**Corrupted Files**:
- JSON parsing errors logged but not shown to user
- Fall back to defaults when possible
- Recreate files if unrecoverable

---

## Related Features

- **[Profile System](Features/profile-system.md)** - Profile file structure
- **[Reference History](Features/reference-history.md)** - Version storage
- **[Configuration](configuration.md)** - Configuration files

---

## Best Practices

1. **Always UTF-8 without BOM** for `Plugins.txt` and related files
2. **Atomic writes** to prevent corruption
3. **Validate after read** to catch corrupted data early
4. **Create parent directories** before writing files
5. **Handle access denied** with clear user guidance
6. **Log errors** for debugging without interrupting user flow
7. **Use Path.Combine** instead of string concatenation
8. **Normalize paths** (backslashes, no trailing separator)
