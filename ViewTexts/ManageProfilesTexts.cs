using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ManageProfilesWindow.
    /// </summary>
    public partial class ManageProfilesTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ManageProfilesTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("ManageProfiles", "WindowTitle");
        
        // Menu
        public string FileMenuText => _localization.GetString("ManageProfiles", "FileMenuText");
        public string AddProfileMenuText => _localization.GetString("ManageProfiles", "AddProfileMenuText");
        public string ExitMenuText => _localization.GetString("ManageProfiles", "ExitMenuText");
        public string EditProfileMenuText => _localization.GetString("ManageProfiles", "EditProfileMenuText");
        public string DeleteProfileMenuText => _localization.GetString("ManageProfiles", "DeleteProfileMenuText");
        public string CopyProfileMenuText => _localization.GetString("ManageProfiles", "CopyProfileMenuText");
        
        // Buttons
        public string AddProfileButtonText => _localization.GetString("ManageProfiles", "AddProfileButtonText");
        public string CloseButtonText => _localization.GetString("ManageProfiles", "CloseButtonText");
        
        // Column Headers
        public string LabelColumnHeader => _localization.GetString("ManageProfiles", "LabelColumnHeader");
        public string DescriptionColumnHeader => _localization.GetString("ManageProfiles", "DescriptionColumnHeader");
        
        // Default Profile
        public string DefaultProfileLabel => _localization.GetString("ManageProfiles", "DefaultProfileLabel");
        public string DefaultProfileDescription => _localization.GetString("ManageProfiles", "DefaultProfileDescription");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(FileMenuText));
            OnPropertyChanged(nameof(AddProfileMenuText));
            OnPropertyChanged(nameof(ExitMenuText));
            OnPropertyChanged(nameof(EditProfileMenuText));
            OnPropertyChanged(nameof(DeleteProfileMenuText));
            OnPropertyChanged(nameof(CopyProfileMenuText));
            OnPropertyChanged(nameof(AddProfileButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(LabelColumnHeader));
            OnPropertyChanged(nameof(DescriptionColumnHeader));
            OnPropertyChanged(nameof(DefaultProfileLabel));
            OnPropertyChanged(nameof(DefaultProfileDescription));
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
