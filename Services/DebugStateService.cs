using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LoadOrderKeeper.Coordinators;
using LoadOrderKeeper.Models;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Service for capturing and exporting debug state of the application.
    /// </summary>
    public static class DebugStateService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Captures the current application state and serializes it to JSON.
        /// Paths and message texts are sanitized to replace user-specific information with placeholders.
        /// Works even when configuration is invalid; sections that cannot be captured are reported
        /// in <see cref="DebugStateModel.CaptureWarnings"/> instead of failing the whole export.
        /// </summary>
        /// <param name="changeList">
        /// Any diff result. Context lines and separators are dropped, so a trimmed or a full diff
        /// produces the same report.
        /// </param>
        public static async Task<string> CaptureDebugStateAsync(
            AppConfigModel config,
            IReadOnlyList<DiffLineModel> changeList,
            ConfigurationCoordinator? configCoordinator = null,
            StatusCoordinator? statusCoordinator = null)
        {
            // Get validation result if coordinator provided
            ValidationResult validationResult;
            if (configCoordinator != null)
            {
                validationResult = configCoordinator.GetValidationResult();
            }
            else
            {
                // Fallback to basic validation if no coordinator provided
                validationResult = config.IsValid() 
                    ? ValidationResult.Success() 
                    : ValidationResult.Failed("Configuration validation not available");
            }

            var warnings = new List<string>();
            var changes = BuildChangeEntries(changeList);
            string activeProfileId = config.ActiveProfileId ?? "default";

            var debugState = new DebugStateModel
            {
                ApplicationVersion = VersionService.GetApplicationVersion(),
                Environment = new DebugStateModel.EnvironmentState
                {
                    OperatingSystem = RuntimeInformation.OSDescription,
                    Framework = RuntimeInformation.FrameworkDescription,
                    OsArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    PreferredLanguage = config.PreferredLanguage,
                    ActiveCulture = ViewTexts.LocalizationService.Instance.CurrentCulture
                },
                Configuration = new DebugStateModel.ConfigurationState
                {
                    AppDataPath = SanitizePath(config.StarfieldAppDataPath),
                    GamePath = SanitizePath(config.StarfieldGamePath),
                    ActiveProfileId = config.ActiveProfileId,
                    IsValid = validationResult.IsValid,
                    ValidationError = validationResult.ErrorMessage is null ? null : SanitizeText(validationResult.ErrorMessage),
                    CustomEditorPath = string.IsNullOrWhiteSpace(config.CustomEditorPath) ? null : SanitizePath(config.CustomEditorPath)
                },
                Steam = new DebugStateModel.SteamState
                {
                    IsInstalled = SettingsService.IsStarfieldInstalledViaSteam(),
                    IsRunning = SettingsService.IsSteamRunning()
                },
                Profiles = await CaptureSectionAsync(warnings, "Profiles", async () =>
                    (await ProfileService.LoadProfilesAsync(config).ConfigureAwait(false))
                        .Select(p => new DebugStateModel.ProfileEntry
                        {
                            Id = p.Id,
                            Label = p.Label,
                            IsActive = string.Equals(p.Id, activeProfileId, StringComparison.OrdinalIgnoreCase)
                        })
                        .ToList(), new List<DebugStateModel.ProfileEntry>()),
                PluginsTxt = await CaptureSectionAsync(warnings, "PluginsTxt",
                    () => ReadFileStateAsync(config.GetPluginsFilePath()), new DebugStateModel.FileState()),
                Reference = await CaptureSectionAsync(warnings, "Reference",
                    () => ReadFileStateAsync(config.GetReferenceFilePath()), new DebugStateModel.FileState()),
                ReferenceHistory = await CaptureSectionAsync(warnings, "ReferenceHistory", async () =>
                    (await ReferenceHistoryService.LoadVersionHistoryAsync(config).ConfigureAwait(false))
                        .Select(v => new DebugStateModel.VersionHistoryEntry
                        {
                            VersionNumber = v.VersionNumber,
                            Timestamp = v.Timestamp,
                            AddedModCount = v.AddedMods.Count,
                            RemovedModCount = v.RemovedMods.Count
                        })
                        .ToList(), new List<DebugStateModel.VersionHistoryEntry>()),
                TotalChangesDetected = changes.Count,
                TotalDependentChanges = changes.Sum(c => c.DependentChanges?.Count ?? 0),
                PluginsTxtContents = await ReadFileContentsSafeAsync(config.GetPluginsFilePath()),
                ReferenceContents = await ReadFileContentsSafeAsync(config.GetReferenceFilePath()),
                ChangeList = changes,
                StatusMessages = statusCoordinator?.GetAllMessages()
                    .Select(m => new DebugStateModel.StatusEntry
                    {
                        Timestamp = m.Timestamp,
                        Type = m.Type,
                        Message = SanitizeText(m.Message)
                    })
                    .ToList() ?? new List<DebugStateModel.StatusEntry>(),
                CaptureWarnings = warnings
            };

            return JsonSerializer.Serialize(debugState, JsonOptions);
        }

        /// <summary>
        /// Converts a diff result into report entries, keeping only real changes.
        /// Unchanged context lines and separators are not changes and would inflate the count.
        /// </summary>
        internal static List<DebugStateModel.ChangeEntry> BuildChangeEntries(IEnumerable<DiffLineModel> lines)
        {
            return lines
                .Where(l => l.ChangeType != DiffChangeType.Unchanged && l.ChangeType != DiffChangeType.Separator)
                .Select(ToChangeEntry)
                .ToList();
        }

        private static DebugStateModel.ChangeEntry ToChangeEntry(DiffLineModel line)
        {
            return new DebugStateModel.ChangeEntry
            {
                ChangeType = line.ChangeType,
                FileName = line.FileName,
                ReplacementFileName = line.ReplacementFileName,
                ReferenceNumber = line.ReferenceNumber,
                CurrentNumber = line.CurrentNumber,
                DependentChanges = line.DependentChanges.Count > 0
                    ? line.DependentChanges.Select(ToChangeEntry).ToList()
                    : null
            };
        }

        private static async Task<T> CaptureSectionAsync<T>(List<string> warnings, string section, Func<Task<T>> capture, T fallback)
        {
            try
            {
                return await capture().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                warnings.Add($"{section}: {SanitizeText(ex.Message)}");
                return fallback;
            }
        }

        /// <summary>
        /// Reads size, timestamp, encoding validity, BOM and line-ending style of a file.
        /// These are the facts that explain "the diff looks wrong" reports caused by file format.
        /// </summary>
        internal static async Task<DebugStateModel.FileState> ReadFileStateAsync(string filePath)
        {
            var state = new DebugStateModel.FileState();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return state;
            }

            var info = new FileInfo(filePath);
            state.Exists = true;
            state.SizeBytes = info.Length;
            state.LastWriteTimeUtc = info.LastWriteTimeUtc;

            byte[] bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            state.HasUtf8Bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;

            try
            {
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
                state.IsValidUtf8 = true;
            }
            catch (DecoderFallbackException)
            {
                state.IsValidUtf8 = false;
            }

            state.LineEndings = DetectLineEndings(bytes);
            return state;
        }

        private static string DetectLineEndings(byte[] bytes)
        {
            int crlf = 0;
            int lf = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0x0A)
                {
                    continue;
                }

                if (i > 0 && bytes[i - 1] == 0x0D)
                {
                    crlf++;
                }
                else
                {
                    lf++;
                }
            }

            return (crlf, lf) switch
            {
                (0, 0) => "None",
                (> 0, 0) => "CRLF",
                (0, > 0) => "LF",
                _ => "Mixed"
            };
        }

        /// <summary>
        /// Sanitizes file paths by replacing user-specific segments with placeholders.
        /// Replaces user profile path (C:\Users\username) with %USERPROFILE%.
        /// </summary>
        private static string SanitizePath(string path) => SanitizeText(path);

        /// <summary>
        /// Replaces every occurrence of the user profile path in free text (status messages,
        /// exception messages) with %USERPROFILE%.
        /// </summary>
        internal static string SanitizeText(string text)
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

            return text.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads file contents as a list of lines, or returns empty list if file doesn't exist or cannot be read.
        /// Safe version that never throws exceptions.
        /// </summary>
        private static async Task<List<string>> ReadFileContentsSafeAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new List<string>();
            }

            if (!File.Exists(filePath))
            {
                return new List<string>();
            }

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
                return lines.ToList();
            }
            catch
            {
                // If reading fails, return empty list
                return new List<string>();
            }
        }
    }
}
