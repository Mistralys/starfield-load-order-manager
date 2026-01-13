using System;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Coordinators.Events
{
    /// <summary>
    /// Event arguments for profile change events.
    /// </summary>
    public sealed class ProfileChangedEventArgs : EventArgs
    {
        public ProfileChangedEventArgs(ProfileModel oldProfile, ProfileModel newProfile)
        {
            OldProfile = oldProfile ?? throw new ArgumentNullException(nameof(oldProfile));
            NewProfile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
        }

        /// <summary>
        /// Gets the profile before the change.
        /// </summary>
        public ProfileModel OldProfile { get; }

        /// <summary>
        /// Gets the profile after the change.
        /// </summary>
        public ProfileModel NewProfile { get; }
    }
}
