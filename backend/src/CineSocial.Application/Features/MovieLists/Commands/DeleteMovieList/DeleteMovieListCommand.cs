using CineSocial.Application.Common.Results;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Commands.DeleteMovieList;

public record DeleteMovieListCommand(Guid MovieListId) : IRequest<Result>;
