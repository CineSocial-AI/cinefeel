using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Comments.Commands.AddReaction;

public class AddReactionCommandHandler : IRequestHandler<AddReactionCommand, Result<ReactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddReactionCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReactionResponse>> Handle(AddReactionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure<ReactionResponse>(Error.Unauthorized(
                "Reaction.Unauthorized",
                "User must be authenticated to add reactions"
            ));
        }

        // Check if comment exists
        var commentExists = await _unitOfWork.Repository<Comment>()
            .Query()
            .AnyAsync(c => c.Id == request.CommentId, cancellationToken);

        if (!commentExists)
        {
            return Result.Failure<ReactionResponse>(Error.NotFound(
                "Reaction.CommentNotFound",
                "Comment not found"
            ));
        }

        // Check if user already reacted to this comment
        var existingReaction = await _unitOfWork.Repository<Reaction>()
            .Query()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CommentId == request.CommentId, cancellationToken);

        bool isNew = false;
        Reaction reaction;

        if (existingReaction != null)
        {
            // Update existing reaction
            existingReaction.Type = request.ReactionType;
            existingReaction.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Reaction>().Update(existingReaction);
            reaction = existingReaction;
        }
        else
        {
            // Create new reaction
            reaction = new Reaction
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                CommentId = request.CommentId,
                Type = request.ReactionType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<Reaction>().AddAsync(reaction, cancellationToken);
            isNew = true;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new ReactionResponse
        {
            ReactionId = reaction.Id,
            CommentId = reaction.CommentId,
            UserId = reaction.UserId,
            ReactionType = reaction.Type.ToString(),
            IsNew = isNew,
            CreatedAt = reaction.CreatedAt
        };

        return Result.Success(response);
    }
}
