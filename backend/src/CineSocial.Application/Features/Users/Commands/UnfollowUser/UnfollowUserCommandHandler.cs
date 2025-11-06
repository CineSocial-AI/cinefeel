using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Commands.UnfollowUser;

public class UnfollowUserCommandHandler : IRequestHandler<UnfollowUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnfollowUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var follow = await _unitOfWork.Repository<Follow>()
                .Query()
                .FirstOrDefaultAsync(f => f.FollowerId == userId.Value && f.FollowingId == request.TargetUserId, cancellationToken);

            if (follow == null)
            {
                return Error.NotFound("Follow.NotFound", "You are not following this user");
            }

            _unitOfWork.Repository<Follow>().HardDelete(follow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Unfollow.RequestCancelled", "Request was cancelled");
        }
    }
}
