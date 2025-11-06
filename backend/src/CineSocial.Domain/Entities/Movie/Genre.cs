namespace CineSocial.Domain.Entities.Movie;

public class Genre
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
