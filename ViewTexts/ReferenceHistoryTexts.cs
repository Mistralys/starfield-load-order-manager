using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for ReferenceHistoryWindow.
    /// </summary>
    public partial class ReferenceHistoryTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public ReferenceHistoryTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("ReferenceHistory", "WindowTitle");
        
        // Menu
        public string FileMenuText => _localization.GetString("ReferenceHistory", "FileMenuText");
        public string ExitMenuText => _localization.GetString("ReferenceHistory", "ExitMenuText");
        public string EditMenuText => _localization.GetString("ReferenceHistory", "EditMenuText");
        public string ClearHistoryMenuText => _localization.GetString("ReferenceHistory", "ClearHistoryMenuText");
        
        // Buttons
        public string RollbackButtonText => _localization.GetString("ReferenceHistory", "RollbackButtonText");
        public string DeleteVersionButtonText => _localization.GetString("ReferenceHistory", "DeleteVersionButtonText");
        public string ClearHistoryButtonText => _localization.GetString("ReferenceHistory", "ClearHistoryButtonText");
        public string CloseButtonText => _localization.GetString("ReferenceHistory", "CloseButtonText");
        
        // Column Headers
        public string VersionColumnHeader => _localization.GetString("ReferenceHistory", "VersionColumnHeader");
        public string DateColumnHeader => _localization.GetString("ReferenceHistory", "DateColumnHeader");
        public string ChangesColumnHeader => _localization.GetString("ReferenceHistory", "ChangesColumnHeader");
        public string SummaryColumnHeader => _localization.GetString("ReferenceHistory", "SummaryColumnHeader");
        
        // Messages
        public string NoVersionsMessage => _localization.GetString("ReferenceHistory", "NoVersionsMessage");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(FileMenuText));
            OnPropertyChanged(nameof(ExitMenuText));
            OnPropertyChanged(nameof(EditMenuText));
            OnPropertyChanged(nameof(ClearHistoryMenuText));
            OnPropertyChanged(nameof(RollbackButtonText));
            OnPropertyChanged(nameof(DeleteVersionButtonText));
            OnPropertyChanged(nameof(ClearHistoryButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(VersionColumnHeader));
            OnPropertyChanged(nameof(DateColumnHeader));
            OnPropertyChanged(nameof(ChangesColumnHeader));
            OnPropertyChanged(nameof(SummaryColumnHeader));
            OnPropertyChanged(nameof(NoVersionsMessage));
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
