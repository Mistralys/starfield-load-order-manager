# Scenario: Inserted and Moved Combination

## Description

A new mod has been inserted in the middle of the load order
while other existing mods have been reordered.

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
#02 *ShipBuilderCategories.esm
#03 *AmazonCrew.esm
#04 *BetterShipPartFlips.esm
#05 *InsertedMod.esm
#06 *BetterShipPartSnaps.esm
#07 *Better_Living.esm
#08 *Richer Merchants.esm
#09 *xatmosPerkUpVendors.esp
#10 *BuySwimsuits.esm
#11 *fixgraydockingcolors.esm
#12 *DayLengthMessage.esm
#13 *Eit_Clothiers_Z.esm
#14 *Easy Digipick.esm
#15 *Eli_RenamedSnowglobes.esm
#16 *Nanosuit_f_new.esm
#17 *OutpostFishTank.esm
#18 *Fragile.esm
#19 *GagarinNewDawn.esm
```

## Expected Changes Detected

New mod inserted and existing mods reordered.

- [? #02->#03] AmazonCrew.esm
- [? #03->#02] ShipBuilderCategories.esm
- [> #05] InsertedMod.esm
- [? #05->#06] BetterShipPartSnaps.esm
- [? #06->#07] Better_Living.esm
- [? #07->#08] Richer Merchants.esm
- [? #08->#09] xatmosPerkUpVendors.esp
- [? #09->#10] BuySwimsuits.esm
- [? #10->#11] fixgraydockingcolors.esm
- [? #11->#12] DayLengthMessage.esm
- [? #12->#13] Eit_Clothiers_Z.esm
- [? #13->#14] Easy Digipick.esm
- [? #14->#15] Eli_RenamedSnowglobes.esm
- [? #15->#16] Nanosuit_f_new.esm
- [? #16->#17] OutpostFishTank.esm
- [? #17->#18] Fragile.esm
- [? #18->#19] GagarinNewDawn.esm

### Dependent Mod Lists

- InsertedMod.esm
    - BetterShipPartSnaps.esm
    - Better_Living.esm
    - Richer Merchants.esm
    - xatmosPerkUpVendors.esp
    - BuySwimsuits.esm
    - fixgraydockingcolors.esm
    - DayLengthMessage.esm
    - Eit_Clothiers_Z.esm
    - Easy Digipick.esm
    - Eli_RenamedSnowglobes.esm
    - Nanosuit_f_new.esm
    - OutpostFishTank.esm
    - Fragile.esm
    - GagarinNewDawn.esm

All mods from position #05 onwards shifted down by one position
due to the insertion of InsertedMod.

## Expected Sorting Results

Sorting moves the inserted mod to the end and restores
the original order for moved mods.

- [+ #19] InsertedMod.esm

After sorting, AmazonCrew and ShipBuilderCategories return
to their reference positions, and all mods that were shifted
down by the insertion return to their original positions.

### Dependent Mod Lists

None - after sorting, all dependent changes are resolved.
