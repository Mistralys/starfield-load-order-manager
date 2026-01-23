using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.ViewTexts;
using LoadOrderKeeper.Views;
using WpfApplication = System.Windows.Application;

namespace LoadOrderKeeper.ViewModels
{
    /// <summary>
    /// ViewModel for viewing and editing pending changes.
    /// </summary>
    public partial class ViewPendingChangesViewModel : ObservableObject
    {
        private readonly ViewPendingChangesTexts _texts = new();
        private readonly AppConfigModel _config;
        private readonly ConfigurationCoordinator? _configCoordinator;

        [ObservableProperty]
        private string _comment = string.Empty;

        [ObservableProperty]
        private string _commentDisplay = "(No comment entered)";

        [ObservableProperty]
        private List<string> _addedMods = new();

        [ObservableProperty]
        private List<string> _removedMods = new();

        [ObservableProperty]
        private bool _hasAddedMods;

        [ObservableProperty]
        private bool _hasRemovedMods;

        [ObservableProperty]
        private bool _hasPendingChanges;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _totalChanges;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowOverlay))]
        private bool _isConfigValid = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowOverlay))]
        private bool _isOperationInProgress;

        public bool ShowOverlay => !IsConfigValid && !IsOperationInProgress;

        public string WindowTitle => _texts.WindowTitle;
        public string ExplanationText => _texts.ExplanationText;
        public string CommentLabel => _texts.CommentLabel;
        public string AddedModsLabel => _texts.AddedModsLabel;
        public string RemovedModsLabel => _texts.RemovedModsLabel;
        public string EditCommentButtonText => _texts.EditCommentButtonText;
        public string CloseButtonText => _texts.CloseButtonText;
        public string NoPendingChangesMessage => _texts.NoPendingChangesMessage;

        public event EventHandler? CloseRequested;

        public ViewPendingChangesViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _configCoordinator = configCoordinator;

            if (_configCoordinator != null)
            {
                IsConfigValid = _configCoordinator.IsConfigValid;
                _configCoordinator.ValidationChanged += OnConfigValidationChanged;
            }

            _ = LoadPendingChangesAsync();
        }

        private void OnConfigValidationChanged(object? sender, Coordinators.Events.ConfigValidationChangedEventArgs e)
        {
            IsConfigValid = e.IsValid;
        }

        public async Task LoadPendingChangesAsync()
        {
            IsLoading = true;

            try
            {
                var pendingChanges = await ReferenceHistoryService.LoadPendingChangesAsync(_config);

                Comment = pendingChanges.Comment ?? string.Empty;
                CommentDisplay = string.IsNullOrWhiteSpace(Comment) ? "(No comment entered)" : Comment;
                AddedMods = pendingChanges.AddedMods.ToList();
                RemovedMods = pendingChanges.RemovedMods.ToList();
                HasAddedMods = AddedMods.Count > 0;
                HasRemovedMods = RemovedMods.Count > 0;
                TotalChanges = pendingChanges.TotalChanges;
                HasPendingChanges = !pendingChanges.IsEmpty;
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to load pending changes: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task EditCommentAsync()
        {
            var commentVm = new CommentInputViewModel(Comment);
            var commentDialog = new CommentInputDialog
            {
                Owner = WpfApplication.Current?.MainWindow,
                DataContext = commentVm
            };

            bool? result = commentDialog.ShowDialog();
            if (result == true)
            {
                try
                {
                    // Load current pending changes
                    var pendingChanges = await ReferenceHistoryService.LoadPendingChangesAsync(_config);
                    
                    // Update comment
                    pendingChanges.Comment = commentVm.Comment;
                    
                    // Save back
                    await ReferenceHistoryService.SavePendingChangesAsync(_config, pendingChanges);
                    
                    // Reload to refresh display
                    await LoadPendingChangesAsync();
                }
                catch (Exception ex)
                {
                    ConfirmationDialog.Show(
                        "Error",
                        $"Failed to update comment: {ex.Message}",
                        ConfirmationIcon.Error,
                        ConfirmationButton.OK,
                        ConfirmationResult.OK,
                        WpfApplication.Current?.MainWindow);
                }
            }
        }

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
