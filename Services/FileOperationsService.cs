using System.Diagnostics;
using System.IO;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Handles file and folder opening operations.
    /// Provides a centralized service for launching shell targets.
    /// </summary>
    public class FileOperationsService
    {
        /// <summary>
        /// Opens the Plugins.txt file in the default text editor.
        /// </summary>
        public void OpenPluginsFile(AppConfigModel config)
        {
            var path = config.GetPluginsFilePath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Plugins file not found: {path}");
            }

            LaunchShellTarget(path);
        }

        /// <summary>
        /// Opens the reference file in the default text editor.
        /// </summary>
        public void OpenReferenceFile(AppConfigModel config)
        {
            var path = config.GetReferenceFilePath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Reference file not found: {path}");
            }

            LaunchShellTarget(path);
        }

        /// <summary>
        /// Opens the Starfield AppData folder in File Explorer.
        /// </summary>
        public void OpenAppDataFolder(AppConfigModel config)
        {
            var path = config.StarfieldAppDataPath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("AppData folder is not configured or does not exist.");
            }

            LaunchShellTarget(path);
        }

        /// <summary>
        /// Opens the Starfield game folder in File Explorer.
        /// </summary>
        public void OpenGameFolder(AppConfigModel config)
        {
            var path = config.StarfieldGamePath;
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("Game folder is not configured or does not exist.");
            }

            LaunchShellTarget(path);
        }

        /// <summary>
        /// Opens the application configuration folder in File Explorer.
        /// </summary>
        public void OpenConfigFolder()
        {
            var path = SettingsService.GetConfigFolderPath();
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Configuration folder not found: {path}");
            }

            LaunchShellTarget(path);
        }

        /// <summary>
        /// Launches a file or folder using the default shell application.
        /// </summary>
        private void LaunchShellTarget(string target)
        {
            var psi = new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(psi);
        }
    }
}
