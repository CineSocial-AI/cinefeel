namespace CineSocial.Domain.Entities.Movie;

public class Keyword
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<MovieKeyword> MovieKeywords { get; set; } = new List<MovieKeyword>();
}
