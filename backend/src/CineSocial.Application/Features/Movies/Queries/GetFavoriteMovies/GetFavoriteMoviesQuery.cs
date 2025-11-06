using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Queries.GetFavoriteMovies;

public record GetFavoriteMoviesQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<List<FavoriteMovieDto>>>;

public record FavoriteMovieDto
{
    public Guid MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public double? VoteAverage { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public DateTime FavoritedAt { get; init; }
}
