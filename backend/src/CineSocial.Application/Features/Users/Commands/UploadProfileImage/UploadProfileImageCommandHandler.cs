using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Services;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CineSocial.Application.Features.Users.Commands.UploadProfileImage;

public class UploadProfileImageCommandHandler : IRequestHandler<UploadProfileImageCommand, Result<UploadProfileImageResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageService _imageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadProfileImageCommandHandler> _logger;

    public UploadProfileImageCommandHandler(
        IUnitOfWork unitOfWork,
        IImageService imageService,
        ICurrentUserService currentUserService,
        ILogger<UploadProfileImageCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _imageService = imageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UploadProfileImageResponse>> Handle(
        UploadProfileImageCommand request,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Result.Failure<UploadProfileImageResponse>(Error.Unauthorized(
                "User.Unauthorized",
                "User is not authenticated."));
        }

        // Get current user
        var user = await _unitOfWork.Repository<AppUser>()
            .Query()
            .FirstOrDefaultAsync(u => u.Id == currentUserId.Value, cancellationToken);

        if (user == null)
        {
            return Result.Failure<UploadProfileImageResponse>(Error.NotFound(
                "User.NotFound",
                "User not found."));
        }

        // Save the new profile image
        var saveImageResult = await _imageService.SaveImageAsync(request.File, cancellationToken);
        if (saveImageResult.IsFailure)
        {
            return Result.Failure<UploadProfileImageResponse>(saveImageResult.Error);
        }

        var newImage = saveImageResult.Value;

        // Delete old profile image if exists
        if (user.ProfileImageId.HasValue)
        {
            _logger.LogInformation("Deleting old profile image {OldImageId} for user {UserId}",
                user.ProfileImageId.Value, currentUserId);

            await _imageService.DeleteImageAsync(user.ProfileImageId.Value, cancellationToken);
        }

        // Update user's profile image reference
        user.ProfileImageId = newImage.Id;
        user.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<AppUser>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} uploaded new profile image {ImageId}",
            currentUserId, newImage.Id);

        return Result.Success(new UploadProfileImageResponse(
            newImage.Id,
            "Profile image uploaded successfully."));
    }
}
