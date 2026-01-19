using System;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests.Services;

/// <summary>
/// Tests for DateTimeFormattingService covering friendly formatting,
/// timestamp formatting, ISO formatting, and date boundary cases.
/// </summary>
public sealed class DateTimeFormattingServiceTests
{
    #region FormatFriendly Tests

    [Fact]
    public void FormatFriendly_Today_ReturnsToday()
    {
        // Arrange
        var now = DateTime.Now;
        var today = new DateTime(now.Year, now.Month, now.Day, 14, 30, 0);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(today);

        // Assert
        Assert.StartsWith("Today", result);
        Assert.Contains("14:30", result);
    }

    [Fact]
    public void FormatFriendly_Yesterday_ReturnsYesterday()
    {
        // Arrange
        var now = DateTime.Now;
        var yesterday = now.Date.AddDays(-1).AddHours(10).AddMinutes(45);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(yesterday);

        // Assert
        Assert.StartsWith("Yesterday", result);
        Assert.Contains("10:45", result);
    }

    [Fact]
    public void FormatFriendly_SameYearNotTodayOrYesterday_OmitsYear()
    {
        // Arrange
        var now = DateTime.Now;
        var sameYear = new DateTime(now.Year, 1, 15, 8, 20, 0);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(sameYear);

        // Assert
        // Month name might be culture-specific (e.g., "Jan" in English, "janv." in French)
        // Just verify it contains the day and time, and doesn't contain "Yesterday" or "Today"
        Assert.Contains(" 15 ", result);
        Assert.Contains("08:20", result);
        Assert.DoesNotContain("Yesterday", result);
        Assert.DoesNotContain("Today", result);
        Assert.DoesNotContain(now.Year.ToString(), result); // Year should not be present
    }

    [Fact]
    public void FormatFriendly_DifferentYear_IncludesYear()
    {
        // Arrange
        var lastYear = new DateTime(DateTime.Now.Year - 1, 6, 10, 16, 45, 0);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(lastYear);

        // Assert
        // Month name might be culture-specific
        // Just verify it contains the day, year, and time
        Assert.Contains(" 10", result);
        Assert.Contains((DateTime.Now.Year - 1).ToString(), result);
        Assert.Contains("16:45", result);
    }

    [Fact]
    public void FormatFriendly_Midnight_FormatsCorrectly()
    {
        // Arrange
        var midnight = DateTime.Now.Date;

        // Act
        var result = DateTimeFormattingService.FormatFriendly(midnight);

        // Assert
        Assert.StartsWith("Today", result);
        Assert.Contains("00:00", result);
    }

    [Fact]
    public void FormatFriendly_EndOfDay_FormatsCorrectly()
    {
        // Arrange
        var endOfDay = DateTime.Now.Date.AddHours(23).AddMinutes(59);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(endOfDay);

        // Assert
        Assert.StartsWith("Today", result);
        Assert.Contains("23:59", result);
    }

    [Fact]
    public void FormatFriendly_FirstDayOfMonth_FormatsCorrectly()
    {
        // Arrange
        var firstDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, 12, 0, 0);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(firstDay);

