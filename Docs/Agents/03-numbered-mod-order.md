# Application upgrade: Numbered Mod Order

For Starfield, the position of mods in the load order translates to a numbered mod order. This means that each mod is
assigned a specific number based on the line number in the `Plugins.txt` file. 

## Assigning a load order number to each mod

Mods are assigned numbers based on their line number in the target file. When reading the `Plugins.txt` file (or the 
reference file), the application must assign a load order number to each mod based on its position in the file. 
The first mod in the list is assigned number 1, the second mod number 2, and so on. Ideally, this should be done through 
mod objects to store the information.

When comparing the `Plugins.txt` and the reference file, the application uses both the mod name and its assigned number 
to determine if a mod has been moved, added, or removed.

## Impact of deleting lines

This application is a helper to guarantee that the numbered mod order remains consistent. In principle, lines must 
not be deleted once a game has been started with a specific mod order, as this would shift the numbers of all
subsequent mods, potentially causing issues with save games and mod functionality.

## Informing of removals

If a mod is removed from the load order, the application must inform the user of the consequences. In the DIFF window,
a message must be displayed indicating that a line has been deleted and that this must be resolved somehow to avoid
breaking the savegame.

## Possibilities for fixing disabled mods

1. **Re-enabling the mods**: The user can choose to re-enable the disabled mods to restore the original numbered mod order.
2. **Replace with a newly added mod**: If the removal is combined with the addition of a new mod, the user can choose to
   replace the deleted line with one of the new mods. 
3. **Manual adjustment**: The user can manually adjust the load order in `Plugins.txt` to ensure that the order remains consistent.

# Agent Upgrade Plan: Numbered Mod Order & Removals Handling

This guide describes how to upgrade the **Starfield Load Order Keeper** application to support the **Numbered Mod Order** behavior and **safe handling of removals**, based on the requirements in:

- `Docs/Agents/01-initial-agent-plan.md`
- `Docs/Agents/03-numbered-mod-order.md`

The steps are organized so that an implementation agent can follow them sequentially and keep the app functional at each stage.

---

## Phase 0 – Preconditions & Goals

### 0.1. Existing Behavior (from initial plan)

The current application:

- Uses a reference copy of `Plugins.txt` (`Plugins.reference.txt`) to preserve load order.
- Reorders the current `Plugins.txt` to:
  - Keep all mods in the same order as the reference.
  - Append any new mods at the end.
- Restores file name casing based on `.esm` / `.esp` files in the `Data` folder.

### 0.2. New Behavior (from numbered mod order doc)

New expectations:

1. Each mod line has an implicit **load order number** (1, 2, 3, …) based on its position in `Plugins.txt`.
2. The application must:
   - Track this number per mod (via model objects).
   - Use both **mod name** and **number** when comparing reference vs. current.
3. **Deleting lines** is dangerous:
   - Deletions shift all subsequent numbers.
   - The user must be warned via a DIFF UI that a line has been deleted.
4. The app should offer conceptual ways to fix issues:
   - Re-enable removed mods.
   - Replace deleted entries with newly added mods.
   - Manual adjustment in `Plugins.txt`.

This phase does **not** require you to fully implement a DIFF UI; it focuses on **data and API** that a UI agent can later consume.

---

## Phase 1 – Extend Models to Support Numbered Mod Order

### 1.1. Extend `ModEntryModel` to store line numbers

**Goal:** Each mod knows its position in the file (1-based index), both for **reference** and **current** states.

1. Open `ModEntryModel.cs` in the `LoadOrderKeeper.Models` namespace.
2. Add the following properties:

   - `int? LineNumber`  
     - The mod’s line index in the **current** file (1-based over mod lines, not including empty/comment lines).
   - `int? OriginalLineNumber`  
     - The mod’s line index in the **reference** file when it was first read.
   - Optional derived state for convenience:
     - `bool IsNew`
     - `bool IsRemoved`
     - `bool IsMoved` (`ReferenceNumber != null && CurrentNumber != null && different`)

3. Ensure **equality and hash code** remain based on the **file name only**, so they continue to work with `HashSet` and `Contains` for identity:

   - `Equals` and `GetHashCode` must **not** use line numbers.

### 1.2. Introduce a diff model for comparisons

Create a new model `ModDiffModel.cs` in `LoadOrderKeeper.Models`:

- Properties:
  - `string FileName`
  - `int? ReferenceNumber`
  - `int? CurrentNumber`
- Convenience flags:
  - `bool IsNew => ReferenceNumber == null && CurrentNumber != null;`
  - `bool IsRemoved => ReferenceNumber != null && CurrentNumber == null;`
  - `bool IsMoved => ReferenceNumber != null && CurrentNumber != null && ReferenceNumber != CurrentNumber;`

This model will be used by the DIFF/diagnostics logic and later by the DIFF UI.

---

## Phase 2 – Numbering Mods While Reading Files

### 2.1. Update `FileService.ReadFileAsync` to assign line numbers

**Goal:** Whenever a mod file (`Plugins.txt` or reference) is read into `ModEntryModel` objects, assign a **1-based line number** according to its position *among mod lines*.

Steps:

1. Open `FileService.cs`.
2. Locate the private `ReadFileAsync(string filePath)` method.
3. Change the implementation to:

   - Read all lines.
   - Iterate with an internal `logicalIndex` counter starting at 0.
   - Skip:
     - Empty / whitespace-only lines.
     - Comment lines starting with `#`.
   - For every remaining line:
     - Increment `logicalIndex`.
     - Create `ModEntryModel` with the line text and `lineNumber: logicalIndex`.

4. This ensures:
   - `LineNumber` for a mod is its visible, effective position in the load order list.
   - Reference reads will initialize both `LineNumber` and `OriginalLineNumber` to the same value.

**Note:** You should continue preserving the raw file structure when copying to `Plugins.reference.txt`. Numbering is only for models.

---

## Phase 3 – Add Comparison API for Numbered Mod Order

### 3.1. Implement a diff function in `FileService`

**Goal:** Provide a reusable way to ask: “What changed between reference and current, considering both names and numbers?”

Steps:

1. In `FileService.cs`, add a new public method:
