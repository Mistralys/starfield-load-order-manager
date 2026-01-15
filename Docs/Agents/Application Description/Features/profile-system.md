# Profile System

[? Back to Overview](../README.md)

---

## Overview

The profile system allows users to maintain separate load orders for different characters or playthroughs. Each profile stores its own load order state independently.

---

## Profile Structure

Each profile maintains its own isolated state:

**Folder Structure**:
```
AppData/Starfield/Profiles/
  ??? default/
  ?   ??? main.txt          (current state)
  ?   ??? reference.txt     (known-good state)
  ??? my-character/
  ?   ??? profile.json      (metadata)
  ?   ??? main.txt
  ?   ??? reference.txt
  ??? another-character/
      ??? profile.json
      ??? main.txt
      ??? reference.txt
```

**Profile Files**:
1. **`profile.json`**: Stores profile metadata (label, description)
2. **`main.txt`**: Current `Plugins.txt` state for this profile
3. **`reference.txt`**: Known-good reference state for change detection

---

## Profile Properties

- **ID**: Auto-generated from label (lowercase, dash-separated, ASCII-only with numeric suffixes for uniqueness)
- **Label**: User-facing name (2-30 characters, unique, case-insensitive, cannot be "Default")
- **Description**: Optional description (max 500 characters)

---

## Profile Switching

When switching profiles:
1. Current `Plugins.txt` is backed up to the active profile's `main.txt`
2. Target profile's `main.txt` is copied to `Plugins.txt`
3. Active profile ID is updated in configuration
4. Change detection automatically uses the new profile's reference file

**No data loss**: The current state is always preserved, even if there are unsaved changes.

---

## Default Profile

- **Virtual Profile**: No `profile.json` file, but otherwise behaves like any profile
- **Always Available**: Automatically created on first use
- **Immutable Properties**: Cannot rename or change description
- **Cannot Delete**: Ensures users always have at least one profile
- **Auto-Recreation**: Automatically restored if manually deleted

---

## Profile UI Features

### Active Profile Display
- Shown below the menu bar in the main window
- Clickable to open profile switcher

### Profile Menu
- Positioned between "File" and "Edit"
- Quick access to switch and manage profiles

### Switch Profile Window
- Card-based interface with visual feedback
- Hover effects for easy selection
- Active profile indicated with checkmark icon
- One-click switching

### Manage Profiles Window
- Non-modal window allowing interaction with main application
- ListView showing all custom profiles
- Add, edit, delete, and copy operations
- Context menu for quick actions
- Double-click to edit

### Profile Properties Window
- Create or edit profile metadata
- Real-time validation with Material Design error display
- Shared between create and edit modes
- Prevents duplicate labels

---

## Related Features

- **[Reference History](reference-history.md)** - Each profile maintains independent history
- **[Configuration](../configuration.md)** - Active profile stored in configuration

---

## Technical Implementation

**Key Classes**:
- `ProfileService` - Profile CRUD operations
- `ProfileCoordinator` - Active profile state management
- `SwitchProfileViewModel` - Profile switching UI
- `ManageProfilesViewModel` - Profile management UI

**Profile ID Generation**:
- Transliterated from label (accented chars ? ASCII equivalents)
- Lowercase, dash-separated
- Numeric suffix added if duplicate (`my-profile`, `my-profile-1`, `my-profile-2`)
- Falls back to `profile` if label contains only non-ASCII chars

**See Also**:
- [File Handling](../file-handling.md) - Profile storage format details
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - ProfileCoordinator architecture
