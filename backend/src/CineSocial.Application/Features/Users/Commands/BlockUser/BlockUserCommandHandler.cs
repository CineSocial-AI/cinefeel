using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Commands.BlockUser;

public class BlockUserCommandHandler : IRequestHandler<BlockUserCommand, Result<BlockResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public BlockUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<BlockResponse>> Handle(BlockUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            // Cannot block yourself
            if (userId.Value == request.TargetUserId)
            {
                return Error.Validation("Block.CannotBlockSelf", "You cannot block yourself");
            }

            // Check if target user exists
            var targetUserExists = await _unitOfWork.Repository<AppUser>()
                .Query()
                .AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);

            if (!targetUserExists)
            {
                return Error.NotFound("User.NotFound", $"User with ID {request.TargetUserId} not found");
            }

            // Check if already blocked
            var existingBlock = await _unitOfWork.Repository<Block>()
                .Query()
                .FirstOrDefaultAsync(b => b.BlockerId == userId.Value && b.BlockedUserId == request.TargetUserId, cancellationToken);

            if (existingBlock != null)
            {
                return Result.Success(new BlockResponse
                {
                    IsNew = false,
                    Message = "Already blocked this user"
                });
            }

            // Remove follow relationships if they exist
            var followRelationships = await _unitOfWork.Repository<Follow>()
                .Query()
                .Where(f => (f.FollowerId == userId.Value && f.FollowingId == request.TargetUserId) ||
                           (f.FollowerId == request.TargetUserId && f.FollowingId == userId.Value))
                .ToListAsync(cancellationToken);

            foreach (var follow in followRelationships)
            {
                _unitOfWork.Repository<Follow>().HardDelete(follow);
            }

            // Create block relationship
            var block = new Block
            {
                Id = Guid.NewGuid(),
                BlockerId = userId.Value,
                BlockedUserId = request.TargetUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Block>().AddAsync(block);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new BlockResponse
            {
                IsNew = true,
                Message = "Successfully blocked user"
            });
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Block.RequestCancelled", "Request was cancelled");
        }
    }
}
