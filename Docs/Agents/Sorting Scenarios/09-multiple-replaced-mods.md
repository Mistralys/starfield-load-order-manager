# Scenario: Multiple Mods Replaced

## Description

Multiple mods have been replaced with updated versions
or alternatives (e.g., upgrading a mod pack).

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
#05 *BetterShipPartSnaps.esm
#06 *Better_Living.esm
#07 *Richer Merchants.esm
#08 *xatmosPerkUpVendors.esp
#09 *BuySwimsuits_v2.esm
#10 *fixgraydockingcolors.esm
#11 *DayLengthMessage.esm
#12 *Eit_Clothiers_Enhanced.esm
#13 *Easy Digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 *Nanosuit_f_new.esm
#16 *ImprovedFishTank.esm
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
```

## Expected Changes Detected

Multiple mods replaced with alternative versions.

- [~ #09] BuySwimsuits.esm -> BuySwimsuits_v2.esm
- [~ #12] Eit_Clothiers_Z.esm -> Eit_Clothiers_Enhanced.esm
- [~ #16] OutpostFishTank.esm -> ImprovedFishTank.esm

### Dependent Mod Lists

None - replacements are user-directed changes.

## Expected Sorting Results

Because mod replacements are considered user-directed changes,
replacements are excluded from sorting actions. Therefore, no
sorting changes are made to the load order.

- [~ #09] BuySwimsuits.esm -> BuySwimsuits_v2.esm
- [~ #12] Eit_Clothiers_Z.esm -> Eit_Clothiers_Enhanced.esm
- [~ #16] OutpostFishTank.esm -> ImprovedFishTank.esm

### Dependent Mod Lists

None
