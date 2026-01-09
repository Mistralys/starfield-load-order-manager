# Scenario: Disabled Mods

## Description

Some mods have been disabled (the `*` prefix removed), which
causes them to be treated as removed from the load order.

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
#09 BuySwimsuits.esm
#10 *fixgraydockingcolors.esm
#11 *DayLengthMessage.esm
#12 Eit_Clothiers_Z.esm
#13 *Easy Digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 Nanosuit_f_new.esm
#16 *OutpostFishTank.esm
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
```

## Expected Changes Detected

Disabled mods are treated as removed from the load order.

- [-] BuySwimsuits.esm
- [-] Eit_Clothiers_Z.esm
- [-] Nanosuit_f_new.esm

### Dependent Mod Lists

None - disabled mods do not cause position shifts since
they remain in their file positions.

## Expected Sorting Results

Sorting does not change disabled mods. They remain in
the file but are excluded from the active load order.

- [-] BuySwimsuits.esm
- [-] Eit_Clothiers_Z.esm
- [-] Nanosuit_f_new.esm

### Dependent Mod Lists

None
