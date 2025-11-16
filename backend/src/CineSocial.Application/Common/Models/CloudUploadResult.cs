namespace CineSocial.Application.Common.Models;

/// <summary>
/// Result of a cloud storage upload operation
/// </summary>
public class CloudUploadResult
{
    /// <summary>
    /// Public URL of the uploaded file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific public ID (used for deletion)
    /// </summary>
    public string PublicId { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Content type (e.g., image/jpeg)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
