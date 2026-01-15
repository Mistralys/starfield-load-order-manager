# Version Check

[? Back to Overview](../README.md)

---

## Overview

The application automatically checks for updates and notifies users when new versions are available.

---

## Automatic Update Check

### Background Check

- Runs silently when application starts
- Checks GitHub API for latest release
- Compares current version with latest stable release
- Uses 24-hour caching to avoid excessive API calls
- Fails silently if network unavailable

### Smart Version Comparison

- Uses semantic versioning (Major.Minor.Patch)
- Ignores pre-release versions (beta, rc, etc.)
- Only notifies for newer stable releases
- Handles version downgrades correctly (no notification)

### Update Notification

- Non-intrusive info bar appears at top of main window
- Shows update message: "Version X.X.X is available!"
- Provides "Download options..." button
- Can be dismissed for current session
- Reappears on next app launch if update still available

---

## Manual Update Check

### Check for Updates Menu

- Located in Help menu for easy access
- Bypasses 24-hour cache for immediate check
- Shows success dialog if already on latest version
- Displays download options dialog if update available

### Download Options Dialog

- Material Design styled with prominent buttons
- Shows current and latest version numbers
- Two download sources with clickable buttons:
  - **Nexusmods**: Primary distribution platform
  - **GitHub Releases**: Alternative source
- Opens selected download page in default browser
- Closes automatically after selection

---

## Error Handling

### Network Failures

- Background check fails silently (no user disruption)
- Manual check shows download options dialog
- Explains inability to check automatically
- Still provides access to download pages

### GitHub API Rate Limits

- Cached results prevent hitting rate limits
- 24-hour cache duration balances freshness and API limits
- Unauthenticated requests (no token required)
- Suitable for small to medium user base

---

## Technical Details

### GitHub API Integration

- Queries: `https://api.github.com/repos/Mistralys/starfield-load-order-manager/releases/latest`
- 10-second timeout for network requests
- Parses release tag name for version number
- Checks `prerelease` flag to filter beta versions

### Caching System

- Cache file: `%LOCALAPPDATA%\StarfieldLoadOrderKeeper\update-check-cache.json`
- Stores timestamp and last check result
- 24-hour expiration
- Survives application restarts

### Version Source

- Current version from assembly attributes via `VersionService`
- Latest version from GitHub release tag name
- Strips commit hashes and extra metadata
- Clean semantic version format (e.g., "1.4.0")

### Download Locations

- **Nexusmods**: https://www.nexusmods.com/starfield/mods/15786
- **GitHub Releases**: https://github.com/Mistralys/starfield-load-order-manager/releases
- URLs stored as constants for easy maintenance
- Both options presented equally in download dialog

---

## Related Features

- **[UI Guidelines](../ui-guidelines.md)** - Info bar and dialog design patterns

---

## Technical Implementation

**Key Classes**:
- `UpdateCheckCoordinator` - Update checking logic and caching
- `UpdateCheckService` - GitHub API integration
- `MainViewModel` - Update notification display
- `UpdateOptionsViewModel` - Download options dialog

**Caching Strategy**:
- File-based cache in LocalAppData
- 24-hour TTL with timestamp comparison
- Thread-safe read/write operations
- Graceful fallback on cache errors

**See Also**:
- [Coordinator Pattern](../Architecture/coordinator-pattern.md) - UpdateCheckCoordinator details
- [UI Guidelines](../ui-guidelines.md) - Non-intrusive notification patterns
