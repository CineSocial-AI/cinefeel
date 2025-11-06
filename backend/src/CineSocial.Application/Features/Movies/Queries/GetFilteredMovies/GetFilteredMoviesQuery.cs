using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.Movies.Queries.GetMovies;
using MediatR;

namespace CineSocial.Application.Features.Movies.Queries.GetFilteredMovies;

public record GetFilteredMoviesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    int? GenreId = null,
    int? Year = null,
    string? Decade = null, // "1940s", "1950s", etc.
    int? YearFrom = null,
    int? YearTo = null,
    double? MinRating = null,
    double? MaxRating = null,
    string? Language = null,
    MovieSortBy SortBy = MovieSortBy.Popularity
) : IRequest<PagedResult<List<MovieDto>>>;
