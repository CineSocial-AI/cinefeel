using CineSocial.Application.Common.Results;
using CineSocial.Domain.Enums;
using MediatR;

namespace CineSocial.Application.Features.Comments.Commands.AddReaction;

public record AddReactionCommand(
    Guid CommentId,
    ReactionType ReactionType
) : IRequest<Result<ReactionResponse>>;

public record ReactionResponse
{
    public Guid ReactionId { get; init; }
    public Guid CommentId { get; init; }
    public Guid UserId { get; init; }
    public string ReactionType { get; init; } = string.Empty;
    public bool IsNew { get; init; }
    public DateTime CreatedAt { get; init; }
}
