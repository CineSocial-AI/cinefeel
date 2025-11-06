namespace CineSocial.Domain.Entities.Movie;

public class MovieCrew
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public string? Job { get; set; }
    public string? Department { get; set; }
}
