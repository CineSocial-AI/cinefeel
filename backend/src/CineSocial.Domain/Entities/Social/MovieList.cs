using CineSocial.Domain.Common;

namespace CineSocial.Domain.Entities.Social;

public class MovieList : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CoverImageId { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsWatchlist { get; set; } = false;
    public int FavoriteCount { get; set; } = 0;

    // Navigation properties
    public virtual User.AppUser User { get; set; } = null!;
    public virtual ICollection<MovieListItem> Items { get; set; } = new List<MovieListItem>();
    public virtual ICollection<MovieListFavorite> Favorites { get; set; } = new List<MovieListFavorite>();
}
