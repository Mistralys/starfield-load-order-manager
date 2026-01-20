using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ProfilePropertiesWindow with hardcoded strings.
    /// Will be connected to resource files in Phase 2.
    /// </summary>
    public partial class ProfilePropertiesTexts : ObservableObject
    {
        // Window
        public string WindowTitleCreate { get; } = "Create Profile";
        public string WindowTitleEdit { get; } = "Edit Profile";
        
        // Labels
        public string LabelLabelText { get; } = "Label:";
        public string DescriptionLabelText { get; } = "Description:";
        
        // Buttons
        public string SaveButtonText { get; } = "Save";
        public string CreateButtonText { get; } = "Create";
        public string CancelButtonText { get; } = "Cancel";
        
        // Validation Messages
        public string LabelRequiredError { get; } = "Label is required.";
        public string LabelTooShortError { get; } = "Label must be at least 2 characters.";
        public string LabelTooLongError { get; } = "Label must not exceed 30 characters.";
        public string LabelReservedError { get; } = "The label 'Default' is reserved.";
        public string LabelDuplicateError { get; } = "A profile with this label already exists.";
        public string DescriptionTooLongError { get; } = "Description must not exceed 500 characters.";
        
        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        public void RefreshAll()
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
    }
}
