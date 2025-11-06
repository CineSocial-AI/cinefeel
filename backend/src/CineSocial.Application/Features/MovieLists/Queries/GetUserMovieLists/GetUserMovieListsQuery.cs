using CineSocial.Application.Common.Results;
using CineSocial.Application.Features.MovieLists.Queries.GetMyMovieLists;
using MediatR;

namespace CineSocial.Application.Features.MovieLists.Queries.GetUserMovieLists;

public record GetUserMovieListsQuery(Guid UserId) : IRequest<Result<List<MovieListSummaryDto>>>;
