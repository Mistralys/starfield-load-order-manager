# Steam Library Integration

## The Problem

Currently, when using the Settings window, it is attempted to detect the 
Starfield installation path for Steam users. However, this assumes that
Starfield is installed in the default Steam library location. However, it
can be installed in any of the user's Steam library folders, which can
be freely added by users.

## The Solution

Instead of checking only the main Steam installation folder, a more robust 
solution must be implemented that reads Steam's library folders from the 
Steam configuration, and checks each one for the Starfield installation.

## Implementation Details

### Steam Library Folders configuration

Steam stores its library folders in a file named `libraryfolders.vdf`,
which is located in the `steamapps` folder of the main Steam installation.
The format of this file is a Valve Data Format (VDF), which is a key-value
store format used by Valve for configuration files.

### VDF format parser library

The package "Gameloop.Vdf" can be used to parse VDF files in C#. This package
has already been added to the project, and is ready to be used.

### Parsing the library VDF file

An example of a VDF file structure is available in the file 
[example-steam-library.vdf](/Docs/Agents/example-steam-library.vdf).

The libraries are listed under the `libraryfolders` key, with each library 
folder having a numeric key (0, 1, 2, ...). Each library folder is an object 
with multiple properties. The installed apps are stored under the `apps`
property, with each app having its AppID as the key.

Starfield's app ID is `1716740`.

The presence of the AppID under the `apps` property indicates that the game
is installed in that library folder. The absolute installation path to the 
library is stored in the `path` property of the library folder object.

The Starfield installation path can be constructed by combining the library
path with the relative path to the game's installation folder, which is
`steamapps/common/Starfield`.

### Implementation Steps

1. Locate the main Steam installation folder (this has already been implemented).
1. Parse the `libraryfolders.vdf` file located in the main Steam install folder using the Gameloop.Vdf library.
1. Iterate through each library folder listed in the VDF file.
1. Check if the Starfield AppID (`1716740`) is present in the `apps` property of the library folder.
1. If found, use that folder to return the detected Starfield installation path.

> NOTE: In the unlikely event that Starfield is installed in multiple library folders,
> the first one found should be used.

### Error handling

If the Steam installation cannot be detected, or if the `libraryfolders.vdf` file
cannot be found or parsed the process may fail silently. At worst, no default location
will be detected for the Starfield installation, and the user will have to manually select it.

### Performance and caching

No special performance considerations are necessary, as this process is only performed occasionally.

## Implementation Guidelines

Refer to the [Application Description](./application-description.md) for a high-level overview of the
application's goals, features and architecture.

Refer to the [Project Manifest](./project-manifest.md) document for an overview of the tech stack, file tree,
architecture, MVVM patterns and key components of the application.

Refer to the [Implementation Guidelines](../implementation-guidelines.md) document for guidelines
on implementing features (code behind and UI). 

See [MVVM Architecture Overview](./impl-mvvm-architecture-overview.md) for an overview of the MVVM architecture
used in the application.

**IMPORTANT**: Overall, don't ask about existing architecture; follow the established patterns in the codebase.
