using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Queries.GetMovieListDetail;

public record GetMovieListDetailQuery(Guid MovieListId) : IRequest<Result<MovieListDetailDto>>;

public record MovieListDetailDto
{
    public Guid MovieListId { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsPublic { get; init; }
    public bool IsWatchlist { get; init; }
    public int FavoriteCount { get; init; }
    public bool IsFavorited { get; init; }
    public bool IsOwner { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<MovieInListDto> Movies { get; init; } = new();
}

public record MovieInListDto
{
    public Guid MovieId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? PosterPath { get; init; }
    public double? VoteAverage { get; init; }
    public DateTime? ReleaseDate { get; init; }
    public int Order { get; init; }
}
