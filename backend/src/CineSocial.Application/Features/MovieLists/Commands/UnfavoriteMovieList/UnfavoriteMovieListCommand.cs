using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.UnfavoriteMovieList;

public record UnfavoriteMovieListCommand(Guid MovieListId) : IRequest<Result>;
