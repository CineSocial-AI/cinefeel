namespace CineSocial.Domain.Entities.Movie;

public class ProductionCompany
{
    public int Id { get; set; }
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoPath { get; set; }
    public string? OriginCountry { get; set; }

    // Navigation properties
    public ICollection<MovieProductionCompany> MovieProductionCompanies { get; set; } = new List<MovieProductionCompany>();
}
