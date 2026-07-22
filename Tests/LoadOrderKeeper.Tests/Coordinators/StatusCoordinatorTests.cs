using System;
using System.Linq;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Tests.Fixtures;
using Xunit;

namespace LoadOrderKeeper.Tests.Coordinators;

/// <summary>
/// Tests for StatusCoordinator covering status message management, history handling,
/// and ready message generation.
/// </summary>
[Collection(LocaleSequentialCollection.Name)]
public sealed class StatusCoordinatorTests : IClassFixture<EnglishLocaleFixture>, IDisposable
{
    private readonly StatusCoordinator _coordinator;

    public StatusCoordinatorTests(EnglishLocaleFixture localeFixture)
    {
        _ = localeFixture; // Ensures en-US culture is active for the lifetime of this test class
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
    public void AddStatusMessage_AllowsUnlimitedHistory()
    {
        // Arrange
        _coordinator.ClearHistory();

        // Act - Add many messages
        for (int i = 1; i <= 10; i++)
        {
            _coordinator.AddStatusMessage($"Message {i}", StatusMessageType.Info);
        }

        // Assert - All messages should be preserved
        Assert.Equal(10, _coordinator.StatusMessageHistory.Count);
        Assert.Equal("Message 10", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal("Message 9", _coordinator.StatusMessageHistory[1].Message);
        Assert.Equal("Message 1", _coordinator.StatusMessageHistory[9].Message);
    }

    [Fact]
    public void AddStatusMessage_MaintainsAllMessagesInDisplayHistory()
    {
        // Arrange
        _coordinator.ClearHistory();
        var messages = new[] { "First", "Second", "Third", "Fourth", "Fifth" };

        // Act
        foreach (var message in messages)
        {
            _coordinator.AddStatusMessage(message, StatusMessageType.Info);
        }

        // Assert - All messages should be in display history
        Assert.Equal(5, _coordinator.StatusMessageHistory.Count);
        Assert.Equal("Fifth", _coordinator.StatusMessageHistory[0].Message);
        Assert.Equal("First", _coordinator.StatusMessageHistory[4].Message);
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

    #region GetAllMessages Tests

    [Fact]
    public void GetAllMessages_ReturnsAllLoggedMessages()
    {
        // Arrange - Add 5 messages (more than MaxHistoryCount of 3)
        _coordinator.AddStatusMessage("Message 1", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Message 2", StatusMessageType.Success);
        _coordinator.AddStatusMessage("Message 3", StatusMessageType.Warning);
        _coordinator.AddStatusMessage("Message 4", StatusMessageType.Error);
        _coordinator.AddStatusMessage("Message 5", StatusMessageType.Info);

        // Act
        var allMessages = _coordinator.GetAllMessages();

        // Assert - Should contain all 6 messages (5 added + 1 initial from Initialize)
        Assert.Equal(6, allMessages.Count);
        Assert.Equal("Message 1", allMessages[1].Message);
        Assert.Equal("Message 2", allMessages[2].Message);
        Assert.Equal("Message 3", allMessages[3].Message);
        Assert.Equal("Message 4", allMessages[4].Message);
        Assert.Equal("Message 5", allMessages[5].Message);
    }

    [Fact]
    public void GetAllMessages_ReturnsMessagesInChronologicalOrder()
    {
        // Arrange
        _coordinator.AddStatusMessage("First", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Second", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Third", StatusMessageType.Info);

        // Act
        var allMessages = _coordinator.GetAllMessages();

        // Assert - Messages should be in order they were added (oldest first)
        Assert.Equal("First", allMessages[1].Message);
        Assert.Equal("Second", allMessages[2].Message);
        Assert.Equal("Third", allMessages[3].Message);
    }

    [Fact]
    public void GetAllMessages_DoesNotAffectStatusMessageHistory()
    {
        // Arrange - Add 5 messages
        _coordinator.AddStatusMessage("Message 1");
        _coordinator.AddStatusMessage("Message 2");
        _coordinator.AddStatusMessage("Message 3");
        _coordinator.AddStatusMessage("Message 4");
        _coordinator.AddStatusMessage("Message 5");

        // Act
        var allMessages = _coordinator.GetAllMessages();

        // Assert - StatusMessageHistory should contain all messages (now unlimited)
        Assert.Equal(6, _coordinator.StatusMessageHistory.Count); // 5 added + 1 initial
        Assert.Equal("Message 5", _coordinator.StatusMessageHistory[0].Message); // Most recent first
        Assert.Equal("Message 4", _coordinator.StatusMessageHistory[1].Message);
        Assert.Equal("Message 3", _coordinator.StatusMessageHistory[2].Message);
        
        // GetAllMessages should also have all 6 (5 added + 1 initial) in chronological order
        Assert.Equal(6, allMessages.Count);
        Assert.Equal("Initializing application...", allMessages[0].Message);
        Assert.Equal("Message 1", allMessages[1].Message);
    }

    [Fact]
    public void GetAllMessages_PreservesMessageTypes()
    {
        // Arrange
        _coordinator.AddStatusMessage("Info message", StatusMessageType.Info);
        _coordinator.AddStatusMessage("Success message", StatusMessageType.Success);
        _coordinator.AddStatusMessage("Warning message", StatusMessageType.Warning);
        _coordinator.AddStatusMessage("Error message", StatusMessageType.Error);

        // Act
        var allMessages = _coordinator.GetAllMessages();

        // Assert
        Assert.Equal(StatusMessageType.Info, allMessages[1].Type);
        Assert.Equal(StatusMessageType.Success, allMessages[2].Type);
        Assert.Equal(StatusMessageType.Warning, allMessages[3].Type);
        Assert.Equal(StatusMessageType.Error, allMessages[4].Type);
    }

    [Fact]
    public void GetAllMessages_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        _coordinator.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _coordinator.GetAllMessages());
    }

    [Fact]
    public void ClearHistory_ClearsBothDisplayAndInternalLog()
    {
        // Arrange - Add multiple messages
        _coordinator.AddStatusMessage("Message 1");
        _coordinator.AddStatusMessage("Message 2");
        _coordinator.AddStatusMessage("Message 3");
        _coordinator.AddStatusMessage("Message 4");

        // Act
        _coordinator.ClearHistory();

        // Assert
        Assert.Empty(_coordinator.StatusMessageHistory);
        Assert.Empty(_coordinator.GetAllMessages());
    }

    #endregion
}
