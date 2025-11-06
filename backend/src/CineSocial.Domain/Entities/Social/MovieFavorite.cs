using CineSocial.Domain.Common;
using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.User;

namespace CineSocial.Domain.Entities.Social;

public class MovieFavorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MovieId { get; set; }

    // Navigation properties
    public virtual AppUser User { get; set; } = null!;
    public virtual MovieEntity Movie { get; set; } = null!;
}
