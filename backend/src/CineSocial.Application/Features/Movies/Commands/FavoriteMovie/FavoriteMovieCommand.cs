using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.Movies.Commands.FavoriteMovie;

public record FavoriteMovieCommand(Guid MovieId) : IRequest<Result>;
