# The problem to solve

The game Starfield uses a line-based text file called `Plugins.txt` in which each line contains
the file name of a mod to load when starting the game. This is typically referred to as the 
"Load Order". Once a save game has been created, it is crucial that the order of existing lines 
in the file is not modified: New lines can be added, but the existing ones must stay in the 
same order.

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
- Every mod is assigned a numerical position according to the line on which it was found, starting at 1 for the first line.

## The technology

The application will be a WPF .NET 9 application.

## File name case handling

My mod manager tool lowercases all mod file names. While Starfield does not mind this, it will restore the correct file name case when it loads the `Plugins.txt` file.

To guarantee stable `Plugins.txt` contents, the mod file names should always use the original file name case of the mods. This can be done by cross-referening the mod names with all `.esp` and `.esm` files as can be found in the game's installation folder under `Data` (where all mods are stored). This way, even if the mod manager lowercases mod names, they will always be restored to the original file name case.

## Configuration settings

The application needs two configuration settings:

- Local appdata Starfield folder location (where `Plugins.txt` is located)
- Starfield game installation folder (where the `Data` folder with mods is located)

These settings should be configured first thing when running the application if they are not set, or if any of the folders cannot be found on disk. The folders cannot always be easily auto-discovered, as installations can vary a lot between gaming platforms (Steam, GoG, etc.). However, to help with configuration, typical locations can be checked and pre-filled if found.

## Example `Plugins.txt`

See the file [example-plugins.txt](./example-plugins.txt) for an example of a `Plugins.txt` file.

## Handling disabled mods

As illustrated by the example `Plugin.txt` above, all valid mod lines start with the character `*`. 
Any lines that do not start with this character are considered disabled. **A disabled mod is functionally 
equivalent to a missing mod**. Thus, when a mod is disabled, it should be treated as if it is not present 
in the load order.

## Detecting changes

One of the core aspects of the application is to detect changes between the current `Plugins.txt` file and the
reference file. This includes detecting:

- Mods that have been moved (i.e., their position in the load order has changed)
- Mods that have been added (i.e., new mods that were not present in the reference file)
- Mods that have been removed (i.e., mods that were present in the reference file but are no longer in the current file)

When comparing the `Plugins.txt` and the reference file, the application uses both the mod name and its 
assigned load order number to determine if a mod has been moved, added, or removed.

