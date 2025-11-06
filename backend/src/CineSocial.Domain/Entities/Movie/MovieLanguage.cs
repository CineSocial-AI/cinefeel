namespace CineSocial.Domain.Entities.Movie;

public class MovieLanguage
{
    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public int LanguageId { get; set; }
    public Language Language { get; set; } = null!;
}
