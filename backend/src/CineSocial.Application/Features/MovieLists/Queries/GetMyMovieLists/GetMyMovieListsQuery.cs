using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;

public record GetMyMovieListsQuery() : IRequest<Result<List<MovieListSummaryDto>>>;

public record MovieListSummaryDto
{
    public Guid MovieListId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public bool IsWatchlist { get; init; }
    public int MovieCount { get; init; }
    public int FavoriteCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
