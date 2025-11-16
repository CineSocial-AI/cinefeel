using CineSocial.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace CineSocial.Application.Common.Interfaces;

/// <summary>
/// Generic interface for cloud storage providers (Cloudinary, Cloudflare R2, AWS S3, etc.)
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Uploads a file to cloud storage
    /// </summary>
    /// <param name="file">The file to upload</param>
    /// <param name="folder">Optional folder path in cloud storage</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with URL and public ID</returns>
    Task<CloudUploadResult> UploadImageAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from cloud storage
    /// </summary>
    /// <param name="publicId">Provider-specific public ID of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion succeeded</returns>
    Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the public URL for a file (if not already stored)
    /// </summary>
    /// <param name="publicId">Provider-specific public ID of the file</param>
    /// <returns>Public URL</returns>
    string GetPublicUrl(string publicId);
}
