# Changelog

## v1.8.1 - Language Update
- Translation: Language handling now with with zero-hardcoding architecture.
- Translation: Fixed screens that had untranslated texts leftover.
- Translation: Added translations: Simplified Chinese, Japanese.

## v1.8.0 - Language Selection
- Translation: The app language can be changed in the settings.
- Translation: Added support for multiple languages. Uses the system language by default ([#10](https://github.com/Mistralys/starfield-load-order-manager/issues/10)).
- Translation: Added translations: German, French, Spanish, Italian.

## v1.7.3 - Bug Fix
- Fixed: Exception on startup when no configuration exists.
- Configuration: Removed the automatic opening of the config window ([#27](https://github.com/Mistralys/starfield-load-order-manager/pull/27)).

## v1.7.2 - Exception Dialog
- General: Added a global exception dialog to show unhandled exceptions in a user-friendly manner.
- General: Added logging for unhandled exceptions to aid in debugging.
- Core: Sanity check update of the project manifest and application description.
- Tests: Added a comprehensive suite of tests that were incomplete or missing.

## v1.7.1 - Invalid Config Handling
- Configuration: Removed all paths leading to the app closing on its own.
- General: Added error overlays to all windows when the configuration is invalid.
- General: The "Manage changes" button now stays enabled even if the configuration is invalid.
- General: More graceful handling of invalid configuration on startup.
- Core: Added a debug menu item to open the app's configuration folder.

## v1.7.0 - Pending Changes Dialog
- Profiles: Added a dialog to view all pending changes.
- Profiles: Fixed the change comment being stored for the wrong version in the version archive ([#14](https://github.com/Mistralys/starfield-load-order-manager/issues/14)).

## v1.6.1 - Bug Fixes
- Settings: The settings are now saved even if the window is closed via the "X" button.
- Settings: Fixed the "Edit settings" button in the configuration missing banner causing a crash.
- Core: Added a debug-time "Debug" menu to reset the settings for easier testing.

## v1.6.0 - Improved Change Handling
- General: Added Steam process detection to inform when SFSE needs Steam running ([#15](https://github.com/Mistralys/starfield-load-order-manager/issues/15)).
- General: Added a debug feature to export the current application state to JSON for easier debugging.
- Changes window: Added a menu strip with all existing actions.
- Changes window: Added the "Help" > "Copy Debug State" menu item.
- Changes Window: Fixed replaced mods being unrecognized after sorting ([#12](https://github.com/Mistralys/starfield-load-order-manager/issues/12)).
- Changes Window: Improved sorting recommendation, now shown more intelligently.
- Changes Window: Added an info banner when multiple replacements are detected.
- Changes Window: Added the same utility File menu items as the main window.
- Changes Window: Added button icons.
- Core: Added a comprehensive suite of sorting scenarios for documentation and testing.
- Core: Split the main window logic into separate files for better maintainability.

## v1.5.0 - Improved Error Handling
- Settings: A banner now shows instantly if the selected folders are valid.
- Settings: Removed the update delay setting.
- Profiles: Improved error messages when issues with the `Pofiles` folder occur.
- General: An error banner now informs of configuration issues.
- General: Improved error messages when there are configuration or file access issues.
- General: The `Plugins.txt` file is now expected to exist.
- General: Improved handling of application exit to prevent lingering processes.

## v1.4.1 - Bug Fixes
- Core: Fixed the version check recognizing older versions as newer ones.

## v1.4.0 - Version History
- Versioning: Changes to reference files are now tracked with version history.
- General: Improved data grid styling.
- General: Renamed some menu items and buttons for clarity.
- Settings: Now searching in Steam library folders for custom game installations ([#5](https://github.com/Mistralys/starfield-load-order-manager/issues/5)).
- Core: Moved central style presets into separate files.
- Core: Added application icon.
- Core: Added an application version check ([#6](https://github.com/Mistralys/starfield-load-order-manager/issues/6)).

## v1.3.0 - Settings helper
- Settings: Now auto-detecting the game path for Steam users.
- Changes window: Now grouping dependent mod changes together for better clarity.
- Changes window: Added a confirmation when updating the reference file.
- Changes window: Added a confirmation to reset all changes.
- General: Confirmation and message dialogs now inherit the dark theme.

## v1.2.0 - Minor Improvements
- Removed version commit hash from the title bar and about window.
- Now displaying up to the last three status messages in the main window.

## v1.1.0 - About
- Added an About window with application information.
- The changelist window can now be opened even if there are no changes.
- Moved the Settings menu item into a new "Edit" menu.

## v1.0.0 - Initial Release
- First release with profile switching.
