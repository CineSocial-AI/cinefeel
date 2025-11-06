using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Comments.Commands.RemoveReaction;

public class RemoveReactionCommandHandler : IRequestHandler<RemoveReactionCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveReactionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(RemoveReactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure(Error.Unauthorized(
                "Reaction.Unauthorized",
                "User must be authenticated to remove reactions"
            ));
        }

        var reaction = await _unitOfWork.Repository<Reaction>()
            .Query()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CommentId == request.CommentId, cancellationToken);

        if (reaction == null)
        {
            return Result.Failure(Error.NotFound(
                "Reaction.NotFound",
                "Reaction not found"
            ));
        }

        _unitOfWork.Repository<Reaction>().Delete(reaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
