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
        public bool OpenPluginsFile(AppConfigModel config)
        {
            var path = config.GetPluginsFilePath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Plugins file not found: {path}");
            }

            return LaunchFileWithEditor(path, config.CustomEditorPath);
        }

        /// <summary>
        /// Opens the reference file in the default text editor.
        /// </summary>
        public bool OpenReferenceFile(AppConfigModel config)
        {
            var path = config.GetReferenceFilePath();
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Reference file not found: {path}");
            }

            return LaunchFileWithEditor(path, config.CustomEditorPath);
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
        /// Opens a file with custom editor if specified, otherwise uses system default.
        /// </summary>
        /// <param name="filePath">Path to file to open</param>
        /// <param name="customEditorPath">Optional custom editor executable path</param>
        /// <returns>True if launched successfully, false if fallback to default was needed</returns>
        private bool LaunchFileWithEditor(string filePath, string? customEditorPath)
        {
            // If custom editor specified, try to use it
            if (!string.IsNullOrWhiteSpace(customEditorPath))
            {
                if (File.Exists(customEditorPath))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = customEditorPath,
                            Arguments = $"\"{filePath}\"",
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                        return true;
                    }
                    catch
                    {
                        // Custom editor failed, fall through to default behavior
                    }
                }
                
                // Custom editor not found or failed, use default editor
                LaunchShellTarget(filePath);
                return false;
            }

            // Use system default editor
            LaunchShellTarget(filePath);
            return true;
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
