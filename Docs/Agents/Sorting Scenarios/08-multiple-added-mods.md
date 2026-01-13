# Scenario: Multiple Mods Added

## Description

Multiple new mods have been added to the end of the load order
(e.g., from a mod pack installation or bulk manual addition).

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
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
#19 *NewModA.esm
#20 *NewModB.esm
#21 *NewModC.esm
```

## Expected Changes Detected

Multiple new mods added at the end of the load order.

- [+] NewModA.esm
- [+] NewModB.esm
- [+] NewModC.esm

### Dependent Mod Lists

None - additions do not cause dependent changes.

## Expected Sorting Results

Sorting preserves the newly added mods at the end,
maintaining their relative order.

- [+] NewModA.esm
- [+] NewModB.esm
- [+] NewModC.esm

### Dependent Mod Lists

None
