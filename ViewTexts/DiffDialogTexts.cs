using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for DiffDialog.
    /// </summary>
    public partial class DiffDialogTexts : ObservableObject
    {
        private readonly LocalizationService _localization = LocalizationService.Instance;

        public DiffDialogTexts()
        {
            // Subscribe to culture changes
            _localization.CultureChanged += OnCultureChanged;
        }

        // Window
        public string WindowTitle => _localization.GetString("DiffDialog", "WindowTitle");
        
        // Description
        public string DescriptionText => _localization.GetString("DiffDialog", "DescriptionText");
        
        // Buttons
        public string AcceptChangesButtonText => _localization.GetString("DiffDialog", "AcceptChangesButtonText");
        public string DiscardChangesButtonText => _localization.GetString("DiffDialog", "DiscardChangesButtonText");
        public string CloseButtonText => _localization.GetString("DiffDialog", "CloseButtonText");
        
        // Column Headers
        public string ChangeColumnHeader => _localization.GetString("DiffDialog", "ChangeColumnHeader");
        public string ModNameColumnHeader => _localization.GetString("DiffDialog", "ModNameColumnHeader");
        public string PositionColumnHeader => _localization.GetString("DiffDialog", "PositionColumnHeader");
        
        // Change Types
        public string AddedText => _localization.GetString("DiffDialog", "AddedText");
        public string RemovedText => _localization.GetString("DiffDialog", "RemovedText");
        public string MovedText => _localization.GetString("DiffDialog", "MovedText");
        public string ReplacedText => _localization.GetString("DiffDialog", "ReplacedText");
        
        // Messages
        public string NoDifferencesMessage => _localization.GetString("DiffDialog", "NoDifferencesMessage");
        public string MultipleChangesHelp => _localization.GetString("DiffDialog", "MultipleChangesHelp");
        
        // Status Messages
        public string DifferencesLoadedStatus => _localization.GetString("DiffDialog", "DifferencesLoadedStatus");
        public string NoNewDifferencesStatus => _localization.GetString("DiffDialog", "NoNewDifferencesStatus");
        public string DetectedChangesStatus => _localization.GetString("DiffDialog", "DetectedChangesStatus");
        public string MatchesReferenceStatus => _localization.GetString("DiffDialog", "MatchesReferenceStatus");
        public string FailedToRefreshError => _localization.GetString("DiffDialog", "FailedToRefreshError");
        public string NoDifferencesToDiscardStatus => _localization.GetString("DiffDialog", "NoDifferencesToDiscardStatus");
        public string CannotDiscardNowStatus => _localization.GetString("DiffDialog", "CannotDiscardNowStatus");
        public string DiscardCancelledStatus => _localization.GetString("DiffDialog", "DiscardCancelledStatus");
        
        // Confirmations
        public string ConfirmDiscardTitle => _localization.GetString("DiffDialog", "ConfirmDiscardTitle");
        public string ConfirmDiscardMessage => _localization.GetString("DiffDialog", "ConfirmDiscardMessage");
        public string ConfirmUpdateTitle => _localization.GetString("DiffDialog", "ConfirmUpdateTitle");
        public string ConfirmUpdateMessage => _localization.GetString("DiffDialog", "ConfirmUpdateMessage");
        public string ReferenceUpdateCancelledStatus => _localization.GetString("DiffDialog", "ReferenceUpdateCancelledStatus");
        
        // Mod Actions
        public string ReEnabledModStatus => _localization.GetString("DiffDialog", "ReEnabledModStatus");
        public string ModAlreadyEnabledStatus => _localization.GetString("DiffDialog", "ModAlreadyEnabledStatus");
        public string FailedToReEnableError => _localization.GetString("DiffDialog", "FailedToReEnableError");
        public string RemovedModStatus => _localization.GetString("DiffDialog", "RemovedModStatus");
        public string ModAlreadyRemovedStatus => _localization.GetString("DiffDialog", "ModAlreadyRemovedStatus");
        public string FailedToRemoveError => _localization.GetString("DiffDialog", "FailedToRemoveError");
        public string ReplacedModStatus => _localization.GetString("DiffDialog", "ReplacedModStatus");
        public string ModNoLongerPendingStatus => _localization.GetString("DiffDialog", "ModNoLongerPendingStatus");
        public string FailedToReplaceError => _localization.GetString("DiffDialog", "FailedToReplaceError");
        
        // Debug Copy
        public string DebugStateCopiedTitle => _localization.GetString("DiffDialog", "DebugStateCopiedTitle");
        public string DebugStateCopiedMessage => _localization.GetString("DiffDialog", "DebugStateCopiedMessage");
        public string DebugStateCopiedStatus => _localization.GetString("DiffDialog", "DebugStateCopiedStatus");
        public string CopyFailedTitle => _localization.GetString("DiffDialog", "CopyFailedTitle");
        public string FailedToCopyError => _localization.GetString("DiffDialog", "FailedToCopyError");
        
        // Context Menu
        public string ReEnableModMenuText => _localization.GetString("DiffDialog", "ReEnableModMenuText");
        public string ReplaceWithMenuText => _localization.GetString("DiffDialog", "ReplaceWithMenuText");
        public string RemoveModMenuText => _localization.GetString("DiffDialog", "RemoveModMenuText");
        public string CopyDebugStateMenuText => _localization.GetString("DiffDialog", "CopyDebugStateMenuText");
        public string FixLoadOrderButtonText => _localization.GetString("DiffDialog", "FixLoadOrderButtonText");
        public string SortingInsertedWarning => _localization.GetString("DiffDialog", "SortingInsertedWarning");
        public string InsertedWarningTooltip => _localization.GetString("DiffDialog", "InsertedWarningTooltip");
        public string ShowAllModsToggleText => _localization.GetString("DiffDialog", "ShowAllModsToggleText");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(AcceptChangesButtonText));
            OnPropertyChanged(nameof(DiscardChangesButtonText));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(ChangeColumnHeader));
            OnPropertyChanged(nameof(ModNameColumnHeader));
            OnPropertyChanged(nameof(PositionColumnHeader));
            OnPropertyChanged(nameof(AddedText));
            OnPropertyChanged(nameof(RemovedText));
            OnPropertyChanged(nameof(MovedText));
            OnPropertyChanged(nameof(ReplacedText));
            OnPropertyChanged(nameof(NoDifferencesMessage));
            OnPropertyChanged(nameof(MultipleChangesHelp));
            OnPropertyChanged(nameof(DifferencesLoadedStatus));
            OnPropertyChanged(nameof(NoNewDifferencesStatus));
            OnPropertyChanged(nameof(DetectedChangesStatus));
            OnPropertyChanged(nameof(MatchesReferenceStatus));
            OnPropertyChanged(nameof(FailedToRefreshError));
            OnPropertyChanged(nameof(NoDifferencesToDiscardStatus));
            OnPropertyChanged(nameof(CannotDiscardNowStatus));
            OnPropertyChanged(nameof(DiscardCancelledStatus));
            OnPropertyChanged(nameof(ConfirmDiscardTitle));
            OnPropertyChanged(nameof(ConfirmDiscardMessage));
            OnPropertyChanged(nameof(ConfirmUpdateTitle));
            OnPropertyChanged(nameof(ConfirmUpdateMessage));
            OnPropertyChanged(nameof(ReferenceUpdateCancelledStatus));
            OnPropertyChanged(nameof(ReEnabledModStatus));
            OnPropertyChanged(nameof(ModAlreadyEnabledStatus));
            OnPropertyChanged(nameof(FailedToReEnableError));
            OnPropertyChanged(nameof(RemovedModStatus));
            OnPropertyChanged(nameof(ModAlreadyRemovedStatus));
            OnPropertyChanged(nameof(FailedToRemoveError));
            OnPropertyChanged(nameof(ReplacedModStatus));
            OnPropertyChanged(nameof(ModNoLongerPendingStatus));
            OnPropertyChanged(nameof(FailedToReplaceError));
            OnPropertyChanged(nameof(DebugStateCopiedTitle));
            OnPropertyChanged(nameof(DebugStateCopiedMessage));
            OnPropertyChanged(nameof(DebugStateCopiedStatus));
            OnPropertyChanged(nameof(CopyFailedTitle));
            OnPropertyChanged(nameof(FailedToCopyError));
            OnPropertyChanged(nameof(ReEnableModMenuText));
            OnPropertyChanged(nameof(ReplaceWithMenuText));
            OnPropertyChanged(nameof(RemoveModMenuText));
            OnPropertyChanged(nameof(CopyDebugStateMenuText));
            OnPropertyChanged(nameof(FixLoadOrderButtonText));
            OnPropertyChanged(nameof(SortingInsertedWarning));
            OnPropertyChanged(nameof(InsertedWarningTooltip));
            OnPropertyChanged(nameof(ShowAllModsToggleText));
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
