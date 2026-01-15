# Reference History

[? Back to Overview](../README.md)

---

## Overview

The application maintains a version history of reference file updates, allowing users to track changes over time and rollback to previous states if needed.

**Key Features**:
- **Automatic Versioning**: Each reference update creates a new version
- **Change Tracking**: Records what mods were added/removed in each version
- **User Comments**: Optional comments to describe what changed
- **Rollback Support**: Restore previous reference states
- **Per-Profile History**: Each profile maintains its own independent history
- **Version Limit**: Keeps last 16 versions, automatically pruning older ones

---

## Version Storage

Each version is stored in the profile's `History` folder:

**Folder Structure**:
```
Profiles/{profileId}/
  ??? main.txt
  ??? reference.txt
  ??? pending-changes.json
  ??? History/
      ??? reference_v1.txt       (archived reference)
      ??? reference_v1.json      (version metadata)
      ??? reference_v2.txt
      ??? reference_v2.json
      ??? ...
```

**Version Metadata** (`reference_vX.json`):
```json
{
  "versionNumber": 2,
  "timestamp": "2025-01-05T14:30:00",
  "comment": "Added new gameplay mods",
  "addedMods": ["ModX.esp", "ModY.esp"],
  "removedMods": []
}
```

---

## How It Works

### Pending Changes System

1. User makes changes to load order
2. User accepts changes ? current changes stored as "pending"
3. Reference file updated to match current state
4. On next update, previous pending changes are recorded in history
5. Current changes become new pending changes

### Example Flow

```
Update 1: Add ModX
  ? Archive: Version 1 "Initial version" (no changes)
  ? Store pending: {Added: [ModX]}

Update 2: Add ModY
  ? Archive: Version 2 "Added ModX" 
  ? Store pending: {Added: [ModY]}

Update 3: Remove ModX
  ? Archive: Version 3 "Added ModY"
  ? Store pending: {Removed: [ModX]}
```

This approach ensures each version accurately describes what changed when creating that version.

---

## Version Information

Each version displays:
- **Version Number**: Sequential numbering starting at 1
- **Date & Time**: User-friendly timestamps
  - Today: "Today 14:56"
  - Yesterday: "Yesterday 16:41"
  - This year: "Jan 15 14:56"
  - Previous years: "Dec 25, 2023 14:56"
- **Changes**: Total number of mods added + removed
- **Summary**: Human-readable change description

### Summary Display

- **User comment** (italic, optional): Personal notes about the update
- **Change details**: Lists of added/removed mods
  - ?3 mods: Shows names (e.g., "Added ModX and ModY")
  - \>3 mods: Shows count (e.g., "Added 5 mods")
- Text wraps for long content
- Multiple lines for readability

---

## Reference History Window

A dedicated non-modal window for managing version history.

### Features

- DataGrid showing all versions (newest first)
- Sortable columns with enhanced headers
- Text wrapping in summary column
- Horizontal grid lines for clarity
- Context menu for quick actions
- Real-time updates when new versions created

### Actions

**Rollback** (button + context menu + double-click):
- Replaces `Plugins.txt` with archived version
- Opens diff window to review before accepting
- Shows confirmation with version details

**Edit Comment** (context menu):
- Modify version comments after creation
- Opens dialog with existing comment pre-filled
- Updates immediately in history

**Delete Version** (context menu):
- Removes specific version from history
- Shows confirmation warning
- Cannot be undone

**Clear All History** (menu + button):
- Deletes entire version history
- Shows confirmation warning
- Does not affect current reference

### Menu Bar

- **File** ? Exit: Close window
- **Edit** ? Clear all history: Delete all versions

### Window Behavior

- Non-modal: Can interact with main window while open
- Single instance: Prevents duplicate windows
- Auto-refresh: Updates when new versions created
- Dynamic updates: Reflects changes from main window

---

## Comment Dialog

Optional comment input when updating reference.

### Features

- Multi-line text input (max 500 characters)
- Material Design styled with proper text colors
- OK/Cancel buttons with proper event handling
- Reusable for creating and editing comments

### Behavior

- Cancel aborts the reference update (no version created)
- Empty comment allowed (defaults to "Initial version" for first update)
- Comment appears in italic in history window
- Editable after creation via context menu

---

## View Pending Changes Window

A dedicated non-modal window for viewing and editing pending changes before they are archived.

### Features

- Shows explanation of pending changes concept
- Displays pending comment (or "(No comment entered)" placeholder)
- Lists all added mods (with green + icon)
- Lists all removed mods (with red - icon)
- Material Design v5 styled with dark theme
- Single instance: Prevents duplicate windows

### Actions

**Edit comment...**:
- Opens comment dialog to modify pending comment
- Updates comment immediately in pending changes
- Saves to pending-changes.json

**Close**: Closes the window

### Access

- Profile menu ? "View Pending Changes..."
- Available even when no pending changes exist
- Shows "No pending changes" message when empty

### Window Behavior

- Non-modal: Can interact with main window while open
- Single instance: Brings existing window to front if already open
- Scrollable content area for long mod lists

### Purpose

- Review what will be archived on next reference update
- Edit comment before archiving
- Understand current pending state at a glance

---

## Automatic Migration

For existing installations without history:

### On-Demand Creation

- When history is empty and no pending changes exist
- Creates "Initial version" automatically
- Archives current reference state
- Transparent to user (no special indication)
- Works per-profile independently

### Benefits

- Seamless upgrade experience
- No manual migration needed
- Handles external file changes
- Restores history if manually deleted

---

## Version Limits

- **Maximum Versions**: 16 per profile
- **Auto-Pruning**: Oldest versions deleted when limit exceeded
- **Prune Timing**: After each new version created
- **Sort Order**: Oldest versions (lowest numbers) pruned first

---

## Technical Details

### File Encoding

- All files stored in UTF-8 without BOM
- Consistent with main file handling

### Storage Location

- Per-profile: `Profiles/{profileId}/History/`
- Profile-specific: Independent histories per profile
- Pending changes: `Profiles/{profileId}/pending-changes.json`

### Error Handling

- Archive failures show warning but continue update
- Load failures return empty history
- Corrupted files silently ignored
- Missing folders automatically created

---

## Related Features

- **[Profile System](profile-system.md)** - Each profile has independent history
- **[Change Detection](change-detection.md)** - Changes tracked in pending state

---

## Technical Implementation

**Key Classes**:
- `ReferenceHistoryService` - History CRUD operations
- `ReferenceHistoryViewModel` - History window UI
- `ViewPendingChangesViewModel` - Pending changes window UI
- `CommentDialogService` - Comment input dialogs

**See Also**:
- [File Handling](../file-handling.md) - Version storage format details
- [UI Guidelines](../ui-guidelines.md) - Window behavior patterns
