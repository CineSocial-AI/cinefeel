using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Social;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Comments.Commands.UpdateComment;

public class UpdateCommentCommandHandler : IRequestHandler<UpdateCommentCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public UpdateCommentCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure(Error.Unauthorized(
                "Comment.Unauthorized",
                "User must be authenticated to update comments"
            ));
        }

        var comment = await _unitOfWork.Repository<Comment>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
        {
            return Result.Failure(Error.NotFound(
                "Comment.NotFound",
                "Comment not found"
            ));
        }

        // Check if user owns the comment
        if (comment.UserId != userId.Value)
        {
            return Result.Failure(Error.Forbidden(
                "Comment.NotOwner",
                "You can only update your own comments"
            ));
        }

        comment.Content = request.Content;
        comment.IsEdited = true;
        comment.EditedAt = DateTime.UtcNow;
        comment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Comment>().Update(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
