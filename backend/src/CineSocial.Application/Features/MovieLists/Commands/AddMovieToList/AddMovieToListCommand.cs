using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.AddMovieToList;

public record AddMovieToListCommand(
    Guid MovieListId,
    Guid MovieId
) : IRequest<Result>;
