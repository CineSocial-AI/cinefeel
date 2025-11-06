using CineSocial.Domain.Common;

namespace CineSocial.Domain.Entities.User;

public class Block : BaseEntity
{
    public Guid BlockerId { get; set; }
    public AppUser Blocker { get; set; } = null!;

    public Guid BlockedUserId { get; set; }
    public AppUser BlockedUser { get; set; } = null!;
}
