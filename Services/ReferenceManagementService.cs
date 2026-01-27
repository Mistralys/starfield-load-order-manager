using System.Windows;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewModels;
using LoadOrderKeeper.Views;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Handles reference file management operations including creation, updating, and archiving.
    /// Manages the complex workflow for reference versioning with pending changes.
    /// </summary>
    public class ReferenceManagementService
    {
        private readonly Action<string, StatusMessageType> _addStatusMessage;
        private readonly Func<Task> _refreshHistoryWindow;
        private readonly ViewTexts.ReferenceManagementStatusTexts _statusTexts = new();

        public ReferenceManagementService(
            Action<string, StatusMessageType> addStatusMessage,
            Func<Task> refreshHistoryWindow)
        {
            _addStatusMessage = addStatusMessage;
            _refreshHistoryWindow = refreshHistoryWindow;
        }

        /// <summary>
        /// Creates or updates the reference file with versioning support.
        /// Handles comment input, archiving, pending changes, and reference file creation.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        /// <param name="refExists">Whether a reference file already exists.</param>
        /// <param name="owner">The owner window for dialogs.</param>
        /// <returns>True if the operation completed successfully, false if cancelled.</returns>
        public async Task<bool> CreateOrUpdateReferenceAsync(AppConfigModel config, bool refExists, Window? owner)
        {
            if (refExists)
            {
                return await UpdateExistingReferenceAsync(config, owner);
            }
            else
            {
                return await CreateNewReferenceAsync(config);
            }
        }

        /// <summary>
        /// Updates an existing reference file with version history.
        /// </summary>
        private async Task<bool> UpdateExistingReferenceAsync(AppConfigModel config, Window? owner)
        {
            _addStatusMessage(_statusTexts.UpdatingReference, StatusMessageType.Info);

            // Prompt for optional comment
            var commentDialog = new CommentInputDialog
            {
                Owner = owner
            };

            bool? commentResult = commentDialog.ShowDialog();

            // If user cancelled, abort the operation
            if (commentResult != true)
            {
                _addStatusMessage(_statusTexts.UpdateCancelled, StatusMessageType.Info);
                return false;
            }

            string? comment = commentDialog.Comment;

            // Load pending changes from previous update (what changed LAST time)
            var pendingChanges = await ReferenceHistoryService.LoadPendingChangesAsync(config);

            // Calculate current changes (what changed THIS time)
            var (currentAddedMods, currentRemovedMods) = await FileService.CalculateReferenceChangesAsync(config);

            // Archive current reference with PREVIOUS changes (including the previous comment from pending changes)
            // This makes the history entry describe what that version accomplished
            try
            {
                await ReferenceHistoryService.ArchiveCurrentReferenceAsync(
                    config,
                    pendingChanges.AddedMods,
                    pendingChanges.RemovedMods);

                // Refresh history window if open
                await _refreshHistoryWindow();
            }
            catch (Exception ex)
            {
                _addStatusMessage($"Warning: Failed to archive version: {ex.Message}", StatusMessageType.Warning);
                // Continue with update even if archiving fails
            }

            // Store CURRENT changes and comment as pending for the NEXT update
            var newPendingChanges = PendingChangesModel.Create(comment, currentAddedMods, currentRemovedMods);
            try
            {
                await ReferenceHistoryService.SavePendingChangesAsync(config, newPendingChanges);
            }
            catch (Exception ex)
            {
                _addStatusMessage($"Warning: Failed to save pending changes: {ex.Message}", StatusMessageType.Warning);
                // Continue even if saving pending changes fails
            }

            // Update the reference file
            await FileService.CreateReferenceFileAsync(config);
            _addStatusMessage(_statusTexts.ReferenceUpdatedSuccess, StatusMessageType.Success);

            return true;
        }

        /// <summary>
        /// Creates a new reference file without version history.
        /// </summary>
        private async Task<bool> CreateNewReferenceAsync(AppConfigModel config)
        {
            _addStatusMessage(_statusTexts.CreatingReference, StatusMessageType.Info);

            // First reference creation - no changes to track yet
            // Clear any stale pending changes
            await ReferenceHistoryService.ClearPendingChangesAsync(config);

            // Create the reference file
            await FileService.CreateReferenceFileAsync(config);
            _addStatusMessage(_statusTexts.ReferenceCreatedSuccess, StatusMessageType.Success);

            return true;
        }

        /// <summary>
        /// Discards all changes and restores Plugins.txt from the reference file.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        public async Task DiscardChangesAsync(AppConfigModel config)
        {
            _addStatusMessage(_statusTexts.DiscardingChanges, StatusMessageType.Info);

            await FileService.DiscardChangesAsync(config);
            _addStatusMessage(_statusTexts.ChangesDiscarded, StatusMessageType.Success);
        }

        /// <summary>
        /// Handles rollback to a specific version with confirmation.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        /// <param name="version">The version to rollback to.</param>
        /// <param name="parentWindow">The parent window for confirmation dialog.</param>
        /// <param name="onSuccess">Callback to execute on successful rollback.</param>
        public async Task<bool> HandleRollbackAsync(
            AppConfigModel config,
            ReferenceVersionMetadataModel version,
            Window parentWindow,
            Func<Task> onSuccess)
        {
            // Show confirmation dialog
            var result = ConfirmationDialog.Show(
                "Rollback Confirmation",
                $"Are you sure you want to rollback to version {version.VersionNumber}?\n\n" +
                $"Date: {version.FormattedTimestamp}\n" +
                $"Changes: {version.TotalModsChanged}\n" +
                $"Summary: {version.GetChangeSummary()}\n\n" +
                $"The current Plugins.txt will be replaced with the list from version {version.VersionNumber}. " +
                "You will then have the opportunity to review the changes before accepting them.",
                ConfirmationIcon.Question,
                ConfirmationButton.YesNo,
                ConfirmationResult.No,
                parentWindow);

            if (result != ConfirmationResult.Yes)
            {
                return false;
            }

            try
            {
                // Perform rollback
                await ReferenceHistoryService.RollbackToVersionAsync(config, version.VersionNumber);
                _addStatusMessage(string.Format(_statusTexts.RolledBackFormat, version.VersionNumber), StatusMessageType.Success);

                // Close history window
                parentWindow.Close();

                // Execute success callback (typically triggers change detection)
                await onSuccess();

                return true;
            }
            catch (Exception ex)
            {
                ConfirmationDialog.Show(
                    "Rollback Failed",
                    $"Failed to rollback to version {version.VersionNumber}: {ex.Message}",
                    ConfirmationIcon.Error,
                    ConfirmationButton.OK,
                    ConfirmationResult.OK,
                    parentWindow);

                return false;
            }
        }
    }
}
