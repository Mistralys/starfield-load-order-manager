using System;
using System.Threading.Tasks;
using LoadOrderKeeper.Coordinators.Events;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Coordinates profile-related state and operations.
    /// Manages the active profile and provides profile switching functionality.
    /// </summary>
    public sealed class ProfileCoordinator : CoordinatorBase
    {
        private AppConfigModel? _config;
        private ProfileModel _activeProfile = ProfileModel.CreateDefault();
        private string _activeProfileLabel = "Default";

        /// <summary>
        /// Gets the currently active profile.
        /// </summary>
        public ProfileModel ActiveProfile
        {
            get => _activeProfile;
            private set => SetProperty(ref _activeProfile, value);
        }

        /// <summary>
        /// Gets the label of the currently active profile.
        /// </summary>
        public string ActiveProfileLabel
        {
            get => _activeProfileLabel;
            private set => SetProperty(ref _activeProfileLabel, value);
        }

        /// <summary>
        /// Raised when the active profile changes.
        /// </summary>
        public event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

        public ProfileCoordinator()
        {
            Initialize();
        }

        public override void Initialize()
        {
            // Default profile is already set in field initializers
        }

        /// <summary>
        /// Updates the coordinator with the current configuration.
        /// Should be called when configuration changes.
        /// </summary>
        /// <param name="config">The application configuration.</param>
        public void UpdateConfiguration(AppConfigModel config)
        {
            ThrowIfDisposed();
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Loads and sets the active profile from the current configuration.
        /// </summary>
        public async Task RefreshActiveProfileAsync()
        {
            ThrowIfDisposed();

            if (_config == null || !_config.IsValid())
            {
                SetDefaultProfile();
                return;
            }

            try
            {
                var oldProfile = ActiveProfile;
                var profile = await ProfileService.GetActiveProfileAsync(_config);
                
                ActiveProfile = profile;
                ActiveProfileLabel = profile.Label;

                // Raise event if profile actually changed
                if (oldProfile.Id != profile.Id)
                {
                    ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(oldProfile, profile));
                }
            }
            catch
            {
                // Fallback to default on error
                SetDefaultProfile();
            }
        }

        /// <summary>
        /// Switches to a different profile.
        /// </summary>
        /// <param name="targetProfileId">The ID of the profile to switch to.</param>
        /// <returns>True if the switch was successful, false otherwise.</returns>
        public async Task<bool> SwitchProfileAsync(string targetProfileId)
        {
            ThrowIfDisposed();

            if (_config == null || !_config.IsValid())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(targetProfileId))
            {
                throw new ArgumentException("Profile ID cannot be null or empty.", nameof(targetProfileId));
            }

            try
            {
                var oldProfile = ActiveProfile;

                // Perform the switch
                await ProfileService.SwitchProfileAsync(_config, targetProfileId);

                // Reload active profile
                await RefreshActiveProfileAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a given profile is currently active.
        /// </summary>
        public bool IsActiveProfile(string profileId)
        {
            ThrowIfDisposed();
            return string.Equals(ActiveProfile.Id, profileId, StringComparison.OrdinalIgnoreCase);
        }

        private void SetDefaultProfile()
        {
            var oldProfile = ActiveProfile;
            var defaultProfile = ProfileModel.CreateDefault();
            
            ActiveProfile = defaultProfile;
            ActiveProfileLabel = defaultProfile.Label;

            if (oldProfile.Id != defaultProfile.Id)
            {
                ProfileChanged?.Invoke(this, new ProfileChangedEventArgs(oldProfile, defaultProfile));
            }
        }

        protected override void OnDisposing()
        {
            _config = null;
        }
    }
}
