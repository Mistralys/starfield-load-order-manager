# Scenario:Inserted A New Mod

## Description

The mod "InsertedMod.esm" has been inserted somewhere in
the existing mod list.

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
#16 *OutpostFishTank.esm
#17 *InsertedMod.esm
#18 *Fragile.esm
#19 *GagarinNewDawn.esm
```

## Expected Changes Detected

The inserted mod causes mods coming after it to shift down.

- [> #17] InsertedMod.esm
- [↓ #17->#18] Fragile.esm
- [↓ #18->#19] GagarinNewDawn.esm

## Expected Action Results

### Sorting

The sorting correctly appends the newly inserted mod to the 
end of the load order, preserving the order of existing mods.

- [+ #19] InsertedMod.esm
