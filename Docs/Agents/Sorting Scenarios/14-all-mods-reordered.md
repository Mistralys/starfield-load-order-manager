# Scenario: All Mods Reordered

## Description

The entire load order has been completely reordered
(e.g., reversed or shuffled by an external tool).

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
#01 *GagarinNewDawn.esm
#02 *Fragile.esm
#03 *OutpostFishTank.esm
#04 *Nanosuit_f_new.esm
#05 *Eli_RenamedSnowglobes.esm
#06 *Easy Digipick.esm
#07 *Eit_Clothiers_Z.esm
#08 *DayLengthMessage.esm
#09 *fixgraydockingcolors.esm
#10 *BuySwimsuits.esm
#11 *xatmosPerkUpVendors.esp
#12 *Richer Merchants.esm
#13 *Better_Living.esm
#14 *BetterShipPartSnaps.esm
#15 *BetterShipPartFlips.esm
#16 *ShipBuilderCategories.esm
#17 *AmazonCrew.esm
#18 *StarfieldCommunityPatch.esm
```

## Expected Changes Detected

Complete reversal of the load order.

- [? #18->#01] GagarinNewDawn.esm
- [? #17->#02] Fragile.esm
- [? #16->#03] OutpostFishTank.esm
- [? #15->#04] Nanosuit_f_new.esm
- [? #14->#05] Eli_RenamedSnowglobes.esm
- [? #13->#06] Easy Digipick.esm
- [? #12->#07] Eit_Clothiers_Z.esm
- [? #11->#08] DayLengthMessage.esm
- [? #10->#09] fixgraydockingcolors.esm
- [? #09->#10] BuySwimsuits.esm
- [? #08->#11] xatmosPerkUpVendors.esp
- [? #07->#12] Richer Merchants.esm
- [? #06->#13] Better_Living.esm
- [? #05->#14] BetterShipPartSnaps.esm
- [? #04->#15] BetterShipPartFlips.esm
- [? #03->#16] ShipBuilderCategories.esm
- [? #02->#17] AmazonCrew.esm
- [? #01->#18] StarfieldCommunityPatch.esm

### Dependent Mod Lists

None - position changes from external modifications do not
create dependent relationships.

## Expected Sorting Results

Sorting restores all mods to their reference positions,
resulting in no differences between current and reference.

- NO CHANGES

### Dependent Mod Lists

None
