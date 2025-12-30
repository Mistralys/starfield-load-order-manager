# Change Resolution Controls

## Possible change states

### A) Removed mod

When a mod has been removed from the load order, there are two possible actions:

1. **Re-enable the mod**: The user can choose to re-enable the removed mod to restore the original numbered mod order.
1. **Replace with a newly added mod**: If the removal is combined with the addition of a new mod, the user can choose to replace the deleted line with one of the new mods.

### B) Newly added mods

The user can choose to remove newly added mods from the load order if they do not want them to be included.

### C) Modified mods (sort order)

When a mod has been moved in the load order, no user action is needed. The order can be fixed anytime
by clicking the "Sort Load Order" button. In fact, the DIFF window already informs the user of any
sort order changes detected, and recommends sorting the load order before resolving other issues.

## DIFF window update

In the DIFF window, the ListView control that shows the changes should be updated with a context menu that
allows the user to choose the desired action for each change. The options depend on the type of change:

- For **removed mods**, the context menu offers:
  - "Re-enable mod" to restore the mod to its original position.
  - "Replace with..." submenu listing newly added mods to replace the removed mod.
- For **newly added mods**, the context menu offers:
  - "Remove mod" to delete the newly added mod from the load order.
- For **modified mods**, no context menu is provided as no action is needed.

