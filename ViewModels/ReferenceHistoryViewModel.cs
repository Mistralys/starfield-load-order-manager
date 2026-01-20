using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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
    public partial class ReferenceHistoryViewModel : ObservableObject
    {
        private readonly ReferenceHistoryTexts _texts = new();
        private readonly AppConfigModel _config;
        private readonly ConfigurationCoordinator? _configCoordinator;

        [ObservableProperty]
        private ObservableCollection<ReferenceVersionMetadataModel> _versions = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RollbackCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVersionCommand))]
        private ReferenceVersionMetadataModel? _selectedVersion;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RollbackCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteVersionCommand))]
        [NotifyCanExecuteChangedFor(nameof(ClearHistoryCommand))]
        private bool _isLoading;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ClearHistoryCommand))]
        private bool _hasVersions;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowOverlay))]
        private bool _isConfigValid = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowOverlay))]
        private bool _isOperationInProgress;

        public bool ShowOverlay => !IsConfigValid && !IsOperationInProgress;

        public string WindowTitle => _texts.WindowTitle;
        public string RollbackButtonText => _texts.RollbackButtonText;
        public string DeleteVersionButtonText => _texts.DeleteVersionButtonText;
        public string ClearHistoryButtonText => _texts.ClearHistoryButtonText;
        public string CloseButtonText => _texts.CloseButtonText;
        public string NoVersionsMessage => _texts.NoVersionsMessage;
        public string VersionColumnHeader => _texts.VersionColumnHeader;
        public string DateColumnHeader => _texts.DateColumnHeader;
        public string ChangesColumnHeader => _texts.ChangesColumnHeader;
        public string SummaryColumnHeader => _texts.SummaryColumnHeader;
        public string FileMenuText => _texts.FileMenuText;
        public string ExitMenuText => _texts.ExitMenuText;
        public string EditMenuText => _texts.EditMenuText;
        public string ClearHistoryMenuText => _texts.ClearHistoryMenuText;

        public event EventHandler? CloseRequested;
        public event EventHandler<ReferenceVersionMetadataModel>? RollbackRequested;

        public ReferenceHistoryViewModel(AppConfigModel config, ConfigurationCoordinator? configCoordinator = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _configCoordinator = configCoordinator;

            if (_configCoordinator != null)
            {
                IsConfigValid = _configCoordinator.IsConfigValid;
                _configCoordinator.ValidationChanged += OnConfigValidationChanged;
            }

            _ = LoadVersionsAsync();
        }

        private void OnConfigValidationChanged(object? sender, Coordinators.Events.ConfigValidationChangedEventArgs e)
        {
            IsConfigValid = e.IsValid;
        }

        public async Task RefreshVersionsAsync()
        {
            await LoadVersionsAsync();
        }

        public async Task LoadVersionsAsync()
        {
            IsLoading = true;

            try
            {
                var versions = await ReferenceHistoryService.LoadVersionHistoryAsync(_config);
                
                Versions.Clear();
                foreach (var version in versions)
                {
                    Versions.Add(version);
                }

                HasVersions = Versions.Count > 0;
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to load version history: {ex.Message}",
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

        [RelayCommand(CanExecute = nameof(CanRollback))]
        private void Rollback()
        {
            if (SelectedVersion == null)
            {
                return;
            }

            RollbackRequested?.Invoke(this, SelectedVersion);
        }

        [RelayCommand(CanExecute = nameof(CanRollback))]
        private void RollbackVersion(ReferenceVersionMetadataModel? version)
        {
            if (version == null)
            {
                return;
            }

            RollbackRequested?.Invoke(this, version);
        }

        private bool CanRollback() => SelectedVersion != null && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanDeleteVersion))]
        private async Task DeleteVersionAsync()
        {
            if (SelectedVersion == null)
            {
                return;
            }

            var result = ConfirmationDialog.Show(
                "Delete Version",
                $"Are you sure you want to delete version {SelectedVersion.VersionNumber}?\n\nThis action cannot be undone.",
                ConfirmationIcon.Warning,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                WpfApplication.Current?.MainWindow);

            if (result != ConfirmationResult.Yes)
            {
                return;
            }

            try
            {
                await ReferenceHistoryService.DeleteVersionAsync(_config, SelectedVersion.VersionNumber);
                await LoadVersionsAsync();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to delete version: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
        }

        [RelayCommand]
        private async Task DeleteSpecificVersionAsync(ReferenceVersionMetadataModel? version)
        {
            if (version == null)
            {
                return;
            }

            var result = ConfirmationDialog.Show(
                "Delete Version",
                $"Are you sure you want to delete version {version.VersionNumber}?\n\nThis action cannot be undone.",
                ConfirmationIcon.Warning,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                WpfApplication.Current?.MainWindow);

            if (result != ConfirmationResult.Yes)
            {
                return;
            }

            try
            {
                await ReferenceHistoryService.DeleteVersionAsync(_config, version.VersionNumber);
                await LoadVersionsAsync();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to delete version: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
        }

        [RelayCommand]
        private async Task EditCommentAsync(ReferenceVersionMetadataModel? version)
        {
            if (version == null)
            {
                return;
            }

            var commentVm = new CommentInputViewModel(version.Comment ?? string.Empty);
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
                    await ReferenceHistoryService.UpdateVersionCommentAsync(_config, version.VersionNumber, commentVm.Comment);
                    await LoadVersionsAsync();
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

        private bool CanDeleteVersion() => SelectedVersion != null && !IsLoading;

        [RelayCommand(CanExecute = nameof(CanClearHistory))]
        private async Task ClearHistoryAsync()
        {
            var result = ConfirmationDialog.Show(
                "Clear History",
                "Are you sure you want to clear all version history?\n\nThis action cannot be undone.",
                ConfirmationIcon.Warning,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                WpfApplication.Current?.MainWindow);

            if (result != ConfirmationResult.Yes)
            {
                return;
            }

            try
            {
                await ReferenceHistoryService.ClearHistoryAsync(_config);
                await LoadVersionsAsync();
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Error",
                    $"Failed to clear history: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    WpfApplication.Current?.MainWindow);
            }
        }

        private bool CanClearHistory() => HasVersions && !IsLoading;

        [RelayCommand]
        private void Close()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
