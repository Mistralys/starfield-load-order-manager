# User Interface Guidelines

[? Back to Overview](README.md)

---

## Overview

The application follows **Material Design v5** guidelines with a consistent dark mode theme and modern aesthetic.

---

## Design Principles

The application adheres to these core design principles:

- **Dark mode theme** by default (Lime primary, LightGreen secondary)
- **Semantic color brushes** for theme consistency
- **Elevated surfaces** with shadows for depth
- **Rounded corners** (8px) for modern aesthetic
- **Responsive layouts** with proper spacing and padding

---

## Dialogs & Confirmations

### Custom Material Design Dialogs

The application uses custom styled dialogs instead of system `MessageBox`:

**Features**:
- Support for multiple icon types (Information, Question, Warning, Error)
- Color-coded icons (Blue, Purple, Orange, Red)
- Multiple button configurations (OK, OKCancel, YesNo, YesNoCancel)
- Transparent backgrounds with rounded corners and elevation shadows
- Scrollable message area for long text

### Confirmation Patterns

**Destructive Actions**:
- Show warning icon and detailed impact
- Default to "No" for safety
- Ellipsis in button labels indicates dialog will appear
- Clear action descriptions with bullet-point summaries

**Example**:
```
Title: "Discard Changes?"
Icon: Warning (Orange)
Message: "The following changes will be discarded:
  • 5 added mods
  • 2 removed mods
  • 10 position changes

This action cannot be undone."
Buttons: Yes, No (default)
```

---

## Button Conventions

### Button Types

**Raised buttons**: Primary actions (Save, Create, Update)
- Material Design elevated style
- Prominent visual emphasis
- Used for main action in a form

**Outlined buttons**: Secondary actions (Cancel, Browse, Close)
- Border with no fill
- Less visual weight than raised
- Used for alternative or cancel actions

**Flat buttons**: Tertiary/inline actions (detected paths)
- No border or elevation
- Minimal visual presence
- Used for secondary inline actions

### Button Labels

**Ellipsis suffix**: Indicates dialog or additional interaction
- "Save..." - Shows save confirmation dialog
- "Browse..." - Opens file/folder browser
- "Download options..." - Shows download dialog

**Action verbs**: Clear, specific action words
- Good: "Delete Profile", "Switch Profile", "Fix Load Order"
- Bad: "OK", "Submit", "Apply"

---

## Status Indicators

### Main Window

**Current Status**:
- Icon with color coding (Info, Success, Warning, Error)
- Timestamped message
- Updates in real-time

**Status History**:
- Last 3 status messages in list
- Chronological order (newest first)
- Each entry shows timestamp

**Color Coding**:
- Info: Primary brush (blue)
- Success: Tertiary brush (green)
- Warning: Secondary brush (orange)
- Error: Error brush (red)

**Banners**:
- Configuration error banner when paths invalid
- Update notification info bar when new version available
- Steam process warning banner when Steam is running
- Stack vertically when multiple banners present

### Settings Window

**Status Banner**:
- Permanent, always visible
- Success state (green): "The configured paths are valid."
- Error state (red): Specific validation error messages
- Cannot be dismissed
- Updates in real-time on field changes

### Change Badge

- Shows total changes including dependent changes
- Updates automatically as changes detected/resolved
- Button text: "Manage load order" or "Manage load order (X changes)"
- Badge appears only when changes present

### Sorting Recommendation

- Prominent banner when mods shifted position (not in dependent lists)
- Warning icon and colored text
- Explains need to sort first
- Dismissible after user acknowledges

### Steam Warning

- Shows when Steam process detected running
- Orange warning banner with helpful tooltip
- Non-blocking—warns but doesn't prevent operations
- Automatically disappears when Steam closes

---

## Window Types

### Modal Windows
Block parent interaction until closed:

- Settings Window
- Switch Profile Window
- Profile Properties Window
- Confirmation Dialog
- About Window
- Update Options Dialog
- Comment Input Dialog

**Behavior**:
- Center on parent window
- Dim parent window background
- Escape key closes (if `IsCancel` button present)
- Enter key activates default button (if `IsDefault` set)

### Non-Modal Windows
Allow parent interaction while open:

- Diff Window (tracks changes, prevents duplicates)
- Manage Profiles Window (tracks instance, prevents duplicates)
- Reference History Window (tracks instance, prevents duplicates, auto-refreshes)
- View Pending Changes Window (tracks instance, prevents duplicates)

**Behavior**:
- Independent positioning
- Single instance prevention
- Auto-refresh when data changes
- Can be kept open alongside main window

---

## Window Management

### Single Instance Prevention

Non-modal windows implement single instance pattern:

```csharp
if (_diffWindow != null && _diffWindow.IsVisible)
{
    _diffWindow.Activate();
    return;
}

_diffWindow = new DiffWindow();
_diffWindow.Show();
```

### Window Lifecycle

**Creation**:
- ViewModel injected via constructor
- DataContext set in code-behind
- Window positioned relative to parent

**Closing**:
- Non-modal windows set instance to null on close
- Modal windows return `DialogResult`
- ViewModels disposed to release resources

