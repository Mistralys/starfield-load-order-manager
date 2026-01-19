using System;
using System.Linq;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for StatusCoordinator covering status message management, history handling,
/// and ready message generation.
/// </summary>
public sealed class StatusCoordinatorTests : IDisposable
{
    private readonly StatusCoordinator _coordinator;

    public StatusCoordinatorTests()
    {
        _coordinator = new StatusCoordinator();
    }

    public void Dispose()
    {
        _coordinator?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_InitializesWithDefaultMessage()
    {
        // Assert
        Assert.Equal("Initializing application...", _coordinator.StatusMessage);
    }

    [Fact]
    public void Constructor_AddsInitialMessageToHistory()
    {
        // Assert
        Assert.Single(_coordinator.StatusMessageHistory);
        Assert.Equal("Initializing application...", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal(StatusMessageType.Info, _coordinator.StatusMessageHistory[0].Type);
    }

    [Fact]
    public void Initialize_SetsInitializingMessage()
    {
        // Arrange
        var coordinator = new StatusCoordinator();
        coordinator.ClearHistory();

        // Act
        coordinator.Initialize();

        // Assert
        Assert.Equal("Initializing application...", coordinator.StatusMessage);
        Assert.Single(coordinator.StatusMessageHistory);
    }

    #endregion

    #region AddStatusMessage Tests

    [Fact]
    public void AddStatusMessage_UpdatesStatusMessage()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act
        _coordinator.AddStatusMessage("Test message", StatusMessageType.Info);

        // Assert
        Assert.Equal("Test message", _coordinator.StatusMessage);
    }

    [Fact]
    public void AddStatusMessage_AddsToHistory()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act
        _coordinator.AddStatusMessage("Test message", StatusMessageType.Info);

        // Assert
        Assert.Single(_coordinator.StatusMessageHistory);
        Assert.Equal("Test message", _coordinator.StatusMessageHistory[0].Message);
    }

    [Fact]
    public void AddStatusMessage_InsertsAtBeginning()
    {
        // Arrange
        _coordinator.ClearHistory();
        _coordinator.AddStatusMessage("First", StatusMessageType.Info);

        // Act
        _coordinator.AddStatusMessage("Second", StatusMessageType.Info);

        // Assert
        Assert.Equal(2, _coordinator.StatusMessageHistory.Count);
        Assert.Equal("Second", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal("First", _coordinator.StatusMessageHistory[1].Message);
    }

    [Fact]
    public void AddStatusMessage_RespectsMaxHistoryCount()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act - Add more than max history count (3)
        _coordinator.AddStatusMessage("Message 1", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 2", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 3", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 4", StatusMessageType.Info);

        // Assert
        Assert.Equal(3, _coordinator.StatusMessageHistory.Count);
        Assert.Equal("Message 4", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal("Message 3", _coordinator.StatusMessageHistory[1].Message);
        Assert.Equal("Message 2", _coordinator.StatusMessageHistory[2].Message);
        // Message 1 should be removed
    }

    [Fact]
    public void AddStatusMessage_RemovesOldestEntry()
    {
        // Arrange
        _coordinator.ClearHistory();
        _coordinator.AddStatusMessage("Old 1", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Old 2", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Old 3", StatusMessageType.Info);

        // Act - Add fourth message, should remove "Old 1"
        _coordinator.AddStatusMessage("New", StatusMessageType.Info);

        // Assert
        Assert.DoesNotContain(_coordinator.StatusMessageHistory, m => m.Message == "Old 1");
        Assert.Contains(_coordinator.StatusMessageHistory, m => m.Message == "New");
        Assert.Contains(_coordinator.StatusMessageHistory, m => m.Message == "Old 3");
        Assert.Contains(_coordinator.StatusMessageHistory, m => m.Message == "Old 2");
    }

    [Fact]
    public void AddStatusMessage_DefaultTypeIsInfo()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act
        _coordinator.AddStatusMessage("Test");

        // Assert
        Assert.Equal(StatusMessageType.Info, _coordinator.StatusMessageHistory[0].Type);
    }

    [Fact]
    public void AddStatusMessage_PreservesMessageType()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act & Assert - Info
        _coordinator.AddStatusMessage("Info message", StatusMessageType.Info);
        Assert.Equal(StatusMessageType.Info, _coordinator.StatusMessageHistory[0].Type);

        // Act & Assert - Success
        _coordinator.AddStatusMessage("Success message", StatusMessageType.Success);
        Assert.Equal(StatusMessageType.Success, _coordinator.StatusMessageHistory[0].Type);

        // Act & Assert - Warning
        _coordinator.AddStatusMessage("Warning message", StatusMessageType.Warning);
        Assert.Equal(StatusMessageType.Warning, _coordinator.StatusMessageHistory[0].Type);

        // Act & Assert - Error
        _coordinator.AddStatusMessage("Error message", StatusMessageType.Error);
        Assert.Equal(StatusMessageType.Error, _coordinator.StatusMessageHistory[0].Type);
    }

    [Fact]
    public void AddStatusMessage_CreatesTimestamp()
    {
        // Arrange
        _coordinator.ClearHistory();
        var beforeAdd = DateTime.Now;

        // Act
        _coordinator.AddStatusMessage("Test", StatusMessageType.Info);
        var afterAdd = DateTime.Now;

        // Assert
        var timestamp = _coordinator.StatusMessageHistory[0].Timestamp;
        Assert.True(timestamp >= beforeAdd && timestamp <= afterAdd,
            $"Timestamp {timestamp} should be between {beforeAdd} and {afterAdd}");
    }

