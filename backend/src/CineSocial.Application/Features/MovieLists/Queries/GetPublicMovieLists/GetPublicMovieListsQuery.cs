using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Queries.GetPublicMovieLists;

public record GetPublicMovieListsQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<List<PublicMovieListDto>>>;

public record PublicMovieListDto
{
    public Guid MovieListId { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int MovieCount { get; init; }
    public int FavoriteCount { get; init; }
    public bool IsFavorited { get; init; }
    public DateTime CreatedAt { get; init; }
}
