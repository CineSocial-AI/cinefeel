using CineSocial.Domain.Common;
using CineSocial.Domain.Enums;

namespace CineSocial.Domain.Entities.User;

public class Image : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Legacy: Image data stored in database (nullable for backward compatibility)
    /// Only used when StorageProvider = Database
    /// </summary>
    public byte[]? Data { get; set; }

    public long Size { get; set; }

    /// <summary>
    /// Cloud storage URL (Cloudinary, Cloudflare R2, etc.)
    /// </summary>
    public string? CloudUrl { get; set; }

    /// <summary>
    /// Provider-specific public ID (used for deletion)
    /// </summary>
    public string? CloudPublicId { get; set; }

    /// <summary>
    /// Storage provider type (Database, Cloudinary, CloudflareR2)
    /// </summary>
    public StorageProvider Provider { get; set; } = StorageProvider.Database;
}
