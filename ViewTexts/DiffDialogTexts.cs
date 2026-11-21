using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Text ViewModel for DiffDialog. Exposes all localized strings consumed by
    /// <c>DiffWindow.xaml</c> as <see cref="string"/> properties backed by
    /// <see cref="LocalizationService"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>CultureChanged subscription model</b><br/>
    /// The constructor subscribes <c>OnCultureChanged</c> to
    /// <c>LocalizationService.CultureChanged</c>. When the user switches language
    /// at runtime, <c>OnCultureChanged</c> raises <c>INotifyPropertyChanged</c>
    /// for every property so that all bound UI elements re-read their string values
    /// without requiring a window reload.</para>
    ///
    /// <para><b>RefreshAll()</b><br/>
    /// <c>RefreshAll()</c> is a public wrapper that calls <c>OnCultureChanged</c>
    /// with dummy arguments. It exists for legacy compatibility with call sites that
    /// trigger a bulk refresh outside of a real culture-change event (e.g. after a
    /// programmatic locale reset in tests). New code should rely on the
    /// <c>CultureChanged</c> event instead.</para>
    ///
    /// <para><b>Locale-key-to-property naming convention</b><br/>
    /// Each property maps 1-to-1 to a key in the <c>"DiffDialog"</c> section of
    /// the locale JSON files. The property name is identical to the locale key
    /// (e.g., property <c>GuidanceSidebarTitle</c> reads key
    /// <c>"GuidanceSidebarTitle"</c>). This 1:1 convention means that grepping for
    /// a property name in the source will always find the matching JSON key, and
    /// vice-versa, with no transformation layer. The only intentional exception is
    /// the small set of <c>ConfirmRiskyDetail_*</c> properties, which use an
    /// underscore separator to mirror their locale keys exactly and preserve this
    /// grep-ability.</para>
    /// </remarks>
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
        public string AcceptChangesButtonTooltip => _localization.GetString("DiffDialog", "AcceptChangesButtonTooltip");
        public string DiscardChangesButtonTooltip => _localization.GetString("DiffDialog", "DiscardChangesButtonTooltip");
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

        // Per-item tooltips (Added, Removed, Replaced, Moved — Inserted already handled by InsertedWarningTooltip)
        public string AddedTooltip => _localization.GetString("DiffDialog", "AddedTooltip");
        public string RemovedTooltip => _localization.GetString("DiffDialog", "RemovedTooltip");
        public string ReplacedTooltip => _localization.GetString("DiffDialog", "ReplacedTooltip");
        public string MovedTooltip => _localization.GetString("DiffDialog", "MovedTooltip");

        // Sidebar recommendation cards
        public string GuidanceSidebarTitle => _localization.GetString("DiffDialog", "GuidanceSidebarTitle");
        public string GuidanceRemovalTitle => _localization.GetString("DiffDialog", "GuidanceRemovalTitle");
        public string GuidanceRemovalBody => _localization.GetString("DiffDialog", "GuidanceRemovalBody");
        public string GuidanceRemovalCountFormat => _localization.GetString("DiffDialog", "GuidanceRemovalCountFormat");
        public string GuidanceRemovalCountSingular => _localization.GetString("DiffDialog", "GuidanceRemovalCountSingular");
        public string GuidanceInsertionTitle => _localization.GetString("DiffDialog", "GuidanceInsertionTitle");
        public string GuidanceInsertionBody => _localization.GetString("DiffDialog", "GuidanceInsertionBody");
        public string GuidanceInsertionCountFormat => _localization.GetString("DiffDialog", "GuidanceInsertionCountFormat");
        public string GuidanceInsertionCountSingular => _localization.GetString("DiffDialog", "GuidanceInsertionCountSingular");
        public string GuidanceReplacementTitle => _localization.GetString("DiffDialog", "GuidanceReplacementTitle");
        public string GuidanceReplacementBody => _localization.GetString("DiffDialog", "GuidanceReplacementBody");
        public string GuidanceReplacementCountFormat => _localization.GetString("DiffDialog", "GuidanceReplacementCountFormat");
        public string GuidanceReplacementCountSingular => _localization.GetString("DiffDialog", "GuidanceReplacementCountSingular");
        public string GuidanceAddedTitle => _localization.GetString("DiffDialog", "GuidanceAddedTitle");
        public string GuidanceAddedBody => _localization.GetString("DiffDialog", "GuidanceAddedBody");
        public string GuidanceAddedCountFormat => _localization.GetString("DiffDialog", "GuidanceAddedCountFormat");
        public string GuidanceAddedCountSingular => _localization.GetString("DiffDialog", "GuidanceAddedCountSingular");
        public string GuidanceMovedTitle => _localization.GetString("DiffDialog", "GuidanceMovedTitle");
        public string GuidanceMovedBody => _localization.GetString("DiffDialog", "GuidanceMovedBody");
        public string GuidanceMovedCountFormat => _localization.GetString("DiffDialog", "GuidanceMovedCountFormat");
        public string GuidanceMovedCountSingular => _localization.GetString("DiffDialog", "GuidanceMovedCountSingular");
        public string GuidanceNoChangesTitle => _localization.GetString("DiffDialog", "GuidanceNoChangesTitle");
        public string GuidanceNoChangesBody => _localization.GetString("DiffDialog", "GuidanceNoChangesBody");

        // Enhanced confirmation dialog strings (risky changes only)
        public string ConfirmUpdateMessage_RiskyChanges => _localization.GetString("DiffDialog", "ConfirmUpdateMessage_RiskyChanges");
        public string ConfirmRiskyDetail_Removals => _localization.GetString("DiffDialog", "ConfirmRiskyDetail_Removals");
        public string ConfirmRiskyDetail_Insertions => _localization.GetString("DiffDialog", "ConfirmRiskyDetail_Insertions");
        public string ConfirmRiskyDetail_RemovedSingular => _localization.GetString("DiffDialog", "ConfirmRiskyDetail_RemovedSingular");
        public string ConfirmRiskyDetail_InsertedSingular => _localization.GetString("DiffDialog", "ConfirmRiskyDetail_InsertedSingular");

        /// <summary>
        /// Handles culture changes by refreshing all properties.
        /// </summary>
        private void OnCultureChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(DescriptionText));
            OnPropertyChanged(nameof(AcceptChangesButtonText));
            OnPropertyChanged(nameof(DiscardChangesButtonText));
            OnPropertyChanged(nameof(AcceptChangesButtonTooltip));
            OnPropertyChanged(nameof(DiscardChangesButtonTooltip));
            OnPropertyChanged(nameof(CloseButtonText));
            OnPropertyChanged(nameof(ChangeColumnHeader));
            OnPropertyChanged(nameof(ModNameColumnHeader));
            OnPropertyChanged(nameof(PositionColumnHeader));
            OnPropertyChanged(nameof(AddedText));
            OnPropertyChanged(nameof(RemovedText));
            OnPropertyChanged(nameof(MovedText));
            OnPropertyChanged(nameof(ReplacedText));
            OnPropertyChanged(nameof(NoDifferencesMessage));
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
            OnPropertyChanged(nameof(AddedTooltip));
            OnPropertyChanged(nameof(RemovedTooltip));
            OnPropertyChanged(nameof(ReplacedTooltip));
            OnPropertyChanged(nameof(MovedTooltip));
            OnPropertyChanged(nameof(GuidanceSidebarTitle));
            OnPropertyChanged(nameof(GuidanceRemovalTitle));
            OnPropertyChanged(nameof(GuidanceRemovalBody));
            OnPropertyChanged(nameof(GuidanceRemovalCountFormat));
            OnPropertyChanged(nameof(GuidanceRemovalCountSingular));
            OnPropertyChanged(nameof(GuidanceInsertionTitle));
            OnPropertyChanged(nameof(GuidanceInsertionBody));
            OnPropertyChanged(nameof(GuidanceInsertionCountFormat));
            OnPropertyChanged(nameof(GuidanceInsertionCountSingular));
            OnPropertyChanged(nameof(GuidanceReplacementTitle));
            OnPropertyChanged(nameof(GuidanceReplacementBody));
            OnPropertyChanged(nameof(GuidanceReplacementCountFormat));
            OnPropertyChanged(nameof(GuidanceReplacementCountSingular));
            OnPropertyChanged(nameof(GuidanceAddedTitle));
            OnPropertyChanged(nameof(GuidanceAddedBody));
            OnPropertyChanged(nameof(GuidanceAddedCountFormat));
            OnPropertyChanged(nameof(GuidanceAddedCountSingular));
            OnPropertyChanged(nameof(GuidanceMovedTitle));
            OnPropertyChanged(nameof(GuidanceMovedBody));
            OnPropertyChanged(nameof(GuidanceMovedCountFormat));
            OnPropertyChanged(nameof(GuidanceMovedCountSingular));
            OnPropertyChanged(nameof(GuidanceNoChangesTitle));
            OnPropertyChanged(nameof(GuidanceNoChangesBody));
            OnPropertyChanged(nameof(ConfirmUpdateMessage_RiskyChanges));
            OnPropertyChanged(nameof(ConfirmRiskyDetail_Removals));
            OnPropertyChanged(nameof(ConfirmRiskyDetail_Insertions));
            OnPropertyChanged(nameof(ConfirmRiskyDetail_RemovedSingular));
            OnPropertyChanged(nameof(ConfirmRiskyDetail_InsertedSingular));
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
