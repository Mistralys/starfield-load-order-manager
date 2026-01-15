# MVVM Structure

[? Back to Overview](../README.md)

---

## Overview

The application follows the **Model-View-ViewModel (MVVM)** pattern using CommunityToolkit.Mvvm with coordinator-based architecture for improved separation of concerns.

---

## MVVM Components

### Model
Domain models and data structures:
- `AppConfigModel` - Application configuration data
- `Profile` - Profile metadata
- `ReferenceVersion` - Version history metadata
- `LoadOrderEntry` - Individual mod entry
- `ChangeDetectionResult` - Diff analysis results

**Characteristics**:
- Plain C# classes (POCOs)
- No UI dependencies
- Serializable to JSON
- Immutable when possible

---

### View
XAML-based user interface:
- `MainWindow.xaml` - Main application window
- `DiffWindow.xaml` - Change diff window
- `SettingsWindow.xaml` - Configuration settings
- `ManageProfilesWindow.xaml` - Profile management
- `ReferenceHistoryWindow.xaml` - Version history
- And more...

**Characteristics**:
- No code-behind logic (except UI-specific initialization)
- Data binding to ViewModel properties
- Command binding for user actions
- Design-time data context for visual designer support

---

### ViewModel
Presentation logic and state management:
- `MainViewModel` - Main window logic
- `DiffDialogViewModel` - Diff window logic
- `SettingsViewModel` - Settings window logic
- `ManageProfilesViewModel` - Profile management logic
- And more...

**Characteristics**:
- Inherits from `ObservableObject` (CommunityToolkit.Mvvm)
- Properties with `INotifyPropertyChanged`
- Commands using `RelayCommand<T>` and `AsyncRelayCommand<T>`
- Coordinators injected via constructor
- No direct UI references

---

## Coordinator Integration

### Traditional MVVM Problem
ViewModels often become bloated with:
- Domain logic
- State management
- Service coordination
- Event handling

### Solution: Coordinator Pattern
Domain logic extracted into specialized coordinators:

```
MainViewModel
  ??? FileMonitoringCoordinator (change detection)
  ??? StatusCoordinator (status messages)
  ??? UpdateCheckCoordinator (version checking)
  ??? ProfileCoordinator (profile management)
  ??? ConfigurationCoordinator (validation)
  ??? GameLauncherCoordinator (game launching)
```

**Benefits**:
- ViewModels focus on presentation logic only
- Domain logic reusable across ViewModels
- Easier unit testing with mocked coordinators
- Clearer separation of concerns

---

## Data Binding Patterns

### Property Binding

**ViewModel**:
```csharp
private string _statusMessage;
public string StatusMessage
{
    get => _statusMessage;
    set => SetProperty(ref _statusMessage, value);
}
```

**XAML**:
```xml
<TextBlock Text="{Binding StatusMessage}" />
```

### Command Binding

**ViewModel**:
```csharp
public ICommand OpenSettingsCommand { get; }

public MainViewModel()
{
    OpenSettingsCommand = new RelayCommand(OpenSettings);
}

private void OpenSettings()
{
    // Command logic here
}
```

**XAML**:
```xml
<Button Content="Settings" Command="{Binding OpenSettingsCommand}" />
```

### Async Command Binding

**ViewModel**:
```csharp
public IAsyncRelayCommand CheckUpdatesCommand { get; }

public MainViewModel()
{
    CheckUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
}

private async Task CheckForUpdatesAsync()
{
    // Async command logic here
}
```

**XAML**:
```xml
<Button Content="Check for Updates" Command="{Binding CheckUpdatesCommand}" />
```

---

## Event-Driven Communication

### Coordinator to ViewModel

Coordinators fire events that ViewModels subscribe to:

**Coordinator**:
```csharp
public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

private void OnProfileChanged(string profileId)
{
    ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(profileId));
}
```

