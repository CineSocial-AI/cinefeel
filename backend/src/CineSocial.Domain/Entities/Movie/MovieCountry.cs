namespace CineSocial.Domain.Entities.Movie;

public class MovieCountry
{
    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public int CountryId { get; set; }
    public Country Country { get; set; } = null!;
}
