namespace CineSocial.Domain.Entities.Movie;

public class MovieCast
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string? Character { get; set; }
    public int? CastOrder { get; set; }
}