**ViewModel**:
```csharp
public MainViewModel(ProfileCoordinator profileCoordinator)
{
    _profileCoordinator = profileCoordinator;
    _profileCoordinator.ProfileChanged += OnProfileChanged;
}

private void OnProfileChanged(object? sender, ProfileChangedEventArgs e)
{
    // React to profile change
    ActiveProfileLabel = _profileCoordinator.ActiveProfileLabel;
}
```

### ViewModel to ViewModel

Use `WeakReferenceMessenger` from CommunityToolkit.Mvvm for cross-ViewModel communication:

**Sender**:
```csharp
WeakReferenceMessenger.Default.Send(new ReferenceUpdatedMessage());
```

**Receiver**:
```csharp
WeakReferenceMessenger.Default.Register<ReferenceUpdatedMessage>(this, OnReferenceUpdated);

private void OnReferenceUpdated(object recipient, ReferenceUpdatedMessage message)
{
    // React to reference update
}
```

---

## Dependency Injection

### Constructor Injection

Services and coordinators injected via constructor:

```csharp
public class MainViewModel : ObservableObject
{
    private readonly FileMonitoringCoordinator _fileMonitoring;
    private readonly StatusCoordinator _status;
    private readonly ConfigurationCoordinator _configuration;

    public MainViewModel(
        FileMonitoringCoordinator fileMonitoring,
        StatusCoordinator status,
        ConfigurationCoordinator configuration)
    {
        _fileMonitoring = fileMonitoring;
        _status = status;
        _configuration = configuration;
        
        InitializeSubscriptions();
    }
}
```

### Service Registration

In `App.xaml.cs`:

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Coordinators
    services.AddSingleton<FileMonitoringCoordinator>();
    services.AddSingleton<StatusCoordinator>();
    services.AddSingleton<ConfigurationCoordinator>();
    
    // ViewModels
    services.AddTransient<MainViewModel>();
    services.AddTransient<SettingsViewModel>();
    
    // Services
    services.AddSingleton<IAppConfigService, AppConfigService>();
}
```

---

## Design-Time Data

Support Visual Studio XAML designer with design-time data:

**ViewModel**:
```csharp
public MainViewModel()
{
    if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
    {
        // Design-time test data
        StatusMessage = "Design-time status message";
        ChangeCount = 5;
    }
}
```

**XAML**:
```xml
<Window 
    xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
    xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
    xmlns:vm="clr-namespace:LoadOrderKeeper.ViewModels"
    mc:Ignorable="d"
    d:DataContext="{d:DesignInstance Type=vm:MainViewModel, IsDesignTimeCreatable=True}">
```

---

## Validation Patterns

### Property Validation

Using `ValidationAttribute` from `System.ComponentModel.DataAnnotations`:

```csharp
private string _profileLabel;

[Required(ErrorMessage = "Label is required")]
[StringLength(30, MinimumLength = 2, ErrorMessage = "Label must be 2-30 characters")]
public string ProfileLabel
{
    get => _profileLabel;
    set
    {
        SetProperty(ref _profileLabel, value);
        ValidateProperty(value);
    }
}
```

### Form Validation

Check `HasErrors` property before submitting:

```csharp
private void SaveProfile()
{
    ValidateAllProperties();
    
    if (HasErrors)
    {
        // Show validation errors
        return;
    }
    
    // Proceed with save
}
```

---

## Related Documentation

- **[Coordinator Pattern](coordinator-pattern.md)** - Detailed coordinator architecture
- **[UI Guidelines](../ui-guidelines.md)** - UI binding patterns and conventions

---

## Best Practices

1. **ViewModels Never Reference Views**: Use data binding exclusively
2. **Use Coordinators for Domain Logic**: Keep ViewModels focused on presentation
3. **Event-Driven Updates**: Subscribe to coordinator events for reactive UI
4. **Async Commands**: Use `AsyncRelayCommand` for long-running operations
5. **Validate Early**: Validate properties on change, not just on submit
6. **Design-Time Data**: Support XAML designer with test data
7. **Dispose Properly**: Unsubscribe from events in ViewModel.Dispose()
