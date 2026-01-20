using System.ComponentModel;
using LoadOrderKeeper.Coordinators;

namespace LoadOrderKeeper.Helpers
{
    /// <summary>
    /// Helper class to bind coordinator property changes to ViewModel property notifications.
    /// Reduces boilerplate code for pass-through property bindings.
    /// </summary>
    public class CoordinatorEventBinder
    {
        private readonly Action<string> _notifyPropertyChanged;

        public CoordinatorEventBinder(Action<string> notifyPropertyChanged)
        {
            _notifyPropertyChanged = notifyPropertyChanged;
        }

        /// <summary>
        /// Binds a single coordinator property to forward PropertyChanged notifications.
        /// </summary>
        public void BindProperty(INotifyPropertyChanged coordinator, string coordinatorPropertyName, string viewModelPropertyName)
        {
            coordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == coordinatorPropertyName)
                {
                    _notifyPropertyChanged(viewModelPropertyName);
                }
            };
        }

        /// <summary>
        /// Binds multiple coordinator properties at once using a mapping dictionary.
        /// </summary>
        public void BindProperties(INotifyPropertyChanged coordinator, Dictionary<string, string> propertyMappings)
        {
            coordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && propertyMappings.TryGetValue(e.PropertyName, out var viewModelPropertyName))
                {
                    _notifyPropertyChanged(viewModelPropertyName);
                }
            };
        }

        /// <summary>
        /// Binds coordinator properties where the ViewModel property name matches the coordinator property name.
        /// </summary>
        public void BindPropertiesDirect(INotifyPropertyChanged coordinator, params string[] propertyNames)
        {
            coordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null && propertyNames.Contains(e.PropertyName))
                {
                    _notifyPropertyChanged(e.PropertyName);
                }
            };
        }

        /// <summary>
        /// Binds a custom action when a specific property changes.
        /// </summary>
        public void BindPropertyWithAction(INotifyPropertyChanged coordinator, string propertyName, Action action)
        {
            coordinator.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == propertyName)
                {
                    action();
                }
            };
        }
    }
}