    [Fact]
    public void AddStatusMessage_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            _coordinator.AddStatusMessage("Test", StatusMessageType.Info));
    }

    #endregion

    #region GetReadyStatusMessage Tests

    [Fact]
    public void GetReadyStatusMessage_ValidConfig_ReturnsReadyMessage()
    {
        // Act
        var message = _coordinator.GetReadyStatusMessage(configValid: true);

        // Assert
        Assert.Equal("Ready. Configuration is valid.", message);
    }

    [Fact]
    public void GetReadyStatusMessage_InvalidConfig_ReturnsConfigRequiredMessage()
    {
        // Act
        var message = _coordinator.GetReadyStatusMessage(configValid: false);

        // Assert
        Assert.Equal("Configuration is required. Please set paths in the Settings window.", message);
    }

    [Fact]
    public void GetReadyStatusMessage_DoesNotModifyHistory()
    {
        // Arrange
        _coordinator.ClearHistory();
        _coordinator.AddStatusMessage("Test", StatusMessageType.Info);
        var historyCountBefore = _coordinator.StatusMessageHistory.Count;

        // Act
        _ = _coordinator.GetReadyStatusMessage(true);
        _ = _coordinator.GetReadyStatusMessage(false);

        // Assert
        Assert.Equal(historyCountBefore, _coordinator.StatusMessageHistory.Count);
    }

    [Fact]
    public void GetReadyStatusMessage_DoesNotModifyStatusMessage()
    {
        // Arrange
        _coordinator.ClearHistory();
        _coordinator.AddStatusMessage("Current Status", StatusMessageType.Info);

        // Act
        _ = _coordinator.GetReadyStatusMessage(true);

        // Assert
        Assert.Equal("Current Status", _coordinator.StatusMessage);
    }

    #endregion

    #region ClearHistory Tests

    [Fact]
    public void ClearHistory_RemovesAllMessages()
    {
        // Arrange
        _coordinator.AddStatusMessage("Message 1", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 2", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 3", StatusMessageType.Info);

        // Act
        _coordinator.ClearHistory();

        // Assert
        Assert.Empty(_coordinator.StatusMessageHistory);
    }

    [Fact]
    public void ClearHistory_DoesNotChangeCurrentStatusMessage()
    {
        // Arrange
        _coordinator.AddStatusMessage("Last Message", StatusMessageType.Info);
        var currentMessage = _coordinator.StatusMessage;

        // Act
        _coordinator.ClearHistory();

        // Assert
        Assert.Equal(currentMessage, _coordinator.StatusMessage);
    }

    [Fact]
    public void ClearHistory_CanBeCalledMultipleTimes()
    {
        // Arrange
        _coordinator.AddStatusMessage("Test", StatusMessageType.Info);

        // Act & Assert - Should not throw
        _coordinator.ClearHistory();
        _coordinator.ClearHistory();
        _coordinator.ClearHistory();

        Assert.Empty(_coordinator.StatusMessageHistory);
    }

    [Fact]
    public void ClearHistory_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.ClearHistory());
    }

    #endregion

    #region Message History Management Tests

    [Fact]
    public void StatusMessageHistory_IsObservableCollection()
    {
        // Assert
        Assert.IsAssignableFrom<System.Collections.ObjectModel.ObservableCollection<StatusMessageModel>>(
            _coordinator.StatusMessageHistory);
    }

    [Fact]
    public void StatusMessageHistory_MaintainsInsertionOrder()
    {
        // Arrange
        _coordinator.ClearHistory();
        var messages = new[] { "First", "Second", "Third" };

        // Act
        foreach (var message in messages)
        {
            _coordinator.AddStatusMessage(message, StatusMessageType.Info);
        }

        // Assert
        Assert.Equal("Third", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal("Second", _coordinator.StatusMessageHistory[1].Message);
        Assert.Equal("First", _coordinator.StatusMessageHistory[2].Message);
    }

    [Fact]
    public void StatusMessageHistory_NeverExceedsMaxCount()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act - Add 10 messages
        for (int i = 0; i < 10; i++)
        {
            _coordinator.AddStatusMessage($"Message {i}", StatusMessageType.Info);
        }

        // Assert
        Assert.True(_coordinator.StatusMessageHistory.Count <= 3);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_ClearsHistory()
    {
        // Arrange
        _coordinator.AddStatusMessage("Test 1", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Test 2", StatusMessageType.Info);

        // Act
        _coordinator.Dispose();

        // Assert
        Assert.Empty(_coordinator.StatusMessageHistory);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var coordinator = new StatusCoordinator();

        // Act & Assert - Should not throw
        coordinator.Dispose();
        coordinator.Dispose();
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public void StatusMessage_RaisesPropertyChanged()
    {
        // Arrange
        _coordinator.ClearHistory();
        bool propertyChangedRaised = false;
        _coordinator.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(StatusCoordinator.StatusMessage))
            {
                propertyChangedRaised = true;
            }
        };

        // Act
        _coordinator.AddStatusMessage("New message", StatusMessageType.Info);

        // Assert
        Assert.True(propertyChangedRaised);
    }

    #endregion
}
