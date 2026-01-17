using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates application update checking and notification.
    /// </summary>
    public sealed partial class UpdateCheckCoordinator : CoordinatorBase
    {
        private readonly CancellationTokenSource _shutdownCts = new();

        [ObservableProperty]
        private bool _updateAvailable;

        [ObservableProperty]
        private string _updateMessage = string.Empty;

        [ObservableProperty]
        private bool _updateInfoBarVisible;

        /// <summary>
        /// Performs a background update check (silent failure, 24-hour cache).
        /// </summary>
        public async Task CheckForUpdatesBackgroundAsync()
        {
            try
            {
                var result = await UpdateCheckService.CheckForUpdatesAsync(bypassCache: false, _shutdownCts.Token);

                if (result.UpdateAvailable)
                {
                    UpdateAvailable = true;
                    UpdateMessage = string.Format(Resources.MainWindowResources.UpdateAvailableFormat, result.LatestVersion);
                    UpdateInfoBarVisible = true;
                }
            }
            catch
            {
                // Silent failure for background check
            }
        }

        /// <summary>
        /// Performs a manual update check (shows dialog on result, bypasses cache).
        /// Returns true if update available, false otherwise.
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesManualAsync()
        {
            var result = await UpdateCheckService.CheckForUpdatesAsync(bypassCache: true);

            if (result.UpdateAvailable)
            {
                UpdateAvailable = true;
                UpdateMessage = string.Format(Resources.MainWindowResources.UpdateAvailableFormat, result.LatestVersion);
                UpdateInfoBarVisible = true;
            }

            return result;
        }

        /// <summary>
        /// Dismisses the update notification info bar.
        /// </summary>
        public void DismissUpdateNotification()
        {
            ThrowIfDisposed();
            UpdateInfoBarVisible = false;
        }

        /// <summary>
        /// Gets the latest version string from the update message, or null if not available.
        /// </summary>
        public string? GetLatestVersion()
        {
            if (string.IsNullOrEmpty(UpdateMessage))
            {
                return null;
            }

            // Extract version using the localized format pattern
            // The format is "Version {0} is available!" where {0} is the version
            var formatTemplate = Resources.MainWindowResources.UpdateAvailableFormat;
            
            // Replace {0} with a regex pattern to match the version
            var pattern = formatTemplate.Replace("{0}", @"(.+?)");
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            var match = regex.Match(UpdateMessage);
            
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            return null;
        }

        protected override void OnDisposing()
        {
            try
            {
                _shutdownCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed, ignore
            }

            _shutdownCts?.Dispose();
        }
    }
}
