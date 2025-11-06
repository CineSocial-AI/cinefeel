using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.CreateMovieList;

public record CreateMovieListCommand(
    string Name,
    string? Description,
    bool IsPublic
) : IRequest<Result<MovieListResponse>>;

public record MovieListResponse
{
    public Guid MovieListId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public bool IsWatchlist { get; init; }
    public DateTime CreatedAt { get; init; }
}
