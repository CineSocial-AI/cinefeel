using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Services;
using CineSocial.Domain.Entities.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CineSocial.Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImageService> _logger;

    // Configuration constants
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png" };

    public ImageService(IUnitOfWork unitOfWork, ILogger<ImageService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Result ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Result.Failure(Error.Validation("Image.Empty", "No file was uploaded."));
        }

        // Check file size
        if (file.Length > MaxFileSizeBytes)
        {
            return Result.Failure(Error.Validation(
                "Image.TooLarge",
                $"File size exceeds the maximum limit of {MaxFileSizeBytes / 1024 / 1024} MB."));
        }

        // Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result.Failure(Error.Validation(
                "Image.InvalidFormat",
                $"File format '{extension}' is not supported. Allowed formats: {string.Join(", ", AllowedExtensions)}."));
        }

        // Check content type
        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return Result.Failure(Error.Validation(
                "Image.InvalidContentType",
                $"Content type '{file.ContentType}' is not supported. Allowed types: {string.Join(", ", AllowedContentTypes)}."));
        }

        return Result.Success();
    }

    public async Task<Result<Image>> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        // Validate the image first
        var validationResult = ValidateImage(file);
        if (validationResult.IsFailure)
        {
            return Result.Failure<Image>(validationResult.Error);
        }

        try
        {
            // Read file data into byte array
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            var imageData = memoryStream.ToArray();

            // Create Image entity
            var image = new Image
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                Data = imageData,
                Size = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            // Save to database
            await _unitOfWork.Repository<Image>().AddAsync(image, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Image {ImageId} saved successfully. Size: {Size} bytes, FileName: {FileName}",
                image.Id, image.Size, image.FileName);

            return Result.Success(image);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving image {FileName}", file.FileName);
            return Result.Failure<Image>(Error.Failure(
                "Image.SaveFailed",
                "An error occurred while saving the image."));
        }
    }

    public async Task<Result<Image>> GetImageByIdAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _unitOfWork.Repository<Image>().GetByIdAsync(imageId, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving image {ImageId}", imageId);
            return Result.Failure<Image>(Error.Failure(
                "Image.RetrievalFailed",
                "An error occurred while retrieving the image."));
        }
    }

    public async Task<Result> DeleteImageAsync(Guid imageId, CancellationToken cancellationToken = default)
    {
        try
        {
            var imageResult = await _unitOfWork.Repository<Image>().GetByIdAsync(imageId, cancellationToken);

            if (imageResult.IsFailure)
            {
                return Result.Failure(imageResult.Error);
            }

            // Soft delete the image
            var deleteResult = _unitOfWork.Repository<Image>().Delete(imageResult.Value);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Image {ImageId} deleted successfully", imageId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", imageId);
            return Result.Failure(Error.Failure(
                "Image.DeleteFailed",
                "An error occurred while deleting the image."));
        }
    }
}
