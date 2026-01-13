using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;
using LoadOrderKeeper.Views;
using WpfApplication = System.Windows.Application;

namespace LoadOrderKeeper.ViewModels
{
    /// <summary>
    /// ViewModel for viewing and editing pending changes.
    /// </summary>
    public partial class ViewPendingChangesViewModel : ObservableObject
    {
        private readonly AppConfigModel _config;

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

        public string WindowTitle { get; } = "Pending Changes";
        public string ExplanationText { get; } = "This shows all changes you have made since the last reference update. When you next update the reference file, these changes will be archived.";
        public string CommentLabel { get; } = "Comment:";
        public string AddedModsLabel { get; } = "Added Mods:";
        public string RemovedModsLabel { get; } = "Removed Mods:";
        public string EditCommentButtonText { get; } = "Edit comment...";
        public string CloseButtonText { get; } = "Close";
        public string NoPendingChangesMessage { get; } = "No pending changes.";

        public event EventHandler? CloseRequested;

        public ViewPendingChangesViewModel(AppConfigModel config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _ = LoadPendingChangesAsync();
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
