using CineSocial.Application.Common.Interfaces;
using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Comments.Commands.AddComment;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Features.Comments.Queries.GetMovieComments;

public class GetMovieCommentsQueryHandler : IRequestHandler<GetMovieCommentsQuery, PagedResult<List<CommentWithRepliesDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public GetMovieCommentsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<List<CommentWithRepliesDto>>> Handle(GetMovieCommentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;

            // Check if movie exists
            var movieExists = await _unitOfWork.Repository<MovieEntity>()
                .Query()
                .AnyAsync(m => m.Id == request.MovieId, cancellationToken);

            if (!movieExists)
            {
                return PagedResult<List<CommentWithRepliesDto>>.Failure(Error.NotFound(
                    "Comment.MovieNotFound",
                    $"Movie with ID {request.MovieId} not found"
                ));
            }

            // Get root comments (Depth = 0)
            var query = _unitOfWork.Repository<Comment>()
                .Query()
                .Include(c => c.User)
                .Include(c => c.Reactions)
                .Where(c =>
                    c.CommentableId == request.MovieId &&
                    c.CommentableType == CommentableType.Movie &&
                    c.Depth == 0);

        // Apply sorting
        query = request.SortBy switch
        {
            CommentSortBy.Oldest => query.OrderBy(c => c.CreatedAt),
            CommentSortBy.MostUpvoted => query.OrderByDescending(c => c.Reactions.Count(r => r.Type == ReactionType.Upvote)),
            CommentSortBy.MostReplies => query.OrderByDescending(c => c.Replies.Count),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var rootComments = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Get all reply IDs for these root comments
        var rootCommentIds = rootComments.Select(c => c.Id).ToList();

        // Load all nested replies (recursively)
        var allReplies = await LoadAllRepliesAsync(rootCommentIds, cancellationToken);

            // Map to DTOs
            var result = rootComments.Select(c => MapToDto(c, allReplies, userId)).ToList();

            return PagedResult<List<CommentWithRepliesDto>>.Success(
                result,
                request.Page,
                request.PageSize,
                totalCount
            );
        }
        catch (OperationCanceledException)
        {
            return PagedResult<List<CommentWithRepliesDto>>.Failure(Error.Failure(
                "Comment.RequestCancelled",
                "Request was cancelled"
            ));
        }
    }

    private async Task<List<Comment>> LoadAllRepliesAsync(List<Guid> parentCommentIds, CancellationToken cancellationToken)
    {
        if (!parentCommentIds.Any())
            return new List<Comment>();

        var replies = await _unitOfWork.Repository<Comment>()
            .Query()
            .Include(c => c.User)
            .Include(c => c.Reactions)
            .Where(c => parentCommentIds.Contains(c.ParentCommentId!.Value))
            .OrderBy(c => c.CreatedAt)
            .Take(100) // Limit total replies loaded
            .ToListAsync(cancellationToken);

        if (!replies.Any())
            return replies;

        // Recursively load nested replies
        var nestedParentIds = replies.Select(r => r.Id).ToList();
        var nestedReplies = await LoadAllRepliesAsync(nestedParentIds, cancellationToken);

        replies.AddRange(nestedReplies);
        return replies;
    }

    private CommentWithRepliesDto MapToDto(Comment comment, List<Comment> allReplies, Guid? currentUserId)
    {
        var directReplies = allReplies.Where(r => r.ParentCommentId == comment.Id).ToList();

        var upvotes = comment.Reactions.Count(r => r.Type == ReactionType.Upvote);
        var downvotes = comment.Reactions.Count(r => r.Type == ReactionType.Downvote);
        var userReaction = currentUserId.HasValue
            ? comment.Reactions.FirstOrDefault(r => r.UserId == currentUserId.Value)?.Type.ToString()
            : null;

        return new CommentWithRepliesDto
        {
            CommentId = comment.Id,
            UserId = comment.UserId,
            Username = comment.User.Username,
            Content = comment.Content,
            Depth = comment.Depth,
            IsEdited = comment.IsEdited,
            CreatedAt = comment.CreatedAt,
            EditedAt = comment.EditedAt,
            Reactions = new ReactionStats
            {
                Upvotes = upvotes,
                Downvotes = downvotes,
                Score = upvotes - downvotes,
                UserReaction = userReaction
            },
            ReplyCount = directReplies.Count,
            Replies = directReplies.Select(r => MapToDto(r, allReplies, currentUserId)).ToList()
        };
    }
}
