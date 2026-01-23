using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ConfirmationDialog.
    /// </summary>
    public partial class ConfirmationDialogTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ConfirmationDialogTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Buttons
        public string OkButtonText => _localization.GetString("ConfirmationDialog", "OkButtonText");
        public string CancelButtonText => _localization.GetString("ConfirmationDialog", "CancelButtonText");
        public string YesButtonText => _localization.GetString("ConfirmationDialog", "YesButtonText");
        public string NoButtonText => _localization.GetString("ConfirmationDialog", "NoButtonText");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(OkButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            OnPropertyChanged(nameof(YesButtonText));
            OnPropertyChanged(nameof(NoButtonText));
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
