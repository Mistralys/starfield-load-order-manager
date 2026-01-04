# Feature Plan: Group Dependent Mod Changes

## The Problem

Some changes to `Plugins.txt` can cause a cascade of position changes in the 
load order. For example, if a mod is deleted from a line in `Plugins.txt`, it 
causes all mods below it to change their position in the load order. 
In the DIFF window, this results in a large number of lines being marked as 
reordered, even though only one mod was actually removed. This makes it difficult 
to identify the actual changes made to the load order.

## Proposed Solution

### Tracking Dependent Changes

All mod changes must be analyzed to determine if they cause other mods to 
be reordered. If a change does, all dependent changes (resulting directly from 
that mod change) must be collapsed into a single entry that indicates the type
of change, without listing all the subsequent position changes.

### Known Change Types

#### Removed mod

Identified by:

- Mod is present in reference file.
- Mod is no longer present in `Plugins.txt`.

This case is already handled in the codebase.

#### Inserted mod

Identified by:

- Mod is not present in the reference file.
- Mod is present in `Plugins.txt`.
- Mod's position is above one of the mods in the reference file.

This case must be added in the codebase.

#### Added mod

Identified by:

- Mod is not present in the reference file.
- Mod is present in `Plugins.txt`.
- Mod's position is below all mods in the reference file.

#### Reordered mod

Identified by:

- Mod is present in the reference file.
- Mod is present in `Plugins.txt`.
- Mod's position is not the same as the reference file.

#### Unchanged mod

Identified by:

- Mod is present in the reference file.
- Mod is present in `Plugins.txt`.
- Mod's position is the same in both files.

### Dependency Detection

Once the change type of all mods has been determined, the change dependency
detection can be run. Only the following change types should be included in 
the dependency analysis:

- Removed mod
- Inserted mod

All other change types (added mod, reordered mod, unchanged mod) can be 
excluded from the dependency analysis, because they are handled manually
by the user through the mod sorting feature.

#### Detection Logic

Dependent mods of removed or inserted mods are detected by using these mods
as the starting point (e.g. a removed mod in line 5) to move down the list
of mods from there. For each subsequent mod, the following logic is applied:

- Is of change type "Reordered": Add to dependent changes.
- Any other change type: Stop.

### Visualizing Dependent Changes

To visualize that a mod causes other mods to be reorderd, a line is added directly 
below its collapsed entry. This summary line clearly states how many mods are directly
affected by the removal: "+ {NUMBER_MOD_POSITIONS} mod positions affected by this change".
The number of changes is the number of dependent changes of the mod.

Dependent change lines are hidden by default, and can be toggled by clicking on the summary
line.

## Implementation Steps

1. Modify the DIFF generation logic to track position changes, storing per 
   mod change which mods are directly affected.
2. Update the DIFF window to display collapsed mod entries, including the
   summary line below it indicating the number of affected mods.

### Existing logic

The existing logic for the DIFF window already detects most of what is needed,
and must be extended to support the new functionality. Please refer to the
Implementation Guidelines below and the existing code to dertmine the necessary
changes.

### Implementation Guidelines

Refer to the [Application Description](./application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](./project-manifest.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

See [MVVM Architecture Overview](./impl-mvvm-architecture-overview.md) for an overview of the MVVM architecture
used in the application.

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.

