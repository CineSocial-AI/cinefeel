using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Services;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Features.Users.Commands.UploadBackgroundImage;

public class UploadBackgroundImageCommandHandler : IRequestHandler<UploadBackgroundImageCommand, Result<UploadBackgroundImageResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadBackgroundImageCommandHandler> _logger;

    public UploadBackgroundImageCommandHandler(
        IUnitOfWork unitOfWork,
        IImageService imageService,
        ICurrentUserService currentUserService,
        ILogger<UploadBackgroundImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UploadBackgroundImageResponse>> Handle(
        UploadBackgroundImageCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Result.Failure<UploadBackgroundImageResponse>(Error.Unauthorized(
                "User.Unauthorized",
                "User is not authenticated."));
        }

        // Get current user
        var user = await _unitOfWork.Repository<AppUser>()
            .Query()
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

        if (user == null)
        {
            return Result.Failure<UploadBackgroundImageResponse>(Error.NotFound(
                "User.NotFound",
                "User not found."));
        }

        // Save the new background image
        var saveImageResult = await _imageService.SaveImageAsync(request.File, cancellationToken);
        if (saveImageResult.IsFailure)
        {
            return Result.Failure<UploadBackgroundImageResponse>(saveImageResult.Error);
        }

        var newImage = saveImageResult.Value;

        // Delete old background image if exists
        if (user.BackgroundImageId.HasValue)
        {
            _logger.LogInformation("Deleting old background image {OldImageId} for user {UserId}",
                user.BackgroundImageId.Value, currentUserId);

            await _imageService.DeleteImageAsync(user.BackgroundImageId.Value, cancellationToken);
        }

        // Update user's background image reference
        user.BackgroundImageId = newImage.Id;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<AppUser>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} uploaded new background image {ImageId}",
            currentUserId, newImage.Id);

        return Result.Success(new UploadBackgroundImageResponse(
            newImage.Id,
            "Background image uploaded successfully."));
    }
}
