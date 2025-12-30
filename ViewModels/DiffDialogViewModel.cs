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
            UpdateReferenceCommand = _mainViewModel.CreateReferenceCommand;
            FixLoadOrderCommand = _mainViewModel.FixLoadOrderCommand;
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

        public bool ShowSortingRecommendation => HasDifferences && _mainViewModel.SortingRecommendationActive;

        public string SortingRecommendationMessage => _mainViewModel.SortingRecommendationMessage;

        public IAsyncRelayCommand UpdateReferenceCommand { get; }

        public IAsyncRelayCommand FixLoadOrderCommand { get; }

        public event EventHandler? CloseRequested;
        public event EventHandler? ScrollRequested;

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

        public void Dispose()
        {
            DiffLines.CollectionChanged -= OnDiffCollectionChanged;
            _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
        }
    }
}
