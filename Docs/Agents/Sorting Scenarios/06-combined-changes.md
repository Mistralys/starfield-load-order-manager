# Scenario: Replaced a Mod

## Description

The mod "OutpostFishTank.esm" has been replaced with
"ReplacementMod.esm".

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
#13	*Easy Digipick.esm
#14	*Eli_RenamedSnowglobes.esm
#15	*Nanosuit_f_new.esm
#16	*OutpostFishTank.esm
#17	*Fragile.esm
#18	*GagarinNewDawn.esm
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
#09 *fixgraydockingcolors.esm
#10 *DayLengthMessage.esm
#11 *Eit_Clothiers_Z.esm
#12	*ReplacementMod.esm
#13	*Eli_RenamedSnowglobes.esm
#14	*Nanosuit_f_new.esm
#15 *InsertedMod.esm*
#16	*OutpostFishTank.esm
#17	*Fragile.esm
#18	*GagarinNewDawn.esm
```

## Detected Changes

The deleted mod causes mods coming after it to shift up.
The replaced mod is recognized despite also shifting up,
because this is not tied to position but to identity.
Mods further down shifting up is stopped by a mod being 
inserted.

- [#09 -] *BuySwimsuits.esm
- [#10->#09] *fixgraydockingcolors.esm
- [#11->#10] *DayLengthMessage.esm
- [#12->#11] *Eit_Clothiers_Z.esm
- [#13->#12 <>] *Easy Digipick.esm -> ReplacementMod.esm
- [#14->#13] *Eli_RenamedSnowglobes.esm
- [#15->#14] *Nanosuit_f_new.esm
- [#15 +] *InsertedMod.esm

### Dependent Changes

All mods that were shifted up by the deleted mod are 
detected as changes being dependent on the deleted mod:

- [#10->#09] *fixgraydockingcolors.esm
- [#11->#10] *DayLengthMessage.esm
- [#12->#11] *Eit_Clothiers_Z.esm
- [#13->#12 <>] *Easy Digipick.esm -> ReplacementMod.esm
- [#14->#13] *Eli_RenamedSnowglobes.esm
- [#15->#14] *Nanosuit_f_new.esm

It stops at the inserted mod, which is not dependent
on the deleted mod, and stops the ripple effect.

## Action Results

### Sorting

Only the inserted mod is affected by position shifts.

- [#09 -] *BuySwimsuits.esm
- [#10->#09] *fixgraydockingcolors.esm
- [#11->#10] *DayLengthMessage.esm
- [#12->#11] *Eit_Clothiers_Z.esm
- [#13->#12 <>] *Easy Digipick.esm -> ReplacementMod.esm
- [#14->#13] *Eli_RenamedSnowglobes.esm
- [#15->#14] *Nanosuit_f_new.esm
- [#16->#15] *OutpostFishTank.esm
- [#17->#16] *Fragile.esm
- [#18->#17] *GagarinNewDawn.esm
- [#18 +] *InsertedMod.esm

#### Dependent Changes

Because the inserted mod has been moved to the end,
the deleted mod's dependent changes are extended to
include all mods after the deleted mod:

- [#10->#09] *fixgraydockingcolors.esm
- [#11->#10] *DayLengthMessage.esm
- [#12->#11] *Eit_Clothiers_Z.esm
- [#13->#12 <>] *Easy Digipick.esm -> ReplacementMod.esm
- [#14->#13] *Eli_RenamedSnowglobes.esm
- [#15->#14] *Nanosuit_f_new.esm
- [#16->#15] *OutpostFishTank.esm
- [#17->#16] *Fragile.esm
- [#18->#17] *GagarinNewDawn.esm

The mod added at the end is not dependent on the deleted 
mod, so it is not included here.
