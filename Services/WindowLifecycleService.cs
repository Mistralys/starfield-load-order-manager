using System.Windows;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewModels;
using LoadOrderKeeper.Views;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Manages non-modal window lifecycle, including singleton tracking, activation, and cleanup.
    /// Provides a simplified service layer for window management operations.
    /// </summary>
    public class WindowLifecycleService
    {
        private ManageProfilesWindow? _manageProfilesWindow;
        private DiffWindow? _diffWindow;
        private ReferenceHistoryWindow? _referenceHistoryWindow;
        private ViewPendingChangesWindow? _viewPendingChangesWindow;

        /// <summary>
        /// Shows the Manage Profiles window. If already open, brings it to front.
        /// </summary>
        public void ShowManageProfilesWindow(AppConfigModel config, ConfigurationCoordinator configCoordinator, Window? owner = null, Action? onClosed = null)
        {
            if (_manageProfilesWindow != null)
            {
                _manageProfilesWindow.Activate();
                _manageProfilesWindow.Focus();
                return;
            }

            var manageVm = new ManageProfilesViewModel(config, configCoordinator);
            _manageProfilesWindow = new ManageProfilesWindow(config)
            {
                Owner = owner,
                DataContext = manageVm
            };

            _manageProfilesWindow.Closed += (s, e) =>
            {
                _manageProfilesWindow = null;
                onClosed?.Invoke();
            };

            _manageProfilesWindow.Show();
        }

        /// <summary>
        /// Shows the Diff window. If already open, brings it to front.
        /// </summary>
        public async Task ShowDiffWindowAsync(AppConfigModel config, MainViewModel mainViewModel, Window? owner = null)
        {
            if (_diffWindow != null)
            {
                _diffWindow.Activate();
                _diffWindow.Focus();
                return;
            }

            var diffLines = await DiffService.GetPluginsDiffAsync(config);
            
            var diffViewModel = new DiffDialogViewModel(diffLines, mainViewModel);
            _diffWindow = new DiffWindow
            {
                Owner = owner,
                DataContext = diffViewModel
            };

            _diffWindow.Closed += (s, e) =>
            {
                _diffWindow = null;
            };

            _diffWindow.Show();
        }

        /// <summary>
        /// Shows the Reference History window. If already open, brings it to front.
        /// </summary>
        public void ShowReferenceHistoryWindow(
            AppConfigModel config, 
            ConfigurationCoordinator configCoordinator, 
            Window? owner = null, 
            EventHandler<ReferenceVersionMetadataModel>? onRollbackRequested = null)
        {
            if (_referenceHistoryWindow != null)
            {
                _referenceHistoryWindow.Activate();
                _referenceHistoryWindow.Focus();
                return;
            }

            var historyVm = new ReferenceHistoryViewModel(config, configCoordinator);
            _referenceHistoryWindow = new ReferenceHistoryWindow
            {
                Owner = owner,
                DataContext = historyVm
            };

            if (onRollbackRequested != null)
            {
                historyVm.RollbackRequested += onRollbackRequested;
            }

            _referenceHistoryWindow.Closed += (s, e) =>
            {
                _referenceHistoryWindow = null;
            };

            _referenceHistoryWindow.Show();
        }

        /// <summary>
        /// Shows the View Pending Changes window. If already open, brings it to front.
        /// </summary>
        public void ShowViewPendingChangesWindow(AppConfigModel config, ConfigurationCoordinator configCoordinator, Window? owner = null)
        {
            if (_viewPendingChangesWindow != null)
            {
                _viewPendingChangesWindow.Activate();
                _viewPendingChangesWindow.Focus();
                return;
            }

            var pendingChangesVm = new ViewPendingChangesViewModel(config, configCoordinator);
            _viewPendingChangesWindow = new ViewPendingChangesWindow
            {
                Owner = owner,
                DataContext = pendingChangesVm
            };

            _viewPendingChangesWindow.Closed += (s, e) =>
            {
                _viewPendingChangesWindow = null;
            };

            _viewPendingChangesWindow.Show();
        }

        /// <summary>
        /// Refreshes the Reference History window if it's currently open.
        /// </summary>
        public async Task RefreshReferenceHistoryWindowAsync()
        {
            if (_referenceHistoryWindow?.DataContext is ReferenceHistoryViewModel historyVm)
            {
                await historyVm.RefreshVersionsAsync();
            }
        }

        /// <summary>
        /// Closes all managed windows.
        /// </summary>
        public void CloseAllWindows()
        {
            _diffWindow?.Close();
            _manageProfilesWindow?.Close();
            _referenceHistoryWindow?.Close();
            _viewPendingChangesWindow?.Close();
        }

        /// <summary>
        /// Checks if the Diff window is currently open.
        /// </summary>
        public bool IsDiffWindowOpen => _diffWindow != null;

        /// <summary>
        /// Checks if the Manage Profiles window is currently open.
        /// </summary>
        public bool IsManageProfilesWindowOpen => _manageProfilesWindow != null;

        /// <summary>
        /// Checks if the Reference History window is currently open.
        /// </summary>
        public bool IsReferenceHistoryWindowOpen => _referenceHistoryWindow != null;

        /// <summary>
        /// Checks if the View Pending Changes window is currently open.
        /// </summary>
        public bool IsViewPendingChangesWindowOpen => _viewPendingChangesWindow != null;
    }
}
