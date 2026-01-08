using System;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewModels;
using LoadOrderKeeper.Views;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates non-modal window lifecycle management.
    /// Prevents duplicate windows and manages cleanup.
    /// </summary>
    public sealed class WindowManager : CoordinatorBase
    {
        private DiffWindow? _diffWindow;
        private ManageProfilesWindow? _manageProfilesWindow;
        private ReferenceHistoryWindow? _referenceHistoryWindow;

        #region Diff Window

        /// <summary>
        /// Shows the diff window or brings existing instance to front.
        /// </summary>
        public DiffWindow? ShowOrActivateDiffWindow(
            System.Collections.Generic.IEnumerable<DiffLineModel> diffLines,
            MainViewModel mainViewModel,
            System.Windows.Window? owner)
        {
            ThrowIfDisposed();

            // If already open, bring to front
            if (_diffWindow != null)
            {
                _diffWindow.Activate();
                _diffWindow.Focus();
                return _diffWindow;
            }

            var diffViewModel = new DiffDialogViewModel(diffLines, mainViewModel);
            _diffWindow = new DiffWindow
            {
                Owner = owner,
                DataContext = diffViewModel
            };

            // Clean up reference when closed
            _diffWindow.Closed += (s, e) =>
            {
                _diffWindow = null;
            };

            _diffWindow.Show();
            return _diffWindow;
        }

        /// <summary>
        /// Gets the active diff dialog ViewModel if window is open.
        /// </summary>
        public DiffDialogViewModel? GetActiveDiffDialog()
        {
            return _diffWindow?.DataContext as DiffDialogViewModel;
        }

        #endregion

        #region Manage Profiles Window

        /// <summary>
        /// Shows the manage profiles window or brings existing instance to front.
        /// </summary>
        public void ShowOrActivateManageProfiles(AppConfigModel config, System.Windows.Window? owner)
        {
            ThrowIfDisposed();

            // If already open, bring to front
            if (_manageProfilesWindow != null)
            {
                _manageProfilesWindow.Activate();
                _manageProfilesWindow.Focus();
                return;
            }

            var manageVm = new ManageProfilesViewModel(config);
            _manageProfilesWindow = new ManageProfilesWindow(config)
            {
                Owner = owner,
                DataContext = manageVm
            };

            // Clean up reference when closed
            _manageProfilesWindow.Closed += (s, e) =>
            {
                _manageProfilesWindow = null;
            };

            _manageProfilesWindow.Show();
        }

        /// <summary>
        /// Event raised when manage profiles window is closed.
        /// Subscribe to this to refresh profile state in MainViewModel.
        /// </summary>
        public event EventHandler? ManageProfilesWindowClosed
        {
            add
            {
                if (_manageProfilesWindow != null)
                {
                    _manageProfilesWindow.Closed += value;
                }
            }
            remove
            {
                if (_manageProfilesWindow != null)
                {
                    _manageProfilesWindow.Closed -= value;
                }
            }
        }

        #endregion

        #region Reference History Window

        /// <summary>
        /// Shows the reference history window or brings existing instance to front.
        /// </summary>
        public void ShowOrActivateReferenceHistory(
            AppConfigModel config,
            System.Windows.Window? owner,
            EventHandler<ReferenceVersionMetadataModel>? rollbackRequestedHandler = null)
        {
            ThrowIfDisposed();

            // If already open, bring to front
            if (_referenceHistoryWindow != null)
            {
                _referenceHistoryWindow.Activate();
                _referenceHistoryWindow.Focus();
                return;
            }

            var historyVm = new ReferenceHistoryViewModel(config);
            _referenceHistoryWindow = new ReferenceHistoryWindow
            {
                Owner = owner,
                DataContext = historyVm
            };

            // Handle rollback request if handler provided
            if (rollbackRequestedHandler != null)
            {
                historyVm.RollbackRequested += rollbackRequestedHandler;
            }

            // Clean up reference when closed
            _referenceHistoryWindow.Closed += (s, e) =>
            {
                if (rollbackRequestedHandler != null && historyVm != null)
                {
                    historyVm.RollbackRequested -= rollbackRequestedHandler;
                }
                _referenceHistoryWindow = null;
            };

            _referenceHistoryWindow.Show();
        }

        /// <summary>
        /// Refreshes the reference history window if it's open.
        /// </summary>
        public async System.Threading.Tasks.Task RefreshReferenceHistoryAsync()
        {
            if (_referenceHistoryWindow?.DataContext is ReferenceHistoryViewModel historyVm)
            {
                await historyVm.RefreshVersionsAsync();
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// Closes all managed windows.
        /// </summary>
        public void CloseAll()
        {
            ThrowIfDisposed();

            _diffWindow?.Close();
            _manageProfilesWindow?.Close();
            _referenceHistoryWindow?.Close();
        }

        protected override void OnDisposing()
        {
            CloseAll();
        }

        #endregion
    }
}
