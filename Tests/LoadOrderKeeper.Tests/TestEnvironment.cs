using System;
using System.IO;
using System.Runtime.CompilerServices;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Tests;

/// <summary>
/// Assembly-wide test setup. Redirects <see cref="SettingsService"/> to a throw-away temp folder
/// so no test ever reads or overwrites the developer's real
/// <c>%LOCALAPPDATA%\LoadOrderKeeper\config.json</c> (or its error log).
/// </summary>
internal static class TestEnvironment
{
    /// <summary>The config folder used by every test in this run.</summary>
    internal static string ConfigFolder { get; private set; } = string.Empty;

    [ModuleInitializer]
    internal static void Initialize()
    {
        ConfigFolder = Path.Combine(
            Path.GetTempPath(), "LoadOrderKeeperTests", "Config-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ConfigFolder);
        SettingsService.ConfigFolderOverride = ConfigFolder;

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(ConfigFolder, recursive: true);
            }
            catch (Exception)
            {
                // Best-effort cleanup of a temp folder; a leftover directory is harmless.
            }
        };
    }
}
