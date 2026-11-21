using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.ViewModels
{
    public class ConfirmationRequestedEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public ConfirmationIcon Icon { get; }
        public ConfirmationButton Buttons { get; }
        public ConfirmationResult Result { get; set; }

        public ConfirmationRequestedEventArgs(string title, string message, ConfirmationIcon icon = ConfirmationIcon.Warning, ConfirmationButton buttons = ConfirmationButton.YesNo)
        {
            Title = title;
            Message = message;
            Icon = icon;
            Buttons = buttons;
            Result = ConfirmationResult.None;
        }
    }

    public partial class DiffDialogViewModel : ObservableObject, IDisposable
    {
        private readonly MainViewModel _mainViewModel;
        private readonly ListCollectionView _filteredDiffLines;
        private bool _suppressCollectionNotification;
        private string _lastDiffSignature = string.Empty;
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _isConfigValid = true;

        [ObservableProperty]
        private bool _isOperationInProgress;

        [ObservableProperty]
        private bool _showAllMods = false;

        // ── Sidebar card expansion state ────────────────────────────────────────
        // _isRemovalCardExpanded uses a field initializer so the test-only constructor
        // (which never runs production-constructor code) inherits the correct true default.
        [ObservableProperty]
        private bool _isRemovalCardExpanded = true;

        [ObservableProperty]
        private bool _isInsertionCardExpanded;

        [ObservableProperty]
        private bool _isReplacementCardExpanded;

        [ObservableProperty]
        private bool _isAddedCardExpanded;

        [ObservableProperty]
        private bool _isMovedCardExpanded;

        [ObservableProperty]
        private bool _isNoChangesCardExpanded;

        public bool ShowOverlay => !IsConfigValid && !IsOperationInProgress;

        // Add localization texts
        public DiffDialogTexts Texts { get; } = new();
        public MenuViewModel Menu => _mainViewModel.Menu;

        public DiffDialogViewModel(IEnumerable<DiffLineModel> trimmedLines, IEnumerable<DiffLineModel> fullLines, MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            var trimmedList = trimmedLines.ToList();
            DiffLines = new ObservableCollection<DiffLineModel>(fullLines);
            _filteredDiffLines = new ListCollectionView(DiffLines);
            _filteredDiffLines.Filter = FilterDiffLines;
            DiffLines.CollectionChanged += OnDiffCollectionChanged;
            UpdateReferenceCommand = new AsyncRelayCommand(UpdateReferenceWithConfirmationAsync, CanUpdateReference);
            FixLoadOrderCommand = _mainViewModel.FixLoadOrderCommand;
            ReEnableModCommand = new AsyncRelayCommand<DiffLineModel>(ReEnableModAsync);
            RemoveNewModCommand = new AsyncRelayCommand<DiffLineModel>(RemoveNewModAsync);
            ReplaceRemovedModCommand = new AsyncRelayCommand<(DiffLineModel Removed, DiffLineModel Replacement)>(ReplaceRemovedModAsync);
            ToggleDependentChangesCommand = new RelayCommand<DiffLineModel>(ToggleDependentChanges);
            _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
            
            // Subscribe to configuration validation changes
            var configCoordinator = _mainViewModel.GetConfigurationCoordinator();
            IsConfigValid = configCoordinator.IsConfigValid;
            configCoordinator.ValidationChanged += OnConfigValidationChanged;
            
            // Subscribe to file monitoring coordinator's change detected event
            var coordinator = _mainViewModel.GetFileMonitoringCoordinator();
            coordinator.ChangeDetected += OnFileChangeDetected;
            
            ApplyLineNumbers(DiffLines);
            UpdateDiffState();
            _lastDiffSignature = BuildSignature(trimmedList);
            DiffStatusMessage = Texts.DifferencesLoadedStatus;

            // Restore card expansion states from config
            _isRemovalCardExpanded = _mainViewModel.Config.DiffRemovalCardExpanded;
            _isInsertionCardExpanded = _mainViewModel.Config.DiffInsertionCardExpanded;
            _isReplacementCardExpanded = _mainViewModel.Config.DiffReplacementCardExpanded;
            _isAddedCardExpanded = _mainViewModel.Config.DiffAddedCardExpanded;
            _isMovedCardExpanded = _mainViewModel.Config.DiffMovedCardExpanded;
            _isNoChangesCardExpanded = _mainViewModel.Config.DiffNoChangesCardExpanded;
        }

        /// <summary>
        /// Test-only constructor. Accepts a pre-populated diff collection and skips all
        /// coordinator subscriptions that require a full <see cref="MainViewModel"/> dependency
        /// graph. Not for production use.
        /// </summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal DiffDialogViewModel(ObservableCollection<DiffLineModel> diffLines)
        {
            _mainViewModel = null!;
            DiffLines = diffLines;
            _filteredDiffLines = new ListCollectionView(DiffLines);
            _filteredDiffLines.Filter = FilterDiffLines;
            DiffLines.CollectionChanged += OnDiffCollectionChanged;
            UpdateReferenceCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            FixLoadOrderCommand = new AsyncRelayCommand(() => Task.CompletedTask);
            ReEnableModCommand = new AsyncRelayCommand<DiffLineModel>(_ => Task.CompletedTask);
            RemoveNewModCommand = new AsyncRelayCommand<DiffLineModel>(_ => Task.CompletedTask);
            ReplaceRemovedModCommand = new AsyncRelayCommand<(DiffLineModel Removed, DiffLineModel Replacement)>(_ => Task.CompletedTask);
            ToggleDependentChangesCommand = new RelayCommand<DiffLineModel>(ToggleDependentChanges);
            ApplyLineNumbers(DiffLines);
            UpdateDiffState();
        }

        public ObservableCollection<DiffLineModel> DiffLines { get; }

        public ICollectionView FilteredDiffLines => _filteredDiffLines;

        public string Title => Texts.WindowTitle;

        public string Description => Texts.DescriptionText;

        public string ShowAllModsToggleText => Texts.ShowAllModsToggleText;

        public string UpdateReferenceButtonText
        {
            get
            {
                // Add ellipsis if confirmation dialog will be shown
                bool needsConfirmation = DiffLines.Any(line => line.ChangeType == DiffChangeType.Removed || line.ChangeType == DiffChangeType.Inserted);
                string baseText = Texts.AcceptChangesButtonText;
                return needsConfirmation ? $"{baseText}..." : baseText;
            }
        }

        public string FixLoadOrderButtonText => Texts.FixLoadOrderButtonText;

        public string DiscardChangesButtonText => Texts.DiscardChangesButtonText;

        public string UpdateReferenceButtonTooltip => Texts.AcceptChangesButtonTooltip;

        public string DiscardChangesButtonTooltip => Texts.DiscardChangesButtonTooltip;

        public string CloseButtonText => Texts.CloseButtonText;

        public string NoDifferencesMessage => Texts.NoDifferencesMessage;

        public string ReEnableModMenuText => Texts.ReEnableModMenuText;

        public string ReplaceWithMenuText => Texts.ReplaceWithMenuText;

        public string RemoveModMenuText => Texts.RemoveModMenuText;

        public string FileMenuHeader => Menu.FileMenuHeader;
        
        public string OpenPluginsMenuText => Menu.OpenPluginsMenuText;
        
        public string OpenReferenceMenuText => Menu.OpenReferenceMenuText;
        
        public string OpenAppDataFolderMenuText => Menu.OpenAppDataFolderMenuText;
        
        public string OpenGameFolderMenuText => Menu.OpenGameFolderMenuText;

        public string ExitMenuText => Menu.ExitMenuText;

        public string EditMenuHeader => Menu.EditMenuHeader;

        public IRelayCommand OpenPluginsFileCommand => _mainViewModel.OpenPluginsFileCommand;

        public IRelayCommand OpenReferenceFileCommand => _mainViewModel.OpenReferenceFileCommand;

        public IRelayCommand OpenAppDataFolderCommand => _mainViewModel.OpenAppDataFolderCommand;

        public IRelayCommand OpenGameFolderCommand => _mainViewModel.OpenGameFolderCommand;

        public bool ShowSortingRecommendation => HasDifferences && _mainViewModel.SortingRecommendationActive;

        public string SortingRecommendationMessage => _mainViewModel.SortingRecommendationMessage;

        public IReadOnlyList<DiffLineModel> AddedMods => DiffLines.Where(line => line.ChangeType == DiffChangeType.Added).ToList();

        public bool HasAddedMods => DiffLines.Any(line => line.ChangeType == DiffChangeType.Added);

        public bool HasInsertedMods => DiffLines.Any(line => line.ChangeType == DiffChangeType.Inserted);

        public string InsertedWarningTooltip => Texts.InsertedWarningTooltip;

        // ── Change type counts ──────────────────────────────────────────────────
        public int RemovedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Removed);
        public int InsertedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Inserted);
        public int ReplacedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Replaced);
        public int AddedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Added);
        public int MovedModCount => DiffLines.Count(l => l.ChangeType == DiffChangeType.Moved);

        // ── Sidebar card visibility ─────────────────────────────────────────────
        // The sidebar container itself is always rendered; card-level visibility
        // properties control which cards appear and disappear.
        public bool HasRemovedMods => RemovedModCount > 0;
        public bool HasReplacedMods => ReplacedModCount > 0;
        public bool ShowRemovalGuidance => HasDifferences && HasRemovedMods;
        public bool ShowInsertionGuidance => HasDifferences && HasInsertedMods;
        public bool ShowReplacementGuidance => HasDifferences && HasReplacedMods;
        public bool ShowAddedGuidance => HasDifferences && HasAddedMods;
        public bool ShowMovedGuidance => HasDifferences && MovedModCount > 0;
        public bool ShowNoChangesGuidance => !HasDifferences;

        // ── Count display strings (singular / plural) ───────────────────────────
        public string RemovedModCountText => RemovedModCount == 1
            ? Texts.GuidanceRemovalCountSingular
            : string.Format(Texts.GuidanceRemovalCountFormat, RemovedModCount);

        public string InsertedModCountText => InsertedModCount == 1
            ? Texts.GuidanceInsertionCountSingular
            : string.Format(Texts.GuidanceInsertionCountFormat, InsertedModCount);

        public string ReplacedModCountText => ReplacedModCount == 1
            ? Texts.GuidanceReplacementCountSingular
            : string.Format(Texts.GuidanceReplacementCountFormat, ReplacedModCount);

        public string AddedModCountText => AddedModCount == 1
            ? Texts.GuidanceAddedCountSingular
            : string.Format(Texts.GuidanceAddedCountFormat, AddedModCount);

        public string MovedModCountText => MovedModCount == 1
            ? Texts.GuidanceMovedCountSingular
            : string.Format(Texts.GuidanceMovedCountFormat, MovedModCount);

        // ── Pass-through text properties for sidebar card headings and body text ─
        // DiffWindow.xaml binds to VM properties rather than Texts.* sub-paths,
        // consistent with all other text bindings in the file.
        public string GuidanceSidebarTitle => Texts.GuidanceSidebarTitle;
        public string GuidanceRemovalTitle => Texts.GuidanceRemovalTitle;
        public string GuidanceRemovalBody => Texts.GuidanceRemovalBody;
        public string GuidanceInsertionTitle => Texts.GuidanceInsertionTitle;
        public string GuidanceInsertionBody => Texts.GuidanceInsertionBody;
        public string GuidanceReplacementTitle => Texts.GuidanceReplacementTitle;
        public string GuidanceReplacementBody => Texts.GuidanceReplacementBody;
        public string GuidanceAddedTitle => Texts.GuidanceAddedTitle;
        public string GuidanceAddedBody => Texts.GuidanceAddedBody;
        public string GuidanceMovedTitle => Texts.GuidanceMovedTitle;
        public string GuidanceMovedBody => Texts.GuidanceMovedBody;
        public string GuidanceNoChangesTitle => Texts.GuidanceNoChangesTitle;
        public string GuidanceNoChangesBody => Texts.GuidanceNoChangesBody;

        // ── Tooltip pass-through properties for PackIcon bindings ───────────────
        // Added, Removed, Replaced, Moved only — Inserted already has InsertedWarningTooltip.
        public string AddedTooltip => Texts.AddedTooltip;
        public string RemovedTooltip => Texts.RemovedTooltip;
        public string ReplacedTooltip => Texts.ReplacedTooltip;
        public string MovedTooltip => Texts.MovedTooltip;

        public IAsyncRelayCommand UpdateReferenceCommand { get; }

        public IAsyncRelayCommand FixLoadOrderCommand { get; }

        public IAsyncRelayCommand<DiffLineModel> ReEnableModCommand { get; }

        public IAsyncRelayCommand<DiffLineModel> RemoveNewModCommand { get; }

        public IAsyncRelayCommand<(DiffLineModel Removed, DiffLineModel Replacement)> ReplaceRemovedModCommand { get; }

        public IRelayCommand<DiffLineModel> ToggleDependentChangesCommand { get; }

        public event EventHandler? CloseRequested;
        public event EventHandler? ScrollRequested;
        public event EventHandler<ConfirmationRequestedEventArgs>? ConfirmationRequested;

        private bool _hasDifferences;
        public bool HasDifferences
        {
            get => _hasDifferences;
            private set => SetProperty(ref _hasDifferences, value);
        }

        private int _scrollTargetIndex = -1;
        public int ScrollTargetIndex
        {
            get => _scrollTargetIndex;
            private set => SetProperty(ref _scrollTargetIndex, value);
        }

        private string _diffStatusMessage = string.Empty;
        public string DiffStatusMessage
        {
            get => _diffStatusMessage;
            private set
            {
                if (SetProperty(ref _diffStatusMessage, value))
                {
                    HasStatusMessage = !string.IsNullOrWhiteSpace(value);
                }
            }
        }

        private bool _hasStatusMessage;
        public bool HasStatusMessage
        {
            get => _hasStatusMessage;
            private set => SetProperty(ref _hasStatusMessage, value);
        }

        private void OnDiffCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_suppressCollectionNotification)
            {
                return;
            }

            UpdateDiffState();
        }

        private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ReferenceButtonText))
            {
                OnPropertyChanged(nameof(UpdateReferenceButtonText));
            }
            else if (e.PropertyName == nameof(MainViewModel.SortingRecommendationMessage) ||
                     e.PropertyName == nameof(MainViewModel.SortingRecommendationActive))
            {
                OnPropertyChanged(nameof(SortingRecommendationMessage));
                OnPropertyChanged(nameof(ShowSortingRecommendation));
            }
        }

        /// <summary>
        /// Recomputes <see cref="HasDifferences"/> and <see cref="ScrollTargetIndex"/>, then
        /// broadcasts a full property-change notification via
        /// <c>OnPropertyChanged(string.Empty)</c>.
        /// <para>
        /// Using <c>string.Empty</c> (rather than individual <c>nameof(...)</c> calls) is the
        /// documented CommunityToolkit.Mvvm convention for signalling that all properties on the
        /// ViewModel may have changed. This covers every computed property —
        /// <c>ShowRemovalGuidance</c>, <c>ShowAddedGuidance</c>, <c>ShowMovedGuidance</c>,
        /// count text strings, etc. — without requiring a manual update each time a new
        /// computed property is added.
        /// </para>
        /// <para>
        /// Note: <see cref="HasDifferences"/> receives two <c>PropertyChanged</c> notifications
        /// per call — one from <c>SetProperty</c> and one from the bulk broadcast. WPF bindings
        /// are idempotent, so this is harmless and is an accepted trade-off of the pattern.
        /// </para>
        /// </summary>
        private void UpdateDiffState()
        {
            HasDifferences = DiffLines.Any(line => line.ChangeType != DiffChangeType.Unchanged && line.ChangeType != DiffChangeType.Separator);
            ScrollTargetIndex = ComputeScrollTargetIndex();
            OnPropertyChanged(string.Empty);
        }

        private int ComputeScrollTargetIndex()
        {
            if (DiffLines.Count == 0)
            {
                return -1;
            }

            if (!ShowAllMods)
            {
                return HasDifferences ? 0 : -1;
            }

            if (HasDifferences)
            {
                for (int index = 0; index < DiffLines.Count; index++)
                {
                    if (DiffLines[index].ChangeType != DiffChangeType.Unchanged && DiffLines[index].ChangeType != DiffChangeType.Separator)
                    {
                        return index;
                    }
                }
            }

            return DiffLines.Count - 1;
        }

        private void ReplaceDiffLines(IEnumerable<DiffLineModel> fullLines)
        {
            _suppressCollectionNotification = true;
            try
            {
                DiffLines.Clear();
                foreach (var line in fullLines)
                {
                    DiffLines.Add(line);
                }
            }
            finally
            {
                _suppressCollectionNotification = false;
            }

            ApplyLineNumbers(DiffLines);

            // Force property change notifications for all computed properties
            UpdateDiffState();
        }

        private bool FilterDiffLines(object item)
        {
            if (item is not DiffLineModel line)
            {
                return false;
            }

            if (line.ChangeType == DiffChangeType.Separator)
            {
                return false;
            }

            if (ShowAllMods)
            {
                return true;
            }

            return line.ChangeType != DiffChangeType.Unchanged;
        }

        partial void OnShowAllModsChanged(bool value)
        {
            _filteredDiffLines.Refresh();
            UpdateDiffState();
            RequestScroll();
        }

        private static void ApplyLineNumbers(IEnumerable<DiffLineModel> lines)
        {
            var list = lines as IList<DiffLineModel> ?? lines.ToList();
            int maxNumber = list.Count > 0
                ? list.Max(l => l.ReferenceNumber ?? l.CurrentNumber ?? 0)
                : 0;
            int width = Math.Max(3, maxNumber.ToString().Length);
            string format = new string('0', width);

            foreach (var line in list)
            {
                int? displayNumber = line.ReferenceNumber ?? line.CurrentNumber;
                line.FormattedLineNumber = displayNumber.HasValue
                    ? displayNumber.Value.ToString(format)
                    : string.Empty;
            }
        }

        private static string BuildSignature(IEnumerable<DiffLineModel> lines)
        {
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                builder.Append((int)line.ChangeType)
                       .Append(':')
                       .AppendLine(line.Text);
            }

            return builder.ToString();
        }

        private void RequestScroll()
        {
            ScrollRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleDependentChanges(DiffLineModel? line)
        {
            if (line is null || !line.HasDependentChanges)
            {
                return;
            }

            line.IsDependentChangesExpanded = !line.IsDependentChangesExpanded;
        }

        public async Task<bool> RefreshDiffAsync(string? reason = null)
        {
            if (_isRefreshing)
            {
                return false;
            }

            _isRefreshing = true;
            try
            {
                var trimmedLines = await DiffService.GetPluginsDiffAsync(_mainViewModel.Config);
                string newSignature = BuildSignature(trimmedLines);

                bool signatureChanged = !string.Equals(newSignature, _lastDiffSignature, StringComparison.Ordinal);
                
                if (signatureChanged)
                {
                    var fullLines = await DiffService.GetFullPluginsDiffAsync(_mainViewModel.Config);
                    ReplaceDiffLines(fullLines);
                    _lastDiffSignature = newSignature;
                    DiffStatusMessage = reason ?? Texts.DifferencesLoadedStatus;
                    RequestScroll();
                }
                else
                {
                    DiffStatusMessage = Texts.NoNewDifferencesStatus;
                }

                return signatureChanged;
            }
            catch (Exception ex)
            {
                DiffStatusMessage = Texts.FailedToRefreshError + " " + ex.Message;
                return false;
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task DiscardChangesAsync()
        {
            if (!HasDifferences)
            {
                DiffStatusMessage = Texts.NoDifferencesToDiscardStatus;
                return;
            }

            var discardCommand = _mainViewModel.DiscardChangesCommand;
            if (discardCommand is null || !discardCommand.CanExecute(null))
            {
                DiffStatusMessage = Texts.CannotDiscardNowStatus;
                return;
            }

            // Request confirmation from the view
            var eventArgs = new ConfirmationRequestedEventArgs(
                Texts.ConfirmDiscardTitle, 
                Texts.ConfirmDiscardMessage,
                ConfirmationIcon.Warning,
                ConfirmationButton.YesNo);
            ConfirmationRequested?.Invoke(this, eventArgs);

            if (eventArgs.Result != ConfirmationResult.Yes)
            {
                DiffStatusMessage = Texts.DiscardCancelledStatus;
                return;
            }

            await discardCommand.ExecuteAsync(null);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private async Task ReEnableModAsync(DiffLineModel? line)
        {
            if (line is null || line.ChangeType != DiffChangeType.Removed)
            {
                return;
            }

            try
            {
                bool changed = await FileService.ReEnableModAsync(_mainViewModel.Config, line.FileName);
                if (changed)
                {
                    DiffStatusMessage = string.Format(Texts.ReEnabledModStatus, line.FileName);
                    await RefreshDiffAsync(DiffStatusMessage);
                }
                else
                {
                    DiffStatusMessage = string.Format(Texts.ModAlreadyEnabledStatus, line.FileName);
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = string.Format(Texts.FailedToReEnableError, line.FileName) + " " + ex.Message;
            }
        }

        private async Task RemoveNewModAsync(DiffLineModel? line)
        {
            if (line is null || line.ChangeType != DiffChangeType.Added)
            {
                return;
            }

            try
            {
                bool changed = await FileService.RemoveNewModAsync(_mainViewModel.Config, line.FileName);
                if (changed)
                {
                    DiffStatusMessage = string.Format(Texts.RemovedModStatus, line.FileName);
                    await RefreshDiffAsync(DiffStatusMessage);
                }
                else
                {
                    DiffStatusMessage = string.Format(Texts.ModAlreadyRemovedStatus, line.FileName);
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = string.Format(Texts.FailedToRemoveError, line.FileName) + " " + ex.Message;
            }
        }

        private async Task ReplaceRemovedModAsync((DiffLineModel Removed, DiffLineModel Replacement) request)
        {
            var (removed, replacement) = request;
            if (removed is null || replacement is null)
            {
                return;
            }

            if (removed.ChangeType != DiffChangeType.Removed || replacement.ChangeType != DiffChangeType.Added)
            {
                return;
            }

            try
            {
                bool changed = await FileService.ReplaceModWithNewAsync(_mainViewModel.Config, removed.FileName, replacement.FileName);
                if (changed)
                {
                    DiffStatusMessage = string.Format(Texts.ReplacedModStatus, removed.FileName, replacement.FileName);
                    await RefreshDiffAsync(DiffStatusMessage);
                }
                else
                {
                    DiffStatusMessage = string.Format(Texts.ModNoLongerPendingStatus, replacement.FileName);
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = string.Format(Texts.FailedToReplaceError, removed.FileName) + " " + ex.Message;
            }
        }

        private bool CanUpdateReference()
        {
            return _mainViewModel?.CreateReferenceCommand?.CanExecute(null) ?? false;
        }

        /// <summary>
        /// Builds a risk-specific confirmation message based on the current diff state.
        /// </summary>
        /// <returns>
        /// <c>null</c> when only safe change types are present (Added, Replaced, Moved, or
        /// Unchanged) — no confirmation dialog is required. A non-<c>null</c> formatted string
        /// when at least one <see cref="DiffChangeType.Removed"/> or
        /// <see cref="DiffChangeType.Inserted"/> line is present; the string is passed directly
        /// to the <see cref="ConfirmationRequested"/> event so the view can display it in a
        /// warning dialog before the update proceeds.
        /// </returns>
        internal string? BuildConfirmationMessage()
        {
            var removedCount = DiffLines.Count(line => line.ChangeType == DiffChangeType.Removed);
            var insertedCount = DiffLines.Count(line => line.ChangeType == DiffChangeType.Inserted);

            if (removedCount == 0 && insertedCount == 0)
                return null;

            var detailLines = new List<string>();

            if (removedCount > 0)
            {
                detailLines.Add(removedCount == 1
                    ? Texts.ConfirmRiskyDetail_RemovedSingular
                    : string.Format(Texts.ConfirmRiskyDetail_Removals, removedCount));
            }

            if (insertedCount > 0)
            {
                detailLines.Add(insertedCount == 1
                    ? Texts.ConfirmRiskyDetail_InsertedSingular
                    : string.Format(Texts.ConfirmRiskyDetail_Insertions, insertedCount));
            }

            string detailBlock = string.Join("\n", detailLines);
            return string.Format(Texts.ConfirmUpdateMessage_RiskyChanges, detailBlock);
        }

        private async Task UpdateReferenceWithConfirmationAsync()
        {
            string? confirmMessage = BuildConfirmationMessage();

            if (confirmMessage is not null)
            {
                var eventArgs = new ConfirmationRequestedEventArgs(
                    Texts.ConfirmUpdateTitle,
                    confirmMessage,
                    ConfirmationIcon.Warning,
                    ConfirmationButton.YesNo);
                ConfirmationRequested?.Invoke(this, eventArgs);

                if (eventArgs.Result != ConfirmationResult.Yes)
                {
                    DiffStatusMessage = Texts.ReferenceUpdateCancelledStatus;
                    return;
                }
            }

            // Proceed with the update
            if (_mainViewModel.CreateReferenceCommand?.CanExecute(null) ?? false)
            {
                await _mainViewModel.CreateReferenceCommand.ExecuteAsync(null);
            }
        }

        private async void OnFileChangeDetected(object? sender, Coordinators.Events.ChangeDetectedEventArgs e)
        {
            string reason = e.HasChanges ? Texts.DetectedChangesStatus : Texts.MatchesReferenceStatus;
            await RefreshDiffAsync(reason);
        }

        private void OnConfigValidationChanged(object? sender, Coordinators.Events.ConfigValidationChangedEventArgs e)
        {
            IsConfigValid = e.IsValid;
        }

        partial void OnIsConfigValidChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowOverlay));
        }

        partial void OnIsOperationInProgressChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowOverlay));
        }

        // ── Sidebar card toggle commands ────────────────────────────────────────

        [RelayCommand]
        private void ToggleRemovalCard() => IsRemovalCardExpanded = !IsRemovalCardExpanded;

        [RelayCommand]
        private void ToggleInsertionCard() => IsInsertionCardExpanded = !IsInsertionCardExpanded;

        [RelayCommand]
        private void ToggleReplacementCard() => IsReplacementCardExpanded = !IsReplacementCardExpanded;

        [RelayCommand]
        private void ToggleAddedCard() => IsAddedCardExpanded = !IsAddedCardExpanded;

        [RelayCommand]
        private void ToggleMovedCard() => IsMovedCardExpanded = !IsMovedCardExpanded;

        [RelayCommand]
        private void ToggleNoChangesCard() => IsNoChangesCardExpanded = !IsNoChangesCardExpanded;

        // ── Persistence ─────────────────────────────────────────────────────────

        /// <summary>Returns the persisted sidebar width, or 300.0 if not available.</summary>
        internal double GetSidebarWidth()
        {
            return _mainViewModel?.Config.DiffSidebarWidth ?? 300.0;
        }

        /// <summary>
        /// Writes the current sidebar width and all card expansion states back to config
        /// and persists to disk. Guarded for the test-only constructor where
        /// <see cref="_mainViewModel"/> is <c>null</c>.
        /// </summary>
        internal async Task PersistWindowStateAsync(double sidebarWidth)
        {
            if (_mainViewModel is null)
            {
                return;
            }

            _mainViewModel.Config.DiffSidebarWidth = sidebarWidth;
            _mainViewModel.Config.DiffRemovalCardExpanded = IsRemovalCardExpanded;
            _mainViewModel.Config.DiffInsertionCardExpanded = IsInsertionCardExpanded;
            _mainViewModel.Config.DiffReplacementCardExpanded = IsReplacementCardExpanded;
            _mainViewModel.Config.DiffAddedCardExpanded = IsAddedCardExpanded;
            _mainViewModel.Config.DiffMovedCardExpanded = IsMovedCardExpanded;
            _mainViewModel.Config.DiffNoChangesCardExpanded = IsNoChangesCardExpanded;

            await SettingsService.SaveSettingsAsync(_mainViewModel.Config);
        }

        public void Dispose()
        {
            DiffLines.CollectionChanged -= OnDiffCollectionChanged;

            if (_mainViewModel is null)
            {
                return;
            }

            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            var fileCoordinator = _mainViewModel.GetFileMonitoringCoordinator();
            fileCoordinator.ChangeDetected -= OnFileChangeDetected;
            var configCoordinator = _mainViewModel.GetConfigurationCoordinator();
            configCoordinator.ValidationChanged -= OnConfigValidationChanged;
        }
    }
}
