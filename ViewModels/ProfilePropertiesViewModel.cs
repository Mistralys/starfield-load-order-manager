using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.ViewModels;

/// <summary>
/// ViewModel for creating or editing a profile.
/// </summary>
public partial class ProfilePropertiesViewModel : ObservableObject
{
    private readonly ProfileModel? _editingProfile;
    private readonly IReadOnlyList<ProfileModel> _existingProfiles;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LabelError))]
    private string _label = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionError))]
    private string _description = string.Empty;

    [ObservableProperty]
    private string? _labelError;

    [ObservableProperty]
    private string? _descriptionError;

    [ObservableProperty]
    private bool _hasErrors;

    public event EventHandler? SaveRequested;
    public event EventHandler? CancelRequested;

    public bool IsEditMode => _editingProfile != null;

    public string WindowTitle => IsEditMode ? "Edit Profile" : "Create Profile";
    public string SaveButtonText => IsEditMode ? "Save" : "Create";
    public string CancelButtonText => "Cancel";
    public string LabelLabelText => "Label:";
    public string DescriptionLabelText => "Description:";

    /// <summary>
    /// Creates a ViewModel for creating a new profile.
    /// </summary>
    public ProfilePropertiesViewModel(IReadOnlyList<ProfileModel> existingProfiles)
    {
        _existingProfiles = existingProfiles ?? throw new ArgumentNullException(nameof(existingProfiles));
        _editingProfile = null;
    }

    /// <summary>
    /// Creates a ViewModel for editing an existing profile.
    /// </summary>
    public ProfilePropertiesViewModel(ProfileModel profileToEdit, IReadOnlyList<ProfileModel> existingProfiles)
    {
        _editingProfile = profileToEdit ?? throw new ArgumentNullException(nameof(profileToEdit));
        _existingProfiles = existingProfiles ?? throw new ArgumentNullException(nameof(existingProfiles));

        Label = profileToEdit.Label;
        Description = profileToEdit.Description;
    }

    partial void OnLabelChanged(string value)
    {
        ValidateLabel();
    }

    partial void OnDescriptionChanged(string value)
    {
        ValidateDescription();
    }

    [RelayCommand]
    private void Save()
    {
        ValidateAll();

        if (!HasErrors)
        {
            SaveRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    public (string Label, string Description) GetProfileData()
    {
        return (Label.Trim(), Description.Trim());
    }

    private void ValidateAll()
    {
        ValidateLabel();
        ValidateDescription();
        UpdateHasErrors();
    }

    private void ValidateLabel()
    {
        LabelError = null;

        var trimmedLabel = Label.Trim();

        if (string.IsNullOrWhiteSpace(trimmedLabel))
        {
            LabelError = "Label is required.";
        }
        else if (trimmedLabel.Length < 2)
        {
            LabelError = "Label must be at least 2 characters.";
        }
        else if (trimmedLabel.Length > 30)
        {
            LabelError = "Label must not exceed 30 characters.";
        }
        else if (string.Equals(trimmedLabel, "Default", StringComparison.OrdinalIgnoreCase))
        {
            LabelError = "The label 'Default' is reserved.";
        }
        else
        {
            // Check for uniqueness (excluding the profile being edited)
            var isDuplicate = _existingProfiles
                .Where(p => _editingProfile == null || p.Id != _editingProfile.Id)
                .Any(p => string.Equals(p.Label, trimmedLabel, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                LabelError = "A profile with this label already exists.";
            }
        }

        UpdateHasErrors();
    }

    private void ValidateDescription()
    {
        DescriptionError = null;

        var trimmedDescription = Description.Trim();

        if (trimmedDescription.Length > 500)
        {
            DescriptionError = "Description must not exceed 500 characters.";
        }

        UpdateHasErrors();
    }

    private void UpdateHasErrors()
    {
        HasErrors = !string.IsNullOrEmpty(LabelError) || !string.IsNullOrEmpty(DescriptionError);
    }
}
