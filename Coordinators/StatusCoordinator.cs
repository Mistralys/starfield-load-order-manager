using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates status message history and management.
    /// Maintains a rolling history of the last N status messages for display,
    /// while storing all messages internally for debugging and logging purposes.
    /// </summary>
    public sealed partial class StatusCoordinator : CoordinatorBase
    {
        private readonly ViewTexts.LocalizationService _localization = ViewTexts.LocalizationService.Instance;
        private const int MaxHistoryCount = 3;
        private readonly List<StatusMessageModel> _allMessages = new();

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
        /// The message is stored both in the rolling display history and the complete internal log.
        /// </summary>
        /// <param name="message">The message text.</param>
        /// <param name="type">The message type (Info, Success, Warning, Error).</param>
        public void AddStatusMessage(string message, StatusMessageType type = StatusMessageType.Info)
        {
            ThrowIfDisposed();

            var statusEntry = new StatusMessageModel(message, DateTime.Now, type);
            
            // Store in complete internal log (unlimited)
            _allMessages.Add(statusEntry);
            
            // Add to beginning of rolling display collection (most recent first)
            StatusMessageHistory.Insert(0, statusEntry);
            
            // Keep only the last MaxHistoryCount messages in display history
            while (StatusMessageHistory.Count > MaxHistoryCount)
            {
                StatusMessageHistory.RemoveAt(StatusMessageHistory.Count - 1);
            }

            // Update the current status message
            StatusMessage = message;
        }

        /// <summary>
        /// Gets all status messages that have been logged during this session.
        /// Messages are returned in chronological order (oldest first).
        /// </summary>
        /// <returns>Read-only list of all logged status messages.</returns>
        public IReadOnlyList<StatusMessageModel> GetAllMessages()
        {
            ThrowIfDisposed();
            return _allMessages.AsReadOnly();
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
        /// Clears all status history (both display and internal log).
        /// </summary>
        public void ClearHistory()
        {
            ThrowIfDisposed();
            StatusMessageHistory.Clear();
            _allMessages.Clear();
        }

        protected override void OnDisposing()
        {
            StatusMessageHistory.Clear();
            _allMessages.Clear();
        }
    }
}
