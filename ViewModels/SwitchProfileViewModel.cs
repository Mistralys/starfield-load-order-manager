using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels;

/// <summary>
/// ViewModel for switching between profiles.
/// </summary>
public partial class SwitchProfileViewModel : ObservableObject
{
    private readonly AppConfigModel _config;
    private readonly string _activeProfileId;
    public string ActiveProfileId => _activeProfileId;

    [ObservableProperty]
    private ObservableCollection<ProfileModel> _profiles = new();

    [ObservableProperty]
    private bool _isLoading;

    public event EventHandler<ProfileModel>? ProfileSelected;

    public string WindowTitle => "Switch Profile";

    public SwitchProfileViewModel(AppConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _activeProfileId = config.ActiveProfileId ?? "default";
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
