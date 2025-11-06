using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.UpdateMovieList;

public record UpdateMovieListCommand(
    Guid MovieListId,
    string Name,
    string? Description,
    bool IsPublic
) : IRequest<Result>;
