namespace CineSocial.Domain.Entities.Movie;

public class Collection
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }

    // Navigation properties
    public ICollection<MovieCollection> MovieCollections { get; set; } = new List<MovieCollection>();
}
