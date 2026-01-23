using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Service for managing application localization, culture selection, and pluralization.
    /// </summary>
    public class LocalizationService
    {
        private CultureInfo _currentCulture;

        public LocalizationService()
        {
            // Default to system culture
            _currentCulture = CultureInfo.CurrentUICulture;
        }

        /// <summary>
        /// Gets the current culture being used by the application.
        /// </summary>
        public CultureInfo CurrentCulture => _currentCulture;

        /// <summary>
        /// Gets the FlowDirection for the current culture (for future RTL support).
        /// </summary>
        public System.Windows.FlowDirection CurrentFlowDirection =>
            _currentCulture.TextInfo.IsRightToLeft
                ? System.Windows.FlowDirection.RightToLeft
                : System.Windows.FlowDirection.LeftToRight;

        /// <summary>
        /// Sets the application culture. Use "auto" to detect system locale.
        /// </summary>
        /// <param name="cultureName">Culture code (e.g., "en-US", "fr-FR") or "auto" for system default</param>
        public void SetCulture(string cultureName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cultureName) || cultureName.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    _currentCulture = CultureInfo.CurrentUICulture;
                }
                else
                {
                    _currentCulture = new CultureInfo(cultureName);
                }

                // Apply to current thread
                CultureInfo.CurrentUICulture = _currentCulture;
                CultureInfo.CurrentCulture = _currentCulture;

                // Notify that culture has changed
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (CultureNotFoundException)
            {
                // Fall back to system culture if requested culture not found
                _currentCulture = CultureInfo.CurrentUICulture;
                CultureInfo.CurrentUICulture = _currentCulture;
                CultureInfo.CurrentCulture = _currentCulture;
            }
        }

        /// <summary>
        /// Event raised when the application culture is changed.
        /// ViewModels can subscribe to reload localized strings.
        /// </summary>
        public event EventHandler? CultureChanged;

        /// <summary>
        /// Returns the appropriate plural form based on count.
        /// Supports simple English pluralization rules.
        /// </summary>
        /// <param name="count">The count to evaluate</param>
        /// <param name="singularFormat">Format string for singular (e.g., "{0} item")</param>
        /// <param name="pluralFormat">Format string for plural (e.g., "{0} items")</param>
        /// <returns>Formatted string with the appropriate plural form</returns>
        public string GetPlural(int count, string singularFormat, string pluralFormat)
        {
            // For now, simple English rules: 1 is singular, everything else is plural
            // This can be extended for other languages with more complex rules
            string format = count == 1 ? singularFormat : pluralFormat;
            return string.Format(_currentCulture, format, count);
        }

        /// <summary>
        /// Returns the appropriate plural form with resource key lookup.
        /// Useful when singular and plural forms are stored in resource files.
        /// </summary>
        /// <param name="resourceManager">The resource manager to use for lookups</param>
        /// <param name="count">The count to evaluate</param>
        /// <param name="singularKey">Resource key for singular form</param>
        /// <param name="pluralKey">Resource key for plural form</param>
        /// <returns>Formatted string with the appropriate plural form</returns>
        public string GetPluralFromResources(ResourceManager resourceManager, int count, string singularKey, string pluralKey)
        {
            string format = count == 1
                ? resourceManager.GetString(singularKey, _currentCulture) ?? singularKey
                : resourceManager.GetString(pluralKey, _currentCulture) ?? pluralKey;

            return string.Format(_currentCulture, format, count);
        }

        /// <summary>
        /// Gets a list of supported cultures.
        /// Currently only English, but can be expanded as translations are added.
        /// </summary>
        public IEnumerable<CultureInfo> GetSupportedCultures()
        {
            return new[]
            {
                new CultureInfo("en-US") // English (default)
                // Add more cultures here as translations become available
                // new CultureInfo("fr-FR"), // French
                // new CultureInfo("de-DE"), // German
                // new CultureInfo("es-ES"), // Spanish
            };
        }

        /// <summary>
        /// Gets the display name for a culture in its own language.
        /// </summary>
        public string GetCultureDisplayName(CultureInfo culture)
        {
            return culture.NativeName;
        }
    }
}
