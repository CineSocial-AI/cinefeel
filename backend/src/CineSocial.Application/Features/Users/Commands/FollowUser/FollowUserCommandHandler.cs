using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Commands.FollowUser;

public class FollowUserCommandHandler : IRequestHandler<FollowUserCommand, Result<FollowResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public FollowUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<FollowResponse>> Handle(FollowUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Cannot follow yourself
            if (userId.Value == request.TargetUserId)
            {
                return Error.Validation("Follow.CannotFollowSelf", "You cannot follow yourself");
            }

            // Check if target user exists
            var targetUserExists = await _unitOfWork.Repository<AppUser>()
                .Query()
                .AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);

            if (!targetUserExists)
            {
                return Error.NotFound("User.NotFound", $"User with ID {request.TargetUserId} not found");
            }

            // Check if already following
            var existingFollow = await _unitOfWork.Repository<Follow>()
                .Query()
                .FirstOrDefaultAsync(f => f.FollowerId == userId.Value && f.FollowingId == request.TargetUserId, cancellationToken);

            if (existingFollow != null)
            {
                return Result.Success(new FollowResponse
                {
                    IsNew = false,
                    Message = "Already following this user"
                });
            }

            // Create follow relationship
            var follow = new Follow
            {
                Id = Guid.NewGuid(),
                FollowerId = userId.Value,
                FollowingId = request.TargetUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Follow>().AddAsync(follow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new FollowResponse
            {
                IsNew = true,
                Message = "Successfully followed user"
            });
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Follow.RequestCancelled", "Request was cancelled");
        }
    }
}
