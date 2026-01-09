using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Coordinators.Events;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates file monitoring, change detection, and Steam status detection.
    /// Runs periodic checks and notifies subscribers of state changes via events.
    /// </summary>
    public sealed partial class FileMonitoringCoordinator : CoordinatorBase
    {
        private const int PluginCheckIntervalSeconds = 3;

        private readonly DispatcherTimer _pluginsMonitorTimer;
        private bool _isCheckingPluginsFile;
        private string _lastObservedPluginsSignature = string.Empty;
        private AppConfigModel? _config;
        private bool _refExists;
        private bool _isBusy;
        private bool _configIsInvalid;

        #region Observable Properties

        [ObservableProperty]
        private bool _pluginsFileChangedExternally;

        [ObservableProperty]
        private bool _isSteamInstalled;

        [ObservableProperty]
        private bool _isSteamRunning;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SteamWarningTooltip))]
        private bool _showSteamWarning;

        [ObservableProperty]
        private string _sortingRecommendationMessage = string.Empty;

        [ObservableProperty]
        private bool _sortingRecommendationActive;

        [ObservableProperty]
        private int _changeCount;

        #endregion

        #region Computed Properties

        /// <summary>
        /// Tooltip text for Steam warning. Empty when no warning is active.
        /// </summary>
        public string SteamWarningTooltip => ShowSteamWarning
            ? "Steam is not running. SFSE requires Steam to be open to function correctly."
            : string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// Raised when changes are detected between current and reference files.
        /// </summary>
        public event EventHandler<ChangeDetectedEventArgs>? ChangeDetected;

        /// <summary>
        /// Raised when Steam warning state changes.
        /// </summary>
        public event EventHandler<SteamWarningChangedEventArgs>? SteamWarningChanged;

        /// <summary>
        /// Raised when sorting recommendation state changes.
        /// </summary>
        public event EventHandler<SortingRecommendationChangedEventArgs>? SortingRecommendationChanged;

        #endregion

        public FileMonitoringCoordinator()
        {
            _pluginsMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(PluginCheckIntervalSeconds)
            };
            _pluginsMonitorTimer.Tick += OnPluginsMonitorTick;
        }

        /// <summary>
        /// Updates the configuration and reference existence state.
        /// Call this whenever configuration or reference file state changes.
        /// </summary>
        public void UpdateState(AppConfigModel config, bool refExists, bool isBusy, bool configIsInvalid)
        {
            ThrowIfDisposed();

            _config = config;
            _refExists = refExists;
            _isBusy = isBusy;
            _configIsInvalid = configIsInvalid;

            ConfigureMonitoring();
        }

        /// <summary>
        /// Starts monitoring if conditions are met, stops otherwise.
        /// </summary>
        private void ConfigureMonitoring()
        {
            if (_config?.IsValid() == true && _refExists)
            {
                if (!_pluginsMonitorTimer.IsEnabled)
                {
                    _pluginsMonitorTimer.Start();
                }
            }
            else
            {
                if (_pluginsMonitorTimer.IsEnabled)
                {
                    _pluginsMonitorTimer.Stop();
                }

                PluginsFileChangedExternally = false;
                ChangeCount = 0;
            }
        }

        private async void OnPluginsMonitorTick(object? sender, EventArgs e)
        {
            await CheckPluginsFileAsync();
        }

        /// <summary>
        /// Performs a check of the plugins file and updates state accordingly.
        /// </summary>
        public async Task CheckPluginsFileAsync()
        {
            ThrowIfDisposed();

            if (_isCheckingPluginsFile || _isBusy || _config == null)
            {
                return;
            }

            // Update Steam detection state (only if Steam-installed)
            UpdateSteamDetectionState();

            if (_configIsInvalid || !_config.IsValid() || !_refExists)
            {
                PluginsFileChangedExternally = false;
                ChangeCount = 0;
                ChangeDetected?.Invoke(this, new ChangeDetectedEventArgs(false, 0));
                return;
            }

            _isCheckingPluginsFile = true;

            try
            {
                var comparison = await FileService.ComparePluginsWithReferenceAsync(_config);
                bool hasChanged = comparison.HasDifferences;
                bool signatureChanged = !string.Equals(_lastObservedPluginsSignature, comparison.PluginsSignature, StringComparison.Ordinal);
                _lastObservedPluginsSignature = comparison.PluginsSignature;
                bool sortingRecommendation = false;
                bool hasInsertedMods = false;

                if (hasChanged)
                {
                    // Only recommend sorting if there are independent moved mods
                    // (mods that have changed position but aren't part of dependent change lists)
                    sortingRecommendation = await DiffService.HasIndependentMovedModsAsync(_config);

                    // Check if there are any inserted mods for the warning message
                    var diffLines = await DiffService.GetPluginsDiffAsync(_config);
                    hasInsertedMods = diffLines.Any(line => line.ChangeType == DiffChangeType.Inserted);
                }

                await UpdateChangeCountDisplayAsync(hasChanged);

                if (hasChanged != PluginsFileChangedExternally)
                {
                    PluginsFileChangedExternally = hasChanged;
                    ChangeDetected?.Invoke(this, new ChangeDetectedEventArgs(hasChanged, ChangeCount));
                }

                UpdateSortingRecommendationState(sortingRecommendation, hasInsertedMods);
            }
            catch (Exception)
            {
                // Silently fail - MainViewModel will handle logging via events
                ChangeCount = 0;
            }
            finally
            {
                _isCheckingPluginsFile = false;
            }
        }

        /// <summary>
        /// Updates Steam detection state if the game is installed via Steam.
        /// This method is called on every file monitoring tick (every 3 seconds).
        /// </summary>
        private void UpdateSteamDetectionState()
        {
            try
            {
                // Only check Steam status if Starfield was installed via Steam
                bool steamInstalled = SettingsService.IsStarfieldInstalledViaSteam();
                IsSteamInstalled = steamInstalled;

                bool previousWarningState = ShowSteamWarning;
                string previousTooltip = SteamWarningTooltip;

                if (steamInstalled)
                {
                    bool steamRunning = SettingsService.IsSteamRunning();
                    IsSteamRunning = steamRunning;

                    // Show warning only if SFSE is installed AND Steam is NOT running
                    bool hasSfse = HasSfseExecutable();
                    ShowSteamWarning = hasSfse && !steamRunning;
                }
                else
                {
                    // Not installed via Steam, no warning needed
                    IsSteamRunning = false;
                    ShowSteamWarning = false;
                }

                // Notify if warning state changed
                if (previousWarningState != ShowSteamWarning || previousTooltip != SteamWarningTooltip)
                {
                    SteamWarningChanged?.Invoke(this, new SteamWarningChangedEventArgs(ShowSteamWarning, SteamWarningTooltip));
                }
            }
            catch
            {
                // Detection failed, fail silently
                IsSteamInstalled = false;
                IsSteamRunning = false;
                ShowSteamWarning = false;
            }
        }

        private bool HasSfseExecutable()
        {
            var gamePath = _config?.StarfieldGamePath;
            if (string.IsNullOrWhiteSpace(gamePath))
            {
                return false;
            }

            string sfsePath = System.IO.Path.Combine(gamePath, "sfse_loader.exe");
            return System.IO.File.Exists(sfsePath);
        }

        private async Task UpdateChangeCountDisplayAsync(bool hasDifferences)
        {
            if (!hasDifferences || _config == null)
            {
                ChangeCount = 0;
                return;
            }

            try
            {
                var diffLines = await DiffService.GetPluginsDiffAsync(_config);
                int totalCount = diffLines.Count;

                // Include dependent changes in the total count
                foreach (var line in diffLines)
                {
                    totalCount += line.DependentChanges.Count;
                }

                ChangeCount = totalCount;
            }
            catch
            {
                ChangeCount = 0;
            }
        }

        private void UpdateSortingRecommendationState(bool sortingRecommended, bool hasInsertedMods = false)
        {
            bool previousState = SortingRecommendationActive;
            string previousMessage = SortingRecommendationMessage;

            if (sortingRecommended)
            {
                if (hasInsertedMods)
                {
                    SortingRecommendationMessage = "?? IMPORTANT: Mods were inserted in the middle of the load order. Sort the list first to move them to the end before making other changes.";
                }
                else
                {
                    SortingRecommendationMessage = "Sorting recommended: run Fix Load Order before resolving other changes.";
                }
                SortingRecommendationActive = true;
            }
            else
            {
                SortingRecommendationMessage = string.Empty;
                SortingRecommendationActive = false;
            }

            // Notify if state changed
            if (previousState != SortingRecommendationActive || previousMessage != SortingRecommendationMessage)
            {
                SortingRecommendationChanged?.Invoke(this, new SortingRecommendationChangedEventArgs(
                    SortingRecommendationActive,
                    SortingRecommendationMessage));
            }
        }

        protected override void OnDisposing()
        {
            if (_pluginsMonitorTimer != null)
            {
                _pluginsMonitorTimer.Stop();
                _pluginsMonitorTimer.Tick -= OnPluginsMonitorTick;
            }
        }
    }
}
