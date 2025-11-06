using CineSocial.Domain.Common;

namespace CineSocial.Domain.Entities.Movie;

public class Person : BaseEntity
{
    public int TmdbId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Deathday { get; set; }
    public string? PlaceOfBirth { get; set; }
    public string? ProfilePath { get; set; }
    public double? Popularity { get; set; }
    public int? Gender { get; set; }
    public string? KnownForDepartment { get; set; }
    public string? ImdbId { get; set; }

    // Navigation properties
    public ICollection<MovieCast> MovieCasts { get; set; } = new List<MovieCast>();
    public ICollection<MovieCrew> MovieCrews { get; set; } = new List<MovieCrew>();
}
