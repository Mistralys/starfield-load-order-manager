# Scenario: Deleted a Mod

## Description

The mod OutpostFishTank.esm has been removed from the existing 
mod list.

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
#09 *BuySwimsuits.esm
#10 *fixgraydockingcolors.esm
#11 *DayLengthMessage.esm
#12 *Eit_Clothiers_Z.esm
#13 *Easy Digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 *Nanosuit_f_new.esm
#16 *Fragile.esm
#17 *GagarinNewDawn.esm
```

## Expected Changes Detected

The deleted mod causes mods coming after it to shift up.

- [- #16] OutpostFishTank.esm
- [↑ #17->#16] Fragile.esm
- [↑ #18->#17] GagarinNewDawn.esm

## Expected Action Results

### Sorting

Sorting does not change anything to the fact that the
mod has been deleted, so the result remains:

- [- #16] OutpostFishTank.esm
- [↑ #17->#16] Fragile.esm
- [↑ #18->#17] GagarinNewDawn.esm

