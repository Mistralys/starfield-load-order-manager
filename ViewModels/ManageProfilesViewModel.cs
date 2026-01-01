using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.ViewModels;

/// <summary>
/// ViewModel for managing profiles.
/// </summary>
public partial class ManageProfilesViewModel : ObservableObject
{
    private readonly AppConfigModel _config;

    [ObservableProperty]
    private ObservableCollection<ProfileModel> _profiles = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteProfileCommand))]
    [NotifyCanExecuteChangedFor(nameof(CopyProfileCommand))]
    private ProfileModel? _selectedProfile;

    [ObservableProperty]
    private bool _isLoading;

    public event EventHandler? CloseRequested;
    public event EventHandler<ProfileModel>? AddProfileRequested;
    public event EventHandler<ProfileModel>? EditProfileRequested;
    public event EventHandler<ProfileModel>? CopyProfileRequested;

    public string WindowTitle => "Manage Profiles";
    public string AddProfileButtonText => "Add Profile";
    public string FileMenuText => "File";
    public string AddProfileMenuText => "Add Profile";
    public string EditProfileMenuText => "Edit";
    public string DeleteProfileMenuText => "Delete";
    public string CopyProfileMenuText => "Copy";
    public string CloseButtonText => "Close";

    public ManageProfilesViewModel(AppConfigModel config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
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

    [RelayCommand]
    private void AddProfile()
    {
        AddProfileRequested?.Invoke(this, ProfileModel.CreateDefault());
    }

    [RelayCommand(CanExecute = nameof(CanEditProfile))]
    private void EditProfile(ProfileModel? profile = null)
    {
        System.Diagnostics.Debug.WriteLine($"EditProfile called with profile: {profile?.Label ?? "null"}");
        var targetProfile = profile ?? SelectedProfile;
        if (targetProfile != null && !targetProfile.IsDefault)
        {
            EditProfileRequested?.Invoke(this, targetProfile);
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteProfile))]
    private async Task DeleteProfileAsync(ProfileModel? profile = null)
    {
        System.Diagnostics.Debug.WriteLine($"DeleteProfile called with profile: {profile?.Label ?? "null"}");
        var targetProfile = profile ?? SelectedProfile;
        if (targetProfile == null || targetProfile.IsDefault)
        {
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete the profile '{targetProfile.Label}'?\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await ProfileService.DeleteProfileAsync(_config, targetProfile.Id);
            await LoadProfilesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Failed to delete profile: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopyProfile))]
    private void CopyProfile(ProfileModel? profile = null)
    {
        System.Diagnostics.Debug.WriteLine($"CopyProfile called with profile: {profile?.Label ?? "null"}");
        var targetProfile = profile ?? SelectedProfile;
        if (targetProfile != null)
        {
            CopyProfileRequested?.Invoke(this, targetProfile);
        }
    }

    [RelayCommand]
    private void Close()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private bool CanEditProfile(ProfileModel? profile = null)
    {
        var targetProfile = profile ?? SelectedProfile;
        var canEdit = targetProfile is { IsDefault: false };
        System.Diagnostics.Debug.WriteLine($"CanEditProfile: profile={profile?.Label ?? "null"}, selected={SelectedProfile?.Label ?? "null"}, canEdit={canEdit}");
        return canEdit;
    }
    
    private bool CanDeleteProfile(ProfileModel? profile = null)
    {
        var targetProfile = profile ?? SelectedProfile;
        var canDelete = targetProfile is { IsDefault: false };
        System.Diagnostics.Debug.WriteLine($"CanDeleteProfile: profile={profile?.Label ?? "null"}, selected={SelectedProfile?.Label ?? "null"}, canDelete={canDelete}");
        return canDelete;
    }
    
    private bool CanCopyProfile(ProfileModel? profile = null)
    {
        var targetProfile = profile ?? SelectedProfile;
        var canCopy = targetProfile != null;
        System.Diagnostics.Debug.WriteLine($"CanCopyProfile: profile={profile?.Label ?? "null"}, selected={SelectedProfile?.Label ?? "null"}, canCopy={canCopy}");
        return canCopy;
    }
}
