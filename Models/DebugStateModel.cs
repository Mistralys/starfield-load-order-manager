using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LoadOrderKeeper.Models
{
    /// <summary>
    /// Represents the complete debug state of the application for troubleshooting purposes.
    /// Uses dedicated report types (not UI models) so the exported JSON stays stable,
    /// language-independent and free of view-only properties.
    /// </summary>
    public sealed class DebugStateModel
    {
        public string ApplicationVersion { get; set; } = string.Empty;
        public EnvironmentState Environment { get; set; } = new();
        public ConfigurationState Configuration { get; set; } = new();
        public SteamState Steam { get; set; } = new();
        public List<ProfileEntry> Profiles { get; set; } = new();
        public FileState PluginsTxt { get; set; } = new();
        public FileState Reference { get; set; } = new();
        public List<VersionHistoryEntry> ReferenceHistory { get; set; } = new();

        /// <summary>Number of top-level changes (context lines and separators excluded).</summary>
        public int TotalChangesDetected { get; set; }

        /// <summary>Number of shifted mods grouped beneath the top-level changes.</summary>
        public int TotalDependentChanges { get; set; }

        public List<string> PluginsTxtContents { get; set; } = new();
        public List<string> ReferenceContents { get; set; } = new();
        public List<ChangeEntry> ChangeList { get; set; } = new();
        public List<StatusEntry> StatusMessages { get; set; } = new();

        /// <summary>Report sections that could not be captured, with the reason.</summary>
        public List<string> CaptureWarnings { get; set; } = new();

        public sealed class EnvironmentState
        {
            public string OperatingSystem { get; set; } = string.Empty;
            public string Framework { get; set; } = string.Empty;
            public string OsArchitecture { get; set; } = string.Empty;
            public string ProcessArchitecture { get; set; } = string.Empty;
            public string PreferredLanguage { get; set; } = string.Empty;
            public string ActiveCulture { get; set; } = string.Empty;
        }

        public sealed class ConfigurationState
        {
            public string AppDataPath { get; set; } = string.Empty;
            public string GamePath { get; set; } = string.Empty;
            public string? ActiveProfileId { get; set; }
            public bool IsValid { get; set; }
            public string? ValidationError { get; set; }
            public string? CustomEditorPath { get; set; }
        }

        public sealed class SteamState
        {
            public bool IsInstalled { get; set; }
            public bool IsRunning { get; set; }
        }

        public sealed class ProfileEntry
        {
            public string Id { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        /// <summary>
        /// Physical facts about a mod list file. Surfaces encoding and line-ending problems
        /// that the plain line contents cannot show.
        /// </summary>
        public sealed class FileState
        {
            public bool Exists { get; set; }
            public long SizeBytes { get; set; }
            public DateTime? LastWriteTimeUtc { get; set; }
            public bool IsValidUtf8 { get; set; }
            public bool HasUtf8Bom { get; set; }

            /// <summary>"CRLF", "LF", "Mixed" or "None".</summary>
            public string LineEndings { get; set; } = "None";
        }

        /// <summary>Reference history entry. Counts only: comments and mod names are left out.</summary>
        public sealed class VersionHistoryEntry
        {
            public int VersionNumber { get; set; }
            public DateTime Timestamp { get; set; }
            public int AddedModCount { get; set; }
            public int RemovedModCount { get; set; }
        }

        public sealed class ChangeEntry
        {
            /// <summary>Serialized as the enum name (for example "Replaced").</summary>
            public DiffChangeType ChangeType { get; set; }
            public string FileName { get; set; } = string.Empty;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? ReplacementFileName { get; set; }

            public int? ReferenceNumber { get; set; }
            public int? CurrentNumber { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public List<ChangeEntry>? DependentChanges { get; set; }
        }

        public sealed class StatusEntry
        {
            public DateTime Timestamp { get; set; }
            public StatusMessageType Type { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
