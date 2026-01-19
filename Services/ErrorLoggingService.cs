using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Service for logging unhandled exceptions and application state to disk.
    /// </summary>
    public static class ErrorLoggingService
    {
        private const string ErrorLogFileName = "error.log";
        private static readonly object FileLock = new();
        
        /// <summary>
        /// Gets the path to the error log file in the application data folder.
        /// </summary>
        public static string GetErrorLogPath()
        {
            return Path.Combine(SettingsService.GetConfigFolderPath(), ErrorLogFileName);
        }

        /// <summary>
        /// Initializes the error log file by clearing any previous content.
        /// Should be called once on application startup.
        /// </summary>
        public static void InitializeErrorLog()
        {
            try
            {
                var logPath = GetErrorLogPath();
                var directory = Path.GetDirectoryName(logPath);
                
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                lock (FileLock)
                {
                    File.WriteAllText(logPath, string.Empty, Encoding.UTF8);
                }
            }
            catch
            {
                // Silently fail - we don't want initialization to crash the app
            }
        }

        /// <summary>
        /// Logs an exception with full application state to the error log file.
        /// Returns true if logging was successful, false otherwise.
        /// </summary>
        public static async Task<bool> LogExceptionAsync(Exception exception, AppConfigModel? config, IReadOnlyList<DiffLineModel>? changeList)
        {
            try
            {
                var logPath = GetErrorLogPath();
                var sb = new StringBuilder();

                // Header
                sb.AppendLine("================================================================================");
                sb.AppendLine($"ERROR OCCURRED AT: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("================================================================================");
                sb.AppendLine();

                // Exception Details
                sb.AppendLine("EXCEPTION DETAILS:");
                sb.AppendLine($"Type: {exception.GetType().FullName}");
                sb.AppendLine($"Message: {SanitizeText(exception.Message)}");
                sb.AppendLine();
                sb.AppendLine("Stack Trace:");
                sb.AppendLine(SanitizeText(exception.StackTrace ?? "(No stack trace available)"));
                sb.AppendLine();

                // Inner exception if present
                if (exception.InnerException != null)
                {
                    sb.AppendLine("Inner Exception:");
                    sb.AppendLine($"Type: {exception.InnerException.GetType().FullName}");
                    sb.AppendLine($"Message: {SanitizeText(exception.InnerException.Message)}");
                    sb.AppendLine($"Stack Trace: {SanitizeText(exception.InnerException.StackTrace ?? "(No stack trace available)")}");
                    sb.AppendLine();
                }

                // Application State (if available)
                if (config != null && changeList != null)
                {
                    try
                    {
                        var debugState = await DebugStateService.CaptureDebugStateAsync(config, changeList);
                        sb.AppendLine("APPLICATION STATE (JSON):");
                        sb.AppendLine(debugState);
                        sb.AppendLine();
                    }
                    catch (Exception debugEx)
                    {
                        sb.AppendLine("APPLICATION STATE:");
                        sb.AppendLine($"(Failed to capture debug state: {SanitizeText(debugEx.Message)})");
                        sb.AppendLine();
                    }
                }
                else
                {
                    sb.AppendLine("APPLICATION STATE:");
                    sb.AppendLine("(Not available - application may be in early initialization)");
                    sb.AppendLine();
                }

                // Write to file (without lock to avoid await inside lock)
                var logContent = sb.ToString();
                await File.AppendAllTextAsync(logPath, logContent, Encoding.UTF8);

                return true;
            }
            catch
            {
                // Failed to log - return false but don't throw (avoid recursive exceptions)
                return false;
            }
        }

        /// <summary>
        /// Sanitizes text by replacing user-specific paths with placeholders.
        /// Replaces user profile path (C:\Users\username) with %USERPROFILE%.
        /// </summary>
        private static string SanitizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            // Get the user profile path (e.g., C:\Users\username)
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
            if (string.IsNullOrWhiteSpace(userProfile))
            {
                return text;
            }

            // Replace with placeholder (case-insensitive)
            return text.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }
    }
}
