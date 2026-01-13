# Scenario: Multiple Mods Moved Externally

## Description

An external tool has reordered multiple mods in the load order,
moving them to different positions.

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
#01 *Fragile.esm
#02 *StarfieldCommunityPatch.esm
#03 *BetterShipPartSnaps.esm
#04 *AmazonCrew.esm
#05 *ShipBuilderCategories.esm
#06 *BetterShipPartFlips.esm
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
#18 *GagarinNewDawn.esm
```

## Expected Changes Detected

Multiple mods have been moved to different positions by
an external tool.

- [? #17->#01] Fragile.esm
- [? #01->#02] StarfieldCommunityPatch.esm
- [? #05->#03] BetterShipPartSnaps.esm
- [? #02->#04] AmazonCrew.esm

### Dependent Mod Lists

None - position changes from external modifications do not
create dependent relationships.

## Expected Sorting Results

Sorting correctly repositions the modified mods, the result
being that there are no differences between the current
and reference orders.

- NO CHANGES

### Dependent Mod Lists

None
