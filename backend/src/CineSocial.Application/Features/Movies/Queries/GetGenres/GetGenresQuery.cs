using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Queries.GetGenres;

public record GetGenresQuery() : IRequest<Result<List<GenreDto>>>;

public record GenreDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
