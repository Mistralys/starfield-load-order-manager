using System;
using System.IO;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Tests;

internal sealed class TestConfigContext : IDisposable
{
    private readonly string _rootPath;

    public TestConfigContext()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "LoadOrderKeeperTests", Guid.NewGuid().ToString("N"));
        StarfieldAppDataPath = Path.Combine(_rootPath, "AppData");
        StarfieldGamePath = Path.Combine(_rootPath, "Game");
        Directory.CreateDirectory(StarfieldAppDataPath);
        Directory.CreateDirectory(Path.Combine(StarfieldGamePath, "Data"));

        Config = new AppConfigModel
        {
            StarfieldAppDataPath = StarfieldAppDataPath,
            StarfieldGamePath = StarfieldGamePath
        };

        // Create an empty Plugins.txt file to satisfy validation requirements
        File.WriteAllText(PluginsFilePath, string.Empty);
    }

    public AppConfigModel Config { get; }

    public string StarfieldAppDataPath { get; }

    public string StarfieldGamePath { get; }

    public string PluginsFilePath => Config.GetPluginsFilePath();

    public string ReferenceFilePath => Config.GetReferenceFilePath();

    public async Task WritePluginsAsync(params string[] lines)
    {
        await File.WriteAllLinesAsync(PluginsFilePath, lines);
    }

    public async Task WriteReferenceAsync(params string[] lines)
    {
        var referenceDir = Path.GetDirectoryName(ReferenceFilePath);
        if (!string.IsNullOrEmpty(referenceDir) && !Directory.Exists(referenceDir))
        {
            Directory.CreateDirectory(referenceDir);
        }
        await File.WriteAllLinesAsync(ReferenceFilePath, lines);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }
        catch
        {
            // Swallow exceptions in cleanup to avoid hiding test results.
        }
    }
}
