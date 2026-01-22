using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LoadOrderKeeper.ViewTexts
{
    /// <summary>
    /// Singleton service for loading and managing JSON-based localization.
    /// Provides thread-safe access to translated strings with culture switching support.
    /// </summary>
    public sealed class LocalizationService : ObservableObject
    {
        private static readonly Lazy<LocalizationService> _instance = 
            new Lazy<LocalizationService>(() => new LocalizationService());

        private readonly object _lock = new object();
        private Dictionary<string, Dictionary<string, string>> _translations = new();
        private string _currentCulture = "en-US";
        private readonly string _localesPath;

        /// <summary>
        /// Gets the singleton instance of the LocalizationService.
        /// </summary>
        public static LocalizationService Instance => _instance.Value;

        /// <summary>
        /// Event raised when the current culture changes.
        /// </summary>
        public event EventHandler? CultureChanged;

        /// <summary>
        /// Gets the current culture name (e.g., "en-US", "de-DE", "fr-FR").
        /// </summary>
        public string CurrentCulture
        {
            get => _currentCulture;
            private set => SetProperty(ref _currentCulture, value);
        }

        private LocalizationService()
        {
            // Determine locales path relative to executable
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            _localesPath = Path.Combine(exeDir, "ViewTexts", "Locales");
            
            // Detect system culture and load appropriate language
            var systemCulture = DetectSystemCulture();
            _currentCulture = systemCulture;
            LoadCulture(_currentCulture);
        }

        /// <summary>
        /// Detects the system culture and returns the appropriate supported culture.
        /// Falls back to "en-US" if system culture is not supported.
        /// </summary>
        private string DetectSystemCulture()
        {
            try
            {
                var currentCulture = CultureInfo.CurrentUICulture;
                var cultureName = currentCulture.Name; // e.g., "fr-FR", "de-DE"
                
                // Check if we have a translation for this culture
                var culturePath = Path.Combine(_localesPath, $"{cultureName}.json");
                if (File.Exists(culturePath))
                {
                    return cultureName;
                }
                
                // Try parent culture (e.g., "fr" if "fr-FR" not found)
                if (currentCulture.Parent != null && !string.IsNullOrEmpty(currentCulture.Parent.Name))
                {
                    var parentCulture = currentCulture.Parent.Name;
                    
                    // Build parent culture map dynamically from locale files
                    var parentMap = BuildParentCultureMap();
                    
                    if (parentMap.TryGetValue(parentCulture, out var mappedCulture))
                    {
                        var mappedPath = Path.Combine(_localesPath, $"{mappedCulture}.json");
                        if (File.Exists(mappedPath))
                        {
                            return mappedCulture;
                        }
                    }
                }
                
                // Default to English if no match found
                return "en-US";
            }
            catch
            {
                // If detection fails, default to English
                return "en-US";
            }
        }

        /// <summary>
        /// Gets a translated string from the specified section and key.
        /// </summary>
        /// <param name="section">The JSON section (e.g., "MainWindow", "ErrorDialog")</param>
        /// <param name="key">The string key within the section</param>
        /// <returns>The translated string, or a fallback if not found</returns>
        public string GetString(string section, string key)
        {
            lock (_lock)
            {
                var fullKey = $"{section}.{key}";
                
                if (_translations.TryGetValue(fullKey, out var value))
                {
                    return value.GetValueOrDefault(_currentCulture, $"[{fullKey}]");
                }

                return $"[{fullKey}]";
            }
        }

        /// <summary>
        /// Gets a formatted translated string with arguments.
        /// </summary>
        /// <param name="section">The JSON section</param>
        /// <param name="key">The string key</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The formatted translated string</returns>
        public string GetString(string section, string key, params object[] args)
        {
            var template = GetString(section, key);
            
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                // If format fails, return template with args appended
                return $"{template} [{string.Join(", ", args)}]";
            }
        }

        /// <summary>
        /// Initializes the localization service with application configuration.
        /// Should be called early in application startup after config is loaded.
        /// </summary>
        /// <param name="preferredLanguage">Preferred language from config ("auto" or specific culture like "fr-FR")</param>
        public void InitializeFromConfig(string preferredLanguage)
        {
            if (string.IsNullOrWhiteSpace(preferredLanguage) || preferredLanguage == "auto")
            {
                // Auto-detect already happened in constructor, just ensure it's loaded
                return;
            }

            // Apply specific language preference
            try
            {
                SetCulture(preferredLanguage);
            }
            catch
            {
                // If preferred language fails to load, keep the auto-detected one
            }
        }

        /// <summary>
        /// Changes the current culture and reloads translations.
        /// </summary>
        /// <param name="cultureName">Culture name (e.g., "en-US", "de-DE", "fr-FR")</param>
        public void SetCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                throw new ArgumentException("Culture name cannot be null or empty", nameof(cultureName));
            }

            lock (_lock)
            {
                if (_currentCulture == cultureName)
                {
                    return; // Already set
                }

                LoadCulture(cultureName);
                CurrentCulture = cultureName;
                
                // Notify subscribers
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Loads translations from the JSON file for the specified culture.
        /// </summary>
        private void LoadCulture(string cultureName)
        {
            var filePath = Path.Combine(_localesPath, $"{cultureName}.json");

            if (!File.Exists(filePath))
            {
                // Fallback to en-US if culture file doesn't exist
                if (cultureName != "en-US")
                {
                    filePath = Path.Combine(_localesPath, "en-US.json");
                    if (!File.Exists(filePath))
                    {
                        throw new FileNotFoundException($"Localization file not found: {filePath}");
                    }
                }
                else
                {
                    throw new FileNotFoundException($"Default localization file not found: {filePath}");
                }
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var root = JsonDocument.Parse(json).RootElement;

                var newTranslations = new Dictionary<string, Dictionary<string, string>>();

                // Parse JSON structure: { "Section": { "Key": "Value" } }
                foreach (var section in root.EnumerateObject())
                {
                    var sectionName = section.Name;
                    
                    foreach (var entry in section.Value.EnumerateObject())
                    {
                        var key = entry.Name;
                        var value = entry.Value.GetString() ?? string.Empty;
                        var fullKey = $"{sectionName}.{key}";

                        if (!newTranslations.ContainsKey(fullKey))
                        {
                            newTranslations[fullKey] = new Dictionary<string, string>();
                        }

                        newTranslations[fullKey][cultureName] = value;
                    }
                }

                // Merge with existing translations (for fallback support)
                foreach (var kvp in newTranslations)
                {
                    if (!_translations.ContainsKey(kvp.Key))
                    {
                        _translations[kvp.Key] = new Dictionary<string, string>();
                    }

                    foreach (var cultureKvp in kvp.Value)
                    {
                        _translations[kvp.Key][cultureKvp.Key] = cultureKvp.Value;
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse localization file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Gets the list of available cultures that the application supports.
        /// </summary>
        /// <returns>A list of culture codes (e.g., "en-US", "de-DE", "fr-FR", "es-ES", "it-IT")</returns>
        public IReadOnlyList<string> GetAvailableCultures()
        {
            lock (_lock)
            {
                var cultures = new List<string>();
                
                if (!Directory.Exists(_localesPath))
                {
                    return cultures;
                }
                
                // Find all .json files in the locales directory
                var jsonFiles = Directory.GetFiles(_localesPath, "*.json");
                
                foreach (var file in jsonFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    // Validate that it's a culture code format (e.g., en-US, de-DE)
                    if (!string.IsNullOrWhiteSpace(fileName) && fileName.Contains("-"))
                    {
                        cultures.Add(fileName);
                    }
                }
                
                // Sort alphabetically
                cultures.Sort(StringComparer.Ordinal);
                
                return cultures;
            }
        }

        /// <summary>
        /// Gets the native name for a specific culture from its locale file.
        /// </summary>
        /// <param name="cultureName">Culture code (e.g., "en-US", "de-DE")</param>
        /// <returns>Native name (e.g., "English", "Deutsch") or culture code if not found</returns>
        public string GetLocaleName(string cultureName)
        {
            var filePath = Path.Combine(_localesPath, $"{cultureName}.json");
            
            if (!File.Exists(filePath))
            {
                return cultureName; // Fallback to culture code
            }
            
            try
            {
                var json = File.ReadAllText(filePath);
                var root = JsonDocument.Parse(json).RootElement;
                
                if (root.TryGetProperty("LocaleName", out var localeNameElement))
                {
                    return localeNameElement.GetString() ?? cultureName;
                }
                
                return cultureName; // No LocaleName property found
            }
            catch
            {
                return cultureName; // Error reading file
            }
        }

        /// <summary>
        /// Builds a map of parent cultures to specific locale files by reading all locale files.
        /// </summary>
        /// <returns>Dictionary mapping parent culture codes (e.g., "fr") to specific cultures (e.g., "fr-FR")</returns>
        private Dictionary<string, string> BuildParentCultureMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            if (!Directory.Exists(_localesPath))
            {
                return map;
            }
            
            var jsonFiles = Directory.GetFiles(_localesPath, "*.json");
            
            foreach (var file in jsonFiles)
            {
                try
                {
                    var cultureName = Path.GetFileNameWithoutExtension(file);
                    var json = File.ReadAllText(file);
                    var root = JsonDocument.Parse(json).RootElement;
                    
                    if (root.TryGetProperty("ParentCulture", out var parentElement))
                    {
                        var parentCulture = parentElement.GetString();
                        if (!string.IsNullOrWhiteSpace(parentCulture))
                        {
                            // Map parent culture to this specific culture
                            // e.g., "fr" -> "fr-FR", "de" -> "de-DE"
                            map[parentCulture] = cultureName;
                        }
                    }
                }
                catch
                {
                    // Skip files that can't be parsed
                    continue;
                }
            }
            
            return map;
        }

        /// <summary>
        /// Clears all cached translations (useful for testing).
        /// </summary>
        internal void ClearCache()
        {
            lock (_lock)
            {
                _translations.Clear();
            }
        }

        /// <summary>
        /// Reloads the current culture (useful after JSON file changes).
        /// </summary>
        public void ReloadCurrentCulture()
        {
            lock (_lock)
            {
                var culture = _currentCulture;
                _translations.Clear();
                LoadCulture(culture);
                CultureChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