---

## Accessibility Features

### Keyboard Navigation

- Tab order follows logical flow
- `IsCancel` property on Cancel buttons (Escape key)
- `IsDefault` property on primary buttons (Enter key)
- Access keys on menu items (Alt+key)

### Visual Hierarchy

- Clear contrast ratios for text
- Consistent spacing and alignment
- Semantic color usage
- Icon + text for important actions

### Hover Effects

- Buttons show hover state
- Cards highlight on hover (profile switcher)
- Interactive elements provide visual feedback
- Cursor changes to pointer for clickable items

### Tooltips

- Descriptive tooltips on icon-only buttons
- Helpful guidance on complex features
- Auto-hide after delay
- Placement avoids obscuring content

---

## Material Design Components

### Cards

Used for profile display and grouping:
- 8px rounded corners
- Elevation shadow for depth
- Padding for content spacing
- Hover effect for interactive cards

### Lists

Used for status history, profile management:
- Alternating row backgrounds for readability
- Hover highlight on interactive rows
- Context menu on right-click
- Double-click action when applicable

### Text Fields

Used for configuration, profile editing:
- Floating label style
- Validation error display
- Helper text below field
- Icon prefix/suffix when appropriate

### Banners

Used for warnings and notifications:
- Full-width at top of window
- Color-coded by severity
- Icon + message + optional action button
- Dismissible or permanent based on type

---

## Layout Patterns

### Main Window

```
???????????????????????????????????????
? Menu Bar                            ?
? Active Profile (clickable)          ?
???????????????????????????????????????
? [Banners: Update, Error, Steam]    ?
???????????????????????????????????????
?                                     ?
? Main Content Area                   ?
?                                     ?
???????????????????????????????????????
? Status: Current message             ?
? History:                            ?
?   - Recent message 1                ?
?   - Recent message 2                ?
?   - Recent message 3                ?
???????????????????????????????????????
```

### Settings Window

```
???????????????????????????????????????
? Status Banner (always visible)      ?
???????????????????????????????????????
? AppData Path:                       ?
? [Text Field]          [Browse...]   ?
? Auto-detected: C:\...               ?
?                                     ?
? Game Path:                          ?
? [Text Field]          [Browse...]   ?
? Auto-detected: C:\...               ?
???????????????????????????????????????
?                [Cancel]  [Save]     ?
???????????????????????????????????????
```

### Diff Window

```
???????????????????????????????????????
? [Sorting Recommendation Banner]     ?
???????????????????????????????????????
? Diff List:                          ?
?   + Added mod                       ?
?   - Removed mod                     ?
?   ~ Moved mod (5?4)                ?
?   ? 10 dependent changes (click)   ?
?       ~ ModB (6?5)                 ?
?       ~ ModC (7?6)                 ?
???????????????????????????????????????
? [Update Reference...] [Fix] [Disc..]?
???????????????????????????????????????
? Status: Last operation at 14:56     ?
???????????????????????????????????????
```

---

## Color Palette

### Primary Colors

- **Primary**: Lime (#CDDC39)
- **PrimaryDark**: Lime 700
- **Secondary**: LightGreen (#8BC34A)
- **SecondaryDark**: LightGreen 700

### Semantic Colors

- **Success**: Green (#4CAF50)
- **Warning**: Orange (#FF9800)
- **Error**: Red (#F44336)
- **Info**: Blue (#2196F3)

### Surface Colors

- **Background**: #121212 (dark gray)
- **Surface**: #1E1E1E (slightly lighter)
- **SurfaceElevated**: #252525 (with shadow)

### Text Colors

- **PrimaryText**: #FFFFFF (white)
- **SecondaryText**: #B0B0B0 (light gray)
- **DisabledText**: #666666 (dark gray)

---

## Icon Usage

### Material Design Icons

- Consistent icon pack (Material Design Icons)
- 24x24px standard size
- 16x16px for inline icons
- Semantic icon choices

### Common Icons

- ? Checkmark: Success, valid, selected
- ? Alert: Warning, caution
- ? Error: Error, invalid, failed
- ? Info: Information, help
- ? Settings: Configuration
- ? Refresh: Reload, sync
- ? Upload: Update, save
- ? Download: Get, retrieve
- + Add: Create new
- × Remove: Delete, close

---

## Animation & Transitions

### Subtle Animations

- Fade in/out for dialogs (200ms)
- Hover state transitions (100ms)
- Expand/collapse animations (300ms)
- Smooth scrolling

### Performance

- GPU-accelerated transforms
- Avoid layout thrashing
- Cancel animations on rapid interaction
- Respect system animation settings

---

## Related Documentation

- **[MVVM Structure](Architecture/mvvm-structure.md)** - Data binding patterns
- **[All Features](README.md#core-features)** - UI implementations

---

## Design-Time Support

All XAML files include design-time attributes:

```xml
<Window 
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    mc:Ignorable="d"
    d:DesignHeight="600" d:DesignWidth="800">
```

Benefits:
- Visual Studio XAML designer preview
- IntelliSense for bindings
- Design-time data context
- Layout debugging
