namespace CineSocial.Domain.Entities.Movie;

public class MovieImage
{
    public Guid Id { get; set; }

    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;
    public string? ImageType { get; set; }
    public string? Language { get; set; }
    public double? VoteAverage { get; set; }
    public int? VoteCount { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
