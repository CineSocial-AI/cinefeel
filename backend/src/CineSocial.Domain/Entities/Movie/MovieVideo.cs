namespace CineSocial.Domain.Entities.Movie;

public class MovieVideo
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public string VideoKey { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Site { get; set; }
    public string? Type { get; set; }
    public bool Official { get; set; } = false;
}
