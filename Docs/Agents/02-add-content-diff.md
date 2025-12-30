# Adding a file content DIFF 

When the mod manager or Starfield add new mod entries in the `Plugins.txt` file, clicking the
"Fix load order" button correctly updates the load order. However, the message that the file has
been modified is constantly shown, even after fixing the load order.

This behavior is expected: the "Fix load order" button only updates the load order, it does not 
revert any changes made to the file content itself. However, to make the changes visible to the 
uiser, and so that the user can decide whether to keep or revert them, we add a file content DIFF.

## The new DIFF button

A new button "Show Changes" is added next to the "Fix load order" button. This is disabled by default,
and enabled automatically when changes are detected. When clicked, it opens the DIFF dialog.

## The DIFF modal dialog

This modal dialog displays the differences between the current content of `Plugins.txt` and the reference
file. To keep the DIFF user-friendly, and because the file is mod-based, we can display a simple line-based
DIFF, highlighting added, removed, and modified lines. Because the file has already been sorted by load order, 
the DIFF will primarily focus on lines that have been added or removed.

## Moving the "Update reference file" button

To streamline the user interface, the "Update reference file" button is moved into the DIFF modal dialog.
This makes it clear that updating the reference file is an action related to the changes being reviewed.

## Implementation Notes

- Added a persistent "Show Changes" button beside the primary action that is enabled whenever `Plugins.txt` differs from the reference file.
- The new modal window renders a line-based diff (added, removed, unchanged) so users can review external changes before continuing.
- The reference update workflow now lives inside the diff dialog, reusing the existing reference command so users can promote the reviewed changes when desired.

