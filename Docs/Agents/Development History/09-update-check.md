# Update Check

Currently, the version of the app is shown in the "About" dialog. This new feature aims to add a version
check to verify if a newer version is available online.

## Release Process

The project uses GitHub actions to automate building binaries and creating releases based on tags. 
The release implementation is available in [release.yml](/github/workflows/release.yml), and the 
WiX installer configuration files are in the [installer](/installer) folder.

The version is inferred from the `changelog.md` file at build time. The tags are created to match
the latest version in the changelog.

## UI Integration

The version check will be done silently in the background when the application has started. If a 
newer version is found, a notification will be shown to the user with a link to the download page.
This notification should be non-intrusive and allow the user to continue using the application 
without interruption.

Preferred implementation is an "Info Bar" at the top of the main window, similar to notifications 
seen in web browsers,and which can be dismissed once per session.

### Help Menu

A new menu item "Check for Updates" will be added to the "Help" menu. This will trigger a manual
version check, bypassing any caching mechanism. If a newer version is found, the same notification
will be shown. If no newer version is found, a dialog will inform the user that they are using
the latest version.

## Version Check Implementation

The version check will be implemented by querying the GitHub API to get the latest release information,
and compare this with the current application version. The check must be done asynchronously and silently
on application startup.

### Version Comparison Logic

The project uses semantic versioning (SemVer) for versioning, with three components: Major, Minor, 
and Patch (e.g. 1.2.0). Only stable releases must be considered for the version check. Pre-releases
like betas (1.2.0-beta) or release candidates (1.2.0-rc) should be ignored.

Downgrades or equal versions should not trigger any notification. Only higher versions should inform
the user about an available update.

### API Requests and Caching

The version check must use unauthenticated requests to the GitHub API to avoid requiring users
to provide personal access tokens. The amount of concurrent users is currently expected to be
low. If this should change in the future, other methods will be considered.

To avoid excessive API calls, the application should cache the result of the version check for a
whole 24 hours. Subsequent checks within this period should use the cached result. The user can 
use the manual "Check for Updates" option in the "Help" menu to bypass the cache and perform an 
immediate check.

### Error Handling

If the GitHub API is unreachable or returns an error (like having reached the rate limit for
unauthenticated requests), the application should fail silently.

If the user manually triggered the version check, a dialog should be shown on failure to inform 
the user of the issue. This dialog should contain the links to the download pages for manual 
update checks.

### GitHub Repository

The GitHub repository for the project is located at:

https://github.com/Mistralys/starfield-load-order-manager

> NOTE: The repository owner and project name should be stored in a constant to allow easy 
> modification in the future. 

### Releases Page

There are two download pages for the application, which the user can choose from depending on
their preference: 

1. Nexusmods Page: https://www.nexusmods.com/starfield/mods/15786
1. GitHub Releases Page: https://github.com/Mistralys/starfield-load-order-manager/releases

> NOTE: These links should be stored in constants to allow easy modification in the future.

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
