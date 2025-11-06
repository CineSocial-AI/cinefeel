using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Comments.Commands.AddComment;
using MediatR;

namespace CineSocial.Application.Features.Comments.Queries.GetMovieComments;

public record GetMovieCommentsQuery(
    Guid MovieId,
    int Page = 1,
    int PageSize = 20,
    CommentSortBy SortBy = CommentSortBy.Newest
) : IRequest<PagedResult<List<CommentWithRepliesDto>>>;

public enum CommentSortBy
{
    Newest = 1,
    Oldest = 2,
    MostUpvoted = 3,
    MostReplies = 4
}

public record CommentWithRepliesDto
{
    public Guid CommentId { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public int Depth { get; init; }
    public bool IsEdited { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public ReactionStats Reactions { get; init; } = new();
    public int ReplyCount { get; init; }
    public List<CommentWithRepliesDto> Replies { get; init; } = new();
}
