using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using LoadOrderKeeper.Services;
using Xunit;

namespace LoadOrderKeeper.Tests;

public class SettingsServiceTests
{
    private static readonly MethodInfo SteamLibraryLookupMethod = typeof(SettingsService).GetMethod(
        "TryFindStarfieldInSteamLibraries",
        BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException(
            "Could not locate SettingsService.TryFindStarfieldInSteamLibraries.");

    [Fact]
    public void TryGetDefaultSteamPath_FindsStarfieldInSteamLibrary_WhenLibraryFoldersVdfExists()
    {
        // Arrange: Create a temporary directory structure mimicking Steam
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            // Create main Steam installation directory
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            // Create a Steam library folder with Starfield - using C:\Steam location from VDF
            var cSteamLibraryPath = Path.Combine(tempRoot, "CSteamLibrary");
            var cSteamStarfieldDataPath = Path.Combine(cSteamLibraryPath, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(cSteamStarfieldDataPath);

            // Create another library folder (without Starfield)
            var libraryPath = Path.Combine(tempRoot, "SteamLibrary");
            var libraryAppsPath = Path.Combine(libraryPath, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(libraryAppsPath);

            // Create the libraryfolders.vdf file with the example content
            // Starfield AppID 1716740 is in library "0" at C:\Steam, but we'll map it to our temp directory
            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""" + cSteamLibraryPath.Replace("\\", "\\\\") + @"""
		""label""		""""
		""contentid""		""8941062469189073444""
		""totalsize""		""0""
		""update_clean_bytes_tally""		""2149385173""
		""time_last_update_verified""		""1767373377""
		""apps""
		{
			""228980""		""1238805183""
			""1716740""		""145641509170""
		}
	}
	""1""
	{
		""path""		""" + libraryPath.Replace("\\", "\\\\") + @"""
		""label""		""""
		""contentid""		""5016554487110174516""
		""totalsize""		""8001545039872""
		""update_clean_bytes_tally""		""2147867792""
		""time_last_update_verified""		""1765476453""
		""apps""
		{
			""200170""		""1713583360""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should find Starfield in library 0
            Assert.NotNull(result);
            Assert.Contains("steamapps", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("common", result, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Starfield", result, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_FindsFirstMatch_WhenMultipleLibrariesHaveStarfield()
    {
        // Arrange: Create a temporary directory structure with Starfield in multiple libraries
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            // Create main Steam installation directory
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            // Create first library with Starfield
            var library1Path = Path.Combine(tempRoot, "Library1");
            var library1StarfieldPath = Path.Combine(library1Path, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(library1StarfieldPath);

            // Create second library with Starfield
            var library2Path = Path.Combine(tempRoot, "Library2");
            var library2StarfieldPath = Path.Combine(library2Path, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(library2StarfieldPath);

            // Create VDF with Starfield in both libraries (0 and 1)
            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""" + library1Path.Replace("\\", "\\\\") + @"""
		""apps""
		{
			""1716740""		""145641509170""
		}
	}
	""1""
	{
		""path""		""" + library2Path.Replace("\\", "\\\\") + @"""
		""apps""
		{
			""1716740""		""145641509170""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should return the first library (library1)
            Assert.NotNull(result);
            Assert.Contains("Library1", result);
            Assert.DoesNotContain("Library2", result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_ReturnsNull_WhenStarfieldNotInLibraries()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            // Create VDF without Starfield AppID
            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""C:\\Steam""
		""apps""
		{
			""228980""		""1238805183""
			""244850""		""44717224268""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_ReturnsNull_WhenVdfFileDoesNotExist()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            Directory.CreateDirectory(mainSteamPath);
            // Note: No steamapps folder or VDF file created

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_ReturnsNull_WhenDataFolderMissing()
    {
        // Arrange: Starfield AppID present but Data folder doesn't exist
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            var libraryPath = Path.Combine(tempRoot, "Library");
            var libraryStarfieldPath = Path.Combine(libraryPath, "steamapps", "common", "Starfield");
            Directory.CreateDirectory(libraryStarfieldPath);
            // Note: Data folder is NOT created

            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""" + libraryPath.Replace("\\", "\\\\") + @"""
		""apps""
		{
			""1716740""		""145641509170""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should return null because Data folder doesn't exist
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_ReturnsNull_WhenVdfIsCorrupted()
    {
        // Arrange
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            // Create corrupted VDF content
            var vdfContent = @"This is not valid VDF content { broken syntax ][][";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should fail silently and return null
            Assert.Null(result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_NormalizesPathSlashes()
    {
        // Arrange: Create VDF with forward slashes (common in Steam files)
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            var libraryPath = Path.Combine(tempRoot, "Library");
            var libraryStarfieldDataPath = Path.Combine(libraryPath, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(libraryStarfieldDataPath);

            // Use forward slashes in path (as Steam often does)
            var forwardSlashPath = libraryPath.Replace("\\", "/");
            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""" + forwardSlashPath + @"""
		""apps""
		{
			""1716740""		""145641509170""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should normalize path with backslashes
            Assert.NotNull(result);
            Assert.DoesNotContain("/", result); // Should have backslashes, not forward slashes
            Assert.Contains("\\", result); // Should contain backslashes
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_SkipsLibrariesWithoutAppsProperty()
    {
        // Arrange: Create VDF where one library doesn't have an apps section
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            var library2Path = Path.Combine(tempRoot, "Library2");
            var library2StarfieldDataPath = Path.Combine(library2Path, "steamapps", "common", "Starfield", "Data");
            Directory.CreateDirectory(library2StarfieldDataPath);

            var vdfContent = @"""libraryfolders""
{
	""0""
	{
		""path""		""C:\\EmptyLibrary""
	}
	""1""
	{
		""path""		""" + library2Path.Replace("\\", "\\\\") + @"""
		""apps""
		{
			""1716740""		""145641509170""
		}
	}
}";

            var vdfPath = Path.Combine(steamAppsPath, "libraryfolders.vdf");
            File.WriteAllText(vdfPath, vdfContent);

            // Act
            var result = TryFindStarfieldInSteamLibraries(mainSteamPath);

            // Assert: Should skip library 0 and find in library 1
            Assert.NotNull(result);
            Assert.Contains("Library2", result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void TryFindStarfieldInSteamLibraries_SkipsLibrariesWithNonObjectAppsProperty()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "SteamLibraryTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var mainSteamPath = Path.Combine(tempRoot, "Steam");
            var steamAppsPath = Path.Combine(mainSteamPath, "steamapps");
            Directory.CreateDirectory(steamAppsPath);

            var libraryPath = Path.Combine(tempRoot, "Library");
            Directory.CreateDirectory(Path.Combine(libraryPath, "steamapps", "common", "Starfield", "Data"));

            var vdfContent = @"""libraryfolders""
{
	""0"" { ""path"" ""C:\\NotUsed"" ""apps"" ""not-an-object"" }
	""1"" { ""path"" """ + libraryPath.Replace("\\", "\\\\") + @""" ""apps"" { ""1716740"" ""1"" } }
}";
            File.WriteAllText(Path.Combine(steamAppsPath, "libraryfolders.vdf"), vdfContent);

            var method = typeof(SettingsService).GetMethod("TryFindStarfieldInSteamLibraries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);

            var result = method.Invoke(null, new object[] { mainSteamPath }) as string;

            Assert.NotNull(result);
            Assert.Contains("Library", result);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static string? TryFindStarfieldInSteamLibraries(string steamInstallPath)
    {
        return SteamLibraryLookupMethod.Invoke(null, new object[] { steamInstallPath }) as string;
    }
}
