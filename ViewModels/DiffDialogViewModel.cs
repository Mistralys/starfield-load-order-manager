using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private bool _suppressCollectionNotification;
        private string _lastDiffSignature = string.Empty;
        private bool _isRefreshing;

        [ObservableProperty]
        private bool _isConfigValid = true;

        [ObservableProperty]
        private bool _isOperationInProgress;

        public bool ShowOverlay => !IsConfigValid && !IsOperationInProgress;

        // Add localization texts
        public DiffDialogTexts Texts { get; } = new();
        public MenuViewModel Menu => _mainViewModel.Menu;

        public DiffDialogViewModel(IEnumerable<DiffLineModel> diffLines, MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            DiffLines = new ObservableCollection<DiffLineModel>(diffLines);
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
            
            UpdateDiffState();
            _lastDiffSignature = BuildSignature(DiffLines);
            DiffStatusMessage = Texts.DifferencesLoadedStatus;
        }

        public ObservableCollection<DiffLineModel> DiffLines { get; }

        public string Title => Texts.WindowTitle;

        public string Description => Texts.DescriptionText;

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

        public bool ShowSortingRecommendation => HasDifferences && _mainViewModel.SortingRecommendationActive;

        public string SortingRecommendationMessage => _mainViewModel.SortingRecommendationMessage;

        public bool ShowMultipleReplacementsHelp => HasMultipleReplacementsOrRemovals;

        public string MultipleReplacementsHelpMessage => Texts.MultipleChangesHelp;

        private bool HasMultipleReplacementsOrRemovals => 
            DiffLines.Count(line => line.ChangeType == DiffChangeType.Removed || line.ChangeType == DiffChangeType.Replaced) > 1;

        public IReadOnlyList<DiffLineModel> AddedMods => DiffLines.Where(line => line.ChangeType == DiffChangeType.Added).ToList();

        public bool HasAddedMods => DiffLines.Any(line => line.ChangeType == DiffChangeType.Added);

        public bool HasInsertedMods => DiffLines.Any(line => line.ChangeType == DiffChangeType.Inserted);

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

        private void UpdateDiffState()
        {
            HasDifferences = DiffLines.Any(line => line.ChangeType != DiffChangeType.Unchanged);
            ScrollTargetIndex = ComputeScrollTargetIndex();
            OnPropertyChanged(nameof(ShowSortingRecommendation));
            OnPropertyChanged(nameof(ShowMultipleReplacementsHelp));
            OnPropertyChanged(nameof(AddedMods));
            OnPropertyChanged(nameof(HasAddedMods));
            OnPropertyChanged(nameof(HasInsertedMods));
            OnPropertyChanged(nameof(UpdateReferenceButtonText));
        }

        private int ComputeScrollTargetIndex()
        {
            if (DiffLines.Count == 0)
            {
                return -1;
            }

            if (HasDifferences)
            {
                for (int index = 0; index < DiffLines.Count; index++)
                {
                    if (DiffLines[index].ChangeType != DiffChangeType.Unchanged)
                    {
                        return index;
                    }
                }
            }

            return DiffLines.Count - 1;
        }

        private void ReplaceDiffLines(IEnumerable<DiffLineModel> newLines)
        {
            _suppressCollectionNotification = true;
            try
            {
                DiffLines.Clear();
                foreach (var line in newLines)
                {
                    DiffLines.Add(line);
                }
            }
            finally
            {
                _suppressCollectionNotification = false;
            }
            
            // Force property change notifications for all computed properties
            UpdateDiffState();
            
            // Also notify that the DiffLines collection property itself may have changed
            // This ensures WPF rebinds to the collection
            OnPropertyChanged(nameof(DiffLines));
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
                var latestLines = await DiffService.GetPluginsDiffAsync(_mainViewModel.Config);
                string newSignature = BuildSignature(latestLines);

                bool signatureChanged = !string.Equals(newSignature, _lastDiffSignature, StringComparison.Ordinal);
                
                if (signatureChanged)
                {
                    ReplaceDiffLines(latestLines);
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
            return _mainViewModel.CreateReferenceCommand?.CanExecute(null) ?? false;
        }

        private async Task UpdateReferenceWithConfirmationAsync()
        {
            // Check if we have removed or inserted mods that require confirmation
            var removedMods = DiffLines.Where(line => line.ChangeType == DiffChangeType.Removed).ToList();
            var insertedMods = DiffLines.Where(line => line.ChangeType == DiffChangeType.Inserted).ToList();

            bool hasRemovedMods = removedMods.Any();
            bool hasInsertedMods = insertedMods.Any();

            if (hasRemovedMods || hasInsertedMods)
            {
                // Request confirmation from the view
                var eventArgs = new ConfirmationRequestedEventArgs(
                    Texts.ConfirmUpdateTitle,
                    Texts.ConfirmUpdateMessage,
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
            string reason = e.HasChanges ? "Detected changes" : "Plugins.txt now matches the reference";
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

        public void Dispose()
        {
            DiffLines.CollectionChanged -= OnDiffCollectionChanged;
            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
            var fileCoordinator = _mainViewModel.GetFileMonitoringCoordinator();
            fileCoordinator.ChangeDetected -= OnFileChangeDetected;
            var configCoordinator = _mainViewModel.GetConfigurationCoordinator();
            configCoordinator.ValidationChanged -= OnConfigValidationChanged;
        }
    }
}
