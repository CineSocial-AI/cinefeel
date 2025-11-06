using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.RemoveMovieFromList;

public record RemoveMovieFromListCommand(
    Guid MovieListId,
    Guid MovieId
) : IRequest<Result>;
