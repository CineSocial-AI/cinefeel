using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Comments.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<CommentResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddCommentCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CommentResponse>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Result.Failure<CommentResponse>(Error.Unauthorized(
                "Comment.Unauthorized",
                "User must be authenticated to add comments"
            ));
        }

        // Check if movie exists
        var movieExists = await _unitOfWork.Repository<MovieEntity>()
            .Query()
            .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

        if (!movieExists)
        {
            return Result.Failure<CommentResponse>(Error.NotFound(
                "Comment.MovieNotFound",
                $"Movie with ID {request.MovieId} not found"
            ));
        }

        int depth = 0;
        Guid? parentCommentId = null;

        // If this is a reply, validate parent comment
        if (request.ParentCommentId.HasValue)
        {
            var parentComment = await _unitOfWork.Repository<Comment>()
                .Query()
                .FirstOrDefaultAsync(c =>
                    c.Id == request.ParentCommentId.Value &&
                    c.CommentableId == request.MovieId &&
                    c.CommentableType == CommentableType.Movie,
                    cancellationToken);

            if (parentComment == null)
            {
                return Result.Failure<CommentResponse>(Error.NotFound(
                    "Comment.ParentNotFound",
                    "Parent comment not found"
                ));
            }

            depth = parentComment.Depth + 1;
            parentCommentId = parentComment.Id;

            // Optional: Limit nesting depth
            if (depth > 10)
            {
                return Result.Failure<CommentResponse>(Error.Validation(
                    "Comment.MaxDepthExceeded",
                    "Maximum comment nesting depth exceeded"
                ));
            }
        }

        // Create comment
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = userId.Value,
            Content = request.Content,
            CommentableType = CommentableType.Movie,
            CommentableId = request.MovieId,
            ParentCommentId = parentCommentId,
            Depth = depth,
            IsEdited = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Comment>().AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get user info
        var user = await _unitOfWork.Repository<Domain.Entities.User.AppUser>()
            .Query()
            .FirstAsync(u => u.Id == userId.Value, cancellationToken);

        var response = new CommentResponse
        {
            CommentId = comment.Id,
            UserId = comment.UserId,
            Username = user.Username,
            Content = comment.Content,
            MovieId = request.MovieId,
            ParentCommentId = comment.ParentCommentId,
            Depth = comment.Depth,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            ReplyCount = 0,
            Reactions = new ReactionStats
            {
                Upvotes = 0,
                Downvotes = 0,
                Score = 0,
                UserReaction = null
            }
        };

        return Result.Success(response);
    }
}
