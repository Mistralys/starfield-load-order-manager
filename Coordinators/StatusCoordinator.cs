using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates status message history and management.
    /// Maintains a rolling history of the last N status messages.
    /// </summary>
    public sealed partial class StatusCoordinator : CoordinatorBase
    {
        private readonly ViewTexts.LocalizationService _localization = ViewTexts.LocalizationService.Instance;
        private const int MaxHistoryCount = 3;

        [ObservableProperty]
        private string _statusMessage = "Initializing...";

        [ObservableProperty]
        private ObservableCollection<StatusMessageModel> _statusMessageHistory = new();

        public StatusCoordinator()
        {
            Initialize();
        }

        public override void Initialize()
        {
            AddStatusMessage(_localization.GetString("StatusCoordinator", "InitializingApplication"), StatusMessageType.Info);
        }

        /// <summary>
        /// Adds a new status message to the history.
        /// Automatically manages history size and updates current status.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="type">The message type (Info, Success, Warning, Error).</param>
        public void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
        {
            ThrowIfDisposed();

            var statusEntry = new StatusMessageModel(message, DateTime.Now, type);
            
            // Add to beginning of collection (most recent first)
            StatusMessageHistory.Insert(0, statusEntry);
            
            // Keep only the last MaxHistoryCount messages
            while (StatusMessageHistory.Count > MaxHistoryCount)
            {
                StatusMessageHistory.RemoveAt(StatusMessageHistory.Count - 1);
            }

            // Update the current status message
            StatusMessage = message;
        }

        /// <summary>
        /// Gets a context-aware ready message based on configuration validity.
        /// </summary>
        /// <param name="configValid">Whether the configuration is valid.</param>
        /// <returns>Appropriate ready message.</returns>
        public string GetReadyStatusMessage(bool configValid)
        {
            return configValid
                ? _localization.GetString("StatusCoordinator", "ReadyConfigValid")
                : _localization.GetString("StatusCoordinator", "ConfigRequired");
        }

        /// <summary>
        /// Clears all status history.
        /// </summary>
        public void ClearHistory()
        {
            ThrowIfDisposed();
            StatusMessageHistory.Clear();
        }

        protected override void OnDisposing()
        {
            StatusMessageHistory.Clear();
        }
    }
}
