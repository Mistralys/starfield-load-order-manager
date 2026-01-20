using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping CommonResources for common UI text across the application.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class CommonTexts : ObservableObject
    {
        // Common Buttons
        public string ButtonCancel => Resources.CommonResources.ButtonCancel;
        public string ButtonClose => Resources.CommonResources.ButtonClose;
        public string ButtonNo => Resources.CommonResources.ButtonNo;
        public string ButtonOk => Resources.CommonResources.ButtonOk;
        public string ButtonSave => Resources.CommonResources.ButtonSave;
        public string ButtonYes => Resources.CommonResources.ButtonYes;
        
        // Configuration Messages
        public string ConfigInvalidGuidance => Resources.CommonResources.ConfigInvalidGuidance;
        public string ErrorPrefix => Resources.CommonResources.ErrorPrefix;
        public string PluginsTxtRequired => Resources.CommonResources.PluginsTxtRequired;
        public string ProfilesFolderAccessDenied => Resources.CommonResources.ProfilesFolderAccessDenied;
        public string ProfilesFolderRequired => Resources.CommonResources.ProfilesFolderRequired;
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
        {
            OnPropertyChanged(nameof(ButtonCancel));
            OnPropertyChanged(nameof(ButtonClose));
            OnPropertyChanged(nameof(ButtonNo));
            OnPropertyChanged(nameof(ButtonOk));
            OnPropertyChanged(nameof(ButtonSave));
            OnPropertyChanged(nameof(ButtonYes));
            OnPropertyChanged(nameof(ConfigInvalidGuidance));
            OnPropertyChanged(nameof(ErrorPrefix));
            OnPropertyChanged(nameof(PluginsTxtRequired));
            OnPropertyChanged(nameof(ProfilesFolderAccessDenied));
            OnPropertyChanged(nameof(ProfilesFolderRequired));
        }
    }
}
