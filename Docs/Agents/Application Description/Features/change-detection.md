# Change Detection

[? Back to Overview](../README.md)

---

## Overview

The application periodically monitors the `Plugins.txt` file for changes and provides a comprehensive diff window to review, accept, or revert those changes.

---

## Automatic Change Detection

The application checks the `Plugins.txt` file on disk for changes:
- **Fixed Interval**: Checks every 3 seconds (foundational value optimized through testing)
- **Signature Tracking**: Detects changes without re-reading the entire file
- **Profile-Aware**: Automatically uses the active profile's reference file
- **Smart Updates**: Only refreshes diff window when actual changes detected

---

## Types of Changes Detected

1. **Moved Mods**: Position in load order has changed
2. **Added Mods**: New mods not present in reference file, appended at the end
3. **Inserted Mods**: New mods added in the middle of the load order
4. **Removed Mods**: Mods in reference but missing from current file
5. **Replaced Mods**: New mod replacing a removed mod's position
6. **Unchanged Mods**: Mods at correct positions

Each mod is assigned a numerical position based on its line number (starting at 1). The application compares both mod names and positions to determine the type of change.

---

## Change Count Display

- Main window shows total number of changes (including dependent changes)
- Badge updates automatically as changes are detected or resolved
- Button text dynamically displays change count: "Manage load order (X changes)"

---

## Dependent Change Tracking

When a mod is removed or inserted in the middle of the load order, all mods below it shift positions. The application intelligently groups these cascading changes to avoid clutter.

### How It Works

**Detection**:
- Removed and inserted mods are identified as "parent" changes
- All subsequent "Moved" mods are tracked as dependent changes
- Grouping stops when a non-moved mod is encountered

**Display**:
- Collapsed by default with summary: "+ X mod positions affected by this change"
- Click summary to expand/collapse dependent changes
- Visual hierarchy shows relationship between parent and dependent changes
- Dependent changes are indented and visually distinct

**Benefits**:
- Reduces visual noise in diff window
- Quickly identifies root cause of position changes
- Easy to understand cascading effects
- Improves decision-making when reviewing changes

**Example**:
```
- ModA (Removed from line 5)
  + 10 mod positions affected by this change
    [Click to expand/collapse]
```

When expanded:
```
- ModA (Removed from line 5)
  ~ ModB (5?4)
  ~ ModC (6?5)
  ~ ModD (7?6)
  ...
```

---

## The DIFF Window

A dedicated non-modal window shows and manages detected changes.

### Features

- Visual diff showing all changes with color coding and prefixes
- Line-by-line comparison with reference and current position numbers
- Sorting recommendation banner when order changes detected
- Real-time diff updates as changes are resolved
- Collapsible dependent change groups
- Scrolls to first change automatically

### Action Buttons

**Update Reference**: Accept current state as new reference
- Shows ellipsis ("...") to indicate confirmation dialog
- Warns when removed or inserted mods detected
- Shows total affected mods including dependent changes

**Fix Load Order**: Restore correct order from reference

**Discard Changes**: Revert `Plugins.txt` to reference state
- Shows ellipsis ("...") to indicate confirmation dialog
- Displays detailed breakdown of changes to be discarded
- Warns that action cannot be undone

### Change Resolution

- **Re-enable removed mods**: Right-click context menu on removed mod
- **Remove new mods**: Right-click context menu on added mod
- **Replace old with new**: Right-click on removed mod to replace with added mod
- **Expand/collapse dependent changes**: Click on dependency summary line

### Replacement Workflow Notes

When working with multiple mod replacements in a single session, be aware that the application compares the current state against the reference file on disk. This means:

- **Option 1**: Click "Accept changes" after each replacement to make it permanent before making the next replacement
- **Option 2**: Make all your replacements together, then click "Accept changes" once to accept all changes

The app shows an informational banner when multiple removals or replacements are detected to remind you of these options. This is the intended behavior—replacements are temporary change resolution actions until explicitly accepted.

### Status Messages

- Timestamped updates shown at bottom of window
- Success, warning, and error messages color-coded
- Indicates when no new changes detected

### Sorting Recommendations

- Prominent banner to show when there are mods that have shifted position but only under the condition that they are not part of any dependent change lists (because those are not affected by sorting anyway)
- Warning icon and colored text
- Explains need to sort first

---

## Related Features

- **[Load Order Management](load-order-management.md)** - Core sorting and protection
- **[Reference History](reference-history.md)** - Tracking accepted changes over time

---

## Technical Implementation

**Key Classes**:
- `FileMonitoringCoordinator` - Periodic change detection
- `DiffDialogViewModel` - Diff window UI and logic
- `LoadOrderComparison` - Change detection algorithms
- `DependentChangeTracker` - Groups cascading changes

**Change Detection Algorithm**:
- Computes hash signature of `Plugins.txt` for quick comparison
- Full diff only performed when signature changes
- Position-based comparison with mod name matching
- Dependent change detection via sequential position analysis

**See Also**:
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - FileMonitoringCoordinator details
- [UI Guidelines](../ui-guidelines.md) - Non-modal window patterns
