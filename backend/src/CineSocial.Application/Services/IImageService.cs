using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using Microsoft.AspNetCore.Http;

namespace CineSocial.Application.Services;

/// <summary>
/// Service for handling image uploads, validation, and storage.
/// Supports profile images, background images, and future features like forum posts.
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Validates and saves an uploaded image file to the database.
    /// </summary>
    /// <param name="file">The uploaded image file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the saved Image entity</returns>
    Task<Result<Image>> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an image by its ID from the database.
    /// </summary>
    /// <param name="imageId">The unique identifier of the image</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the Image entity</returns>
    Task<Result<Image>> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an image from the database.
    /// </summary>
    /// <param name="imageId">The unique identifier of the image to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an uploaded image file (format, size, content type).
    /// </summary>
    /// <param name="file">The uploaded image file to validate</param>
    /// <returns>Result indicating if validation passed or failed</returns>
    Result ValidateImage(IFormFile file);
}
