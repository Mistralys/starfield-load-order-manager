# Scenario: Sorting Modified Externally

## Description

An external program has modified the load order by moving 
"AmazonCrew.esm" before "StarfieldCommunityPatch.esm". 

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
#01 *AmazonCrew.esm
#02 *StarfieldCommunityPatch.esm
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
#13	*Easy Digipick.esm
#14	*Eli_RenamedSnowglobes.esm
#15	*Nanosuit_f_new.esm
#16	*OutpostFishTank.esm
#17	*Fragile.esm
#18	*GagarinNewDawn.esm
```

## Expected Changes Detected

- [↑ #2->#1] AmazonCrew.esm
- [↓ #1->#2] StarfieldCommunityPatch.esm

## Expected Action Results

### Sorting

Sorting correctly repositions the modified mods, the result
being that there are no differences between the current
and reference orders.

- NO CHANGES
