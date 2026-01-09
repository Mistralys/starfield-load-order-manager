# Scenario: Case Sensitivity

## Description

Mod filename case has been changed in the plugins file,
which should be handled as the same mod (case-insensitive
matching).

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
#01 *starfieldcommunitypatch.esm
#02 *amazoncrew.esm
#03 *shipbuildercategories.esm
#04 *BETTERSHIPPARTFLIPS.ESM
#05 *BetterShipPartSnaps.esm
#06 *better_living.esm
#07 *Richer Merchants.esm
#08 *xatmosperkupvendors.esp
#09 *BuySwimsuits.esm
#10 *FIXGRAYDOCKINGCOLORS.ESM
#11 *DayLengthMessage.esm
#12 *Eit_Clothiers_Z.esm
#13 *easy digipick.esm
#14 *Eli_RenamedSnowglobes.esm
#15 *Nanosuit_f_new.esm
#16 *OutpostFishTank.esm
#17 *Fragile.esm
#18 *GagarinNewDawn.esm
```

## Expected Changes Detected

No real changes detected - case differences are ignored
for mod name comparison.

- NO CHANGES

### Dependent Mod Lists

None

## Expected Sorting Results

Sorting restores the proper case from the actual file system
to match the canonical filenames in the Data folder.

- NO CHANGES

The application should normalize case based on the actual
file names found in the Starfield Data folder, ensuring
consistency with the file system.

### Dependent Mod Lists

None
