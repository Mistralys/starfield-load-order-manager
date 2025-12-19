using System.IO;

namespace LoadOrderKeeper.Models
{
    public class AppConfigModel
    {
        public string StarfieldAppDataPath { get; set; } = string.Empty;
        public string StarfieldGamePath { get; set; } = string.Empty;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(StarfieldAppDataPath) ||
                string.IsNullOrWhiteSpace(StarfieldGamePath))
            {
                return false;
            }

            return Directory.Exists(StarfieldAppDataPath) &&
                   Directory.Exists(StarfieldGamePath) &&
                   Directory.Exists(Path.Combine(StarfieldGamePath, "Data"));
        }

        public string GetPluginsFilePath() => Path.Combine(StarfieldAppDataPath, "Plugins.txt");
        public string GetReferenceFilePath() => Path.Combine(StarfieldAppDataPath, "Plugins.reference.txt");
    }
}
