namespace LoadOrderKeeper.Models;

/// <summary>
/// Represents a load order profile with its metadata.
/// </summary>
public sealed class ProfileModel
{
    /// <summary>
    /// The unique identifier for this profile, derived from the folder name.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The user-facing label for this profile.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for this profile.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the default profile.
    /// </summary>
    public bool IsDefault => Id == "default";

    /// <summary>
    /// Creates a new profile model.
    /// </summary>
    public ProfileModel()
    {
    }

    /// <summary>
    /// Creates a new profile model with the specified properties.
    /// </summary>
    public ProfileModel(string id, string label, string description = "")
    {
        Id = id;
        Label = label;
        Description = description;
    }

    /// <summary>
    /// Creates the default profile instance.
    /// </summary>
    /// <param name="label">Localized label for the default profile.</param>
    /// <param name="description">Localized description for the default profile.</param>
    public static ProfileModel CreateDefault(string label = "Default", string description = "The default profile is always available.")
    {
        return new ProfileModel("default", label, description);
    }

    
}
