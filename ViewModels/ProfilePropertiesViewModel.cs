using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.ViewModels;

/// <summary>
/// ViewModel for creating or editing a profile.
/// </summary>
public partial class ProfilePropertiesViewModel : ObservableObject
{
    private readonly ProfilePropertiesTexts _texts = new();
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

    public string WindowTitle => IsEditMode ? _texts.WindowTitleEdit : _texts.WindowTitleCreate;
    public string SaveButtonText => IsEditMode ? _texts.SaveButtonText : _texts.CreateButtonText;
    public string CancelButtonText => _texts.CancelButtonText;
    public string LabelLabelText => _texts.LabelLabelText;
    public string DescriptionLabelText => _texts.DescriptionLabelText;

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
            LabelError = _texts.LabelRequiredError;
        }
        else if (trimmedLabel.Length < 2)
        {
            LabelError = _texts.LabelTooShortError;
        }
        else if (trimmedLabel.Length > 30)
        {
            LabelError = _texts.LabelTooLongError;
        }
        else if (string.Equals(trimmedLabel, "Default", StringComparison.OrdinalIgnoreCase))
        {
            LabelError = _texts.LabelReservedError;
        }
        else
        {
            // Check for uniqueness (excluding the profile being edited)
            var isDuplicate = _existingProfiles
                .Where(p => _editingProfile == null || p.Id != _editingProfile.Id)
                .Any(p => string.Equals(p.Label, trimmedLabel, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                LabelError = _texts.LabelDuplicateError;
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
            DescriptionError = _texts.DescriptionTooLongError;
        }

        UpdateHasErrors();
    }

    private void UpdateHasErrors()
    {
        HasErrors = !string.IsNullOrEmpty(LabelError) || !string.IsNullOrEmpty(DescriptionError);
    }
}
