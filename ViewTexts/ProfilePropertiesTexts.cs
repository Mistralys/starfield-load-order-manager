using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ProfilePropertiesWindow.
    /// </summary>
    public partial class ProfilePropertiesTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ProfilePropertiesTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitleCreate => _localization.GetString("ProfileProperties", "WindowTitleCreate");
        public string WindowTitleEdit => _localization.GetString("ProfileProperties", "WindowTitleEdit");
        
        // Labels
        public string LabelLabelText => _localization.GetString("ProfileProperties", "LabelLabelText");
        public string DescriptionLabelText => _localization.GetString("ProfileProperties", "DescriptionLabelText");
        
        // Buttons
        public string SaveButtonText => _localization.GetString("ProfileProperties", "SaveButtonText");
        public string CreateButtonText => _localization.GetString("ProfileProperties", "CreateButtonText");
        public string CancelButtonText => _localization.GetString("ProfileProperties", "CancelButtonText");
        
        // Validation Messages
        public string LabelRequiredError => _localization.GetString("ProfileProperties", "LabelRequiredError");
        public string LabelTooShortError => _localization.GetString("ProfileProperties", "LabelTooShortError");
        public string LabelTooLongError => _localization.GetString("ProfileProperties", "LabelTooLongError");
        public string LabelReservedError => _localization.GetString("ProfileProperties", "LabelReservedError");
        public string LabelDuplicateError => _localization.GetString("ProfileProperties", "LabelDuplicateError");
        public string DescriptionTooLongError => _localization.GetString("ProfileProperties", "DescriptionTooLongError");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitleCreate));
            OnPropertyChanged(nameof(WindowTitleEdit));
            OnPropertyChanged(nameof(LabelLabelText));
            OnPropertyChanged(nameof(DescriptionLabelText));
            OnPropertyChanged(nameof(SaveButtonText));
            OnPropertyChanged(nameof(CreateButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(LabelRequiredError));
            OnPropertyChanged(nameof(LabelTooShortError));
            OnPropertyChanged(nameof(LabelTooLongError));
            OnPropertyChanged(nameof(LabelReservedError));
            OnPropertyChanged(nameof(LabelDuplicateError));
            OnPropertyChanged(nameof(DescriptionTooLongError));
        }

        /// <summary>
        /// Refreshes all localized properties when culture changes (legacy compatibility).
        /// </summary>
        public void RefreshAll()
        {
            OnCultureChanged(this, EventArgs.Empty);
        }
    }
}
