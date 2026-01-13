# Feature: Multiple Replacements Help 

## The Problem

Some edge cases cannot be handled automatically due to the complexity of
managing multiple mod replacements in a single session, as described in
[17-sequential-replacement-fix.md](17-sequential-replacement-fix.md).

## The Solution

Add an informational banner in the DIFF window that appears when multiple 
removals or replacements are detected.

### Visual Design

- Information icon on the left
- Clear, concise message
- Appears below sorting recommendation banner

**Message**:

> "When replacing multiple mods, click 'Accept changes' after each replacement to preserve your changes. Alternatively, make all replacements, then click 'Accept changes' once to accept all changes together."

### Behavior

- Shows when 2+ mods are removed or replaced
- Hidden when only 1 or 0 removals/replacements
- Updates dynamically as changes are resolved
- Non-intrusive, informational only

## User Experience

### When Banner Appears

Users see the blue info banner when:
- They have deleted 2+ mods
- They have replaced 2+ mods
- Any combination of removals and replacements totaling 2+

### Message Clarity

The banner provides two clear paths forward:
1. **Sequential approach**: Accept after each change
2. **Batch approach**: Make all changes, accept once

Both are valid workflows with no preference suggested.

## Why This Solution Works

### Acknowledges Reality
- Doesn't pretend the limitation doesn't exist
- Explains the technical reason (disk-based comparison)
- Sets correct expectations

### Provides Solutions
- Two viable workflows documented
- No "correct" way implied
- Users can choose what fits their workflow

### Non-Disruptive
- Information banner only (not a blocking dialog)
- Appears contextually when relevant
- Can be easily dismissed mentally once understood

### Low Maintenance
- No complex session state management
- No risk of memory leaks
- No lifecycle management needed
- Simple boolean logic

