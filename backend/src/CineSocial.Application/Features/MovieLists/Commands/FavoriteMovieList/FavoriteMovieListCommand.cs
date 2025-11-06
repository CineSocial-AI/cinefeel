using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.FavoriteMovieList;

public record FavoriteMovieListCommand(Guid MovieListId) : IRequest<Result>;
