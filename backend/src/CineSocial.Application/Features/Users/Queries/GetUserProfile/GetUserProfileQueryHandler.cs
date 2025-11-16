using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetUserProfileQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _unitOfWork.Repository<AppUser>()
                .Query()
                .Include(u => u.Followers)
                .Include(u => u.Following)
                .Include(u => u.ProfileImage)
                .Include(u => u.BackgroundImage)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return Error.NotFound("User.NotFound", $"User with ID {request.UserId} not found");
            }

            var currentUserId = _currentUserService.UserId;

            bool isFollowing = false;
            bool isBlocked = false;

            if (currentUserId.HasValue && currentUserId.Value != request.UserId)
            {
                isFollowing = await _unitOfWork.Repository<Follow>()
                    .Query()
                    .AnyAsync(f => f.FollowerId == currentUserId.Value && f.FollowingId == request.UserId, cancellationToken);

                isBlocked = await _unitOfWork.Repository<Block>()
                    .Query()
                    .AnyAsync(b => b.BlockerId == currentUserId.Value && b.BlockedUserId == request.UserId, cancellationToken);
            }

            var dto = new UserProfileDto
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Bio = user.Bio,
                ProfileImageId = user.ProfileImageId,
                BackgroundImageId = user.BackgroundImageId,
                // Provide direct CloudURLs if available (avoids extra API calls)
                ProfileImageUrl = user.ProfileImage?.CloudUrl,
                BackgroundImageUrl = user.BackgroundImage?.CloudUrl,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsFollowing = isFollowing,
                IsBlocked = isBlocked,
                FollowerCount = user.Followers.Count,
                FollowingCount = user.Following.Count
            };

            return Result.Success(dto);
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("User.RequestCancelled", "Request was cancelled");
        }
    }
}
