using CineSocial.Domain.Common;
using Pgvector;

namespace CineSocial.Domain.Entities.Movie;

public class MovieEntity : BaseAuditableEntity
{
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public int? Runtime { get; set; }
    public decimal? Budget { get; set; }
    public decimal? Revenue { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public string? ImdbId { get; set; }
    public string? OriginalLanguage { get; set; }
    public double? Popularity { get; set; }
    public double? VoteAverage { get; set; }
    public int? VoteCount { get; set; }
    public string? Status { get; set; }
    public string? Tagline { get; set; }
    public string? Homepage { get; set; }
    public bool Adult { get; set; } = false;
    
    // Vector embedding for content-based recommendations
    public Vector? ContentEmbedding { get; set; }

    // Navigation properties
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<MovieCast> MovieCasts { get; set; } = new List<MovieCast>();
    public ICollection<MovieCrew> MovieCrews { get; set; } = new List<MovieCrew>();
    public ICollection<MovieProductionCompany> MovieProductionCompanies { get; set; } = new List<MovieProductionCompany>();
    public ICollection<MovieCountry> MovieCountries { get; set; } = new List<MovieCountry>();
    public ICollection<MovieLanguage> MovieLanguages { get; set; } = new List<MovieLanguage>();
    public ICollection<MovieKeyword> MovieKeywords { get; set; } = new List<MovieKeyword>();
    public ICollection<MovieVideo> MovieVideos { get; set; } = new List<MovieVideo>();
    public ICollection<MovieImage> MovieImages { get; set; } = new List<MovieImage>();
    public ICollection<MovieCollection> MovieCollections { get; set; } = new List<MovieCollection>();
}
