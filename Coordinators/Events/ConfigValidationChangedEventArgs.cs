using System;

namespace LoadOrderKeeper.Coordinators.Events
{
    /// <summary>
    /// Event arguments for configuration validation state changes.
    /// </summary>
    public sealed class ConfigValidationChangedEventArgs : EventArgs
    {
        public ConfigValidationChangedEventArgs(bool wasValid, bool isValid)
        {
            WasValid = wasValid;
            IsValid = isValid;
        }

        /// <summary>
        /// Whether the configuration was valid before the change.
        /// </summary>
        public bool WasValid { get; }

        /// <summary>
        /// Whether the configuration is currently valid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Whether the validation state changed.
        /// </summary>
        public bool StateChanged => WasValid != IsValid;
    }
}
