using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using System.Threading;

namespace LoadOrderKeeper.Services;

public static class UpdateCheckService
{
    private const string GitHubOwner = "Mistralys";
    private const string GitHubRepo = "starfield-load-order-manager";
    private const string NexusModsUrl = "https://www.nexusmods.com/starfield/mods/15786";
    private const string GitHubReleasesUrl = "https://github.com/Mistralys/starfield-load-order-manager/releases";
    
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    static UpdateCheckService()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "StarfieldLoadOrderKeeper");
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentVersion = VersionService.GetApplicationVersion();
            var cacheInfo = GetCacheInfo();
            
            // Invalidate cache if current version has changed since cache was created
            var isCacheValid = !bypassCache && 
                               cacheInfo.IsValid && 
                               cacheInfo.CachedResult != null &&
                               cacheInfo.CachedResult.CurrentVersion == currentVersion;
            
            // Check cache validity (24 hours) and version match
            if (isCacheValid)
            {
                return cacheInfo.CachedResult!;
            }

            var latestRelease = await FetchLatestReleaseAsync(cancellationToken);

            if (latestRelease == null)
            {
                var result = new UpdateCheckResult(false, currentVersion, null, null);
                SaveToCache(result);
                return result;
            }

            // Parse and compare versions
            var latestVersion = ParseVersion(latestRelease.TagName);
            if (latestVersion == null || latestVersion.IsPreRelease)
            {
                var result = new UpdateCheckResult(false, currentVersion, null, null);
                SaveToCache(result);
                return result;
            }

            var currentParsed = ParseVersion(currentVersion);
            if (currentParsed == null)
            {
                var result = new UpdateCheckResult(false, currentVersion, null, null);
                SaveToCache(result);
                return result;
            }

            bool updateAvailable = IsNewerVersion(latestVersion, currentParsed);
            var updateResult = new UpdateCheckResult(
                updateAvailable,
                currentVersion,
                updateAvailable ? latestVersion.OriginalVersion : null,
                latestRelease.HtmlUrl);

            SaveToCache(updateResult);
            return updateResult;
        }
        catch (OperationCanceledException)
        {
            // Silent cancellation
            return new UpdateCheckResult(false, VersionService.GetApplicationVersion(), null, null);
        }
        catch
        {
            // Silent failure for automatic checks
            return new UpdateCheckResult(false, VersionService.GetApplicationVersion(), null, null);
        }
    }

    public static string GetNexusModsUrl() => NexusModsUrl;
    public static string GetGitHubReleasesUrl() => GitHubReleasesUrl;

    private static async Task<GitHubRelease?> FetchLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
        
        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json);
        
        return release;
    }

    private static SemanticVersion? ParseVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
        {
            return null;
        }

        // Remove 'v' prefix if present
        var cleaned = versionString.TrimStart('v', 'V');
        
        // Split by '-' to separate pre-release suffix
        var parts = cleaned.Split('-', 2);
        var versionPart = parts[0];
        var preReleasePart = parts.Length > 1 ? parts[1] : null;

        // Parse semantic version (Major.Minor.Patch)
        var versionComponents = versionPart.Split('.');
        if (versionComponents.Length < 3)
        {
            return null;
        }

        if (!int.TryParse(versionComponents[0], out int major) ||
            !int.TryParse(versionComponents[1], out int minor) ||
            !int.TryParse(versionComponents[2], out int patch))
        {
            return null;
        }

        return new SemanticVersion(major, minor, patch, preReleasePart, versionString);
    }

    private static bool IsNewerVersion(SemanticVersion latest, SemanticVersion current)
    {
        if (latest.Major > current.Major) return true;
        if (latest.Major < current.Major) return false;

        if (latest.Minor > current.Minor) return true;
        if (latest.Minor < current.Minor) return false;

        if (latest.Patch > current.Patch) return true;

        return false;
    }

    private static CacheInfo GetCacheInfo()
    {
        var cacheFilePath = GetCacheFilePath();
        if (!File.Exists(cacheFilePath))
        {
            return new CacheInfo(false, null);
        }

        try
        {
            var json = File.ReadAllText(cacheFilePath);
            var cache = JsonSerializer.Deserialize<CachedUpdateCheck>(json);
            
            if (cache == null)
            {
                return new CacheInfo(false, null);
            }

            var age = DateTime.UtcNow - cache.Timestamp;
            if (age.TotalHours >= 24)
            {
                return new CacheInfo(false, null);
            }

            return new CacheInfo(true, cache.Result);
        }
        catch
        {
            return new CacheInfo(false, null);
        }
    }

    private static void SaveToCache(UpdateCheckResult result)
    {
        try
        {
            var cache = new CachedUpdateCheck
            {
                Timestamp = DateTime.UtcNow,
                Result = result
            };

            var json = JsonSerializer.Serialize(cache, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var cacheFilePath = GetCacheFilePath();
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(cacheFilePath, json);
        }
        catch
        {
            // Silent failure on cache write
        }
    }

    private static string GetCacheFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "StarfieldLoadOrderKeeper");
        return Path.Combine(appFolder, "update-check-cache.json");
    }

    private sealed record SemanticVersion(int Major, int Minor, int Patch, string? PreRelease, string OriginalVersion)
    {
        public bool IsPreRelease => !string.IsNullOrWhiteSpace(PreRelease);
    }

    private sealed record CacheInfo(bool IsValid, UpdateCheckResult? CachedResult);

    private sealed class CachedUpdateCheck
    {
        public DateTime Timestamp { get; set; }
        public UpdateCheckResult? Result { get; set; }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("prerelease")]
        public bool PreRelease { get; set; }
    }
}
