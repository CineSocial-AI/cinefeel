using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Commands.UnfavoriteMovie;

public record UnfavoriteMovieCommand(Guid MovieId) : IRequest<Result>;
