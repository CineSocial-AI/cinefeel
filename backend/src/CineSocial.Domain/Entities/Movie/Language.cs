namespace CineSocial.Domain.Entities.Movie;

public class Language
{
    public int Id { get; set; }
    public string Iso6391 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? EnglishName { get; set; }

    // Navigation properties
    public ICollection<MovieLanguage> MovieLanguages { get; set; } = new List<MovieLanguage>();
}
