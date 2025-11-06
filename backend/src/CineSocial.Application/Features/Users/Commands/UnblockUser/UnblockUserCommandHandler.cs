using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.User;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Users.Commands.UnblockUser;

public class UnblockUserCommandHandler : IRequestHandler<UnblockUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UnblockUserCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UnblockUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated");
            }

            var block = await _unitOfWork.Repository<Block>()
                .Query()
                .FirstOrDefaultAsync(b => b.BlockerId == userId.Value && b.BlockedUserId == request.TargetUserId, cancellationToken);

            if (block == null)
            {
                return Error.NotFound("Block.NotFound", "You have not blocked this user");
            }

            _unitOfWork.Repository<Block>().HardDelete(block);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return Error.Failure("Unblock.RequestCancelled", "Request was cancelled");
        }
    }
}
