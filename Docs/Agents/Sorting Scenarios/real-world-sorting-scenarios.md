# Real World Sorting Examples

This folder contains a collection of real-world examples for sorting
mod load orders. These scenarios illustrate various situations that users
may encounter when managing their mod load orders. Each example includes
the reference order, current order, expected changes, and the results of
applying sorting actions.

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
