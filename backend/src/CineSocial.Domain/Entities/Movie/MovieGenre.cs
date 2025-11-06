namespace CineSocial.Domain.Entities.Movie;

public class MovieGenre
{
    public Guid MovieId { get; set; }
    public MovieEntity Movie { get; set; } = null!;

    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
}
