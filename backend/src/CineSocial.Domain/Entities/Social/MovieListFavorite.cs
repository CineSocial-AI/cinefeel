using CineSocial.Domain.Common;
using CineSocial.Domain.Entities.User;

namespace CineSocial.Domain.Entities.Social;

public class MovieListFavorite : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid MovieListId { get; set; }
    public virtual AppUser User { get; set; } = null!;
    public virtual MovieList MovieList { get; set; } = null!;
}