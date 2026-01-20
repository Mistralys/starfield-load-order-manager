using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ManageProfilesWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ManageProfilesTexts : ObservableObject
    {
        // Window
        public string WindowTitle { get; } = "Manage Profiles";
        
        // Menu
        public string FileMenuText { get; } = "File";
        public string AddProfileMenuText { get; } = "Add Profile";
        public string EditProfileMenuText { get; } = "Edit";
        public string DeleteProfileMenuText { get; } = "Delete";
        public string CopyProfileMenuText { get; } = "Copy";
        
        // Buttons
        public string AddProfileButtonText { get; } = "Add Profile";
        public string CloseButtonText { get; } = "Close";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(FileMenuText));
            OnPropertyChanged(nameof(AddProfileMenuText));
            OnPropertyChanged(nameof(EditProfileMenuText));
            OnPropertyChanged(nameof(DeleteProfileMenuText));
            OnPropertyChanged(nameof(CopyProfileMenuText));
            OnPropertyChanged(nameof(AddProfileButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
        }
    }
}
