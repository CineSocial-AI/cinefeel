using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Queries.GetMovies;

public record GetMoviesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    int? GenreId = null,
    int? Year = null,
    MovieSortBy SortBy = MovieSortBy.Popularity
) : IRequest<PagedResult<List<MovieDto>>>;

public record MovieDto
{
    public Guid Id { get; init; }
    public int TmdbId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? OriginalTitle { get; init; }
    public string? Overview { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public string? PosterPath { get; init; }
    public string? BackdropPath { get; init; }
    public double? VoteAverage { get; init; }
    public int? VoteCount { get; init; }
    public double? Popularity { get; init; }
    public List<string> Genres { get; init; } = new();
}
