namespace CineSocial.Infrastructure.CloudStorage;

/// <summary>
/// Configuration settings for Cloudinary cloud storage
/// </summary>
public class CloudinarySettings
{
    /// <summary>
    /// Cloudinary cloud name (from dashboard)
    /// </summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>
    /// Cloudinary API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Cloudinary API secret
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Default upload folder in Cloudinary
    /// </summary>
    public string UploadFolder { get; set; } = "cinefeel";

    /// <summary>
    /// Validates that all required settings are configured
    /// </summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(CloudName) &&
               !string.IsNullOrWhiteSpace(ApiKey) &&
               !string.IsNullOrWhiteSpace(ApiSecret);
    }
}
