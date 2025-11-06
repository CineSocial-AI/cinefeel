namespace CineSocial.Domain.Entities.Movie;

public class Country
{
    public int Id { get; set; }
    public string Iso31661 { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<MovieCountry> MovieCountries { get; set; } = new List<MovieCountry>();
}
