using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

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
            var steamPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam", "steamapps", "common", "Starfield");

            return Directory.Exists(Path.Combine(steamPath, "Data")) ? steamPath : string.Empty;
        }

        public static string TryGetDefaultAppDataPath()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Starfield");

            return Directory.Exists(appDataPath) ? appDataPath : string.Empty;
        }
    }
}
