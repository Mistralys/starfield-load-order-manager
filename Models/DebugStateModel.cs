using System.Collections.Generic;

namespace LoadOrderKeeper.Models
{
    /// <summary>
    /// Represents the complete debug state of the application for troubleshooting purposes.
    /// </summary>
    public sealed class DebugStateModel
    {
        public string ApplicationVersion { get; set; } = string.Empty;
        public ConfigurationState Configuration { get; set; } = new();
        public SteamState Steam { get; set; } = new();
        public int TotalChangesDetected { get; set; }
        public List<string> PluginsTxtContents { get; set; } = new();
        public List<string> ReferenceContents { get; set; } = new();
        public List<DiffLineModel> ChangeList { get; set; } = new();
        public List<StatusMessageModel> StatusMessages { get; set; } = new();

        public sealed class ConfigurationState
        {
            public string AppDataPath { get; set; } = string.Empty;
            public string GamePath { get; set; } = string.Empty;
            public string? ActiveProfileId { get; set; }
        }

        public sealed class SteamState
        {
            public bool IsInstalled { get; set; }
            public bool IsRunning { get; set; }
        }
    }
}
