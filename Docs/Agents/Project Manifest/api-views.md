# Views & Converters API

> Public API signatures for Views (Windows), User Controls, and Value Converters.

---

## Application Entry Point

### `LoadOrderKeeper.App`

```csharp
public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e);
}
```

---

## Main Windows

### `LoadOrderKeeper.MainWindow`

```csharp
public partial class MainWindow : Window
{
    public MainWindow();
}
```

### `LoadOrderKeeper.Views.SettingsWindow`

```csharp
public partial class SettingsWindow : Window
{
    public SettingsWindow();
}
```

---

## Non-Modal Windows

### `LoadOrderKeeper.Views.DiffWindow`

```csharp
public partial class DiffWindow : Window
{
    public DiffWindow();
}
```

**Configuration Overlay:**
- Includes `ConfigInvalidOverlay` control with `Grid.RowSpan` spanning all rows.
- Overlay visibility bound to `ShowOverlay` property in ViewModel.
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all content.

### `LoadOrderKeeper.Views.ManageProfilesWindow`

```csharp
public partial class ManageProfilesWindow : Window
{
    public ManageProfilesWindow(AppConfigModel config);
}
```

**Configuration Overlay:**
- Includes `ConfigInvalidOverlay` control with `Grid.RowSpan` spanning all rows.
- Overlay visibility bound to `ShowOverlay` property in ViewModel.
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all content.

### `LoadOrderKeeper.Views.ReferenceHistoryWindow`

```csharp
public partial class ReferenceHistoryWindow : Window
{
    public ReferenceHistoryWindow();
}
```

**Configuration Overlay:**
- Includes `ConfigInvalidOverlay` control with `Grid.RowSpan` spanning all rows.
- Overlay visibility bound to `ShowOverlay` property in ViewModel.
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all content.

### `LoadOrderKeeper.Views.ViewPendingChangesWindow`

```csharp
public partial class ViewPendingChangesWindow : Window
{
    public ViewPendingChangesWindow();
}
```

**Configuration Overlay:**
- Includes `ConfigInvalidOverlay` control with `Grid.RowSpan` spanning all rows.
- Overlay visibility bound to `ShowOverlay` property in ViewModel.
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all content.

---

## Modal Dialog Windows

### `LoadOrderKeeper.Views.SwitchProfileWindow`

```csharp
public partial class SwitchProfileWindow : Window
{
    public SwitchProfileWindow(AppConfigModel config);
}
```

**Configuration Overlay:**
- Includes `ConfigInvalidOverlay` control with `Grid.RowSpan` spanning all rows.
- Overlay visibility bound to `ShowOverlay` property in ViewModel.
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all content.

### `LoadOrderKeeper.Views.ProfilePropertiesWindow`

```csharp
public partial class ProfilePropertiesWindow : Window
{
    public ProfilePropertiesWindow();
}
```

### `LoadOrderKeeper.Views.ConfirmationDialog`

```csharp
public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog();
    public ConfirmationDialog(string title, string message, ConfirmationIcon icon = ConfirmationIcon.None, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK);

    public new ConfirmationResult ShowDialog();
    public static ConfirmationResult Show(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Information, ConfirmationButton buttons = ConfirmationButton.OK, ConfirmationResult defaultResult = ConfirmationResult.OK, Window? owner = null);
}
```

### `LoadOrderKeeper.Views.CommentInputDialog`

```csharp
public partial class CommentInputDialog : Window
{
    public CommentInputDialog();
    public CommentInputDialog(string? existingComment);

    public string? Comment { get; }
}
```

### `LoadOrderKeeper.Views.AboutWindow`

```csharp
public partial class AboutWindow : Window
{
    public AboutWindow();
}
```

### `LoadOrderKeeper.Views.UpdateOptionsDialog`

```csharp
public partial class UpdateOptionsDialog : Window
{
    public UpdateOptionsDialog();
}
```

### `LoadOrderKeeper.Views.ErrorDialog`

```csharp
public partial class ErrorDialog : Window
{
    public ErrorDialog();
}
```

**Exception Dialog:**
- Material Design v5 styled dialog for displaying unhandled exceptions.
- Fixed size: 600x420 (non-resizable), always centered on screen.
- Alert icon (48x48) in error color at top.
- Title: "An Unexpected Error Occurred".
- Exception message displayed in readable text.
- Scrollable error details section showing exception type and message.
- Four action buttons arranged in two rows:
  - Primary row: "Open Log Folder" (opens app data folder), "Report Bug" (opens GitHub issues)
  - Secondary row: "Exit" (recommended, closes app), "Ignore (Unsafe)" (continues running, warning color)
- `CloseRequested` event raised when Ignore button clicked.
- `ExitRequested` event raised when Exit button clicked (also triggers app shutdown).

---

## User Controls

### `LoadOrderKeeper.Controls.ConfigInvalidOverlay`

```csharp
public partial class ConfigInvalidOverlay : UserControl
{
    public ConfigInvalidOverlay();
}
```

**Design:**
- Material Design v5 styled overlay control for blocking window interaction when configuration is invalid.
- Semi-transparent dark background (`#CC000000`) covering entire window area.
- Centered message card with Material Design elevation shadow.
- Alert icon (48x48) in error color at top of card.
- Concise title: "Configuration Required".
- Brief message explaining need to fix configuration in main window settings.
- Secondary text explaining automatic recovery when configuration becomes valid.

**Usage:**
- Added to window XAML with `Grid.RowSpan` spanning all grid rows to overlay entire window.
- Visibility bound to ViewModel's `ShowOverlay` property (`!IsConfigValid && !IsOperationInProgress`).
- Positioned with `Panel.ZIndex="1000"` to ensure it appears on top of all other content.
- No interaction required—purely informational and automatically hides when configuration becomes valid.

**XAML Integration:**
```xaml
<controls:ConfigInvalidOverlay Grid.RowSpan="N"
                              Visibility="{Binding ShowOverlay, Converter={StaticResource BooleanToVisibilityConverter}}"
                              Panel.ZIndex="1000" />
```

---

## Value Converters

### `LoadOrderKeeper.Converters.ReplacementCommandParameterConverter`

```csharp
public sealed class ReplacementCommandParameterConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.ActiveProfileVisibilityConverter`

```csharp
public sealed class ActiveProfileVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture);
    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.BooleanAndConverter`

```csharp
public sealed class BooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.CountToVisibilityConverter`

```csharp
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.InverseCountToVisibilityConverter`

```csharp
public sealed class InverseCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.InverseBooleanToVisibilityConverter`

```csharp
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

### `LoadOrderKeeper.Converters.ChangeSummaryConverter`

```csharp
public sealed class ChangeSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture);
}
```

---

[<< Back to Index](README.md)
