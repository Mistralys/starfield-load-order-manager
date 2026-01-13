# Scenario: Whitespace and Comments

## Description

The plugins file contains blank lines and comment lines
interspersed with mod entries, which should be ignored
during comparison.

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
# Core mods section
#01 *StarfieldCommunityPatch.esm
#02 *AmazonCrew.esm

#03 *ShipBuilderCategories.esm
#04 *BetterShipPartFlips.esm
#05 *BetterShipPartSnaps.esm

# Quality of life mods
#06 *Better_Living.esm
#07 *Richer Merchants.esm
#08 *xatmosPerkUpVendors.esp
#09 *BuySwimsuits.esm

#10 *fixgraydockingcolors.esm
#11 *DayLengthMessage.esm

# Miscellaneous mods
#12 *Eit_Clothiers_Z.esm
#13 *Easy Digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 *Nanosuit_f_new.esm

#16 *OutpostFishTank.esm
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
```

## Expected Changes Detected

No real changes - blank lines and comments are ignored.

- NO CHANGES

### Dependent Mod Lists

None

## Expected Sorting Results

Sorting preserves the active mods and cleans up
whitespace and comments, writing a normalized file.

- NO CHANGES

The application should strip comments and normalize
whitespace when writing the plugins file.

### Dependent Mod Lists

None
