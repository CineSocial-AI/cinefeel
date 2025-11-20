namespace CineSocial.Application.Features.Movies.Queries.GetMovieRecommendations;

public class MovieRecommendationDto
{
    public Guid Id { get; set; }
    public int TmdbId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Overview { get; set; }
    public string? PosterPath { get; set; }
    public string? BackdropPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public double? VoteAverage { get; set; }
    public double? Popularity { get; set; }
    public double SimilarityScore { get; set; }
    public List<string> Genres { get; set; } = new();
}
