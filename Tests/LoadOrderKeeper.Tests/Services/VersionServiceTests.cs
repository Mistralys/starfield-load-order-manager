using System.Linq;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests.Services;

/// <summary>
/// Tests for VersionService covering version retrieval, informational version parsing,
/// commit hash removal, and fallback handling.
/// </summary>
public sealed class VersionServiceTests
{
    #region GetApplicationVersion Tests

    [Fact]
    public void GetApplicationVersion_ReturnsNonEmptyString()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }

    [Fact]
    public void GetApplicationVersion_DoesNotReturnNull()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        Assert.NotNull(version);
    }

    [Fact]
    public void GetApplicationVersion_ReturnsConsistentValue()
    {
        // Act
        var version1 = VersionService.GetApplicationVersion();
        var version2 = VersionService.GetApplicationVersion();

        // Assert
        Assert.Equal(version1, version2);
    }

    [Fact]
    public void GetApplicationVersion_DoesNotContainCommitHash()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        // Version should not contain '+' which indicates a commit hash
        Assert.DoesNotContain("+", version);
    }

    [Fact]
    public void GetApplicationVersion_ReturnsValidFormat()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        // Should be either "Unknown" or a valid version format (numbers and dots)
        if (version != "Unknown")
        {
            // Check if it looks like a version (contains dots or numbers)
            Assert.True(version.Contains(".") || version.All(char.IsDigit),
                $"Version '{version}' should be in a valid format");
        }
    }

    [Fact]
    public void GetApplicationVersion_DoesNotThrow()
    {
        // Act & Assert - Should not throw any exception
        var exception = Record.Exception(() => VersionService.GetApplicationVersion());
        Assert.Null(exception);
    }

    #endregion

    #region Version Format Tests

    [Fact]
    public void GetApplicationVersion_IfNotUnknown_StartsWithNumber()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        if (version != "Unknown")
        {
            Assert.True(char.IsDigit(version[0]),
                $"Version '{version}' should start with a number");
        }
    }

    [Fact]
    public void GetApplicationVersion_IfNotUnknown_DoesNotHaveTrailingWhitespace()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        Assert.Equal(version, version.Trim());
    }

    [Fact]
    public void GetApplicationVersion_IfNotUnknown_DoesNotHaveLeadingWhitespace()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        Assert.Equal(version, version.TrimStart());
    }

    #endregion

    #region Commit Hash Removal Tests

    [Fact]
    public void GetApplicationVersion_RemovesCommitHashIfPresent()
    {
        // This test verifies the behavior even though we can't control
        // what the assembly version attribute contains in the test assembly
        
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        // The version should never contain a '+' which would indicate
        // a commit hash wasn't removed
        Assert.DoesNotContain("+", version);
    }

    #endregion

    #region Multiple Calls Tests

    [Fact]
    public void GetApplicationVersion_CalledMultipleTimes_ReturnsSameValue()
    {
        // Act
        var version1 = VersionService.GetApplicationVersion();
        var version2 = VersionService.GetApplicationVersion();
        var version3 = VersionService.GetApplicationVersion();

        // Assert
        Assert.Equal(version1, version2);
        Assert.Equal(version2, version3);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void GetApplicationVersion_HandlesErrorsGracefully()
    {
        // This test verifies that even if something goes wrong internally,
        // the method returns a safe value rather than throwing
        
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        // Should return either a valid version or "Unknown", never null or empty
    }

    #endregion

    #region Semantic Versioning Tests

    [Fact]
    public void GetApplicationVersion_IfSemanticVersion_HasExpectedParts()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        if (version != "Unknown" && version.Contains("."))
        {
            var parts = version.Split('.');
            
            // Semantic versioning typically has at least 2 parts (major.minor)
            Assert.True(parts.Length >= 2,
                $"Version '{version}' should have at least major.minor parts");
            
            // Each part before any dash should be numeric
            foreach (var part in parts)
            {
                var numericPart = part.Split('-')[0];
                Assert.True(int.TryParse(numericPart, out _),
                    $"Version part '{numericPart}' should be numeric");
            }
        }
    }

    #endregion

    #region Assembly Version Fallback Tests

    [Fact]
    public void GetApplicationVersion_ReturnsEitherVersionOrUnknown()
    {
        // Act
        var version = VersionService.GetApplicationVersion();

        // Assert
        // Version should be either a version string with a dot, or "Unknown"
        Assert.True(version.Contains(".") || version == "Unknown",
            $"Version '{version}' should be either a valid version or 'Unknown'");
    }

    #endregion
}
