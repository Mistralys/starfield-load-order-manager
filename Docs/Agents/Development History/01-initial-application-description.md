# The problem to solve

The game Starfield uses a line-based text file called `Plugins.txt` in which each line contains
the file name of a mod to load when starting the game. This is typically referred to as the 
"Load Order". Once a save game has been created, it is crucial that the order of existing lines 
in the file is not modified: New lines can be added, but all previously enabled mod lines must
preserve their order.

Changing the load order in the middle of a save game can cause all manner of issues. Internal 
object references depend on the load order. For example, this means that if you are wearing a 
spacesuit that is added by a mod and that mod's position in the lkoad order changes, you will 
lose that spacesuit.

The problem is that the game itself, as well as mod manager tools, tend to change the order 
around - hence the need for a small application to observe and fix these changes.

## The application's working principle

- When the user is satisfied with the `Plugins.txt` file, make a copy of it.
- The copied file will be used as reference for the correct order of mods.
- When you run the tool, it will load the reference file and sort all entries according to the reference file's order.
- Any new mod files are appended at the end.
- A periodic check is performed on the `Plugins.txt` file to notify the user of changes on disk.
- Utility functions: Start the game (via SFSE or Vanilla if SFSE is not installed), access files and folders.

## The technology

The application will be a WPF .NET 9 application.

## File name case handling

My mod manager tool lowercases all mod file names. While Starfield does not mind this, it will restore the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the mod file names should always use the original file name case of the mods. This can be done by cross-referening the mod names with all `.esp` and `.esm` files as can be found in the game's installation folder under `Data` (where all mods are stored). This way, even if the mod manager lowercases mod names, they will always be restored to the original file name case.

## Configuration settings

The application needs two configuration settings:

- Local appdata Starfield folder location (where `Plugins.txt` is located)
- Starfield game installation folder (where the `Data` folder with mods is located)
- Optional: Automatic change detection interval (in seconds, default: 5 seconds)

These settings must be configured for the application to woirk correctly, so a status message is shown:

1. If they are not set
2. If any of the folders cannot be found on disk. 

None of the application's core features will work until these settings are correctly configured.

> NOTE: The folders cannot always be easily auto-discovered, as installations can vary a lot between gaming 
> platforms (Steam, GoG, etc.). However, to help with configuration, typical locations can be checked and 
> pre-filled if found.

## Example `Plugins.txt`

See the file [example-plugins.txt](./example-plugins.txt) for an example of a `Plugins.txt` file.

## File encoding

The `Plugins.txt` file must be encoded in UTF-8 without BOM (Byte Order Mark). The application expects this 
encoding to correctly read and write the file. In my tests, adding a BOM caused the game to ignore the first line
of the file.

## Whitespace handling

- Leading and trailing whitespace characters on each line are ignored when reading the `Plugins.txt` file.
- Empty lines at the end of the file are ignored when reading the `Plugins.txt` file.
- When writing the file back to disk, no leading or trailing whitespace characters are added.

## Reference file

The reference file is a copy of a known good `Plugins.txt` file. It is used to determine the correct load order
of mods. The reference file is stored in the application's data folder under the name `Plugins.reference.txt`.
When the user has added new mods and is satisfied with the load order, they can update the reference file
to reflect the current state of the `Plugins.txt` file, which is done by overwriting the reference file with the
current `Plugins.txt` file.

> NOTE: The reference file is created automatically by the application when it does not exist.

## The Play button

If the user wants to start the game from within the application, they can use the Play button. This automatically
detects if SFSE (Starfield Script Extender) is installed and uses it to start the game. If SFSE is not installed,
the game is started from its standard executable.

> NOTE: Whether SFSE is installed is determined by checking for the presence of the `sfse_loader.exe` file.

## Handling disabled mods

As illustrated by the example `Plugin.txt` above, all valid mod lines start with the character `*`. 
Any lines that do not start with this character are considered disabled by Starfield. **A disabled mod is 
functionally equivalent to a missing mod**. From the application's point of view, to keep the logic
as simple as possible, these lines are treated as if they did not exist.

> NOTE: This means that saving changes to the `Plugins.txt` file will remove any disabled mod lines from 
> the file. This has no adverse effect on the game, as Starfield ignores these lines anyway.

## Detecting changes

One of the core aspects of the application is to detect changes between the current `Plugins.txt` file and the
reference file. This includes detecting:

- Mods that have been moved (i.e., their position in the load order has changed)
- Mods that have been added (i.e., new mods that were not present in the reference file)
- Mods that have been removed (i.e., mods that were present in the reference file but are no longer in the current file)

Every mod is assigned a numerical position according to the line on which it was found, starting at 1 for 
the first line. When comparing the `Plugins.txt` and the reference file, the application uses both the mod
name and its assigned load order number to determine if a mod has been moved, added, or removed.

## Automatic change detection 

The application periodically checks the `Plugins.txt` file on disk for changes. The interval for this check
can be configured in the settings (default: every 5 seconds).

### Signature tracking 

Signature tracking is used internall to ensure that the automatic change detection, which is tied into the periodic 
file system checks, prevents the diff window from sitting stale. Instead, the user is informed of new changes
with accurate diff recommendations and zero manual action. 

## Managing changes: the DIFF window

A dedicated window (the DIFF window) is used to show and manage detected changes.

The window gives the user the possibility to choose what to do with the changes: either accept them (update the reference 
file) or revert them (restore the `Plugins.txt` file to match the reference file).

If there are sorting order changes to fix, a sorting recommendation hint is shown to the user to inform them that they 
should fix the load order before doing any other changes. 

