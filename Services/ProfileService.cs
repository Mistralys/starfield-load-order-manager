using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LoadOrderKeeper.Models;
using LoadOrderKeeper.ViewTexts;

namespace LoadOrderKeeper.Services;

/// <summary>
/// Manages profile operations including creation, loading, switching, and deletion.
/// </summary>
public static class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Loads all available profiles from the profiles folder.
    /// </summary>
    public static async Task<IReadOnlyList<ProfileModel>> LoadProfilesAsync(AppConfigModel config)
    {
        var profiles = new List<ProfileModel>();
        
        // Always include the default profile first with localized strings
        var localization = ViewTexts.LocalizationService.Instance;
        var defaultLabel = localization.GetString("ManageProfiles", "DefaultProfileLabel");
        var defaultDescription = localization.GetString("ManageProfiles", "DefaultProfileDescription");
        profiles.Add(ProfileModel.CreateDefault(defaultLabel, defaultDescription));

        if (!config.IsValid())
        {
            return profiles;
        }

        var profilesFolder = GetProfilesFolder(config);
        if (!Directory.Exists(profilesFolder))
        {
            return profiles;
        }

        var directories = Directory.GetDirectories(profilesFolder);
        foreach (var dir in directories)
        {
            var profileId = Path.GetFileName(dir);
            
            // Skip the default profile folder (it's already added as virtual)
            if (profileId == "default")
            {
                continue;
            }

            var profileJsonPath = Path.Combine(dir, "profile.json");
            if (!File.Exists(profileJsonPath))
            {
                continue;
            }

            try
            {
                var json = await File.ReadAllTextAsync(profileJsonPath);
                var profileData = JsonSerializer.Deserialize<ProfileJsonData>(json);
                
                if (profileData?.Label != null)
                {
                    profiles.Add(new ProfileModel(profileId, profileData.Label, profileData.Description ?? string.Empty));
                }
            }
            catch
            {
                // Invalid profile, skip silently
            }
        }

        // Sort custom profiles alphabetically by label (default is already first)
        var customProfiles = profiles.Skip(1).OrderBy(p => p.Label, StringComparer.OrdinalIgnoreCase).ToList();
        profiles = new List<ProfileModel> { profiles[0] };
        profiles.AddRange(customProfiles);

        return profiles;
    }

    /// <summary>
    /// Gets the currently active profile from the configuration.
    /// </summary>
    public static async Task<ProfileModel> GetActiveProfileAsync(AppConfigModel config)
    {
        var profiles = await LoadProfilesAsync(config);
        var activeId = config.ActiveProfileId ?? "default";
        
        return profiles.FirstOrDefault(p => p.Id == activeId) ?? ProfileModel.CreateDefault();
    }

    /// <summary>
    /// Creates a new profile with the specified label and description.
    /// </summary>
    public static async Task<ProfileModel> CreateProfileAsync(AppConfigModel config, string label, string description)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        // Ensure Profiles folder exists before creating profile
        EnsureProfilesFolderExists(config);

        var profiles = await LoadProfilesAsync(config);
        var existingIds = profiles.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var profileId = GenerateProfileId(label, existingIds);
        var profileFolder = GetProfileFolder(config, profileId);

        // Create profile folder
        try
        {
            Directory.CreateDirectory(profileFolder);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException($"Access denied when creating profile folder: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to create profile folder: {ex.Message}", ex);
        }

        // Create profile.json
        var profileJsonPath = Path.Combine(profileFolder, "profile.json");
        var profileData = new ProfileJsonData { Label = label, Description = description };
        
        try
        {
            var json = JsonSerializer.Serialize(profileData, JsonOptions);
            await File.WriteAllTextAsync(profileJsonPath, json);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to create profile.json: {ex.Message}", ex);
        }

        // Create main.txt from current Plugins.txt
        await EnsureProfileMainFileAsync(config, profileId);

        // Create reference.txt from main.txt
        await EnsureProfileReferenceFileAsync(config, profileId);

        return new ProfileModel(profileId, label, description);
    }

    /// <summary>
    /// Updates an existing profile's properties.
    /// </summary>
    public static async Task UpdateProfileAsync(AppConfigModel config, ProfileModel oldProfile, string newLabel, string newDescription)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        if (oldProfile.IsDefault)
        {
            throw new InvalidOperationException("Cannot edit the default profile.");
        }

        var profiles = await LoadProfilesAsync(config);
        var existingIds = profiles.Where(p => p.Id != oldProfile.Id).Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        var newProfileId = GenerateProfileId(newLabel, existingIds);
        var needsRename = newProfileId != oldProfile.Id;

        var oldProfileFolder = GetProfileFolder(config, oldProfile.Id);
        var newProfileFolder = GetProfileFolder(config, newProfileId);

        // Update profile.json
        var profileJsonPath = Path.Combine(needsRename ? oldProfileFolder : newProfileFolder, "profile.json");
        var profileData = new ProfileJsonData { Label = newLabel, Description = newDescription };
        
        try
        {
            var json = JsonSerializer.Serialize(profileData, JsonOptions);
            await File.WriteAllTextAsync(profileJsonPath, json);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to update profile.json: {ex.Message}", ex);
        }

        // Rename folder if needed
        if (needsRename)
        {
            try
            {
                if (Directory.Exists(newProfileFolder))
                {
                    throw new IOException("Target profile folder already exists.");
                }
                Directory.Move(oldProfileFolder, newProfileFolder);

                // Update active profile ID if this was the active profile
                if (config.ActiveProfileId == oldProfile.Id)
                {
                    config.ActiveProfileId = newProfileId;
                    await SettingsService.SaveSettingsAsync(config);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to rename profile folder: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Deletes a profile and its folder.
    /// </summary>
    public static async Task DeleteProfileAsync(AppConfigModel config, string profileId)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        if (profileId == "default")
        {
            throw new InvalidOperationException("Cannot delete the default profile.");
        }

        var profileFolder = GetProfileFolder(config, profileId);
        
        try
        {
            if (Directory.Exists(profileFolder))
            {
                Directory.Delete(profileFolder, true);
            }

            // If this was the active profile, switch to default
            if (config.ActiveProfileId == profileId)
            {
                config.ActiveProfileId = "default";
                await SettingsService.SaveSettingsAsync(config);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to delete profile: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Copies a profile to a new profile with a different label.
    /// </summary>
    public static async Task<ProfileModel> CopyProfileAsync(AppConfigModel config, string sourceProfileId, string newLabel)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        // Ensure Profiles folder exists before copying profile
        EnsureProfilesFolderExists(config);

        var profiles = await LoadProfilesAsync(config);
        var sourceProfile = profiles.FirstOrDefault(p => p.Id == sourceProfileId);
        
        if (sourceProfile == null)
        {
            throw new InvalidOperationException("Source profile not found.");
        }

        var existingIds = profiles.Select(p => p.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newProfileId = GenerateProfileId(newLabel, existingIds);

        var sourceFolder = GetProfileFolder(config, sourceProfileId);
        var targetFolder = GetProfileFolder(config, newProfileId);

        try
        {
            // Create target folder
            Directory.CreateDirectory(targetFolder);

            // Copy profile files
            if (sourceProfile.IsDefault)
            {
                // For default profile, ensure files exist first
                await EnsureDefaultProfileFilesAsync(config);
            }

            // Copy main.txt if exists
            var sourceMain = Path.Combine(sourceFolder, "main.txt");
            var targetMain = Path.Combine(targetFolder, "main.txt");
            if (File.Exists(sourceMain))
            {
                File.Copy(sourceMain, targetMain);
            }

            // Copy reference.txt if exists
            var sourceRef = Path.Combine(sourceFolder, "reference.txt");
            var targetRef = Path.Combine(targetFolder, "reference.txt");
            if (File.Exists(sourceRef))
            {
                File.Copy(sourceRef, targetRef);
            }

            // Create profile.json with new label
            var profileJsonPath = Path.Combine(targetFolder, "profile.json");
            var profileData = new ProfileJsonData { Label = newLabel, Description = sourceProfile.Description };
            var json = JsonSerializer.Serialize(profileData, JsonOptions);
            await File.WriteAllTextAsync(profileJsonPath, json);

            return new ProfileModel(newProfileId, newLabel, sourceProfile.Description);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException($"Access denied when copying profile: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to copy profile: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Switches to a different profile.
    /// </summary>
    public static async Task SwitchProfileAsync(AppConfigModel config, string targetProfileId)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        var profiles = await LoadProfilesAsync(config);
        if (!profiles.Any(p => p.Id == targetProfileId))
        {
            throw new InvalidOperationException("Target profile not found.");
        }

        var currentProfileId = config.ActiveProfileId ?? "default";

        // Step 1: Save current Plugins.txt to active profile's main.txt
        var pluginsPath = config.GetPluginsFilePath();
        var currentMainPath = GetProfileMainFilePath(config, currentProfileId);
        
        EnsureProfileFolder(config, currentProfileId);
        
        try
        {
            if (File.Exists(pluginsPath))
            {
                var pluginsContent = await File.ReadAllTextAsync(pluginsPath);
                await File.WriteAllTextAsync(currentMainPath, pluginsContent, new UTF8Encoding(false));
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to backup current profile: {ex.Message}", ex);
        }

        // Step 2: Ensure target profile files exist
        await EnsureProfileMainFileAsync(config, targetProfileId);
        await EnsureProfileReferenceFileAsync(config, targetProfileId);

        // Step 3: Copy target profile's main.txt to Plugins.txt
        var targetMainPath = GetProfileMainFilePath(config, targetProfileId);
        
        try
        {
            var targetContent = await File.ReadAllTextAsync(targetMainPath);
            await File.WriteAllTextAsync(pluginsPath, targetContent, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to switch to target profile: {ex.Message}", ex);
        }

        // Step 4: Update active profile ID
        config.ActiveProfileId = targetProfileId;
        await SettingsService.SaveSettingsAsync(config);
    }

    /// <summary>
    /// Generates a unique profile ID from a label.
    /// </summary>
    public static string GenerateProfileId(string label, ISet<string> existingIds)
    {
        var baseId = TransliterateLabel(label);
        
        if (string.IsNullOrWhiteSpace(baseId))
        {
            baseId = "profile";
        }

        var candidateId = baseId;
        var suffix = 1;

        while (existingIds.Contains(candidateId))
        {
            candidateId = $"{baseId}-{suffix}";
            suffix++;
        }

        return candidateId;
    }

    /// <summary>
    /// Ensures the profile's main.txt file exists.
    /// </summary>
    public static async Task EnsureProfileMainFileAsync(AppConfigModel config, string profileId)
    {
        EnsureProfileFolder(config, profileId);
        
        var mainPath = GetProfileMainFilePath(config, profileId);
        if (!File.Exists(mainPath))
        {
            var pluginsPath = config.GetPluginsFilePath();
            if (File.Exists(pluginsPath))
            {
                var content = await File.ReadAllTextAsync(pluginsPath);
                await File.WriteAllTextAsync(mainPath, content, new UTF8Encoding(false));
            }
            else
            {
                await File.WriteAllTextAsync(mainPath, string.Empty, new UTF8Encoding(false));
            }
        }
    }

    /// <summary>
    /// Ensures the profile's reference.txt file exists.
    /// </summary>
    public static async Task EnsureProfileReferenceFileAsync(AppConfigModel config, string profileId)
    {
        EnsureProfileFolder(config, profileId);
        await EnsureProfileMainFileAsync(config, profileId);
        
        var referencePath = GetProfileReferenceFilePath(config, profileId);
        if (!File.Exists(referencePath))
        {
            var mainPath = GetProfileMainFilePath(config, profileId);
            var content = await File.ReadAllTextAsync(mainPath);
            await File.WriteAllTextAsync(referencePath, content, new UTF8Encoding(false));
        }
    }

    /// <summary>
    /// Ensures the default profile files exist.
    /// </summary>
    public static async Task EnsureDefaultProfileFilesAsync(AppConfigModel config)
    {
        if (!config.IsValid())
        {
            return;
        }

        await EnsureProfileMainFileAsync(config, "default");
        await EnsureProfileReferenceFileAsync(config, "default");
    }

    /// <summary>
    /// Ensures the Profiles folder exists and is writable.
    /// Throws IOException with actionable message if folder cannot be created or accessed.
    /// </summary>
    public static void EnsureProfilesFolderExists(AppConfigModel config)
    {
        if (!config.IsValid())
        {
            throw new InvalidOperationException("Configuration is not valid.");
        }

        var profilesFolder = GetProfilesFolder(config);
        
        try
        {
            if (!Directory.Exists(profilesFolder))
            {
                Directory.CreateDirectory(profilesFolder);
            }
            
            // Verify writability by creating a test file
            var testFile = Path.Combine(profilesFolder, $".test_{Guid.NewGuid():N}");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException(
                $"Access denied when creating Profiles folder at '{profilesFolder}'. " +
                "Please check folder permissions or choose a different app data path.",
                ex);
        }
        catch (Exception ex)
        {
            throw new IOException(
                $"Failed to create or access Profiles folder at '{profilesFolder}': {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Gets the path to the profiles folder.
    /// </summary>
    public static string GetProfilesFolder(AppConfigModel config)
    {
        return Path.Combine(config.StarfieldAppDataPath, "Profiles");
    }

    /// <summary>
    /// Gets the path to a specific profile folder.
    /// </summary>
    public static string GetProfileFolder(AppConfigModel config, string profileId)
    {
        return Path.Combine(GetProfilesFolder(config), profileId);
    }

    /// <summary>
    /// Gets the path to a profile's main.txt file.
    /// </summary>
    public static string GetProfileMainFilePath(AppConfigModel config, string profileId)
    {
        return Path.Combine(GetProfileFolder(config, profileId), "main.txt");
    }

    /// <summary>
    /// Gets the path to a profile's reference.txt file.
    /// </summary>
    public static string GetProfileReferenceFilePath(AppConfigModel config, string profileId)
    {
        return Path.Combine(GetProfileFolder(config, profileId), "reference.txt");
    }

    private static void EnsureProfileFolder(AppConfigModel config, string profileId)
    {
        // Ensure Profiles folder exists first
        EnsureProfilesFolderExists(config);
        
        var profileFolder = GetProfileFolder(config, profileId);
        if (!Directory.Exists(profileFolder))
        {
            try
            {
                Directory.CreateDirectory(profileFolder);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IOException(
                    $"Access denied when creating profile folder at '{profileFolder}'. " +
                    "Please check folder permissions.",
                    ex);
            }
            catch (Exception ex)
            {
                throw new IOException(
                    $"Failed to create profile folder at '{profileFolder}': {ex.Message}",
                    ex);
            }
        }
    }

    private static string TransliterateLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.Empty;
        }

        // Normalize to decomposed form (separate base characters from diacritics)
        var normalized = label.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            // Skip non-spacing marks (diacritics)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        // Convert back to composed form and process
        var decomposed = sb.ToString().Normalize(NormalizationForm.FormC);
        sb.Clear();

        var lastWasSeparator = true; // Start true to avoid leading dash

        foreach (var c in decomposed)
        {
            if (char.IsLetterOrDigit(c) && c < 128) // ASCII letters and digits only
            {
                sb.Append(char.ToLowerInvariant(c));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c)))
            {
                sb.Append('-');
                lastWasSeparator = true;
            }
        }

        // Remove trailing dash if present
        var result = sb.ToString().TrimEnd('-');
        return result;
    }

    private class ProfileJsonData
    {
        public string? Label { get; set; }
        public string? Description { get; set; }
    }
}
