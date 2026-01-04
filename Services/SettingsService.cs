using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using Microsoft.Win32;

namespace LoadOrderKeeper.Services
{
    public static class SettingsService
    {
        private static readonly string ConfigPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "LoadOrderKeeper",
                         "config.json");

        public static async Task<AppConfigModel> LoadSettingsAsync()
        {
            if (!File.Exists(ConfigPath))
            {
                return new AppConfigModel();
            }

            try
            {
                var json = await File.ReadAllTextAsync(ConfigPath);
                return JsonSerializer.Deserialize<AppConfigModel>(json) ?? new AppConfigModel();
            }
            catch
            {
                return new AppConfigModel();
            }
        }

        public static async Task SaveSettingsAsync(AppConfigModel config)
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(ConfigPath, json);
        }

        public static string TryGetDefaultSteamPath()
        {
            // Try to detect Steam installation and find Starfield
            var steamPath = TryGetSteamInstallPath();
            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                var starfieldPath = Path.Combine(steamPath, "steamapps", "common", "Starfield");
                if (Directory.Exists(Path.Combine(starfieldPath, "Data")))
                {
                    return starfieldPath;
                }
            }

            // Fallback to default Program Files location
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps", "common", "Starfield");

            return Directory.Exists(Path.Combine(defaultPath, "Data")) ? defaultPath : string.Empty;
        }

        public static string TryGetDefaultAppDataPath()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Starfield");

            return Directory.Exists(appDataPath) ? appDataPath : string.Empty;
        }

        /// <summary>
        /// Attempts to detect the Steam installation path by checking registry keys.
        /// Returns null if Steam is not found.
        /// </summary>
        private static string? TryGetSteamInstallPath()
        {
            // 1. Check current user registry
            var steamPath = TryGetRegistryValue(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                steamPath = NormalizePath(steamPath);
                if (Directory.Exists(steamPath))
                {
                    return steamPath;
                }
            }

            // 2. Check local machine registry (64-bit systems)
            steamPath = TryGetRegistryValue(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                steamPath = NormalizePath(steamPath);
                if (Directory.Exists(steamPath))
                {
                    return steamPath;
                }
            }

            // 3. Check local machine registry (32-bit systems)
            steamPath = TryGetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");
            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                steamPath = NormalizePath(steamPath);
                if (Directory.Exists(steamPath))
                {
                    return steamPath;
                }
            }

            return null;
        }

        /// <summary>
        /// Safely attempts to read a registry value.
        /// Returns null if the key or value doesn't exist.
        /// </summary>
        private static string? TryGetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName)
        {
            try
            {
                using var key = rootKey.OpenSubKey(subKeyPath);
                return key?.GetValue(valueName)?.ToString();
            }
            catch
            {
                // Registry access denied or other error
                return null;
            }
        }

        /// <summary>
        /// Normalizes a path by converting forward slashes to backslashes.
        /// Steam registry values often use forward slashes which look inconsistent on Windows.
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            return path.Replace('/', '\\');
        }
    }
}
