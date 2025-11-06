using CineSocial.Domain.Common;

namespace CineSocial.Domain.Entities.User;

public class Follow : BaseEntity
{
    public Guid FollowerId { get; set; }
    public AppUser Follower { get; set; } = null!;

    public Guid FollowingId { get; set; }
    public AppUser Following { get; set; } = null!;
}
