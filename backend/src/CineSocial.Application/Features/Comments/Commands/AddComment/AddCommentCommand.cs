using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Comments.Commands.AddComment;

public record AddCommentCommand(
    Guid MovieId,
    string Content,
    Guid? ParentCommentId = null
) : IRequest<Result<CommentResponse>>;

public record CommentResponse
{
    public Guid CommentId { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Guid MovieId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public int Depth { get; init; }
    public bool IsEdited { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? EditedAt { get; init; }
    public int ReplyCount { get; init; }
    public ReactionStats Reactions { get; init; } = new();
}

public record ReactionStats
{
    public int Upvotes { get; init; }
    public int Downvotes { get; init; }
    public int Score { get; init; }
    public string? UserReaction { get; init; } // "Upvote", "Downvote", or null
}
