using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.Coordinators
{
    /// <summary>
    /// Abstract base class for coordinators providing common functionality.
    /// </summary>
    public abstract class CoordinatorBase : ObservableObject, ICoordinator
    {
        private bool _disposed;

        /// <summary>
        /// Initializes the coordinator. Override to add custom initialization logic.
        /// </summary>
        public virtual void Initialize()
        {
            // Base implementation does nothing, override in derived classes if needed
        }

        /// <summary>
        /// Disposes the coordinator. Override to add custom cleanup logic.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the coordinator. Override to add custom cleanup logic.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Derived classes should override and add cleanup logic here
                OnDisposing();
            }

            _disposed = true;
        }

        /// <summary>
        /// Called when the coordinator is being disposed. Override to add cleanup logic.
        /// </summary>
        protected virtual void OnDisposing()
        {
            // Base implementation does nothing, override in derived classes
        }

        /// <summary>
        /// Throws if the coordinator has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
