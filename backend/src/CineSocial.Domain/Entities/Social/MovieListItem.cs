using CineSocial.Domain.Common;
using CineSocial.Domain.Entities.Movie;

namespace CineSocial.Domain.Entities.Social;

public class MovieListItem : BaseEntity
{
    public Guid MovieListId { get; set; }
    public Guid MovieId { get; set; }
    public int Order { get; set; } = 0;

    // Navigation properties
    public virtual MovieList MovieList { get; set; } = null!;
    public virtual MovieEntity Movie { get; set; } = null!;
}
