using CineSocial.Domain.Common;
using CineSocial.Domain.Entities.User;
using CineSocial.Domain.Enums;

namespace CineSocial.Domain.Entities.Social;

public class Reaction : BaseEntity
{
    // User who created the reaction
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    // Comment this reaction is attached to
    public Guid CommentId { get; set; }
    public Comment Comment { get; set; } = null!;

    // Reaction type (Upvote or Downvote)
    public ReactionType Type { get; set; }
}
