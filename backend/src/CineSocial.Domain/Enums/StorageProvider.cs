namespace CineSocial.Domain.Enums;

/// <summary>
/// Defines the storage provider for image files
/// </summary>
public enum StorageProvider
{
    /// <summary>
    /// Image stored as byte array in database (legacy/fallback)
    /// </summary>
    Database = 1,

    /// <summary>
    /// Image stored in Cloudinary CDN
    /// </summary>
    Cloudinary = 2,

    /// <summary>
    /// Image stored in Cloudflare R2 (future support)
    /// </summary>
    CloudflareR2 = 3
}
