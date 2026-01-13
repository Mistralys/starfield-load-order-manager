# Scenario: Replacement with Position Shift

## Description

Multiple mods have been deleted, and a mod has been replaced
with a new version. The replacement occurs at a shifted position
due to earlier deletions.

In this scenario:
1. BetterShipPartSnaps.esm (position #5) is deleted
2. BuySwimsuits.esm (position #9) is deleted  
3. Fragile.esm (position #17) is replaced with Fragile2.esm

The replacement should be detected even though Fragile2.esm
appears at position #15 (shifted from #17 due to the 2 earlier
deletions).

## Reference Order

```
#01 *StarfieldCommunityPatch.esm
#02 *AmazonCrew.esm
#03 *ShipBuilderCategories.esm
#04 *BetterShipPartFlips.esm
#05 *BetterShipPartSnaps.esm
#06 *Better_Living.esm
#07 *Richer Merchants.esm
#08 *xatmosPerkUpVendors.esp
#09 *BuySwimsuits.esm
#10 *fixgraydockingcolors.esm
#11 *DayLengthMessage.esm
#12 *Eit_Clothiers_Z.esm
#13 *Easy Digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 *Nanosuit_f_new.esm
#16 *OutpostFishTank.esm
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
```

## Current Order

```
#01 *StarfieldCommunityPatch.esm
#02 *AmazonCrew.esm
#03 *ShipBuilderCategories.esm
#04 *BetterShipPartFlips.esm
#05 *Better_Living.esm
#06 *Richer Merchants.esm
#07 *xatmosPerkUpVendors.esp
#08 *fixgraydockingcolors.esm
#09 *DayLengthMessage.esm
#10 *Eit_Clothiers_Z.esm
#11 *Easy Digipick.esm
#12 *Eli_RenamedSnowglobes.esm
#13 *Nanosuit_f_new.esm
#14 *OutpostFishTank.esm
#15 *Fragile2.esm
#16 *GagarinNewDawn.esm
```

## Expected Changes Detected

The replacement should be detected despite the position shift
caused by the two deletions above it.

- [- #05] BetterShipPartSnaps.esm
- [- #09] BuySwimsuits.esm
- [~ #17->#15] Fragile.esm -> Fragile2.esm

### Dependent Mod Lists

BetterShipPartSnaps.esm deletion causes mods #6-8 to shift:
- BetterShipPartSnaps.esm
    - Better_Living.esm (6->5)
    - Richer Merchants.esm (7->6)
    - xatmosPerkUpVendors.esp (8->7)

BuySwimsuits.esm deletion causes mods #10-14 to shift:
- BuySwimsuits.esm
    - fixgraydockingcolors.esm (10->8)
    - DayLengthMessage.esm (11->9)
    - Eit_Clothiers_Z.esm (12->10)
    - Easy Digipick.esm (13->11)
    - Eli_RenamedSnowglobes.esm (14->12)
    - Nanosuit_f_new.esm (15->13)
    - OutpostFishTank.esm (16->14)

The replacement mod Fragile2.esm is at position 15, which is
Fragile.esm's shifted position (17 - 2 deletions = 15).
It should NOT be part of BuySwimsuits.esm's dependent changes.

GagarinNewDawn.esm (18->16) is dependent on the replacement
shift.

## Expected Sorting Results

After sorting, the replacement is preserved at its reference
position, and mods after it adjust accordingly.

- [- #05] BetterShipPartSnaps.esm
- [- #09] BuySwimsuits.esm
- [~ #17->#15] Fragile.esm -> Fragile2.esm

### Dependent Mod Lists

After sorting, all deletions have cascading dependent changes
that extend to the end of the list (since no insertions block them):

- BetterShipPartSnaps.esm
    - Better_Living.esm (6->5)
    - Richer Merchants.esm (7->6)
    - xatmosPerkUpVendors.esp (8->7)
    - fixgraydockingcolors.esm (10->8)
    - DayLengthMessage.esm (11->9)
    - Eit_Clothiers_Z.esm (12->10)
    - Easy Digipick.esm (13->11)
    - Eli_RenamedSnowglobes.esm (14->12)
    - Nanosuit_f_new.esm (15->13)
    - OutpostFishTank.esm (16->14)
    - Fragile2.esm (17->15)
    - GagarinNewDawn.esm (18->16)

- BuySwimsuits.esm (dependent changes stop at replacement)
    - (no dependent changes as all subsequent mods are already shifted by BetterShipPartSnaps)

The replacement itself is preserved as a user-directed change.
