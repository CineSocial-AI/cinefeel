using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CineSocial.Infrastructure.CloudStorage;

/// <summary>
/// Cloudinary implementation of cloud storage provider
/// </summary>
public class CloudinaryProvider : ICloudStorageProvider
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinarySettings _settings;
    private readonly ILogger<CloudinaryProvider> _logger;

    public CloudinaryProvider(
        IOptions<CloudinarySettings> settings,
        ILogger<CloudinaryProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!_settings.IsConfigured())
        {
            throw new InvalidOperationException(
                "Cloudinary is not properly configured. Please check CLOUDINARY_CLOUD_NAME, CLOUDINARY_API_KEY, and CLOUDINARY_API_SECRET environment variables.");
        }

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true; // Always use HTTPS
    }

    public async Task<CloudUploadResult> UploadImageAsync(
        IFormFile file,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder ?? _settings.UploadFolder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                // Optional transformations
                // Transformation = new Transformation().Width(500).Height(500).Crop("limit")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            _logger.LogInformation(
                "Successfully uploaded image to Cloudinary. PublicId: {PublicId}, URL: {Url}",
                uploadResult.PublicId,
                uploadResult.SecureUrl);

            return new CloudUploadResult
            {
                Url = uploadResult.SecureUrl.ToString(),
                PublicId = uploadResult.PublicId,
                Size = uploadResult.Bytes,
                ContentType = file.ContentType,
                FileName = file.FileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image to Cloudinary: {FileName}", file.FileName);
            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                _logger.LogWarning("Attempted to delete image with empty publicId");
                return false;
            }

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deletionParams);

            if (result.Result == "ok")
            {
                _logger.LogInformation("Successfully deleted image from Cloudinary. PublicId: {PublicId}", publicId);
                return true;
            }

            _logger.LogWarning(
                "Failed to delete image from Cloudinary. PublicId: {PublicId}, Result: {Result}",
                publicId,
                result.Result);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image from Cloudinary: {PublicId}", publicId);
            return false;
        }
    }

    public string GetPublicUrl(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return string.Empty;
        }

        // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/image/upload/{public_id}
        return _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
    }
}
