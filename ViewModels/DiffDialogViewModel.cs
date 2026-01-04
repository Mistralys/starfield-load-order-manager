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

namespace LoadOrderKeeper.ViewModels
{
    public class UpdateReferenceConfirmationEventArgs : EventArgs
    {
        public string Message { get; }
        public bool Confirmed { get; set; }

        public UpdateReferenceConfirmationEventArgs(string message)
        {
            Message = message;
            Confirmed = false;
        }
    }

    public partial class DiffDialogViewModel : ObservableObject, IDisposable
    {
        private readonly MainViewModel _mainViewModel;
        private bool _suppressCollectionNotification;
        private string _lastDiffSignature = string.Empty;
        private bool _isRefreshing;

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
            UpdateDiffState();
            _lastDiffSignature = BuildSignature(DiffLines);
            DiffStatusMessage = "Differences loaded.";
        }

        public ObservableCollection<DiffLineModel> DiffLines { get; }

        public string Title => "Detected Changes";

        public string Description => "Review differences between Plugins.txt and the reference file.";

        public string UpdateReferenceButtonText => _mainViewModel.ReferenceButtonText;

        public string FixLoadOrderButtonText => _mainViewModel.FixLoadOrderButtonText;

        public string DiscardChangesButtonText { get; } = "Discard all changes";

        public string CloseButtonText { get; } = "Close";

        public string NoDifferencesMessage { get; } = "No differences detected.";

        public string ReEnableModMenuText { get; } = "Re-enable mod";

        public string ReplaceWithMenuText { get; } = "Replace with...";

        public string RemoveModMenuText { get; } = "Remove mod";

        public bool ShowSortingRecommendation => HasDifferences && _mainViewModel.SortingRecommendationActive;

        public string SortingRecommendationMessage => _mainViewModel.SortingRecommendationMessage;

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
        public event EventHandler<UpdateReferenceConfirmationEventArgs>? UpdateReferenceConfirmationRequested;

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
            else if (e.PropertyName == nameof(MainViewModel.FixLoadOrderButtonText))
            {
                OnPropertyChanged(nameof(FixLoadOrderButtonText));
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
            OnPropertyChanged(nameof(AddedMods));
            OnPropertyChanged(nameof(HasAddedMods));
            OnPropertyChanged(nameof(HasInsertedMods));
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
            DiffLines.Clear();
            foreach (var line in newLines)
            {
                DiffLines.Add(line);
            }
            _suppressCollectionNotification = false;
            UpdateDiffState();
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
                    string prefix = string.IsNullOrWhiteSpace(reason) ? "Differences updated" : reason;
                    DiffStatusMessage = $"{prefix} at {DateTime.Now:T}.";
                    RequestScroll();
                }
                else
                {
                    DiffStatusMessage = $"No new differences detected ({DateTime.Now:T}).";
                }

                return signatureChanged;
            }
            catch (Exception ex)
            {
                DiffStatusMessage = $"Failed to refresh differences: {ex.Message}";
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
                DiffStatusMessage = "No differences to discard.";
                return;
            }

            var discardCommand = _mainViewModel.DiscardChangesCommand;
            if (discardCommand is null || !discardCommand.CanExecute(null))
            {
                DiffStatusMessage = "Cannot discard changes right now.";
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
                    await RefreshDiffAsync($"Re-enabled {line.FileName}");
                }
                else
                {
                    DiffStatusMessage = $"{line.FileName} is already enabled.";
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = $"Failed to re-enable {line.FileName}: {ex.Message}";
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
                    await RefreshDiffAsync($"Removed {line.FileName}");
                }
                else
                {
                    DiffStatusMessage = $"{line.FileName} is already removed.";
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = $"Failed to remove {line.FileName}: {ex.Message}";
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
                    await RefreshDiffAsync($"Replaced {removed.FileName} with {replacement.FileName}");
                }
                else
                {
                    DiffStatusMessage = $"{replacement.FileName} is no longer pending.";
                }
            }
            catch (Exception ex)
            {
                DiffStatusMessage = $"Failed to replace {removed.FileName}: {ex.Message}";
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
                // Calculate total affected mods (removed + inserted + their dependent changes)
                int totalAffectedMods = removedMods.Count + insertedMods.Count;
                totalAffectedMods += removedMods.Sum(mod => mod.DependentChanges.Count);
                totalAffectedMods += insertedMods.Sum(mod => mod.DependentChanges.Count);

                // Build confirmation message
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("You are about to update the reference file with the following changes:");
                messageBuilder.AppendLine();

                if (hasRemovedMods)
                {
                    messageBuilder.AppendLine($"• {removedMods.Count} mod(s) have been removed.");
                }

                if (hasInsertedMods)
                {
                    messageBuilder.AppendLine($"• {insertedMods.Count} mod(s) have been inserted.");
                }

                messageBuilder.AppendLine();
                messageBuilder.AppendLine($"This affects a total of {totalAffectedMods} mod(s).");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("These changes will become the new reference state.");
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("Do you want to continue?");

                // Request confirmation from the view
                var eventArgs = new UpdateReferenceConfirmationEventArgs(messageBuilder.ToString());
                UpdateReferenceConfirmationRequested?.Invoke(this, eventArgs);

                if (!eventArgs.Confirmed)
                {
                    DiffStatusMessage = "Reference update cancelled.";
                    return;
                }
            }

            // Proceed with the update
            if (_mainViewModel.CreateReferenceCommand?.CanExecute(null) ?? false)
            {
                await _mainViewModel.CreateReferenceCommand.ExecuteAsync(null);
            }
        }

        public void Dispose()
        {
            DiffLines.CollectionChanged -= OnDiffCollectionChanged;
            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
        }
    }
}
