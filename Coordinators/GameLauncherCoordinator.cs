using System;
using System.Diagnostics;
using System.IO;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates game launching functionality including SFSE detection.
    /// Manages game executable paths and provides dynamic button text.
    /// </summary>
    public sealed class GameLauncherCoordinator : CoordinatorBase
    {
        private string? _gamePath;
        private string _playButtonText = string.Empty;
        private bool _hasSfseInstalled;

        /// <summary>
        /// Gets the text to display on the play button (includes SFSE status).
        /// </summary>
        public string PlayButtonText
        {
            get => _playButtonText;
            private set => SetProperty(ref _playButtonText, value);
        }

        /// <summary>
        /// Gets whether SFSE (Starfield Script Extender) is installed.
        /// </summary>
        public bool HasSfseInstalled
        {
            get => _hasSfseInstalled;
            private set
            {
                if (SetProperty(ref _hasSfseInstalled, value))
                {
                    UpdatePlayButtonText();
                }
            }
        }

        public GameLauncherCoordinator()
        {
            Initialize();
        }

        public override void Initialize()
        {
            UpdatePlayButtonText();
        }

        /// <summary>
        /// Updates the coordinator with the current game path.
        /// </summary>
        /// <param name="gamePath">The path to the game installation folder.</param>
        public void UpdateGamePath(string? gamePath)
        {
            ThrowIfDisposed();

            _gamePath = gamePath;
            DetectSfse();
        }

        /// <summary>
        /// Updates the coordinator with configuration.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        public void UpdateConfiguration(AppConfigModel? config)
        {
            ThrowIfDisposed();

            _gamePath = config?.StarfieldGamePath;
            DetectSfse();
        }

        /// <summary>
        /// Launches the game using SFSE if available, otherwise vanilla executable.
        /// </summary>
        /// <returns>True if game was launched successfully, false otherwise.</returns>
        public bool LaunchGame()
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(_gamePath) || !Directory.Exists(_gamePath))
            {
                return false;
            }

            string executablePath = HasSfseInstalled
                ? Path.Combine(_gamePath, "sfse_loader.exe")
                : Path.Combine(_gamePath, "starfield.exe");

            if (!File.Exists(executablePath))
            {
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = _gamePath,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the path to the game executable that would be launched.
        /// </summary>
        /// <returns>The full path to the executable, or null if not found.</returns>
        public string? GetExecutablePath()
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(_gamePath))
            {
                return null;
            }

            string executablePath = HasSfseInstalled
                ? Path.Combine(_gamePath, "sfse_loader.exe")
                : Path.Combine(_gamePath, "starfield.exe");

            return File.Exists(executablePath) ? executablePath : null;
        }

        private void DetectSfse()
        {
            if (string.IsNullOrWhiteSpace(_gamePath))
            {
                HasSfseInstalled = false;
                return;
            }

            string sfsePath = Path.Combine(_gamePath, "sfse_loader.exe");
            HasSfseInstalled = File.Exists(sfsePath);
        }

        private void UpdatePlayButtonText()
        {
            PlayButtonText = HasSfseInstalled
                ? Resources.MainWindowResources.PlayButtonSfse
                : Resources.MainWindowResources.PlayButtonVanilla;
        }

        protected override void OnDisposing()
        {
            _gamePath = null;
        }
    }
}
