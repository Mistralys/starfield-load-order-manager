using System;
using System.Linq;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Provides centralized application version information.
    /// </summary>
    public static class VersionService
    {
        /// <summary>
        /// Gets the application version from assembly attributes.
        /// Returns clean semantic version without commit hash (e.g., "1.1.0" instead of "1.1.0+abc123").
        /// </summary>
        /// <returns>The version string, preferring InformationalVersion over AssemblyVersion.</returns>
        public static string GetApplicationVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // Try to get the InformationalVersion first (this contains the original Git tag)
                var informationalVersionAttribute = assembly
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .FirstOrDefault() as System.Reflection.AssemblyInformationalVersionAttribute;
                
                if (informationalVersionAttribute?.InformationalVersion is not null && 
                    !string.IsNullOrWhiteSpace(informationalVersionAttribute.InformationalVersion))
                {
                    var version = informationalVersionAttribute.InformationalVersion;
                    
                    // Remove commit hash if present (format: "1.1.0+abc123" -> "1.1.0")
                    var plusIndex = version.IndexOf('+');
                    if (plusIndex >= 0)
                    {
                        version = version.Substring(0, plusIndex);
                    }
                    
                    return version;
                }
                
                // Fallback to AssemblyVersion
                var assemblyVersion = assembly.GetName().Version;
                if (assemblyVersion != null)
                {
                    return assemblyVersion.ToString();
                }
                
                // Last resort fallback
                return "Unknown";
            }
            catch
            {
                // If anything goes wrong, return a safe fallback
                return "Unknown";
            }
        }
    }
}