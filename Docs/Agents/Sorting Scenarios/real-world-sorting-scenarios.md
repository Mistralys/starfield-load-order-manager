# Real World Sorting Examples

This folder contains a collection of real-world examples for sorting
mod load orders. These scenarios illustrate various situations that users
may encounter when managing their mod load orders. Each example includes
the reference order, current order, expected changes, and the results of
applying sorting actions.

## Scenarios

### Basic Operations
1. **Added New Mod** - A single mod added to the end of the load order
2. **Sorting Modified Externally** - External tool swaps two mods
3. **Inserted New Mod** - A mod inserted in the middle of the load order
4. **Deleted Mod** - A single mod removed from the load order
5. **Replaced Mod** - A mod replaced with an alternative version

### Complex Operations
6. **Combined Changes** - Deletion, replacement, and insertion combined
7. **Multiple Deleted Mods** - Several mods removed at once
8. **Multiple Added Mods** - Several new mods added simultaneously
9. **Multiple Replaced Mods** - Multiple mod upgrades/replacements
10. **Multiple Moved Externally** - External tool reorders several mods
11. **Disabled Mods** - Mods with asterisk prefix removed
12. **Inserted and Moved Combination** - New mod inserted plus reordering

### Edge Cases
13. **Case Sensitivity** - Filename case changes (should be ignored)
14. **All Mods Reordered** - Complete reversal or shuffle of load order
15. **Whitespace and Comments** - File contains blank lines and comments

## Line Numbering

The example mod lists contain line numbers for clarity, in the format `#01`.
These line numbers are for reference only and are not part of the actual
mod load order files, and can be safely ignored when managing load orders.

## Change Type Markers

The expected changes sections use specific markers to indicate the type
of change detected:

- `~` : Replacement (mod replaced with another)
- `↑`  : Moved Up (mod moved to an earlier position)
- `↓`  : Moved Down (mod moved to a later position)
- `+`  : Added (mod added to the load order)
- `-`  : Removed (mod removed from the load order)
- `>` : Inserted (mod inserted at a specific position)
