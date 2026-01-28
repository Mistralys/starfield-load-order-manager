using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel wrapping ReferenceManagement status message localization strings.
    /// Provides INotifyPropertyChanged support for language switching.
    /// </summary>
    public partial class ReferenceManagementStatusTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ReferenceManagementStatusTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Status Messages
        public string UpdatingReference => _localization.GetString("ReferenceManagementStatus", "UpdatingReference");
        public string UpdateCancelled => _localization.GetString("ReferenceManagementStatus", "UpdateCancelled");
        public string ReferenceUpdatedSuccess => _localization.GetString("ReferenceManagementStatus", "ReferenceUpdatedSuccess");
        public string CreatingReference => _localization.GetString("ReferenceManagementStatus", "CreatingReference");
        public string ReferenceCreatedSuccess => _localization.GetString("ReferenceManagementStatus", "ReferenceCreatedSuccess");
        public string DiscardingChanges => _localization.GetString("ReferenceManagementStatus", "DiscardingChanges");
        public string ChangesDiscarded => _localization.GetString("ReferenceManagementStatus", "ChangesDiscarded");
        public string RolledBackFormat => _localization.GetString("ReferenceManagementStatus", "RolledBackFormat");

        /// <summary>
        /// Refreshes all localized properties when culture changes.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(UpdatingReference));
            OnPropertyChanged(nameof(UpdateCancelled));
            OnPropertyChanged(nameof(ReferenceUpdatedSuccess));
            OnPropertyChanged(nameof(CreatingReference));
            OnPropertyChanged(nameof(ReferenceCreatedSuccess));
            OnPropertyChanged(nameof(DiscardingChanges));
            OnPropertyChanged(nameof(ChangesDiscarded));
            OnPropertyChanged(nameof(RolledBackFormat));
        }
    }
}
