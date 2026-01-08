using System;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Base interface for all coordinator components.
    /// Coordinators encapsulate specific areas of functionality and communicate via events.
    /// </summary>
    public interface ICoordinator : IDisposable
    {
        /// <summary>
        /// Initializes the coordinator. Called after all dependencies are set up.
        /// </summary>
        void Initialize();
    }
}
