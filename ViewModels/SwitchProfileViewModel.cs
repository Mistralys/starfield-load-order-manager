using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.ViewModels;

/// <summary>
/// ViewModel for switching between profiles.
/// </summary>
public partial class SwitchProfileViewModel : ObservableObject
{
    private readonly SwitchProfileTexts _texts = new();
    private readonly AppConfigModel _config;
    private readonly ConfigurationCoordinator? _configCoordinator;
    private readonly string _activeProfileId;
    public string ActiveProfileId => _activeProfileId;

    [ObservableProperty]
    private ObservableCollection<ProfileModel> _profiles = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOverlay))]
    private bool _isConfigValid = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOverlay))]
    private bool _isOperationInProgress;

    public bool ShowOverlay => !IsConfigValid && !IsOperationInProgress;

    public event EventHandler<ProfileModel>? ProfileSelected;

    public string WindowTitle => _texts.WindowTitle;

    public SwitchProfileViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _configCoordinator = configCoordinator;
        _activeProfileId = config.ActiveProfileId ?? "default";

        if (_configCoordinator != null)
        {
            IsConfigValid = _configCoordinator.IsConfigValid;
            _configCoordinator.ValidationChanged += OnConfigValidationChanged;
        }
    }

    private void OnConfigValidationChanged(object? sender, Coordinators.Events.ConfigValidationChangedEventArgs e)
    {
        IsConfigValid = e.IsValid;
    }

    public async Task LoadProfilesAsync()
    {
        IsLoading = true;
        try
        {
            var profiles = await ProfileService.LoadProfilesAsync(_config);
            Profiles = new ObservableCollection<ProfileModel>(profiles);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void SelectProfile(ProfileModel profile)
    {
        if (profile != null)
        {
            ProfileSelected?.Invoke(this, profile);
        }
    }

    public bool IsActiveProfile(ProfileModel profile)
    {
        return profile?.Id == _activeProfileId;
    }
}
