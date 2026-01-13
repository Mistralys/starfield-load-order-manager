# Fix Investigation: Multiple Sequential Replacements Not Persisting

## Issue Report

See [16-position-shift-investigation.md](16-position-shift-investigation.md).

When replacing two mods sequentially using the right-click "Replace with..." context menu:

1. Replace `Fragile.esm` → `Fragile2.esm` ✓ Shows as "Replaced"
2. Replace `OutpostBuildArea.esm` → `a98_Outpost-Build-Area.esm` ✓ Shows as "Replaced"
3. **Problem**: Only ONE replacement shows at a time. The first replacement reverts to showing as "Removed" + separate "Added" mod

## Root Cause Analysis

The `ReplaceModWithNewAsync` method in `FileService.cs` modifies the reference list **in memory only**:

```csharp
// Modified the reference list in memory
referenceMods[referenceIndex] = replacementEntry;

// Wrote to Plugins.txt
await WriteAlignedLoadOrderAsync(config, referenceMods, currentMods, targetPath);

// ❌ Never persisted the modified reference!
```

**Why This Causes the Problem**:
1. First replacement modifies `referenceMods` in memory
2. Writes to `Plugins.txt` successfully
3. **But**: Each subsequent call to `ReplaceModWithNewAsync` loads a **fresh reference from disk**
4. The fresh reference doesn't have the first replacement, so it shows as "removed + added"

## Why My Initial "Fix" Was WRONG

I initially added code to persist the reference file after each replacement. **This completely broke the intended behavior**:

```csharp
// ❌ WRONG - Auto-accepts changes without user confirmation
await File.WriteAllLinesAsync(referencePath, referenceLines, Utf8NoBom).ConfigureAwait(false);
```

**The Fundamental Problem**: The reference file should **ONLY** be updated when the user explicitly clicks **"Update Reference"** (Accept changes) button. Replacements are **temporary in-session operations** to help resolve changes, not automatic acceptances.

My "fix" would have:
- ✗ Auto-accepted replacements without user confirmation
- ✗ Made the "Update Reference" button meaningless for replacements
- ✗ Prevented users from reviewing and reverting replacements
- ✗ Violated the core workflow: temporary changes → review → explicit accept

## The REAL Issue

The problem is that **replacement state needs to be session-persistent, not disk-persistent**. The current architecture loads fresh state from disk for each operation, losing in-memory modifications.

### Possible Solutions

#### Option 1: Session-Level State Cache (Recommended)
- Maintain replacement state in `DiffDialogViewModel` or a new `SessionStateService`
- Pass modified reference list between operations instead of reloading from disk
- Keep reference file read-only until "Update Reference" clicked

#### Option 2: Temporary Staging File
- Write replacements to a temporary staging file (e.g., `reference.pending.txt`)
- Load from staging file if it exists, otherwise load from reference
- Delete staging file when "Update Reference" clicked (making it permanent)

#### Option 3: In-Memory Reference Management
- `DiffDialogViewModel` loads reference once and maintains it
- All change operations (re-enable, remove, replace) modify this in-memory state
- Only write to disk when "Update Reference" clicked

## Current State

**Reverted the incorrect fix**. 

The issue remains:

- Reference file only updated on explicit "Update Reference" (correct behavior preserved)
- Multiple sequential replacements still don't work correctly
- Issue documented for proper fix implementation

## Test Coverage

All 97 tests pass with the revert, confirming the original behavior is restored.

## Next Steps

Attempting to fix this is a proper engineering challenge that requires architectural consideration.
Before we go down that road, let's consider what we have.

### What We Have

• Core function works perfectly: Sorting load order (**the main purpose**)
• Single replacement detection: Works reliably
• Two-pass algorithm: Handles position shifts from single deletions
• User can always manually fix: Re-enable mods, remove unwanted ones, use "Update Reference"

### What's Broken

• Multiple sequential replacements in one session: Edge case
• Scenario: Remove 2 mods, replace 1 other mod, do replacements via context menu

### The Complexity Cost

To "fix" this properly would require:

1. Session-level state management (new service/coordinator)
2. Passing state between operations instead of reloading
3. Complex lifecycle management (when to clear session state?)
4. Potential memory leaks if state not cleaned properly
5. Testing all edge cases of session state persistence
6. More complex code that's harder to maintain

For a benefit of:

• Users can do multiple replacements in one session without them reverting

### Conclusion: Don't Fix It, Document It

Instead, we will document the limitation and provide a workaround.

See [18-multiple-replacements-help.md](18-multiple-replacements-help.md) for the proposed user guidance.