        // Assert
        // Could be today, yesterday, or a specific date depending on when test runs
        Assert.Contains("12:00", result);
    }

    [Fact]
    public void FormatFriendly_LastDayOfMonth_FormatsCorrectly()
    {
        // Arrange
        var now = DateTime.Now;
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var lastDay = new DateTime(now.Year, now.Month, daysInMonth, 18, 30, 0);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(lastDay);

        // Assert
        Assert.Contains("18:30", result);
    }

    #endregion

    #region FormatTimestamp Tests

    [Fact]
    public void FormatTimestamp_FormatsHoursMinutesSeconds()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var result = DateTimeFormattingService.FormatTimestamp(dateTime);

        // Assert
        Assert.Equal("14:30:45", result);
    }

    [Fact]
    public void FormatTimestamp_Midnight_FormatsAsZeros()
    {
        // Arrange
        var midnight = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var result = DateTimeFormattingService.FormatTimestamp(midnight);

        // Assert
        Assert.Equal("00:00:00", result);
    }

    [Fact]
    public void FormatTimestamp_SingleDigits_AddsPadding()
    {
        // Arrange
        var dateTime = new DateTime(2024, 1, 1, 9, 5, 3);

        // Act
        var result = DateTimeFormattingService.FormatTimestamp(dateTime);

        // Assert
        Assert.Equal("09:05:03", result);
    }

    [Fact]
    public void FormatTimestamp_EndOfDay_FormatsCorrectly()
    {
        // Arrange
        var endOfDay = new DateTime(2024, 12, 31, 23, 59, 59);

        // Act
        var result = DateTimeFormattingService.FormatTimestamp(endOfDay);

        // Assert
        Assert.Equal("23:59:59", result);
    }

    [Fact]
    public void FormatTimestamp_DoesNotIncludeDate()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var result = DateTimeFormattingService.FormatTimestamp(dateTime);

        // Assert
        Assert.DoesNotContain("2024", result);
        Assert.DoesNotContain("Jun", result);
        Assert.DoesNotContain("15", result);
    }

    #endregion

    #region FormatIso Tests

    [Fact]
    public void FormatIso_FormatsInIsoFormat()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var result = DateTimeFormattingService.FormatIso(dateTime);

        // Assert
        Assert.Equal("2024-06-15 14:30:45", result);
    }

    [Fact]
    public void FormatIso_Midnight_FormatsWithZeros()
    {
        // Arrange
        var midnight = new DateTime(2024, 1, 1, 0, 0, 0);

        // Act
        var result = DateTimeFormattingService.FormatIso(midnight);

        // Assert
        Assert.Equal("2024-01-01 00:00:00", result);
    }

    [Fact]
    public void FormatIso_SingleDigitMonthAndDay_AddsPadding()
    {
        // Arrange
        var dateTime = new DateTime(2024, 3, 5, 8, 15, 30);

        // Act
        var result = DateTimeFormattingService.FormatIso(dateTime);

        // Assert
        Assert.Equal("2024-03-05 08:15:30", result);
    }

    [Fact]
    public void FormatIso_EndOfYear_FormatsCorrectly()
    {
        // Arrange
        var endOfYear = new DateTime(2024, 12, 31, 23, 59, 59);

        // Act
        var result = DateTimeFormattingService.FormatIso(endOfYear);

        // Assert
        Assert.Equal("2024-12-31 23:59:59", result);
    }

    [Fact]
    public void FormatIso_LeapYearDate_FormatsCorrectly()
    {
        // Arrange
        var leapDay = new DateTime(2024, 2, 29, 12, 0, 0);

        // Act
        var result = DateTimeFormattingService.FormatIso(leapDay);

        // Assert
        Assert.Equal("2024-02-29 12:00:00", result);
    }

    [Fact]
    public void FormatIso_IncludesFourDigitYear()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var result = DateTimeFormattingService.FormatIso(dateTime);

        // Assert
        Assert.StartsWith("2024", result);
    }

    #endregion

    #region Cross-Method Consistency Tests

    [Fact]
    public void AllMethods_SameDateTime_ContainSameTimeComponents()
    {
        // Arrange
        var dateTime = new DateTime(2024, 6, 15, 14, 30, 45);

        // Act
        var friendly = DateTimeFormattingService.FormatFriendly(dateTime);
        var timestamp = DateTimeFormattingService.FormatTimestamp(dateTime);
        var iso = DateTimeFormattingService.FormatIso(dateTime);

        // Assert
        // All should contain the time 14:30
        Assert.Contains("14:30", friendly);
        Assert.Contains("14:30", timestamp);
        Assert.Contains("14:30", iso);
    }

    #endregion

    #region Date Boundary Tests

    [Fact]
    public void FormatFriendly_JustBeforeMidnight_IsToday()
    {
        // Arrange
        var almostMidnight = DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(almostMidnight);

        // Assert
        Assert.StartsWith("Today", result);
    }

    [Fact]
    public void FormatFriendly_JustAfterMidnight_IsToday()
    {
        // Arrange
        var justAfterMidnight = DateTime.Now.Date.AddSeconds(1);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(justAfterMidnight);

        // Assert
        Assert.StartsWith("Today", result);
    }

    [Fact]
    public void FormatFriendly_YesterdayAtMidnight_IsYesterday()
    {
        // Arrange
        var yesterdayMidnight = DateTime.Now.Date.AddDays(-1);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(yesterdayMidnight);

        // Assert
        Assert.StartsWith("Yesterday", result);
    }

    [Fact]
    public void FormatFriendly_YesterdayEndOfDay_IsYesterday()
    {
        // Arrange
        var yesterdayEnd = DateTime.Now.Date.AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(yesterdayEnd);

        // Assert
        Assert.StartsWith("Yesterday", result);
    }

    [Fact]
    public void FormatFriendly_TwoDaysAgo_IsNotYesterday()
    {
        // Arrange
        var twoDaysAgo = DateTime.Now.Date.AddDays(-2).AddHours(12);

        // Act
        var result = DateTimeFormattingService.FormatFriendly(twoDaysAgo);

        // Assert
        Assert.DoesNotContain("Yesterday", result);
        Assert.DoesNotContain("Today", result);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FormatFriendly_MinValue_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            DateTimeFormattingService.FormatFriendly(DateTime.MinValue));
        
        Assert.Null(exception);
    }

    [Fact]
    public void FormatTimestamp_MinValue_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            DateTimeFormattingService.FormatTimestamp(DateTime.MinValue));
        
        Assert.Null(exception);
    }

    [Fact]
    public void FormatIso_MinValue_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => 
            DateTimeFormattingService.FormatIso(DateTime.MinValue));
        
        Assert.Null(exception);
    }

    #endregion
}
